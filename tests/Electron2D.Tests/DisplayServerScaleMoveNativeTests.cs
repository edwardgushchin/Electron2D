using Electron2D;
using SDL3;

internal static class DisplayServerScaleMoveNativeTests
{
    public static void Run()
    {
        using var display = DisplayServer.Open("Electron2D scale move test", new Vector2i(320, 240));
        if (display.GetName() != "Wayland")
            throw new InvalidOperationException("The scale move test requires Wayland.");

        var windows = SDL.GetWindows(out var count);
        if (count != 1 || windows is not { Length: 1 })
            throw new InvalidOperationException("The scale move test needs one window.");
        var window = windows[0];
        display.WindowSetMaxSize(new Vector2i(501, 401));
        display.WindowSetMinSize(new Vector2i(97, 73));

        var renderer = SDL.CreateRenderer(window, "software");
        if (renderer == 0)
            throw new InvalidOperationException($"The scale move test cannot draw its window: {SDL.GetError()}");
        try
        {
            RunVisibleProbe(display, window, renderer);
        }
        finally
        {
            SDL.DestroyRenderer(renderer);
        }
    }

    private static void RunVisibleProbe(DisplayServer display, nint window, nint renderer)
    {
        if (!SDL.SetRenderDrawColor(renderer, 35, 80, 180, 255) ||
            !SDL.RenderClear(renderer) || !SDL.RenderPresent(renderer))
            throw new InvalidOperationException($"The scale move test cannot present its window: {SDL.GetError()}");

        var settledAt = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < settledAt)
        {
            display.ProcessEvents();
            if (!SDL.RenderClear(renderer) || !SDL.RenderPresent(renderer))
                throw new InvalidOperationException($"The scale move test cannot present its window: {SDL.GetError()}");
            Thread.Sleep(20);
        }

        var startingDensity = SDL.GetWindowPixelDensity(window);
        var previousDensity = startingDensity;
        var transitions = 0;
        var scaleEvents = 0;
        display.WindowDpiChanged += () => scaleEvents++;
        Console.WriteLine($"Scale move window ready: density {startingDensity}. Drag it to a monitor with a different scale and back.");
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline && transitions < 2)
        {
            display.ProcessEvents();
            if (!SDL.RenderClear(renderer) || !SDL.RenderPresent(renderer))
                throw new InvalidOperationException($"The scale move test cannot present its window: {SDL.GetError()}");
            var density = SDL.GetWindowPixelDensity(window);
            if (density != previousDensity)
            {
                transitions++;
                var size = display.WindowGetSize();
                var minimum = display.WindowGetMinSize();
                var maximum = display.WindowGetMaxSize();
                if (!SDL.GetWindowSizeInPixels(window, out var pixelWidth, out var pixelHeight) ||
                    !SDL.GetWindowMinimumSize(window, out var minWidth, out var minHeight) ||
                    !SDL.GetWindowMaximumSize(window, out var maxWidth, out var maxHeight) ||
                    size != new Vector2i(pixelWidth, pixelHeight) ||
                    minimum != new Vector2i(97, 73) || maximum != new Vector2i(501, 401) ||
                    minWidth != (int)Math.Ceiling(97 / (double)density) ||
                    minHeight != (int)Math.Ceiling(73 / (double)density) ||
                    maxWidth != (int)Math.Floor(501 / (double)density) ||
                    maxHeight != (int)Math.Floor(401 / (double)density))
                    throw new InvalidOperationException($"Scale move state did not match pixel bounds at density {density}.");
                if (scaleEvents < transitions)
                    throw new InvalidOperationException($"Missing scale notification after density transition {transitions}.");
                Console.WriteLine($"Scale transition {transitions}: density {previousDensity} -> {density}, client {size.X}x{size.Y} pixels, scale events {scaleEvents}.");
                previousDensity = density;
            }
            Thread.Sleep(20);
        }

        if (transitions < 2 || previousDensity != startingDensity)
            throw new InvalidOperationException($"Observed {transitions} density transition(s), expected a move to another density and back.");
        Console.WriteLine("Wayland scale move passed.");
    }
}
