namespace Electron2D;

internal sealed class GPUPhysicsJointImplementation(PhysicsSpace space) : PhysicsJointImplementation(space)
{
    private GPUPhysicsBodyStore.JointHandle _gpuJoint;
    private GPUPhysicsBodyStore.JointDefinition _gpuDefinition;

    private GPUPhysicsBodyStore.JointDefinition GPUDefinition(PhysicsJointRuntime settings) => _gpuDefinition with
    {
        DisableCollision = settings.DisableCollision,
        LimitEnabled = settings.PinLimitEnabled,
        LowerAngle = settings.PinLimitEnabled ? settings.PinLimitLower : 0,
        UpperAngle = settings.PinLimitEnabled ? settings.PinLimitUpper : 0,
        MotorEnabled = settings.PinMotorEnabled,
        MotorVelocity = settings.PinMotorVelocity,
        MotorMaxTorque = settings.PinMotorMaxTorque,
        LowerTranslation = settings.LowerTranslation,
        UpperTranslation = settings.UpperTranslation,
        RestLength = settings.EffectiveSpringRestLength,
        Stiffness = settings.SpringStiffness,
        Damping = settings.SpringDamping,
        Bias = settings.Bias == 0 ? Space.ConstraintDefaultBias : settings.Bias,
        MaxBias = settings.MaxBias,
        MaxForce = settings.MaxForce,
        Softness = settings.PinSoftness
    };
    internal override bool IsAttached => _gpuJoint.Generation != 0;
    internal GPUPhysicsBodyStore.JointHandle Handle => _gpuJoint;
    internal override void Attach(PhysicsColliderBackend first, PhysicsColliderBackend? second, PhysicsJointRuntime settings)
    {
        if (IsAttached) throw new InvalidOperationException("A joint already has a backend attachment.");
        _gpuDefinition = new(settings.Type, first.GPUHandle, second?.GPUHandle ?? default, settings.FrameA, settings.FrameB);
        _gpuDefinition = GPUDefinition(settings);
        _gpuJoint = Space.GPUStore!.AddJoint(_gpuDefinition);
    }
    internal override void Detach()
    {
        try { if (IsAttached && !Space.HasBackendFailure) Space.GPUStore!.RemoveJoint(_gpuJoint); }
        finally { _gpuJoint = default; _gpuDefinition = default; }
    }
    internal override void ApplySolverPolicy(PhysicsJointRuntime settings) => SetGPUJoint(GPUDefinition(settings));
    internal override void ApplyPortableFrames(Transform first, Transform second) => SetGPUJoint(_gpuDefinition with { FrameA = first, FrameB = second });
    internal override void SetCollideConnected(bool value) => SetGPUJoint(_gpuDefinition with { DisableCollision = !value });
    internal override void SetPinLimits(bool enabled, float lower, float upper) =>
        SetGPUJoint(_gpuDefinition with { LimitEnabled = enabled, LowerAngle = enabled ? lower : 0, UpperAngle = enabled ? upper : 0 });
    internal override void SetPinMotorEnabled(bool value) => SetGPUJoint(_gpuDefinition with { MotorEnabled = value });
    internal override void SetPinMotorVelocity(float value) => SetGPUJoint(_gpuDefinition with { MotorVelocity = value });
    internal override void SetPinMotorMaxTorque(float value) => SetGPUJoint(_gpuDefinition with { MotorMaxTorque = value });
    internal override void SetGrooveLimits(float lower, float upper) => SetGPUJoint(_gpuDefinition with { LowerTranslation = lower, UpperTranslation = upper });
    private void SetGPUJoint(in GPUPhysicsBodyStore.JointDefinition definition)
    {
        if (_gpuDefinition == definition) return;
        Space.GPUStore!.SetJoint(_gpuJoint, definition); _gpuDefinition = definition; Space.InvalidateGPUStates();
    }
}
