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
/// Provides an Electron2D dynamic texture for the contents of a <see cref="Viewport" />.
/// </summary>
///
/// <remarks>
/// <para>
/// A viewport texture is created by <see cref="Viewport.GetTexture" /> and is
/// local to the scene that owns the viewport. It reports the viewport's current
/// size when queried.
/// </para>
///
/// <para>
/// Electron2D 0.1.0 Preview does not expose public GPU texture handles or image
/// readback through this type. Pixel opacity queries therefore return
/// <c>false</c>.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Query and mutate the owning viewport on the
/// main scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Viewport" />
/// <seealso cref="Texture2D" />
public sealed class ViewportTexture : Texture2D
{
    private readonly Viewport viewport;

    internal ViewportTexture(Viewport viewport)
    {
        ArgumentNullException.ThrowIfNull(viewport);

        this.viewport = viewport;
        ResourceLocalToScene = true;
    }

    /// <summary>
    /// Gets the current viewport width in pixels.
    /// </summary>
    ///
    /// <returns>The owning viewport width in pixels, clamped to <c>0</c> for invalid negative sizes.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public override int GetWidth()
    {
        return Math.Max(0, viewport.Size.X);
    }

    /// <summary>
    /// Gets the current viewport height in pixels.
    /// </summary>
    ///
    /// <returns>The owning viewport height in pixels, clamped to <c>0</c> for invalid negative sizes.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public override int GetHeight()
    {
        return Math.Max(0, viewport.Size.Y);
    }

    /// <summary>
    /// Checks whether the viewport texture has an alpha channel.
    /// </summary>
    ///
    /// <returns><c>true</c>, because Electron2D viewport textures are treated as alpha-capable render targets.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public override bool HasAlpha()
    {
        return true;
    }

    /// <summary>
    /// Checks whether a viewport texture pixel is fully opaque.
    /// </summary>
    ///
    /// <param name="x">The pixel X coordinate in viewport texture space.</param>
    /// <param name="y">The pixel Y coordinate in viewport texture space.</param>
    /// <returns><c>false</c> until public image readback is available.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public override bool IsPixelOpaque(int x, int y)
    {
        return false;
    }
}
