namespace Electron2D;

public sealed partial class TileData
{
    internal TileSetAtlasSource? TerrainSource;
    private int _terrainSet = -1, _terrain = -1;
    private readonly int[] _terrainBits = Enumerable.Repeat(-1, 16).ToArray();
    private float _probability = 1;
    private TileSet? TerrainOwner => TerrainSource is { IsDisposed: false, OwnerSet: { IsDisposed: false } set } ? set : null;
    /// <summary>Gets or sets the terrain set index, or -1 to disable terrain assignments.</summary>
    /// <remarks>A change on data attached to a set clears its center and peering assignments. Standalone atlas authoring can retain indices before association.</remarks>
    public int TerrainSet
    {
        get { ThrowIfDisposed(); return _terrainSet; }
        set
        {
            ThrowIfDisposed(); if (value < -1 || TerrainOwner is { } set && value >= set.GetTerrainSetsCount()) throw new ArgumentOutOfRangeException(nameof(value));
            if (_terrainSet == value) return; _terrainSet = value;
            if (TerrainOwner is not null || value < 0) ClearTerrainAssignments(); Change();
        }
    }
    /// <summary>Gets or sets the center terrain index, or -1 for an empty center.</summary>
    /// <remarks>A nonempty center requires an assigned terrain set and must belong to the attached set.</remarks>
    public int Terrain
    {
        get { ThrowIfDisposed(); return _terrain; }
        set { ThrowIfDisposed(); ValidateTerrain(value); if (_terrain == value) return; _terrain = value; Change(); }
    }
    /// <summary>Gets or sets the finite nonnegative weight used to select atlas alternatives with the same terrain pattern.</summary>
    /// <remarks>The default is one. When all matching weights are zero the first candidate is selected.</remarks>
    public float Probability
    {
        get { ThrowIfDisposed(); return _probability; }
        set { ThrowIfDisposed(); if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_probability == value) return; _probability = value; Change(); }
    }
    /// <summary>Returns an applicable terrain peering value; -1 is an empty terrain.</summary>
    /// <param name="peeringBit">A side or corner supported by the attached set's shape and terrain mode.</param>
    /// <exception cref="ArgumentOutOfRangeException">The direction is undefined or not applicable.</exception>
    public int GetTerrainPeeringBit(CellNeighbor peeringBit)
    { ThrowIfDisposed(); if (!IsValidTerrainPeeringBit(peeringBit)) throw new ArgumentOutOfRangeException(nameof(peeringBit)); return _terrainBits[(int)peeringBit]; }
    /// <summary>Sets a terrain peering value; -1 clears it.</summary>
    /// <param name="peeringBit">A defined direction, applicable when attached to a set.</param>
    /// <param name="terrainIndex">A terrain index or -1 for empty terrain.</param>
    /// <exception cref="ArgumentOutOfRangeException">The direction or terrain value is invalid.</exception>
    public void SetTerrainPeeringBit(CellNeighbor peeringBit, int terrainIndex)
    {
        ThrowIfDisposed(); if ((uint)peeringBit > 15 || TerrainOwner is not null && _terrainSet >= 0 && !IsValidTerrainPeeringBit(peeringBit)) throw new ArgumentOutOfRangeException(nameof(peeringBit));
        ValidateTerrain(terrainIndex); if (_terrainBits[(int)peeringBit] == terrainIndex) return; _terrainBits[(int)peeringBit] = terrainIndex; Change();
    }
    /// <summary>Tests whether a defined peering direction is applicable to this data's attached set, terrain set and mode.</summary>
    /// <param name="peeringBit">The direction to test.</param>
    /// <returns>False for unassigned, detached, undefined or unavailable directions.</returns>
    public bool IsValidTerrainPeeringBit(CellNeighbor peeringBit)
    { ThrowIfDisposed(); return TerrainOwner is { } set && set.ValidTerrainBit(_terrainSet, peeringBit); }
    private void ValidateTerrain(int value)
    {
        if (value < -1 || _terrainSet < 0 && value >= 0 || TerrainOwner is { } set && _terrainSet >= 0 && value >= set.GetTerrainsCount(_terrainSet)) throw new ArgumentOutOfRangeException(nameof(value));
    }
    private void ClearTerrainAssignments() { _terrain = -1; Array.Fill(_terrainBits, -1); }
    internal void TerrainSchemaChanged(bool notify = true)
    {
        if (TerrainOwner is { } set)
        {
            if (_terrainSet >= set.GetTerrainSetsCount()) { _terrainSet = -1; ClearTerrainAssignments(); }
            else if (_terrainSet >= 0)
            {
                var count = set.GetTerrainsCount(_terrainSet); if (_terrain >= count) _terrain = -1;
                for (var i = 0; i < 16; i++) if (_terrainBits[i] >= count) _terrainBits[i] = -1;
            }
            else ClearTerrainAssignments();
        }
        if (notify) NotifyPropertyListChanged();
    }
    private static int MoveIdentity(int id, int from, int to)
    {
        if (id < 0) return id; var destination = from < to ? to - 1 : to;
        if (id == from) return destination;
        if (id > from && id <= destination) return id - 1;
        if (id >= destination && id < from) return id + 1;
        return id;
    }
    internal void RemapTerrainSetInsert(int at) { if (_terrainSet >= at) _terrainSet++; TerrainSchemaChanged(false); }
    internal void RemapTerrainSetMove(int from, int to) { _terrainSet = MoveIdentity(_terrainSet, from, to); TerrainSchemaChanged(false); }
    internal void RemapTerrainSetRemove(int at)
    { if (_terrainSet == at) { _terrainSet = -1; ClearTerrainAssignments(); } else if (_terrainSet > at) _terrainSet--; TerrainSchemaChanged(false); }
    internal void RemapTerrainInsert(int set, int at)
    { if (_terrainSet != set) return; if (_terrain >= at) _terrain++; for (var i = 0; i < 16; i++) if (_terrainBits[i] >= at) _terrainBits[i]++; TerrainSchemaChanged(false); }
    internal void RemapTerrainMove(int set, int from, int to)
    { if (_terrainSet != set) return; _terrain = MoveIdentity(_terrain, from, to); for (var i = 0; i < 16; i++) _terrainBits[i] = MoveIdentity(_terrainBits[i], from, to); TerrainSchemaChanged(false); }
    internal void RemapTerrainRemove(int set, int at)
    { if (_terrainSet != set) return; _terrain = _terrain == at ? -1 : _terrain > at ? _terrain - 1 : _terrain; for (var i = 0; i < 16; i++) _terrainBits[i] = _terrainBits[i] == at ? -1 : _terrainBits[i] > at ? _terrainBits[i] - 1 : _terrainBits[i]; TerrainSchemaChanged(false); }
    internal void ClearTerrains(int set) { if (_terrainSet == set) { ClearTerrainAssignments(); TerrainSchemaChanged(false); } }
    internal int TerrainBit(int index) => _terrainBits[index];
    private void CopyTerrainTo(TileData copy)
    { copy.TerrainSource = TerrainSource; copy._terrainSet = _terrainSet; copy._terrain = _terrain; copy._probability = _probability; _terrainBits.CopyTo(copy._terrainBits, 0); }
    private IEnumerable<PropertyDescriptor> TerrainProperties<T>(string prefix, Func<T, TileData> data) where T : ElectronObject
    {
        yield return new PropertyDescriptor<T, int>(prefix + nameof(TerrainSet), n => data(n).TerrainSet, (n, v) => data(n).TerrainSet = v, _ => -1, stored: true) { StoredSetter = (n, v) => { if (v < -1) throw new ArgumentOutOfRangeException(nameof(v)); data(n)._terrainSet = v; } };
        yield return new PropertyDescriptor<T, int>(prefix + nameof(Terrain), n => data(n).Terrain, (n, v) => data(n).Terrain = v, _ => -1, stored: true) { StoredSetter = (n, v) => { if (v < -1) throw new ArgumentOutOfRangeException(nameof(v)); data(n)._terrain = v; } };
        yield return new PropertyDescriptor<T, float>(prefix + nameof(Probability), n => data(n).Probability, (n, v) => data(n).Probability = v, _ => 1, stored: true);
        for (var i = 0; i < 16; i++)
        {
            var bit = i;
            yield return new PropertyDescriptor<T, int>(prefix + $"TerrainBits/{i}", n => data(n)._terrainBits[bit], (n, v) => data(n).SetTerrainPeeringBit((CellNeighbor)bit, v), _ => -1, stored: true) { StoredSetter = (n, v) => { if (v < -1) throw new ArgumentOutOfRangeException(nameof(v)); data(n)._terrainBits[bit] = v; } };
        }
    }
}
