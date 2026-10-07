namespace Electron2D;

/// <summary>Places authored navigation polygons in the same map used by its selected scene World.</summary>
/// <remarks>Scene properties stage server edits; synchronization publishes query topology. Geometry is borrowed.
/// Source parsing/baking, clearance and avoidance have separate prerequisites.</remarks>
public sealed class NavigationRegion : Entity
{
    private readonly RID _rid;
    private RID _mapOverride;
    private NavigationPolygon? _polygon;
    private bool _enabled = true, _edgeConnections = true;
    private uint _layers = 1;
    private float _enterCost, _travelCost = 1;
    /// <summary>Creates an enabled detached region with stable borrowed RID identity.</summary>
    public NavigationRegion() { _rid = NavigationServer.Service.CreateRegion(this); NotifyTransformChanges = true; }
    /// <summary>Returns this node's stable borrowed region identity.</summary>
    /// <returns>Returns this node's stable borrowed region identity.</returns>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public RID GetRID() { ThrowIfDisposed(); return _rid; }
    /// <summary>Gets or changes whether authored region geometry participates in navigation.</summary>
    /// <value>Gets or changes whether authored region geometry participates in navigation.</value>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public bool Enabled { get { ThrowIfDisposed(); return _enabled; } set { EnsureMutable(); _enabled = value; NavigationServer.RegionSetEnabled(_rid, value); } }
    /// <summary>Gets or changes automatic edge connections to other regions.</summary>
    /// <value>Gets or changes automatic edge connections to other regions.</value>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public bool UseEdgeConnections { get { ThrowIfDisposed(); return _edgeConnections; } set { EnsureMutable(); _edgeConnections = value; NavigationServer.RegionSetUseEdgeConnections(_rid, value); } }
    /// <summary>Gets or changes the region's navigation layer bits, initially one.</summary>
    /// <value>Gets or changes the region's navigation layer bits, initially one.</value>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public uint NavigationLayers { get { ThrowIfDisposed(); return _layers; } set { EnsureMutable(); _layers = value; NavigationServer.RegionSetNavigationLayers(_rid, value); } }
    /// <summary>Gets or changes finite nonnegative cost paid on entry from another region.</summary>
    /// <value>Gets or changes finite nonnegative cost paid on entry from another region.</value>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public float EnterCost { get { ThrowIfDisposed(); return _enterCost; } set { EnsureMutable(); NavigationServer.RegionSetEnterCost(_rid, value); _enterCost = value; } }
    /// <summary>Gets or changes finite nonnegative travel cost per unit of world distance.</summary>
    /// <value>Gets or changes finite nonnegative travel cost per unit of world distance.</value>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public float TravelCost { get { ThrowIfDisposed(); return _travelCost; } set { EnsureMutable(); NavigationServer.RegionSetTravelCost(_rid, value); _travelCost = value; } }
    /// <summary>Gets or sets the borrowed authored navigation polygon resource.</summary>
    /// <remarks>Assignment stages a complete geometry version before notifying; null clears it. Resource edits stage later versions without replacing this source property.</remarks>
    /// <value>Gets or sets the borrowed authored navigation polygon resource.</value>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public NavigationPolygon? NavigationPolygon
    {
        get { ThrowIfDisposed(); return _polygon; }
        set { EnsureMutable(); NavigationServer.RegionSetNavigationPolygon(_rid, value); _polygon = value; NavigationPolygonChanged?.Invoke(); }
    }
    /// <summary>Occurs after polygon replacement or an authored resource edit stages server geometry.</summary>
    public event Action? NavigationPolygonChanged;
    internal void NotifyPolygonChanged() => NavigationPolygonChanged?.Invoke();
    /// <summary>Returns the explicit map override or the selected World's map while attached.</summary>
    /// <returns>Returns the explicit map override or the selected World's map while attached.</returns>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public RID GetNavigationMap() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _mapOverride.IsValid() ? _mapOverride : GetWorld()?.NavigationMap ?? default; }
    /// <summary>Sets a validated explicit navigation map, or empty to select the scene World's map.</summary>
    /// <param name="map">Live navigation map RID; empty is accepted only when detaching a region.</param>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public void SetNavigationMap(RID map) { EnsureMutable(); var selected = map.IsValid() ? map : GetWorld()?.NavigationMap ?? default; NavigationServer.RegionSetMap(_rid, selected); _mapOverride = map; }
    /// <summary>Returns committed world-space bounds of this region's current topology.</summary>
    /// <returns>Returns committed world-space bounds of this region's current topology.</returns>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public Rect2 GetBounds() { ThrowIfDisposed(); return NavigationServer.RegionGetBounds(_rid); }
    /// <summary>Returns one navigation-layer bit using one-based indices 1 through 32.</summary>
    /// <param name="layerNumber">One-based navigation layer number, from 1 through 32.</param>
    /// <returns>Returns one navigation-layer bit using one-based indices 1 through 32.</returns>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public bool GetNavigationLayerValue(int layerNumber) { Layer(layerNumber); return (NavigationLayers & (1u << (layerNumber - 1))) != 0; }
    /// <summary>Changes one navigation-layer bit using one-based indices 1 through 32.</summary>
    /// <param name="layerNumber">One-based navigation layer number, from 1 through 32.</param>
    /// <param name="value">Whether the selected navigation layer bit is enabled.</param>
    /// <exception cref="ObjectDisposedException">This instance is disposed.</exception>
    public void SetNavigationLayerValue(int layerNumber, bool value) { Layer(layerNumber); var bit = 1u << (layerNumber - 1); NavigationLayers = value ? NavigationLayers | bit : NavigationLayers & ~bit; }
    private static void Layer(int value) { if (value is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(value)); }
    private void Publish()
    {
        NavigationServer.RegionSetMap(_rid, GetNavigationMap()); NavigationServer.RegionSetTransform(_rid, GlobalTransform);
        NavigationServer.RegionSetEnabled(_rid, _enabled); NavigationServer.RegionSetUseEdgeConnections(_rid, _edgeConnections); NavigationServer.RegionSetNavigationLayers(_rid, _layers);
        NavigationServer.RegionSetEnterCost(_rid, _enterCost); NavigationServer.RegionSetTravelCost(_rid, _travelCost); NavigationServer.RegionSetNavigationPolygon(_rid, _polygon); NavigationServer.RegionSetOwnerID(_rid, InstanceID);
    }
    /// <inheritdoc />
    protected override void OnEnterTree() { base.OnEnterTree(); Publish(); }
    /// <inheritdoc />
    protected override void OnExitTree() { try { NavigationServer.RegionSetMap(_rid, default); } finally { base.OnExitTree(); } }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    { base.OnNotification(what); if (IsInsideTree && what == NotificationWorldChanged) Publish(); else if (IsInsideTree && what == NotificationTransformChanged) NavigationServer.RegionSetTransform(_rid, GlobalTransform); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { NavigationServer.Service.ReleaseSceneRegion(_rid); NavigationPolygonChanged = null; _polygon = null; } base.Dispose(disposing); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(RegionProperties);
    private static readonly PropertyDescriptor[] RegionProperties =
    [new PropertyDescriptor<NavigationRegion, bool>(nameof(Enabled), n => n.Enabled, (n,v) => n.Enabled=v, _=>true, stored:true),
     new PropertyDescriptor<NavigationRegion, bool>(nameof(UseEdgeConnections), n => n.UseEdgeConnections, (n,v) => n.UseEdgeConnections=v, _=>true, stored:true),
     new PropertyDescriptor<NavigationRegion, uint>(nameof(NavigationLayers), n => n.NavigationLayers, (n,v) => n.NavigationLayers=v, _=>1u, stored:true),
     new PropertyDescriptor<NavigationRegion, float>(nameof(EnterCost), n => n.EnterCost, (n,v) => n.EnterCost=v, _=>0f, stored:true),
     new PropertyDescriptor<NavigationRegion, float>(nameof(TravelCost), n => n.TravelCost, (n,v) => n.TravelCost=v, _=>1f, stored:true),
     new PropertyDescriptor<NavigationRegion, NavigationPolygon?>(nameof(NavigationPolygon), n => n.NavigationPolygon, (n,v) => n.NavigationPolygon=v, _=>null, stored:true)];
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateRegionNode;
    private static NavigationRegion CreateRegionNode() => new();
}
