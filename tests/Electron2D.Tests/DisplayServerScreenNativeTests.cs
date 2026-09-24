using Electron2D;
using SDL3;

internal static class DisplayServerScreenNativeTests
{
    public static void RunHiddenWindow()
    {
        using var display = DisplayServer.Open("Electron2D hidden scale check", new Vector2i(320, 240), hidden: true);
        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is [var window] && window != 0,
            "The hidden scale check has one SDL window.");
        var hiddenScale = display.ScreenGetScale();
        Check(hiddenScale == SDL.GetWindowDisplayScale(windows![0]),
            "The hidden window reports its current native scale before mapping.");
        Check(SDL.SyncWindow(windows![0]), "SDL synchronized the hidden window.");
        var synchronizedHiddenScale = display.ScreenGetScale();
        Check(SDL.ShowWindow(windows[0]) && SDL.SyncWindow(windows[0]),
            "SDL mapped and synchronized the formerly hidden Wayland window.");
        var shownScale = display.ScreenGetScale();
        Check(shownScale == SDL.GetWindowDisplayScale(windows[0]),
            "The shown window reports its current native scale before its first buffer.");
        var renderer = SDL.CreateRenderer(windows[0], "software");
        Check(renderer != 0, "The formerly hidden window has a test renderer.");
        try
        {
            Check(SDL.RenderClear(renderer) && SDL.RenderPresent(renderer),
                "The formerly hidden window presented its first buffer.");
            var deadline = DateTime.UtcNow.AddSeconds(2);
            while (DateTime.UtcNow < deadline && display.ScreenGetScale() !=
                   (SDL.GetCurrentDisplayMode(SDL.GetDisplayForWindow(windows[0]))?.PixelDensity ?? 0f))
            {
                display.ProcessEvents();
                Thread.Sleep(10);
            }
        }
        finally
        {
            SDL.DestroyRenderer(renderer);
        }
        var presentedScale = display.ScreenGetScale();
        var outputId = SDL.GetDisplayForWindow(windows[0]);
        var outputScale = SDL.GetCurrentDisplayMode(outputId)?.PixelDensity ?? 0f;
        Check(outputScale > 0f && presentedScale == outputScale,
            $"The presented window has its output's fractional scale (window {presentedScale}, output {outputScale}).");
        Console.WriteLine($"Wayland hidden scale probe: initial {hiddenScale}, hidden after SyncWindow {synchronizedHiddenScale}, shown {shownScale}, presented {presentedScale}, output {outputScale}.");
    }

    public static void Run(DisplayServer display)
    {
        if (display.GetName() != "Wayland")
            return;

        var windows = SDL.GetWindows(out var windowCount);
        Check(windowCount == 1 && windows is [var window] && window != 0,
            "The native screen check needs the main window.");
        var windowScale = SDL.GetWindowDisplayScale(windows![0]);
        var windowPixelDensity = SDL.GetWindowPixelDensity(windows[0]);
        var initialDisplayId = SDL.GetDisplayForWindow(windows[0]);
        var initialDisplayMode = SDL.GetCurrentDisplayMode(initialDisplayId);
        var initialDisplayDensity = initialDisplayMode?.PixelDensity ?? 0f;
        Console.WriteLine($"Wayland visible initial scale probe: Open {windowScale}, estimated output {initialDisplayDensity}.");
        var hasLogicalSize = SDL.GetWindowSize(windows[0], out var logicalWindowWidth, out var logicalWindowHeight);
        var hasPixelSize = SDL.GetWindowSizeInPixels(windows[0], out var pixelWindowWidth, out var pixelWindowHeight);
        Check(hasLogicalSize && hasPixelSize, "The native window exposes logical and pixel dimensions.");
        Check(float.IsFinite(windowScale) && windowScale > 0f &&
              display.ScreenGetScale() == windowScale,
            "The main-window selector reports the Wayland window's fractional content scale.");
        Check(float.IsFinite(windowPixelDensity) && windowPixelDensity > 0f &&
              Math.Abs(pixelWindowWidth - logicalWindowWidth * windowPixelDensity) <= 1f &&
              Math.Abs(pixelWindowHeight - logicalWindowHeight * windowPixelDensity) <= 1f,
            "Window pixel dimensions agree with the reported window pixel density.");

        var nativeDisplays = SDL.GetDisplays(out var displayCount);
        Check(nativeDisplays is not null && displayCount == display.GetScreenCount() && displayCount > 0,
            "The screen list matches the native display snapshot.");
        Console.WriteLine($"Wayland display probe: {displayCount} SDL display(s), window content scale {windowScale}, pixel density {windowPixelDensity}, window {logicalWindowWidth}x{logicalWindowHeight} logical / {pixelWindowWidth}x{pixelWindowHeight} pixels.");
        var maximumScale = 1f;
        for (var index = 0; index < displayCount; index++)
        {
            var displayId = nativeDisplays![index];
            Check(SDL.GetDisplayBounds(displayId, out var bounds),
                "A connected display has native bounds.");
            var nativeMode = SDL.GetCurrentDisplayMode(displayId);
            var pixelDensity = nativeMode?.PixelDensity ?? 0f;
            Check(float.IsFinite(pixelDensity) && pixelDensity > 0f,
                "A Wayland display mode reports its logical-to-physical pixel density.");
            var physicalSize = new Vector2i((int)Math.Round(bounds.W * (double)pixelDensity),
                (int)Math.Round(bounds.H * (double)pixelDensity));
            Check(display.ScreenGetPosition(index) == new Vector2i(bounds.X, bounds.Y) &&
                  display.ScreenGetSize(index) == physicalSize &&
                  display.ScreenGetUsableRect(index) ==
                  new Rect2i(new Vector2i(bounds.X, bounds.Y), physicalSize),
                "Wayland screen position and physical size match the native output snapshot.");
            if (index == 0 && physicalSize.X > bounds.W)
                Check(display.GetScreenFromRect(new Rect2(bounds.X + bounds.W, bounds.Y, 1, 1)) == 0,
                    "Screen overlap uses the physical width of the fractionally scaled first display.");

            var nativeScale = SDL.GetDisplayContentScale(displayId);
            Console.WriteLine($"Wayland display {index}: {bounds.W}x{bounds.H} logical / {physicalSize.X}x{physicalSize.Y} physical at ({bounds.X}, {bounds.Y}), SDL scale {nativeScale}, pixel density {pixelDensity}.");
            var expectedScale = System.MathF.Ceiling(pixelDensity);
            Check(display.ScreenGetScale(index) == expectedScale,
                "An indexed Wayland screen rounds its fractional pixel density up to the integer output scale.");
            Console.WriteLine($"Wayland display {index}: indexed scale {display.ScreenGetScale(index)}, refresh {display.ScreenGetRefreshRate(index)} Hz (SDL precise {nativeMode?.RefreshRateNumerator}/{nativeMode?.RefreshRateDenominator}).");
            maximumScale = Math.Max(maximumScale, expectedScale);

            var nativeRefreshRate = nativeMode is { RefreshRateNumerator: > 0, RefreshRateDenominator: > 0 } precise
                ? (float)precise.RefreshRateNumerator / precise.RefreshRateDenominator
                : nativeMode?.RefreshRate ?? 0f;
            var expectedRefreshRate = float.IsFinite(nativeRefreshRate) && nativeRefreshRate > 0f
                ? nativeRefreshRate : -1f;
            Check(display.ScreenGetRefreshRate(index) == expectedRefreshRate,
                "The current mode refresh rate agrees with SDL's native snapshot.");
        }

        Check(display.ScreenGetMaxScale() == maximumScale,
            "The maximum scale is the greatest indexed Wayland screen scale.");
        Check(display.ScreenGetSize(DisplayServer.ScreenWithMouseFocus) == display.ScreenGetSize(0),
            "Wayland's mouse-focus screen selector uses screen zero even without SDL mouse focus.");
        Check(display.ScreenGetUsableRect(int.MaxValue) == default,
            "An invalid screen has an empty usable rectangle.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
