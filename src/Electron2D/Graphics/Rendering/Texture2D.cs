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
/// Provides the Electron2D base resource for 2D textures.
/// </summary>
///
/// <remarks>
/// <para>
/// `Texture2D` describes texture metadata and transparency queries. Concrete
/// texture loaders and image-backed texture classes are intentionally separate
/// from this base type.
/// </para>
///
/// <para>
/// Electron2D 0.1.0 Preview does not expose image import through this type.
/// PNG/JPEG import and concrete image texture creation are handled by later
/// resource pipeline tasks.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// Read-only metadata queries are safe to call from any thread when the
/// concrete texture implementation is immutable.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="AtlasTexture" />
public abstract class Texture2D : Resource
{

    /// <summary>
    /// Initializes a new instance of the Texture2D type.
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
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Texture2D" />
    ///
    public Texture2D()
    {
    }

    /// <summary>
    /// Gets the texture width in pixels.
    /// </summary>
    ///
    /// <returns>The texture width in pixels, or <c>0</c> for an empty texture.</returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when the concrete texture is immutable.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Texture2D" />
    ///
    public abstract int GetWidth();

    /// <summary>
    /// Gets the texture height in pixels.
    /// </summary>
    ///
    /// <returns>The texture height in pixels, or <c>0</c> for an empty texture.</returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when the concrete texture is immutable.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Texture2D" />
    ///
    public abstract int GetHeight();

    /// <summary>
    /// Gets the texture size in pixels.
    /// </summary>
    ///
    /// <returns>A vector whose X component is the width and Y component is the height.</returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when the concrete texture is immutable.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetWidth" />
    /// <seealso cref="GetHeight" />
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public Vector2 GetSize()
    {
        return new Vector2(GetWidth(), GetHeight());
    }

    /// <summary>
    /// Checks whether the texture contains an alpha channel.
    /// </summary>
    ///
    /// <returns><c>true</c> if the texture has an alpha channel; otherwise, <c>false</c>.</returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when the concrete texture is immutable.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Texture2D" />
    ///
    public abstract bool HasAlpha();

    /// <summary>
    /// Checks whether the texture contains mipmaps.
    /// </summary>
    ///
    /// <returns><c>true</c> if the texture has mipmaps; otherwise, <c>false</c>.</returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when the concrete texture is immutable.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetMipmapCount" />
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public virtual bool HasMipmaps()
    {
        return false;
    }

    /// <summary>
    /// Gets the number of mipmap levels available on this texture.
    /// </summary>
    ///
    /// <returns>The number of mipmap levels, or <c>0</c> when no mipmaps are available.</returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when the concrete texture is immutable.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="HasMipmaps" />
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public virtual int GetMipmapCount()
    {
        return 0;
    }

    /// <summary>
    /// Checks whether a texture pixel is fully opaque.
    /// </summary>
    ///
    /// <param name="x">The pixel X coordinate in texture space.</param>
    /// <param name="y">The pixel Y coordinate in texture space.</param>
    ///
    /// <returns><c>true</c> if the pixel is inside the texture and fully opaque; otherwise, <c>false</c>.</returns>
    ///
    /// <remarks>
    /// The base implementation treats textures without alpha as opaque inside
    /// their bounds and treats alpha textures as not opaque unless a concrete
    /// implementation overrides the query.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when the concrete texture is immutable.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="HasAlpha" />
    public virtual bool IsPixelOpaque(int x, int y)
    {
        return x >= 0 &&
            y >= 0 &&
            x < GetWidth() &&
            y < GetHeight() &&
            !HasAlpha();
    }
}
