using System.Buffers.Binary;

namespace Electron2D;

/// <summary>Owns copied two-dimensional surfaces with indexed geometry and live region updates.</summary>
/// <remarks>Surface arrays are private. Edits validate before commit and emit Changed afterward; throwing
/// listeners do not roll back committed geometry. Mesh draws retain this resource and observe live data.</remarks>
public sealed partial class ArrayMesh : Mesh
{
    internal sealed class Surface(MeshSurfaceData data, PrimitiveType primitive, ArrayFormat flags, uint[]? frameWords = null, MeshSurfaceData[]? blendShapes = null, ArrayFormat blendMask = ArrayFormat.Vertex)
    {
        internal readonly MeshSurfaceData Data = data;
        internal readonly MeshSurfaceData[] BlendShapes = blendShapes ?? [];
        internal readonly ArrayFormat BlendMask = blendMask;
        internal readonly uint[] FrameWords = Prepare(data, frameWords);
        private static uint[] Prepare(MeshSurfaceData data, uint[]? words) { data.PrepareCustoms(); return words ?? MeshFrameCodec.Capture(data); }
        internal readonly PrimitiveType Primitive = primitive;
        internal readonly ArrayFormat Format = Channels(data) | flags;
        internal static ArrayFormat Channels(MeshSurfaceData data) => MeshCustomCodec.Channels(data) | ArrayFormat.Vertex | ArrayFormat.Use2DVertices |
            (data.Normals.Length != 0 ? ArrayFormat.Normal : 0) | (data.Tangents.Length != 0 ? ArrayFormat.Tangent : 0) | (data.Colors.Length != 0 ? ArrayFormat.Color : 0) | (data.UVs.Length != 0 ? ArrayFormat.TexUV : 0) | (data.UV2s.Length != 0 ? ArrayFormat.TexUV2 : 0) | (data.Indices.Length != 0 ? ArrayFormat.Index : 0) | (data.Bones.Length != 0 ? ArrayFormat.Bones | ArrayFormat.Weights : 0);
        internal string Name = "";
        internal Material? Material;
    }
    internal const ArrayFormat SupportedFormats = ArrayFormat.Custom0 | ArrayFormat.Custom1 | ArrayFormat.Custom2 | ArrayFormat.Custom3 | MeshCustomCodec.FormatMask | ArrayFormat.UseDynamicUpdate | ArrayFormat.Use2DVertices | ArrayFormat.Use8BoneWeights | ArrayFormat.Vertex | ArrayFormat.Normal | ArrayFormat.Tangent | ArrayFormat.Color | ArrayFormat.TexUV | ArrayFormat.TexUV2 | ArrayFormat.Index | ArrayFormat.Bones | ArrayFormat.Weights;
    internal readonly object Gate = new();
    private readonly List<Surface> _surfaces = [];
    /// <summary>Creates an empty mesh.</summary>
    public ArrayMesh() { }
    /// <summary>Adds one copied two-dimensional surface.</summary>
    /// <param name="primitive">Vertex topology, including points, lines and strips.</param>
    /// <param name="arrays">Borrowed typed vertex/normal/tangent/color/primary and secondary UV/custom/index/skin arrays copied before mutation.</param>
    /// <param name="blendShapes">One copied position/normal/tangent-only target per named definition; null or empty for unshaped meshes.</param>
    /// <param name="lods">Null or empty until scale-selected index sets are integrated.</param>
    /// <param name="flags">UseDynamicUpdate, Use2DVertices, Use8BoneWeights and matching channel bits. Unsupported bits reject before mutation.</param>
    /// <remarks>Channel bits are inferred; explicitly supplied channel bits must match the arrays. The new surface index is the old GetSurfaceCount result.</remarks>
    /// <exception cref="ArgumentException">Channel counts, primitives or finite values are invalid.</exception>
    /// <exception cref="ArgumentNullException">The data or a channel array is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index or primitive is invalid.</exception>
    /// <exception cref="NotSupportedException">Flags require an unimplemented storage/attribute layer.</exception>
    /// <exception cref="ObjectDisposedException">The mesh is disposed.</exception>
    public void AddSurfaceFromArrays(PrimitiveType primitive, MeshSurfaceData arrays, IReadOnlyList<MeshSurfaceData>? blendShapes = null, IReadOnlyDictionary<float, int[]>? lods = null, ArrayFormat flags = ArrayFormat.None)
        => AddSurface(primitive, arrays, blendShapes, lods, flags, null);
    internal void AddSurface(PrimitiveType primitive, MeshSurfaceData arrays, IReadOnlyList<MeshSurfaceData>? blendShapes, IReadOnlyDictionary<float, int[]>? lods, ArrayFormat flags, Material? material)
    {
        ArgumentNullException.ThrowIfNull(arrays);
        if (material is { IsDisposed: true }) throw new ObjectDisposedException(nameof(material));
        if (lods is { Count: > 0 }) throw new NotSupportedException("LOD index sets require their first executable scale-selection integration.");
        if ((flags & ~SupportedFormats) != 0) throw new NotSupportedException("These mesh flags require their attribute storage integration.");
        var copy = arrays.Copy(); copy.Validate(primitive);
        var blendMask = ArrayFormat.Vertex | (copy.Normals.Length != 0 ? ArrayFormat.Normal : 0) | (copy.Tangents.Length != 0 ? ArrayFormat.Tangent : 0);
        var shapes = blendShapes?.Select(shape => { ArgumentNullException.ThrowIfNull(shape); var result = shape.Copy(); ValidateBlendShape(result, copy, blendMask); for (var i = 0; i < result.Normals.Length; i++) result.Normals[i] = MeshFrameCodec.Normal(MeshFrameCodec.PackNormal(result.Normals[i])); for (var i = 0; i < result.Tangents.Length; i++) result.Tangents[i] = MeshFrameCodec.Tangent(MeshFrameCodec.PackTangent(result.Tangents[i])); return result; }).ToArray() ?? [];
        var packed = new Surface(copy, primitive, flags, blendShapes: shapes, blendMask: blendMask); var inferred = Surface.Channels(copy); const ArrayFormat channels = ArrayFormat.Custom0 | ArrayFormat.Custom1 | ArrayFormat.Custom2 | ArrayFormat.Custom3 | ArrayFormat.Vertex | ArrayFormat.Normal | ArrayFormat.Tangent | ArrayFormat.Color | ArrayFormat.TexUV | ArrayFormat.TexUV2 | ArrayFormat.Index | ArrayFormat.Bones | ArrayFormat.Weights; if ((flags & channels & ~inferred) != 0) throw new ArgumentException("Explicit channel flags require matching typed arrays.", nameof(flags)); if (copy.SkinSlots != 0 && copy.SkinSlots != ((flags & ArrayFormat.Use8BoneWeights) != 0 ? 8 : 4)) throw new ArgumentException("Skin slot count must match the eight-weight flag.", nameof(flags)); copy.QuantizeSkin();
        for (var channel = 0; channel < 4; channel++)
        {
            var supplied = (ulong)flags >> MeshCustomCodec.Shift(channel) & 7;
            if (supplied != 0 && (copy.Custom(channel) is not { } custom || (ulong)custom.Format != supplied)) throw new ArgumentException("Custom format flags require matching typed data.", nameof(flags));
        }
        for (var i = 0; i < copy.Colors.Length; i++) { var color = copy.Colors[i]; copy.Colors[i] = new(Quantize(color.R), Quantize(color.G), Quantize(color.B), Quantize(color.A)); }
        lock (Gate) { ThrowIfDisposed(); if (shapes.Length != _blendShapeNames.Count) throw new ArgumentException("Every surface requires one payload per named blend shape.", nameof(blendShapes)); packed.Material = material; _surfaces.Add(packed); }
        EmitChanged();
    }
    /// <summary>Removes every surface without disposing borrowed materials.</summary>
    /// <exception cref="ObjectDisposedException">The mesh is disposed.</exception>
    public void ClearSurfaces() { lock (Gate) { ThrowIfDisposed(); _surfaces.Clear(); } EmitChanged(); }
    /// <summary>Removes one surface and shifts subsequent indices down.</summary>
    /// <param name="surfaceIndex">Existing zero-based index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The mesh is disposed.</exception>
    public void SurfaceRemove(int surfaceIndex) { lock (Gate) { Get(surfaceIndex); _surfaces.RemoveAt(surfaceIndex); } EmitChanged(); }
    /// <summary>Gets an authored surface name.</summary>
    /// <param name="surfaceIndex">Existing zero-based index.</param>
    /// <returns>The literal name, empty initially.</returns>
    public string SurfaceGetName(int surfaceIndex) { lock (Gate) return Get(surfaceIndex).Name; }
    /// <summary>Assigns a literal name and emits Changed.</summary>
    /// <param name="surfaceIndex">Existing zero-based index.</param>
    /// <param name="name">Non-null name; duplicates and empty names are allowed.</param>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    public void SurfaceSetName(int surfaceIndex, string name) { ArgumentNullException.ThrowIfNull(name); lock (Gate) Get(surfaceIndex).Name = name; EmitChanged(); }
    /// <summary>Finds the first exact ordinal surface name.</summary>
    /// <param name="name">Literal non-null name.</param>
    /// <returns>The first index, or minus one.</returns>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    public int SurfaceFindByName(string name) { ArgumentNullException.ThrowIfNull(name); lock (Gate) { ThrowIfDisposed(); for (var i = 0; i < _surfaces.Count; i++) if (_surfaces[i].Name == name) return i; return -1; } }
    /// <summary>Updates bytes in packed positions and octahedral normal/tangent records.</summary>
    /// <param name="surfaceIndex">Existing zero-based index.</param>
    /// <param name="offset">Byte offset in the position block followed by interleaved normal/tangent words.</param>
    /// <param name="data">Little-endian float X/Y positions followed by per-vertex octahedral UNORM16 normal/tangent words, including partial byte records.</param>
    /// <remarks>Retained draws observe the update without rerecording.</remarks>
    /// <exception cref="ArgumentException">An affected position becomes nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The region exceeds the vertex buffer.</exception>
    public void SurfaceUpdateVertexRegion(int surfaceIndex, int offset, ReadOnlySpan<byte> data)
    {
        lock (Gate)
        {
            var surface = Get(surfaceIndex); var positionBytes = surface.Data.Vertices.Length * 8L;
            if (offset < 0 || (long)offset + data.Length > positionBytes + surface.FrameWords.Length * 4L) throw new ArgumentOutOfRangeException(nameof(offset));
            Span<byte> record = stackalloc byte[8]; var end = (long)offset + data.Length;
            for (var pass = 0; pass < 2; pass++)
            {
                for (var index = offset / 8; index * 8L < Math.Min(end, positionBytes); index++)
                {
                    var vertex = surface.Data.Vertices[index]; BinaryPrimitives.WriteSingleLittleEndian(record, vertex.X); BinaryPrimitives.WriteSingleLittleEndian(record[4..], vertex.Y);
                    Overlay(record, index * 8L, offset, data); var next = ReadVector(record);
                    if (!next.IsFinite()) throw new ArgumentException("Vertex updates must be finite.", nameof(data));
                    if (pass != 0) surface.Data.Vertices[index] = next;
                }
                if (pass != 0 && end > positionBytes)
                    for (var word = (int)Math.Max(0, ((long)offset - positionBytes) / 4); positionBytes + word * 4L < end; word++)
                    {
                        var bytes = record[..4]; BinaryPrimitives.WriteUInt32LittleEndian(bytes, surface.FrameWords[word]); Overlay(bytes, positionBytes + word * 4L, offset, data);
                        var value = BinaryPrimitives.ReadUInt32LittleEndian(bytes); surface.FrameWords[word] = value; MeshFrameCodec.RestoreWord(surface.Data, word, value);
                    }
            }
        }
        EmitChanged();
    }
    /// <summary>Updates bytes in a surface's packed color, primary/secondary UV and custom buffer.</summary>
    /// <param name="surfaceIndex">Existing zero-based surface index.</param>
    /// <param name="offset">Byte offset within the packed attribute buffer.</param>
    /// <param name="data">Present RGBA8 UNORM, primary/secondary float UV pairs, then Custom0 through Custom3 records in their selected layouts.</param>
    /// <remarks>Color uses four bytes per vertex; each UV channel uses eight; custom formats use four, eight, twelve or sixteen. Partial records retain untouched bytes.
    /// Validation of every affected UV and custom component precedes any write.</remarks>
    /// <exception cref="ArgumentException">An affected UV or custom component becomes nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The region exceeds the buffer.</exception>
    public void SurfaceUpdateAttributeRegion(int surfaceIndex, int offset, ReadOnlySpan<byte> data)
    {
        lock (Gate)
        {
            var surface = Get(surfaceIndex); var colors = surface.Data.Colors.Length != 0; var uvs = surface.Data.UVs.Length != 0; var uv2s = surface.Data.UV2s.Length != 0;
            var stride = MeshCustomCodec.AttributeStride(surface.Format); if (stride == 0) throw new InvalidOperationException("The surface has no attribute buffer.");
            Region(offset, data.Length, surface.Data.Vertices.Length, stride); Span<byte> record = stackalloc byte[84]; var end = (long)offset + data.Length;
            for (var pass = 0; pass < 2; pass++)
                for (var index = offset / stride; index * (long)stride < end; index++)
                {
                    var buffer = record[..stride]; var uvOffset = colors ? 4 : 0; var secondaryOffset = uvOffset + (uvs ? 8 : 0);
                    if (colors) { var color = surface.Data.Colors[index]; buffer[0] = Byte(color.R); buffer[1] = Byte(color.G); buffer[2] = Byte(color.B); buffer[3] = Byte(color.A); }
                    if (uvs) { var uv = surface.Data.UVs[index]; BinaryPrimitives.WriteSingleLittleEndian(buffer[uvOffset..], uv.X); BinaryPrimitives.WriteSingleLittleEndian(buffer[(uvOffset + 4)..], uv.Y); }
                    if (uv2s) { var uv = surface.Data.UV2s[index]; BinaryPrimitives.WriteSingleLittleEndian(buffer[secondaryOffset..], uv.X); BinaryPrimitives.WriteSingleLittleEndian(buffer[(secondaryOffset + 4)..], uv.Y); }
                    var customOffset = secondaryOffset + (uv2s ? 8 : 0);
                    for (var channel = 0; channel < 4; channel++) if (surface.Data.Custom(channel) is { } custom) { var size = MeshCustomCodec.Stride(custom.Format); custom.Data.AsSpan(index * size, size).CopyTo(buffer[customOffset..]); customOffset += size; }
                    Overlay(buffer, index * (long)stride, offset, data);
                    customOffset = secondaryOffset + (uv2s ? 8 : 0);
                    for (var channel = 0; channel < 4; channel++) if (surface.Data.Custom(channel) is { } custom)
                        {
                            var size = MeshCustomCodec.Stride(custom.Format); var recordBytes = buffer.Slice(customOffset, size);
                            if (!MeshCustomCodec.Read(custom.Format, recordBytes).IsFinite()) throw new ArgumentException("Custom updates must remain finite.", nameof(data));
                            if (pass != 0) { recordBytes.CopyTo(custom.Data.AsSpan(index * size, size)); custom.Refresh(index); }
                            customOffset += size;
                        }
                    if (uvs && !ReadVector(buffer[uvOffset..]).IsFinite() || uv2s && !ReadVector(buffer[secondaryOffset..]).IsFinite()) throw new ArgumentException("Attribute updates must be finite.", nameof(data));
                    if (pass == 0) continue;
                    if (colors) surface.Data.Colors[index] = new(buffer[0] / 255f, buffer[1] / 255f, buffer[2] / 255f, buffer[3] / 255f);
                    if (uvs) surface.Data.UVs[index] = ReadVector(buffer[uvOffset..]);
                    if (uv2s) surface.Data.UV2s[index] = ReadVector(buffer[secondaryOffset..]);
                }
        }
        EmitChanged();
    }
    private static void Overlay(Span<byte> record, long recordOffset, int offset, ReadOnlySpan<byte> data)
    {
        var first = Math.Max(recordOffset, offset); var end = Math.Min(recordOffset + record.Length, (long)offset + data.Length);
        data.Slice((int)(first - offset), (int)(end - first)).CopyTo(record[(int)(first - recordOffset)..]);
    }
    private static Vector2 ReadVector(ReadOnlySpan<byte> data) => new(BinaryPrimitives.ReadSingleLittleEndian(data), BinaryPrimitives.ReadSingleLittleEndian(data[4..]));
    private static void Region(int offset, int length, int count, int stride) { if (offset < 0 || (long)offset + length > (long)count * stride) throw new ArgumentOutOfRangeException(nameof(offset)); }
    private static byte Byte(float value) => (byte)Math.Clamp(value * 255f + .00001f, 0, 255);
    private static float Quantize(float value) => (byte)Math.Clamp(value * 255f, 0, 255) / 255f;
    internal Surface Get(int index) { ThrowIfDisposed(); if ((uint)index >= (uint)_surfaces.Count) throw new ArgumentOutOfRangeException(nameof(index)); return _surfaces[index]; }
    internal int SurfaceCount => _surfaces.Count;
    /// <inheritdoc />
    protected override int OnGetSurfaceCount() { lock (Gate) { ThrowIfDisposed(); return _surfaces.Count; } }
    /// <inheritdoc />
    protected override MeshSurfaceData OnSurfaceGetArrays(int surfaceIndex) { lock (Gate) return Get(surfaceIndex).Data.Copy(); }
    /// <inheritdoc />
    protected override int OnSurfaceGetArrayLen(int surfaceIndex) { lock (Gate) return Get(surfaceIndex).Data.Vertices.Length; }
    /// <inheritdoc />
    protected override int OnSurfaceGetArrayIndexLen(int surfaceIndex) { lock (Gate) return Get(surfaceIndex).Data.Indices.Length; }
    /// <inheritdoc />
    protected override PrimitiveType OnSurfaceGetPrimitiveType(int surfaceIndex) { lock (Gate) return Get(surfaceIndex).Primitive; }
    /// <inheritdoc />
    protected override ArrayFormat OnSurfaceGetFormat(int surfaceIndex) { lock (Gate) return Get(surfaceIndex).Format; }
    /// <inheritdoc />
    protected override Material? OnSurfaceGetMaterial(int surfaceIndex) { lock (Gate) return Get(surfaceIndex).Material; }
    /// <inheritdoc />
    protected override void OnSurfaceSetMaterial(int surfaceIndex, Material? material) { lock (Gate) Get(surfaceIndex).Material = material; EmitChanged(); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new ArrayMesh();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        Surface[] copies; string[] names; BlendShapeMode mode;
        lock (Gate) { ThrowIfDisposed(); names = _blendShapeNames.ToArray(); mode = _blendShapeMode; copies = new Surface[_surfaces.Count]; for (var i = 0; i < copies.Length; i++) { var s = _surfaces[i]; copies[i] = new(s.Data.Copy(), s.Primitive, s.Format & (ArrayFormat.UseDynamicUpdate | ArrayFormat.Use2DVertices | ArrayFormat.Use8BoneWeights), (uint[])s.FrameWords.Clone(), s.BlendShapes.Select(shape => shape.Copy()).ToArray(), s.BlendMask) { Name = s.Name, Material = s.Material }; } }
        foreach (var copy in copies) copy.Material = (Material?)duplicateSubresource(copy.Material);
        var other = (ArrayMesh)target; lock (other.Gate) { other.ThrowIfDisposed(); other._surfaces.Clear(); other._surfaces.AddRange(copies); other._blendShapeNames.Clear(); other._blendShapeNames.AddRange(names); other._blendShapeMode = mode; }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (Gate) { _surfaces.Clear(); _blendShapeNames.Clear(); } base.Dispose(disposing); }
}
