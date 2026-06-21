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
/// Provides bit flags for mouse buttons held during a mouse event.
/// </summary>
/// <remarks>
/// <para>
/// The flag values match Godot's mouse button masks. SDL button flags are
/// converted to this representation by the internal SDL input mapper.
/// </para>
/// <threadsafety>
/// This enum is immutable and is safe to use from any thread.
/// </threadsafety>
/// </remarks>
/// <since>
/// This enum is available since Electron2D 0.1.0 Preview.
/// </since>
[Flags]
public enum MouseButtonMask
{
    /// <summary>No mouse buttons are held.</summary>
    None = 0,

    /// <summary>The primary mouse button is held.</summary>
    Left = 1,

    /// <summary>The secondary mouse button is held.</summary>
    Right = 2,

    /// <summary>The middle mouse button is held.</summary>
    Middle = 4,

    /// <summary>Extra mouse button 1 is held.</summary>
    Xbutton1 = 128,

    /// <summary>Extra mouse button 2 is held.</summary>
    Xbutton2 = 256
}
