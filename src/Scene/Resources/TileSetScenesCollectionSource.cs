namespace Electron2D;

/// <summary>Exposes borrowed canvas-rooted scenes as alternatives of the single tile at (0,0).</summary>
/// <remarks>Placed scenes become ordinary children of an enabled tile layer after deferred publication.
/// Definitions retain nonnegative authored identities; cell placement follows the existing transform-bit encoding. Editor placeholder policy is not exposed.</remarks>
public sealed class TileSetScenesCollectionSource : TileSetSource
{
    private readonly SortedList<int, PackedScene?> _scenes = [];
    private readonly Dictionary<PackedScene, int> _sceneReferences = [];
    private int _nextSceneID = 1;
    /// <summary>Creates an empty scene collection with one tile slot and no alternatives.</summary>
    public TileSetScenesCollectionSource() { }
    /// <summary>Creates a scene alternative with a free or explicitly selected identity.</summary>
    /// <param name="packedScene">Borrowed live scene with a canvas root, or null for an empty definition.</param>
    /// <param name="idOverride">Unoccupied nonnegative authored identity, or minus one for automatic selection starting at one.</param>
    /// <returns>The assigned scene identity.</returns>
    /// <exception cref="ArgumentException">The identity is occupied or the scene is empty or has a noncanvas root.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The identity is negative.</exception>
    /// <exception cref="InvalidOperationException">All automatic identities are occupied.</exception>
    public int CreateSceneTile(PackedScene? packedScene, int idOverride = -1)
    {
        ThrowIfDisposed(); if (idOverride < -1) throw new ArgumentOutOfRangeException(nameof(idOverride));
        var id = idOverride == -1 ? GetNextSceneTileID() : idOverride;
        if (id < 0) throw new InvalidOperationException("No scene tile identity is available.");
        CheckID(id); if (_scenes.ContainsKey(id)) throw new ArgumentException("Scene identity is occupied.", nameof(idOverride));
        ValidateScene(packedScene); _scenes.Add(id, packedScene); Observe(packedScene); AdvanceID(); EmitChanged(); return id;
    }
    /// <summary>Returns the next automatically selected identity, or minus one when all nonzero identities are occupied.</summary>
    /// <returns>An available scene identity starting at one, or minus one.</returns>
    public int GetNextSceneTileID() { ThrowIfDisposed(); return _nextSceneID; }
    /// <summary>Returns a scene identity in ascending order.</summary>
    /// <param name="index">Zero-based scene index.</param>
    /// <returns>The stored identity without cell transform flags.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside this collection.</exception>
    public int GetSceneTileID(int index) { ThrowIfDisposed(); if ((uint)index >= (uint)_scenes.Count) throw new ArgumentOutOfRangeException(nameof(index)); return _scenes.Keys[index]; }
    /// <summary>Returns an exact authored scene definition, falling back to a transformed cell identity when that exact definition is absent.</summary>
    /// <param name="id">Scene identity with optional cell transform flags.</param>
    /// <returns>The borrowed scene or null for an empty definition.</returns>
    /// <exception cref="KeyNotFoundException">The scene identity is absent.</exception>
    public PackedScene? GetSceneTileScene(int id) { ThrowIfDisposed(); return _scenes.TryGetValue(id, out var scene) ? scene : _scenes[id & ~TileSetAtlasSource.TransformMask]; }
    internal PackedScene? SceneForCell(int alternative) => _scenes[alternative & ~TileSetAtlasSource.TransformMask];
    /// <summary>Returns the number of scene definitions, including empty definitions.</summary>
    /// <returns>The scene definition count.</returns>
    public int GetSceneTilesCount() { ThrowIfDisposed(); return _scenes.Count; }
    /// <summary>Tests whether an exact authored scene identity exists.</summary>
    /// <param name="id">Untransformed authored identity.</param>
    /// <returns>Whether that exact identity is present.</returns>
    public bool HasSceneTileID(int id) { ThrowIfDisposed(); return _scenes.ContainsKey(id); }
    /// <summary>Removes a scene definition without disposing its borrowed scene.</summary>
    /// <param name="id">Existing untransformed scene identity.</param>
    /// <exception cref="KeyNotFoundException">The scene identity is absent.</exception>
    public void RemoveSceneTile(int id) { ThrowIfDisposed(); var scene = _scenes[id]; _scenes.Remove(id); Unobserve(scene); if (_nextSceneID < 0) { _nextSceneID = 1; AdvanceID(); } EmitChanged(); }
    /// <summary>Renames a scene definition without rewriting already authored cell identities.</summary>
    /// <param name="id">Existing untransformed scene identity.</param>
    /// <param name="newID">Unoccupied nonnegative authored identity.</param>
    /// <exception cref="KeyNotFoundException">The old identity is absent.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The new identity is negative.</exception>
    /// <exception cref="ArgumentException">The new identity is occupied.</exception>
    public void SetSceneTileID(int id, int newID)
    { ThrowIfDisposed(); CheckID(newID); var scene = _scenes[id]; if (id == newID) return; if (_scenes.ContainsKey(newID)) throw new ArgumentException("Scene identity is occupied.", nameof(newID)); _scenes.Add(newID, scene); _scenes.Remove(id); AdvanceID(); EmitChanged(); }
    /// <summary>Replaces a borrowed scene definition and publishes assignments, including equal assignments.</summary>
    /// <param name="id">Existing untransformed scene identity.</param>
    /// <param name="packedScene">Live canvas-rooted scene, or null to clear the definition.</param>
    /// <exception cref="KeyNotFoundException">The scene identity is absent.</exception>
    /// <exception cref="ArgumentException">The scene is empty or its root is not a canvas item.</exception>
    public void SetSceneTileScene(int id, PackedScene? packedScene)
    { ThrowIfDisposed(); var old = _scenes[id]; ValidateScene(packedScene); Unobserve(old); _scenes[id] = packedScene; Observe(packedScene); EmitChanged(); }
    /// <inheritdoc />
    public override int GetTilesCount() { ThrowIfDisposed(); return 1; }
    /// <inheritdoc />
    public override Vector2i GetTileID(int index) { ThrowIfDisposed(); if (index != 0) throw new ArgumentOutOfRangeException(nameof(index)); return Vector2i.Zero; }
    /// <inheritdoc />
    public override bool HasTile(Vector2i atlasCoords) { ThrowIfDisposed(); return atlasCoords == Vector2i.Zero; }
    /// <inheritdoc />
    public override int GetAlternativeTilesCount(Vector2i atlasCoords) { ThrowIfDisposed(); return _scenes.Count; }
    /// <inheritdoc />
    public override int GetAlternativeTileID(Vector2i atlasCoords, int index) { ThrowIfDisposed(); if (atlasCoords != Vector2i.Zero) throw new ArgumentOutOfRangeException(nameof(atlasCoords)); return GetSceneTileID(index); }
    /// <inheritdoc />
    public override bool HasAlternativeTile(Vector2i atlasCoords, int alternativeTile) { ThrowIfDisposed(); return atlasCoords == Vector2i.Zero && alternativeTile >= 0 && _scenes.ContainsKey(alternativeTile & ~TileSetAtlasSource.TransformMask); }
    private static void CheckID(int id) { if (id < 0) throw new ArgumentOutOfRangeException(nameof(id)); }
    private static void ValidateScene(PackedScene? scene)
    { if (scene is not null && scene.CanvasRootType is null) throw new ArgumentException("A scene tile requires a nonempty canvas-rooted scene.", nameof(scene)); }
    private void AdvanceID()
    {
        if (_nextSceneID < 0) _nextSceneID = 1;
        for (var i = 0; i <= _scenes.Count; i++)
        { if (!_scenes.ContainsKey(_nextSceneID)) return; _nextSceneID = _nextSceneID % 1073741823 + 1; }
        _nextSceneID = -1;
    }
    private void Observe(PackedScene? scene) { if (scene is null) return; if (_sceneReferences.TryGetValue(scene, out var count)) { _sceneReferences[scene] = count + 1; return; } _sceneReferences.Add(scene, 1); scene.Changed += SceneChanged; scene.Disposed += SceneDisposed; }
    private void Unobserve(PackedScene? scene) { if (scene is null) return; var count = _sceneReferences[scene]; if (count > 1) { _sceneReferences[scene] = count - 1; return; } _sceneReferences.Remove(scene); scene.Changed -= SceneChanged; scene.Disposed -= SceneDisposed; }
    private void SceneChanged(Resource _) => EmitChanged();
    private void SceneDisposed(ElectronObject scene)
    { foreach (var id in _scenes.Where(p => ReferenceEquals(p.Value, scene)).Select(p => p.Key).ToArray()) { Unobserve(_scenes[id]); _scenes[id] = null; } EmitChanged(); }
    private void ClearDefinitions() { foreach (var scene in _scenes.Values) Unobserve(scene); _scenes.Clear(); _nextSceneID = 1; }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) ClearDefinitions(); base.Dispose(disposing); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new TileSetScenesCollectionSource();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    { var copy = (TileSetScenesCollectionSource)target; copy.ClearDefinitions(); foreach (var p in _scenes) { var scene = (PackedScene?)(deep ? duplicate(p.Value) : p.Value); copy._scenes.Add(p.Key, scene); copy.Observe(scene); } copy._nextSceneID = _nextSceneID; }
    private void RestoreIDs(int[] ids)
    { ArgumentNullException.ThrowIfNull(ids); if (ids.Distinct().Count() != ids.Length) throw new ArgumentException("Scene identities must be unique.", nameof(ids)); foreach (var id in ids) CheckID(id); ClearDefinitions(); foreach (var id in ids) _scenes.Add(id, null); AdvanceID(); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<TileSetScenesCollectionSource, int[]>("SceneIDs", n => n._scenes.Keys.ToArray(), (n, v) => n.RestoreIDs(v), _ => [], stored: true);
        foreach (var id in _scenes.Keys) { var key = id; yield return new PropertyDescriptor<TileSetScenesCollectionSource, PackedScene?>("Scenes/" + key, n => n._scenes[key], (n, v) => n.SetSceneTileScene(key, v), _ => null, stored: true); }
        yield return new PropertyDescriptor<TileSetScenesCollectionSource, int>("NextSceneID", n => n._nextSceneID, (n, v) => { if (v != -1) CheckID(v); if (v == 0 || v > 1073741823 || v == -1 && n._scenes.Count < 1073741823 || v >= 0 && n._scenes.ContainsKey(v)) throw new ArgumentException("Invalid next scene identity."); n._nextSceneID = v; }, _ => 1, stored: true);
    }
    internal override void ResizeOcclusionLayers(int count) { }
    internal override void InsertOcclusionLayer(int index) { }
    internal override void RemoveOcclusionLayer(int index) { }
    internal override void MoveOcclusionLayer(int from, int to) { }
    internal override void ResizeNavigationLayers(int count) { }
    internal override void InsertNavigationLayer(int index) { }
    internal override void RemoveNavigationLayer(int index) { }
    internal override void MoveNavigationLayer(int from, int to) { }
    internal override void ResizePhysicsLayers(int count) { }
    internal override void InsertPhysicsLayer(int index) { }
    internal override void RemovePhysicsLayer(int index) { }
    internal override void MovePhysicsLayer(int from, int to) { }
}
