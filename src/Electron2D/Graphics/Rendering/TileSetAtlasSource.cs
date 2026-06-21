/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
namespace Electron2D;

/// <summary>
/// Provides a tile set source backed by a texture atlas.
/// </summary>
///
/// <remarks>
/// <para>
/// `TileSetAtlasSource` maps atlas coordinates to <see cref="TileData"/> and
/// texture regions. It is the only tile source implemented by the 0.1.0 Preview
/// runtime.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate atlas sources on the main
/// scene thread or during loading.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="TileSet"/>
/// <seealso cref="TileData"/>
public sealed class TileSetAtlasSource : TileSetSource
{
    private readonly Dictionary<TileKey, TileEntry> tiles = new();
    private Texture2D? texture;
    private Vector2I textureRegionSize = new(16, 16);

    /// <summary>
    /// Initializes a new instance of the <see cref="TileSetAtlasSource"/> type.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The source starts without a texture and uses a <c>16x16</c> texture
    /// region size until configured otherwise.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it from the thread that owns
    /// the resource being created.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Texture"/>
    public TileSetAtlasSource()
    {
    }

    /// <summary>
    /// Gets or sets the atlas texture sampled by this source.
    /// </summary>
    ///
    /// <value>
    /// The texture containing tile regions, or <c>null</c> when no texture is
    /// assigned.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// `TileMapLayer` skips cells that reference a source without a texture.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Texture2D"/>
    public Texture2D? Texture
    {
        get
        {
            ThrowIfFreed();
            return texture;
        }
        set
        {
            ThrowIfFreed();
            texture = value;
        }
    }

    /// <summary>
    /// Gets or sets the size of one atlas tile region in pixels.
    /// </summary>
    ///
    /// <value>
    /// The positive width and height used to compute source rectangles.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// A tile at atlas coordinates <c>(x, y)</c> uses a source rectangle whose
    /// position is <c>(x * width, y * height)</c>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either component is less than or equal to zero.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetTileTextureRegion"/>
    public Vector2I TextureRegionSize
    {
        get
        {
            ThrowIfFreed();
            return textureRegionSize;
        }
        set
        {
            ThrowIfFreed();
            ValidatePositiveSize(value, nameof(TextureRegionSize));
            textureRegionSize = value;
        }
    }

    /// <summary>
    /// Creates tile data for an atlas coordinate.
    /// </summary>
    ///
    /// <param name="atlasCoords">The atlas coordinates identifying the tile.</param>
    /// <param name="sizeInAtlas">The positive tile size in atlas cells.</param>
    ///
    /// <remarks>
    /// <para>
    /// Recreating an existing tile keeps the existing <see cref="TileData"/> and
    /// updates only its atlas-cell size.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when atlas coordinates are negative or size components are not positive.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetTileData"/>
    public void CreateTile(Vector2I atlasCoords, Vector2I sizeInAtlas = default)
    {
        ThrowIfFreed();
        ValidateAtlasCoords(atlasCoords);
        var normalizedSize = sizeInAtlas == default ? Vector2I.One : sizeInAtlas;
        ValidatePositiveSize(normalizedSize, nameof(sizeInAtlas));

        var key = new TileKey(atlasCoords, AlternativeTile: 0);
        if (tiles.TryGetValue(key, out var existing))
        {
            existing.SizeInAtlas = normalizedSize;
            return;
        }

        tiles.Add(key, new TileEntry(new TileData(), normalizedSize));
    }

    /// <summary>
    /// Removes tile data for an atlas coordinate.
    /// </summary>
    ///
    /// <param name="atlasCoords">The atlas coordinates identifying the tile.</param>
    ///
    /// <remarks>
    /// <para>
    /// Removing a missing tile does nothing.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when atlas coordinates are negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="HasTile"/>
    public void RemoveTile(Vector2I atlasCoords)
    {
        ThrowIfFreed();
        ValidateAtlasCoords(atlasCoords);
        tiles.Remove(new TileKey(atlasCoords, AlternativeTile: 0));
    }

    /// <summary>
    /// Checks whether tile data exists for an atlas coordinate.
    /// </summary>
    ///
    /// <param name="atlasCoords">The atlas coordinates identifying the tile.</param>
    /// <returns><c>true</c> when the tile exists; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>
    /// <para>
    /// This method checks the default alternative tile entry.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when atlas coordinates are negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="CreateTile"/>
    public bool HasTile(Vector2I atlasCoords)
    {
        ThrowIfFreed();
        ValidateAtlasCoords(atlasCoords);
        return tiles.ContainsKey(new TileKey(atlasCoords, AlternativeTile: 0));
    }

    /// <summary>
    /// Gets the number of tiles stored by this atlas source.
    /// </summary>
    ///
    /// <returns>The number of default alternative tiles.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The count includes tiles even when <see cref="Texture"/> is not assigned.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetTileId"/>
    public int GetTilesCount()
    {
        ThrowIfFreed();
        return tiles.Count(static pair => pair.Key.AlternativeTile == 0);
    }

    /// <summary>
    /// Gets the atlas coordinates of a tile by index.
    /// </summary>
    ///
    /// <param name="index">The zero-based index in sorted tile order.</param>
    /// <returns>The atlas coordinates of the tile.</returns>
    ///
    /// <remarks>
    /// <para>
    /// Tiles are ordered by Y coordinate and then X coordinate for stable
    /// serialization and tests.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the stored tile range.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetTilesCount"/>
    public Vector2I GetTileId(int index)
    {
        ThrowIfFreed();
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        var tileIds = SortedTileIds();
        if (index >= tileIds.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Tile index is outside the stored tile range.");
        }

        return tileIds[index];
    }

    /// <summary>
    /// Gets the tile data for an atlas coordinate.
    /// </summary>
    ///
    /// <param name="atlasCoords">The atlas coordinates identifying the tile.</param>
    /// <param name="alternativeTile">The alternative tile id.</param>
    /// <returns>The tile data, or <c>null</c> when the tile does not exist.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The 0.1.0 Preview stores only the default alternative tile, but the
    /// parameter is accepted for call-site compatibility.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when atlas coordinates or the alternative tile id are negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="CreateTile"/>
    public TileData? GetTileData(Vector2I atlasCoords, int alternativeTile = 0)
    {
        ThrowIfFreed();
        ValidateTileKey(atlasCoords, alternativeTile);
        return tiles.TryGetValue(new TileKey(atlasCoords, alternativeTile), out var tile) ? tile.Data : null;
    }

    /// <summary>
    /// Gets the texture region used by a tile.
    /// </summary>
    ///
    /// <param name="atlasCoords">The atlas coordinates identifying the tile.</param>
    /// <param name="alternativeTile">The alternative tile id.</param>
    /// <returns>The source rectangle in atlas texture pixels.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The returned region is based on <see cref="TextureRegionSize"/> and the
    /// tile's size in atlas cells.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when atlas coordinates or the alternative tile id are negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the tile does not exist.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="TextureRegionSize"/>
    public Rect2 GetTileTextureRegion(Vector2I atlasCoords, int alternativeTile = 0)
    {
        ThrowIfFreed();
        return TryGetTileTextureRegion(atlasCoords, alternativeTile, out var region)
            ? region
            : throw new InvalidOperationException($"Tile at atlas coordinates '{atlasCoords}' does not exist.");
    }

    internal bool TryGetTileTextureRegion(Vector2I atlasCoords, int alternativeTile, out Rect2 region)
    {
        ValidateTileKey(atlasCoords, alternativeTile);
        if (!tiles.TryGetValue(new TileKey(atlasCoords, alternativeTile), out var tile))
        {
            region = default;
            return false;
        }

        region = new Rect2(
            atlasCoords.X * textureRegionSize.X,
            atlasCoords.Y * textureRegionSize.Y,
            tile.SizeInAtlas.X * textureRegionSize.X,
            tile.SizeInAtlas.Y * textureRegionSize.Y);
        return true;
    }

    internal Vector2I GetTileSizeInAtlas(Vector2I atlasCoords, int alternativeTile = 0)
    {
        ValidateTileKey(atlasCoords, alternativeTile);
        return tiles.TryGetValue(new TileKey(atlasCoords, alternativeTile), out var tile)
            ? tile.SizeInAtlas
            : throw new InvalidOperationException($"Tile at atlas coordinates '{atlasCoords}' does not exist.");
    }

    internal IEnumerable<(Vector2I AtlasCoords, int AlternativeTile, Vector2I SizeInAtlas, TileData Data)> EnumerateTiles()
    {
        foreach (var pair in tiles
            .OrderBy(static pair => pair.Key.AlternativeTile)
            .ThenBy(static pair => pair.Key.AtlasCoords.Y)
            .ThenBy(static pair => pair.Key.AtlasCoords.X))
        {
            yield return (pair.Key.AtlasCoords, pair.Key.AlternativeTile, pair.Value.SizeInAtlas, pair.Value.Data);
        }
    }

    private Vector2I[] SortedTileIds()
    {
        return tiles.Keys
            .Where(static key => key.AlternativeTile == 0)
            .OrderBy(static key => key.AtlasCoords.Y)
            .ThenBy(static key => key.AtlasCoords.X)
            .Select(static key => key.AtlasCoords)
            .ToArray();
    }

    private static void ValidateTileKey(Vector2I atlasCoords, int alternativeTile)
    {
        ValidateAtlasCoords(atlasCoords);
        ArgumentOutOfRangeException.ThrowIfNegative(alternativeTile);
    }

    private static void ValidateAtlasCoords(Vector2I atlasCoords)
    {
        if (atlasCoords.X < 0 || atlasCoords.Y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(atlasCoords), atlasCoords, "Atlas coordinates must be non-negative.");
        }
    }

    private static void ValidatePositiveSize(Vector2I size, string parameterName)
    {
        if (size.X <= 0 || size.Y <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, size, "Size components must be greater than zero.");
        }
    }

    private readonly record struct TileKey(Vector2I AtlasCoords, int AlternativeTile);

    private sealed class TileEntry
    {
        public TileEntry(TileData data, Vector2I sizeInAtlas)
        {
            Data = data;
            SizeInAtlas = sizeInAtlas;
        }

        public TileData Data { get; }

        public Vector2I SizeInAtlas { get; set; }
    }
}
