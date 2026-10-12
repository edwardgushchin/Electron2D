namespace Electron2D;

/// <summary>Authors copied raw mesh surfaces and generates compiled drawable meshes and levels.</summary>
/// <remarks>Import/generation/copies are cold operations. Materials are borrowed. GetMesh returns a cached
/// caller-owned snapshot; edits invalidate the cache without disposing earlier returned meshes.
/// Individual operations serialize; callers synchronize complete authoring sequences.</remarks>
public sealed partial class ImporterMesh : Resource
{
    private sealed class Surface(MeshSurfaceData data, MeshSurfaceData[] shapes, MeshLOD[] lods, Mesh.PrimitiveType primitive, Mesh.ArrayFormat flags, string name, Material? material)
    {
        internal readonly MeshSurfaceData Data = data;
        internal readonly MeshSurfaceData[] Shapes = shapes;
        internal MeshLOD[] LODs = lods;
        internal readonly Mesh.PrimitiveType Primitive = primitive;
        internal readonly Mesh.ArrayFormat Flags = flags;
        internal string Name = name;
        internal Material? Material = material;
        internal Surface Copy() { var data = Data.Copy(); return new(data, Shapes.Select(s => s.Copy()).ToArray(), MeshLOD.Capture(MeshLOD.Copy(LODs), data, Primitive), Primitive, Flags, Name, Material); }
    }
    private readonly object _gate = new();
    private readonly List<Surface> _surfaces = [];
    private readonly List<string> _names = [];
    private BlendShapeMode _mode = BlendShapeMode.Normalized;
    private ArrayMesh? _cached;
    private long _cachedRevision = -1;
    private bool _preparing;
    /// <summary>Creates an empty normalized-mode producer.</summary>
    public ImporterMesh() { }
    /// <summary>Gets or selects the target mode used by subsequently compiled meshes.</summary>
    /// <value>Normalized initially; edits invalidate the compiled snapshot.</value>
    public BlendShapeMode BlendShapeMode
    {
        get { lock (_gate) { ThrowIfDisposed(); return _mode; } }
        set { lock (_gate) { Mutable(); if (value is not (BlendShapeMode.Normalized or BlendShapeMode.Relative)) throw new ArgumentOutOfRangeException(nameof(value)); _mode = value; Invalidate(); } EmitChanged(); }
    }
    private void Mutable() { ThrowIfDisposed(); if (_preparing) throw new InvalidOperationException("Importer callbacks cannot reenter authoring."); }
    private void Invalidate() { _cached = null; _cachedRevision = -1; }
    private Surface Get(int surface) { ThrowIfDisposed(); if ((uint)surface >= (uint)_surfaces.Count) throw new ArgumentOutOfRangeException(nameof(surface)); return _surfaces[surface]; }
    /// <summary>Adds a raw target name before surfaces exist; duplicate names retain their authored order.</summary>
    /// <param name="name">Non-null name; colons become underscores and compilation applies ordinary mesh collisions.</param>
    public void AddBlendShape(string name) { ArgumentNullException.ThrowIfNull(name); lock (_gate) { Mutable(); if (_surfaces.Count != 0) throw new InvalidOperationException("Names precede surfaces."); _names.Add(name.Replace(':', '_')); Invalidate(); } EmitChanged(); }
    /// <summary>Gets the number of raw target definitions.</summary>
    /// <returns>Nonnegative definition count.</returns>
    public int GetBlendShapeCount() { lock (_gate) { ThrowIfDisposed(); return _names.Count; } }
    /// <summary>Gets an exact raw target name.</summary>
    /// <param name="index">Existing definition index.</param>
    /// <returns>The stored name.</returns>
    public string GetBlendShapeName(int index) { lock (_gate) { ThrowIfDisposed(); if ((uint)index >= (uint)_names.Count) throw new ArgumentOutOfRangeException(nameof(index)); return _names[index]; } }
    /// <summary>Copies one complete raw surface without rendering quantization.</summary>
    /// <param name="primitive">One of the five supported topologies.</param>
    /// <param name="arrays">Finite typed channels, copied before publication.</param>
    /// <param name="blendShapes">One position/normal/tangent payload per raw name.</param>
    /// <param name="lods">Positive thresholds with shorter complete index sets over an indexed base.</param>
    /// <param name="material">Borrowed live material or null.</param>
    /// <param name="name">Non-null surface name.</param>
    /// <param name="flags">Supported raw channel/storage flags; kept exactly for GetSurfaceFormat.</param>
    /// <remarks>Channel/skin/schema failures leave prior surfaces intact. Changed runs after commit.</remarks>
    public void AddSurface(Mesh.PrimitiveType primitive, MeshSurfaceData arrays, IReadOnlyList<MeshSurfaceData>? blendShapes = null, IReadOnlyDictionary<float, int[]>? lods = null, Material? material = null, string name = "", Mesh.ArrayFormat flags = Mesh.ArrayFormat.None)
    {
        ArgumentNullException.ThrowIfNull(arrays); ArgumentNullException.ThrowIfNull(name);
        var data = arrays.Copy(); data.Validate(primitive); if (material is { IsDisposed: true }) throw new ObjectDisposedException(nameof(material));
        if ((flags & ~ArrayMesh.SupportedFormats) != 0) throw new NotSupportedException("Flags require absent mesh storage.");
        var mask = Mesh.ArrayFormat.Vertex | (data.Normals.Length != 0 ? Mesh.ArrayFormat.Normal : 0) | (data.Tangents.Length != 0 ? Mesh.ArrayFormat.Tangent : 0);
        var shapes = blendShapes?.Select(s => { ArgumentNullException.ThrowIfNull(s); var copy = s.Copy(); ArrayMesh.ValidateBlendShape(copy, data, mask); return copy; }).ToArray() ?? [];
        var levels = MeshLOD.Capture(lods, data, primitive);
        // The compiled importer is a real validation boundary, but raw values stay in this producer.
        using var validation = new ArrayMesh(); for (var i = 0; i < shapes.Length; i++) validation.AddBlendShape(i.ToString(System.Globalization.CultureInfo.InvariantCulture)); validation.AddSurfaceFromArrays(primitive, data, shapes, lods, flags);
        var prepared = new Surface(data, shapes, levels, primitive, flags, name, material);
        lock (_gate) { Mutable(); if (shapes.Length != _names.Count) throw new ArgumentException("Every surface requires one target per definition.", nameof(blendShapes)); _surfaces.Add(prepared); Invalidate(); }
        EmitChanged();
    }
    /// <summary>Gets the number of authored surfaces.</summary>
    /// <returns>Nonnegative surface count.</returns>
    public int GetSurfaceCount() { lock (_gate) { ThrowIfDisposed(); return _surfaces.Count; } }
    /// <summary>Gets a surface topology.</summary>
    /// <param name="surface">Existing surface index.</param>
    /// <returns>The authored topology.</returns>
    public Mesh.PrimitiveType GetSurfacePrimitiveType(int surface) { lock (_gate) return Get(surface).Primitive; }
    /// <summary>Gets independent copies of raw surface channels.</summary>
    /// <param name="surface">Existing surface index.</param>
    /// <returns>Copied arrays with authoring precision.</returns>
    public MeshSurfaceData GetSurfaceArrays(int surface) { lock (_gate) return Get(surface).Data.Copy(); }
    /// <summary>Gets the exact authored storage flags rather than inferred channels.</summary>
    /// <param name="surface">Existing surface index.</param>
    /// <returns>The AddSurface flags value.</returns>
    public Mesh.ArrayFormat GetSurfaceFormat(int surface) { lock (_gate) return Get(surface).Flags; }
    /// <summary>Gets a copied raw target payload.</summary>
    /// <param name="surface">Existing surface index.</param>
    /// <param name="shape">Existing definition index.</param>
    /// <returns>Copied position/normal/tangent arrays.</returns>
    public MeshSurfaceData GetSurfaceBlendShapeArrays(int surface, int shape) { lock (_gate) { var s = Get(surface); if ((uint)shape >= (uint)s.Shapes.Length) throw new ArgumentOutOfRangeException(nameof(shape)); return s.Shapes[shape].Copy(); } }
    /// <summary>Gets a stored surface name.</summary>
    /// <param name="surface">Existing surface index.</param>
    /// <returns>Exact ordinal name, possibly empty.</returns>
    public string GetSurfaceName(int surface) { lock (_gate) return Get(surface).Name; }
    /// <summary>Sets a surface name and invalidates the compiled snapshot.</summary>
    /// <param name="surface">Existing surface index.</param>
    /// <param name="name">Non-null name.</param>
    public void SetSurfaceName(int surface, string name) { ArgumentNullException.ThrowIfNull(name); lock (_gate) { Mutable(); Get(surface).Name = name; Invalidate(); } EmitChanged(); }
    /// <summary>Gets the borrowed surface material.</summary>
    /// <param name="surface">Existing surface index.</param>
    /// <returns>The authored material or null.</returns>
    public Material? GetSurfaceMaterial(int surface) { lock (_gate) return Get(surface).Material; }
    /// <summary>Sets a borrowed material and invalidates the compiled snapshot.</summary>
    /// <param name="surface">Existing surface index.</param>
    /// <param name="material">Live material or null.</param>
    public void SetSurfaceMaterial(int surface, Material? material) { if (material is { IsDisposed: true }) throw new ObjectDisposedException(nameof(material)); lock (_gate) { Mutable(); Get(surface).Material = material; Invalidate(); } EmitChanged(); }
    /// <summary>Gets the number of stored sorted levels.</summary>
    /// <param name="surface">Existing surface index.</param>
    /// <returns>Nonnegative LOD count.</returns>
    public int GetSurfaceLODCount(int surface) { lock (_gate) return Get(surface).LODs.Length; }
    private MeshLOD Level(int surface, int lod) { var s = Get(surface); if ((uint)lod >= (uint)s.LODs.Length) throw new ArgumentOutOfRangeException(nameof(lod)); return s.LODs[lod]; }
    /// <summary>Gets independent copies of one level's indices.</summary>
    /// <param name="surface">Existing surface index.</param>
    /// <param name="lod">Existing level in ascending-threshold order.</param>
    /// <returns>Copied indices into the raw base channels.</returns>
    public int[] GetSurfaceLODIndices(int surface, int lod) { lock (_gate) return (int[])Level(surface, lod).Data.Indices.Clone(); }
    /// <summary>Gets one level's local edge-error threshold.</summary>
    /// <param name="surface">Existing surface index.</param>
    /// <param name="lod">Existing level in ascending-threshold order.</param>
    /// <returns>Positive finite local error.</returns>
    public float GetSurfaceLODSize(int surface, int lod) { lock (_gate) return Level(surface, lod).Error; }
    /// <summary>Removes all raw surfaces, definitions and compiled cache; mode is retained.</summary>
    public void Clear() { lock (_gate) { Mutable(); _surfaces.Clear(); _names.Clear(); Invalidate(); } EmitChanged(); }
    /// <summary>Compiles a caller-owned drawable snapshot, cached until authoring changes.</summary>
    /// <param name="baseMesh">Optional live target; unshaped existing surfaces are appended atomically. Ignored when the current cache exists.</param>
    /// <returns>Cached ArrayMesh, or null when no surfaces exist.</returns>
    /// <remarks>Quantization occurs here. Earlier returned snapshots remain alive after edits/disposal of the producer.
    /// A target Changed listener failure retains committed geometry and the producer's cache.</remarks>
    public ArrayMesh? GetMesh(ArrayMesh? baseMesh = null)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); var revision = ChangeRevision;
            if (_cached is { IsDisposed: false } && _cachedRevision == revision) return _cached;
            if (_preparing) throw new InvalidOperationException("Mesh compilation cannot reenter."); if (_surfaces.Count == 0) return null;
            _preparing = true;
            try
            {
                using var draft = new ArrayMesh { ResourceName = ResourceName, BlendShapeMode = _mode }; foreach (var name in _names) draft.AddBlendShape(name);
                foreach (var s in _surfaces) { draft.AddSurface(s.Primitive, s.Data, s.Shapes, MeshLOD.Copy(s.LODs), s.Flags, s.Material); draft.SurfaceSetName(draft.GetSurfaceCount() - 1, s.Name); }
                if (revision != ChangeRevision || IsDisposed) throw new InvalidOperationException("Compilation requires stable live authoring state.");
                var target = baseMesh ?? new ArrayMesh(); try { target.AppendCompiled(draft); } catch { if (baseMesh is null) target.Dispose(); throw; }
                _cached = target; _cachedRevision = revision; target.ResourceName = ResourceName; ObjectDisposedException.ThrowIf(target.IsDisposed, target); return target;
            }
            finally { _preparing = false; }
        }
    }
    /// <summary>Copies applicable mesh state into a raw producer.</summary>
    /// <param name="mesh">Live source or null for an empty producer.</param>
    /// <returns>A new caller-owned ImporterMesh.</returns>
    public static ImporterMesh FromMesh(Mesh? mesh)
    {
        var result = new ImporterMesh(); if (mesh is null) return result;
        try
        {
            var revision = mesh.ChangeRevision; if (mesh is ArrayMesh arrays) result._mode = arrays.BlendShapeMode;
            for (var i = 0; i < mesh.GetBlendShapeCount(); i++) result.AddBlendShape(mesh.GetBlendShapeName(i));
            var count = mesh.GetSurfaceCount(); for (var i = 0; i < count; i++) { var material = mesh.SurfaceGetMaterial(i); var name = mesh is ArrayMesh a ? a.SurfaceGetName(i) : ""; if (name.Length == 0 && material is not null) name = material.ResourceName; result.AddSurface(mesh.SurfaceGetPrimitiveType(i), mesh.SurfaceGetArrays(i), mesh.SurfaceGetBlendShapeArrays(i), mesh.SurfaceGetLODs(i), material, name, mesh.SurfaceGetFormat(i)); }
            if (revision != mesh.ChangeRevision || mesh.IsDisposed) throw new InvalidOperationException("Source callbacks require stable live state."); result.ResourceName = mesh.ResourceName; return result;
        }
        catch { result.Dispose(); throw; }
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new ImporterMesh();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        Surface[] copies; string[] names; BlendShapeMode mode; lock (_gate) { ThrowIfDisposed(); copies = _surfaces.Select(s => s.Copy()).ToArray(); names = _names.ToArray(); mode = _mode; }
        foreach (var s in copies) s.Material = (Material?)duplicateSubresource(s.Material); var other = (ImporterMesh)target; lock (other._gate) { other.Mutable(); other._surfaces.Clear(); other._surfaces.AddRange(copies); other._names.Clear(); other._names.AddRange(names); other._mode = mode; other.Invalidate(); }
    }
    /// <inheritdoc />
    protected override void OnResetState() { lock (_gate) { Mutable(); Invalidate(); } }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (_gate) { if (_preparing) throw new InvalidOperationException("Cannot dispose during compilation."); _surfaces.Clear(); _names.Clear(); Invalidate(); } base.Dispose(disposing); }
}
