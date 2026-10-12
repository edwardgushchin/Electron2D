namespace Electron2D;

/// <summary>Builds and edits copied two-dimensional mesh surfaces for ordinary and indexed device drawing.</summary>
/// <remarks>Begin resets geometry, attributes and material while retaining the selected skin width.
/// Enable channels before the first vertex. Commit preserves the builder and borrows its material.
/// Authoring, snapshots and structural edits may allocate; completed meshes use the retained renderer.
/// Individual operations serialize; callers synchronize whole construction sequences shared across threads.</remarks>
public sealed partial class SurfaceTool : ElectronObject
{
    /// <summary>Selects the number of retained bone influences per vertex.</summary>
    public enum SkinWeightCount
    {
        /// <summary>Four influences per vertex.</summary>
        Four = 0,
        /// <summary>Eight influences per vertex.</summary>
        Eight = 1
    }
    private readonly record struct Vertex(Vector2 Position, Color Color, Vector2 UV, Vector2 UV2, uint Group,
        Vector4i Bones, Vector4i ExtraBones, Vector4 Weights, Vector4 ExtraWeights);
    private readonly object _gate = new();
    private List<Vertex> _vertices = [];
    private List<int> _indices = [];
    private Vertex _current = new(default, Colors.White, default, default, 0, default, default, default, default);
    private Mesh.PrimitiveType _primitive = Mesh.PrimitiveType.Lines;
    private Mesh.ArrayFormat _format;
    private Material? _material;
    private SkinWeightCount _skin;
    private int[] _bones = [];
    private float[] _weights = [];
    private bool _begun, _skinDirty, _readingSource;
    /// <summary>Creates an empty builder with line topology and four influence slots.</summary>
    public SurfaceTool() { }
    /// <summary>Clears prior authoring and starts a surface.</summary>
    /// <param name="primitive">One of the five supported mesh topologies.</param>
    /// <remarks>Retains the selected skin width. An existing draft is replaced.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The topology is invalid.</exception>
    public void Begin(Mesh.PrimitiveType primitive)
    {
        lock (_gate) { Mutable(); if (primitive is < Mesh.PrimitiveType.Points or > Mesh.PrimitiveType.TriangleStrip) throw new ArgumentOutOfRangeException(nameof(primitive)); var skin = _skin; Reset(); _skin = skin; _primitive = primitive; _begun = true; }
    }
    /// <summary>Clears geometry, attributes and borrowed material and restores four-slot line defaults.</summary>
    public void Clear() { lock (_gate) { Mutable(); Reset(); } }
    /// <summary>Gets the selected primitive topology, including while empty.</summary>
    /// <returns>Lines initially and after Clear.</returns>
    public Mesh.PrimitiveType GetPrimitiveType() { lock (_gate) { ThrowIfDisposed(); return _primitive; } }
    /// <summary>Gets the selected influence-width mode.</summary>
    /// <returns>Four initially; imported skin selects its actual width.</returns>
    public SkinWeightCount GetSkinWeightCount() { lock (_gate) { ThrowIfDisposed(); return _skin; } }
    /// <summary>Selects four or eight influence slots before any vertex is captured.</summary>
    /// <param name="count">An enum value, not the literal slot count.</param>
    /// <remarks>Works before Begin or before its first vertex; Begin retains this choice.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The mode is invalid.</exception>
    /// <exception cref="InvalidOperationException">Geometry has already been captured.</exception>
    public void SetSkinWeightCount(SkinWeightCount count)
    {
        lock (_gate) { Mutable(); if (count is not (SkinWeightCount.Four or SkinWeightCount.Eight)) throw new ArgumentOutOfRangeException(nameof(count)); if (_vertices.Count != 0 && count != _skin) throw new InvalidOperationException("Select skin width before capturing vertices."); _skin = count; _skinDirty = true; }
    }
    /// <summary>Enables vertex color before the first vertex, or changes its subsequent captured value.</summary>
    /// <param name="color">Finite color; mesh commit applies RGBA8 storage precision.</param>
    /// <exception cref="ArgumentException">A component is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">No draft exists or a new channel is enabled after a vertex.</exception>
    public void SetColor(Color color) { lock (_gate) { Active(); if (!color.IsFinite()) throw new ArgumentException("Color must be finite.", nameof(color)); Activate(Mesh.ArrayFormat.Color); _current = _current with { Color = color }; } }
    /// <summary>Enables primary UV before the first vertex, or changes its subsequent captured value.</summary>
    /// <param name="uv">Finite primary coordinate.</param>
    /// <exception cref="ArgumentException">A component is nonfinite.</exception>
    public void SetUV(Vector2 uv) { lock (_gate) { Active(); if (!uv.IsFinite()) throw new ArgumentException("UV must be finite.", nameof(uv)); Activate(Mesh.ArrayFormat.TexUV); _current = _current with { UV = uv }; } }
    /// <summary>Enables secondary UV before the first vertex, or changes its subsequent captured value.</summary>
    /// <param name="uv2">Finite coordinate independent of primary sampling and instance data.</param>
    /// <exception cref="ArgumentException">A component is nonfinite.</exception>
    public void SetUV2(Vector2 uv2) { lock (_gate) { Active(); if (!uv2.IsFinite()) throw new ArgumentException("UV2 must be finite.", nameof(uv2)); Activate(Mesh.ArrayFormat.TexUV2); _current = _current with { UV2 = uv2 }; } }
    /// <summary>Copies the current bone indices before capturing a vertex.</summary>
    /// <param name="bones">Indices zero through 65535, paired with current weights.</param>
    /// <remarks>Short lists are padded; overfull lists retain the strongest four/eight influences.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An index is outside the stored range.</exception>
    public void SetBones(ReadOnlySpan<int> bones)
    {
        lock (_gate) { Active(); foreach (var bone in bones) if ((uint)bone > 65535) throw new ArgumentOutOfRangeException(nameof(bones)); CanActivate(Mesh.ArrayFormat.Bones); var copy = bones.ToArray(); _bones = copy; _format |= Mesh.ArrayFormat.Bones; _skinDirty = true; }
    }
    /// <summary>Copies current finite influence weights before capturing a vertex.</summary>
    /// <param name="weights">Values paired with current bones; commit clamps to UNORM16.</param>
    /// <remarks>Only overfull lists normalize their retained strongest influences; shorter lists pad zero.</remarks>
    /// <exception cref="ArgumentException">A value is nonfinite.</exception>
    public void SetWeights(ReadOnlySpan<float> weights)
    {
        lock (_gate) { Active(); foreach (var weight in weights) if (!float.IsFinite(weight)) throw new ArgumentException("Weights must be finite.", nameof(weights)); CanActivate(Mesh.ArrayFormat.Weights); var copy = weights.ToArray(); _weights = copy; _format |= Mesh.ArrayFormat.Weights; _skinDirty = true; }
    }
    /// <summary>Sets the group identity captured by future vertices and retained by exact indexing.</summary>
    /// <param name="index">A 32-bit group identity; minus one denotes the flat group.</param>
    public void SetSmoothGroup(int index) { lock (_gate) { Mutable(); _current = _current with { Group = unchecked((uint)index) }; } }
    /// <summary>Assigns a live borrowed surface material, including on imported geometry.</summary>
    /// <param name="material">Material or null for the drawing item's inherited material.</param>
    /// <exception cref="ObjectDisposedException">The material or builder is disposed.</exception>
    public void SetMaterial(Material? material) { lock (_gate) { Mutable(); if (material is { IsDisposed: true }) throw new ObjectDisposedException(nameof(material)); _material = material; } }
    /// <summary>Captures a finite local vertex with the current enabled attributes.</summary>
    /// <param name="vertex">Two-dimensional local position.</param>
    /// <exception cref="ArgumentException">The position is nonfinite or influence counts do not match.</exception>
    /// <exception cref="InvalidOperationException">No draft exists or only one skin channel is enabled.</exception>
    public void AddVertex(Vector2 vertex)
    {
        lock (_gate) { Active(); if (!vertex.IsFinite()) throw new ArgumentException("Position must be finite.", nameof(vertex)); PrepareSkin(); _vertices.Add(_current with { Position = vertex }); _format |= Mesh.ArrayFormat.Vertex; }
    }
    /// <summary>Appends a nonnegative index, permitting references to vertices authored later.</summary>
    /// <param name="index">Zero-based vertex index; Commit validates its final bounds.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is negative.</exception>
    public void AddIndex(int index) { lock (_gate) { Active(); if (index < 0) throw new ArgumentOutOfRangeException(nameof(index)); _indices.Add(index); _format |= Mesh.ArrayFormat.Index; } }
    /// <summary>Returns independent typed arrays for the current authoring state.</summary>
    /// <returns>Caller-owned channels; empty without vertices. Pending topology/indices may be incomplete.</returns>
    public MeshSurfaceData CommitToArrays() { lock (_gate) { ThrowIfDisposed(); return Arrays(); } }
    /// <summary>Publishes one validated copied surface with its borrowed material and preserves the builder.</summary>
    /// <param name="existing">Target mesh, or null to create a caller-owned mesh.</param>
    /// <param name="flags">Supported mesh policies and matching channel flags.</param>
    /// <returns>The same target, or a newly owned mesh. Empty authoring adds no surface.</returns>
    /// <remarks>One Changed reports complete geometry/material publication. Listener errors retain the surface.</remarks>
    /// <exception cref="ArgumentException">Topology, final index bounds or explicit channels are invalid.</exception>
    /// <exception cref="NotSupportedException">Flags require an absent storage consumer.</exception>
    public ArrayMesh Commit(ArrayMesh? existing = null, Mesh.ArrayFormat flags = Mesh.ArrayFormat.None)
    {
        lock (_gate)
        {
            Mutable(); if (existing is { IsDisposed: true } || _material is { IsDisposed: true }) throw new ObjectDisposedException("Mesh or material");
            if ((flags & ~ArrayMesh.SupportedFormats) != 0) throw new NotSupportedException("Flags require an absent mesh storage consumer.");
            var data = Arrays(); if (data.Vertices.Length != 0) data.Validate(_primitive);
            var mesh = existing ?? new ArrayMesh();
            try { if (data.Vertices.Length != 0) mesh.AddSurface(_primitive, data, null, null, flags | (data.SkinSlots == 8 ? Mesh.ArrayFormat.Use8BoneWeights : 0), _material); return mesh; }
            catch { if (existing is null) mesh.Dispose(); throw; }
        }
    }
    /// <summary>Gets the local rectangle enclosing every stored vertex, including unreferenced vertices.</summary>
    /// <returns>Empty without geometry; otherwise finite nonnegative bounds.</returns>
    /// <exception cref="ArgumentException">The extent cannot be represented by finite floats.</exception>
    public Rect2 GetAABB()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (_vertices.Count == 0) return default; var min = _vertices[0].Position; var max = min;
            foreach (var v in _vertices) { min = new(Math.Min(min.X, v.Position.X), Math.Min(min.Y, v.Position.Y)); max = new(Math.Max(max.X, v.Position.X), Math.Max(max.Y, v.Position.Y)); }
            var size = max - min; if (!size.IsFinite()) throw new ArgumentException("Bounds exceed finite rectangle extents."); return new(min, size);
        }
    }
    private void Mutable() { ThrowIfDisposed(); if (_readingSource) throw new InvalidOperationException("Mesh source callbacks cannot mutate their receiving builder."); }
    private void Active() { Mutable(); if (!_begun) throw new InvalidOperationException("Begin a surface before appending vertices or attributes."); }
    private void CanActivate(Mesh.ArrayFormat flag) { if (_vertices.Count != 0 && (_format & flag) == 0) throw new InvalidOperationException("Enable channels before capturing the first vertex."); }
    private void Activate(Mesh.ArrayFormat flag) { CanActivate(flag); _format |= flag; }
    private void Reset()
    {
        _vertices.Clear(); _indices.Clear(); _current = new(default, Colors.White, default, default, 0, default, default, default, default);
        _bones = []; _weights = []; _material = null; _skin = SkinWeightCount.Four; _format = 0; _primitive = Mesh.PrimitiveType.Lines; _begun = _skinDirty = false;
    }
    private void PrepareSkin()
    {
        var skin = _format & (Mesh.ArrayFormat.Bones | Mesh.ArrayFormat.Weights); if (skin == 0) return;
        if (skin != (Mesh.ArrayFormat.Bones | Mesh.ArrayFormat.Weights)) throw new InvalidOperationException("Enable both bones and weights before capturing a skinned vertex.");
        if (_bones.Length != _weights.Length) throw new ArgumentException("Current bone and weight counts must match.");
        if (!_skinDirty) return;
        var slots = _skin == SkinWeightCount.Eight ? 8 : 4;
        Span<int> selected = stackalloc int[8]; Span<int> bones = stackalloc int[8]; Span<float> weights = stackalloc float[8]; selected.Fill(-1); bones.Clear(); weights.Clear();
        if (_bones.Length <= slots) for (var i = 0; i < _bones.Length; i++) selected[i] = i;
        else for (var i = 0; i < _weights.Length; i++)
                for (var j = 0; j < slots; j++) if (selected[j] < 0 || _weights[i] > _weights[selected[j]])
                    { for (var k = slots - 1; k > j; k--) selected[k] = selected[k - 1]; selected[j] = i; break; }
        double total = 0; for (var i = 0; i < slots; i++) if (selected[i] >= 0) { bones[i] = _bones[selected[i]]; weights[i] = _weights[selected[i]]; total += weights[i]; }
        if (_bones.Length > slots) for (var i = 0; i < slots; i++) weights[i] = total > 0 ? (float)(weights[i] / total) : 0;
        _current = _current with { Bones = new(bones[0], bones[1], bones[2], bones[3]), ExtraBones = new(bones[4], bones[5], bones[6], bones[7]), Weights = new(weights[0], weights[1], weights[2], weights[3]), ExtraWeights = new(weights[4], weights[5], weights[6], weights[7]) }; _skinDirty = false;
    }
    private MeshSurfaceData Arrays()
    {
        var count = _vertices.Count; var skin = (_format & (Mesh.ArrayFormat.Bones | Mesh.ArrayFormat.Weights)) != 0; var slots = _skin == SkinWeightCount.Eight ? 8 : 4;
        var data = new MeshSurfaceData { Vertices = new Vector2[count], Colors = (_format & Mesh.ArrayFormat.Color) != 0 ? new Color[count] : [], UVs = (_format & Mesh.ArrayFormat.TexUV) != 0 ? new Vector2[count] : [], UV2s = (_format & Mesh.ArrayFormat.TexUV2) != 0 ? new Vector2[count] : [], Indices = _indices.ToArray(), Bones = skin ? new int[checked(count * slots)] : [], Weights = skin ? new float[checked(count * slots)] : [] };
        for (var i = 0; i < count; i++)
        {
            var v = _vertices[i]; data.Vertices[i] = v.Position; if (data.Colors.Length != 0) data.Colors[i] = v.Color; if (data.UVs.Length != 0) data.UVs[i] = v.UV; if (data.UV2s.Length != 0) data.UV2s[i] = v.UV2;
            if (skin) for (var j = 0; j < slots; j++) { data.Bones[i * slots + j] = j < 4 ? v.Bones[j] : v.ExtraBones[j - 4]; data.Weights[i * slots + j] = j < 4 ? v.Weights[j] : v.ExtraWeights[j - 4]; }
        }
        return data;
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() { lock (_gate) { if (_readingSource) throw new InvalidOperationException("Cannot dispose a builder during a mesh source callback."); } base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (_gate) Reset(); base.Dispose(disposing); }
}
