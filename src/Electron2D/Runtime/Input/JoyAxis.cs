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
/// Identifies a normalized gamepad axis.
/// </summary>
///
/// <remarks>
/// <para>
/// Axis values are reported through <see cref="InputEventJoypadMotion"/> and
/// queried through <see cref="Input.GetJoyAxis(int, JoyAxis)"/>. Stick axes use
/// a signed range from <c>-1.0</c> to <c>1.0</c>; trigger axes use the same
/// storage range and are expected to report non-negative values on common
/// gamepad mappings.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// Enumeration values are immutable and may be used from any thread.
/// </threadsafety>
///
/// <since>
/// This enum is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="InputEventJoypadMotion"/>
/// <seealso cref="Input.GetJoyAxis(int, JoyAxis)"/>
public enum JoyAxis
{
    /// <summary>
    /// Represents an invalid or unmapped gamepad axis.
    /// </summary>
    /// <remarks>
    /// Use this value with APIs that accept JoyAxis.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="JoyAxis" />
    ///
    Invalid = -1,

    /// <summary>
    /// Represents the horizontal axis of the left stick.
    /// </summary>
    /// <remarks>
    /// Use this value with APIs that accept JoyAxis.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="JoyAxis" />
    ///
    LeftX = 0,

    /// <summary>
    /// Represents the vertical axis of the left stick.
    /// </summary>
    /// <remarks>
    /// Use this value with APIs that accept JoyAxis.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="JoyAxis" />
    ///
    LeftY = 1,

    /// <summary>
    /// Represents the horizontal axis of the right stick.
    /// </summary>
    /// <remarks>
    /// Use this value with APIs that accept JoyAxis.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="JoyAxis" />
    ///
    RightX = 2,

    /// <summary>
    /// Represents the vertical axis of the right stick.
    /// </summary>
    /// <remarks>
    /// Use this value with APIs that accept JoyAxis.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="JoyAxis" />
    ///
    RightY = 3,

    /// <summary>
    /// Represents the left trigger axis.
    /// </summary>
    /// <remarks>
    /// Use this value with APIs that accept JoyAxis.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="JoyAxis" />
    ///
    TriggerLeft = 4,

    /// <summary>
    /// Represents the right trigger axis.
    /// </summary>
    /// <remarks>
    /// Use this value with APIs that accept JoyAxis.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="JoyAxis" />
    ///
    TriggerRight = 5
}
