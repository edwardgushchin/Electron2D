namespace Electron2D;

public sealed partial class ArrayMesh
{
    /// <inheritdoc />
    protected override IReadOnlyDictionary<float, int[]> OnSurfaceGetLODs(int surfaceIndex)
    {
        lock (Gate) { ThrowIfDisposed(); return MeshLOD.Copy(Get(surfaceIndex).LODs); }
    }
}
