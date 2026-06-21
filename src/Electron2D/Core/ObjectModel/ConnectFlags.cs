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
/// Identifies values used by the connect flags API.
/// </summary>
///
/// <remarks>
/// This type is part of the Electron2D 0.1.0 Preview public API.
/// </remarks>
///
/// <since>
/// This API is available since Electron2D 0.1.0 Preview.
/// </since>
///
[Flags]
public enum ConnectFlags
{
    /// <summary>
    /// Identifies the none value.
    /// </summary>
    ///
    /// <remarks>
    /// Use this value with APIs that accept ConnectFlags.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ConnectFlags" />
    ///
    None = 0,
    /// <summary>
    /// Identifies the deferred value.
    /// </summary>
    ///
    /// <remarks>
    /// Use this value with APIs that accept ConnectFlags.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ConnectFlags" />
    ///
    Deferred = 1,
    /// <summary>
    /// Identifies the persist value.
    /// </summary>
    ///
    /// <remarks>
    /// Use this value with APIs that accept ConnectFlags.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ConnectFlags" />
    ///
    Persist = 2,
    /// <summary>
    /// Identifies the one shot value.
    /// </summary>
    ///
    /// <remarks>
    /// Use this value with APIs that accept ConnectFlags.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ConnectFlags" />
    ///
    OneShot = 4,
    /// <summary>
    /// Identifies the reference counted value.
    /// </summary>
    ///
    /// <remarks>
    /// Use this value with APIs that accept ConnectFlags.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ConnectFlags" />
    ///
    ReferenceCounted = 8,
    /// <summary>
    /// Identifies the append source object value.
    /// </summary>
    ///
    /// <remarks>
    /// Use this value with APIs that accept ConnectFlags.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ConnectFlags" />
    ///
    AppendSourceObject = 16
}
