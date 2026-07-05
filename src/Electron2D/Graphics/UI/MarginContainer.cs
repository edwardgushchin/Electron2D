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
/// Places direct child controls inside theme-defined margins.
/// </summary>
///
/// <remarks>
/// <para>
/// Margins are read from <c>margin_left</c>, <c>margin_top</c>,
/// <c>margin_right</c> and <c>margin_bottom</c> theme constant overrides.
/// Missing margin constants default to zero.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate it on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Container"/>
public class MarginContainer : Container
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarginContainer"/> type.
    /// </summary>
    ///
    /// <remarks>
    /// The new container starts with zero margins until theme constants are
    /// overridden.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Control.AddThemeConstantOverride(string, int)"/>
    public MarginContainer()
    {
    }

    /// <summary>
    /// Gets the minimum size required by this margin container.
    /// </summary>
    ///
    /// <returns>
    /// The largest child minimum size plus horizontal and vertical margins.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Multiple child controls share the same inset rectangle.
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
    /// <seealso cref="Container.FitChildInRect(Control, Rect2)"/>
    public override Vector2 _GetMinimumSize()
    {
        var childMinimum = Vector2.Zero;
        foreach (var child in GetLayoutChildren())
        {
            childMinimum = Max(childMinimum, child.GetCombinedMinimumSize());
        }

        var margins = GetMargins();
        return new Vector2(childMinimum.X + margins.Left + margins.Right, childMinimum.Y + margins.Top + margins.Bottom);
    }

    protected override void SortChildren()
    {
        var margins = GetMargins();
        var rect = new Rect2(
            margins.Left,
            margins.Top,
            MathF.Max(0f, Size.X - margins.Left - margins.Right),
            MathF.Max(0f, Size.Y - margins.Top - margins.Bottom));

        foreach (var child in GetLayoutChildren())
        {
            FitChildInRect(child, rect);
        }
    }

    private Margins GetMargins()
    {
        return new Margins(
            GetThemeConstantOrDefault("margin_left", 0),
            GetThemeConstantOrDefault("margin_top", 0),
            GetThemeConstantOrDefault("margin_right", 0),
            GetThemeConstantOrDefault("margin_bottom", 0));
    }

    private readonly record struct Margins(float Left, float Top, float Right, float Bottom);
}
