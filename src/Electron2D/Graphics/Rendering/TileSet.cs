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
/// Stores tile sources and shared tilemap settings.
/// </summary>
///
/// <remarks>
/// <para>
/// `TileSet` is a resource assigned to <see cref="TileMapLayer"/>. The 0.1-preview
/// Preview supports atlas sources and a single rectangular cell size.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate tile sets on the main
/// scene thread or during loading.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="TileMapLayer"/>
/// <seealso cref="TileSetAtlasSource"/>
public sealed class TileSet : Resource
{
    private readonly Dictionary<int, TileSetSource> sources = new();
    private Vector2I tileSize = new(16, 16);

    /// <summary>
    /// Initializes a new instance of the <see cref="TileSet"/> type.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The tile set starts empty and uses a <c>16x16</c> cell size.
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
    /// <seealso cref="AddSource"/>
    public TileSet()
    {
    }

    /// <summary>
    /// Gets or sets the size of a tilemap cell.
    /// </summary>
    ///
    /// <value>
    /// The positive width and height used by <see cref="TileMapLayer"/> cells.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Rendering destination rectangles and tile collision placement use this
    /// size.
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="TileMapLayer.MapToLocal"/>
    public Vector2I TileSize
    {
        get
        {
            ThrowIfFreed();
            return tileSize;
        }
        set
        {
            ThrowIfFreed();
            ValidatePositiveSize(value, nameof(TileSize));
            tileSize = value;
        }
    }

    /// <summary>
    /// Adds a source to this tile set.
    /// </summary>
    ///
    /// <param name="source">The source resource to add.</param>
    /// <param name="atlasSourceIdOverride">The requested source id, or <c>-1</c> to allocate one.</param>
    /// <returns>The source id assigned to <paramref name="source"/>.</returns>
    ///
    /// <remarks>
    /// <para>
    /// Automatically allocated ids use the lowest non-negative id that is not
    /// currently assigned.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="atlasSourceIdOverride"/> is less than <c>-1</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the requested source id is already used.
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
    /// <seealso cref="GetSource"/>
    public int AddSource(TileSetSource source, int atlasSourceIdOverride = -1)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(source);
        if (atlasSourceIdOverride < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(atlasSourceIdOverride), atlasSourceIdOverride, "Source id must be -1 or greater.");
        }

        var sourceId = atlasSourceIdOverride == -1 ? AllocateSourceId() : atlasSourceIdOverride;
        if (sources.ContainsKey(sourceId))
        {
            throw new InvalidOperationException($"Tile source id '{sourceId}' is already used.");
        }

        sources.Add(sourceId, source);
        return sourceId;
    }

    /// <summary>
    /// Removes a source from this tile set.
    /// </summary>
    ///
    /// <param name="sourceId">The source id to remove.</param>
    ///
    /// <remarks>
    /// <para>
    /// Removing a missing source does nothing. Existing tilemap cells that point
    /// to the source remain stored but will not render or collide.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="sourceId"/> is negative.
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
    /// <seealso cref="HasSource"/>
    public void RemoveSource(int sourceId)
    {
        ThrowIfFreed();
        ArgumentOutOfRangeException.ThrowIfNegative(sourceId);
        sources.Remove(sourceId);
    }

    /// <summary>
    /// Gets a source by id.
    /// </summary>
    ///
    /// <param name="sourceId">The source id to query.</param>
    /// <returns>The source resource, or <c>null</c> when no source has that id.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The returned source is the same resource instance that was passed to
    /// <see cref="AddSource"/>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="sourceId"/> is negative.
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
    /// <seealso cref="AddSource"/>
    public TileSetSource? GetSource(int sourceId)
    {
        ThrowIfFreed();
        ArgumentOutOfRangeException.ThrowIfNegative(sourceId);
        return sources.TryGetValue(sourceId, out var source) ? source : null;
    }

    /// <summary>
    /// Checks whether a source id exists.
    /// </summary>
    ///
    /// <param name="sourceId">The source id to query.</param>
    /// <returns><c>true</c> when the source exists; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>
    /// <para>
    /// This method does not validate the source resource contents.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="sourceId"/> is negative.
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
    /// <seealso cref="GetSource"/>
    public bool HasSource(int sourceId)
    {
        ThrowIfFreed();
        ArgumentOutOfRangeException.ThrowIfNegative(sourceId);
        return sources.ContainsKey(sourceId);
    }

    /// <summary>
    /// Gets the number of sources stored by this tile set.
    /// </summary>
    ///
    /// <returns>The number of assigned sources.</returns>
    ///
    /// <remarks>
    /// <para>
    /// The count includes sources of every implemented source type.
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
    /// <seealso cref="GetSourceId"/>
    public int GetSourceCount()
    {
        ThrowIfFreed();
        return sources.Count;
    }

    /// <summary>
    /// Gets a source id by index.
    /// </summary>
    ///
    /// <param name="index">The zero-based index in sorted source id order.</param>
    /// <returns>The source id at the requested index.</returns>
    ///
    /// <remarks>
    /// <para>
    /// Source ids are returned in ascending numeric order.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the stored source range.
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
    /// <seealso cref="GetSourceCount"/>
    public int GetSourceId(int index)
    {
        ThrowIfFreed();
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        var ids = sources.Keys.Order().ToArray();
        if (index >= ids.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Source index is outside the stored source range.");
        }

        return ids[index];
    }

    /// <summary>
    /// Removes all sources from this tile set.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// This method keeps <see cref="TileSize"/> unchanged.
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
    /// <seealso cref="RemoveSource"/>
    public void Clear()
    {
        ThrowIfFreed();
        sources.Clear();
    }

    internal Vector2 GetTileSizeVector()
    {
        return new Vector2(tileSize.X, tileSize.Y);
    }

    internal IEnumerable<KeyValuePair<int, TileSetSource>> EnumerateSources()
    {
        foreach (var pair in sources.OrderBy(static pair => pair.Key))
        {
            yield return pair;
        }
    }

    private int AllocateSourceId()
    {
        var sourceId = 0;
        while (sources.ContainsKey(sourceId))
        {
            sourceId++;
        }

        return sourceId;
    }

    private static void ValidatePositiveSize(Vector2I size, string parameterName)
    {
        if (size.X <= 0 || size.Y <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, size, "Size components must be greater than zero.");
        }
    }
}
