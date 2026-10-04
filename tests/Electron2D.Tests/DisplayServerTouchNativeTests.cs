using Electron2D;
using SDL3;

internal static class DisplayServerTouchNativeTests
{
    public static void Run(DisplayServer display)
    {
        DisplayServer.ProcessEvents();
        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is { Length: 1 }, "Touch checks require the main native window.");
        var size = DisplayServer.WindowGetSize();
        var previousEmulation = Input.EmulateMouseFromTouch;
        var probe = new TouchProbe();
        using var tree = new SceneTree(probe);
        Engine.Start(tree);
        try
        {
            Input.EmulateMouseFromTouch = false;
            Input.ReleasePressedEvents();
            var down = new SDL.Event
            {
                TFinger = new SDL.TouchFingerEvent
                {
                    Type = SDL.EventType.FingerDown,
                    WindowID = SDL.GetWindowID(windows![0]),
                    TouchID = 1,
                    FingerID = 1,
                    Timestamp = SDL.GetTicksNS() + 10_000_000_000UL,
                    X = 0.5f,
                    Y = 0.5f,
                },
            };
            var invalidDown = down;
            invalidDown.TFinger.X = float.NaN;
            var orphan = down;
            orphan.TFinger.Type = SDL.EventType.FingerMotion;
            var invalidMotion = orphan;
            invalidMotion.TFinger.Timestamp = down.TFinger.Timestamp + 500_000_000UL;
            invalidMotion.TFinger.Pressure = float.NaN;
            var motion = orphan;
            motion.TFinger.Timestamp = down.TFinger.Timestamp + 1_000_000_000UL;
            motion.TFinger.X = 0.75f;
            motion.TFinger.DX = 0.25f;
            motion.TFinger.Pressure = 0.5f;
            var invalidCancel = motion;
            invalidCancel.TFinger.Type = SDL.EventType.FingerCanceled;
            invalidCancel.TFinger.X = float.PositiveInfinity;
            var cancel = motion;
            cancel.TFinger.Type = SDL.EventType.FingerCanceled;
            var nextDown = down;
            nextDown.TFinger.FingerID = 2;
            var nextUp = nextDown;
            nextUp.TFinger.Type = SDL.EventType.FingerUp;

            Check(SDL.PushEvent(ref invalidDown) && SDL.PushEvent(ref orphan) &&
                  SDL.PushEvent(ref down) && SDL.PushEvent(ref down) &&
                  SDL.PushEvent(ref invalidMotion) && SDL.PushEvent(ref motion) &&
                  SDL.PushEvent(ref invalidCancel) && SDL.PushEvent(ref cancel) &&
                  SDL.PushEvent(ref nextDown) && SDL.PushEvent(ref nextUp),
                "SDL accepts malformed and valid touch events in one queue.");
            try
            {
                DisplayServer.ProcessEvents();
                throw new InvalidOperationException("Malformed native touch input must fail.");
            }
            catch (AggregateException errors)
            {
                Check(errors.InnerExceptions.Count == 4 &&
                      errors.InnerExceptions.Count(error => error is ArgumentOutOfRangeException) == 3 &&
                      errors.InnerExceptions.Count(error => error is InvalidOperationException) == 1 &&
                      probe.Events.Count == 5 &&
                      probe.Events[0] is InputEventScreenTouch { Index: 0, Pressed: true, Canceled: false, DoubleTap: false } first &&
                      first.Position == new Vector2(size.X * 0.5f, size.Y * 0.5f) &&
                      probe.Events[1] is InputEventScreenDrag { Index: 0, Pressure: 0.5f } drag &&
                      drag.Position == new Vector2(size.X * 0.75f, size.Y * 0.5f) &&
                      drag.Relative == new Vector2(size.X * 0.25f, 0f) &&
                      drag.ScreenRelative == drag.Relative && drag.Velocity == drag.Relative &&
                      drag.ScreenVelocity == drag.Velocity &&
                      probe.Events[2] is InputEventScreenTouch { Index: 0, Pressed: false, Canceled: true } &&
                      probe.Events[3] is InputEventScreenTouch { Index: 0, Pressed: true } &&
                      probe.Events[4] is InputEventScreenTouch { Index: 0, Pressed: false },
                    "Touch failures preserve contact index and timestamp while later events continue in order.");
            }
            Console.WriteLine("Native touch contact validation passed.");
        }
        finally
        {
            probe.Clear();
            Input.ReleasePressedEvents();
            Input.EmulateMouseFromTouch = previousEmulation;
            Engine.Stop();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class TouchProbe : Entity
    {
        public List<InputEvent> Events { get; } = [];

        public TouchProbe() => InputEnabled = true;

        protected override void OnInput(InputEvent inputEvent) => Events.Add((InputEvent)inputEvent.Duplicate());

        public void Clear()
        {
            foreach (var inputEvent in Events)
                inputEvent.Dispose();
            Events.Clear();
        }
    }
}
