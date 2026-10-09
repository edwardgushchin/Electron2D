namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    internal RID RID => rid;
    internal WeakReference<CollisionObject>? SceneOwnerReference => _sceneOwner;
    internal bool GPUSensor;
    internal uint GPUMask;
    internal CollisionObject? SceneOwner => _sceneOwner is { } weak && weak.TryGetTarget(out var node) && !node.IsDisposed ? node : null;
    internal GPUPhysicsBodyStore.BodyHandle GPUHandle { get; private set; }
    internal readonly List<(GPUPhysicsBodyStore.ShapeHandle Handle, int Slot, GPUPhysicsBodyStore.QueryGeometry? Query, Transform Pose)> GPUShapes = [];
    private GPUPhysicsBodyStore? GPU => Space?.GPUStore;
    private GPUPhysicsBodyStore.Snapshot _gpuState;
    private bool _gpuStateValid, _gpuStateMatchesPublication;
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
    private void AttachGPU(PhysicsSpace space, Vector2 position, float rotation, in PhysicsBodyConfiguration configuration, long version)
    {
        var store = space.GPUStore!;
        GPUHandle = store.Add(new(configuration.Mode, position, rotation, configuration.LinearVelocity,
            configuration.AngularVelocity, GravityScale: configuration.GravityScale, CanSleep: configuration.CanSleep,
            Sleeping: configuration.Sleeping, LockRotation: configuration.LockRotation));
        Space = space; AttachmentVersion = version; GPUStateValid = false; _gpuStateMatchesPublication = false;
        _gpuConstantForce = _gpuSurfaceLinear = default; _gpuSurfaceAngular = _gpuConstantTorque = 0;
        space.RegisterGPUCollider(this); ExternalObjectNode()?.AddPhysicsObjectBinding(this);
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
        GPU.SetQueryIdentity(handle, (ulong)rid.GetID(), index, CanvasInstanceID, ObjectIdentity.ID);
        if (oneWay is not null) GPU.SetShapeOneWay(handle, new(true, oneWay.LocalDirection.Rotated(-transform.Rotation), oneWay.Margin));
        Space!.RegisterGPUShape(this, handle, index);
        GPUShapes.Add((handle, index, sensor ? GPU.RetainQueryGeometry(shape) : null, transform)); GPUStateValid = false;
    }
    internal void RefreshGPUIdentity()
    {
        if (GPU is not { } store) return;
        foreach (var shape in GPUShapes) store.SetQueryIdentity(shape.Handle, (ulong)rid.GetID(), shape.Slot, CanvasInstanceID, ObjectIdentity.ID);
    }
    internal void SetGPUConstants(Vector2 force, float torque)
    {
        if (force == _gpuConstantForce && torque == _gpuConstantTorque) return;
        GPU!.SetConstantForce(GPUHandle, force, torque); _gpuConstantForce = force; _gpuConstantTorque = torque;
    }
    internal void PrepareGPUParameters(PhysicsBodyRuntime runtime)
    {
        var rigid = runtime.Owners.Scene as RigidBody;
        var policy = new GPUPhysicsBodyStore.IntegrationPolicy(rigid?.GravityScale ?? runtime.BodyGravityScale,
            rigid?.LinearDamp ?? runtime.BodyLinearDamp, rigid?.AngularDamp ?? runtime.BodyAngularDamp,
            rigid is null ? runtime.RotationLocked : !rigid.Freeze && rigid.LockRotation, runtime.Omitted,
            rigid?.LinearDampMode ?? runtime.BodyLinearDampMode, rigid?.AngularDampMode ?? runtime.BodyAngularDampMode);
        if (GPU!.GetIntegrationPolicy(GPUHandle) != policy) { GPU.SetIntegrationPolicy(GPUHandle, policy); Space!.InvalidateGPUStates(); }
        GPU.SetCCDMode(GPUHandle, runtime.ContinuousMode);
        SetGPUConstants(runtime.GetConstantForce(), runtime.GetConstantTorque());
    }
    internal void PublishGPUFields(PhysicsBodyRuntime runtime)
    {
        ref readonly var state = ref GPUState;
        runtime.Gravity = state.Gravity; runtime.LinearDamp = state.TotalLinearDamp;
        runtime.AngularDamp = state.TotalAngularDamp; runtime.FieldsInitialized = state.FieldsInitialized;
    }
}
