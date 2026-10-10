namespace Electron2D;

public partial class TileMapLayer
{
    private sealed class TerrainPattern(int terrain, int[] bits) : IComparable<TerrainPattern>
    {
        internal readonly int Terrain = terrain;
        internal readonly int[] Bits = bits;
        public int CompareTo(TerrainPattern? other)
        {
            if (other is null) return 1; var center = Terrain.CompareTo(other.Terrain); if (center != 0) return center;
            for (var i = 0; i < 16; i++) { var comparison = Bits[i].CompareTo(other.Bits[i]); if (comparison != 0) return comparison; }
            return 0;
        }
    }
    private readonly record struct TerrainConstraint(int Terrain, int Priority);
    private readonly record struct TerrainChoice(Cell Cell, float Weight);
    /// <summary>Paints a terrain and joins adjacent matching centers, updating surrounding transitions when needed.</summary>
    /// <remarks>The tile set must contain the required terrain combinations. Edits are published through the normal layer batch. This authoring operation can allocate.</remarks>
    /// <param name="cells">Cell coordinates to paint.</param>
    /// <param name="terrainSet">The terrain set index.</param>
    /// <param name="terrain">The terrain index, or -1 for empty terrain.</param>
    /// <param name="ignoreEmptyTerrains">Whether existing empty terrain constraints are ignored.</param>
    /// <exception cref="ArgumentOutOfRangeException">A terrain identity is invalid.</exception>
    /// <exception cref="OverflowException">Required neighboring coordinates cannot fit Vector2i.</exception>
    public void SetCellsTerrainConnect(Vector2i[] cells, int terrainSet, int terrain, bool ignoreEmptyTerrains = true)
    { ArgumentNullException.ThrowIfNull(cells); PaintTerrain(cells, terrainSet, terrain, ignoreEmptyTerrains, false); }
    /// <summary>Paints a path, constraining successive neighboring cells to connect through the selected terrain.</summary>
    /// <remarks>The tile set must contain the required terrain combinations. Neighboring transitions can also change; unconstrained bits are preserved.</remarks>
    /// <param name="path">Ordered neighboring cell coordinates.</param>
    /// <param name="terrainSet">The terrain set index.</param>
    /// <param name="terrain">The terrain index, or -1 for empty terrain.</param>
    /// <param name="ignoreEmptyTerrains">Whether existing empty terrain constraints are ignored.</param>
    /// <exception cref="ArgumentException">Successive cells are not neighbors.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A terrain identity is invalid.</exception>
    /// <exception cref="OverflowException">Required neighboring coordinates cannot fit Vector2i.</exception>
    public void SetCellsTerrainPath(Vector2i[] path, int terrainSet, int terrain, bool ignoreEmptyTerrains = true)
    { ArgumentNullException.ThrowIfNull(path); PaintTerrain(path, terrainSet, terrain, ignoreEmptyTerrains, true); }
    private static TerrainPattern EmptyTerrainPattern() => new(-1, Enumerable.Repeat(-1, 16).ToArray());
    private static TerrainPattern CaptureTerrainPattern(TileSet set, int terrainSet, TileData? data)
    {
        if (data is null || data.TerrainSet != terrainSet) return EmptyTerrainPattern();
        var bits = new int[16]; for (var i = 0; i < 16; i++) bits[i] = set.ValidTerrainBit(terrainSet, (CellNeighbor)i) ? data.TerrainBit(i) : -1;
        return new(data.Terrain, bits);
    }
    private TerrainPattern CurrentTerrainPattern(TileSet set, int terrainSet, Vector2i coords) => CaptureTerrainPattern(set, terrainSet, GetCellTileData(coords));
    private static SortedDictionary<TerrainPattern, List<TerrainChoice>> TerrainCatalog(TileSet set, int terrainSet)
    {
        var catalog = new SortedDictionary<TerrainPattern, List<TerrainChoice>>();
        for (var sourceIndex = 0; sourceIndex < set.GetSourceCount(); sourceIndex++)
        {
            var sourceID = set.GetSourceID(sourceIndex); if (set.GetSource(sourceID) is not TileSetAtlasSource source) continue;
            for (var tileIndex = 0; tileIndex < source.GetTilesCount(); tileIndex++)
            {
                var atlas = source.GetTileID(tileIndex);
                for (var alternativeIndex = 0; alternativeIndex < source.GetAlternativeTilesCount(atlas); alternativeIndex++)
                {
                    var alternative = source.GetAlternativeTileID(atlas, alternativeIndex); var data = source.GetTileData(atlas, alternative); if (data.TerrainSet != terrainSet) continue;
                    var pattern = CaptureTerrainPattern(set, terrainSet, data); if (pattern.Terrain < 0 && pattern.Bits.All(v => v < 0)) continue;
                    if (!catalog.TryGetValue(pattern, out var choices)) catalog.Add(pattern, choices = []);
                    choices.Add(new(new(sourceID, atlas, alternative), data.Probability));
                }
            }
        }
        catalog.Add(EmptyTerrainPattern(), [new(new(-1, new(-1, -1), -1), 1)]); return catalog;
    }
    private void PaintTerrain(Vector2i[] input, int terrainSet, int terrain, bool ignoreEmpty, bool path)
    {
        EnsureMutable(); var set = RequireSet(); var count = set.GetTerrainsCount(terrainSet);
        if (terrain < -1 || terrain >= count) throw new ArgumentOutOfRangeException(nameof(terrain));
        if (input.Length == 0) return; var cells = (Vector2i[])input.Clone();
        var neighbors = Enumerable.Range(0, 16).Select(i => (CellNeighbor)i).Where(b => ExistingTerrainNeighbor(set, b)).ToArray();
        var bits = Enumerable.Range(0, 16).Select(i => (CellNeighbor)i).Where(b => set.ValidTerrainBit(terrainSet, b)).ToArray();
        var pathBits = new CellNeighbor[Math.Max(0, cells.Length - 1)];
        if (path) for (var i = 0; i < pathBits.Length; i++)
            {
                var found = false; foreach (var b in neighbors) if (set.GetNeighborCell(cells[i], b) == cells[i + 1]) { pathBits[i] = b; found = true; break; }
                if (!found) throw new ArgumentException("Successive terrain path cells must be neighbors.", nameof(input));
            }
        var painted = cells.ToHashSet(); var modify = new List<Vector2i>(); var modifying = new HashSet<Vector2i>();
        for (var i = cells.Length - 1; i >= 0; i--) { modify.Add(cells[i]); modifying.Add(cells[i]); }
        foreach (var coords in cells) foreach (var b in neighbors)
                if ((!path || set.ValidTerrainBit(terrainSet, b)) && modifying.Add(set.GetNeighborCell(coords, b))) modify.Add(set.GetNeighborCell(coords, b));
        if (path) foreach (var coords in cells) foreach (var b in bits)
                    if (!ExistingTerrainNeighbor(set, b)) foreach (var overlap in OverlappingBits(set, PeeringKey(set, coords, b)))
                            if (modifying.Add(overlap.Coords)) modify.Add(overlap.Coords);
        var constraints = new Dictionary<TerrainKey, TerrainConstraint>();
        foreach (var coords in cells) constraints.TryAdd(new(coords, 0), new(terrain, 10));
        if (path)
        {
            for (var i = 0; i < pathBits.Length; i++) constraints.TryAdd(PeeringKey(set, cells[i], pathBits[i]), new(terrain, 10));
        }
        else
        {
            var matchingCenters = modifying.Where(c => painted.Contains(c) || GetCellTileData(c) is { } d && d.TerrainSet == terrainSet && d.Terrain == terrain).ToHashSet();
            foreach (var coords in cells) foreach (var b in bits)
                {
                    var key = PeeringKey(set, coords, b);
                    if (((int)b & 1) == 0 ? matchingCenters.Contains(set.GetNeighborCell(coords, b)) : OverlappingBits(set, key).All(o => matchingCenters.Contains(o.Coords))) constraints.TryAdd(key, new(terrain, 10));
                }
        }
        AddExistingTerrainConstraints(set, terrainSet, painted, bits, ignoreEmpty, constraints);
        var catalog = TerrainCatalog(set, terrainSet); var output = new Dictionary<Vector2i, TerrainPattern>();
        foreach (var coords in modify)
        {
            var current = CurrentTerrainPattern(set, terrainSet, coords); var best = BestTerrainPattern(set, coords, bits, current, catalog, constraints); output[coords] = best;
            constraints[new(coords, 0)] = new(best.Terrain, 5);
            foreach (var b in bits) constraints[PeeringKey(set, coords, b)] = new(best.Bits[(int)b], 5);
        }
        var changes = new List<(Vector2i Coords, Cell Cell)>();
        foreach (var (coords, pattern) in output)
        {
            if (!painted.Contains(coords) && pattern.CompareTo(CurrentTerrainPattern(set, terrainSet, coords)) == 0) continue;
            var cell = catalog.TryGetValue(pattern, out var choices) ? PickTerrain(choices) : new Cell(-1, new(-1, -1), -1); changes.Add((coords, cell));
        }
        foreach (var (coords, cell) in changes) SetCell(coords, cell.SourceID, cell.Atlas, cell.Alternative);
    }
    private static bool ExistingTerrainNeighbor(TileSet set, CellNeighbor b)
    {
        var mask = set.TileShape switch
        {
            TileShape.Square => 0x9999,
            TileShape.Isometric => 0x6666,
            _ when set.TileOffsetAxis == TileOffsetAxis.Horizontal => 0x4545,
            _ => 0x5454
        };
        return (mask & 1 << (int)b) != 0;
    }
    private void AddExistingTerrainConstraints(TileSet set, int terrainSet, HashSet<Vector2i> painted, CellNeighbor[] bits, bool ignoreEmpty, Dictionary<TerrainKey, TerrainConstraint> constraints)
    {
        var dummy = new SortedSet<TerrainKey>(); foreach (var coords in painted) foreach (var b in bits) dummy.Add(PeeringKey(set, coords, b));
        foreach (var key in dummy)
        {
            var counts = new Dictionary<int, int>();
            foreach (var overlap in OverlappingBits(set, key))
            {
                var data = GetCellTileData(overlap.Coords); var value = data is not null && data.TerrainSet == terrainSet ? data.TerrainBit((int)overlap.Bit) : -1;
                if (!ignoreEmpty || value >= 0) counts[value] = counts.GetValueOrDefault(value) + 1;
            }
            var maximum = 0; var terrain = -1; foreach (var pair in counts) if (pair.Value > maximum) { maximum = pair.Value; terrain = pair.Key; }
            if (maximum > 0) constraints.TryAdd(key, new(terrain, 1));
        }
        foreach (var coords in painted)
        {
            var value = CurrentTerrainPattern(set, terrainSet, coords).Terrain;
            if (!ignoreEmpty || value >= 0) constraints.TryAdd(new(coords, 0), new(value, 1));
        }
    }
    private static TerrainPattern BestTerrainPattern(TileSet set, Vector2i coords, CellNeighbor[] bits, TerrainPattern current, SortedDictionary<TerrainPattern, List<TerrainChoice>> catalog, Dictionary<TerrainKey, TerrainConstraint> constraints)
    {
        var best = current; var minimum = int.MaxValue;
        foreach (var candidate in catalog.Keys)
        {
            var score = 0;
            if (constraints.TryGetValue(new(coords, 0), out var center)) { if (candidate.Terrain != center.Terrain) score += center.Priority; }
            else if (candidate.Terrain != current.Terrain) continue;
            var valid = true;
            foreach (var b in bits)
            {
                if (constraints.TryGetValue(PeeringKey(set, coords, b), out var constraint)) { if (candidate.Bits[(int)b] != constraint.Terrain) score += constraint.Priority; }
                else if (candidate.Bits[(int)b] != current.Bits[(int)b]) { valid = false; break; }
            }
            if (valid && score < minimum) { minimum = score; best = candidate; }
        }
        return best;
    }
    private static Cell PickTerrain(List<TerrainChoice> choices)
    {
        double total = 0; foreach (var choice in choices) total += choice.Weight;
        if (total == 0) return choices[0].Cell;
        var picked = Random.Shared.NextDouble() * total; double cumulative = 0;
        foreach (var choice in choices) { cumulative += choice.Weight; if (choice.Weight > 0 && cumulative > picked) return choice.Cell; }
        return choices[^1].Cell;
    }
}
