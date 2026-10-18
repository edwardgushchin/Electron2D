namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<RID> _ownedMultiMeshRIDs = [];
    internal RID MultiMeshCreateCore() { EnsureTextureChange(); var resource = new MultiMesh(); var rid = resource.RegisterOwned(this); _ownedMultiMeshRIDs.Add(rid); return rid; }
    internal void MultiMeshAllocateDataCore(RID multiMesh, int instances, bool useColors = false, bool useCustomData = false, bool useIndirect = false)
    {
        EnsureTextureChange(); var resource = RenderingMultiMeshRegistry.Owned(multiMesh, this);
        if (useIndirect && GetRenderingDeviceCore() is null) throw new NotSupportedException("Indirect instance drawing requires the GPU renderer.");
        if (useIndirect && (long)instances * (8 + (useColors ? 4 : 0) + (useCustomData ? 4 : 0)) * 4 > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(instances));
        resource.Allocate(instances, useColors, useCustomData, useIndirect);
    }
    /// <summary>Gets the retained renderer-device instance buffer.</summary>
    /// <param name="multiMesh">A live borrowed or renderer-owned instance resource.</param>
    /// <returns>The writable storage RID, or empty for zero capacity or Compatibility.</returns>
    /// <remarks>The resource owns its lifetime; FreeRID rejects the live exported buffer. Each record contains eight transform floats, then four color and four custom floats when enabled, matching MultiMesh.Buffer. CPU edits upload only their changed channels. GPU writes leave authored CPU queries unchanged, so provide CustomAABB covering generated output. Capacity/schema/mode replacement, disposal and session shutdown invalidate the RID. Use the global rendering device on its owner thread outside command lists and native submission.</remarks>
    /// <exception cref="ArgumentException">The logical identity is not live.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Storage exceeds the device-buffer byte budget.</exception>
    /// <exception cref="InvalidOperationException">The owner thread, command-list, callback or native device state is invalid.</exception>
    public static RID MultiMeshGetBufferRDRID(RID multiMesh) => RequireService().MultiMeshBufferCore(multiMesh, false);
    /// <summary>Gets the indirect draw-command buffer of one instance resource.</summary>
    /// <param name="multiMesh">A live borrowed or renderer-owned instance resource.</param>
    /// <returns>The writable five-uint-per-surface command RID, or empty without indirect allocation, surfaces or GPU storage.</returns>
    /// <remarks>Each surface occupies five uints. Indexed fields are index count, instance count, first index, signed base vertex bits and first instance; nonindexed fields are vertex count, instance count, first vertex, first instance and an unused word. Initial instance counts are zero. Visible-prefix edits update each instance count, normalizing -1 to capacity; GPU writes may select another valid instance subrange. Mesh replacement, child-mesh disposal or changed surface invocation layout replaces the command RID; capacity/schema/mode and session changes also expire it. Counts, indices and signed vertex offsets must address valid surface geometry. The resource owns live buffer lifetime.</remarks>
    /// <exception cref="ArgumentException">The logical identity is not live.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Storage exceeds the device-buffer byte budget.</exception>
    /// <exception cref="InvalidOperationException">The owner thread, command-list, callback or native device state is invalid.</exception>
    public static RID MultiMeshGetCommandBufferRDRID(RID multiMesh) => RequireService().MultiMeshBufferCore(multiMesh, true);
    private RID MultiMeshBufferCore(RID rid, bool commands)
    {
        EnsureDeviceAccess(); var resource = RenderingMultiMeshRegistry.Resolve(rid); var device = GetRenderingDeviceCore(); if (device is null) return default;
        device.ValidateCanvasMutation(); var state = resource.PrepareGPU(device, commands); return state is null ? default : device.ExportMultiMeshBuffer(commands ? state.Commands : state.Current);
    }
    internal Rect2 MultiMeshGetAABBCore(RID multiMesh) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).GetAABB(); }
    internal Rect2 MultiMeshGetCustomAABBCore(RID multiMesh) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).CustomAABB; }
    internal void MultiMeshSetCustomAABBCore(RID multiMesh, Rect2 aabb) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).CustomAABB = aabb; }
    internal int MultiMeshGetInstanceCountCore(RID multiMesh) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).InstanceCount; }
    internal int MultiMeshGetVisibleInstancesCore(RID multiMesh) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).VisibleInstanceCount; }
    internal void MultiMeshSetVisibleInstancesCore(RID multiMesh, int visible) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).VisibleInstanceCount = visible; }
    internal float[] MultiMeshGetBufferCore(RID multiMesh) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).Buffer; }
    internal void MultiMeshSetBufferCore(RID multiMesh, ReadOnlySpan<float> buffer) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).SetBuffer(buffer); }
    internal void MultiMeshSetBufferInterpolatedCore(RID multiMesh, ReadOnlySpan<float> bufferCurrent, ReadOnlySpan<float> bufferPrevious) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).SetBufferInterpolated(bufferCurrent, bufferPrevious); }
    internal void MultiMeshSetMeshCore(RID multiMesh, RID mesh) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).Mesh = mesh.IsValid() ? RenderingMeshRegistry.Resolve(mesh) : null; }
    internal RID MultiMeshGetMeshCore(RID multiMesh) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).Mesh?.GetRID() ?? default; }
    internal void MultiMeshInstanceSetTransform2DCore(RID multiMesh, int index, Transform transform) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).SetInstanceTransform2D(index, transform); }
    internal Transform MultiMeshInstanceGetTransform2DCore(RID multiMesh, int index) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).GetInstanceTransform2D(index); }
    internal void MultiMeshInstanceSetColorCore(RID multiMesh, int index, Color color) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).SetInstanceColor(index, color); }
    internal Color MultiMeshInstanceGetColorCore(RID multiMesh, int index) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).GetInstanceColor(index); }
    internal void MultiMeshInstanceSetCustomDataCore(RID multiMesh, int index, Color customData) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).SetInstanceCustomData(index, customData); }
    internal Color MultiMeshInstanceGetCustomDataCore(RID multiMesh, int index) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).GetInstanceCustomData(index); }
    internal void MultiMeshInstanceResetPhysicsInterpolationCore(RID multiMesh, int index) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).ResetInstancePhysicsInterpolation(index); }
    internal void MultiMeshInstancesResetPhysicsInterpolationCore(RID multiMesh) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).ResetInstancesPhysicsInterpolation(); }
    internal void MultiMeshSetPhysicsInterpolatedCore(RID multiMesh, bool interpolated) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).SetInterpolationEnabled(interpolated); }
    internal void MultiMeshSetPhysicsInterpolationQualityCore(RID multiMesh, MultiMesh.PhysicsInterpolationQuality quality) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).PhysicsInterpolationQualityMode = quality; }
    private void ReleaseOwnedMultiMeshes()
    {
        List<Exception>? errors = null;
        foreach (var rid in _ownedMultiMeshRIDs) try { var resource = RenderingMultiMeshRegistry.Owned(rid, this); RenderingMultiMeshRegistry.Remove(rid); resource.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        _ownedMultiMeshRIDs.Clear(); if (errors is not null) throw new AggregateException("Instance storage shutdown callbacks failed.", errors);
    }
}
