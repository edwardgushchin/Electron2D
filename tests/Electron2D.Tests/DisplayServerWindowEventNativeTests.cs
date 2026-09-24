using Electron2D;
using SDL3;

internal static class DisplayServerWindowEventNativeTests
{
    public static void Run()
    {
        using var display = DisplayServer.Open("Electron2D close event test", new Vector2i(480, 360));
        Check(display.GetName() == "Wayland", "The native close test requires Wayland.");
        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is { Length: 1 }, "The native close test needs one window.");
        var window = windows![0];
        var windowId = SDL.GetWindowID(window);
        var renderer = SDL.CreateRenderer(window, "software");
        Check(renderer != 0, $"Cannot draw the native close test window: {SDL.GetError()}");
        var closeRequests = 0;
        var focusIn = 0;
        var focusOut = 0;
        var mouseEnter = 0;
        var mouseExit = 0;
        void OnClose() => closeRequests++;
        void OnFocus(bool focused)
        {
            if (focused)
                focusIn++;
            else
                focusOut++;
        }
        void OnEnter() => mouseEnter++;
        void OnExit() => mouseExit++;
        display.CloseRequested += OnClose;
        display.WindowFocusChanged += OnFocus;
        display.WindowMouseEntered += OnEnter;
        display.WindowMouseExited += OnExit;
        try
        {
            Check(SDL.SetRenderDrawColor(renderer, 25, 115, 165, 255), SDL.GetError());
            Console.WriteLine("Blue Wayland window ready: click its title-bar Close button within 60 seconds.");
            var deadline = DateTime.UtcNow.AddSeconds(60);
            while (DateTime.UtcNow < deadline && closeRequests == 0)
            {
                display.ProcessEvents();
                Check(SDL.RenderClear(renderer) && SDL.RenderPresent(renderer), SDL.GetError());
                Thread.Sleep(20);
            }
            Check(closeRequests == 1, $"Expected one real compositor close request, observed {closeRequests}.");
            Check(ReferenceEquals(DisplayServer.Instance, display) && SDL.GetWindowID(window) == windowId &&
                  (SDL.GetWindowFlags(window) & SDL.WindowFlags.Hidden) == 0,
                "A compositor close request must leave the native window and display server alive.");
            Console.WriteLine($"Wayland close callback passed; focus in/out={focusIn}/{focusOut}, pointer enter/exit={mouseEnter}/{mouseExit}.");
        }
        finally
        {
            display.CloseRequested -= OnClose;
            display.WindowFocusChanged -= OnFocus;
            display.WindowMouseEntered -= OnEnter;
            display.WindowMouseExited -= OnExit;
            SDL.DestroyRenderer(renderer);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
