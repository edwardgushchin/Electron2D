namespace Electron2D;

/// <summary>Draws a square atlas tile layer and generates tile-owned collision bodies in its selected world.</summary>
/// <remarks>Cell and resource edits are batched until the tree flushes deferred work or UpdateInternals is called.
/// Generated bodies do not appear as child nodes. Queries and contacts report this layer as their collider object.</remarks>
public partial class TileMapLayer : Entity
{
    /// <summary>Selects collision overlay visibility independently of ordinary tile drawing.</summary>
    public enum DebugVisibilityMode
    {
        /// <summary>Follows the scene tree's collision hint.</summary>
        Default = 0,
        /// <summary>Draws collision geometry whenever this layer is visible.</summary>
        ForceShow = 1,
        /// <summary>Hides collision geometry even when the scene hint is enabled.</summary>
        ForceHide = 2
    }
    private readonly record struct Cell(int SourceID, Vector2i Atlas, int Alternative);
    private readonly Dictionary<Vector2i, Cell> _cells = [];
    private readonly Dictionary<Vector2i, TileData> _runtimeData = [];
    private readonly HashSet<Vector2i> _dirtyCells = [];
    private TileSet? _tileSet;
    private bool _enabled = true, _collisionEnabled = true, _kinematic, _allDirty = true, _queued, _updating;
    private int _quadrantSize = 16;
    private DebugVisibilityMode _collisionVisibility;
    private readonly Action _deferredUpdate;

    /// <summary>Creates an empty enabled tile layer.</summary>
    public TileMapLayer() { NotifyTransformChanges = true; _deferredUpdate = DeferredUpdate; }
    /// <summary>Occurs after cell, resource or layer settings change; multiple changes may precede one rebuild.</summary>
    public event Action? Changed;
    /// <summary>Gets or sets the borrowed tile set used for rendering and collision.</summary>
    public TileSet? TileSet
    {
        get { ThrowIfDisposed(); return _tileSet; }
        set
        {
            EnsureMutable(); if (value is not null) ObjectDisposedException.ThrowIf(value.IsDisposed, value);
            if (ReferenceEquals(_tileSet, value)) return;
            DetachSet(); _tileSet = value;
            if (value is not null) { value.Changed += SetChanged; value.Disposed += SetDisposed; }
            MarkAll();
        }
    }
    /// <summary>Gets or sets whether tiles render and generate collision bodies.</summary>
    public bool Enabled { get { ThrowIfDisposed(); return _enabled; } set { EnsureMutable(); if (_enabled == value) return; _enabled = value; MarkAll(); } }
    /// <summary>Gets or sets whether this layer generates physics bodies.</summary>
    public bool CollisionEnabled { get { ThrowIfDisposed(); return _collisionEnabled; } set { EnsureMutable(); if (_collisionEnabled == value) return; _collisionEnabled = value; MarkAll(); } }
    /// <summary>Gets or sets the positive number of cells along each physical quadrant side, 16 by default.</summary>
    public int PhysicsQuadrantSize { get { ThrowIfDisposed(); return _quadrantSize; } set { EnsureMutable(); if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_quadrantSize == value) return; _quadrantSize = value; MarkAll(); } }
    /// <summary>Gets or sets whether generated bodies use kinematic instead of static motion.</summary>
    public bool UseKinematicBodies { get { ThrowIfDisposed(); return _kinematic; } set { EnsureMutable(); if (_kinematic == value) return; _kinematic = value; MarkAll(); } }
    /// <summary>Gets or sets the policy for drawing tile collision geometry.</summary>
    public DebugVisibilityMode CollisionVisibilityMode
    { get { ThrowIfDisposed(); return _collisionVisibility; } set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_collisionVisibility == value) return; _collisionVisibility = value; QueueRedraw(); } }
    /// <summary>Assigns a tile identity; any invalid identity component erases the cell.</summary>
    /// <param name="coords">Layer cell coordinates.</param>
    /// <param name="sourceID">Source identity; minus one erases or acts as a query wildcard where documented.</param>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    /// <param name="alternativeTile">Alternative identity, with optional cell transform flags where supported.</param>
    public void SetCell(Vector2i coords, int sourceID = -1, Vector2i? atlasCoords = null, int alternativeTile = 0)
    {
        EnsureMutable(); var atlas = atlasCoords ?? new Vector2i(-1, -1);
        if (sourceID == -1 || atlas == new Vector2i(-1, -1) || alternativeTile == -1) { EraseCell(coords); return; }
        if (sourceID < 0 || sourceID >= ushort.MaxValue || atlas.X < 0 || atlas.Y < 0 || atlas.X >= ushort.MaxValue || atlas.Y >= ushort.MaxValue || alternativeTile < 0 || alternativeTile >= ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(sourceID), "Tile identities must fit unsigned 16-bit storage without the invalid sentinel.");
        var cell = new Cell(sourceID, atlas, alternativeTile); if (_cells.TryGetValue(coords, out var old) && old == cell) return;
        _cells[coords] = cell; MarkCell(coords);
    }
    /// <summary>Erases a cell if present.</summary>
    /// <param name="coords">Layer cell coordinates.</param>
    public void EraseCell(Vector2i coords) { EnsureMutable(); if (_cells.Remove(coords)) MarkCell(coords); }
    /// <summary>Erases every authored cell.</summary>
    public void Clear() { EnsureMutable(); if (_cells.Count == 0) return; foreach (var coord in _cells.Keys) _dirtyCells.Add(coord); _cells.Clear(); MarkAll(); }
    /// <summary>Removes cells whose source, atlas coordinates or alternative no longer exists.</summary>
    public void FixInvalidTiles() { EnsureMutable(); foreach (var coord in _cells.Keys.ToArray()) if (Resolve(_cells[coord]) is null) EraseCell(coord); }
    /// <summary>Returns a cell's source identity, or minus one for an empty cell.</summary>
    /// <param name="coords">Layer cell coordinates.</param>
    /// <returns>Returns a cell's source identity, or minus one for an empty cell.</returns>
    public int GetCellSourceID(Vector2i coords) { ThrowIfDisposed(); return _cells.TryGetValue(coords, out var c) ? c.SourceID : -1; }
    /// <summary>Returns a cell's atlas coordinates, or (-1,-1) for an empty cell.</summary>
    /// <param name="coords">Layer cell coordinates.</param>
    /// <returns>Returns a cell's atlas coordinates, or (-1,-1) for an empty cell.</returns>
    public Vector2i GetCellAtlasCoords(Vector2i coords) { ThrowIfDisposed(); return _cells.TryGetValue(coords, out var c) ? c.Atlas : new(-1, -1); }
    /// <summary>Returns a cell's alternative identity including transform flags, or minus one when empty.</summary>
    /// <param name="coords">Layer cell coordinates.</param>
    /// <returns>Returns a cell's alternative identity including transform flags, or minus one when empty.</returns>
    public int GetCellAlternativeTile(Vector2i coords) { ThrowIfDisposed(); return _cells.TryGetValue(coords, out var c) ? c.Alternative : -1; }
    /// <summary>Returns borrowed authored tile data, or null if the cell cannot be resolved.</summary>
    /// <param name="coords">Layer cell coordinates.</param>
    /// <returns>Returns borrowed authored tile data, or null if the cell cannot be resolved.</returns>
    public TileData? GetCellTileData(Vector2i coords) { ThrowIfDisposed(); return _cells.TryGetValue(coords, out var c) ? Resolve(c)?.GetTileData(c.Atlas, c.Alternative) : null; }
    /// <summary>Returns whether the cell's transform flag reflects its horizontal axis.</summary>
    /// <param name="coords">Layer cell coordinates.</param>
    /// <returns>Returns whether the cell's transform flag reflects its horizontal axis.</returns>
    public bool IsCellFlippedH(Vector2i coords) => CellFlag(coords, TileSetAtlasSource.TransformFlipH);
    /// <summary>Returns whether the cell's transform flag reflects its vertical axis.</summary>
    /// <param name="coords">Layer cell coordinates.</param>
    /// <returns>Returns whether the cell's transform flag reflects its vertical axis.</returns>
    public bool IsCellFlippedV(Vector2i coords) => CellFlag(coords, TileSetAtlasSource.TransformFlipV);
    /// <summary>Returns whether the cell's transform flag exchanges its axes.</summary>
    /// <param name="coords">Layer cell coordinates.</param>
    /// <returns>Returns whether the cell's transform flag exchanges its axes.</returns>
    public bool IsCellTransposed(Vector2i coords) => CellFlag(coords, TileSetAtlasSource.TransformTranspose);
    /// <summary>Returns a caller-owned array of occupied cell coordinates.</summary>
    /// <returns>Returns a caller-owned array of occupied cell coordinates.</returns>
    public Vector2i[] GetUsedCells() { ThrowIfDisposed(); return _cells.Keys.ToArray(); }
    /// <summary>Returns occupied coordinates matching each specified identity; minus one components are wildcards.</summary>
    /// <param name="sourceID">Source identity; minus one erases or acts as a query wildcard where documented.</param>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    /// <param name="alternativeTile">Alternative identity, with optional cell transform flags where supported.</param>
    /// <returns>Returns occupied coordinates matching each specified identity; minus one components are wildcards.</returns>
    public Vector2i[] GetUsedCellsByID(int sourceID = -1, Vector2i? atlasCoords = null, int alternativeTile = -1)
    {
        ThrowIfDisposed(); var atlas = atlasCoords ?? new Vector2i(-1, -1);
        return _cells.Where(p => (sourceID == -1 || sourceID == p.Value.SourceID) && (atlas == new Vector2i(-1, -1) || atlas == p.Value.Atlas) && (alternativeTile == -1 || alternativeTile == p.Value.Alternative)).Select(p => p.Key).ToArray();
    }
    /// <summary>Returns the cell-coordinate bounding rectangle, or an empty rectangle for an empty layer.</summary>
    /// <returns>Returns the cell-coordinate bounding rectangle, or an empty rectangle for an empty layer.</returns>
    public Rect2i GetUsedRect()
    {
        ThrowIfDisposed(); if (_cells.Count == 0) return default;
        var min = new Vector2i(int.MaxValue, int.MaxValue); var max = new Vector2i(int.MinValue, int.MinValue);
        foreach (var c in _cells.Keys) { min = min.Min(c); max = max.Max(c); }
        return new(min, new(checked(max.X - min.X + 1), checked(max.Y - min.Y + 1)));
    }
    /// <summary>Returns the center of a square cell in layer-local scene units.</summary>
    /// <param name="mapPosition">Square-grid cell coordinates.</param>
    /// <returns>Returns the center of a square cell in layer-local scene units.</returns>
    public Vector2 MapToLocal(Vector2i mapPosition)
    { ThrowIfDisposed(); var size = RequireSet().TileSize; return new((mapPosition.X + .5f) * size.X, (mapPosition.Y + .5f) * size.Y); }
    /// <summary>Returns the square cell containing a finite layer-local position, using floor for negative coordinates.</summary>
    /// <param name="localPosition">Finite layer-local position in scene units.</param>
    /// <returns>Returns the square cell containing a finite layer-local position, using floor for negative coordinates.</returns>
    public Vector2i LocalToMap(Vector2 localPosition)
    {
        ThrowIfDisposed(); if (!localPosition.IsFinite()) throw new ArgumentOutOfRangeException(nameof(localPosition)); var size = RequireSet().TileSize;
        return new(checked((int)Math.Floor((double)localPosition.X / size.X)), checked((int)Math.Floor((double)localPosition.Y / size.Y)));
    }
    /// <summary>Tests whether a body RID currently belongs to this layer.</summary>
    /// <param name="body">Borrowed generated body identity; do not release it separately.</param>
    /// <returns>Tests whether a body RID currently belongs to this layer.</returns>
    public bool HasBodyRID(RID body) { ThrowIfDisposed(); return _bodyCoords.ContainsKey(body); }
    /// <summary>Returns physical quadrant coordinates for a generated body; quadrant size one identifies a cell.</summary>
    /// <exception cref="ArgumentException">The RID is not a current body of this layer.</exception>
    /// <param name="body">Borrowed generated body identity; do not release it separately.</param>
    /// <returns>Returns physical quadrant coordinates for a generated body; quadrant size one identifies a cell.</returns>
    public Vector2i GetCoordsForBodyRID(RID body) { ThrowIfDisposed(); return _bodyCoords.TryGetValue(body, out var c) ? c : throw new ArgumentException("The body does not belong to this layer.", nameof(body)); }
    /// <summary>Schedules recreation of per-cell runtime tile data.</summary>
    public void NotifyRuntimeTileDataUpdate() { EnsureMutable(); MarkAll(); }
    /// <summary>Chooses which visible enabled cells need an owned runtime copy of tile data.</summary>
    /// <param name="coords">Layer cell coordinates.</param>
    /// <returns>Chooses which visible enabled cells need an owned runtime copy of tile data.</returns>
    protected virtual bool UseTileDataRuntimeUpdate(Vector2i coords) => false;
    /// <summary>Modifies a temporary cell-specific tile-data copy before rendering and physics publication.</summary>
    /// <param name="coords">Layer cell coordinates.</param>
    /// <param name="tileData">Borrowed runtime copy, valid until rebuild or cleanup.</param>
    protected virtual void TileDataRuntimeUpdate(Vector2i coords, TileData tileData) { }
    /// <summary>Observes batched modified coordinates; cleanup is true when disabled, hidden, detached or without a tile set.</summary>
    /// <param name="coords">Layer cell coordinates.</param>
    /// <param name="forcedCleanup">Whether drawing and runtime tile data are being cleaned up.</param>
    protected virtual void UpdateCells(Vector2i[] coords, bool forcedCleanup) { }

    private TileSet RequireSet() => _tileSet ?? throw new InvalidOperationException("The layer has no tile set.");
    private TileSetAtlasSource? Resolve(Cell cell) => _tileSet is { IsDisposed: false } && _tileSet.HasSource(cell.SourceID) && _tileSet.GetSource(cell.SourceID) is TileSetAtlasSource source && source.HasAlternativeTile(cell.Atlas, cell.Alternative) ? source : null;
    private bool CellFlag(Vector2i coords, int bit)
    {
        ThrowIfDisposed(); return _cells.TryGetValue(coords, out var c) && (c.Alternative & bit) != 0;
    }
    private void MarkCell(Vector2i coords) { _dirtyCells.Add(coords); Schedule(); Changed?.Invoke(); }
    private void MarkAll() { _allDirty = true; Schedule(); Changed?.Invoke(); }
    private void Schedule() { QueueRedraw(); if (_queued || !IsInsideTree) return; _queued = true; Tree!.Defer(_deferredUpdate); }
    private void DeferredUpdate() { _queued = false; if (!IsDisposed && IsInsideTree) UpdateInternals(); }
    private void SetChanged(Resource _) => MarkAll();
    private void SetDisposed(ElectronObject _) { DetachSet(); MarkAll(); }
    private void DetachSet() { if (_tileSet is not null) { _tileSet.Changed -= SetChanged; _tileSet.Disposed -= SetDisposed; _tileSet = null; } }
    /// <inheritdoc />
    protected override void OnEnterTree() { base.OnEnterTree(); MarkAll(); }
    /// <inheritdoc />
    protected override void OnExitTree() { try { ReleasePhysics(); ClearRuntimeData(); _allDirty = true; UpdateCells(_cells.Keys.ToArray(), true); } finally { base.OnExitTree(); } }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationWorldChanged && IsInsideTree) MoveWorld();
        else if (what == NotificationTransformChanged && IsInsideTree) UpdateBodyTransforms();
        else if (what == NotificationVisibilityChanged) MarkAll();
        else if (what is NotificationEnterCanvas or NotificationExitCanvas) UpdateCanvas(what == NotificationExitCanvas ? 0 : GetCanvasLayerNode()?.InstanceID ?? 0);
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() { Tree?.EnsurePhysicsParticipationChange(releasing: true); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        try { base.Dispose(disposing); }
        finally { if (disposing) { DetachSet(); ReleasePhysics(); ClearRuntimeData(); Changed = null; } }
    }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateTileLayer;
    private static Node CreateTileLayer() => new TileMapLayer();
    private void ClearRuntimeData() { foreach (var data in _runtimeData.Values) data.Release(); _runtimeData.Clear(); }
    private TileData? EffectiveData(Vector2i coords, Cell cell) => _runtimeData.TryGetValue(coords, out var runtime) ? runtime : Resolve(cell)?.GetTileData(cell.Atlas, cell.Alternative);
}
