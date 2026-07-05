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
/// Provides a text button control.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>Button</c> builds on <see cref="BaseButton"/> and draws a simple text
/// label using the inherited <c>font</c> and <c>font_size</c> theme overrides.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate buttons on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="BaseButton"/>
/// <seealso cref="CheckBox"/>
public class Button : BaseButton
{
    private string text = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="Button"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// The new button inherits signal and focus defaults from
    /// <see cref="BaseButton"/>.
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
    /// <seealso cref="Button"/>
    public Button()
    {
    }

    /// <summary>
    /// Gets or sets the text displayed by this button.
    /// </summary>
    ///
    /// <value>
    /// The button text. The value is never <c>null</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Assigning text queues a redraw. Empty text is valid and draws only the
    /// button background.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when the assigned value is <c>null</c>.
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
    /// <seealso cref="Button"/>
    public string Text
    {
        get
        {
            ThrowIfFreed();
            return text;
        }
        set
        {
            ThrowIfFreed();
            ArgumentNullException.ThrowIfNull(value);
            text = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Draws the button background and text.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The preview drawing path uses neutral colors and theme font overrides.
    /// Full theme style boxes are outside this task.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="BaseButton"/>
    public override void _Draw()
    {
        var rect = new Rect2(Vector2.Zero, Size);
        if (GetThemeStyleBox(GetButtonStyleBoxName()) is { } styleBox)
        {
            styleBox.Draw(this, rect);
        }
        else
        {
            DrawButtonFrame(rect, GetButtonColor());
        }

        DrawButtonText(new Vector2(8f, 0f), MathF.Max(0f, Size.X - 16f));
    }

    /// <summary>
    /// Gets the minimum size requested by this button.
    /// </summary>
    ///
    /// <returns>
    /// A size large enough to contain the current text and button padding.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// If no font override is available, the method returns a conservative
    /// text-independent fallback.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Control.GetMinimumSize"/>
    public override Vector2 _GetMinimumSize()
    {
        var font = GetThemeFont("font");
        if (font is null || Text.Length == 0)
        {
            return new Vector2(64f, 24f);
        }

        var fontSize = GetThemeFontSize("font_size");
        var textSize = font.GetStringSize(Text, HorizontalAlignment.Left, width: -1f, fontSize);
        return new Vector2(textSize.X + 16f, MathF.Max(24f, textSize.Y + 8f));
    }

    internal void DrawButtonFrame(Rect2 rect, Color color)
    {
        if (rect.Size.X <= 0f || rect.Size.Y <= 0f)
        {
            return;
        }

        DrawRect(rect, color);
    }

    internal void DrawButtonText(Vector2 offset, float width)
    {
        if (Text.Length == 0)
        {
            return;
        }

        var font = GetThemeFont("font");
        if (font is null)
        {
            return;
        }

        var fontSize = GetThemeFontSize("font_size");
        var baseline = offset + new Vector2(0f, MathF.Max(font.GetAscent(fontSize), ((Size.Y - font.GetHeight(fontSize)) * 0.5f) + font.GetAscent(fontSize)));
        var color = HasThemeColor("font_color") ? GetThemeColor("font_color") : (Color?)null;
        DrawString(font, baseline, Text, HorizontalAlignment.Center, width, fontSize, color);
    }

    internal Color GetButtonColor()
    {
        if (Disabled)
        {
            return HasThemeColor("disabled_color") ? GetThemeColor("disabled_color") : new Color(0.18f, 0.18f, 0.19f, 0.75f);
        }

        if (ButtonPressed || IsPressing)
        {
            return HasThemeColor("pressed_color") ? GetThemeColor("pressed_color") : new Color(0.25f, 0.32f, 0.45f, 1f);
        }

        if (HasFocus())
        {
            return HasThemeColor("focus_color") ? GetThemeColor("focus_color") : new Color(0.22f, 0.28f, 0.38f, 1f);
        }

        return HasThemeColor("normal_color") ? GetThemeColor("normal_color") : new Color(0.20f, 0.22f, 0.25f, 1f);
    }

    private string GetButtonStyleBoxName()
    {
        if (Disabled)
        {
            return "disabled";
        }

        if (ButtonPressed || IsPressing)
        {
            return "pressed";
        }

        return HasFocus() ? "focus" : "normal";
    }
}
