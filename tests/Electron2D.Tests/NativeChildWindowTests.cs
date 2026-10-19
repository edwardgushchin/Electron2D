using Electron2D;
using SDL = SDL3.SDL;

internal static partial class NativeChildWindowTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }

    internal static void Display()
    {
        using var display = DisplayServer.Open("Main", new(80, 80), hidden: true);
        using var first = display.CreateChildWindow("First", new(96, 80));
        using var second = display.CreateChildWindow("Second", new(112, 80));
        Check(ReferenceEquals(DisplayServer.Service, display), "Child contexts preserve process service identity.");
        Check(first.WindowID > 0 && second.WindowID > first.WindowID, "Native child identities are distinct and monotonic.");
        Check(DisplayServer.GetWindowList().SequenceEqual(new[] { 0, first.WindowID, second.WindowID }), "Public window census.");
        DisplayServer.WindowSetTitle("Changed", first.WindowID);
        Check(display.WindowGetTitle(first.WindowID) == "Changed" && display.WindowGetTitle(second.WindowID) == "Second" && display.WindowGetTitle() == "Main", "Titles route to their native context.");
        DisplayServer.WindowSetMinSize(new(40, 50), first.WindowID);
        DisplayServer.WindowSetMaxSize(new(180, 190), first.WindowID);
        Check(DisplayServer.WindowGetMinSize(first.WindowID) == new Vector2i(40, 50) && DisplayServer.WindowGetMinSize(second.WindowID) == Vector2i.Zero, "Minimums are independent.");
        Check(DisplayServer.WindowGetMaxSize(first.WindowID) == new Vector2i(180, 190) && DisplayServer.WindowGetMaxSize(second.WindowID) == Vector2i.Zero, "Maximums are independent.");
        Task.Run(() => Reject<InvalidOperationException>(() => DisplayServer.WindowGetSize(first.WindowID))).GetAwaiter().GetResult();
        Task.Run(() => Reject<InvalidOperationException>(() => DisplayServer.WindowGetCurrentScreen(first.WindowID))).GetAwaiter().GetResult();
        Events(display, first, second);
        var retired = first.WindowID;
        first.Dispose();
        Reject<ArgumentOutOfRangeException>(() => display.WindowGetTitle(retired));
        Check(DisplayServer.WindowGetCurrentScreen(retired) == DisplayServer.InvalidScreen, "Retired screen query keeps its invalid-screen policy.");
        Check(DisplayServer.GetWindowList().SequenceEqual(new[] { 0, second.WindowID }), "Retirement removes only its own native identity.");
        using var replacement = display.CreateChildWindow("Replacement", new(80, 80));
        Check(replacement.WindowID > second.WindowID && display.WindowGetTitle() == "Main", "Recreation does not recycle stale native identities or replace main.");
        Console.WriteLine("Native child display identity, metadata, owner and retirement passed.");
    }

    private sealed class Receiver : Node
    {
        internal readonly List<long> Keys = [];
        internal readonly List<(long Window, float Relative)> Motion = [];
        internal readonly List<(long Window, int Index)> Touches = [];
        internal int FocusOut;
        internal Receiver() { InputEnabled = true; }
        protected override void OnInput(InputEvent input)
        {
            if (input is InputEventKey { Pressed: true } key) Keys.Add(key.WindowID);
            if (input is InputEventMouseMotion mouse) Motion.Add((mouse.WindowID, mouse.Relative.X));
            if (input is InputEventScreenTouch { Pressed: true } touch) Touches.Add((touch.WindowID, touch.Index));
        }
        protected override void OnNotification(int what)
        { base.OnNotification(what); if (what == MainLoop.NotificationApplicationFocusOut) FocusOut++; }
    }

    private static uint NativeID(DisplayServer context)
    {
        var handle = context.AcquireRenderingWindow();
        try { return SDL.GetWindowID(handle.DangerousGetHandle()); }
        finally { context.ReleaseRenderingWindow(); }
    }

    private static void Events(DisplayServer display, DisplayServer first, DisplayServer second)
    {
        using var receiver = new Receiver(); using var tree = new SceneTree(receiver);
        var accumulated = Input.UseAccumulatedInput; Input.UseAccumulatedInput = true;
        var emulateMouse = Input.EmulateMouseFromTouch; Input.EmulateMouseFromTouch = false;
        Engine.Start(tree);
        try
        {
            DisplayServer.ProcessEvents(); receiver.Keys.Clear(); receiver.Motion.Clear();
            var ids = new[] { NativeID(display), NativeID(first), NativeID(second) };
            foreach (var id in ids)
            {
                var key = new SDL.Event { Key = new SDL.KeyboardEvent { Type = SDL.EventType.KeyDown, WindowID = id, Key = SDL.Keycode.A, Scancode = SDL.Scancode.A, Down = true } };
                Check(SDL.PushEvent(ref key), "Queue native key.");
            }
            DisplayServer.ProcessEvents();
            Check(receiver.Keys.SequenceEqual(new long[] { 0, first.WindowID, second.WindowID }), "Native events retain the correct public window identity.");
            foreach (var (index, relative) in new[] { (1, 2f), (1, 3f), (2, 7f), (1, 11f) })
            {
                var motion = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = ids[index], Which = 1, X = 20, Y = 20, XRel = relative } };
                Check(SDL.PushEvent(ref motion), "Queue native motion.");
            }
            DisplayServer.ProcessEvents();
            var firstScale = SDL.GetCurrentVideoDriver() == "wayland" ? SDL.GetWindowPixelDensity(SDL.GetWindowFromID(ids[1])) : 1f;
            var secondScale = SDL.GetCurrentVideoDriver() == "wayland" ? SDL.GetWindowPixelDensity(SDL.GetWindowFromID(ids[2])) : 1f;
            Check(receiver.Motion.SequenceEqual(new[] { ((long)first.WindowID, 5f * firstScale), ((long)second.WindowID, 7f * secondScale), ((long)first.WindowID, 11f * firstScale) }), "Accumulated native motion flushes on a window boundary, applies its density and retains event order: " + string.Join(", ", receiver.Motion));
            foreach (var index in new[] { 1, 2 })
            {
                var touch = new SDL.Event { TFinger = new SDL.TouchFingerEvent { Type = SDL.EventType.FingerDown, WindowID = ids[index], TouchID = (ulong)index, FingerID = 1, X = .25f, Y = .25f, Pressure = 1 } };
                Check(SDL.PushEvent(ref touch), "Queue concurrent native contacts.");
            }
            DisplayServer.ProcessEvents();
            Check(receiver.Touches.SequenceEqual(new[] { ((long)first.WindowID, 0), ((long)second.WindowID, 1) }), "Concurrent windows share distinct active touch indexes.");
            var lost = new SDL.Event { Window = new SDL.WindowEvent { Type = SDL.EventType.WindowFocusLost, WindowID = ids[1] } };
            var replacement = new SDL.Event { TFinger = new SDL.TouchFingerEvent { Type = SDL.EventType.FingerDown, WindowID = ids[0], TouchID = 3, FingerID = 1, X = .25f, Y = .25f, Pressure = 1 } };
            Check(SDL.PushEvent(ref lost) && SDL.PushEvent(ref replacement), "Queue focus retirement and another window contact.");
            DisplayServer.ProcessEvents();
            Check(receiver.Touches[^1] == (0L, 0), "Focus retirement returns only that window's contact indexes to the shared pool.");
            Check(receiver.FocusOut == 1, "The first application focus-out event is delivered even without a preceding focus-in event.");
            using var temporary = display.CreateChildWindow("Pressed input retirement", new(80, 80));
            var pressed = new SDL.Event { Key = new SDL.KeyboardEvent { Type = SDL.EventType.KeyDown, WindowID = NativeID(temporary), Key = SDL.Keycode.B, Scancode = SDL.Scancode.B, Down = true } };
            Check(SDL.PushEvent(ref pressed), "Queue held input from a temporary native lifetime."); DisplayServer.ProcessEvents();
            Check(Input.IsKeyPressed(Key.B), "The native key enters global pressed state.");
            temporary.Dispose(); Check(!Input.IsKeyPressed(Key.B), "Retiring a native input source cannot leave its held key stuck.");
        }
        finally { Engine.Stop(); Input.UseAccumulatedInput = accumulated; Input.EmulateMouseFromTouch = emulateMouse; }
    }
}
