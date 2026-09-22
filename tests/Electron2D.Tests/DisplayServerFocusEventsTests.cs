using Electron2D;
using SDL3;

internal static class DisplayServerFocusEventsTests
{
    public static void Run(DisplayServer display)
    {
        display.ProcessEvents();
        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is { Length: 1 }, "The focus probe needs one native window.");
        var windowId = SDL.GetWindowID(windows![0]);
        var order = new List<string>();
        using var loop = new FocusProbeLoop(order);
        var previousAccumulation = Input.Instance.UseAccumulatedInput;
        var previousTouchEmulation = Input.Instance.EmulateMouseFromTouch;
        Engine.Instance.Start(loop);
        try
        {
            Input.Instance.UseAccumulatedInput = false;
            Input.Instance.EmulateMouseFromTouch = true;
            Input.Instance.ReleasePressedEvents();
            void OnFocus(bool focused) => order.Add($"window:{focused}:{Input.Instance.IsKeyPressed(Key.A)}");
            display.WindowFocusChanged += OnFocus;
            try
            {
                PushWindow(windowId, SDL.EventType.WindowFocusGained);
                display.ProcessEvents();
                Check(order.SequenceEqual(["window:True:False", "loop:True:False"]),
                    "Focus gain reaches the window callback before the application notification.");
                order.Clear();

                PushKey(windowId, SDL.EventType.KeyDown);
                display.ProcessEvents();
                Check(Input.Instance.IsKeyPressed(Key.A), "The key press reaches Input before focus loss.");
                PushWindow(windowId, SDL.EventType.WindowFocusLost);
                display.ProcessEvents();
                Check(order.SequenceEqual(["window:False:True", "loop:False:True"]) &&
                      !Input.Instance.IsKeyPressed(Key.A),
                    "Window and application focus callbacks observe pressed state before release.");

                PushKey(windowId, SDL.EventType.KeyDown);
                PushMouse(windowId, SDL.EventType.MouseButtonDown);
                display.ProcessEvents();
                Check(Input.Instance.IsKeyPressed(Key.A) && Input.Instance.IsMouseButtonPressed(MouseButton.Left),
                    "Native events for this window are not discarded solely because keyboard focus was lost.");
                PushKey(windowId, SDL.EventType.KeyUp);
                PushMouse(windowId, SDL.EventType.MouseButtonUp);
                display.ProcessEvents();

                PushTouch(windowId, SDL.EventType.FingerDown);
                display.ProcessEvents();
                Check(Input.Instance.IsMouseButtonPressed(MouseButton.Left),
                    "A touch contact can begin without keyboard focus.");
                PushTouch(windowId, SDL.EventType.FingerUp);
                display.ProcessEvents();
                Check(!Input.Instance.IsMouseButtonPressed(MouseButton.Left),
                    "The touch contact releases its emulated mouse button.");

                var callbackFailure = new InvalidOperationException("window focus failure");
                var notificationFailure = new InvalidOperationException("application focus failure");
                Action<bool> failFocus = focused =>
                {
                    if (!focused)
                        throw callbackFailure;
                };
                display.WindowFocusChanged += failFocus;
                loop.FocusOutFailure = notificationFailure;
                try
                {
                    PushWindow(windowId, SDL.EventType.WindowFocusGained);
                    display.ProcessEvents();
                    PushKey(windowId, SDL.EventType.KeyDown);
                    display.ProcessEvents();
                    order.Clear();
                    PushWindow(windowId, SDL.EventType.WindowFocusLost);
                    try
                    {
                        display.ProcessEvents();
                        throw new InvalidOperationException("Both focus failures must be reported.");
                    }
                    catch (AggregateException errors)
                    {
                        Check(errors.Flatten().InnerExceptions.Count == 2 &&
                              errors.Flatten().InnerExceptions.Contains(callbackFailure) &&
                              errors.Flatten().InnerExceptions.Contains(notificationFailure) &&
                              order.SequenceEqual(["window:False:True", "loop:False:True"]) &&
                              !Input.Instance.IsKeyPressed(Key.A),
                            "Both focus failures survive while pressed input is released.");
                    }
                }
                finally
                {
                    loop.FocusOutFailure = null;
                    display.WindowFocusChanged -= failFocus;
                }
            }
            finally
            {
                display.WindowFocusChanged -= OnFocus;
            }

            PushWindow(windowId, SDL.EventType.WindowMouseLeave);
            display.ProcessEvents();
            var hover = new List<string>();
            void Enter() => hover.Add("enter");
            void Exit() => hover.Add("exit");
            display.WindowMouseEntered += Enter;
            display.WindowMouseExited += Exit;
            try
            {
                PushWindow(windowId, SDL.EventType.WindowMouseLeave);
                PushWindow(windowId, SDL.EventType.WindowMouseEnter);
                PushWindow(windowId, SDL.EventType.WindowMouseEnter);
                PushWindow(windowId, SDL.EventType.WindowMouseLeave);
                PushWindow(windowId, SDL.EventType.WindowMouseLeave);
                display.ProcessEvents();
                Check(hover.SequenceEqual(["enter", "exit"]),
                    "Repeated hover messages publish only effective transitions.");
            }
            finally
            {
                display.WindowMouseEntered -= Enter;
                display.WindowMouseExited -= Exit;
            }
        }
        finally
        {
            Input.Instance.UseAccumulatedInput = previousAccumulation;
            Input.Instance.EmulateMouseFromTouch = previousTouchEmulation;
            Input.Instance.ReleasePressedEvents();
            Engine.Instance.Stop();
        }
    }

    private static void PushWindow(uint windowId, SDL.EventType type)
    {
        var @event = new SDL.Event { Window = new SDL.WindowEvent { Type = type, WindowID = windowId } };
        Check(SDL.PushEvent(ref @event), "SDL accepts a synthetic window event.");
    }

    private static void PushKey(uint windowId, SDL.EventType type)
    {
        var @event = new SDL.Event
        {
            Key = new SDL.KeyboardEvent
            {
                Type = type,
                WindowID = windowId,
                Key = SDL.Keycode.A,
                Scancode = SDL.Scancode.A,
                Down = type == SDL.EventType.KeyDown,
            },
        };
        Check(SDL.PushEvent(ref @event), "SDL accepts a synthetic key event.");
    }

    private static void PushMouse(uint windowId, SDL.EventType type)
    {
        var @event = new SDL.Event
        {
            Button = new SDL.MouseButtonEvent
            {
                Type = type,
                WindowID = windowId,
                Button = 1,
                Down = type == SDL.EventType.MouseButtonDown,
            },
        };
        Check(SDL.PushEvent(ref @event), "SDL accepts a synthetic pointer button event.");
    }

    private static void PushTouch(uint windowId, SDL.EventType type)
    {
        var @event = new SDL.Event
        {
            TFinger = new SDL.TouchFingerEvent
            {
                Type = type,
                WindowID = windowId,
                TouchID = 1,
                FingerID = 1,
                X = 0.5f,
                Y = 0.5f,
            },
        };
        Check(SDL.PushEvent(ref @event), "SDL accepts a synthetic touch event.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class FocusProbeLoop(List<string> order) : MainLoop
    {
        public Exception? FocusOutFailure { get; set; }

        protected override void OnNotification(int what)
        {
            if (what is NotificationApplicationFocusIn or NotificationApplicationFocusOut)
            {
                var focused = what == NotificationApplicationFocusIn;
                order.Add($"loop:{focused}:{Input.Instance.IsKeyPressed(Key.A)}");
                if (!focused && FocusOutFailure is not null)
                    throw FocusOutFailure;
            }
            base.OnNotification(what);
        }
    }
}
