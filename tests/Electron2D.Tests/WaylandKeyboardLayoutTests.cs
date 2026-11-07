using System.Runtime.InteropServices;
using Electron2D;

internal static partial class WaylandKeyboardLayoutTests
{
    private const uint CoreKeyboard = 0x100;
    private const uint PhysicalA = 38;
    private const uint PhysicalKeypad1 = 87;
    private const uint NumLock = 0xff7f;
    private const Key CyrillicA = (Key)0x0424;

    internal static void Run()
    {
        var wayland = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_WAYLAND_PRIVATE_DISPLAY");
        var x11 = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_X11_PRIVATE_DISPLAY");
        Check(!string.IsNullOrEmpty(wayland) && wayland == Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") &&
            !string.IsNullOrEmpty(x11) && x11 == Environment.GetEnvironmentVariable("DISPLAY") &&
            Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "wayland",
            "Use a private nested Wayland compositor on a private Xvfb display.");
        Reject<InvalidOperationException>(() => DisplayServer.KeyboardGetLayoutCount());

        var actor = XOpenDisplay(0);
        Check(actor != 0, "An independent X11 client connects to the compositor's X server.");
        try
        {
            Layouts(actor);
            using var reopened = DisplayServer.Open("Wayland keyboard reopen", new(320, 240), hidden: true);
            Check(DisplayServer.KeyboardGetLayoutCount() == 2, "A fresh Wayland display receives its own keymap.");
        }
        finally
        {
            XTestFakeKeyEvent(actor, PhysicalA, 0, 0);
            XTestFakeKeyEvent(actor, PhysicalKeypad1, 0, 0);
            XCloseDisplay(actor);
        }
        Check(!DisplayServer.IsAvailable, "The Wayland display retires.");
        Console.WriteLine("Wayland keyboard layouts, physical conversions, held-key retirement and live XKB keypad input passed.");
    }

    private static void Layouts(nint actor)
    {
        using var display = DisplayServer.Open("Wayland keyboard fixture", new(320, 240));
        PumpUntil(() => DisplayServer.KeyboardGetLayoutCount() == 2, "Both compositor keymap groups are available.");
        Check(DisplayServer.KeyboardGetLayoutName(0) == "English (US)" && DisplayServer.KeyboardGetLayoutName(1) == "Russian",
            "Layout names come from the delivered keymap.");
        Check(DisplayServer.KeyboardGetLayoutLanguage(0) == "English (US)" && DisplayServer.KeyboardGetLayoutLanguage(1) == "Russian",
            "Wayland exposes the keymap group names as language descriptions.");
        Reject<ArgumentOutOfRangeException>(() => DisplayServer.KeyboardGetLayoutName(-1));
        Reject<ArgumentOutOfRangeException>(() => DisplayServer.KeyboardGetLayoutLanguage(2));
        Reject<InvalidOperationException>(() => Task.Run(DisplayServer.KeyboardGetCurrentLayout).GetAwaiter().GetResult());
        Reject<NotSupportedException>(() => DisplayServer.KeyboardSetCurrentLayout(1));

        Select(actor, 0);
        Check(DisplayServer.KeyboardGetLabelFromPhysical(Key.A) == Key.A, "The first group has the Latin label.");
        Select(actor, 1);
        Check(DisplayServer.KeyboardGetLabelFromPhysical(Key.A | (Key)KeyModifierMask.Control) == (CyrillicA | (Key)KeyModifierMask.Control) &&
            DisplayServer.KeyboardGetKeycodeFromPhysical(Key.A | (Key)KeyModifierMask.Control) == (Key.A | (Key)KeyModifierMask.Control),
            "The Cyrillic label and logical fallback preserve modifier bits.");
        Check(DisplayServer.KeyboardGetLabelFromPhysical(Key.F12) == Key.F12, "The layout preserves a function key identity.");

        const string action = "wayland-layout-held-key";
        InputMap.AddAction(action);
        try
        {
            using var binding = new InputEventKey { KeyLabel = CyrillicA };
            InputMap.ActionAddEvent(action, binding);
            Send(actor, PhysicalA, true);
            PumpUntil(() => Input.IsPhysicalKeyPressed(Key.A), "The compositor delivers the actual physical press.");
            Check(Input.IsKeyPressed(Key.A) && Input.IsKeyLabelPressed(CyrillicA) && Input.IsActionPressed(action),
                "The native Cyrillic label reaches the Input action.");
            Select(actor, 0);
            Send(actor, PhysicalA, false);
            PumpUntil(() => !Input.IsPhysicalKeyPressed(Key.A), "The compositor delivers the physical release.");
            Check(!Input.IsKeyLabelPressed(CyrillicA) && !Input.IsActionPressed(action), "The release clears the previous group's held label.");

            Select(actor, 1);
            var labels = new List<Key>();
            DisplayServer.GetWindowEvents().Input += ObserveA;
            try
            {
                Send(actor, PhysicalA, true);
                Check(XkbLockGroup(actor, CoreKeyboard, 0) != 0, "The group changes while the press waits in the queue.");
                Send(actor, PhysicalA, false);
                Thread.Sleep(40);
                PumpUntil(() => labels.Count >= 2, "The display delivers the queued key pair.");
                Check(labels.SequenceEqual(new[] { CyrillicA, CyrillicA }), "Queued press and release preserve the label from before the group change.");
            }
            finally { DisplayServer.GetWindowEvents().Input -= ObserveA; }

            void ObserveA(InputEvent input)
            {
                if (input is InputEventKey { PhysicalKeycode: Key.A, Echo: false } key)
                    labels.Add(key.KeyLabel);
            }
        }
        finally { InputMap.EraseAction(action); }

        var mask = XkbKeysymToModifiers(actor, NumLock);
        Check(mask != 0, "XKB supplies the Num Lock modifier mask.");
        SetNumLock(false, Key.End, Key.End);
        SetNumLock(true, Key.Keypad1, Key.Key1);
        const uint LeftShift = 50;
        Send(actor, LeftShift, true);
        PumpUntil(() => DisplayServer.KeyboardGetKeycodeFromPhysical(Key.Keypad1) == Key.End,
            "Shift selects keypad navigation while Num Lock is active.");
        CheckKeypad(Key.End, Key.End);
        Send(actor, LeftShift, false);
        PumpUntil(() => !Input.IsPhysicalKeyPressed(Key.Shift), "The Shift release clears physical state.");
        SetNumLock(false, Key.End, Key.End);

        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_WAYLAND_EXTENDED_KEYS") == "1")
        {
            for (uint raw = 203; raw <= 213; raw++)
            {
                var function = (Key)((int)Key.F25 + raw - 203);
                Check(DisplayServer.KeyboardGetKeycodeFromPhysical(function) == function &&
                    DisplayServer.KeyboardGetLabelFromPhysical(function) == function,
                    $"The delivered map supports {function}.");
                Send(actor, raw, true);
                PumpUntil(() => Input.IsPhysicalKeyPressed(function), $"The native event carries physical {function}.");
                Check(Input.IsKeyPressed(function) && Input.IsKeyLabelPressed(function), $"The native event carries logical {function}.");
                Send(actor, raw, false);
                PumpUntil(() => !Input.IsPhysicalKeyPressed(function), $"The native release clears {function}.");
            }
            Console.WriteLine("Wayland F25–F35 public conversions and physical input passed.");
        }

        void SetNumLock(bool enabled, Key code, Key label)
        {
            Check(XkbLockModifiers(actor, CoreKeyboard, mask, enabled ? mask : 0) != 0,
                "The independent client toggles the Num Lock state.");
            XSync(actor, 0);
            PumpUntil(() => DisplayServer.KeyboardGetKeycodeFromPhysical(Key.Keypad1) == code,
                $"The live XKB keypad conversion observes Num Lock={enabled}.");
            CheckKeypad(code, label);
        }

        void CheckKeypad(Key code, Key label)
        {
            Check(DisplayServer.KeyboardGetLabelFromPhysical(Key.Keypad1) == label, "The keypad label follows the active symbol.");
            Send(actor, PhysicalKeypad1, true);
            PumpUntil(() => Input.IsPhysicalKeyPressed(Key.Keypad1), "The compositor delivers the keypad press.");
            Check(Input.IsKeyPressed(code) && Input.IsKeyLabelPressed(label), "The actual keypad event matches the public conversions.");
            Send(actor, PhysicalKeypad1, false);
            PumpUntil(() => !Input.IsPhysicalKeyPressed(Key.Keypad1), "The compositor delivers the keypad release.");
            Check(!Input.IsKeyPressed(code) && !Input.IsKeyLabelPressed(label), "The keypad release leaves no held state.");
        }
    }

    private static void Select(nint actor, uint group)
    {
        Check(XkbLockGroup(actor, CoreKeyboard, group) != 0, "The independent client selects a group.");
        XSync(actor, 0);
        PumpUntil(() => DisplayServer.KeyboardGetCurrentLayout() == (int)group, $"Wayland observes XKB group {group}.");
    }

    private static void Send(nint actor, uint key, bool down)
    {
        Check(XTestFakeKeyEvent(actor, key, down ? 1 : 0, 0) != 0, "XTEST sends a keyboard event.");
        XSync(actor, 0);
    }

    private static void PumpUntil(Func<bool> ready, string message)
    {
        var deadline = Environment.TickCount64 + 4000;
        while (!ready() && Environment.TickCount64 < deadline)
        {
            DisplayServer.ProcessEvents();
            Thread.Sleep(2);
        }
        Check(ready(), message);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    [LibraryImport("libX11.so.6")] private static partial nint XOpenDisplay(nint name);
    [LibraryImport("libX11.so.6")] private static partial int XCloseDisplay(nint display);
    [LibraryImport("libX11.so.6")] private static partial int XSync(nint display, int discard);
    [LibraryImport("libX11.so.6")] private static partial int XkbLockGroup(nint display, uint device, uint group);
    [LibraryImport("libX11.so.6")] private static partial int XkbLockModifiers(nint display, uint device, uint affect, uint values);
    [LibraryImport("libX11.so.6")] private static partial uint XkbKeysymToModifiers(nint display, nuint symbol);
    [LibraryImport("libXtst.so.6")] private static partial int XTestFakeKeyEvent(nint display, uint key, int press, nuint delay);
}
