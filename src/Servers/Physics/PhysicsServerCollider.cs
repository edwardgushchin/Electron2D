using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Electron2D;

internal sealed class PhysicsServerShape(RID rid, Shape geometry, bool ownsGeometry = true)
{
    internal RID RID { get; } = rid;
    internal Shape Geometry { get; set; } = geometry;
    internal bool OwnsGeometry { get; } = ownsGeometry;
}

internal sealed class PhysicsServerCollider(RID rid, bool isArea)
{
    private readonly List<ShapeSlot> _slots = [];
    private readonly List<B2ShapeId> _backendShapes = [];
    private B2BodyId _bodyID;
    private PhysicsSpace? _space;
    private Transform _transform = Transform.Identity;
    private uint _layer = 1;
    private uint _mask = 1;
    private Vector2 _linearVelocity;
    private float _angularVelocity;
    private PhysicsServer.BodyMode _mode = PhysicsServer.BodyMode.Rigid;

    private readonly record struct ShapeSlot(PhysicsServerShape Shape, Transform LocalTransform, bool Disabled,
        bool OneWay = false, float Margin = 0, Vector2 Direction = default);

    internal RID RID { get; } = rid;
    internal B2BodyId BackendID => _bodyID;
    internal bool IsArea { get; } = isArea;
    internal PhysicsAreaFields? AreaFields { get; } = isArea ? new(9.80665f, new(0, -1)) : null;
    internal bool Monitorable { get; set; }
    internal RID SpaceRID { get; private set; }
    internal PhysicsSpace? Space => _space;
    internal IReadOnlyList<B2ShapeId> BackendShapes => _backendShapes;
    internal PhysicsServer.BodyMode Mode => _mode;
    internal int ShapeCount => _slots.Count;
    private volatile bool _shapesDirty;

    internal void MarkShapesDirty() => _shapesDirty = true;

    internal void PrepareBackend()
    {
        if (_shapesDirty) RebuildShapes();
    }

    internal void AttachBackend(PhysicsSpace space, RID spaceRID)
    {
        if (_space is not null) throw new InvalidOperationException("A server collider already belongs to a space.");
        var definition = b2DefaultBodyDef();
        definition.type = BackendType;
        definition.position = Shape.ToBackend(_transform.Origin);
        definition.rotation = b2MakeRot(_transform.Rotation);
        definition.angularVelocity = _mode == PhysicsServer.BodyMode.RigidLinear ? 0 : _angularVelocity;
        _bodyID = b2CreateBody(space.WorldID, definition);
        _space = space;
        SpaceRID = spaceRID;
        try
        {
            if (_mode == PhysicsServer.BodyMode.RigidLinear)
                b2Body_SetMotionLocks(_bodyID, new(false, false, true));
            if (!IsArea && _mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear)
                b2Body_SetLinearVelocity(_bodyID, Shape.ToBackend(_linearVelocity));
            RebuildShapes();
        }
        catch { DetachBackend(); throw; }
    }

    internal void DetachBackend()
    {
        if (IsArea) PhysicsServer.Service.FindAreaRuntime(RID)?.Reset();
        PhysicsServer.Service.InvalidateBodyView(RID);
        if (_space is null) return;
        CaptureMotion();
        b2DestroyBody(_bodyID);
        _backendShapes.Clear();
        _space = null;
        SpaceRID = default;
    }

    internal void AddShape(PhysicsServerShape shape, Transform localTransform, bool disabled)
    {
        ValidateTransform(localTransform);
        _slots.Add(new(shape, localTransform, disabled));
        if (_space is null) return;
        try { RebuildShapes(); }
        catch
        {
            _slots.RemoveAt(_slots.Count - 1);
            RebuildShapes();
            throw;
        }
    }

    internal bool RemoveShape(PhysicsServerShape shape)
    {
        var removed = _slots.RemoveAll(slot => ReferenceEquals(slot.Shape, shape)) != 0;
        if (removed && _space is not null) RebuildShapes();
        return removed;
    }

    internal bool UsesShape(PhysicsServerShape shape)
    {
        foreach (var slot in _slots)
            if (ReferenceEquals(slot.Shape, shape)) return true;
        return false;
    }

    internal void SetShapeDisabled(int index, bool disabled)
    {
        if ((uint)index >= (uint)_slots.Count) throw new ArgumentOutOfRangeException(nameof(index));
        var previous = _slots[index];
        if (previous.Disabled == disabled) return;
        _slots[index] = previous with { Disabled = disabled };
        try { RebuildShapes(); }
        catch { _slots[index] = previous; RebuildShapes(); throw; }
    }

    private ShapeSlot Slot(int index) => (uint)index < (uint)_slots.Count ? _slots[index] :
        throw new ArgumentOutOfRangeException(nameof(index));

    internal PhysicsServerShape GetShape(int index) => Slot(index).Shape;
    internal Transform GetShapeTransform(int index) => Slot(index).LocalTransform;

    internal void SetShape(int index, PhysicsServerShape shape) => ReplaceSlot(index, Slot(index) with { Shape = shape });
    internal void SetShapeTransform(int index, Transform transform)
    {
        ValidateTransform(transform);
        ReplaceSlot(index, Slot(index) with { LocalTransform = transform });
    }
    internal void SetShapeOneWay(int index, bool enable, float margin, Vector2 direction) =>
        ReplaceSlot(index, Slot(index) with { OneWay = enable, Margin = margin, Direction = direction });

    private void ReplaceSlot(int index, ShapeSlot next)
    {
        var previous = Slot(index);
        if (previous == next) return;
        _slots[index] = next;
        try { RebuildShapes(); }
        catch { _slots[index] = previous; RebuildShapes(); throw; }
    }

    internal void ClearShapes()
    {
        if (_slots.Count == 0) return;
        _slots.Clear(); RebuildShapes();
    }

    internal void RemoveShapeAt(int index)
    {
        if ((uint)index >= (uint)_slots.Count) throw new ArgumentOutOfRangeException(nameof(index));
        var previous = _slots[index];
        _slots.RemoveAt(index);
        try { RebuildShapes(); }
        catch { _slots.Insert(index, previous); RebuildShapes(); throw; }
    }

    internal void SetMode(PhysicsServer.BodyMode mode)
    {
        if (IsArea) throw new InvalidOperationException("An Area has no body mode.");
        if (_mode == mode) return;
        CaptureMotion();
        _mode = mode;
        if (mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic)
            _linearVelocity = Vector2.Zero;
        if (mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic or PhysicsServer.BodyMode.RigidLinear)
            _angularVelocity = 0;
        if (_space is null) return;
        b2Body_SetType(_bodyID, BackendType);
        b2Body_SetMotionLocks(_bodyID, new(false, false, mode == PhysicsServer.BodyMode.RigidLinear));
        if (mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic)
            b2Body_SetLinearVelocity(_bodyID, default);
        if (mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic or PhysicsServer.BodyMode.RigidLinear)
            b2Body_SetAngularVelocity(_bodyID, 0);
        RebuildShapes();
    }

    internal void SetTransform(Transform transform)
    {
        ValidateTransform(transform);
        _transform = transform;
        if (_space is not null)
            b2Body_SetTransform(_bodyID, Shape.ToBackend(transform.Origin), b2MakeRot(transform.Rotation));
    }

    internal Transform GetTransform()
    {
        if (_space is null || IsArea || _mode == PhysicsServer.BodyMode.Static)
            return _transform;
        var position = b2Body_GetPosition(_bodyID);
        var rotation = b2Rot_GetAngle(b2Body_GetRotation(_bodyID));
        return new(rotation, Vector2.One, 0,
            new(position.X * PhysicsSpace.UnitsPerMeter, position.Y * PhysicsSpace.UnitsPerMeter));
    }

    internal void AppendMassGeometry(List<B2ShapeProxy> proxies)
    {
        foreach (var slot in _slots)
            if (!slot.Disabled) PhysicsMass.AppendGeometry(slot.Shape.Geometry, slot.LocalTransform, proxies);
    }

    internal Vector2 GetLinearVelocity() => _space is null || _mode == PhysicsServer.BodyMode.Static ? _linearVelocity :
        new(b2Body_GetLinearVelocity(_bodyID).X * PhysicsSpace.UnitsPerMeter, b2Body_GetLinearVelocity(_bodyID).Y * PhysicsSpace.UnitsPerMeter);
    internal float GetAngularVelocity() => _space is null || _mode == PhysicsServer.BodyMode.Static ? _angularVelocity : b2Body_GetAngularVelocity(_bodyID);
    internal void SetAngularVelocity(float velocity)
    {
        _angularVelocity = velocity;
        if (_space is not null && !IsArea) b2Body_SetAngularVelocity(_bodyID, velocity);
    }

    internal void SetLinearVelocity(Vector2 velocity)
    {
        if (!velocity.IsFinite()) throw new ArgumentOutOfRangeException(nameof(velocity));
        _linearVelocity = velocity;
        if (_space is not null && !IsArea) b2Body_SetLinearVelocity(_bodyID, Shape.ToBackend(velocity));
    }

    internal void SetFilter(uint layer, uint mask)
    {
        _layer = layer;
        _mask = mask;
        if (_space is not null) RebuildShapes();
    }

    internal uint CollisionLayer => _layer;
    internal uint CollisionMask => _mask;

    private void CaptureMotion()
    {
        if (_space is null || IsArea || _mode == PhysicsServer.BodyMode.Static)
            return;
        _transform = GetTransform();
        var velocity = b2Body_GetLinearVelocity(_bodyID);
        _linearVelocity = new(velocity.X * PhysicsSpace.UnitsPerMeter,
            velocity.Y * PhysicsSpace.UnitsPerMeter);
        _angularVelocity = b2Body_GetAngularVelocity(_bodyID);
    }

    internal void RebuildShapes()
    {
        if (_space is null) return;
        foreach (var slot in _slots)
            if (!slot.Disabled) ValidateTransform(slot.LocalTransform);
        foreach (var id in _backendShapes) b2DestroyShape(id, updateBodyMass: false);
        _backendShapes.Clear();
        var definition = b2DefaultShapeDef();
        definition.updateBodyMass = false;
        definition.filter.categoryBits = _layer;
        definition.filter.maskBits = _mask;
        definition.isSensor = IsArea;
        if (!IsArea)
        {
            var runtime = PhysicsServer.Service.BodyRuntime(RID);
            PhysicsSpace.SetMaterial(ref definition, runtime.GetFriction(), runtime.GetBounce());
        }
        definition.enablePreSolveEvents = !IsArea && PhysicsServer.Service.HasBodyCollisionExceptions(RID);
        definition.density = !IsArea && _mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear ? 1 : 0;
        for (var index = 0; index < _slots.Count; index++)
        {
            var slot = _slots[index];
            if (slot.Disabled || slot.Shape.Geometry.IsDisposed) continue;
            var contact = !IsArea && slot.OneWay
                ? new OneWayContactData(slot.Direction.Rotated(slot.LocalTransform.Rotation), slot.Margin) : null;
            definition.enablePreSolveEvents = !IsArea && (contact is not null || PhysicsServer.Service.HasBodyCollisionExceptions(RID));
            definition.userData = new B2UserData(new PhysicsFixtureTag(RID, index, contact));
            slot.Shape.Geometry.AppendToBody(_bodyID, slot.LocalTransform.Origin,
                slot.LocalTransform.Rotation, definition, _backendShapes);
        }
        if (!IsArea) PhysicsServer.Service.BodyRuntime(RID).ApplyMassProfile();
        _shapesDirty = false;
    }

    private B2BodyType BackendType => IsArea || _mode == PhysicsServer.BodyMode.Static
        ? B2BodyType.b2_staticBody : _mode == PhysicsServer.BodyMode.Kinematic
            ? B2BodyType.b2_kinematicBody : B2BodyType.b2_dynamicBody;

    internal static void ValidateTransform(Transform transform)
    {
        if (!transform.IsFinite() || !transform.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(transform.Skew))
            throw new ArgumentException("Physics transforms require finite translation, unit scale and zero skew.", nameof(transform));
    }
}
