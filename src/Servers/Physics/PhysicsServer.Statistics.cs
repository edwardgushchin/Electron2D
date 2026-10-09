namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Selects a count from the latest completed steps of active physics spaces.</summary>
    public enum ProcessInfo
    {
        /// <summary>Awake nonstatic bodies, including kinematic bodies.</summary>
        ActiveObjects = 0,
        /// <summary>Backend collision candidates, including sensor pairs; not contact points or events.</summary>
        CollisionPairs = 1,
        /// <summary>Active dynamic constraint groups containing contacts or joints.</summary>
        IslandCount = 2
    }

    /// <summary>Reads aggregated completed-step statistics from active registered spaces.</summary>
    /// <param name="processInfo">The requested physical count.</param>
    /// <returns>The sum of each currently active world's latest published sample.</returns>
    /// <remarks>Safe on any thread. Reads do not step, synchronize devices or allocate.
    /// Zero-time and globally suspended steps retain samples. Local inactive spaces are excluded.
    /// Candidate counts and constraint grouping reflect each backend's actual work.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selector is undefined.</exception>
    /// <exception cref="OverflowException">The aggregate exceeds the supported signed count range.</exception>
    public static int GetProcessInfo(ProcessInfo processInfo) => Service.GetProcessInfoCore(processInfo);

    private int GetProcessInfoCore(ProcessInfo processInfo)
    {
        if (!Enum.IsDefined(processInfo)) throw new ArgumentOutOfRangeException(nameof(processInfo));
        lock (_registryGate)
        {
            var count = 0;
            foreach (var space in _sceneSpaces.Values)
                if (space.IsActive) count = checked(count + (processInfo switch
                {
                    ProcessInfo.ActiveObjects => space.PublishedStatistics.Active,
                    ProcessInfo.CollisionPairs => space.PublishedStatistics.Pairs,
                    _ => space.PublishedStatistics.Islands
                }));
            return count;
        }
    }

    internal void PublishStatistics(PhysicsSpace space, PhysicsSpace.Statistics value)
    {
        lock (_registryGate) space.PublishedStatistics = value;
    }
}
