using System.Buffers.Binary;

namespace Electron2D;

/// <summary>Owns copied two-dimensional surfaces with indexed geometry and live region updates.</summary>
/// <remarks>Surface arrays are private. Edits validate before commit and emit Changed afterward; throwing
/// listeners do not roll back committed geometry. Mesh draws retain this resource and observe live data.</remarks>
public sealed partial class ArrayMesh : Mesh
{
    internal sealed class Surface(MeshSurfaceData data, PrimitiveType primitive, ArrayFormat flags)
    {
        internal readonly MeshSurfaceData Data = data;
        internal readonly PrimitiveType Primitive = primitive;
        internal readonly ArrayFormat Format = ArrayFormat.Vertex | ArrayFormat.Use2DVertices | flags |
            (data.Colors.Length != 0 ? ArrayFormat.Color : 0) | (data.UVs.Length != 0 ? ArrayFormat.TexUV : 0) | (data.Indices.Length != 0 ? ArrayFormat.Index : 0) | (data.Bones.Length != 0 ? ArrayFormat.Bones | ArrayFormat.Weights : 0);
        internal string Name = "";
        internal Material? Material;
    }
    internal readonly object Gate = new();
    private readonly List<Surface> _surfaces = [];
    /// <summary>Creates an empty mesh.</summary>
    public ArrayMesh() { }
    /// <summary>Adds one copied two-dimensional surface.</summary>
    /// <param name="primitive">Vertex topology, including points, lines and strips.</param>
    /// <param name="arrays">Borrowed typed vertex/color/UV/index/skin arrays copied before mutation.</param>
    /// <param name="blendShapes">Null or empty until the typed deformation consumer is integrated.</param>
    /// <param name="lods">Null or empty until scale-selected index sets are integrated.</param>
    /// <param name="flags">UseDynamicUpdate, Use2DVertices, Use8BoneWeights and matching channel bits. Unsupported bits reject before mutation.</param>
    /// <remarks>Channel bits are inferred; explicitly supplied channel bits must match the arrays. The new surface index is the old GetSurfaceCount result.</remarks>
    /// <exception cref="ArgumentException">Channel counts, primitives or finite values are invalid.</exception>
    /// <exception cref="ArgumentNullException">The data or a channel array is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index or primitive is invalid.</exception>
    /// <exception cref="NotSupportedException">Flags require an unimplemented storage/attribute layer.</exception>
    /// <exception cref="ObjectDisposedException">The mesh is disposed.</exception>
    public void AddSurfaceFromArrays(PrimitiveType primitive, MeshSurfaceData arrays, IReadOnlyList<MeshSurfaceData>? blendShapes = null, IReadOnlyDictionary<float, int[]>? lods = null, ArrayFormat flags = ArrayFormat.None)
    {
        ArgumentNullException.ThrowIfNull(arrays);
        if (blendShapes is { Count: > 0 } || lods is { Count: > 0 }) throw new NotSupportedException("Morph targets and LOD index sets require their first executable deformation/scale-selection integration.");
        if ((flags & ~(ArrayFormat.UseDynamicUpdate | ArrayFormat.Use2DVertices | ArrayFormat.Use8BoneWeights | ArrayFormat.Vertex | ArrayFormat.Color | ArrayFormat.TexUV | ArrayFormat.Index | ArrayFormat.Bones | ArrayFormat.Weights)) != 0) throw new NotSupportedException("These mesh flags require their attribute storage integration.");
        var copy = arrays.Copy(); copy.Validate(primitive); var inferred = new Surface(copy, primitive, ArrayFormat.None).Format; const ArrayFormat channels = ArrayFormat.Vertex | ArrayFormat.Color | ArrayFormat.TexUV | ArrayFormat.Index | ArrayFormat.Bones | ArrayFormat.Weights; if ((flags & channels & ~inferred) != 0) throw new ArgumentException("Explicit channel flags require matching typed arrays.", nameof(flags)); if (copy.SkinSlots != 0 && copy.SkinSlots != ((flags & ArrayFormat.Use8BoneWeights) != 0 ? 8 : 4)) throw new ArgumentException("Skin slot count must match the eight-weight flag.", nameof(flags)); copy.QuantizeSkin();
        for (var i = 0; i < copy.Colors.Length; i++) { var color = copy.Colors[i]; copy.Colors[i] = new(Quantize(color.R), Quantize(color.G), Quantize(color.B), Quantize(color.A)); }
        lock (Gate) { ThrowIfDisposed(); _surfaces.Add(new(copy, primitive, flags)); }
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
    /// <summary>Updates bytes in packed two-float positions in the surface vertex buffer.</summary>
    /// <param name="surfaceIndex">Existing zero-based index.</param>
    /// <param name="offset">Byte offset within the two-float position buffer.</param>
    /// <param name="data">Little-endian float X/Y storage, including partial byte records.</param>
    /// <remarks>Retained draws observe the update without rerecording.</remarks>
    /// <exception cref="ArgumentException">An affected position becomes nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The region exceeds the vertex buffer.</exception>
    public void SurfaceUpdateVertexRegion(int surfaceIndex, int offset, ReadOnlySpan<byte> data)
    {
        lock (Gate)
        {
            var surface = Get(surfaceIndex); Region(offset, data.Length, surface.Data.Vertices.Length, 8);
            Span<byte> record = stackalloc byte[8]; var end = (long)offset + data.Length;
            for (var pass = 0; pass < 2; pass++)
                for (var index = offset / 8; index * 8L < end; index++)
                {
                    var vertex = surface.Data.Vertices[index]; BinaryPrimitives.WriteSingleLittleEndian(record, vertex.X); BinaryPrimitives.WriteSingleLittleEndian(record[4..], vertex.Y);
                    Overlay(record, index * 8L, offset, data); var next = ReadVector(record);
                    if (!next.IsFinite()) throw new ArgumentException("Vertex updates must be finite.", nameof(data));
                    if (pass != 0) surface.Data.Vertices[index] = next;
                }
        }
        EmitChanged();
    }
    /// <summary>Updates bytes in a surface's packed color and UV buffer.</summary>
    /// <param name="surfaceIndex">Existing zero-based surface index.</param>
    /// <param name="offset">Byte offset within the packed attribute buffer.</param>
    /// <param name="data">Present RGBA8 UNORM bytes followed by present little-endian float UV pairs.</param>
    /// <remarks>Color uses four bytes per vertex, UV uses eight; partial records retain untouched bytes.
    /// Validation of the complete affected UV records precedes any write.</remarks>
    /// <exception cref="ArgumentException">An affected UV becomes nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The region exceeds the buffer.</exception>
    public void SurfaceUpdateAttributeRegion(int surfaceIndex, int offset, ReadOnlySpan<byte> data)
    {
        lock (Gate)
        {
            var surface = Get(surfaceIndex); var colors = surface.Data.Colors.Length != 0; var uvs = surface.Data.UVs.Length != 0;
            var stride = (colors ? 4 : 0) + (uvs ? 8 : 0); if (stride == 0) throw new InvalidOperationException("The surface has no attribute buffer.");
            Region(offset, data.Length, surface.Data.Vertices.Length, stride); Span<byte> record = stackalloc byte[12]; var end = (long)offset + data.Length;
            for (var pass = 0; pass < 2; pass++)
                for (var index = offset / stride; index * (long)stride < end; index++)
                {
                    var buffer = record[..stride]; var uvOffset = colors ? 4 : 0;
                    if (colors) { var color = surface.Data.Colors[index]; buffer[0] = Byte(color.R); buffer[1] = Byte(color.G); buffer[2] = Byte(color.B); buffer[3] = Byte(color.A); }
                    if (uvs) { var uv = surface.Data.UVs[index]; BinaryPrimitives.WriteSingleLittleEndian(buffer[uvOffset..], uv.X); BinaryPrimitives.WriteSingleLittleEndian(buffer[(uvOffset + 4)..], uv.Y); }
                    Overlay(buffer, index * (long)stride, offset, data);
                    if (uvs && !ReadVector(buffer[uvOffset..]).IsFinite()) throw new ArgumentException("Attribute updates must be finite.", nameof(data));
                    if (pass == 0) continue;
                    if (colors) surface.Data.Colors[index] = new(buffer[0] / 255f, buffer[1] / 255f, buffer[2] / 255f, buffer[3] / 255f);
                    if (uvs) surface.Data.UVs[index] = ReadVector(buffer[uvOffset..]);
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
        Surface[] copies;
        lock (Gate) { ThrowIfDisposed(); copies = new Surface[_surfaces.Count]; for (var i = 0; i < copies.Length; i++) { var s = _surfaces[i]; copies[i] = new(s.Data.Copy(), s.Primitive, s.Format & (ArrayFormat.UseDynamicUpdate | ArrayFormat.Use2DVertices | ArrayFormat.Use8BoneWeights)) { Name = s.Name, Material = s.Material }; } }
        foreach (var copy in copies) copy.Material = (Material?)duplicateSubresource(copy.Material);
        var other = (ArrayMesh)target; lock (other.Gate) { other.ThrowIfDisposed(); other._surfaces.Clear(); other._surfaces.AddRange(copies); }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (Gate) _surfaces.Clear(); base.Dispose(disposing); }
}
