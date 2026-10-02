using Electron2D;
using SDL3;

internal static class DisplayServerPointerPixelNativeTests
{
    public static void Run(DisplayServer display)
    {
        if (display.GetName() != "Wayland")
            return;

        display.ProcessEvents();
        var windows = SDL.GetWindows(out var count);
        Check(count == 1 && windows is { Length: 1 }, "The pointer pixel test needs one native window.");
        var window = windows![0];
        var density = SDL.GetWindowPixelDensity(window);
        Check(float.IsFinite(density) && density > 0f, "The Wayland window reports a positive pixel density.");
        var id = SDL.GetWindowID(window);
        var probe = new PointerProbe();
        using var tree = new SceneTree(probe);
        var previousAccumulation = Input.Instance.UseAccumulatedInput;
        var previousMouseEmulation = Input.Instance.EmulateMouseFromTouch;
        var previousTouchEmulation = Input.Instance.EmulateTouchFromMouse;
        Engine.Instance.Start(tree);
        try
        {
            Input.Instance.UseAccumulatedInput = false;
            Input.Instance.EmulateMouseFromTouch = false;
            Input.Instance.EmulateTouchFromMouse = false;

            var motion = new SDL.Event
            {
                Motion = new SDL.MouseMotionEvent
                {
                    Type = SDL.EventType.MouseMotion,
                    WindowID = id,
                    X = 80f,
                    Y = 60f,
                    XRel = 4f,
                    YRel = 8f,
                },
            };
            var press = new SDL.Event
            {
                Button = new SDL.MouseButtonEvent
                {
                    Type = SDL.EventType.MouseButtonDown,
                    WindowID = id,
                    Button = 1,
                    Down = true,
                    X = 80f,
                    Y = 60f,
                },
            };
            var release = press;
            release.Button.Type = SDL.EventType.MouseButtonUp;
            release.Button.Down = false;
            var wheel = new SDL.Event
            {
                Wheel = new SDL.MouseWheelEvent
                {
                    Type = SDL.EventType.MouseWheel,
                    WindowID = id,
                    Y = 1f,
                    MouseX = 80f,
                    MouseY = 60f,
                },
            };
            Check(SDL.PushEvent(ref motion) && SDL.PushEvent(ref press) &&
                  SDL.PushEvent(ref release) && SDL.PushEvent(ref wheel),
                "SDL accepts the pointer pixel-space probes.");
            display.ProcessEvents();

            var expectedPosition = new Vector2(80f * density, 60f * density);
            var expectedRelative = new Vector2(4f * density, 8f * density);
            Check(probe.Events.Count == 5 &&
                  probe.Events[0] is InputEventMouseMotion moved &&
                  moved.Position == expectedPosition && moved.GlobalPosition == expectedPosition &&
                  moved.Relative == expectedRelative && moved.ScreenRelative == expectedRelative &&
                  probe.Events[1] is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } down &&
                  down.Position == expectedPosition && down.GlobalPosition == expectedPosition &&
                  probe.Events[2] is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } up &&
                  up.Position == expectedPosition && up.GlobalPosition == expectedPosition &&
                  probe.Events[3] is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true } wheelDown &&
                  wheelDown.Position == expectedPosition && wheelDown.GlobalPosition == expectedPosition &&
                  probe.Events[4] is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: false } wheelUp &&
                  wheelUp.Position == expectedPosition && wheelUp.GlobalPosition == expectedPosition,
                "Mouse motion, buttons, and both wheel phases use physical Wayland client pixels.");

            probe.Clear();
            var invalidMotion = motion;
            invalidMotion.Motion.X = float.NaN;
            var invalidPress = press;
            invalidPress.Button.X = float.PositiveInfinity;
            var invalidWheel = wheel;
            invalidWheel.Wheel.Y = float.PositiveInfinity;
            var validMotion = motion;
            validMotion.Motion.X = 21f;
            Check(SDL.PushEvent(ref invalidMotion) && SDL.PushEvent(ref invalidPress) &&
                  SDL.PushEvent(ref invalidWheel) && SDL.PushEvent(ref validMotion) && SDL.PushEvent(ref wheel),
                "SDL accepts malformed and valid pointer events in one queue.");
            try
            {
                display.ProcessEvents();
                throw new InvalidOperationException("Malformed native pointer values must fail.");
            }
            catch (AggregateException errors)
            {
                Check(errors.InnerExceptions.Count == 3 &&
                      errors.InnerExceptions.All(error => error is ArgumentOutOfRangeException) &&
                      probe.Events.Count == 3 &&
                      probe.Events[0] is InputEventMouseMotion { ButtonMask: MouseButtonMask.None } valid &&
                      valid.Position == new Vector2(21f * density, 60f * density) &&
                      probe.Events[1] is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true, Factor: 1f } &&
                      probe.Events[2] is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: false, Factor: 1f },
                    "Invalid native pointer values leave button state unchanged and later events continue in order.");
            }

            probe.Clear();
            var preciseWheel = wheel;
            preciseWheel.Wheel.Y = 1.25f;
            Check(SDL.PushEvent(ref preciseWheel), "SDL accepts a fractional wheel amount.");
            display.ProcessEvents();
            Check(probe.Events.Count == 2 &&
                  probe.Events[0] is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true, Factor: 1.25f } &&
                  probe.Events[1] is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: false, Factor: 1.25f },
                "The native wheel adapter keeps a fractional factor on both phases.");

            probe.Clear();
            var doublePress = press;
            doublePress.Button.Clicks = 2;
            var doubleRelease = release;
            doubleRelease.Button.Clicks = 2;
            Check(SDL.PushEvent(ref doublePress) && SDL.PushEvent(ref doubleRelease),
                "SDL accepts a double-click press and release.");
            display.ProcessEvents();
            Check(probe.Events.Count == 2 &&
                  probe.Events[0] is InputEventMouseButton { DoubleClick: true, Pressed: true, ButtonMask: MouseButtonMask.Left } &&
                  probe.Events[1] is InputEventMouseButton { DoubleClick: true, Pressed: false, ButtonMask: MouseButtonMask.None },
                "The native click count and held-button transitions survive both phases.");

            probe.Clear();
            var timestampAnchor = motion;
            timestampAnchor.Motion.Timestamp = SDL.GetTicksNS() + 10_000_000_000UL;
            timestampAnchor.Motion.XRel = 0f;
            timestampAnchor.Motion.YRel = 0f;
            var timedMotion = motion;
            timedMotion.Motion.Timestamp = timestampAnchor.Motion.Timestamp + 1_000_000_000UL;
            Check(SDL.PushEvent(ref timestampAnchor) && SDL.PushEvent(ref timedMotion),
                "SDL accepts consecutive pointer motions with explicit timestamps.");
            display.ProcessEvents();
            Check(probe.Events.Count == 2 && probe.Events[1] is InputEventMouseMotion timed &&
                  timed.Velocity == new Vector2(4f * density, 8f * density) &&
                  timed.ScreenVelocity == timed.Velocity,
                "Native motion velocity uses the original timestamp interval and client pixel density.");

            probe.Clear();
            var previousMode = display.MouseGetMode();
            try
            {
                display.MouseSetMode(MouseMode.Captured);
                display.ProcessEvents();
                probe.Clear();
                var capturedMotion = motion;
                capturedMotion.Motion.Timestamp = timedMotion.Motion.Timestamp + 1_000_000_000UL;
                capturedMotion.Motion.XRel = 5f;
                capturedMotion.Motion.YRel = 3f;
                Check(SDL.PushEvent(ref capturedMotion), "SDL accepts a captured pointer motion.");
                display.ProcessEvents();
                Check(probe.Events.Any(inputEvent => inputEvent is InputEventMouseMotion moved &&
                    moved.ScreenRelative == new Vector2(5f * density, 3f * density) &&
                    moved.Velocity == Vector2.Zero && moved.ScreenVelocity == Vector2.Zero),
                    "Captured motion retains relative movement but reports zero cursor velocities.");
            }
            finally
            {
                display.MouseSetMode(previousMode);
            }
            Console.WriteLine($"Wayland pointer pixel probe: density {density}; fractional path {(density % 1f == 0f ? "not exercised" : "exercised")}.");
        }
        finally
        {
            Input.Instance.ReleasePressedEvents();
            Input.Instance.UseAccumulatedInput = previousAccumulation;
            Input.Instance.EmulateMouseFromTouch = previousMouseEmulation;
            Input.Instance.EmulateTouchFromMouse = previousTouchEmulation;
            Engine.Instance.Stop();
            probe.Clear();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class PointerProbe : Entity
    {
        public List<InputEvent> Events { get; } = [];

        public PointerProbe() => InputEnabled = true;

        protected override void OnInput(InputEvent @event) => Events.Add((InputEvent)@event.Duplicate());

        public void Clear()
        {
            foreach (var @event in Events)
                @event.Dispose();
            Events.Clear();
        }
    }
}
