namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Gets the world's quiet velocity threshold in scene units per second.</summary>
    /// <param name="space">A live scene-owned or server-created space RID.</param>
    /// <returns>The finite nonnegative value sampled from ProjectSettings.Physics2DSleepThresholdLinear at world creation, or its later override.</returns>
    /// <exception cref="ArgumentException">The RID is stale or does not identify a space.</exception>
    /// <exception cref="InvalidOperationException">Access violates the world's owner thread or solver phase.</exception>
    public static float SpaceGetBodyLinearVelocitySleepThreshold(RID space) => Service.SpaceGetBodyLinearVelocitySleepThresholdCore(space);

    /// <summary>Sets the world's quiet velocity threshold in scene units per second.</summary>
    /// <param name="space">A live scene-owned or server-created space RID.</param>
    /// <param name="value">A finite nonnegative value within the backend's representable range.</param>
    /// <remarks>Changed settings wake dynamic bodies and restart their quiet timers. Equal writes are silent.
    /// Automatic sleep requires both speeds strictly below their thresholds for longer than the duration.
    /// A zero velocity threshold disables automatic sleep; zero duration allows sleep after one eligible positive step.
    /// Explicit body sleep and CanSleep remain independent. Inactive worlds do not accumulate quiet time.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or does not identify a space.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative, nonfinite or unrepresentable.</exception>
    /// <exception cref="InvalidOperationException">Access violates the world's owner thread or solver phase.</exception>
    public static void SpaceSetBodyLinearVelocitySleepThreshold(RID space, float value) => Service.SpaceSetBodyLinearVelocitySleepThresholdCore(space, value);

    /// <summary>Gets the world's quiet velocity threshold in radians per second.</summary>
    /// <param name="space">A live scene-owned or server-created space RID.</param>
    /// <returns>The finite nonnegative value sampled from ProjectSettings.Physics2DSleepThresholdAngular at world creation, or its later override.</returns>
    /// <exception cref="ArgumentException">The RID is stale or does not identify a space.</exception>
    /// <exception cref="InvalidOperationException">Access violates the world's owner thread or solver phase.</exception>
    public static float SpaceGetBodyAngularVelocitySleepThreshold(RID space) => Service.SpaceGetBodyAngularVelocitySleepThresholdCore(space);

    /// <summary>Sets the world's quiet velocity threshold in radians per second.</summary>
    /// <param name="space">A live scene-owned or server-created space RID.</param>
    /// <param name="value">A finite nonnegative value within the backend's representable range.</param>
    /// <remarks>Changed settings wake dynamic bodies and restart their quiet timers. Equal writes are silent.
    /// Automatic sleep requires both speeds strictly below their thresholds for longer than the duration.
    /// A zero velocity threshold disables automatic sleep; zero duration allows sleep after one eligible positive step.
    /// Explicit body sleep and CanSleep remain independent. Inactive worlds do not accumulate quiet time.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or does not identify a space.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative, nonfinite or unrepresentable.</exception>
    /// <exception cref="InvalidOperationException">Access violates the world's owner thread or solver phase.</exception>
    public static void SpaceSetBodyAngularVelocitySleepThreshold(RID space, float value) => Service.SpaceSetBodyAngularVelocitySleepThresholdCore(space, value);

    /// <summary>Gets the world's quiet duration in seconds.</summary>
    /// <param name="space">A live scene-owned or server-created space RID.</param>
    /// <returns>The finite nonnegative value sampled from ProjectSettings.Physics2DTimeBeforeSleep at world creation, or its later override.</returns>
    /// <exception cref="ArgumentException">The RID is stale or does not identify a space.</exception>
    /// <exception cref="InvalidOperationException">Access violates the world's owner thread or solver phase.</exception>
    public static float SpaceGetBodyTimeToSleep(RID space) => Service.SpaceGetBodyTimeToSleepCore(space);

    /// <summary>Sets the world's quiet duration in seconds.</summary>
    /// <param name="space">A live scene-owned or server-created space RID.</param>
    /// <param name="value">A finite nonnegative value within the backend's representable range.</param>
    /// <remarks>Changed settings wake dynamic bodies and restart their quiet timers. Equal writes are silent.
    /// Automatic sleep requires both speeds strictly below their thresholds for longer than the duration.
    /// A zero velocity threshold disables automatic sleep; zero duration allows sleep after one eligible positive step.
    /// Explicit body sleep and CanSleep remain independent. Inactive worlds do not accumulate quiet time.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or does not identify a space.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative, nonfinite or unrepresentable.</exception>
    /// <exception cref="InvalidOperationException">Access violates the world's owner thread or solver phase.</exception>
    public static void SpaceSetBodyTimeToSleep(RID space, float value) => Service.SpaceSetBodyTimeToSleepCore(space, value);

}
