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
/// Provides a rectangular UI control used as a simple visual panel.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>Panel</c> submits one filled rectangle that matches <see cref="Control.Size"/>.
/// It is intended for simple runtime interface backgrounds and container
/// decoration in the 0.1.0 Preview UI subset.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate panels on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Control"/>
public class Panel : Control
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Panel"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// The new panel uses the inherited <see cref="Control.MouseFilter"/> and
    /// drawing state until changed by user code.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Panel"/>
    public Panel()
    {
    }

    /// <summary>
    /// Draws the panel rectangle.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The preview implementation uses a neutral default fill color. Full theme
    /// style boxes are outside the current runtime API scope.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="CanvasItem.DrawRect(Rect2, Color, bool, float, bool)"/>
    public override void _Draw()
    {
        if (Size.X <= 0f || Size.Y <= 0f)
        {
            return;
        }

        var rect = new Rect2(Vector2.Zero, Size);
        if (GetThemeStyleBox("panel") is { } styleBox)
        {
            styleBox.Draw(this, rect);
            return;
        }

        DrawRect(rect, HasThemeColor("panel") ? GetThemeColor("panel") : new Color(0.16f, 0.17f, 0.19f, 1f));
    }
}
