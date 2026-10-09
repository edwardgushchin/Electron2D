using Box2D.NET;

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
    private readonly PhysicsColliderBackend _backend = new(rid);
    private Transform _transform = Transform.Identity;
    private uint _layer = 1;
    private uint _mask = 1;
    private Vector2 _linearVelocity;
    private float _angularVelocity;
    private bool _canSleep = true;
    private bool _sleeping;
    private bool _firstKinematicTransform = true;
    private bool _hasKinematicTarget;
    private Transform _kinematicTarget;
    private PhysicsServer.BodyMode _mode = PhysicsServer.BodyMode.Rigid;
    private PhysicsBodyRuntime? _runtime;
    internal PhysicsBodyRuntime Runtime => _runtime ??= PhysicsServer.Service.BodyRuntime(RID);

    internal readonly record struct ShapeSlot(PhysicsServerShape Shape, Transform LocalTransform, bool Disabled,
        bool OneWay = false, float Margin = 0, Vector2 Direction = default);

    internal RID RID { get; } = rid;
    internal PhysicsColliderBackend Backend => _backend;
    internal B2BodyId BackendID => _backend.BodyID;
    internal bool IsArea { get; } = isArea;
    internal PhysicsAreaFields? AreaFields { get; } = isArea ? new(9.80665f, new(0, -1)) : null;
    internal bool Monitorable { get; set; }
    internal RID SpaceRID { get; private set; }
    internal PhysicsSpace? Space => _backend.Space;
    internal IReadOnlyList<B2ShapeId> BackendShapes => _backend.Shapes;
    internal PhysicsServer.BodyMode Mode => _mode;
    internal int ShapeCount => _slots.Count;
    private volatile bool _shapesDirty;

    internal void MarkShapesDirty() => _shapesDirty = true;

    internal void PrepareBackend()
    {
        _backend.PrepareContactPolicy();
        if (_shapesDirty) RebuildShapes();
    }

    internal void AttachBackend(PhysicsSpace space, RID spaceRID)
    {
        if (Space is not null) throw new InvalidOperationException("A server collider already belongs to a space.");
        var configuration = new PhysicsBodyConfiguration(IsArea ? PhysicsServer.BodyMode.Static : _mode,
            AngularVelocity: _mode == PhysicsServer.BodyMode.Rigid ? _angularVelocity : 0,
            CanSleep: _canSleep, Sleeping: _sleeping);
        _backend.Attach(space, _transform.Origin, _transform.Rotation, configuration);
        SpaceRID = spaceRID;
        try
        {
            if (!IsArea && _mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear)
                _backend.SetLinearVelocity(_linearVelocity);
            RebuildShapes();
            if (!IsArea && _mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic)
                _backend.SetSurfaceVelocity(_linearVelocity, _angularVelocity);
            if (_sleeping) _backend.SetAwake(false);
        }
        catch { DetachBackend(); throw; }
    }

    internal void DetachBackend()
    {
        if (IsArea) PhysicsServer.Service.FindAreaRuntime(RID)?.Reset();
        PhysicsServer.Service.InvalidateBodyView(RID);
        if (Space is null) return;
        if (!Space.HasBackendFailure) CaptureMotion();
        _backend.Detach();
        SpaceRID = default;
    }

    internal void AddShape(PhysicsServerShape shape, Transform localTransform, bool disabled)
    {
        ValidateTransform(localTransform);
        _slots.Add(new(shape, localTransform, disabled));
        if (Space is null) return;
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
        if (removed && Space is not null) RebuildShapes();
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
        _firstKinematicTransform = true; _hasKinematicTarget = false;
        _sleeping = false;
        if (mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic)
            _linearVelocity = Vector2.Zero;
        if (mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic or PhysicsServer.BodyMode.RigidLinear)
            _angularVelocity = 0;
        if (Space is null) return;
        _backend.SetMotionMode(_mode);
        _backend.SetRotationLocked(mode == PhysicsServer.BodyMode.RigidLinear);
        _backend.SetLinearVelocity(_linearVelocity);
        if (mode is PhysicsServer.BodyMode.Rigid) _backend.SetAngularVelocity(_angularVelocity);
        if (mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic or PhysicsServer.BodyMode.RigidLinear)
            _backend.SetAngularVelocity(0);
        RebuildShapes();
    }

    internal void SetTransform(Transform transform)
    {
        ValidateTransform(transform);
        if (!IsArea && _mode == PhysicsServer.BodyMode.Kinematic)
        {
            _kinematicTarget = transform;
            _hasKinematicTarget = true;
            if (!_firstKinematicTransform) return;
            _firstKinematicTransform = false;
        }
        _transform = transform;
        if (Space is not null)
            _backend.SetPose(transform);
    }

    internal void PrepareMotion(double delta)
    {
        if (IsArea || _mode != PhysicsServer.BodyMode.Kinematic) return;
        if (_hasKinematicTarget)
            _backend.SetTargetPose(_kinematicTarget, delta);
        else _backend.ClearVelocity();
    }

    internal void CompleteMotion()
    {
        if (IsArea || _mode != PhysicsServer.BodyMode.Kinematic) return;
        _transform = GetTransform();
        _hasKinematicTarget = false;
    }

    internal Transform GetTransform()
    {
        if (Space is null || IsArea || _mode == PhysicsServer.BodyMode.Static)
            return _transform;
        var pose = _backend.GetPose();
        return new(pose.Rotation, Vector2.One, 0, pose.Position);
    }

    internal void AppendMassGeometry(PhysicsMass.Geometry geometry)
    {
        foreach (var slot in _slots)
            if (!slot.Disabled) geometry.Append(slot.Shape.Geometry, slot.LocalTransform, false);
    }

    internal Vector2 GetLinearVelocity() => Space is null || _mode == PhysicsServer.BodyMode.Static ? _linearVelocity :
        _backend.LinearVelocity;
    internal float GetAngularVelocity() => Space is null || _mode == PhysicsServer.BodyMode.Static ? _angularVelocity : _backend.AngularVelocity;
    internal void SetAngularVelocity(float velocity)
    {
        if (!float.IsFinite(velocity)) throw new ArgumentOutOfRangeException(nameof(velocity));
        if (Space is not null && !IsArea)
        {
            if (_mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic)
            {
                _backend.SetAngularVelocity(0);
                _backend.SetSurfaceVelocity(_linearVelocity, velocity);
            }
            else _backend.SetAngularVelocity(velocity);
        }
        _angularVelocity = velocity;
    }

    internal void SetLinearVelocity(Vector2 velocity)
    {
        if (!velocity.IsFinite()) throw new ArgumentOutOfRangeException(nameof(velocity));
        if (Space is not null && !IsArea)
        {
            if (_mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic)
            {
                _backend.SetLinearVelocity(default);
                _backend.SetSurfaceVelocity(velocity, _angularVelocity);
            }
            else _backend.SetLinearVelocity(velocity);
        }
        _linearVelocity = velocity;
    }

    internal bool GetSleeping() => Space is not null ? !_backend.IsAwake :
        _mode == PhysicsServer.BodyMode.Static || _mode != PhysicsServer.BodyMode.Kinematic && _sleeping;

    internal void SetSleeping(bool sleeping)
    {
        if (_mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic) return;
        if (Space is not null) _backend.SetAwake(!sleeping);
        _sleeping = sleeping;
        if (sleeping) { _linearVelocity = Vector2.Zero; _angularVelocity = 0; }
    }

    internal bool GetCanSleep() => _canSleep;
    internal void SetCanSleep(bool canSleep)
    {
        if (Space is not null) _backend.SetCanSleep(canSleep);
        _canSleep = canSleep;
        if (!canSleep) _sleeping = false;
    }

    internal void SetFilter(uint layer, uint mask)
    {
        _layer = layer;
        _mask = mask;
        if (Space is not null) RebuildShapes();
    }

    internal uint CollisionLayer => _layer;
    internal uint CollisionMask => _mask;

    private void CaptureMotion()
    {
        if (Space is null || IsArea || _mode == PhysicsServer.BodyMode.Static)
            return;
        _transform = GetTransform();
        if (_mode == PhysicsServer.BodyMode.Kinematic) return;
        _sleeping = !_backend.IsAwake;
        _linearVelocity = _backend.LinearVelocity;
        _angularVelocity = _backend.AngularVelocity;
    }

    internal void RebuildShapes()
    {
        if (Space is null) return;
        var runtime = IsArea ? null : PhysicsServer.Service.BodyRuntime(RID);
        _backend.RebuildShapes(_slots, _layer, _mask, IsArea,
            !IsArea && _mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear ? 1 : 0,
            runtime?.GetFriction() ?? 1, runtime?.GetBounce() ?? 0);
        if (!IsArea) PhysicsServer.Service.BodyRuntime(RID).ApplyMassProfile();
        _shapesDirty = false;
    }

    internal static void ValidateTransform(Transform transform)
    {
        if (!transform.IsFinite() || !transform.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(transform.Skew))
            throw new ArgumentException("Physics transforms require finite translation, unit scale and zero skew.", nameof(transform));
    }
}
