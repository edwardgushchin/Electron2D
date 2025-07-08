using SDL3;

namespace Electron2D;

public struct Vector2(float x, float y)
{
    public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        => LerpUnclamped(a, b, Math.Clamp(t, 0f, 1f));
    
    public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float t)
        => a + (b - a) * t;
    
    public static Vector2 RotatePoint(Vector2 point, float angleRadians)
    {
        var cos = MathF.Cos(angleRadians);
        var sin = MathF.Sin(angleRadians);
        return new Vector2(
            point.X * cos - point.Y * sin,
            point.X * sin + point.Y * cos
        );
    }
    
    public static float Distance(Vector2 a, Vector2 b)
    {
        return (a - b).Length;
    }
    
    public static float Dot(Vector2 a, Vector2 b)
    {
        return a.X * b.X + a.Y * b.Y;
    }
    
    public static Vector2 Normalize(Vector2 vector)
    {
        var length = vector.Length;
        return length > 0 ? vector / length : Vector2.Zero;
    }

    public float Length => MathF.Sqrt(X * X + Y * Y);
    
    public static Vector2 Zero => new(0, 0);
    
    public static Vector2 operator +(Vector2 a, Vector2 b) =>
        new Vector2(a.X + b.X, a.Y + b.Y);
    
    public static Vector2 operator -(Vector2 a, Vector2 b) =>
        new Vector2(a.X - b.X, a.Y - b.Y);

    public static Vector2 operator *(Vector2 v, float scalar) =>
        new Vector2(v.X * scalar, v.Y * scalar);
    
    public static Vector2 operator /(Vector2 v, float scalar) => 
        new Vector2(v.X / scalar, v.Y / scalar);
    
    public static Vector2 operator +(Vector2 v, float scalar) =>
        new Vector2(v.X + scalar, v.Y + scalar);

    public static Vector2 operator -(Vector2 v, float scalar) =>
        new Vector2(v.X - scalar, v.Y - scalar);
    
    public static implicit operator SDL.FPoint(Vector2 r) =>
        new() { X = r.X, Y = r.Y };

    public static implicit operator Vector2(SDL.FPoint r) =>
        new(){ X = r.X, Y = r.Y };
    
    
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
}