namespace Electron2D;

public sealed partial class PhysicsServer
{
    private int _active = 1;
    internal bool IsActive => Volatile.Read(ref _active) != 0;

    /// <summary>Enables or suspends subsequent physics-world steps process-wide.</summary>
    /// <param name="active">True advances locally active worlds; false retains their solver state without simulation.</param>
    /// <remarks>The server starts enabled. This atomic policy can change on any thread and takes effect at each
    /// world's next step boundary. An interval already running completes. Scene callbacks, timers, direct queries
    /// and explicit configuration remain available; skipped time is not accumulated.</remarks>
    public void SetActive(bool active)
    {
        ThrowIfDisposed();
        Volatile.Write(ref _active, active ? 1 : 0);
    }

    /// <summary>Enables or suspends one live scene or caller-owned physics space.</summary>
    /// <param name="space">A live physics space RID.</param>
    /// <param name="active">Whether subsequent nonzero steps may advance this space.</param>
    /// <remarks>SpaceCreate starts inactive; the SceneTree activates its own world. Local policy survives global
    /// suspension. Queries and configuration work while inactive; reactivation consumes no skipped time.</remarks>
    /// <exception cref="ArgumentException">The RID is stale or not a physics space.</exception>
    /// <exception cref="InvalidOperationException">The space is off-owner or currently solving.</exception>
    public void SpaceSetActive(RID space, bool active)
    {
        ThrowIfDisposed();
        GetSceneSpace(space).SetActive(active);
    }

    /// <summary>Returns a physics space's local activation policy.</summary>
    /// <param name="space">A live scene or caller-owned space RID.</param>
    /// <returns>The local flag, independent of the process-wide suspension policy.</returns>
    /// <exception cref="ArgumentException">The RID is stale or not a physics space.</exception>
    /// <exception cref="InvalidOperationException">The space is off-owner or currently solving.</exception>
    public bool SpaceIsActive(RID space)
    {
        ThrowIfDisposed();
        var world = GetSceneSpace(space);
        world.EnsureQueryAccess();
        return world.IsActive;
    }
}
