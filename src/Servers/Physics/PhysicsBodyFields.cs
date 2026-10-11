namespace Electron2D;

/// <summary>Resolved engine-unit gravity and damping sampled for current body observers and portable state.</summary>
internal readonly record struct PhysicsBodyFields(Vector2 Gravity, float LinearDamp, float AngularDamp, bool Initialized);
