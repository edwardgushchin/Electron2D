namespace Electron2D;

/// <summary>Combines stable canvas, shared physics space and navigation map identities of a two-dimensional world.</summary>
/// <remarks>Viewports may share a world or select independent worlds. Physics storage is created on first use;
/// resources and scene ownership retain it. Resource duplicates borrow the same runtime identities and never copy solver state.
/// Navigation map ownership shares the same runtime lifetime; topology synchronization remains a separate boundary.</remarks>
public sealed class World : Resource
{
    private WorldRuntime _runtime;
    private bool _released;
    /// <summary>Creates a caller-owned world with an independent logical canvas and lazy physics space.</summary>
    public World() : this(new WorldRuntime(sceneOwned: false)) { }
    internal World(WorldRuntime runtime) { _runtime = runtime; runtime.Retain(); }
    internal WorldRuntime Runtime => _runtime;
    /// <summary>Gets this world's stable borrowed canvas identity, independent of native startup.</summary>
    /// <value>The canvas used by every viewport selecting this world.</value>
    /// <exception cref="ArgumentException">The scene-owned world expired.</exception>
    /// <exception cref="ObjectDisposedException">This wrapper is disposed.</exception>
    public RID Canvas { get { ThrowIfDisposed(); _runtime.EnsureAlive(); return _runtime.Canvas.RID; } }
    /// <summary>Gets the stable borrowed identity of the world's live physics space.</summary>
    /// <value>Created on first access; shared with scene bodies and direct query views.</value>
    /// <exception cref="ArgumentException">The scene-owned world expired.</exception>
    /// <exception cref="ObjectDisposedException">This wrapper is disposed.</exception>
    public RID Space { get { ThrowIfDisposed(); return _runtime.SpaceRID; } }
    /// <summary>Gets the cached direct-query view of this world's physics space.</summary>
    /// <value>The view shares the solver state and obeys its owner thread and query boundaries.</value>
    /// <exception cref="ArgumentException">The scene-owned world expired.</exception>
    /// <exception cref="ObjectDisposedException">This wrapper is disposed.</exception>
    public PhysicsDirectSpaceState DirectSpaceState { get { ThrowIfDisposed(); return PhysicsServer.SpaceGetDirectState(_runtime.SpaceRID); } }
    /// <summary>Gets this world's stable borrowed active navigation-map identity.</summary>
    /// <remarks>Allocated on first access and shared by every wrapper/view selecting this runtime.</remarks>
    /// <value>The real active map shared by all viewports selecting this world.</value>
    /// <exception cref="ArgumentException">The scene-owned world expired.</exception>
    /// <exception cref="ObjectDisposedException">This wrapper is disposed.</exception>
    public RID NavigationMap { get { ThrowIfDisposed(); return _runtime.NavigationMap; } }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new World(_runtime);
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var world = (World)target; if (ReferenceEquals(world._runtime, _runtime)) return;
        if (world._runtime.SceneOwner is not null) throw new InvalidOperationException("Bound world identity cannot be replaced through resource copying; assign Viewport.World instead.");
        world._runtime.ValidateRelease(); _runtime.Retain(); world._runtime.Release(); world._runtime = _runtime;
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); _runtime.ValidateRelease(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing && !_released) { _released = true; _runtime.Release(); } base.Dispose(disposing); }
}
