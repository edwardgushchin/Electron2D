using Box2D.NET;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;

namespace Electron2D;

/// <summary>Defines reusable two-dimensional collision geometry.</summary>
/// <remarks>A <see cref="CollisionShape"/> borrows a Shape resource; callers retain its ownership.</remarks>
public abstract class Shape : Resource
{
    private ulong _revision;

    /// <summary>Gets the local bounding rectangle of the shape.</summary>
    /// <returns>A rectangle centered on the shape origin.</returns>
    public abstract Rect2 GetRect();

    internal abstract B2ShapeId AddToBody(B2BodyId bodyID, Vector2 localPosition, float localRotation, in B2ShapeDef definition);

    internal ulong GeometryRevision => _revision;

    internal void EmitGeometryChanged()
    {
        _revision++;
        EmitChanged();
    }

    internal static B2Vec2 ToBackend(Vector2 value) => new(value.X * PhysicsSpace.MetersPerUnit, value.Y * PhysicsSpace.MetersPerUnit);
}

/// <summary>A circular collision shape with a configurable radius.</summary>
public sealed class CircleShape : Shape
{
    private float _radius = 10f;

    /// <summary>Creates a circle with the default ten-unit radius.</summary>
    public CircleShape() { }

    /// <summary>Gets or sets the positive finite radius in scene units.</summary>
    /// <value>Ten by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is not positive and finite, or its diameter would overflow.</exception>
    public float Radius
    {
        get { ThrowIfDisposed(); return _radius; }
        set
        {
            ThrowIfDisposed();
            if (!float.IsFinite(value) || value <= 0 || value > float.MaxValue / 2)
                throw new ArgumentOutOfRangeException(nameof(value));
            _radius = value;
            EmitGeometryChanged();
        }
    }

    /// <inheritdoc />
    public override Rect2 GetRect() { ThrowIfDisposed(); return new(-_radius, -_radius, 2 * _radius, 2 * _radius); }

    internal override B2ShapeId AddToBody(B2BodyId bodyID, Vector2 localPosition, float localRotation, in B2ShapeDef definition)
    {
        ThrowIfDisposed();
        var circle = new B2Circle { center = ToBackend(localPosition), radius = _radius * PhysicsSpace.MetersPerUnit };
        return b2CreateCircleShape(bodyID, definition, circle);
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CircleShape();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ((CircleShape)target)._radius = _radius;
    }
}

/// <summary>A vertical capsule collision shape with linked radius and full height.</summary>
/// <remarks>A direct <see cref="CollisionShape"/> borrows this resource. Center segments within the physics
/// backend's linear tolerance use a circular fixture while public dimensions and bounds remain exact.</remarks>
public sealed class CapsuleShape : Shape
{
    private float _radius = 10f;
    private float _height = 30f;

    /// <summary>Creates a capsule with ten-unit radius and thirty-unit full height.</summary>
    public CapsuleShape() { }

    /// <summary>Gets or sets the nonnegative finite end-cap radius in scene units.</summary>
    /// <value>Ten by default; increasing it beyond half the height also increases the height.</value>
    /// <exception cref="ArgumentOutOfRangeException">The radius is negative, nonfinite, or its diameter overflows.</exception>
    public float Radius
    {
        get { ThrowIfDisposed(); return _radius; }
        set
        {
            ThrowIfDisposed();
            if (!float.IsFinite(value) || value < 0 || value > float.MaxValue / 2)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (_radius == value) return;
            _radius = value;
            _height = MathF.Max(_height, 2 * value);
            EmitGeometryChanged();
        }
    }

    /// <summary>Gets or sets the nonnegative finite full height, including both end caps.</summary>
    /// <value>Thirty by default; decreasing it below twice the radius also decreases the radius.</value>
    /// <exception cref="ArgumentOutOfRangeException">The height is negative or nonfinite.</exception>
    public float Height
    {
        get { ThrowIfDisposed(); return _height; }
        set
        {
            ThrowIfDisposed();
            if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (_height == value) return;
            _height = value;
            _radius = MathF.Min(_radius, value * 0.5f);
            EmitGeometryChanged();
        }
    }

    /// <summary>Gets or sets the center segment height between the two end-cap centers.</summary>
    /// <value>Ten by default; this is <see cref="Height"/> minus twice <see cref="Radius"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">The height is negative, nonfinite, or makes the full height overflow.</exception>
    public float MidHeight
    {
        get { ThrowIfDisposed(); return _height - 2 * _radius; }
        set
        {
            ThrowIfDisposed();
            var height = value + 2 * _radius;
            if (!float.IsFinite(value) || value < 0 || !float.IsFinite(height))
                throw new ArgumentOutOfRangeException(nameof(value));
            _height = height;
            EmitGeometryChanged();
        }
    }

    /// <inheritdoc />
    public override Rect2 GetRect()
    {
        ThrowIfDisposed();
        return new(-_radius, -_height * 0.5f, 2 * _radius, _height);
    }

    internal override B2ShapeId AddToBody(B2BodyId bodyID, Vector2 localPosition, float localRotation, in B2ShapeDef definition)
    {
        ThrowIfDisposed();
        var midHeight = (_height - 2 * _radius) * PhysicsSpace.MetersPerUnit;
        var circle = new B2Circle { center = ToBackend(localPosition), radius = _radius * PhysicsSpace.MetersPerUnit };
        if (midHeight <= B2_LINEAR_SLOP)
            return b2CreateCircleShape(bodyID, definition, circle);
        var offset = new Vector2(0, (_height - 2 * _radius) * 0.5f).Rotated(localRotation);
        var capsule = new B2Capsule(ToBackend(localPosition - offset), ToBackend(localPosition + offset),
            _radius * PhysicsSpace.MetersPerUnit);
        var id = b2CreateCapsuleShape(bodyID, definition, capsule);
        return id.index1 == 0 ? b2CreateCircleShape(bodyID, definition, circle) : id;
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CapsuleShape();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ((CapsuleShape)target)._radius = _radius;
        ((CapsuleShape)target)._height = _height;
    }
}

/// <summary>A rectangular collision shape centered at its origin.</summary>
public sealed class RectangleShape : Shape
{
    private Vector2 _size = new(20, 20);

    /// <summary>Creates a rectangle with the default twenty-unit dimensions.</summary>
    public RectangleShape() { }

    /// <summary>Gets or sets the finite positive width and height in scene units.</summary>
    /// <value>(20, 20) by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">Either component is not positive and finite.</exception>
    public Vector2 Size
    {
        get { ThrowIfDisposed(); return _size; }
        set
        {
            ThrowIfDisposed();
            if (!value.IsFinite() || value.X <= 0 || value.Y <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            _size = value;
            EmitGeometryChanged();
        }
    }

    /// <inheritdoc />
    public override Rect2 GetRect() { ThrowIfDisposed(); return new(-_size.X / 2, -_size.Y / 2, _size.X, _size.Y); }

    internal override B2ShapeId AddToBody(B2BodyId bodyID, Vector2 localPosition, float localRotation, in B2ShapeDef definition)
    {
        ThrowIfDisposed();
        var polygon = b2MakeOffsetBox(_size.X * PhysicsSpace.MetersPerUnit / 2,
            _size.Y * PhysicsSpace.MetersPerUnit / 2, ToBackend(localPosition), B2MathFunction.b2MakeRot(localRotation));
        return b2CreatePolygonShape(bodyID, definition, polygon);
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new RectangleShape();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ((RectangleShape)target)._size = _size;
    }
}
