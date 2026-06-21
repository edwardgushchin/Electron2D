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
/// Identifies the vertical alignment used by controls that draw text.
/// </summary>
///
/// <threadsafety>
/// This enum is immutable and can be used from any thread.
/// </threadsafety>
///
/// <since>
/// This enum is available since Electron2D 0.1.0 Preview.
/// </since>
    /// <remarks>
    /// This type is part of the Electron2D 0.1.0 Preview public API.
    /// </remarks>
    ///
public enum VerticalAlignment
{
    /// <summary>
    /// Aligns content to the top edge.
    /// </summary>
    /// <remarks>
    /// Use this value with APIs that accept VerticalAlignment.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="VerticalAlignment" />
    ///
    Top = 0,

    /// <summary>
    /// Centers content vertically.
    /// </summary>
    /// <remarks>
    /// Use this value with APIs that accept VerticalAlignment.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="VerticalAlignment" />
    ///
    Center = 1,

    /// <summary>
    /// Aligns content to the bottom edge.
    /// </summary>
    /// <remarks>
    /// Use this value with APIs that accept VerticalAlignment.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="VerticalAlignment" />
    ///
    Bottom = 2,

    /// <summary>
    /// Uses fill alignment. Electron2D 0.1.0 Preview treats this like top alignment for single-line labels.
    /// </summary>
    /// <remarks>
    /// Use this value with APIs that accept VerticalAlignment.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="VerticalAlignment" />
    ///
    Fill = 3
}
