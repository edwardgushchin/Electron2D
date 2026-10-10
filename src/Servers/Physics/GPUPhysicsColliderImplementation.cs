namespace Electron2D;

/// <summary>Owns one resident GPU collider's handles, shape leases and publication caches.</summary>
internal sealed partial class GPUPhysicsColliderImplementation(PhysicsColliderBackend owner, PhysicsSpace space) : PhysicsColliderImplementation(owner, space)
{
    internal bool GPUSensor;
    internal uint GPUMask;
    internal GPUPhysicsBodyStore.BodyHandle GPUHandle { get; private set; }
    internal readonly List<(GPUPhysicsBodyStore.ShapeHandle Handle, int Slot, GPUPhysicsBodyStore.QueryGeometry? Query, Transform Pose)> GPUShapes = [];
    internal override int ShapeCount => GPUShapes.Count;
    private GPUPhysicsBodyStore GPU => Space.GPUStore!;
    private GPUPhysicsBodyStore.Snapshot _gpuState;
    private System.Numerics.Vector4 _gpuPose;
    private bool _gpuPoseValid;
    private long _gpuPoseEpoch;
    internal void AcceptGPUPose(in System.Numerics.Vector4 pose)
    {
        _gpuPose = pose; _gpuPoseValid = true; _gpuPoseEpoch = Space.GPUStateEpoch;
    }
    private System.Numerics.Vector4 CurrentPose => GPUStateValid ? _gpuState.Pose :
        _gpuPoseValid && _gpuPoseEpoch == Space.GPUStateEpoch ? _gpuPose : GPUState.Pose;

    private bool _gpuStateValid, _gpuStateMatchesPublication;
    private bool _gpuParametersPrepared;
    private long _gpuStateEpoch;
    internal bool GPUStateValid
    {
        get => _gpuStateValid && _gpuStateEpoch == Space!.GPUStateEpoch;
        set { _gpuStateValid = value; if (value) _gpuStateEpoch = Space!.GPUStateEpoch; }
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
        _gpuStateMatchesPublication = published || Space!.GPUStatePublicationEpoch == Space.GPUStateEpoch;
    }
    internal void CompleteGPUStatePublication() => GPUStateValid = _gpuStateMatchesPublication;
    internal override void Attach(Vector2 position, float rotation, in PhysicsBodyConfiguration configuration)
    {
        var space = Space;
        var store = GPU;
        GPUHandle = store.Add(new(configuration.Mode, position, rotation, configuration.LinearVelocity,
            configuration.AngularVelocity, GravityScale: configuration.GravityScale, CanSleep: configuration.CanSleep,
            Sleeping: configuration.Sleeping, LockRotation: configuration.LockRotation));
        GPUStateValid = false; _gpuStateMatchesPublication = false;
        _gpuParametersPrepared = false;
        _gpuConstantForce = _gpuSurfaceLinear = default; _gpuSurfaceAngular = _gpuConstantTorque = 0;
        space.RegisterGPUCollider(Owner);
    }

    private void ClearGPUShapes()
    {
        foreach (var shape in GPUShapes) { shape.Query?.Dispose(); Space!.UnregisterGPUShape(shape.Handle); GPU!.RemoveShape(shape.Handle); }
        GPUShapes.Clear();
    }
    private void AddGPUShape(Shape shape, Transform transform, int index, bool sensor, uint layer, uint mask,
        float friction, float bounce, OneWayContactData? oneWay)
    {
        var handle = GPU!.AddShape(GPUHandle, shape, transform, layer, mask, sensor, friction, bounce);
        GPU.SetQueryIdentity(handle, (ulong)Owner.RID.GetID(), index, Owner.CanvasInstanceID, Owner.ObjectIdentity.ID);
        if (oneWay is not null) GPU.SetShapeOneWay(handle, new(true, oneWay.LocalDirection.Rotated(-transform.Rotation), oneWay.Margin));
        Space!.RegisterGPUShape(Owner, handle, index);
        GPUShapes.Add((handle, index, sensor ? GPU.RetainQueryGeometry(shape) : null, transform)); GPUStateValid = false;
    }
    internal void RefreshGPUIdentity()
    {
        if (GPU is not { } store) return;
        foreach (var shape in GPUShapes) store.SetQueryIdentity(shape.Handle, (ulong)Owner.RID.GetID(), shape.Slot, Owner.CanvasInstanceID, Owner.ObjectIdentity.ID);
    }
    internal void SetGPUConstants(Vector2 force, float torque)
    {
        if (force == _gpuConstantForce && torque == _gpuConstantTorque) return;
        GPU!.SetConstantForce(GPUHandle, force, torque); _gpuConstantForce = force; _gpuConstantTorque = torque;
    }
    /// <summary>Synchronizes authored integration edits, rebuilding after attachment, role changes or replay.</summary>
    internal void PrepareGPUParameters(PhysicsBodyRuntime runtime, bool force = false)
    {
        var rigid = runtime.Owners.Scene as RigidBody;
        if (!force && _gpuParametersPrepared && !runtime.GPUParametersDirty && rigid?.GPUParametersDirty != true) return;
        var policy = new GPUPhysicsBodyStore.IntegrationPolicy(rigid?.GravityScale ?? runtime.BodyGravityScale,
            rigid?.LinearDamp ?? runtime.BodyLinearDamp, rigid?.AngularDamp ?? runtime.BodyAngularDamp,
            rigid is null ? RotationLocked : !rigid.Freeze && rigid.LockRotation, rigid?.CustomIntegrator ?? runtime.OmitForces,
            rigid?.LinearDampMode ?? runtime.BodyLinearDampMode, rigid?.AngularDampMode ?? runtime.BodyAngularDampMode);
        if (GPU!.GetIntegrationPolicy(GPUHandle) != policy) { GPU.SetIntegrationPolicy(GPUHandle, policy); Space!.InvalidateGPUStates(); }
        GPU.SetCCDMode(GPUHandle, runtime.ContinuousMode);
        SetGPUConstants(rigid?.ConstantForce ?? runtime.ConstantForce, rigid?.ConstantTorque ?? runtime.ConstantTorque);
        if (!force)
        {
            _gpuParametersPrepared = true; runtime.GPUParametersDirty = false;
            if (rigid is not null) rigid.GPUParametersDirty = false;
        }
    }
    internal void PublishGPUFields(PhysicsBodyRuntime runtime)
    {
        ref readonly var state = ref GPUState;
        runtime.Gravity = state.Gravity; runtime.LinearDamp = state.TotalLinearDamp;
        runtime.AngularDamp = state.TotalAngularDamp; runtime.FieldsInitialized = state.FieldsInitialized;
    }
}
