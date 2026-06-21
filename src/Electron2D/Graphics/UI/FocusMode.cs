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
/// Identifies how a <see cref="Control"/> can receive keyboard, gamepad or
/// mouse focus.
/// </summary>
///
/// <remarks>
/// <para>
/// Focus is owned by the root <see cref="Viewport"/> that contains the control.
/// Only one control can be focused in that viewport at a time.
/// </para>
/// <para>
/// This enumeration controls whether <see cref="Control.GrabFocus"/> can focus
/// a control and whether mouse press events focus the control before
/// <see cref="Control._GuiInput(InputEvent)"/> is called.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// Enumeration values are immutable and may be read from any thread.
/// </threadsafety>
///
/// <since>
/// This enum is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Control.FocusMode"/>
/// <seealso cref="Control.GrabFocus"/>
public enum FocusMode
{
    /// <summary>
    /// The control cannot receive focus.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// <see cref="Control.GrabFocus"/> ignores controls in this mode, and
    /// mouse presses do not focus them.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// Enumeration values are immutable and may be read from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This field is available since Electron2D 0.1.0 Preview.
    /// </since>
    None = 0,

    /// <summary>
    /// The control can receive focus from mouse or touch presses.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// This mode is intended for controls that should become focused when
    /// clicked but should not be selected by keyboard navigation in the preview
    /// baseline.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// Enumeration values are immutable and may be read from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This field is available since Electron2D 0.1.0 Preview.
    /// </since>
    Click = 1,

    /// <summary>
    /// The control can receive focus from direct focus calls and pointer input.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Keyboard/gamepad focus navigation is introduced by later UI tasks, but
    /// this mode already allows direct <see cref="Control.GrabFocus"/> calls
    /// and click focus in the input dispatch baseline.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// Enumeration values are immutable and may be read from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This field is available since Electron2D 0.1.0 Preview.
    /// </since>
    All = 2
}
