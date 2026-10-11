namespace Electron2D;

/// <summary>Samples complete default-force intent in engine units without scene/runtime objects or solver storage.</summary>
/// <param name="PendingForce">Pending Force in the owning operation's engine-unit contract.</param>
/// <param name="PendingTorque">Pending Torque in the owning operation's engine-unit contract.</param>
/// <param name="ConstantForce">Constant Force in the owning operation's engine-unit contract.</param>
/// <param name="ConstantTorque">Constant Torque in the owning operation's engine-unit contract.</param>
/// <param name="GravityScale">Gravity Scale in the owning operation's engine-unit contract.</param>
/// <param name="OmitForces">Omit Forces in the owning operation's engine-unit contract.</param>
/// <param name="RotationLocked">Rotation Locked in the owning operation's engine-unit contract.</param>
/// <param name="ApplyPersistentForces">Apply Persistent Forces in the owning operation's engine-unit contract.</param>
/// <param name="RefreshGravityScale">Refresh Gravity Scale in the owning operation's engine-unit contract.</param>
public readonly record struct PhysicsBodyForceConfiguration(Vector2 PendingForce, float PendingTorque,
    Vector2 ConstantForce, float ConstantTorque, float GravityScale, bool OmitForces, bool RotationLocked,
    bool ApplyPersistentForces, bool RefreshGravityScale);
