namespace Electron2D;

public sealed partial class PhysicsServer
{
    internal void AreaSetGravitySpaceOverrideCore(RID area, Area.SpaceOverride value)
    {
        PhysicsAreaFields.ValidateMode(value);
        AreaFieldState(area, true).GravitySpaceOverride = value;
    }
    internal Area.SpaceOverride AreaGetGravitySpaceOverrideCore(RID area) => AreaFieldState(area).GravitySpaceOverride;

    internal void AreaSetGravityCore(RID area, float value)
    {
        PhysicsAreaFields.ValidateFinite(value);
        AreaFieldState(area, true).Gravity = value;
    }
    internal float AreaGetGravityCore(RID area) => AreaFieldState(area).Gravity;

    internal void AreaSetGravityVectorCore(RID area, Vector2 value)
    {
        PhysicsAreaFields.ValidateFinite(value);
        AreaFieldState(area, true).GravityVector = value;
    }
    internal Vector2 AreaGetGravityVectorCore(RID area) => AreaFieldState(area).GravityVector;

    internal void AreaSetGravityPointCore(RID area, bool value)
    {
        AreaFieldState(area, true).GravityPoint = value;
    }
    internal bool AreaGetGravityPointCore(RID area) => AreaFieldState(area).GravityPoint;

    internal void AreaSetGravityPointUnitDistanceCore(RID area, float value)
    {
        PhysicsAreaFields.ValidateFinite(value);
        AreaFieldState(area, true).GravityPointUnitDistance = value;
    }
    internal float AreaGetGravityPointUnitDistanceCore(RID area) => AreaFieldState(area).GravityPointUnitDistance;

    internal void AreaSetLinearDampSpaceOverrideCore(RID area, Area.SpaceOverride value)
    {
        PhysicsAreaFields.ValidateMode(value);
        AreaFieldState(area, true).LinearDampSpaceOverride = value;
    }
    internal Area.SpaceOverride AreaGetLinearDampSpaceOverrideCore(RID area) => AreaFieldState(area).LinearDampSpaceOverride;

    internal void AreaSetLinearDampCore(RID area, float value)
    {
        PhysicsAreaFields.ValidateFinite(value);
        AreaFieldState(area, true).LinearDamp = value;
    }
    internal float AreaGetLinearDampCore(RID area) => AreaFieldState(area).LinearDamp;

    internal void AreaSetAngularDampSpaceOverrideCore(RID area, Area.SpaceOverride value)
    {
        PhysicsAreaFields.ValidateMode(value);
        AreaFieldState(area, true).AngularDampSpaceOverride = value;
    }
    internal Area.SpaceOverride AreaGetAngularDampSpaceOverrideCore(RID area) => AreaFieldState(area).AngularDampSpaceOverride;

    internal void AreaSetAngularDampCore(RID area, float value)
    {
        PhysicsAreaFields.ValidateFinite(value);
        AreaFieldState(area, true).AngularDamp = value;
    }
    internal float AreaGetAngularDampCore(RID area) => AreaFieldState(area).AngularDamp;

    internal void AreaSetPriorityCore(RID area, int value)
    {
        AreaFieldState(area, true).Priority = value;
    }
    internal int AreaGetPriorityCore(RID area) => AreaFieldState(area).Priority;

    private PhysicsAreaFields AreaFieldState(RID area, bool writing = false)
    {
        ThrowIfDisposed();
        lock (_registryGate)
            if (_sceneSpaces.TryGetValue(area, out var space))
            {
                space.EnsureQueryAccess();
                return space.DefaultAreaFields;
            }
        var runtime = AreaRuntime(area);
        runtime.EnsureAccess();
        var owners = runtime.Owners;
        if (writing) owners.Scene?.EnsureFieldChange();
        return owners.Scene?.Fields ?? owners.Server!.AreaFields!;
    }
}
