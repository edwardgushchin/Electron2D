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
/// Provides the Electron2D control for drawing a single line of plain text.
/// </summary>
///
/// <remarks>
/// `Label` uses the `font` and `font_size` theme overrides from
/// <see cref="Control" /> and submits text through <see cref="CanvasItem.DrawString" />.
/// Multiline wrapping, clipping, overrun behavior, language-specific shaping
/// and label settings are planned later UI/text tasks.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate labels on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Control" />
/// <seealso cref="Font" />
public class Label : Control
{
    private string text = string.Empty;

    /// <summary>
    /// Gets or sets the plain text drawn by this label.
    /// </summary>
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
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
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
    /// Gets or sets the horizontal alignment used when drawing text inside <see cref="Control.Size" />.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;

    /// <summary>
    /// Gets or sets the vertical alignment used when drawing text inside <see cref="Control.Size" />.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

    /// <summary>
    /// Gets or sets whether text is converted to invariant uppercase before drawing.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool Uppercase { get; set; }

    /// <summary>
    /// Draws the label text when the control is redrawn.
    /// </summary>
    ///
    /// <remarks>
    /// The method reads the `font` and `font_size` theme overrides. If no
    /// `font` override exists, no draw command is submitted.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public override void _Draw()
    {
        var font = GetThemeFont("font");
        if (font is null)
        {
            return;
        }

        var fontSize = GetThemeFontSize("font_size");
        var drawText = Uppercase ? Text.ToUpperInvariant() : Text;
        var width = Size.X > 0f ? Size.X : -1f;
        var baseline = new Vector2(0f, GetBaseline(font, fontSize));
        DrawString(font, baseline, drawText, HorizontalAlignment, width, fontSize);
    }

    private float GetBaseline(Font font, int fontSize)
    {
        var height = font.GetHeight(fontSize);
        var ascent = font.GetAscent(fontSize);
        var descent = font.GetDescent(fontSize);

        return VerticalAlignment switch
        {
            VerticalAlignment.Center => MathF.Max(0f, (Size.Y - height) / 2f) + ascent,
            VerticalAlignment.Bottom => MathF.Max(ascent, Size.Y - descent),
            _ => ascent
        };
    }
}
