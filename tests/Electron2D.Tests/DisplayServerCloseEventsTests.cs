using Electron2D;
using SDL3;

internal static class DisplayServerCloseEventsTests
{
    public static void Run(DisplayServer display)
    {
        display.ProcessEvents();
        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is { Length: 1 }, "The close probe needs one native window.");
        var window = windows![0];
        var windowId = SDL.GetWindowID(window);
        var order = new List<string>();
        Action onClose = () => order.Add("close");
        Action onQuit = () => order.Add("quit");
        display.CloseRequested += onClose;
        display.QuitRequested += onQuit;
        try
        {
            PushClose(windowId + 1);
            PushClose(windowId);
            PushQuit();
            PushClose(windowId);
            display.ProcessEvents();
            Check(order.SequenceEqual(["close", "quit", "close"]),
                "Only main-window close requests are delivered, in native queue order.");
            Check(ReferenceEquals(DisplayServer.Instance, display) && SDL.GetWindowID(window) == windowId,
                "A close request does not destroy the main window or server.");

            order.Clear();
            var failure = new InvalidOperationException("injected close failure");
            Action failClose = () => throw failure;
            display.CloseRequested += failClose;
            try
            {
                PushClose(windowId);
                PushQuit();
                try
                {
                    display.ProcessEvents();
                    throw new InvalidOperationException("The failing close callback must be reported.");
                }
                catch (AggregateException errors)
                {
                    Check(errors.InnerExceptions is [var reported] && ReferenceEquals(reported, failure) &&
                          order.SequenceEqual(["close", "quit"]) && SDL.GetWindowID(window) == windowId,
                        "A failing close callback is aggregated after later events without closing the window.");
                }
            }
            finally
            {
                display.CloseRequested -= failClose;
            }

            order.Clear();
            PushClose(windowId);
            PushQuit();
            display.ForceProcessAndDropEvents();
            Check(order.SequenceEqual(["close", "quit"]) && SDL.GetWindowID(window) == windowId,
                "Dropping pending input retains ordered window-close and quit notifications.");
        }
        finally
        {
            display.CloseRequested -= onClose;
            display.QuitRequested -= onQuit;
        }
    }

    private static void PushClose(uint windowId)
    {
        var @event = new SDL.Event
        {
            Window = new SDL.WindowEvent { Type = SDL.EventType.WindowCloseRequested, WindowID = windowId },
        };
        Check(SDL.PushEvent(ref @event), "SDL accepts a synthetic close request.");
    }

    private static void PushQuit()
    {
        var @event = new SDL.Event { Type = (uint)SDL.EventType.Quit };
        Check(SDL.PushEvent(ref @event), "SDL accepts a synthetic quit request.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
