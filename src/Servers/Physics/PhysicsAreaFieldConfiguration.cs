namespace Electron2D;

/// <summary>Samples authored Area or default-world fields in engine units without solver storage.</summary>
internal readonly record struct PhysicsAreaFieldConfiguration(Vector2 GravityVector, float Gravity, bool GravityPoint,
    float PointUnitDistance, float LinearDamp, float AngularDamp, Area.SpaceOverride GravityMode,
    Area.SpaceOverride LinearMode, Area.SpaceOverride AngularMode, int Priority);
