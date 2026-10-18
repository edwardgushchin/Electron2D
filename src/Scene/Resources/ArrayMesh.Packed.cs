using System.Buffers.Binary;

namespace Electron2D;

public sealed partial class ArrayMesh
{
    internal static Surface PreparePacked(MeshSurfaceBuffers source, int shapeCount)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(source.VertexData); ArgumentNullException.ThrowIfNull(source.AttributeData);
        ArgumentNullException.ThrowIfNull(source.SkinData); ArgumentNullException.ThrowIfNull(source.IndexData);
        ArgumentNullException.ThrowIfNull(source.BlendShapeData); ArgumentNullException.ThrowIfNull(source.BoneAABBs); ArgumentNullException.ThrowIfNull(source.LODs);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(source.VertexCount); ArgumentOutOfRangeException.ThrowIfNegative(source.IndexCount);
        RenderingServer.ValidateMeshFormat(source.Format, source.VertexCount);
        var format = source.Format; var compressed = (format & ArrayFormat.CompressAttributes) != 0;
        if ((format & (ArrayFormat.Vertex | ArrayFormat.Use2DVertices)) != (ArrayFormat.Vertex | ArrayFormat.Use2DVertices)) throw new NotSupportedException("Packed surfaces require explicit planar positions.");
        if (compressed && (format & ArrayFormat.Normal) != 0) format |= ArrayFormat.Tangent;
        var baseShapeFormat = format & (ArrayFormat.Vertex | ArrayFormat.Normal | ArrayFormat.Tangent);
        var shapeFormat = source.BlendShapeFormat == 0 ? baseShapeFormat : source.BlendShapeFormat;
        if ((shapeFormat & ArrayFormat.Vertex) == 0 || (shapeFormat & ~baseShapeFormat) != 0 || compressed && (shapeFormat & ArrayFormat.Tangent) != 0 && (shapeFormat & ArrayFormat.Normal) == 0) throw new ArgumentException("Shape format must select base position/direction channels.", nameof(source));
        if (((format & ArrayFormat.Bones) != 0) != ((format & ArrayFormat.Weights) != 0)) throw new ArgumentException("Packed skin requires both bones and weights.", nameof(source));
        if (((format & ArrayFormat.Index) != 0) != (source.IndexCount != 0)) throw new ArgumentException("Index flags and counts differ.", nameof(source));
        if (source.Material is { IsDisposed: true }) throw new ObjectDisposedException(nameof(source.Material));
        PackedRect(source.AABB);
        if (!source.UVScale.IsFinite() || source.UVScale.X < 0 || source.UVScale.Y < 0 || source.UVScale.Z < 0 || source.UVScale.W < 0) throw new ArgumentException("UV scale must be finite and nonnegative.", nameof(source));
        if (source.BoneAABBs.Length > 65536 || source.BoneAABBs.Length != 0 && (format & ArrayFormat.Bones) == 0) throw new ArgumentException("Bone bounds require skin and at most 65536 entries.", nameof(source));
        foreach (var bounds in source.BoneAABBs) if (bounds is { } rect) PackedRect(rect);
        var count = source.VertexCount; var frames = PackedFrameStride(format); var attributes = MeshCustomCodec.AttributeStride(format);
        var slots = (format & ArrayFormat.Bones) == 0 ? 0 : (format & ArrayFormat.Use8BoneWeights) != 0 ? 8 : 4;
        PackedLength(source.VertexData, (8L + frames) * count); PackedLength(source.AttributeData, (long)attributes * count);
        var shapePacking = shapeFormat | (format & ArrayFormat.CompressAttributes); var shapeStride = checked((8 + PackedFrameStride(shapePacking)) * count);
        PackedLength(source.SkinData, (long)slots * 4 * count); PackedLength(source.BlendShapeData, (long)shapeStride * shapeCount);
        var data = ReadPackedVertices(source.VertexData, count, format, out var words, out var frameBytes);
        data.Indices = ReadPackedIndices(source.IndexData, source.IndexCount, count);
        if ((format & ArrayFormat.Color) != 0) data.Colors = new Color[count];
        if ((format & ArrayFormat.TexUV) != 0) data.UVs = new Vector2[count];
        if ((format & ArrayFormat.TexUV2) != 0) data.UV2s = new Vector2[count];
        var primary = compressed ? new byte[data.UVs.Length * 4] : []; var secondary = compressed ? new byte[data.UV2s.Length * 4] : [];
        for (var channel = 0; channel < 4; channel++)
        {
            var customFormat = (MeshCustomFormat)((ulong)format >> MeshCustomCodec.Shift(channel) & 7);
            if ((format & MeshCustomCodec.Mask(channel)) != 0) data.SetCustom(channel, new() { Format = customFormat, Data = new byte[checked(count * MeshCustomCodec.Stride(customFormat))] });
            else if (customFormat != 0) throw new ArgumentException("Custom format bits require their channel.", nameof(source));
        }
        for (var i = 0; i < count; i++)
        {
            var record = source.AttributeData.AsSpan(i * attributes, attributes); var offset = 0;
            if (data.Colors.Length != 0) { data.Colors[i] = new(record[0] / 255f, record[1] / 255f, record[2] / 255f, record[3] / 255f); offset = 4; }
            if (data.UVs.Length != 0) { if (compressed) record.Slice(offset, 4).CopyTo(primary.AsSpan(i * 4)); else data.UVs[i] = ReadVector(record[offset..]); offset += compressed ? 4 : 8; }
            if (data.UV2s.Length != 0) { if (compressed) record.Slice(offset, 4).CopyTo(secondary.AsSpan(i * 4)); else data.UV2s[i] = ReadVector(record[offset..]); offset += compressed ? 4 : 8; }
            for (var channel = 0; channel < 4; channel++) if (data.Custom(channel) is { } custom) { var size = MeshCustomCodec.Stride(custom.Format); record.Slice(offset, size).CopyTo(custom.Data.AsSpan(i * size)); offset += size; }
        }
        MeshCompression? compression = compressed ? new(data, source.UVScale, primary, secondary, frameBytes) : null;
        if (slots != 0)
        {
            data.Bones = new int[checked(count * slots)]; data.Weights = new float[data.Bones.Length];
            for (var i = 0; i < count; i++) for (var slot = 0; slot < slots; slot++)
                {
                    var index = i * slots + slot; var record = source.SkinData.AsSpan(i * slots * 4);
                    data.Bones[index] = BinaryPrimitives.ReadUInt16LittleEndian(record[(slot * 2)..]); data.Weights[index] = BinaryPrimitives.ReadUInt16LittleEndian(record[(slots * 2 + slot * 2)..]) / 65535f;
                }
        }
        data.Validate(source.Primitive);
        var shapes = new MeshSurfaceData[shapeCount];
        for (var i = 0; i < shapes.Length; i++) shapes[i] = ReadPackedVertices(source.BlendShapeData.AsSpan(i * shapeStride, shapeStride), count, shapePacking, out _, out _);
        var lods = new Dictionary<float, int[]>(); var indexWidth = count <= 65536 ? 2 : 4;
        foreach (var (error, bytes) in source.LODs)
        {
            ArgumentNullException.ThrowIfNull(bytes); if (bytes.Length % indexWidth != 0) throw new ArgumentException("LOD indices require complete records.", nameof(source));
            lods.Add(error, ReadPackedIndices(bytes, bytes.Length / indexWidth, count));
        }
        var result = new Surface(data, source.Primitive, format, words, shapes, shapeFormat, lods, compression)
        {
            Material = source.Material,
            PackedAABB = source.AABB,
            PackedUVScale = source.UVScale,
            PackedBoneAABBs = (Rect2?[])source.BoneAABBs.Clone(),
            PackedBlendShapeData = (byte[])source.BlendShapeData.Clone()
        };
        if (result.Format != format) throw new ArgumentException("Packed flags do not describe their complete channel data.", nameof(source));
        return result;
    }
    internal void AppendPacked(Surface surface)
    {
        lock (Gate) { ThrowIfDisposed(); if (surface.BlendShapes.Length != _blendShapeNames.Count) throw new ArgumentException("Packed shape count differs from mesh definitions."); _surfaces.Add(surface); }
        EmitChanged();
    }
    private static MeshSurfaceData ReadPackedVertices(ReadOnlySpan<byte> bytes, int count, ArrayFormat format, out uint[] words, out byte[] frames)
    {
        var data = new MeshSurfaceData { Vertices = new Vector2[count] }; var normal = (format & ArrayFormat.Normal) != 0; var tangent = (format & ArrayFormat.Tangent) != 0;
        if (normal) data.Normals = new Vector3[count]; if (tangent || normal && (format & ArrayFormat.CompressAttributes) != 0) data.Tangents = new Vector4[count];
        for (var i = 0; i < count; i++) { data.Vertices[i] = ReadVector(bytes[(i * 8)..]); if (!data.Vertices[i].IsFinite()) throw new ArgumentException("Packed positions must be finite."); }
        var directions = bytes[(count * 8)..];
        if ((format & ArrayFormat.CompressAttributes) != 0)
        {
            words = []; frames = directions.ToArray();
            for (var i = 0; i < (normal ? count : 0); i++) MeshCompression.ReadFrame(frames.AsSpan(i * 6), out data.Normals[i], out data.Tangents[i]);
            if (!tangent) data.Tangents = [];
        }
        else
        {
            frames = []; words = new uint[directions.Length / 4];
            for (var i = 0; i < words.Length; i++) { words[i] = BinaryPrimitives.ReadUInt32LittleEndian(directions[(i * 4)..]); MeshFrameCodec.RestoreWord(data, i, words[i]); }
        }
        return data;
    }
    private static int[] ReadPackedIndices(byte[] bytes, int count, int vertices)
    {
        var width = vertices <= 65536 ? 2 : 4; PackedLength(bytes, (long)count * width); var result = new int[count];
        for (var i = 0; i < count; i++) { var value = width == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(i * 2)) : BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(i * 4)); if (value >= (uint)vertices) throw new ArgumentException("Packed indices must address a vertex."); result[i] = (int)value; }
        return result;
    }
    private static void PackedLength(byte[] bytes, long expected) { if (bytes.LongLength != expected) throw new ArgumentException("Packed buffer length differs from its format and count."); }
    private static int PackedFrameStride(ArrayFormat format) => (format & ArrayFormat.CompressAttributes) != 0 ? (format & ArrayFormat.Normal) != 0 ? 6 : 0 : ((format & ArrayFormat.Normal) != 0 ? 4 : 0) + ((format & ArrayFormat.Tangent) != 0 ? 4 : 0);
    private static void PackedRect(Rect2 bounds) { if (!bounds.IsFinite() || !bounds.End.IsFinite() || bounds.Size.X < 0 || bounds.Size.Y < 0) throw new ArgumentException("Packed bounds must be finite nonnegative rectangles."); }
    internal MeshSurfaceBuffers ExportPacked(int index) { lock (Gate) return ExportPacked(Get(index)); }
    internal static MeshSurfaceBuffers ExportPacked(Surface surface)
    {
        var data = surface.Data; var count = data.Vertices.Length; var format = surface.Format;
        var baseShapeFormat = format & (ArrayFormat.Vertex | ArrayFormat.Normal | ArrayFormat.Tangent); var shapePacking = surface.BlendMask | (format & ArrayFormat.CompressAttributes);
        var result = new MeshSurfaceBuffers
        {
            Primitive = surface.Primitive,
            Format = format,
            VertexCount = count,
            IndexCount = data.Indices.Length,
            VertexData = WritePackedVertices(data, format, surface.FrameWords, surface.Compression),
            AttributeData = new byte[checked(count * MeshCustomCodec.AttributeStride(format))],
            SkinData = new byte[checked(data.Bones.Length * 4)],
            IndexData = WritePackedIndices(data.Indices, count),
            AABB = surface.PackedAABB ?? PackedBounds(data.Vertices),
            UVScale = surface.PackedUVScale ?? surface.Compression?.UVScale ?? default,
            BlendShapeFormat = surface.BlendMask == baseShapeFormat ? 0 : surface.BlendMask,
            BoneAABBs = surface.PackedAABB is not null ? (Rect2?[])surface.PackedBoneAABBs.Clone() : BoneBounds(data),
            Material = surface.Material
        };
        var stride = MeshCustomCodec.AttributeStride(format);
        for (var i = 0; i < count; i++)
        {
            var record = result.AttributeData.AsSpan(i * stride, stride); var offset = 0;
            if (data.Colors.Length != 0) { var color = data.Colors[i]; record[0] = Byte(color.R); record[1] = Byte(color.G); record[2] = Byte(color.B); record[3] = Byte(color.A); offset = 4; }
            if (data.UVs.Length != 0) { if (surface.Compression is { } packed) packed.UVs.AsSpan(i * 4, 4).CopyTo(record[offset..]); else WritePackedVector(record[offset..], data.UVs[i]); offset += surface.Compression is null ? 8 : 4; }
            if (data.UV2s.Length != 0) { if (surface.Compression is { } packed) packed.UV2s.AsSpan(i * 4, 4).CopyTo(record[offset..]); else WritePackedVector(record[offset..], data.UV2s[i]); offset += surface.Compression is null ? 8 : 4; }
            for (var channel = 0; channel < 4; channel++) if (data.Custom(channel) is { } custom) { var size = MeshCustomCodec.Stride(custom.Format); custom.Data.AsSpan(i * size, size).CopyTo(record[offset..]); offset += size; }
            for (var slot = 0; slot < data.SkinSlots; slot++) { var entry = i * data.SkinSlots + slot; BinaryPrimitives.WriteUInt16LittleEndian(result.SkinData.AsSpan(i * data.SkinSlots * 4 + slot * 2), (ushort)data.Bones[entry]); BinaryPrimitives.WriteUInt16LittleEndian(result.SkinData.AsSpan(i * data.SkinSlots * 4 + data.SkinSlots * 2 + slot * 2), EncodeWeight(data.Weights[entry])); }
        }
        result.LODs = surface.LODs.ToDictionary(lod => lod.Error, lod => WritePackedIndices(lod.Data.Indices, count));
        if (surface.PackedBlendShapeData is { } shapeBytes) result.BlendShapeData = (byte[])shapeBytes.Clone();
        else
        {
            var shapeStride = checked(count * (8 + PackedFrameStride(shapePacking))); result.BlendShapeData = new byte[checked(shapeStride * surface.BlendShapes.Length)];
            for (var i = 0; i < surface.BlendShapes.Length; i++)
            {
                var shape = surface.BlendShapes[i].Copy(); var compression = (format & ArrayFormat.CompressAttributes) != 0 ? new MeshCompression(shape) : null;
                WritePackedVertices(shape, shapePacking, null, compression).CopyTo(result.BlendShapeData, i * shapeStride);
            }
        }
        return result;
    }
    private static byte[] WritePackedVertices(MeshSurfaceData data, ArrayFormat format, uint[]? words, MeshCompression? compression)
    {
        var count = data.Vertices.Length; var result = new byte[checked(count * (8 + PackedFrameStride(format)))];
        for (var i = 0; i < count; i++) WritePackedVector(result.AsSpan(i * 8), data.Vertices[i]);
        if (compression is not null) compression.Frames.CopyTo(result, count * 8);
        else if (words is not null) { for (var i = 0; i < words.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(count * 8 + i * 4), words[i]); }
        else
        {
            var offset = count * 8;
            for (var i = 0; i < count; i++)
            {
                if ((format & ArrayFormat.Normal) != 0) { BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset), MeshFrameCodec.PackNormal(data.Normals[i])); offset += 4; }
                if ((format & ArrayFormat.Tangent) != 0) { BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset), MeshFrameCodec.PackTangent(data.Tangents[i])); offset += 4; }
            }
        }
        return result;
    }
    private static void WritePackedVector(Span<byte> target, Vector2 value) { BinaryPrimitives.WriteSingleLittleEndian(target, value.X); BinaryPrimitives.WriteSingleLittleEndian(target[4..], value.Y); }
    private static byte[] WritePackedIndices(int[] indices, int count)
    {
        var width = count <= 65536 ? 2 : 4; var result = new byte[checked(indices.Length * width)];
        for (var i = 0; i < indices.Length; i++) if (width == 2) BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(i * 2), (ushort)indices[i]); else BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(i * 4), (uint)indices[i]);
        return result;
    }
    internal static Rect2 PackedBounds(Vector2[] vertices)
    {
        var minimum = vertices[0]; var maximum = minimum; foreach (var value in vertices) { minimum = minimum.Min(value); maximum = maximum.Max(value); }
        var result = new Rect2(minimum, maximum - minimum); PackedRect(result); return result;
    }
    private static Rect2?[] BoneBounds(MeshSurfaceData data)
    {
        var count = 0; for (var i = 0; i < data.Bones.Length; i++) if (data.Weights[i] > 0) count = Math.Max(count, data.Bones[i] + 1);
        var result = new Rect2?[count];
        for (var i = 0; i < data.Bones.Length; i++) if (data.Weights[i] > 0) { var bone = data.Bones[i]; var point = data.Vertices[i / data.SkinSlots]; result[bone] = result[bone] is { } bounds ? bounds.Expand(point) : new(point, Vector2.Zero); }
        return result;
    }
}
