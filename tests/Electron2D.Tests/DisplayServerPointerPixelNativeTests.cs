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
