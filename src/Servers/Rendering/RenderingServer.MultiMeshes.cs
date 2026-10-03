namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<RID> _ownedMultiMeshRIDs = [];
    /// <summary>Creates owned empty two-dimensional instance storage.</summary>
    /// <returns>A logical identity valid until FreeRID or renderer teardown.</returns>
    public RID MultiMeshCreate() { EnsureTextureChange(); var resource = new MultiMesh(); var rid = resource.RegisterOwned(this); _ownedMultiMeshRIDs.Add(rid); return rid; }
    /// <summary>Allocates copied packed instance records for an owned resource.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="instances">Nonnegative capacity.</param>
    /// <param name="useColors">Include four-float color multipliers.</param>
    /// <param name="useCustomData">Include four-float shader data.</param>
    /// <param name="useIndirect">False for retained canvas storage; true requires a future writable GPU command-buffer backend.</param>
    /// <exception cref="NotSupportedException">Indirect GPU instance commands are requested.</exception>
    /// <remarks>Transforms are always two-dimensional. Equal capacity and flags preserve storage.</remarks>
    public void MultiMeshAllocateData(RID multiMesh, int instances, bool useColors = false, bool useCustomData = false, bool useIndirect = false)
    {
        EnsureTextureChange(); var resource = RenderingMultiMeshRegistry.Owned(multiMesh, this); if (useIndirect) throw new NotSupportedException("Writable GPU instance and indirect command buffers are not integrated."); resource.Allocate(instances, useColors, useCustomData);
    }
    /// <summary>Gets the two-dimensional local visibility rectangle of a live instance resource.</summary>
    /// <param name="multiMesh">Live owned or borrowed instance identity.</param>
    /// <returns>The manual or computed rectangle of its visible prefix.</returns>
    public Rect2 MultiMeshGetAABB(RID multiMesh) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).GetAABB(); }
    /// <summary>Gets the authored manual local visibility rectangle.</summary>
    /// <param name="multiMesh">Live instance identity.</param>
    /// <returns>The stored rectangle; zero selects computed bounds.</returns>
    public Rect2 MultiMeshGetCustomAABB(RID multiMesh) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).CustomAABB; }
    /// <summary>Sets the manual visibility rectangle of owned instance storage.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="aabb">Finite nonnegative two-dimensional local rectangle.</param>
    public void MultiMeshSetCustomAABB(RID multiMesh, Rect2 aabb) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).CustomAABB = aabb; }
    /// <summary>Returns the allocated instance count.</summary>
    /// <param name="multiMesh">Live owned or borrowed instance identity.</param>
    /// <returns>The allocated capacity.</returns>
    public int MultiMeshGetInstanceCount(RID multiMesh) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).InstanceCount; }
    /// <summary>Gets the stored visible-prefix policy.</summary>
    /// <param name="multiMesh">Live instance identity.</param>
    /// <returns>Minus one for all instances, or the visible prefix count.</returns>
    public int MultiMeshGetVisibleInstances(RID multiMesh) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).VisibleInstanceCount; }
    /// <summary>Changes the visible prefix of an owned resource without reallocating.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="visible">Minus one or a count through capacity.</param>
    public void MultiMeshSetVisibleInstances(RID multiMesh, int visible) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).VisibleInstanceCount = visible; }
    /// <summary>Returns copied packed instance records.</summary>
    /// <param name="multiMesh">Live instance identity.</param>
    /// <returns>Caller-owned eight-float transforms and optional channels.</returns>
    public float[] MultiMeshGetBuffer(RID multiMesh) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).Buffer; }
    /// <summary>Copies whole finite packed records into owned storage.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="buffer">Whole packed buffer matching capacity and flags.</param>
    public void MultiMeshSetBuffer(RID multiMesh, ReadOnlySpan<float> buffer) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).SetBuffer(buffer); }
    /// <summary>Copies explicit current and previous packed presentation records.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="bufferCurrent">Whole current buffer.</param>
    /// <param name="bufferPrevious">Whole previous buffer.</param>
    public void MultiMeshSetBufferInterpolated(RID multiMesh, ReadOnlySpan<float> bufferCurrent, ReadOnlySpan<float> bufferPrevious) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).SetBufferInterpolated(bufferCurrent, bufferPrevious); }
    /// <summary>Assigns a borrowed mesh to owned instance storage.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="mesh">Live mesh RID or an empty identity to clear it.</param>
    public void MultiMeshSetMesh(RID multiMesh, RID mesh) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).Mesh = mesh.IsValid() ? RenderingMeshRegistry.Resolve(mesh) : null; }
    /// <summary>Gets the borrowed mesh identity used by an instance resource.</summary>
    /// <param name="multiMesh">Live instance identity.</param>
    /// <returns>The live mesh RID or an empty identity.</returns>
    public RID MultiMeshGetMesh(RID multiMesh) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).Mesh?.GetRID() ?? default; }
    /// <summary>Sets one owned instance's finite local transform.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="index">Existing zero-based index.</param>
    /// <param name="transform">Finite two-dimensional transform.</param>
    public void MultiMeshInstanceSetTransform2D(RID multiMesh, int index, Transform transform) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).SetInstanceTransform2D(index, transform); }
    /// <summary>Gets one instance's current local transform.</summary>
    /// <param name="multiMesh">Live instance identity.</param>
    /// <param name="index">Existing zero-based index.</param>
    /// <returns>The logical current transform.</returns>
    public Transform MultiMeshInstanceGetTransform2D(RID multiMesh, int index) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).GetInstanceTransform2D(index); }
    /// <summary>Sets one owned instance color multiplier.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="index">Existing zero-based index.</param>
    /// <param name="color">Finite four-component multiplier.</param>
    public void MultiMeshInstanceSetColor(RID multiMesh, int index, Color color) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).SetInstanceColor(index, color); }
    /// <summary>Gets one stored instance color multiplier.</summary>
    /// <param name="multiMesh">Live instance identity.</param>
    /// <param name="index">Existing zero-based index.</param>
    /// <returns>The raw current color.</returns>
    public Color MultiMeshInstanceGetColor(RID multiMesh, int index) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).GetInstanceColor(index); }
    /// <summary>Sets one owned instance's raw shader components.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="index">Existing zero-based index.</param>
    /// <param name="customData">Finite four-component value.</param>
    public void MultiMeshInstanceSetCustomData(RID multiMesh, int index, Color customData) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).SetInstanceCustomData(index, customData); }
    /// <summary>Gets one stored four-component shader value.</summary>
    /// <param name="multiMesh">Live instance identity.</param>
    /// <param name="index">Existing zero-based index.</param>
    /// <returns>The raw current shader data.</returns>
    public Color MultiMeshInstanceGetCustomData(RID multiMesh, int index) { EnsureOwner(); return RenderingMultiMeshRegistry.Resolve(multiMesh).GetInstanceCustomData(index); }
    /// <summary>Resets one owned instance's previous presentation record.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="index">Existing zero-based index.</param>
    public void MultiMeshInstanceResetPhysicsInterpolation(RID multiMesh, int index) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).ResetInstancePhysicsInterpolation(index); }
    /// <summary>Resets all previous presentation records in owned storage.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    public void MultiMeshInstancesResetPhysicsInterpolation(RID multiMesh) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).ResetInstancesPhysicsInterpolation(); }
    /// <summary>Enables or disables presentation interpolation of owned packed records.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="interpolated">Whether scene physics snapshots contribute to rendering.</param>
    /// <remarks>Changing the policy resets previous records to current values without changing logical data.</remarks>
    public void MultiMeshSetPhysicsInterpolated(RID multiMesh, bool interpolated) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).SetInterpolationEnabled(interpolated); }
    /// <summary>Changes the basis interpolation quality of owned storage.</summary>
    /// <param name="multiMesh">Owned instance identity.</param>
    /// <param name="quality">Fast component or High angular interpolation.</param>
    public void MultiMeshSetPhysicsInterpolationQuality(RID multiMesh, MultiMesh.PhysicsInterpolationQuality quality) { EnsureTextureChange(); RenderingMultiMeshRegistry.Owned(multiMesh, this).PhysicsInterpolationQualityMode = quality; }
    private void ReleaseOwnedMultiMeshes()
    {
        List<Exception>? errors = null;
        foreach (var rid in _ownedMultiMeshRIDs) try { var resource = RenderingMultiMeshRegistry.Owned(rid, this); RenderingMultiMeshRegistry.Remove(rid); resource.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        _ownedMultiMeshRIDs.Clear(); if (errors is not null) throw new AggregateException("Instance storage shutdown callbacks failed.", errors);
    }
}
