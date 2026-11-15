/*
  Simple DirectMedia Layer
  Copyright (C) 1997-2026 Sam Lantinga <slouken@libsdl.org>

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.
*/
// Adapted from SDL release-3.4.18 src/events/scancodes_windows.h.
// Numeric entries preserve the pinned native keyboard mapping; see licence/SDL-LICENSE.txt.
using SDL3;

namespace Electron2D;

public sealed partial class NativeMenu
{
    private static ReadOnlySpan<ushort> WindowsScancodes =>
    [
        0, 41, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 45, 46, 42, 43,
        20, 26, 8, 21, 23, 28, 24, 12, 18, 19, 47, 48, 40, 224, 4, 22,
        7, 9, 10, 11, 13, 14, 15, 51, 52, 53, 225, 49, 29, 27, 6, 25,
        5, 17, 16, 54, 55, 56, 229, 85, 226, 44, 57, 58, 59, 60, 61, 62,
        63, 64, 65, 66, 67, 83, 71, 95, 96, 97, 86, 92, 93, 94, 87, 89,
        90, 91, 98, 99, 0, 0, 100, 68, 69, 103, 0, 0, 140, 0, 0, 0,
        0, 0, 0, 0, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 0,
        136, 145, 144, 135, 0, 0, 115, 147, 146, 138, 0, 139, 0, 137, 133, 0,
        0, 0, 0, 0, 0, 0, 0, 121, 122, 0, 125, 0, 0, 0, 0, 0,
        268, 0, 0, 0, 0, 0, 0, 123, 124, 267, 0, 0, 88, 228, 0, 0,
        127, 0, 271, 0, 269, 0, 0, 0, 0, 0, 0, 0, 270, 0, 129, 0,
        128, 0, 281, 0, 0, 84, 229, 70, 230, 0, 0, 117, 0, 0, 273, 274,
        275, 0, 0, 0, 0, 83, 72, 74, 82, 75, 0, 80, 0, 79, 0, 77,
        81, 78, 73, 76, 0, 0, 0, 277, 278, 0, 0, 227, 231, 101, 102, 258,
        0, 0, 0, 259, 0, 280, 286, 285, 284, 283, 282, 0, 0, 272, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    ];

    private static Key WindowsKeycode(nuint value, nint details)
    {
        var scan = ((uint)(nuint)details >> 16) & 0x7f;
        if (scan == 0) { scan = Win.MapVirtualKey((uint)value, 4); if (scan == 0xe11d) scan = 0xe046; }
        else if ((details & 0x1000000) != 0) scan |= 0xe000;
        else if (scan == 0x45) scan = 0xe046;
        var index = (int)((scan & 0x7f) | ((scan & 0xff00) != 0 ? 0x80u : 0));
        var scancode = (SDL.Scancode)WindowsScancodes[index];
        return DisplayServer.MapKeycode(SDL.GetKeyFromScancode(scancode, SDL.Keymod.None, false));
    }
}
