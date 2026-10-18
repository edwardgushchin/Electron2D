using System.Diagnostics;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    internal bool SetGamepadMotionEnabled(int device, bool enable)
    {
        EnsureOwner();
        if (!_gamepadsByDevice.TryGetValue(device, out var gamepad) || !gamepad.Mapped) return false;
        var acceleration = SDL.GamepadHasSensor(gamepad.Handle, SDL.SensorType.Accel);
        var gyroscope = SDL.GamepadHasSensor(gamepad.Handle, SDL.SensorType.Gyro);
        if (!acceleration && !gyroscope) return false;
        var previousAcceleration = acceleration && SDL.GamepadSensorEnabled(gamepad.Handle, SDL.SensorType.Accel);
        if (acceleration && !SDL.SetGamepadSensorEnabled(gamepad.Handle, SDL.SensorType.Accel, enable))
            throw new InvalidOperationException($"Controller accelerometer update failed: {SDL.GetError()}");
        if (gyroscope && !SDL.SetGamepadSensorEnabled(gamepad.Handle, SDL.SensorType.Gyro, enable))
        {
            var error = new InvalidOperationException($"Controller gyroscope update failed: {SDL.GetError()}");
            if (acceleration && !SDL.SetGamepadSensorEnabled(gamepad.Handle, SDL.SensorType.Accel, previousAcceleration))
                throw new AggregateException(error, new InvalidOperationException($"Controller accelerometer rollback failed: {SDL.GetError()}"));
            throw error;
        }
        return true;
    }

    private void SampleGamepadMotion(bool dropInput)
    {
        var suppressed = dropInput || ShouldIgnoreGamepads();
        Input.Service.SetNativeJoyMotionSuppressed(suppressed);
        if (suppressed) return;
        Span<float> acceleration = stackalloc float[3];
        Span<float> gyroscope = stackalloc float[3];
        var timestamp = Stopwatch.GetTimestamp();
        foreach (var gamepad in _gamepadsByInstance.Values)
        {
            if (!gamepad.Mapped || !Input.Service.IsNativeJoyMotionSampling(gamepad.ID)) continue;
            acceleration.Clear(); gyroscope.Clear();
            if (SDL.GamepadHasSensor(gamepad.Handle, SDL.SensorType.Accel) &&
                !SDL.GetGamepadSensorData(gamepad.Handle, SDL.SensorType.Accel, acceleration, 3))
                throw new InvalidOperationException($"Controller accelerometer read failed: {SDL.GetError()}");
            if (SDL.GamepadHasSensor(gamepad.Handle, SDL.SensorType.Gyro) &&
                !SDL.GetGamepadSensorData(gamepad.Handle, SDL.SensorType.Gyro, gyroscope, 3))
                throw new InvalidOperationException($"Controller gyroscope read failed: {SDL.GetError()}");
            Input.Service.SampleNativeJoyMotion(gamepad.ID, new(-acceleration[0], -acceleration[1], -acceleration[2]),
                new(gyroscope[0], gyroscope[1], gyroscope[2]), timestamp);
        }
    }
}
