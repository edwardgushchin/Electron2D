namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<RID> _ownedMeshRIDs = [];
    internal RID MeshCreateCore()
    {
        EnsureTextureChange(); var mesh = new ArrayMesh(); var rid = mesh.RegisterOwned(this); _ownedMeshRIDs.Add(rid); return rid;
    }
    internal void MeshAddSurfaceFromArraysCore(RID mesh, Mesh.PrimitiveType primitive, MeshSurfaceData arrays, Mesh.ArrayFormat flags = Mesh.ArrayFormat.None)
    { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).AddSurfaceFromArrays(primitive, arrays, flags: flags); }
    internal int MeshGetSurfaceCountCore(RID mesh) { EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).GetSurfaceCount(); }
    internal MeshSurfaceData MeshSurfaceGetArraysCore(RID mesh, int surface) { EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).SurfaceGetArrays(surface); }
    internal void MeshClearCore(RID mesh) { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).ClearSurfaces(); }
    internal void MeshSurfaceRemoveCore(RID mesh, int surface) { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).SurfaceRemove(surface); }
    internal void MeshSurfaceUpdateVertexRegionCore(RID mesh, int surface, int offset, ReadOnlySpan<byte> data) { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).SurfaceUpdateVertexRegion(surface, offset, data); }
    internal void MeshSurfaceUpdateAttributeRegionCore(RID mesh, int surface, int offset, ReadOnlySpan<byte> data) { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).SurfaceUpdateAttributeRegion(surface, offset, data); }
    internal int MeshSurfaceGetFormatVertexStrideCore(Mesh.ArrayFormat format, int vertexCount) { EnsureOwner(); ValidateMeshFormat(format, vertexCount); return (format & Mesh.ArrayFormat.Vertex) != 0 ? 8 : 0; }
    internal int MeshSurfaceGetFormatAttributeStrideCore(Mesh.ArrayFormat format, int vertexCount) { EnsureOwner(); ValidateMeshFormat(format, vertexCount); return ((format & Mesh.ArrayFormat.Color) != 0 ? 4 : 0) + ((format & Mesh.ArrayFormat.TexUV) != 0 ? 8 : 0); }
    private static void ValidateMeshFormat(Mesh.ArrayFormat format, int vertexCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(vertexCount);
        const Mesh.ArrayFormat supported = Mesh.ArrayFormat.Vertex | Mesh.ArrayFormat.Color | Mesh.ArrayFormat.TexUV | Mesh.ArrayFormat.Index | Mesh.ArrayFormat.Use2DVertices | Mesh.ArrayFormat.UseDynamicUpdate;
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
