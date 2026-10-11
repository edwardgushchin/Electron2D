namespace Electron2D;

/// <summary>Samples the complete authored integration policy and persistent force values without runtime or scene references.</summary>
/// <param name="GravityScale">Gravity Scale in the owning operation's engine-unit contract.</param>
/// <param name="LinearDamp">Linear Damp in the owning operation's engine-unit contract.</param>
/// <param name="AngularDamp">Angular Damp in the owning operation's engine-unit contract.</param>
/// <param name="LinearDampMode">Linear Damp Mode in the owning operation's engine-unit contract.</param>
/// <param name="AngularDampMode">Angular Damp Mode in the owning operation's engine-unit contract.</param>
/// <param name="LockRotation">Lock Rotation in the owning operation's engine-unit contract.</param>
/// <param name="OmitForces">Omit Forces in the owning operation's engine-unit contract.</param>
/// <param name="ConstantForce">Constant Force in the owning operation's engine-unit contract.</param>
/// <param name="ConstantTorque">Constant Torque in the owning operation's engine-unit contract.</param>
/// <param name="CCDMode">CCDMode in the owning operation's engine-unit contract.</param>
public readonly record struct PhysicsBodyIntegrationConfiguration(float GravityScale, float LinearDamp, float AngularDamp,
    RigidBody.DampMode LinearDampMode, RigidBody.DampMode AngularDampMode, bool LockRotation, bool OmitForces,
    Vector2 ConstantForce, float ConstantTorque, CCDMode CCDMode);
