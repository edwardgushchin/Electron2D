using System.Runtime.ExceptionServices;

namespace Electron2D;

public sealed partial class NavigationServer
{
    internal sealed class IterationWork(NavigationRegionState? region, NavigationMapState? map, NavigationRegionState[] regions, NavigationLinkState[] links)
    {
        internal readonly NavigationRegionState? Region = region;
        internal readonly NavigationMapState? Map = map;
        internal readonly NavigationRegionState[] Regions = regions;
        internal readonly NavigationLinkState[] Links = links;
        internal long TaskID;
        internal NavigationMapIteration? Result;
        internal ExceptionDispatchInfo? Failure;
        internal void Run()
        {
            try { Result = Region is { } region ? NavigationMapIteration.BuildRegion(region) : NavigationMapIteration.Build(Map!, Regions, [], Links); }
            catch (Exception error) { Failure = ExceptionDispatchInfo.Capture(error); }
        }
    }
    /// <summary>Returns whether automatic map topology rebuilds run on workers.</summary>
    /// <param name="map">Live navigation map RID.</param><returns>The selected policy; true initially on threaded native runtimes.</returns>
    public static bool MapGetUseAsyncIterations(RID map) { lock (Shared._gate) return Shared.Map(map).UseAsyncIterations; }
    /// <summary>Selects worker or scene-lane computation for following automatic map iterations.</summary>
    /// <param name="map">Live navigation map RID.</param><param name="enabled">Whether topology builds use workers.</param>
    /// <remarks>In-flight work remains owned and is retired. Explicit Synchronize retains its complete deterministic host boundary.</remarks>
    public static void MapSetUseAsyncIterations(RID map, bool enabled)
    { lock (Shared._gate) { var value = Shared.Map(map); CheckIterationPlatform(enabled); value.UseAsyncIterations = enabled; } }
    /// <summary>Returns whether automatic region geometry rebuilds run on workers.</summary>
    /// <param name="region">Live navigation region RID.</param><returns>The selected policy; true initially on threaded native runtimes.</returns>
    public static bool RegionGetUseAsyncIterations(RID region) { lock (Shared._gate) return Shared.Region(region).UseAsyncIterations; }
    /// <summary>Selects worker or scene-lane computation for following automatic region geometry iterations.</summary>
    /// <param name="region">Live navigation region RID.</param><param name="enabled">Whether geometry builds use workers.</param>
    /// <remarks>Queries retain completed geometry until owner synchronization publishes a complete replacement.</remarks>
    public static void RegionSetUseAsyncIterations(RID region, bool enabled)
    { lock (Shared._gate) { var value = Shared.Region(region); CheckIterationPlatform(enabled); value.UseAsyncIterations = enabled; } }
    private static void CheckIterationPlatform(bool enabled) { if (enabled && OperatingSystem.IsBrowser()) throw new PlatformNotSupportedException("Navigation worker iterations require a threaded browser runtime."); }
    private static NavigationRegionState CaptureRegion(NavigationRegionState region) => new(region.RID, null)
    {
        Map = region.Map,
        Geometry = region.Geometry,
        Transform = region.Transform,
        Enabled = region.Enabled,
        UseEdgeConnections = region.UseEdgeConnections,
        Layers = region.Layers,
        EnterCost = region.EnterCost,
        TravelCost = region.TravelCost,
        OwnerID = region.OwnerID,
        Iteration = region.Iteration
    };
    private static NavigationMapState CaptureMap(NavigationMapState map) => new(map.RID, null)
    { Active = map.Active, UseEdgeConnections = map.UseEdgeConnections, EdgeMargin = map.EdgeMargin, LinkRadius = map.LinkRadius, CellSize = map.CellSize, RasterScale = map.RasterScale };
    private static NavigationLinkState CaptureLink(NavigationLinkState link) => new(link.RID, null)
    { Map = link.Map, Enabled = link.Enabled, Bidirectional = link.Bidirectional, Start = link.Start, End = link.End, Layers = link.Layers, EnterCost = link.EnterCost, TravelCost = link.TravelCost, OwnerID = link.OwnerID };
    private static bool Matches(NavigationRegionState value, NavigationRegionState input) => value.Map == input.Map && ReferenceEquals(value.Geometry, input.Geometry) && value.Transform == input.Transform && value.Enabled == input.Enabled && value.UseEdgeConnections == input.UseEdgeConnections && value.Layers == input.Layers && value.EnterCost == input.EnterCost && value.TravelCost == input.TravelCost && value.OwnerID == input.OwnerID;
    private static bool Matches(NavigationLinkState value, NavigationLinkState input) => value.Map == input.Map && value.Enabled == input.Enabled && value.Bidirectional == input.Bidirectional && value.Start == input.Start && value.End == input.End && value.Layers == input.Layers && value.EnterCost == input.EnterCost && value.TravelCost == input.TravelCost && value.OwnerID == input.OwnerID;
    private bool Matches(NavigationMapState map, IterationWork work)
    {
        var input = work.Map!;
        if (map.Active != input.Active || map.UseEdgeConnections != input.UseEdgeConnections || map.EdgeMargin != input.EdgeMargin || map.LinkRadius != input.LinkRadius || map.CellSize != input.CellSize || map.RasterScale != input.RasterScale) return false;
        var regions = 0; var links = 0;
        foreach (var region in _regions.Values) if (region.Map == map.RID) regions++;
        foreach (var link in _links.Values) if (link.Map == map.RID) links++;
        if (regions != work.Regions.Length || links != work.Links.Length) return false;
        foreach (var region in work.Regions) if (!_regions.TryGetValue(region.RID, out var current) || current.Dirty || !Matches(current, region) || !ReferenceEquals(current.Iteration, region.Iteration)) return false;
        foreach (var link in work.Links) if (!_links.TryGetValue(link.RID, out var current) || !Matches(current, link)) return false;
        return true;
    }
    private static void StartIteration(IterationWork work) => work.TaskID = WorkerThreadPool.AddTask(work.Run, highPriority: true, description: "navigation iteration");
    private static void RetireIteration(IterationWork? work) { if (work is null || work.TaskID == 0) return; try { WorkerThreadPool.WaitForNavigationTask(work.TaskID); } finally { work.TaskID = 0; } }
    private void DrainIterationWork()
    {
        foreach (var region in _regions.Values) if (region.Pending is { } work) { try { RetireIteration(work); } finally { region.Pending = null; } }
        foreach (var map in _maps.Values) if (map.Pending is { } work) { try { RetireIteration(work); } finally { map.Pending = null; } }
    }
    private void ValidateRegionCell(NavigationRegionState region)
    { if (region.Geometry.CellSize is { } cell && _maps.TryGetValue(region.Map, out var map) && !Mathf.IsEqualApprox(cell, map.CellSize)) throw new InvalidOperationException("Generated navigation cell size must match its map."); }
    private static void PublishRegion(NavigationRegionState region, NavigationMapIteration iteration)
    { region.Iteration = iteration; region.IterationID = region.IterationID == uint.MaxValue ? 1 : region.IterationID + 1; region.Dirty = false; }
    private List<RID>? SynchronizeIterationWork()
    {
        List<RID>? changed = null;
        foreach (var region in _regions.Values)
        {
            if (region.Pending is { } work)
            {
                if (!WorkerThreadPool.IsTaskCompleted(work.TaskID)) continue;
                try { RetireIteration(work); } finally { region.Pending = null; }
                if (Matches(region, work.Region!)) { work.Failure?.Throw(); ValidateRegionCell(region); PublishRegion(region, work.Result!); }
            }
            if (!region.Dirty) continue; ValidateRegionCell(region);
            if (region.UseAsyncIterations && region.Geometry.Polygons.Length != 0)
            { var prepared = new IterationWork(CaptureRegion(region), null, [], []); StartIteration(prepared); region.Pending = prepared; }
            else PublishRegion(region, NavigationMapIteration.BuildRegion(region));
        }
        List<(NavigationMapState Map, NavigationMapIteration Iteration)>? readyMaps = null;
        foreach (var map in _maps.Values)
        {
            if (map.Pending is { } work)
            {
                if (!WorkerThreadPool.IsTaskCompleted(work.TaskID)) continue;
                try { RetireIteration(work); } finally { map.Pending = null; }
                if (Matches(map, work)) { work.Failure?.Throw(); (readyMaps ??= []).Add((map, work.Result!)); continue; }
            }
            if (!map.Dirty) continue;
            var ready = true; foreach (var region in _regions.Values) if (region.Map == map.RID && region.Dirty) { ready = false; break; }
            if (!ready) continue;
            if (map.UseAsyncIterations && map.Active && HasMapGeometry(map.RID))
            {
                var prepared = CaptureMapWork(map); StartIteration(prepared); map.Pending = prepared;
            }
            else (readyMaps ??= []).Add((map, NavigationMapIteration.Build(map, _regions.Values, [], _links.Values)));
        }
        if (readyMaps is not null) foreach (var (map, iteration) in readyMaps) PublishMap(map, iteration, ref changed);
        Span<int> counters = stackalloc int[10]; ReadProcessInfo(counters, null); counters.CopyTo(_processInfo); return changed;
    }
    private IterationWork CaptureMapWork(NavigationMapState map) => new(null, CaptureMap(map), _regions.Values.Where(r => r.Map == map.RID).Select(CaptureRegion).ToArray(), _links.Values.Where(l => l.Map == map.RID).Select(CaptureLink).ToArray());
    private bool HasMapGeometry(RID map) { foreach (var region in _regions.Values) if (region.Map == map && region.Iteration.Cells.Length != 0) return true; return false; }
    private void PublishMap(NavigationMapState map, NavigationMapIteration iteration, ref List<RID>? changed)
    {
        map.Iteration = iteration; map.IterationID = map.IterationID == ulong.MaxValue ? 1 : map.IterationID + 1; map.Dirty = false; (changed ??= []).Add(map.RID);
        foreach (var region in _regions.Values) if (region.Map == map.RID) region.PublishedMap = map.RID;
        foreach (var link in _links.Values) if (link.Map == map.RID && link.Dirty) { link.IterationID = link.IterationID == uint.MaxValue ? 1 : link.IterationID + 1; link.Dirty = false; }
    }
}
