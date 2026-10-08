namespace Electron2D;

/// <summary>Describes initial body motion and integration policy in scene units without backend storage.</summary>
internal readonly record struct PhysicsBodyConfiguration(
    PhysicsServer.BodyMode Mode,
    Vector2 LinearVelocity = default,
    float AngularVelocity = 0,
    float GravityScale = 1,
    bool CanSleep = true,
    bool Sleeping = false,
    bool LockRotation = false);
