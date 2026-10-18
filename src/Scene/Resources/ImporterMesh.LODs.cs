namespace Electron2D;

public sealed partial class ImporterMesh
{
    /// <summary>Replaces indexed triangle levels with actual attribute-aware simplification.</summary>
    /// <param name="normalMergeAngle">Finite angular threshold in degrees for welding coincident normal-compatible vertices.</param>
    /// <param name="boneTransformArray">Optional finite two-dimensional skin pose used to measure errors; original channels remain raw.</param>
    /// <remarks>Nontriangle and shader-generated levels remain intact; unindexed/small triangles produce no generated levels.
    /// All surfaces preflight before publication. Generation may allocate managed/native temporary storage and needs the matching native mesh library.</remarks>
    /// <exception cref="ArgumentException">Coordinates, pose, thresholds or generated data are invalid.</exception>
    public void GenerateLODs(float normalMergeAngle, IReadOnlyList<Transform>? boneTransformArray = null)
    {
        if (!float.IsFinite(normalMergeAngle)) throw new ArgumentOutOfRangeException(nameof(normalMergeAngle));
        var transforms = boneTransformArray?.ToArray(); if (transforms is not null) foreach (var t in transforms) if (!t.X.IsFinite() || !t.Y.IsFinite() || !t.Origin.IsFinite()) throw new ArgumentException("Bone transforms must be finite.", nameof(boneTransformArray));
        lock (_gate)
        {
            Mutable(); _preparing = true;
            try { var levels = new MeshLOD[_surfaces.Count][]; for (var i = 0; i < levels.Length; i++) { var s = _surfaces[i]; levels[i] = MeshSimplifier.Generate(s.Data, s.Shapes, s.Primitive, s.LODs, normalMergeAngle, transforms); } for (var i = 0; i < levels.Length; i++) _surfaces[i].LODs = levels[i]; Invalidate(); }
            finally { _preparing = false; }
        }
        EmitChanged();
    }
}
