namespace Electron2D;

internal sealed class PhysicsServerShape(RID rid, Shape geometry, bool ownsGeometry = true)
{
    internal RID RID { get; } = rid;
    internal Shape Geometry { get; set; } = geometry;
    internal bool OwnsGeometry { get; } = ownsGeometry;
    private int _dataUses;
    private static int _activeDataUses;
    internal static bool HasDataUses => Volatile.Read(ref _activeDataUses) != 0;
    internal void BeginDataUse() { Interlocked.Increment(ref _dataUses); Interlocked.Increment(ref _activeDataUses); }
    internal void EndDataUse() { Interlocked.Decrement(ref _dataUses); Interlocked.Decrement(ref _activeDataUses); }
    internal void EnsureBindingAccess()
    {
        if (Volatile.Read(ref _dataUses) != 0)
            throw new InvalidOperationException("Shape consumers and lifetime cannot change while server shape data is borrowed.");
    }
}

internal sealed partial class PhysicsServerCollider(RID rid, bool isArea)
{
    private readonly List<ShapeSlot> _slots = [];
    private ShapeSlot[] _clearBackup = [];
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
        bool OneWay = false, float Margin = 0, Vector2 Direction = default)
    {
        internal object HistoryIdentity { get; init; } = new();
    }

    internal RID RID { get; } = rid;
    internal PhysicsColliderBackend Backend => _backend;
    internal bool IsArea { get; } = isArea;
    internal PhysicsAreaFields? AreaFields { get; } = isArea ? new(9.80665f, new(0, -1)) : null;
    internal bool Monitorable { get; set; }
    internal RID SpaceRID => Space?.RID ?? default;
    internal PhysicsSpace? Space => _backend.Space;
    internal PhysicsServer.BodyMode Mode => _mode;
    internal int ShapeCount => _slots.Count;
    private volatile bool _shapesDirty;
    private bool _modeDirty, _preparingMode;
    private bool _poseDirty, _preparingPose;

    internal void MarkShapesDirty() => _shapesDirty = true;

    internal void PrepareBackend()
    {
        _backend.PreparePendingMotion();
        PreparePendingPose();
        _backend.PrepareContactPolicy();
        if (_shapesDirty) RebuildShapes();
    }

    internal void AttachBackend(PhysicsSpace space)
    {
        if (Space is not null) throw new InvalidOperationException("A server collider already belongs to a space.");
        var configuration = new PhysicsBodyConfiguration(IsArea ? PhysicsServer.BodyMode.Static : _mode,
            AngularVelocity: _mode == PhysicsServer.BodyMode.Rigid ? _angularVelocity : 0,
            CanSleep: _canSleep, Sleeping: _sleeping, LockRotation: _mode == PhysicsServer.BodyMode.RigidLinear);
        _backend.Attach(space, _transform.Origin, _transform.Rotation, configuration);
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
        var space = Space;
        if (space is null) return;
        List<Exception>? errors = null;
        PhysicsSpace.Cleanup(() => { if (!space.HasBackendFailure) CaptureMotion(); }, ref errors);
        PhysicsSpace.Cleanup(_backend.Detach, ref errors);
        _modeDirty = false;
        _poseDirty = false;
        PhysicsSpace.FinishCleanup(errors);
    }

    internal void AddShape(PhysicsServerShape shape, Transform localTransform, bool disabled)
    {
        ValidateTransform(localTransform);
        if (!disabled) PhysicsShapeBackend.RequireSupported(Space, shape.Geometry);
        _slots.Add(new(shape, localTransform, disabled));
        if (Space is null) return;
        try { RebuildShapes(); }
        catch (Exception error)
        {
            _slots.RemoveAt(_slots.Count - 1);
            RestoreShapes(error);
            throw;
        }
    }

    internal bool RemoveShape(PhysicsServerShape shape)
    {
        var removed = _slots.RemoveAll(slot => ReferenceEquals(slot.Shape, shape)) != 0;
        if (removed) _shapesDirty = true;
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
        if (!disabled) PhysicsShapeBackend.RequireSupported(Space, previous.Shape.Geometry);
        _slots[index] = previous with { Disabled = disabled };
        try { RebuildShapes(); }
        catch (Exception error) { _slots[index] = previous; RestoreShapes(error); throw; }
    }

    private ShapeSlot Slot(int index) => (uint)index < (uint)_slots.Count ? _slots[index] :
        throw new ArgumentOutOfRangeException(nameof(index));

    internal PhysicsServerShape GetShape(int index) => Slot(index).Shape;
    internal object GetShapeHistoryIdentity(int index) => Slot(index).HistoryIdentity;
    internal Transform GetShapeTransform(int index) => Slot(index).LocalTransform;
    internal bool IsShapeDisabled(int index) => Slot(index).Disabled;

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
        if (!next.Disabled) PhysicsShapeBackend.RequireSupported(Space, next.Shape.Geometry);
        _slots[index] = next;
        try { RebuildShapes(); }
        catch (Exception error) { _slots[index] = previous; RestoreShapes(error); throw; }
    }

    internal void ClearShapes()
    {
        if (_slots.Count == 0) return;
        if (Space is null) { _slots.Clear(); return; }
        var count = _slots.Count;
        if (_clearBackup.Length < count) Array.Resize(ref _clearBackup, Math.Max(8, count));
        _slots.CopyTo(_clearBackup);
        try
        {
            _slots.Clear();
            try { RebuildShapes(); }
            catch (Exception error)
            {
                for (var i = 0; i < count; i++) _slots.Add(_clearBackup[i]);
                RestoreShapes(error);
                throw;
            }
        }
        finally { Array.Clear(_clearBackup, 0, count); }
    }

    internal void RemoveShapeAt(int index)
    {
        if ((uint)index >= (uint)_slots.Count) throw new ArgumentOutOfRangeException(nameof(index));
        var previous = _slots[index];
        _slots.RemoveAt(index);
        try { RebuildShapes(); }
        catch (Exception error) { _slots.Insert(index, previous); RestoreShapes(error); throw; }
    }

    private void RestoreShapes(Exception error)
    {
        try { RebuildShapes(); }
        catch (Exception rollback) { throw new AggregateException("Collider shape edit and restoration failed.", error, rollback); }
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
        _modeDirty = true;
        PreparePendingMode();
    }

    internal void PreparePendingMode()
    {
        if (!_modeDirty || Space is null) return;
        if (_preparingMode) throw new InvalidOperationException("Body mode preparation cannot reenter its own transition.");
        _preparingMode = true;
        try
        {
            _backend.SetMotionMode(_mode);
            _backend.SetRotationLocked(_mode == PhysicsServer.BodyMode.RigidLinear);
            _backend.SetLinearVelocity(_linearVelocity);
            _backend.SetAngularVelocity(_mode == PhysicsServer.BodyMode.Rigid ? _angularVelocity : 0);
            var dynamic = _mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
            _backend.SetSurfaceVelocity(dynamic ? default : _linearVelocity, dynamic ? 0 : _angularVelocity);
            if (dynamic) _backend.SetAwake(true);
            RebuildShapes();
            _modeDirty = false;
        }
        catch { Space.MarkColliderMetadataDirty(); throw; }
        finally { _preparingMode = false; }
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
        {
            _poseDirty = true;
            PreparePendingPose();
        }
    }

    internal void PreparePendingPose()
    {
        if (!_poseDirty || Space is null) return;
        if (_preparingPose) throw new InvalidOperationException("Collider pose preparation cannot reenter its own publication.");
        _preparingPose = true;
        try { _backend.SetPose(_transform); _poseDirty = false; }
        catch { Space.MarkColliderMetadataDirty(); throw; }
        finally { _preparingPose = false; }
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
        return _backend.GetTransform();
    }

    private bool _validatingGeometry;
    internal bool Releasing;
    internal void EnsureGeometryLifetimeAccess()
    {
        EnsureAuthoringAccess();
        Space?.EnsureExtensionHierarchyChange();
    }
    internal void EnsureAuthoringAccess()
    {
        if (Releasing) throw new InvalidOperationException("A collider being released cannot be rebound or released recursively.");
        if (_validatingGeometry) throw new InvalidOperationException("A collider cannot be released or transferred while its geometry is borrowed.");
        foreach (var slot in _slots) slot.Shape.EnsureBindingAccess();
    }
    internal void ValidateGeometry(PhysicsSpace space)
    {
        if (space.ActualBackend == PhysicsServer.Backend.Extension) return;
        EnsureGeometryLifetimeAccess(); _validatingGeometry = true;
        try
        {
            foreach (var slot in _slots)
                if (!slot.Disabled && !slot.Shape.Geometry.IsDisposed) PhysicsShapeBackend.RequireSupported(space, slot.Shape.Geometry);
        }
        finally { _validatingGeometry = false; }
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
        _backend.PreparePendingMotion();
        _angularVelocity = velocity;
        var dynamic = _mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
        if (dynamic) _sleeping = false;
        if (Space is not null && !IsArea)
        {
            if (_mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic)
                _backend.PublishMotion(angular: 0, surface: (_linearVelocity, velocity));
            else _backend.PublishMotion(angular: velocity, awake: true);
        }
    }

    internal void SetLinearVelocity(Vector2 velocity)
    {
        if (!velocity.IsFinite()) throw new ArgumentOutOfRangeException(nameof(velocity));
        _backend.PreparePendingMotion();
        _linearVelocity = velocity;
        var dynamic = _mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
        if (dynamic) _sleeping = false;
        if (Space is not null && !IsArea)
        {
            if (_mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic)
                _backend.PublishMotion(linear: Vector2.Zero, surface: (velocity, _angularVelocity));
            else _backend.PublishMotion(linear: velocity, awake: true);
        }
    }

    internal bool GetSleeping() => Space is not null ? !_backend.IsAwake :
        _mode == PhysicsServer.BodyMode.Static || _mode != PhysicsServer.BodyMode.Kinematic && _sleeping;

    internal void SetSleeping(bool sleeping)
    {
        if (_mode is PhysicsServer.BodyMode.Static or PhysicsServer.BodyMode.Kinematic) return;
        _backend.PreparePendingMotion();
        _sleeping = sleeping;
        if (sleeping) { _linearVelocity = Vector2.Zero; _angularVelocity = 0; }
        if (Space is not null) _backend.PublishMotion(awake: !sleeping);
    }

    internal bool GetCanSleep() => _canSleep;
    internal void SetCanSleep(bool canSleep)
    {
        _backend.PreparePendingMotion();
        _canSleep = canSleep;
        if (!canSleep) _sleeping = false;
        if (Space is not null) _backend.PublishMotion(canSleep: canSleep,
            awake: !canSleep && _mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear ? true : null);
    }

    internal void SetFilter(uint layer, uint mask)
    {
        if (Space is not null) _backend.UpdateFilter(layer, mask, wakeBody: !IsArea);
        _layer = layer;
        _mask = mask;
    }

    internal uint CollisionLayer => _layer;
    internal uint CollisionMask => _mask;

    private void CaptureMotion()
    {
        if (Space is null || IsArea || _modeDirty || _mode == PhysicsServer.BodyMode.Static)
            return;
        if (!_poseDirty) _transform = GetTransform();
        if (_mode == PhysicsServer.BodyMode.Kinematic) return;
        var motion = _backend.GetRetainedSolverMotion();
        _sleeping = motion.Sleeping;
        _linearVelocity = motion.LinearVelocity;
        _angularVelocity = motion.AngularVelocity;
    }

    internal void RebuildShapes()
    {
        if (Space is null) return;
        _shapesDirty = true;
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
