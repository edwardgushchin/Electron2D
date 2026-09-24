using Electron2D;
using SDL3;

internal static class DisplayServerWindowPixelNativeTests
{
    public static void Run(DisplayServer display)
    {
        if (display.GetName() != "Wayland")
            return;

        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is { Length: 1 }, "The Wayland pixel-size test needs one window.");
        var window = windows![0];
        var originalSize = display.WindowGetSize();
        var originalMinimum = display.WindowGetMinSize();
        var originalMaximum = display.WindowGetMaxSize();
        var lastRect = default(Rect2i);
        var rectChanged = false;
        void OnRectChanged(Rect2i rectangle)
        {
            lastRect = rectangle;
            rectChanged = true;
        }

        display.WindowRectChanged += OnRectChanged;
        try
        {
            Check(SDL.GetWindowSizeInPixels(window, out var initialWidth, out var initialHeight) &&
                  originalSize == new Vector2i(initialWidth, initialHeight),
                "The initial public window size uses native client pixels.");

            display.WindowSetMaxSize(new Vector2i(501, 401));
            display.WindowSetMinSize(new Vector2i(97, 73));
            var density = SDL.GetWindowPixelDensity(window);
            var scale = SDL.GetWindowDisplayScale(window);
            Check(float.IsFinite(density) && density > 0f && float.IsFinite(scale) && scale > 0f,
                "The Wayland window reports positive pixel density and content scale.");
            Check(display.WindowGetMinSize() == new Vector2i(97, 73) &&
                  display.WindowGetMaxSize() == new Vector2i(501, 401) &&
                  SDL.GetWindowMinimumSize(window, out var minWidth, out var minHeight) &&
                  SDL.GetWindowMaximumSize(window, out var maxWidth, out var maxHeight) &&
                  minWidth == ToLogicalMinimum(97, density) && minHeight == ToLogicalMinimum(73, density) &&
                  maxWidth == ToLogicalMaximum(501, density) && maxHeight == ToLogicalMaximum(401, density),
                "Public pixel limits round inward to native logical window limits.");

            display.WindowSetMaxSize(new Vector2i(0, 201));
            display.WindowSetMinSize(new Vector2i(97, 0));
            Check(display.WindowGetMaxSize() == new Vector2i(0, 201) &&
                  display.WindowGetMinSize() == new Vector2i(97, 0) &&
                  SDL.GetWindowMinimumSize(window, out minWidth, out minHeight) &&
                  SDL.GetWindowMaximumSize(window, out maxWidth, out maxHeight) &&
                  minWidth == ToLogicalMinimum(97, density) && minHeight == 0 &&
                  maxWidth == 0 && maxHeight == ToLogicalMaximum(201, density),
                "Zero leaves its own minimum or maximum axis unbounded.");
            display.WindowSetMaxSize(new Vector2i(501, 401));
            display.WindowSetMinSize(new Vector2i(97, 73));

            _ = SDL.SyncWindow(window);
            display.ProcessEvents();
            var requested = display.WindowGetSize() == new Vector2i(400, 300)
                ? new Vector2i(480, 360) : new Vector2i(400, 300);
            rectChanged = false;
            display.WindowSetSize(requested);
            _ = SDL.SyncWindow(window);
            display.ProcessEvents();
            var logicalRead = SDL.GetWindowSize(window, out var logicalWidth, out var logicalHeight);
            var pixelRead = SDL.GetWindowSizeInPixels(window, out var pixelWidth, out var pixelHeight);
            Check(logicalRead && pixelRead &&
                  logicalWidth == ToLogical(requested.X, density) &&
                  logicalHeight == ToLogical(requested.Y, density) &&
                  display.WindowGetSize() == new Vector2i(pixelWidth, pixelHeight) &&
                  display.WindowGetSizeWithDecorations() == new Vector2i(pixelWidth, pixelHeight) &&
                  Math.Abs(pixelWidth - requested.X) <= 1 &&
                  Math.Abs(pixelHeight - requested.Y) <= 1,
                "A public pixel-size request maps to SDL logical units and reads back native pixels.");
            Check(rectChanged && lastRect.Size == new Vector2i(pixelWidth, pixelHeight),
                "The resize callback carries pixel dimensions on Wayland.");

            Console.WriteLine($"Wayland pixel-window probe: pixel density {density}, content scale {scale}, {logicalWidth}x{logicalHeight} logical, {pixelWidth}x{pixelHeight} pixels; fractional-density path {(density % 1f != 0f ? "exercised" : "not exercised")}.");
        }
        finally
        {
            display.WindowRectChanged -= OnRectChanged;
            display.WindowSetMaxSize(Vector2i.Zero);
            display.WindowSetMinSize(originalMinimum);
            display.WindowSetMaxSize(originalMaximum);
            display.WindowSetSize(originalSize);
            _ = SDL.SyncWindow(window);
            display.ProcessEvents();
        }
    }

    private static int ToLogical(int pixels, float density) =>
        checked((int)Math.Round(pixels / (double)density, MidpointRounding.AwayFromZero));

    private static int ToLogicalMinimum(int pixels, float density) =>
        checked((int)Math.Ceiling(pixels / (double)density));

    private static int ToLogicalMaximum(int pixels, float density) =>
        checked((int)Math.Floor(pixels / (double)density));

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
