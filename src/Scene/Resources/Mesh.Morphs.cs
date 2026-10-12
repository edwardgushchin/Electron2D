namespace Electron2D;

/// <summary>Selects absolute normalized targets or additive relative mesh deformation.</summary>
public enum BlendShapeMode
{
    /// <summary>Base contribution is one minus the sum of effective target weights.</summary>
    Normalized = 0,
    /// <summary>Base contribution remains one and targets are additive offsets.</summary>
    Relative = 1
}

public abstract partial class Mesh
{
    /// <summary>Gets the number of named blend shapes.</summary>
    /// <returns>A nonnegative count, zero for an unshaped mesh.</returns>
    public int GetBlendShapeCount() { ThrowIfDisposed(); var count = OnGetBlendShapeCount(); if (count < 0) throw new InvalidOperationException("Blend shape count must be nonnegative."); return count; }
    /// <summary>Gets one shape's authored name.</summary>
    /// <param name="index">Existing zero-based shape index.</param>
    /// <returns>A non-null name; empty names are permitted.</returns>
    public string GetBlendShapeName(int index) { ThrowIfDisposed(); CheckBlendShapeIndex(index); return OnGetBlendShapeName(index) ?? throw new InvalidOperationException("Blend shape names must not be null."); }
    /// <summary>Renames one shape without changing its index or payload.</summary>
    /// <param name="index">Existing zero-based shape index.</param>
    /// <param name="name">Non-null authored name.</param>
    public void SetBlendShapeName(int index, string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); CheckBlendShapeIndex(index); OnSetBlendShapeName(index, name); }
    /// <summary>Gets copied position/normal/tangent payloads for all named shapes of one surface.</summary>
    /// <param name="surfaceIndex">Existing zero-based surface index.</param>
    /// <returns>Independent payloads in shape-index order; empty for an unshaped mesh.</returns>
    public MeshSurfaceData[] SurfaceGetBlendShapeArrays(int surfaceIndex) { ThrowIfDisposed(); if ((uint)surfaceIndex >= (uint)GetSurfaceCount()) throw new ArgumentOutOfRangeException(nameof(surfaceIndex)); return OnSurfaceGetBlendShapeArrays(surfaceIndex) ?? throw new InvalidOperationException("Blend shape arrays must not be null."); }
    private void CheckBlendShapeIndex(int index) { if ((uint)index >= (uint)GetBlendShapeCount()) throw new ArgumentOutOfRangeException(nameof(index)); }
    /// <summary>Supplies the number of named shapes; unshaped meshes return zero.</summary>
    /// <returns>A nonnegative count.</returns>
    protected virtual int OnGetBlendShapeCount() => 0;
    /// <summary>Supplies a name for an existing shape.</summary>
    /// <param name="index">Existing zero-based shape index.</param>
    /// <returns>A non-null name.</returns>
    protected virtual string OnGetBlendShapeName(int index) => throw new ArgumentOutOfRangeException(nameof(index));
    /// <summary>Renames an existing shape and publishes its resource change.</summary>
    /// <param name="index">Existing zero-based shape index.</param>
    /// <param name="name">Non-null authored name.</param>
    protected virtual void OnSetBlendShapeName(int index, string name) => throw new ArgumentOutOfRangeException(nameof(index));
    /// <summary>Supplies copied shape payloads in the mesh's shape-index order.</summary>
    /// <param name="surfaceIndex">Existing zero-based surface index.</param>
    /// <returns>Position/normal/tangent-only payloads, or empty when the mesh has no shapes.</returns>
    protected virtual MeshSurfaceData[] OnSurfaceGetBlendShapeArrays(int surfaceIndex) => GetBlendShapeCount() == 0 ? [] : throw new NotSupportedException("A shaped mesh must supply its surface shape payloads.");
}
