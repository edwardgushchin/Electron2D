namespace Electron2D;

public partial class TileMapLayer
{
    /// <summary>Captures the selected cells as a new reusable pattern, including explicit empty cells.</summary>
    /// <param name="coordsArray">Layer coordinates; duplicates overwrite the same pattern cell.</param>
    /// <returns>A caller-owned pattern normalized from the coordinate minimum with offset-grid parity correction.</returns>
    /// <remarks>Offset layouts may add a one-cell margin to keep all pattern coordinates nonnegative.</remarks>
    /// <exception cref="InvalidOperationException">No tile set is assigned.</exception>
    /// <exception cref="OverflowException">Normalized coordinates or extent exceed signed 32-bit storage.</exception>
    public TileMapPattern GetPattern(Vector2i[] coordsArray)
    {
        ThrowIfDisposed(); var set = RequireSet(); ArgumentNullException.ThrowIfNull(coordsArray);
        var pattern = new TileMapPattern();
        try
        {
            if (coordsArray.Length == 0) return pattern;
            var minimum = coordsArray[0]; foreach (var coord in coordsArray) minimum = minimum.Min(coord);
            var offsets = new Vector2i[coordsArray.Length]; var positive = Vector2i.Zero;
            for (var i = 0; i < offsets.Length; i++) { offsets[i] = set.PatternOffset(minimum, coordsArray[i], true); if (offsets[i].X < 0) positive.X = 1; if (offsets[i].Y < 0) positive.Y = 1; }
            for (var i = 0; i < offsets.Length; i++)
            {
                var coord = coordsArray[i]; var offset = new Vector2i(checked(offsets[i].X + positive.X), checked(offsets[i].Y + positive.Y));
                pattern.SetCell(offset, GetCellSourceID(coord), GetCellAtlasCoords(coord), GetCellAlternativeTile(coord));
            }
            return pattern;
        }
        catch { pattern.Dispose(); throw; }
    }
    /// <summary>Maps a stored pattern coordinate to a layer coordinate with offset-grid parity correction.</summary>
    /// <param name="positionInTilemap">Placement anchor in layer coordinates.</param><param name="coordsInPattern">Stored pattern coordinate.</param>
    /// <param name="pattern">Live borrowed pattern.</param><returns>The layer coordinate.</returns>
    /// <exception cref="ArgumentException">The pattern does not store the coordinate.</exception>
    /// <exception cref="InvalidOperationException">No tile set is assigned.</exception>
    /// <exception cref="OverflowException">The mapped coordinate exceeds signed 32-bit storage.</exception>
    public Vector2i MapPattern(Vector2i positionInTilemap, Vector2i coordsInPattern, TileMapPattern pattern)
    {
        ThrowIfDisposed(); var set = RequireSet(); ArgumentNullException.ThrowIfNull(pattern);
        if (!pattern.HasCell(coordsInPattern)) throw new ArgumentException("The pattern has no cell at this coordinate.", nameof(coordsInPattern));
        return set.PatternOffset(positionInTilemap, coordsInPattern);
    }
    /// <summary>Places stored pattern cells through the normal drawing, collision and navigation batch.</summary>
    /// <param name="position">Placement anchor in layer coordinates.</param><param name="pattern">Live borrowed pattern.</param>
    /// <remarks>Explicit empty cells erase destinations; unstored margins leave existing cells unchanged.
    /// All coordinates and identities are validated before mutation. Changed runs once after the complete edit;
    /// listener failure leaves the committed cells queued for publication. The pattern remains independent.</remarks>
    /// <exception cref="InvalidOperationException">No tile set is assigned or mutation is forbidden during a runtime-data callback.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A nonempty stored identity is outside the layer's tile storage domain.</exception>
    /// <exception cref="OverflowException">A mapped coordinate exceeds signed 32-bit storage.</exception>
    public void SetPattern(Vector2i position, TileMapPattern pattern)
    {
        EnsureMutable(); var set = RequireSet(); ArgumentNullException.ThrowIfNull(pattern); var cells = pattern.Snapshot();
        var changes = new List<(Vector2i Coords, Cell? Cell)>();
        foreach (var pair in cells)
        {
            var coords = set.PatternOffset(position, pair.Key); var data = pair.Value; var cell = ValidateCell(data.SourceID, data.Atlas, data.Alternative);
            if (cell is null ? _cells.ContainsKey(coords) : !_cells.TryGetValue(coords, out var old) || old != cell.Value) changes.Add((coords, cell));
        }
        if (changes.Count == 0) return;
        _cells.EnsureCapacity(checked(_cells.Count + changes.Count)); _dirtyCells.EnsureCapacity(checked(_dirtyCells.Count + changes.Count));
        foreach (var change in changes) { if (change.Cell is { } cell) _cells[change.Coords] = cell; else _cells.Remove(change.Coords); _dirtyCells.Add(change.Coords); }
        Schedule(); BeginEventEmission(Changed)?.Invoke();
    }
}
