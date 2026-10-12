namespace Electron2D;

public abstract partial class Mesh
{
    /// <summary>Returns copied local edge-error thresholds and alternative vertex indices.</summary>
    /// <param name="surfaceIndex">Existing zero-based surface index.</param>
    /// <returns>Ascending positive finite thresholds mapped to independent index arrays.</returns>
    /// <remarks>Canvas drawing chooses the largest threshold whose transformed error is at most one
    /// framebuffer pixel. Base arrays, bounds and face queries keep the full authored geometry.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The surface index is outside the mesh.</exception>
    public IReadOnlyDictionary<float, int[]> SurfaceGetLODs(int surfaceIndex)
    {
        ThrowIfDisposed();
        if ((uint)surfaceIndex >= (uint)GetSurfaceCount()) throw new ArgumentOutOfRangeException(nameof(surfaceIndex));
        return OnSurfaceGetLODs(surfaceIndex);
    }
    /// <summary>Supplies copied alternative index sets for one surface.</summary>
    /// <param name="surfaceIndex">Existing zero-based surface index.</param>
    /// <returns>Positive finite local thresholds and independent index arrays; empty without LODs.</returns>
    protected virtual IReadOnlyDictionary<float, int[]> OnSurfaceGetLODs(int surfaceIndex) => new Dictionary<float, int[]>();
}

internal readonly record struct MeshLOD(float Error, MeshSurfaceData Data)
{
    internal static MeshLOD[] Capture(IReadOnlyDictionary<float, int[]>? source, MeshSurfaceData basis, Mesh.PrimitiveType primitive)
    {
        if (source is null || source.Count == 0) return [];
        if (source.Count > 4096) throw new ArgumentException("LOD count exceeds the surface budget.", nameof(source));
        if (basis.Indices.Length == 0) throw new ArgumentException("LOD surfaces require base indices.", nameof(source));
        var result = new MeshLOD[source.Count]; var next = 0;
        foreach (var (error, indices) in source)
        {
            ArgumentNullException.ThrowIfNull(indices);
            if (!float.IsFinite(error) || error <= 0) throw new ArgumentException("LOD errors must be finite and positive.", nameof(source));
            if (indices.Length == 0 || indices.Length >= basis.Indices.Length) throw new ArgumentException("LOD indices must be nonempty and fewer than base indices.", nameof(source));
            var data = new MeshSurfaceData
            {
                Vertices = basis.Vertices,
                Normals = basis.Normals,
                Tangents = basis.Tangents,
                Colors = basis.Colors,
                UVs = basis.UVs,
                UV2s = basis.UV2s,
                Bones = basis.Bones,
                Weights = basis.Weights,
                Custom0 = basis.Custom0,
                Custom1 = basis.Custom1,
                Custom2 = basis.Custom2,
                Custom3 = basis.Custom3,
                Indices = (int[])indices.Clone()
            };
            data.Validate(primitive); result[next++] = new(error, data);
        }
        if (next != result.Length) throw new ArgumentException("LOD count changed during capture.", nameof(source));
        Array.Sort(result, static (a, b) => a.Error.CompareTo(b.Error));
        for (var i = 1; i < result.Length; i++) if (result[i - 1].Error == result[i].Error) throw new ArgumentException("LOD errors must be distinct.", nameof(source));
        return result;
    }
    internal static Dictionary<float, int[]> Copy(MeshLOD[] lods)
    {
        var result = new Dictionary<float, int[]>(lods.Length);
        foreach (var lod in lods) result.Add(lod.Error, (int[])lod.Data.Indices.Clone());
        return result;
    }
    internal static MeshSurfaceData Select(MeshSurfaceData basis, MeshLOD[] lods, Transform transform)
    {
        if (lods.Length == 0) return basis;
        // The largest singular value includes shear, anisotropic scale and reflection.
        double a = transform.X.X, b = transform.X.Y, c = transform.Y.X, d = transform.Y.Y;
        var xx = a * a + b * b; var yy = c * c + d * d; var xy = a * c + b * d;
        var scale = Math.Sqrt((xx + yy + double.Hypot(xx - yy, 2 * xy)) * .5);
        var result = basis;
        foreach (var lod in lods) { if (lod.Error * scale > 1) break; result = lod.Data; }
        return result;
    }
}
