namespace Electron2D;

public sealed partial class Input
{
    /// <summary>Reports whether the connected controller has an accelerometer or gyroscope.</summary>
    /// <param name="device">The non-negative connected controller ID.</param>
    /// <returns>True when either motion channel is available; false for an absent or unsupported controller.</returns>
    /// <remarks>Reading this value does not create a display or enable sensors. Reads may run on any thread.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The controller ID is negative.</exception>
    public static bool HasJoyMotionSensors(int device) => Service.HasJoyMotionSensorsCore(device);
    /// <summary>Gets the controller's advertised motion sample frequency in hertz, or zero when absent.</summary>
    /// <param name="device">The non-negative connected controller ID.</param>
    /// <returns>The advertised accelerometer frequency, or the gyroscope frequency when only that channel is available; zero if unknown or absent.</returns>
    /// <remarks>The rate remains available while sensors are disabled. Reads may run on any thread.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The controller ID is negative.</exception>
    public static float GetJoyMotionSensorsRate(int device) => Service.GetJoyMotionSensorsRateCore(device);
    /// <summary>Reports whether motion sampling is enabled for the controller.</summary>
    /// <param name="device">The non-negative connected controller ID.</param>
    /// <returns>True when the available native channels are enabled, including during focus suppression; otherwise false.</returns>
    /// <remarks>Focus suppression hides readings without disabling the native channels. Reads may run on any thread.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The controller ID is negative.</exception>
    public static bool IsJoyMotionSensorsEnabled(int device) => Service.IsJoyMotionSensorsEnabledCore(device);
    /// <summary>Enables or disables the controller's available motion sensors on the display owner thread.</summary>
    /// <param name="device">The non-negative connected controller ID.</param>
    /// <param name="enable">Whether to enable all available motion channels.</param>
    /// <remarks>Available channels start disabled. An absent or unsupported controller is unchanged. Repeating the current state retains the sample clock. The live display owner must perform this operation.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The controller ID is negative.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the live display owner. A native sensor update failed.</exception>
    /// <exception cref="AggregateException">A native sensor update and rollback both failed.</exception>
    public static void SetJoyMotionSensorsEnabled(int device, bool enable) => Service.SetJoyMotionSensorsEnabledCore(device, enable);
    /// <summary>Gets controller acceleration including gravity in metres per second squared, or zero when unavailable.</summary>
    /// <param name="device">The non-negative connected controller ID.</param>
    /// <returns>The latest acceleration snapshot, or zero when absent, disabled or suppressed.</returns>
    /// <remarks>The value includes gravity. Held in front of the player, positive axes point left, down and away. Reads may run on any thread.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The controller ID is negative.</exception>
    public static Vector3 GetJoyAccelerometer(int device) => Service.GetJoyMotionValue(device, 0);
    /// <summary>Gets the controller's estimated gravity vector in metres per second squared, or zero when unavailable.</summary>
    /// <param name="device">The non-negative connected controller ID.</param>
    /// <returns>The latest estimated gravity snapshot, or zero when absent, disabled, suppressed or no gravity direction has been observed.</returns>
    /// <remarks>An observed direction has magnitude 9.80665 metres per second squared. A gyroscope-only controller has no inferred initial gravity direction. Reads may run on any thread.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The controller ID is negative.</exception>
    public static Vector3 GetJoyGravity(int device) => Service.GetJoyMotionValue(device, 1);
    /// <summary>Gets calibrated pitch, yaw and roll rates in radians per second, or zero when unavailable.</summary>
    /// <param name="device">The non-negative connected controller ID.</param>
    /// <returns>The latest calibrated angular-rate snapshot, or zero when absent, disabled or suppressed.</returns>
    /// <remarks>The running stationary bias is already applied during calibration. Reads may run on any thread.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The controller ID is negative.</exception>
    public static Vector3 GetJoyGyroscope(int device) => Service.GetJoyMotionValue(device, 2);
    /// <summary>Resets the bias samples and starts stationary gyroscope calibration on an enabled controller.</summary>
    /// <param name="device">The non-negative connected controller ID.</param>
    /// <remarks>Clears the previous samples and completion flag. An absent or unsupported controller is unchanged. The live display owner must perform this operation.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The controller ID is negative.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the live display owner. Sensors are disabled or calibration is already active.</exception>
    public static void StartJoyMotionSensorsCalibration(int device) => Service.StartJoyMotionSensorsCalibrationCore(device);
    /// <summary>Stops calibration and retains the accumulated stationary bias.</summary>
    /// <param name="device">The non-negative connected controller ID.</param>
    /// <remarks>A zero-sample interval completes with zero bias. An absent or unsupported controller is unchanged. The live display owner must perform this operation.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The controller ID is negative.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the live display owner. Sensors are disabled or calibration is not active.</exception>
    public static void StopJoyMotionSensorsCalibration(int device) => Service.StopJoyMotionSensorsCalibrationCore(device);
    /// <summary>Clears accumulated bias samples without ending an active calibration or changing its completion flag.</summary>
    /// <param name="device">The non-negative connected controller ID.</param>
    /// <remarks>Both the active and completed flags are retained. An absent or unsupported controller is unchanged. The live display owner must perform this operation.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The controller ID is negative.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the live display owner.</exception>
    public static void ClearJoyMotionSensorsCalibration(int device) => Service.ClearJoyMotionSensorsCalibrationCore(device);
    /// <summary>Gets completed calibration data, or null when the controller is absent or has not completed calibration.</summary>
    /// <param name="device">The non-negative connected controller ID.</param>
    /// <returns>A copy of the completed stationary bias, or null when absent or not yet calibrated.</returns>
    /// <remarks>Restored or completed data remains available while sensors are disabled. Reads may run on any thread.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The controller ID is negative.</exception>
    public static JoyMotionCalibration? GetJoyMotionSensorsCalibration(int device) => Service.GetJoyMotionSensorsCalibrationCore(device);
    /// <summary>Restores a finite stationary bias when calibration is not running; the zero value restores zero bias.</summary>
    /// <param name="device">The non-negative connected controller ID.</param>
    /// <param name="calibrationInfo">The stationary gyroscope bias to restore, in radians per second.</param>
    /// <remarks>Marks calibration complete. The restored bias affects readings on the next sample and may be set while sensors are disabled. An absent or unsupported controller is unchanged. The live display owner must perform this operation.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The controller ID is negative, or the bias is non-finite or too large for motion filtering.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the live display owner. Calibration is active.</exception>
    public static void SetJoyMotionSensorsCalibration(int device, JoyMotionCalibration calibrationInfo) => Service.SetJoyMotionSensorsCalibrationCore(device, calibrationInfo);
    /// <summary>Reports whether controller calibration has completed or calibration data has been restored.</summary>
    /// <param name="device">The non-negative connected controller ID.</param>
    /// <returns>True when calibration has completed or calibration data has been restored; false when absent.</returns>
    /// <remarks>Clearing samples retains this flag; starting calibration clears it. Reads may run on any thread.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The controller ID is negative.</exception>
    public static bool IsJoyMotionSensorsCalibrated(int device) => Service.IsJoyMotionSensorsCalibratedCore(device);
    /// <summary>Reports whether the controller is currently collecting stationary calibration samples.</summary>
    /// <param name="device">The non-negative connected controller ID.</param>
    /// <returns>True while stationary samples are being collected; false when absent.</returns>
    /// <remarks>Disabling sensors or suppressing input pauses sample collection without clearing this flag. Reads may run on any thread.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The controller ID is negative.</exception>
    public static bool IsJoyMotionSensorsCalibrating(int device) => Service.IsJoyMotionSensorsCalibratingCore(device);
}
