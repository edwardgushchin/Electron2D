namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Captures a reusable local rewind point for a live scene-owned or server-owned physics space.</summary>
    /// <param name="space">The borrowed local space identity; it is never a portable network identity.</param>
    /// <returns>A caller-owned checkpoint also released when its source world is disposed.</returns>
    /// <remarks>Requires a completed physics interval and scene frame. Both CPU and GPU retain their own
    /// solver history together with scene/server observer state. No simulation tick is advanced.</remarks>
    /// <exception cref="ArgumentException">The identity does not refer to a live physics space.</exception>
    /// <exception cref="InvalidOperationException">The world failed, access is off-owner, or an interval/callback/scene capture is active.</exception>
    public static PhysicsCheckpoint SpaceCreateCheckpoint(RID space) => Service.SpaceCreateCheckpointCore(space);

    /// <summary>Gets the current simulation tick of a live physics space.</summary>
    /// <param name="space">The borrowed local scene-owned or server-owned space identity.</param>
    /// <returns>The number of active positive-duration intervals since creation, or the tick last restored.</returns>
    /// <remarks>Empty intervals advance. Inactive/zero-duration intervals do not. Result-callback failure does
    /// not undo a solved interval. Use a fixed step duration for network ticks; this counter does not enforce one.
    /// Checkpoint restoration rewinds this world's counter without changing scene/global clocks.</remarks>
    /// <exception cref="ArgumentException">The identity does not refer to a live physics space.</exception>
    /// <exception cref="InvalidOperationException">The world failed, access is off-owner, or solving is active.</exception>
    public static ulong SpaceGetTick(RID space) => Service.SpaceGetTickCore(space);

    internal PhysicsCheckpoint SpaceCreateCheckpointCore(RID space)
    {
        ThrowIfDisposed();
        var world = GetSceneSpace(space);
        return new(world);
    }
    internal ulong SpaceGetTickCore(RID space)
    {
        ThrowIfDisposed(); var world = GetSceneSpace(space); world.EnsureQueryAccess(); return world.Tick;
    }
}
