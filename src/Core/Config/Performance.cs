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
        PhysicsIslandCount = 19
    }

    /// <summary>Reads the latest published value of a runtime monitor.</summary>
    /// <param name="monitor">An implemented monitor selector.</param>
    /// <returns>A current object population, frame rate, duration in seconds, canvas submission count or physical count selected by the monitor.</returns>
    /// <remarks>Safe on any thread. Does not synchronize a device, invoke callbacks or allocate.
    /// Object selectors scan weak registrations or the path cache on demand, without forcing collection or taking
    /// scene ownership. They include queued and cancellation-pending objects until disposal commits; unreachable
    /// undisposed objects disappear after collection. Orphan counts remain available in Release builds. Concurrent
    /// construction, collection and membership changes can affect a scan; several reads are not one atomic census.
    /// Construction allocates the weak identity and amortized registry storage. No device is opened to count objects.
    /// Timing durations publish maxima once per completed unscaled Engine host-second window. Successful
    /// attachment resets samples; stop and failed frames retain completed values. Owned Window presentation
    /// contributes to process timing, while independent direct hosts do not publish Engine timing windows.
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
        Monitor.ObjectCount or Monitor.ObjectNodeCount or Monitor.ObjectOrphanNodeCount => ObjectCensus.Count(monitor),
        Monitor.ObjectResourceCount => Resource.CountCachedResources(),
        Monitor.RenderTotalObjectsInFrame => RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalObjectsInFrame),
        Monitor.RenderTotalPrimitivesInFrame => RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalPrimitivesInFrame),
        Monitor.RenderTotalDrawCallsInFrame => RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame),
        Monitor.PhysicsActiveObjects => PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.ActiveObjects),
        Monitor.PhysicsCollisionPairs => PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.CollisionPairs),
        Monitor.PhysicsIslandCount => PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.IslandCount),
        _ => throw new ArgumentOutOfRangeException(nameof(monitor))
    };

    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("The shared performance service cannot be disposed by a consumer.");
}
