namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Tests whether a live physics space is delivering completed physics notifications.</summary>
    /// <param name="space">The live scene-owned or explicitly created physics space.</param>
    /// <returns>True during body force/integration/state and sleep callbacks, selected query flushing,
    /// and contact/Area notification delivery; false when this world has no active delivery phase.</returns>
    /// <remarks>The flag is scoped to the addressed world and restored after callback errors. This lifecycle
    /// read allocates nothing, observes no numerical state and remains available during terminal-error cleanup.
    /// Nested queries preserve an ongoing delivery phase; another world has independent status.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live physics space.</exception>
    /// <exception cref="InvalidOperationException">The caller is off the space owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The physics service or addressed space is disposed.</exception>
    public static bool SpaceIsFlushingQueries(RID space) => Service.GetSceneSpace(space).IsFlushingQueries;
}
