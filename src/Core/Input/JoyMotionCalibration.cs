namespace Electron2D;

/// <summary>Stores a controller gyroscope's stationary bias in radians per second.</summary>
/// <remarks>A value copy owns no native device. The default value restores zero bias; <see cref="Input.SetJoyMotionSensorsCalibration"/> validates restored numeric data.</remarks>
public readonly struct JoyMotionCalibration
{
    /// <summary>Creates calibration data with the supplied stationary gyroscope bias.</summary>
    /// <param name="gyroscopeOffset">The pitch, yaw and roll bias, in radians per second.</param>
    public JoyMotionCalibration(Vector3 gyroscopeOffset) => GyroscopeOffset = gyroscopeOffset;

    /// <summary>Gets the pitch, yaw and roll bias in radians per second.</summary>
    /// <value>The supplied stationary angular-rate bias.</value>
    public Vector3 GyroscopeOffset { get; }
}
