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

    internal void AreaSetMonitorCallbackCore(RID area, Action<AreaBodyStatus, RID, ulong, int, int>? callback)
    {
        ThrowIfDisposed(); AreaRuntime(area).SetCallback(callback, false);
    }

    internal void AreaSetAreaMonitorCallbackCore(RID area, Action<AreaBodyStatus, RID, ulong, int, int>? callback)
    {
        ThrowIfDisposed(); AreaRuntime(area).SetCallback(callback, true);
    }

    internal void AreaSetCollisionMaskCore(RID area, uint mask)
    {
        ThrowIfDisposed(); var runtime = AreaRuntime(area); runtime.EnsureAccess(true);
        var owners = runtime.Owners;
        if (owners.Scene is { } scene) scene.CollisionMask = mask; else owners.Server!.SetFilter(owners.Server.CollisionLayer, mask);
    }

    internal uint AreaGetCollisionLayerCore(RID area)
    {
        var owners = ShapeOwners(area, isArea: true, input: true);
        return owners.Scene?.CollisionLayer ?? owners.Server!.CollisionLayer;
    }

    internal uint AreaGetCollisionMaskCore(RID area)
    {
        var owners = ShapeOwners(area, isArea: true, input: true);
        return owners.Scene?.CollisionMask ?? owners.Server!.CollisionMask;
    }

    internal Transform AreaGetTransformCore(RID area)
    {
        var owners = ShapeOwners(area, isArea: true, input: true);
        if (owners.Scene is Area scene)
            return scene.Space?.IsNumericalInputThread == true ? scene.PreparedTransform : scene.GlobalTransform;
        return owners.Server!.GetTransform();
    }
}
