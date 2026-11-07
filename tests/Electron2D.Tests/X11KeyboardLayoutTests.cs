using System.Runtime.InteropServices;
using Electron2D;

internal static partial class X11KeyboardLayoutTests
{
    private const uint CoreKeyboard = 0x100, PhysicalA = 38;
    private const Key CyrillicA = (Key)0x0424;
    private static int _checks;

    internal static void Run(string profile)
    {
        if (profile == "unavailable")
        {
            using var display = DisplayServer.Open("X11 layout unavailable", new(320, 240), hidden: true);
            Reject<NotSupportedException>(() => DisplayServer.KeyboardGetLayoutCount());
            Reject<NotSupportedException>(() => DisplayServer.KeyboardGetCurrentLayout());
            Reject<NotSupportedException>(() => DisplayServer.KeyboardGetLayoutName(0));
            Reject<NotSupportedException>(() => DisplayServer.KeyboardGetLayoutLanguage(0));
            Reject<NotSupportedException>(() => DisplayServer.KeyboardSetCurrentLayout(0));
        }
        else
        {
            var fixture = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_X11_PRIVATE_DISPLAY");
            Check(!string.IsNullOrEmpty(fixture) && fixture == Environment.GetEnvironmentVariable("DISPLAY") &&
                Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "x11", "Use a private Xvfb display with the X11 SDL driver.");
            if (profile is "gpu" or "compatibility")
                Scene(profile);
            else
                Layouts();
        }

        Check(!DisplayServer.IsAvailable, "The display retired.");
        Console.WriteLine($"X11 keyboard layouts: {_checks} checks passed; mode={profile}.");
    }

    private static void Layouts()
    {
        Reject<InvalidOperationException>(() => DisplayServer.KeyboardGetLayoutCount());
        using var display = DisplayServer.Open("Electron2D XKB fixture", new(320, 240));
        Reject<InvalidOperationException>(() => Task.Run(DisplayServer.KeyboardGetLayoutCount).GetAwaiter().GetResult());
        var native = XOpenDisplay(0);
        Check(native != 0, "An independent Xlib client connects.");
        try
        {
            XSetInputFocus(native, (nuint)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle), 1, 0);
            XSync(native, 0);
            PumpUntil(() => DisplayServer.WindowIsFocused(), "X11 focus belongs to the owned fixture.");

            Check(DisplayServer.KeyboardGetLayoutCount() == 2, "Both XKB groups are listed.");
            Check(DisplayServer.KeyboardGetLayoutName(0) == "English (US)" &&
                  DisplayServer.KeyboardGetLayoutName(1) == "Russian", "Names come from the active XKB group atoms.");
            Check(DisplayServer.KeyboardGetLayoutLanguage(0) == "us" &&
                  DisplayServer.KeyboardGetLayoutLanguage(1) == "ru", "The language queries follow the active symbols.");
            Check(DisplayServer.KeyboardGetCurrentLayout() == ActiveGroup(native), "Both clients see the same active group.");
            foreach (var index in new[] { -1, 2, int.MaxValue })
            {
                Reject<ArgumentOutOfRangeException>(() => DisplayServer.KeyboardGetLayoutName(index));
                Reject<ArgumentOutOfRangeException>(() => DisplayServer.KeyboardGetLayoutLanguage(index));
                Reject<ArgumentOutOfRangeException>(() => DisplayServer.KeyboardSetCurrentLayout(index));
            }

            Select(native, 0, Key.A);
            Select(native, 1, CyrillicA);
            var controlA = (Key)((int)Key.A | (int)KeyModifierMask.Control);
            Check(DisplayServer.KeyboardGetLabelFromPhysical(controlA) == (Key)((int)CyrillicA | (int)KeyModifierMask.Control) &&
                DisplayServer.KeyboardGetKeycodeFromPhysical(controlA) == controlA, "Unicode labels and logical fallbacks preserve modifier bits.");
            Check(DisplayServer.KeyboardGetLabelFromPhysical(Key.F12) == Key.F12, "Special keys retain their typed identity.");

            const string Action = "x11-layout-held-key";
            InputMap.AddAction(Action);
            try
            {
                using var binding = new InputEventKey { KeyLabel = CyrillicA };
                InputMap.ActionAddEvent(Action, binding);
                SendKey(native, pressed: true);
                PumpUntil(() => Input.IsPhysicalKeyPressed(Key.A), "SDL delivers the native scancode.");
                Check(Input.IsKeyLabelPressed(CyrillicA) && Input.IsActionPressed(Action), "The native Cyrillic label holds its action.");
                Select(native, 0, Key.A);
                SendKey(native, pressed: false);
                PumpUntil(() => !Input.IsPhysicalKeyPressed(Key.A), "SDL delivers the native release.");
                Check(!Input.IsKeyLabelPressed(CyrillicA) && !Input.IsActionPressed(Action), "Release clears the previous group's label and action.");
            }
            finally { InputMap.EraseAction(Action); }

            Check(XkbLockGroup(native, CoreKeyboard, 1) != 0, "The independent X11 client changes the group.");
            XSync(native, 0);
            PumpUntil(() => DisplayServer.KeyboardGetLabelFromPhysical(Key.A) == CyrillicA, "The normal SDL pump observes external group changes.");
            Check(DisplayServer.KeyboardGetCurrentLayout() == 1, "An externally selected group is current.");
            Select(native, 0, Key.A);
        }
        finally
        {
            XTestFakeKeyEvent(native, PhysicalA, 0, 0);
            XkbLockGroup(native, CoreKeyboard, 0);
            XSync(native, 0);
            XCloseDisplay(native);
        }
    }

    private static void Select(nint native, int group, Key label)
    {
        DisplayServer.KeyboardSetCurrentLayout(group);
        Check(ActiveGroup(native) == group && DisplayServer.KeyboardGetCurrentLayout() == group, "Both connections observe the requested XKB group.");
        Check(DisplayServer.KeyboardGetLabelFromPhysical(Key.A) == label, "The physical label reflects the selected group before another pump.");
    }

    private static int ActiveGroup(nint native)
    {
        Span<byte> state = stackalloc byte[18];
        unsafe
        {
            fixed (byte* buffer = state)
                Check(XkbGetState(native, CoreKeyboard, (nint)buffer) == 0, "The independent client reads the native XKB state.");
        }
        return state[0];
    }

    private static void SendKey(nint native, bool pressed)
    {
        Check(XTestFakeKeyEvent(native, PhysicalA, pressed ? 1 : 0, 0) != 0, "XTEST accepts an event on the private server.");
        XSync(native, 0);
    }

    private static void PumpUntil(Func<bool> condition, string message)
    {
        var deadline = Environment.TickCount64 + 3000;
        while (!condition() && Environment.TickCount64 < deadline)
        {
            DisplayServer.ProcessEvents();
            Thread.Sleep(2);
        }
        Check(condition(), message);
    }

    private static void Scene(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var root = new Window { Size = new(320, 240), Title = "Electron2D XKB scene" };
        root.AddChild(new ColorRect { Size = new(320, 240), Color = Colors.Red });
        var frames = 0;
        root.Ready += _ => RenderingServer.FramePostDraw += Drawn;
        void Drawn()
        {
            if (++frames == 1)
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == backend, "The requested renderer owns the window.");
                DisplayServer.KeyboardSetCurrentLayout(1);
                Check(DisplayServer.KeyboardGetLabelFromPhysical(Key.A) == CyrillicA, "The scene selects its native keyboard group.");
            }
            else
            {
                using var image = root.GetTexture().GetImage();
                Check(image != null && image.GetPixel(8, 8) == Colors.Red, "The next rendered frame has the expected pixels.");
                DisplayServer.KeyboardSetCurrentLayout(0);
                RenderingServer.FramePostDraw -= Drawn;
                root.Tree!.Quit();
            }
        }
        Check(Engine.Run(root) == 0 && frames == 2 && !RenderingServer.IsAvailable, "The renderer and scene retire.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        _checks++;
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { _checks++; return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    [LibraryImport("libX11.so.6")] private static partial nint XOpenDisplay(nint name);
    [LibraryImport("libX11.so.6")] private static partial int XCloseDisplay(nint display);
    [LibraryImport("libX11.so.6")] private static partial int XSetInputFocus(nint display, nuint window, int revert, nuint time);
    [LibraryImport("libX11.so.6")] private static partial int XSync(nint display, int discard);
    [LibraryImport("libX11.so.6")] private static partial int XkbGetState(nint display, uint device, nint state);
    [LibraryImport("libX11.so.6")] private static partial int XkbLockGroup(nint display, uint device, uint group);
    [LibraryImport("libXtst.so.6")] private static partial int XTestFakeKeyEvent(nint display, uint keycode, int press, nuint delay);
}
