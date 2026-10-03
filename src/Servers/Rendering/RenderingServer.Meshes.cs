namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<RID> _ownedMeshRIDs = [];
    /// <summary>Creates an owned empty two-dimensional mesh.</summary>
    /// <returns>A logical mesh identity owned until FreeRID or renderer shutdown.</returns>
    /// <exception cref="InvalidOperationException">The renderer is off-owner or submitting.</exception>
    public RID MeshCreate()
    {
        EnsureTextureChange(); var mesh = new ArrayMesh(); var rid = mesh.RegisterOwned(this); _ownedMeshRIDs.Add(rid); return rid;
    }
    /// <summary>Adds copied typed surface channels to an owned mesh.</summary>
    /// <param name="mesh">Owned mesh identity.</param>
    /// <param name="primitive">Primitive topology.</param>
    /// <param name="arrays">Borrowed arrays copied before mutation.</param>
    /// <param name="flags">Supported two-dimensional update policies.</param>
    /// <exception cref="ArgumentException">The identity or geometry is invalid.</exception>
    /// <exception cref="InvalidOperationException">The mesh is borrowed or owned by another renderer.</exception>
    public void MeshAddSurfaceFromArrays(RID mesh, Mesh.PrimitiveType primitive, MeshSurfaceData arrays, Mesh.ArrayFormat flags = Mesh.ArrayFormat.None)
    { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).AddSurfaceFromArrays(primitive, arrays, flags: flags); }
    /// <summary>Gets a live mesh's surface count.</summary>
    /// <param name="mesh">Borrowed or owned mesh identity.</param>
    /// <returns>The current surface count.</returns>
    public int MeshGetSurfaceCount(RID mesh) { EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).GetSurfaceCount(); }
    /// <summary>Gets copied typed channels from one mesh surface.</summary>
    /// <param name="mesh">Borrowed or owned mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <returns>Independent caller-owned channels.</returns>
    public MeshSurfaceData MeshSurfaceGetArrays(RID mesh, int surface) { EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).SurfaceGetArrays(surface); }
    /// <summary>Removes every surface from an owned mesh.</summary>
    /// <param name="mesh">Owned mesh identity.</param>
    public void MeshClear(RID mesh) { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).ClearSurfaces(); }
    /// <summary>Removes one surface from an owned mesh.</summary>
    /// <param name="mesh">Owned mesh identity.</param>
    /// <param name="surface">Existing zero-based index.</param>
    public void MeshSurfaceRemove(RID mesh, int surface) { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).SurfaceRemove(surface); }
    /// <summary>Updates packed position bytes, including partial records, in an owned mesh.</summary>
    /// <param name="mesh">Owned mesh identity.</param>
    /// <param name="surface">Existing zero-based index.</param>
    /// <param name="offset">Byte offset into two-float positions.</param>
    /// <param name="data">Little-endian X/Y records.</param>
    public void MeshSurfaceUpdateVertexRegion(RID mesh, int surface, int offset, ReadOnlySpan<byte> data) { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).SurfaceUpdateVertexRegion(surface, offset, data); }
    /// <summary>Updates packed attribute bytes, including partial records, in an owned mesh.</summary>
    /// <param name="mesh">Owned mesh identity.</param>
    /// <param name="surface">Existing zero-based index.</param>
    /// <param name="offset">Byte offset into color/UV records.</param>
    /// <param name="data">Present RGBA8 UNORM bytes followed by present little-endian UV floats.</param>
    public void MeshSurfaceUpdateAttributeRegion(RID mesh, int surface, int offset, ReadOnlySpan<byte> data) { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).SurfaceUpdateAttributeRegion(surface, offset, data); }
    /// <summary>Gets the primary two-dimensional vertex stride for a supported surface format.</summary>
    /// <param name="format">Surface format mask.</param>
    /// <param name="vertexCount">Nonnegative surface vertex count.</param>
    /// <returns>Eight bytes for positions, zero when no vertex channel exists.</returns>
    /// <exception cref="NotSupportedException">The format requires unsupported channels or non-2D vertices.</exception>
    public int MeshSurfaceGetFormatVertexStride(Mesh.ArrayFormat format, int vertexCount) { EnsureOwner(); ValidateMeshFormat(format, vertexCount); return (format & Mesh.ArrayFormat.Vertex) != 0 ? 8 : 0; }
    /// <summary>Gets the packed color/UV stride for a supported format.</summary>
    /// <param name="format">Surface format mask.</param>
    /// <param name="vertexCount">Nonnegative surface vertex count.</param>
    /// <returns>Four bytes for color plus eight bytes for primary UV when present.</returns>
    /// <exception cref="NotSupportedException">The format requires unsupported channels or non-2D vertices.</exception>
    public int MeshSurfaceGetFormatAttributeStride(Mesh.ArrayFormat format, int vertexCount) { EnsureOwner(); ValidateMeshFormat(format, vertexCount); return ((format & Mesh.ArrayFormat.Color) != 0 ? 4 : 0) + ((format & Mesh.ArrayFormat.TexUV) != 0 ? 8 : 0); }
    private static void ValidateMeshFormat(Mesh.ArrayFormat format, int vertexCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(vertexCount);
        const Mesh.ArrayFormat supported = Mesh.ArrayFormat.Vertex | Mesh.ArrayFormat.Color | Mesh.ArrayFormat.TexUV | Mesh.ArrayFormat.Index | Mesh.ArrayFormat.Use2DVertices | Mesh.ArrayFormat.UseDynamicUpdate;
        if ((format & ~supported) != 0 || (format & Mesh.ArrayFormat.Vertex) != 0 && (format & Mesh.ArrayFormat.Use2DVertices) == 0)
            throw new NotSupportedException("The mesh format requires unsupported attribute storage or non-2D vertices.");
    }
    /// <summary>Gets a live surface's channel and policy mask.</summary>
    /// <param name="mesh">Live borrowed or owned mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <returns>The supported surface format.</returns>
    public Mesh.ArrayFormat MeshSurfaceGetFormat(RID mesh, int surface) { EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).SurfaceGetFormat(surface); }
    /// <summary>Gets one surface's primitive topology.</summary>
    /// <param name="mesh">Live borrowed or owned mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <returns>The topology.</returns>
    public Mesh.PrimitiveType MeshSurfaceGetPrimitiveType(RID mesh, int surface) { EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).SurfaceGetPrimitiveType(surface); }
    /// <summary>Gets the surface vertex count.</summary>
    /// <param name="mesh">Live borrowed or owned mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <returns>The vertex count.</returns>
    public int MeshSurfaceGetArrayLen(RID mesh, int surface) { EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).SurfaceGetArrayLen(surface); }
    /// <summary>Gets the explicit surface index count.</summary>
    /// <param name="mesh">Live borrowed or owned mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <returns>Zero for sequential vertices.</returns>
    public int MeshSurfaceGetArrayIndexLen(RID mesh, int surface) { EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).SurfaceGetArrayIndexLen(surface); }
    /// <summary>Assigns a borrowed surface material on an owned mesh.</summary>
    /// <param name="mesh">Owned mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <param name="material">Borrowed live material or null.</param>
    public void MeshSurfaceSetMaterial(RID mesh, int surface, Material? material) { EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, this).SurfaceSetMaterial(surface, material); }
    /// <summary>Gets a borrowed material from a live surface.</summary>
    /// <param name="mesh">Live borrowed or owned mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <returns>The borrowed material or null.</returns>
    public Material? MeshSurfaceGetMaterial(RID mesh, int surface) { EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).SurfaceGetMaterial(surface); }
    private void ReleaseOwnedMeshes()
    {
        List<Exception>? errors = null;
        foreach (var rid in _ownedMeshRIDs) try { var mesh = RenderingMeshRegistry.Owned(rid, this); RenderingMeshRegistry.Remove(rid); mesh.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        _ownedMeshRIDs.Clear(); if (errors is not null) throw new AggregateException("Mesh shutdown callbacks failed.", errors);
    }
}
