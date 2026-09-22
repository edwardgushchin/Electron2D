using Electron2D;
using SDL3;

internal static class DisplayServerWindowNativeSemanticsTests
{
    public static void Run(DisplayServer display)
    {
        if (display.GetName() != "Wayland")
            return;

        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is { Length: 1 }, "One native main window exists.");
        var window = windows![0];
        Check(display.WindowIsFocused() == (SDL.GetMouseFocus() == window),
            "Wayland focus follows the window under the pointer.");
        Check(display.WindowGetSizeWithDecorations() == display.WindowGetSize(),
            "Wayland decorated size uses the client size when server decorations cannot be measured.");
        ExpectNotSupported(() => display.WindowGetPosition());
        ExpectNotSupported(() => display.WindowGetPositionWithDecorations());
        ExpectNotSupported(() => display.WindowSetPosition(Vector2I.Zero));
        var currentScreen = display.WindowGetCurrentScreen();
        display.WindowSetCurrentScreen(currentScreen);
        Check(display.WindowGetCurrentScreen() == currentScreen,
            "A request for the current Wayland screen leaves the window on that screen.");
        if (display.GetScreenCount() > 1)
            ExpectNotSupported(() => display.WindowSetCurrentScreen((currentScreen + 1) % display.GetScreenCount()));

        CheckWindowModes(display, window);
        CheckWindowFlags(display, window);

        var originalTitle = display.WindowGetTitle();
        var originalSize = display.WindowGetSize();
        var originalMinimum = display.WindowGetMinSize();
        var originalMaximum = display.WindowGetMaxSize();
        try
        {
            display.WindowSetMaxSize(new Vector2I(640, 480));
            display.WindowSetMinSize(new Vector2I(96, 72));
            Check(display.WindowGetMaxSize() == new Vector2I(640, 480) &&
                  display.WindowGetMinSize() == new Vector2I(96, 72),
                "Wayland minimum and maximum size requests are observable.");

            ExpectOutOfRange(() => display.WindowSetMaxSize(new Vector2I(95, 480)));
            ExpectOutOfRange(() => display.WindowSetMinSize(new Vector2I(641, 72)));
            Check(display.WindowGetMaxSize() == new Vector2I(640, 480) &&
                  display.WindowGetMinSize() == new Vector2I(96, 72),
                "Rejected size limits preserve the previous native constraints.");

            display.WindowSetMaxSize(new Vector2I(0, 480));
            display.WindowSetMinSize(new Vector2I(96, 0));
            Check(display.WindowGetMaxSize() == new Vector2I(0, 480) &&
                  display.WindowGetMinSize() == new Vector2I(96, 0),
                "Each zero size-limit component leaves only its own axis unbounded.");
            display.WindowSetMaxSize(new Vector2I(640, 480));
            display.WindowSetMinSize(new Vector2I(96, 72));

            display.WindowSetTitle("Electron2D Wayland window semantics");
            Check(display.WindowGetTitle() == SDL.GetWindowTitle(window),
                "The title getter observes the native title change.");
            display.WindowSetSize(new Vector2I(400, 300));
            display.ProcessEvents();
            Check(SDL.GetWindowSizeInPixels(window, out var width, out var height) &&
                  display.WindowGetSize() == new Vector2I(width, height) &&
                  display.WindowGetSizeWithDecorations() == new Vector2I(width, height),
                "Client and decorated size getters observe the same Wayland pixel size.");
            display.WindowSetSize(new Vector2I(0, -3));
            display.ProcessEvents();
            Check(display.WindowGetSize() is { X: >= 1, Y: >= 1 },
                "Wayland clamps nonpositive resize requests before applying native size limits.");

            display.WindowSetMaxSize(Vector2I.Zero);
            Check(display.WindowGetMaxSize() == Vector2I.Zero,
                "A zero maximum clears the native limit.");
        }
        finally
        {
            display.WindowSetMaxSize(Vector2I.Zero);
            display.WindowSetMinSize(originalMinimum);
            display.WindowSetMaxSize(originalMaximum);
            display.WindowSetTitle(originalTitle);
            display.WindowSetSize(originalSize);
            display.ProcessEvents();
        }
    }

    private static void CheckWindowModes(DisplayServer display, nint window)
    {
        var renderer = SDL.CreateRenderer(window, "software");
        Check(renderer != 0, $"Create a visible Wayland test surface: {SDL.GetError()}");
        try
        {
            Check(SDL.SetRenderDrawColor(renderer, 35, 80, 180, 255) &&
                  SDL.RenderClear(renderer) && SDL.RenderPresent(renderer),
                $"Present the Wayland test surface: {SDL.GetError()}");
            Check(SDL.SyncWindow(window), "The Wayland window reached its initial visible state.");

            var originalMode = display.WindowGetMode();
            try
            {
                display.WindowSetMode(DisplayServer.WindowMode.Maximized);
                Check(SDL.SyncWindow(window), "The compositor applied maximization.");
                display.ProcessEvents();
                Check(display.WindowGetMode() == DisplayServer.WindowMode.Maximized &&
                      (SDL.GetWindowFlags(window) & SDL.WindowFlags.Maximized) != 0,
                    "The maximized mode reflects the accepted compositor state.");

                display.WindowSetMode(DisplayServer.WindowMode.Windowed);
                Check(SDL.SyncWindow(window), "The compositor restored the floating window.");
                display.ProcessEvents();
                Check(display.WindowGetMode() == DisplayServer.WindowMode.Windowed,
                    "The windowed mode reflects restoration from maximized state.");

                display.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
                Check(SDL.SyncWindow(window), "The compositor applied ordinary fullscreen.");
                display.ProcessEvents();
                Check(display.WindowGetMode() == DisplayServer.WindowMode.Fullscreen &&
                      SDL.GetWindowFullscreenMode(window) is null,
                    "Wayland fullscreen covers the output without choosing a video mode.");
                Check(display.WindowIsMaximizeAllowed(),
                    "A resizable fullscreen window can leave fullscreen before requesting maximization.");

                display.WindowSetMode(DisplayServer.WindowMode.Windowed);
                Check(SDL.SyncWindow(window), "The compositor restored the window after fullscreen.");
                display.ProcessEvents();
                Check(display.WindowGetMode() == DisplayServer.WindowMode.Windowed,
                    "The windowed mode reflects restoration from fullscreen state.");

                display.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
                Check(SDL.SyncWindow(window), "The compositor applied the exclusive-fullscreen request.");
                display.ProcessEvents();
                Check(display.WindowGetMode() == DisplayServer.WindowMode.Fullscreen &&
                      SDL.GetWindowFullscreenMode(window) is null,
                    "Wayland exclusive fullscreen is ordinary compositor fullscreen.");
            }
            finally
            {
                display.WindowSetMode(originalMode);
                Check(SDL.SyncWindow(window), "The compositor restored the original window mode.");
                display.ProcessEvents();
            }
        }
        finally
        {
            SDL.DestroyRenderer(renderer);
        }
    }

    public static void CheckMinimizeLast(DisplayServer display)
    {
        if (display.GetName() != "Wayland")
            return;

        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is { Length: 1 }, "One native main window exists before minimization.");
        var window = windows![0];
        var renderer = SDL.CreateRenderer(window, "software");
        Check(renderer != 0, $"Create a visible Wayland test surface: {SDL.GetError()}");
        try
        {
            Check(SDL.SetRenderDrawColor(renderer, 35, 80, 180, 255) &&
                  SDL.RenderClear(renderer) && SDL.RenderPresent(renderer),
                $"Present the Wayland test surface: {SDL.GetError()}");
            Check(SDL.SyncWindow(window), "The Wayland window reached its visible state before minimization.");
            display.WindowSetMode(DisplayServer.WindowMode.Minimized);
            Check(SDL.SyncWindow(window), "The compositor handled the minimization request.");
            display.ProcessEvents();
            Check(display.WindowGetMode() == DisplayServer.WindowMode.Minimized &&
                  (SDL.GetWindowFlags(window) & SDL.WindowFlags.Minimized) != 0,
                "Wayland retains the requested minimized state until focus is restored.");
        }
        finally
        {
            SDL.DestroyRenderer(renderer);
        }
    }

    private static void CheckWindowFlags(DisplayServer display, nint window)
    {
        foreach (var flag in new[] { DisplayServer.WindowFlag.Borderless, DisplayServer.WindowFlag.ResizeDisabled })
        {
            var original = display.WindowGetFlag(flag);
            try
            {
                display.WindowSetFlag(flag, !original);
                Check(SDL.SyncWindow(window), $"The compositor applied {flag}.");
                display.ProcessEvents();
                Check(display.WindowGetFlag(flag) == !original,
                    $"The {flag} getter reflects the accepted SDL policy.");
                if (flag == DisplayServer.WindowFlag.ResizeDisabled)
                    Check(display.WindowIsMaximizeAllowed() == original,
                        "The maximize approximation follows the native resizable policy.");
            }
            finally
            {
                display.WindowSetFlag(flag, original);
                Check(SDL.SyncWindow(window), $"The compositor restored {flag}.");
                display.ProcessEvents();
            }
        }

        var unsupportedMask = SDL.WindowFlags.AlwaysOnTop | SDL.WindowFlags.NotFocusable;
        var originalUnsupported = SDL.GetWindowFlags(window) & unsupportedMask;
        foreach (var flag in new[] { DisplayServer.WindowFlag.AlwaysOnTop, DisplayServer.WindowFlag.NoFocus })
        {
            ExpectNotSupported(() => display.WindowGetFlag(flag));
            ExpectNotSupported(() => display.WindowSetFlag(flag, true));
            Check((SDL.GetWindowFlags(window) & unsupportedMask) == originalUnsupported,
                $"Rejecting {flag} leaves the native Wayland policy flags unchanged.");
        }
    }

    private static void ExpectOutOfRange(Action action)
    {
        try
        {
            action();
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }
        throw new InvalidOperationException("An incompatible window size limit was accepted.");
    }

    private static void ExpectNotSupported(Action action)
    {
        try
        {
            action();
        }
        catch (NotSupportedException)
        {
            return;
        }
        throw new InvalidOperationException("A global Wayland window position was reported as authoritative.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
