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
    /// presence/width. Missing color/UV/frame channels use white/zero. Normal data uses inverse-transpose XY basis; tangent data uses the basis and reflected orientation. Singular normal-bearing append rejects. Mixed indexing expands sequential order to indices. Present incoming custom formats replace the selected formats; existing values reencode, absent channels fill zero RGB/one alpha. All reencoding validates before mutation.</remarks>
    /// <exception cref="ArgumentException">Topology, skin width, indices or transformed positions are incompatible.</exception>
    public void AppendFrom(Mesh existing, int surface, Transform transform)
    {
        lock (_gate)
        {
            Mutable(); if (!transform.X.IsFinite() || !transform.Y.IsFinite() || !transform.Origin.IsFinite()) throw new ArgumentException("Transform must be finite.", nameof(transform));
            var source = ReadSource(existing, surface); var appended = Vertices(source.Data); var oldCount = _vertices.Count;
            if (oldCount != 0 && (source.Primitive != _primitive || source.Data.SkinSlots != CapturedSkinSlots)) throw new ArgumentException("Appended topology and skin width must match existing geometry.");
            ValidateIndices();
            var determinant = (double)transform.X.X * transform.Y.Y - (double)transform.X.Y * transform.Y.X;
            if (source.Data.Normals.Length != 0 && determinant == 0) throw new ArgumentException("Normal-bearing append requires an invertible basis.", nameof(transform));
            for (var i = 0; i < appended.Count; i++)
            {
                var vertex = appended[i]; var position = transform * vertex.Position;
                if (!position.IsFinite()) throw new ArgumentException("Transformed positions must be finite.", nameof(transform));
                var normal = vertex.Normal; var tangent = vertex.Tangent;
                if (source.Data.Normals.Length != 0) normal = MeshFrameCodec.Unit((transform.Y.Y * (double)normal.X - transform.X.Y * (double)normal.Y) / determinant, (transform.X.X * (double)normal.Y - transform.Y.X * (double)normal.X) / determinant, normal.Z);
                if (source.Data.Tangents.Length != 0) { var direction = MeshFrameCodec.Unit(transform.X.X * (double)tangent.X + transform.Y.X * (double)tangent.Y, transform.X.Y * (double)tangent.X + transform.Y.Y * (double)tangent.Y, tangent.Z); tangent = new(direction.X, direction.Y, direction.Z, tangent.W * (determinant < 0 ? -1 : 1)); }
                appended[i] = vertex with { Position = position, Normal = normal, Tangent = tangent };
            }
            var vertices = new List<Vertex>(checked(oldCount + appended.Count)); vertices.AddRange(_vertices); vertices.AddRange(appended);
            var indices = new List<int>();
            if (_indices.Count != 0 || source.Data.Indices.Length != 0)
            {
                indices.EnsureCapacity(checked((_indices.Count != 0 ? _indices.Count : oldCount) + (source.Data.Indices.Length != 0 ? source.Data.Indices.Length : appended.Count)));
                if (_indices.Count != 0) indices.AddRange(_indices); else for (var i = 0; i < oldCount; i++) indices.Add(i);
                if (source.Data.Indices.Length != 0) foreach (var index in source.Data.Indices) indices.Add(checked(oldCount + index)); else for (var i = 0; i < appended.Count; i++) indices.Add(checked(oldCount + i));
            }
            var sourceFormat = Format(source.Data); var formats = (MeshCustomFormat[])_customFormats.Clone();
            for (var channel = 0; channel < 4; channel++) if (source.Data.Custom(channel) is { } custom) formats[channel] = custom.Format; else if (oldCount == 0) formats[channel] = MeshCustomFormat.Max;
            Span<byte> customRecord = stackalloc byte[16];
            for (var channel = 0; channel < 4; channel++)
                if (formats[channel] != MeshCustomFormat.Max && (((oldCount != 0 ? _format : 0) | sourceFormat) & MeshCustomCodec.Mask(channel)) != 0)
                    foreach (var vertex in vertices) WriteCustom(vertex, channel, formats[channel], customRecord);
            _format = ((oldCount != 0 ? _format : 0) | sourceFormat) & ~MeshCustomCodec.FormatMask;
            for (var channel = 0; channel < 4; channel++) if ((_format & MeshCustomCodec.Mask(channel)) != 0) _format |= (Mesh.ArrayFormat)((ulong)formats[channel] << MeshCustomCodec.Shift(channel));
            Array.Copy(formats, _customFormats, 4);
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
    /// <param name="normals">Optional finite numerical normal vectors.</param>
    /// <param name="tangents">Optional finite tangent XYZ vectors and W orientation.</param>
    /// <remarks>Missing entries retain the current captured attribute at that point in fan traversal.
    /// Channels must be enabled before prior vertices. Existing indexed geometry retains its indices and gains
    /// indices for the new triangles. Invalid input leaves geometry and current values unchanged.</remarks>
    /// <exception cref="InvalidOperationException">No triangle draft exists or a new channel follows prior vertices.</exception>
    /// <exception cref="ArgumentException">Optional counts or values are invalid.</exception>
    public void AddTriangleFan(ReadOnlySpan<Vector2> vertices, ReadOnlySpan<Vector2> uvs = default, ReadOnlySpan<Color> colors = default, ReadOnlySpan<Vector2> uv2s = default, ReadOnlySpan<Vector3> normals = default, ReadOnlySpan<Vector4> tangents = default)
    {
        lock (_gate)
        {
            Active(); if (_primitive != Mesh.PrimitiveType.Triangles) throw new InvalidOperationException("Fans require triangle topology.");
            if (uvs.Length > vertices.Length || colors.Length > vertices.Length || uv2s.Length > vertices.Length || normals.Length > vertices.Length || tangents.Length > vertices.Length) throw new ArgumentException("Fan attributes exceed their vertex count.");
            foreach (var v in vertices) if (!v.IsFinite()) throw new ArgumentException("Fan positions must be finite.", nameof(vertices));
            foreach (var v in uvs) if (!v.IsFinite()) throw new ArgumentException("Fan UVs must be finite.", nameof(uvs));
            foreach (var v in uv2s) if (!v.IsFinite()) throw new ArgumentException("Fan UV2s must be finite.", nameof(uv2s));
            foreach (var c in colors) if (!c.IsFinite()) throw new ArgumentException("Fan colors must be finite.", nameof(colors));
            foreach (var n in normals) if (!n.IsFinite()) throw new ArgumentException("Fan normals must be finite.", nameof(normals));
            foreach (var t in tangents) if (!t.IsFinite()) throw new ArgumentException("Fan tangents must be finite.", nameof(tangents));
            if (vertices.Length < 3) return;
            if (uvs.Length != 0) CanActivate(Mesh.ArrayFormat.TexUV); if (uv2s.Length != 0) CanActivate(Mesh.ArrayFormat.TexUV2); if (colors.Length != 0) CanActivate(Mesh.ArrayFormat.Color);
            if (normals.Length != 0) CanActivate(Mesh.ArrayFormat.Normal); if (tangents.Length != 0) CanActivate(Mesh.ArrayFormat.Tangent);
            ValidateIndices(); ValidateCustomCapture(); PrepareSkin(); var current = _current;
            var count = checked((vertices.Length - 2) * 3); var appended = new List<Vertex>(count);
            for (var triangle = 1; triangle < vertices.Length - 1; triangle++)
                for (var corner = 0; corner < 3; corner++)
                {
                    var index = corner == 0 ? 0 : triangle + corner - 1;
                    if (index < uvs.Length) current = current with { UV = uvs[index] }; if (index < uv2s.Length) current = current with { UV2 = uv2s[index] }; if (index < colors.Length) current = current with { Color = colors[index] };
                    if (index < normals.Length) current = current with { Normal = normals[index] }; if (index < tangents.Length) current = current with { Tangent = tangents[index] };
                    appended.Add(current with { Position = vertices[index] });
                }
            var oldCount = _vertices.Count; _vertices.EnsureCapacity(checked(oldCount + count)); if (_indices.Count != 0) _indices.EnsureCapacity(checked(_indices.Count + count));
            if (_indices.Count != 0) for (var i = 0; i < count; i++) _indices.Add(oldCount + i);
            if (normals.Length != 0) _format |= Mesh.ArrayFormat.Normal; if (tangents.Length != 0) _format |= Mesh.ArrayFormat.Tangent;
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
            foreach (var vertex in _vertices) { var key = IndexKey(vertex); if (!unique.TryGetValue(key, out var index)) { index = vertices.Count; unique.Add(key, index); vertices.Add(vertex); } indices.Add(index); }
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
    private static Mesh.ArrayFormat Format(MeshSurfaceData data) => MeshCustomCodec.Channels(data) | Mesh.ArrayFormat.Vertex | Mesh.ArrayFormat.Use2DVertices |
        (data.Normals.Length != 0 ? Mesh.ArrayFormat.Normal : 0) | (data.Tangents.Length != 0 ? Mesh.ArrayFormat.Tangent : 0) | (data.Colors.Length != 0 ? Mesh.ArrayFormat.Color : 0) | (data.UVs.Length != 0 ? Mesh.ArrayFormat.TexUV : 0) | (data.UV2s.Length != 0 ? Mesh.ArrayFormat.TexUV2 : 0) |
        (data.Indices.Length != 0 ? Mesh.ArrayFormat.Index : 0) | (data.Bones.Length != 0 ? Mesh.ArrayFormat.Bones | Mesh.ArrayFormat.Weights : 0) | (data.SkinSlots == 8 ? Mesh.ArrayFormat.Use8BoneWeights : 0);
    private static List<Vertex> Vertices(MeshSurfaceData data)
    {
        var result = new List<Vertex>(data.Vertices.Length); var slots = data.SkinSlots;
        for (var i = 0; i < data.Vertices.Length; i++)
        {
            Vector4i bones = default, extraBones = default; Vector4 weights = default, extraWeights = default;
            for (var j = 0; j < slots; j++) { if (j < 4) { bones[j] = data.Bones[i * slots + j]; weights[j] = data.Weights[i * slots + j]; } else { extraBones[j - 4] = data.Bones[i * slots + j]; extraWeights[j - 4] = data.Weights[i * slots + j]; } }
            result.Add(new(data.Vertices[i], data.Colors.Length != 0 ? data.Colors[i] : Colors.White, data.UVs.Length != 0 ? data.UVs[i] : default, data.UV2s.Length != 0 ? data.UV2s[i] : default, 0, bones, extraBones, weights, extraWeights, data.Normals.Length == 0 ? default : data.Normals[i], data.Tangents.Length == 0 ? default : data.Tangents[i], ReadCustom(data, 0, i), ReadCustom(data, 1, i), ReadCustom(data, 2, i), ReadCustom(data, 3, i)));
        }
        return result;
    }
    private void Import(MeshSurfaceData data, Mesh.PrimitiveType primitive, Material? material)
    {
        var vertices = Vertices(data); var indices = new List<int>(data.Indices); var format = Format(data); Reset(); _vertices = vertices; _indices = indices; _primitive = primitive; _material = material; _format = format; for (var channel = 0; channel < 4; channel++) _customFormats[channel] = data.Custom(channel)?.Format ?? MeshCustomFormat.Max; _skin = data.SkinSlots == 8 ? SkinWeightCount.Eight : SkinWeightCount.Four;
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
            const Mesh.ArrayFormat policies = Mesh.ArrayFormat.Use2DVertices | Mesh.ArrayFormat.UseDynamicUpdate | Mesh.ArrayFormat.Use8BoneWeights | Mesh.ArrayFormat.CompressAttributes;
            const Mesh.ArrayFormat channels = Mesh.ArrayFormat.Custom0 | Mesh.ArrayFormat.Custom1 | Mesh.ArrayFormat.Custom2 | Mesh.ArrayFormat.Custom3 | Mesh.ArrayFormat.Vertex | Mesh.ArrayFormat.Normal | Mesh.ArrayFormat.Tangent | Mesh.ArrayFormat.Color | Mesh.ArrayFormat.TexUV | Mesh.ArrayFormat.TexUV2 | Mesh.ArrayFormat.Index | Mesh.ArrayFormat.Bones | Mesh.ArrayFormat.Weights;
            if ((format & ~(policies | channels | MeshCustomCodec.FormatMask)) != 0) throw new NotSupportedException("Source channels require their typed storage and shader consumer.");
            if ((format & (channels | MeshCustomCodec.FormatMask)) != (Format(data) & (channels | MeshCustomCodec.FormatMask)) || data.SkinSlots != 0 && data.SkinSlots != ((format & Mesh.ArrayFormat.Use8BoneWeights) != 0 ? 8 : 4)) throw new ArgumentException("Source format must describe its actual channels and skin width.");
            return new(data, primitive, material);
        }
        finally { _readingSource = false; }
    }
}
