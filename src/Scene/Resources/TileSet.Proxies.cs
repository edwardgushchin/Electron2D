namespace Electron2D;

public sealed partial class TileSet
{
    private readonly Dictionary<int, int> _sourceProxies = [];
    private readonly Dictionary<(int SourceID, Vector2i AtlasCoords), (int SourceID, Vector2i AtlasCoords)> _coordsProxies = [];
    private readonly Dictionary<(int SourceID, Vector2i AtlasCoords, int AlternativeTile), (int SourceID, Vector2i AtlasCoords, int AlternativeTile)> _alternativeProxies = [];

    /// <summary>Assigns a source-level replacement for an unresolved tile identity.</summary>
    /// <param name="sourceFrom">Nonnegative original source identity.</param><param name="sourceTo">Nonnegative destination source identity.</param>
    /// <remarks>Sources need not currently exist. Every assignment emits Changed, including repeated assignments.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Either source identity is negative.</exception>
    public void SetSourceLevelTileProxy(int sourceFrom, int sourceTo)
    { ThrowIfDisposed(); ProxySource(sourceFrom); ProxySource(sourceTo); _sourceProxies[sourceFrom] = sourceTo; EmitChanged(); }
    /// <summary>Returns the stored destination source identity.</summary>
    /// <param name="sourceFrom">Original source identity.</param><returns>The destination source identity.</returns>
    /// <exception cref="KeyNotFoundException">No mapping is stored for this source.</exception>
    public int GetSourceLevelTileProxy(int sourceFrom) { ThrowIfDisposed(); return _sourceProxies[sourceFrom]; }
    /// <summary>Returns whether a source-level replacement is stored.</summary>
    /// <param name="sourceFrom">Original source identity.</param><returns>Whether the mapping is stored.</returns>
    public bool HasSourceLevelTileProxy(int sourceFrom) { ThrowIfDisposed(); return _sourceProxies.ContainsKey(sourceFrom); }
    /// <summary>Removes one stored source replacement and emits Changed.</summary>
    /// <param name="sourceFrom">Original source identity.</param>
    /// <exception cref="KeyNotFoundException">No mapping is stored for this source.</exception>
    public void RemoveSourceLevelTileProxy(int sourceFrom)
    { ThrowIfDisposed(); if (!_sourceProxies.Remove(sourceFrom)) throw new KeyNotFoundException("No source-level tile proxy."); EmitChanged(); }
    /// <summary>Assigns a source-and-atlas replacement, retaining the caller's alternative on mapping.</summary>
    /// <param name="sourceFrom">Nonnegative original source identity.</param><param name="coordsFrom">Nonnegative original atlas coordinates.</param>
    /// <param name="sourceTo">Nonnegative destination source identity.</param><param name="coordsTo">Nonnegative destination atlas coordinates.</param>
    /// <exception cref="ArgumentOutOfRangeException">A source identity or atlas component is negative.</exception>
    public void SetCoordsLevelTileProxy(int sourceFrom, Vector2i coordsFrom, int sourceTo, Vector2i coordsTo)
    { ThrowIfDisposed(); ProxyCoords(sourceFrom, coordsFrom); ProxyCoords(sourceTo, coordsTo); _coordsProxies[(sourceFrom, coordsFrom)] = (sourceTo, coordsTo); EmitChanged(); }
    /// <summary>Returns an independent typed destination source-and-atlas pair.</summary>
    /// <param name="sourceFrom">Original source identity.</param><param name="coordsFrom">Original atlas coordinates.</param><returns>The destination source and atlas coordinates.</returns>
    /// <exception cref="KeyNotFoundException">No mapping is stored for this pair.</exception>
    public (int SourceID, Vector2i AtlasCoords) GetCoordsLevelTileProxy(int sourceFrom, Vector2i coordsFrom)
    { ThrowIfDisposed(); return _coordsProxies[(sourceFrom, coordsFrom)]; }
    /// <summary>Returns whether a source-and-atlas replacement is stored.</summary>
    /// <param name="sourceFrom">Original source identity.</param><param name="coordsFrom">Original atlas coordinates.</param><returns>Whether the mapping is stored.</returns>
    public bool HasCoordsLevelTileProxy(int sourceFrom, Vector2i coordsFrom)
    { ThrowIfDisposed(); return _coordsProxies.ContainsKey((sourceFrom, coordsFrom)); }
    /// <summary>Removes one stored source-and-atlas replacement and emits Changed.</summary>
    /// <param name="sourceFrom">Original source identity.</param><param name="coordsFrom">Original atlas coordinates.</param>
    /// <exception cref="KeyNotFoundException">No mapping is stored for this pair.</exception>
    public void RemoveCoordsLevelTileProxy(int sourceFrom, Vector2i coordsFrom)
    { ThrowIfDisposed(); if (!_coordsProxies.Remove((sourceFrom, coordsFrom))) throw new KeyNotFoundException("No coordinate-level tile proxy."); EmitChanged(); }
    /// <summary>Assigns a complete source, atlas and alternative replacement.</summary>
    /// <param name="sourceFrom">Nonnegative original source identity.</param><param name="coordsFrom">Nonnegative original atlas coordinates.</param><param name="alternativeFrom">Original alternative, including transform flags, or minus one.</param>
    /// <param name="sourceTo">Nonnegative destination source identity.</param><param name="coordsTo">Nonnegative destination atlas coordinates.</param><param name="alternativeTo">Destination alternative, including transform flags, or minus one.</param>
    /// <remarks>The complete alternative identity is matched and replaced; transform flags are not appended to the destination.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A source/atlas component is negative or an alternative is below minus one.</exception>
    public void SetAlternativeLevelTileProxy(int sourceFrom, Vector2i coordsFrom, int alternativeFrom, int sourceTo, Vector2i coordsTo, int alternativeTo)
    {
        ThrowIfDisposed(); ProxyCoords(sourceFrom, coordsFrom); ProxyCoords(sourceTo, coordsTo); ProxyAlternative(alternativeFrom); ProxyAlternative(alternativeTo);
        _alternativeProxies[(sourceFrom, coordsFrom, alternativeFrom)] = (sourceTo, coordsTo, alternativeTo); EmitChanged();
    }
    /// <summary>Returns an independent typed destination tile identity.</summary>
    /// <param name="sourceFrom">Original source identity.</param><param name="coordsFrom">Original atlas coordinates.</param><param name="alternativeFrom">Complete original alternative identity.</param><returns>The complete destination identity.</returns>
    /// <exception cref="KeyNotFoundException">No mapping is stored for this identity.</exception>
    public (int SourceID, Vector2i AtlasCoords, int AlternativeTile) GetAlternativeLevelTileProxy(int sourceFrom, Vector2i coordsFrom, int alternativeFrom)
    { ThrowIfDisposed(); return _alternativeProxies[(sourceFrom, coordsFrom, alternativeFrom)]; }
    /// <summary>Returns whether a complete identity replacement is stored.</summary>
    /// <param name="sourceFrom">Original source identity.</param><param name="coordsFrom">Original atlas coordinates.</param><param name="alternativeFrom">Complete original alternative identity.</param><returns>Whether the mapping is stored.</returns>
    public bool HasAlternativeLevelTileProxy(int sourceFrom, Vector2i coordsFrom, int alternativeFrom)
    { ThrowIfDisposed(); return _alternativeProxies.ContainsKey((sourceFrom, coordsFrom, alternativeFrom)); }
    /// <summary>Removes one complete identity replacement and emits Changed.</summary>
    /// <param name="sourceFrom">Original source identity.</param><param name="coordsFrom">Original atlas coordinates.</param><param name="alternativeFrom">Complete original alternative identity.</param>
    /// <exception cref="KeyNotFoundException">No mapping is stored for this identity.</exception>
    public void RemoveAlternativeLevelTileProxy(int sourceFrom, Vector2i coordsFrom, int alternativeFrom)
    { ThrowIfDisposed(); if (!_alternativeProxies.Remove((sourceFrom, coordsFrom, alternativeFrom))) throw new KeyNotFoundException("No alternative-level tile proxy."); EmitChanged(); }
    /// <summary>Returns the original valid tile or applies one most-specific replacement to an unresolved identity.</summary>
    /// <param name="sourceFrom">Original source identity.</param><param name="coordsFrom">Original atlas coordinates.</param><param name="alternativeFrom">Complete original alternative identity.</param>
    /// <returns>The original or mapped source, atlas and alternative identity as an independent value.</returns>
    /// <remarks>Valid originals bypass all proxies. Otherwise alternative, coordinate and source levels are tried in that order.
    /// Mapping does not follow chains or validate the destination; cycles therefore terminate after one lookup.
    /// Callers explicitly use the result with a layer or pattern SetCell; existing cells are not rewritten.</remarks>
    public (int SourceID, Vector2i AtlasCoords, int AlternativeTile) MapTileProxy(int sourceFrom, Vector2i coordsFrom, int alternativeFrom)
    {
        ThrowIfDisposed(); var from = (sourceFrom, coordsFrom, alternativeFrom);
        if (ProxyOriginalExists(sourceFrom, coordsFrom, alternativeFrom)) return from;
        if (_alternativeProxies.TryGetValue(from, out var alternative)) return alternative;
        if (_coordsProxies.TryGetValue((sourceFrom, coordsFrom), out var coords)) return (coords.SourceID, coords.AtlasCoords, alternativeFrom);
        return (_sourceProxies.GetValueOrDefault(sourceFrom, sourceFrom), coordsFrom, alternativeFrom);
    }
    /// <summary>Removes obsolete mappings whose original source, tile or complete alternative now exists.</summary>
    /// <remarks>Each level checks only its own original identity. Missing destinations are retained.
    /// All removals commit before one Changed notification; listener failure cannot leave a partial cleanup.</remarks>
    public void CleanupInvalidTileProxies()
    {
        ThrowIfDisposed(); var sources = _sourceProxies.Keys.Where(HasSource).ToArray();
        var coords = _coordsProxies.Keys.Where(k => ProxyOriginalExists(k.SourceID, k.AtlasCoords)).ToArray();
        var alternatives = _alternativeProxies.Keys.Where(k => ProxyOriginalExists(k.SourceID, k.AtlasCoords, k.AlternativeTile)).ToArray();
        foreach (var key in sources) _sourceProxies.Remove(key); foreach (var key in coords) _coordsProxies.Remove(key); foreach (var key in alternatives) _alternativeProxies.Remove(key);
        if (sources.Length + coords.Length + alternatives.Length > 0) EmitChanged();
    }
    /// <summary>Clears all three replacement tables and emits Changed, including when already empty.</summary>
    public void ClearTileProxies() { ThrowIfDisposed(); ClearProxies(); EmitChanged(); }
    private void ClearProxies() { _sourceProxies.Clear(); _coordsProxies.Clear(); _alternativeProxies.Clear(); }
    private bool ProxyOriginalExists(int source, Vector2i coords, int? alternative = null)
    { return _sources.TryGetValue(source, out var item) && item is { IsDisposed: false } && item.HasTile(coords) && (alternative is null || item.HasAlternativeTile(coords, alternative.Value)); }
    private static void ProxySource(int source) { if (source < 0) throw new ArgumentOutOfRangeException(nameof(source)); }
    private static void ProxyCoords(int source, Vector2i coords) { ProxySource(source); if (coords.X < 0 || coords.Y < 0) throw new ArgumentOutOfRangeException(nameof(coords)); }
    private static void ProxyAlternative(int alternative) { if (alternative < -1) throw new ArgumentOutOfRangeException(nameof(alternative)); }
    private int[] StoredProxies(int level)
    {
        var result = new List<int>();
        if (level == 0) foreach (var p in _sourceProxies.OrderBy(p => p.Key)) { result.Add(p.Key); result.Add(p.Value); }
        else if (level == 1) foreach (var p in _coordsProxies.OrderBy(p => p.Key.SourceID).ThenBy(p => p.Key.AtlasCoords.X).ThenBy(p => p.Key.AtlasCoords.Y))
            { result.AddRange([p.Key.SourceID, p.Key.AtlasCoords.X, p.Key.AtlasCoords.Y, p.Value.SourceID, p.Value.AtlasCoords.X, p.Value.AtlasCoords.Y]); }
        else foreach (var p in _alternativeProxies.OrderBy(p => p.Key.SourceID).ThenBy(p => p.Key.AtlasCoords.X).ThenBy(p => p.Key.AtlasCoords.Y).ThenBy(p => p.Key.AlternativeTile))
            { result.AddRange([p.Key.SourceID, p.Key.AtlasCoords.X, p.Key.AtlasCoords.Y, p.Key.AlternativeTile, p.Value.SourceID, p.Value.AtlasCoords.X, p.Value.AtlasCoords.Y, p.Value.AlternativeTile]); }
        return result.ToArray();
    }
    private void RestoreProxies(int level, int[] data)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(data); var width = level == 0 ? 2 : level == 1 ? 6 : 8;
        if (data.Length % width != 0) throw new ArgumentException("Corrupted tile proxies.", nameof(data));
        var sources = new Dictionary<int, int>(); var coords = new Dictionary<(int, Vector2i), (int, Vector2i)>(); var alternatives = new Dictionary<(int, Vector2i, int), (int, Vector2i, int)>();
        for (var i = 0; i < data.Length; i += width)
        {
            if (level == 0) { ProxySource(data[i]); ProxySource(data[i + 1]); if (!sources.TryAdd(data[i], data[i + 1])) throw new ArgumentException("Duplicate source proxy.", nameof(data)); }
            else
            {
                var from = new Vector2i(data[i + 1], data[i + 2]); var target = i + (level == 1 ? 3 : 4); var to = new Vector2i(data[target + 1], data[target + 2]); ProxyCoords(data[i], from); ProxyCoords(data[target], to);
                if (level == 1) { if (!coords.TryAdd((data[i], from), (data[target], to))) throw new ArgumentException("Duplicate coordinate proxy.", nameof(data)); }
                else { ProxyAlternative(data[i + 3]); ProxyAlternative(data[i + 7]); if (!alternatives.TryAdd((data[i], from, data[i + 3]), (data[target], to, data[i + 7]))) throw new ArgumentException("Duplicate alternative proxy.", nameof(data)); }
            }
        }
        if (level == 0) { _sourceProxies.Clear(); foreach (var p in sources) _sourceProxies.Add(p.Key, p.Value); }
        else if (level == 1) { _coordsProxies.Clear(); foreach (var p in coords) _coordsProxies.Add(p.Key, p.Value); }
        else { _alternativeProxies.Clear(); foreach (var p in alternatives) _alternativeProxies.Add(p.Key, p.Value); }
        EmitChanged();
    }
    private IEnumerable<PropertyDescriptor> ProxyProperties()
    {
        foreach (var level in new[] { 0, 1, 2 })
        { var id = level; yield return new PropertyDescriptor<TileSet, int[]>(new[] { "SourceProxies", "CoordsProxies", "AlternativeProxies" }[id], n => n.StoredProxies(id), (n, v) => n.RestoreProxies(id, v), _ => [], stored: true); }
    }
}
