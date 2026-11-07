using System.Runtime.InteropServices;
using System.Text;
using Electron2D;
using SDL3;

internal static unsafe partial class WindowsKeyboardLayoutTests
{
    private static int _checks;

    internal static void Run(string profile)
    {
        Reject<InvalidOperationException>(() => DisplayServer.KeyboardGetLayoutCount());
        Reject<InvalidOperationException>(() => DisplayServer.KeyboardSetCurrentLayout(0));
        if (profile is "gpu" or "compatibility")
            Scene(profile);
        else
        {
            using var display = DisplayServer.Open("Electron2D keyboard layout fixture", new(320, 240), hidden: true);
            Reject<InvalidOperationException>(() => Task.Run(DisplayServer.KeyboardGetCurrentLayout).GetAwaiter().GetResult());
            if (profile == "unavailable")
            {
                Reject<NotSupportedException>(() => DisplayServer.KeyboardGetLayoutCount());
                Reject<NotSupportedException>(() => DisplayServer.KeyboardGetCurrentLayout());
                Reject<NotSupportedException>(() => DisplayServer.KeyboardGetLayoutLanguage(0));
                Reject<NotSupportedException>(() => DisplayServer.KeyboardGetLayoutName(0));
                Reject<NotSupportedException>(() => DisplayServer.KeyboardSetCurrentLayout(0));
            }
            else
                CheckLayouts(deliverKey: true);
        }
        Check(!DisplayServer.IsAvailable, "The display retires.");
        Console.WriteLine($"Windows keyboard layouts: {_checks} checks passed; mode={profile}.");
    }

    private static void CheckLayouts(bool deliverKey)
    {
        var original = GetKeyboardLayout(0);
        var layouts = NativeLayouts();
        Check(layouts.Length >= 2 && Array.IndexOf(layouts, original) >= 0, "The fixture needs two loaded layouts and an active initial layout.");
        Check(DisplayServer.KeyboardGetLayoutCount() == layouts.Length, "The public count is the native count.");
        foreach (var index in new[] { -1, layouts.Length, int.MaxValue })
        {
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.KeyboardGetLayoutName(index));
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.KeyboardGetLayoutLanguage(index));
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.KeyboardSetCurrentLayout(index));
        }
        Check(GetKeyboardLayout(0) == original, "Invalid requests do not change the layout.");
        var modified = (Key)((int)Key.A | (int)KeyModifierMask.Control);
        var labels = new HashSet<Key>();
        try
        {
            foreach (var layout in layouts)
            {
                var index = Array.IndexOf(NativeLayouts(), layout);
                Check(index >= 0, "The requested layout is still loaded.");
                var previous = GetKeyboardLayout(0);
                var name = DisplayServer.KeyboardGetLayoutName(index);
                var language = DisplayServer.KeyboardGetLayoutLanguage(index);
                Check(!string.IsNullOrWhiteSpace(name), "A Windows layout has a friendly name.");
                Check(language.Length >= 2, "A Windows input locale has a language tag.");
                Check(GetKeyboardLayout(0) == previous, "Queries never activate an inactive layout.");
                DisplayServer.KeyboardSetCurrentLayout(index);
                Check(GetKeyboardLayout(0) == layout, "Activation changes the actual owner-thread input locale.");
                Check(DisplayServer.KeyboardGetCurrentLayout() == Array.IndexOf(NativeLayouts(), layout), "Current index resolves in the current list.");
                Check(DisplayServer.KeyboardGetLayoutCount() == layouts.Length, "Switching does not load or unload layouts.");
                var label = ExpectedLetter(layout);
                if (label == Key.None)
                    continue;
                labels.Add(label);
                Check(DisplayServer.KeyboardGetLabelFromPhysical(Key.A) == label, "The live SDL label follows native translation without another pump.");
                var logical = (uint)label <= 127 ? label : Key.A;
                Check(DisplayServer.KeyboardGetKeycodeFromPhysical(Key.A) == logical, "The public logical mapping distinguishes the label and shortcut identity.");
                Check(DisplayServer.KeyboardGetLabelFromPhysical(modified) == (Key)((int)label | (int)KeyModifierMask.Control), "Lookup retains the supplied modifier bits.");
                Check(DisplayServer.KeyboardGetLabelFromPhysical(Key.F12) == Key.F12, "Special keys retain their typed identity.");
                if (deliverKey)
                    KeyEvent(label);
                Console.WriteLine($"Native layout {language} ({name}) maps physical A to U+{(uint)label:X4}.");
            }
            Check(labels.Count >= 2, "Distinct native layouts produce distinct labels.");
            if (deliverKey)
                HeldAcrossLayoutChange(layouts);
        }
        finally
        {
            var index = Array.IndexOf(NativeLayouts(), original);
            if (index >= 0)
                DisplayServer.KeyboardSetCurrentLayout(index);
        }
        Check(GetKeyboardLayout(0) == original, "The previous input layout is restored.");
    }

    private static void Scene(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var root = new Window { Size = new(320, 240), Title = "Electron2D keyboard layout scene" };
        root.AddChild(new ColorRect { Size = new(320, 240), Color = Colors.Red });
        var frames = 0;
        root.Ready += _ => RenderingServer.FramePostDraw += Drawn;
        void Drawn()
        {
            if (++frames == 1)
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == backend, "The requested renderer owns the window.");
                CheckLayouts(deliverKey: false);
            }
            else
            {
                using var image = root.GetTexture().GetImage();
                Check(image != null && image.GetPixel(8, 8) == Colors.Red, "The next native frame produces the expected pixels.");
                RenderingServer.FramePostDraw -= Drawn;
                root.Tree!.Quit();
            }
        }
        Check(Engine.Run(root) == 0 && frames == 2 && !RenderingServer.IsAvailable, "The native renderer retires.");
    }

    private static Key ExpectedLetter(nint layout)
    {
        const uint ScanA = 0x1E;
        var state = stackalloc byte[256];
        new Span<byte>(state, 256).Clear();
        var chars = stackalloc char[8];
        var vk = MapVirtualKeyExW(ScanA, 3, layout);
        var written = ToUnicodeEx(vk, ScanA, state, chars, 8, 4, layout);
        return written == 1 && Rune.TryCreate(chars[0], out var rune) ? (Key)Rune.ToUpperInvariant(rune).Value : Key.None;
    }

    private static void HeldAcrossLayoutChange(nint[] layouts)
    {
        var before = GetKeyboardLayout(0);
        var label = ExpectedLetter(before);
        var after = layouts.First(layout => ExpectedLetter(layout) is var next && next != Key.None && next != label);
        const string Action = "windows-layout-held-key";
        InputMap.AddAction(Action);
        try
        {
            using var binding = new InputEventKey { KeyLabel = label };
            InputMap.ActionAddEvent(Action, binding);
            KeyEvent(pressed: true);
            Check(Input.IsPhysicalKeyPressed(Key.A) && Input.IsKeyLabelPressed(label) && Input.IsActionPressed(Action),
                "The previous layout's label binding holds.");
            DisplayServer.KeyboardSetCurrentLayout(Array.IndexOf(NativeLayouts(), after));
            Check(DisplayServer.KeyboardGetLabelFromPhysical(Key.A) == ExpectedLetter(after), "The new layout is live while a key is held.");
            KeyEvent(pressed: false);
            Check(!Input.IsPhysicalKeyPressed(Key.A) && !Input.IsKeyLabelPressed(label) && !Input.IsActionPressed(Action),
                "A release after switching retires the previous label and action.");
        }
        finally
        {
            InputMap.EraseAction(Action);
            Input.ReleasePressedEvents();
        }
    }

    private static void KeyEvent(Key label)
    {
        KeyEvent(pressed: true);
        Check(Input.IsPhysicalKeyPressed(Key.A) && Input.IsKeyLabelPressed(label), "The display delivers the physical identity and refreshed label.");
        KeyEvent(pressed: false);
        Check(!Input.IsPhysicalKeyPressed(Key.A) && !Input.IsKeyLabelPressed(label), "Release retires both identities.");
    }

    private static void KeyEvent(bool pressed)
    {
        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is [not 0], "The test SDL event targets its own main window.");
        var key = new SDL.Event
        {
            Key = new SDL.KeyboardEvent
            {
                Type = pressed ? SDL.EventType.KeyDown : SDL.EventType.KeyUp,
                Down = pressed,
                WindowID = SDL.GetWindowID(windows![0]),
                Scancode = SDL.Scancode.A,
                Key = SDL.GetKeyFromScancode(SDL.Scancode.A, SDL.Keymod.None, true),
            },
        };
        Check(SDL.PushEvent(ref key), "The native queue accepts the test key event.");
        DisplayServer.ProcessEvents();
    }

    private static nint[] NativeLayouts()
    {
        var layouts = new nint[GetKeyboardLayoutList(0, null)];
        fixed (nint* buffer = layouts)
            if (GetKeyboardLayoutList(layouts.Length, buffer) != layouts.Length)
                throw new InvalidOperationException("The native test inventory changed during enumeration.");
        return layouts;
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

    [LibraryImport("user32.dll")] private static partial int GetKeyboardLayoutList(int count, nint* values);
    [LibraryImport("user32.dll")] private static partial nint GetKeyboardLayout(uint thread);
    [LibraryImport("user32.dll")] private static partial uint MapVirtualKeyExW(uint code, uint type, nint layout);
    [LibraryImport("user32.dll")] private static partial int ToUnicodeEx(uint key, uint scan, byte* state, char* text, int size, uint flags, nint layout);
}
