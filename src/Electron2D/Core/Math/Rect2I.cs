namespace Electron2D;

public struct Rect2I : IEquatable<Rect2I>
{
    public Rect2I(Vector2I position, Vector2I size)
    {
        Position = position;
        Size = size;
    }

    public Rect2I(int x, int y, int width, int height)
        : this(new Vector2I(x, y), new Vector2I(width, height))
    {
    }

    public Vector2I Position { get; set; }

    public Vector2I Size { get; set; }

    public Vector2I End
    {
        get => Position + Size;
        set => Size = value - Position;
    }

    public int GetArea()
    {
        return Size.X * Size.Y;
    }

    public Vector2I GetCenter()
    {
        return Position + (Size / 2);
    }

    public bool HasArea()
    {
        return Size.X > 0 && Size.Y > 0;
    }

    public Rect2I Abs()
    {
        var end = End;
        var min = Position.Min(end);
        var max = Position.Max(end);
        return new Rect2I(min, max - min);
    }

    public bool HasPoint(Vector2I point)
    {
        return point.X >= Position.X &&
            point.Y >= Position.Y &&
            point.X < Position.X + Size.X &&
            point.Y < Position.Y + Size.Y;
    }

    public bool Encloses(Rect2I rect)
    {
        return rect.Position.X >= Position.X &&
            rect.Position.Y >= Position.Y &&
            rect.End.X <= End.X &&
            rect.End.Y <= End.Y;
    }

    public bool Intersects(Rect2I rect, bool includeBorders = false)
    {
        if (includeBorders)
        {
            return Position.X <= rect.End.X &&
                End.X >= rect.Position.X &&
                Position.Y <= rect.End.Y &&
                End.Y >= rect.Position.Y;
        }

        return Position.X < rect.End.X &&
            End.X > rect.Position.X &&
            Position.Y < rect.End.Y &&
            End.Y > rect.Position.Y;
    }

    public Rect2I Intersection(Rect2I rect)
    {
        var start = Position.Max(rect.Position);
        var end = End.Min(rect.End);
        var size = end - start;
        if (size.X < 0 || size.Y < 0)
        {
            return new Rect2I(start, Vector2I.Zero);
        }

        return new Rect2I(start, size);
    }

    public Rect2I Merge(Rect2I rect)
    {
        var start = Position.Min(rect.Position);
        var end = End.Max(rect.End);
        return new Rect2I(start, end - start);
    }

    public Rect2I Expand(Vector2I to)
    {
        var start = Position.Min(to);
        var end = End.Max(to);
        return new Rect2I(start, end - start);
    }

    public Rect2I Grow(int amount)
    {
        return new Rect2I(Position - new Vector2I(amount, amount), Size + new Vector2I(amount * 2, amount * 2));
    }

    public bool Equals(Rect2I other)
    {
        return Position == other.Position && Size == other.Size;
    }

    public override bool Equals(object? obj)
    {
        return obj is Rect2I other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Position, Size);
    }

    public override string ToString()
    {
        return $"[P: {Position}, S: {Size}]";
    }

    public static bool operator ==(Rect2I left, Rect2I right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Rect2I left, Rect2I right)
    {
        return !left.Equals(right);
    }
}
