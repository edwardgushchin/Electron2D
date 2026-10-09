namespace Electron2D;

internal sealed partial class PhysicsJointRuntime
{
    internal readonly record struct ReplayConfiguration(PhysicsServer.JointType Type, RID BodyA, RID BodyB, Transform FrameA, Transform FrameB, float LowerTranslation, float UpperTranslation, bool DisableCollision, float Bias, float MaxBias, float MaxForce, float PinSoftness, bool PinLimitEnabled, float PinLimitLower, float PinLimitUpper, bool PinMotorEnabled, float PinMotorVelocity, float PinMotorMaxTorque, float SpringRestLength, float SpringStiffness, float SpringDamping, bool SpringAutomaticRest, float SpringAutomaticLength);
    internal ReplayConfiguration CaptureReplay() => new(Type, BodyA, BodyB, FrameA, FrameB, LowerTranslation, UpperTranslation, DisableCollision, Bias, MaxBias, MaxForce, PinSoftness, PinLimitEnabled, PinLimitLower, PinLimitUpper, PinMotorEnabled, PinMotorVelocity, PinMotorMaxTorque, SpringRestLength, SpringStiffness, SpringDamping, SpringAutomaticRest, SpringAutomaticLength);
}
