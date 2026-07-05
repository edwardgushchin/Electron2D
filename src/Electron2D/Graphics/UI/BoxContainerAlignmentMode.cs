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
/// Describes how a <see cref="BoxContainer"/> positions children when free space remains.
/// </summary>
///
/// <remarks>
/// <para>
/// Alignment is applied only after non-expanded children, separation and
/// expanded slots have been measured.
/// </para>
/// </remarks>
///
/// <since>
/// This enum is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="BoxContainer.Alignment"/>
public enum BoxContainerAlignmentMode
{
    /// <summary>
    /// Places children at the beginning of the box axis.
    /// </summary>
    ///
    /// <remarks>
    /// This is the default alignment for a <see cref="BoxContainer"/>.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1-preview.
    /// </since>
    Begin = 0,

    /// <summary>
    /// Centers children along the box axis.
    /// </summary>
    ///
    /// <remarks>
    /// Free space is split before and after the children.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1-preview.
    /// </since>
    Center = 1,

    /// <summary>
    /// Places children at the end of the box axis.
    /// </summary>
    ///
    /// <remarks>
    /// Free space is placed before the first child.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1-preview.
    /// </since>
    End = 2
}
