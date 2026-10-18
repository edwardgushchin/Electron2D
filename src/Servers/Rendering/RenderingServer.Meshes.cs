namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<RID> _ownedMeshRIDs = [];
    internal RID MeshCreateCore(int blendShapeCount = 0)
    {
        EnsureTextureChange(); if ((uint)blendShapeCount > 4096) throw new ArgumentOutOfRangeException(nameof(blendShapeCount)); var mesh = new ArrayMesh(); for (var i = 0; i < blendShapeCount; i++) mesh.AddBlendShape(i.ToString(System.Globalization.CultureInfo.InvariantCulture)); var rid = mesh.RegisterOwned(this); _ownedMeshRIDs.Add(rid); return rid;
    }
    internal void MeshAddSurfaceFromArraysCore(RID mesh, Mesh.PrimitiveType primitive, MeshSurfaceData arrays, Mesh.ArrayFormat flags = Mesh.ArrayFormat.None, IReadOnlyList<MeshSurfaceData>? blendShapes = null, IReadOnlyDictionary<float, int[]>? lods = null)
    { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).AddSurfaceFromArrays(primitive, arrays, blendShapes, lods, flags); }
    internal int MeshGetSurfaceCountCore(RID mesh) { EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).GetSurfaceCount(); }
    internal MeshSurfaceData MeshSurfaceGetArraysCore(RID mesh, int surface) { EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).SurfaceGetArrays(surface); }
    internal void MeshClearCore(RID mesh) { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).ClearSurfaces(); }
    internal void MeshSurfaceRemoveCore(RID mesh, int surface) { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).SurfaceRemove(surface); }
    internal void MeshSurfaceUpdateVertexRegionCore(RID mesh, int surface, int offset, ReadOnlySpan<byte> data) { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).SurfaceUpdateVertexRegion(surface, offset, data); }
    internal void MeshSurfaceUpdateAttributeRegionCore(RID mesh, int surface, int offset, ReadOnlySpan<byte> data) { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).SurfaceUpdateAttributeRegion(surface, offset, data); }
    /// <summary>Number of independently packed numerical mesh custom channels.</summary>
    public const int ArrayCustomCount = 4;
    internal int MeshSurfaceGetFormatVertexStrideCore(Mesh.ArrayFormat format, int vertexCount) { EnsureOwner(); ValidateMeshFormat(format, vertexCount); return (format & Mesh.ArrayFormat.Vertex) != 0 ? 8 : 0; }
    internal int MeshSurfaceGetFormatAttributeStrideCore(Mesh.ArrayFormat format, int vertexCount) { EnsureOwner(); ValidateMeshFormat(format, vertexCount); return MeshCustomCodec.AttributeStride(format); }
    /// <summary>Returns the packed mesh-local normal/tangent stride.</summary>
    /// <param name="format">Supported typed mesh channel flags.</param>
    /// <param name="vertexCount">Nonnegative vertex count; does not affect the stride.</param>
    /// <returns>Six bytes for a CompressAttributes normal/tangent frame; otherwise four bytes per present direction channel. Zero without directions.</returns>
    public static int MeshSurfaceGetFormatNormalTangentStride(Mesh.ArrayFormat format, int vertexCount)
    {
        RequireService().EnsureOwner(); ValidateMeshFormat(format, vertexCount);
        if ((format & Mesh.ArrayFormat.CompressAttributes) != 0) return (format & Mesh.ArrayFormat.Normal) != 0 ? 6 : 0;
        return ((format & Mesh.ArrayFormat.Normal) != 0 ? 4 : 0) + ((format & Mesh.ArrayFormat.Tangent) != 0 ? 4 : 0);
    }
    internal void MeshSurfaceUpdateSkinRegionCore(RID mesh, int surface, int offset, ReadOnlySpan<byte> data) { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).SurfaceUpdateSkinRegion(surface, offset, data); }
    internal int MeshSurfaceGetFormatSkinStrideCore(Mesh.ArrayFormat format, int vertexCount) { EnsureOwner(); ValidateMeshFormat(format, vertexCount); var slots = (format & Mesh.ArrayFormat.Use8BoneWeights) != 0 ? 8 : 4; return ((format & Mesh.ArrayFormat.Bones) != 0 ? slots * 2 : 0) + ((format & Mesh.ArrayFormat.Weights) != 0 ? slots * 2 : 0); }
    /// <summary>Updates copied bytes in a caller-owned mesh's packed skin channel.</summary>
    /// <param name="mesh">Caller-owned mesh identity.</param>
    /// <param name="surface">Existing skinned surface.</param>
    /// <param name="offset">Byte offset into the skin buffer.</param>
    /// <param name="data">Little-endian uint16 indices followed by UNORM16 weights per vertex.</param>
    public static void MeshSurfaceUpdateSkinRegion(RID mesh, int surface, int offset, ReadOnlySpan<byte> data) => RequireService().MeshSurfaceUpdateSkinRegionCore(mesh, surface, offset, data);
    /// <summary>Returns the packed skin stride for a supported two-dimensional surface format.</summary>
    /// <param name="format">Typed channel flags.</param>
    /// <param name="vertexCount">Nonnegative count; does not affect the fixed skin stride.</param>
    /// <returns>Zero without skin; two bytes per present index/weight slot, with four or eight slots.</returns>
    public static int MeshSurfaceGetFormatSkinStride(Mesh.ArrayFormat format, int vertexCount) => RequireService().MeshSurfaceGetFormatSkinStrideCore(format, vertexCount);
    internal static void ValidateMeshFormat(Mesh.ArrayFormat format, int vertexCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(vertexCount);
        if ((format & Mesh.ArrayFormat.CompressAttributes) != 0 && (format & Mesh.ArrayFormat.Tangent) != 0 && (format & Mesh.ArrayFormat.Normal) == 0) throw new ArgumentException("Compressed tangents require normals.", nameof(format));
        const Mesh.ArrayFormat supported = Mesh.ArrayFormat.Custom0 | Mesh.ArrayFormat.Custom1 | Mesh.ArrayFormat.Custom2 | Mesh.ArrayFormat.Custom3 | MeshCustomCodec.FormatMask | Mesh.ArrayFormat.Vertex | Mesh.ArrayFormat.Normal | Mesh.ArrayFormat.Tangent | Mesh.ArrayFormat.Color | Mesh.ArrayFormat.TexUV | Mesh.ArrayFormat.TexUV2 | Mesh.ArrayFormat.Index | Mesh.ArrayFormat.Use2DVertices | Mesh.ArrayFormat.UseDynamicUpdate | Mesh.ArrayFormat.Bones | Mesh.ArrayFormat.Weights | Mesh.ArrayFormat.Use8BoneWeights | Mesh.ArrayFormat.CompressAttributes;
        if ((format & ~supported) != 0 || (format & Mesh.ArrayFormat.Vertex) != 0 && (format & Mesh.ArrayFormat.Use2DVertices) == 0)
            throw new NotSupportedException("The mesh format requires unsupported attribute storage or non-2D vertices.");
    }
    internal Mesh.ArrayFormat MeshSurfaceGetFormatCore(RID mesh, int surface) { EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).SurfaceGetFormat(surface); }
    internal Mesh.PrimitiveType MeshSurfaceGetPrimitiveTypeCore(RID mesh, int surface) { EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).SurfaceGetPrimitiveType(surface); }
    internal int MeshSurfaceGetArrayLenCore(RID mesh, int surface) { EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).SurfaceGetArrayLen(surface); }
    internal int MeshSurfaceGetArrayIndexLenCore(RID mesh, int surface) { EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).SurfaceGetArrayIndexLen(surface); }
    internal void MeshSurfaceSetMaterialCore(RID mesh, int surface, Material? material) { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).SurfaceSetMaterial(surface, material); }
    internal Material? MeshSurfaceGetMaterialCore(RID mesh, int surface) { EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).SurfaceGetMaterial(surface); }
    private void ReleaseOwnedMeshes()
    {
        List<Exception>? errors = null;
        foreach (var rid in _ownedMeshRIDs) try { var mesh = RenderingMeshRegistry.Owned(rid, this); RenderingMeshRegistry.Remove(rid); mesh.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        _ownedMeshRIDs.Clear(); if (errors is not null) throw new AggregateException("Mesh shutdown callbacks failed.", errors);
    }
}
