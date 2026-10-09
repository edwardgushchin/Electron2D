namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Gets a scene or server body's relative penetration-recovery priority.</summary>
    /// <param name="body">A live body RID, including a detached body.</param>
    /// <returns>The finite positive weight, initially one.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access violates owner-thread or solver-phase rules.</exception>
    public static float BodyGetCollisionPriority(RID body) => Service.BodyGetCollisionPriorityCore(body);

    /// <summary>Sets how strongly body-motion recovery favors separation from this body.</summary>
    /// <param name="body">A live body RID, including a detached body.</param>
    /// <param name="priority">A finite positive relative weight.</param>
    /// <remarks>Applies to penetration recovery in BodyTestMotion and scene movement. It does not change
    /// rigid contact impulses, sleep or fixture identity. Scene-owned bodies expose the same value.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The weight is nonfinite or not positive.</exception>
    /// <exception cref="InvalidOperationException">Attached access violates owner-thread or solver-phase rules.</exception>
    public static void BodySetCollisionPriority(RID body, float priority) => Service.BodySetCollisionPriorityCore(body, priority);
    /// <summary>Gets the world's body-local radius for reusing cached contact impulses.</summary>
    /// <param name="space">A live scene-owned or server-created space RID.</param>
    /// <returns>The configured nonnegative scene-unit distance.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live space.</exception>
    /// <exception cref="InvalidOperationException">Access violates owner-thread or solver-phase rules.</exception>
    public static float SpaceGetContactRecycleRadius(RID space) => Service.SpaceGetContactRecycleRadiusCore(space);

    /// <summary>Sets the world's body-local radius for reusing cached contact impulses.</summary>
    /// <param name="space">A live scene-owned or server-created space RID.</param>
    /// <param name="value">A finite nonnegative distance with representable squared value and backend conversion.</param>
    /// <remarks>Fresh collision geometry is still computed every interval. This setting controls history reuse,
    /// not query margins or collision-event hysteresis. Changed values wake dynamics; equal writes preserve sleep.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live space.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative, nonfinite or outside its representable range.</exception>
    /// <exception cref="InvalidOperationException">Access violates owner-thread or solver-phase rules.</exception>
    public static void SpaceSetContactRecycleRadius(RID space, float value) => Service.SpaceSetContactRecycleRadiusCore(space, value);

    /// <summary>Gets the world's maximum normal separation or tangential drift of cached contacts.</summary>
    /// <param name="space">A live scene-owned or server-created space RID.</param>
    /// <returns>The configured nonnegative scene-unit distance.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live space.</exception>
    /// <exception cref="InvalidOperationException">Access violates owner-thread or solver-phase rules.</exception>
    public static float SpaceGetContactMaxSeparation(RID space) => Service.SpaceGetContactMaxSeparationCore(space);

    /// <summary>Sets the world's maximum normal separation or tangential drift of cached contacts.</summary>
    /// <param name="space">A live scene-owned or server-created space RID.</param>
    /// <param name="value">A finite nonnegative distance with representable squared value and backend conversion.</param>
    /// <remarks>Fresh collision geometry is still computed every interval. This setting controls history reuse,
    /// not query margins or collision-event hysteresis. Changed values wake dynamics; equal writes preserve sleep.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live space.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative, nonfinite or outside its representable range.</exception>
    /// <exception cref="InvalidOperationException">Access violates owner-thread or solver-phase rules.</exception>
    public static void SpaceSetContactMaxSeparation(RID space, float value) => Service.SpaceSetContactMaxSeparationCore(space, value);

    /// <summary>Gets the number of contact and joint solver sweeps per simulation substep.</summary>
    /// <param name="space">A live scene-owned or server-created physics space.</param>
    /// <returns>A positive count sampled from project settings when the space was created, initially sixteen.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live space.</exception>
    /// <exception cref="InvalidOperationException">Access violates owner-thread or solver-phase rules.</exception>
    public static int SpaceGetSolverIterations(RID space) => Service.SpaceGetSolverIterationsCore(space);

    /// <summary>Sets the number of contact and joint solver sweeps per simulation substep.</summary>
    /// <param name="space">A live scene-owned or server-created physics space.</param>
    /// <param name="value">A positive number of sweeps. Higher counts increase convergence work and step cost.</param>
    /// <remarks>The setting does not change the number of time substeps, elapsed time, force or correction budgets.
    /// A changed value wakes dynamics and resets quiet timers; an equal value preserves them.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live space.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The count is not positive.</exception>
    /// <exception cref="InvalidOperationException">Access violates owner-thread or solver-phase rules.</exception>
    public static void SpaceSetSolverIterations(RID space, int value) => Service.SpaceSetSolverIterationsCore(space, value);

    /// <summary>Gets the world's outer-tick contact correction fraction.</summary>
    /// <param name="space">A live scene-owned or server-created space RID.</param>
    /// <returns>The current value as a fraction in [0,1], initially sampled from project defaults.</returns>
    /// <exception cref="ArgumentException">The RID is stale or does not identify a space.</exception>
    /// <exception cref="InvalidOperationException">Access violates owner thread or solver phase.</exception>
    public static float SpaceGetContactDefaultBias(RID space) => Service.SpaceGetContactDefaultBiasCore(space);

    /// <summary>Sets the world's outer-tick contact correction fraction.</summary>
    /// <param name="space">A live scene-owned or server-created space RID.</param>
    /// <param name="value">A finite value as a fraction in [0,1], within the backend range.</param>
    /// <remarks>Changed values wake dynamics without replacing shapes, contacts or borrowed views. Equal writes are silent.
    /// Shape.CustomSolverBias overrides the default fraction. Slack affects only positional correction, not collision/query eligibility.
    /// Positional correction is distributed over the tick's actual substep durations.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or does not identify a space.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite, negative or outside its documented range.</exception>
    /// <exception cref="InvalidOperationException">Access violates owner thread or solver phase.</exception>
    public static void SpaceSetContactDefaultBias(RID space, float value) => Service.SpaceSetContactDefaultBiasCore(space, value);

    /// <summary>Gets the world's contact penetration slack.</summary>
    /// <param name="space">A live scene-owned or server-created space RID.</param>
    /// <returns>The current value as a nonnegative scene-unit distance, initially sampled from project defaults.</returns>
    /// <exception cref="ArgumentException">The RID is stale or does not identify a space.</exception>
    /// <exception cref="InvalidOperationException">Access violates owner thread or solver phase.</exception>
    public static float SpaceGetContactMaxAllowedPenetration(RID space) => Service.SpaceGetContactMaxAllowedPenetrationCore(space);

    /// <summary>Sets the world's contact penetration slack.</summary>
    /// <param name="space">A live scene-owned or server-created space RID.</param>
    /// <param name="value">A finite value as a nonnegative scene-unit distance, within the backend range.</param>
    /// <remarks>Changed values wake dynamics without replacing shapes, contacts or borrowed views. Equal writes are silent.
    /// Shape.CustomSolverBias overrides the default fraction. Slack affects only positional correction, not collision/query eligibility.
    /// Positional correction is distributed over the tick's actual substep durations.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or does not identify a space.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite, negative or outside its documented range.</exception>
    /// <exception cref="InvalidOperationException">Access violates owner thread or solver phase.</exception>
    public static void SpaceSetContactMaxAllowedPenetration(RID space, float value) => Service.SpaceSetContactMaxAllowedPenetrationCore(space, value);

}
