namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly HashSet<Mesh> _packedMeshReads = [];
    /// <summary>Creates one caller-owned mesh from copied packed surfaces.</summary>
    /// <param name="surfaces">Ordered planar surface descriptions; an empty list creates an empty mesh.</param>
    /// <param name="blendShapeCount">Zero through 4096; every surface supplies this many packed shape blocks.</param>
    /// <returns>A logical mesh identity owned by the current renderer and released by FreeRID.</returns>
    /// <remarks>All surfaces validate before identity publication. Source arrays remain caller-owned; materials are borrowed.
    /// Structural import may allocate. Shape definitions use ordinal index names and Relative mode.</remarks>
    /// <exception cref="ArgumentNullException">The list, a surface or a required buffer is null.</exception>
    /// <exception cref="ArgumentException">Counts, geometry, bounds or channel data are invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A shape count, vertex/index count or primitive is invalid.</exception>
    /// <exception cref="NotSupportedException">A format requires non-planar or unsupported storage.</exception>
    /// <exception cref="ObjectDisposedException">A supplied material is disposed.</exception>
    /// <exception cref="InvalidOperationException">The renderer is inactive, off-owner, in submission/shutdown or the list changes during import.</exception>
    public static RID MeshCreateFromSurfaces(IReadOnlyList<MeshSurfaceBuffers> surfaces, int blendShapeCount = 0)
    {
        var service = RequireService(); service.EnsureTextureChange(); ArgumentNullException.ThrowIfNull(surfaces);
        if ((uint)blendShapeCount > 4096) throw new ArgumentOutOfRangeException(nameof(blendShapeCount));
        var count = surfaces.Count; ArgumentOutOfRangeException.ThrowIfNegative(count); var prepared = new ArrayMesh.Surface[count];
        for (var i = 0; i < count; i++) prepared[i] = ArrayMesh.PreparePacked(surfaces[i], blendShapeCount);
        if (surfaces.Count != count) throw new InvalidOperationException("Packed surface count changed during import.");
        var mesh = new ArrayMesh();
        try
        {
            for (var i = 0; i < blendShapeCount; i++) mesh.AddBlendShape(i.ToString(System.Globalization.CultureInfo.InvariantCulture));
            foreach (var surface in prepared) mesh.AppendPacked(surface);
            service._ownedMeshRIDs.EnsureCapacity(checked(service._ownedMeshRIDs.Count + 1));
            var rid = mesh.RegisterOwned(service); service._ownedMeshRIDs.Add(rid); return rid;
        }
        catch { mesh.Dispose(); throw; }
    }
    /// <summary>Appends one copied packed surface to a caller-owned mesh.</summary>
    /// <param name="mesh">Live mesh identity owned by the current renderer.</param>
    /// <param name="surface">Complete channel, shape, LOD, material and visibility data.</param>
    /// <remarks>Validation completes before append. Retained drawing observes a successful edit without rerecording.</remarks>
    /// <exception cref="ArgumentNullException">The surface or a required buffer is null.</exception>
    /// <exception cref="ArgumentException">The identity or packed data is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A count or primitive is invalid.</exception>
    /// <exception cref="NotSupportedException">The format requires unsupported storage.</exception>
    /// <exception cref="ObjectDisposedException">A supplied material is disposed.</exception>
    /// <exception cref="InvalidOperationException">The renderer is inactive, off-owner, in submission/shutdown or the identity is borrowed.</exception>
    public static void MeshAddSurface(RID mesh, MeshSurfaceBuffers surface)
    {
        var service = RequireService(); service.EnsureTextureChange(); var target = RenderingMeshRegistry.Owned(mesh, service);
        target.AppendPacked(ArrayMesh.PreparePacked(surface, target.GetBlendShapeCount()));
    }
    /// <summary>Returns independent packed buffers and metadata from a live mesh surface.</summary>
    /// <param name="mesh">Borrowed resource or caller-owned mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <returns>Caller-owned vertex/attribute/skin/index/shape/LOD/bounds arrays; the material remains borrowed.</returns>
    /// <remarks>Ordinary resource surfaces export their current geometry and computed bounds. Custom callbacks are
    /// captured with stable-revision and reentry guards. This allocating query is an authoring operation.</remarks>
    /// <exception cref="ArgumentException">The identity or custom surface data is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The surface index is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The mesh is disposed during custom preparation.</exception>
    /// <exception cref="InvalidOperationException">The renderer is inactive/off-owner, or custom preparation reenters or changes revision.</exception>
    public static MeshSurfaceBuffers MeshGetSurface(RID mesh, int surface)
    {
        var service = RequireService(); service.EnsureOwner(); var source = RenderingMeshRegistry.Resolve(mesh);
        if (source is ArrayMesh arrays) return arrays.ExportPacked(surface);
        if (source is ImmediateMesh immediate) return immediate.Surfaces.ExportPacked(surface);
        if (!service._packedMeshReads.Add(source)) throw new InvalidOperationException("Packed mesh preparation cannot reenter its source.");
        try
        {
            var revision = source.ChangeRevision; var shapes = source.GetBlendShapeCount();
            using var draft = new ArrayMesh(); for (var i = 0; i < shapes; i++) draft.AddBlendShape(i.ToString(System.Globalization.CultureInfo.InvariantCulture));
            draft.AddSurface(source.SurfaceGetPrimitiveType(surface), source.SurfaceGetArrays(surface), source.SurfaceGetBlendShapeArrays(surface), source.SurfaceGetLODs(surface), source.SurfaceGetFormat(surface), source.SurfaceGetMaterial(surface));
            if (draft.Get(0).Data.IsProcedural) draft.Get(0).PackedAABB = source.GetAABB();
            source.EnsurePropertyInspection();
            if (source.ChangeRevision != revision) throw new InvalidOperationException("Custom mesh changed during packed preparation.");
            return draft.ExportPacked(0);
        }
        finally { service._packedMeshReads.Remove(source); }
    }
}
