namespace Electron2D;

public partial class TileMapLayer
{
    private int _renderingQuadrantSize = 16, _ySortOrigin;
    private bool _xDrawOrderReversed, _drawingDirty = true, _drawingYSorted;
    private readonly List<CanvasItem> _drawingItems = [];
    internal ShaderProgram? FindTileInstanceProgram(string name)
    {
        foreach (var item in _drawingItems) if (item.InstanceProgram is { } program && program.InstanceUniforms.ContainsKey(name)) return program;
        return null;
    }
    internal IEnumerable<ShaderProgram> TileInstancePrograms()
    {
        foreach (var item in _drawingItems) if (item.InstanceProgram is { } program) yield return program;
    }

    /// <summary>Gets or sets the positive side length of rendering quadrants in cells, independently of physical quadrants.</summary>
    /// <value>16 by default. Y sorting groups cells by their sorting anchor instead of this size.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than one.</exception>
    public int RenderingQuadrantSize { get { ThrowIfDisposed(); return _renderingQuadrantSize; } set { EnsureMutable(); if (value < 1) throw new ArgumentOutOfRangeException(nameof(value)); if (_renderingQuadrantSize == value) return; _renderingQuadrantSize = value; InvalidateTileDrawing(); Changed?.Invoke(); } }
    /// <summary>Gets or sets the layer's additional Y sorting anchor offset in scene units.</summary>
    /// <value>Zero by default; only affects Y-sorted tile grouping.</value>
    public int YSortOrigin { get { ThrowIfDisposed(); return _ySortOrigin; } set { EnsureMutable(); if (_ySortOrigin == value) return; _ySortOrigin = value; InvalidateTileDrawing(); Changed?.Invoke(); } }
    /// <summary>Gets or sets whether X cell ordering is reversed within a Y-sorted group.</summary>
    /// <value>False by default; ordinary quadrant cell order is unaffected.</value>
    public bool XDrawOrderReversed { get { ThrowIfDisposed(); return _xDrawOrderReversed; } set { EnsureMutable(); if (_xDrawOrderReversed == value) return; _xDrawOrderReversed = value; InvalidateTileDrawing(); Changed?.Invoke(); } }
    internal void DrawingYSortChanged(bool changed) { InvalidateTileDrawing(); if (changed) Changed?.Invoke(); }
    internal void InvalidateTileDrawing() { _drawingDirty = true; QueueRedraw(); }
    private void ReleaseDrawing()
    {
        foreach (var item in _drawingItems) if (!item.IsDisposed) item.ServerState!.Owner.ReleaseTileCanvasItem(item, this);
        _drawingItems.Clear(); _drawingDirty = true;
    }
    private CanvasItem? _drawingDiagnostics;
    private CanvasItem? DrawingDiagnosticsTarget()
    {
        if (RenderingServer.Service is not { } server) return this;
        var wanted = _collisionVisibility == DebugVisibilityMode.ForceShow || _collisionVisibility == DebugVisibilityMode.Default && Tree?.DebugCollisionsHint == true ||
            _navigationEnabled && (_navigationVisibility == DebugVisibilityMode.ForceShow || _navigationVisibility == DebugVisibilityMode.Default && Tree?.DebugNavigationHint == true);
        if (_drawingDiagnostics is not { IsDisposed: false })
        {
            if (!wanted) return null;
            _drawingDiagnostics = server.CreateTileCanvasItem(this); _drawingDiagnostics.ServerState!.Z = MaximumZIndex - 1; _drawingDiagnostics.ServerState.DrawIndex = int.MaxValue; server.AttachTileCanvasItem(_drawingDiagnostics, this);
        }
        _drawingDiagnostics.ClearServerCommands(); return _drawingDiagnostics;
    }
    private void ReleaseDrawingDiagnostics()
    { if (_drawingDiagnostics is { IsDisposed: false } item) item.ServerState!.Owner.ReleaseTileCanvasItem(item, this); _drawingDiagnostics = null; }
    private readonly record struct DrawingBucket(Vector2i Quadrant, float Y);
    private readonly record struct DrawingCell(Vector2i Coords, Cell Cell, TileSetAtlasSource Source, TileData Data, Texture Texture);
    private void PrepareTileDrawing()
    {
        if (RenderingServer.Service is null)
        {
            foreach (var pair in _cells)
                if (Resolve(pair.Value) is { } source && EffectiveData(pair.Key, pair.Value) is { IsDisposed: false } data && source.GetRuntimeTexture() is { IsDisposed: false } texture)
                    RecordDrawingCell(this, new(pair.Key, pair.Value, source, data, texture), Vector2.Zero);
            return;
        }
        var sorted = RenderYSort;
        if (!_drawingDirty && _drawingYSorted == sorted && (_drawingItems.Count == 0 || !_drawingItems[0].IsDisposed)) return;
        RebuildTileDrawing(sorted);
    }
    private void RebuildTileDrawing(bool sorted)
    {
        var server = RenderingServer.Service ?? throw new InvalidOperationException("Tile drawing requires the active renderer.");
        var buckets = new Dictionary<DrawingBucket, (Vector2 Anchor, List<DrawingCell> Cells)>();
        foreach (var pair in _cells)
        {
            if (Resolve(pair.Value) is not { } source || EffectiveData(pair.Key, pair.Value) is not { IsDisposed: false } data || source.GetRuntimeTexture() is not { IsDisposed: false } texture) continue;
            DrawingBucket key; Vector2 anchor;
            if (sorted)
            {
                var y = MapToLocal(pair.Key).Y + data.YSortOrigin + (float)_ySortOrigin;
                var quantized = MathF.Truncate(y * 100);
                if (!float.IsFinite(quantized)) throw new OverflowException("The tile sorting anchor is not representable.");
                key = new(Vector2i.Zero, quantized); anchor = new Vector2(0, y);
            }
            else
            {
                var q = new Vector2i(FloorDivide(pair.Key.X, _renderingQuadrantSize), FloorDivide(pair.Key.Y, _renderingQuadrantSize));
                key = new(q, 0); anchor = MapToLocal(new Vector2i(checked(q.X * _renderingQuadrantSize), checked(q.Y * _renderingQuadrantSize)));
            }
            if (!buckets.TryGetValue(key, out var bucket)) { bucket = (anchor, []); buckets.Add(key, bucket); }
            bucket.Cells.Add(new(pair.Key, pair.Value, source, data, texture));
        }
        var ordered = buckets.ToArray();
        Array.Sort(ordered, (a, b) =>
        {
            if (sorted) return a.Key.Y.CompareTo(b.Key.Y);
            var left = MapToLocal(a.Key.Quadrant); var right = MapToLocal(b.Key.Quadrant);
            return left.Y == right.Y ? right.X.CompareTo(left.X) : left.Y.CompareTo(right.Y);
        });
        var prepared = new List<CanvasItem>();
        try
        {
            foreach (var bucket in ordered)
            {
                bucket.Value.Cells.Sort((a, b) => a.Coords.X == b.Coords.X ? a.Coords.Y.CompareTo(b.Coords.Y) : sorted && _xDrawOrderReversed ? b.Coords.X.CompareTo(a.Coords.X) : a.Coords.X.CompareTo(b.Coords.X));
                CanvasItem? item = null; Material? material = null; var z = 0;
                foreach (var cell in bucket.Value.Cells)
                {
                    if (item is null || !ReferenceEquals(material, cell.Data.Material) || z != cell.Data.ZIndex)
                    {
                        item = server.CreateTileCanvasItem(this); prepared.Add(item); material = cell.Data.Material; z = cell.Data.ZIndex;
                        item.Material = material; item.UseParentMaterial = material is null;
                        var state = item.ServerState!; state.Transform = new Transform(0, bucket.Value.Anchor); state.Z = z; state.DrawIndex = int.MinValue + prepared.Count - 1;
                    }
                    RecordDrawingCell(item, cell, bucket.Value.Anchor);
                }
            }
            foreach (var item in prepared) server.AttachTileCanvasItem(item, this);
        }
        catch { foreach (var item in prepared) if (!item.IsDisposed) server.ReleaseTileCanvasItem(item, this); throw; }
        ReleaseDrawing(); _drawingItems.AddRange(prepared); _drawingYSorted = sorted; _drawingDirty = false;
    }
    private void RecordDrawingCell(CanvasItem item, DrawingCell draw, Vector2 anchor)
    {
        var cell = draw.Cell; var data = draw.Data; var source = draw.Source;
        var flags = (data.FlipH ? TileSetAtlasSource.TransformFlipH : 0) | (data.FlipV ? TileSetAtlasSource.TransformFlipV : 0) | (data.Transpose ? TileSetAtlasSource.TransformTranspose : 0);
        var x = CellPoint(CellPoint(Vector2.Right, flags), cell.Alternative); var y = CellPoint(CellPoint(Vector2.Down, flags), cell.Alternative);
        using var scope = item.StartServerDrawing();
        item.DrawSetTransformMatrix(new Transform(x, y, MapToLocal(draw.Coords) - anchor - CellPoint(data.TextureOrigin, cell.Alternative)));
        var count = source.GetTileAnimationFramesCount(cell.Atlas); double total = 0;
        for (var frame = 0; frame < count; frame++) total += source.GetTileAnimationFrameDuration(cell.Atlas, frame);
        var speed = source.GetTileAnimationSpeed(cell.Atlas); var length = total / speed;
        var offset = source.GetTileAnimationMode(cell.Atlas) == TileSetAtlasSource.TileAnimationMode.RandomStartTimes ? TileAnimationPhase(draw.Coords) * length : 0; double elapsed = 0;
        for (var frame = 0; frame < count; frame++)
        {
            var duration = source.GetTileAnimationFrameDuration(cell.Atlas, frame); if (count > 1) item.DrawAnimationSlice(length, elapsed / speed, (elapsed + duration) / speed, offset);
            var region = source.GetRuntimeTileTextureRegion(cell.Atlas, frame); var size = (Vector2)region.Size;
            item.DrawTextureRectRegion(draw.Texture, new Rect2(-size * .5f, size), new Rect2(region.Position, region.Size), data.Modulate, clipUV: _tileSet!.UVClipping); elapsed += duration;
        }
        if (count > 1) item.DrawEndAnimation();
    }
    private IEnumerable<PropertyDescriptor> DrawingPolicyProperties()
    {
        yield return new PropertyDescriptor<TileMapLayer, int>(nameof(RenderingQuadrantSize), n => n.RenderingQuadrantSize, (n, v) => n.RenderingQuadrantSize = v, _ => 16, stored: true);
        yield return new PropertyDescriptor<TileMapLayer, int>(nameof(YSortOrigin), n => n.YSortOrigin, (n, v) => n.YSortOrigin = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<TileMapLayer, bool>(nameof(XDrawOrderReversed), n => n.XDrawOrderReversed, (n, v) => n.XDrawOrderReversed = v, _ => false, stored: true);
    }
}
