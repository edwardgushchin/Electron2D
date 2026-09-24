using System.Diagnostics;
using System.Globalization;

namespace Electron2D;

public sealed partial class Input
{
    private readonly Dictionary<int, JoypadState> _connectedJoypads = [];
    private readonly Dictionary<int, VibrationState> _joyVibrations = [];
    private readonly Dictionary<string, string> _joyMappings = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _removedJoyMappings = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<uint> _ignoredJoypadIDs = ReadIgnoredJoypads();
    private bool _ignoreJoypadOnUnfocusedApplication;

    /// <summary>Occurs after a native controller connects or disconnects and its state is committed.</summary>
    /// <remarks>Raised synchronously on the display owner thread during event delivery.</remarks>
    public event Action<int, bool>? JoyConnectionChanged;

    /// <summary>Gets a sorted snapshot of connected native controller IDs.</summary>
    /// <returns>Caller-owned IDs, or an empty array when no native controller is connected.</returns>
    public int[] GetConnectedJoypads()
    {
        lock (_gate) return _connectedJoypads.Keys.Order().ToArray();
    }

    /// <summary>Gets the mapped name of a native controller.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>The name, or an empty string for an absent device.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public string GetJoyName(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _connectedJoypads.GetValueOrDefault(device)?.Name ?? string.Empty;
    }

    /// <summary>Gets the SDL-compatible identifier of a native controller.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>The hexadecimal GUID, or an empty string for an absent device.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public string GetJoyGUID(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _connectedJoypads.GetValueOrDefault(device)?.GUID ?? string.Empty;
    }

    /// <summary>Gets extra typed native information for a connected controller.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>Information for the device, or null when absent.</returns>
    /// <remarks>Raw name, USB IDs and optional serial are available. Platform-specific Steam Input and XInput indices are not exposed by this runtime projection.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public JoypadInfo? GetJoyInfo(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _connectedJoypads.TryGetValue(device, out var joypad) ? joypad.Info : null;
    }

    /// <summary>Returns whether the controller has a standardized gamepad mapping.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>True for a mapped native controller.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public bool IsJoyKnown(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _connectedJoypads.GetValueOrDefault(device)?.Known ?? false;
    }

    /// <summary>Returns whether the connected controller supports vibration.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>True when the native backend reports rumble support.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public bool HasJoyVibration(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _connectedJoypads.GetValueOrDefault(device)?.Vibration ?? false;
    }

    /// <summary>Returns whether the connected controller has a controllable LED.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>True when the native backend reports LED support.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public bool HasJoyLight(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _connectedJoypads.GetValueOrDefault(device)?.Light ?? false;
    }

    /// <summary>Reports whether an environment-configured vendor/product pair is ignored by native controller discovery.</summary>
    /// <param name="vendorID">USB vendor identifier.</param>
    /// <param name="productID">USB product identifier.</param>
    /// <returns>True for a pair in SDL_GAMECONTROLLER_IGNORE_DEVICES at input initialization.</returns>
    public bool ShouldIgnoreDevice(int vendorID, int productID)
    {
        var fullID = unchecked((uint)(vendorID << 16) | (ushort)productID);
        return _ignoredJoypadIDs.Contains(fullID);
    }

    /// <summary>Adds an SDL-style controller mapping for future connections.</summary>
    /// <param name="mapping">A GUID, name and binding list separated by commas.</param>
    /// <param name="updateExisting">Whether connected devices with this GUID should adopt the mapping now.</param>
    /// <remarks>The process-wide overlay applies through the native host; a false update flag leaves current devices unchanged until reconnection. The binding grammar beyond the required GUID/name fields is validated by SDL when applied to a device.</remarks>
    /// <exception cref="ArgumentNullException">Mapping is null.</exception>
    /// <exception cref="ArgumentException">The GUID, name or binding section is missing.</exception>
    /// <exception cref="InvalidOperationException">A live display is accessed off its owner thread.</exception>
    public void AddJoyMapping(string mapping, bool updateExisting = false)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        var first = mapping.IndexOf(',');
        var second = first < 0 ? -1 : mapping.IndexOf(',', first + 1);
        if (first <= 0 || second <= first + 1)
            throw new ArgumentException("A gamepad mapping requires a GUID, name and bindings.", nameof(mapping));
        var guid = mapping[..first];
        var display = DisplayServer.Instance;
        display?.EnsureGamepadOwner();
        lock (_gate)
        {
            _joyMappings[guid] = mapping;
            _removedJoyMappings.Remove(guid);
        }
        if (updateExisting) display?.UpdateJoyMapping(guid, mapping);
    }

    /// <summary>Removes a controller mapping and restores raw input for connected matching devices.</summary>
    /// <param name="guid">The SDL-compatible GUID to remove.</param>
    /// <remarks>Removal also suppresses the built-in mapping for this GUID in the current process; raw joystick events remain available.</remarks>
    /// <exception cref="ArgumentException">The GUID is blank.</exception>
    /// <exception cref="InvalidOperationException">A live display is accessed off its owner thread.</exception>
    public void RemoveJoyMapping(string guid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(guid);
        var display = DisplayServer.Instance;
        display?.EnsureGamepadOwner();
        lock (_gate)
        {
            _joyMappings.Remove(guid);
            _removedJoyMappings.Add(guid);
        }
        display?.UpdateJoyMapping(guid, null);
    }

    /// <summary>Starts a controller rumble effect with independent weak and strong motors.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <param name="weakMagnitude">Weak motor strength from zero through one.</param>
    /// <param name="strongMagnitude">Strong motor strength from zero through one.</param>
    /// <param name="duration">Duration in seconds; zero requests the native maximum of about 65 seconds.</param>
    /// <exception cref="ArgumentOutOfRangeException">The device ID, motor strength or duration is invalid.</exception>
    /// <exception cref="InvalidOperationException">A live display is accessed off its owner thread.</exception>
    public void StartJoyVibration(int device, float weakMagnitude, float strongMagnitude, float duration = 0)
    {
        ValidateDevice(device);
        ValidateStrength(weakMagnitude, nameof(weakMagnitude));
        ValidateStrength(strongMagnitude, nameof(strongMagnitude));
        if (!float.IsFinite(duration) || duration < 0) throw new ArgumentOutOfRangeException(nameof(duration));
        var display = DisplayServer.Instance;
        display?.EnsureGamepadOwner();
        if (display?.ShouldIgnoreGamepads() == true) return;
        lock (_gate) _joyVibrations[device] = new VibrationState(new(weakMagnitude, strongMagnitude), duration, Stopwatch.GetTimestamp());
        display?.StartGamepadVibration(device, weakMagnitude, strongMagnitude, duration);
    }

    /// <summary>Stops the controller's current rumble effect.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    /// <exception cref="InvalidOperationException">A live display is accessed off its owner thread.</exception>
    public void StopJoyVibration(int device)
    {
        ValidateDevice(device);
        var display = DisplayServer.Instance;
        display?.EnsureGamepadOwner();
        lock (_gate) _joyVibrations[device] = new VibrationState(Vector2.Zero, 0, Stopwatch.GetTimestamp());
        display?.StopGamepadVibration(device);
    }

    /// <summary>Gets the last requested weak and strong rumble strengths.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>The retained request, or zero when none was made.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public Vector2 GetJoyVibrationStrength(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _joyVibrations.GetValueOrDefault(device).Strength;
    }

    /// <summary>Gets the last requested rumble duration in seconds.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>The retained duration, or zero when none was made.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public float GetJoyVibrationDuration(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _joyVibrations.GetValueOrDefault(device).Duration;
    }

    /// <summary>Estimates the remaining native rumble duration in seconds.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>Zero when the controller is absent, unsupported, stopped or expired.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public float GetJoyVibrationRemainingDuration(int device)
    {
        ValidateDevice(device);
        lock (_gate)
        {
            if (!_connectedJoypads.TryGetValue(device, out var joypad) || !joypad.Vibration ||
                !_joyVibrations.TryGetValue(device, out var vibration) || vibration.Strength == Vector2.Zero)
                return 0;
            var duration = vibration.Duration is 0 or > 65.535f ? 65.535f : vibration.Duration;
            return MathF.Max(0, duration - (float)Stopwatch.GetElapsedTime(vibration.Timestamp).TotalSeconds);
        }
    }

    /// <summary>Returns whether a requested controller rumble effect still has time remaining.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>True while a supported device's effect is active.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public bool IsJoyVibrating(int device) => GetJoyVibrationRemainingDuration(device) > 0;

    /// <summary>Sets a connected controller's LED when its hardware supports it.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <param name="color">Finite color; RGB channels clamp to zero through one.</param>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    /// <exception cref="ArgumentException">The color is not finite.</exception>
    /// <exception cref="InvalidOperationException">A live display is accessed off its owner thread.</exception>
    public void SetJoyLight(int device, Color color)
    {
        ValidateDevice(device);
        if (!color.IsFinite()) throw new ArgumentException("The LED color must be finite.", nameof(color));
        var display = DisplayServer.Instance;
        display?.EnsureGamepadOwner();
        if (display?.ShouldIgnoreGamepads() == true) return;
        display?.SetGamepadLight(device, color);
    }

    /// <summary>Gets or sets whether native controller input and effects are ignored while the application lacks focus.</summary>
    /// <value>False by default.</value>
    /// <remarks>Engine.Run samples <see cref="ProjectSettings.IgnoreJoypadOnUnfocusedApplication"/> before opening the native host. Enabling this while unfocused releases pressed input and stops native rumble; subsequent controller input and effect requests are suppressed until focus returns.</remarks>
    /// <exception cref="InvalidOperationException">The active display policy is changed off its owner thread.</exception>
    public bool IgnoreJoypadOnUnfocusedApplication
    {
        get { lock (_gate) return _ignoreJoypadOnUnfocusedApplication; }
        set
        {
            DisplayServer.Instance?.EnsureGamepadOwner();
            lock (_gate) _ignoreJoypadOnUnfocusedApplication = value;
            if (value) DisplayServer.Instance?.ApplyGamepadFocusPolicy();
        }
    }

    internal void ConnectNativeJoypad(int device, string name, string guid, JoypadInfo info,
        bool known, bool vibration, bool light, bool notify)
    {
        lock (_gate)
        {
            _joyVibrations.Remove(device);
            _connectedJoypads[device] = new JoypadState(name, guid, info, known, vibration, light);
        }
        if (notify) JoyConnectionChanged?.Invoke(device, true);
    }

    internal void EmitNativeJoypadConnection(int device) => JoyConnectionChanged?.Invoke(device, true);

    internal string? GetCustomJoyMapping(string guid)
    {
        lock (_gate) return _joyMappings.GetValueOrDefault(guid);
    }

    internal bool IsJoyMappingRemoved(string guid)
    {
        lock (_gate) return _removedJoyMappings.Contains(guid);
    }

    internal void DisconnectNativeJoypad(int device, bool notify)
    {
        lock (_gate)
        {
            _connectedJoypads.Remove(device);
            _joyVibrations.Remove(device);
            _joyButtonsPressed.RemoveWhere(button => button.Device == device);
            foreach (var axis in _joyAxes.Keys.Where(axis => axis.Device == device).ToArray()) _joyAxes.Remove(axis);
            foreach (var state in _actions.Values)
            {
                var wasPressed = state.Pressed;
                var wasExactPressed = state.ExactPressed;
                foreach (var source in state.Contributions.Keys.Where(source => source.Device == device).ToArray())
                    state.Contributions.Remove(source);
                Recalculate(state);
                if (wasPressed && !state.Pressed)
                { state.ProcessJustReleased = true; state.PhysicsJustReleased = true; state.LastReleasedEventId = 0; }
                if (wasExactPressed && !state.ExactPressed)
                { state.ExactProcessJustReleased = true; state.ExactPhysicsJustReleased = true; state.ExactLastReleasedEventId = 0; }
            }
        }
        if (notify) JoyConnectionChanged?.Invoke(device, false);
    }

    internal void StopNativeJoypadVibration(int device)
    {
        lock (_gate) _joyVibrations.Remove(device);
    }

    private sealed record JoypadState(string Name, string GUID, JoypadInfo Info, bool Known, bool Vibration, bool Light);
    private readonly record struct VibrationState(Vector2 Strength, float Duration, long Timestamp);

    private static void ValidateStrength(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(name);
    }

    private static HashSet<uint> ReadIgnoredJoypads()
    {
        var result = new HashSet<uint>();
        var value = Environment.GetEnvironmentVariable("SDL_GAMECONTROLLER_IGNORE_DEVICES");
        if (string.IsNullOrEmpty(value)) return result;
        foreach (var entry in value.Split(','))
        {
            var parts = entry.Split('/');
            if (parts.Length < 2) continue;
            static bool Hex(string text, out ushort number) => ushort.TryParse(
                text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text[2..] : text,
                NumberStyles.HexNumber, CultureInfo.InvariantCulture, out number);
            if (!Hex(parts[0], out var vendor) || !Hex(parts[1], out var product)) continue;
            vendor = (ushort)((vendor << 8) | (vendor >> 8));
            product = (ushort)((product << 8) | (product >> 8));
            result.Add(((uint)vendor << 16) | product);
        }
        return result;
    }
}
