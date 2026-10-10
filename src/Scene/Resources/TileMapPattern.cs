namespace Electron2D;

/// <summary>Stores a reusable tile stamp in nonnegative cell coordinates, including explicit empty cells.</summary>
/// <remarks>Identities are independent of any tile set. Capture and placement use the consuming layer's grid.
/// Copies own independent cell storage. Editing and exporting are allocating authoring operations.</remarks>
public sealed class TileMapPattern : Resource
{
    internal readonly record struct PatternCell(int SourceID, Vector2i Atlas, int Alternative);
    private readonly Dictionary<Vector2i, PatternCell> _cells = [];
    private Vector2i _size;
    /// <summary>Creates an empty zero-size pattern.</summary>
    public TileMapPattern() { }
    /// <summary>Assigns a stored cell, including an explicit empty cell when identifiers are minus one.</summary>
    /// <param name="coords">Nonnegative pattern coordinates.</param>
    /// <param name="sourceID">Source identity, or minus one.</param>
    /// <param name="atlasCoords">Atlas coordinates, or null for (-1,-1).</param>
    /// <param name="alternativeTile">Alternative identity including transforms, or minus one.</param>
    /// <remarks>Unlike layer erasure, an empty assignment remains present and erases its destination on placement.
    /// The size expands to contain the assigned coordinate. Changed is emitted even for a repeated assignment.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Coordinates are negative or identities exceed their storage domain.</exception>
    /// <exception cref="OverflowException">The cell extent cannot fit a signed 32-bit size.</exception>
    public void SetCell(Vector2i coords, int sourceID = -1, Vector2i? atlasCoords = null, int alternativeTile = -1)
    {
        ThrowIfDisposed(); var atlas = atlasCoords ?? new Vector2i(-1, -1);
        Validate(coords, sourceID, atlas, alternativeTile);
        var extent = new Vector2i(checked(coords.X + 1), checked(coords.Y + 1));
        _cells[coords] = new(sourceID, atlas, alternativeTile); _size = _size.Max(extent); EmitChanged();
    }
    /// <summary>Returns whether a coordinate is stored, including explicit empty cells.</summary>
    /// <param name="coords">Pattern coordinates.</param><returns>Whether the coordinate is stored.</returns>
    public bool HasCell(Vector2i coords) { ThrowIfDisposed(); return _cells.ContainsKey(coords); }
    /// <summary>Removes a stored cell and optionally recomputes the extent from remaining cells.</summary>
    /// <param name="coords">Stored pattern coordinate.</param><param name="updateSize">Whether to discard unused margins and recompute size.</param>
    /// <exception cref="ArgumentException">The coordinate is not stored.</exception>
    public void RemoveCell(Vector2i coords, bool updateSize)
    {
        ThrowIfDisposed(); if (!_cells.Remove(coords)) throw new ArgumentException("The pattern has no cell at this coordinate.", nameof(coords));
        if (updateSize) { _size = default; foreach (var coord in _cells.Keys) _size = _size.Max(coord + Vector2i.One); }
        EmitChanged();
    }
    /// <summary>Returns a stored source identity, or minus one for an absent cell.</summary>
    /// <param name="coords">Pattern coordinates.</param><returns>The source identity or minus one.</returns>
    public int GetCellSourceID(Vector2i coords) { ThrowIfDisposed(); return _cells.TryGetValue(coords, out var cell) ? cell.SourceID : -1; }
    /// <summary>Returns stored atlas coordinates, or (-1,-1) for an absent cell.</summary>
    /// <param name="coords">Pattern coordinates.</param><returns>The atlas coordinates or (-1,-1).</returns>
    public Vector2i GetCellAtlasCoords(Vector2i coords) { ThrowIfDisposed(); return _cells.TryGetValue(coords, out var cell) ? cell.Atlas : new(-1, -1); }
    /// <summary>Returns a stored alternative identity, or minus one for an absent cell.</summary>
    /// <param name="coords">Pattern coordinates.</param><returns>The alternative identity or minus one.</returns>
    public int GetCellAlternativeTile(Vector2i coords) { ThrowIfDisposed(); return _cells.TryGetValue(coords, out var cell) ? cell.Alternative : -1; }
    /// <summary>Returns an independent coordinate array, including explicit empty cells; ordering is unspecified.</summary>
    /// <returns>A copied array of stored coordinates.</returns>
    public Vector2i[] GetUsedCells() { ThrowIfDisposed(); return _cells.Keys.ToArray(); }
    /// <summary>Returns the current cell extent, including retained margins.</summary><returns>The pattern size.</returns>
    public Vector2i GetSize() { ThrowIfDisposed(); return _size; }
    /// <summary>Sets a nonnegative size containing every stored cell; unused margins may shrink.</summary>
    /// <param name="size">New cell extent.</param>
    /// <exception cref="ArgumentOutOfRangeException">Size is negative or excludes a stored cell.</exception>
    public void SetSize(Vector2i size)
    {
        ThrowIfDisposed(); if (size.X < 0 || size.Y < 0 || _cells.Keys.Any(c => c.X >= size.X || c.Y >= size.Y)) throw new ArgumentOutOfRangeException(nameof(size));
        _size = size; EmitChanged();
    }
    /// <summary>Returns whether no coordinates are stored, independently of the retained size.</summary>
    /// <returns>Whether the pattern contains no stored coordinates.</returns>
    public bool IsEmpty() { ThrowIfDisposed(); return _cells.Count == 0; }
    internal KeyValuePair<Vector2i, PatternCell>[] Snapshot() { ThrowIfDisposed(); return _cells.ToArray(); }
    private static void Validate(Vector2i coords, int source, Vector2i atlas, int alternative)
    {
        if (coords.X < 0 || coords.Y < 0 || source < -1 || source >= ushort.MaxValue || atlas.X < -1 || atlas.Y < -1 || atlas.X >= ushort.MaxValue || atlas.Y >= ushort.MaxValue || alternative < -1 || alternative >= ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(coords), "Coordinates must be nonnegative and identities must fit the tile storage domain.");
    }
    private int[] StoredCells()
    {
        var data = new int[checked(_cells.Count * 6)]; var index = 0;
        foreach (var pair in _cells.OrderBy(p => p.Key.X).ThenBy(p => p.Key.Y))
        {
            data[index++] = pair.Key.X; data[index++] = pair.Key.Y; data[index++] = pair.Value.SourceID;
            data[index++] = pair.Value.Atlas.X; data[index++] = pair.Value.Atlas.Y; data[index++] = pair.Value.Alternative;
        }
        return data;
    }
    private void RestoreCells(int[] data)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(data); if (data.Length % 6 != 0) throw new ArgumentException("Corrupted pattern cells.", nameof(data));
        var cells = new Dictionary<Vector2i, PatternCell>(); var size = Vector2i.Zero;
        for (var i = 0; i < data.Length; i += 6)
        {
            var coords = new Vector2i(data[i], data[i + 1]); var atlas = new Vector2i(data[i + 3], data[i + 4]); Validate(coords, data[i + 2], atlas, data[i + 5]);
            size = size.Max(new Vector2i(checked(coords.X + 1), checked(coords.Y + 1)));
            if (!cells.TryAdd(coords, new(data[i + 2], atlas, data[i + 5]))) throw new ArgumentException("Duplicate pattern coordinate.", nameof(data));
        }
        _cells.Clear(); foreach (var pair in cells) _cells.Add(pair.Key, pair.Value); _size = size; EmitChanged();
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new TileMapPattern();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    {
        var copy = (TileMapPattern)target; copy._cells.Clear(); foreach (var pair in _cells) copy._cells.Add(pair.Key, pair.Value); copy._size = _size;
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<TileMapPattern, int[]>("Cells", n => n.StoredCells(), (n, v) => n.RestoreCells(v), _ => [], stored: true);
        yield return new PropertyDescriptor<TileMapPattern, Vector2i>("Size", n => n.GetSize(), (n, v) => n.SetSize(v), _ => default, stored: true);
    }
}
