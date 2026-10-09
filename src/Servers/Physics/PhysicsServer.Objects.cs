namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Borrows the object instance reported by a body's queries and contacts.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="instance">A live engine object, or null to clear the association.</param>
    /// <remarks>Stores the instance ID and a weak reference. Does not move, wake or recreate the body.
    /// Existing query/contact snapshots retain their sampled association.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="ObjectDisposedException">The supplied instance is disposed.</exception>
    /// <exception cref="InvalidOperationException">Access violates an owner-thread or solver boundary.</exception>
    public static void BodyAttachObject(RID body, ElectronObject? instance) => Service.ColliderBackend(body, false).AttachObject(instance);

    /// <summary>Gets a body's retained object instance ID.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>Zero when unassigned; an assigned ID survives target disposal until explicitly replaced.</returns>
    /// <exception cref="ArgumentException">The RID is not a live body.</exception>
    /// <exception cref="InvalidOperationException">Access violates an owner-thread or solver boundary.</exception>
    public static ulong BodyGetObjectInstanceID(RID body) => Service.ColliderBackend(body, false).ObjectIdentity.ID;

    /// <summary>Borrows the object instance reported for an Area in queries and overlap callbacks.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="instance">A live engine object, or null to clear the association.</param>
    /// <remarks>Stores the instance ID and a weak reference without changing geometry or existing snapshots.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live Area.</exception>
    /// <exception cref="ObjectDisposedException">The supplied instance is disposed.</exception>
    /// <exception cref="InvalidOperationException">Access violates an owner-thread or solver boundary.</exception>
    public static void AreaAttachObject(RID area, ElectronObject? instance) => Service.ColliderBackend(area, true).AttachObject(instance);

    /// <summary>Gets an Area's retained object instance ID.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <returns>Zero when unassigned; an assigned ID survives target disposal until explicitly replaced.</returns>
    /// <exception cref="ArgumentException">The RID is not a live Area.</exception>
    /// <exception cref="InvalidOperationException">Access violates an owner-thread or solver boundary.</exception>
    public static ulong AreaGetObjectInstanceID(RID area) => Service.ColliderBackend(area, true).ObjectIdentity.ID;
}
