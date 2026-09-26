namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Identifies addition or removal of a logical Area overlap pair.</summary>
    public enum AreaBodyStatus
    {
        /// <summary>A body or Area shape pair entered the receiver.</summary>
        Added = 0,
        /// <summary>A previously reported shape pair left the receiver.</summary>
        Removed = 1
    }

    private readonly Dictionary<RID, PhysicsAreaRuntime> _areaRuntimes = [];

    internal (Area? Scene, PhysicsServerCollider? Server) ResolveAreaOwners(RID rid)
    {
        if (ResolveSceneObject(rid) is Area scene) return (scene, null);
        lock (_registryGate)
            if (_serverColliders.TryGetValue(rid, out var collider) && collider.IsArea) return (null, collider);
        throw new ArgumentException("The RID does not identify a live physics Area.", nameof(rid));
    }

    internal PhysicsAreaRuntime AreaRuntime(RID rid)
    {
        ResolveAreaOwners(rid);
        lock (_registryGate)
        {
            if (!_areaRuntimes.TryGetValue(rid, out var runtime)) _areaRuntimes.Add(rid, runtime = new(rid));
            return runtime;
        }
    }

    internal PhysicsAreaRuntime? FindAreaRuntime(RID rid)
    {
        lock (_registryGate) return _areaRuntimes.GetValueOrDefault(rid);
    }

    /// <summary>Sets or clears the receiver's body-pair overlap callback.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="callback">Status, other RID, object instance ID (zero for server-only), other logical shape and local shape; null clears.</param>
    /// <remarks>Registration resets pending pair history; current overlaps enter at the next nonzero scan.
    /// Scene-owned overlap snapshots/events remain mandatory and independent of this observer.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live Area.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, solver-owned, or receiver configuration is mutated from its own callback.</exception>
    public void AreaSetMonitorCallback(RID area, Action<AreaBodyStatus, RID, ulong, int, int>? callback)
    {
        ThrowIfDisposed(); AreaRuntime(area).SetCallback(callback, false);
    }

    /// <summary>Sets or clears the receiver's monitorable-Area pair callback.</summary>
    /// <param name="area">A live Area RID.</param>
    /// <param name="callback">Status, other RID, object instance ID, other logical shape index and local index; null clears.</param>
    /// <remarks>Replacing either callback resets both pair histories. Only monitorable other Areas enter this lane.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live Area.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, solver-owned, or receiver configuration is mutated from its own callback.</exception>
    public void AreaSetAreaMonitorCallback(RID area, Action<AreaBodyStatus, RID, ulong, int, int>? callback)
    {
        ThrowIfDisposed(); AreaRuntime(area).SetCallback(callback, true);
    }

    /// <summary>Sets the Area's directional body/Area detection mask.</summary>
    /// <param name="area">A live Area RID.</param>
    /// <param name="mask">All accepted category bits; default one.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live Area.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, solver-owned, or receiver configuration is mutated from its own callback.</exception>
    public void AreaSetCollisionMask(RID area, uint mask)
    {
        ThrowIfDisposed(); var runtime = AreaRuntime(area); runtime.EnsureAccess(true);
        var owners = runtime.Owners;
        if (owners.Scene is { } scene) scene.CollisionMask = mask; else owners.Server!.SetFilter(owners.Server.CollisionLayer, mask);
    }

    /// <summary>Gets the Area's 32 collision-layer bits.</summary>
    /// <param name="area">A live Area RID.</param>
    /// <returns>Current categories; default one.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live Area.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, solver-owned, or receiver configuration is mutated from its own callback.</exception>
    public uint AreaGetCollisionLayer(RID area)
    {
        ThrowIfDisposed(); var runtime = AreaRuntime(area); runtime.EnsureAccess(); return runtime.Layer;
    }

    /// <summary>Gets the Area's directional overlap mask.</summary>
    /// <param name="area">A live Area RID.</param>
    /// <returns>Current accepted category bits; default one.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live Area.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, solver-owned, or receiver configuration is mutated from its own callback.</exception>
    public uint AreaGetCollisionMask(RID area)
    {
        ThrowIfDisposed(); var runtime = AreaRuntime(area); runtime.EnsureAccess(); return runtime.Mask;
    }

    /// <summary>Gets the Area's current global scene-unit pose.</summary>
    /// <param name="area">A live Area RID.</param>
    /// <returns>Stored scene or server translation and rotation.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live Area.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, solver-owned, or receiver configuration is mutated from its own callback.</exception>
    public Transform AreaGetTransform(RID area)
    {
        ThrowIfDisposed(); var runtime = AreaRuntime(area); runtime.EnsureAccess(); var owners = runtime.Owners;
        return owners.Scene?.GlobalTransform ?? owners.Server!.GetTransform();
    }
}
