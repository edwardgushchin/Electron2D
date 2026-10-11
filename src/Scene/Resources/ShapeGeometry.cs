namespace Electron2D;

/// <summary>Immutable exact local collision geometry supplied by a shape resource.</summary>
/// <remarks>Factories validate scene-unit data and copy contours. The data owns no RID, body, world or rendering
/// resource. Reuse an instance until authoring changes; all runtime geometry consumers borrow it without copying.</remarks>
public sealed partial class ShapeGeometry
{
    private readonly PhysicsShapeGeometry.ShapeKind _kind;
    private readonly Vector2 _a, _b;
    private readonly float _radius;
    private readonly Vector2[] _points;
    private readonly bool _slideOnSlope;

    private ShapeGeometry(PhysicsShapeGeometry.ShapeKind kind, Rect2 bounds, Vector2 a = default,
        Vector2 b = default, float radius = 0, Vector2[]? points = null, bool slideOnSlope = false)
    {
        _kind = kind; Bounds = bounds; _a = a; _b = b; _radius = radius;
        _points = points ?? []; _slideOnSlope = slideOnSlope;
    }

    /// <summary>Gets the actual represented collision primitive.</summary>
    /// <value>The corresponding geometry type, including Custom for a caller-defined format.</value>
    public PhysicsServer.ShapeType Type => _kind switch
    {
        PhysicsShapeGeometry.ShapeKind.Circle => PhysicsServer.ShapeType.Circle,
        PhysicsShapeGeometry.ShapeKind.Capsule => PhysicsServer.ShapeType.Capsule,
        PhysicsShapeGeometry.ShapeKind.Segment => PhysicsServer.ShapeType.Segment,
        PhysicsShapeGeometry.ShapeKind.Rectangle => PhysicsServer.ShapeType.Rectangle,
        PhysicsShapeGeometry.ShapeKind.ConvexPolygon => PhysicsServer.ShapeType.ConvexPolygon,
        PhysicsShapeGeometry.ShapeKind.ConcavePolygon => PhysicsServer.ShapeType.ConcavePolygon,
        PhysicsShapeGeometry.ShapeKind.SeparationRay => PhysicsServer.ShapeType.SeparationRay,
        PhysicsShapeGeometry.ShapeKind.Custom => PhysicsServer.ShapeType.Custom,
        _ => PhysicsServer.ShapeType.WorldBoundary
    };

    /// <summary>Gets the local bounding rectangle, or the editing-marker envelope for an infinite boundary.</summary>
    /// <value>Exact local-axis finite bounds; separation rays include drawing padding.</value>
    public Rect2 Bounds { get; }

    /// <summary>Reads the represented circle's radius.</summary>
    /// <returns>The positive scene-unit radius.</returns>
    /// <exception cref="InvalidOperationException">This data is not a circle.</exception>
    public float GetCircleRadius() { Require(PhysicsShapeGeometry.ShapeKind.Circle); return _radius; }

    /// <summary>Reads the represented vertical capsule dimensions.</summary>
    /// <returns>The radius and full height in scene units.</returns>
    /// <exception cref="InvalidOperationException">This data is not a capsule.</exception>
    public (float Radius, float Height) GetCapsuleDimensions() { Require(PhysicsShapeGeometry.ShapeKind.Capsule); return (_radius, Bounds.Size.Y); }

    /// <summary>Reads the represented segment's local endpoints.</summary>
    /// <returns>The exact finite first and second endpoints.</returns>
    /// <exception cref="InvalidOperationException">This data is not a segment.</exception>
    public (Vector2 A, Vector2 B) GetSegmentEndpoints() { Require(PhysicsShapeGeometry.ShapeKind.Segment); return (_a, _b); }

    /// <summary>Reads the represented rectangle's full dimensions.</summary>
    /// <returns>The positive scene-unit width and height.</returns>
    /// <exception cref="InvalidOperationException">This data is not a rectangle.</exception>
    public Vector2 GetRectangleSize() { Require(PhysicsShapeGeometry.ShapeKind.Rectangle); return Bounds.Size; }

    /// <summary>Borrows the represented convex contour without copying.</summary>
    /// <returns>The immutable copied input perimeter in its original order.</returns>
    /// <exception cref="InvalidOperationException">This data is not a convex polygon.</exception>
    public ReadOnlySpan<Vector2> GetConvexPolygonPoints() { Require(PhysicsShapeGeometry.ShapeKind.ConvexPolygon); return _points; }

    /// <summary>Borrows the represented concave edge endpoints without copying.</summary>
    /// <returns>The immutable copied input endpoint pairs in their original order.</returns>
    /// <exception cref="InvalidOperationException">This data is not a concave polygon.</exception>
    public ReadOnlySpan<Vector2> GetConcavePolygonSegments() { Require(PhysicsShapeGeometry.ShapeKind.ConcavePolygon); return _points; }

    /// <summary>Reads the represented directed ray's physical parameters.</summary>
    /// <returns>The positive-local-Y length and slope policy.</returns>
    /// <exception cref="InvalidOperationException">This data is not a separation ray.</exception>
    public (float Length, bool SlideOnSlope) GetSeparationRayParameters() { Require(PhysicsShapeGeometry.ShapeKind.SeparationRay); return (_b.Y, _slideOnSlope); }

    /// <summary>Reads the represented infinite plane's normalized equation.</summary>
    /// <returns>The unit outward normal and signed scene-unit distance.</returns>
    /// <exception cref="InvalidOperationException">This data is not a world boundary.</exception>
    public (Vector2 Normal, float Distance) GetWorldBoundaryPlane() { Require(PhysicsShapeGeometry.ShapeKind.WorldBoundary); return (_a, _radius); }

    private void Require(PhysicsShapeGeometry.ShapeKind kind)
    {
        if (_kind != kind) throw new InvalidOperationException("The geometry has a different primitive type.");
    }

    /// <summary>Creates a circle centered at the local origin.</summary>
    /// <param name="radius">Positive finite radius with a representable diameter.</param>
    /// <returns>Immutable circle data.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The radius or diameter is invalid.</exception>
    public static ShapeGeometry Circle(float radius)
    {
        if (!float.IsFinite(radius) || radius <= 0 || radius > float.MaxValue / 2) throw new ArgumentOutOfRangeException(nameof(radius));
        return new(PhysicsShapeGeometry.ShapeKind.Circle, new(-radius, -radius, radius * 2, radius * 2), radius: radius);
    }

    /// <summary>Creates a vertical capsule centered at the local origin.</summary>
    /// <param name="radius">Finite nonnegative end-cap radius.</param>
    /// <param name="height">Finite full height, at least twice the radius; zero dimensions represent a point.</param>
    /// <returns>Immutable capsule data.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Dimensions are invalid or inconsistent.</exception>
    public static ShapeGeometry Capsule(float radius, float height)
    {
        if (!float.IsFinite(radius) || radius < 0 || radius > float.MaxValue / 2) throw new ArgumentOutOfRangeException(nameof(radius));
        if (!float.IsFinite(height) || height < radius * 2) throw new ArgumentOutOfRangeException(nameof(height));
        var half = (height - radius * 2) * .5f;
        return new(PhysicsShapeGeometry.ShapeKind.Capsule, new(-radius, -height * .5f, radius * 2, height), new(0, -half), new(0, half), radius);
    }

    /// <summary>Creates a two-sided segment, including a coincident point.</summary>
    /// <param name="a">Finite first local endpoint.</param>
    /// <param name="b">Finite second endpoint whose span from the first is representable.</param>
    /// <returns>Immutable segment data.</returns>
    /// <exception cref="ArgumentException">Endpoints or their span are invalid.</exception>
    public static ShapeGeometry Segment(Vector2 a, Vector2 b)
    {
        if (!a.IsFinite() || !b.IsFinite() || !(b - a).IsFinite()) throw new ArgumentException("Segment endpoints require a finite span.");
        return new(PhysicsShapeGeometry.ShapeKind.Segment, BoundsOf([a, b]), a, b);
    }

    /// <summary>Creates a filled rectangle centered at the local origin.</summary>
    /// <param name="size">Finite positive full width and height.</param>
    /// <returns>Immutable rectangle data.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A size component is nonpositive or nonfinite.</exception>
    public static ShapeGeometry Rectangle(Vector2 size)
    {
        if (!size.IsFinite() || size.X <= 0 || size.Y <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        return new(PhysicsShapeGeometry.ShapeKind.Rectangle, new(-size * .5f, size), -size * .5f, size * .5f);
    }

    /// <summary>Creates a filled convex contour with any supported vertex count.</summary>
    /// <param name="points">Perimeter-ordered finite vertices in either winding; an empty span creates no fixtures.</param>
    /// <returns>Immutable data with its own copied contour.</returns>
    /// <exception cref="ArgumentException">A nonempty contour is crossing, nonconvex, degenerate or outside the solver range.</exception>
    public static ShapeGeometry ConvexPolygon(ReadOnlySpan<Vector2> points)
    {
        var copy = points.ToArray(); PhysicsShapeBackend.ValidatePolygonData(copy);
        return new(PhysicsShapeGeometry.ShapeKind.ConvexPolygon, BoundsOf(copy), points: copy);
    }

    /// <summary>Creates a hollow collection of two-sided paired edges.</summary>
    /// <param name="segments">An even-length span of finite endpoint pairs; empty input clears geometry.</param>
    /// <returns>Immutable data with its own copied endpoints.</returns>
    /// <exception cref="ArgumentException">Input is unpaired or a coordinate, edge span or total bounds is nonfinite.</exception>
    public static ShapeGeometry ConcavePolygon(ReadOnlySpan<Vector2> segments)
    {
        if ((segments.Length & 1) != 0) throw new ArgumentException("Segments require paired endpoints.", nameof(segments));
        var copy = segments.ToArray();
        for (var i = 1; i < copy.Length; i += 2)
            if (!(copy[i] - copy[i - 1]).IsFinite()) throw new ArgumentException("Segment spans must be finite.", nameof(segments));
        return new(PhysicsShapeGeometry.ShapeKind.ConcavePolygon, BoundsOf(copy), points: copy);
    }

    /// <summary>Creates a directed separation ray along the positive local Y axis.</summary>
    /// <param name="length">Finite nonnegative physical length within the solver range.</param>
    /// <param name="slideOnSlope">Uses the intersected surface normal for separation when true.</param>
    /// <returns>Immutable ray data, including its finite drawing envelope.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The length is invalid or outside the solver range.</exception>
    public static ShapeGeometry SeparationRay(float length, bool slideOnSlope = false)
    {
        var meters = length * PhysicsSpace.MetersPerUnit;
        if (!float.IsFinite(length) || length < 0 || !float.IsFinite(meters * meters)) throw new ArgumentOutOfRangeException(nameof(length));
        return new(PhysicsShapeGeometry.ShapeKind.SeparationRay, new Rect2(0, 0, 0, length).Grow(MathF.Sqrt(.5f) * 4),
            b: new(0, length), slideOnSlope: slideOnSlope);
    }

    /// <summary>Creates the infinite solid half-plane normal.Dot(point) less than or equal to distance.</summary>
    /// <param name="normal">Finite nonzero local outward normal; its magnitude need not be one.</param>
    /// <param name="distance">Finite signed plane offset multiplied by the normal magnitude.</param>
    /// <returns>Immutable normalized infinite boundary data; bounds describe only its editing marker.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The plane or normalized offset is not representable.</exception>
    public static ShapeGeometry WorldBoundary(Vector2 normal, float distance)
    {
        var plane = WorldBoundaryShape.Normalize(normal, distance); var point = plane.Normal * plane.Distance;
        var tangent = new Vector2(-plane.Normal.Y, plane.Normal.X) * 100;
        var bounds = new Rect2(point - tangent, Vector2.Zero).Expand(point + tangent).Expand(point + plane.Normal * 30);
        return new(PhysicsShapeGeometry.ShapeKind.WorldBoundary, bounds, a: plane.Normal, radius: plane.Distance);
    }

    private static Rect2 BoundsOf(ReadOnlySpan<Vector2> points)
    {
        if (points.IsEmpty) return default;
        var min = points[0]; var max = min;
        foreach (var point in points)
        {
            if (!point.IsFinite()) throw new ArgumentException("Geometry coordinates must be finite.");
            min = min.Min(point); max = max.Max(point);
        }
        if (!(max - min).IsFinite()) throw new ArgumentException("Geometry bounds must be finite.");
        return new(min, max - min);
    }
    internal PhysicsShapeGeometry Borrow() => new() { Kind = _kind, A = _a, B = _b, Radius = _radius, Points = _points, SlideOnSlope = _slideOnSlope };
}
