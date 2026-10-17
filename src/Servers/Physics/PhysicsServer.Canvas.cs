namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Assigns a body's exact canvas association for point queries.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="id">CanvasLayer instance ID, or zero for the default canvas.</param>
    /// <remarks>The numeric association borrows no object and does not change pose or collisions.
    /// Scene canvas entry and exit replace it with the scene's current association. A selected publication failure retains committed authoring and object lifetime guards; live query or nonzero-step preparation retries the complete current metadata.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Access violates the scene/world owner or solver boundary.</exception>
    public static void BodyAttachCanvasInstanceID(RID body, ulong id) => Service.ColliderBackend(body, false).CanvasInstanceID = id;

    /// <summary>Gets a body's retained canvas association for point queries.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <returns>The assigned ID; zero denotes the default canvas.</returns>
    /// <remarks>Reads common prepared authoring without a solver query, including from the selected implementation's owner or borrowed numerical hook.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">The attached world has failed or access is outside its owner or borrowed numerical hook.</exception>
    public static ulong BodyGetCanvasInstanceID(RID body) => Service.ColliderBackend(body, false, input: true).CanvasInstanceID;

    /// <summary>Assigns an Area's exact canvas association for point queries.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="id">CanvasLayer instance ID, or zero for the default canvas.</param>
    /// <remarks>The numeric association borrows no object and does not change pose or overlaps.
    /// Scene canvas entry and exit replace it with the scene's current association. A selected publication failure retains committed authoring and object lifetime guards; live query or nonzero-step preparation retries the complete current metadata.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live Area.</exception>
    /// <exception cref="InvalidOperationException">Access violates the scene/world owner or solver boundary.</exception>
    public static void AreaAttachCanvasInstanceID(RID area, ulong id) => Service.ColliderBackend(area, true).CanvasInstanceID = id;

    /// <summary>Gets an Area's retained canvas association for point queries.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <returns>The assigned ID; zero denotes the default canvas.</returns>
    /// <remarks>Reads common prepared authoring without a solver query, including from the selected implementation's owner or borrowed numerical hook.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live Area.</exception>
    /// <exception cref="InvalidOperationException">The attached world has failed or access is outside its owner or borrowed numerical hook.</exception>
    public static ulong AreaGetCanvasInstanceID(RID area) => Service.ColliderBackend(area, true, input: true).CanvasInstanceID;

    private PhysicsColliderBackend ColliderBackend(RID rid, bool area, bool input = false)
    {
        var owners = ShapeOwners(rid, area, input: input);
        return owners.Scene?.Backend ?? owners.Server!.Backend;
    }
}
