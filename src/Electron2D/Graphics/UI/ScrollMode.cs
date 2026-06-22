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
/// Describes how a <see cref="ScrollContainer"/> exposes scrolling on one axis.
/// </summary>
///
/// <remarks>
/// <para>
/// The 0.1.0 Preview runtime uses these values for scroll offset policy. Visual
/// scrollbar controls are outside this container task.
/// </para>
/// </remarks>
///
/// <since>
/// This enum is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="ScrollContainer.HorizontalScrollMode"/>
/// <seealso cref="ScrollContainer.VerticalScrollMode"/>
public enum ScrollMode
{
    /// <summary>
    /// Disables scrolling on the axis.
    /// </summary>
    ///
    /// <remarks>
    /// The scroll offset is clamped to zero while this mode is active.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1.0 Preview.
    /// </since>
    Disabled = 0,

    /// <summary>
    /// Allows scrolling when content is larger than the container.
    /// </summary>
    ///
    /// <remarks>
    /// This is the default mode for both scroll axes.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1.0 Preview.
    /// </since>
    Auto = 1,

    /// <summary>
    /// Keeps the axis available even when content currently fits.
    /// </summary>
    ///
    /// <remarks>
    /// Visual scrollbar presentation is not part of the 0.1.0 Preview baseline.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1.0 Preview.
    /// </since>
    ShowAlways = 2,

    /// <summary>
    /// Keeps the axis hidden while preserving programmatic policy state.
    /// </summary>
    ///
    /// <remarks>
    /// This mode does not create a public scrollbar control.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1.0 Preview.
    /// </since>
    ShowNever = 3,

    /// <summary>
    /// Reserves room for future visual scrollbar presentation.
    /// </summary>
    ///
    /// <remarks>
    /// The preview implementation keeps layout stable without creating
    /// scrollbar nodes.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1.0 Preview.
    /// </since>
    Reserve = 4,

    /// <summary>
    /// Prefers maximizing the content area before showing scroll affordances.
    /// </summary>
    ///
    /// <remarks>
    /// This value is preserved for API compatibility with future visual
    /// scrollbars.
    /// </remarks>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1.0 Preview.
    /// </since>
    MaximizeFirst = 5
}
