namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Selects the physics implementation independently of rendering.</summary>
    public enum Backend
    {
        /// <summary>Managed CPU physics; does not require a graphics device.</summary>
        CPU,
        /// <summary>Independent physics with resident state and compute execution on the GPU.</summary>
        GPU,
        /// <summary>A world implementation supplied through the typed extension contract.</summary>
        Extension
    }

    /// <summary>Creates an inactive physics space using an explicitly selected implementation.</summary>
    /// <param name="backend">Requested CPU or GPU implementation, independently of the renderer.</param>
    /// <param name="allowCPUFallback">Allows CPU startup when the requested GPU cannot initialize. False by default.</param>
    /// <returns>A caller-owned RID; activate with SpaceSetActive and release with FreeRID.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The backend is not a selectable built-in implementation.</exception>
    /// <exception cref="InvalidOperationException">GPU initialization fails and fallback is disabled.</exception>
    /// <exception cref="PlatformNotSupportedException">The project requests an unavailable separate numerical thread.</exception>
    /// <remarks>Fallback occurs only during creation. A failed GPU step never replays on CPU.
    /// Captures the feature-aware separate numerical thread setting for this space; publication and callbacks retain the owner.</remarks>
    public static RID SpaceCreate(Backend backend, bool allowCPUFallback = false) => Service.SpaceCreateCore(backend, allowCPUFallback);

    /// <summary>Creates an inactive space using a captured named registration.</summary>
    /// <param name="backendName">A registered name, CPU, GPU or DEFAULT.</param>
    /// <param name="allowCPUFallback">Allows startup fallback only for built-in GPU.</param>
    /// <returns>A caller-owned space RID released with FreeRID.</returns>
    /// <exception cref="ArgumentException">The registration is unknown or empty.</exception>
    /// <exception cref="PlatformNotSupportedException">The project requests an unavailable separate numerical thread.</exception>
    /// <remarks>Captures the feature-aware separate numerical thread setting; later edits affect new spaces only.</remarks>
    public static RID SpaceCreate(string backendName, bool allowCPUFallback = false)
    {
        var selection = Service.BackendRegistry.Resolve(backendName);
        return Service.CreateRegisteredSpace(selection.Registration.BuiltIn ?? Backend.CPU, allowCPUFallback, true, selection: selection).RID;
    }
    /// <summary>Gets the original named selection of a live space.</summary>
    /// <param name="space">Live scene or caller-owned space RID.</param>
    /// <returns>The requested name, including DEFAULT when supplied.</returns>
    public static string SpaceGetRequestedBackendName(RID space) => Service.GetSceneSpace(space).RequestedBackendName;
    /// <summary>Gets the actual implementation registration used by a live space.</summary>
    /// <param name="space">Live scene or caller-owned space RID.</param>
    /// <returns>The captured registration name or CPU after permitted startup fallback.</returns>
    public static string SpaceGetBackendName(RID space) => Service.GetSceneSpace(space).ActualBackendName;

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
