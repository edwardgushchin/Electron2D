namespace Electron2D;

public sealed partial class NavigationServer
{
    /// <summary>Samples an enabled nonempty committed map surface matching the supplied navigation layers.</summary>
    /// <param name="map">A live map RID.</param><param name="navigationLayers">Eligible layer mask.</param><param name="uniformly">True weights every surface level by area; false chooses regions, polygons and triangles uniformly.</param>
    /// <returns>A world point inside a surface, or zero when no eligible surface exists.</returns>
    /// <remarks>Prepared surface groups are reused. Random sequences are not guaranteed across runtime versions.</remarks>
    public static Vector2 MapGetRandomPoint(RID map, uint navigationLayers, bool uniformly) { lock (Shared._gate) return Shared.Map(map).Iteration.RandomPoint(navigationLayers, uniformly); }
    /// <summary>Samples an enabled committed region surface matching the supplied navigation layers.</summary>
    /// <param name="region">A live region RID.</param><param name="navigationLayers">Eligible layer mask.</param><param name="uniformly">True weights polygons and triangles by area; false chooses both uniformly.</param>
    /// <returns>A world point inside the region, or zero when no eligible surface exists.</returns>
    /// <remarks>Detached regions retain geometry for this query. Prepared surface groups are reused.</remarks>
    public static Vector2 RegionGetRandomPoint(RID region, uint navigationLayers, bool uniformly) { lock (Shared._gate) return Shared.Region(region).Iteration.RandomPoint(navigationLayers, uniformly); }
}
