namespace Electron2D;

/// <summary>Returns resolved body mass, polar inertia and local center in engine units.</summary>
/// <param name="Mass">Positive finite mass.</param>
/// <param name="Inertia">Finite nonnegative inertia about the local center.</param>
/// <param name="Center">Finite local center of mass.</param>
public readonly record struct PhysicsMassProperties(float Mass, float Inertia, Vector2 Center);
