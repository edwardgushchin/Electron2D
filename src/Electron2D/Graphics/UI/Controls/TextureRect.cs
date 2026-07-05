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
/// Provides a control that draws a texture inside its rectangle.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>TextureRect</c> stores a <see cref="Texture2D"/> and submits texture draw
/// commands according to <see cref="StretchMode"/> and <see cref="ExpandMode"/>.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate texture rectangles on the
/// main scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Texture2D"/>
/// <seealso cref="Control"/>
public class TextureRect : Control
{
    private Texture2D? texture;
    private ExpandModeEnum expandMode;
    private StretchModeEnum stretchMode;
    private bool flipH;
    private bool flipV;

    /// <summary>
    /// Identifies how texture size contributes to minimum size.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Expand mode affects <see cref="_GetMinimumSize"/>. Drawing is controlled
    /// by <see cref="StretchMode"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This enum is immutable and is safe to use from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="TextureRect.ExpandMode"/>
    public enum ExpandModeEnum
    {
        /// <summary>Keeps the texture size as minimum size.</summary>
        /// <remarks>Use this value with <see cref="TextureRect.ExpandMode"/>.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="ExpandModeEnum"/>
        KeepSize = 0,

        /// <summary>Ignores texture size for minimum size.</summary>
        /// <remarks>Use this value when layout should fully control size.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="ExpandModeEnum"/>
        IgnoreSize = 1,

        /// <summary>Uses texture width as minimum width.</summary>
        /// <remarks>The minimum height is zero.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="ExpandModeEnum"/>
        FitWidth = 2,

        /// <summary>Uses the full texture size while fitting by width.</summary>
        /// <remarks>Use this value to preserve proportional minimum size.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="ExpandModeEnum"/>
        FitWidthProportional = 3,

        /// <summary>Uses texture height as minimum height.</summary>
        /// <remarks>The minimum width is zero.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="ExpandModeEnum"/>
        FitHeight = 4,

        /// <summary>Uses the full texture size while fitting by height.</summary>
        /// <remarks>Use this value to preserve proportional minimum size.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="ExpandModeEnum"/>
        FitHeightProportional = 5
    }

    /// <summary>
    /// Identifies how a texture is drawn inside the control rectangle.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Stretch mode affects draw command destination rectangles. Tiling modes
    /// use a single texture draw command in the preview renderer path.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This enum is immutable and is safe to use from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="TextureRect.StretchMode"/>
    public enum StretchModeEnum
    {
        /// <summary>Scales the texture to the control rectangle.</summary>
        /// <remarks>Use this value with <see cref="TextureRect.StretchMode"/>.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="StretchModeEnum"/>
        Scale = 0,

        /// <summary>Draws the texture over the control rectangle.</summary>
        /// <remarks>Use this value for texture-backed UI surfaces.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="StretchModeEnum"/>
        Tile = 1,

        /// <summary>Keeps the original texture size at the top-left corner.</summary>
        /// <remarks>Use this value to avoid scaling.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="StretchModeEnum"/>
        Keep = 2,

        /// <summary>Keeps the original texture size and centers it.</summary>
        /// <remarks>Use this value for fixed-size centered images.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="StretchModeEnum"/>
        KeepCentered = 3,

        /// <summary>Fits the texture inside the control while preserving aspect ratio.</summary>
        /// <remarks>Use this value when the full texture should remain visible.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="StretchModeEnum"/>
        KeepAspect = 4,

        /// <summary>Fits the texture inside the control and centers it.</summary>
        /// <remarks>Use this value for centered aspect-ratio drawing.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="StretchModeEnum"/>
        KeepAspectCentered = 5,

        /// <summary>Covers the control while preserving aspect ratio.</summary>
        /// <remarks>The destination rectangle can extend outside the control area.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="StretchModeEnum"/>
        KeepAspectCovered = 6
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextureRect"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// The new control ignores mouse input by passing it through to controls
    /// below it.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="TextureRect"/>
    public TextureRect()
    {
        MouseFilter = MouseFilter.Pass;
    }

    /// <summary>
    /// Gets or sets the texture drawn by this control.
    /// </summary>
    /// <value>The texture resource, or <c>null</c> to draw nothing.</value>
    /// <remarks>Assigning this property queues a redraw.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Texture2D"/>
    public Texture2D? Texture
    {
        get => texture;
        set
        {
            texture = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets how texture size affects minimum size.
    /// </summary>
    /// <value>The expand mode used by <see cref="_GetMinimumSize"/>.</value>
    /// <remarks>The default is <see cref="ExpandModeEnum.KeepSize"/>.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="ExpandModeEnum"/>
    public ExpandModeEnum ExpandMode
    {
        get => expandMode;
        set
        {
            expandMode = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets how the texture is drawn.
    /// </summary>
    /// <value>The stretch mode used by <see cref="_Draw"/>.</value>
    /// <remarks>The default is <see cref="StretchModeEnum.Scale"/>.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="StretchModeEnum"/>
    public StretchModeEnum StretchMode
    {
        get => stretchMode;
        set
        {
            stretchMode = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets whether the texture is flipped horizontally.
    /// </summary>
    /// <value><c>true</c> to request horizontal flip; otherwise, <c>false</c>.</value>
    /// <remarks>The flip flag is stored in the internal texture draw command.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="FlipV"/>
    public bool FlipH
    {
        get => flipH;
        set
        {
            flipH = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets whether the texture is flipped vertically.
    /// </summary>
    /// <value><c>true</c> to request vertical flip; otherwise, <c>false</c>.</value>
    /// <remarks>The flip flag is stored in the internal texture draw command.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="FlipH"/>
    public bool FlipV
    {
        get => flipV;
        set
        {
            flipV = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Draws the texture according to <see cref="StretchMode"/>.
    /// </summary>
    /// <remarks>No draw command is submitted when <see cref="Texture"/> is <c>null</c>.</remarks>
    /// <threadsafety>This callback is invoked on the main scene thread.</threadsafety>
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="CanvasItem.DrawTexture(Texture2D, Vector2, Color?)"/>
    public override void _Draw()
    {
        if (Texture is null)
        {
            return;
        }

        var destination = UiTextureDrawHelper.GetDestinationRect(Texture.GetSize(), Size, (int)StretchMode);
        if (destination.Size.X <= 0f || destination.Size.Y <= 0f)
        {
            return;
        }

        DrawTextureRect(Texture, destination, flipH: FlipH, flipV: FlipV);
    }

    /// <summary>
    /// Gets the minimum size requested by this texture rectangle.
    /// </summary>
    /// <returns>A size derived from <see cref="Texture"/> and <see cref="ExpandMode"/>.</returns>
    /// <remarks>When no texture is assigned, the minimum size is zero.</remarks>
    /// <threadsafety>This callback is invoked on the main scene thread.</threadsafety>
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="ExpandMode"/>
    public override Vector2 _GetMinimumSize()
    {
        if (Texture is null)
        {
            return Vector2.Zero;
        }

        var size = Texture.GetSize();
        return ExpandMode switch
        {
            ExpandModeEnum.IgnoreSize => Vector2.Zero,
            ExpandModeEnum.FitWidth => new Vector2(size.X, 0f),
            ExpandModeEnum.FitHeight => new Vector2(0f, size.Y),
            _ => size
        };
    }
}
