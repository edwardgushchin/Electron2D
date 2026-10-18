namespace Electron2D;

public sealed partial class Input
{
    private Vector3 _deviceAcceleration, _deviceGravity, _deviceGyroscope, _deviceMagneticField;

    /// <summary>Gets the retained device acceleration in metres per second squared.</summary>
    /// <returns>A copy of the last supplied value, initially zero.</returns>
    /// <remarks>Thread-safe. Manual values are available without a display. Native mobile acquisition remains unavailable.</remarks>
    public static Vector3 GetAccelerometer() => Service.AccelerometerCore;

    /// <summary>Supplies device acceleration in metres per second squared for input simulation.</summary>
    /// <param name="value">The device-axis acceleration to retain.</param>
    /// <remarks>Thread-safe. Copies all components unchanged, including non-finite test values, without emitting input events or activating hardware.</remarks>
    public static void SetAccelerometer(Vector3 value) => Service.AccelerometerCore = value;

    /// <summary>Gets the retained device gravity in metres per second squared.</summary>
    /// <returns>A copy of the last supplied value, initially zero.</returns>
    /// <remarks>Thread-safe. Manual values are available without a display. Native mobile acquisition remains unavailable.</remarks>
    public static Vector3 GetGravity() => Service.DeviceGravityCore;

    /// <summary>Supplies device gravity in metres per second squared for input simulation.</summary>
    /// <param name="value">The device-axis gravity vector to retain.</param>
    /// <remarks>Thread-safe. Copies all components unchanged, including non-finite test values, without normalization, input events or hardware activation.</remarks>
    public static void SetGravity(Vector3 value) => Service.DeviceGravityCore = value;

    /// <summary>Gets retained rotation rates around the device axes in radians per second.</summary>
    /// <returns>A copy of the last supplied value, initially zero.</returns>
    /// <remarks>Thread-safe. Manual values are available without a display. Native mobile acquisition remains unavailable.</remarks>
    public static Vector3 GetGyroscope() => Service.DeviceGyroscopeCore;

    /// <summary>Supplies device-axis rotation rates in radians per second for input simulation.</summary>
    /// <param name="value">The rotation rates to retain.</param>
    /// <remarks>Thread-safe. Copies all components unchanged, including non-finite test values, without calibration, input events or hardware activation.</remarks>
    public static void SetGyroscope(Vector3 value) => Service.DeviceGyroscopeCore = value;

    /// <summary>Gets the retained device-axis magnetic field strength in microteslas.</summary>
    /// <returns>A copy of the last supplied value, initially zero.</returns>
    /// <remarks>Thread-safe. Manual values are available without a display. Native mobile acquisition remains unavailable.</remarks>
    public static Vector3 GetMagnetometer() => Service.MagnetometerCore;

    /// <summary>Supplies device-axis magnetic field strength in microteslas for input simulation.</summary>
    /// <param name="value">The magnetic field vector to retain.</param>
    /// <remarks>Thread-safe. Copies all components unchanged, including non-finite test values, without input events or hardware activation.</remarks>
    public static void SetMagnetometer(Vector3 value) => Service.MagnetometerCore = value;

    private Vector3 AccelerometerCore
    {
        get { lock (_gate) return _deviceAcceleration; }
        set { lock (_gate) _deviceAcceleration = value; }
    }

    private Vector3 DeviceGravityCore
    {
        get { lock (_gate) return _deviceGravity; }
        set { lock (_gate) _deviceGravity = value; }
    }

    private Vector3 DeviceGyroscopeCore
    {
        get { lock (_gate) return _deviceGyroscope; }
        set { lock (_gate) _deviceGyroscope = value; }
    }

    private Vector3 MagnetometerCore
    {
        get { lock (_gate) return _deviceMagneticField; }
        set { lock (_gate) _deviceMagneticField = value; }
    }
}
