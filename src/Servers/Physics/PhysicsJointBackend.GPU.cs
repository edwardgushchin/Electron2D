namespace Electron2D;

internal sealed partial class PhysicsJointBackend
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
        Bias = settings.Bias == 0 ? _space!.ConstraintDefaultBias : settings.Bias,
        MaxBias = settings.MaxBias,
        MaxForce = settings.MaxForce,
        Softness = settings.PinSoftness
    };
    private void AttachGPU(PhysicsSpace space, PhysicsColliderBackend first, PhysicsColliderBackend? second, PhysicsJointRuntime settings)
    {
        _space = space;
        _gpuDefinition = new(settings.Type, first.GPUHandle, second?.GPUHandle ?? default, settings.FrameA, settings.FrameB);
        _gpuDefinition = GPUDefinition(settings);
        try { _gpuJoint = space.GPUStore!.AddJoint(_gpuDefinition); }
        catch { _space = null; _gpuDefinition = default; throw; }
    }
    private void SetGPUJoint(in GPUPhysicsBodyStore.JointDefinition definition)
    {
        if (_gpuDefinition == definition) return;
        _space!.GPUStore!.SetJoint(_gpuJoint, definition); _gpuDefinition = definition; _space.InvalidateGPUStates();
    }
}
