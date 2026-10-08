using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Electron2D;

/// <summary>Owns the body and fixture attachment shared by scene and server colliders.</summary>
internal sealed class PhysicsColliderBackend(RID rid, CollisionObject? sceneOwner = null)
{
    private readonly List<B2ShapeId> _shapes = [];
    private readonly WeakReference<CollisionObject>? _sceneOwner = sceneOwner is null ? null : new(sceneOwner);

    internal PhysicsSpace? Space { get; private set; }
    internal B2BodyId BodyID { get; private set; }
    internal IReadOnlyList<B2ShapeId> Shapes => _shapes;

    internal void Attach(PhysicsSpace space, in B2BodyDef definition)
    {
        if (Space is not null) throw new InvalidOperationException("A collider already belongs to a physics world.");
        BodyID = b2CreateBody(space.WorldID, definition);
        Space = space;
    }

    internal void Detach()
    {
        if (Space is null) return;
        if (!Space.HasBackendFailure) b2DestroyBody(BodyID);
        _shapes.Clear();
        BodyID = default;
        Space = null;
    }

    internal void RebuildShapes(IReadOnlyList<CollisionObject.ShapeSlot> slots, uint layer, uint mask,
        bool sensor, float density, float friction = 1, float bounce = 0)
    {
        foreach (var slot in slots)
        {
            if (!slot.Active) continue;
            if (!slot.Transform.IsFinite() || !slot.Transform.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(slot.Transform.Skew))
                throw new InvalidOperationException("Physics shapes require unit scale and zero skew.");
        }
        var definition = BeginShapeUpdate(layer, mask, sensor, density, friction, bounce);
        for (var index = 0; index < slots.Count; index++)
        {
            var slot = slots[index];
            if (slot.Active) AppendShape(slot.Shape, slot.Transform, index, sensor ? null : slot.OneWay, ref definition);
        }
    }

    internal void RebuildShapes(IReadOnlyList<PhysicsServerCollider.ShapeSlot> slots, uint layer, uint mask,
        bool sensor, float density, float friction = 1, float bounce = 0)
    {
        foreach (var slot in slots)
            if (!slot.Disabled) PhysicsServerCollider.ValidateTransform(slot.LocalTransform);
        var definition = BeginShapeUpdate(layer, mask, sensor, density, friction, bounce);
        for (var index = 0; index < slots.Count; index++)
        {
            var slot = slots[index];
            if (slot.Disabled || slot.Shape.Geometry.IsDisposed) continue;
            var contact = !sensor && slot.OneWay
                ? new OneWayContactData(slot.Direction.Rotated(slot.LocalTransform.Rotation), slot.Margin) : null;
            AppendShape(slot.Shape.Geometry, slot.LocalTransform, index, contact, ref definition);
        }
    }

    private B2ShapeDef BeginShapeUpdate(uint layer, uint mask, bool sensor, float density, float friction, float bounce)
    {
        if (Space is null) throw new InvalidOperationException("A collider must be attached before creating shapes.");
        foreach (var id in _shapes) b2DestroyShape(id, updateBodyMass: false);
        _shapes.Clear();
        var definition = b2DefaultShapeDef();
        // The owning body applies its complete mass profile after all pieces are created.
        definition.updateBodyMass = false;
        definition.filter.categoryBits = layer;
        definition.filter.maskBits = mask;
        definition.isSensor = sensor;
        definition.density = density;
        if (!sensor) PhysicsSpace.SetMaterial(ref definition, friction, bounce);
        return definition;
    }

    private void AppendShape(Shape shape, Transform transform, int index, OneWayContactData? oneWay, ref B2ShapeDef definition)
    {
        definition.userData = new B2UserData(new PhysicsFixtureTag(rid, index, oneWay) { SceneOwner = _sceneOwner });
        definition.enablePreSolveEvents = !definition.isSensor && (oneWay is not null || PhysicsServer.Service.HasBodyCollisionExceptions(rid));
        shape.AppendToBody(BodyID, transform.Origin, transform.Rotation, definition, _shapes);
    }
}
