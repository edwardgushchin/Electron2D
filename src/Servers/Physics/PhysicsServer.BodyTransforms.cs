namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Copies current poses for a caller-selected body sequence attached to one space.</summary>
    /// <param name="space">The live space shared by every requested scene or server body.</param>
    /// <param name="bodies">Live physical body RIDs; duplicates preserve their input order.</param>
    /// <param name="transforms">Caller-owned destination with capacity for every body; unused elements remain unchanged.</param>
    /// <remarks>Matches the scalar getter's scene presentation and raw solver pose semantics. All identities, membership
    /// and access guards are checked before any destination write. Prepared scratch is retained by the space.
    /// GPU raw poses use one compact read without creating body views or mirroring velocity, fields or sleep state.</remarks>
    /// <exception cref="ArgumentException">A RID is invalid, is not a body in the selected space, or the destination is too small.</exception>
    /// <exception cref="InvalidOperationException">Access violates the owning thread, solver, failed-world or nested batch guard.</exception>
    /// <exception cref="ObjectDisposedException">The owning world has been disposed.</exception>
    public static void BodyGetTransform(RID space, ReadOnlySpan<RID> bodies, Span<Transform> transforms) =>
        Service.BodyGetTransformCore(space, bodies, transforms);

    internal void BodyGetTransformCore(RID space, ReadOnlySpan<RID> bodies, Span<Transform> transforms)
    {
        ThrowIfDisposed();
        if (transforms.Length < bodies.Length) throw new ArgumentException("The transform destination is too small.", nameof(transforms));
        GetSceneSpace(space).ReadBodyTransforms(bodies, transforms);
    }
}
