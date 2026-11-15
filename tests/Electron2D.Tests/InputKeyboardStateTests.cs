using Electron2D;
using SDL3;

internal static class InputKeyboardStateTests
{
    internal static void Run(bool native)
    {
        if (native) { Native(); return; }
        Input.ReleasePressedEvents();
        try
        {
            using var key = new InputEventKey { Keycode = Key.A, PhysicalKeycode = Key.Q, KeyLabel = Key.Z, Pressed = true, ShiftPressed = true };
            key.Echo = true;
            Input.ParseInputEvent(key);
            State(false);
            key.Echo = false;
            Input.ParseInputEvent(key);
            State(true);
            Check(!Input.IsKeyPressed(Key.Q) && !Input.IsPhysicalKeyPressed(Key.Z) && !Input.IsKeyLabelPressed(Key.A), "Independent identities.");
            Check(!Input.IsKeyPressed((Key)((int)Key.A | (int)KeyModifierMask.Shift)), "Modifier query is exact.");
            Input.ParseInputEvent(key);
            key.Pressed = false;
            key.Echo = true;
            Input.ParseInputEvent(key);
            State(true);
            key.Echo = false;
            Input.ParseInputEvent(key);
            Input.ParseInputEvent(key);
            State(false);
            using var empty = new InputEventKey { Pressed = true };
            Input.ParseInputEvent(empty);
            Check(!Input.IsKeyPressed(Key.None) && !Input.IsPhysicalKeyPressed(Key.None) && !Input.IsKeyLabelPressed(Key.None), "None never enters a set.");
            foreach (var modifier in new[] { Key.Shift, Key.Control, Key.Alt, Key.Meta })
            {
                using var press = new InputEventKey { Keycode = modifier, PhysicalKeycode = modifier, KeyLabel = modifier, Pressed = true };
                Input.ParseInputEvent(press);
                Check(Input.IsKeyPressed(modifier) && Input.IsPhysicalKeyPressed(modifier) && Input.IsKeyLabelPressed(modifier), "Modifier key identity.");
                press.Pressed = false;
                Input.ParseInputEvent(press);
                Check(!Input.IsKeyPressed(modifier), "Modifier release.");
            }
            var encoded = (Key)((int)Key.A | (int)KeyModifierMask.Control);
            using var encodedEvent = new InputEventKey { Keycode = encoded, PhysicalKeycode = encoded, KeyLabel = encoded, Pressed = true };
            Input.ParseInputEvent(encodedEvent);
            Check(Input.IsKeyPressed(encoded) && Input.IsPhysicalKeyPressed(encoded) && Input.IsKeyLabelPressed(encoded) && !Input.IsKeyPressed(Key.A), "Submitted codes remain exact.");
            Input.ReleasePressedEvents();
            using var node = new Probe { InputEnabled = true };
            using var tree = new SceneTree(node);
            Engine.Start(tree);
            try
            {
                foreach (var pressed in new[] { true, false })
                {
                    key.Pressed = pressed;
                    node.During = () => { State(pressed); throw new InvalidOperationException("keyboard callback control"); };
                    var failed = false;
                    try { Input.ParseInputEvent(key); } catch (AggregateException) { failed = true; }
                    Check(failed, "Callback failure reported.");
                    State(pressed);
                }
                node.During = null;
                key.Pressed = true;
                Input.ParseInputEvent(key);
                // Keyboard events have no cancellation payload; pointer cancellation cannot clear keys.
                using var cancel = new InputEventScreenTouch { Canceled = true };
                Input.ParseInputEvent(cancel);
                State(true);
                Input.ReleasePressedEvents();
                State(false);
                for (var i = 0; i < 1024; i++) State(false);
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 16384; i++) State(false);
                Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed polling allocates zero bytes.");
            }
            finally { Engine.Stop(); }
        }
        finally { Input.ReleasePressedEvents(); }
        Console.WriteLine("Keyboard state: identities, echo press/release, exact modifiers, None, duplicates, cancellation boundary, callbacks, release-all; 16384 warmed triples = 0 bytes.");
    }

    private static void Native()
    {
        for (var session = 0; session < 2; session++)
        {
            using var display = DisplayServer.Open("Keyboard state queue", new Vector2i(160, 120), hidden: true);
            Check(SDL.GetCurrentVideoDriver() == "windows", "Windows native backend.");
            Check(!Input.IsKeyPressed(Key.F8) && !Input.IsPhysicalKeyPressed(Key.F8) && !Input.IsKeyLabelPressed(Key.F8), "Reopen starts released.");
            DisplayServer.ProcessEvents();
            Input.ReleasePressedEvents();
            DisplayServerKeyboardNativeTests.Run(display);
            var windows = SDL.GetWindows(out var count);
            Check(count == 1, "One native window.");
            var id = SDL.GetWindowID(windows![0]);
            var callbacks = 0;
            DisplayServer.GetWindowEvents().Input += value =>
            {
                if (value is InputEventKey { PhysicalKeycode: Key.F8 } key)
                {
                    Check(Input.IsKeyPressed(key.Keycode) == key.Pressed && Input.IsPhysicalKeyPressed(Key.F8) == key.Pressed && Input.IsKeyLabelPressed(key.KeyLabel) == key.Pressed, "State before native callback.");
                    callbacks++;
                }
            };
            var e = new SDL.Event { Key = new SDL.KeyboardEvent { Type = SDL.EventType.KeyDown, WindowID = id, Scancode = SDL.Scancode.F8, Key = SDL.Keycode.F8, Down = true } };
            Check(SDL.PushEvent(ref e), "Queue press.");
            DisplayServer.ProcessEvents();
            Check(Input.IsPhysicalKeyPressed(Key.F8), "Native press.");
            e.Key.Type = SDL.EventType.KeyUp; e.Key.Down = false;
            Check(SDL.PushEvent(ref e), "Queue release.");
            DisplayServer.ProcessEvents();
            Check(!Input.IsPhysicalKeyPressed(Key.F8) && callbacks == 2, "Native release and callbacks.");
            e.Key.Type = SDL.EventType.KeyDown; e.Key.Down = true;
            Check(SDL.PushEvent(ref e), "Queue held key.");
            DisplayServer.ProcessEvents();
            var focus = new SDL.Event { Window = new SDL.WindowEvent { Type = SDL.EventType.WindowFocusLost, WindowID = id } };
            Check(SDL.PushEvent(ref focus), "Queue focus loss.");
            DisplayServer.ProcessEvents();
            Check(!Input.IsKeyPressed(Key.F8) && !Input.IsPhysicalKeyPressed(Key.F8) && !Input.IsKeyLabelPressed(Key.F8), "Focus clears all identities.");
            Check(SDL.PushEvent(ref e), "Queue hold before close.");
            DisplayServer.ProcessEvents();
            Check(Input.IsPhysicalKeyPressed(Key.F8), "Held before close.");
            display.Dispose();
            Check(!Input.IsKeyPressed(Key.F8) && !Input.IsPhysicalKeyPressed(Key.F8) && !Input.IsKeyLabelPressed(Key.F8), "Close clears all identities.");
        }
        Console.WriteLine("Windows SDL queue: key delivery, mapping, focus reset and two open/close sessions passed; no physical keyboard injection.");
    }

    private static void State(bool pressed) => Check(Input.IsKeyPressed(Key.A) == pressed && Input.IsPhysicalKeyPressed(Key.Q) == pressed && Input.IsKeyLabelPressed(Key.Z) == pressed, "Three keyboard sets.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class Probe : Node
    {
        internal Action? During;
        protected override void OnInput(InputEvent value) => During?.Invoke();
    }
}
