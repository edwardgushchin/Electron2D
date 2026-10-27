namespace Electron2D;

/// <summary>Reads completed engine diagnostics and invokes typed application monitors.</summary>
public sealed partial class Performance : ElectronObject
{
    private static readonly Performance SharedInstance = new();
    private Performance() { }
    internal static Performance Service => SharedInstance;

    /// <summary>Selects an implemented runtime diagnostic producer.</summary>
    public enum Monitor
    {
        /// <summary>Completed Engine frames per unscaled host second, updated once per sampling window.</summary>
        TimeFPS = 0,
        /// <summary>Largest completed process-frame duration in the last sampling window, in seconds, including owned presentation.</summary>
        TimeProcess = 1,
        /// <summary>Largest completed individual physics-step duration in the last sampling window, in seconds.</summary>
        TimePhysicsProcess = 2,
        /// <summary>Largest navigation synchronization and avoidance duration within one physics step in the last sampling window, in seconds.</summary>
        TimeNavigationProcess = 3,
        /// <summary>Process-lifetime peak reserved SceneTree deferred queue arrays and typed invocation storage, in managed bytes.</summary>
        MemoryMessageBufferMax = 6,
        /// <summary>Weakly reachable engine objects whose disposal has not committed.</summary>
        ObjectCount = 7,
        /// <summary>Live resources registered in the process-wide path cache; uncached resources are excluded.</summary>
        ObjectResourceCount = 8,
        /// <summary>Live nodes inside the currently attached Engine SceneTree, including its root.</summary>
        ObjectNodeCount = 9,
        /// <summary>Live nodes outside the attached Engine SceneTree, including nodes of independent trees.</summary>
        ObjectOrphanNodeCount = 10,
        /// <summary>Submitted logical canvas drawing records in the last completed renderer frame.</summary>
        RenderTotalObjectsInFrame = 11,
        /// <summary>Submitted canvas primitive/instance budgets in that frame.</summary>
        RenderTotalPrimitivesInFrame = 12,
        /// <summary>Canvas geometry draw calls issued by the native backend in that frame.</summary>
        RenderTotalDrawCallsInFrame = 13,
        /// <summary>Awake nonstatic bodies in active physics spaces.</summary>
        PhysicsActiveObjects = 17,
        /// <summary>Backend collision candidates, including sensors.</summary>
        PhysicsCollisionPairs = 18,
        /// <summary>Active dynamic constraint islands.</summary>
        PhysicsIslandCount = 19,
        /// <summary>Last valid native output-buffer latency in seconds; zero without active audio output.</summary>
        AudioOutputLatency = 23,
        /// <summary>Committed active navigation maps across the engine's navigation domains.</summary>
        NavigationActiveMaps = 24,
        /// <summary>Committed regions belonging to active navigation maps.</summary>
        NavigationRegionCount = 25,
        /// <summary>Committed agents belonging to active navigation maps.</summary>
        NavigationAgentCount = 26,
        /// <summary>Committed links belonging to active navigation maps.</summary>
        NavigationLinkCount = 27,
        /// <summary>Committed authored navigation polygons, excluding synthetic link polygons.</summary>
        NavigationPolygonCount = 28,
        /// <summary>Committed distinct raster edge keys within navigation regions.</summary>
        NavigationEdgeCount = 29,
        /// <summary>Committed paired raster edges within navigation regions.</summary>
        NavigationEdgeMergeCount = 30,
        /// <summary>Committed interregion raster pairs and directed margin connections.</summary>
        NavigationEdgeConnectionCount = 31,
        /// <summary>Committed external navigation edges eligible for margin matching.</summary>
        NavigationEdgeFreeCount = 32,
        /// <summary>Committed obstacles belonging to active navigation maps.</summary>
        NavigationObstacleCount = 33,
        /// <summary>Committed active planar navigation maps.</summary>
        Navigation2DActiveMaps = 39,
        /// <summary>Committed regions belonging to active planar maps.</summary>
        Navigation2DRegionCount = 40,
        /// <summary>Committed agents belonging to active planar maps.</summary>
        Navigation2DAgentCount = 41,
        /// <summary>Committed links belonging to active planar maps.</summary>
        Navigation2DLinkCount = 42,
        /// <summary>Committed authored planar polygons, excluding synthetic link polygons.</summary>
        Navigation2DPolygonCount = 43,
        /// <summary>Committed distinct raster edge keys within planar regions.</summary>
        Navigation2DEdgeCount = 44,
        /// <summary>Committed paired raster edges within planar regions.</summary>
        Navigation2DEdgeMergeCount = 45,
        /// <summary>Committed planar interregion raster pairs and directed margin connections.</summary>
        Navigation2DEdgeConnectionCount = 46,
        /// <summary>Committed external planar edges eligible for margin matching.</summary>
        Navigation2DEdgeFreeCount = 47,
        /// <summary>Committed obstacles belonging to active planar maps.</summary>
        Navigation2DObstacleCount = 48
    }

    /// <summary>Reads the latest published value of a runtime monitor.</summary>
    /// <param name="monitor">An implemented monitor selector.</param>
    /// <returns>The selected population, byte peak, frame rate, duration in seconds or submission/physical count.</returns>
    /// <remarks>Safe on any thread. Does not synchronize a device, invoke callbacks or allocate.
    /// Object selectors scan weak registrations or the path cache on demand, without forcing collection or taking
    /// scene ownership. They include queued and cancellation-pending objects until disposal commits; unreachable
    /// undisposed objects disappear after collection. Orphan counts remain available in Release builds. Concurrent
    /// construction, collection and membership changes can affect a scan; several reads are not one atomic census.
    /// Construction allocates the weak identity and amortized registry storage. No device is opened to count objects.
    /// Timing durations publish maxima once per completed unscaled Engine host-second window. Successful
    /// attachment resets samples; stop and failed frames retain completed values. Owned Window presentation
    /// contributes to process timing, while independent direct hosts do not publish Engine timing windows.
    /// Navigation counts read the last successful NavigationServer synchronization, including disabled members of active
    /// maps. Aggregate and planar selectors share that snapshot in this planar engine. Reads never synchronize navigation.
    /// Audio reads the active output's last valid latency without initializing the service/device or calling its owner.
    /// It returns zero before output initialization, during offline recording and after output closes. Concurrent output
    /// replacement/retirement may yield the prior snapshot; it never accesses a retired native handle.
    /// Deferred memory counts retained queue backing arrays and engine-owned typed invocation objects/delegates across
    /// live trees, including independent trees and processing groups. It retains the process peak after drain or teardown.
    /// Growth measures actual managed allocations; caller callbacks, referenced payload graphs and pool bookkeeping are
    /// excluded. Unreachable or disposed trees do not contribute to subsequent peaks; reads never force collection.
    /// Canvas counts publish after completed native rendering and follow RenderingServer.GetRenderingInfo, including
    /// GPU-authored capacity budgets and zero without an active renderer. Several reads do not form an atomic snapshot. Durations measure owner-side wall time, not device timestamps.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The monitor is undefined.</exception>
    /// <exception cref="OverflowException">The aggregate exceeds the supported signed count range.</exception>
    public static double GetMonitor(Monitor monitor) => Service.GetMonitorCore(monitor);

    private double GetMonitorCore(Monitor monitor) => monitor switch
    {
        Monitor.TimeFPS => Engine.FramesPerSecond,
        Monitor.TimeProcess => AtomicFloatingPoint.Read(ref _processTime),
        Monitor.TimePhysicsProcess => AtomicFloatingPoint.Read(ref _physicsTime),
        Monitor.TimeNavigationProcess => AtomicFloatingPoint.Read(ref _navigationTime),
        Monitor.MemoryMessageBufferMax => ObjectCensus.DeferredStoragePeak,
        Monitor.ObjectCount or Monitor.ObjectNodeCount or Monitor.ObjectOrphanNodeCount => ObjectCensus.Count(monitor),
        Monitor.ObjectResourceCount => Resource.CountCachedResources(),
        Monitor.RenderTotalObjectsInFrame => RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalObjectsInFrame),
        Monitor.RenderTotalPrimitivesInFrame => RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalPrimitivesInFrame),
        Monitor.RenderTotalDrawCallsInFrame => RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame),
        Monitor.PhysicsActiveObjects => PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.ActiveObjects),
        Monitor.PhysicsCollisionPairs => PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.CollisionPairs),
        Monitor.PhysicsIslandCount => PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.IslandCount),
        Monitor.AudioOutputLatency => AudioServer.OutputLatencySnapshot,
        Monitor.NavigationActiveMaps or Monitor.Navigation2DActiveMaps => NavigationServer.GetProcessInfo(NavigationServer.ProcessInfo.ActiveMaps),
        Monitor.NavigationRegionCount or Monitor.Navigation2DRegionCount => NavigationServer.GetProcessInfo(NavigationServer.ProcessInfo.RegionCount),
        Monitor.NavigationAgentCount or Monitor.Navigation2DAgentCount => NavigationServer.GetProcessInfo(NavigationServer.ProcessInfo.AgentCount),
        Monitor.NavigationLinkCount or Monitor.Navigation2DLinkCount => NavigationServer.GetProcessInfo(NavigationServer.ProcessInfo.LinkCount),
        Monitor.NavigationPolygonCount or Monitor.Navigation2DPolygonCount => NavigationServer.GetProcessInfo(NavigationServer.ProcessInfo.PolygonCount),
        Monitor.NavigationEdgeCount or Monitor.Navigation2DEdgeCount => NavigationServer.GetProcessInfo(NavigationServer.ProcessInfo.EdgeCount),
        Monitor.NavigationEdgeMergeCount or Monitor.Navigation2DEdgeMergeCount => NavigationServer.GetProcessInfo(NavigationServer.ProcessInfo.EdgeMergeCount),
        Monitor.NavigationEdgeConnectionCount or Monitor.Navigation2DEdgeConnectionCount => NavigationServer.GetProcessInfo(NavigationServer.ProcessInfo.EdgeConnectionCount),
        Monitor.NavigationEdgeFreeCount or Monitor.Navigation2DEdgeFreeCount => NavigationServer.GetProcessInfo(NavigationServer.ProcessInfo.EdgeFreeCount),
        Monitor.NavigationObstacleCount or Monitor.Navigation2DObstacleCount => NavigationServer.GetProcessInfo(NavigationServer.ProcessInfo.ObstacleCount),
        _ => throw new ArgumentOutOfRangeException(nameof(monitor))
    };

    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("The shared performance service cannot be disposed by a consumer.");
}
