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
/// Provides an Electron2D texture that draws a region of another <see cref="Texture2D" />.
/// </summary>
///
/// <remarks>
/// Multiple atlas textures can reference the same atlas resource. This is useful
/// for packing many small sprites into one larger texture while preserving a
/// separate resource object for each region.
/// </remarks>
///
/// <threadsafety>
/// Read-only queries are safe to call from any thread when the referenced atlas
/// texture is immutable and no thread is mutating this resource.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Texture2D" />
public sealed class AtlasTexture : Texture2D
{

    /// <summary>
    /// Initializes a new instance of the AtlasTexture type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="AtlasTexture" />
    ///
    public AtlasTexture()
    {
    }

    /// <summary>
    /// Gets or sets the texture that contains this atlas region.
    /// </summary>
    ///
    /// <remarks>
    /// The atlas can be any <see cref="Texture2D" />, including another
    /// <see cref="AtlasTexture" />. When this property is <c>null</c>, this
    /// resource reports an empty texture.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it during loading or on the main thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <value>
    /// The current atlas value.
    /// </value>
    ///
    /// <seealso cref="AtlasTexture" />
    ///
    public Texture2D? Atlas { get; set; }

    /// <summary>
    /// Gets or sets whether sampling outside the region is clipped to reduce bleeding.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it during loading or on the main thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current filter clip value.
    /// </value>
    ///
    /// <seealso cref="AtlasTexture" />
    ///
    public bool FilterClip { get; set; }

    /// <summary>
    /// Gets or sets the margin around the atlas region.
    /// </summary>
    ///
    /// <remarks>
    /// A non-zero margin can be used by future drawing code to adjust the
    /// placement of the region without changing the sampled atlas rectangle.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it during loading or on the main thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <value>
    /// The current margin value.
    /// </value>
    ///
    /// <seealso cref="AtlasTexture" />
    ///
    public Rect2 Margin { get; set; }

    /// <summary>
    /// Gets or sets the rectangle in the atlas texture that this resource draws.
    /// </summary>
    ///
    /// <remarks>
    /// If a region width or height is <c>0</c>, the corresponding size from the
    /// atlas texture is used for that axis.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it during loading or on the main thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <value>
    /// The current region value.
    /// </value>
    ///
    /// <seealso cref="AtlasTexture" />
    ///
    public Rect2 Region { get; set; }

    /// <summary>
    /// Gets the width value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current width value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="AtlasTexture" />
    ///
    public override int GetWidth()
    {
        return Atlas is null ? 0 : ToPixelSize(Region.Size.X, Atlas.GetWidth());
    }

    /// <summary>
    /// Gets the height value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current height value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="AtlasTexture" />
    ///
    public override int GetHeight()
    {
        return Atlas is null ? 0 : ToPixelSize(Region.Size.Y, Atlas.GetHeight());
    }

    /// <summary>
    /// Checks whether alpha is available.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// <c>true</c> when the condition is met; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="AtlasTexture" />
    ///
    public override bool HasAlpha()
    {
        return Atlas?.HasAlpha() == true;
    }

    /// <summary>
    /// Checks whether mipmaps is available.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// <c>true</c> when the condition is met; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="AtlasTexture" />
    ///
    public override bool HasMipmaps()
    {
        return Atlas?.HasMipmaps() == true;
    }

    /// <summary>
    /// Gets the mipmap count value.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <returns>
    /// The current mipmap count value.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="AtlasTexture" />
    ///
    public override int GetMipmapCount()
    {
        return Atlas?.GetMipmapCount() ?? 0;
    }

    /// <summary>
    /// Checks whether pixel opaque is true.
    /// </summary>
    ///
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <param name="x">
    /// The X coordinate or component.
    /// </param>
    ///
    /// <param name="y">
    /// The Y coordinate or component.
    /// </param>
    ///
    /// <returns>
    /// <c>true</c> when the condition is met; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="AtlasTexture" />
    ///
    public override bool IsPixelOpaque(int x, int y)
    {
        if (Atlas is null || x < 0 || y < 0 || x >= GetWidth() || y >= GetHeight())
        {
            return false;
        }

        var sourceX = (int)MathF.Floor(Region.Position.X) + x;
        var sourceY = (int)MathF.Floor(Region.Position.Y) + y;
        if (sourceX < 0 || sourceY < 0 || sourceX >= Atlas.GetWidth() || sourceY >= Atlas.GetHeight())
        {
            return false;
        }

        return Atlas.IsPixelOpaque(sourceX, sourceY);
    }

    internal Rect2 GetSourceRegion()
    {
        return new Rect2(Region.Position, new Vector2(GetWidth(), GetHeight()));
    }

    internal override long RenderContentVersion
    {
        get
        {
            var hash = new HashCode();
            hash.Add(Atlas?.RenderContentVersion ?? 0);
            hash.Add(FilterClip);
            AddRect(ref hash, Margin);
            AddRect(ref hash, Region);
            return hash.ToHashCode();
        }
    }

    private static void AddRect(ref HashCode hash, Rect2 rect)
    {
        hash.Add(rect.Position.X);
        hash.Add(rect.Position.Y);
        hash.Add(rect.Size.X);
        hash.Add(rect.Size.Y);
    }

    private static int ToPixelSize(float value, int fallback)
    {
        return value > 0f ? Math.Max(0, (int)MathF.Floor(value)) : fallback;
    }
}
