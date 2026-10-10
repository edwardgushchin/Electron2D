namespace Electron2D;

/// <summary>Samples complete engine-unit joint frames and policies without runtime or native storage.</summary>
internal readonly record struct PhysicsJointConfiguration(PhysicsServer.JointType Type, Transform FrameA, Transform FrameB,
    bool DisableCollision, float LowerTranslation, float UpperTranslation, float Bias, float MaxBias, float MaxForce,
    float PinSoftness, bool PinLimitEnabled, float PinLimitLower, float PinLimitUpper, bool PinMotorEnabled,
    float PinMotorVelocity, float PinMotorMaxTorque, float EffectiveSpringRestLength, float SpringStiffness, float SpringDamping);
