namespace Electron2D;

/// <summary>Resolved engine-unit gravity and damping sampled for current body observers and portable state.</summary>
/// <param name="Gravity">Gravity in the owning operation's engine-unit contract.</param>
/// <param name="LinearDamp">Linear Damp in the owning operation's engine-unit contract.</param>
/// <param name="AngularDamp">Angular Damp in the owning operation's engine-unit contract.</param>
/// <param name="Initialized">Initialized in the owning operation's engine-unit contract.</param>
public readonly record struct PhysicsBodyFields(Vector2 Gravity, float LinearDamp, float AngularDamp, bool Initialized);
