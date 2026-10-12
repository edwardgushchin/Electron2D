namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Creates caller-owned empty mesh storage with a fixed shape count before its first surface.</summary>
    /// <param name="blendShapeCount">Zero through 4096; owned definitions use ordinal index names.</param>
    /// <returns>A new renderer-owned logical identity released by FreeRID.</returns>
    /// <remarks>The no-argument MeshCreate overload retains its zero-shape default. Every added surface
    /// must supply this many typed shape payloads.</remarks>
    public static RID MeshCreate(int blendShapeCount) => RequireService().MeshCreateCore(blendShapeCount);
    /// <summary>Gets the number of named shapes on an owned or borrowed live mesh.</summary>
    /// <param name="mesh">Live logical mesh identity.</param>
    /// <returns>The resource's nonnegative shape count.</returns>
    public static int MeshGetBlendShapeCount(RID mesh) { var service = RequireService(); service.EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).GetBlendShapeCount(); }
    /// <summary>Gets a mesh's selected target mixing mode.</summary>
    /// <param name="mesh">Owned or borrowed live mesh identity.</param>
    /// <returns>The array resource's mode, or Relative for other mesh implementations.</returns>
    public static BlendShapeMode MeshGetBlendShapeMode(RID mesh) { var service = RequireService(); service.EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh) is ArrayMesh array ? array.BlendShapeMode : BlendShapeMode.Relative; }
    /// <summary>Selects target mixing mode on a caller-owned mesh.</summary>
    /// <param name="mesh">Caller-owned mesh identity; resource-owned identities cannot be mutated.</param>
    /// <param name="mode">Normalized or Relative.</param>
    public static void MeshSetBlendShapeMode(RID mesh, BlendShapeMode mode) { var service = RequireService(); service.EnsureTextureChange(); RenderingMeshRegistry.Owned(mesh, service).BlendShapeMode = mode; }
    /// <summary>Gets copied shape payloads for one live mesh surface.</summary>
    /// <param name="mesh">Owned or borrowed live mesh identity.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <returns>Copied position/normal/tangent data in shape-index order.</returns>
    public static MeshSurfaceData[] MeshSurfaceGetBlendShapeArrays(RID mesh, int surface) { var service = RequireService(); service.EnsureOwner(); return RenderingMeshRegistry.Resolve(mesh).SurfaceGetBlendShapeArrays(surface); }
}
