using Electron2D;
using SDL3;

internal static class DisplayServerPointerFocusNativeTests
{
    public static void RunConfinement()
    {
        using var display = DisplayServer.Open("Electron2D pointer confinement test", new Vector2I(480, 360));
        Check(display.GetName() == "Wayland", "The confinement test requires Wayland.");
        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is { Length: 1 }, "The confinement test needs one native window.");
        var window = windows![0];
        var renderer = SDL.CreateRenderer(window, "software");
        Check(renderer != 0, $"Cannot draw the confinement test window: {SDL.GetError()}");
        try
        {
            Check(SDL.SetRenderDrawColor(renderer, 25, 115, 165, 255), SDL.GetError());
            Console.WriteLine("Move the pointer into the blue window. Confinement starts when it gets mouse focus.");
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (DateTime.UtcNow < deadline && SDL.GetMouseFocus() != window)
                PumpAndDraw(display, renderer);
            Check(SDL.GetMouseFocus() == window, "The visible Wayland window never received pointer focus.");

            display.MouseSetMode(DisplayServer.MouseMode.Confined);
            Check(display.MouseGetMode() == DisplayServer.MouseMode.Confined, "The public mode did not enter Confined.");
            Console.WriteLine("Confined active for 25 seconds. Push the pointer beyond EACH of the four blue-window edges; do not click another window.");
            var size = display.WindowGetSize();
            var minX = int.MaxValue;
            var maxX = int.MinValue;
            var minY = int.MaxValue;
            var maxY = int.MinValue;
            var lostFocus = false;
            deadline = DateTime.UtcNow.AddSeconds(25);
            while (DateTime.UtcNow < deadline)
            {
                PumpAndDraw(display, renderer);
                lostFocus |= SDL.GetMouseFocus() != window;
                var position = display.MouseGetPosition();
                minX = Math.Min(minX, position.X);
                maxX = Math.Max(maxX, position.X);
                minY = Math.Min(minY, position.Y);
                maxY = Math.Max(maxY, position.Y);
            }
            var edge = Math.Max(24, (int)MathF.Ceiling(SDL.GetWindowPixelDensity(window) * 12));
            Console.WriteLine($"Confined range: x=[{minX},{maxX}], y=[{minY},{maxY}], client size={size}, lost focus={lostFocus}.");
            Check(!lostFocus, "Pointer focus escaped while Confined was active.");
            Check(minX <= edge && maxX >= size.X - edge && minY <= edge && maxY >= size.Y - edge,
                "The pointer did not reach all four window edges during the confinement test.");
            Check(maxX < size.X && maxY < size.Y && minX >= 0 && minY >= 0,
                "The confined pointer moved outside the client rectangle.");
            Console.WriteLine("Wayland physical pointer confinement passed while pushing all four window edges.");
        }
        finally
        {
            display.MouseSetMode(DisplayServer.MouseMode.Visible);
            SDL.DestroyRenderer(renderer);
        }
    }

    public static void Run()
    {
        using var display = DisplayServer.Open("Electron2D pointer focus test", new Vector2I(480, 360));
        Check(display.GetName() == "Wayland", "The focused pointer test requires Wayland.");
        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is { Length: 1 }, "The focused pointer test needs one native window.");
        var window = windows![0];
        var renderer = SDL.CreateRenderer(window, "software");
        Check(renderer != 0, $"Cannot draw the pointer test window: {SDL.GetError()}");
        try
        {
            Check(SDL.SetRenderDrawColor(renderer, 25, 115, 165, 255), SDL.GetError());
            for (var i = 0; i < 100; i++)
                PumpAndDraw(display, renderer);

            Console.WriteLine("Pointer window ready. Move inside the blue window, move the mouse, hold the left button briefly, then release it.");
            var moved = false;
            var pressed = false;
            var released = false;
            var lastPosition = display.MouseGetPosition();
            var deadline = DateTime.UtcNow.AddSeconds(60);
            while (DateTime.UtcNow < deadline && !(moved && released))
            {
                PumpAndDraw(display, renderer);
                if (SDL.GetMouseFocus() != window)
                    continue;
                var nativeButtons = SDL.GetMouseState(out var x, out var y);
                var density = SDL.GetWindowPixelDensity(window);
                Check(float.IsFinite(density) && density > 0f, "The focused window has valid pixel density.");
                var position = display.MouseGetPosition();
                Check(position == new Vector2I((int)(x * density), (int)(y * density)),
                    "The focused pointer position matches native SDL state in client pixels.");
                if (position != lastPosition)
                    moved = true;
                lastPosition = position;
                var leftDown = (nativeButtons & SDL.MouseButtonFlags.Left) != 0;
                Check(((display.MouseGetButtonState() & MouseButtonMask.Left) != 0) == leftDown,
                    "The public left-button mask matches a real focused pointer press.");
                pressed |= leftDown;
                released = pressed && !leftDown;
            }
            Check(moved && pressed && released, "No complete real pointer motion and left-button press/release was observed.");

            var beforeWarp = display.MouseGetPosition();
            var focusBeforeWarp = SDL.GetMouseFocus() == window;
            Check(!display.HasFeature(DisplayServer.Feature.MouseWarp),
                "The Wayland backend does not advertise pointer warping.");
            var warpRejected = false;
            try
            {
                display.WarpMouse(new Vector2I(beforeWarp.X + 40, beforeWarp.Y + 40));
            }
            catch (NotSupportedException)
            {
                warpRejected = true;
            }
            Check(warpRejected && display.MouseGetPosition() == beforeWarp &&
                  SDL.GetMouseFocus() == window && focusBeforeWarp,
                "Unavailable focused Wayland warp must reject without changing synthetic pointer state or focus.");

            Console.WriteLine("Pointer capture active. Keep moving the mouse inside the blue window for a moment.");
            display.MouseSetMode(DisplayServer.MouseMode.Captured);
            SDL.GetRelativeMouseState(out _, out _);
            var relativeMotion = false;
            deadline = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < deadline && !relativeMotion)
            {
                PumpAndDraw(display, renderer);
                SDL.GetRelativeMouseState(out var dx, out var dy);
                relativeMotion = SDL.GetMouseFocus() == window && (dx != 0f || dy != 0f);
            }
            Check(relativeMotion, "No real relative pointer motion arrived with focused capture enabled.");

            foreach (var mode in new[]
                     {
                         DisplayServer.MouseMode.Hidden,
                         DisplayServer.MouseMode.Confined,
                         DisplayServer.MouseMode.ConfinedHidden,
                         DisplayServer.MouseMode.Captured,
                         DisplayServer.MouseMode.Visible,
                     })
            {
                display.MouseSetMode(mode);
                for (var i = 0; i < 10; i++)
                    PumpAndDraw(display, renderer);
                var flags = SDL.GetWindowFlags(window);
                Check(display.MouseGetMode() == mode &&
                      SDL.GetWindowRelativeMouseMode(window) == (mode == DisplayServer.MouseMode.Captured) &&
                      ((flags & SDL.WindowFlags.MouseGrabbed) != 0) ==
                      (mode is DisplayServer.MouseMode.Confined or DisplayServer.MouseMode.ConfinedHidden) &&
                      SDL.CursorVisible() == (mode is DisplayServer.MouseMode.Visible or DisplayServer.MouseMode.Confined),
                    $"Focused mouse mode {mode} disagrees with SDL's native state.");
            }
            Console.WriteLine("Wayland focused pointer motion, button state, relative capture, mouse modes, and warp rejection passed.");
        }
        finally
        {
            display.MouseSetMode(DisplayServer.MouseMode.Visible);
            SDL.DestroyRenderer(renderer);
        }
    }

    private static void PumpAndDraw(DisplayServer display, nint renderer)
    {
        display.ProcessEvents();
        Check(SDL.RenderClear(renderer) && SDL.RenderPresent(renderer), $"Cannot present pointer test window: {SDL.GetError()}");
        Thread.Sleep(20);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
