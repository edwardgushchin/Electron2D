using Electron2D;
using SDL3;

internal static class DisplayServerNativeSmokeTests
{
    public static void Run()
    {
        var requestedDriver = Environment.GetEnvironmentVariable("SDL_VIDEODRIVER");
        if (requestedDriver is not ("wayland" or "x11"))
            throw new InvalidOperationException("Set SDL_VIDEODRIVER to wayland or x11 for the native display smoke test.");

        if (requestedDriver == "wayland")
            DisplayServerScreenNativeTests.RunHiddenWindow();

        using var display = DisplayServer.Open("Electron2D native smoke", new Vector2I(320, 240));
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_PORTAL_SETTINGS") is { } expectedPortal)
        {
            Check(expectedPortal is "0" or "1", "Portal test expectation must be zero or one.");
            Check(display.IsDarkModeSupported() == (expectedPortal == "1"),
                "Linux theme support follows the Settings portal independently of the preference value.");
            Check(display.IsDarkMode() == (expectedPortal == "1" && SDL.GetSystemTheme() == SDL.SystemTheme.Dark),
                "Linux dark mode requires both Settings support and a dark preference.");
            Console.WriteLine($"{requestedDriver} Settings portal theme probe passed: supported={expectedPortal}.");
            return;
        }
        DisplayServerTaskbarProgressNativeTests.Run(display);
        DisplayServerScreenNativeTests.Run(display);
        DisplayServerKeyboardNativeTests.Run(display);
        DisplayServerWindowPixelNativeTests.Run(display);
        DisplayServerPointerPixelNativeTests.Run(display);
        DisplayServerImeNativeTests.Run(display);
        DisplayServerWindowNativeSemanticsTests.Run(display);
        DisplayServerPointerNativeTests.Run(display);
        DisplayServerClipboardNativeTests.Run(display);
        DisplayServerIconTests.Run(display);
        DisplayServerCloseEventsTests.Run(display);
        Check(display.GetName() == (requestedDriver == "wayland" ? "Wayland" : "X11"),
            "The public backend name identifies the selected native display driver.");
        Check(display.GetWindowList() is [DisplayServer.MainWindowId], "The main window has the public ID zero.");

        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is [var window] && SDL.GetWindowID(window) != 0,
            "SDL created one identifiable native window.");

        var screen = display.WindowGetCurrentScreen();
        Check(display.GetScreenCount() > 0 && screen >= 0 && screen < display.GetScreenCount() &&
              display.ScreenGetSize(screen) is { X: > 0, Y: > 0 },
            "The window belongs to a connected screen with a positive size.");
        var nativeDisplays = SDL.GetDisplays(out var nativeDisplayCount);
        var expectedPrimary = requestedDriver == "wayland" ? 0 : nativeDisplays is null ? DisplayServer.InvalidScreen : Array.IndexOf(nativeDisplays, SDL.GetPrimaryDisplay());
        Check(nativeDisplays is not null && display.GetScreenCount() == nativeDisplayCount &&
              display.GetPrimaryScreen() == expectedPrimary &&
              screen == Array.IndexOf(nativeDisplays, SDL.GetDisplayForWindow(windows![0])) &&
              display.WindowGetCurrentScreen(DisplayServer.InvalidWindowId) == DisplayServer.InvalidScreen,
            "Screen count, primary index, window-center screen, and invalid-window fallback match the native display snapshot.");
        if (requestedDriver == "wayland")
        {
            try
            {
                display.GetWindowAtScreenPosition(Vector2I.Zero);
                throw new InvalidOperationException("Wayland unexpectedly exposed a global window-position hit test.");
            }
            catch (NotSupportedException)
            {
                // Global top-level positions are unavailable under the accepted Wayland contract.
            }
            Check(display.HasHardwareKeyboard(), "The desktop hardware-keyboard query is true on Wayland.");
            Check(display.GetKeyboardFocusScreen() == display.GetPrimaryScreen(),
                "Wayland keyboard-focus screen falls back to the primary screen.");
            SDL.GetMouseState(out var mouseX, out var mouseY);
            var mousePixelScale = SDL.GetWindowPixelDensity(windows![0]);
            Check(float.IsFinite(mousePixelScale) && mousePixelScale > 0f &&
                  display.MouseGetPosition() == new Vector2I((int)(mouseX * mousePixelScale),
                      (int)(mouseY * mousePixelScale)),
                "Wayland mouse position converts SDL's window-relative state to physical client pixels.");
            Check(display.HasFeature(DisplayServer.Feature.Ime),
                "Wayland reports its integrated text-input capability.");
            display.WindowSetIMEActive(true);
            Check(SDL.TextInputActive(windows![0]), "Wayland started native text input for the main window.");
            display.WindowSetIMEActive(false);
            Check(!SDL.TextInputActive(windows[0]), "Wayland stopped native text input for the main window.");
        }
        for (var index = 0; index < (int)DisplayServer.CursorShape.Max; index++)
        {
            var shape = (DisplayServer.CursorShape)index;
            display.CursorSetShape(shape);
            Check(display.CursorGetShape() == shape && SDL.GetCursor() != 0,
                $"Cursor shape {shape} is retained and installed natively.");
        }
        display.CursorSetShape(DisplayServer.CursorShape.Arrow);
        var systemCursor = SDL.GetCursor();
        using (var maximum = Electron2D.Image.CreateEmpty(256, 1, false, Electron2D.Image.Format.Rgba8))
            display.CursorSetCustomImage(maximum, hotspot: new Vector2(255, 0));
        Check(SDL.GetCursor() != 0 && SDL.GetCursor() != systemCursor,
            "A 256-pixel-wide cursor and its last pixel hotspot are accepted natively.");
        nint validCursor;
        using (var image = Electron2D.Image.CreateFromData(2, 2, false, Electron2D.Image.Format.Rgba8,
                   [255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 255, 255]))
        {
            display.CursorSetCustomImage(image, hotspot: new Vector2(1.75f, 0.25f));
            validCursor = SDL.GetCursor();
            try
            {
                display.CursorSetCustomImage(image, hotspot: new Vector2(float.NaN, 0));
                throw new InvalidOperationException("A nonfinite cursor hotspot was accepted.");
            }
            catch (ArgumentOutOfRangeException)
            {
                // Native cursor remains the last valid image.
            }
        }
        using (var oversized = Electron2D.Image.CreateEmpty(257, 1, false, Electron2D.Image.Format.Rgba8))
        {
            try
            {
                display.CursorSetCustomImage(oversized);
                throw new InvalidOperationException("A cursor image above the 256-pixel limit was accepted.");
            }
            catch (ArgumentException)
            {
                // An invalid image does not replace the active native cursor.
            }
        }
        Check(SDL.GetCursor() == validCursor, "Invalid custom cursor requests preserve the active native image.");
        var customCursor = SDL.GetCursor();
        Check(customCursor != 0 && customCursor != systemCursor &&
              display.CursorGetShape() == DisplayServer.CursorShape.Arrow,
            "The active custom cursor owns a native copy after its source image is disposed.");
        display.CursorSetShape(DisplayServer.CursorShape.IBeam);
        display.CursorSetShape(DisplayServer.CursorShape.Arrow);
        Check(SDL.GetCursor() == customCursor, "The custom cursor remains bound to its shape slot.");
        display.CursorSetCustomImage(null);
        Check(SDL.GetCursor() != 0 && SDL.GetCursor() != customCursor,
            "Clearing a custom cursor restores the system shape.");
        Check(display.WindowGetNativeHandle(DisplayServer.HandleType.DisplayHandle) != 0 &&
              display.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle) != 0,
            "SDL exposes borrowed native display and window handles.");
        if (requestedDriver == "wayland")
        {
            var windowProperties = SDL.GetWindowProperties(windows![0]);
            Check(windowProperties != 0 &&
                  display.WindowGetNativeHandle(DisplayServer.HandleType.DisplayHandle) ==
                  SDL.GetPointerProperty(windowProperties, SDL.Props.WindowWaylandDisplayPointer, 0) &&
                  display.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle) ==
                  SDL.GetPointerProperty(windowProperties, SDL.Props.WindowWaylandSurfacePointer, 0),
                "Borrowed Wayland display and surface handles retain their exact SDL identities.");
        }

        display.WindowSetTitle("Electron2D native smoke updated");
        display.WindowSetMinSize(new Vector2I(96, 72));
        display.WindowSetSize(new Vector2I(400, 300));
        display.ProcessEvents();
        Check(display.WindowGetTitle() == "Electron2D native smoke updated" &&
              display.WindowGetMinSize() == new Vector2I(96, 72) &&
              display.WindowGetSize() is { X: > 0, Y: > 0 },
            "Window getters observe valid state after title, minimum-size, and size requests.");

        if (requestedDriver == "x11")
        {
            var origin = display.WindowGetPosition();
            var size = display.WindowGetSize();
            Check(display.GetWindowAtScreenPosition(origin) == DisplayServer.MainWindowId &&
                  display.GetWindowAtScreenPosition(new Vector2I(origin.X + size.X, origin.Y)) == DisplayServer.InvalidWindowId &&
                  display.GetWindowAtScreenPosition(new Vector2I(origin.X, origin.Y + size.Y)) == DisplayServer.InvalidWindowId,
                "X11 window hit testing uses half-open client bounds.");
            if (SDL.GetWindowBordersSize(windows![0], out var top, out var left, out _, out _) &&
                (top > 0 || left > 0))
            {
                var borderPoint = top > 0
                    ? new Vector2I(origin.X, origin.Y - 1)
                    : new Vector2I(origin.X - 1, origin.Y);
                Check(display.GetWindowAtScreenPosition(borderPoint) == DisplayServer.InvalidWindowId,
                    "X11 title bar and window borders are outside the client hit area.");
            }

            if (display.GetScreenCount() > 1)
            {
                var originalPosition = display.WindowGetPosition();
                var originalMode = display.WindowGetMode();
                var sourceScreen = display.WindowGetCurrentScreen();
                var targetScreen = (sourceScreen + 1) % display.GetScreenCount();
                try
                {
                    var sourceUsable = display.ScreenGetUsableRect(sourceScreen);
                    display.WindowSetPosition(sourceUsable.Position + new Vector2I(80, 80));
                    Check(SDL.SyncWindow(windows![0]), "X11 applied the normal-window test position.");
                    display.ProcessEvents();
                    var before = display.WindowGetPosition();
                    var targetUsable = display.ScreenGetUsableRect(targetScreen);
                    var oldScreenPosition = display.ScreenGetPosition(sourceScreen);
                    var windowSize = display.WindowGetSize();
                    var expectedX = Math.Clamp((long)before.X - oldScreenPosition.X + targetUsable.Position.X,
                        targetUsable.Position.X,
                        Math.Max((long)targetUsable.Position.X,
                            (long)targetUsable.Position.X + targetUsable.Size.X - windowSize.X / 3));
                    var expectedY = Math.Clamp((long)before.Y - oldScreenPosition.Y + targetUsable.Position.Y,
                        targetUsable.Position.Y,
                        Math.Max((long)targetUsable.Position.Y,
                            (long)targetUsable.Position.Y + targetUsable.Size.Y - windowSize.Y / 3));

                    display.WindowSetCurrentScreen(targetScreen);
                    Check(SDL.SyncWindow(windows[0]), "X11 completed the requested display transfer.");
                    display.ProcessEvents();
                    var actual = display.WindowGetPosition();
                    Check(display.WindowGetCurrentScreen() == targetScreen &&
                          Math.Abs((long)actual.X - expectedX) <= 2 &&
                          Math.Abs((long)actual.Y - expectedY) <= 2 &&
                          display.WindowGetMode() == originalMode,
                        "X11 moved the window to the target display, preserving its relative position and mode.");
                    Console.WriteLine($"X11 display transfer passed: {sourceScreen} -> {targetScreen}.");
                }
                finally
                {
                    if (display.WindowGetMode() != originalMode)
                        display.WindowSetMode(originalMode);
                    display.WindowSetPosition(originalPosition);
                    Check(SDL.SyncWindow(windows![0]), "X11 restored the initial window position.");
                    display.ProcessEvents();
                }
            }
        }

        DisplayServerWindowNativeSemanticsTests.CheckMinimizeLast(display);
        Console.WriteLine($"Native DisplayServer smoke passed: {display.GetName()}, screen {screen}.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
