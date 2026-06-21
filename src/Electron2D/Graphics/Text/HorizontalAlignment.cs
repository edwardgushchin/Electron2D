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
/// Defines horizontal text alignment values used by Electron2D text drawing APIs.
/// </summary>
///
/// <threadsafety>
/// Enum values are immutable and can be used from any thread.
/// </threadsafety>
///
/// <since>
/// This enum is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="CanvasItem.DrawString" />
public enum HorizontalAlignment
{
    /// <summary>
    /// Align text to the left side of the available width.
    /// </summary>
    Left = 0,

    /// <summary>
    /// Align text to the center of the available width.
    /// </summary>
    Center = 1,

    /// <summary>
    /// Align text to the right side of the available width.
    /// </summary>
    Right = 2,

    /// <summary>
    /// Expand text spacing to fill the available width when supported.
    /// </summary>
    Fill = 3
}
