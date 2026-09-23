namespace Electron2D;

internal static class TweenMath
{
    internal static void Validate(Tween.TransitionType transition, Tween.EaseType ease)
    {
        if (!Enum.IsDefined(transition))
            throw new ArgumentOutOfRangeException(nameof(transition), transition, "The tween transition is undefined.");
        if (!Enum.IsDefined(ease))
            throw new ArgumentOutOfRangeException(nameof(ease), ease, "The tween easing mode is undefined.");
    }

    internal static double Ease(double value, Tween.TransitionType transition, Tween.EaseType ease)
    {
        Validate(transition, ease);
        if (transition == Tween.TransitionType.Linear)
            return value;
        if (value == 0d || value == 1d)
            return value;

        return ease switch
        {
            Tween.EaseType.In => EaseIn(value, transition),
            Tween.EaseType.Out => EaseOut(value, transition),
            Tween.EaseType.InOut => EaseInOut(value, transition),
            Tween.EaseType.OutIn => value < 0.5d
                ? EaseOut(value * 2d, transition) * 0.5d
                : 0.5d + (EaseIn((value * 2d) - 1d, transition) * 0.5d),
            _ => throw new ArgumentOutOfRangeException(nameof(ease), ease, "The tween easing mode is undefined."),
        };
    }

    private static double EaseIn(double value, Tween.TransitionType transition) => transition switch
    {
        Tween.TransitionType.Sine => 1d - Math.Cos(value * Math.PI / 2d),
        Tween.TransitionType.Quint => Math.Pow(value, 5d),
        Tween.TransitionType.Quart => Math.Pow(value, 4d),
        Tween.TransitionType.Quad => value * value,
        Tween.TransitionType.Expo => Math.Pow(2d, 10d * (value - 1d)) - 0.001d,
        Tween.TransitionType.Elastic => ElasticIn(value),
        Tween.TransitionType.Cubic => value * value * value,
        Tween.TransitionType.Circ => 1d - Math.Sqrt(1d - (value * value)),
        Tween.TransitionType.Bounce => 1d - BounceOut(1d - value),
        Tween.TransitionType.Back => value * value * ((2.70158d * value) - 1.70158d),
        Tween.TransitionType.Spring => 1d - SpringOut(1d - value),
        _ => value,
    };

    private static double EaseOut(double value, Tween.TransitionType transition) => transition switch
    {
        Tween.TransitionType.Sine => Math.Sin(value * Math.PI / 2d),
        Tween.TransitionType.Quint => Math.Pow(value - 1d, 5d) + 1d,
        Tween.TransitionType.Quart => 1d - Math.Pow(value - 1d, 4d),
        Tween.TransitionType.Quad => -value * (value - 2d),
        Tween.TransitionType.Expo => 1.001d * (1d - Math.Pow(2d, -10d * value)),
        Tween.TransitionType.Elastic => ElasticOut(value),
        Tween.TransitionType.Cubic => Math.Pow(value - 1d, 3d) + 1d,
        Tween.TransitionType.Circ => Math.Sqrt(1d - Math.Pow(value - 1d, 2d)),
        Tween.TransitionType.Bounce => BounceOut(value),
        Tween.TransitionType.Back => Math.Pow(value - 1d, 2d) * ((2.70158d * (value - 1d)) + 1.70158d) + 1d,
        Tween.TransitionType.Spring => SpringOut(value),
        _ => value,
    };

    private static double EaseInOut(double value, Tween.TransitionType transition) => transition switch
    {
        Tween.TransitionType.Expo => ExpoInOut(value),
        Tween.TransitionType.Elastic => ElasticInOut(value),
        Tween.TransitionType.Back => BackInOut(value),
        _ => value < 0.5d
            ? EaseIn(value * 2d, transition) * 0.5d
            : 0.5d + (EaseOut((value * 2d) - 1d, transition) * 0.5d),
    };

    private static double ExpoInOut(double value)
    {
        var time = value * 2d;
        return time < 1d
            ? (0.5d * Math.Pow(2d, 10d * (time - 1d))) - 0.0005d
            : 0.5d * 1.0005d * (-Math.Pow(2d, -10d * (time - 1d)) + 2d);
    }

    private static double ElasticInOut(double value)
    {
        var time = value * 2d;
        const double period = 0.45d;
        const double shift = period / 4d;
        if (time < 1d)
        {
            time -= 1d;
            return -0.5d * Math.Pow(2d, 10d * time) *
                Math.Sin((time - shift) * (2d * Math.PI) / period);
        }

        time -= 1d;
        return 0.5d * Math.Pow(2d, -10d * time) *
            Math.Sin((time - shift) * (2d * Math.PI) / period) + 1d;
    }

    private static double BackInOut(double value)
    {
        const double overshoot = 1.70158d * 1.525d;
        var time = value * 2d;
        if (time < 1d)
            return 0.5d * time * time * (((overshoot + 1d) * time) - overshoot);

        time -= 2d;
        return 0.5d * ((time * time * (((overshoot + 1d) * time) + overshoot)) + 2d);
    }

    private static double ElasticIn(double value)
    {
        var shifted = value - 1d;
        return -(Math.Pow(2d, 10d * shifted) * Math.Sin((shifted - 0.075d) * (2d * Math.PI) / 0.3d));
    }

    private static double ElasticOut(double value) =>
        (Math.Pow(2d, -10d * value) * Math.Sin((value - 0.075d) * (2d * Math.PI) / 0.3d)) + 1d;

    private static double BounceOut(double value)
    {
        if (value < 1d / 2.75d)
            return 7.5625d * value * value;
        if (value < 2d / 2.75d)
        {
            value -= 1.5d / 2.75d;
            return (7.5625d * value * value) + 0.75d;
        }
        if (value < 2.5d / 2.75d)
        {
            value -= 2.25d / 2.75d;
            return (7.5625d * value * value) + 0.9375d;
        }
        value -= 2.625d / 2.75d;
        return (7.5625d * value * value) + 0.984375d;
    }

    private static double SpringOut(double value)
    {
        var remaining = 1d - value;
        return (Math.Sin(value * Math.PI * (0.2d + (2.5d * value * value * value))) *
                Math.Pow(remaining, 2.2d) + value) * (1d + (1.2d * remaining));
    }
}

internal static class TweenValue<TValue>
{
    internal static readonly Func<TValue, TValue, double, TValue>? Interpolate = CreateInterpolator();
    internal static readonly Func<TValue, TValue, TValue>? Add = CreateAdder();

    internal static NotSupportedException Unsupported() =>
        new($"Tween interpolation is not defined for {typeof(TValue).FullName}. Supply a typed interpolator explicitly.");

    private static Func<TValue, TValue, double, TValue>? CreateInterpolator()
    {
        if (typeof(TValue) == typeof(float))
            return Cast((float from, float to, double weight) => Mathf.Lerp(from, to, (float)weight));
        if (typeof(TValue) == typeof(double))
            return Cast((double from, double to, double weight) => Mathf.Lerp(from, to, weight));
        if (typeof(TValue) == typeof(bool))
            return Cast((bool from, bool to, double weight) =>
                ((from ? 1d : 0d) + (((to ? 1d : 0d) - (from ? 1d : 0d)) * weight)) >= 0.5d);
        if (typeof(TValue) == typeof(int))
            return Cast((int from, int to, double weight) => RoundToInt(from + ((to - (double)from) * weight)));
        if (typeof(TValue) == typeof(long))
            return Cast((long from, long to, double weight) => RoundToLong(from + ((to - (double)from) * weight)));
        if (typeof(TValue) == typeof(Vector2))
            return Cast((Vector2 from, Vector2 to, double weight) => from.Lerp(to, (float)weight));
        if (typeof(TValue) == typeof(Vector2I))
            return Cast((Vector2I from, Vector2I to, double weight) => new Vector2I(
                RoundToInt(from.X + ((to.X - (double)from.X) * weight)),
                RoundToInt(from.Y + ((to.Y - (double)from.Y) * weight))));
        if (typeof(TValue) == typeof(Vector3))
            return Cast((Vector3 from, Vector3 to, double weight) => from.Lerp(to, (float)weight));
        if (typeof(TValue) == typeof(Vector3I))
            return Cast((Vector3I from, Vector3I to, double weight) => new Vector3I(
                RoundToInt(from.X + ((to.X - (double)from.X) * weight)),
                RoundToInt(from.Y + ((to.Y - (double)from.Y) * weight)),
                RoundToInt(from.Z + ((to.Z - (double)from.Z) * weight))));
        if (typeof(TValue) == typeof(Vector4))
            return Cast((Vector4 from, Vector4 to, double weight) => from.Lerp(to, (float)weight));
        if (typeof(TValue) == typeof(Vector4I))
            return Cast((Vector4I from, Vector4I to, double weight) => new Vector4I(
                RoundToInt(from.X + ((to.X - (double)from.X) * weight)),
                RoundToInt(from.Y + ((to.Y - (double)from.Y) * weight)),
                RoundToInt(from.Z + ((to.Z - (double)from.Z) * weight)),
                RoundToInt(from.W + ((to.W - (double)from.W) * weight))));
        if (typeof(TValue) == typeof(Color))
            return Cast((Color from, Color to, double weight) => from.Lerp(to, (float)weight));
        if (typeof(TValue) == typeof(Rect))
            return Cast((Rect from, Rect to, double weight) => new Rect(
                from.Position.Lerp(to.Position, (float)weight),
                from.Size.Lerp(to.Size, (float)weight)));
        if (typeof(TValue) == typeof(RectI))
            return Cast((RectI from, RectI to, double weight) => new RectI(
                TweenValue<Vector2I>.Interpolate!(from.Position, to.Position, weight),
                TweenValue<Vector2I>.Interpolate!(from.Size, to.Size, weight)));
        if (typeof(TValue) == typeof(Transform))
            return Cast((Transform from, Transform to, double weight) => from.InterpolateWith(to, (float)weight));
        return null;
    }

    private static Func<TValue, TValue, TValue>? CreateAdder()
    {
        if (typeof(TValue) == typeof(float))
            return Cast((float left, float right) => left + right);
        if (typeof(TValue) == typeof(double))
            return Cast((double left, double right) => left + right);
        if (typeof(TValue) == typeof(bool))
            return Cast((bool _, bool right) => right);
        if (typeof(TValue) == typeof(int))
            return Cast((int left, int right) => checked(left + right));
        if (typeof(TValue) == typeof(long))
            return Cast((long left, long right) => checked(left + right));
        if (typeof(TValue) == typeof(Vector2))
            return Cast((Vector2 left, Vector2 right) => left + right);
        if (typeof(TValue) == typeof(Vector2I))
            return Cast((Vector2I left, Vector2I right) => left + right);
        if (typeof(TValue) == typeof(Vector3))
            return Cast((Vector3 left, Vector3 right) => left + right);
        if (typeof(TValue) == typeof(Vector3I))
            return Cast((Vector3I left, Vector3I right) => left + right);
        if (typeof(TValue) == typeof(Vector4))
            return Cast((Vector4 left, Vector4 right) => left + right);
        if (typeof(TValue) == typeof(Vector4I))
            return Cast((Vector4I left, Vector4I right) => left + right);
        if (typeof(TValue) == typeof(Color))
            return Cast((Color left, Color right) => left + right);
        if (typeof(TValue) == typeof(Rect))
            return Cast((Rect left, Rect right) => new Rect(left.Position + right.Position, left.Size + right.Size));
        if (typeof(TValue) == typeof(RectI))
            return Cast((RectI left, RectI right) => new RectI(left.Position + right.Position, left.Size + right.Size));
        if (typeof(TValue) == typeof(Transform))
            return Cast((Transform left, Transform right) => left * right);
        return null;
    }

    private static Func<TValue, TValue, double, TValue> Cast<T>(Func<T, T, double, T> function) =>
        (Func<TValue, TValue, double, TValue>)(object)function;

    private static Func<TValue, TValue, TValue> Cast<T>(Func<T, T, T> function) =>
        (Func<TValue, TValue, TValue>)(object)function;

    private static int RoundToInt(double value) =>
        checked((int)Math.Round(value, MidpointRounding.AwayFromZero));

    private static long RoundToLong(double value) =>
        checked((long)Math.Round(value, MidpointRounding.AwayFromZero));
}
