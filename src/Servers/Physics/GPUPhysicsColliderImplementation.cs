namespace Electron2D;

/// <summary>Owns one collider's resident handles, shape leases and publication caches in its retained GPU store.</summary>
/// <remarks>Sensor poses reuse the authored pose cache between numerical epochs. Compact activity never marks full state current.
/// Scalar velocity observations retain separate validity across queued waking and unrelated velocity edits; sleep clocks and fields still require complete current state.</remarks>
internal sealed partial class GPUPhysicsColliderImplementation(PhysicsColliderBackend owner, PhysicsSpace space, GPUPhysicsBodyStore store) : PhysicsColliderImplementation(owner, space)
{
    internal bool GPUSensor;
    internal uint GPUMask;
    internal GPUPhysicsBodyStore.BodyHandle GPUHandle { get; private set; }
    internal readonly List<(GPUPhysicsBodyStore.ShapeHandle Handle, int Slot, GPUPhysicsBodyStore.QueryGeometry? Query, Transform Pose)> GPUShapes = [];
    private GPUPhysicsBodyStore.QueryGeometry?[] _sensorQueries = [];
    internal override int ShapeCount => GPUShapes.Count;
    private GPUPhysicsBodyStore GPU => store;
    internal bool IsAttachedTo(GPUPhysicsBodyStore owner) => ReferenceEquals(store, owner) && GPUHandle.Generation != 0;
    private GPUPhysicsBodyStore.Snapshot _gpuState;
    /// <summary>Estimates motion from the last observation solely to choose a readback strategy; it never establishes current state.</summary>
    internal bool GPUChangeLikely => _gpuState.Mode != PhysicsServer.BodyMode.Static && !_gpuState.Sleeping &&
        (_gpuState.Velocity.X != 0 || _gpuState.Velocity.Y != 0 || _gpuState.Velocity.Z != 0);
    private System.Numerics.Vector4 _gpuPose;
    private bool _gpuPoseValid;
    private long _gpuPoseEpoch;
    internal void AcceptGPUPose(in System.Numerics.Vector4 pose)
    {
        _gpuPose = pose; _gpuPoseValid = true; _gpuPoseEpoch = Space.GPUStateEpoch;
    }
    // Sensor poses do not move in the numerical solver.
    private System.Numerics.Vector4 CurrentPose => GPUSensor && _gpuPoseValid ? _gpuPose : GPUStateValid ? _gpuState.Pose :
        _gpuPoseValid && _gpuPoseEpoch == Space.GPUStateEpoch ? _gpuPose : ReadPose();

    private System.Numerics.Vector4 ReadPose()
    {
        Space.FlushGPUWakes();
        Span<System.Numerics.Vector4> poses = stackalloc System.Numerics.Vector4[1];
        Span<GPUPhysicsBodyStore.BodyHandle> handles = stackalloc GPUPhysicsBodyStore.BodyHandle[1] { GPUHandle };
        GPU.ReadPoses(handles, poses);
        var pose = poses[0];
        AcceptGPUPose(pose); return pose;
    }

    private bool _gpuStateValid, _gpuStateMatchesPublication;
    private bool _gpuActivityValid, _gpuActive;
    private long _gpuActivityEpoch;
    internal void AcceptGPUActivity(bool active)
    {
        _gpuActive = active; _gpuActivityValid = true; _gpuActivityEpoch = Space.GPUStateEpoch;
    }
    private bool _gpuParametersPrepared, _gpuResolveStaticFields;
    private long _gpuStateEpoch, _gpuVelocityEpoch;
    internal bool GPUStateValid
    {
        get => _gpuStateValid && _gpuStateEpoch == Space!.GPUStateEpoch;
        set
        {
            _gpuStateValid = value;
            if (value) { _gpuStateEpoch = Space.GPUStateEpoch; _gpuVelocityEpoch = Space.GPUVelocityEpoch; }
            else _gpuActivityValid = false;
        }
    }
    private Vector2 _gpuSurfaceLinear, _gpuConstantForce;
    private float _gpuSurfaceAngular, _gpuConstantTorque;
    private (Vector2 Position, float Rotation) _gpuSavedPose;

    private ref readonly GPUPhysicsBodyStore.Snapshot GPUState
    {
        get
        {
            if (!GPUStateValid)
            {
                Space!.FlushGPUWakes();
                Span<GPUPhysicsBodyStore.Snapshot> snapshot = stackalloc GPUPhysicsBodyStore.Snapshot[1];
                Span<GPUPhysicsBodyStore.BodyHandle> handles = stackalloc GPUPhysicsBodyStore.BodyHandle[1] { GPUHandle };
                GPU!.Read(handles, snapshot); AcceptGPUState(snapshot[0]);
            }
            return ref _gpuState;
        }
    }

    internal void AcceptGPUState(in GPUPhysicsBodyStore.Snapshot state, bool published = false)
    {
        _gpuState = state; GPUStateValid = true;
        AcceptGPUActivity(!state.Sleeping);
        if (GPUSensor) AcceptGPUPose(state.Pose);
        _gpuStateMatchesPublication = published || Space!.GPUStatePublicationEpoch == Space.GPUStateEpoch;
    }
    internal void CompleteGPUStatePublication() => GPUStateValid = _gpuStateMatchesPublication;
    /// <summary>Qualifies only linear/angular velocity in the retained snapshot, without treating its other fields as current.</summary>
    private void PrepareGPUVelocity()
    {
        if (_gpuStateValid && _gpuVelocityEpoch == Space.GPUVelocityEpoch) return;
        _ = GPUState;
        _gpuVelocityEpoch = Space.GPUVelocityEpoch;
    }
    internal override void Attach(Vector2 position, float rotation, in PhysicsBodyConfiguration configuration)
    {
        var space = Space;
        var store = GPU;
        GPUHandle = store.Add(new(configuration.Mode, position, rotation, configuration.LinearVelocity,
            configuration.AngularVelocity, GravityScale: configuration.GravityScale, CanSleep: configuration.CanSleep,
            Sleeping: configuration.Sleeping, LockRotation: configuration.LockRotation));
        GPUStateValid = false; _gpuStateMatchesPublication = false;
        GPUSensor = Owner.SceneOwner is Area;
        _gpuPoseValid = false;
        if (GPUSensor) AcceptGPUPose(new(position.X, position.Y, MathF.Cos(rotation), MathF.Sin(rotation)));
        _gpuParametersPrepared = false;
        _gpuConstantForce = _gpuSurfaceLinear = default; _gpuSurfaceAngular = _gpuConstantTorque = 0;
        space.RegisterGPUCollider(Owner);
    }

    private void ClearGPUShapes()
    {
        foreach (var shape in GPUShapes) { shape.Query?.Dispose(); Space!.UnregisterGPUShape(shape.Handle); GPU!.RemoveShape(shape.Handle); }
        foreach (var query in _sensorQueries) query?.Dispose();
        GPUShapes.Clear();
    }
    private void AddGPUShape(Shape shape, Transform transform, int index, bool sensor, uint layer, uint mask,
        float friction, float bounce, OneWayContactData? oneWay)
    {
        var handle = GPU!.AddShape(GPUHandle, shape, transform, layer, mask, sensor, friction, bounce);
        GPU.SetQueryIdentity(handle, (ulong)Owner.RID.GetID(), index, Owner.CanvasInstanceID, Owner.ObjectIdentity.ID);
        if (oneWay is not null) GPU.SetShapeOneWay(handle, new(true, oneWay.LocalDirection.Rotated(-transform.Rotation), oneWay.Margin));
        Space!.RegisterGPUShape(Owner, handle, index);
        GPUPhysicsBodyStore.QueryGeometry? query = null;
        if (sensor)
        {
            if (_sensorQueries.Length <= index) Array.Resize(ref _sensorQueries, Math.Max(8, index + 1));
            query = _sensorQueries[index] ??= GPU.RetainQueryGeometry(shape);
            query.RebindColliderSlot(shape);
        }
        GPUShapes.Add((handle, index, query, transform)); GPUStateValid = false;
    }
    internal override void RefreshIdentity()
    {
        foreach (var shape in GPUShapes) GPU.SetQueryIdentity(shape.Handle, (ulong)Owner.RID.GetID(), shape.Slot, Owner.CanvasInstanceID, Owner.ObjectIdentity.ID);
    }
    internal void SetGPUConstants(Vector2 force, float torque)
    {
        if (force == _gpuConstantForce && torque == _gpuConstantTorque) return;
        GPU!.SetConstantForce(GPUHandle, force, torque); _gpuConstantForce = force; _gpuConstantTorque = torque;
    }
    /// <summary>Checks attachment-local preparation and static-field demand; edits and replay invalidate the prepared policy.</summary>
    internal bool NeedsIntegrationPreparation(bool dirty, bool force, bool resolveStaticFields) => force || !_gpuParametersPrepared || dirty ||
        _gpuResolveStaticFields != resolveStaticFields;
    /// <summary>Consumes value-only integration policy, rebuilding after attachment, role changes or replay.</summary>
    /// <returns>True when ordinary successful preparation permits the binding layer to retire its dirty flags.</returns>
    internal bool PrepareGPUParameters(in PhysicsBodyIntegrationConfiguration configuration, bool dirty, bool force, bool resolveStaticFields)
    {
        if (!NeedsIntegrationPreparation(dirty, force, resolveStaticFields)) return false;
        var policy = new GPUPhysicsBodyStore.IntegrationPolicy(configuration.GravityScale, configuration.LinearDamp, configuration.AngularDamp,
            configuration.LockRotation, configuration.OmitForces, configuration.LinearDampMode, configuration.AngularDampMode, resolveStaticFields);
        if (GPU!.GetIntegrationPolicy(GPUHandle) != policy) { GPU.SetIntegrationPolicy(GPUHandle, policy); Space!.InvalidateGPUStates(); }
        GPU.SetCCDMode(GPUHandle, configuration.CCDMode);
        SetGPUConstants(configuration.ConstantForce, configuration.ConstantTorque);
        if (!force)
        {
            _gpuResolveStaticFields = resolveStaticFields;
            _gpuParametersPrepared = true;
        }
        return !force;
    }
    internal override void InvalidateCollisionExceptions() => Space.BackendImplementation.InvalidateCollisionExceptions(Owner);
    internal override PhysicsBodyFields ReadResolvedFields(in PhysicsBodyFields current)
    {
        ref readonly var state = ref GPUState;
        return new(state.Gravity, state.TotalLinearDamp, state.TotalAngularDamp, state.FieldsInitialized);
    }
}
