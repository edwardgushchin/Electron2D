namespace Electron2D;

public sealed partial class NavigationServer
{
    private NavigationAgentState Agent(RID rid) => _agents.TryGetValue(rid, out var agent) && (agent.Scene is null || agent.Scene.TryGetTarget(out var node) && !node.IsDisposed) ? agent : throw new ArgumentException("The RID does not identify a live navigation agent.", nameof(rid));
    internal RID CreateAgent(NavigationAgent? node = null) { lock (_gate) { var rid = RID.Allocate(); _agents.Add(rid, new(rid, node)); return rid; } }
    internal void ReleaseSceneAgent(RID rid) { lock (_gate) _agents.Remove(rid); }
    internal ElectronObject? ResolveNavigationOwner(ulong ownerID)
    {
        if (ownerID == 0) return null;
        lock (_gate)
        {
            // ponytail: scan weak navigation owners on waypoint delivery; index if measured scene scale requires it.
            foreach (var region in _regions.Values) if (region.Scene is not null && region.Scene.TryGetTarget(out var node) && !node.IsDisposed && node.InstanceID == ownerID) return node;
            foreach (var link in _links.Values) if (link.Scene is not null && link.Scene.TryGetTarget(out var node) && !node.IsDisposed && node.InstanceID == ownerID) return node;
            return null;
        }
    }
    /// <summary>Creates a caller-owned navigation agent identity detached from every map.</summary>
    /// <returns>Live agent RID requiring FreeRID.</returns>
    public static RID AgentCreate() => Shared.CreateAgent();
    /// <summary>Assigns an agent to a live map; an empty RID detaches it.</summary>
    /// <param name="agent">Live agent RID.</param><param name="map">Live map RID or empty.</param>
    /// <exception cref="ArgumentException">A required RID is absent, stale or of another kind.</exception>
    public static void AgentSetMap(RID agent, RID map) { lock (Shared._gate) { var value = Shared.Agent(agent); if (map.IsValid()) Shared.Map(map); value.Map = map; } }
    /// <summary>Returns the agent's current map membership.</summary>
    /// <param name="agent">Live agent RID.</param><returns>Assigned map or empty.</returns>
    /// <exception cref="ArgumentException">The agent RID is absent, stale or of another kind.</exception>
    public static RID AgentGetMap(RID agent) { lock (Shared._gate) return Shared.Agent(agent).Map; }
    /// <summary>Returns and consumes whether the assigned map's committed version differs from the last observed version.</summary>
    /// <param name="agent">Live agent RID.</param><returns>False for a detached agent; otherwise whether the iteration changed.</returns>
    /// <exception cref="ArgumentException">The agent RID is absent, stale or of another kind.</exception>
    public static bool AgentIsMapChanged(RID agent)
    {
        lock (Shared._gate) { var value = Shared.Agent(agent); if (!value.Map.IsValid()) return false; var iteration = Shared.Map(value.Map).IterationID; var changed = iteration != value.LastIteration; value.LastIteration = iteration; return changed; }
    }
    /// <summary>Returns copied staged agent memberships, including scene-owned agents.</summary>
    /// <param name="map">Live map RID.</param><returns>Independent agent RID array.</returns>
    /// <exception cref="ArgumentException">The map RID is absent, stale or of another kind.</exception>
    public static RID[] MapGetAgents(RID map) { lock (Shared._gate) { Shared.Map(map); return Shared._agents.Values.Where(a => a.Map == map).Select(a => a.RID).ToArray(); } }
}
internal sealed class NavigationAgentState(RID rid, NavigationAgent? scene)
{
    internal readonly RID RID = rid;
    internal readonly WeakReference<NavigationAgent>? Scene = scene is null ? null : new(scene);
    internal RID Map;
    internal ulong LastIteration;
}
