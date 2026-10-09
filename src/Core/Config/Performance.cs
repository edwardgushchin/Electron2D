namespace Electron2D;

/// <summary>Provides completed runtime diagnostic samples without advancing their producers.</summary>
public sealed class Performance : ElectronObject
{
    private static readonly Performance SharedInstance = new();
    private Performance() { }
    internal static Performance Service => SharedInstance;

    /// <summary>Selects an implemented runtime diagnostic producer.</summary>
    public enum Monitor
    {
        /// <summary>Awake nonstatic bodies in active physics spaces.</summary>
        PhysicsActiveObjects = 17,
        /// <summary>Backend collision candidates, including sensors.</summary>
        PhysicsCollisionPairs = 18,
        /// <summary>Active dynamic constraint islands.</summary>
        PhysicsIslandCount = 19
    }

    /// <summary>Reads the latest published value of a runtime monitor.</summary>
    /// <param name="monitor">An implemented monitor selector.</param>
    /// <returns>The corresponding physical count as a double-precision number.</returns>
    /// <remarks>Safe on any thread. Does not synchronize a device, invoke callbacks or allocate.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The monitor is undefined.</exception>
    /// <exception cref="OverflowException">The aggregate exceeds the supported signed count range.</exception>
    public static double GetMonitor(Monitor monitor) => Service.GetMonitorCore(monitor);

    private double GetMonitorCore(Monitor monitor) => PhysicsServer.GetProcessInfo(monitor switch
    {
        Monitor.PhysicsActiveObjects => PhysicsServer.ProcessInfo.ActiveObjects,
        Monitor.PhysicsCollisionPairs => PhysicsServer.ProcessInfo.CollisionPairs,
        Monitor.PhysicsIslandCount => PhysicsServer.ProcessInfo.IslandCount,
        _ => throw new ArgumentOutOfRangeException(nameof(monitor))
    });

    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("The shared performance service cannot be disposed by a consumer.");
}
