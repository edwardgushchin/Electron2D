namespace Electron2D;

/// <summary>Samples complete engine-unit joint frames and policies without runtime or native storage.</summary>
/// <param name="Type">Type in the owning operation's engine-unit contract.</param>
/// <param name="FrameA">Frame A in the owning operation's engine-unit contract.</param>
/// <param name="FrameB">Frame B in the owning operation's engine-unit contract.</param>
/// <param name="DisableCollision">Disable Collision in the owning operation's engine-unit contract.</param>
/// <param name="LowerTranslation">Lower Translation in the owning operation's engine-unit contract.</param>
/// <param name="UpperTranslation">Upper Translation in the owning operation's engine-unit contract.</param>
/// <param name="Bias">Bias in the owning operation's engine-unit contract.</param>
/// <param name="MaxBias">Max Bias in the owning operation's engine-unit contract.</param>
/// <param name="MaxForce">Max Force in the owning operation's engine-unit contract.</param>
/// <param name="PinSoftness">Pin Softness in the owning operation's engine-unit contract.</param>
/// <param name="PinLimitEnabled">Pin Limit Enabled in the owning operation's engine-unit contract.</param>
/// <param name="PinLimitLower">Pin Limit Lower in the owning operation's engine-unit contract.</param>
/// <param name="PinLimitUpper">Pin Limit Upper in the owning operation's engine-unit contract.</param>
/// <param name="PinMotorEnabled">Pin Motor Enabled in the owning operation's engine-unit contract.</param>
/// <param name="PinMotorVelocity">Pin Motor Velocity in the owning operation's engine-unit contract.</param>
/// <param name="PinMotorMaxTorque">Pin Motor Max Torque in the owning operation's engine-unit contract.</param>
/// <param name="EffectiveSpringRestLength">Effective Spring Rest Length in the owning operation's engine-unit contract.</param>
/// <param name="SpringStiffness">Spring Stiffness in the owning operation's engine-unit contract.</param>
/// <param name="SpringDamping">Spring Damping in the owning operation's engine-unit contract.</param>
public readonly record struct PhysicsJointConfiguration(PhysicsServer.JointType Type, Transform FrameA, Transform FrameB,
    bool DisableCollision, float LowerTranslation, float UpperTranslation, float Bias, float MaxBias, float MaxForce,
    float PinSoftness, bool PinLimitEnabled, float PinLimitLower, float PinLimitUpper, bool PinMotorEnabled,
    float PinMotorVelocity, float PinMotorMaxTorque, float EffectiveSpringRestLength, float SpringStiffness, float SpringDamping);
