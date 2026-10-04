using Electron2D;
using SDL3;

internal static class DisplayServerKeyboardNativeTests
{
    public static void Run(DisplayServer display)
    {
        var previousModifiers = SDL.GetModState();
        try
        {
            SDL.SetModState(SDL.Keymod.None);
            Check(DisplayServer.KeyboardGetKeycodeFromPhysical(Key.None) == Key.None &&
                  DisplayServer.KeyboardGetLabelFromPhysical(Key.None) == Key.None,
                "No physical key maps to no logical key or label.");

            var unmapped = (Key)0x123456;
            var modifiedUnmapped = (Key)((int)unmapped | (int)KeyModifierMask.Control);
            Check(DisplayServer.KeyboardGetKeycodeFromPhysical(unmapped) == unmapped &&
                  DisplayServer.KeyboardGetLabelFromPhysical(modifiedUnmapped) == modifiedUnmapped,
                "An unmapped physical identity and its modifier bits survive both lookups.");

            var physical = Key.Key1;
            CheckMapping(display, physical, SDL.Scancode.Alpha1, SDL.Keymod.None);
            CheckMapping(display, Key.BraceLeft, SDL.Scancode.Leftbracket, SDL.Keymod.None);
            CheckMapping(display, Key.BraceRight, SDL.Scancode.Rightbracket, SDL.Keymod.None);
            CheckMapping(display, Key.Section, SDL.Scancode.Grave, SDL.Keymod.None);
            CheckMapping(display, Key.QuoteLeft, SDL.Scancode.NonUsBackSlash, SDL.Keymod.None);
            Check(DisplayServer.KeyboardGetKeycodeFromPhysical(Key.Shift) == Key.Shift &&
                  DisplayServer.KeyboardGetLabelFromPhysical(Key.Shift) == Key.Shift,
                "A physical Shift identity survives the native layout lookup.");
            SDL.SetModState(SDL.Keymod.LShift);
            CheckMapping(display, physical, SDL.Scancode.Alpha1, SDL.Keymod.LShift);

            var shiftPhysical = (Key)((int)physical | (int)KeyModifierMask.Shift);
            Check(((int)DisplayServer.KeyboardGetKeycodeFromPhysical(shiftPhysical) & (int)KeyModifierMask.Shift) != 0 &&
                  ((int)DisplayServer.KeyboardGetLabelFromPhysical(shiftPhysical) & (int)KeyModifierMask.Shift) != 0,
                "Both layout lookups preserve the supplied modifier bits.");

            var expectedEventLabel = PrintableKey(SDL.GetKeyFromScancode(SDL.Scancode.Alpha1,
                SDL.Keymod.LShift, false), false);
            if (expectedEventLabel != Key.None)
            {
                var windows = SDL.GetWindows(out var windowCount);
                Check(windowCount == 1 && windows is [var window] && window != 0,
                    "Keyboard event check needs the main native window.");
                var nativeWindowId = SDL.GetWindowID(windows![0]);
                Input.ReleasePressedEvents();
                try
                {
                    var key = new SDL.Event
                    {
                        Key = new SDL.KeyboardEvent
                        {
                            Type = SDL.EventType.KeyDown,
                            WindowID = nativeWindowId,
                            Scancode = SDL.Scancode.Alpha1,
                            Key = SDL.GetKeyFromScancode(SDL.Scancode.Alpha1, SDL.Keymod.LShift, true),
                            Mod = SDL.Keymod.LShift,
                            Down = true,
                        },
                    };
                    Check(SDL.PushEvent(ref key), "Native queue accepts a shifted number-row key press.");
                    DisplayServer.ProcessEvents();
                    Check(Input.IsKeyLabelPressed(expectedEventLabel),
                        "A key event derives its label using its own modifier state.");
                    key.Key.Type = SDL.EventType.KeyUp;
                    key.Key.Down = false;
                    Check(SDL.PushEvent(ref key), "Native queue accepts the shifted key release.");
                    DisplayServer.ProcessEvents();
                    Check(!Input.IsKeyLabelPressed(expectedEventLabel),
                        "The shifted key label is released.");

                    key.Key.Scancode = SDL.Scancode.Leftbracket;
                    key.Key.Key = SDL.GetKeyFromScancode(SDL.Scancode.Leftbracket, SDL.Keymod.None, true);
                    key.Key.Mod = SDL.Keymod.None;
                    key.Key.Type = SDL.EventType.KeyDown;
                    key.Key.Down = true;
                    Check(SDL.PushEvent(ref key), "Native queue accepts a physical brace-position press.");
                    DisplayServer.ProcessEvents();
                    Check(Input.IsPhysicalKeyPressed(Key.BraceLeft),
                        "The bracket-position SDL scancode uses its physical brace identity.");
                    key.Key.Type = SDL.EventType.KeyUp;
                    key.Key.Down = false;
                    Check(SDL.PushEvent(ref key), "Native queue accepts the brace-position release.");
                    DisplayServer.ProcessEvents();
                    Check(!Input.IsPhysicalKeyPressed(Key.BraceLeft),
                        "The physical brace identity is released.");
                }
                finally
                {
                    Input.ReleasePressedEvents();
                }
            }
        }
        finally
        {
            SDL.SetModState(previousModifiers);
        }
    }

    private static void CheckMapping(DisplayServer display, Key physical, SDL.Scancode scancode, SDL.Keymod modifiers)
    {
        var keycode = SDL.GetKeyFromScancode(scancode, modifiers, false);
        var label = SDL.GetKeyFromScancode(scancode, modifiers, false);
        var expectedKeycode = PrintableKey(keycode, true);
        var expectedLabel = PrintableKey(label, false);
        Check(DisplayServer.KeyboardGetKeycodeFromPhysical(physical) == (expectedKeycode == Key.None ? physical : expectedKeycode) &&
              DisplayServer.KeyboardGetLabelFromPhysical(physical) == (expectedLabel == Key.None ? physical : expectedLabel),
            $"Physical {physical} follows native scancode {scancode} and modifier state {modifiers}: native {keycode}, expected {expectedKeycode}, actual {DisplayServer.KeyboardGetKeycodeFromPhysical(physical)}.");
    }

    private static Key PrintableKey(SDL.Keycode code, bool logicalKeycode)
    {
        var scalar = (int)code;
        if (!System.Text.Rune.IsValid(scalar) || scalar < 0x20 ||
            logicalKeycode && scalar >= 0x80 && scalar is not 0xa5 and not 0xa7)
            return Key.None;
        return (Key)System.Text.Rune.ToUpperInvariant(new System.Text.Rune(scalar)).Value;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
