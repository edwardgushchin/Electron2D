namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Updates bytes in a caller-owned mesh surface's existing index buffer.</summary>
    /// <param name="mesh">Caller-owned mesh identity.</param>
    /// <param name="surface">Existing indexed surface.</param>
    /// <param name="offset">Byte offset in the packed index buffer; partial records are accepted.</param>
    /// <param name="data">Copied little-endian unsigned index bytes, with width given by MeshSurfaceGetFormatIndexStride.</param>
    /// <remarks>Every affected index is validated before any change. Surface topology/count, bounds, LODs and RID identity
    /// remain intact. Retained draws observe the new indices without rerecording; Changed observers run after commitment.
    /// Empty data at a valid offset publishes Changed consistently with the other mesh region operations.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The surface, byte region or resulting vertex index is invalid.</exception>
    /// <exception cref="InvalidOperationException">There is no index buffer, the mesh is borrowed, submission is active,
    /// the renderer is inactive or the caller is off-owner.</exception>
    /// <exception cref="ArgumentException">The mesh identity is invalid.</exception>
    public static void MeshSurfaceUpdateIndexRegion(RID mesh, int surface, int offset, ReadOnlySpan<byte> data)
    {
        var service = RequireService(); service.EnsureTextureChange();
        RenderingMeshRegistry.Owned(mesh, service).UpdateIndexRegion(surface, offset, data);
    }

    /// <summary>Returns the packed index width for a supported surface format.</summary>
    /// <param name="format">Typed mesh channel and storage flags.</param>
    /// <param name="vertexCount">Nonnegative vertex count.</param>
    /// <returns>Zero without Index; two bytes for 1 through 65536 vertices, otherwise four bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative.</exception>
    /// <exception cref="ArgumentException">Compressed tangent flags omit the required normal channel.</exception>
    /// <exception cref="NotSupportedException">The format requires unsupported storage or non-2D vertices.</exception>
    /// <exception cref="InvalidOperationException">The renderer is inactive or the caller is off-owner.</exception>
    public static int MeshSurfaceGetFormatIndexStride(Mesh.ArrayFormat format, int vertexCount)
    {
        RequireService().EnsureOwner(); ValidateMeshFormat(format, vertexCount);
        return (format & Mesh.ArrayFormat.Index) == 0 ? 0 : ArrayMesh.IndexStride(vertexCount);
    }

    /// <summary>Returns a channel's byte offset from the start of its packed buffer.</summary>
    /// <param name="format">Typed mesh channel and storage flags.</param>
    /// <param name="vertexCount">Nonnegative vertex count, used to skip the position block for direction channels.</param>
    /// <param name="channel">Exactly one Vertex, Normal, Tangent, Color, TexUV, TexUV2, Custom0 through Custom3, Bones, Weights or Index presence bit.</param>
    /// <returns>Zero for absent channels, Vertex and Index. Directions follow all positions; other offsets are within one attribute or skin record.</returns>
    /// <remarks>CompressAttributes uses the engine's six-byte joint frame: Normal starts at the frame and Tangent at its final two bytes.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The count or channel is invalid.</exception>
    /// <exception cref="OverflowException">The direction offset exceeds Int32 storage.</exception>
    /// <exception cref="ArgumentException">Compressed tangent flags omit the required normal channel.</exception>
    /// <exception cref="NotSupportedException">The format requires unsupported storage or non-2D vertices.</exception>
    /// <exception cref="InvalidOperationException">The renderer is inactive or the caller is off-owner.</exception>
    public static int MeshSurfaceGetFormatOffset(Mesh.ArrayFormat format, int vertexCount, Mesh.ArrayFormat channel)
    {
        RequireService().EnsureOwner(); ValidateMeshFormat(format, vertexCount);
        var bit = (ulong)channel;
        if (bit == 0 || bit > (ulong)Mesh.ArrayFormat.Index || (bit & (bit - 1)) != 0) throw new ArgumentOutOfRangeException(nameof(channel));
        if ((format & channel) == 0) return 0;
        return channel switch
        {
            Mesh.ArrayFormat.Vertex or Mesh.ArrayFormat.Index or Mesh.ArrayFormat.Bones => 0,
            Mesh.ArrayFormat.Normal => checked(((format & Mesh.ArrayFormat.Vertex) != 0 ? vertexCount : 0) * 8),
            Mesh.ArrayFormat.Tangent => checked(((format & Mesh.ArrayFormat.Vertex) != 0 ? vertexCount : 0) * 8 + ((format & Mesh.ArrayFormat.Normal) != 0 ? 4 : 0)),
            Mesh.ArrayFormat.Weights => (format & Mesh.ArrayFormat.Bones) == 0 ? 0 : (format & Mesh.ArrayFormat.Use8BoneWeights) != 0 ? 16 : 8,
            _ => MeshCustomCodec.AttributeStride((format & ~(Mesh.ArrayFormat)8191) | (format & (Mesh.ArrayFormat)(bit - 1)))
        };
    }
}
