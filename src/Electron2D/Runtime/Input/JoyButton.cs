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
/// Identifies a standardized gamepad button.
/// </summary>
///
/// <remarks>
/// <para>
/// Button values are used by <see cref="InputEventJoypadButton"/>,
/// <see cref="InputMap"/> bindings and
/// <see cref="Input.IsJoyButtonPressed(int, JoyButton)"/>. The first four face
/// buttons use the common bottom, right, left and top physical layout order.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// Enumeration values are immutable and may be used from any thread.
/// </threadsafety>
///
/// <since>
/// This enum is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="InputEventJoypadButton"/>
/// <seealso cref="Input.IsJoyButtonPressed(int, JoyButton)"/>
public enum JoyButton
{
    /// <summary>
    /// Represents an invalid or unmapped gamepad button.
    /// </summary>
    Invalid = -1,

    /// <summary>
    /// Represents the bottom face button.
    /// </summary>
    A = 0,

    /// <summary>
    /// Represents the right face button.
    /// </summary>
    B = 1,

    /// <summary>
    /// Represents the left face button.
    /// </summary>
    X = 2,

    /// <summary>
    /// Represents the top face button.
    /// </summary>
    Y = 3,

    /// <summary>
    /// Represents the back or select button.
    /// </summary>
    Back = 4,

    /// <summary>
    /// Represents the guide, home or system button.
    /// </summary>
    Guide = 5,

    /// <summary>
    /// Represents the start or menu button.
    /// </summary>
    Start = 6,

    /// <summary>
    /// Represents pressing the left stick.
    /// </summary>
    LeftStick = 7,

    /// <summary>
    /// Represents pressing the right stick.
    /// </summary>
    RightStick = 8,

    /// <summary>
    /// Represents the left shoulder button.
    /// </summary>
    LeftShoulder = 9,

    /// <summary>
    /// Represents the right shoulder button.
    /// </summary>
    RightShoulder = 10,

    /// <summary>
    /// Represents the directional pad up button.
    /// </summary>
    DpadUp = 11,

    /// <summary>
    /// Represents the directional pad down button.
    /// </summary>
    DpadDown = 12,

    /// <summary>
    /// Represents the directional pad left button.
    /// </summary>
    DpadLeft = 13,

    /// <summary>
    /// Represents the directional pad right button.
    /// </summary>
    DpadRight = 14,

    /// <summary>
    /// Represents the first miscellaneous gamepad button.
    /// </summary>
    Misc1 = 15,

    /// <summary>
    /// Represents the first paddle button.
    /// </summary>
    Paddle1 = 16,

    /// <summary>
    /// Represents the second paddle button.
    /// </summary>
    Paddle2 = 17,

    /// <summary>
    /// Represents the third paddle button.
    /// </summary>
    Paddle3 = 18,

    /// <summary>
    /// Represents the fourth paddle button.
    /// </summary>
    Paddle4 = 19,

    /// <summary>
    /// Represents the touchpad click button.
    /// </summary>
    Touchpad = 20
}
