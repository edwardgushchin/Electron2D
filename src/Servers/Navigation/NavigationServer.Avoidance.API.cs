namespace Electron2D;

public sealed partial class NavigationServer
{
    /// <summary>Returns whether reciprocal avoidance participates for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><returns>Current value, initially false.</returns>
    /// <exception cref="ArgumentException">The agent RID is absent, stale or of another kind.</exception>
    public static bool AgentGetAvoidanceEnabled(RID agent) { lock (Shared._gate) return Shared.Agent(agent).AvoidanceEnabled; }
    /// <summary>Sets whether reciprocal avoidance participates for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><param name="value">New setting; default false.</param>
    /// <exception cref="ArgumentException">The agent RID is invalid or a vector is nonfinite.</exception>
    public static void AgentSetAvoidanceEnabled(RID agent, bool value) { lock (Shared._gate) { var state = Shared.Agent(agent); state.AvoidanceEnabled = value; } }
    /// <summary>Returns 32-bit layers visible to other participant masks for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><returns>Current value, initially 1u.</returns>
    /// <exception cref="ArgumentException">The agent RID is absent, stale or of another kind.</exception>
    public static uint AgentGetAvoidanceLayers(RID agent) { lock (Shared._gate) return Shared.Agent(agent).AvoidanceLayers; }
    /// <summary>Sets 32-bit layers visible to other participant masks for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><param name="value">New setting; default 1u.</param>
    /// <exception cref="ArgumentException">The agent RID is invalid or a vector is nonfinite.</exception>
    public static void AgentSetAvoidanceLayers(RID agent, uint value) { lock (Shared._gate) { var state = Shared.Agent(agent); state.AvoidanceLayers = value; } }
    /// <summary>Returns 32-bit mask selecting other participants for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><returns>Current value, initially 1u.</returns>
    /// <exception cref="ArgumentException">The agent RID is absent, stale or of another kind.</exception>
    public static uint AgentGetAvoidanceMask(RID agent) { lock (Shared._gate) return Shared.Agent(agent).AvoidanceMask; }
    /// <summary>Sets 32-bit mask selecting other participants for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><param name="value">New setting; default 1u.</param>
    /// <exception cref="ArgumentException">The agent RID is invalid or a vector is nonfinite.</exception>
    public static void AgentSetAvoidanceMask(RID agent, uint value) { lock (Shared._gate) { var state = Shared.Agent(agent); state.AvoidanceMask = value; } }
    /// <summary>Returns priority from zero through one; higher priority ignores lower for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><returns>Current value, initially 1f.</returns>
    /// <exception cref="ArgumentException">The agent RID is absent, stale or of another kind.</exception>
    public static float AgentGetAvoidancePriority(RID agent) { lock (Shared._gate) return Shared.Agent(agent).Priority; }
    /// <summary>Sets priority from zero through one; higher priority ignores lower for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><param name="value">New setting; default 1f.</param>
    /// <exception cref="ArgumentException">The agent RID is invalid or a vector is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The setting is outside its finite accepted range.</exception>
    public static void AgentSetAvoidancePriority(RID agent, float value) { Nonnegative(value); if (value > 1) throw new ArgumentOutOfRangeException(nameof(value)); lock (Shared._gate) { var state = Shared.Agent(agent); state.Priority = value; } }
    /// <summary>Returns maximum selected neighbors; nonpositive disables agent neighbors for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><returns>Current value, initially 10.</returns>
    /// <exception cref="ArgumentException">The agent RID is absent, stale or of another kind.</exception>
    public static int AgentGetMaxNeighbors(RID agent) { lock (Shared._gate) return Shared.Agent(agent).MaxNeighbors; }
    /// <summary>Sets maximum selected neighbors; nonpositive disables agent neighbors for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><param name="value">New setting; default 10.</param>
    /// <exception cref="ArgumentException">The agent RID is invalid or a vector is nonfinite.</exception>
    public static void AgentSetMaxNeighbors(RID agent, int value) { lock (Shared._gate) { var state = Shared.Agent(agent); state.MaxNeighbors = value; } }
    /// <summary>Returns finite nonnegative neighbor search radius for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><returns>Current value, initially 500f.</returns>
    /// <exception cref="ArgumentException">The agent RID is absent, stale or of another kind.</exception>
    public static float AgentGetNeighborDistance(RID agent) { lock (Shared._gate) return Shared.Agent(agent).NeighborDistance; }
    /// <summary>Sets finite nonnegative neighbor search radius for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><param name="value">New setting; default 500f.</param>
    /// <exception cref="ArgumentException">The agent RID is invalid or a vector is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The setting is outside its finite accepted range.</exception>
    public static void AgentSetNeighborDistance(RID agent, float value) { Nonnegative(value); lock (Shared._gate) { var state = Shared.Agent(agent); state.NeighborDistance = value; } }
    /// <summary>Returns finite nonnegative avoidance disc radius for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><returns>Current value, initially 10f.</returns>
    /// <exception cref="ArgumentException">The agent RID is absent, stale or of another kind.</exception>
    public static float AgentGetRadius(RID agent) { lock (Shared._gate) return Shared.Agent(agent).Radius; }
    /// <summary>Sets finite nonnegative avoidance disc radius for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><param name="value">New setting; default 10f.</param>
    /// <exception cref="ArgumentException">The agent RID is invalid or a vector is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The setting is outside its finite accepted range.</exception>
    public static void AgentSetRadius(RID agent, float value) { Nonnegative(value); lock (Shared._gate) { var state = Shared.Agent(agent); state.Radius = value; } }
    /// <summary>Returns finite nonnegative output speed cap for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><returns>Current value, initially 100f.</returns>
    /// <exception cref="ArgumentException">The agent RID is absent, stale or of another kind.</exception>
    public static float AgentGetMaxSpeed(RID agent) { lock (Shared._gate) return Shared.Agent(agent).MaxSpeed; }
    /// <summary>Sets finite nonnegative output speed cap for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><param name="value">New setting; default 100f.</param>
    /// <exception cref="ArgumentException">The agent RID is invalid or a vector is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The setting is outside its finite accepted range.</exception>
    public static void AgentSetMaxSpeed(RID agent, float value) { Nonnegative(value); lock (Shared._gate) { var state = Shared.Agent(agent); state.MaxSpeed = value; } }
    /// <summary>Returns finite nonnegative agent prediction horizon for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><returns>Current value, initially 1f.</returns>
    /// <exception cref="ArgumentException">The agent RID is absent, stale or of another kind.</exception>
    public static float AgentGetTimeHorizonAgents(RID agent) { lock (Shared._gate) return Shared.Agent(agent).TimeHorizonAgents; }
    /// <summary>Sets finite nonnegative agent prediction horizon for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><param name="value">New setting; default 1f.</param>
    /// <exception cref="ArgumentException">The agent RID is invalid or a vector is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The setting is outside its finite accepted range.</exception>
    public static void AgentSetTimeHorizonAgents(RID agent, float value) { Nonnegative(value); lock (Shared._gate) { var state = Shared.Agent(agent); state.TimeHorizonAgents = value; } }
    /// <summary>Returns finite nonnegative contour prediction horizon for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><returns>Current value, initially 0f.</returns>
    /// <exception cref="ArgumentException">The agent RID is absent, stale or of another kind.</exception>
    public static float AgentGetTimeHorizonObstacles(RID agent) { lock (Shared._gate) return Shared.Agent(agent).TimeHorizonObstacles; }
    /// <summary>Sets finite nonnegative contour prediction horizon for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><param name="value">New setting; default 0f.</param>
    /// <exception cref="ArgumentException">The agent RID is invalid or a vector is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The setting is outside its finite accepted range.</exception>
    public static void AgentSetTimeHorizonObstacles(RID agent, float value) { Nonnegative(value); lock (Shared._gate) { var state = Shared.Agent(agent); state.TimeHorizonObstacles = value; } }
    /// <summary>Returns whether simulation and callback delivery are paused for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><returns>Current value, initially false.</returns>
    /// <exception cref="ArgumentException">The agent RID is absent, stale or of another kind.</exception>
    public static bool AgentGetPaused(RID agent) { lock (Shared._gate) return Shared.Agent(agent).Paused; }
    /// <summary>Sets whether simulation and callback delivery are paused for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><param name="value">New setting; default false.</param>
    /// <exception cref="ArgumentException">The agent RID is invalid or a vector is nonfinite.</exception>
    public static void AgentSetPaused(RID agent, bool value) { lock (Shared._gate) { var state = Shared.Agent(agent); state.Paused = value; } }
    /// <summary>Returns finite world-space source position for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><returns>Current value, initially Vector2.Zero.</returns>
    /// <exception cref="ArgumentException">The agent RID is absent, stale or of another kind.</exception>
    public static Vector2 AgentGetPosition(RID agent) { lock (Shared._gate) return Shared.Agent(agent).Position; }
    /// <summary>Sets finite world-space source position for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><param name="value">New setting; default Vector2.Zero.</param>
    /// <exception cref="ArgumentException">The agent RID is invalid or a vector is nonfinite.</exception>
    public static void AgentSetPosition(RID agent, Vector2 value) { Finite(value); lock (Shared._gate) { var state = Shared.Agent(agent); state.Position = value; state.SimulationPosition = new(value); } }
    /// <summary>Returns finite desired velocity before stepping or computed velocity after stepping for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><returns>Current value, initially Vector2.Zero.</returns>
    /// <exception cref="ArgumentException">The agent RID is absent, stale or of another kind.</exception>
    public static Vector2 AgentGetVelocity(RID agent) { lock (Shared._gate) return Shared.Agent(agent).Velocity; }
    /// <summary>Sets finite desired velocity before stepping or computed velocity after stepping for the agent.</summary>
    /// <param name="agent">Live agent RID.</param><param name="value">New setting; default Vector2.Zero.</param>
    /// <exception cref="ArgumentException">The agent RID is invalid or a vector is nonfinite.</exception>
    public static void AgentSetVelocity(RID agent, Vector2 value) { Finite(value); lock (Shared._gate) { var state = Shared.Agent(agent); state.Velocity = value; state.PreferredVelocity = value; } }
    /// <summary>Replaces the internal simulation velocity after teleporting without changing the desired velocity.</summary>
    /// <param name="agent">Live agent RID.</param><param name="velocity">Finite forced simulation velocity.</param>
    /// <exception cref="ArgumentException">The RID or vector is invalid.</exception>
    public static void AgentSetVelocityForced(RID agent, Vector2 velocity) { Finite(velocity); lock (Shared._gate) Shared.Agent(agent).CurrentVelocity = new(velocity); }
    /// <summary>Sets or clears typed velocity delivery after committed avoidance publication.</summary>
    /// <param name="agent">Live agent RID.</param><param name="callback">Calling-thread handler or null to clear.</param>
    /// <exception cref="ArgumentException">The agent RID is invalid.</exception>
    public static void AgentSetAvoidanceCallback(RID agent, Action<Vector2>? callback) { lock (Shared._gate) Shared.Agent(agent).Callback = callback; }
    /// <summary>Returns whether the agent has a registered avoidance callback.</summary>
    /// <param name="agent">Live agent RID.</param><returns>True when a typed handler is assigned.</returns>
    /// <exception cref="ArgumentException">The agent RID is invalid.</exception>
    public static bool AgentHasAvoidanceCallback(RID agent) { lock (Shared._gate) return Shared.Agent(agent).Callback is not null; }
}
