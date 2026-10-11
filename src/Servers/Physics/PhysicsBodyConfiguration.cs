namespace Electron2D;

/// <summary>Describes initial body motion and integration policy in scene units without backend storage.</summary>
/// <param name="Mode">Mode in the owning operation's engine-unit contract.</param>
/// <param name="LinearVelocity">Linear Velocity in the owning operation's engine-unit contract.</param>
/// <param name="AngularVelocity">Angular Velocity in the owning operation's engine-unit contract.</param>
/// <param name="GravityScale">Gravity Scale in the owning operation's engine-unit contract.</param>
/// <param name="CanSleep">Can Sleep in the owning operation's engine-unit contract.</param>
/// <param name="Sleeping">Sleeping in the owning operation's engine-unit contract.</param>
/// <param name="LockRotation">Lock Rotation in the owning operation's engine-unit contract.</param>
public readonly record struct PhysicsBodyConfiguration(
    PhysicsServer.BodyMode Mode,
    Vector2 LinearVelocity = default,
    float AngularVelocity = 0,
    float GravityScale = 1,
    bool CanSleep = true,
    bool Sleeping = false,
    bool LockRotation = false);
