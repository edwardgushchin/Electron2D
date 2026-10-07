namespace Electron2D;

/// <summary>Owns planar navigation maps, region identities and committed authored-polygon pathfinding.</summary>
/// <remarks>Static operations use one retained service. Geometry/configuration edits stage under a service gate;
/// Synchronize and the physics lane publish immutable iterations before MapChanged. Returned paths and RID arrays are copied.</remarks>
public sealed partial class NavigationServer : ElectronObject
{
    private static readonly NavigationServer Shared = new();
    private readonly object _gate = new();
    private readonly Dictionary<RID, NavigationMapState> _maps = [];
    private readonly Dictionary<RID, NavigationRegionState> _regions = [];
    private bool _synchronizing;
    private Action<RID>? _mapChanged;
    private NavigationServer() { }
    internal static NavigationServer Service => Shared;
    /// <summary>Occurs after a complete dirty map iteration is published.</summary>
    /// <remarks>Callbacks run on the synchronizing thread, outside the service gate. Recursive synchronization rejects;
    /// callback edits stage the next iteration. Subscriber failures are aggregated after all deliveries.</remarks>
    public static event Action<RID> MapChanged { add { lock (Shared._gate) Shared._mapChanged += value; } remove { lock (Shared._gate) Shared._mapChanged -= value; } }
    private NavigationMapState Map(RID rid) => _maps.TryGetValue(rid, out var map) ? map : throw new ArgumentException("The RID does not identify a live navigation map.", nameof(rid));
    private NavigationRegionState Region(RID rid) => _regions.TryGetValue(rid, out var region) && (region.Scene is null || region.Scene.TryGetTarget(out var node) && !node.IsDisposed) ? region : throw new ArgumentException("The RID does not identify a live navigation region.", nameof(rid));
    internal RID CreateMap(WorldRuntime? owner = null) { lock (_gate) { var rid = RID.Allocate(); _maps.Add(rid, new(rid, owner)); return rid; } }
    internal RID CreateRegion(NavigationRegion? node = null) { lock (_gate) { var rid = RID.Allocate(); _regions.Add(rid, new(rid, node)); return rid; } }
    internal void ReleaseWorldMap(RID rid) { lock (_gate) { if (!_maps.ContainsKey(rid)) return; foreach (var region in _regions.Values) if (region.Map == rid) region.Map = default; _maps.Remove(rid); } }
    internal void ReleaseSceneRegion(RID rid) { lock (_gate) { if (_regions.Remove(rid, out var region)) { Dirty(region.Map); region.Unsubscribe(); } } }
    private void Dirty(RID rid) { if (_maps.TryGetValue(rid, out var map)) map.Dirty = true; }
    private void SetRegionMap(RID region, RID map) { var value = Region(region); if (map.IsValid()) Map(map); Dirty(value.Map); value.Map = map; Dirty(map); value.Dirty = true; }
    private void SetPolygon(RID rid, NavigationPolygon? polygon)
    {
        var region = Region(rid); var data = polygon?.Snapshot() ?? new NavigationPolygonData([], []); region.Unsubscribe(); region.Polygon = polygon; region.Geometry = data;
        if (polygon is not null) { region.Changed = _ => { lock (_gate) { if (!_regions.ContainsKey(rid)) return; region.Geometry = polygon.Snapshot(); region.Dirty = true; Dirty(region.Map); } if (region.Scene is not null && region.Scene.TryGetTarget(out var node) && !node.IsDisposed) node.NotifyPolygonChanged(); }; region.Disposed = _ => { lock (_gate) { region.Geometry = new([], []); region.Dirty = true; Dirty(region.Map); } }; polygon.Changed += region.Changed; polygon.Disposed += region.Disposed; }
        region.Dirty = true; Dirty(region.Map);
    }
    internal void SynchronizeCore()
    {
        List<RID>? changed = null; Action<RID>? handlers;
        lock (_gate)
        {
            if (_synchronizing) throw new InvalidOperationException("Navigation synchronization cannot be re-entered."); _synchronizing = true;
            try
            {
                // ponytail: sweep weak scene identities at the synchronization boundary; index only if measured host scale requires it.
                List<RID>? expired = null;
                foreach (var region in _regions.Values)
                    if (region.Scene is not null && (!region.Scene.TryGetTarget(out var node) || node.IsDisposed)) (expired ??= []).Add(region.RID);
                if (expired is not null) foreach (var rid in expired) ReleaseSceneRegion(rid);
                var dirty = false;
                foreach (var region in _regions.Values) if (region.Dirty) { dirty = true; break; }
                if (!dirty) foreach (var map in _maps.Values) if (map.Dirty) { dirty = true; break; }
                if (!dirty) { _synchronizing = false; return; }
                var regionIterations = new Dictionary<RID, NavigationMapIteration>();
                foreach (var region in _regions.Values) if (region.Dirty) regionIterations.Add(region.RID, NavigationMapIteration.BuildRegion(region));
                List<(NavigationMapState Map, NavigationMapIteration Iteration)>? pending = null;
                foreach (var map in _maps.Values)
                    if (map.Dirty) (pending ??= []).Add((map, NavigationMapIteration.Build(map, _regions.Values, regionIterations)));
                foreach (var (rid, iteration) in regionIterations) { var region = _regions[rid]; region.Iteration = iteration; region.Dirty = false; }
                // Build every dirty map before publishing any: failed geometry leaves the previous iterations intact.
                if (pending is not null) foreach (var (map, snapshot) in pending)
                    {
                        map.Iteration = snapshot; map.IterationID = map.IterationID == ulong.MaxValue ? 1 : map.IterationID + 1; map.Dirty = false;
                        (changed ??= []).Add(map.RID);
                    }
                handlers = _mapChanged;
            }
            catch { _synchronizing = false; throw; }
        }
        List<Exception>? errors = null;
        try { if (changed is not null && handlers is not null) foreach (var rid in changed) foreach (var handler in Delegate.EnumerateInvocationList(handlers)) try { handler(rid); } catch (Exception e) { (errors ??= []).Add(e); } }
        finally { lock (_gate) _synchronizing = false; }
        if (errors is not null) throw new AggregateException("Navigation iterations committed with callback failures.", errors);
    }
    /// <summary>Commits staged navigation topology through the same kernel used at a physics boundary.</summary>
    /// <remarks>Useful for typed batch hosts without a SceneTree. Deprecated force-update operations are not exposed.</remarks>
    /// <exception cref="InvalidOperationException">Synchronization is re-entered.</exception>
    /// <exception cref="AggregateException">MapChanged observers fail after committed publication.</exception>
    public static void Synchronize() => Shared.SynchronizeCore();
    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("The shared navigation service cannot be disposed by a consumer.");
}
internal sealed class NavigationMapState(RID rid, WorldRuntime? owner)
{
    internal readonly RID RID = rid;
    internal readonly WorldRuntime? Owner = owner;
    internal bool Active, UseEdgeConnections = true, Dirty = true;
    internal float EdgeMargin = 1;
    internal ulong IterationID;
    internal NavigationMapIteration Iteration = NavigationMapIteration.Empty;
}
internal sealed class NavigationRegionState(RID rid, NavigationRegion? scene)
{
    internal readonly RID RID = rid;
    internal readonly WeakReference<NavigationRegion>? Scene = scene is null ? null : new(scene);
    internal RID Map;
    internal bool Enabled = true, UseEdgeConnections = true, Dirty = true;
    internal NavigationMapIteration Iteration = NavigationMapIteration.Empty;
    internal uint Layers = 1;
    internal float EnterCost, TravelCost = 1;
    internal ulong OwnerID;
    internal Transform Transform = Transform.Identity;
    internal NavigationPolygon? Polygon;
    internal NavigationPolygonData Geometry = new([], []);
    internal Action<Resource>? Changed;
    internal Action<ElectronObject>? Disposed;
    internal void Unsubscribe() { if (Polygon is not null) { Polygon.Changed -= Changed; Polygon.Disposed -= Disposed; } Polygon = null; Changed = null; Disposed = null; }
}
