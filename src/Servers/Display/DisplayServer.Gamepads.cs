using System.Text;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private readonly Dictionary<uint, GamepadDevice> _gamepadsByInstance = [];
    private readonly Dictionary<int, GamepadDevice> _gamepadsByDevice = [];
    private readonly SortedSet<int> _freeGamepadIDs = [];
    private readonly List<int> _pendingGamepadConnections = [];
    private int _nextGamepadID;

    internal void EnsureGamepadOwner() => EnsureOwner();

    private void InitializeGamepads()
    {
        var instances = SDL.GetJoysticks(out _);
        if (instances is null) return;
        foreach (var instance in instances)
        {
            ApplyCustomMapping(instance);
            AddGamepad(instance, initial: true);
        }
    }

    private void AddGamepad(uint instance, bool initial = false, bool refresh = false)
    {
        if (Input.Instance.ShouldIgnoreDevice(SDL.GetJoystickVendorForID(instance), SDL.GetJoystickProductForID(instance))) return;
        var guid = GetGamepadGUID(instance);
        var mapped = SDL.IsGamepad(instance) && !Input.Instance.IsJoyMappingRemoved(guid);
        if (_gamepadsByInstance.TryGetValue(instance, out var prior) && prior.Mapped == mapped && !refresh) return;
        var handle = mapped ? SDL.OpenGamepad(instance) : SDL.OpenJoystick(instance);
        if (handle == 0) return;
        var id = prior is not null ? prior.ID : _freeGamepadIDs.Count == 0 ? _nextGamepadID++ : _freeGamepadIDs.Min;
        if (prior is null && _freeGamepadIDs.Count != 0) _freeGamepadIDs.Remove(id);
        try
        {
            var name = mapped ? SDL.GetGamepadName(handle) ?? string.Empty : SDL.GetJoystickName(handle) ?? string.Empty;
            var rawName = SDL.GetJoystickNameForID(instance) ?? name;
            var info = mapped
                ? new JoypadInfo(rawName, SDL.GetGamepadVendor(handle), SDL.GetGamepadProduct(handle), SDL.GetGamepadSerial(handle))
                : new JoypadInfo(rawName, SDL.GetJoystickVendor(handle), SDL.GetJoystickProduct(handle), SDL.GetJoystickSerial(handle));
            var properties = mapped ? SDL.GetGamepadProperties(handle) : SDL.GetJoystickProperties(handle);
            var vibration = properties != 0 && SDL.GetBooleanProperty(properties, SDL.Props.JoystickCapRumbleBoolean, false);
            var light = properties != 0 && (SDL.GetBooleanProperty(properties, SDL.Props.JoystickCapRGBLedBoolean, false) ||
                SDL.GetBooleanProperty(properties, SDL.Props.JoystickCapMonoLedBoolean, false));
            var gamepad = new GamepadDevice(instance, id, handle, mapped);
            if (prior is null)
            {
                _gamepadsByInstance.Add(instance, gamepad);
                _gamepadsByDevice.Add(id, gamepad);
            }
            else
            {
                _gamepadsByInstance[instance] = gamepad;
                _gamepadsByDevice[id] = gamepad;
                Input.Instance.DisconnectNativeJoypad(id, notify: false);
                CloseHandle(prior);
            }
            Input.Instance.ConnectNativeJoypad(id, name, guid, info, mapped, vibration, light, notify: false);
        }
        catch
        {
            if (prior is null)
            {
                _gamepadsByInstance.Remove(instance);
                _gamepadsByDevice.Remove(id);
                _freeGamepadIDs.Add(id);
            }
            CloseHandle(new GamepadDevice(instance, id, handle, mapped));
            throw;
        }
        if (prior is null)
        {
            if (initial) _pendingGamepadConnections.Add(id);
            else Input.Instance.EmitNativeJoypadConnection(id);
        }
    }

    private void RemoveGamepad(uint instance)
    {
        if (!_gamepadsByInstance.Remove(instance, out var gamepad)) return;
        _gamepadsByDevice.Remove(gamepad.ID);
        _freeGamepadIDs.Add(gamepad.ID);
        CloseHandle(gamepad);
        var pending = _pendingGamepadConnections.Remove(gamepad.ID);
        Input.Instance.DisconnectNativeJoypad(gamepad.ID, notify: !pending);
    }

    private void CloseGamepads()
    {
        foreach (var gamepad in _gamepadsByInstance.Values)
        {
            Input.Instance.DisconnectNativeJoypad(gamepad.ID, notify: false);
            CloseHandle(gamepad);
        }
        _gamepadsByInstance.Clear();
        _gamepadsByDevice.Clear();
        _freeGamepadIDs.Clear();
        _pendingGamepadConnections.Clear();
    }

    private void DispatchPendingGamepadConnections(ref List<Exception>? failures)
    {
        foreach (var id in _pendingGamepadConnections)
            try { Input.Instance.EmitNativeJoypadConnection(id); }
            catch (Exception error) { (failures ??= []).Add(error); }
        _pendingGamepadConnections.Clear();
    }

    private void DispatchGamepadEvent(SDL.Event nativeEvent, SDL.EventType type)
    {
        if (type is SDL.EventType.GamepadAdded or SDL.EventType.JoystickAdded or SDL.EventType.GamepadRemapped)
        {
            var addedInstance = type == SDL.EventType.JoystickAdded ? nativeEvent.JDevice.Which : nativeEvent.GDevice.Which;
            if (type == SDL.EventType.JoystickAdded) ApplyCustomMapping(addedInstance);
            AddGamepad(addedInstance, refresh: type == SDL.EventType.GamepadRemapped);
            return;
        }
        if (type is SDL.EventType.GamepadRemoved or SDL.EventType.JoystickRemoved)
        { RemoveGamepad(type == SDL.EventType.JoystickRemoved ? nativeEvent.JDevice.Which : nativeEvent.GDevice.Which); return; }
        if (ShouldIgnoreGamepads()) return;

        var nativeAxis = type is SDL.EventType.GamepadAxisMotion or SDL.EventType.JoystickAxisMotion;
        var instance = type switch
        {
            SDL.EventType.GamepadAxisMotion => nativeEvent.GAxis.Which,
            SDL.EventType.JoystickAxisMotion => nativeEvent.JAxis.Which,
            SDL.EventType.JoystickButtonDown or SDL.EventType.JoystickButtonUp => nativeEvent.JButton.Which,
            _ => nativeEvent.GButton.Which
        };
        if (!_gamepadsByInstance.TryGetValue(instance, out var gamepad)) return;
        if (gamepad.Mapped != (type is SDL.EventType.GamepadAxisMotion or SDL.EventType.GamepadButtonDown or SDL.EventType.GamepadButtonUp)) return;
        if (nativeAxis)
        {
            var axis = (JoyAxis)(gamepad.Mapped ? nativeEvent.GAxis.Axis : nativeEvent.JAxis.Axis);
            if (axis < JoyAxis.LeftX || axis >= (gamepad.Mapped ? JoyAxis.SdlMax : JoyAxis.Max)) return;
            var value = gamepad.Mapped ? nativeEvent.GAxis.Value : nativeEvent.JAxis.Value;
            using var input = new InputEventJoypadMotion
            {
                Device = gamepad.ID,
                Axis = axis,
                AxisValue = value < 0 ? value / 32768f : value / 32767f
            };
            Input.Instance.ParseInputEvent(input);
        }
        else
        {
            using var input = new InputEventJoypadButton
            {
                Device = gamepad.ID,
                ButtonIndex = (JoyButton)(gamepad.Mapped ? nativeEvent.GButton.Button : nativeEvent.JButton.Button),
                Pressed = type is SDL.EventType.GamepadButtonDown or SDL.EventType.JoystickButtonDown,
                Pressure = type is SDL.EventType.GamepadButtonDown or SDL.EventType.JoystickButtonDown ? 1f : 0f
            };
            Input.Instance.ParseInputEvent(input);
        }
    }

    internal void ApplyGamepadFocusPolicy()
    {
        EnsureOwner();
        if (!ShouldIgnoreGamepads()) return;
        Input.Instance.ReleasePressedEvents();
        foreach (var gamepad in _gamepadsByInstance.Values)
        {
            StopHandleVibration(gamepad);
            Input.Instance.StopNativeJoypadVibration(gamepad.ID);
        }
    }

    internal bool ShouldIgnoreGamepads()
    {
        if (!Input.Instance.IgnoreJoypadOnUnfocusedApplication) return false;
        var flags = SDL.GetWindowFlags(GetWindow(MainWindowId));
        return (flags & (SDL.WindowFlags.Hidden | SDL.WindowFlags.Minimized)) != 0 || !WindowIsFocused();
    }

    internal void UpdateJoyMapping(string guid, string? mapping)
    {
        EnsureOwner();
        foreach (var instance in _gamepadsByInstance.Keys.ToArray())
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(GetGamepadGUID(instance), guid)) continue;
            SDL.SetGamepadMapping(instance, mapping);
            AddGamepad(instance, refresh: true);
        }
    }

    private static string GetGamepadGUID(uint instance)
    {
        Span<byte> bytes = stackalloc byte[33];
        SDL.GUIDToString(SDL.GetJoystickGUIDForID(instance), bytes, bytes.Length);
        var end = bytes.IndexOf((byte)0);
        return Encoding.ASCII.GetString(bytes[..(end < 0 ? bytes.Length : end)]);
    }

    private static void ApplyCustomMapping(uint instance)
    {
        var mapping = Input.Instance.GetCustomJoyMapping(GetGamepadGUID(instance));
        if (mapping is not null) SDL.SetGamepadMapping(instance, mapping);
    }

    internal void StartGamepadVibration(int device, float weak, float strong, float duration)
    {
        EnsureOwner();
        if (!_gamepadsByDevice.TryGetValue(device, out var gamepad) || ShouldIgnoreGamepads()) return;
        var milliseconds = duration == 0 ? 65535u : (uint)Math.Clamp(Math.Round(duration * 1000d), 1d, 65535d);
        var low = (ushort)MathF.Round(strong * ushort.MaxValue);
        var high = (ushort)MathF.Round(weak * ushort.MaxValue);
        if (gamepad.Mapped) SDL.RumbleGamepad(gamepad.Handle, low, high, milliseconds);
        else SDL.RumbleJoystick(gamepad.Handle, unchecked((short)low), unchecked((short)high), (int)milliseconds);
    }

    internal void StopGamepadVibration(int device)
    {
        EnsureOwner();
        if (_gamepadsByDevice.TryGetValue(device, out var gamepad)) StopHandleVibration(gamepad);
    }

    internal void SetGamepadLight(int device, Color color)
    {
        EnsureOwner();
        if (!_gamepadsByDevice.TryGetValue(device, out var gamepad) || ShouldIgnoreGamepads()) return;
        static byte Byte(float value) => (byte)MathF.Round(Mathf.Clamp(value, 0, 1) * byte.MaxValue);
        if (gamepad.Mapped) SDL.SetGamepadLED(gamepad.Handle, Byte(color.R), Byte(color.G), Byte(color.B));
        else SDL.SetJoystickLED(gamepad.Handle, Byte(color.R), Byte(color.G), Byte(color.B));
    }

    private static void CloseHandle(GamepadDevice gamepad)
    {
        if (gamepad.Mapped) SDL.CloseGamepad(gamepad.Handle);
        else SDL.CloseJoystick(gamepad.Handle);
    }

    private static void StopHandleVibration(GamepadDevice gamepad)
    {
        if (gamepad.Mapped) SDL.RumbleGamepad(gamepad.Handle, 0, 0, 0);
        else SDL.RumbleJoystick(gamepad.Handle, 0, 0, 0);
    }

    private sealed record GamepadDevice(uint Instance, int ID, nint Handle, bool Mapped);
}
