using Electron2D;
using SDL3;

internal static class DisplayServerWindowNativeSemanticsTests
{
    public static void Run(DisplayServer display)
    {
        if (DisplayServer.GetName() != "Wayland")
            return;

        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is { Length: 1 }, "One native main window exists.");
        var window = windows![0];
        Check(DisplayServer.WindowIsFocused() == (SDL.GetMouseFocus() == window),
            "Wayland focus follows the window under the pointer.");
        Check(DisplayServer.WindowGetSizeWithDecorations() == DisplayServer.WindowGetSize(),
            "Wayland decorated size uses the client size when server decorations cannot be measured.");
        ExpectNotSupported(() => DisplayServer.WindowGetPosition());
        ExpectNotSupported(() => DisplayServer.WindowGetPositionWithDecorations());
        ExpectNotSupported(() => DisplayServer.WindowSetPosition(Vector2i.Zero));
        var currentScreen = DisplayServer.WindowGetCurrentScreen();
        DisplayServer.WindowSetCurrentScreen(currentScreen);
        Check(DisplayServer.WindowGetCurrentScreen() == currentScreen,
            "A request for the current Wayland screen leaves the window on that screen.");
        if (DisplayServer.GetScreenCount() > 1)
            ExpectNotSupported(() => DisplayServer.WindowSetCurrentScreen((currentScreen + 1) % DisplayServer.GetScreenCount()));

        CheckWindowModes(display, window);
        CheckWindowFlags(display, window);

        var originalTitle = display.WindowGetTitle();
        var originalSize = DisplayServer.WindowGetSize();
        var originalMinimum = DisplayServer.WindowGetMinSize();
        var originalMaximum = DisplayServer.WindowGetMaxSize();
        try
        {
            DisplayServer.WindowSetMaxSize(new Vector2i(640, 480));
            DisplayServer.WindowSetMinSize(new Vector2i(96, 72));
            Check(DisplayServer.WindowGetMaxSize() == new Vector2i(640, 480) &&
                  DisplayServer.WindowGetMinSize() == new Vector2i(96, 72),
                "Wayland minimum and maximum size requests are observable.");

            ExpectOutOfRange(() => DisplayServer.WindowSetMaxSize(new Vector2i(95, 480)));
            ExpectOutOfRange(() => DisplayServer.WindowSetMinSize(new Vector2i(641, 72)));
            Check(DisplayServer.WindowGetMaxSize() == new Vector2i(640, 480) &&
                  DisplayServer.WindowGetMinSize() == new Vector2i(96, 72),
                "Rejected size limits preserve the previous native constraints.");

            DisplayServer.WindowSetMaxSize(new Vector2i(0, 480));
            DisplayServer.WindowSetMinSize(new Vector2i(96, 0));
            Check(DisplayServer.WindowGetMaxSize() == new Vector2i(0, 480) &&
                  DisplayServer.WindowGetMinSize() == new Vector2i(96, 0),
                "Each zero size-limit component leaves only its own axis unbounded.");
            DisplayServer.WindowSetMaxSize(new Vector2i(640, 480));
            DisplayServer.WindowSetMinSize(new Vector2i(96, 72));

            DisplayServer.WindowSetTitle("Electron2D Wayland window semantics");
            Check(display.WindowGetTitle() == SDL.GetWindowTitle(window),
                "The title getter observes the native title change.");
            DisplayServer.WindowSetSize(new Vector2i(400, 300));
            DisplayServer.ProcessEvents();
            Check(SDL.GetWindowSizeInPixels(window, out var width, out var height) &&
                  DisplayServer.WindowGetSize() == new Vector2i(width, height) &&
                  DisplayServer.WindowGetSizeWithDecorations() == new Vector2i(width, height),
                "Client and decorated size getters observe the same Wayland pixel size.");
            DisplayServer.WindowSetSize(new Vector2i(0, -3));
            DisplayServer.ProcessEvents();
            Check(DisplayServer.WindowGetSize() is { X: >= 1, Y: >= 1 },
                "Wayland clamps nonpositive resize requests before applying native size limits.");

            DisplayServer.WindowSetMaxSize(Vector2i.Zero);
            Check(DisplayServer.WindowGetMaxSize() == Vector2i.Zero,
                "A zero maximum clears the native limit.");
        }
        finally
        {
            DisplayServer.WindowSetMaxSize(Vector2i.Zero);
            DisplayServer.WindowSetMinSize(originalMinimum);
            DisplayServer.WindowSetMaxSize(originalMaximum);
            DisplayServer.WindowSetTitle(originalTitle);
            DisplayServer.WindowSetSize(originalSize);
            DisplayServer.ProcessEvents();
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

            var originalMode = DisplayServer.WindowGetMode();
            try
            {
                DisplayServer.WindowSetMode(WindowMode.Maximized);
                Check(SDL.SyncWindow(window), "The compositor applied maximization.");
                DisplayServer.ProcessEvents();
                Check(DisplayServer.WindowGetMode() == WindowMode.Maximized &&
                      (SDL.GetWindowFlags(window) & SDL.WindowFlags.Maximized) != 0,
                    "The maximized mode reflects the accepted compositor state.");

                DisplayServer.WindowSetMode(WindowMode.Windowed);
                Check(SDL.SyncWindow(window), "The compositor restored the floating window.");
                DisplayServer.ProcessEvents();
                Check(DisplayServer.WindowGetMode() == WindowMode.Windowed,
                    "The windowed mode reflects restoration from maximized state.");

                DisplayServer.WindowSetMode(WindowMode.Fullscreen);
                Check(SDL.SyncWindow(window), "The compositor applied ordinary fullscreen.");
                DisplayServer.ProcessEvents();
                Check(DisplayServer.WindowGetMode() == WindowMode.Fullscreen &&
                      SDL.GetWindowFullscreenMode(window) is null,
                    "Wayland fullscreen covers the output without choosing a video mode.");
                Check(DisplayServer.WindowIsMaximizeAllowed(),
                    "A resizable fullscreen window can leave fullscreen before requesting maximization.");

                DisplayServer.WindowSetMode(WindowMode.Windowed);
                Check(SDL.SyncWindow(window), "The compositor restored the window after fullscreen.");
                DisplayServer.ProcessEvents();
                Check(DisplayServer.WindowGetMode() == WindowMode.Windowed,
                    "The windowed mode reflects restoration from fullscreen state.");

                DisplayServer.WindowSetMode(WindowMode.ExclusiveFullscreen);
                Check(SDL.SyncWindow(window), "The compositor applied the exclusive-fullscreen request.");
                DisplayServer.ProcessEvents();
                Check(DisplayServer.WindowGetMode() == WindowMode.Fullscreen &&
                      SDL.GetWindowFullscreenMode(window) is null,
                    "Wayland exclusive fullscreen is ordinary compositor fullscreen.");
            }
            finally
            {
                DisplayServer.WindowSetMode(originalMode);
                Check(SDL.SyncWindow(window), "The compositor restored the original window mode.");
                DisplayServer.ProcessEvents();
            }
        }
        finally
        {
            SDL.DestroyRenderer(renderer);
        }
    }

    public static void CheckMinimizeLast(DisplayServer display)
    {
        if (DisplayServer.GetName() != "Wayland")
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
            DisplayServer.WindowSetMode(WindowMode.Minimized);
            Check(SDL.SyncWindow(window), "The compositor handled the minimization request.");
            DisplayServer.ProcessEvents();
            Check(DisplayServer.WindowGetMode() == WindowMode.Minimized &&
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
        foreach (var flag in new[] { WindowFlag.Borderless, WindowFlag.ResizeDisabled })
        {
            var original = DisplayServer.WindowGetFlag(flag);
            try
            {
                DisplayServer.WindowSetFlag(flag, !original);
                Check(SDL.SyncWindow(window), $"The compositor applied {flag}.");
                DisplayServer.ProcessEvents();
                Check(DisplayServer.WindowGetFlag(flag) == !original,
                    $"The {flag} getter reflects the accepted SDL policy.");
                if (flag == WindowFlag.ResizeDisabled)
                    Check(DisplayServer.WindowIsMaximizeAllowed() == original,
                        "The maximize approximation follows the native resizable policy.");
            }
            finally
            {
                DisplayServer.WindowSetFlag(flag, original);
                Check(SDL.SyncWindow(window), $"The compositor restored {flag}.");
                DisplayServer.ProcessEvents();
            }
        }

        var unsupportedMask = SDL.WindowFlags.AlwaysOnTop | SDL.WindowFlags.NotFocusable;
        var originalUnsupported = SDL.GetWindowFlags(window) & unsupportedMask;
        foreach (var flag in new[] { WindowFlag.AlwaysOnTop, WindowFlag.NoFocus })
        {
            ExpectNotSupported(() => DisplayServer.WindowGetFlag(flag));
            ExpectNotSupported(() => DisplayServer.WindowSetFlag(flag, true));
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
