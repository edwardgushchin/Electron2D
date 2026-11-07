using System.Diagnostics;
using System.Runtime.InteropServices;
using Electron2D;

internal static partial class WindowGestureTests
{
    private static void RunWayland()
    {
        var compositor = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_WAYLAND_PRIVATE_DISPLAY");
        var actorDisplay = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_X11_PRIVATE_DISPLAY");
        Check(!string.IsNullOrEmpty(compositor) && compositor == Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") &&
            !string.IsNullOrEmpty(actorDisplay) && actorDisplay == Environment.GetEnvironmentVariable("DISPLAY") &&
            Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "wayland",
            "Use a private nested Wayland compositor on a private Xvfb display.");
        var actor = XOpenDisplay(0);
        Check(actor != 0, "An independent client connects to the compositor's X server.");
        try { WaylandScene(actor); }
        finally
        {
            XTestFakeButtonEvent(actor, 1, 0, 0);
            XTestFakeButtonEvent(actor, 3, 0, 0);
            XSync(actor, 0);
            XCloseDisplay(actor);
        }
        Check(!DisplayServer.IsAvailable, "The Wayland display retires after the test.");
        Console.WriteLine("Wayland move, eight resize edges, nonresizable and chord guards, dropped input, and input recovery passed.");
    }

    private static void WaylandScene(nint actor)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var window = new Window { Size = new(260, 190), Borderless = true, Title = "Wayland gesture fixture" };
        window.AddChild(new ColorRect { Size = new(48, 48), Color = new Color(1, 0, 1) });

        var index = -1;
        var phase = 0;
        var presses = 0;
        var canceled = 0;
        var normal = 0;
        var expectedCanceled = 0;
        var expectedNormal = 0;
        var screen = XDefaultRootWindow(actor);
        var output = Geometry(actor, screen);
        var origin = Vector2i.Zero;
        var delta = Vector2i.Zero;
        var expectedPosition = Vector2i.Zero;
        var expectedSize = Vector2i.Zero;
        var time = Stopwatch.StartNew();
        var drop = false;
        var disabledResize = false;
        var chord = false;
        var settleFrames = 0;

        window.WindowInput += input =>
        {
            if (input is not InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
                return;
            if (!button.IsPressed())
            {
                if (button.Canceled) canceled++;
                else normal++;
                return;
            }

            presses++;
            if (drop)
                return;
            if (index == -1 || chord)
                window.StartDrag();
            else if (index == 0)
                window.StartResize(Directions[index].Edge);
            else if (disabledResize)
                DisplayServer.WindowStartResize(DisplayServer.WindowResizeEdge.BottomRight);
            else
                DisplayServer.WindowStartResize(Directions[index].Edge);
        };
        window.Ready += _ => RenderingServer.FramePostDraw += Drawn;

        void Drawn()
        {
            Check(time.Elapsed < TimeSpan.FromSeconds(15),
                $"Wayland gesture {index} phase {phase} timed out with {presses} presses, {canceled} canceled, {normal} normal.");
            switch (phase)
            {
                case 0:
                    if (WaylandMarker(actor, screen, output.Size) is not { } position)
                        return;
                    drop = index == Directions.Length;
                    disabledResize = index == Directions.Length + 1;
                    chord = index == Directions.Length + 2;
                    window.Unresizable = disabledResize;
                    var size = DisplayServer.WindowGetSize();
                    origin = position + size / 2;
                    expectedSize = size;
                    expectedPosition = position;
                    if (index == -1 || drop)
                    {
                        if (!drop) DisplayServer.WindowStartDrag();
                        // Move toward the center to leave room for later size changes on every edge.
                        delta = new Vector2i(origin.X < output.Size.X / 2 ? 28 : -28,
                            origin.Y < output.Size.Y / 2 ? 21 : -21);
                        expectedPosition += delta;
                    }
                    else if (disabledResize || chord)
                        delta = new Vector2i(14, 12);
                    else
                    {
                        var (_, x, y) = Directions[index];
                        delta = new Vector2i(x * 12, y * 10);
                        expectedPosition += new Vector2i(Math.Min(0, delta.X), Math.Min(0, delta.Y));
                        expectedSize += new Vector2i(Math.Abs(delta.X), Math.Abs(delta.Y));
                    }
                    MovePointer(actor, origin);
                    if (chord)
                    {
                        Check(XTestFakeButtonEvent(actor, 3, 1, 0) != 0, "Hold the private right mouse button.");
                        XSync(actor, 0);
                    }
                    phase++;
                    break;
                case 1:
                    if (chord && !Input.IsMouseButtonPressed(MouseButton.Right))
                        return;
                    Press(actor, true);
                    if (disabledResize || chord) expectedNormal++;
                    else if (!drop) expectedCanceled++;
                    phase++;
                    break;
                case 2:
                    if (presses != index + 2)
                        return;
                    if (!disabledResize && !chord && canceled != expectedCanceled)
                        return;
                    if (drop)
                    {
                        Check(Input.IsMouseButtonPressed(MouseButton.Left), "The initiating press is still held before the handoff.");
                        DisplayServer.WindowStartDrag();
                        DisplayServer.ForceProcessAndDropEvents();
                        Check(!Input.IsMouseButtonPressed(MouseButton.Left), "Dropping input releases the initiating press.");
                    }
                    MovePointer(actor, origin + delta);
                    if (disabledResize || chord)
                        Press(actor, false);
                    phase++;
                    break;
                case 3:
                    if (WaylandMarker(actor, screen, output.Size) != expectedPosition ||
                        window.Size != expectedSize || DisplayServer.WindowGetSize() != expectedSize)
                    {
                        // The compositor may receive the first motion before acquiring the grab.
                        if (drop)
                        {
                            MovePointer(actor, origin);
                            MovePointer(actor, origin + delta);
                        }
                        return;
                    }
                    if (!disabledResize && !chord)
                        Press(actor, false);
                    if (chord)
                    {
                        Check(XTestFakeButtonEvent(actor, 3, 0, 0) != 0, "Release the private right mouse button.");
                        XSync(actor, 0);
                    }
                    settleFrames = 4;
                    phase++;
                    break;
                case 4:
                    if (settleFrames-- > 0 || normal != expectedNormal || canceled != expectedCanceled ||
                        Input.IsMouseButtonPressed(MouseButton.Left) || Input.IsMouseButtonPressed(MouseButton.Right))
                        return;
                    if (chord)
                    {
                        Check(canceled == 9 && normal == 2, "Handoffs cancel one release, dropped input stays silent, and rejected requests preserve normal input.");
                        RenderingServer.FramePostDraw -= Drawn;
                        window.Tree!.Quit();
                    }
                    else
                    {
                        index++;
                        phase = 0;
                        time.Restart();
                    }
                    break;
            }
        }

        try { Check(Engine.Run(window) == 0 && chord && phase == 4 && !RenderingServer.IsAvailable, "The rendered Wayland scene completes and retires."); }
        finally
        {
            if (RenderingServer.IsAvailable)
                RenderingServer.FramePostDraw -= Drawn;
        }
    }

    private static Vector2i? WaylandMarker(nint actor, nuint root, Vector2i size)
    {
        var image = XGetImage(actor, root, 0, 0, (uint)size.X, (uint)size.Y, nuint.MaxValue, 2);
        Check(image != 0, "Capture pixels from the compositor's independent output.");
        try
        {
            const uint marker = 0xff00ff;
            for (var y = 0; y < size.Y; y += 8)
            {
                for (var x = 0; x < size.X; x += 8)
                {
                    if (XGetPixel(image, x, y) != marker)
                        continue;
                    var left = x;
                    var top = y;
                    while (left > 0 && XGetPixel(image, left - 1, y) == marker) left--;
                    while (top > 0 && XGetPixel(image, x, top - 1) == marker) top--;
                    return new Vector2i(left, top);
                }
            }
            return null;
        }
        finally { XDestroyImage(image); }
    }

    [LibraryImport("libX11.so.6")] private static partial nuint XDefaultRootWindow(nint display);
    [LibraryImport("libX11.so.6")]
    private static partial nint XGetImage(nint display, nuint window, int x, int y, uint width, uint height, nuint planeMask, int format);
    [LibraryImport("libX11.so.6")] private static partial nuint XGetPixel(nint image, int x, int y);
    [LibraryImport("libX11.so.6")] private static partial int XDestroyImage(nint image);
}
