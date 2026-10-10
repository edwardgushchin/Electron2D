namespace Electron2D;

/// <summary>Defines square tile dimensions, atlas sources and shared physics layer policies.</summary>
/// <remarks>Sources are transferred between sets, while materials are borrowed. Copies duplicate sources
/// even in shallow mode so copying cannot remove sources from the original set.</remarks>
public sealed class TileSet : Resource
{
    internal sealed class PhysicsLayer
    {
        internal uint Layer = 1, Mask = 1;
        internal float Priority = 1;
        internal PhysicsMaterial? Material;
    }
    private Vector2i _tileSize = new(16, 16);
    private readonly SortedDictionary<int, TileSetSource?> _sources = [];
    internal readonly List<PhysicsLayer> PhysicsLayers = [];

    /// <summary>Creates an empty square tile set with 16 by 16 scene-unit cells.</summary>
    public TileSet() { }
    /// <summary>Gets or sets the positive square-grid cell dimensions in scene units.</summary>
    public Vector2i TileSize { get { ThrowIfDisposed(); return _tileSize; } set { ThrowIfDisposed(); Positive(value); if (_tileSize == value) return; _tileSize = value; EmitChanged(); } }
    /// <summary>Returns the number of physics layers.</summary>
    /// <returns>Returns the number of physics layers.</returns>
    public int GetPhysicsLayersCount() { ThrowIfDisposed(); return PhysicsLayers.Count; }
    /// <summary>Inserts an empty physics layer; a negative position appends.</summary>
    /// <param name="toPosition">Insertion position; a negative position appends when adding.</param>
    public void AddPhysicsLayer(int toPosition = -1)
    {
        ThrowIfDisposed(); if (toPosition < 0) toPosition = PhysicsLayers.Count;
        PhysicsLayers.Insert(toPosition, new());
        foreach (var source in _sources.Values) source?.InsertPhysicsLayer(toPosition);
        ChangedSchema();
    }
    /// <summary>Moves a physics layer before the given insertion position, preserving tile polygons.</summary>
    /// <param name="layerIndex">Zero-based physics layer index.</param>
    /// <param name="toPosition">Insertion position; a negative position appends when adding.</param>
    public void MovePhysicsLayer(int layerIndex, int toPosition)
    {
        ThrowIfDisposed(); Move(PhysicsLayers, layerIndex, toPosition);
        foreach (var source in _sources.Values) source?.MovePhysicsLayer(layerIndex, toPosition);
        ChangedSchema();
    }
    /// <summary>Removes a physics layer and its polygons from every source alternative.</summary>
    /// <param name="layerIndex">Zero-based physics layer index.</param>
    public void RemovePhysicsLayer(int layerIndex)
    {
        var layer = Layer(layerIndex); UnobserveMaterial(layer.Material); PhysicsLayers.RemoveAt(layerIndex);
        foreach (var source in _sources.Values) source?.RemovePhysicsLayer(layerIndex);
        ChangedSchema();
    }
    /// <summary>Returns the layer's 32-bit collision membership mask.</summary>
    /// <param name="layerIndex">Zero-based physics layer index.</param>
    /// <returns>Returns the layer's 32-bit collision membership mask.</returns>
    public uint GetPhysicsLayerCollisionLayer(int layerIndex) => Layer(layerIndex).Layer;
    /// <summary>Sets the layer's collision membership mask.</summary>
    /// <param name="layerIndex">Zero-based physics layer index.</param>
    /// <param name="layer">Collision membership bits.</param>
    public void SetPhysicsLayerCollisionLayer(int layerIndex, uint layer) { var item = Layer(layerIndex); if (item.Layer == layer) return; item.Layer = layer; EmitChanged(); }
    /// <summary>Returns the layer's 32-bit collision query mask.</summary>
    /// <param name="layerIndex">Zero-based physics layer index.</param>
    /// <returns>Returns the layer's 32-bit collision query mask.</returns>
    public uint GetPhysicsLayerCollisionMask(int layerIndex) => Layer(layerIndex).Mask;
    /// <summary>Sets the layer's collision query mask.</summary>
    /// <param name="layerIndex">Zero-based physics layer index.</param>
    /// <param name="mask">Collision filtering bits.</param>
    public void SetPhysicsLayerCollisionMask(int layerIndex, uint mask) { var item = Layer(layerIndex); if (item.Mask == mask) return; item.Mask = mask; EmitChanged(); }
    /// <summary>Returns the layer's collision recovery priority.</summary>
    /// <param name="layerIndex">Zero-based physics layer index.</param>
    /// <returns>Returns the layer's collision recovery priority.</returns>
    public float GetPhysicsLayerCollisionPriority(int layerIndex) => Layer(layerIndex).Priority;
    /// <summary>Sets a finite positive collision recovery priority.</summary>
    /// <param name="layerIndex">Zero-based physics layer index.</param>
    /// <param name="priority">Finite positive recovery priority.</param>
    public void SetPhysicsLayerCollisionPriority(int layerIndex, float priority) { var item = Layer(layerIndex); if (!float.IsFinite(priority) || priority <= 0) throw new ArgumentOutOfRangeException(nameof(priority)); if (item.Priority == priority) return; item.Priority = priority; EmitChanged(); }
    /// <summary>Returns the borrowed physics material, or null for default friction and bounce.</summary>
    /// <param name="layerIndex">Zero-based physics layer index.</param>
    /// <returns>Returns the borrowed physics material, or null for default friction and bounce.</returns>
    public PhysicsMaterial? GetPhysicsLayerPhysicsMaterial(int layerIndex) => Layer(layerIndex).Material;
    /// <summary>Assigns a borrowed material and observes its changes and disposal.</summary>
    /// <param name="layerIndex">Zero-based physics layer index.</param>
    /// <param name="physicsMaterial">Borrowed live material, or null for defaults.</param>
    public void SetPhysicsLayerPhysicsMaterial(int layerIndex, PhysicsMaterial? physicsMaterial)
    {
        var layer = Layer(layerIndex); if (physicsMaterial is not null) ObjectDisposedException.ThrowIf(physicsMaterial.IsDisposed, physicsMaterial);
        if (ReferenceEquals(layer.Material, physicsMaterial)) return;
        UnobserveMaterial(layer.Material); layer.Material = physicsMaterial; ObserveMaterial(physicsMaterial); EmitChanged();
    }
    /// <summary>Adds a source under an explicit or automatically selected nonnegative identity.</summary>
    /// <returns>The assigned source identity.</returns>
    /// <param name="source">Live source to transfer into this set.</param>
    /// <param name="atlasSourceIDOverride">Nonnegative source identity, or minus one to choose a free identity.</param>
    public int AddSource(TileSetSource source, int atlasSourceIDOverride = -1)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(source); ObjectDisposedException.ThrowIf(source.IsDisposed, source);
        if (atlasSourceIDOverride < -1) throw new ArgumentOutOfRangeException(nameof(atlasSourceIDOverride));
        var id = atlasSourceIDOverride == -1 ? GetNextSourceID() : atlasSourceIDOverride;
        if (_sources.TryGetValue(id, out var existing) && existing is not null) throw new ArgumentException("Source identity is occupied.", nameof(atlasSourceIDOverride));
        if (ReferenceEquals(source.OwnerSet, this)) throw new ArgumentException("The source already belongs to this set.", nameof(source));
        var previous = source.OwnerSet;
        previous?.DetachSourceInstance(source);
        source.ResizePhysicsLayers(PhysicsLayers.Count); source.OwnerSet = this;
        _sources[id] = source; source.Changed += SourceChanged; source.Disposed += SourceDisposed;
        try { previous?.ChangedSchema(); } finally { ChangedSchema(); }
        return id;
    }
    /// <summary>Removes a source without disposing its resource.</summary>
    /// <param name="sourceID">Source identity; minus one erases or acts as a query wildcard where documented.</param>
    public void RemoveSource(int sourceID)
    {
        ThrowIfDisposed(); if (!_sources.Remove(sourceID, out var source)) throw new KeyNotFoundException("Unknown source identity.");
        if (source is not null) { source.Changed -= SourceChanged; source.Disposed -= SourceDisposed; source.OwnerSet = null; }
        ChangedSchema();
    }
    /// <summary>Renames a source identity to an unoccupied nonnegative value.</summary>
    /// <param name="sourceID">Source identity; minus one erases or acts as a query wildcard where documented.</param>
    /// <param name="newSourceID">Unoccupied nonnegative replacement source identity.</param>
    public void SetSourceID(int sourceID, int newSourceID)
    {
        ThrowIfDisposed(); var source = _sources[sourceID];
        if (newSourceID < 0) throw new ArgumentOutOfRangeException(nameof(newSourceID));
        if (sourceID == newSourceID) return;
        if (_sources.ContainsKey(newSourceID)) throw new ArgumentException("Source identity is occupied.", nameof(newSourceID));
        _sources.Remove(sourceID); _sources.Add(newSourceID, source); ChangedSchema();
    }
    /// <summary>Returns a borrowed source by identity.</summary>
    /// <param name="sourceID">Source identity; minus one erases or acts as a query wildcard where documented.</param>
    /// <returns>Returns a borrowed source by identity.</returns>
    public TileSetSource GetSource(int sourceID) { ThrowIfDisposed(); return _sources[sourceID] ?? throw new InvalidOperationException("Source data has not been restored."); }
    /// <summary>Tests whether a live source exists under the identity.</summary>
    /// <param name="sourceID">Source identity; minus one erases or acts as a query wildcard where documented.</param>
    /// <returns>Tests whether a live source exists under the identity.</returns>
    public bool HasSource(int sourceID) { ThrowIfDisposed(); return _sources.TryGetValue(sourceID, out var source) && source is { IsDisposed: false }; }
    /// <summary>Returns the number of sources.</summary>
    /// <returns>Returns the number of sources.</returns>
    public int GetSourceCount() { ThrowIfDisposed(); return _sources.Count; }
    /// <summary>Returns the source identity at a sorted index.</summary>
    /// <param name="index">Zero-based sorted entry index.</param>
    /// <returns>Returns the source identity at a sorted index.</returns>
    public int GetSourceID(int index) { ThrowIfDisposed(); return _sources.Keys.ElementAt(index); }
    /// <summary>Returns the first free nonnegative source identity.</summary>
    /// <returns>Returns the first free nonnegative source identity.</returns>
    public int GetNextSourceID() { ThrowIfDisposed(); var id = 0; foreach (var used in _sources.Keys) { if (used != id) break; id = checked(id + 1); } return id; }

    private PhysicsLayer Layer(int index) { ThrowIfDisposed(); return PhysicsLayers[index]; }
    internal static void Positive(Vector2i value) { if (value.X <= 0 || value.Y <= 0) throw new ArgumentOutOfRangeException(nameof(value)); }
    internal static void Move<T>(List<T> list, int from, int to)
    {
        var item = list[from]; if ((uint)to > (uint)list.Count) throw new ArgumentOutOfRangeException(nameof(to));
        list.Insert(to, item); list.RemoveAt(to < from ? from + 1 : from);
    }
    private void ChangedSchema() { NotifyPropertyListChanged(); EmitChanged(); }
    private void SourceChanged(Resource _) => EmitChanged();
    private void SourceDisposed(ElectronObject source) => RemoveSourceInstance((TileSetSource)source);
    private void RemoveSourceInstance(TileSetSource source)
    { foreach (var pair in _sources) if (ReferenceEquals(pair.Value, source)) { RemoveSource(pair.Key); return; } }
    private void DetachSourceInstance(TileSetSource source)
    {
        var id = _sources.First(p => ReferenceEquals(p.Value, source)).Key;
        _sources.Remove(id); source.Changed -= SourceChanged; source.Disposed -= SourceDisposed; source.OwnerSet = null;
    }
    private void ObserveMaterial(PhysicsMaterial? material) { if (material is null) return; material.Changed += SourceChanged; material.Disposed += MaterialDisposed; }
    private void UnobserveMaterial(PhysicsMaterial? material) { if (material is null) return; material.Changed -= SourceChanged; material.Disposed -= MaterialDisposed; }
    private void MaterialDisposed(ElectronObject material)
    { foreach (var layer in PhysicsLayers) if (ReferenceEquals(layer.Material, material)) { UnobserveMaterial(layer.Material); layer.Material = null; } EmitChanged(); }
    private void ClearContents()
    {
        foreach (var layer in PhysicsLayers) UnobserveMaterial(layer.Material); PhysicsLayers.Clear();
        foreach (var source in _sources.Values) if (source is not null) { source.Changed -= SourceChanged; source.Disposed -= SourceDisposed; source.OwnerSet = null; }
        _sources.Clear();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) ClearContents(); base.Dispose(disposing); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new TileSet();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    {
        var copy = (TileSet)target; copy.ClearContents(); copy._tileSize = _tileSize;
        foreach (var layer in PhysicsLayers)
        {
            copy.PhysicsLayers.Add(new() { Layer = layer.Layer, Mask = layer.Mask, Priority = layer.Priority });
            copy.SetPhysicsLayerPhysicsMaterial(copy.PhysicsLayers.Count - 1, (PhysicsMaterial?)(deep ? duplicate(layer.Material) : layer.Material));
        }
        foreach (var pair in _sources)
        {
            if (pair.Value is null) continue;
            var source = (TileSetSource)force(pair.Value)!;
            if (ReferenceEquals(source, pair.Value)) source = (TileSetSource)pair.Value.Duplicate();
            copy.AddSource(source, pair.Key);
        }
    }
    private void RestoreSourceIDs(int[] ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Any(id => id < 0) || ids.Distinct().Count() != ids.Length) throw new ArgumentException("Source identities must be unique and nonnegative.", nameof(ids));
        foreach (var id in _sources.Keys.Except(ids).ToArray()) RemoveSource(id);
        foreach (var id in ids) _sources.TryAdd(id, null);
        ChangedSchema();
    }
    private void RestoreSource(int id, TileSetSource? source)
    {
        if (_sources.TryGetValue(id, out var old) && ReferenceEquals(old, source)) return;
        if (old is not null) RemoveSource(id);
        if (source is not null) AddSource(source, id); else _sources[id] = null;
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<TileSet, Vector2i>(nameof(TileSize), n => n.TileSize, (n, v) => n.TileSize = v, _ => new(16, 16), stored: true);
        yield return new PropertyDescriptor<TileSet, int>("PhysicsLayerCount", n => n.GetPhysicsLayersCount(), (n, v) => n.RestoreLayerCount(v), _ => 0, stored: true);
        for (var i = 0; i < PhysicsLayers.Count; i++)
        {
            var id = i; var prefix = $"Physics/{id}/";
            yield return new PropertyDescriptor<TileSet, uint>(prefix + "Layer", n => n.GetPhysicsLayerCollisionLayer(id), (n, v) => n.SetPhysicsLayerCollisionLayer(id, v), _ => 1u, stored: true);
            yield return new PropertyDescriptor<TileSet, uint>(prefix + "Mask", n => n.GetPhysicsLayerCollisionMask(id), (n, v) => n.SetPhysicsLayerCollisionMask(id, v), _ => 1u, stored: true);
            yield return new PropertyDescriptor<TileSet, float>(prefix + "Priority", n => n.GetPhysicsLayerCollisionPriority(id), (n, v) => n.SetPhysicsLayerCollisionPriority(id, v), _ => 1, stored: true);
            yield return new PropertyDescriptor<TileSet, PhysicsMaterial?>(prefix + "Material", n => n.GetPhysicsLayerPhysicsMaterial(id), (n, v) => n.SetPhysicsLayerPhysicsMaterial(id, v), _ => null, stored: true);
        }
        yield return new PropertyDescriptor<TileSet, int[]>("SourceIDs", n => n._sources.Keys.ToArray(), (n, v) => n.RestoreSourceIDs(v), _ => [], stored: true);
        foreach (var sourceID in _sources.Keys)
        {
            var id = sourceID;
            yield return new PropertyDescriptor<TileSet, TileSetSource?>("Sources/" + id, n => n._sources[id], (n, v) => n.RestoreSource(id, v), _ => null, stored: true);
        }
    }
    private void RestoreLayerCount(int count)
    {
        ThrowIfDisposed(); if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        while (PhysicsLayers.Count > count) RemovePhysicsLayer(PhysicsLayers.Count - 1);
        while (PhysicsLayers.Count < count) AddPhysicsLayer();
    }
}
