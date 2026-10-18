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
        /// <summary>Awake nonstatic bodies in active physics spaces.</summary>
        PhysicsActiveObjects = 17,
        /// <summary>Backend collision candidates, including sensors.</summary>
        PhysicsCollisionPairs = 18,
        /// <summary>Active dynamic constraint islands.</summary>
        PhysicsIslandCount = 19
    }

    /// <summary>Reads the latest published value of a runtime monitor.</summary>
    /// <param name="monitor">An implemented monitor selector.</param>
    /// <returns>A frame rate, duration in seconds, or physical count selected by the monitor.</returns>
    /// <remarks>Safe on any thread. Does not synchronize a device, invoke callbacks or allocate.
    /// Timing durations publish maxima once per completed unscaled Engine host-second window. Successful
    /// attachment resets samples; stop and failed frames retain completed values. Owned Window presentation
    /// contributes to process timing, while independent direct hosts do not publish Engine timing windows.
    /// Several reads do not form an atomic snapshot. Durations measure owner-side wall time, not device timestamps.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The monitor is undefined.</exception>
    /// <exception cref="OverflowException">The aggregate exceeds the supported signed count range.</exception>
    public static double GetMonitor(Monitor monitor) => Service.GetMonitorCore(monitor);

    private double GetMonitorCore(Monitor monitor) => monitor switch
    {
        Monitor.TimeFPS => Engine.FramesPerSecond,
        Monitor.TimeProcess => AtomicFloatingPoint.Read(ref _processTime),
        Monitor.TimePhysicsProcess => AtomicFloatingPoint.Read(ref _physicsTime),
        Monitor.TimeNavigationProcess => AtomicFloatingPoint.Read(ref _navigationTime),
        Monitor.PhysicsActiveObjects => PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.ActiveObjects),
        Monitor.PhysicsCollisionPairs => PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.CollisionPairs),
        Monitor.PhysicsIslandCount => PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.IslandCount),
        _ => throw new ArgumentOutOfRangeException(nameof(monitor))
    };

    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("The shared performance service cannot be disposed by a consumer.");
}
