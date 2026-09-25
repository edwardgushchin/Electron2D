using Box2D.NET;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;

namespace Electron2D;

/// <summary>A solid convex polygon collision resource with an arbitrary vertex count.</summary>
/// <remarks>Polygons wider than one backend fixture are partitioned into convex pieces. A direct
/// <see cref="CollisionShape"/> borrows this caller-owned resource.</remarks>
public sealed class ConvexPolygonShape : Shape
{
    private Vector2[] _points = [];
    private B2Hull[] _hulls = [];

    /// <summary>Creates an empty polygon with no collision fixture.</summary>
    public ConvexPolygonShape() { }

    /// <summary>Gets or sets a caller-owned array of perimeter-ordered convex vertices.</summary>
    /// <value>Empty by default; clockwise and counterclockwise orders are accepted.</value>
    /// <remarks>Assignment copies the input and emits a resource change, including an equal assignment.
    /// A repeated first vertex may close the contour. Empty input clears all fixtures.</remarks>
    /// <exception cref="ArgumentNullException">The assigned array is null.</exception>
    /// <exception cref="ArgumentException">The nonempty contour is nonfinite, nonconvex, degenerate or outside the finite solver range.</exception>
    public Vector2[] Points
    {
        get { ThrowIfDisposed(); return (Vector2[])_points.Clone(); }
        set
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(value);
            var copy = (Vector2[])value.Clone();
            var hulls = BuildHulls(copy);
            _points = copy;
            _hulls = hulls;
            EmitGeometryChanged();
        }
    }

    /// <summary>Computes the convex hull of a point cloud and assigns it as <see cref="Points"/>.</summary>
    /// <param name="pointCloud">Finite input points in any order.</param>
    /// <exception cref="ArgumentException">The cloud has fewer than three noncollinear hull points or an invalid coordinate.</exception>
    public void SetPointCloud(ReadOnlySpan<Vector2> pointCloud)
    {
        ThrowIfDisposed();
        foreach (var point in pointCloud)
            if (!point.IsFinite()) throw new ArgumentException("Point-cloud coordinates must be finite.", nameof(pointCloud));
        var hull = Geometry.ConvexHull(pointCloud);
        if (hull.Length < 4) throw new ArgumentException("A convex polygon needs three noncollinear points.", nameof(pointCloud));
        Points = hull;
    }

    /// <inheritdoc />
    public override Rect2 GetRect()
    {
        ThrowIfDisposed();
        if (_points.Length == 0) return default;
        var min = _points[0];
        var max = min;
        foreach (var point in _points)
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
        var position = ToBackend(localPosition);
        var rotation = b2MakeRot(localRotation);
        foreach (ref readonly var hull in _hulls.AsSpan())
        {
            var polygon = b2MakeOffsetPolygon(hull, position, rotation);
            fixtures.Add(b2CreatePolygonShape(bodyID, definition, polygon));
        }
    }

    internal override void AppendQueryProxies(List<B2ShapeProxy> proxies)
    {
        ThrowIfDisposed();
        foreach (ref readonly var hull in _hulls.AsSpan())
            proxies.Add(b2MakeProxy(hull.points.AsSpan(), hull.count, 0));
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new ConvexPolygonShape();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (ConvexPolygonShape)target;
        copy._points = (Vector2[])_points.Clone();
        copy._hulls = (B2Hull[])_hulls.Clone();
    }

    private static B2Hull[] BuildHulls(Vector2[] points)
    {
        if (points.Length == 0) return [];
        var min = points[0];
        var max = min;
        foreach (var point in points)
        {
            if (!point.IsFinite()) throw new ArgumentException("Polygon coordinates must be finite.", nameof(points));
            min = new(MathF.Min(min.X, point.X), MathF.Min(min.Y, point.Y));
            max = new(MathF.Max(max.X, point.X), MathF.Max(max.Y, point.Y));
        }
        if (!(max - min).IsFinite()) throw new ArgumentException("Polygon bounds exceed the finite range.", nameof(points));
        var count = points.Length;
        if (count > 1 && points[0] == points[^1]) count--;
        if (count < 3) throw new ArgumentException("A convex polygon needs three distinct perimeter points.", nameof(points));
        // ponytail: Pairwise checks are quadratic; index edges if large authoring contours show a measured cost.
        for (var first = 0; first < count; first++)
            for (var second = first + 2; second < count; second++)
            {
                if (first == 0 && second == count - 1) continue;
                if (Geometry.SegmentIntersectsSegment(points[first], points[(first + 1) % count],
                        points[second], points[(second + 1) % count]) is not null)
                    throw new ArgumentException("Polygon edges must not cross.", nameof(points));
            }

        double sign = 0;
        for (var index = 0; index < count; index++)
        {
            var first = points[index];
            var second = points[(index + 1) % count];
            var third = points[(index + 2) % count];
            var cross = ((double)second.X - first.X) * (third.Y - second.Y) -
                ((double)second.Y - first.Y) * (third.X - second.X);
            if (cross == 0) continue;
            if (sign != 0 && Math.Sign(cross) != Math.Sign(sign))
                throw new ArgumentException("Points must follow a convex perimeter.", nameof(points));
            sign = cross;
        }
        if (sign == 0) throw new ArgumentException("Polygon vertices must span a finite area.", nameof(points));

        var hulls = new List<B2Hull>();
        Span<B2Vec2> piece = stackalloc B2Vec2[B2_MAX_POLYGON_VERTICES];
        for (var start = 1; start < count - 1;)
        {
            var end = Math.Min(start + B2_MAX_POLYGON_VERTICES - 2, count - 1);
            piece[0] = ToBackend(points[0]);
            for (var index = start; index <= end; index++) piece[index - start + 1] = ToBackend(points[index]);
            var hull = b2ComputeHull(piece, end - start + 2);
            if (hull.count < 3)
                throw new ArgumentException("A polygon piece is below the physics solver's size tolerance.", nameof(points));
            hulls.Add(hull);
            start = end;
        }
        return hulls.ToArray();
    }
}
