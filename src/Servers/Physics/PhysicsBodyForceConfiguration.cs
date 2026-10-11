namespace Electron2D;

/// <summary>Samples complete default-force intent in engine units without scene/runtime objects or solver storage.</summary>
internal readonly record struct PhysicsBodyForceConfiguration(Vector2 PendingForce, float PendingTorque,
    Vector2 ConstantForce, float ConstantTorque, float GravityScale, bool OmitForces, bool RotationLocked,
    bool ApplyPersistentForces, bool RefreshGravityScale);
