namespace Electron2D;

/// <summary>Samples authored Area or default-world fields in engine units without solver storage.</summary>
/// <param name="GravityVector">Gravity Vector in the owning operation's engine-unit contract.</param>
/// <param name="Gravity">Gravity in the owning operation's engine-unit contract.</param>
/// <param name="GravityPoint">Gravity Point in the owning operation's engine-unit contract.</param>
/// <param name="PointUnitDistance">Point Unit Distance in the owning operation's engine-unit contract.</param>
/// <param name="LinearDamp">Linear Damp in the owning operation's engine-unit contract.</param>
/// <param name="AngularDamp">Angular Damp in the owning operation's engine-unit contract.</param>
/// <param name="GravityMode">Gravity Mode in the owning operation's engine-unit contract.</param>
/// <param name="LinearMode">Linear Mode in the owning operation's engine-unit contract.</param>
/// <param name="AngularMode">Angular Mode in the owning operation's engine-unit contract.</param>
/// <param name="Priority">Priority in the owning operation's engine-unit contract.</param>
public readonly record struct PhysicsAreaFieldConfiguration(Vector2 GravityVector, float Gravity, bool GravityPoint,
    float PointUnitDistance, float LinearDamp, float AngularDamp, Area.SpaceOverride GravityMode,
    Area.SpaceOverride LinearMode, Area.SpaceOverride AngularMode, int Priority);
