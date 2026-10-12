namespace Electron2D;

public partial class MeshInstance
{
    private float[] _blendShapeValues = [];
    private MeshMorphPose? _morph;
    private bool _readingBlendShapes;
    private void EnsureMorphMutable() { if (_readingBlendShapes || _morph?.IsPreparing == true) throw new InvalidOperationException("Morph source callbacks cannot mutate or dispose their receiving instance."); }
    private int ReadBlendShapeCount()
    {
        if (_mesh is null) throw new InvalidOperationException("Assign a mesh before selecting blend shape values.");
        if (_readingBlendShapes) throw new InvalidOperationException("Blend shape count callbacks cannot reenter their receiving instance.");
        _readingBlendShapes = true; try { return _mesh.GetBlendShapeCount(); } finally { _readingBlendShapes = false; }
    }
    private void EnsureBlendShapeValues() { var count = ReadBlendShapeCount(); if (_blendShapeValues.Length != count) Array.Resize(ref _blendShapeValues, count); }
    /// <summary>Gets the finite deformation weight of one shape on this instance.</summary>
    /// <param name="index">Existing zero-based shape index.</param>
    /// <returns>Zero initially; weights belong to this node independently of shared mesh resources.</returns>
    public float GetBlendShapeValue(int index) { ThrowIfDisposed(); var count = ReadBlendShapeCount(); if ((uint)index >= (uint)count) throw new ArgumentOutOfRangeException(nameof(index)); return index < _blendShapeValues.Length ? _blendShapeValues[index] : 0; }
    /// <summary>Sets one finite deformation weight and validates resulting local geometry before mutation.</summary>
    /// <param name="index">Existing zero-based shape index.</param>
    /// <param name="value">Finite weight; values are not clamped or renormalized; absolute values at most 0.0001 have no drawing contribution.</param>
    /// <remarks>Normalized and relative modes use the mesh's selected policy. Changes update this node's live
    /// positions before skinning; numerical normal/tangent, material, UV/custom and skin channels remain inherited.
    /// Prepared scalar changes and drawing reuse node-owned geometry storage.</remarks>
    public void SetBlendShapeValue(int index, float value)
    {
        EnsureMutable(); EnsureMorphMutable(); if (!float.IsFinite(value)) throw new ArgumentException("Blend shape values must be finite.", nameof(value)); EnsureBlendShapeValues();
        if ((uint)index >= (uint)_blendShapeValues.Length) throw new ArgumentOutOfRangeException(nameof(index)); if (_blendShapeValues[index] == value) return;
        (_morph ??= new()).Prepare(_mesh!, _blendShapeValues, index, value); _blendShapeValues[index] = value; InvalidateCanvas();
    }
    private Mesh DrawnMesh()
    {
        for (var i = 0; i < _blendShapeValues.Length; i++) if (_blendShapeValues[i] != 0) { EnsureBlendShapeValues(); return (_morph ??= new()).Prepare(_mesh!, _blendShapeValues); }
        return _mesh!;
    }
    /// <inheritdoc />
    protected override void ValidateMutation() { EnsureMorphMutable(); base.ValidateMutation(); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { EnsureMorphMutable(); base.ValidateDisposal(); }
    private float[] StoredBlendShapeValues
    {
        get => (float[])_blendShapeValues.Clone();
        set { ArgumentNullException.ThrowIfNull(value); foreach (var weight in value) if (!float.IsFinite(weight)) throw new ArgumentException("Stored blend shape values must be finite."); EnsureMutable(); EnsureMorphMutable(); if (_mesh is not null && value.Length != ReadBlendShapeCount()) throw new ArgumentException("Stored values must match named mesh shapes."); _blendShapeValues = (float[])value.Clone(); _morph?.Dispose(); _morph = null; InvalidateCanvas(); }
    }
}
