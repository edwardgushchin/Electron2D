namespace Electron2D;

public sealed partial class NavigationServer
{
    internal Action<Vector2> SceneAgentCallback(RID rid) { lock (_gate) return Agent(rid).SceneCallback; }
    private SceneTree? MapSceneOwner(RID rid)
    {
        var owner = Map(rid).Owner?.SceneOwner;
        foreach (var agent in _agents.Values) if (agent.Map == rid && agent.Scene is not null && agent.Scene.TryGetTarget(out var node) && node.Tree is { } tree) { if (owner is not null && owner != tree) throw new InvalidOperationException("An avoidance map cannot be driven by two scene trees."); owner = tree; }
        foreach (var obstacle in _obstacles.Values) if (obstacle.Map == rid && obstacle.Scene is not null && obstacle.Scene.TryGetTarget(out var node) && node.Tree is { } tree) { if (owner is not null && owner != tree) throw new InvalidOperationException("An avoidance map cannot be driven by two scene trees."); owner = tree; }
        return owner;
    }
    private void ValidateSceneMap(RID rid, SceneTree? tree) { if (tree is not null && MapSceneOwner(rid) is { } owner && owner != tree) throw new InvalidOperationException("The avoidance map belongs to another scene tree."); }
    /// <summary>Synchronizes topology and advances avoidance on caller maps unbound to a SceneTree.</summary>
    /// <param name="delta">Finite nonnegative float-representable duration in seconds. Zero only synchronizes; no callbacks occur.</param>
    /// <remarks>Bound scene maps advance at their own physics boundary. All outputs publish before callbacks;
    /// callbacks may stage future state but recursive stepping or synchronization rejects.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Duration is negative, nonfinite or outside the float timestep range.</exception>
    /// <exception cref="InvalidOperationException">Stepping or synchronization is reentered.</exception>
    /// <exception cref="AggregateException">Observers fail after completed publication.</exception>
    public static void Step(double delta) { Duration(delta); Shared.SynchronizeCore(); Shared.StepAvoidance(delta, null); }
    private static void Duration(double delta) { if (!double.IsFinite(delta) || delta < 0 || delta > float.MaxValue || delta > 0 && delta < float.Epsilon) throw new ArgumentOutOfRangeException(nameof(delta)); }
    internal void StepAvoidance(double delta, SceneTree? tree)
    {
        Duration(delta); if (delta == 0) return;
        lock (_gate)
        {
            if (_stepping || _synchronizing) throw new InvalidOperationException("Navigation stepping cannot be re-entered."); _stepping = true;
            try
            {
                _stepAgents.Clear();
                foreach (var agent in _agents.Values) if (agent.Scene is not null && agent.Scene.TryGetTarget(out var node) && !node.IsDisposed && node.Tree == tree && tree is not null) node.PrepareAvoidance();
                foreach (var obstacle in _obstacles.Values) if (obstacle.Scene is not null && obstacle.Scene.TryGetTarget(out var node) && !node.IsDisposed && node.Tree == tree && tree is not null) node.PrepareAvoidance();
                foreach (var agent in _agents.Values)
                    if (agent.AvoidanceEnabled && !agent.Paused && agent.Map.IsValid() && Map(agent.Map).Active && MapSceneOwner(agent.Map) == tree)
                    { agent.Neighbors.Clear(); agent.Edges.Clear(); agent.Delivery = agent.Callback; agent.DeliveryMap = agent.Map; _stepAgents.Add(agent); }
                // ponytail: quadratic neighbor/contour scans; reuse buffers and add a spatial index only after measured scale demands it.
                foreach (var agent in _stepAgents)
                {
                    foreach (var other in _stepAgents) if (other != agent && other.Map == agent.Map && (agent.AvoidanceMask & other.AvoidanceLayers) != 0 && agent.Priority <= other.Priority)
                            NavigationAvoidance.AddNeighbor(agent, new((agent.SimulationPosition - other.SimulationPosition).LengthSquared, other.SimulationPosition, other.CurrentVelocity, other.Radius, other.RID.GetID()));
                    foreach (var obstacle in _obstacles.Values)
                        if (obstacle.Map == agent.Map && obstacle.AvoidanceEnabled && !obstacle.Paused && (agent.AvoidanceMask & obstacle.AvoidanceLayers) != 0)
                        {
                            if (obstacle.Radius > 0) { var point = new NavigationAvoidance.V(obstacle.Position); NavigationAvoidance.AddNeighbor(agent, new((agent.SimulationPosition - point).LengthSquared, point, new(obstacle.Velocity), obstacle.Radius, obstacle.RID.GetID())); }
                            NavigationAvoidance.AddEdges(agent, obstacle);
                        }
                    agent.SolvedVelocity = NavigationAvoidance.Solve(agent, delta);
                }
                foreach (var agent in _stepAgents) { agent.Velocity = agent.SolvedVelocity; agent.CurrentVelocity = new(agent.SolvedVelocity); agent.SimulationPosition += agent.CurrentVelocity * delta; }
            }
            catch { _stepAgents.Clear(); _stepping = false; throw; }
        }
        List<Exception>? errors = null;
        try
        {
            foreach (var agent in _stepAgents)
            {
                Action<Vector2>? callback;
                lock (_gate) callback = _agents.ContainsKey(agent.RID) && agent.Map == agent.DeliveryMap && agent.AvoidanceEnabled && !agent.Paused && (agent.Scene is null || agent.Scene.TryGetTarget(out var node) && !node.IsDisposed) ? agent.Delivery : null;
                if (callback is not null) foreach (var handler in Delegate.EnumerateInvocationList(callback)) try { handler(agent.SolvedVelocity); } catch (Exception e) { (errors ??= []).Add(e); }
            }
        }
        finally { lock (_gate) { foreach (var agent in _stepAgents) agent.Delivery = null; _stepAgents.Clear(); _stepping = false; } }
        if (errors is not null) throw new AggregateException("Avoidance velocities published with callback failures.", errors);
    }
}
