using Electron2D;
using System.Diagnostics;

internal static class InputMouseStateTests
{
    internal static void RunNative()
    {
        for (var session = 0; session < 2; session++)
        {
            using var display = DisplayServer.Open("Mouse state contract", new Vector2i(160, 120));
            var motions = 0;
            var inputs = 0;
            DisplayServer.GetWindowEvents().Input += value =>
            {
                inputs++;
                if (value is InputEventMouseMotion) motions++;
            };
            DisplayServer.ProcessEvents();
            var before = motions;
            Input.WarpMouse(new(31, 29));
            for (var i = 0; i < 20 && motions == before; i++) { Thread.Sleep(10); DisplayServer.ProcessEvents(); }
            Check(motions > before, "Native warp produces real motion through the event pump.");
            using var press = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true };
            Input.ParseInputEvent(press);
            var windows = SDL3.SDL.GetWindows(out var count);
            Check(count == 1, "One native mouse host.");
            var nativeID = SDL3.SDL.GetWindowID(windows![0]);
            var counterpart = new SDL3.SDL.Event
            {
                Motion = new SDL3.SDL.MouseMotionEvent { Type = SDL3.SDL.EventType.MouseMotion, WindowID = nativeID, Which = SDL3.SDL.TouchMouseID, XRel = 3 }
            };
            var touchCounterpart = new SDL3.SDL.Event
            {
                TFinger = new SDL3.SDL.TouchFingerEvent { Type = SDL3.SDL.EventType.FingerDown, WindowID = nativeID, TouchID = SDL3.SDL.MouseTouchID, FingerID = 9 }
            };
            before = inputs;
            Check(SDL3.SDL.PushEvent(ref counterpart) && SDL3.SDL.PushEvent(ref touchCounterpart), "Queue native pointer counterparts.");
            DisplayServer.ProcessEvents();
            Check(inputs == before, "SDL pointer counterparts are suppressed before Input parsing.");
            touchCounterpart.TFinger.TouchID = 901;
            Check(SDL3.SDL.PushEvent(ref touchCounterpart), "Queue software native touch contact.");
            DisplayServer.ProcessEvents();
            Check(inputs == before + 2 && Input.IsMouseButtonPressed(MouseButton.Left), "Software SDL touch produces one source and one emulated mouse event.");
            var focus = new SDL3.SDL.Event
            {
                Window = new SDL3.SDL.WindowEvent { Type = SDL3.SDL.EventType.WindowFocusLost, WindowID = nativeID }
            };
            Check(SDL3.SDL.PushEvent(ref focus), "Queue native focus loss.");
            DisplayServer.ProcessEvents();
            Check(Input.MouseButtonMask == MouseButtonMask.None, "Native focus reset clears public mouse state.");
            InputPointerPolicyTests.Run();
            using var motion = new InputEventMouseMotion { Relative = new(8, 9), ScreenRelative = new(16, 18), ButtonMask = MouseButtonMask.Left };
            Input.ParseInputEvent(motion);
            Input.SetDefaultCursorShape(CursorShape.IBeam);
            Check(Input.LastMouseVelocity == Vector2.Zero && Input.LastMouseScreenVelocity == Vector2.Zero && Input.MouseButtonMask == MouseButtonMask.Left,
                "Changed default resets the complete velocity sample without releasing held buttons.");
            Input.ParseInputEvent(motion);
            Thread.Sleep(150);
            Input.SetDefaultCursorShape(CursorShape.IBeam);
            Check(Input.LastMouseVelocity.X > 0, "Identical cursor default preserves the pending velocity sample.");
            Input.SetDefaultCursorShape();
            Input.ParseInputEvent(press);
            display.Dispose();
            Check(Input.MouseButtonMask == MouseButtonMask.None, "Native shutdown clears held state before reopening.");
        }
        Console.WriteLine("Windows mouse: real warp motion/pump, queued focus reset and two native sessions passed; no physical touch device.");
    }

    internal static void Run()
    {
        Check(Input.EmulateMouseFromTouch && !Input.EmulateTouchFromMouse, "Initial pointer emulation policies.");
        Check(Input.LastMouseVelocity == Vector2.Zero && Input.LastMouseScreenVelocity == Vector2.Zero, "Initial velocities are zero.");
        Input.ReleasePressedEvents();
        foreach (var held in new[] { MouseButton.Left, MouseButton.Right, MouseButton.Middle, MouseButton.XButton1, MouseButton.XButton2 })
        {
            using var value = new InputEventMouseButton { ButtonIndex = held, Pressed = true };
            Input.ParseInputEvent(value);
            Check(Input.IsMouseButtonPressed(held), "Every non-wheel button can be polled.");
            value.Pressed = false;
            Input.ParseInputEvent(value);
            Check(!Input.IsMouseButtonPressed(held), "Every non-wheel release clears polling.");
        }
        using var button = new InputEventMouseButton { Device = 101, ButtonIndex = MouseButton.Left, Pressed = true };
        Input.ParseInputEvent(button);
        button.Device = 102;
        Input.ParseInputEvent(button);
        button.Device = 101;
        button.Pressed = false;
        Input.ParseInputEvent(button);
        Check(Input.IsMouseButtonPressed(MouseButton.Left), "Releasing one device preserves the other device.");
        button.Device = 102;
        button.Pressed = true;
        button.Canceled = true;
        Input.ParseInputEvent(button);
        Check(Input.MouseButtonMask == MouseButtonMask.None, "Cancellation releases the device button.");
        foreach (var wheel in new[] { MouseButton.WheelUp, MouseButton.WheelDown, MouseButton.WheelLeft, MouseButton.WheelRight })
        {
            button.ButtonIndex = wheel;
            button.Canceled = false;
            Input.ParseInputEvent(button);
            Check(!Input.IsMouseButtonPressed(wheel) && Input.MouseButtonMask == MouseButtonMask.None, "Wheel impulses do not latch.");
        }
        using var motion = new InputEventMouseMotion
        {
            Device = 103,
            ButtonMask = MouseButtonMask.Right | MouseButtonMask.XButton2,
            Relative = new(12, -6),
            ScreenRelative = new(24, -12),
            Velocity = new(999, 999),
            ScreenVelocity = new(-999, -999)
        };
        Thread.Sleep(3100);
        var start = Stopwatch.GetTimestamp();
        Input.ParseInputEvent(motion);
        Check(Input.IsMouseButtonPressed(MouseButton.Right) && Input.IsMouseButtonPressed(MouseButton.XButton2), "Motion publishes its device mask.");
        Thread.Sleep(150);
        var local = Input.LastMouseVelocity;
        var screen = Input.LastMouseScreenVelocity;
        var elapsed = Stopwatch.GetElapsedTime(start).TotalSeconds;
        Check(local.X > 0 && local.Y == -local.X / 2 && screen == local * 2, "Relative coordinate spaces drive both samples; event velocity is ignored.");
        Check(local.X <= 12 / 0.1 && local.X >= 12 / (elapsed + 1), "Velocity uses displacement per elapsed second.");
        Thread.Sleep(150);
        Check(Input.LastMouseVelocity == Vector2.Zero && Input.LastMouseScreenVelocity == Vector2.Zero, "Read-time zero motion clears an idle sample.");
        motion.ButtonMask = MouseButtonMask.None;
        Input.ParseInputEvent(motion);
        Check(Input.MouseButtonMask == MouseButtonMask.None, "Empty motion releases its device mask.");
        Thread.Sleep(3100);
        Check(Input.LastMouseVelocity == Vector2.Zero && Input.LastMouseScreenVelocity == Vector2.Zero, "Long idle resets both accumulators.");
        using var transformed = (InputEventMouseMotion)motion.XformedBy(new Transform(2, 0, 0, 2, 90, 80));
        Check(transformed.Relative == motion.Relative * 2 && transformed.ScreenRelative == motion.ScreenRelative, "Transform scales local displacement and preserves screen displacement.");
        Input.ParseInputEvent(transformed);
        Thread.Sleep(150);
        local = Input.LastMouseVelocity;
        screen = Input.LastMouseScreenVelocity;
        Check(local.X > 0 && local == screen, "Sampling follows transformed local displacement and unchanged screen displacement.");
        for (var i = 0; i < 1024; i++) { _ = Input.LastMouseVelocity; _ = Input.LastMouseScreenVelocity; }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 16384; i++) { _ = Input.LastMouseVelocity; _ = Input.LastMouseScreenVelocity; }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared scalar polling allocates zero managed bytes.");
        Input.ReleasePressedEvents();
        InputPointerPolicyTests.Run();
        Console.WriteLine("Public mouse state: device union, cancellation, wheel exclusion, motion masks, sampled velocities, idle and 16384 zero-byte query pairs passed.");
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
