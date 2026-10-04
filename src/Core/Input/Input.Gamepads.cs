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

    internal event Action<int, bool>? JoyConnectionChangedCore;

    internal int[] GetConnectedJoypadsCore()
    {
        lock (_gate) return _connectedJoypads.Keys.Order().ToArray();
    }

    internal string GetJoyNameCore(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _connectedJoypads.GetValueOrDefault(device)?.Name ?? string.Empty;
    }

    internal string GetJoyGUIDCore(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _connectedJoypads.GetValueOrDefault(device)?.GUID ?? string.Empty;
    }

    internal JoypadInfo? GetJoyInfoCore(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _connectedJoypads.TryGetValue(device, out var joypad) ? joypad.Info : null;
    }

    internal bool IsJoyKnownCore(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _connectedJoypads.GetValueOrDefault(device)?.Known ?? false;
    }

    internal bool HasJoyVibrationCore(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _connectedJoypads.GetValueOrDefault(device)?.Vibration ?? false;
    }

    internal bool HasJoyLightCore(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _connectedJoypads.GetValueOrDefault(device)?.Light ?? false;
    }

    internal bool ShouldIgnoreDeviceCore(int vendorID, int productID)
    {
        var fullID = unchecked((uint)(vendorID << 16) | (ushort)productID);
        return _ignoredJoypadIDs.Contains(fullID);
    }

    internal void AddJoyMappingCore(string mapping, bool updateExisting = false)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        var first = mapping.IndexOf(',');
        var second = first < 0 ? -1 : mapping.IndexOf(',', first + 1);
        if (first <= 0 || second <= first + 1)
            throw new ArgumentException("A gamepad mapping requires a GUID, name and bindings.", nameof(mapping));
        var guid = mapping[..first];
        var display = DisplayServer.Service;
        display?.EnsureGamepadOwner();
        lock (_gate)
        {
            _joyMappings[guid] = mapping;
            _removedJoyMappings.Remove(guid);
        }
        if (updateExisting) display?.UpdateJoyMapping(guid, mapping);
    }

    internal void RemoveJoyMappingCore(string guid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(guid);
        var display = DisplayServer.Service;
        display?.EnsureGamepadOwner();
        lock (_gate)
        {
            _joyMappings.Remove(guid);
            _removedJoyMappings.Add(guid);
        }
        display?.UpdateJoyMapping(guid, null);
    }

    internal void StartJoyVibrationCore(int device, float weakMagnitude, float strongMagnitude, float duration = 0)
    {
        ValidateDevice(device);
        ValidateStrength(weakMagnitude, nameof(weakMagnitude));
        ValidateStrength(strongMagnitude, nameof(strongMagnitude));
        if (!float.IsFinite(duration) || duration < 0) throw new ArgumentOutOfRangeException(nameof(duration));
        var display = DisplayServer.Service;
        display?.EnsureGamepadOwner();
        if (display?.ShouldIgnoreGamepads() == true) return;
        lock (_gate) _joyVibrations[device] = new VibrationState(new(weakMagnitude, strongMagnitude), duration, Stopwatch.GetTimestamp());
        display?.StartGamepadVibration(device, weakMagnitude, strongMagnitude, duration);
    }

    internal void StopJoyVibrationCore(int device)
    {
        ValidateDevice(device);
        var display = DisplayServer.Service;
        display?.EnsureGamepadOwner();
        lock (_gate) _joyVibrations[device] = new VibrationState(Vector2.Zero, 0, Stopwatch.GetTimestamp());
        display?.StopGamepadVibration(device);
    }

    internal Vector2 GetJoyVibrationStrengthCore(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _joyVibrations.GetValueOrDefault(device).Strength;
    }

    internal float GetJoyVibrationDurationCore(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _joyVibrations.GetValueOrDefault(device).Duration;
    }

    internal float GetJoyVibrationRemainingDurationCore(int device)
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

    internal bool IsJoyVibratingCore(int device) => GetJoyVibrationRemainingDurationCore(device) > 0;

    internal void SetJoyLightCore(int device, Color color)
    {
        ValidateDevice(device);
        if (!color.IsFinite()) throw new ArgumentException("The LED color must be finite.", nameof(color));
        var display = DisplayServer.Service;
        display?.EnsureGamepadOwner();
        if (display?.ShouldIgnoreGamepads() == true) return;
        display?.SetGamepadLight(device, color);
    }

    internal bool IgnoreJoypadOnUnfocusedApplicationCore
    {
        get { lock (_gate) return _ignoreJoypadOnUnfocusedApplication; }
        set
        {
            DisplayServer.Service?.EnsureGamepadOwner();
            lock (_gate) _ignoreJoypadOnUnfocusedApplication = value;
            if (value) DisplayServer.Service?.ApplyGamepadFocusPolicy();
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
        if (notify) JoyConnectionChangedCore?.Invoke(device, true);
    }

    internal void EmitNativeJoypadConnection(int device) => JoyConnectionChangedCore?.Invoke(device, true);

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
        if (notify) JoyConnectionChangedCore?.Invoke(device, false);
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
