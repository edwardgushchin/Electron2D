using System.Reflection;
using Electron2D;
using SDL3;

internal static class InputActionNativeTests
{
    internal static void Run()
    {
        const string action = "native_input_action";
        using var display = DisplayServer.Open("Action input contract", new Vector2i(160, 120), hidden: true);
        Check(SDL.GetCurrentVideoDriver() == "windows", "The Windows SDL host is active.");
        var handles = SDL.GetWindows(out var count);
        Check(count == 1, "One native window.");
        var nativeId = SDL.GetWindowID(handles![0]);
        var events = DisplayServer.GetWindowEvents();
        using var binding = new InputEventKey { Keycode = Key.F8 };
        InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, binding);
        using var node = new Probe();
        using var tree = new SceneTree(node);
        Engine.Start(tree);
        try
        {
            DisplayServer.ProcessEvents();
            Input.ReleasePressedEvents();
            var order = new List<string>();
            events.Input += value =>
            {
                if (value is InputEventKey { Keycode: Key.F8 })
                {
                    Check(Input.IsActionPressed(action) == value.IsPressed() &&
                        Input.IsKeyPressed(Key.F8) == value.IsPressed(), "Native subscriber sees committed key and action state.");
                    Check(value.IsPressed() ? Input.IsActionJustPressedByEvent(action, value) :
                        Input.IsActionJustReleasedByEvent(action, value), "Parsed SDL key is the transition source.");
                    Reject<InvalidOperationException>(Input.FlushBufferedEvents);
                    order.Add("native");
                }
            };
            node.DuringInput = value =>
            {
                if (value is InputEventKey { Keycode: Key.F8 })
                    order.Add("scene");
            };
            var key = new SDL.Event
            {
                Key = new SDL.KeyboardEvent
                {
                    Type = SDL.EventType.KeyDown,
                    WindowID = nativeId,
                    Key = SDL.Keycode.F8,
                    Scancode = SDL.Scancode.F8,
                    Down = true,
                }
            };
            Check(SDL.PushEvent(ref key), "Queue key press.");
            DisplayServer.ProcessEvents();
            key.Key.Type = SDL.EventType.KeyUp;
            key.Key.Down = false;
            Check(SDL.PushEvent(ref key), "Queue key release.");
            DisplayServer.ProcessEvents();
            Check(order.SequenceEqual(["native", "scene", "native", "scene"]) && !Input.IsActionPressed(action),
                "Both SDL key transitions dispatch through native and scene stages in order.");

            var motions = new List<float>();
            node.DuringInput = value =>
            {
                if (value is InputEventMouseMotion { Position.Y: 31 } motion)
                {
                    Check(Input.LastMouseVelocity == motion.Velocity, "Native motion state precedes the scene callback.");
                    motions.Add(motion.Relative.X);
                }
            };
            var motion = new SDL.Event
            {
                Motion = new SDL.MouseMotionEvent
                {
                    Type = SDL.EventType.MouseMotion,
                    WindowID = nativeId,
                    Which = 71,
                    X = 20,
                    Y = 31,
                    XRel = 2,
                    Timestamp = SDL.GetTicksNS() + 1_000_000_000,
                }
            };
            void QueuePair()
            {
                motion.Motion.XRel = 2;
                motion.Motion.Timestamp += 1_000_000_000;
                Check(SDL.PushEvent(ref motion), "Queue first motion.");
                motion.Motion.XRel = 3;
                motion.Motion.Timestamp += 1_000_000_000;
                Check(SDL.PushEvent(ref motion), "Queue second motion.");
            }
            Input.UseAccumulatedInput = true;
            QueuePair();
            DisplayServer.ProcessEvents();
            Check(motions.SequenceEqual([5f]), "Automatic end-of-batch flush combines consecutive same-device motions.");
            motions.Clear();
            Input.UseAccumulatedInput = false;
            QueuePair();
            DisplayServer.ProcessEvents();
            Check(motions.SequenceEqual([2f, 3f]), "Disabled accumulation delivers the original motion order.");

            // The pump normally drains pending motion before returning to the caller.
            var accumulate = typeof(DisplayServer).GetMethod("AccumulateMouseMotion", BindingFlags.NonPublic | BindingFlags.Instance)!;
            motions.Clear();
            accumulate.Invoke(display, [motion.Motion]);
            Task.Run(() => Reject<InvalidOperationException>(Input.FlushBufferedEvents)).GetAwaiter().GetResult();
            Check(motions.Count == 0, "Off-owner flush does not consume pending motion.");
            Input.FlushBufferedEvents();
            Input.FlushBufferedEvents();
            Check(motions.SequenceEqual([3f]), "Explicit flush delivers exactly one staged pending motion.");
            var velocity = Input.LastMouseVelocity;
            motion.Motion.X = float.NaN;
            accumulate.Invoke(display, [motion.Motion]);
            Reject<ArgumentOutOfRangeException>(Input.FlushBufferedEvents);
            Check(Input.LastMouseVelocity == velocity && motions.Count == 1, "Invalid native motion changes neither state nor callbacks.");
            Input.FlushBufferedEvents();
            Console.WriteLine($"SDL {SDL.GetVersion()} windows: action callback order, accumulation on/off, flush owner/reentry and validation passed");
        }
        finally
        {
            Engine.Stop();
            Input.UseAccumulatedInput = true;
            Input.ReleasePressedEvents();
            InputMap.EraseAction(action);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private sealed class Probe : Node
    {
        internal Action<InputEvent>? DuringInput;
        protected override void OnInput(InputEvent value) => DuringInput?.Invoke(value);
    }
}
