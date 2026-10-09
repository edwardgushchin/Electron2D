namespace Electron2D;

public sealed partial class PhysicsServer
{
    internal int SpaceGetSolverIterationsCore(RID space)
    {
        ThrowIfDisposed(); var world = GetSceneSpace(space); world.EnsureQueryAccess(); return world.SolverIterations;
    }
    internal void SpaceSetSolverIterationsCore(RID space, int value)
    {
        ThrowIfDisposed(); GetSceneSpace(space).SetSolverIterations(value);
    }
    internal float SpaceGetContactDefaultBiasCore(RID space)
    {
        ThrowIfDisposed(); var world = GetSceneSpace(space); world.EnsureQueryAccess(); return world.ContactSettings.Bias;
    }
    internal void SpaceSetContactDefaultBiasCore(RID space, float value)
    {
        ThrowIfDisposed(); var world = GetSceneSpace(space); world.SetContactSettings(world.ContactSettings with { Bias = value });
    }
    internal float SpaceGetContactMaxAllowedPenetrationCore(RID space)
    {
        ThrowIfDisposed(); var world = GetSceneSpace(space); world.EnsureQueryAccess(); return world.ContactSettings.AllowedPenetration;
    }
    internal void SpaceSetContactMaxAllowedPenetrationCore(RID space, float value)
    {
        ThrowIfDisposed(); var world = GetSceneSpace(space); world.SetContactSettings(world.ContactSettings with { AllowedPenetration = value });
    }
}
