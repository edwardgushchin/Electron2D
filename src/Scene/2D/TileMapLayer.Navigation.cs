namespace Electron2D;

public partial class TileMapLayer
{
    private bool _navigationEnabled = true;
    private DebugVisibilityMode _navigationVisibility;
    private RID _navigationMap;
    private sealed record GeneratedNavigation(RID RID, NavigationPolygon Polygon, Transform CellTransform);
    private readonly Dictionary<Vector2i, List<GeneratedNavigation>> _navigationCells = [];
    /// <summary>Gets or sets whether enabled tiles generate navigation regions, independently of drawing visibility.</summary>
    public bool NavigationEnabled { get { ThrowIfDisposed(); return _navigationEnabled; } set { EnsureMutable(); if (_navigationEnabled == value) return; _navigationEnabled = value; MarkAll(); } }
    /// <summary>Gets or sets navigation polygon overlay visibility; Default follows the tree navigation hint.</summary>
    public DebugVisibilityMode NavigationVisibilityMode { get { ThrowIfDisposed(); return _navigationVisibility; } set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_navigationVisibility == value) return; _navigationVisibility = value; QueueRedraw(); Changed?.Invoke(); } }
    /// <summary>Returns the borrowed explicit map, the current world map inside a tree, or an empty RID while detached.</summary>
    public RID GetNavigationMap() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _navigationMap.IsValid() ? _navigationMap : IsInsideTree ? GetWorld()!.NavigationMap : default; }
    /// <summary>Assigns a borrowed map; an empty RID restores the current world map.</summary>
    /// <param name="map">Live navigation map, or an empty RID for automatic world selection.</param>
    public void SetNavigationMap(RID map)
    { EnsureMutable(); if (map.IsValid()) NavigationServer.Service.ValidateTileMap(map, Tree); if (_navigationMap == map) return; _navigationMap = map; MarkAll(); }
    private void UpdateNavigation(Vector2i[] changed)
    {
        if (!IsInsideTree || !_enabled || !_navigationEnabled || _tileSet is not { IsDisposed: false }) { ReleaseNavigation(); return; }
        var map = GetNavigationMap(); NavigationServer.Service.ValidateTileMap(map, Tree);
        foreach (var coords in changed) RebuildNavigationCell(coords, map);
    }
    private void RebuildNavigationCell(Vector2i coords, RID map)
    {
        var prepared = new List<GeneratedNavigation>();
        try
        {
            if (_cells.TryGetValue(coords, out var cell) && EffectiveData(coords, cell) is { IsDisposed: false } data)
            {
                var transform = new Transform(0, MapToLocal(coords)); var worldTransform = GlobalTransform * transform;
                if (!worldTransform.IsFinite() || worldTransform.Determinant() == 0) throw new InvalidOperationException("Tile navigation transforms must be finite and invertible.");
                for (var i = 0; i < Math.Min(_tileSet!.NavigationLayers.Count, data.Navigation.Count); i++)
                {
                    var polygon = data.GetNavigationPolygon(i, (cell.Alternative & TileSetAtlasSource.TransformFlipH) != 0, (cell.Alternative & TileSetAtlasSource.TransformFlipV) != 0, (cell.Alternative & TileSetAtlasSource.TransformTranspose) != 0);
                    if (polygon is null || polygon.GetPolygonCount() == 0) continue;
                    var rid = NavigationServer.Service.CreateRegion(this); prepared.Add(new(rid, polygon, transform));
                    NavigationServer.RegionSetOwnerID(rid, InstanceID); NavigationServer.RegionSetTransform(rid, worldTransform);
                    NavigationServer.RegionSetNavigationLayers(rid, _tileSet.NavigationLayers[i]); NavigationServer.RegionSetNavigationPolygon(rid, polygon);
                }
            }
            foreach (var region in prepared) NavigationServer.RegionSetMap(region.RID, map);
        }
        catch { foreach (var region in prepared) NavigationServer.Service.ReleaseSceneRegion(region.RID); throw; }
        ReleaseNavigationCell(coords); if (prepared.Count != 0) _navigationCells.Add(coords, prepared);
    }
    private void ReleaseNavigationCell(Vector2i coords)
    { if (_navigationCells.Remove(coords, out var regions)) foreach (var region in regions) NavigationServer.Service.ReleaseSceneRegion(region.RID); }
    private void ReleaseNavigation()
    { foreach (var regions in _navigationCells.Values) foreach (var region in regions) NavigationServer.Service.ReleaseSceneRegion(region.RID); _navigationCells.Clear(); }
    private void MoveNavigationWorld()
    { var map = GetNavigationMap(); NavigationServer.Service.ValidateTileMap(map, Tree); foreach (var regions in _navigationCells.Values) foreach (var region in regions) NavigationServer.RegionSetMap(region.RID, map); }
    private void UpdateNavigationTransforms()
    { foreach (var regions in _navigationCells.Values) foreach (var region in regions) NavigationServer.RegionSetTransform(region.RID, GlobalTransform * region.CellTransform); }
    private void DrawNavigation()
    {
        if (!_navigationEnabled || _navigationVisibility == DebugVisibilityMode.ForceHide || _navigationVisibility == DebugVisibilityMode.Default && Tree?.DebugNavigationHint != true) return;
        foreach (var regions in _navigationCells.Values) foreach (var region in regions)
            {
                if (region.Polygon.IsDisposed) continue; var geometry = region.Polygon.Snapshot(); DrawSetTransformMatrix(region.CellTransform);
                foreach (var indices in geometry.Polygons) DrawColoredPolygon(indices.Select(i => geometry.Vertices[i]).ToArray(), new Color(.1f, .8f, .5f, .5f));
            }
    }
    internal Vector2[][] CaptureNavigationTraversable()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread(); var result = new List<Vector2[]>();
        if (!_enabled || !_navigationEnabled || _tileSet is not { IsDisposed: false } set) return [];
        foreach (var (coords, cell) in _cells)
        {
            if (EffectiveData(coords, cell) is not { IsDisposed: false } data) continue;
            for (var layer = 0; layer < Math.Min(set.NavigationLayers.Count, data.Navigation.Count); layer++)
            {
                if (data.GetNavigationPolygon(layer) is not { IsDisposed: false } polygon) continue;
                var outlines = polygon.GetOutlinesSnapshot(); var geometry = polygon.Snapshot();
                var contours = outlines.Length != 0 ? outlines : geometry.Polygons.Select(p => p.Select(i => geometry.Vertices[i]).ToArray()).ToArray();
                foreach (var outline in contours)
                {
                    var points = new Vector2[outline.Length]; for (var i = 0; i < points.Length; i++) points[i] = CellPoint(outline[i], cell.Alternative) + MapToLocal(coords);
                    if (((cell.Alternative & TileSetAtlasSource.TransformFlipH) != 0) ^ ((cell.Alternative & TileSetAtlasSource.TransformFlipV) != 0) ^ ((cell.Alternative & TileSetAtlasSource.TransformTranspose) != 0)) Array.Reverse(points);
                    result.Add(points);
                }
            }
        }
        return result.ToArray();
    }
    internal Vector2[][] CaptureNavigationContours(uint collisionMask)
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread(); var result = new List<Vector2[]>();
        if (!_enabled || !_collisionEnabled || _tileSet is not { IsDisposed: false } set) return [];
        foreach (var (coords, cell) in _cells)
        {
            if (EffectiveData(coords, cell) is not { IsDisposed: false } data) continue;
            for (var layer = 0; layer < Math.Min(set.GetPhysicsLayersCount(), data.Layers.Count); layer++) if ((set.GetPhysicsLayerCollisionLayer(layer) & collisionMask) != 0)
                    for (var index = 0; index < data.GetCollisionPolygonsCount(layer); index++)
                    {
                        var points = data.GetCollisionPolygonPoints(layer, index); if (points.Length < 3 || Geometry.DecomposePolygonInConvex(points).Length == 0) continue; var origin = MapToLocal(coords);
                        for (var i = 0; i < points.Length; i++) points[i] = CellPoint(points[i], cell.Alternative) + origin;
                        result.Add(points);
                    }
        }
        return result.ToArray();
    }
}
