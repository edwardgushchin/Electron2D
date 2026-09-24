using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Represents an integer two-dimensional axis-aligned rectangle.</summary>
/// <remarks>
/// The rectangle is defined by a position and size and is commonly used for pixel, image-region,
/// atlas, and grid bounds. Most geometric operations assume non-negative size components. Call
/// <see cref="Abs"/> before those operations when a rectangle may have a negative width or height.
/// Integer arithmetic uses unchecked 32-bit wraparound except where a documented managed operation throws.
/// </remarks>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct RectI : IEquatable<RectI>
{
    private Vector2I _position;
    private Vector2I _size;

    /// <summary>Gets or sets the beginning corner, usually the top-left integer point.</summary>
    /// <value>The rectangle origin. It is normally componentwise less than or equal to <see cref="End"/>.</value>
    public Vector2I Position
    {
        readonly get => _position;
        set => _position = value;
    }

    /// <summary>Gets or sets the integer width and height measured from <see cref="Position"/>.</summary>
    /// <value>The rectangle size. Non-negative components are required by most geometric operations.</value>
    /// <remarks>Assignment changes <see cref="End"/> because the end is computed from position plus size.</remarks>
    public Vector2I Size
    {
        readonly get => _size;
        set => _size = value;
    }

    /// <summary>Gets or sets the ending corner.</summary>
    /// <value><see cref="Position"/> plus <see cref="Size"/> using unchecked integer arithmetic.</value>
    /// <remarks>Assignment changes <see cref="Size"/> while preserving <see cref="Position"/>.</remarks>
    public Vector2I End
    {
        readonly get => _position + _size;
        set => _size = value - _position;
    }

    /// <summary>Gets the signed integer rectangle area.</summary>
    /// <value><c>Size.X * Size.Y</c> using unchecked 32-bit arithmetic.</value>
    /// <remarks>A positive product does not replace <see cref="HasArea"/> because two negative components also have a positive product.</remarks>
    public readonly int Area => unchecked(_size.X * _size.Y);

    /// <summary>Initializes an integer rectangle from a position and size.</summary>
    /// <param name="position">The beginning corner.</param>
    /// <param name="size">The integer width and height.</param>
    public RectI(Vector2I position, Vector2I size)
    {
        _position = position;
        _size = size;
    }

    /// <summary>Initializes an integer rectangle from a position, width, and height.</summary>
    /// <param name="position">The beginning corner.</param>
    /// <param name="width">The integer width.</param>
    /// <param name="height">The integer height.</param>
    public RectI(Vector2I position, int width, int height)
        : this(position, new Vector2I(width, height))
    {
    }

    /// <summary>Initializes an integer rectangle from position coordinates and a size.</summary>
    /// <param name="x">The horizontal position.</param>
    /// <param name="y">The vertical position.</param>
    /// <param name="size">The integer width and height.</param>
    public RectI(int x, int y, Vector2I size)
        : this(new Vector2I(x, y), size)
    {
    }

    /// <summary>Initializes an integer rectangle from position coordinates, width, and height.</summary>
    /// <param name="x">The horizontal position.</param>
    /// <param name="y">The vertical position.</param>
    /// <param name="width">The integer width.</param>
    /// <param name="height">The integer height.</param>
    public RectI(int x, int y, int width, int height)
        : this(new Vector2I(x, y), new Vector2I(width, height))
    {
    }

    /// <summary>Returns an equivalent integer rectangle with a non-negative size and top-left position.</summary>
    /// <returns>The normalized rectangle.</returns>
    /// <remarks>The position adds the negative part of each size component before the size is made absolute.
    /// This order preserves unchecked position wraparound independently of positive size components.</remarks>
    /// <exception cref="OverflowException">A size component is <see cref="int.MinValue"/>.</exception>
    public readonly RectI Abs() => new(_position + _size.Min(0), _size.Abs());

    /// <summary>Tests whether this integer rectangle completely encloses another rectangle.</summary>
    /// <param name="other">The candidate enclosed rectangle.</param>
    /// <returns><see langword="true"/> when both edges of <paramref name="other"/> lie within or on this rectangle.</returns>
    /// <remarks>Negative size components are unsupported; normalize either rectangle with <see cref="Abs"/> first.</remarks>
    public readonly bool Encloses(RectI other) =>
        other._position.X >= _position.X &&
        other._position.Y >= _position.Y &&
        other.End.X <= End.X &&
        other.End.Y <= End.Y;

    /// <summary>Expands the integer rectangle's edges when necessary to include a point.</summary>
    /// <param name="point">The integer point to include.</param>
    /// <returns>The expanded rectangle.</returns>
    /// <remarks>A point exactly on an existing edge does not change the rectangle.</remarks>
    public readonly RectI Expand(Vector2I point)
    {
        var begin = _position;
        var end = End;
        if (point.X < begin.X)
            begin.X = point.X;
        if (point.Y < begin.Y)
            begin.Y = point.Y;
        if (point.X > end.X)
            end.X = point.X;
        if (point.Y > end.Y)
            end.Y = point.Y;
        return new RectI(begin, end - begin);
    }

    /// <summary>Gets the integer center point.</summary>
    /// <returns><c>Position + Size / 2</c>.</returns>
    /// <remarks>Odd size components round toward <see cref="Position"/>.</remarks>
    public readonly Vector2I GetCenter() => _position + (_size / 2);

    /// <summary>Returns a copy extended equally on every side.</summary>
    /// <param name="amount">The integer amount added outward on each side; a negative value shrinks.</param>
    /// <returns>The grown or shrunk rectangle.</returns>
    public readonly RectI Grow(int amount) => GrowIndividual(amount, amount, amount, amount);

    /// <summary>Returns a copy extended independently on each side.</summary>
    /// <param name="left">The amount added outward on the left.</param>
    /// <param name="top">The amount added outward on the top.</param>
    /// <param name="right">The amount added outward on the right.</param>
    /// <param name="bottom">The amount added outward on the bottom.</param>
    /// <returns>The grown or shrunk rectangle.</returns>
    public readonly RectI GrowIndividual(int left, int top, int right, int bottom) => new(
        unchecked(_position.X - left),
        unchecked(_position.Y - top),
        unchecked(_size.X + left + right),
        unchecked(_size.Y + top + bottom));

    /// <summary>Returns a copy extended on one side.</summary>
    /// <param name="side">The side to extend.</param>
    /// <param name="amount">The amount added outward; a negative value shrinks that side.</param>
    /// <returns>The grown or shrunk rectangle. An undefined <paramref name="side"/> leaves the rectangle unchanged.</returns>
    public readonly RectI GrowSide(Side side, int amount) => GrowIndividual(
        side == Side.Left ? amount : 0,
        side == Side.Top ? amount : 0,
        side == Side.Right ? amount : 0,
        side == Side.Bottom ? amount : 0);

    /// <summary>Tests whether both size components are strictly positive.</summary>
    /// <returns><see langword="true"/> when width and height are greater than zero.</returns>
    public readonly bool HasArea() => _size.X > 0 && _size.Y > 0;

    /// <summary>Tests whether an integer point lies in the rectangle's half-open area.</summary>
    /// <param name="point">The point to test.</param>
    /// <returns><see langword="true"/> when the point is on or after the left/top edges and before the right/bottom edges.</returns>
    /// <remarks>Negative size components are unsupported; normalize with <see cref="Abs"/> first.</remarks>
    public readonly bool HasPoint(Vector2I point) =>
        point.X >= _position.X &&
        point.Y >= _position.Y &&
        point.X < End.X &&
        point.Y < End.Y;

    /// <summary>Returns the intersection with another integer rectangle.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns>The intersection, or <see langword="default"/> when the rectangles do not intersect.</returns>
    /// <remarks>
    /// Touching outer borders alone return <see langword="default"/>. A zero-size rectangle strictly inside another
    /// rectangle is considered intersecting and produces a zero-size result at its own position. Negative size
    /// components are unsupported.
    /// </remarks>
    public readonly RectI Intersection(RectI other)
    {
        if (!Intersects(other))
            return default;

        var position = _position.Max(other._position);
        return new RectI(position, End.Min(other.End) - position);
    }

    /// <summary>Tests whether this integer rectangle overlaps another rectangle.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns><see langword="true"/> when the interiors overlap; touching outer borders are excluded.</returns>
    /// <remarks>Negative size components are unsupported; normalize either rectangle with <see cref="Abs"/> first.</remarks>
    public readonly bool Intersects(RectI other) =>
        _position.X < other.End.X && End.X > other._position.X &&
        _position.Y < other.End.Y && End.Y > other._position.Y;

    /// <summary>Returns the smallest axis-aligned integer rectangle enclosing this rectangle and another.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns>The merged rectangle.</returns>
    /// <remarks>Negative size components are unsupported; normalize either rectangle with <see cref="Abs"/> first.</remarks>
    public readonly RectI Merge(RectI other)
    {
        var position = _position.Min(other._position);
        return new RectI(position, End.Max(other.End) - position);
    }

    /// <summary>Tests both position and size for exact component equality.</summary>
    /// <param name="left">The first rectangle.</param>
    /// <param name="right">The second rectangle.</param>
    /// <returns><see langword="true"/> when all four components are equal.</returns>
    public static bool operator ==(RectI left, RectI right) => left.Equals(right);

    /// <summary>Tests whether either position or size differs.</summary>
    /// <param name="left">The first rectangle.</param>
    /// <param name="right">The second rectangle.</param>
    /// <returns><see langword="true"/> when at least one component differs.</returns>
    public static bool operator !=(RectI left, RectI right) => !left.Equals(right);

    /// <summary>Converts an integer rectangle to a floating-point rectangle.</summary>
    /// <param name="value">The integer rectangle to convert.</param>
    /// <returns>A floating-point rectangle with corresponding position and size components.</returns>
    /// <remarks>Large integer components can lose low-order precision.</remarks>
    public static implicit operator Rect(RectI value) => new(value._position, value._size);

    /// <summary>Converts a floating-point rectangle by truncating each position and size component toward zero.</summary>
    /// <param name="value">The floating-point rectangle to convert.</param>
    /// <returns>The truncated integer rectangle.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A component is not finite or is outside the 32-bit signed integer range.</exception>
    public static explicit operator RectI(Rect value) => new((Vector2I)value.Position, (Vector2I)value.Size);

    /// <summary>Tests whether another object is an equal integer rectangle.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is an integer rectangle with equal components.</returns>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is RectI other && Equals(other);

    /// <summary>Tests position and size for exact component equality.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns><see langword="true"/> when all four components are equal.</returns>
    public readonly bool Equals(RectI other) => _position == other._position && _size == other._size;

    /// <summary>Returns a hash code based on position and size.</summary>
    /// <returns>The component hash code.</returns>
    public override readonly int GetHashCode() => HashCode.Combine(_position, _size);

    /// <summary>Formats position and size using invariant culture.</summary>
    /// <returns>A string containing the position followed by the size.</returns>
    public override readonly string ToString() => ToString(null);

    /// <summary>Formats position and size with an integer numeric format and invariant culture.</summary>
    /// <param name="format">A standard or custom numeric format, or <see langword="null"/> for the default format.</param>
    /// <returns>A string containing the position followed by the size.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is invalid.</exception>
    public readonly string ToString(string? format) => $"{_position.ToString(format)}, {_size.ToString(format)}";
}
