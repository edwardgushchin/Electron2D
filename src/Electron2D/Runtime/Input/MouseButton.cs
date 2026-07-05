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
/// Identifies a mouse button or mouse wheel direction in the Electron2D input model.
/// </summary>
/// <remarks>
/// <para>
/// Wheel steps are represented as button constants, matching Electron2D's
/// `InputEventMouseButton` model.
/// </para>
/// <threadsafety>
/// This enum is immutable and is safe to use from any thread.
/// </threadsafety>
/// </remarks>
/// <since>
/// This enum is available since Electron2D 0.1-preview.
/// </since>
public enum MouseButton
{
    /// <summary>No mouse button.</summary>
    /// <remarks>
    /// Use this value with APIs that accept MouseButton.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="MouseButton" />
    ///
    None = 0,

    /// <summary>Primary mouse button.</summary>
    /// <remarks>
    /// Use this value with APIs that accept MouseButton.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="MouseButton" />
    ///
    Left = 1,

    /// <summary>Secondary mouse button.</summary>
    /// <remarks>
    /// Use this value with APIs that accept MouseButton.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="MouseButton" />
    ///
    Right = 2,

    /// <summary>Middle mouse button.</summary>
    /// <remarks>
    /// Use this value with APIs that accept MouseButton.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="MouseButton" />
    ///
    Middle = 3,

    /// <summary>Mouse wheel scrolling up.</summary>
    /// <remarks>
    /// Use this value with APIs that accept MouseButton.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="MouseButton" />
    ///
    WheelUp = 4,

    /// <summary>Mouse wheel scrolling down.</summary>
    /// <remarks>
    /// Use this value with APIs that accept MouseButton.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="MouseButton" />
    ///
    WheelDown = 5,

    /// <summary>Mouse wheel scrolling left.</summary>
    /// <remarks>
    /// Use this value with APIs that accept MouseButton.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="MouseButton" />
    ///
    WheelLeft = 6,

    /// <summary>Mouse wheel scrolling right.</summary>
    /// <remarks>
    /// Use this value with APIs that accept MouseButton.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="MouseButton" />
    ///
    WheelRight = 7,

    /// <summary>Extra mouse button 1.</summary>
    /// <remarks>
    /// Use this value with APIs that accept MouseButton.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="MouseButton" />
    ///
    Xbutton1 = 8,

    /// <summary>Extra mouse button 2.</summary>
    /// <remarks>
    /// Use this value with APIs that accept MouseButton.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="MouseButton" />
    ///
    Xbutton2 = 9
}
