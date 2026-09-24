using Box2D.NET;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;

namespace Electron2D;

/// <summary>Defines reusable two-dimensional collision geometry.</summary>
/// <remarks>A <see cref="CollisionShape"/> borrows a Shape resource; callers retain its ownership.</remarks>
public abstract class Shape : Resource
{
    /// <summary>Gets the local bounding rectangle of the shape.</summary>
    /// <returns>A rectangle centered on the shape origin.</returns>
    public abstract Rect2 GetRect();

    internal abstract B2ShapeId AddToBody(B2BodyId bodyID, Vector2 localPosition, float localRotation, in B2ShapeDef definition);

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
            EmitChanged();
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
            EmitChanged();
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
