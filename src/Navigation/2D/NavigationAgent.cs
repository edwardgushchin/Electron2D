namespace Electron2D;

/// <summary>Follows a committed navigation path for its direct Entity parent.</summary>
/// <remarks>Call GetNextPathPosition from the parent's physics processing and move that parent yourself.
/// The agent owns a stable borrowed RID and retained result; getters that advance navigation reject reentry.
/// Events run after each state transition, deliver all subscribers and aggregate failures after the update.</remarks>
public sealed class NavigationAgent : Node
{
    private readonly RID _rid;
    private readonly NavigationPathQueryParameters _query = new();
    private readonly NavigationPathQueryResult _result;
    private NavigationPathQueryData _path = NavigationPathQueryData.Empty;
    private RID _mapOverride;
    private Vector2 _target;
    private bool _submitted, _targetReached, _finished, _lastReached, _updating, _invalidated;
    private int _index;
    private float _pathDesiredDistance = 20, _targetDesiredDistance = 10, _pathMaxDistance = 100;
    /// <summary>Creates a detached agent with default path thresholds and all waypoint metadata enabled.</summary>
    public NavigationAgent() { _result = new(this); _rid = NavigationServer.Service.CreateAgent(this); }
    /// <summary>Occurs after a new path is published, including an empty path.</summary>
    public event Action? PathChanged;
    /// <summary>Occurs once for each reached waypoint, before LinkReached for a link.</summary>
    public event Action<NavigationWaypoint>? WaypointReached;
    /// <summary>Occurs after WaypointReached for a waypoint whose selected type metadata identifies a link.</summary>
    public event Action<NavigationWaypoint>? LinkReached;
    /// <summary>Occurs when the parent is strictly within TargetDesiredDistance of the requested target.</summary>
    public event Action? TargetReached;
    /// <summary>Occurs after reaching the target or the final waypoint of an unreachable target.</summary>
    public event Action? NavigationFinished;
    /// <summary>Gets or submits a finite world-space target; even equal assignments request a new path.</summary>
    /// <value>Zero initially; assignment resets completion, result and index.</value>
    /// <exception cref="ArgumentOutOfRangeException">The point is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during navigation event delivery or off the tree owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    public Vector2 TargetPosition { get { Read(); return _target; } set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); _target = value; _submitted = true; Repath(); } }
    /// <summary>Returns the stable node-owned agent identity.</summary>
    /// <returns>Borrowed RID valid until disposal; FreeRID rejects it.</returns>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.</exception>
    public RID GetRID() { Read(); return _rid; }
    /// <summary>Returns the explicit override, attached direct Entity parent's World map, or empty.</summary>
    /// <returns>Source map selection; runtime RIDs are not serialized.</returns>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.</exception>
    public RID GetNavigationMap() { Read(); return _mapOverride.IsValid() ? _mapOverride : IsInsideTree && Parent is Entity parent ? parent.GetWorld()!.NavigationMap : default; }
    /// <summary>Changes the live map override; empty restores the parent's selected World map.</summary>
    /// <param name="map">Live map RID or empty.</param>
    /// <exception cref="ArgumentException">The map RID is stale or of another kind.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during navigation delivery or off the tree owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    public void SetNavigationMap(RID map) { EnsureMutable(); var selected = map.IsValid() ? map : IsInsideTree && Parent is Entity parent ? parent.GetWorld()!.NavigationMap : default; NavigationServer.AgentSetMap(_rid, selected); if (_mapOverride == map) return; _mapOverride = map; if (_submitted) Repath(); }
    /// <summary>Returns a copied current world-space path without advancing navigation.</summary>
    /// <returns>Independent waypoint array, empty before querying.</returns>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.</exception>
    public Vector2[] GetCurrentNavigationPath() { Read(); return _result.Path; }
    /// <summary>Returns the current waypoint index without advancing navigation.</summary>
    /// <returns>Zero initially; the final reached waypoint remains selected.</returns>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.</exception>
    public int GetCurrentNavigationPathIndex() { Read(); return _index; }
    /// <summary>Returns the retained result used by this agent.</summary>
    /// <returns>Borrowed result; caller edits are observed safely on the next update. Consumer disposal rejects while this agent is alive.</returns>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.</exception>
    public NavigationPathQueryResult GetCurrentNavigationResult() { Read(); return _result; }
    /// <summary>Returns the current published path length without advancing navigation.</summary>
    /// <returns>World-space length, zero initially.</returns>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.</exception>
    public float GetPathLength() { Read(); return _result.PathLength; }
    /// <summary>Updates navigation and returns the next waypoint or current parent position for an empty path.</summary>
    /// <returns>World-space steering target; the caller moves the parent.</returns>
    /// <exception cref="InvalidOperationException">The direct parent is not an attached Entity, the caller is off-owner, or update reentry is attempted.</exception>
    /// <exception cref="AggregateException">Observers fail after completed transitions.</exception>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    public Vector2 GetNextPathPosition() { var parent = RequireParent(); UpdateNavigation(); return _path.Path.Length == 0 ? parent.GlobalPosition : _path.Path[_index]; }
    /// <summary>Updates navigation and returns its final reachable path position.</summary>
    /// <returns>World-space final position or zero for an empty path.</returns>
    /// <exception cref="InvalidOperationException">The direct parent is not an attached Entity or navigation is reentered.</exception>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    public Vector2 GetFinalPosition() { UpdateNavigation(); return _path.Path.Length == 0 ? Vector2.Zero : _path.Path[^1]; }
    /// <summary>Returns the current parent-to-requested-target distance without advancing the path.</summary>
    /// <returns>World-space distance.</returns>
    /// <exception cref="InvalidOperationException">The direct parent is not an attached Entity.</exception>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    public float DistanceToTarget() => RequireParent().GlobalPosition.DistanceTo(_target);
    /// <summary>Returns cached target completion without advancing the path.</summary>
    /// <returns>Whether TargetReached has occurred since the latest target assignment.</returns>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.</exception>
    public bool IsTargetReached() { Read(); return _targetReached; }
    /// <summary>Updates navigation and tests the final point against the inclusive target reachability threshold.</summary>
    /// <returns>False for an empty path; otherwise whether final-point distance is at most TargetDesiredDistance.</returns>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.</exception>
    public bool IsTargetReachable() { UpdateNavigation(); return Reachable(); }
    /// <summary>Updates navigation and returns completion of the current request.</summary>
    /// <returns>False initially or for an empty path; true after NavigationFinished.</returns>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.</exception>
    public bool IsNavigationFinished() { UpdateNavigation(); return _finished; }
    /// <summary>Returns a navigation layer bit using one-based indices.</summary>
    /// <param name="layerNumber">Index one through thirty-two.</param><returns>Whether the bit is set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Index is outside one through thirty-two.</exception>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.</exception>
    public bool GetNavigationLayerValue(int layerNumber) { Layer(layerNumber); return (NavigationLayers & (1u << (layerNumber - 1))) != 0; }
    /// <summary>Changes a navigation layer bit and repaths an active request.</summary>
    /// <param name="layerNumber">Index one through thirty-two.</param><param name="value">New bit state.</param>
    /// <exception cref="ArgumentOutOfRangeException">Index is outside one through thirty-two.</exception>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.</exception>
    public void SetNavigationLayerValue(int layerNumber, bool value) { Layer(layerNumber); var bit = 1u << (layerNumber - 1); NavigationLayers = value ? NavigationLayers | bit : NavigationLayers & ~bit; }
    /// <summary>Gets or changes the 32-bit navigation mask; changing it repaths an active request.</summary>
    /// <value>Default 1u; retained source setting.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during navigation delivery or off the tree owner thread.</exception>
    public uint NavigationLayers { get { Read(); return _query.NavigationLayers; } set { EnsureMutable(); if (_query.NavigationLayers == value) return; _query.NavigationLayers = value; if (_submitted) Repath(); } }
    /// <summary>Gets or changes the finite strict waypoint distance threshold; nonpositive values never advance.</summary>
    /// <value>Default 20f; retained source setting.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during navigation delivery or off the tree owner thread.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public float PathDesiredDistance { get { Read(); return _pathDesiredDistance; } set { EnsureMutable(); _pathDesiredDistance = Number(value); } }
    /// <summary>Gets or changes the finite strict target threshold, inclusive for reachability; nonpositive values never reach.</summary>
    /// <value>Default 10f; retained source setting.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during navigation delivery or off the tree owner thread.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public float TargetDesiredDistance { get { Read(); return _targetDesiredDistance; } set { EnsureMutable(); _targetDesiredDistance = Number(value); } }
    /// <summary>Gets or changes the finite inclusive distance from the active path segment that triggers a repath.</summary>
    /// <value>Default 100f; retained source setting.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during navigation delivery or off the tree owner thread.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public float PathMaxDistance { get { Read(); return _pathMaxDistance; } set { EnsureMutable(); _pathMaxDistance = Number(value); } }
    /// <summary>Gets or changes the metadata selection applied on the next path query.</summary>
    /// <value>Default NavigationPathQueryParameters.PathMetadataFlags.IncludeAll; retained source setting.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during navigation delivery or off the tree owner thread.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public NavigationPathQueryParameters.PathMetadataFlags PathMetadataFlags { get { Read(); return _query.MetadataFlags; } set { EnsureMutable(); _query.MetadataFlags = value; } }
    /// <summary>Gets or changes the output mode applied on the next path query.</summary>
    /// <value>Default NavigationPathQueryParameters.PathPostProcessing.CorridorFunnel; retained source setting.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during navigation delivery or off the tree owner thread.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public NavigationPathQueryParameters.PathPostProcessing PathPostprocessing { get { Read(); return _query.PostProcessing; } set { EnsureMutable(); _query.PostProcessing = value; } }
    /// <summary>Gets or changes the search algorithm applied on the next path query.</summary>
    /// <value>Default NavigationPathQueryParameters.PathfindingAlgorithm.AStar; retained source setting.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during navigation delivery or off the tree owner thread.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public NavigationPathQueryParameters.PathfindingAlgorithm PathfindingAlgorithm { get { Read(); return _query.Algorithm; } set { EnsureMutable(); _query.Algorithm = value; } }
    /// <summary>Gets or changes the point simplification applied on the next path query.</summary>
    /// <value>Default false; retained source setting.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during navigation delivery or off the tree owner thread.</exception>
    public bool SimplifyPath { get { Read(); return _query.SimplifyPath; } set { EnsureMutable(); _query.SimplifyPath = value; } }
    /// <summary>Gets or changes the finite simplification epsilon clamped to zero, applied on the next path query.</summary>
    /// <value>Default 0f; retained source setting.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during navigation delivery or off the tree owner thread.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public float SimplifyEpsilon { get { Read(); return _query.SimplifyEpsilon; } set { EnsureMutable(); _query.SimplifyEpsilon = value; } }
    /// <summary>Gets or changes the finite maximum output length clamped to zero; zero disables clipping.</summary>
    /// <value>Default 0f; retained source setting.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during navigation delivery or off the tree owner thread.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public float PathReturnMaxLength { get { Read(); return _query.PathReturnMaxLength; } set { EnsureMutable(); _query.PathReturnMaxLength = value; } }
    /// <summary>Gets or changes the finite maximum output circle radius clamped to zero; zero disables clipping.</summary>
    /// <value>Default 0f; retained source setting.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during navigation delivery or off the tree owner thread.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public float PathReturnMaxRadius { get { Read(); return _query.PathReturnMaxRadius; } set { EnsureMutable(); _query.PathReturnMaxRadius = value; } }
    /// <summary>Gets or changes the finite search distance clamped to zero; zero disables the limit.</summary>
    /// <value>Default 0f; retained source setting.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during navigation delivery or off the tree owner thread.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or an unsupported enum/flag.</exception>
    public float PathSearchMaxDistance { get { Read(); return _query.PathSearchMaxDistance; } set { EnsureMutable(); _query.PathSearchMaxDistance = value; } }
    /// <summary>Gets or changes the processed polygon limit; nonpositive values are unlimited.</summary>
    /// <value>Default 4096; retained source setting.</value>
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs during navigation delivery or off the tree owner thread.</exception>
    public int PathSearchMaxPolygons { get { Read(); return _query.PathSearchMaxPolygons; } set { EnsureMutable(); _query.PathSearchMaxPolygons = value; } }
    private static double Distance(Vector2 a, Vector2 b) { var x = (double)a.X - b.X; var y = (double)a.Y - b.Y; return Math.Sqrt(x * x + y * y); }
    private static void Layer(int value) { if (value is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static float Number(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); return value; }
    private void Read() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private Entity RequireParent() { Read(); return IsInsideTree && Parent is Entity parent ? parent : throw new InvalidOperationException("Navigation requires a direct Entity parent inside a SceneTree."); }
    private void Repath() { if (_updating) { _invalidated = true; return; } _result.Reset(); _path = NavigationPathQueryData.Empty; _index = 0; _targetReached = _finished = _lastReached = false; }
    private bool Reachable() => _path.Path.Length > 0 && Distance(_path.Path[^1], _target) <= _targetDesiredDistance;
    private void UpdateNavigation()
    {
        var parent = RequireParent(); if (_updating) throw new InvalidOperationException("Navigation updates cannot be re-entered."); if (!_submitted) { _path = _result.Snapshot(); _index = Math.Clamp(_index, 0, Math.Max(0, _path.Path.Length - 1)); return; }
        _updating = true; List<Exception>? errors = null;
        try
        {
            var origin = parent.GlobalPosition; var published = _result.Snapshot();
            var edited = !ReferenceEquals(published.Path, _path.Path); _path = published;
            var reload = NavigationServer.AgentIsMapChanged(_rid) || _path.Path.Length == 0 || edited || _index >= _path.Path.Length;
            if (!reload && _index > 0)
            {
                var a = _path.Path[_index - 1]; var b = _path.Path[_index]; var dx = (double)b.X - a.X; var dy = (double)b.Y - a.Y; var length = dx * dx + dy * dy;
                var t = length == 0 ? 0 : Math.Clamp((((double)origin.X - a.X) * dx + ((double)origin.Y - a.Y) * dy) / length, 0, 1);
                reload = Distance(origin, new((float)(a.X + dx * t), (float)(a.Y + dy * t))) >= _pathMaxDistance;
            }
            if (reload)
            {
                var map = GetNavigationMap(); _query.Map = map; _query.StartPosition = origin; _query.TargetPosition = _target;
                if (map.IsValid()) NavigationServer.QueryPath(_query, _result); else _result.Reset();
                _path = _result.Snapshot(); _finished = _lastReached = false; _index = 0; Deliver(PathChanged, ref errors); if (_invalidated) return;
            }
            if (_path.Path.Length == 0 || _finished) return;
            if (!_lastReached)
                while (Distance(origin, _path.Path[_index]) < _pathDesiredDistance)
                {
                    var waypoint = Details(_index); if (_index == _path.Path.Length - 1) _lastReached = true; else _index++;
                    Deliver(WaypointReached, waypoint, ref errors); if (waypoint.Type == NavigationPathQueryResult.PathSegmentType.Link) Deliver(LinkReached, waypoint, ref errors);
                    if (_invalidated) return;
                    if (_lastReached) break;
                }
            if (Distance(origin, _target) < _targetDesiredDistance)
            { _targetReached = true; Deliver(TargetReached, ref errors); if (_invalidated) return; _finished = true; _submitted = false; Deliver(NavigationFinished, ref errors); }
            else if (_lastReached && !Reachable()) { _finished = true; _submitted = false; Deliver(NavigationFinished, ref errors); }
        }
        finally { _updating = false; if (_invalidated) { _invalidated = false; Repath(); } if (errors is not null) throw new AggregateException("Navigation transitions completed with observer failures.", errors); }
    }
    private NavigationWaypoint Details(int index)
    {
        var point = _path.Path[index]; var type = index < _path.PathTypes.Length ? _path.PathTypes[index] : (NavigationPathQueryResult.PathSegmentType?)null;
        var ownerID = index < _path.PathOwnerIDs.Length ? _path.PathOwnerIDs[index] : (ulong?)null; var owner = NavigationServer.Service.ResolveNavigationOwner(ownerID ?? 0);
        Vector2? entry = null, exit = null;
        if (type == NavigationPathQueryResult.PathSegmentType.Link && owner is NavigationLink link)
        { var start = link.GetGlobalStartPosition(); var end = link.GetGlobalEndPosition(); var forward = Distance(point, start) < Distance(point, end); entry = forward ? start : end; exit = forward ? end : start; }
        return new() { Position = point, Type = type, RID = index < _path.PathRIDs.Length ? _path.PathRIDs[index] : null, OwnerID = ownerID, Owner = owner, LinkEntryPosition = entry, LinkExitPosition = exit };
    }
    private static void Deliver(Action? handlers, ref List<Exception>? errors) { if (handlers is not null) foreach (var handler in Delegate.EnumerateInvocationList(handlers)) try { handler(); } catch (Exception e) { (errors ??= []).Add(e); } }
    private static void Deliver(Action<NavigationWaypoint>? handlers, NavigationWaypoint point, ref List<Exception>? errors) { if (handlers is not null) foreach (var handler in Delegate.EnumerateInvocationList(handlers)) try { handler(point); } catch (Exception e) { (errors ??= []).Add(e); } }
    internal void RebindWorld() => PublishMap();
    private void PublishMap() { NavigationServer.AgentSetMap(_rid, GetNavigationMap()); if (_submitted) Repath(); }
    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">The agent is disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.</exception>
    public override string[] GetConfigurationWarnings() { var warnings = base.GetConfigurationWarnings(); return Parent is Entity ? warnings : [.. warnings, "NavigationAgent requires a direct Entity parent."]; }
    /// <inheritdoc />
    protected override void OnEnterTree() { base.OnEnterTree(); PublishMap(); }
    /// <inheritdoc />
    protected override void OnExitTree() { try { NavigationServer.AgentSetMap(_rid, default); if (_submitted) Repath(); } finally { base.OnExitTree(); } }
    /// <inheritdoc />
    protected override void OnNotification(int what) { base.OnNotification(what); if (IsInsideTree && what == CanvasItem.NotificationWorldChanged) PublishMap(); }
    /// <inheritdoc />
    protected override void ValidateMutation() { base.ValidateMutation(); if (_updating) throw new InvalidOperationException("Agent mutation cannot occur during navigation delivery."); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); if (_updating) throw new InvalidOperationException("Agent disposal cannot occur during navigation delivery."); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { NavigationServer.Service.ReleaseSceneAgent(_rid); _query.Dispose(); _result.Dispose(); } base.Dispose(disposing); }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateAgentNode;
    private static NavigationAgent CreateAgentNode() => new();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(AgentProperties);
    private static readonly PropertyDescriptor[] AgentProperties =
    [
        new PropertyDescriptor<NavigationAgent,uint>(nameof(NavigationLayers),n=>n.NavigationLayers,(n,v)=>n.NavigationLayers=v,_=>1u,stored:true),
        new PropertyDescriptor<NavigationAgent,float>(nameof(PathDesiredDistance),n=>n.PathDesiredDistance,(n,v)=>n.PathDesiredDistance=v,_=>20f,stored:true),
        new PropertyDescriptor<NavigationAgent,float>(nameof(TargetDesiredDistance),n=>n.TargetDesiredDistance,(n,v)=>n.TargetDesiredDistance=v,_=>10f,stored:true),
        new PropertyDescriptor<NavigationAgent,float>(nameof(PathMaxDistance),n=>n.PathMaxDistance,(n,v)=>n.PathMaxDistance=v,_=>100f,stored:true),
        new PropertyDescriptor<NavigationAgent,NavigationPathQueryParameters.PathMetadataFlags>(nameof(PathMetadataFlags),n=>n.PathMetadataFlags,(n,v)=>n.PathMetadataFlags=v,_=>NavigationPathQueryParameters.PathMetadataFlags.IncludeAll,stored:true),
        new PropertyDescriptor<NavigationAgent,NavigationPathQueryParameters.PathPostProcessing>(nameof(PathPostprocessing),n=>n.PathPostprocessing,(n,v)=>n.PathPostprocessing=v,_=>NavigationPathQueryParameters.PathPostProcessing.CorridorFunnel,stored:true),
        new PropertyDescriptor<NavigationAgent,NavigationPathQueryParameters.PathfindingAlgorithm>(nameof(PathfindingAlgorithm),n=>n.PathfindingAlgorithm,(n,v)=>n.PathfindingAlgorithm=v,_=>NavigationPathQueryParameters.PathfindingAlgorithm.AStar,stored:true),
        new PropertyDescriptor<NavigationAgent,bool>(nameof(SimplifyPath),n=>n.SimplifyPath,(n,v)=>n.SimplifyPath=v,_=>false,stored:true),
        new PropertyDescriptor<NavigationAgent,float>(nameof(SimplifyEpsilon),n=>n.SimplifyEpsilon,(n,v)=>n.SimplifyEpsilon=v,_=>0f,stored:true),
        new PropertyDescriptor<NavigationAgent,float>(nameof(PathReturnMaxLength),n=>n.PathReturnMaxLength,(n,v)=>n.PathReturnMaxLength=v,_=>0f,stored:true),
        new PropertyDescriptor<NavigationAgent,float>(nameof(PathReturnMaxRadius),n=>n.PathReturnMaxRadius,(n,v)=>n.PathReturnMaxRadius=v,_=>0f,stored:true),
        new PropertyDescriptor<NavigationAgent,float>(nameof(PathSearchMaxDistance),n=>n.PathSearchMaxDistance,(n,v)=>n.PathSearchMaxDistance=v,_=>0f,stored:true),
        new PropertyDescriptor<NavigationAgent,int>(nameof(PathSearchMaxPolygons),n=>n.PathSearchMaxPolygons,(n,v)=>n.PathSearchMaxPolygons=v,_=>4096,stored:true),
        new PropertyDescriptor<NavigationAgent,Vector2>(nameof(TargetPosition),n=>n.TargetPosition,(n,v)=>n.TargetPosition=v,_=>Vector2.Zero,stored:true),
    ];
}
