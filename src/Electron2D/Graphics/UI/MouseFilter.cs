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
/// Identifies how a <see cref="Control"/> receives and consumes mouse input.
/// </summary>
///
/// <remarks>
/// <para>
/// The value is read by the root <see cref="Viewport"/> while it routes
/// <see cref="InputEventMouse"/> events to controls.
/// </para>
/// <para>
/// A control can receive <see cref="Control._GuiInput(InputEvent)"/>, pass the
/// event to its parent control, stop further GUI input, or be ignored for mouse
/// hit-testing.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// Enumeration values are immutable and may be read from any thread.
/// </threadsafety>
///
/// <since>
/// This enum is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Control.MouseFilter"/>
/// <seealso cref="Control._GuiInput(InputEvent)"/>
public enum MouseFilter
{
    /// <summary>
    /// The control receives mouse GUI input and stops propagation afterward.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// If <see cref="Control._GuiInput(InputEvent)"/> does not call
    /// <see cref="Control.AcceptEvent"/>, the viewport marks the event handled
    /// after the callback returns.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// Enumeration values are immutable and may be read from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This field is available since Electron2D 0.1-preview.
    /// </since>
    Stop = 0,

    /// <summary>
    /// The control receives mouse GUI input and lets unhandled events bubble.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// If the callback does not accept the event, the viewport continues the
    /// GUI input path with the parent <see cref="Control"/> when one exists.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// Enumeration values are immutable and may be read from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This field is available since Electron2D 0.1-preview.
    /// </since>
    Pass = 1,

    /// <summary>
    /// The control is skipped by mouse GUI input hit-testing.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Descendant controls can still receive input when their own hit-test and
    /// filter settings allow it.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// Enumeration values are immutable and may be read from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This field is available since Electron2D 0.1-preview.
    /// </since>
    Ignore = 2
}
