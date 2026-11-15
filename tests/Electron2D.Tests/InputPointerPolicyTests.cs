using Electron2D;

internal static class InputPointerPolicyTests
{
    internal static void Run()
    {
        Input.ReleasePressedEvents();
        Input.EmulateMouseFromTouch = true;
        Input.EmulateTouchFromMouse = true;
        using var outsideScene = new InputEventMouseButton { Device = 214, ButtonIndex = MouseButton.Left, Pressed = true };
        Input.ParseInputEvent(outsideScene);
        Check(Input.IsMouseButtonPressed(MouseButton.Left), "Mouse polling works without a scene.");
        using var node = new Probe { InputEnabled = true };
        using var tree = new SceneTree(node);
        Engine.Start(tree);
        try
        {
            outsideScene.Pressed = false;
            Input.ParseInputEvent(outsideScene);
            Check(node.Count == 1, "A press without a scene cannot leave an emulated touch lease for a later scene.");
            Input.ReleasePressedEvents();
            using var touch = new InputEventScreenTouch { Device = 201, Index = 3, Pressed = true };
            using var mouse = new InputEventMouseButton { Device = 202, ButtonIndex = MouseButton.Left, Pressed = true };
            foreach (var source in new InputEvent[] { touch, mouse })
            {
                node.Count = 0;
                node.Fail = true;
                try { Input.ParseInputEvent(source); throw new InvalidOperationException("Missing callback failure."); }
                catch (AggregateException) { }
                Check(node.Count == 2 && node.GeneratedFirst && node.ReentryRejected == 2,
                    "Both failures preserve generated/source order and reject reentry without extra delivery.");
                node.Fail = false;
                node.Count = 0;
                if (source == touch) { Input.EmulateMouseFromTouch = false; touch.Canceled = true; }
                else { Input.EmulateTouchFromMouse = false; mouse.Canceled = true; }
                Input.ParseInputEvent(source);
                Check(node.Count == 2 && !Input.IsMouseButtonPressed(MouseButton.Left),
                    "Cancellation pairs the active generated release after a policy change.");
                if (source == touch) { touch.Canceled = false; Input.EmulateMouseFromTouch = true; }
                else { mouse.Canceled = false; Input.EmulateTouchFromMouse = true; }

                Input.ParseInputEvent(source);
                Input.ReleasePressedEvents();
                Check(Input.MouseButtonMask == MouseButtonMask.None, "Reset clears all active pointer contributions.");
                node.Count = 0;
                using var stale = new InputEventScreenDrag { Device = 201, Index = 3 };
                using var staleMouse = new InputEventMouseMotion { Device = 202, ButtonMask = MouseButtonMask.Left };
                Input.ParseInputEvent(source == touch ? stale : staleMouse);
                Check(node.Count == 1, "Reset retires emulation ownership; stale motion cannot resume its contact.");
                Input.ReleasePressedEvents();
                node.Count = 0;
                Input.ParseInputEvent(source);
                Check(node.Count == 2, "A fresh press reacquires ownership after reset.");
                Input.ReleasePressedEvents();
            }
            using var first = new InputEventScreenTouch { Device = 211, Index = 7, Pressed = true };
            using var other = new InputEventScreenTouch { Device = 212, Index = 7, Pressed = true };
            Input.ParseInputEvent(first);
            Thread.Sleep(150);
            Input.ParseInputEvent(other);
            using var drag = new InputEventScreenDrag
            {
                Device = 211,
                Index = 7,
                Relative = new(3, -2),
                ScreenRelative = new(6, -4),
                Velocity = new(999, 999),
                ScreenVelocity = new(999, 999)
            };
            node.Count = 0;
            Input.ParseInputEvent(drag);
            Check(node.Count == 2 && node.MouseVelocity.X > 0 && node.TouchVelocity == node.MouseVelocity &&
                node.TouchScreenVelocity == node.TouchVelocity * 2 && drag.Velocity == new Vector2(999, 999) &&
                node.DeliveredTouch is { IsDisposed: true } && !drag.IsDisposed,
                "Touch consumers and generated mouse receive calibrated velocity; borrowed source survives and owned delivery retires.");
            node.Fail = true;
            try { Input.ParseInputEvent(drag); throw new InvalidOperationException("Missing motion failure."); }
            catch (AggregateException) { }
            Check(node.DeliveredTouch is { IsDisposed: true } && !drag.IsDisposed, "Calibrated delivery retires after callback failures.");
            node.Fail = false;
            first.Canceled = true;
            other.Canceled = true;
            Input.ParseInputEvent(first);
            Input.ParseInputEvent(other);
            Input.ReleasePressedEvents();
            Input.EmulateTouchFromMouse = true;
            using var click = new InputEventMouseButton { Device = 213, ButtonIndex = MouseButton.Left, Pressed = true };
            Input.ParseInputEvent(click);
            Thread.Sleep(3100);
            using var mouseDrag = new InputEventMouseMotion { Device = 213, ButtonMask = MouseButtonMask.Left, Relative = new(2, 1), ScreenRelative = new(4, 2), Velocity = new(999, 999) };
            Input.ParseInputEvent(mouseDrag);
            Check(node.TouchVelocity == Vector2.Zero, "First mouse motion after a long gap starts a new sampled generated drag.");
            Thread.Sleep(150);
            mouseDrag.Relative = Vector2.Zero;
            mouseDrag.ScreenRelative = Vector2.Zero;
            Input.ParseInputEvent(mouseDrag);
            Check(node.TouchVelocity.X > 0 && node.TouchScreenVelocity == node.TouchVelocity * 2 && mouseDrag.Velocity == new Vector2(999, 999),
                "Mouse-to-touch consumers receive sampled velocity without changing the borrowed mouse payload.");
            Input.ReleasePressedEvents();
            foreach (var value in new InputEvent[]
            {
                new InputEventScreenTouch { Device = InputEvent.DeviceIdEmulation, Pressed = true },
                new InputEventMouseButton { Device = InputEvent.DeviceIdEmulation, ButtonIndex = MouseButton.Left, Pressed = true }
            })
            {
                using (value)
                {
                    node.Count = 0;
                    Input.ParseInputEvent(value);
                    Check(node.Count == 1, "Explicit generated events never create the opposite pointer family.");
                }
            }
        }
        finally
        {
            Engine.Stop();
            Input.ReleasePressedEvents();
            Input.EmulateMouseFromTouch = true;
            Input.EmulateTouchFromMouse = false;
        }
        Console.WriteLine("Pointer policy matrix: both directions, combined callback failures, reentry, canceled policy-paired releases, reset/stale motion/reacquisition and recursion guards passed.");
    }

    private sealed class Probe : Node
    {
        internal int Count, ReentryRejected;
        internal bool Fail, GeneratedFirst;
        internal Vector2 MouseVelocity, TouchVelocity, TouchScreenVelocity;
        internal InputEventScreenDrag? DeliveredTouch;
        protected override void OnInput(InputEvent value)
        {
            if (value is InputEventMouseMotion motion) MouseVelocity = motion.Velocity;
            if (value is InputEventScreenDrag drag) { TouchVelocity = drag.Velocity; TouchScreenVelocity = drag.ScreenVelocity; DeliveredTouch = drag; }
            if (Count++ == 0) { GeneratedFirst = value.Device == InputEvent.DeviceIdEmulation; ReentryRejected = 0; }
            try { Input.ParseInputEvent(value); }
            catch (InvalidOperationException) { ReentryRejected++; }
            if (Fail) throw new InvalidOperationException("Injected pointer callback failure.");
        }
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
