namespace Electron2D;

public sealed partial class PhysicsServer
{
    private int _active = 1;
    internal bool IsActive => Volatile.Read(ref _active) != 0;

    internal void SetActiveCore(bool active)
    {
        ThrowIfDisposed();
        Volatile.Write(ref _active, active ? 1 : 0);
    }

    internal void SpaceSetActiveCore(RID space, bool active)
    {
        ThrowIfDisposed();
        GetSceneSpace(space).SetActive(active);
    }

    internal bool SpaceIsActiveCore(RID space)
    {
        ThrowIfDisposed();
        var world = GetSceneSpace(space);
        world.EnsureQueryAccess();
        return world.IsActive;
    }
}
