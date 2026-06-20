namespace Electron2D;

public struct Vector2I : IEquatable<Vector2I>
{
    public static readonly Vector2I Zero = new(0, 0);
    public static readonly Vector2I One = new(1, 1);
    public static readonly Vector2I Left = new(-1, 0);
    public static readonly Vector2I Right = new(1, 0);
    public static readonly Vector2I Up = new(0, -1);
    public static readonly Vector2I Down = new(0, 1);

    public Vector2I(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; set; }

    public int Y { get; set; }

    public float Length()
    {
        return MathF.Sqrt(LengthSquared());
    }

    public int LengthSquared()
    {
        return (X * X) + (Y * Y);
    }

    public float Aspect()
    {
        return (float)X / Y;
    }

    public Vector2I Abs()
    {
        return new Vector2I(Math.Abs(X), Math.Abs(Y));
    }

    public Vector2I Sign()
    {
        return new Vector2I(Math.Sign(X), Math.Sign(Y));
    }

    public Vector2I Min(Vector2I with)
    {
        return new Vector2I(Math.Min(X, with.X), Math.Min(Y, with.Y));
    }

    public Vector2I Max(Vector2I with)
    {
        return new Vector2I(Math.Max(X, with.X), Math.Max(Y, with.Y));
    }

    public Vector2I Clamp(Vector2I min, Vector2I max)
    {
        return new Vector2I(Mathf.Clamp(X, min.X, max.X), Mathf.Clamp(Y, min.Y, max.Y));
    }

    public bool Equals(Vector2I other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector2I other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public override string ToString()
    {
        return $"({MathFormatting.Format(X)}, {MathFormatting.Format(Y)})";
    }

    public static Vector2I operator +(Vector2I left, Vector2I right)
    {
        return new Vector2I(left.X + right.X, left.Y + right.Y);
    }

    public static Vector2I operator -(Vector2I left, Vector2I right)
    {
        return new Vector2I(left.X - right.X, left.Y - right.Y);
    }

    public static Vector2I operator -(Vector2I value)
    {
        return new Vector2I(-value.X, -value.Y);
    }

    public static Vector2I operator *(Vector2I left, Vector2I right)
    {
        return new Vector2I(left.X * right.X, left.Y * right.Y);
    }

    public static Vector2I operator *(Vector2I value, int scalar)
    {
        return new Vector2I(value.X * scalar, value.Y * scalar);
    }

    public static Vector2I operator *(int scalar, Vector2I value)
    {
        return value * scalar;
    }

    public static Vector2I operator /(Vector2I left, Vector2I right)
    {
        return new Vector2I(left.X / right.X, left.Y / right.Y);
    }

    public static Vector2I operator /(Vector2I value, int scalar)
    {
        return new Vector2I(value.X / scalar, value.Y / scalar);
    }

    public static Vector2I operator %(Vector2I left, Vector2I right)
    {
        return new Vector2I(left.X % right.X, left.Y % right.Y);
    }

    public static bool operator ==(Vector2I left, Vector2I right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector2I left, Vector2I right)
    {
        return !left.Equals(right);
    }

    public static implicit operator Vector2(Vector2I value)
    {
        return new Vector2(value.X, value.Y);
    }

    public static explicit operator Vector2I(Vector2 value)
    {
        return new Vector2I((int)value.X, (int)value.Y);
    }
}
