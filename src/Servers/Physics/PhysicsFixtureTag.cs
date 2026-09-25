namespace Electron2D;

internal sealed record PhysicsFixtureTag(RID ColliderRID, int ShapeIndex, OneWayContactData? OneWay);
