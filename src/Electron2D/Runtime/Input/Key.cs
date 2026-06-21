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
/// Identifies a keyboard key in the Electron2D input event model.
/// </summary>
/// <remarks>
/// <para>
/// The 0.1.0 Preview enum contains the printable ASCII keys and the non-printable
/// keys needed by the keyboard mapping baseline. Printable values use their
/// Unicode code point. Non-printable values follow Electron2D's reserved
/// special key range.
/// </para>
/// <para>
/// More key constants can be added as the input backlog expands. Unknown platform
/// keys are mapped to <see cref="Unknown"/>.
/// </para>
/// <threadsafety>
/// This enum is immutable and is safe to use from any thread.
/// </threadsafety>
/// </remarks>
/// <since>
/// This enum is available since Electron2D 0.1.0 Preview.
/// </since>
public enum Key
{
    /// <summary>No key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    None = 0,

    /// <summary>Space key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Space = 32,

    /// <summary>Exclamation mark key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Exclam = 33,

    /// <summary>Double quotation mark key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Quotedbl = 34,

    /// <summary>Number sign key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Numbersign = 35,

    /// <summary>Dollar sign key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Dollar = 36,

    /// <summary>Percent sign key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Percent = 37,

    /// <summary>Ampersand key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Ampersand = 38,

    /// <summary>Apostrophe key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Apostrophe = 39,

    /// <summary>Left parenthesis key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Parenleft = 40,

    /// <summary>Right parenthesis key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Parenright = 41,

    /// <summary>Asterisk key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Asterisk = 42,

    /// <summary>Plus key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Plus = 43,

    /// <summary>Comma key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Comma = 44,

    /// <summary>Minus key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Minus = 45,

    /// <summary>Period key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Period = 46,

    /// <summary>Slash key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Slash = 47,

    /// <summary>Number 0 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Key0 = 48,

    /// <summary>Number 1 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Key1 = 49,

    /// <summary>Number 2 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Key2 = 50,

    /// <summary>Number 3 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Key3 = 51,

    /// <summary>Number 4 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Key4 = 52,

    /// <summary>Number 5 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Key5 = 53,

    /// <summary>Number 6 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Key6 = 54,

    /// <summary>Number 7 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Key7 = 55,

    /// <summary>Number 8 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Key8 = 56,

    /// <summary>Number 9 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Key9 = 57,

    /// <summary>Colon key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Colon = 58,

    /// <summary>Semicolon key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Semicolon = 59,

    /// <summary>Less-than key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Less = 60,

    /// <summary>Equals key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Equal = 61,

    /// <summary>Greater-than key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Greater = 62,

    /// <summary>Question mark key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Question = 63,

    /// <summary>At sign key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    At = 64,

    /// <summary>A key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    A = 65,

    /// <summary>B key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    B = 66,

    /// <summary>C key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    C = 67,

    /// <summary>D key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    D = 68,

    /// <summary>E key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    E = 69,

    /// <summary>F key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    F = 70,

    /// <summary>G key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    G = 71,

    /// <summary>H key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    H = 72,

    /// <summary>I key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    I = 73,

    /// <summary>J key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    J = 74,

    /// <summary>K key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    K = 75,

    /// <summary>L key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    L = 76,

    /// <summary>M key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    M = 77,

    /// <summary>N key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    N = 78,

    /// <summary>O key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    O = 79,

    /// <summary>P key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    P = 80,

    /// <summary>Q key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Q = 81,

    /// <summary>R key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    R = 82,

    /// <summary>S key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    S = 83,

    /// <summary>T key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    T = 84,

    /// <summary>U key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    U = 85,

    /// <summary>V key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    V = 86,

    /// <summary>W key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    W = 87,

    /// <summary>X key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    X = 88,

    /// <summary>Y key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Y = 89,

    /// <summary>Z key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Z = 90,

    /// <summary>Escape key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Escape = 4194305,

    /// <summary>Tab key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Tab = 4194306,

    /// <summary>Shift + Tab key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Backtab = 4194307,

    /// <summary>Backspace key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Backspace = 4194308,

    /// <summary>Enter key on the main keyboard.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Enter = 4194309,

    /// <summary>Enter key on the numeric keypad.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    KpEnter = 4194310,

    /// <summary>Insert key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Insert = 4194311,

    /// <summary>Delete key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Delete = 4194312,

    /// <summary>Pause key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Pause = 4194313,

    /// <summary>Print screen key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Print = 4194314,

    /// <summary>Home key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Home = 4194317,

    /// <summary>End key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    End = 4194318,

    /// <summary>Left arrow key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Left = 4194319,

    /// <summary>Up arrow key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Up = 4194320,

    /// <summary>Right arrow key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Right = 4194321,

    /// <summary>Down arrow key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Down = 4194322,

    /// <summary>Page Up key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Pageup = 4194323,

    /// <summary>Page Down key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Pagedown = 4194324,

    /// <summary>Shift key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Shift = 4194325,

    /// <summary>Control key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Ctrl = 4194326,

    /// <summary>Meta key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Meta = 4194327,

    /// <summary>Alt key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Alt = 4194328,

    /// <summary>Caps Lock key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Capslock = 4194329,

    /// <summary>Num Lock key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Numlock = 4194330,

    /// <summary>Scroll Lock key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Scrolllock = 4194331,

    /// <summary>F1 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    F1 = 4194332,

    /// <summary>F2 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    F2 = 4194333,

    /// <summary>F3 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    F3 = 4194334,

    /// <summary>F4 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    F4 = 4194335,

    /// <summary>F5 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    F5 = 4194336,

    /// <summary>F6 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    F6 = 4194337,

    /// <summary>F7 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    F7 = 4194338,

    /// <summary>F8 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    F8 = 4194339,

    /// <summary>F9 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    F9 = 4194340,

    /// <summary>F10 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    F10 = 4194341,

    /// <summary>F11 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    F11 = 4194342,

    /// <summary>F12 key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    F12 = 4194343,

    /// <summary>Mobile or application back navigation key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Back = 4194344,

    /// <summary>Mobile or application menu navigation key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Menu = 4194345,

    /// <summary>Unknown key.</summary>
    /// <remarks>
    /// Use this value with APIs that accept Key.
    /// </remarks>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Key" />
    ///
    Unknown = 8388607
}
