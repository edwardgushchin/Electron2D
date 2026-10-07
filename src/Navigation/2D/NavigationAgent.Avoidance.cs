namespace Electron2D;

public sealed partial class NavigationAgent
{
    private bool _avoidanceEnabled, _velocitySubmitted, _forcedSubmitted, _deliveringVelocity;
    private Vector2 _velocity, _forced;
    private uint _avoidanceLayers = 1, _avoidanceMask = 1;
    private float _avoidancePriority = 1, _neighborDistance = 500, _radius = 10, _maxSpeed = 100, _timeHorizonAgents = 1, _timeHorizonObstacles;
    private int _maxNeighbors = 10;
    /// <summary>Occurs after the current step publishes all avoidance outputs, before physics simulation.</summary>
    /// <remarks>Handlers may submit next-step velocity and move the parent. Every handler is attempted;
    /// exceptions aggregate after delivery. Disposal during this event rejects.</remarks>
    public event Action<Vector2>? VelocityComputed;
    /// <summary>Gets or changes whether reciprocal avoidance participates.</summary>
    /// <value>False initially; enabling registers typed velocity delivery.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Source mutation is off-owner or during path navigation delivery.</exception>
    public bool AvoidanceEnabled
    {
        get { Read(); return _avoidanceEnabled; }
        set { EnsureMutable(); NavigationServer.AgentSetAvoidanceEnabled(_rid, value); NavigationServer.AgentSetAvoidanceCallback(_rid, value ? NavigationServer.Service.SceneAgentCallback(_rid) : null); _avoidanceEnabled = value; }
    }
    /// <summary>Gets or submits finite desired velocity for the next avoidance boundary of an active target request.</summary>
    /// <value>Zero initially; retained wanted velocity, independent of computed safe velocity.</value>
    /// <exception cref="ArgumentException">Velocity is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Source mutation is unavailable.</exception>
    public Vector2 Velocity { get { Read(); return _velocity; } set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("Velocity must be finite.", nameof(value)); _velocity = value; _velocitySubmitted = true; } }
    /// <summary>Submits a finite simulation velocity replacement for the next active-request boundary after teleporting.</summary>
    /// <param name="velocity">Forced world velocity; desired velocity is retained separately.</param>
    /// <exception cref="ArgumentException">Velocity is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Source mutation is unavailable.</exception>
    public void SetVelocityForced(Vector2 velocity) { EnsureMutable(); if (!velocity.IsFinite()) throw new ArgumentException("Velocity must be finite.", nameof(velocity)); _forced = velocity; _forcedSubmitted = true; }
    /// <summary>Returns an avoidance layer bit using one-based indices.</summary>
    /// <param name="layerNumber">Index one through thirty-two.</param><returns>Whether the bit is set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside one through thirty-two.</exception>
    public bool GetAvoidanceLayerValue(int layerNumber) { Layer(layerNumber); return (AvoidanceLayers & (1u << (layerNumber - 1))) != 0; }
    /// <summary>Changes an avoidance layer bit using one-based indices.</summary>
    /// <param name="layerNumber">Index one through thirty-two.</param><param name="value">New bit state.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside one through thirty-two.</exception>
    public void SetAvoidanceLayerValue(int layerNumber, bool value) { Layer(layerNumber); var bit = 1u << (layerNumber - 1); AvoidanceLayers = value ? AvoidanceLayers | bit : AvoidanceLayers & ~bit; }
    /// <summary>Returns an avoidance mask bit using one-based indices.</summary>
    /// <param name="layerNumber">Index one through thirty-two.</param><returns>Whether the bit is set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside one through thirty-two.</exception>
    public bool GetAvoidanceMaskValue(int layerNumber) { Layer(layerNumber); return (AvoidanceMask & (1u << (layerNumber - 1))) != 0; }
    /// <summary>Changes an avoidance mask bit using one-based indices.</summary>
    /// <param name="layerNumber">Index one through thirty-two.</param><param name="value">New bit state.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside one through thirty-two.</exception>
    public void SetAvoidanceMaskValue(int layerNumber, bool value) { Layer(layerNumber); var bit = 1u << (layerNumber - 1); AvoidanceMask = value ? AvoidanceMask | bit : AvoidanceMask & ~bit; }
    /// <summary>Gets or changes 32-bit layers visible to other masks.</summary>
    /// <value>Default 1u; retained node source independent of direct server edits.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Source mutation is unavailable.</exception>
    public uint AvoidanceLayers { get { Read(); return _avoidanceLayers; } set { EnsureMutable(); NavigationServer.AgentSetAvoidanceLayers(_rid, value); _avoidanceLayers = value; } }
    /// <summary>Gets or changes 32-bit mask selecting other participants.</summary>
    /// <value>Default 1u; retained node source independent of direct server edits.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Source mutation is unavailable.</exception>
    public uint AvoidanceMask { get { Read(); return _avoidanceMask; } set { EnsureMutable(); NavigationServer.AgentSetAvoidanceMask(_rid, value); _avoidanceMask = value; } }
    /// <summary>Gets or changes priority in the interval zero through one.</summary>
    /// <value>Default 1f; retained node source independent of direct server edits.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Source mutation is unavailable.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is outside its finite range.</exception>
    public float AvoidancePriority { get { Read(); return _avoidancePriority; } set { EnsureMutable(); NavigationServer.AgentSetAvoidancePriority(_rid, value); _avoidancePriority = value; } }
    /// <summary>Gets or changes finite nonnegative neighbor search radius.</summary>
    /// <value>Default 500f; retained node source independent of direct server edits.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Source mutation is unavailable.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is outside its finite range.</exception>
    public float NeighborDistance { get { Read(); return _neighborDistance; } set { EnsureMutable(); NavigationServer.AgentSetNeighborDistance(_rid, value); _neighborDistance = value; } }
    /// <summary>Gets or changes maximum selected neighbors; nonpositive disables neighbor selection.</summary>
    /// <value>Default 10; retained node source independent of direct server edits.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Source mutation is unavailable.</exception>
    public int MaxNeighbors { get { Read(); return _maxNeighbors; } set { EnsureMutable(); NavigationServer.AgentSetMaxNeighbors(_rid, value); _maxNeighbors = value; } }
    /// <summary>Gets or changes finite nonnegative avoidance disc radius.</summary>
    /// <value>Default 10f; retained node source independent of direct server edits.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Source mutation is unavailable.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is outside its finite range.</exception>
    public float Radius { get { Read(); return _radius; } set { EnsureMutable(); NavigationServer.AgentSetRadius(_rid, value); _radius = value; } }
    /// <summary>Gets or changes finite nonnegative output speed cap.</summary>
    /// <value>Default 100f; retained node source independent of direct server edits.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Source mutation is unavailable.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is outside its finite range.</exception>
    public float MaxSpeed { get { Read(); return _maxSpeed; } set { EnsureMutable(); NavigationServer.AgentSetMaxSpeed(_rid, value); _maxSpeed = value; } }
    /// <summary>Gets or changes finite nonnegative agent prediction horizon.</summary>
    /// <value>Default 1f; retained node source independent of direct server edits.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Source mutation is unavailable.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is outside its finite range.</exception>
    public float TimeHorizonAgents { get { Read(); return _timeHorizonAgents; } set { EnsureMutable(); NavigationServer.AgentSetTimeHorizonAgents(_rid, value); _timeHorizonAgents = value; } }
    /// <summary>Gets or changes finite nonnegative contour prediction horizon.</summary>
    /// <value>Default 0f; retained node source independent of direct server edits.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Source mutation is unavailable.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is outside its finite range.</exception>
    public float TimeHorizonObstacles { get { Read(); return _timeHorizonObstacles; } set { EnsureMutable(); NavigationServer.AgentSetTimeHorizonObstacles(_rid, value); _timeHorizonObstacles = value; } }
    internal void PrepareAvoidance()
    {
        if (Parent is not Entity parent || !IsInsideTree) return;
        NavigationServer.AgentSetPaused(_rid, !parent.CanProcess()); if (!_avoidanceEnabled) return;
        NavigationServer.AgentSetPosition(_rid, parent.GlobalPosition);
        if (_submitted)
        {
            if (_velocitySubmitted) { NavigationServer.AgentSetVelocity(_rid, _velocity); _velocitySubmitted = false; }
            if (_forcedSubmitted) { NavigationServer.AgentSetVelocityForced(_rid, _forced); _forcedSubmitted = false; }
        }
    }
    internal void DeliverVelocity(Vector2 velocity)
    {
        Read(); _deliveringVelocity = true; List<Exception>? errors = null;
        try { if (VelocityComputed is { } handlers) foreach (var handler in Delegate.EnumerateInvocationList(handlers)) try { handler(velocity); } catch (Exception e) { (errors ??= []).Add(e); } }
        finally { _deliveringVelocity = false; }
        if (errors is not null) throw new AggregateException("Velocity delivery completed with observer failures.", errors);
    }
    private void StopAvoidance()
    { if (_avoidanceEnabled) { if (Parent is Entity parent) NavigationServer.AgentSetPosition(_rid, parent.GlobalPosition); NavigationServer.AgentSetVelocity(_rid, Vector2.Zero); NavigationServer.AgentSetVelocityForced(_rid, Vector2.Zero); _velocitySubmitted = _forcedSubmitted = false; } }
    private static readonly PropertyDescriptor[] AvoidanceProperties =
    [
        new PropertyDescriptor<NavigationAgent,uint>(nameof(AvoidanceLayers),n=>n.AvoidanceLayers,(n,v)=>n.AvoidanceLayers=v,_=>1u,stored:true),
        new PropertyDescriptor<NavigationAgent,uint>(nameof(AvoidanceMask),n=>n.AvoidanceMask,(n,v)=>n.AvoidanceMask=v,_=>1u,stored:true),
        new PropertyDescriptor<NavigationAgent,float>(nameof(AvoidancePriority),n=>n.AvoidancePriority,(n,v)=>n.AvoidancePriority=v,_=>1f,stored:true),
        new PropertyDescriptor<NavigationAgent,float>(nameof(NeighborDistance),n=>n.NeighborDistance,(n,v)=>n.NeighborDistance=v,_=>500f,stored:true),
        new PropertyDescriptor<NavigationAgent,int>(nameof(MaxNeighbors),n=>n.MaxNeighbors,(n,v)=>n.MaxNeighbors=v,_=>10,stored:true),
        new PropertyDescriptor<NavigationAgent,float>(nameof(Radius),n=>n.Radius,(n,v)=>n.Radius=v,_=>10f,stored:true),
        new PropertyDescriptor<NavigationAgent,float>(nameof(MaxSpeed),n=>n.MaxSpeed,(n,v)=>n.MaxSpeed=v,_=>100f,stored:true),
        new PropertyDescriptor<NavigationAgent,float>(nameof(TimeHorizonAgents),n=>n.TimeHorizonAgents,(n,v)=>n.TimeHorizonAgents=v,_=>1f,stored:true),
        new PropertyDescriptor<NavigationAgent,float>(nameof(TimeHorizonObstacles),n=>n.TimeHorizonObstacles,(n,v)=>n.TimeHorizonObstacles=v,_=>0f,stored:true),
        new PropertyDescriptor<NavigationAgent,bool>(nameof(AvoidanceEnabled),n=>n.AvoidanceEnabled,(n,v)=>n.AvoidanceEnabled=v,_=>false,stored:true),
        new PropertyDescriptor<NavigationAgent,Vector2>(nameof(Velocity),n=>n.Velocity,(n,v)=>n.Velocity=v,_=>Vector2.Zero,stored:true),
    ];
}
