namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Returns copied alternative indices for an owned or borrowed mesh surface.</summary>
    /// <param name="mesh">Live logical mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <returns>Positive local error thresholds mapped to independent index arrays.</returns>
    /// <remarks>Canvas replay selects levels from the full framebuffer transform; base queries retain full geometry.</remarks>
    public static IReadOnlyDictionary<float, int[]> MeshSurfaceGetLODs(RID mesh, int surface)
    {
        var service = RequireService(); service.EnsureOwner();
        return RenderingMeshRegistry.Resolve(mesh).SurfaceGetLODs(surface);
    }
}
