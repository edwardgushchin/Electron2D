namespace Electron2D;

public sealed partial class PhysicsServer
{
    internal float BodyGetCollisionPriorityCore(RID body)
    {
        ThrowIfDisposed(); var owner = ResolveBodyOwners(body);
        if (owner.Scene is { } scene) return scene.CollisionPriority;
        var backend = owner.Server!.Backend; backend.Space?.EnsureQueryAccess(); return backend.CollisionPriority;
    }
    internal void BodySetCollisionPriorityCore(RID body, float value)
    {
        ThrowIfDisposed(); var owner = ResolveBodyOwners(body);
        if (owner.Scene is { } scene) scene.CollisionPriority = value;
        else owner.Server!.Backend.SetCollisionPriority(value);
    }
    internal float SpaceGetContactRecycleRadiusCore(RID space)
    {
        ThrowIfDisposed(); var world = GetSceneSpace(space); world.EnsureQueryAccess(); return world.ContactSettings.RecycleRadius;
    }
    internal void SpaceSetContactRecycleRadiusCore(RID space, float value)
    {
        ThrowIfDisposed(); var world = GetSceneSpace(space); world.SetContactSettings(world.ContactSettings with { RecycleRadius = value });
    }
    internal float SpaceGetContactMaxSeparationCore(RID space)
    {
        ThrowIfDisposed(); var world = GetSceneSpace(space); world.EnsureQueryAccess(); return world.ContactSettings.MaxSeparation;
    }
    internal void SpaceSetContactMaxSeparationCore(RID space, float value)
    {
        ThrowIfDisposed(); var world = GetSceneSpace(space); world.SetContactSettings(world.ContactSettings with { MaxSeparation = value });
    }
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
