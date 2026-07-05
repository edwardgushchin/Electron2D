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
/// Provides a read-only visual progress indicator.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>ProgressBar</c> draws the current <see cref="Range.Ratio"/> and does not
/// handle user input.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate progress bars on the main
/// scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Range"/>
public class ProgressBar : Range
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressBar"/> class.
    /// </summary>
    /// <remarks>The new progress bar does not accept focus.</remarks>
    /// <threadsafety>This constructor is not synchronized. Call it from the main scene thread.</threadsafety>
    /// <since>This constructor is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="ProgressBar"/>
    public ProgressBar()
    {
        FocusMode = FocusMode.None;
    }

    /// <summary>
    /// Gets or sets whether a percentage label should be drawn.
    /// </summary>
    /// <value><c>true</c> to draw percentage text when a font is available; otherwise, <c>false</c>.</value>
    /// <remarks>The percentage is rounded to the nearest whole number.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Range.Ratio"/>
    public bool ShowPercentage { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this progress bar is indeterminate.
    /// </summary>
    /// <value><c>true</c> for indeterminate state; otherwise, <c>false</c>.</value>
    /// <remarks>The preview stores this flag and draws an empty progress fill when enabled.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="ProgressBar"/>
    public bool Indeterminate { get; set; }

    /// <summary>
    /// Draws the progress background, fill and optional percentage text.
    /// </summary>
    /// <remarks>The preview drawing path uses rectangle primitives.</remarks>
    /// <threadsafety>This callback is invoked on the main scene thread.</threadsafety>
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Range.Ratio"/>
    public override void _Draw()
    {
        if (Size.X <= 0f || Size.Y <= 0f)
        {
            return;
        }

        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.18f, 0.19f, 0.21f, 1f));
        if (!Indeterminate)
        {
            DrawRect(new Rect2(0f, 0f, Size.X * (float)Ratio, Size.Y), new Color(0.28f, 0.50f, 0.90f, 1f));
        }

        if (ShowPercentage && GetThemeFont("font") is { } font)
        {
            var fontSize = GetThemeFontSize("font_size");
            var text = $"{Math.Round(Ratio * 100d, MidpointRounding.AwayFromZero)}%";
            var baseline = new Vector2(0f, MathF.Max(font.GetAscent(fontSize), ((Size.Y - font.GetHeight(fontSize)) * 0.5f) + font.GetAscent(fontSize)));
            DrawString(font, baseline, text, HorizontalAlignment.Center, Size.X, fontSize);
        }
    }

    /// <summary>
    /// Gets the minimum size requested by this progress bar.
    /// </summary>
    /// <returns>A baseline progress bar size.</returns>
    /// <remarks>The minimum size does not depend on percentage text in this preview.</remarks>
    /// <threadsafety>This callback is invoked on the main scene thread.</threadsafety>
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Control.GetMinimumSize"/>
    public override Vector2 _GetMinimumSize()
    {
        return new Vector2(64f, 16f);
    }
}
