namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Assigns a body's exact canvas association for point queries.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="id">CanvasLayer instance ID, or zero for the default canvas.</param>
    /// <remarks>The numeric association borrows no object and does not change pose or collisions.
    /// Scene canvas entry and exit replace it with the scene's current association.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Access violates the scene/world owner or solver boundary.</exception>
    public static void BodyAttachCanvasInstanceID(RID body, ulong id) => Service.ColliderBackend(body, false).CanvasInstanceID = id;

    /// <summary>Gets a body's retained canvas association for point queries.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>The assigned ID; zero denotes the default canvas.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Access violates the scene/world owner or solver boundary.</exception>
    public static ulong BodyGetCanvasInstanceID(RID body) => Service.ColliderBackend(body, false).CanvasInstanceID;

    /// <summary>Assigns an Area's exact canvas association for point queries.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="id">CanvasLayer instance ID, or zero for the default canvas.</param>
    /// <remarks>The numeric association borrows no object and does not change pose or overlaps.
    /// Scene canvas entry and exit replace it with the scene's current association.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live Area.</exception>
    /// <exception cref="InvalidOperationException">Access violates the scene/world owner or solver boundary.</exception>
    public static void AreaAttachCanvasInstanceID(RID area, ulong id) => Service.ColliderBackend(area, true).CanvasInstanceID = id;

    /// <summary>Gets an Area's retained canvas association for point queries.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <returns>The assigned ID; zero denotes the default canvas.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live Area.</exception>
    /// <exception cref="InvalidOperationException">Access violates the scene/world owner or solver boundary.</exception>
    public static ulong AreaGetCanvasInstanceID(RID area) => Service.ColliderBackend(area, true).CanvasInstanceID;

    private PhysicsColliderBackend ColliderBackend(RID rid, bool area)
    {
        ThrowIfDisposed();
        CollisionObject? scene; PhysicsServerCollider? server;
        if (area) (scene, server) = ResolveAreaOwners(rid);
        else (scene, server) = ResolveBodyOwners(rid);
        scene?.Tree?.EnsureOwnerThread();
        var backend = scene?.Backend ?? server!.Backend;
        backend.Space?.EnsureQueryAccess();
        return backend;
    }
}
