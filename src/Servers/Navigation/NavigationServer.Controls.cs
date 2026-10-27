namespace Electron2D;

public sealed partial class NavigationServer
{
    private bool _active = true, _debugEnabled, _debugPending, _debugDispatching;
    private EventPublisher<Action> _navigationDebugChanged, _avoidanceDebugChanged;

    /// <summary>Enables or suspends process-wide reciprocal avoidance simulation.</summary>
    /// <param name="active">True initially; false suppresses following steps and their velocity callbacks.</param>
    /// <remarks>Retains maps, settings, velocities and query topology. Explicit/scene synchronization still commits
    /// staged geometry. An already started step completes its captured outputs and callback delivery.</remarks>
    public static void SetActive(bool active) { lock (Shared._gate) Shared._active = active; }

    /// <summary>Returns process-wide navigation and avoidance drawing enablement.</summary>
    /// <returns>False initially. Per-tree DebugNavigationHint and editor authoring can independently request drawing.</returns>
    public static bool GetDebugEnabled() { lock (Shared._gate) return Shared._debugEnabled; }

    /// <summary>Enables retained scene-region, link, agent-radius and obstacle diagnostics.</summary>
    /// <param name="enabled">New process-wide drawing request, independent of simulation activity.</param>
    /// <remarks>Works in every build configuration. Changes coalesce until the next navigation synchronization
    /// or scene process boundary, which delivers both debug-change events. Explicit agent path drawing is independent.</remarks>
    public static void SetDebugEnabled(bool enabled)
    {
        lock (Shared._gate)
        {
            if (Shared._debugEnabled == enabled) return;
            Shared._debugEnabled = enabled; Shared._debugPending = true;
        }
    }

    /// <summary>Occurs at a navigation or scene process boundary after global debug enablement changes.</summary>
    /// <remarks>Callbacks run outside the service gate on the boundary thread. All listeners of both debug events
    /// are attempted; failures aggregate. Callback edits stage a later notification; recursive synchronization rejects.</remarks>
    public static event Action NavigationDebugChanged { add { lock (Shared._gate) Shared._navigationDebugChanged.Add(value); } remove { lock (Shared._gate) Shared._navigationDebugChanged.Remove(value); } }

    /// <summary>Occurs with NavigationDebugChanged after global debug enablement changes.</summary>
    /// <remarks>Delivery is coalesced, outside the gate and in every build configuration, including a server host.</remarks>
    public static event Action AvoidanceDebugChanged { add { lock (Shared._gate) Shared._avoidanceDebugChanged.Add(value); } remove { lock (Shared._gate) Shared._avoidanceDebugChanged.Remove(value); } }

    /// <summary>Returns a copied snapshot of the map's staged obstacle membership.</summary>
    /// <param name="map">Live map identity.</param>
    /// <returns>Independent identities, including disabled/paused caller obstacles; detached and retired obstacles are absent.</returns>
    /// <exception cref="ArgumentException">The map identity is absent, stale or of another kind.</exception>
    public static RID[] MapGetObstacles(RID map)
    {
        lock (Shared._gate)
        {
            Shared.Map(map);
            return Shared._obstacles.Values.Where(o => o.Map == map && (o.Scene is null || o.Scene.TryGetTarget(out var node) && SceneIdentityAvailable(node))).Select(o => o.RID).ToArray();
        }
    }

    internal void PumpDebugChanges()
    {
        Action? navigation, avoidance;
        lock (_gate)
        {
            if (!_debugPending || _debugDispatching || _synchronizing || _stepping) return;
            _debugPending = false; _debugDispatching = true;
            navigation = _navigationDebugChanged.Handlers; avoidance = _avoidanceDebugChanged.Handlers;
        }
        List<Exception>? errors = null;
        try { Deliver(navigation); Deliver(avoidance); }
        finally { lock (_gate) _debugDispatching = false; }
        if (errors is not null) throw new AggregateException("Navigation debug observers failed after publication.", errors);
        void Deliver(Action? handlers)
        {
            if (handlers is null) return;
            foreach (var handler in Delegate.EnumerateInvocationList(BeginEventEmission(handlers)))
                try { handler(); } catch (Exception error) { (errors ??= []).Add(error); }
        }
    }
}
