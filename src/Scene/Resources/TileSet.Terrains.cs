namespace Electron2D;

public sealed partial class TileSet
{
    private sealed class TerrainDefinition
    {
        internal string Name = "";
        internal Color Color;
        internal TerrainDefinition Copy() => new() { Name = Name, Color = Color };
    }
    private sealed class TerrainSetDefinition
    {
        internal TerrainMode Mode;
        internal readonly List<TerrainDefinition> Terrains = [];
        internal TerrainSetDefinition Copy() { var copy = new TerrainSetDefinition { Mode = Mode }; foreach (var t in Terrains) copy.Terrains.Add(t.Copy()); return copy; }
    }
    private readonly List<TerrainSetDefinition> _terrainSets = [];
    private bool _editingTerrains;
    private void ChangeTerrainDefinitions(Action change, Action<TileData> update)
    {
        if (_editingTerrains) throw new InvalidOperationException("Terrain definitions cannot re-enter an edit.");
        var data = AtlasData().ToArray(); _editingTerrains = true;
        try
        {
            change(); foreach (var item in data) update(item);
            Exception? failure = null;
            foreach (var item in data) if (!item.IsDisposed) try { item.TerrainSchemaChanged(); } catch (Exception exception) { failure ??= exception; }
            try { NotifyPropertyListChanged(); } catch (Exception exception) { failure ??= exception; }
            try { EmitChanged(); } catch (Exception exception) { failure ??= exception; }
            if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        finally { _editingTerrains = false; }
    }
    /// <summary>Returns the number of terrain sets.</summary>
    public int GetTerrainSetsCount() { ThrowIfDisposed(); return _terrainSets.Count; }
    /// <summary>Inserts an empty terrain set; a negative insertion position appends.</summary>
    /// <param name="toPosition">Insertion position, or a negative value to append.</param>
    public void AddTerrainSet(int toPosition = -1)
    {
        ThrowIfDisposed(); if (toPosition < 0) toPosition = _terrainSets.Count; ChangeTerrainDefinitions(() => _terrainSets.Insert(toPosition, new()), data => data.RemapTerrainSetInsert(toPosition));
    }
    /// <summary>Moves a terrain set before an insertion position, remapping atlas alternatives.</summary>
    /// <param name="terrainSet">The terrain set to move.</param>
    /// <param name="toPosition">Insertion position between zero and the set count, inclusive.</param>
    public void MoveTerrainSet(int terrainSet, int toPosition)
    { ThrowIfDisposed(); ChangeTerrainDefinitions(() => Move(_terrainSets, terrainSet, toPosition), data => data.RemapTerrainSetMove(terrainSet, toPosition)); }
    /// <summary>Removes a terrain set and clears its alternatives' center and peering assignments.</summary>
    /// <param name="terrainSet">The terrain set to remove.</param>
    public void RemoveTerrainSet(int terrainSet)
    { ThrowIfDisposed(); ChangeTerrainDefinitions(() => _terrainSets.RemoveAt(terrainSet), data => data.RemapTerrainSetRemove(terrainSet)); }
    /// <summary>Returns the matching mode of a terrain set.</summary>
    /// <param name="terrainSet">The terrain set index.</param>
    public TerrainMode GetTerrainSetMode(int terrainSet) { ThrowIfDisposed(); return _terrainSets[terrainSet].Mode; }
    /// <summary>Sets the matching mode and updates available peering directions.</summary>
    /// <param name="terrainSet">The terrain set index.</param>
    /// <param name="mode">A defined matching mode.</param>
    public void SetTerrainSetMode(int terrainSet, TerrainMode mode)
    {
        ThrowIfDisposed(); var set = _terrainSets[terrainSet]; if ((uint)mode > 2) throw new ArgumentOutOfRangeException(nameof(mode)); if (set.Mode == mode) return;
        ChangeTerrainDefinitions(() => set.Mode = mode, data => data.TerrainSchemaChanged(false));
    }
    /// <summary>Returns the number of terrains in a set.</summary>
    /// <param name="terrainSet">The terrain set index.</param>
    public int GetTerrainsCount(int terrainSet) { ThrowIfDisposed(); return _terrainSets[terrainSet].Terrains.Count; }
    /// <summary>Inserts a terrain with a generated name and opaque color, remapping center and peering identities.</summary>
    /// <param name="terrainSet">The terrain set index.</param>
    /// <param name="toPosition">Insertion position, or a negative value to append.</param>
    public void AddTerrain(int terrainSet, int toPosition = -1)
    {
        ThrowIfDisposed(); var terrains = _terrainSets[terrainSet].Terrains; if (toPosition < 0) toPosition = terrains.Count;
        if ((uint)toPosition > (uint)terrains.Count) throw new ArgumentOutOfRangeException(nameof(toPosition));
        var definition = new TerrainDefinition { Name = "Terrain " + toPosition, Color = Color.FromHSV(((terrains.Count + 1) % 16) / 16f, .5f, .5f) };
        ChangeTerrainDefinitions(() => terrains.Insert(toPosition, definition), data => data.RemapTerrainInsert(terrainSet, toPosition));
    }
    /// <summary>Moves a terrain before an insertion position, preserving the meaning of atlas assignments.</summary>
    /// <param name="terrainSet">The terrain set index.</param>
    /// <param name="terrainIndex">The terrain to move.</param>
    /// <param name="toPosition">Insertion position between zero and the terrain count, inclusive.</param>
    public void MoveTerrain(int terrainSet, int terrainIndex, int toPosition)
    { ThrowIfDisposed(); ChangeTerrainDefinitions(() => Move(_terrainSets[terrainSet].Terrains, terrainIndex, toPosition), data => data.RemapTerrainMove(terrainSet, terrainIndex, toPosition)); }
    /// <summary>Removes a terrain and clears its assignments, remapping later identities.</summary>
    /// <param name="terrainSet">The terrain set index.</param>
    /// <param name="terrainIndex">The terrain to remove.</param>
    public void RemoveTerrain(int terrainSet, int terrainIndex)
    { ThrowIfDisposed(); ChangeTerrainDefinitions(() => _terrainSets[terrainSet].Terrains.RemoveAt(terrainIndex), data => data.RemapTerrainRemove(terrainSet, terrainIndex)); }
    /// <summary>Removes every terrain in a set and clears its atlas assignments.</summary>
    /// <param name="terrainSet">The terrain set index.</param>
    public void ClearTerrains(int terrainSet)
    { ThrowIfDisposed(); ChangeTerrainDefinitions(() => _terrainSets[terrainSet].Terrains.Clear(), data => data.ClearTerrains(terrainSet)); }
    /// <summary>Returns a terrain's name.</summary>
    /// <param name="terrainSet">The terrain set index.</param>
    /// <param name="terrainIndex">The terrain index.</param>
    public string GetTerrainName(int terrainSet, int terrainIndex) { ThrowIfDisposed(); return _terrainSets[terrainSet].Terrains[terrainIndex].Name; }
    /// <summary>Sets a terrain's name.</summary>
    /// <param name="terrainSet">The terrain set index.</param>
    /// <param name="terrainIndex">The terrain index.</param>
    /// <param name="name">A nonnull name, including an empty name.</param>
    public void SetTerrainName(int terrainSet, int terrainIndex, string name)
    { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); var terrain = _terrainSets[terrainSet].Terrains[terrainIndex]; if (terrain.Name == name) return; terrain.Name = name; EmitChanged(); }
    /// <summary>Returns the terrain's opaque diagnostic color.</summary>
    /// <param name="terrainSet">The terrain set index.</param>
    /// <param name="terrainIndex">The terrain index.</param>
    public Color GetTerrainColor(int terrainSet, int terrainIndex) { ThrowIfDisposed(); return _terrainSets[terrainSet].Terrains[terrainIndex].Color; }
    /// <summary>Sets a finite diagnostic color, coercing its alpha to one.</summary>
    /// <param name="terrainSet">The terrain set index.</param>
    /// <param name="terrainIndex">The terrain index.</param>
    /// <param name="color">Finite color components; alpha is ignored.</param>
    public void SetTerrainColor(int terrainSet, int terrainIndex, Color color)
    { ThrowIfDisposed(); var terrain = _terrainSets[terrainSet].Terrains[terrainIndex]; if (!color.IsFinite()) throw new ArgumentOutOfRangeException(nameof(color)); color.A = 1; if (terrain.Color == color) return; terrain.Color = color; EmitChanged(); }
    private IEnumerable<TileData> AtlasData() => _sources.Values.OfType<TileSetAtlasSource>().SelectMany(s => s.TerrainData());
    internal bool ValidTerrainBit(int set, CellNeighbor bit)
    {
        if ((uint)set >= (uint)_terrainSets.Count || (uint)bit > 15) return false;
        var side = ((int)bit & 1) == 0; var mode = _terrainSets[set].Mode;
        if (side && mode == TerrainMode.MatchCorners || !side && mode == TerrainMode.MatchSides) return false;
        var mask = _tileShape switch
        {
            TileShape.Square => 0x9999,
            TileShape.Isometric => 0x6666,
            _ when _tileOffsetAxis == TileOffsetAxis.Horizontal => 0xEDED,
            _ => 0xDEDE
        };
        return (mask & 1 << (int)bit) != 0;
    }
    private IEnumerable<PropertyDescriptor> TerrainProperties()
    {
        yield return new PropertyDescriptor<TileSet, int>("TerrainSetCount", n => n.GetTerrainSetsCount(), (n, v) => n.RestoreTerrainSetCount(v), _ => 0, stored: true);
        for (var i = 0; i < _terrainSets.Count; i++)
        {
            var set = i; var prefix = $"Terrain/{i}/";
            yield return new PropertyDescriptor<TileSet, TerrainMode>(prefix + "Mode", n => n.GetTerrainSetMode(set), (n, v) => n.SetTerrainSetMode(set, v), _ => TerrainMode.MatchCornersAndSides, stored: true);
            yield return new PropertyDescriptor<TileSet, int>(prefix + "Count", n => n.GetTerrainsCount(set), (n, v) => n.RestoreTerrainCount(set, v), _ => 0, stored: true);
            for (var j = 0; j < _terrainSets[i].Terrains.Count; j++)
            {
                var terrain = j; var key = prefix + j + "/";
                yield return new PropertyDescriptor<TileSet, string>(key + "Name", n => n.GetTerrainName(set, terrain), (n, v) => n.SetTerrainName(set, terrain, v), _ => "Terrain " + terrain, stored: true);
                yield return new PropertyDescriptor<TileSet, Color>(key + "Color", n => n.GetTerrainColor(set, terrain), (n, v) => n.SetTerrainColor(set, terrain, v), _ => Colors.Black, stored: true);
            }
        }
    }
    private void RestoreTerrainSetCount(int count)
    { if (count < 0) throw new ArgumentOutOfRangeException(nameof(count)); while (_terrainSets.Count > count) RemoveTerrainSet(_terrainSets.Count - 1); while (_terrainSets.Count < count) AddTerrainSet(); }
    private void RestoreTerrainCount(int set, int count)
    { if (count < 0) throw new ArgumentOutOfRangeException(nameof(count)); while (GetTerrainsCount(set) > count) RemoveTerrain(set, GetTerrainsCount(set) - 1); while (GetTerrainsCount(set) < count) AddTerrain(set); }
}
