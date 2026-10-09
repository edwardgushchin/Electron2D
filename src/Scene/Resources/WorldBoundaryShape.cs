namespace Electron2D;

/// <summary>An infinite solid half-plane defined by Normal.Dot(point) &lt;= Distance.</summary>
/// <remarks>The normal points into the free half-plane. Its magnitude is retained; the physical
/// signed offset is Distance divided by the normal length. This borrowed resource contributes no
/// geometric mass or inertia. Two world boundaries do not collide with each other.</remarks>
public sealed class WorldBoundaryShape : Shape
{
    private Vector2 _normal = Vector2.Up;
    private float _distance;

    /// <summary>Creates a horizontal boundary through the origin with its free side above it.</summary>
    public WorldBoundaryShape() { }

    /// <summary>Gets or sets the finite nonzero normal pointing into the free half-plane.</summary>
    /// <value>Vector2.Up by default; nonunit magnitudes are preserved.</value>
    /// <exception cref="ArgumentOutOfRangeException">The normal is zero/nonfinite or the resulting offset is not representable.</exception>
    public Vector2 Normal
    {
        get { ThrowIfDisposed(); return _normal; }
        set { ThrowIfDisposed(); Normalize(value, _distance); if (_normal == value) return; _normal = value; EmitGeometryChanged(); }
    }

    /// <summary>Gets or sets the finite right-hand side of the local line equation Normal.Dot(point) = Distance.</summary>
    /// <value>Zero by default, expressed in scene units multiplied by the normal magnitude.</value>
    /// <exception cref="ArgumentOutOfRangeException">The distance or normalized offset is not representable.</exception>
    public float Distance
    {
        get { ThrowIfDisposed(); return _distance; }
        set { ThrowIfDisposed(); Normalize(_normal, value); if (_distance == value) return; _distance = value; EmitGeometryChanged(); }
    }

    internal static (Vector2 Normal, float Distance) Normalize(Vector2 normal, float distance)
    {
        if (!normal.IsFinite() || normal == Vector2.Zero || !float.IsFinite(distance)) throw new ArgumentOutOfRangeException(nameof(normal));
        var length = Math.Sqrt((double)normal.X * normal.X + (double)normal.Y * normal.Y);
        var offset = (float)(distance / length);
        if (!float.IsFinite(offset) || !float.IsFinite((offset * PhysicsSpace.MetersPerUnit) * (offset * PhysicsSpace.MetersPerUnit)))
            throw new ArgumentOutOfRangeException(nameof(distance));
        return (new((float)(normal.X / length), (float)(normal.Y / length)), offset);
    }

    /// <summary>Gets the finite local editing-marker envelope of this infinite shape.</summary>
    /// <returns>A 200-unit tangent segment and a thirty-unit outward normal at the nearest boundary point.</returns>
    public override Rect2 GetRect()
    {
        ThrowIfDisposed(); var plane = Normalize(_normal, _distance); var point = plane.Normal * plane.Distance;
        var tangent = new Vector2(-plane.Normal.Y, plane.Normal.X) * 100;
        return new Rect2(point - tangent, Vector2.Zero).Expand(point + tangent).Expand(point + plane.Normal * 30);
    }

    internal override PhysicsShapeGeometry GetGeometry()
    {
        ThrowIfDisposed(); var plane = Normalize(_normal, _distance);
        return new() { Kind = PhysicsShapeGeometry.ShapeKind.WorldBoundary, A = plane.Normal, Radius = plane.Distance };
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new WorldBoundaryShape();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (WorldBoundaryShape)target; copy._normal = _normal; copy._distance = _distance;
    }
}
