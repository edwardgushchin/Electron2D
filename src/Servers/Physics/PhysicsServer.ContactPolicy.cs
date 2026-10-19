namespace Electron2D;

public sealed partial class PhysicsServer
{
    private PhysicsSpace WorldParameterSpace(RID space)
    {
        ThrowIfDisposed();
        var world = GetSceneSpace(space);
        world.EnsureImplementationInputReadAccess();
        return world;
    }

    internal float BodyGetCollisionPriorityCore(RID body) => ColliderBackend(body, false, input: true).CollisionPriority;
    internal void BodySetCollisionPriorityCore(RID body, float value)
    {
        ThrowIfDisposed(); var owner = ResolveBodyOwners(body);
        if (owner.Scene is { } scene) scene.CollisionPriority = value;
        else owner.Server!.Backend.SetCollisionPriority(value);
    }
    internal float SpaceGetContactRecycleRadiusCore(RID space)
    {
        return WorldParameterSpace(space).ContactSettings.RecycleRadius;
    }
    internal void SpaceSetContactRecycleRadiusCore(RID space, float value)
    {
        ThrowIfDisposed(); var world = GetSceneSpace(space); world.SetContactSettings(world.ContactSettings with { RecycleRadius = value });
    }
    internal float SpaceGetContactMaxSeparationCore(RID space)
    {
        return WorldParameterSpace(space).ContactSettings.MaxSeparation;
    }
    internal void SpaceSetContactMaxSeparationCore(RID space, float value)
    {
        ThrowIfDisposed(); var world = GetSceneSpace(space); world.SetContactSettings(world.ContactSettings with { MaxSeparation = value });
    }
    internal int SpaceGetSolverIterationsCore(RID space)
    {
        return WorldParameterSpace(space).SolverIterations;
    }
    internal void SpaceSetSolverIterationsCore(RID space, int value)
    {
        ThrowIfDisposed(); GetSceneSpace(space).SetSolverIterations(value);
    }
    internal float SpaceGetContactDefaultBiasCore(RID space)
    {
        return WorldParameterSpace(space).ContactSettings.Bias;
    }
    internal void SpaceSetContactDefaultBiasCore(RID space, float value)
    {
        ThrowIfDisposed(); var world = GetSceneSpace(space); world.SetContactSettings(world.ContactSettings with { Bias = value });
    }
    internal float SpaceGetContactMaxAllowedPenetrationCore(RID space)
    {
        return WorldParameterSpace(space).ContactSettings.AllowedPenetration;
    }
    internal void SpaceSetContactMaxAllowedPenetrationCore(RID space, float value)
    {
        ThrowIfDisposed(); var world = GetSceneSpace(space); world.SetContactSettings(world.ContactSettings with { AllowedPenetration = value });
    }
}
