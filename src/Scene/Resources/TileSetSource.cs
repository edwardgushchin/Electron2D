namespace Electron2D;

/// <summary>Provides coordinate and alternative identities for tiles owned by a tile set.</summary>
/// <remarks>A source belongs to at most one set. Adding it to another set transfers it.</remarks>
public abstract class TileSetSource : Resource
{
    internal TileSet? OwnerSet;
    /// <summary>Returns the number of base tiles.</summary>
    /// <returns>Returns the number of base tiles.</returns>
    public abstract int GetTilesCount();
    /// <summary>Returns the coordinates at a sorted tile index.</summary>
    /// <param name="index">Zero-based sorted entry index.</param>
    /// <returns>Returns the coordinates at a sorted tile index.</returns>
    public abstract Vector2i GetTileID(int index);
    /// <summary>Tests whether a base tile exists at the coordinates.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    /// <returns>Tests whether a base tile exists at the coordinates.</returns>
    public abstract bool HasTile(Vector2i atlasCoords);
    /// <summary>Returns the number of alternatives, including the base tile, or minus one for a missing tile.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    /// <returns>Returns the number of alternatives, including the base tile, or minus one for a missing tile.</returns>
    public abstract int GetAlternativeTilesCount(Vector2i atlasCoords);
    /// <summary>Returns the alternative identity at a sorted index.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    /// <param name="index">Zero-based sorted entry index.</param>
    /// <returns>Returns the alternative identity at a sorted index.</returns>
    public abstract int GetAlternativeTileID(Vector2i atlasCoords, int index);
    /// <summary>Tests whether a tile and alternative exist.</summary>
    /// <param name="atlasCoords">Base atlas tile coordinates.</param>
    /// <param name="alternativeTile">Alternative identity, including optional cell transform flags where supported.</param>
    /// <returns>Tests whether a tile and alternative exist.</returns>
    public abstract bool HasAlternativeTile(Vector2i atlasCoords, int alternativeTile);
    internal abstract void ResizePhysicsLayers(int count);
    internal abstract void InsertPhysicsLayer(int index);
    internal abstract void RemovePhysicsLayer(int index);
    internal abstract void MovePhysicsLayer(int from, int to);
}
