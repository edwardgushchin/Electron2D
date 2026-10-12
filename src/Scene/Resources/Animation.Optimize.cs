using System.Runtime.CompilerServices;

namespace Electron2D;

public sealed partial class Animation
{
    /// <summary>Removes redundant keys from built-in float, double and Vector2 property curves.</summary>
    /// <param name="allowedVelocityError">Relative speed tolerance, between zero and one.</param>
    /// <param name="allowedAngularError">Direction tolerance, between zero and one; a dot product must be at least one minus twice this value.</param>
    /// <param name="precision">Decimal precision from zero through fifteen; absolute tolerance is ten to its negative power.</param>
    /// <remarks>Linear, linear-angle and nearest curves participate. Discrete events, cubic curves, custom
    /// interpolators and eased segments retain their keys. Nonconstant endpoints remain; constant curves
    /// may retain only their first key. Tolerances test adjacent segments and do not bound total positional error.
    /// All changes commit before one Changed notification.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A tolerance is not finite or outside zero through one, or precision is outside zero through fifteen.</exception>
    public void Optimize(double allowedVelocityError = .01, double allowedAngularError = .01, int precision = 3)
    {
        ThrowIfDisposed();
        if (!double.IsFinite(allowedVelocityError) || allowedVelocityError < 0 || allowedVelocityError > 1)
            throw new ArgumentOutOfRangeException(nameof(allowedVelocityError));
        if (!double.IsFinite(allowedAngularError) || allowedAngularError < 0 || allowedAngularError > 1)
            throw new ArgumentOutOfRangeException(nameof(allowedAngularError));
        if ((uint)precision > 15) throw new ArgumentOutOfRangeException(nameof(precision));
        var changed = false;
        var tolerance = Math.Pow(.1, precision);
        foreach (var track in _tracks) changed |= track.Optimize(allowedVelocityError, allowedAngularError, tolerance);
        if (changed) EmitChanged();
    }
}

internal static class AnimationKeyOptimizer
{
    internal static bool Optimize<T>(AnimationTypedTrack<T> track, double velocityError, double angularError, double precision)
    {
        if (typeof(T) != typeof(float) && typeof(T) != typeof(double) && typeof(T) != typeof(Vector2)) return false;
        if (track.Update == Animation.UpdateMode.Discrete || track.Interpolate != TweenValue<T>.Interpolate ||
            track.Interpolation is not (Animation.InterpolationType.Linear or Animation.InterpolationType.LinearAngle or Animation.InterpolationType.Nearest)) return false;
        var angle = track.Interpolation == Animation.InterpolationType.LinearAngle;
        var nearest = track.Interpolation == Animation.InterpolationType.Nearest;
        var changed = false;
        for (var i = 0; i < track.Times.Count - 2;)
        {
            if (track.Transitions[i] != 1 || track.Transitions[i + 1] != 1 || track.Transitions[i + 2] != 1 ||
                !CanRemove(track, i, velocityError, angularError, precision, angle, nearest)) { i++; continue; }
            track.RemoveKey(i + 1);
            changed = true;
        }
        if (track.Times.Count == 2 && track.Transitions[0] == 1 && track.Transitions[1] == 1 &&
            Distance(track.Values[0], track.Values[1], angle) < precision)
        {
            track.RemoveKey(1);
            changed = true;
        }
        return changed;
    }

    private static bool CanRemove<T>(AnimationTypedTrack<T> track, int i, double velocityError, double angularError,
        double precision, bool angle, bool nearest)
    {
        var a = track.Values[i]; var b = track.Values[i + 1]; var c = track.Values[i + 2];
        if (Distance(a, b, angle) < precision && Distance(b, c, angle) < precision) return true;
        if (nearest) return false;
        var dt0 = track.Times[i + 1] - track.Times[i];
        var dt1 = track.Times[i + 2] - track.Times[i + 1];
        if (!double.IsFinite(dt0) || !double.IsFinite(dt1) || dt0 <= 0 || dt1 <= 0) return false;
        double v0, v1;
        if (typeof(T) == typeof(Vector2))
        {
            var x = Unsafe.As<T, Vector2>(ref a); var y = Unsafe.As<T, Vector2>(ref b); var z = Unsafe.As<T, Vector2>(ref c);
            if (!float.IsFinite(z.X - x.X) || !float.IsFinite(z.Y - x.Y)) return false;
            var dx0 = ((double)y.X - x.X) / dt0; var dy0 = ((double)y.Y - x.Y) / dt0;
            var dx1 = ((double)z.X - y.X) / dt1; var dy1 = ((double)z.Y - y.Y) / dt1;
            v0 = Math.Sqrt(dx0 * dx0 + dy0 * dy0); v1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);
            if (!double.IsFinite(v0) || !double.IsFinite(v1)) return false;
            // Equal speed alone cannot establish a straight path.
            if (v0 == 0 || v1 == 0) return v0 < precision && v1 < precision;
            if ((dx0 / v0) * (dx1 / v1) + (dy0 / v0) * (dy1 / v1) < 1 - 2 * angularError) return false;
        }
        else
        {
            var x = Scalar(a); var y = Scalar(b); var z = Scalar(c);
            if (angle)
            {
                var d0 = AngleDelta(x, y); var d1 = AngleDelta(y, z);
                if (Math.Abs(d0) + Math.Abs(d1) >= Math.PI) return false;
                y = x + d0; z = y + d1;
            }
            var span = z - x;
            if (!double.IsFinite(span) || (typeof(T) == typeof(float) && !float.IsFinite((float)span))) return false;
            v0 = (y - x) / dt0; v1 = (z - y) / dt1;
            if (!double.IsFinite(v0) || !double.IsFinite(v1)) return false;
            if (Math.Abs(v0 - v1) < precision) return true;
            if (Math.Sign(v0) != Math.Sign(v1)) return false;
            v0 = Math.Abs(v0); v1 = Math.Abs(v1);
        }
        if (Math.Abs(v0 - v1) < precision) return true;
        if (v0 < precision || v1 < precision) return false;
        return Math.Min(v0, v1) / Math.Max(v0, v1) >= 1 - velocityError;
    }

    private static double Distance<T>(T a, T b, bool angle)
    {
        if (typeof(T) == typeof(Vector2))
        {
            var x = Unsafe.As<T, Vector2>(ref a); var y = Unsafe.As<T, Vector2>(ref b);
            var dx = (double)y.X - x.X; var dy = (double)y.Y - x.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        return Math.Abs(angle ? AngleDelta(Scalar(a), Scalar(b)) : Scalar(b) - Scalar(a));
    }
    private static double Scalar<T>(T value) => typeof(T) == typeof(float) ? Unsafe.As<T, float>(ref value) : Unsafe.As<T, double>(ref value);
    private static double AngleDelta(double from, double to) => Mathf.LerpAngle(from, to, 1) - from;
}
