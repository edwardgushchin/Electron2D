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
/// Provides a single 2D tilemap layer.
/// </summary>
///
/// <remarks>
/// <para>
/// `TileMapLayer` stores cell identifiers, submits visible atlas tiles to the
/// canvas render queue and exposes tile collision polygons to the managed 2D
/// physics query path.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate tilemap layers on the main
/// scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="TileSet"/>
/// <seealso cref="TileData"/>
public class TileMapLayer : Node2D, ISceneTreeLifecycleHandler
{
    private static readonly Vector2I InvalidAtlasCoords = new(-1, -1);

    private readonly Dictionary<Vector2I, TileMapCell> cells = new();
    private TileSet? tileSet;
    private bool enabled = true;
    private bool collisionEnabled = true;
    private int renderingQuadrantSize = 16;
    private int physicsQuadrantSize = 16;
    private bool xDrawOrderReversed;
    private int ySortOrigin;
    private Rid bodyRid;

    /// <summary>
    /// Initializes a new instance of the <see cref="TileMapLayer"/> type.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The layer starts enabled, collision-enabled and empty.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it from the thread that owns
    /// the node being created.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="TileSet"/>
    public TileMapLayer()
    {
    }

    /// <summary>
    /// Gets or sets the tile set used by this layer.
    /// </summary>
    ///
    /// <value>
    /// The tile set resource, or <c>null</c> when this layer has no tile
    /// definitions.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Cells remain stored when this property changes. Rendering and collision
    /// use the currently assigned tile set.
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
    /// <seealso cref="TileSet"/>
    public TileSet? TileSet
    {
        get
        {
            ThrowIfFreed();
            return tileSet;
        }
        set
        {
            ThrowIfFreed();
            tileSet = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets whether this layer is active.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> when rendering and collision are enabled for the layer;
    /// otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Disabling the layer leaves stored cells unchanged.
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
    /// <seealso cref="CollisionEnabled"/>
    public bool Enabled
    {
        get
        {
            ThrowIfFreed();
            return enabled;
        }
        set
        {
            ThrowIfFreed();
            enabled = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets whether tile collision polygons participate in physics.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> when tile collision polygons should be active; otherwise,
    /// <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Rendering is controlled separately by <see cref="Enabled"/> and
    /// <see cref="CanvasItem.Visible"/>.
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
    /// <seealso cref="Enabled"/>
    public bool CollisionEnabled
    {
        get
        {
            ThrowIfFreed();
            return collisionEnabled;
        }
        set
        {
            ThrowIfFreed();
            collisionEnabled = value;
        }
    }

    /// <summary>
    /// Gets or sets the rendering quadrant size.
    /// </summary>
    ///
    /// <value>
    /// The positive quadrant side length in map cells.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The preview stores this value for API state. Rendering commands are not
    /// chunk-merged yet.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is less than or equal to zero.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="PhysicsQuadrantSize"/>
    public int RenderingQuadrantSize
    {
        get
        {
            ThrowIfFreed();
            return renderingQuadrantSize;
        }
        set
        {
            ThrowIfFreed();
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
            renderingQuadrantSize = value;
        }
    }

    /// <summary>
    /// Gets or sets the physics quadrant size.
    /// </summary>
    ///
    /// <value>
    /// The positive quadrant side length in map cells.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The preview stores this value for API state. Physics shapes are not
    /// chunk-merged yet.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is less than or equal to zero.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="RenderingQuadrantSize"/>
    public int PhysicsQuadrantSize
    {
        get
        {
            ThrowIfFreed();
            return physicsQuadrantSize;
        }
        set
        {
            ThrowIfFreed();
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
            physicsQuadrantSize = value;
        }
    }

    /// <summary>
    /// Gets or sets whether tile draw order is reversed on the X axis.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to submit cells with higher X coordinates first inside each
    /// row; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The flag affects stable submission order when cells have the same
    /// z-index.
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
    /// <seealso cref="GetUsedCells"/>
    public bool XDrawOrderReversed
    {
        get
        {
            ThrowIfFreed();
            return xDrawOrderReversed;
        }
        set
        {
            ThrowIfFreed();
            xDrawOrderReversed = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets the Y-sort origin offset for submitted tiles.
    /// </summary>
    ///
    /// <value>
    /// The local Y offset used to derive tile y-sort positions.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The value is added to each cell's local Y position before it enters the
    /// canvas render queue.
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
    /// <seealso cref="CanvasItem.YSortEnabled"/>
    public int YSortOrigin
    {
        get
        {
            ThrowIfFreed();
            return ySortOrigin;
        }
        set
        {
            ThrowIfFreed();
            ySortOrigin = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Stores a tile cell at map coordinates.
    /// </summary>
    ///
    /// <param name="coords">The map coordinates of the cell.</param>
    /// <param name="sourceId">The tile set source id, or <c>-1</c> to erase the cell.</param>
    /// <param name="atlasCoords">The atlas coordinates inside the source.</param>
    /// <param name="alternativeTile">The alternative tile id.</param>
    ///
    /// <remarks>
    /// <para>
    /// Passing a negative source id or negative atlas coordinates erases the
    /// cell.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="alternativeTile"/> is negative for a stored cell.
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
    /// <seealso cref="EraseCell"/>
    public void SetCell(Vector2I coords, int sourceId = -1, Vector2I atlasCoords = default, int alternativeTile = 0)
    {
        ThrowIfFreed();
        if (sourceId < 0 || atlasCoords.X < 0 || atlasCoords.Y < 0)
        {
            EraseCell(coords);
            return;
        }

        ArgumentOutOfRangeException.ThrowIfNegative(alternativeTile);
        cells[coords] = new TileMapCell(sourceId, atlasCoords, alternativeTile);
        QueueRedraw();
    }

    /// <summary>
    /// Erases a tile cell at map coordinates.
    /// </summary>
    ///
    /// <param name="coords">The map coordinates of the cell to erase.</param>
    ///
    /// <remarks>
    /// <para>
    /// Erasing a missing cell does nothing.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetCell"/>
    public void EraseCell(Vector2I coords)
    {
        ThrowIfFreed();
        if (cells.Remove(coords))
        {
            QueueRedraw();
        }
    }

    /// <summary>
    /// Removes all cells from this layer.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// This method does not change <see cref="TileSet"/> or layer settings.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="EraseCell"/>
    public void Clear()
    {
        ThrowIfFreed();
        if (cells.Count == 0)
        {
            return;
        }

        cells.Clear();
        QueueRedraw();
    }

    /// <summary>
    /// Gets the source id stored in a cell.
    /// </summary>
    ///
    /// <param name="coords">The map coordinates of the cell.</param>
    /// <returns>The source id, or <c>-1</c> when the cell is empty.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The method reads stored cell data only; it does not validate whether the
    /// current <see cref="TileSet"/> still contains the source.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetCell"/>
    public int GetCellSourceId(Vector2I coords)
    {
        ThrowIfFreed();
        return cells.TryGetValue(coords, out var cell) ? cell.SourceId : -1;
    }

    /// <summary>
    /// Gets the atlas coordinates stored in a cell.
    /// </summary>
    ///
    /// <param name="coords">The map coordinates of the cell.</param>
    /// <returns>The atlas coordinates, or <c>Vector2I(-1, -1)</c> when the cell is empty.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The method reads stored cell data only; it does not validate whether the
    /// source contains the atlas tile.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetCellSourceId"/>
    public Vector2I GetCellAtlasCoords(Vector2I coords)
    {
        ThrowIfFreed();
        return cells.TryGetValue(coords, out var cell) ? cell.AtlasCoords : InvalidAtlasCoords;
    }

    /// <summary>
    /// Gets the alternative tile id stored in a cell.
    /// </summary>
    ///
    /// <param name="coords">The map coordinates of the cell.</param>
    /// <returns>The alternative tile id, or <c>-1</c> when the cell is empty.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The 0.1-preview stores the value but only default atlas alternative
    /// data is implemented by <see cref="TileSetAtlasSource"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetCell"/>
    public int GetCellAlternativeTile(Vector2I coords)
    {
        ThrowIfFreed();
        return cells.TryGetValue(coords, out var cell) ? cell.AlternativeTile : -1;
    }

    /// <summary>
    /// Gets the tile data referenced by a cell.
    /// </summary>
    ///
    /// <param name="coords">The map coordinates of the cell.</param>
    /// <returns>The tile data, or <c>null</c> when no tile data is available.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The method returns <c>null</c> for empty cells, missing tile sets, missing
    /// sources and non-atlas sources.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="TileData"/>
    public TileData? GetCellTileData(Vector2I coords)
    {
        ThrowIfFreed();
        return TryResolveCell(coords, out _, out _, out var tileData, out _, out _) ? tileData : null;
    }

    /// <summary>
    /// Gets all used cell coordinates.
    /// </summary>
    ///
    /// <returns>A new array containing used cell coordinates in stable order.</returns>
    ///
    /// <remarks>
    /// <para>
    /// Coordinates are ordered by Y coordinate and then X coordinate.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetUsedCellsById"/>
    public Vector2I[] GetUsedCells()
    {
        ThrowIfFreed();
        return SortCells(cells.Keys);
    }

    /// <summary>
    /// Gets used cell coordinates filtered by stored tile identifiers.
    /// </summary>
    ///
    /// <param name="sourceId">The source id to match, or <c>-1</c> to ignore source id.</param>
    /// <param name="atlasCoords">The atlas coordinates to match, or <c>null</c> to ignore atlas coordinates.</param>
    /// <param name="alternativeTile">The alternative tile id to match, or <c>-1</c> to ignore alternative tile id.</param>
    /// <returns>A new array containing matching used cell coordinates.</returns>
    ///
    /// <remarks>
    /// <para>
    /// Filtering uses stored cell identifiers and does not require the tile set
    /// source to exist.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetUsedCells"/>
    public Vector2I[] GetUsedCellsById(int sourceId = -1, Vector2I? atlasCoords = null, int alternativeTile = -1)
    {
        ThrowIfFreed();
        return SortCells(cells
            .Where(pair =>
                (sourceId < 0 || pair.Value.SourceId == sourceId) &&
                (atlasCoords is null || pair.Value.AtlasCoords == atlasCoords.Value) &&
                (alternativeTile < 0 || pair.Value.AlternativeTile == alternativeTile))
            .Select(static pair => pair.Key));
    }

    /// <summary>
    /// Gets the rectangle enclosing all used cells.
    /// </summary>
    ///
    /// <returns>The used cell rectangle, or an empty rectangle when no cells are used.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The rectangle is expressed in map coordinates.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetUsedCells"/>
    public Rect2I GetUsedRect()
    {
        ThrowIfFreed();
        if (cells.Count == 0)
        {
            return new Rect2I();
        }

        var used = cells.Keys.ToArray();
        var min = used[0];
        var max = used[0];
        for (var index = 1; index < used.Length; index++)
        {
            min = min.Min(used[index]);
            max = max.Max(used[index]);
        }

        return new Rect2I(min, max - min + Vector2I.One);
    }

    /// <summary>
    /// Converts a local position to map coordinates.
    /// </summary>
    ///
    /// <param name="localPosition">The local position in this layer.</param>
    /// <returns>The map coordinates containing the position.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The conversion uses floor division, so negative positions map to
    /// negative cells.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="MapToLocal"/>
    public Vector2I LocalToMap(Vector2 localPosition)
    {
        ThrowIfFreed();
        var size = GetTileSize();
        return new Vector2I(
            (int)MathF.Floor(localPosition.X / size.X),
            (int)MathF.Floor(localPosition.Y / size.Y));
    }

    /// <summary>
    /// Converts map coordinates to a local cell-center position.
    /// </summary>
    ///
    /// <param name="mapPosition">The map coordinates to convert.</param>
    /// <returns>The local position at the center of the cell.</returns>
    ///
    /// <remarks>
    /// <para>
    /// Rendering destination rectangles still start at the cell's top-left
    /// local position.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="LocalToMap"/>
    public Vector2 MapToLocal(Vector2I mapPosition)
    {
        ThrowIfFreed();
        var size = GetTileSize();
        return new Vector2(
            (mapPosition.X * size.X) + (size.X / 2f),
            (mapPosition.Y * size.Y) + (size.Y / 2f));
    }

    /// <summary>
    /// Checks whether a body RID belongs to this tilemap layer.
    /// </summary>
    ///
    /// <param name="body">The body RID to check.</param>
    /// <returns><c>true</c> when the RID is this layer's internal body RID.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The RID is created only while the layer is inside a <see cref="SceneTree"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetCoordsForBodyRid"/>
    public bool HasBodyRid(Rid body)
    {
        ThrowIfFreed();
        return bodyRid.IsValid() && bodyRid == body;
    }

    /// <summary>
    /// Gets cell coordinates for a body RID when they are unambiguous.
    /// </summary>
    ///
    /// <param name="body">The body RID to query.</param>
    /// <returns>The single used cell coordinates, or <c>Vector2I(-1, -1)</c> when unavailable.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The preview stores one internal body RID for the whole layer. The method
    /// returns cell coordinates only when exactly one cell is used.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="HasBodyRid"/>
    public Vector2I GetCoordsForBodyRid(Rid body)
    {
        ThrowIfFreed();
        return HasBodyRid(body) && cells.Count == 1 ? cells.Keys.Single() : InvalidAtlasCoords;
    }

    /// <summary>
    /// Applies pending tilemap updates immediately.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The preview updates cell data immediately, so this method queues a redraw
    /// and returns.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="NotifyRuntimeTileDataUpdate"/>
    public void UpdateInternals()
    {
        ThrowIfFreed();
        QueueRedraw();
    }

    /// <summary>
    /// Notifies the layer that runtime tile data has changed.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The preview does not defer tile data caches, so this method queues a
    /// redraw and returns.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="UpdateInternals"/>
    public void NotifyRuntimeTileDataUpdate()
    {
        ThrowIfFreed();
        QueueRedraw();
    }

    internal Rid PhysicsBodyRid => bodyRid;

    internal IEnumerable<TileRenderCell> EnumerateRenderableCells()
    {
        if (!enabled || tileSet is null)
        {
            yield break;
        }

        foreach (var pair in SortCellPairsForRendering())
        {
            if (TryResolveCell(pair.Key, out var cell, out var source, out var tileData, out var texture, out var sourceRect))
            {
                var destination = GetCellDestination(pair.Key, tileData);
                yield return new TileRenderCell(
                    pair.Key,
                    cell,
                    source,
                    tileData,
                    texture,
                    sourceRect,
                    destination,
                    GetTileYSortPosition(pair.Key));
            }
        }
    }

    internal IEnumerable<PhysicsQueryShape> CollectActiveTileShapeBounds()
    {
        if (!enabled || !collisionEnabled || tileSet is null || !bodyRid.IsValid())
        {
            yield break;
        }

        var shapeIndex = 0;
        foreach (var pair in SortCellPairs())
        {
            if (!TryResolveCell(pair.Key, out _, out _, out var tileData, out _, out _))
            {
                continue;
            }

            foreach (var polygon in tileData.EnumerateCollisionPolygons())
            {
                if (TryGetPolygonBounds(pair.Key, polygon, out var bounds))
                {
                    yield return PhysicsQueryShape.CreateTile(
                        this,
                        bodyRid,
                        shapeIndex,
                        bounds,
                        tileData,
                        polygon.OneWay,
                        polygon.OneWayMargin);
                    shapeIndex++;
                }
            }
        }
    }

    void ISceneTreeLifecycleHandler.OnEnterTree()
    {
        if (!bodyRid.IsValid())
        {
            bodyRid = PhysicsServer2D.BodyCreate(PhysicsBodyKind.Static);
        }
    }

    void ISceneTreeLifecycleHandler.OnPhysicsProcess(double delta)
    {
        _ = delta;
        if (bodyRid.IsValid())
        {
            PhysicsServer2D.CollisionObjectSetTransform(bodyRid, GlobalTransform);
            PhysicsServer2D.CollisionObjectSetCollisionFilter(bodyRid, new PhysicsCollisionFilter(1u, 1u));
            PhysicsServer2D.BodySetState(bodyRid, new PhysicsBody2DState(MaterialOverride: null, RigidBody: null));
        }
    }

    void ISceneTreeLifecycleHandler.OnExitTree()
    {
        FreePhysicsRid();
    }

    protected override void OnFree()
    {
        FreePhysicsRid();
        base.OnFree();
    }

    private bool TryResolveCell(
        Vector2I coords,
        out TileMapCell cell,
        out TileSetAtlasSource source,
        out TileData tileData,
        out Texture2D texture,
        out Rect2 sourceRect)
    {
        cell = default;
        source = null!;
        tileData = null!;
        texture = null!;
        sourceRect = default;

        if (tileSet is null || !cells.TryGetValue(coords, out cell))
        {
            return false;
        }

        if (tileSet.GetSource(cell.SourceId) is not TileSetAtlasSource atlasSource ||
            atlasSource.Texture is not { } atlasTexture ||
            atlasSource.GetTileData(cell.AtlasCoords, cell.AlternativeTile) is not { } data ||
            !atlasSource.TryGetTileTextureRegion(cell.AtlasCoords, cell.AlternativeTile, out sourceRect))
        {
            return false;
        }

        source = atlasSource;
        tileData = data;
        texture = atlasTexture;
        return true;
    }

    private Rect2 GetCellDestination(Vector2I coords, TileData tileData)
    {
        var size = GetTileSize();
        return new Rect2(
            new Vector2(coords.X * size.X, coords.Y * size.Y) + tileData.TextureOrigin,
            size);
    }

    private float GetTileYSortPosition(Vector2I coords)
    {
        var size = GetTileSize();
        return GlobalTransform.Xform(new Vector2(coords.X * size.X, (coords.Y * size.Y) + ySortOrigin)).Y;
    }

    private bool TryGetPolygonBounds(Vector2I coords, TileCollisionPolygon polygon, out Rect2 bounds)
    {
        bounds = default;
        if (polygon.Points.Count == 0)
        {
            return false;
        }

        var size = GetTileSize();
        var cellOrigin = new Vector2(coords.X * size.X, coords.Y * size.Y);
        var min = cellOrigin + polygon.Points[0];
        var max = min;
        for (var index = 1; index < polygon.Points.Count; index++)
        {
            var point = cellOrigin + polygon.Points[index];
            min = min.Min(point);
            max = max.Max(point);
        }

        var localBounds = new Rect2(min, max - min).Abs();
        bounds = GlobalTransform * localBounds;
        return bounds.HasArea();
    }

    private Vector2 GetTileSize()
    {
        return tileSet?.GetTileSizeVector() ?? new Vector2(16f, 16f);
    }

    private IEnumerable<KeyValuePair<Vector2I, TileMapCell>> SortCellPairsForRendering()
    {
        var ordered = SortCellPairs();
        return xDrawOrderReversed
            ? ordered.OrderBy(static pair => pair.Key.Y).ThenByDescending(static pair => pair.Key.X)
            : ordered;
    }

    private IEnumerable<KeyValuePair<Vector2I, TileMapCell>> SortCellPairs()
    {
        return cells.OrderBy(static pair => pair.Key.Y).ThenBy(static pair => pair.Key.X);
    }

    private static Vector2I[] SortCells(IEnumerable<Vector2I> coords)
    {
        return coords.OrderBy(static coord => coord.Y).ThenBy(static coord => coord.X).ToArray();
    }

    private void FreePhysicsRid()
    {
        if (!bodyRid.IsValid())
        {
            return;
        }

        var ridToFree = bodyRid;
        bodyRid = default;
        PhysicsServer2D.FreeRid(ridToFree);
    }

    internal readonly record struct TileMapCell(
        int SourceId,
        Vector2I AtlasCoords,
        int AlternativeTile);
}

internal readonly record struct TileRenderCell(
    Vector2I Coords,
    TileMapLayer.TileMapCell Cell,
    TileSetAtlasSource Source,
    TileData Data,
    Texture2D Texture,
    Rect2 SourceRect,
    Rect2 DestinationRect,
    float YSortPosition);
