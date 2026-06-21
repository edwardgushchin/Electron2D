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
    None = 0,

    /// <summary>Space key.</summary>
    Space = 32,

    /// <summary>Exclamation mark key.</summary>
    Exclam = 33,

    /// <summary>Double quotation mark key.</summary>
    Quotedbl = 34,

    /// <summary>Number sign key.</summary>
    Numbersign = 35,

    /// <summary>Dollar sign key.</summary>
    Dollar = 36,

    /// <summary>Percent sign key.</summary>
    Percent = 37,

    /// <summary>Ampersand key.</summary>
    Ampersand = 38,

    /// <summary>Apostrophe key.</summary>
    Apostrophe = 39,

    /// <summary>Left parenthesis key.</summary>
    Parenleft = 40,

    /// <summary>Right parenthesis key.</summary>
    Parenright = 41,

    /// <summary>Asterisk key.</summary>
    Asterisk = 42,

    /// <summary>Plus key.</summary>
    Plus = 43,

    /// <summary>Comma key.</summary>
    Comma = 44,

    /// <summary>Minus key.</summary>
    Minus = 45,

    /// <summary>Period key.</summary>
    Period = 46,

    /// <summary>Slash key.</summary>
    Slash = 47,

    /// <summary>Number 0 key.</summary>
    Key0 = 48,

    /// <summary>Number 1 key.</summary>
    Key1 = 49,

    /// <summary>Number 2 key.</summary>
    Key2 = 50,

    /// <summary>Number 3 key.</summary>
    Key3 = 51,

    /// <summary>Number 4 key.</summary>
    Key4 = 52,

    /// <summary>Number 5 key.</summary>
    Key5 = 53,

    /// <summary>Number 6 key.</summary>
    Key6 = 54,

    /// <summary>Number 7 key.</summary>
    Key7 = 55,

    /// <summary>Number 8 key.</summary>
    Key8 = 56,

    /// <summary>Number 9 key.</summary>
    Key9 = 57,

    /// <summary>Colon key.</summary>
    Colon = 58,

    /// <summary>Semicolon key.</summary>
    Semicolon = 59,

    /// <summary>Less-than key.</summary>
    Less = 60,

    /// <summary>Equals key.</summary>
    Equal = 61,

    /// <summary>Greater-than key.</summary>
    Greater = 62,

    /// <summary>Question mark key.</summary>
    Question = 63,

    /// <summary>At sign key.</summary>
    At = 64,

    /// <summary>A key.</summary>
    A = 65,

    /// <summary>B key.</summary>
    B = 66,

    /// <summary>C key.</summary>
    C = 67,

    /// <summary>D key.</summary>
    D = 68,

    /// <summary>E key.</summary>
    E = 69,

    /// <summary>F key.</summary>
    F = 70,

    /// <summary>G key.</summary>
    G = 71,

    /// <summary>H key.</summary>
    H = 72,

    /// <summary>I key.</summary>
    I = 73,

    /// <summary>J key.</summary>
    J = 74,

    /// <summary>K key.</summary>
    K = 75,

    /// <summary>L key.</summary>
    L = 76,

    /// <summary>M key.</summary>
    M = 77,

    /// <summary>N key.</summary>
    N = 78,

    /// <summary>O key.</summary>
    O = 79,

    /// <summary>P key.</summary>
    P = 80,

    /// <summary>Q key.</summary>
    Q = 81,

    /// <summary>R key.</summary>
    R = 82,

    /// <summary>S key.</summary>
    S = 83,

    /// <summary>T key.</summary>
    T = 84,

    /// <summary>U key.</summary>
    U = 85,

    /// <summary>V key.</summary>
    V = 86,

    /// <summary>W key.</summary>
    W = 87,

    /// <summary>X key.</summary>
    X = 88,

    /// <summary>Y key.</summary>
    Y = 89,

    /// <summary>Z key.</summary>
    Z = 90,

    /// <summary>Escape key.</summary>
    Escape = 4194305,

    /// <summary>Tab key.</summary>
    Tab = 4194306,

    /// <summary>Shift + Tab key.</summary>
    Backtab = 4194307,

    /// <summary>Backspace key.</summary>
    Backspace = 4194308,

    /// <summary>Enter key on the main keyboard.</summary>
    Enter = 4194309,

    /// <summary>Enter key on the numeric keypad.</summary>
    KpEnter = 4194310,

    /// <summary>Insert key.</summary>
    Insert = 4194311,

    /// <summary>Delete key.</summary>
    Delete = 4194312,

    /// <summary>Pause key.</summary>
    Pause = 4194313,

    /// <summary>Print screen key.</summary>
    Print = 4194314,

    /// <summary>Home key.</summary>
    Home = 4194317,

    /// <summary>End key.</summary>
    End = 4194318,

    /// <summary>Left arrow key.</summary>
    Left = 4194319,

    /// <summary>Up arrow key.</summary>
    Up = 4194320,

    /// <summary>Right arrow key.</summary>
    Right = 4194321,

    /// <summary>Down arrow key.</summary>
    Down = 4194322,

    /// <summary>Page Up key.</summary>
    Pageup = 4194323,

    /// <summary>Page Down key.</summary>
    Pagedown = 4194324,

    /// <summary>Shift key.</summary>
    Shift = 4194325,

    /// <summary>Control key.</summary>
    Ctrl = 4194326,

    /// <summary>Meta key.</summary>
    Meta = 4194327,

    /// <summary>Alt key.</summary>
    Alt = 4194328,

    /// <summary>Caps Lock key.</summary>
    Capslock = 4194329,

    /// <summary>Num Lock key.</summary>
    Numlock = 4194330,

    /// <summary>Scroll Lock key.</summary>
    Scrolllock = 4194331,

    /// <summary>F1 key.</summary>
    F1 = 4194332,

    /// <summary>F2 key.</summary>
    F2 = 4194333,

    /// <summary>F3 key.</summary>
    F3 = 4194334,

    /// <summary>F4 key.</summary>
    F4 = 4194335,

    /// <summary>F5 key.</summary>
    F5 = 4194336,

    /// <summary>F6 key.</summary>
    F6 = 4194337,

    /// <summary>F7 key.</summary>
    F7 = 4194338,

    /// <summary>F8 key.</summary>
    F8 = 4194339,

    /// <summary>F9 key.</summary>
    F9 = 4194340,

    /// <summary>F10 key.</summary>
    F10 = 4194341,

    /// <summary>F11 key.</summary>
    F11 = 4194342,

    /// <summary>F12 key.</summary>
    F12 = 4194343,

    /// <summary>Mobile or application back navigation key.</summary>
    Back = 4194344,

    /// <summary>Mobile or application menu navigation key.</summary>
    Menu = 4194345,

    /// <summary>Unknown key.</summary>
    Unknown = 8388607
}
