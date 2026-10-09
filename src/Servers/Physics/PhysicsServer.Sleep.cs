namespace Electron2D;

public sealed partial class PhysicsServer
{
    private PhysicsSpace SleepSpace(RID space)
    {
        ThrowIfDisposed(); var world = GetSceneSpace(space); world.EnsureQueryAccess(); return world;
    }
    internal float SpaceGetBodyLinearVelocitySleepThresholdCore(RID space) => SleepSpace(space).SleepSettings.LinearThreshold;
    internal void SpaceSetBodyLinearVelocitySleepThresholdCore(RID space, float value)
    {
        var world = SleepSpace(space); world.SetSleepSettings(world.SleepSettings with { LinearThreshold = value });
    }
    internal float SpaceGetBodyAngularVelocitySleepThresholdCore(RID space) => SleepSpace(space).SleepSettings.AngularThreshold;
    internal void SpaceSetBodyAngularVelocitySleepThresholdCore(RID space, float value)
    {
        var world = SleepSpace(space); world.SetSleepSettings(world.SleepSettings with { AngularThreshold = value });
    }
    internal float SpaceGetBodyTimeToSleepCore(RID space) => SleepSpace(space).SleepSettings.TimeToSleep;
    internal void SpaceSetBodyTimeToSleepCore(RID space, float value)
    {
        var world = SleepSpace(space); world.SetSleepSettings(world.SleepSettings with { TimeToSleep = value });
    }
}
