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
/// Describes how a <see cref="Control"/> uses space allocated by a parent <see cref="Container"/>.
/// </summary>
///
/// <remarks>
/// <para>
/// The expansion and fill values can be combined. Containers use them to decide
/// whether a child fills its allocated slot, receives extra space, or stays near
/// a particular side of that slot.
/// </para>
/// </remarks>
///
/// <since>
/// This enum is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Control.SizeFlagsHorizontal"/>
/// <seealso cref="Control.SizeFlagsVertical"/>
[Flags]
public enum SizeFlags
{
    /// <summary>
    /// The control keeps its minimum size and is placed at the beginning of the allocated slot.
    /// </summary>
    ///
    /// <remarks>
    /// This is the neutral shrink value for the relevant axis.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1.0 Preview.
    /// </since>
    ShrinkBegin = 0,

    /// <summary>
    /// The control fills the allocated slot on the relevant axis.
    /// </summary>
    ///
    /// <remarks>
    /// Use this value when a child should stretch to the full slot size after
    /// the parent container has decided the slot rectangle.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1.0 Preview.
    /// </since>
    Fill = 1,

    /// <summary>
    /// The control receives a share of extra space on the relevant axis.
    /// </summary>
    ///
    /// <remarks>
    /// The share is weighted by <see cref="Control.SizeFlagsStretchRatio"/>.
    /// This flag does not imply that the child fills its final slot.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1.0 Preview.
    /// </since>
    Expand = 2,

    /// <summary>
    /// The control expands and fills the allocated slot on the relevant axis.
    /// </summary>
    ///
    /// <remarks>
    /// This value is equivalent to combining <see cref="Expand"/> with
    /// <see cref="Fill"/>.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1.0 Preview.
    /// </since>
    ExpandFill = 3,

    /// <summary>
    /// The control keeps its minimum size and is centered inside the allocated slot.
    /// </summary>
    ///
    /// <remarks>
    /// This flag is considered only when the child is not filling the slot on
    /// the relevant axis.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1.0 Preview.
    /// </since>
    ShrinkCenter = 4,

    /// <summary>
    /// The control keeps its minimum size and is placed at the end of the allocated slot.
    /// </summary>
    ///
    /// <remarks>
    /// This flag is considered only when the child is not filling the slot on
    /// the relevant axis.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1.0 Preview.
    /// </since>
    ShrinkEnd = 8
}
