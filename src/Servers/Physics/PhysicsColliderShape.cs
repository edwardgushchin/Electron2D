namespace Electron2D;

/// <summary>Describes one authored logical slot without scene/server storage or solver handles.</summary>
/// <param name="Shape">Shape in the owning operation's engine-unit contract.</param>
/// <param name="Transform">Transform in the owning operation's engine-unit contract.</param>
/// <param name="Index">Index in the owning operation's engine-unit contract.</param>
/// <param name="Enabled">Enabled in the owning operation's engine-unit contract.</param>
/// <param name="OneWay">One Way in the owning operation's engine-unit contract.</param>
public readonly record struct PhysicsColliderShape(Shape Shape, Transform Transform, int Index, bool Enabled, OneWayContactData? OneWay);
