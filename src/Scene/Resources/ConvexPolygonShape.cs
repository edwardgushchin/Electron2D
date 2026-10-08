namespace Electron2D;

/// <summary>A solid convex polygon collision resource with an arbitrary vertex count.</summary>
/// <remarks>Polygons wider than one backend fixture are partitioned into convex pieces. A direct
/// <see cref="CollisionShape"/> borrows this caller-owned resource.</remarks>
public sealed class ConvexPolygonShape : Shape
{
    private Vector2[] _points = [];

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
            PhysicsShapeBackend.ValidateAndCachePolygon(this, copy);
            _points = copy;
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

    internal override PhysicsShapeGeometry GetGeometry()
    {
        ThrowIfDisposed();
        return new() { Kind = PhysicsShapeGeometry.ShapeKind.ConvexPolygon, Points = _points };
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new ConvexPolygonShape();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (ConvexPolygonShape)target;
        copy._points = (Vector2[])_points.Clone();
    }
}
