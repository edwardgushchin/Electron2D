using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Represents a floating-point two-dimensional axis-aligned rectangle.</summary>
/// <remarks>
/// The rectangle is defined by a position and size and is commonly used for fast overlap tests.
/// Most geometric operations assume non-negative size components. Call <see cref="Abs"/> before those
/// operations when a rectangle may have a negative width or height.
/// </remarks>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct Rect : IEquatable<Rect>
{
    private Vector2 _position;
    private Vector2 _size;

    /// <summary>Gets or sets the beginning corner, usually the top-left point.</summary>
    /// <value>The rectangle origin. It is normally componentwise less than or equal to <see cref="End"/>.</value>
    public Vector2 Position
    {
        readonly get => _position;
        set => _position = value;
    }

    /// <summary>Gets or sets the width and height measured from <see cref="Position"/>.</summary>
    /// <value>The rectangle size. Non-negative components are required by most geometric operations.</value>
    /// <remarks>Assignment changes <see cref="End"/> because the end is computed from position plus size.</remarks>
    public Vector2 Size
    {
        readonly get => _size;
        set => _size = value;
    }

    /// <summary>Gets or sets the ending corner.</summary>
    /// <value><see cref="Position"/> plus <see cref="Size"/>.</value>
    /// <remarks>Assignment changes <see cref="Size"/> while preserving <see cref="Position"/>.</remarks>
    public Vector2 End
    {
        readonly get => _position + _size;
        set => _size = value - _position;
    }

    /// <summary>Gets the signed rectangle area.</summary>
    /// <value><c>Size.X * Size.Y</c>.</value>
    /// <remarks>A positive product does not replace <see cref="HasArea"/> because two negative components also have a positive product.</remarks>
    public readonly float Area => _size.X * _size.Y;

    /// <summary>Initializes a rectangle from a position and size.</summary>
    /// <param name="position">The beginning corner.</param>
    /// <param name="size">The width and height.</param>
    public Rect(Vector2 position, Vector2 size)
    {
        _position = position;
        _size = size;
    }

    /// <summary>Initializes a rectangle from a position, width, and height.</summary>
    /// <param name="position">The beginning corner.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public Rect(Vector2 position, float width, float height)
        : this(position, new Vector2(width, height))
    {
    }

    /// <summary>Initializes a rectangle from position coordinates and a size.</summary>
    /// <param name="x">The horizontal position.</param>
    /// <param name="y">The vertical position.</param>
    /// <param name="size">The width and height.</param>
    public Rect(float x, float y, Vector2 size)
        : this(new Vector2(x, y), size)
    {
    }

    /// <summary>Initializes a rectangle from position coordinates, width, and height.</summary>
    /// <param name="x">The horizontal position.</param>
    /// <param name="y">The vertical position.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public Rect(float x, float y, float width, float height)
        : this(new Vector2(x, y), new Vector2(width, height))
    {
    }

    /// <summary>Returns an equivalent rectangle with a non-negative size and top-left position.</summary>
    /// <returns>The normalized rectangle.</returns>
    public readonly Rect Abs() => new(End.Min(_position), _size.Abs());

    /// <summary>Tests whether this rectangle completely encloses another rectangle.</summary>
    /// <param name="other">The candidate enclosed rectangle.</param>
    /// <returns><see langword="true"/> when both edges of <paramref name="other"/> lie within or on this rectangle.</returns>
    /// <remarks>Negative size components are unsupported; normalize either rectangle with <see cref="Abs"/> first.</remarks>
    public readonly bool Encloses(Rect other) =>
        other._position.X >= _position.X &&
        other._position.Y >= _position.Y &&
        other.End.X <= End.X &&
        other.End.Y <= End.Y;

    /// <summary>Expands the rectangle's edges when necessary to include a point.</summary>
    /// <param name="point">The point to include.</param>
    /// <returns>The expanded rectangle.</returns>
    /// <remarks>A point exactly on an existing edge does not change the rectangle.</remarks>
    public readonly Rect Expand(Vector2 point)
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
        return new Rect(begin, end - begin);
    }

    /// <summary>Gets the center point.</summary>
    /// <returns><c>Position + Size / 2</c>.</returns>
    public readonly Vector2 GetCenter() => _position + (_size * 0.5f);

    /// <summary>Gets the rectangle vertex farthest along a direction.</summary>
    /// <param name="direction">The support direction.</param>
    /// <returns>The selected vertex. A zero direction component selects the position edge for that axis.</returns>
    /// <remarks>This support mapping is suitable for collision-detection algorithms and assumes a non-negative size.</remarks>
    public readonly Vector2 GetSupport(Vector2 direction) => new(
        direction.X > 0f ? End.X : _position.X,
        direction.Y > 0f ? End.Y : _position.Y);

    /// <summary>Returns a copy extended equally on every side.</summary>
    /// <param name="amount">The amount added outward on each side; negative values shrink the rectangle.</param>
    /// <returns>The grown or shrunk rectangle.</returns>
    public readonly Rect Grow(float amount) => GrowIndividual(amount, amount, amount, amount);

    /// <summary>Returns a copy extended independently on each side.</summary>
    /// <param name="left">The amount added outward on the left.</param>
    /// <param name="top">The amount added outward on the top.</param>
    /// <param name="right">The amount added outward on the right.</param>
    /// <param name="bottom">The amount added outward on the bottom.</param>
    /// <returns>The grown or shrunk rectangle.</returns>
    public readonly Rect GrowIndividual(float left, float top, float right, float bottom) => new(
        _position.X - left,
        _position.Y - top,
        _size.X + left + right,
        _size.Y + top + bottom);

    /// <summary>Returns a copy extended on one side.</summary>
    /// <param name="side">The side to extend.</param>
    /// <param name="amount">The amount added outward; a negative value shrinks that side.</param>
    /// <returns>The grown or shrunk rectangle. An undefined <paramref name="side"/> leaves the rectangle unchanged.</returns>
    public readonly Rect GrowSide(Side side, float amount) => GrowIndividual(
        side == Side.Left ? amount : 0f,
        side == Side.Top ? amount : 0f,
        side == Side.Right ? amount : 0f,
        side == Side.Bottom ? amount : 0f);

    /// <summary>Tests whether both size components are strictly positive.</summary>
    /// <returns><see langword="true"/> when width and height are greater than zero.</returns>
    public readonly bool HasArea() => _size.X > 0f && _size.Y > 0f;

    /// <summary>Tests whether a point lies in the rectangle's half-open area.</summary>
    /// <param name="point">The point to test.</param>
    /// <returns>
    /// <see langword="true"/> when the point is on or after the left/top edges and strictly before the right/bottom edges.
    /// </returns>
    /// <remarks>Negative size components are unsupported; normalize with <see cref="Abs"/> first.</remarks>
    public readonly bool HasPoint(Vector2 point) =>
        point.X >= _position.X &&
        point.Y >= _position.Y &&
        point.X < End.X &&
        point.Y < End.Y;

    /// <summary>Returns the intersection with another rectangle.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns>The intersection, or <see langword="default"/> when the rectangles do not intersect.</returns>
    /// <remarks>
    /// Touching outer borders alone return <see langword="default"/>. A zero-size rectangle strictly inside another
    /// rectangle is considered intersecting and produces a zero-size result at its own position. Negative size
    /// components are unsupported.
    /// </remarks>
    public readonly Rect Intersection(Rect other)
    {
        if (!Intersects(other))
            return default;

        var position = _position.Max(other._position);
        return new Rect(position, End.Min(other.End) - position);
    }

    /// <summary>Tests whether this rectangle overlaps another rectangle.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <param name="includeBorders">Whether touching borders count as an intersection.</param>
    /// <returns><see langword="true"/> when the rectangles overlap under the selected border rule.</returns>
    /// <remarks>Negative size components are unsupported; normalize either rectangle with <see cref="Abs"/> first.</remarks>
    public readonly bool Intersects(Rect other, bool includeBorders = false)
    {
        if (includeBorders)
        {
            return _position.X <= other.End.X && End.X >= other._position.X &&
                   _position.Y <= other.End.Y && End.Y >= other._position.Y;
        }

        return _position.X < other.End.X && End.X > other._position.X &&
               _position.Y < other.End.Y && End.Y > other._position.Y;
    }

    /// <summary>Tests whether all position and size components are finite.</summary>
    /// <returns><see langword="true"/> when no component is NaN or infinity.</returns>
    public readonly bool IsFinite() =>
        Mathf.IsFinite(_position.X) &&
        Mathf.IsFinite(_position.Y) &&
        Mathf.IsFinite(_size.X) &&
        Mathf.IsFinite(_size.Y);

    /// <summary>Tests position and size for scale-aware approximate equality.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns><see langword="true"/> when every component is approximately equal.</returns>
    public readonly bool IsEqualApprox(Rect other) =>
        Mathf.IsEqualApprox(_position.X, other._position.X) &&
        Mathf.IsEqualApprox(_position.Y, other._position.Y) &&
        Mathf.IsEqualApprox(_size.X, other._size.X) &&
        Mathf.IsEqualApprox(_size.Y, other._size.Y);

    /// <summary>Returns the smallest edge-aligned rectangle enclosing this rectangle and another.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns>The merged rectangle.</returns>
    /// <remarks>Negative size components are unsupported; normalize either rectangle with <see cref="Abs"/> first.</remarks>
    public readonly Rect Merge(Rect other)
    {
        var position = _position.Min(other._position);
        return new Rect(position, End.Max(other.End) - position);
    }

    /// <summary>Tests both position and size for exact component equality.</summary>
    /// <param name="left">The first rectangle.</param>
    /// <param name="right">The second rectangle.</param>
    /// <returns><see langword="true"/> when all four components are exactly equal.</returns>
    public static bool operator ==(Rect left, Rect right) => left.Equals(right);

    /// <summary>Tests whether either position or size differs under exact component equality.</summary>
    /// <param name="left">The first rectangle.</param>
    /// <param name="right">The second rectangle.</param>
    /// <returns><see langword="true"/> when at least one component differs.</returns>
    public static bool operator !=(Rect left, Rect right) => !left.Equals(right);

    /// <summary>Tests whether another object is an exactly equal rectangle.</summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is a rectangle with exactly equal components.</returns>
    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Rect other && Equals(other);

    /// <summary>Tests position and size for exact component equality.</summary>
    /// <param name="other">The other rectangle.</param>
    /// <returns><see langword="true"/> when all four components are exactly equal.</returns>
    public readonly bool Equals(Rect other) => _position == other._position && _size == other._size;

    /// <summary>Returns a hash code based on position and size.</summary>
    /// <returns>The component hash code.</returns>
    public override readonly int GetHashCode() => HashCode.Combine(_position, _size);

    /// <summary>Formats position and size using invariant culture.</summary>
    /// <returns>A string containing the position followed by the size.</returns>
    public override readonly string ToString() => ToString(null);

    /// <summary>Formats position and size with a numeric format and invariant culture.</summary>
    /// <param name="format">A standard or custom numeric format, or <see langword="null"/> for the default format.</param>
    /// <returns>A string containing the position followed by the size.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is invalid.</exception>
    public readonly string ToString(string? format) => $"{_position.ToString(format)}, {_size.ToString(format)}";

}
