namespace Electron2D;

/// <summary>Connects two authored navigation surfaces for directed or bidirectional off-surface travel.</summary>
/// <remarks>The node owns a stable borrowed RID; caller-created links use NavigationServer.LinkCreate instead.
/// Local endpoints convert to world coordinates while attached. Source properties cache independently of direct server edits;
/// map topology and queries change only after synchronization. Scene mutation follows the tree owner thread.</remarks>
public sealed class NavigationLink : Entity
{
    private readonly RID _rid;
    private RID _mapOverride;
    private bool _enabled = true, _bidirectional = true;
    private uint _layers = 1;
    private float _enterCost, _travelCost = 1;
    private Vector2 _start, _end;
    /// <summary>Creates an enabled bidirectional detached link with stable node-owned identity.</summary>
    public NavigationLink() { _rid = NavigationServer.Service.CreateLink(this); NavigationServer.LinkSetOwnerID(_rid, InstanceID); NotifyTransformChanges = true; }
    /// <summary>Returns the stable borrowed link RID.</summary>
    /// <returns>Identity valid until node disposal; consumer FreeRID rejects it.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public RID GetRID() { ThrowIfDisposed(); return _rid; }
    /// <summary>Gets or stages whether this link participates in path queries.</summary>
    /// <value>Source value, initially true; direct server edits do not change it.</value>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool Enabled { get { ThrowIfDisposed(); return _enabled; } set { EnsureMutable(); NavigationServer.LinkSetEnabled(_rid, value); _enabled = value; } }
    /// <summary>Gets or stages whether travel is allowed in both endpoint directions.</summary>
    /// <value>Source value, initially true; direct server edits do not change it.</value>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool Bidirectional { get { ThrowIfDisposed(); return _bidirectional; } set { EnsureMutable(); NavigationServer.LinkSetBidirectional(_rid, value); _bidirectional = value; } }
    /// <summary>Gets or stages the 32-bit navigation layer mask.</summary>
    /// <value>Source value, initially 1u; direct server edits do not change it.</value>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public uint NavigationLayers { get { ThrowIfDisposed(); return _layers; } set { EnsureMutable(); NavigationServer.LinkSetNavigationLayers(_rid, value); _layers = value; } }
    /// <summary>Gets or stages finite nonnegative link entry cost.</summary>
    /// <value>Source value, initially 0f; direct server edits do not change it.</value>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or negative.</exception>
    public float EnterCost { get { ThrowIfDisposed(); return _enterCost; } set { EnsureMutable(); NavigationServer.LinkSetEnterCost(_rid, value); _enterCost = value; } }
    /// <summary>Gets or stages finite nonnegative distance multiplier along the link.</summary>
    /// <value>Source value, initially 1f; direct server edits do not change it.</value>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is nonfinite or negative.</exception>
    public float TravelCost { get { ThrowIfDisposed(); return _travelCost; } set { EnsureMutable(); NavigationServer.LinkSetTravelCost(_rid, value); _travelCost = value; } }
    /// <summary>Gets or changes the local-space start endpoint.</summary>
    /// <value>Finite local coordinate, initially zero. Detached edits remain source state until attachment.</value>
    /// <exception cref="ArgumentException">The point or its attached world transformation is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Vector2 StartPosition
    {
        get { ThrowIfDisposed(); return _start; }
        set { EnsureMutable(); Finite(value); if (_start.IsEqualApprox(value)) return; if (IsInsideTree) NavigationServer.LinkSetStartPosition(_rid, ToGlobal(value)); _start = value; UpdateConfigurationWarnings(); }
    }
    /// <summary>Gets or changes the local-space end endpoint.</summary>
    /// <value>Finite local coordinate, initially zero. Detached edits remain source state until attachment.</value>
    /// <exception cref="ArgumentException">The point or its attached world transformation is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Vector2 EndPosition
    {
        get { ThrowIfDisposed(); return _end; }
        set { EnsureMutable(); Finite(value); if (_end.IsEqualApprox(value)) return; if (IsInsideTree) NavigationServer.LinkSetEndPosition(_rid, ToGlobal(value)); _end = value; UpdateConfigurationWarnings(); }
    }
    /// <summary>Returns the explicit map override, selected World's map while attached, or empty when detached.</summary>
    /// <returns>Live source assignment; runtime map RIDs are not serialized.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public RID GetNavigationMap() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _mapOverride.IsValid() ? _mapOverride : IsInsideTree ? GetWorld()!.NavigationMap : default; }
    /// <summary>Stages an explicit map override; empty restores the selected World map.</summary>
    /// <param name="map">Live map RID or empty.</param>
    /// <exception cref="ArgumentException">A nonempty RID is absent, stale or of another kind.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public void SetNavigationMap(RID map) { EnsureMutable(); var selected = map.IsValid() ? map : IsInsideTree ? GetWorld()!.NavigationMap : default; NavigationServer.LinkSetMap(_rid, selected); _mapOverride = map; }
    /// <summary>Returns the start endpoint in world coordinates while attached and local coordinates while detached.</summary>
    /// <returns>Transformed attached endpoint or detached source coordinate.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Vector2 GetGlobalStartPosition() { ThrowIfDisposed(); return IsInsideTree ? ToGlobal(_start) : _start; }
    /// <summary>Returns the end endpoint in world coordinates while attached and local coordinates while detached.</summary>
    /// <returns>Transformed attached endpoint or detached source coordinate.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Vector2 GetGlobalEndPosition() { ThrowIfDisposed(); return IsInsideTree ? ToGlobal(_end) : _end; }
    /// <summary>Changes the local start endpoint using an attached world coordinate or detached local coordinate.</summary>
    /// <param name="position">Finite point in the current global endpoint role.</param>
    /// <exception cref="InvalidOperationException">An attached inverse transform is singular or the caller is off the tree owner.</exception>
    /// <exception cref="ArgumentException">The point is nonfinite.</exception>
    public void SetGlobalStartPosition(Vector2 position) { EnsureMutable(); Finite(position); StartPosition = IsInsideTree ? ToLocal(position) : position; }
    /// <summary>Changes the local end endpoint using an attached world coordinate or detached local coordinate.</summary>
    /// <param name="position">Finite point in the current global endpoint role.</param>
    /// <exception cref="InvalidOperationException">An attached inverse transform is singular or the caller is off the tree owner.</exception>
    /// <exception cref="ArgumentException">The point is nonfinite.</exception>
    public void SetGlobalEndPosition(Vector2 position) { EnsureMutable(); Finite(position); EndPosition = IsInsideTree ? ToLocal(position) : position; }
    /// <summary>Returns a navigation layer bit using one-based indices one through thirty-two.</summary>
    /// <param name="layerNumber">Navigation layer index from one through thirty-two.</param><returns>Whether the source mask contains the bit.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the accepted interval.</exception>
    public bool GetNavigationLayerValue(int layerNumber) { Layer(layerNumber); return (NavigationLayers & (1u << (layerNumber - 1))) != 0; }
    /// <summary>Changes a navigation layer bit using one-based indices one through thirty-two.</summary>
    /// <param name="layerNumber">Navigation layer index from one through thirty-two.</param><param name="value">New bit state.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the accepted interval.</exception>
    public void SetNavigationLayerValue(int layerNumber, bool value) { Layer(layerNumber); var bit = 1u << (layerNumber - 1); NavigationLayers = value ? NavigationLayers | bit : NavigationLayers & ~bit; }
    /// <inheritdoc />
    public override string[] GetConfigurationWarnings() { var warnings = base.GetConfigurationWarnings(); return _start.IsEqualApprox(_end) ? [.. warnings, "Navigation link endpoints should differ to provide useful travel."] : warnings; }
    private static void Layer(int value) { if (value is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void Finite(Vector2 value) { if (!value.IsFinite()) throw new ArgumentException("Navigation link endpoint must be finite.", nameof(value)); }
    private void PublishEndpoints() => NavigationServer.Service.StageSceneLinkEndpoints(_rid, ToGlobal(_start), ToGlobal(_end));
    private void Publish() { var start = ToGlobal(_start); var end = ToGlobal(_end); Finite(start); Finite(end); NavigationServer.LinkSetMap(_rid, GetNavigationMap()); NavigationServer.Service.StageSceneLinkEndpoints(_rid, start, end); NavigationServer.LinkSetEnabled(_rid, _enabled); }
    /// <inheritdoc />
    protected override void OnEnterTree() { base.OnEnterTree(); Publish(); }
    /// <inheritdoc />
    protected override void OnExitTree() { try { NavigationServer.LinkSetMap(_rid, default); } finally { base.OnExitTree(); } }
    /// <inheritdoc />
    protected override void OnNotification(int what) { base.OnNotification(what); if (!IsInsideTree) return; if (what == NotificationWorldChanged) Publish(); else if (what == NotificationTransformChanged) PublishEndpoints(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) NavigationServer.Service.ReleaseSceneLink(_rid); base.Dispose(disposing); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(LinkProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateLinkNode;
    private static NavigationLink CreateLinkNode() => new();
    private static readonly PropertyDescriptor[] LinkProperties =
    [
        new PropertyDescriptor<NavigationLink,bool>(nameof(Enabled),n=>n.Enabled,(n,v)=>n.Enabled=v,_=>true,stored:true),
        new PropertyDescriptor<NavigationLink,bool>(nameof(Bidirectional),n=>n.Bidirectional,(n,v)=>n.Bidirectional=v,_=>true,stored:true),
        new PropertyDescriptor<NavigationLink,uint>(nameof(NavigationLayers),n=>n.NavigationLayers,(n,v)=>n.NavigationLayers=v,_=>1u,stored:true),
        new PropertyDescriptor<NavigationLink,float>(nameof(EnterCost),n=>n.EnterCost,(n,v)=>n.EnterCost=v,_=>0f,stored:true),
        new PropertyDescriptor<NavigationLink,float>(nameof(TravelCost),n=>n.TravelCost,(n,v)=>n.TravelCost=v,_=>1f,stored:true),
        new PropertyDescriptor<NavigationLink,Vector2>(nameof(StartPosition),n=>n.StartPosition,(n,v)=>n.StartPosition=v,_=>Vector2.Zero,stored:true),
        new PropertyDescriptor<NavigationLink,Vector2>(nameof(EndPosition),n=>n.EndPosition,(n,v)=>n.EndPosition=v,_=>Vector2.Zero,stored:true),
    ];
}
