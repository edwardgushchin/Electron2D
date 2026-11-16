using Electron2D;
using SDL3;

internal static class InputKeyboardStateTests
{
    internal static void Run(bool native)
    {
        if (native) { Native(); return; }
        MixedIdentityMatching();
        KeyValues();
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

    private static void KeyValues()
    {
        using var key = new InputEventKey();
        Check(!key.Pressed && !key.Echo && key.Keycode == Key.None && key.PhysicalKeycode == Key.None && key.KeyLabel == Key.None && key.Location == KeyLocation.Unspecified, "Key defaults.");
        foreach (var descriptor in key.GetPropertyList().OfType<PropertyDescriptor<InputEventKey, Key>>())
        {
            Check(descriptor.IsStored && descriptor.TryGetRevertValue(key, out var initial) && initial == Key.None, "Typed stored identity defaults.");
            var raw = (Key)((int)Key.Z | (int)KeyModifierMask.CommandOrControl | (int)KeyModifierMask.Keypad | (int)KeyModifierMask.GroupSwitch);
            descriptor.SetValue(key, raw);
            using var copy = (InputEventKey)key.Duplicate();
            Check(descriptor.GetValue(copy) == raw && descriptor.CanRevert(key), "Copy preserves raw encoded bits.");
            descriptor.Revert(key);
            Check(descriptor.GetValue(key) == Key.None && !descriptor.CanRevert(key), "Identity reverts to None.");
        }
        foreach (var descriptor in key.GetPropertyList().OfType<PropertyDescriptor<InputEventKey, bool>>())
        {
            Check(descriptor.IsStored && descriptor.TryGetRevertValue(key, out var initial) && !initial, "Typed stored bool defaults.");
            descriptor.SetValue(key, true);
            using var copy = (InputEventKey)key.Duplicate();
            Check(descriptor.GetValue(copy), "Copy retains pressed/echo.");
            descriptor.Revert(key);
            Check(!descriptor.GetValue(key), "Bool revert.");
        }
        var side = key.GetPropertyList().OfType<PropertyDescriptor<InputEventKey, KeyLocation>>().Single();
        foreach (var location in new[] { KeyLocation.Left, KeyLocation.Right })
        {
            side.SetValue(key, location);
            using var copy = (InputEventKey)key.Duplicate();
            Check(copy.Location == location && side.IsStored, "Copy retains side.");
            side.Revert(key);
            Check(key.Location == KeyLocation.Unspecified, "Location revert.");
        }
        var changes = 0;
        key.Changed += _ => changes++;
        try { key.Location = (KeyLocation)99; throw new Exception("Invalid location accepted."); }
        catch (ArgumentOutOfRangeException) { Check(changes == 0 && key.Location == KeyLocation.Unspecified, "Invalid side leaves state and notifications intact."); }
        for (var bits = 0; bits < 16; bits++)
        {
            key.ShiftPressed = (bits & 1) != 0;
            key.AltPressed = (bits & 2) != 0;
            key.ControlPressed = (bits & 4) != 0;
            key.MetaPressed = (bits & 8) != 0;
            var mask = (key.ShiftPressed ? KeyModifierMask.Shift : 0) | (key.AltPressed ? KeyModifierMask.Alt : 0) | (key.ControlPressed ? KeyModifierMask.Control : 0) | (key.MetaPressed ? KeyModifierMask.Meta : 0);
            foreach (var raw in new[] { Key.None, Key.A, (Key)((int)Key.Z | (int)KeyModifierMask.CommandOrControl | (int)KeyModifierMask.Keypad | (int)KeyModifierMask.GroupSwitch) })
            {
                key.Keycode = key.PhysicalKeycode = key.KeyLabel = raw;
                var expected = (Key)((int)raw | (int)mask);
                Check(key.GetModifiersMask() == mask && key.GetKeycodeWithModifiers() == expected && key.GetPhysicalKeycodeWithModifiers() == expected && key.GetKeyLabelWithModifiers() == expected, "All 16 masks union raw identities without decoding bits.");
            }
        }
        key.CommandOrControlAutoremap = true;
        Check(key.IsCommandOrControlPressed() && (key.GetModifiersMask() & (OperatingSystem.IsMacOS() ? KeyModifierMask.Meta : KeyModifierMask.Control)) != 0, "Autoremap selects concrete host modifier.");
        key.CommandOrControlAutoremap = false;
        Check(!key.ControlPressed && !key.MetaPressed, "Disabling autoremap clears concrete bits.");
        var before = changes;
        key.Echo = key.Echo;
        Check(changes == before + 1, "Repeated scalar assignment notifies.");
        var reentered = false;
        key.Changed += value => { if (!reentered) { reentered = true; ((InputEventKey)value).KeyLabel = Key.B; } };
        key.KeyLabel = Key.A;
        Check(key.KeyLabel == Key.B && reentered, "Changed reentry observes committed state.");
        key.Changed += _ => throw new InvalidOperationException("key notification control");
        try { key.Pressed = true; throw new Exception("Notification failure swallowed."); }
        catch (InvalidOperationException) { Check(key.Pressed, "Notification failure retains committed value."); }
        key.Dispose();
        foreach (Action query in new Action[] { () => _ = key.Pressed, () => _ = key.Echo, () => _ = key.Keycode, () => _ = key.PhysicalKeycode, () => _ = key.KeyLabel, () => _ = key.Location, () => key.GetKeycodeWithModifiers(), () => key.GetPhysicalKeycodeWithModifiers(), () => key.GetKeyLabelWithModifiers() })
        {
            try { query(); throw new Exception("Disposed key access accepted."); }
            catch (ObjectDisposedException) { }
        }
        Console.WriteLine("Key values: nine members, defaults, typed stored descriptors/revert, copies, 16 masks/raw bits, autoremap, notifications, errors, reentry and Dispose passed.");
    }

    private static void MixedIdentityMatching()
    {
        const string action = "tests.keyboard.mixed-identities";
        InputMap.AddAction(action);
        try
        {
            foreach (var logical in new[] { Key.None, Key.A })
                foreach (var location in new[] { KeyLocation.Unspecified, KeyLocation.Left, KeyLocation.Right })
                    foreach (var pressed in new[] { false, true })
                        foreach (var echo in new[] { false, true })
                        {
                            using var binding = new InputEventKey { Keycode = logical, PhysicalKeycode = Key.Q, KeyLabel = Key.Z, Location = KeyLocation.Left };
                            using var actual = new InputEventKey { Keycode = Key.A, PhysicalKeycode = Key.Q, KeyLabel = Key.Z, Location = location, Pressed = pressed, Echo = echo };
                            using var shortcut = new Shortcut { Events = [binding] };
                            var expected = logical != Key.None || location == KeyLocation.Left;
                            foreach (var exact in new[] { false, true })
                            {
                                Check(binding.IsMatch(actual, exact) == expected, "Location restricts only the selected physical identity.");
                                InputMap.ActionAddEvent(action, binding);
                                Check(InputMap.EventIsAction(actual, action, exact) == expected, "Action matching uses the same selected identity.");
                                InputMap.ActionEraseEvents(action);
                            }
                            Check(shortcut.MatchesEvent(actual) == expected, "Shortcut matching uses the same selected identity.");
                        }
            using var measuredBinding = new InputEventKey { Keycode = Key.A, PhysicalKeycode = Key.Q, Location = KeyLocation.Left };
            using var measuredActual = new InputEventKey { Keycode = Key.A, PhysicalKeycode = Key.Q, KeyLabel = Key.Z, Location = KeyLocation.Right, Pressed = true };
            using var measuredShortcut = new Shortcut { Events = [measuredBinding] };
            InputMap.ActionAddEvent(action, measuredBinding);
            void Queries()
            {
                Check(measuredActual.Pressed && !measuredActual.Echo && measuredActual.Location == KeyLocation.Right &&
                    measuredActual.GetKeycodeWithModifiers() == Key.A && measuredActual.GetPhysicalKeycodeWithModifiers() == Key.Q &&
                    measuredActual.GetKeyLabelWithModifiers() == Key.Z && measuredBinding.IsMatch(measuredActual) &&
                    InputMap.EventIsAction(measuredActual, action) && measuredShortcut.MatchesEvent(measuredActual), "Warmed scalar and matching values.");
            }
            for (var i = 0; i < 1024; i++) Queries();
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 16384; i++) Queries();
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed key scalars and event/action/shortcut matching allocate zero bytes.");
        }
        finally { InputMap.EraseAction(action); }
        Console.WriteLine("Mixed identities: logical/physical precedence, three locations, press/release, echo, exact/subset matching, InputMap and Shortcut passed; 16384 warmed scalar/matching groups = 0 bytes.");
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
