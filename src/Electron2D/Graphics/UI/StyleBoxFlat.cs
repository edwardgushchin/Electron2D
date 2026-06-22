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
/// Provides a rectangle style box with a fill color and optional borders.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>StyleBoxFlat</c> is the lightweight style resource used by the preview UI
/// theme system for panels, buttons and tooltip backgrounds.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate style boxes on the main
/// scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="StyleBox"/>
/// <seealso cref="Theme.SetStyleBox(string, string, StyleBox)"/>
public class StyleBoxFlat : StyleBox
{
    private Color bgColor = new(0.6f, 0.6f, 0.6f, 1f);
    private Color borderColor = new(0.8f, 0.8f, 0.8f, 1f);
    private int borderWidthLeft;
    private int borderWidthTop;
    private int borderWidthRight;
    private int borderWidthBottom;

    /// <summary>
    /// Initializes a new instance of the <see cref="StyleBoxFlat"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// The new style box uses a neutral gray background, a light border color
    /// and zero border widths.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it from the main scene
    /// thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="StyleBoxFlat"/>
    public StyleBoxFlat()
    {
    }

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    ///
    /// <value>
    /// The fill color drawn over the target rectangle.
    /// </value>
    ///
    /// <remarks>
    /// The default value is an opaque neutral gray.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Draw(CanvasItem, Rect2)"/>
    public Color BgColor
    {
        get
        {
            ThrowIfFreed();
            return bgColor;
        }
        set
        {
            ThrowIfFreed();
            bgColor = value;
        }
    }

    /// <summary>
    /// Gets or sets the border color.
    /// </summary>
    ///
    /// <value>
    /// The color used for every non-zero border side.
    /// </value>
    ///
    /// <remarks>
    /// Border rectangles are drawn after the background.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="BorderWidthLeft"/>
    public Color BorderColor
    {
        get
        {
            ThrowIfFreed();
            return borderColor;
        }
        set
        {
            ThrowIfFreed();
            borderColor = value;
        }
    }

    /// <summary>
    /// Gets or sets the left border width.
    /// </summary>
    ///
    /// <value>
    /// The left border width in UI units. The value must be non-negative.
    /// </value>
    ///
    /// <remarks>
    /// A value of <c>0</c> disables the left border.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="BorderColor"/>
    public int BorderWidthLeft
    {
        get
        {
            ThrowIfFreed();
            return borderWidthLeft;
        }
        set
        {
            ThrowIfFreed();
            borderWidthLeft = ValidateBorderWidth(value, nameof(BorderWidthLeft));
        }
    }

    /// <summary>
    /// Gets or sets the top border width.
    /// </summary>
    ///
    /// <value>
    /// The top border width in UI units. The value must be non-negative.
    /// </value>
    ///
    /// <remarks>
    /// A value of <c>0</c> disables the top border.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="BorderColor"/>
    public int BorderWidthTop
    {
        get
        {
            ThrowIfFreed();
            return borderWidthTop;
        }
        set
        {
            ThrowIfFreed();
            borderWidthTop = ValidateBorderWidth(value, nameof(BorderWidthTop));
        }
    }

    /// <summary>
    /// Gets or sets the right border width.
    /// </summary>
    ///
    /// <value>
    /// The right border width in UI units. The value must be non-negative.
    /// </value>
    ///
    /// <remarks>
    /// A value of <c>0</c> disables the right border.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="BorderColor"/>
    public int BorderWidthRight
    {
        get
        {
            ThrowIfFreed();
            return borderWidthRight;
        }
        set
        {
            ThrowIfFreed();
            borderWidthRight = ValidateBorderWidth(value, nameof(BorderWidthRight));
        }
    }

    /// <summary>
    /// Gets or sets the bottom border width.
    /// </summary>
    ///
    /// <value>
    /// The bottom border width in UI units. The value must be non-negative.
    /// </value>
    ///
    /// <remarks>
    /// A value of <c>0</c> disables the bottom border.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="BorderColor"/>
    public int BorderWidthBottom
    {
        get
        {
            ThrowIfFreed();
            return borderWidthBottom;
        }
        set
        {
            ThrowIfFreed();
            borderWidthBottom = ValidateBorderWidth(value, nameof(BorderWidthBottom));
        }
    }

    /// <summary>
    /// Draws the flat background and borders into a canvas item.
    /// </summary>
    ///
    /// <param name="canvasItem">
    /// The canvas item that receives draw commands.
    /// </param>
    ///
    /// <param name="rect">
    /// The local rectangle to draw into.
    /// </param>
    ///
    /// <remarks>
    /// The background is drawn first, then each non-zero border side is drawn
    /// as a filled rectangle.
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="canvasItem"/> is <c>null</c>.
    /// </exception>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="rect"/> contains non-finite components.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="CanvasItem.DrawRect(Rect2, Color, bool, float, bool)"/>
    public override void Draw(CanvasItem canvasItem, Rect2 rect)
    {
        base.Draw(canvasItem, rect);
        if (rect.Size.X <= 0f || rect.Size.Y <= 0f)
        {
            return;
        }

        canvasItem.DrawRect(rect, BgColor);
        DrawBorder(canvasItem, new Rect2(rect.Position, new Vector2(MathF.Min(BorderWidthLeft, rect.Size.X), rect.Size.Y)), BorderWidthLeft, BorderColor);
        DrawBorder(canvasItem, new Rect2(rect.Position, new Vector2(rect.Size.X, MathF.Min(BorderWidthTop, rect.Size.Y))), BorderWidthTop, BorderColor);
        DrawBorder(
            canvasItem,
            new Rect2(
                new Vector2(rect.Position.X + MathF.Max(0f, rect.Size.X - BorderWidthRight), rect.Position.Y),
                new Vector2(MathF.Min(BorderWidthRight, rect.Size.X), rect.Size.Y)),
            BorderWidthRight,
            BorderColor);
        DrawBorder(
            canvasItem,
            new Rect2(
                new Vector2(rect.Position.X, rect.Position.Y + MathF.Max(0f, rect.Size.Y - BorderWidthBottom)),
                new Vector2(rect.Size.X, MathF.Min(BorderWidthBottom, rect.Size.Y))),
            BorderWidthBottom,
            BorderColor);
    }

    private static void DrawBorder(CanvasItem canvasItem, Rect2 rect, int width, Color color)
    {
        if (width <= 0 || rect.Size.X <= 0f || rect.Size.Y <= 0f)
        {
            return;
        }

        canvasItem.DrawRect(rect, color);
    }

    private static int ValidateBorderWidth(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Border width must be non-negative.");
        }

        return value;
    }
}
