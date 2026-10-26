using Electron2D;
using SDL3;

internal static class WindowInputRegionTests
{
    private static int _checks;
    private static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void Managed()
    {
        using var window = new Window();
        Check(!window.MousePassthrough && window.MousePassthroughPolygon.Length == 0 && !window.GetFlag(WindowFlag.MousePassthrough), "Default native input is unrestricted.");
        window.MousePassthrough = true; Check(window.GetFlag(WindowFlag.MousePassthrough), "The property and flag use the same authored state.");
        Vector2[] polygon = [new(2, 3), new(70, 4), new(65, 80)]; window.MousePassthroughPolygon = polygon;
        polygon[0] = new(9, 9); var copy = window.MousePassthroughPolygon; copy[1] = Vector2.Zero;
        Check(window.MousePassthroughPolygon[0] == new Vector2(2, 3) && window.MousePassthroughPolygon[1] == new Vector2(70, 4), "Polygon storage copies input and output.");
        Reject<ArgumentNullException>(() => window.MousePassthroughPolygon = null!);
        foreach (var value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity }) Reject<ArgumentOutOfRangeException>(() => window.MousePassthroughPolygon = [new(value, 0)]);
        Check(window.MousePassthroughPolygon.Length == 3, "Failed edits preserve the authored polygon.");
        using var duplicate = (Window)window.Duplicate(); using var scene = new PackedScene(); scene.Pack(window); using var instance = (Window)scene.Instantiate();
        foreach (var item in new[] { duplicate, instance }) Check(item.MousePassthrough && item.MousePassthroughPolygon.AsSpan().SequenceEqual(window.MousePassthroughPolygon), "Scene copying preserves the complete input policy.");
        window.MousePassthrough = false; window.MousePassthroughPolygon = [];
        Check(duplicate.MousePassthrough && instance.MousePassthroughPolygon.Length == 3, "Copies retain independent authored state.");
        Reject<InvalidOperationException>(() => DisplayServer.WindowSetMousePassthrough([]));
        window.Dispose(); Reject<ObjectDisposedException>(() => window.MousePassthrough = true); Reject<ObjectDisposedException>(() => window.MousePassthroughPolygon = []);
        using (var host = new SubViewport { Size = new(200, 120), GUIEmbedSubwindows = true })
        {
            var embedded = new Window { Borderless = true, Position = new(20, 20), Size = new(100, 80), MousePassthrough = true, MousePassthroughPolygon = [Vector2.Zero] }; host.AddChild(embedded); using var tree = new SceneTree(host);
            var calls = 0; embedded.WindowInput += _ => calls++; using var click = new InputEventMouseButton { Position = new(35, 35), ButtonIndex = MouseButton.Left, Pressed = true }; host.PushInput(click, true);
            Check(calls == 1, "Stored native input policies do not filter embedded GUI reception.");
        }
        Console.WriteLine($"PASS {_checks} managed native-window input policy, copying contracts.");
    }
    internal static void Queue()
    {
        using var display = DisplayServer.Open("Input mask queue", new(64, 64), hidden: true);
        using var child = display.CreateChildWindow("Input mask child", new(64, 64));
        static uint NativeID(DisplayServer context)
        {
            var window = context.AcquireRenderingWindow();
            try { return SDL.GetWindowID(window.DangerousGetHandle()); }
            finally { context.ReleaseRenderingWindow(); }
        }
        var mainID = NativeID(display); var childID = NativeID(child);
        DisplayServer.ProcessEvents();
        var motionCount = 0; var motionMask = MouseButtonMask.Left; var releaseSource = -1L;
        DisplayServer.GetWindowEvents(child.WindowID).Input += e => { if (e is InputEventMouseMotion motion) { motionCount++; motionMask = motion.ButtonMask; } };
        DisplayServer.GetWindowEvents(DisplayServer.MainWindowId).Input += e => { if (e is InputEventMouseButton { Pressed: false } button) releaseSource = button.WindowID; };
        var press = new SDL.Event { Button = new SDL.MouseButtonEvent { Type = SDL.EventType.MouseButtonDown, WindowID = childID, Button = 1, Down = true, X = 10, Y = 10 } };
        var release = press; release.Button.Type = SDL.EventType.MouseButtonUp; release.Button.WindowID = mainID; release.Button.Down = false;
        var movement = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = childID, X = 20, Y = 20, XRel = 10, YRel = 10 } };
        Check(SDL.PushEvent(ref press) && SDL.PushEvent(ref release) && SDL.PushEvent(ref movement), "The native queue accepts a cross-window release without a focus-loss event.");
        DisplayServer.ProcessEvents();
        Check(releaseSource == DisplayServer.MainWindowId && !Input.IsMouseButtonPressed(MouseButton.Left), "The native release keeps its actual destination and clears shared polling.");
        Check(motionCount == 1 && motionMask == MouseButtonMask.None, "Later motion in the former press owner has no stale button mask.");
        Console.WriteLine($"PASS {_checks} native queue release/mask contracts.");
    }
}
