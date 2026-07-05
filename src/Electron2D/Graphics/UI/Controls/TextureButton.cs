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
/// Provides a button that draws textures for its visual states.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>TextureButton</c> uses <see cref="BaseButton"/> input and signals, then
/// selects a texture for normal, pressed, disabled and focused states.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate texture buttons on the main
/// scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="BaseButton"/>
/// <seealso cref="Texture2D"/>
public class TextureButton : BaseButton
{
    private Texture2D? textureNormal;
    private Texture2D? texturePressed;
    private Texture2D? textureHover;
    private Texture2D? textureDisabled;
    private Texture2D? textureFocused;
    private Texture2D? textureClickMask;
    private bool ignoreTextureSize;
    private StretchModeEnum stretchMode;

    /// <summary>
    /// Identifies how a texture button scales its selected texture.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The values use the same numeric order as <see cref="TextureRect.StretchModeEnum"/>.
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
    /// <seealso cref="TextureButton.StretchMode"/>
    public enum StretchModeEnum
    {
        /// <summary>Scales the texture to the control rectangle.</summary>
        /// <remarks>Use this value with <see cref="TextureButton.StretchMode"/>.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="StretchModeEnum"/>
        Scale = 0,

        /// <summary>Draws the texture in the control rectangle.</summary>
        /// <remarks>Repeating tiles are not split into public draw calls.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="StretchModeEnum"/>
        Tile = 1,

        /// <summary>Keeps the texture size at the top-left corner.</summary>
        /// <remarks>Use this value to avoid scaling.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="StretchModeEnum"/>
        Keep = 2,

        /// <summary>Keeps the texture size and centers it.</summary>
        /// <remarks>Use this value to draw fixed-size centered button art.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="StretchModeEnum"/>
        KeepCentered = 3,

        /// <summary>Fits the texture inside the control while preserving aspect ratio.</summary>
        /// <remarks>Use this value when clipping is not desired.</remarks>
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
    /// Initializes a new instance of the <see cref="TextureButton"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// The new texture button uses scale drawing until
    /// <see cref="StretchMode"/> is changed.
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
    /// <seealso cref="TextureButton"/>
    public TextureButton()
    {
    }

    /// <summary>
    /// Gets or sets the default texture.
    /// </summary>
    /// <value>The texture used when no more specific state texture applies.</value>
    /// <remarks>Assigning this property queues a redraw.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Texture2D"/>
    public Texture2D? TextureNormal
    {
        get => textureNormal;
        set
        {
            textureNormal = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets the pressed-state texture.
    /// </summary>
    /// <value>The texture used when the button is pressed.</value>
    /// <remarks>If this value is <c>null</c>, <see cref="TextureNormal"/> is used.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="TextureNormal"/>
    public Texture2D? TexturePressed
    {
        get => texturePressed;
        set
        {
            texturePressed = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets the hover-state texture.
    /// </summary>
    /// <value>The texture reserved for hover rendering.</value>
    /// <remarks>Pointer hover tracking is not exposed as public state in this preview.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="TextureNormal"/>
    public Texture2D? TextureHover
    {
        get => textureHover;
        set
        {
            textureHover = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets the disabled-state texture.
    /// </summary>
    /// <value>The texture used when <see cref="BaseButton.Disabled"/> is <c>true</c>.</value>
    /// <remarks>If this value is <c>null</c>, <see cref="TextureNormal"/> is used.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="BaseButton.Disabled"/>
    public Texture2D? TextureDisabled
    {
        get => textureDisabled;
        set
        {
            textureDisabled = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets the focus overlay texture.
    /// </summary>
    /// <value>The texture drawn after the state texture when the button has focus.</value>
    /// <remarks>Set this to <c>null</c> to skip focus overlay drawing.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Control.HasFocus"/>
    public Texture2D? TextureFocused
    {
        get => textureFocused;
        set
        {
            textureFocused = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets the optional click-mask texture.
    /// </summary>
    /// <value>The texture reserved for opaque-pixel hit testing.</value>
    /// <remarks>The preview stores this resource for API compatibility; rectangular hit testing remains active.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Texture2D.IsPixelOpaque(int, int)"/>
    public Texture2D? TextureClickMask
    {
        get => textureClickMask;
        set
        {
            textureClickMask = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets whether the texture size is ignored for minimum-size calculation.
    /// </summary>
    /// <value><c>true</c> to return zero texture minimum size; otherwise, <c>false</c>.</value>
    /// <remarks>This property does not affect drawing, only <see cref="_GetMinimumSize"/>.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="_GetMinimumSize"/>
    public bool IgnoreTextureSize
    {
        get => ignoreTextureSize;
        set
        {
            ignoreTextureSize = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets how the selected texture is drawn.
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
    /// Draws the selected texture for the current button state.
    /// </summary>
    /// <remarks>The focus texture, when set, is drawn after the state texture.</remarks>
    /// <threadsafety>This callback is invoked on the main scene thread.</threadsafety>
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="TextureNormal"/>
    public override void _Draw()
    {
        DrawTextureState(GetStateTexture());
        if (HasFocus() && TextureFocused is not null)
        {
            DrawTextureState(TextureFocused);
        }
    }

    /// <summary>
    /// Gets the minimum size requested by this texture button.
    /// </summary>
    /// <returns>The normal texture size, or <see cref="Vector2.Zero"/> when no texture is available or size is ignored.</returns>
    /// <remarks>The pressed/disabled/focus textures do not change minimum size.</remarks>
    /// <threadsafety>This callback is invoked on the main scene thread.</threadsafety>
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="TextureNormal"/>
    public override Vector2 _GetMinimumSize()
    {
        return IgnoreTextureSize || TextureNormal is null ? Vector2.Zero : TextureNormal.GetSize();
    }

    private Texture2D? GetStateTexture()
    {
        if (Disabled)
        {
            return TextureDisabled ?? TextureNormal;
        }

        if (ButtonPressed || IsPressing)
        {
            return TexturePressed ?? TextureNormal;
        }

        return TextureNormal;
    }

    private void DrawTextureState(Texture2D? texture)
    {
        if (texture is null)
        {
            return;
        }

        var destination = UiTextureDrawHelper.GetDestinationRect(texture.GetSize(), Size, (int)StretchMode);
        if (destination.Size.X <= 0f || destination.Size.Y <= 0f)
        {
            return;
        }

        DrawTextureRect(texture, destination);
    }
}
