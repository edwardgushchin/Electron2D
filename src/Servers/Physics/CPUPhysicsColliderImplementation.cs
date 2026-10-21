using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Electron2D;

/// <summary>Owns one CPU collider's native body, fixtures and exact state caches.</summary>
internal sealed partial class CPUPhysicsColliderImplementation(PhysicsColliderBackend owner, PhysicsSpace space,
    CPUPhysicsWorldBackend worldOwner) : PhysicsColliderImplementation(owner, space)
{
    private readonly List<B2ShapeId> _shapes = [];
    internal B2BodyId BodyID { get; private set; }
    internal IReadOnlyList<B2ShapeId> Shapes => _shapes;
    internal override int ShapeCount => _shapes.Count;

    internal override void Attach(Vector2 position, float rotation, in PhysicsBodyConfiguration configuration)
    {
        var space = Space;
        var definition = b2DefaultBodyDef();
        definition.type = BodyType(configuration.Mode);
        definition.position = PhysicsShapeBackend.ToBackend(position);
        definition.rotation = PhysicsShapeBackend.RotationToBackend(rotation);
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
        if (definition.type != B2BodyType.b2_staticBody && definition.enableSleep) PrepareSleepCapacity();
    }

    internal override bool HasMotionMode(PhysicsServer.BodyMode mode) => b2Body_GetType(BodyID) == BodyType(mode);
    internal override void SetMotionMode(PhysicsServer.BodyMode mode)
    {
        b2Body_SetType(BodyID, BodyType(mode));
        if (BodyType(mode) != B2BodyType.b2_staticBody && _body!.enableSleep) PrepareSleepCapacity();
    }
    private CPUPhysicsWorldBackend WorldOwner => worldOwner;
    private void PrepareSleepCapacity() => WorldOwner.PrepareSleepCapacity();
    private static B2BodyType BodyType(PhysicsServer.BodyMode mode) => mode switch
    {
        PhysicsServer.BodyMode.Static => B2BodyType.b2_staticBody,
        PhysicsServer.BodyMode.Kinematic => B2BodyType.b2_kinematicBody,
        PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear => B2BodyType.b2_dynamicBody,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    internal override void Detach()
    {
        try { if (B2Worlds.b2Body_IsValid(BodyID) && !Space.HasBackendFailure) b2DestroyBody(BodyID); }
        finally { _shapes.Clear(); _shapeInputs = []; _shapeInputCount = -1; _world = null; _body = null; _savedPose = default; BodyID = default; _contactReporting = false; WorldOwner.RetireCollider(Owner, this); }
    }

    private bool _contactReporting;
    internal override void SetContactReporting(bool enabled) => _contactReporting = enabled;

    internal override void UpdateFilter(uint layer, uint mask, bool wakeBody)
    {
        Space!.EnsureQueryAccess();
        if (wakeBody) { WakeTouching(); SetAwake(true); }
        foreach (var id in _shapes)
        {
            var filter = b2Shape_GetFilter(id);
            filter.categoryBits = layer; filter.maskBits = mask;
            b2Shape_SetFilter(id, filter);
        }
    }

    internal override void RebuildShapes(ReadOnlySpan<PhysicsColliderShape> slots, uint layer, uint mask,
        bool sensor, float density, float friction = 1, float bounce = 0)
    {
        foreach (ref readonly var slot in slots)
        {
            if (!slot.Enabled) continue;
            if (!slot.Transform.IsFinite() || !slot.Transform.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(slot.Transform.Skew))
                throw new InvalidOperationException("Physics shapes require unit scale and zero skew.");
            if (!slot.Shape.IsDisposed) PhysicsShapeBackend.RequireBuiltIn(slot.Shape);
        }
        if (UpdateShapeProperties(slots, layer, mask, sensor, density, friction, bounce)) return;
        _shapeInputCount = -1;
        var definition = BeginShapeUpdate(layer, mask, sensor, density, friction, bounce);
        foreach (ref readonly var slot in slots)
            if (slot.Enabled && !slot.Shape.IsDisposed)
                AppendShape(slot.Shape, slot.Transform, slot.Index, sensor ? null : slot.OneWay, ref definition);
        CaptureShapeInputs(slots, layer, mask, sensor);
    }

    private B2ShapeDef BeginShapeUpdate(uint layer, uint mask, bool sensor, float density, float friction, float bounce)
    {
        if (Space is null) throw new InvalidOperationException("A collider must be attached before creating shapes.");
        foreach (var id in _shapes) b2DestroyShape(id, updateBodyMass: false);
        _shapes.Clear();
        // Former fixtures must retire before their private tags can be rewritten.
        _fixtureTagCount = 0;
        var definition = b2DefaultShapeDef();
        // The owning body applies its complete mass profile after all pieces are created.
        definition.updateBodyMass = false;
        definition.filter.categoryBits = layer;
        definition.filter.maskBits = mask;
        definition.isSensor = sensor;
        definition.density = density;
        if (!sensor) CPUPhysicsWorldBackend.SetMaterial(ref definition, friction, bounce);
        return definition;
    }

    private void AppendShape(Shape shape, Transform transform, int index, OneWayContactData? oneWay, ref B2ShapeDef definition)
    {
        var geometry = shape.GetGeometry();
        var tag = TakeFixtureTag();
        tag.ColliderRID = Owner.RID; tag.ShapeIndex = index; tag.OneWay = oneWay; tag.SeparationRay = null;
        tag.Owner = Owner; tag.SceneOwner = Owner.SceneOwnerReference; tag.Source!.SetTarget(shape); tag.CompoundPiece = 0;
        tag.Compound = null; tag.Kind = geometry.Kind;
        if (geometry.Kind == PhysicsShapeGeometry.ShapeKind.ConvexPolygon && geometry.Points.Length > B2Constants.B2_MAX_POLYGON_VERTICES)
        {
            tag.CachedCompound ??= new(tag.Source, transform);
            tag.CachedCompound.LocalPose = transform; tag.Compound = tag.CachedCompound;
        }
        definition.userData = new B2UserData(tag);
        definition.enablePreSolveEvents = !definition.isSensor && (oneWay is not null || PhysicsServer.Service.HasBodyCollisionExceptions(Owner.RID));
        var first = _shapes.Count;
        PhysicsShapeBackend.AppendToBody(shape, BodyID, transform.Origin, transform.Rotation, definition, _shapes, this);
        for (var i = first; i < _shapes.Count; i++) b2GetShape(_world!, _shapes[i]).customSolverBias = shape.CustomSolverBias;
    }

    private readonly List<PhysicsFixtureTag> _fixtureTags = [];
    private int _fixtureTagCount;
    private PhysicsFixtureTag TakeFixtureTag()
    {
        if (_fixtureTagCount == _fixtureTags.Count) _fixtureTags.Add(new(Owner.RID, 0, null) { Source = new(null!) });
        return _fixtureTags[_fixtureTagCount++];
    }
    internal PhysicsFixtureTag PieceTag(PhysicsFixtureTag source, int piece, SeparationRayData? ray = null)
    {
        var tag = TakeFixtureTag();
        tag.ColliderRID = source.ColliderRID; tag.ShapeIndex = source.ShapeIndex; tag.OneWay = source.OneWay;
        tag.SeparationRay = ray; tag.Owner = source.Owner; tag.SceneOwner = source.SceneOwner;
        source.Source!.TryGetTarget(out var shape); tag.Source!.SetTarget(shape!);
        tag.Compound = source.Compound; tag.CompoundPiece = piece; tag.Kind = source.Kind;
        return tag;
    }
}
