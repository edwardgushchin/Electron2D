namespace Electron2D;

/// <summary>Defines static texture-atlas tiles and their owned alternatives.</summary>
/// <remarks>Texture and material resources are borrowed. Tile data is invalidated by removal or source disposal.
/// Texture changes preserve authored tiles even when their regions no longer fit.</remarks>
public sealed class TileSetAtlasSource : TileSetSource
{
    /// <summary>Cell alternative flag for horizontal reflection.</summary>
    public const int TransformFlipH = 4096;
    /// <summary>Cell alternative flag for vertical reflection.</summary>
    public const int TransformFlipV = 8192;
    /// <summary>Cell alternative flag for exchanging axes before reflection.</summary>
    public const int TransformTranspose = 16384;
    internal const int TransformMask = TransformFlipH | TransformFlipV | TransformTranspose;
    private sealed class Tile
    {
        internal Vector2i Size = Vector2i.One;
        internal readonly SortedDictionary<int, TileData> Alternatives = [];
    }
    private readonly Dictionary<Vector2i, Tile> _tiles = [];
    private Texture? _texture;
    private Vector2i _margins, _separation, _regionSize = new(16, 16);
    private int _physicsLayers;

    /// <summary>Creates an empty source with 16 by 16 pixel texture regions.</summary>
    public TileSetAtlasSource() { }
    /// <summary>Gets or sets the borrowed atlas texture; null has an empty atlas grid.</summary>
    public Texture? Texture
    {
        get { ThrowIfDisposed(); return _texture; }
        set
        {
            ThrowIfDisposed(); if (value is not null) ObjectDisposedException.ThrowIf(value.IsDisposed, value);
            if (ReferenceEquals(value, _texture)) return;
            DetachTexture(); _texture = value;
            if (value is not null) { value.Changed += TextureChanged; value.Disposed += TextureDisposed; }
            EmitChanged();
        }
    }
    /// <summary>Gets or sets the nonnegative top and left texture margins in pixels.</summary>
    public Vector2i Margins { get { ThrowIfDisposed(); return _margins; } set { ThrowIfDisposed(); Nonnegative(value); if (_margins == value) return; _margins = value; EmitChanged(); } }
    /// <summary>Gets or sets nonnegative spacing between atlas cells in pixels.</summary>
    public Vector2i Separation { get { ThrowIfDisposed(); return _separation; } set { ThrowIfDisposed(); Nonnegative(value); CheckSum(value, _regionSize); if (_separation == value) return; _separation = value; EmitChanged(); } }
    /// <summary>Gets or sets positive atlas cell dimensions in pixels.</summary>
    public Vector2i TextureRegionSize { get { ThrowIfDisposed(); return _regionSize; } set { ThrowIfDisposed(); TileSet.Positive(value); CheckSum(value, _separation); if (_regionSize == value) return; _regionSize = value; EmitChanged(); } }
    /// <summary>Returns the number of complete atlas cells inside the current texture.</summary>
    /// <returns>Returns the number of complete atlas cells inside the current texture.</returns>
    public Vector2i GetAtlasGridSize()
    {
        ThrowIfDisposed(); if (_texture is null) return default;
        var size = _texture.GetSize() - (Vector2)_margins;
        if (size.X < _regionSize.X || size.Y < _regionSize.Y) return default;
        return new(1 + (int)((size.X - _regionSize.X) / (_regionSize.X + _separation.X)), 1 + (int)((size.Y - _regionSize.Y) / (_regionSize.Y + _separation.Y)));
    }
    /// <summary>Creates a base tile occupying a positive rectangular region of currently unused atlas cells.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    /// <param name="size">Positive atlas-cell size, or null for one cell.</param>
    public void CreateTile(Vector2i atlasCoords, Vector2i? size = null)
    {
        ThrowIfDisposed(); var extent = size ?? Vector2i.One; TileSet.Positive(extent); Nonnegative(atlasCoords);
        if (!HasRoom(atlasCoords, extent, new(-1, -1))) throw new ArgumentException("The atlas region is occupied or outside the texture.", nameof(atlasCoords));
        var tile = new Tile { Size = extent }; tile.Alternatives.Add(0, NewData(false)); _tiles.Add(atlasCoords, tile); Change();
    }
    /// <summary>Removes a tile and disposes all its owned data alternatives.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    public void RemoveTile(Vector2i atlasCoords)
    {
        ThrowIfDisposed(); if (!_tiles.Remove(atlasCoords, out var tile)) throw new KeyNotFoundException("Unknown atlas tile.");
        foreach (var data in tile.Alternatives.Values) ReleaseData(data); Change();
    }
    /// <summary>Returns the tile occupying an atlas cell, or (-1,-1) when unused.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    /// <returns>Returns the tile occupying an atlas cell, or (-1,-1) when unused.</returns>
    public Vector2i GetTileAtCoords(Vector2i atlasCoords)
    {
        ThrowIfDisposed(); foreach (var pair in _tiles) if (Contains(pair.Key, pair.Value.Size, atlasCoords)) return pair.Key; return new(-1, -1);
    }
    /// <summary>Returns the tile's dimensions in atlas cells.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    /// <returns>Returns the tile's dimensions in atlas cells.</returns>
    public Vector2i GetTileSizeInAtlas(Vector2i atlasCoords) => GetTile(atlasCoords).Size;
    /// <summary>Returns a static tile's pixel region; only frame zero exists.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    /// <param name="frame">Static frame index; only zero is currently available.</param>
    /// <returns>Returns a static tile's pixel region; only frame zero exists.</returns>
    public Rect2i GetTileTextureRegion(Vector2i atlasCoords, int frame = 0)
    {
        var tile = GetTile(atlasCoords); if (frame != 0) throw new ArgumentOutOfRangeException(nameof(frame));
        return new(new(checked(_margins.X + atlasCoords.X * (_regionSize.X + _separation.X)), checked(_margins.Y + atlasCoords.Y * (_regionSize.Y + _separation.Y))),
            new(checked(_regionSize.X * tile.Size.X + _separation.X * (tile.Size.X - 1)), checked(_regionSize.Y * tile.Size.Y + _separation.Y * (tile.Size.Y - 1))));
    }
    /// <summary>Moves or resizes a tile while retaining its alternatives; null leaves that component unchanged.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    /// <param name="newAtlasCoords">Replacement coordinates, or null or (-1,-1) to retain them.</param>
    /// <param name="newSize">Replacement positive size, or null or (-1,-1) to retain it.</param>
    public void MoveTileInAtlas(Vector2i atlasCoords, Vector2i? newAtlasCoords = null, Vector2i? newSize = null)
    {
        var tile = GetTile(atlasCoords); var coords = newAtlasCoords ?? atlasCoords; var size = newSize ?? tile.Size;
        if (coords == new Vector2i(-1, -1)) coords = atlasCoords; if (size == new Vector2i(-1, -1)) size = tile.Size;
        Nonnegative(coords); TileSet.Positive(size);
        if (!HasRoom(coords, size, atlasCoords)) throw new ArgumentException("The atlas region is occupied or outside the texture.");
        _tiles.Remove(atlasCoords); tile.Size = size; _tiles.Add(coords, tile); Change();
    }
    /// <summary>Tests whether any authored tile extends outside the current texture.</summary>
    /// <returns>Tests whether any authored tile extends outside the current texture.</returns>
    public bool HasTilesOutsideTexture() { ThrowIfDisposed(); foreach (var pair in _tiles) if (!HasRoom(pair.Key, pair.Value.Size, pair.Key)) return true; return false; }
    /// <summary>Removes tiles extending outside the current texture.</summary>
    public void ClearTilesOutsideTexture() { ThrowIfDisposed(); foreach (var coords in _tiles.Keys.ToArray()) if (!HasRoom(coords, _tiles[coords].Size, coords)) RemoveTile(coords); }
    /// <summary>Adds an alternative with empty physics data and returns its nonzero identity.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    /// <param name="alternativeIDOverride">Nonzero alternative identity without transform bits, or minus one for automatic selection.</param>
    /// <returns>Adds an alternative with empty physics data and returns its nonzero identity.</returns>
    public int CreateAlternativeTile(Vector2i atlasCoords, int alternativeIDOverride = -1)
    {
        var tile = GetTile(atlasCoords); var id = alternativeIDOverride == -1 ? GetNextAlternativeTileID(atlasCoords) : alternativeIDOverride;
        AlternativeID(id); if (tile.Alternatives.ContainsKey(id)) throw new ArgumentException("Alternative identity is occupied.", nameof(alternativeIDOverride));
        tile.Alternatives.Add(id, NewData(true)); Change(); return id;
    }
    /// <summary>Returns the first free alternative identity without transform bits.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    /// <returns>Returns the first free alternative identity without transform bits.</returns>
    public int GetNextAlternativeTileID(Vector2i atlasCoords)
    { var tile = GetTile(atlasCoords); for (var id = 1; id < TransformFlipH; id++) if (!tile.Alternatives.ContainsKey(id)) return id; throw new InvalidOperationException("Alternative identities are exhausted."); }
    /// <summary>Removes a nonbase alternative and invalidates its borrowed data.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    /// <param name="alternativeTile">Alternative identity, with optional cell transform flags where supported.</param>
    public void RemoveAlternativeTile(Vector2i atlasCoords, int alternativeTile)
    {
        var tile = GetTile(atlasCoords); var id = alternativeTile & ~TransformMask; AlternativeID(id);
        if (!tile.Alternatives.Remove(id, out var data)) throw new KeyNotFoundException("Unknown alternative."); ReleaseData(data); Change();
    }
    /// <summary>Renames a nonbase alternative to an unoccupied nonzero identity.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    /// <param name="alternativeTile">Alternative identity, with optional cell transform flags where supported.</param>
    /// <param name="newID">Unoccupied nonzero alternative identity without transform bits.</param>
    public void SetAlternativeTileID(Vector2i atlasCoords, int alternativeTile, int newID)
    {
        var tile = GetTile(atlasCoords); var id = alternativeTile & ~TransformMask; AlternativeID(id); AlternativeID(newID);
        if (id == newID) return; if (tile.Alternatives.ContainsKey(newID)) throw new ArgumentException("Alternative identity is occupied.", nameof(newID));
        var data = tile.Alternatives[id]; tile.Alternatives.Remove(id); tile.Alternatives.Add(newID, data); Change();
    }
    /// <summary>Returns borrowed data for an alternative, ignoring cell transform flags.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    /// <param name="alternativeTile">Alternative identity, with optional cell transform flags where supported.</param>
    /// <returns>Returns borrowed data for an alternative, ignoring cell transform flags.</returns>
    public TileData GetTileData(Vector2i atlasCoords, int alternativeTile) => GetTile(atlasCoords).Alternatives[alternativeTile & ~TransformMask];
    /// <inheritdoc />
    public override int GetTilesCount() { ThrowIfDisposed(); return _tiles.Count; }
    /// <inheritdoc />
    public override Vector2i GetTileID(int index) { ThrowIfDisposed(); return SortedCoords()[index]; }
    /// <inheritdoc />
    public override bool HasTile(Vector2i atlasCoords) { ThrowIfDisposed(); return _tiles.ContainsKey(atlasCoords); }
    /// <inheritdoc />
    public override int GetAlternativeTilesCount(Vector2i atlasCoords) { ThrowIfDisposed(); return _tiles.TryGetValue(atlasCoords, out var tile) ? tile.Alternatives.Count : -1; }
    /// <inheritdoc />
    public override int GetAlternativeTileID(Vector2i atlasCoords, int index) => GetTile(atlasCoords).Alternatives.Keys.ElementAt(index);
    /// <inheritdoc />
    public override bool HasAlternativeTile(Vector2i atlasCoords, int alternativeTile)
    { ThrowIfDisposed(); return _tiles.TryGetValue(atlasCoords, out var tile) && tile.Alternatives.TryGetValue(alternativeTile & ~TransformMask, out var data) && !data.IsDisposed; }

    private Tile GetTile(Vector2i coords) { ThrowIfDisposed(); return _tiles[coords]; }
    private Vector2i[] SortedCoords() => _tiles.Keys.OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();
    private static void AlternativeID(int id) { if (id <= 0 || id >= TransformFlipH) throw new ArgumentOutOfRangeException(nameof(id)); }
    private static void Nonnegative(Vector2i value) { if (value.X < 0 || value.Y < 0) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void CheckSum(Vector2i a, Vector2i b) { _ = checked(a.X + b.X); _ = checked(a.Y + b.Y); }
    private static bool Contains(Vector2i origin, Vector2i size, Vector2i point) => point.X >= origin.X && point.Y >= origin.Y && (long)point.X < (long)origin.X + size.X && (long)point.Y < (long)origin.Y + size.Y;
    private bool HasRoom(Vector2i coords, Vector2i size, Vector2i ignored)
    {
        var grid = GetAtlasGridSize(); if ((long)coords.X + size.X > grid.X || (long)coords.Y + size.Y > grid.Y) return false;
        foreach (var pair in _tiles)
            if (pair.Key != ignored && coords.X < (long)pair.Key.X + pair.Value.Size.X && pair.Key.X < (long)coords.X + size.X && coords.Y < (long)pair.Key.Y + pair.Value.Size.Y && pair.Key.Y < (long)coords.Y + size.Y) return false;
        return true;
    }
    private TileData NewData(bool alternative) { var data = new TileData(alternative); data.ResizeLayers(_physicsLayers); data.Changed += Change; return data; }
    private void ReleaseData(TileData data) { data.Changed -= Change; data.Release(); }
    private void Change() { NotifyPropertyListChanged(); EmitChanged(); }
    private void TextureChanged(Resource _) => EmitChanged();
    private void TextureDisposed(ElectronObject _) { DetachTexture(); EmitChanged(); }
    private void DetachTexture() { if (_texture is not null) { _texture.Changed -= TextureChanged; _texture.Disposed -= TextureDisposed; _texture = null; } }
    internal override void ResizePhysicsLayers(int count) { _physicsLayers = count; foreach (var tile in _tiles.Values) foreach (var data in tile.Alternatives.Values) data.ResizeLayers(count); }
    internal override void InsertPhysicsLayer(int index) { _physicsLayers++; foreach (var tile in _tiles.Values) foreach (var data in tile.Alternatives.Values) data.Layers.Insert(index, new()); }
    internal override void RemovePhysicsLayer(int index) { _physicsLayers--; foreach (var tile in _tiles.Values) foreach (var data in tile.Alternatives.Values) data.Layers.RemoveAt(index); }
    internal override void MovePhysicsLayer(int from, int to) { foreach (var tile in _tiles.Values) foreach (var data in tile.Alternatives.Values) TileSet.Move(data.Layers, from, to); }
    private void ClearTiles() { foreach (var tile in _tiles.Values) foreach (var data in tile.Alternatives.Values) ReleaseData(data); _tiles.Clear(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { DetachTexture(); ClearTiles(); } base.Dispose(disposing); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new TileSetAtlasSource();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    {
        var copy = (TileSetAtlasSource)target; copy.ClearTiles(); copy._physicsLayers = _physicsLayers;
        copy._margins = _margins; copy._separation = _separation; copy._regionSize = _regionSize; copy.Texture = deep ? (Texture?)duplicate(_texture) : _texture;
        foreach (var pair in _tiles)
        {
            var tile = new Tile { Size = pair.Value.Size };
            foreach (var alt in pair.Value.Alternatives) { var data = alt.Value.Copy(); data.Changed += copy.Change; tile.Alternatives.Add(alt.Key, data); }
            copy._tiles.Add(pair.Key, tile);
        }
    }
    private void RestoreCoords(Vector2i[] coords)
    {
        ArgumentNullException.ThrowIfNull(coords); if (coords.Distinct().Count() != coords.Length) throw new ArgumentException("Duplicate tile coordinates.", nameof(coords));
        foreach (var coord in coords) Nonnegative(coord);
        foreach (var old in _tiles.Keys.Except(coords).ToArray()) RemoveTile(old);
        foreach (var coord in coords) if (!_tiles.ContainsKey(coord)) { var tile = new Tile(); tile.Alternatives.Add(0, NewData(false)); _tiles.Add(coord, tile); }
        Change();
    }
    private void RestoreAlternatives(Vector2i coords, int[] ids)
    {
        ArgumentNullException.ThrowIfNull(ids); if (!ids.Contains(0) || ids.Distinct().Count() != ids.Length || ids.Any(i => i < 0 || i >= TransformFlipH)) throw new ArgumentException("Invalid alternative identities.", nameof(ids));
        var tile = GetTile(coords);
        foreach (var old in tile.Alternatives.Keys.Except(ids).ToArray()) RemoveAlternativeTile(coords, old);
        foreach (var id in ids) if (!tile.Alternatives.ContainsKey(id)) CreateAlternativeTile(coords, id);
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<TileSetAtlasSource, Texture?>(nameof(Texture), n => n.Texture, (n, v) => n.Texture = v, _ => null, stored: true);
        yield return new PropertyDescriptor<TileSetAtlasSource, Vector2i>(nameof(Margins), n => n.Margins, (n, v) => n.Margins = v, _ => default, stored: true);
        yield return new PropertyDescriptor<TileSetAtlasSource, Vector2i>(nameof(Separation), n => n.Separation, (n, v) => n.Separation = v, _ => default, stored: true);
        yield return new PropertyDescriptor<TileSetAtlasSource, Vector2i>(nameof(TextureRegionSize), n => n.TextureRegionSize, (n, v) => n.TextureRegionSize = v, _ => new(16, 16), stored: true);
        yield return new PropertyDescriptor<TileSetAtlasSource, int>("PhysicsLayerCount", n => n._physicsLayers, (n, v) => { if (v < 0) throw new ArgumentOutOfRangeException(nameof(v)); n.ResizePhysicsLayers(v); }, _ => 0, stored: true);
        yield return new PropertyDescriptor<TileSetAtlasSource, Vector2i[]>("TileCoordinates", n => n.SortedCoords(), (n, v) => n.RestoreCoords(v), _ => [], stored: true);
        foreach (var coord in SortedCoords())
        {
            var c = coord; var prefix = $"Tiles/{c.X},{c.Y}/";
            yield return new PropertyDescriptor<TileSetAtlasSource, Vector2i>(prefix + "Size", n => n._tiles[c].Size, (n, v) => { TileSet.Positive(v); n._tiles[c].Size = v; n.Change(); }, _ => Vector2i.One, stored: true);
            yield return new PropertyDescriptor<TileSetAtlasSource, int[]>(prefix + "Alternatives", n => n._tiles[c].Alternatives.Keys.ToArray(), (n, v) => n.RestoreAlternatives(c, v), _ => [0], stored: true);
            foreach (var alternative in _tiles[c].Alternatives)
            {
                var id = alternative.Key;
                foreach (var property in alternative.Value.StoredProperties<TileSetAtlasSource>(prefix + id + "/", n => n.GetTileData(c, id))) yield return property;
            }
        }
    }
}
