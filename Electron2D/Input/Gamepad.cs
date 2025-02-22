using SDL3;

namespace Electron2D.Input;

public class Gamepad : IDisposable
{
    private static readonly Dictionary<uint, Gamepad> Gamepads = new();
    
    private readonly IntPtr _handle;
    private readonly uint _id;
    private bool _disposed;

    internal Gamepad(uint id)
    {
        _handle = SDL.OpenGamepad(id);

        if (_handle == IntPtr.Zero)
        {
            throw new ElectronException($"Failed to open gamepad with id {id}");
        }
        
        _id = id;
    }

    internal static void Add(uint id)
    {
        if (!Gamepads.ContainsKey(id))
        {
            Gamepads[id] = new Gamepad(id);
        }
        
        Console.WriteLine($"Added gamepad with id {id}, name: {Gamepads[id].Name}");
    }

    internal static void Remove(uint id)
    {
        if (!Gamepads.TryGetValue(id, out var gamepad)) return;
        Console.WriteLine($"Removed gamepad with id {id}, name: {Gamepads[id].Name}");
        gamepad.Dispose();
    }

    public static List<Gamepad> GetGamepads() => Gamepads.Values.ToList();

    public static Gamepad GetGamepad(uint id)
    {
        if (Gamepads.TryGetValue(id, out var gamepad))
        {
            return gamepad;
        }
        throw new ElectronException($"Gamepad with id {id} does not exist");
    }
    
    /// <summary>
    /// Check if a gamepad has been opened and is currently connected.
    /// </summary>
    /// <returns>Returns true if the gamepad has been opened and is currently connected, or false if not.</returns>
    public bool Connected => SDL.GamepadConnected(_handle);
    
    /// <summary>
    /// Get the implementation-dependent name for an opened gamepad.
    /// </summary>
    /// <returns>Returns the implementation dependent name for the gamepad, or <c>null</c> if there is no name or the identifier passed is invalid.</returns>
    public string? Name => SDL.GetGamepadName(_handle);
    
    /// <summary>
    /// Get the implementation-dependent path for an opened gamepad.
    /// </summary>
    /// <returns>Returns the implementation dependent path for the gamepad, or <c>null</c> if there is no path or the identifier passed is invalid.</returns>
    public string? Path => SDL.GetGamepadPath(_handle);

    /// <summary>
    /// Get or set the player index of an opened gamepad.
    /// </summary>
    /// <exception cref="ElectronException">Failed to set gamepad player index</exception>
    public int PlayerIndex
    {
        get => SDL.GetGamepadPlayerIndex(_handle);
        set
        {
            var result = SDL.SetGamepadPlayerIndex(_handle, value);
            if(result == false) throw new ElectronException($"Failed to set gamepad player index {value}: {SDL.GetError()}");
        }
    }


    /// <summary>
    /// Get the battery state of a gamepad.  You should never take a battery status as absolute truth.
    /// Batteries (especially failing batteries) are delicate hardware, and the values reported here are
    /// best estimates based on what that hardware reports. It's not uncommon for older batteries to lose
    /// stored power much faster than it reports, or completely drain when reporting it has 20 percent left, etc.
    /// </summary>
    public PowerInfo PowerInfo
    {
        get
        {
            var value = SDL.GetGamepadPowerInfo(_handle, out var percent);
            if (value == SDL.PowerState.Error)
            {
                throw new ElectronException($"Gamepad power state error: {SDL.GetError()}");
            }
            return new PowerInfo((PowerState)value, percent);
        }
    }
    
    /// <summary>
    /// Get the USB product ID of an opened gamepad, if available.
    /// </summary>
    /// <returns>Returns the USB product ID, or zero if unavailable.</returns>
    public ushort Product => SDL.GetGamepadProduct(_handle);
    
    /// <summary>
    /// Get the product version of an opened gamepad, if available.
    /// </summary>
    /// <returns>Returns the USB product version, or zero if unavailable.</returns>
    public ushort ProductVersion => SDL.GetGamepadProductVersion(_handle);
    
    /// <summary>
    /// Get the serial number of an opened gamepad, if available.
    /// </summary>
    /// <remarks>Returns the serial number, or <c>null</c> if unavailable.</remarks>
    public string? Serial => SDL.GetGamepadSerial(_handle);
    
    /// <summary>
    /// Get the Steam Input handle of an opened gamepad, if available.
    /// </summary>
    /// <returns>Returns the gamepad handle, or 0 if unavailable.</returns>
    public ulong SteamHandle => SDL.GetGamepadSteamHandle(_handle);
    
    /// <summary>
    /// Get the type of an opened gamepad.
    /// </summary>
    /// <returns>Returns the gamepad type, or <see cref="GamepadType.Unknown"/> if it's not available.</returns>
    public GamepadType Type => (GamepadType) SDL.GetGamepadType(_handle);
    
    /// <summary>
    /// Get the USB vendor ID of an opened gamepad, if available.
    /// </summary>
    /// <returns>Returns the USB vendor ID, or zero if unavailable.</returns>
    public ushort Vendor => SDL.GetGamepadVendor(_handle);
    
    /// <summary>
    /// Query whether a gamepad has a given axis.
    /// </summary>
    /// <param name="axis">an axis enum value</param>
    /// <returns>Returns true if the gamepad has this axis, false otherwise.</returns>
    public bool HasAxis(GamepadAxis axis) => SDL.GamepadHasAxis(_handle, (SDL.GamepadAxis) axis);
    
    /// <summary>
    /// Query whether a gamepad has a given button.
    /// </summary>
    /// <param name="button">a button enum value</param>
    /// <returns>Returns true if the gamepad has this button, false otherwise.</returns>
    public bool HasButton(GamepadButton button) => SDL.GamepadHasButton(_handle, (SDL.GamepadButton) button);
    
    /// <summary>
    /// Return whether a gamepad has a particular sensor.
    /// </summary>
    /// <param name="type">the type of sensor to query.</param>
    /// <returns>Returns true if the sensor exists, false otherwise.</returns>
    public bool HasSensor(SensorType type) => SDL.GamepadHasSensor(_handle, (SDL.SensorType) type);
    
    /// <summary>
    /// Query whether sensor data reporting is enabled for a gamepad.
    /// </summary>
    /// <param name="type">the type of sensor to query.</param>
    /// <returns>Returns true if the sensor is enabled, false otherwise.</returns>
    public bool SensorEnabled(SensorType type) => SDL.GamepadSensorEnabled(_handle, (SDL.SensorType) type);
    
    /// <summary>
    /// Get the current state of an axis control on a gamepad.
    /// </summary>
    /// <param name="axis">an axis index</param>
    /// <returns>Returns axis state</returns>
    public short GetAxis(GamepadAxis axis) => SDL.GetGamepadAxis(_handle, (SDL.GamepadAxis) axis);
    
    /// <summary>
    /// Get the current state of a button on a gamepad.
    /// </summary>
    /// <param name="button">a button index</param>
    /// <returns>Returns true if the button is pressed, false otherwise.</returns>
    public bool GetButton(GamepadButton button) => SDL.GetGamepadButton(_handle, (SDL.GamepadButton) button);

    /// <summary>
    /// Get the current state of a gamepad sensor.
    /// </summary>
    /// <param name="type">the type of sensor to query.</param>
    /// <param name="numValues">the number of values to write to data.</param>
    /// <returns>current sensor state</returns>
    /// <exception cref="ElectronException">Gamepad sensor data error</exception>
    /// <remarks>The number of values and interpretation of the data is sensor dependent.</remarks>
    public float[] GetSensorData(SensorType type, int numValues)
    {
        var result = SDL.GetGamepadSensorData(_handle, (SDL.SensorType) type, out var data, numValues);
        
        if(result == false) throw new ElectronException($"Gamepad sensor data error: {SDL.GetError()}");

        return data;
    }

    /// <summary>
    /// Get the data rate (number of events per second) of a gamepad sensor.
    /// </summary>
    /// <param name="type">the type of sensor to query.</param>
    /// <returns>Returns the data rate, or 0.0f if the data rate is not available.</returns>
    public float GetSensorDataRate(SensorType type) => SDL.GetGamepadSensorDataRate(_handle, (SDL.SensorType) type);

    /// <summary>
    /// Get the current state of a finger on a touchpad on a gamepad.
    /// </summary>
    /// <param name="touchpad">a touchpad.</param>
    /// <param name="finger">a finger.</param>
    /// <param name="x">the x position, normalized 0 to 1, with the origin in the upper left</param>
    /// <param name="y">the y position, normalized 0 to 1, with the origin in the upper left</param>
    /// <param name="pressure">pressure value</param>
    /// <returns>Return true if the finger is down, false otherwise</returns>
    /// <exception cref="ElectronException">Gamepad touchpad finger error</exception>
    public bool GetTouchpadFinger(int touchpad, int finger, out float x, out float y, out float pressure)
    {
        var result = SDL.GetGamepadTouchpadFinger(_handle, touchpad, finger, out var down, out x, out y, out pressure);
        
        if(result == false) throw new ElectronException($"Gamepad touchpad finger error: {SDL.GetError()}");

        return down;
    }
    
    /// <summary>
    /// Get the number of supported simultaneous fingers on a touchpad on a game gamepad.
    /// </summary>
    /// <param name="touchpad">a touchpad.</param>
    /// <returns>Returns number of supported simultaneous fingers.</returns>
    public int GetNumTouchpadFingers(int touchpad) => SDL.GetNumGamepadTouchpadFingers(_handle, touchpad);
    
    /// <summary>
    /// Get the number of touchpads on a gamepad.
    /// </summary>
    /// <returns>Returns number of touchpads.</returns>
    public int GetNumTouchpads() => SDL.GetNumGamepadTouchpads(_handle);

    /// <summary>
    /// Start a rumble effect on a gamepad.
    /// </summary>
    /// <param name="lowFrequencyRumble">the intensity of the low frequency (left) rumble motor, from 0 to 0xFFFF.</param>
    /// <param name="highFrequencyRumble">the intensity of the high frequency (right) rumble motor, from 0 to 0xFFFF.</param>
    /// <param name="durationMS">the duration of the rumble effect, in milliseconds.</param>
    /// <exception cref="ElectronException">Gamepad rumble error:</exception>
    /// <remarks>Each call to this function cancels any previous rumble effect, and calling it with 0 intensity stops any rumbling.</remarks>
    public void Rumble(ushort lowFrequencyRumble, ushort highFrequencyRumble, uint durationMS)
    {
        var result = SDL.RumbleGamepad(_handle, lowFrequencyRumble, highFrequencyRumble, durationMS);
        if(result == false) throw new ElectronException($"Gamepad rumble error: {SDL.GetError()}");
    }

    /// <summary>
    /// Start a rumble effect in the gamepad's triggers.
    /// </summary>
    /// <param name="leftRumble">the intensity of the left trigger rumble motor, from 0 to 0xFFFF.</param>
    /// <param name="rightRumble">the intensity of the right trigger rumble motor, from 0 to 0xFFFF.</param>
    /// <param name="durationMS">the duration of the rumble effect, in milliseconds.</param>
    /// <exception cref="ElectronException">Gamepad rumble triggers error</exception>
    /// <remarks>Each call to this function cancels any previous trigger rumble effect, and calling it with 0 intensity stops any rumbling.</remarks>
    public void RumbleTriggers(ushort leftRumble, ushort rightRumble, uint durationMS)
    {
        var result = SDL.RumbleGamepadTriggers(_handle, leftRumble, rightRumble, durationMS);
        if(result == false) throw new ElectronException($"Gamepad rumble triggers error: {SDL.GetError()}");
    }

    /// <summary>
    /// Update a gamepad's LED color.
    /// </summary>
    /// <param name="color">a new LED color</param>
    /// <exception cref="ElectronException">Gamepad LED error</exception>
    public void SetLED(Color color)
    {
        var result = SDL.SetGamepadLED(_handle, color.R, color.G, color.B);
        if(result == false) throw new ElectronException($"Gamepad LED error: {SDL.GetError()}");
    }

    /// <summary>
    /// Set whether data reporting for a gamepad sensor is enabled.
    /// </summary>
    /// <param name="type">the type of sensor to enable/disable.</param>
    /// <param name="enabled">whether data reporting should be enabled.</param>
    /// <exception cref="ElectronException">Gamepad sensor enabled error</exception>
    public void SetSensorEnabled(SensorType type, bool enabled)
    {
        var result = SDL.SetGamepadSensorEnabled(_handle, (SDL.SensorType) type, enabled);
        if(result == false) throw new ElectronException($"Gamepad sensor enabled error: {SDL.GetError()}");
    }
    
    
    public void Dispose()
    {
        if (_disposed) return;
        
        SDL.CloseGamepad(_handle);
        Gamepads.Remove(_id);
        _disposed = true;
    }

    ~Gamepad()
    {
        Dispose();
    }
}