namespace Electron2D;

// Gravity filtering adapted from GamepadMotionHelpers v9; see licence/GamepadMotionHelpers-LICENSE.txt.
internal sealed class JoyMotionState(float rate)
{
    internal const float StandardGravity = 9.80665f;
    internal readonly float Rate = rate;
    internal bool Enabled;
    internal bool Calibrating;
    internal bool Calibrated;
    internal long Timestamp;
    internal Vector3 Acceleration;
    internal Vector3 Gyroscope;
    internal Vector3 Gravity => _gravity.Normalized() * StandardGravity;
    private Vector3 _gravity, _smoothAcceleration;
    private float _shakiness;
    private double _samples, _sumX, _sumY, _sumZ, _accelerationSum;

    internal Vector3 Offset => _samples == 0 ? default :
        new((float)(_sumX / _samples), (float)(_sumY / _samples), (float)(_sumZ / _samples));

    internal void ResetCalibration() => _samples = _sumX = _sumY = _sumZ = _accelerationSum = 0;

    internal void SetOffset(Vector3 offset)
    {
        _accelerationSum = _samples > 1 ? _accelerationSum / _samples : 1;
        _samples = 1;
        _sumX = offset.X; _sumY = offset.Y; _sumZ = offset.Z;
    }

    internal void Process(Vector3 acceleration, Vector3 gyroscope, float delta)
    {
        if (acceleration == Vector3.Zero && gyroscope == Vector3.Zero) return;
        var accel = -acceleration / StandardGravity;
        var magnitude = accel.Length();
        if (Calibrating)
        {
            _samples++;
            _sumX += gyroscope.X; _sumY += gyroscope.Y; _sumZ += gyroscope.Z;
            _accelerationSum += magnitude;
        }
        Gyroscope = gyroscope - Offset;
        Acceleration = acceleration;
        var speed = Gyroscope.Length();
        _gravity = RotateInverse(_gravity, Gyroscope, speed, delta);
        if (magnitude <= 0) return;
        _smoothAcceleration = RotateInverse(_smoothAcceleration, Gyroscope, speed, delta);
        var smooth = MathF.Pow(2, -delta / .25f);
        _shakiness = MathF.Max(_shakiness * smooth, (accel - _smoothAcceleration).Length());
        _smoothAcceleration = accel.Lerp(_smoothAcceleration, smooth);
        var gravityLength = _samples == 0 ? 1 : (float)(_accelerationSum / _samples);
        var target = accel / magnitude * -gravityLength;
        var correction = target - _gravity;
        var correctionSpeed = 1 + (.1f - 1) * Math.Clamp((_shakiness - .01f) / (.4f - .01f), 0, 1);
        var limit = MathF.Max(speed * .1f, .01f);
        if (correctionSpeed > limit)
            correctionSpeed = limit + (correctionSpeed - limit) * Math.Clamp((correction.Length() - .05f) / (.25f - .05f), 0, 1);
        var change = correction.Normalized() * correctionSpeed * delta;
        _gravity = change.LengthSquared() < correction.LengthSquared() ? _gravity + change : target;
    }

    private static Vector3 RotateInverse(Vector3 value, Vector3 gyro, float speed, float delta)
    {
        if (speed == 0) return value;
        var axis = gyro / speed;
        var angle = -speed * delta;
        var cosine = MathF.Cos(angle);
        return value * cosine + axis.Cross(value) * MathF.Sin(angle) + axis * axis.Dot(value) * (1 - cosine);
    }
}
