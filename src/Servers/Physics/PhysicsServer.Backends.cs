namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Selects the physics implementation independently of rendering.</summary>
    public enum Backend
    {
        /// <summary>Managed CPU physics; does not require a graphics device.</summary>
        CPU,
        /// <summary>Independent physics with resident state and compute execution on the GPU.</summary>
        GPU
    }

    /// <summary>Creates an inactive physics space using an explicitly selected implementation.</summary>
    /// <param name="backend">Requested CPU or GPU implementation, independently of the renderer.</param>
    /// <param name="allowCPUFallback">Allows CPU startup when the requested GPU cannot initialize. False by default.</param>
    /// <returns>A caller-owned RID; activate with SpaceSetActive and release with FreeRID.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The backend value is unknown.</exception>
    /// <exception cref="InvalidOperationException">GPU initialization fails and fallback is disabled.</exception>
    /// <remarks>Fallback occurs only during creation. A failed GPU step never replays on CPU.</remarks>
    public static RID SpaceCreate(Backend backend, bool allowCPUFallback = false) => Service.SpaceCreateCore(backend, allowCPUFallback);

    /// <summary>Gets the implementation requested when the space was created.</summary>
    /// <param name="space">A live scene or explicitly created space RID.</param>
    /// <returns>The original requested implementation, including after startup fallback.</returns>
    /// <exception cref="ArgumentException">The RID is not a live space.</exception>
    public static Backend SpaceGetRequestedBackend(RID space) => Service.GetSceneSpace(space).RequestedBackend;

    /// <summary>Gets the implementation actually used by a space.</summary>
    /// <param name="space">A live scene or explicitly created space RID.</param>
    /// <returns>The selected implementation, including CPU after allowed startup fallback.</returns>
    /// <exception cref="ArgumentException">The RID is not a live space.</exception>
    public static Backend SpaceGetBackend(RID space) => Service.GetSceneSpace(space).ActualBackend;

    /// <summary>Gets the startup GPU failure that caused an allowed CPU fallback.</summary>
    /// <param name="space">A live scene or explicitly created space RID.</param>
    /// <returns>A diagnostic message, or null when no fallback occurred.</returns>
    /// <exception cref="ArgumentException">The RID is not a live space.</exception>
    public static string? SpaceGetBackendFallbackReason(RID space) => Service.GetSceneSpace(space).BackendFallbackReason;
}
