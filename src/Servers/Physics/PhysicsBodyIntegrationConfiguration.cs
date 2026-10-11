namespace Electron2D;

/// <summary>Samples the complete authored integration policy and persistent force values without runtime or scene references.</summary>
internal readonly record struct PhysicsBodyIntegrationConfiguration(float GravityScale, float LinearDamp, float AngularDamp,
    RigidBody.DampMode LinearDampMode, RigidBody.DampMode AngularDampMode, bool LockRotation, bool OmitForces,
    Vector2 ConstantForce, float ConstantTorque, CCDMode CCDMode);
