namespace Electron2D;

internal sealed class WorldRuntime
{
    private readonly object _gate = new();
    private int _resources;
    private bool _alive = true;
    internal readonly bool SceneOwned;
    internal SceneTree? SceneOwner { get; private set; }
    internal PhysicsSpace? ExistingSpace { get; private set; }
    private RID _spaceRID, _navigationMap;
    internal RID NavigationMap { get { lock (_gate) { EnsureAlive(); if (!_navigationMap.IsValid()) { _navigationMap = NavigationServer.Service.CreateMap(this); NavigationServer.MapSetActive(_navigationMap, true); } return _navigationMap; } } }
    internal readonly RenderingCanvasRuntime Canvas;
    internal bool Alive => Volatile.Read(ref _alive);
    internal PhysicsServer.Backend RequestedBackend { get; }
    internal string RequestedBackendName => _backendSelection?.RequestedName ?? RequestedBackend.ToString();
    private readonly bool _allowCPUFallback;
    private readonly bool _separateSolverThread;
    private readonly Func<PhysicsSpace, PhysicsWorldBackend>? _backendFactory;
    private readonly PhysicsBackendRegistry.Selection? _backendSelection;
    private bool _creatingSpace;
    internal static WorldRuntime CreateSceneDefault() => new(sceneOwned: true,
        allowCPUFallback: ProjectSettings.GetWithOverride(ProjectSettings.Physics2DAllowCPUFallback),
        selection: PhysicsServer.Service.BackendRegistry.Resolve(ProjectSettings.GetWithOverride(ProjectSettings.Physics2DEngine)));
    internal WorldRuntime(bool sceneOwned, PhysicsServer.Backend backend = PhysicsServer.Backend.CPU, bool allowCPUFallback = false,
        Func<PhysicsSpace, PhysicsWorldBackend>? factory = null, PhysicsBackendRegistry.Selection? selection = null)
    {
        if (backend is not (PhysicsServer.Backend.CPU or PhysicsServer.Backend.GPU)) throw new ArgumentOutOfRangeException(nameof(backend));
        if (factory is not null && selection is not null) throw new ArgumentException("A world cannot select two physics factories.");
        _separateSolverThread = ProjectSettings.GetWithOverride(ProjectSettings.Physics2DRunOnSeparateThread);
        if (_separateSolverThread && OperatingSystem.IsBrowser()) throw new PlatformNotSupportedException("This host cannot run physics on a separate thread.");
        RequestedBackend = selection is { } named ? named.Registration.BuiltIn ?? PhysicsServer.Backend.Extension : backend; _allowCPUFallback = allowCPUFallback; _backendFactory = factory; _backendSelection = selection;
        SceneOwned = sceneOwned; Canvas = RenderingCanvasRegistry.Register(world: this);
    }
    internal void Retain() { lock (_gate) { EnsureAlive(); _resources++; } }
    internal void EnsureAlive() { if (!Alive) throw new ArgumentException("The world identity has expired."); }
    internal void ValidateOwner(SceneTree tree)
    {
        lock (_gate) { EnsureAlive(); if (SceneOwner is not null && !ReferenceEquals(SceneOwner, tree)) throw new InvalidOperationException("A world cannot be driven by two scene trees."); ExistingSpace?.EnsureWorldBindingChange(); }
    }
    internal bool Bind(SceneTree tree)
    {
        lock (_gate) { EnsureAlive(); if (ReferenceEquals(SceneOwner, tree)) return false; ValidateOwner(tree); SceneOwner = tree; return true; }
    }
    internal RID SpaceRID
    {
        get
        {
            lock (_gate)
            {
                EnsureAlive();
                if (ExistingSpace is not null)
                {
                    PhysicsServer.Service.GetSceneSpace(_spaceRID); return _spaceRID;
                }
                if (_creatingSpace) throw new InvalidOperationException("A world cannot recursively create its physics space.");
                _creatingSpace = true;
            }
            PhysicsSpace? created = null;
            try
            {
                created = PhysicsServer.Service.CreateRegisteredSpace(_backendSelection is { } named ? named.Registration.BuiltIn ?? PhysicsServer.Backend.CPU : RequestedBackend, _allowCPUFallback, owned: false, factory: _backendFactory, selection: _backendSelection, separateSolverThread: _separateSolverThread);
                lock (_gate) { EnsureAlive(); _spaceRID = created.RID; ExistingSpace = created; }
                return created.RID;
            }
            catch (Exception error)
            {
                if (created is not null)
                {
                    try { created.Dispose(); }
                    catch (Exception cleanup) { throw new AggregateException("World physics publication and cleanup failed.", error, cleanup); }
                    finally { PhysicsServer.Service.UnregisterSceneSpace(created.RID); }
                }
                throw;
            }
            finally { lock (_gate) _creatingSpace = false; }
        }
    }
    internal PhysicsSpace Space { get { _ = SpaceRID; return ExistingSpace!; } }
    internal void ValidateRelease()
    {
        lock (_gate)
            if (_alive && SceneOwner is null && _resources == 1)
            {
                if (_creatingSpace) throw new InvalidOperationException("A world cannot be released while its physics implementation is being created.");
                ExistingSpace?.EnsureWorldRelease();
            }
    }
    internal void Release()
    {
        lock (_gate) { if (--_resources == 0 && SceneOwner is null) Destroy(); }
    }
    internal void Detach(SceneTree tree)
    {
        lock (_gate) { if (!ReferenceEquals(SceneOwner, tree)) return; SceneOwner = null; if (SceneOwned || _resources == 0) Destroy(); }
    }
    private void Destroy()
    {
        if (!_alive) return;
        try { ExistingSpace?.Dispose(); }
        finally { PhysicsServer.Service.UnregisterSceneSpace(_spaceRID); if (_navigationMap.IsValid()) NavigationServer.Service.ReleaseWorldMap(_navigationMap); _navigationMap = default; ExistingSpace = null; _spaceRID = default; RenderingCanvasRegistry.Remove(Canvas.RID); _alive = false; }
    }
}
