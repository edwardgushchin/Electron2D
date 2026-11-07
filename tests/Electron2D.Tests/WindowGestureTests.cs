using System.Runtime.InteropServices;
using Electron2D;
using Edge = Electron2D.DisplayServer.WindowResizeEdge;

internal static partial class WindowGestureTests
{
    private sealed class ChromeButton : BaseButton { }

    private static readonly (Edge Edge, int X, int Y)[] Directions =
    [
        (Edge.TopLeft, -1, -1), (Edge.Top, 0, -1), (Edge.TopRight, 1, -1),
        (Edge.Left, -1, 0), (Edge.Right, 1, 0),
        (Edge.BottomLeft, -1, 1), (Edge.Bottom, 0, 1), (Edge.BottomRight, 1, 1),
    ];

    internal static void Run(string profile)
    {
        if (profile == "embedded") { Embedded(); return; }
        Reject<InvalidOperationException>(() => DisplayServer.WindowStartDrag());
        if (profile == "unavailable")
        {
            using var display = DisplayServer.Open("Gesture adapter unavailable", new(320, 240));
            Reject<NotSupportedException>(() => DisplayServer.WindowStartDrag());
            Reject<NotSupportedException>(() => DisplayServer.WindowStartResize(Edge.TopLeft));
            return;
        }

        var fixture = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_X11_PRIVATE_DISPLAY");
        Check(!string.IsNullOrEmpty(fixture) && fixture == Environment.GetEnvironmentVariable("DISPLAY") &&
            Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "x11", "Use a private Xvfb display with SDL X11.");
        var actor = XOpenDisplay(0);
        Check(actor != 0, "An independent client connects to the private X server.");
        try
        {
            if (profile == "scene") NativeScene(actor);
            else NativeDisplay(actor, profile == "no-manager");
        }
        finally { XTestFakeButtonEvent(actor, 1, 0, 0); XSync(actor, 0); XCloseDisplay(actor); }
        Check(!DisplayServer.IsAvailable, "The native display retires after the test.");
        Console.WriteLine($"Native window gestures passed ({profile}).");
    }

    internal static void Embedded()
    {
        using var tree = new SceneTree(new SubViewport { Size = new(720, 560), GUIEmbedSubwindows = true });
        var host = (SubViewport)tree.Root;
        var window = new Window { Borderless = true, Visible = false, MinSize = new(100, 80), MaxSize = new(160, 140) };
        var chrome = new ChromeButton { Size = new(110, 70) };
        window.AddChild(chrome);
        host.AddChild(window);
        var initial = new Vector2i(280, 220);
        var size = new Vector2i(130, 110);
        var point = (Vector2)initial + new Vector2(60, 40);
        window.Position = initial;
        window.Size = size;
        window.Show();
        var clicks = 0;
        var ups = 0;
        chrome.Pressed += () => clicks++;
        chrome.ButtonUp += () => ups++;
        Action? start = window.StartDrag;
        chrome.ButtonDown += () => start?.Invoke();

        Reject<ArgumentOutOfRangeException>(() => window.StartResize(Edge.Max));
        Reject<ArgumentOutOfRangeException>(() => window.StartResize((Edge)(-1)));
        window.StartDrag();
        MouseMove(host, point + new Vector2(20, 15));
        Check(window.Position == initial, "Without a client press the window stays in place.");

        MouseButtonAt(host, point, true);
        var offset = new Vector2i(32, 24);
        MouseMove(host, point + (Vector2)offset);
        MouseButtonAt(host, point + (Vector2)offset, false);
        Check(window.Position == initial + offset && window.Size == size && clicks == 0 && ups == 1,
            "Borderless client chrome moves its window and releases the GUI button without a click.");
        MouseMove(host, point + new Vector2(100, 110));
        Check(window.Position == initial + offset, "Movement ends with release.");

        foreach (var (edge, x, y) in Directions)
        {
            start = () => window.StartResize(edge);
            window.Position = initial;
            window.Size = size;
            var delta = new Vector2(85 * x, 70 * y);
            MouseMove(host, point);
            MouseButtonAt(host, point, true);
            MouseMove(host, point + delta);
            MouseButtonAt(host, point + delta, false);
            Check(window.Size == new Vector2i(x == 0 ? size.X : 160, y == 0 ? size.Y : 140) &&
                window.Position == initial - new Vector2i(x < 0 ? 30 : 0, y < 0 ? 30 : 0),
                $"Embedded {edge} anchors the opposite edges at the maximum size.");
        }
        Check(clicks == 0 && ups == 9, "Each gesture retires the initiating GUI press.");
        window.Position = initial;
        window.Size = size;
        window.Unresizable = true;
        start = () => window.StartResize(Edge.TopLeft);
        MouseMove(host, point);
        MouseButtonAt(host, point, true);
        MouseButtonAt(host, point, false);
        Check(window.Size == size && clicks == 1, "Resize-disabled chrome retains normal clicks.");

        window.Unresizable = false;
        MouseButtonAt(host, point, true);
        window.Unresizable = true;
        MouseMove(host, point - new Vector2(20, 20));
        MouseButtonAt(host, point - new Vector2(20, 20), false);
        Check(window.Size == size && clicks == 1, "Disabling resize stops geometry changes and cancels the original click.");
        start = window.StartDrag;
        MouseMove(host, point);
        MouseButtonAt(host, point, true);
        window.Hide();
        MouseMove(host, point + new Vector2(60, 50));
        MouseButtonAt(host, point, false);
        Check(window.Position == initial && clicks == 1, "Hiding the child retires its move and pointer capture.");
        window.Show();
        start = null;
        MouseMove(host, point);
        MouseButtonAt(host, point, true);
        MouseButtonAt(host, point, false);
        Check(clicks == 2, "The next visible click works.");
        Console.WriteLine("Embedded window move, eight resize edges, constraints, click cancellation and hide passed.");
    }

    private static void NativeDisplay(nint actor, bool noManager)
    {
        using var display = DisplayServer.Open("Window gesture fixture", new(320, 240));
        DisplayServer.WindowSetFlag(WindowFlag.Borderless, true);
        Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowStartDrag(int.MaxValue));
        Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowStartResize(Edge.Max));
        Reject<InvalidOperationException>(() => Task.Run(() => DisplayServer.WindowStartDrag()).GetAwaiter().GetResult());

        var native = WindowHandle();
        if (noManager)
        {
            XSetInputFocus(actor, native, 1, 0);
            XSync(actor, 0);
        }
        PumpUntil(() => DisplayServer.WindowIsFocused(), "The fixture window is focused.");
        var canceled = 0;
        var clicks = 0;
        DisplayServer.GetWindowEvents().Input += input =>
        {
            if (input is not InputEventMouseButton { ButtonIndex: MouseButton.Left } button) return;
            if (button.Canceled) canceled++;
            else if (button.IsReleased()) clicks++;
        };
        void StartAt(Vector2i pointer)
        {
            MovePointer(actor, pointer);
            DisplayServer.ProcessEvents();
            Press(actor, true);
            PumpUntil(() => Input.IsMouseButtonPressed(MouseButton.Left), "The real left press reaches the display service.");
        }
        void EndGesture(Rect2i expected, int expectedCancel)
        {
            Press(actor, false);
            PumpUntil(() => canceled == expectedCancel && !Input.IsMouseButtonPressed(MouseButton.Left),
                "The handoff delivers one canceled release and leaves no held input.");
            PumpUntil(() => DisplayServer.WindowGetPosition() == expected.Position && DisplayServer.WindowGetSize() == expected.Size,
                "The display observes the committed native geometry.");
            Check(Geometry(actor, native) == expected, "Release preserves the manager's final geometry.");
        }

        var start = Geometry(actor, native);
        DisplayServer.WindowStartDrag();
        MovePointer(actor, start.Position + new Vector2i(32, 44));
        DisplayServer.ProcessEvents();
        Check(Geometry(actor, native) == start, "A move without a press is inert.");
        var center = start.Position + start.Size / 2;
        StartAt(center);
        if (noManager)
        {
            Reject<NotSupportedException>(() => DisplayServer.WindowStartDrag());
            Reject<NotSupportedException>(() => DisplayServer.WindowStartResize(Edge.TopLeft));
            Press(actor, false);
            PumpUntil(() => clicks > 0 && !Input.IsMouseButtonPressed(MouseButton.Left), "An unsupported request preserves the original input sequence.");
            return;
        }

        DisplayServer.WindowStartDrag();
        DisplayServer.WindowStartResize(Edge.BottomRight);
        var moveDelta = new Vector2i(38, 26);
        var moved = start.Position + moveDelta;
        MoveGesturePointer(actor, center, moveDelta, () => Geometry(actor, native).Position == moved,
            "Openbox moves the actual window.");
        EndGesture(new Rect2i(moved, start.Size), 1);

        foreach (var (edge, x, y) in Directions)
        {
            DisplayServer.WindowSetPosition(new(340, 240));
            DisplayServer.WindowSetSize(new(320, 240));
            PumpUntil(() => Geometry(actor, native) == new Rect2i(340, 240, 320, 240), "Reset the native fixture rectangle.");
            start = Geometry(actor, native);
            var delta = new Vector2i(x * 18, y * 16);
            var pointer = start.Position + new Vector2i(x < 0 ? 5 : x > 0 ? start.Size.X - 5 : start.Size.X / 2,
                y < 0 ? 5 : y > 0 ? start.Size.Y - 5 : start.Size.Y / 2);
            StartAt(pointer);
            var expectedCancel = canceled + 1;
            DisplayServer.WindowStartResize(edge);

            var position = start.Position + new Vector2i(Math.Min(0, delta.X), Math.Min(0, delta.Y));
            var size = start.Size + new Vector2i(Math.Abs(delta.X), Math.Abs(delta.Y));
            var expected = new Rect2i(position, size);
            MoveGesturePointer(actor, pointer, delta, () => Geometry(actor, native) == expected,
                $"Openbox resizes {edge} from {start} to {expected}.");
            EndGesture(expected, expectedCancel);
        }

        start = Geometry(actor, native);
        center = start.Position + start.Size / 2;
        StartAt(center);
        DisplayServer.WindowStartDrag();
        MoveGesturePointer(actor, center, moveDelta, () => Geometry(actor, native).Position == start.Position + moveDelta,
            "Openbox moves the window before the gesture is dropped.");
        DisplayServer.ForceProcessAndDropEvents();
        PumpUntil(() => Geometry(actor, native) == start, "Dropping the gesture rolls back the manager's move.");
        Press(actor, false);
        DisplayServer.ProcessEvents();
        Check(canceled == 9 && clicks == 0 && !Input.IsMouseButtonPressed(MouseButton.Left),
            "Dropping a gesture clears input and publishes no release callback.");
        Check(Geometry(actor, native) == start, "Dropping the gesture preserves the starting geometry.");

        DisplayServer.WindowSetFlag(WindowFlag.ResizeDisabled, true);
        start = Geometry(actor, native);
        StartAt(start.Position + start.Size / 2);
        DisplayServer.WindowStartResize(Edge.TopLeft);
        Press(actor, false);
        PumpUntil(() => clicks > 0 && !Input.IsMouseButtonPressed(MouseButton.Left), "A nonresizable native window receives a normal release.");
        Check(canceled == 9 && Geometry(actor, native) == start, "The disabled resize cannot change the window.");
    }

    private static void NativeScene(nint actor)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var root = new Window { Size = new(320, 240), Borderless = true, Title = "Window gesture render fixture" };
        root.AddChild(new ColorRect { Size = new(64, 64), Color = Colors.Red });
        var phase = 0;
        var frames = 0;
        var releases = 0;
        var gestureStarted = false;
        Rect2i initial = default;
        var delta = new Vector2i(35, 22);
        root.WindowInput += input =>
        {
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
            {
                if (button.IsPressed())
                {
                    root.StartDrag();
                    gestureStarted = true;
                }
                else if (button.Canceled) releases++;
            }
        };
        root.Ready += _ => RenderingServer.FramePostDraw += Drawn;
        void Drawn()
        {
            Check(++frames < 600, $"Rendered gesture timed out in phase {phase}, native {Geometry(actor, WindowHandle())}, scene {root.Position}, initial {initial}.");
            var native = WindowHandle();
            if (phase == 0 && DisplayServer.WindowIsFocused())
            {
                initial = Geometry(actor, native);
                MovePointer(actor, initial.Position + new Vector2i(95, 95));
                Press(actor, true);
                phase++;
            }
            else if (phase == 1 && gestureStarted)
            {
                if (Geometry(actor, native).Position == initial.Position + delta)
                {
                    Press(actor, false);
                    phase++;
                }
                else
                {
                    var origin = initial.Position + new Vector2i(95, 95);
                    MovePointer(actor, origin);
                    MovePointer(actor, origin + delta);
                }
            }
            else if (phase == 2 && releases == 1 && root.Position == initial.Position + delta)
            {
                using var image = root.GetTexture().GetImage();
                Check(image is not null && image.GetPixel(8, 8) == Colors.Red, "The moved scene still draws its expected pixels.");
                Check(releases == 1 && !Input.IsMouseButtonPressed(MouseButton.Left), "Scene input remains released.");
                RenderingServer.FramePostDraw -= Drawn;
                root.Tree!.Quit();
            }
        }
        Check(Engine.Run(root) == 0 && phase == 2 && !RenderingServer.IsAvailable, "Rendered window gesture completes and retires.");
    }

    private static void MouseButtonAt(Viewport host, Vector2 point, bool pressed)
    {
        using var input = new InputEventMouseButton
        {
            Position = point,
            ButtonIndex = MouseButton.Left,
            Pressed = pressed,
            ButtonMask = pressed ? MouseButtonMask.Left : 0
        };
        host.PushInput(input, true);
    }

    private static void MouseMove(Viewport host, Vector2 point)
    {
        using var input = new InputEventMouseMotion { Position = point, ButtonMask = MouseButtonMask.Left };
        host.PushInput(input, true);
    }

    private static nuint WindowHandle() => (nuint)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);

    private static Rect2i Geometry(nint actor, nuint window)
    {
        Check(XGetGeometry(actor, window, out var root, out _, out _, out var w, out var h, out _, out _) != 0, "Xlib reads the client size.");
        Check(
            XTranslateCoordinates(actor, window, root, 0, 0, out var x, out var y, out _) != 0, "Xlib reads independent client geometry.");
        return new(x, y, (int)w, (int)h);
    }

    private static void MoveGesturePointer(nint actor, Vector2i origin, Vector2i delta, Func<bool> arrived, string message)
    {
        PumpUntil(() =>
        {
            if (arrived()) return true;
            // Retry through two distinct positions because the manager acquires the grab asynchronously.
            MovePointer(actor, origin);
            MovePointer(actor, origin + delta);
            return false;
        }, message);
    }

    private static void Press(nint actor, bool pressed)
    {
        Check(XTestFakeButtonEvent(actor, 1, pressed ? 1 : 0, 0) != 0, "XTEST sends the hardware-style button event.");
        XSync(actor, 0);
    }

    private static void MovePointer(nint actor, Vector2i point)
    {
        Check(XTestFakeMotionEvent(actor, -1, point.X, point.Y, 0) != 0, "XTEST moves the private pointer.");
        XSync(actor, 0);
    }

    private static void PumpUntil(Func<bool> condition, string message)
    {
        for (var i = 0; i < 200; i++)
        {
            DisplayServer.ProcessEvents();
            if (condition()) return;
            Thread.Sleep(5);
        }
        throw new InvalidOperationException(message);
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
    [LibraryImport("libX11.so.6")] private static partial int XSetInputFocus(nint display, nuint window, int revert, nuint time);
    [LibraryImport("libX11.so.6")]
    private static partial int XGetGeometry(nint display, nuint window, out nuint root,
        out int x, out int y, out uint width, out uint height, out uint border, out uint depth);
    [LibraryImport("libX11.so.6")]
    private static partial int XTranslateCoordinates(nint display, nuint source, nuint destination,
        int x, int y, out int resultX, out int resultY, out nuint child);
    [LibraryImport("libXtst.so.6")] private static partial int XTestFakeButtonEvent(nint display, uint button, int press, nuint delay);
    [LibraryImport("libXtst.so.6")] private static partial int XTestFakeMotionEvent(nint display, int screen, int x, int y, nuint delay);
}
