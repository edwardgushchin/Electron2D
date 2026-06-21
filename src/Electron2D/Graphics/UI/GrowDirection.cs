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
/// Describes which side of a <see cref="Control"/> moves when the control must grow to satisfy its minimum size.
/// </summary>
///
/// <remarks>
/// <para>
/// <see cref="Control.SetSize(Vector2)"/> and <see cref="Control.ResetSize"/> use this value when the requested size is smaller than <see cref="Control.GetCombinedMinimumSize"/>.
/// </para>
/// <para>
/// The value is interpreted independently for horizontal and vertical axes through <see cref="Control.GrowHorizontal"/> and <see cref="Control.GrowVertical"/>.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This enum is immutable and is safe to use from any thread.
/// </threadsafety>
///
/// <since>
/// This enum is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Control.GrowHorizontal"/>
/// <seealso cref="Control.GrowVertical"/>
public enum GrowDirection
{
    /// <summary>
    /// Moves the beginning side while preserving the ending side.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// On the horizontal axis this moves the left side. On the vertical axis this moves the top side.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This enum value is immutable and is safe to use from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="End"/>
    /// <seealso cref="Both"/>
    Begin = 0,

    /// <summary>
    /// Preserves the beginning side while moving the ending side.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// On the horizontal axis this moves the right side. On the vertical axis this moves the bottom side.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This enum value is immutable and is safe to use from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Begin"/>
    /// <seealso cref="Both"/>
    End = 1,

    /// <summary>
    /// Splits the required growth between the beginning and ending sides.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// This keeps the control centered around the requested rectangle as closely as floating-point coordinates allow.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This enum value is immutable and is safe to use from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This enum value is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Begin"/>
    /// <seealso cref="End"/>
    Both = 2
}
