namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<RID> _ownedMultiMeshRIDs = [];
    internal RID MultiMeshCreateCore() { EnsureTextureChange(); var resource = new MultiMesh(); var rid = resource.RegisterOwned(this); _ownedMultiMeshRIDs.Add(rid); return rid; }
    internal void MultiMeshAllocateDataCore(RID multiMesh, int instances, bool useColors = false, bool useCustomData = false, bool useIndirect = false)
    {
        EnsureTextureChange(); var resource = RenderingMultiMeshRegistry.Owned(multiMesh, this); if (useIndirect) throw new NotSupportedException("Writable GPU instance and indirect command buffers are not integrated."); resource.Allocate(instances, useColors, useCustomData);
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
