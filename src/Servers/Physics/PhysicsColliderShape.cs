namespace Electron2D;

/// <summary>Describes one authored logical slot without scene/server storage or solver handles.</summary>
internal readonly record struct PhysicsColliderShape(Shape Shape, Transform Transform, int Index, bool Enabled, OneWayContactData? OneWay);
