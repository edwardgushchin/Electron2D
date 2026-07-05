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
/// Stores per-tile metadata consumed by <see cref="TileMapLayer"/>.
/// </summary>
///
/// <remarks>
/// <para>
/// `TileData` contains the rendering modulation and collision polygon metadata
/// for a tile entry inside a <see cref="TileSetAtlasSource"/>.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate tile data on the main
/// scene thread or during loading.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="TileSetAtlasSource"/>
/// <seealso cref="TileMapLayer"/>
public sealed class TileData : Object
{
    private readonly Dictionary<int, List<CollisionPolygon>> collisionLayers = new();
    private Color modulate = Color.White;
    private Vector2 textureOrigin = Vector2.Zero;
    private int zIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="TileData"/> type.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// New tile data starts with white modulation, zero texture origin, zero
    /// z-index and no collision polygons.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it from the thread that owns
    /// the resource being created.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="TileSetAtlasSource.CreateTile"/>
    public TileData()
    {
    }

    /// <summary>
    /// Gets or sets the color multiplied into the tile draw command.
    /// </summary>
    ///
    /// <value>
    /// The color multiplied into this tile; the default value is
    /// <see cref="Color.White"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The value is combined with the owning layer's inherited canvas
    /// modulation during rendering.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="TileMapLayer"/>
    public Color Modulate
    {
        get
        {
            ThrowIfFreed();
            return modulate;
        }
        set
        {
            ThrowIfFreed();
            modulate = value;
        }
    }

    /// <summary>
    /// Gets or sets the visual offset applied to this tile inside its cell.
    /// </summary>
    ///
    /// <value>
    /// The local offset in pixels from the cell's top-left corner.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// `TileMapLayer` adds this value to the destination rectangle position
    /// when it submits the tile for rendering.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="TileMapLayer.MapToLocal"/>
    public Vector2 TextureOrigin
    {
        get
        {
            ThrowIfFreed();
            return textureOrigin;
        }
        set
        {
            ThrowIfFreed();
            textureOrigin = value;
        }
    }

    /// <summary>
    /// Gets or sets the z-index offset for this tile.
    /// </summary>
    ///
    /// <value>
    /// The z-index offset added to the owning <see cref="TileMapLayer.ZIndex"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// This preview uses the value when ordering submitted tile commands inside
    /// the canvas render queue.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="CanvasItem.ZIndex"/>
    public int ZIndex
    {
        get
        {
            ThrowIfFreed();
            return zIndex;
        }
        set
        {
            ThrowIfFreed();
            zIndex = value;
        }
    }

    /// <summary>
    /// Sets the number of collision polygons stored in a collision layer.
    /// </summary>
    ///
    /// <param name="layerId">The collision layer metadata id.</param>
    /// <param name="polygonsCount">The number of polygons to keep in the layer.</param>
    ///
    /// <remarks>
    /// <para>
    /// Increasing the count creates empty polygons. Decreasing the count removes
    /// polygons at the end of the layer.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="layerId"/> or <paramref name="polygonsCount"/>
    /// is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetCollisionPolygonsCount"/>
    public void SetCollisionPolygonsCount(int layerId, int polygonsCount)
    {
        ThrowIfFreed();
        ValidateLayerId(layerId);
        ArgumentOutOfRangeException.ThrowIfNegative(polygonsCount);

        var polygons = GetOrCreateCollisionLayer(layerId);
        while (polygons.Count < polygonsCount)
        {
            polygons.Add(new CollisionPolygon());
        }

        if (polygons.Count > polygonsCount)
        {
            polygons.RemoveRange(polygonsCount, polygons.Count - polygonsCount);
        }
    }

    /// <summary>
    /// Gets the number of collision polygons in a collision layer.
    /// </summary>
    ///
    /// <param name="layerId">The collision layer metadata id.</param>
    /// <returns>The number of polygons stored in the layer.</returns>
    ///
    /// <remarks>
    /// <para>
    /// Missing layers report <c>0</c>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="layerId"/> is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetCollisionPolygonsCount"/>
    public int GetCollisionPolygonsCount(int layerId)
    {
        ThrowIfFreed();
        ValidateLayerId(layerId);
        return collisionLayers.TryGetValue(layerId, out var polygons) ? polygons.Count : 0;
    }

    /// <summary>
    /// Sets the points for a collision polygon.
    /// </summary>
    ///
    /// <param name="layerId">The collision layer metadata id.</param>
    /// <param name="polygonIndex">The polygon index inside the layer.</param>
    /// <param name="polygon">The polygon points in cell-local coordinates.</param>
    ///
    /// <remarks>
    /// <para>
    /// The runtime physics baseline uses the AABB of these points for direct
    /// queries and body movement.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="polygon"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the layer or polygon index is invalid.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetCollisionPolygonPoints"/>
    public void SetCollisionPolygonPoints(int layerId, int polygonIndex, Vector2[] polygon)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(polygon);
        GetCollisionPolygon(layerId, polygonIndex).Points = polygon.ToArray();
    }

    /// <summary>
    /// Gets the points for a collision polygon.
    /// </summary>
    ///
    /// <param name="layerId">The collision layer metadata id.</param>
    /// <param name="polygonIndex">The polygon index inside the layer.</param>
    /// <returns>A new array containing the polygon points.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The returned array can be modified by the caller without mutating this
    /// tile data. Use <see cref="SetCollisionPolygonPoints"/> to store changes.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the layer or polygon index is invalid.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetCollisionPolygonPoints"/>
    public Vector2[] GetCollisionPolygonPoints(int layerId, int polygonIndex)
    {
        ThrowIfFreed();
        return GetCollisionPolygon(layerId, polygonIndex).Points.ToArray();
    }

    /// <summary>
    /// Sets whether a collision polygon is one-way.
    /// </summary>
    ///
    /// <param name="layerId">The collision layer metadata id.</param>
    /// <param name="polygonIndex">The polygon index inside the layer.</param>
    /// <param name="oneWay">Whether the polygon should block only from one side.</param>
    ///
    /// <remarks>
    /// <para>
    /// The managed movement baseline uses this flag for downward movement
    /// against tile polygons.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the layer or polygon index is invalid.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="IsCollisionPolygonOneWay"/>
    public void SetCollisionPolygonOneWay(int layerId, int polygonIndex, bool oneWay)
    {
        ThrowIfFreed();
        GetCollisionPolygon(layerId, polygonIndex).OneWay = oneWay;
    }

    /// <summary>
    /// Checks whether a collision polygon is one-way.
    /// </summary>
    ///
    /// <param name="layerId">The collision layer metadata id.</param>
    /// <param name="polygonIndex">The polygon index inside the layer.</param>
    /// <returns><c>true</c> when the polygon is one-way; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>
    /// <para>
    /// Missing or invalid polygons are not treated as one-way polygons.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the layer or polygon index is invalid.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetCollisionPolygonOneWay"/>
    public bool IsCollisionPolygonOneWay(int layerId, int polygonIndex)
    {
        ThrowIfFreed();
        return GetCollisionPolygon(layerId, polygonIndex).OneWay;
    }

    /// <summary>
    /// Sets the one-way collision margin for a polygon.
    /// </summary>
    ///
    /// <param name="layerId">The collision layer metadata id.</param>
    /// <param name="polygonIndex">The polygon index inside the layer.</param>
    /// <param name="margin">The non-negative margin in local units.</param>
    ///
    /// <remarks>
    /// <para>
    /// Higher values make one-way collision more tolerant for fast downward
    /// movement.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the layer, polygon index or margin is invalid.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetCollisionPolygonOneWayMargin"/>
    public void SetCollisionPolygonOneWayMargin(int layerId, int polygonIndex, float margin)
    {
        ThrowIfFreed();
        if (!Mathf.IsFinite(margin) || margin < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(margin), margin, "One-way collision margin must be finite and non-negative.");
        }

        GetCollisionPolygon(layerId, polygonIndex).OneWayMargin = margin;
    }

    /// <summary>
    /// Gets the one-way collision margin for a polygon.
    /// </summary>
    ///
    /// <param name="layerId">The collision layer metadata id.</param>
    /// <param name="polygonIndex">The polygon index inside the layer.</param>
    /// <returns>The stored one-way collision margin.</returns>
    ///
    /// <remarks>
    /// <para>
    /// New collision polygons use a margin of <c>1</c>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the layer or polygon index is invalid.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetCollisionPolygonOneWayMargin"/>
    public float GetCollisionPolygonOneWayMargin(int layerId, int polygonIndex)
    {
        ThrowIfFreed();
        return GetCollisionPolygon(layerId, polygonIndex).OneWayMargin;
    }

    internal IEnumerable<TileCollisionPolygon> EnumerateCollisionPolygons()
    {
        foreach (var layer in collisionLayers.OrderBy(static pair => pair.Key))
        {
            for (var index = 0; index < layer.Value.Count; index++)
            {
                var polygon = layer.Value[index];
                if (polygon.Points.Length >= 3)
                {
                    yield return new TileCollisionPolygon(layer.Key, index, polygon.Points, polygon.OneWay, polygon.OneWayMargin);
                }
            }
        }
    }

    private CollisionPolygon GetCollisionPolygon(int layerId, int polygonIndex)
    {
        ValidateLayerId(layerId);
        if (!collisionLayers.TryGetValue(layerId, out var polygons) ||
            polygonIndex < 0 ||
            polygonIndex >= polygons.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(polygonIndex), polygonIndex, "Collision polygon index is outside the layer range.");
        }

        return polygons[polygonIndex];
    }

    private List<CollisionPolygon> GetOrCreateCollisionLayer(int layerId)
    {
        if (!collisionLayers.TryGetValue(layerId, out var polygons))
        {
            polygons = new List<CollisionPolygon>();
            collisionLayers.Add(layerId, polygons);
        }

        return polygons;
    }

    private static void ValidateLayerId(int layerId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(layerId);
    }

    private sealed class CollisionPolygon
    {
        public Vector2[] Points { get; set; } = Array.Empty<Vector2>();

        public bool OneWay { get; set; }

        public float OneWayMargin { get; set; } = 1f;
    }
}

internal readonly record struct TileCollisionPolygon(
    int LayerId,
    int PolygonIndex,
    IReadOnlyList<Vector2> Points,
    bool OneWay,
    float OneWayMargin);
