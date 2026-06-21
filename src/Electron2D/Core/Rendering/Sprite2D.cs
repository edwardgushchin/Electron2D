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
/// Provides a Godot-like node that submits a 2D texture for drawing.
/// </summary>
///
/// <remarks>
/// `Sprite2D` displays the assigned <see cref="Texture2D" /> or a rectangular
/// region of that texture. Sprite sheet frame animation is outside the
/// Electron2D 0.1.0 Preview subset.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate sprites on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Texture2D" />
/// <seealso cref="Node2D" />
public class Sprite2D : Node2D
{
    /// <summary>
    /// Gets or sets whether the texture is drawn centered on the node origin.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool Centered { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the texture is flipped horizontally.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool FlipH { get; set; }

    /// <summary>
    /// Gets or sets whether the texture is flipped vertically.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool FlipV { get; set; }

    /// <summary>
    /// Gets or sets the local drawing offset.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Vector2 Offset { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets whether <see cref="RegionRect" /> limits the sampled texture region.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RegionRect" />
    public bool RegionEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether filtering should clip to the enabled region.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool RegionFilterClipEnabled { get; set; }

    /// <summary>
    /// Gets or sets the source rectangle used when <see cref="RegionEnabled" /> is <c>true</c>.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Rect2 RegionRect { get; set; }

    /// <summary>
    /// Gets or sets the texture displayed by this sprite.
    /// </summary>
    ///
    /// <remarks>
    /// If this property is <c>null</c>, the sprite remains part of the scene
    /// tree but does not submit a draw command.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Texture2D? Texture { get; set; }

    /// <summary>
    /// Gets the local rectangle occupied by this sprite.
    /// </summary>
    ///
    /// <returns>The local drawing rectangle derived from the texture size, region and offset.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Rect2 GetRect()
    {
        ThrowIfFreed();
        var size = GetDrawSize();
        var position = Centered ? Offset - (size / 2f) : Offset;
        return new Rect2(position, size);
    }

    /// <summary>
    /// Checks whether a local sprite position maps to an opaque texture pixel.
    /// </summary>
    ///
    /// <param name="pos">The position in the sprite's local drawing coordinates.</param>
    /// <returns><c>true</c> if the mapped texture pixel is opaque; otherwise, <c>false</c>.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool IsPixelOpaque(Vector2 pos)
    {
        ThrowIfFreed();
        if (Texture is null)
        {
            return false;
        }

        var rect = GetRect();
        if (!rect.HasPoint(pos))
        {
            return false;
        }

        var local = pos - rect.Position;
        var source = GetSourceRect();
        var x = (int)MathF.Floor(source.Position.X + local.X);
        var y = (int)MathF.Floor(source.Position.Y + local.Y);
        return Texture.IsPixelOpaque(x, y);
    }

    internal Rect2 GetSourceRect()
    {
        if (Texture is null)
        {
            return new Rect2();
        }

        if (RegionEnabled)
        {
            return RegionRect;
        }

        return new Rect2(0f, 0f, Texture.GetWidth(), Texture.GetHeight());
    }

    private Vector2 GetDrawSize()
    {
        if (Texture is null)
        {
            return Vector2.Zero;
        }

        var source = GetSourceRect();
        return new Vector2(MathF.Max(0f, source.Size.X), MathF.Max(0f, source.Size.Y));
    }
}
