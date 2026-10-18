using System.Diagnostics;

namespace Electron2D;

public sealed partial class Input
{
    private readonly Dictionary<int, JoyMotionState> _joyMotion = [];
    private bool _joyMotionSuppressed;

    internal bool HasJoyMotionSensorsCore(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _joyMotion.ContainsKey(device);
    }

    internal float GetJoyMotionSensorsRateCore(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _joyMotion.GetValueOrDefault(device)?.Rate ?? 0;
    }

    internal bool IsJoyMotionSensorsEnabledCore(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _joyMotion.GetValueOrDefault(device)?.Enabled ?? false;
    }

    internal void SetJoyMotionSensorsEnabledCore(int device, bool enable)
    {
        ValidateDevice(device);
        var display = DisplayServer.Service;
        display?.EnsureGamepadOwner();
        if (display?.SetGamepadMotionEnabled(device, enable) != true) return;
        lock (_gate)
            if (_joyMotion.TryGetValue(device, out var state))
            {
                if (state.Enabled != enable) state.Timestamp = Stopwatch.GetTimestamp();
                state.Enabled = enable;
            }
    }

    internal Vector3 GetJoyMotionValue(int device, int lane)
    {
        ValidateDevice(device);
        lock (_gate)
        {
            if (_joyMotionSuppressed || !_joyMotion.TryGetValue(device, out var state) || !state.Enabled) return default;
            return lane switch { 0 => state.Acceleration, 1 => state.Gravity, _ => state.Gyroscope };
        }
    }

    internal void StartJoyMotionSensorsCalibrationCore(int device)
    {
        ValidateDevice(device);
        DisplayServer.Service?.EnsureGamepadOwner();
        lock (_gate)
        {
            if (!_joyMotion.TryGetValue(device, out var state)) return;
            if (!state.Enabled || state.Calibrating) throw new InvalidOperationException("Calibration requires enabled sensors and no active calibration.");
            state.ResetCalibration();
            state.Calibrating = true;
            state.Calibrated = false;
        }
    }

    internal void StopJoyMotionSensorsCalibrationCore(int device)
    {
        ValidateDevice(device);
        DisplayServer.Service?.EnsureGamepadOwner();
        lock (_gate)
        {
            if (!_joyMotion.TryGetValue(device, out var state)) return;
            if (!state.Enabled || !state.Calibrating) throw new InvalidOperationException("Stopping calibration requires enabled sensors and an active calibration.");
            state.Calibrating = false;
            state.Calibrated = true;
        }
    }

    internal void ClearJoyMotionSensorsCalibrationCore(int device)
    {
        ValidateDevice(device);
        DisplayServer.Service?.EnsureGamepadOwner();
        lock (_gate)
            if (_joyMotion.TryGetValue(device, out var state)) state.ResetCalibration();
    }

    internal JoyMotionCalibration? GetJoyMotionSensorsCalibrationCore(int device)
    {
        ValidateDevice(device);
        lock (_gate)
            return _joyMotion.TryGetValue(device, out var state) && state.Calibrated ? new JoyMotionCalibration(state.Offset) : null;
    }

    internal void SetJoyMotionSensorsCalibrationCore(int device, JoyMotionCalibration calibrationInfo)
    {
        ValidateDevice(device);
        if (!calibrationInfo.GyroscopeOffset.IsFinite() || !float.IsFinite(calibrationInfo.GyroscopeOffset.LengthSquared()))
            throw new ArgumentOutOfRangeException(nameof(calibrationInfo));
        DisplayServer.Service?.EnsureGamepadOwner();
        lock (_gate)
        {
            if (!_joyMotion.TryGetValue(device, out var state)) return;
            if (state.Calibrating) throw new InvalidOperationException("Calibration data cannot be replaced during calibration.");
            state.SetOffset(calibrationInfo.GyroscopeOffset);
            state.Calibrated = true;
        }
    }

    internal bool IsJoyMotionSensorsCalibratedCore(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _joyMotion.GetValueOrDefault(device)?.Calibrated ?? false;
    }

    internal bool IsJoyMotionSensorsCalibratingCore(int device)
    {
        ValidateDevice(device);
        lock (_gate) return _joyMotion.GetValueOrDefault(device)?.Calibrating ?? false;
    }

    internal void SetNativeJoyMotionSuppressed(bool suppressed)
    {
        lock (_gate)
        {
            var resetTimestamp = suppressed || _joyMotionSuppressed != suppressed;
            _joyMotionSuppressed = suppressed;
            if (resetTimestamp)
                foreach (var state in _joyMotion.Values) state.Timestamp = Stopwatch.GetTimestamp();
        }
    }

    internal bool IsNativeJoyMotionSampling(int device)
    {
        lock (_gate) return _joyMotion.TryGetValue(device, out var state) && state.Enabled;
    }

    internal void SampleNativeJoyMotion(int device, Vector3 acceleration, Vector3 gyroscope, long timestamp)
    {
        lock (_gate)
        {
            if (_joyMotionSuppressed || !_joyMotion.TryGetValue(device, out var state) || !state.Enabled) return;
            var delta = (float)Stopwatch.GetElapsedTime(state.Timestamp, timestamp).TotalSeconds;
            state.Timestamp = timestamp;
            if (!acceleration.IsFinite() || !gyroscope.IsFinite() ||
                !float.IsFinite(acceleration.LengthSquared()) || !float.IsFinite(gyroscope.LengthSquared()) ||
                !float.IsFinite((gyroscope - state.Offset).LengthSquared())) return;
            state.Process(acceleration, gyroscope, delta);
        }
    }
}
