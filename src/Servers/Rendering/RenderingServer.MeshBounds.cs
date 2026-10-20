namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Gets the authored mesh-local visibility override of a logical mesh.</summary>
    /// <param name="mesh">Owned or borrowed live mesh identity.</param>
    /// <returns>The manual rectangle, or the zero rectangle for automatic bounds.</returns>
    /// <exception cref="ArgumentException">The identity is absent or disposed.</exception>
    /// <exception cref="InvalidOperationException">The renderer is inactive or the caller is not its owner thread.</exception>
    public static Rect2 MeshGetCustomAABB(RID mesh)
    {
        RequireService().EnsureOwner();
        return RenderingMeshRegistry.Resolve(mesh).VisibilityOverride;
    }

    /// <summary>Sets a caller-owned mesh's local visibility rectangle.</summary>
    /// <param name="mesh">Live mesh identity owned by the active renderer.</param>
    /// <param name="aabb">Finite nonnegative bounds; the zero rectangle restores automatic visibility.</param>
    /// <remarks>Existing retained draws and instances observe the override without rerecording. It does not
    /// change geometry, per-surface bounds or packed surface descriptions.</remarks>
    /// <exception cref="ArgumentException">The identity or rectangle is invalid.</exception>
    /// <exception cref="InvalidOperationException">The identity is borrowed, the caller is not the owner, or mutation is unavailable during submission or shutdown.</exception>
    public static void MeshSetCustomAABB(RID mesh, Rect2 aabb)
    {
        var service = RequireService(); service.EnsureTextureChange();
        RenderingMeshRegistry.Owned(mesh, service).CustomAABB = aabb;
    }
}
