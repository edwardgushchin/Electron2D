namespace Electron2D;

/// <summary>Constrains navigation avoidance with a moving disc and an oriented static contour.</summary>
/// <remarks>Owns a stable borrowed obstacle RID. Local source vertices are copied; attachment publishes transformed
/// offsets and world position. Static contour velocity has no effect; Radius uses the moving velocity prediction.</remarks>
public sealed class NavigationObstacle : Entity
{
    private readonly RID _rid;
    private RID _mapOverride;
    private bool _avoidanceEnabled = true;
    private uint _avoidanceLayers = 1;
    private float _radius;
    private Vector2 _velocity;
    private Vector2[] _vertices = [];
    private Transform _publishedTransform;
    private bool _published, _scenePaused;
    private RID _mapBeforePause;
    /// <summary>Creates an enabled detached obstacle with zero radius and empty contour.</summary>
    public NavigationObstacle() { _rid = NavigationServer.Service.CreateObstacle(this); NotifyTransformChanges = true; }
    /// <summary>Gets or changes participation in avoidance.</summary>
    /// <value>True initially.</value>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool AvoidanceEnabled { get { ThrowIfDisposed(); return _avoidanceEnabled; } set { EnsureMutable(); NavigationServer.ObstacleSetAvoidanceEnabled(_rid, value); _avoidanceEnabled = value; } }
    /// <summary>Gets or changes the 32-bit layers visible to agent masks.</summary>
    /// <value>One initially; independent of direct server edits.</value>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public uint AvoidanceLayers { get { ThrowIfDisposed(); return _avoidanceLayers; } set { EnsureMutable(); NavigationServer.ObstacleSetAvoidanceLayers(_rid, value); _avoidanceLayers = value; } }
    /// <summary>Gets or changes the finite nonnegative source disc radius.</summary>
    /// <value>Zero initially; world radius uses the largest absolute scale axis, clamped to .001.</value>
    /// <exception cref="ArgumentOutOfRangeException">Radius or transformed radius is negative/nonfinite.</exception>
    public float Radius { get { ThrowIfDisposed(); return _radius; } set { EnsureMutable(); if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); var scale = (IsInsideTree ? GlobalScale : Scale).Abs(); var radius = Math.Max(.001f, Math.Max(scale.X, scale.Y)) * value; NavigationServer.ObstacleSetRadius(_rid, radius); _radius = value; _published = false; } }
    /// <summary>Gets or changes finite moving-disc velocity; static contour prediction ignores it.</summary>
    /// <value>Zero initially; retained source velocity.</value>
    /// <exception cref="ArgumentException">Velocity is nonfinite.</exception>
    public Vector2 Velocity { get { ThrowIfDisposed(); return _velocity; } set { EnsureMutable(); NavigationServer.ObstacleSetVelocity(_rid, value); _velocity = value; } }
    /// <summary>Gets or changes a copied finite simple oriented local contour.</summary>
    /// <value>Empty initially. Two points form a two-sided wall; larger contours preserve their winding.</value>
    /// <exception cref="ArgumentNullException">Assigned array is null.</exception>
    /// <exception cref="ArgumentException">Points are nonfinite, duplicate or form a degenerate/intersecting contour.</exception>
    public Vector2[] Vertices { get { ThrowIfDisposed(); return (Vector2[])_vertices.Clone(); } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); var copy = NavigationServer.ValidateContour(value); var transformed = TransformVertices(copy, IsInsideTree ? GlobalTransform : Transform.Identity); NavigationServer.ObstacleSetVertices(_rid, transformed); _vertices = copy; _published = false; UpdateConfigurationWarnings(); } }
    /// <summary>Returns the stable node-owned obstacle identity.</summary>
    /// <returns>Borrowed RID valid until disposal; FreeRID rejects it.</returns>
    public RID GetRID() { ThrowIfDisposed(); return _rid; }
    /// <summary>Returns explicit map selection, attached World map, or empty when detached.</summary>
    /// <returns>Source assignment independent of direct server edits.</returns>
    public RID GetNavigationMap() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _mapOverride.IsValid() ? _mapOverride : IsInsideTree ? GetWorld()!.NavigationMap : default; }
    /// <summary>Changes a live override; empty restores the attached World map.</summary>
    /// <param name="map">Live map RID or empty.</param>
    /// <exception cref="ArgumentException">Map RID is invalid.</exception>
    /// <exception cref="InvalidOperationException">The map belongs to another scene tree or mutation is off-owner.</exception>
    public void SetNavigationMap(RID map) { EnsureMutable(); var selected = map.IsValid() ? map : IsInsideTree ? GetWorld()!.NavigationMap : default; NavigationServer.ObstacleSetMap(_rid, selected); _mapOverride = map; if (IsInsideTree) { _scenePaused = false; UpdatePause(); } }
    /// <summary>Returns an avoidance layer bit using one-based indices.</summary>
    /// <param name="layerNumber">Index one through thirty-two.</param><returns>Whether the bit is set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Index is outside the accepted interval.</exception>
    public bool GetAvoidanceLayerValue(int layerNumber) { Layer(layerNumber); return (AvoidanceLayers & (1u << (layerNumber - 1))) != 0; }
    /// <summary>Changes an avoidance layer bit using one-based indices.</summary>
    /// <param name="layerNumber">Index one through thirty-two.</param><param name="value">New bit state.</param>
    /// <exception cref="ArgumentOutOfRangeException">Index is outside the accepted interval.</exception>
    public void SetAvoidanceLayerValue(int layerNumber, bool value) { Layer(layerNumber); var bit = 1u << (layerNumber - 1); AvoidanceLayers = value ? AvoidanceLayers | bit : AvoidanceLayers & ~bit; }
    private static void Layer(int value) { if (value is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static Vector2[] TransformVertices(Vector2[] source, Transform transform) { if (source.Length == 0) return []; var values = new Vector2[source.Length]; for (var i = 0; i < values.Length; i++) values[i] = transform.BasisXform(source[i]); return values; }
    internal void PrepareAvoidance() { UpdatePause(); PublishGeometry(); }
    private void UpdatePause()
    {
        var paused = !CanProcess();
        if (paused && !_scenePaused) { _mapBeforePause = NavigationServer.ObstacleGetMap(_rid); NavigationServer.ObstacleSetMap(_rid, default); }
        else if (!paused && _scenePaused) NavigationServer.ObstacleSetMap(_rid, _mapBeforePause);
        NavigationServer.ObstacleSetPaused(_rid, paused); _scenePaused = paused;
    }
    private void PublishMap() { NavigationServer.ObstacleSetMap(_rid, GetNavigationMap()); _scenePaused = false; UpdatePause(); }
    private void PublishGeometry()
    { var transform = GlobalTransform; if (_published && _publishedTransform == transform) return; var vertices = TransformVertices(_vertices, transform); var scale = GlobalScale.Abs(); var radius = Math.Max(.001f, Math.Max(scale.X, scale.Y)) * _radius; NavigationServer.Service.StageSceneObstacle(_rid, transform.Origin, radius, vertices); _publishedTransform = transform; _published = true; }
    /// <inheritdoc />
    public override string[] GetConfigurationWarnings() { var warnings = base.GetConfigurationWarnings(); return _radius == 0 && _vertices.Length < 2 ? [.. warnings, "Navigation obstacle needs a positive radius or at least two contour vertices."] : warnings; }
    /// <inheritdoc />
    protected override void OnEnterTree() { base.OnEnterTree(); PublishMap(); _published = false; PublishGeometry(); }
    /// <inheritdoc />
    protected override void OnExitTree() { try { NavigationServer.ObstacleSetMap(_rid, default); _published = false; _scenePaused = false; _mapBeforePause = default; } finally { base.OnExitTree(); } }
    /// <inheritdoc />
    protected override void OnNotification(int what) { base.OnNotification(what); if (!IsInsideTree) return; if (what == NotificationWorldChanged) { PublishMap(); _published = false; PublishGeometry(); } else if (what == NotificationTransformChanged) PublishGeometry(); else if (what is NotificationPaused or NotificationUnpaused or NotificationDisabled or NotificationEnabled) UpdatePause(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) NavigationServer.Service.ReleaseSceneObstacle(_rid); base.Dispose(disposing); }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateObstacleNode;
    private static NavigationObstacle CreateObstacleNode() => new();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(ObstacleProperties);
    private static readonly PropertyDescriptor[] ObstacleProperties =
    [new PropertyDescriptor<NavigationObstacle,bool>(nameof(AvoidanceEnabled),n=>n.AvoidanceEnabled,(n,v)=>n.AvoidanceEnabled=v,_=>true,stored:true),
     new PropertyDescriptor<NavigationObstacle,uint>(nameof(AvoidanceLayers),n=>n.AvoidanceLayers,(n,v)=>n.AvoidanceLayers=v,_=>1u,stored:true),
     new PropertyDescriptor<NavigationObstacle,float>(nameof(Radius),n=>n.Radius,(n,v)=>n.Radius=v,_=>0f,stored:true),
     new PropertyDescriptor<NavigationObstacle,Vector2>(nameof(Velocity),n=>n.Velocity,(n,v)=>n.Velocity=v,_=>Vector2.Zero,stored:true),
     new PropertyDescriptor<NavigationObstacle,Vector2[]>(nameof(Vertices),n=>n.Vertices,(n,v)=>n.Vertices=v,_=>[],stored:true)];
}
