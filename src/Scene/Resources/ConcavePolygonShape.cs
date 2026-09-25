using Box2D.NET;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2MathFunction;

namespace Electron2D;

/// <summary>A hollow collection of independent two-sided collision segments.</summary>
/// <remarks>Each consecutive pair of <see cref="Segments"/> forms one fixture. A direct
/// <see cref="CollisionShape"/> borrows this caller-owned resource.</remarks>
public sealed class ConcavePolygonShape : Shape
{
    private Vector2[] _segments = [];

    /// <summary>Creates an empty segment collection with no collision fixtures.</summary>
    public ConcavePolygonShape() { }

    /// <summary>Gets or sets paired local endpoints for the hollow contour.</summary>
    /// <value>Empty by default. Every consecutive pair forms one independent segment.</value>
    /// <remarks>Reads and writes copy arrays. Every successful assignment emits a resource change,
    /// including an equal assignment; empty input removes all fixtures.</remarks>
    /// <exception cref="ArgumentNullException">The assigned array is null.</exception>
    /// <exception cref="ArgumentException">The array has odd length or a coordinate or span exceeds the finite range.</exception>
    public Vector2[] Segments
    {
        get { ThrowIfDisposed(); return (Vector2[])_segments.Clone(); }
        set
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(value);
            if ((value.Length & 1) != 0) throw new ArgumentException("Segments require paired endpoints.", nameof(value));
            var copy = (Vector2[])value.Clone();
            var min = copy.Length == 0 ? Vector2.Zero : copy[0];
            var max = min;
            for (var index = 0; index < copy.Length; index++)
            {
                var point = copy[index];
                if (!point.IsFinite() || ((index & 1) != 0 && !(point - copy[index - 1]).IsFinite()))
                    throw new ArgumentException("Segment endpoints must have finite bounds.", nameof(value));
                min = new(MathF.Min(min.X, point.X), MathF.Min(min.Y, point.Y));
                max = new(MathF.Max(max.X, point.X), MathF.Max(max.Y, point.Y));
            }
            if (!(max - min).IsFinite()) throw new ArgumentException("Contour bounds exceed the finite range.", nameof(value));
            _segments = copy;
            EmitGeometryChanged();
        }
    }

    /// <inheritdoc />
    public override Rect2 GetRect()
    {
        ThrowIfDisposed();
        if (_segments.Length == 0) return default;
        var min = _segments[0];
        var max = min;
        foreach (var point in _segments)
        {
            min = new(MathF.Min(min.X, point.X), MathF.Min(min.Y, point.Y));
            max = new(MathF.Max(max.X, point.X), MathF.Max(max.Y, point.Y));
        }
        return new(min.X, min.Y, max.X - min.X, max.Y - min.Y);
    }

    internal override void AppendToBody(B2BodyId bodyID, Vector2 localPosition, float localRotation,
        in B2ShapeDef definition, List<B2ShapeId> fixtures)
    {
        ThrowIfDisposed();
        for (var index = 0; index < _segments.Length; index += 2)
        {
            var first = ToBackend(localPosition + _segments[index].Rotated(localRotation));
            var second = ToBackend(localPosition + _segments[index + 1].Rotated(localRotation));
            fixtures.Add(CreateSegmentOrPoint(bodyID, definition, first, second));
        }
    }

    internal override void AppendQueryProxies(List<B2ShapeProxy> proxies)
    {
        ThrowIfDisposed();
        for (var index = 0; index < _segments.Length; index += 2)
        {
            var first = ToBackend(_segments[index]);
            var second = ToBackend(_segments[index + 1]);
            proxies.Add(b2DistanceSquared(first, second) <= B2_LINEAR_SLOP * B2_LINEAR_SLOP
                ? b2MakeProxy(new B2Vec2(first.X + (second.X - first.X) * 0.5f,
                        first.Y + (second.Y - first.Y) * 0.5f), 1, 0)
                : b2MakeProxy(first, second, 2, 0));
        }
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new ConcavePolygonShape();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ((ConcavePolygonShape)target)._segments = (Vector2[])_segments.Clone();
    }
}
