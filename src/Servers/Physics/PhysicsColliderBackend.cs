using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Electron2D;

/// <summary>Owns shared scene/server body and fixture attachments and adapts live motion to engine units.</summary>
internal sealed partial class PhysicsColliderBackend(RID rid, CollisionObject? sceneOwner = null)
{
    private readonly List<B2ShapeId> _shapes = [];
    private readonly WeakReference<CollisionObject>? _sceneOwner = sceneOwner is null ? null : new(sceneOwner);

    internal PhysicsSpace? Space { get; private set; }
    internal B2BodyId BodyID { get; private set; }
    internal long AttachmentVersion { get; private set; }
    internal IReadOnlyList<B2ShapeId> Shapes => _shapes;
    internal float CollisionPriority { get; private set; } = 1;
    internal ulong CanvasInstanceID { get; set; }

    internal static void ValidateCollisionPriority(float value)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
    }
    internal void SetCollisionPriority(float value)
    {
        Space?.EnsureQueryAccess(); ValidateCollisionPriority(value); CollisionPriority = value;
    }

    internal void Attach(PhysicsSpace space, Vector2 position, float rotation, in PhysicsBodyConfiguration configuration)
    {
        if (Space is not null) throw new InvalidOperationException("A collider already belongs to a physics world.");
        var version = checked(AttachmentVersion + 1);
        var definition = b2DefaultBodyDef();
        definition.type = BodyType(configuration.Mode);
        definition.position = PhysicsShapeBackend.ToBackend(position);
        definition.rotation = B2MathFunction.b2MakeRot(rotation);
        definition.linearVelocity = PhysicsShapeBackend.ToBackend(configuration.LinearVelocity);
        definition.angularVelocity = configuration.AngularVelocity;
        definition.gravityScale = configuration.GravityScale;
        definition.enableSleep = configuration.CanSleep;
        definition.sleepThreshold = space.SleepSettings.LinearThreshold * PhysicsSpace.MetersPerUnit;
        definition.isAwake = !configuration.Sleeping;
        definition.motionLocks.angularZ = configuration.LockRotation || configuration.Mode == PhysicsServer.BodyMode.RigidLinear;
        BodyID = b2CreateBody(space.WorldID, definition);
        _world = B2Worlds.b2GetWorldFromId(space.WorldID);
        _body = b2GetBodyFullId(_world, BodyID);
        _savedPose = b2GetBodyTransformQuick(_world, _body);
        Space = space; AttachmentVersion = version;
    }

    // Motion-role matching intentionally ignores rotation locks; callers retain
    // their own lock/freeze restoration policy when changing the role.
    internal bool HasMotionMode(PhysicsServer.BodyMode mode) => b2Body_GetType(BodyID) == BodyType(mode);
    internal void SetMotionMode(PhysicsServer.BodyMode mode) => b2Body_SetType(BodyID, BodyType(mode));

    private static B2BodyType BodyType(PhysicsServer.BodyMode mode) => mode switch
    {
        PhysicsServer.BodyMode.Static => B2BodyType.b2_staticBody,
        PhysicsServer.BodyMode.Kinematic => B2BodyType.b2_kinematicBody,
        PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear => B2BodyType.b2_dynamicBody,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    internal void Detach()
    {
        if (Space is null) return;
        if (!Space.HasBackendFailure) b2DestroyBody(BodyID);
        _shapes.Clear();
        _world = null; _body = null; _savedPose = default;
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
        definition.userData = new B2UserData(new PhysicsFixtureTag(rid, index, oneWay)
        {
            SceneOwner = _sceneOwner,
            Source = new(shape),
            Compound = shape is ConvexPolygonShape polygon && polygon.GetGeometry().Points.Length > B2Constants.B2_MAX_POLYGON_VERTICES
                ? new(new(polygon), transform) : null
        });
        definition.enablePreSolveEvents = !definition.isSensor && (oneWay is not null || PhysicsServer.Service.HasBodyCollisionExceptions(rid));
        var first = _shapes.Count;
        PhysicsShapeBackend.AppendToBody(shape, BodyID, transform.Origin, transform.Rotation, definition, _shapes);
        for (var i = first; i < _shapes.Count; i++) b2GetShape(_world!, _shapes[i]).customSolverBias = shape.CustomSolverBias;
    }
}
