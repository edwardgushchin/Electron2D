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
    private readonly bool _allowCPUFallback;
    internal WorldRuntime(bool sceneOwned, PhysicsServer.Backend backend = PhysicsServer.Backend.CPU, bool allowCPUFallback = false)
    {
        if (!Enum.IsDefined(backend)) throw new ArgumentOutOfRangeException(nameof(backend));
        RequestedBackend = backend; _allowCPUFallback = allowCPUFallback;
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
                if (ExistingSpace is null)
                {
                    var space = new PhysicsSpace(RequestedBackend, _allowCPUFallback);
                    try { _spaceRID = PhysicsServer.Service.RegisterSceneSpace(space); ExistingSpace = space; }
                    catch { space.Dispose(); throw; }
                }
                PhysicsServer.Service.GetSceneSpace(_spaceRID); return _spaceRID;
            }
        }
    }
    internal PhysicsSpace Space { get { _ = SpaceRID; return ExistingSpace!; } }
    internal void ValidateRelease() { lock (_gate) { if (_alive && SceneOwner is null && _resources == 1) ExistingSpace?.EnsureWorldRelease(); } }
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
