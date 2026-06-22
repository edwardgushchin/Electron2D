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
/// Provides a toggle button with a check indicator and text.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>CheckBox</c> is a <see cref="Button"/> with <see cref="BaseButton.ToggleMode"/>
/// enabled by default. It emits the inherited button signals.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate check boxes on the main
/// scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Button"/>
/// <seealso cref="BaseButton.ButtonPressed"/>
public class CheckBox : Button
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CheckBox"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// The new check box enables <see cref="BaseButton.ToggleMode"/> so user
    /// activation changes <see cref="BaseButton.ButtonPressed"/>.
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
    /// <seealso cref="CheckBox"/>
    public CheckBox()
    {
        ToggleMode = true;
    }

    /// <summary>
    /// Draws the checkbox indicator and text.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The preview visual style uses rectangle primitives. Theme style boxes
    /// and icons can be layered later without changing the public API.
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
    /// <seealso cref="Button._Draw"/>
    public override void _Draw()
    {
        var boxRect = new Rect2(0f, MathF.Max(0f, (Size.Y - 16f) * 0.5f), 16f, 16f);
        DrawRect(boxRect, GetButtonColor(), filled: true);
        DrawRect(boxRect, Color.White, filled: false, width: 1f);
        if (ButtonPressed)
        {
            DrawRect(new Rect2(boxRect.Position + new Vector2(4f, 4f), new Vector2(8f, 8f)), Color.White);
        }

        DrawButtonText(new Vector2(24f, 0f), MathF.Max(0f, Size.X - 24f));
    }

    /// <summary>
    /// Gets the minimum size requested by this check box.
    /// </summary>
    ///
    /// <returns>
    /// The inherited text minimum size plus space for the check indicator.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The indicator reserves 24 pixels on the horizontal axis.
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
    /// <seealso cref="Button._GetMinimumSize"/>
    public override Vector2 _GetMinimumSize()
    {
        var textMinimum = base._GetMinimumSize();
        return new Vector2(textMinimum.X + 24f, MathF.Max(24f, textMinimum.Y));
    }
}
