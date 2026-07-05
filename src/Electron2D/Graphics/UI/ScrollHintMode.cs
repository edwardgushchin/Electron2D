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
/// Describes which scroll hint edges a <see cref="ScrollContainer"/> may expose.
/// </summary>
///
/// <remarks>
/// <para>
/// The 0.1-preview stores the value for UI policy. Rendering of hint
/// affordances is intentionally left to later widget work.
/// </para>
/// </remarks>
///
/// <since>
/// This enum is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="ScrollContainer.ScrollHintMode"/>
public enum ScrollHintMode
{
    /// <summary>
    /// Disables scroll hints.
    /// </summary>
    ///
    /// <remarks>
    /// No edge hint should be rendered for this mode.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1-preview.
    /// </since>
    Disabled = 0,

    /// <summary>
    /// Allows scroll hints on every edge.
    /// </summary>
    ///
    /// <remarks>
    /// This value is useful when both axes can scroll.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1-preview.
    /// </since>
    All = 1,

    /// <summary>
    /// Allows hints on the top and left edges.
    /// </summary>
    ///
    /// <remarks>
    /// Use this value for layouts where the start edges are more important.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1-preview.
    /// </since>
    TopAndLeft = 2,

    /// <summary>
    /// Allows hints on the bottom and right edges.
    /// </summary>
    ///
    /// <remarks>
    /// Use this value for layouts where the end edges are more important.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1-preview.
    /// </since>
    BottomAndRight = 3
}
