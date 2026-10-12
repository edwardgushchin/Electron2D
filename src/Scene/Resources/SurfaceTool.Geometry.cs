namespace Electron2D;

public sealed partial class SurfaceTool
{
    /// <summary>Replaces geometry with a validated copied surface and its borrowed material.</summary>
    /// <param name="existing">Live source mesh.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <remarks>Imported geometry can be indexed, expanded, appended and committed; Begin starts a new draft.</remarks>
    /// <exception cref="InvalidOperationException">Source callbacks mutate their mesh or the receiving builder.</exception>
    public void CreateFrom(Mesh existing, int surface)
    {
        lock (_gate) { Mutable(); var source = ReadSource(existing, surface); Import(source.Data, source.Primitive, source.Material); }
    }
    /// <summary>Replaces geometry with validated copied typed arrays.</summary>
    /// <param name="arrays">Finite supported channel arrays and complete topology.</param>
    /// <param name="primitiveType">Topology, triangles by default.</param>
    /// <exception cref="ArgumentException">The arrays, counts or topology are invalid.</exception>
    public void CreateFromArrays(MeshSurfaceData arrays, Mesh.PrimitiveType primitiveType = Mesh.PrimitiveType.Triangles)
    {
        lock (_gate) { Mutable(); ArgumentNullException.ThrowIfNull(arrays); var copy = arrays.Copy(); copy.Validate(primitiveType); Import(copy, primitiveType, null); }
    }
    /// <summary>Appends copied transformed geometry while preserving existing draw order and attributes.</summary>
    /// <param name="existing">Live source mesh.</param>
    /// <param name="surface">Existing zero-based source surface.</param>
    /// <param name="transform">Finite local two-dimensional transform.</param>
    /// <remarks>Empty geometry adopts source topology/skin width. Nonempty inputs must share topology and skin
    /// presence/width. Missing color/UV channels use white/zero. Mixed indexing expands sequential order to indices.</remarks>
    /// <exception cref="ArgumentException">Topology, skin width, indices or transformed positions are incompatible.</exception>
    public void AppendFrom(Mesh existing, int surface, Transform transform)
    {
        lock (_gate)
        {
            Mutable(); if (!transform.X.IsFinite() || !transform.Y.IsFinite() || !transform.Origin.IsFinite()) throw new ArgumentException("Transform must be finite.", nameof(transform));
            var source = ReadSource(existing, surface); var appended = Vertices(source.Data); var oldCount = _vertices.Count;
            if (oldCount != 0 && (source.Primitive != _primitive || source.Data.SkinSlots != CapturedSkinSlots)) throw new ArgumentException("Appended topology and skin width must match existing geometry.");
            ValidateIndices();
            for (var i = 0; i < appended.Count; i++) { var position = transform * appended[i].Position; if (!position.IsFinite()) throw new ArgumentException("Transformed positions must be finite.", nameof(transform)); appended[i] = appended[i] with { Position = position }; }
            var vertices = new List<Vertex>(checked(oldCount + appended.Count)); vertices.AddRange(_vertices); vertices.AddRange(appended);
            var indices = new List<int>();
            if (_indices.Count != 0 || source.Data.Indices.Length != 0)
            {
                indices.EnsureCapacity(checked((_indices.Count != 0 ? _indices.Count : oldCount) + (source.Data.Indices.Length != 0 ? source.Data.Indices.Length : appended.Count)));
                if (_indices.Count != 0) indices.AddRange(_indices); else for (var i = 0; i < oldCount; i++) indices.Add(i);
                if (source.Data.Indices.Length != 0) foreach (var index in source.Data.Indices) indices.Add(checked(oldCount + index)); else for (var i = 0; i < appended.Count; i++) indices.Add(checked(oldCount + i));
            }
            var sourceFormat = Format(source.Data); _format = (oldCount != 0 ? _format : 0) | sourceFormat;
            if (indices.Count != 0) _format |= Mesh.ArrayFormat.Index; else _format &= ~Mesh.ArrayFormat.Index;
            if (oldCount == 0) { _primitive = source.Primitive; _skin = source.Data.SkinSlots == 8 ? SkinWeightCount.Eight : SkinWeightCount.Four; }
            _vertices = vertices; _indices = indices; _skinDirty = true;
        }
    }
    /// <summary>Appends sequential triangles for a fan using the current supported attributes.</summary>
    /// <param name="vertices">Finite fan positions; fewer than three produce no triangles.</param>
    /// <param name="uvs">Optional finite primary coordinates, at most one per fan position.</param>
    /// <param name="colors">Optional finite colors, at most one per fan position.</param>
    /// <param name="uv2s">Optional finite secondary coordinates, at most one per fan position.</param>
    /// <remarks>Missing entries retain the current captured attribute at that point in fan traversal.
    /// Channels must be enabled before prior vertices. Existing indexed geometry retains its indices and gains
    /// indices for the new triangles. Invalid input leaves geometry and current values unchanged.</remarks>
    /// <exception cref="InvalidOperationException">No triangle draft exists or a new channel follows prior vertices.</exception>
    /// <exception cref="ArgumentException">Optional counts or values are invalid.</exception>
    public void AddTriangleFan(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<Vector2> uvs = default, ReadOnlySpan<Color> colors = default, ReadOnlySpan<Vector2> uv2s = default)
    {
        lock (_gate)
        {
            Active(); if (_primitive != Mesh.PrimitiveType.Triangles) throw new InvalidOperationException("Fans require triangle topology.");
            if (uvs.Length > vertices.Length || colors.Length > vertices.Length || uv2s.Length > vertices.Length) throw new ArgumentException("Fan attributes exceed their vertex count.");
            foreach (var v in vertices) if (!v.IsFinite()) throw new ArgumentException("Fan positions must be finite.", nameof(vertices));
            foreach (var v in uvs) if (!v.IsFinite()) throw new ArgumentException("Fan UVs must be finite.", nameof(uvs));
            foreach (var v in uv2s) if (!v.IsFinite()) throw new ArgumentException("Fan UV2s must be finite.", nameof(uv2s));
            foreach (var c in colors) if (!c.IsFinite()) throw new ArgumentException("Fan colors must be finite.", nameof(colors));
            if (vertices.Length < 3) return;
            if (uvs.Length != 0) CanActivate(Mesh.ArrayFormat.TexUV); if (uv2s.Length != 0) CanActivate(Mesh.ArrayFormat.TexUV2); if (colors.Length != 0) CanActivate(Mesh.ArrayFormat.Color);
            ValidateIndices(); PrepareSkin(); var current = _current;
            var count = checked((vertices.Length - 2) * 3); var appended = new List<Vertex>(count);
            for (var triangle = 1; triangle < vertices.Length - 1; triangle++)
                for (var corner = 0; corner < 3; corner++)
                {
                    var index = corner == 0 ? 0 : triangle + corner - 1;
                    if (index < uvs.Length) current = current with { UV = uvs[index] }; if (index < uv2s.Length) current = current with { UV2 = uv2s[index] }; if (index < colors.Length) current = current with { Color = colors[index] };
                    appended.Add(current with { Position = vertices[index] });
                }
            var oldCount = _vertices.Count; _vertices.EnsureCapacity(checked(oldCount + count)); if (_indices.Count != 0) _indices.EnsureCapacity(checked(_indices.Count + count));
            if (_indices.Count != 0) for (var i = 0; i < count; i++) _indices.Add(oldCount + i);
            _vertices.AddRange(appended); _current = current; _format |= Mesh.ArrayFormat.Vertex;
            if (uvs.Length != 0) _format |= Mesh.ArrayFormat.TexUV; if (uv2s.Length != 0) _format |= Mesh.ArrayFormat.TexUV2; if (colors.Length != 0) _format |= Mesh.ArrayFormat.Color;
        }
    }
    /// <summary>Replaces sequential vertices with exact unique records and their original ordered indices.</summary>
    /// <remarks>Includes enabled attributes, skin values and smooth groups. Existing indices are retained unchanged.</remarks>
    public void Index()
    {
        lock (_gate)
        {
            Mutable(); if (_indices.Count != 0) return;
            var unique = new Dictionary<Vertex, int>(_vertices.Count); var vertices = new List<Vertex>(_vertices.Count); var indices = new List<int>(_vertices.Count);
            foreach (var vertex in _vertices) { if (!unique.TryGetValue(vertex, out var index)) { index = vertices.Count; unique.Add(vertex, index); vertices.Add(vertex); } indices.Add(index); }
            _vertices = vertices; _indices = indices; _format |= Mesh.ArrayFormat.Index;
        }
    }
    /// <summary>Expands current indices into ordered complete vertex records and removes the index channel.</summary>
    /// <exception cref="ArgumentException">An index does not address the current geometry; state is preserved.</exception>
    public void Deindex()
    {
        lock (_gate) { Mutable(); if (_indices.Count == 0) return; ValidateIndices(); var vertices = new List<Vertex>(_indices.Count); foreach (var index in _indices) vertices.Add(_vertices[index]); _vertices = vertices; _indices.Clear(); _format &= ~Mesh.ArrayFormat.Index; }
    }
    private int CapturedSkinSlots => (_format & (Mesh.ArrayFormat.Bones | Mesh.ArrayFormat.Weights)) != 0 ? (_skin == SkinWeightCount.Eight ? 8 : 4) : 0;
    private void ValidateIndices() { foreach (var index in _indices) if ((uint)index >= (uint)_vertices.Count) throw new ArgumentException("An index does not address captured geometry."); }
    private static Mesh.ArrayFormat Format(MeshSurfaceData data) => Mesh.ArrayFormat.Vertex | Mesh.ArrayFormat.Use2DVertices |
        (data.Colors.Length != 0 ? Mesh.ArrayFormat.Color : 0) | (data.UVs.Length != 0 ? Mesh.ArrayFormat.TexUV : 0) | (data.UV2s.Length != 0 ? Mesh.ArrayFormat.TexUV2 : 0) |
        (data.Indices.Length != 0 ? Mesh.ArrayFormat.Index : 0) | (data.Bones.Length != 0 ? Mesh.ArrayFormat.Bones | Mesh.ArrayFormat.Weights : 0) | (data.SkinSlots == 8 ? Mesh.ArrayFormat.Use8BoneWeights : 0);
    private static List<Vertex> Vertices(MeshSurfaceData data)
    {
        var result = new List<Vertex>(data.Vertices.Length); var slots = data.SkinSlots;
        for (var i = 0; i < data.Vertices.Length; i++)
        {
            Vector4i bones = default, extraBones = default; Vector4 weights = default, extraWeights = default;
            for (var j = 0; j < slots; j++) { if (j < 4) { bones[j] = data.Bones[i * slots + j]; weights[j] = data.Weights[i * slots + j]; } else { extraBones[j - 4] = data.Bones[i * slots + j]; extraWeights[j - 4] = data.Weights[i * slots + j]; } }
            result.Add(new(data.Vertices[i], data.Colors.Length != 0 ? data.Colors[i] : Colors.White, data.UVs.Length != 0 ? data.UVs[i] : default, data.UV2s.Length != 0 ? data.UV2s[i] : default, 0, bones, extraBones, weights, extraWeights));
        }
        return result;
    }
    private void Import(MeshSurfaceData data, Mesh.PrimitiveType primitive, Material? material)
    {
        var vertices = Vertices(data); var indices = new List<int>(data.Indices); var format = Format(data); Reset(); _vertices = vertices; _indices = indices; _primitive = primitive; _material = material; _format = format; _skin = data.SkinSlots == 8 ? SkinWeightCount.Eight : SkinWeightCount.Four;
    }
    private readonly record struct Source(MeshSurfaceData Data, Mesh.PrimitiveType Primitive, Material? Material);
    private Source ReadSource(Mesh mesh, int surface)
    {
        ArgumentNullException.ThrowIfNull(mesh); _readingSource = true;
        try
        {
            var revision = mesh.ChangeRevision; if ((uint)surface >= (uint)mesh.GetSurfaceCount()) throw new ArgumentOutOfRangeException(nameof(surface));
            var primitive = mesh.SurfaceGetPrimitiveType(surface); var data = (mesh.SurfaceGetArrays(surface) ?? throw new ArgumentException("Source surface arrays must not be null.", nameof(mesh))).Copy(); var format = mesh.SurfaceGetFormat(surface); var material = mesh.SurfaceGetMaterial(surface);
            if (mesh.IsDisposed || material is { IsDisposed: true }) throw new ObjectDisposedException("Source mesh or material"); if (revision != mesh.ChangeRevision) throw new InvalidOperationException("Mesh source callbacks must retain a stable revision."); data.Validate(primitive);
            const Mesh.ArrayFormat policies = Mesh.ArrayFormat.Use2DVertices | Mesh.ArrayFormat.UseDynamicUpdate | Mesh.ArrayFormat.Use8BoneWeights;
            const Mesh.ArrayFormat channels = Mesh.ArrayFormat.Vertex | Mesh.ArrayFormat.Color | Mesh.ArrayFormat.TexUV | Mesh.ArrayFormat.TexUV2 | Mesh.ArrayFormat.Index | Mesh.ArrayFormat.Bones | Mesh.ArrayFormat.Weights;
            if ((format & ~(policies | channels)) != 0) throw new NotSupportedException("Source channels require their typed storage and shader consumer.");
            if ((format & channels) != (Format(data) & channels) || data.SkinSlots != 0 && data.SkinSlots != ((format & Mesh.ArrayFormat.Use8BoneWeights) != 0 ? 8 : 4)) throw new ArgumentException("Source format must describe its actual channels and skin width.");
            return new(data, primitive, material);
        }
        finally { _readingSource = false; }
    }
}
