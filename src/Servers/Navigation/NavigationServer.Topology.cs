namespace Electron2D;

public sealed partial class NavigationServer
{
    /// <summary>Selects one counter from the latest successfully synchronized active-map snapshot.</summary>
    public enum ProcessInfo
    {
        /// <summary>Number of active maps.</summary>
        ActiveMaps = 0,
        /// <summary>Number of regions belonging to active maps, including disabled regions.</summary>
        RegionCount = 1,
        /// <summary>Number of agents belonging to active maps.</summary>
        AgentCount = 2,
        /// <summary>Number of links belonging to active maps, including unattached or disabled links.</summary>
        LinkCount = 3,
        /// <summary>Number of authored region polygons; synthetic link polygons are excluded.</summary>
        PolygonCount = 4,
        /// <summary>Sum of distinct raster edge keys within each region.</summary>
        EdgeCount = 5,
        /// <summary>Number of paired raster edges within regions.</summary>
        EdgeMergeCount = 6,
        /// <summary>Interregion raster pairs plus directed margin connections.</summary>
        EdgeConnectionCount = 7,
        /// <summary>Unpaired external edges eligible for margin connections, before margin matching.</summary>
        EdgeFreeCount = 8,
        /// <summary>Number of obstacles belonging to active maps.</summary>
        ObstacleCount = 9
    }
    private readonly int[] _processInfo = new int[10];
    /// <summary>Returns a counter from the last complete synchronization; staged changes do not affect this snapshot.</summary>
    /// <param name="processInfo">A defined counter identity.</param><returns>The committed count, initially zero.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The counter identity is unknown.</exception>
    public static int GetProcessInfo(ProcessInfo processInfo)
    { if ((uint)processInfo >= 10) throw new ArgumentOutOfRangeException(nameof(processInfo)); lock (Shared._gate) return Shared._processInfo[(int)processInfo]; }
    /// <summary>Stages the raster cell size, clamping finite values to at least 0.0001 world units.</summary>
    /// <param name="map">A live map RID.</param><param name="cellSize">Finite cell size; one initially.</param>
    /// <exception cref="ArgumentException">The RID is invalid or the value is nonfinite.</exception>
    public static void MapSetCellSize(RID map, float cellSize)
    { if (!float.IsFinite(cellSize)) throw new ArgumentOutOfRangeException(nameof(cellSize)); cellSize = Math.Max(cellSize, .0001f); lock (Shared._gate) { var m = Shared.Map(map); if (m.CellSize == cellSize) return; m.CellSize = cellSize; Shared.RasterChanged(m); } }
    /// <summary>Returns the staged raster cell size.</summary><param name="map">A live map RID.</param><returns>One initially.</returns>
    public static float MapGetCellSize(RID map) { lock (Shared._gate) return Shared.Map(map).CellSize; }
    /// <summary>Stages the raster scale, clamping finite values to 0.0001 through 0.1.</summary>
    /// <param name="map">A live map RID.</param><param name="scale">Finite raster scale; 0.1 initially.</param>
    /// <remarks>The effective square cell dimension is CellSize multiplied by this scale.</remarks>
    /// <exception cref="ArgumentException">The RID is invalid or the value is nonfinite.</exception>
    public static void MapSetMergeRasterizerCellScale(RID map, float scale)
    { if (!float.IsFinite(scale)) throw new ArgumentOutOfRangeException(nameof(scale)); scale = Math.Clamp(scale, .0001f, .1f); lock (Shared._gate) { var m = Shared.Map(map); if (m.RasterScale == scale) return; m.RasterScale = scale; Shared.RasterChanged(m); } }
    /// <summary>Returns the staged raster scale.</summary><param name="map">A live map RID.</param><returns>0.1 initially.</returns>
    public static float MapGetMergeRasterizerCellScale(RID map) { lock (Shared._gate) return Shared.Map(map).RasterScale; }
    private void RasterChanged(NavigationMapState map) { map.Dirty = true; foreach (var region in _regions.Values) if (region.Map == map.RID) region.Dirty = true; }
    /// <summary>Returns the region's last published nonzero iteration identity, or zero before its first publication.</summary>
    /// <param name="region">A live region RID.</param><returns>A 32-bit wrapping identity projected to ulong.</returns>
    public static ulong RegionGetIterationID(RID region) { lock (Shared._gate) return Shared.Region(region).IterationID; }
    private NavigationMapIteration RegionMapIteration(NavigationRegionState region) => _maps.TryGetValue(region.PublishedMap, out var map) ? map.Iteration : NavigationMapIteration.Empty;
    /// <summary>Returns the region's committed directed free-edge margin pathway count.</summary>
    /// <param name="region">A live region RID.</param><returns>Zero for detached regions or absent pathways; raster pairs and links are excluded.</returns>
    public static int RegionGetConnectionsCount(RID region) { lock (Shared._gate) return Shared.RegionMapIteration(Shared.Region(region)).ConnectionCount(region); }
    /// <summary>Returns the first endpoint of one published margin pathway.</summary>
    /// <param name="region">A live region RID.</param><param name="connection">A valid zero-based pathway index.</param><returns>A world-space endpoint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index does not identify a published pathway.</exception>
    public static Vector2 RegionGetConnectionPathwayStart(RID region, int connection) { lock (Shared._gate) return Shared.RegionMapIteration(Shared.Region(region)).Connection(region, connection).Start; }
    /// <summary>Returns the second endpoint of one published margin pathway.</summary>
    /// <param name="region">A live region RID.</param><param name="connection">A valid zero-based pathway index.</param><returns>A world-space endpoint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index does not identify a published pathway.</exception>
    public static Vector2 RegionGetConnectionPathwayEnd(RID region, int connection) { lock (Shared._gate) return Shared.RegionMapIteration(Shared.Region(region)).Connection(region, connection).End; }
    private void ReadProcessInfo(Span<int> counts, List<(NavigationMapState Map, NavigationMapIteration Iteration)>? pending)
    {
        counts.Clear();
        foreach (var map in _maps.Values)
        {
            if (!map.Active) continue; counts[0]++;
            var iteration = map.Iteration; if (pending is not null) foreach (var next in pending) if (next.Map == map) { iteration = next.Iteration; break; }
            var profile = iteration.Profile;
            counts[4] = checked(counts[4] + profile.Polygons); counts[5] = checked(counts[5] + profile.Edges); counts[6] = checked(counts[6] + profile.Merges); counts[7] = checked(counts[7] + profile.Connections); counts[8] = checked(counts[8] + profile.FreeEdges);
        }
        foreach (var value in _regions.Values) if (_maps.TryGetValue(value.Map, out var map) && map.Active) counts[1]++;
        foreach (var value in _agents.Values) if (_maps.TryGetValue(value.Map, out var map) && map.Active) counts[2]++;
        foreach (var value in _links.Values) if (_maps.TryGetValue(value.Map, out var map) && map.Active) counts[3]++;
        foreach (var value in _obstacles.Values) if (_maps.TryGetValue(value.Map, out var map) && map.Active) counts[9]++;
    }
}
