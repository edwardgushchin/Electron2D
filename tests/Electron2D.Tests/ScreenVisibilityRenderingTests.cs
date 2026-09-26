using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyScreenVisibility(string backend)
    {
        using var shape = new CircleShape();
        var window = new Window { Size = new(100, 80), CanvasCullMask = 1 }; var target = new RigidBody { Name = "Target", Position = new(500, 500), CanSleep = false, CollisionMask = 0 };
        target.ShapeOwnerAddShape(target.CreateShapeOwner(null), shape); window.AddChild(target);
        var notifier = new VisibleOnScreenNotifier { Name = "Sensor", Position = new(10, 10), Rect = new(0, 0, 10, 10) }; window.AddChild(notifier);
        var enabler = new VisibleOnScreenEnabler { Name = "Enabler", EnableNodePath = "../Target", Position = new(10, 10), Rect = new(0, 0, 10, 10) }; window.AddChild(enabler);
        var layer = new CanvasLayer { Offset = new(5, 0) }; window.AddChild(layer);
        var layered = new VisibleOnScreenNotifier { Position = new(-10, 10), Rect = new(0, 0, 10, 10) }; layer.AddChild(layered);
        var entries = 0; var exits = 0; var frames = 0;
        notifier.ScreenEntered += () => { entries++; Check(notifier.IsOnScreen(), "Native entry commits state."); };
        notifier.ScreenExited += () => { exits++; Check(!notifier.IsOnScreen(), "Native exit commits state."); };
        window.Ready += _ =>
        {
            Check(PhysicsServer.Instance.BodyGetDirectState(target.GetRID()) is null && !notifier.IsOnScreen() && target.ProcessMode == ProcessMode.Disabled, "No on-screen state before first render.");
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                frames++;
                switch (frames)
                {
                    case 1:
                        Check(PhysicsServer.Instance.BodyGetDirectState(target.GetRID()) is not null && notifier.IsOnScreen() && layered.IsOnScreen() && target.ProcessMode == ProcessMode.Inherit, "Initial region, CanvasLayer transform and automatic enable.");
                        using (var pixels = server.Readback()) Pixel(pixels, 11, 11, Colors.Black);
                        notifier.Position = new(101, 10); enabler.Position = new(101, 10); break;
                    case 2:
                        Check(PhysicsServer.Instance.BodyGetDirectState(target.GetRID()) is null && !notifier.IsOnScreen() && target.ProcessMode == ProcessMode.Disabled, "Region beyond the viewport exits and disables its target.");
                        notifier.Position = new(99, 10); enabler.Position = new(99, 10); break;
                    case 3:
                        Check(notifier.IsOnScreen() && target.ProcessMode == ProcessMode.Inherit, "Any positive intersection enters."); notifier.Hide(); break;
                    case 4:
                        Check(!notifier.IsOnScreen(), "Hidden notifier exits."); notifier.Show(); notifier.VisibilityLayer = 2; break;
                    case 5:
                        Check(!notifier.IsOnScreen(), "Viewport mask rejects layer bit."); window.CanvasCullMask = 3; break;
                    case 6:
                        Check(notifier.IsOnScreen(), "Live viewport mask accepts the region."); window.CanvasTransform = new(0, new(-200, 0)); break;
                    case 7:
                        Check(!notifier.IsOnScreen() && layered.IsOnScreen() && target.ProcessMode == ProcessMode.Disabled, "Canvas transform moves default group independently of CanvasLayer.");
                        window.CanvasTransform = Transform.Identity; notifier.Rect = new(0, 0, 0, 10); break;
                    case 8:
                        Check(notifier.IsOnScreen(), "A zero-width line still participates in inclusive conservative culling."); notifier.Rect = new(10, 10, -10, -10); notifier.Position = new(10, 10); break;
                    case 9:
                        Check(notifier.IsOnScreen(), "Negative extent normalization."); window.RemoveChild(notifier); Check(!notifier.IsOnScreen(), "Tree departure resets immediately."); window.AddChild(notifier); break;
                    case 10:
                        Check(notifier.IsOnScreen() && entries == 5 && exits == 3, "Reentry samples on next submitted frame without a synthetic removal exit.");
                        notifier.Modulate = new(1, 1, 1, 0); break;
                    case 11:
                        Check(!notifier.IsOnScreen(), "Inherited modulation alpha below the culling threshold suppresses detection.");
                        notifier.Modulate = Colors.White; notifier.SelfModulate = new(1, 1, 1, 0); notifier.Position = new(100, 10); break;
                    default:
                        Check(notifier.IsOnScreen() && entries == 6 && exits == 4, "Border-only contact is included and SelfModulate does not change culling."); window.Tree!.Quit(); break;
                }
            };
        };
        Engine.Instance.Run(window); Released(window); Check(frames == 12, "Twelve native visibility stages.");
        VerifyScreenClipAndRepeatedDrawing();
        VerifyScreenCallbackMutation();
        VerifyScreenAllocations(backend);
        Console.WriteLine($"Screen region detection, target processing and warmed native frames passed: {backend}.");
    }

    private static void VerifyScreenClipAndRepeatedDrawing()
    {
        var window = new Window { Size = new(100, 80) }; var clip = new Control { Size = new(10, 10), ClipContents = true }; window.AddChild(clip);
        var clipped = new VisibleOnScreenNotifier { Position = new(20, 20), Rect = new(0, 0, 5, 5) }; clip.AddChild(clipped);
        var repeated = new Parallax { RepeatSize = new(100, 0), RepeatTimes = 2 }; window.AddChild(repeated);
        var repeatSensor = new VisibleOnScreenNotifier { Position = new(150, 10), Rect = new(0, 0, 5, 5) }; repeated.AddChild(repeatSensor);
        var drawn = new DrawingScreenNotifier { Position = new(200, 10), Rect = new(0, 0, 1, 1) }; window.AddChild(drawn);
        window.Ready += _ => RenderingServer.Instance!.FramePostDraw += () =>
        {
            Check(!clipped.IsOnScreen() && repeatSensor.IsOnScreen() && drawn.IsOnScreen(),
                "Control clipping, visible repeated copies and inherited draw geometry contribute to conservative culling.");
            window.Tree!.Quit();
        };
        Engine.Instance.Run(window); Released(window);
    }
    private sealed class DrawingScreenNotifier : VisibleOnScreenNotifier
    {
        protected override void OnDraw() => DrawRect(new(-180, 0, 5, 5), Colors.White);
    }

    private static void VerifyScreenCallbackMutation()
    {
        var window = new Window { Size = new(100, 80) }; var first = new VisibleOnScreenNotifier { Name = "First", Position = new(10, 10) };
        var second = new VisibleOnScreenNotifier { Name = "Second", Position = new(20, 10) }; window.AddChild(first); window.AddChild(second);
        var calls = 0; var frames = 0;
        first.ScreenEntered += () =>
        {
            Check(second.IsOnScreen(), "All snapshots are committed before dispatch.");
            window.RemoveChild(second); window.AddChild(second);
        };
        second.ScreenEntered += () => calls++;
        window.Ready += _ => RenderingServer.Instance!.FramePostDraw += () =>
        {
            if (++frames == 1) Check(!second.IsOnScreen() && calls == 0, "Reentry invalidates queued stale delivery.");
            else { Check(second.IsOnScreen() && calls == 1, "Reentry enters on the following frame."); window.Tree!.Quit(); }
        };
        Engine.Instance.Run(window); Released(window);
        var failing = new Window { Size = new(100, 80) }; var early = new VisibleOnScreenNotifier { Name = "Early" }; var late = new VisibleOnScreenNotifier { Name = "Late" };
        failing.AddChild(early); failing.AddChild(late); var laterDelivered = false;
        early.ScreenEntered += () => throw new ApplicationException("expected screen handler failure");
        late.ScreenEntered += () => laterDelivered = true;
        Exception? failure = null;
        try { Engine.Instance.Run(failing); } catch (Exception error) { failure = error; }
        Released(failing);
        Check(failure is AggregateException && laterDelivered, "A failed transition still delivers later queued notifiers and releases native ownership.");
    }

    private static void VerifyScreenAllocations(string backend)
    {
        var window = new Window { Size = new(100, 80) }; var target = new Node { Name = "WarmTarget" }; window.AddChild(target);
        var notifier = new VisibleOnScreenEnabler { EnableNodePath = "../WarmTarget", Rect = new(0, 0, 5, 5) }; window.AddChild(notifier);
        var frames = 0; var entries = 0; var exits = 0; long before = 0, allocated = 0;
        notifier.ScreenEntered += () => entries++; notifier.ScreenExited += () => exits++;
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!;
            server.FramePreDraw += () => { before = GC.GetAllocatedBytesForCurrentThread(); notifier.Position = frames % 2 == 0 ? new(10, 10) : new(200, 10); };
            server.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                if (++frames > 64) allocated += bytes;
                if (frames == 128) window.Tree!.Quit();
            };
        };
        Engine.Instance.Run(window); Released(window);
        Check(frames == 128 && entries == 64 && exits == 64 && allocated == 0, $"Warmed active {backend} render visibility transitions allocated {allocated} bytes.");
    }
}
