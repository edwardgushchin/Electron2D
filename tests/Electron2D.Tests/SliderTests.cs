using Electron2D;

internal static class SliderTests
{
    internal static void Run()
    {
        VerifyDefaultsGeometryAndTicks();
        VerifyDraggingAndSharedSignals();
        VerifySharedPeerReentry();
        VerifyKeyboardWheelAndJoypad();
        VerifyFailuresLifetimeAndPacking();
        VerifyWarmInput();
        Console.WriteLine("Slider defaults, themed geometry, ticks, drag/shared signals, keyboard/wheel/gamepad repeat, lifecycle and packing passed.");
    }

    private static void VerifyDefaultsGeometryAndTicks()
    {
        using var skin = new Skin(); using var h = new HSlider { Theme = skin.Theme, Size = new(100, 20), Value = 50 };
        using var v = new VSlider { Theme = skin.Theme, Size = new(20, 100) };
        Check(typeof(Slider).IsAbstract && h.Editable && h.Scrollable && h.TickCount == 0 && !h.TicksOnBorders && h.TicksPosition == Slider.TickPosition.BottomRight && h.Step == 1 && h.FocusMode == FocusMode.All,
            "Slider's concrete orientations retain editable, scrollable, one-unit step, full focus and no-tick defaults.");
        Check(h.SizeFlagsVertical == Control.SizeFlags.ShrinkBegin && v.SizeFlagsHorizontal == Control.SizeFlags.ShrinkBegin && h.GetMinimumSize() == new Vector2(4, 8) && v.GetMinimumSize() == new Vector2(6, 4),
            "Orientation constrains the cross-axis minimum using the normal grabber and retains its specialization sizing flag.");
        skin.Track.ContentMarginLeft = skin.Track.ContentMarginRight = 2.9f;
        Check(h.GetMinimumSize() == new Vector2(5, 8) && v.GetMinimumSize() == new Vector2(6, 4), "Style minimum components truncate before combination with integer grabber dimensions.");
        skin.Track.SetContentMarginAll(2);
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        int TickVertices()
        {
            h.InvalidateCanvas(); h.PrepareCanvas(); vertices.Clear(); batches.Clear(); h.AppendCanvas(vertices, batches, Transform.Identity);
            return batches.Where(batch => ReferenceEquals(batch.Texture, skin.Tick)).Sum(batch => batch.Count);
        }
        h.TickCount = 5; Check(TickVertices() == 18, "Five nominal ticks omit both endpoints by default.");
        h.TicksPosition = Slider.TickPosition.Both; Check(TickVertices() == 36, "Both-sided ticks draw two commands per interior mark.");
        h.TicksOnBorders = true; Check(TickVertices() == 60, "Endpoint marks participate when enabled.");
        h.TicksPosition = Slider.TickPosition.Center; Check(TickVertices() == 30, "Centered ticks draw one command per mark.");
        h.TicksPosition = (Slider.TickPosition)99; Check(TickVertices() == 0 && (int)h.TicksPosition == 99, "Unknown tick-position values are retained and draw no tick branch.");
        h.TicksPosition = Slider.TickPosition.BottomRight; h.TickCount = -4; Check(TickVertices() == 0 && h.TickCount == -4, "Negative tick counts are stored without drawing a tick loop.");
        h.TickCount = 1; Check(TickVertices() == 0, "One nominal tick does not divide by zero.");
        h.Value = double.NaN; TickVertices(); Check(vertices.All(vertex => vertex.Position.IsFinite()), "NaN range values use a finite zero drawing ratio.");
        h.TickCount = int.MaxValue; Reject<InvalidOperationException>(() => TickVertices());
        h.TickCount = 0; TickVertices(); Check(vertices.Count > 0, "A rejected excessive tick recording does not poison the next valid draw.");
    }

    private static void VerifyDraggingAndSharedSignals()
    {
        using var skin = new Skin(); var root = new Node();
        var slider = new HorizontalProbe { Theme = skin.Theme, Size = new(100, 20), Step = 1 }; var peer = new HSlider { Name = "Peer", Theme = skin.Theme };
        root.AddChild(slider); root.AddChild(peer); using var tree = new SceneTree(root); slider.Share(peer);
        var order = new List<string>(); slider.DragStarted += () => order.Add("start:" + slider.Value);
        slider.ValueChanged += value => order.Add("slider:" + value); peer.ValueChanged += value => order.Add("peer:" + value); slider.DragEnded += changed => order.Add("end:" + changed);
        using var press = Button(new(50.9f, 10)); slider.Send(press);
        Check(slider.Value == 50 && peer.Value == 50 && order.SequenceEqual(new[] { "start:0", "peer:50", "slider:50", "peer:50" }),
            "Click truncates its anchor and emits DragStarted before the unblocked peer phase and the forced shared phase.");
        using var motion = new InputEventMouseMotion { Position = new(95, 10), ButtonMask = MouseButtonMask.Left }; slider.Send(motion);
        Check(slider.Value == 100 && peer.Value == 100, "Dragging uses highlighted grabber dimensions and motion from the original integer anchor.");
        using var release = Button(new(95, 10), false); slider.Send(release);
        Check(order[^1] == "end:True", "Release reports a ratio change since the start of the gesture.");
        slider.Value = 50; order.Clear(); slider.Send(press); slider.Send(release);
        Check(order.SequenceEqual(new[] { "start:50", "slider:50", "peer:50", "end:False" }), "An unchanged click still forces one shared notification and reports no final change.");
        slider.Step = 0; slider.Size = new(100.9f, 20); slider.Send(press);
        Near(slider.Value, 100 * 47 / 94.9f, .00001, "Click uses the fractional control width with a truncated click anchor.");
        var clicked = slider.Value; using var fractionalMotion = new InputEventMouseMotion { Position = new(50.9f, 10), ButtonMask = MouseButtonMask.Left }; slider.Send(fractionalMotion);
        Near(slider.Value, clicked + 1, .00001, "Motion truncates control size but retains the fractional pointer coordinate.");
        slider.Send(release); slider.Size = new(6, 20); using var zeroTrackPress = Button(new(3, 10)); slider.Send(zeroTrackPress);
        Check(double.IsNaN(slider.Value), "A centered click on a zero-length track preserves the source NaN range result.");
        slider.Send(motion); Check(double.IsNaN(slider.Value), "Nonpositive motion travel does not modify the current ratio."); slider.Send(release);
        slider.Value = 40; slider.Size = new(100, 20); slider.LayoutDirection = LayoutDirection.RTL; slider.Send(zeroTrackPress);
        Check(slider.Value == 100, "Horizontal RTL clicks reverse the ratio."); slider.Send(release);
        slider.AddThemeConstantOverride("center_grabber", 1); using var centeredPress = Button(new(25, 10)); slider.Send(centeredPress);
        Check(slider.Value == 75, "Centered grabbers use the full horizontal track in RTL."); slider.Send(release);
    }

    private static void VerifySharedPeerReentry()
    {
        using var skin = new Skin(); var root = new Node(); var slider = new HorizontalProbe { Theme = skin.Theme, Size = new(100, 20) }; var peer = new HSlider { Name = "Peer", Theme = skin.Theme };
        root.AddChild(slider); root.AddChild(peer); using var tree = new SceneTree(root); slider.Share(peer);
        using var outerPress = Button(new(50, 10)); using var innerPress = Button(new(75, 10)); var nested = false; var ownSignals = 0;
        slider.ValueChanged += _ => ownSignals++;
        peer.ValueChanged += _ => { if (!nested) { nested = true; slider.Send(innerPress); } };
        slider.Send(outerPress);
        Check(nested && slider.Value == 77 && peer.Value == 77 && ownSignals == 1,
            "A nested press from a shared peer still emits its own forced signal while the outer local-signal suppression scope is active.");
    }

    private static void VerifyKeyboardWheelAndJoypad()
    {
        InputMap.LoadFromProjectSettings(); Input.Service.BeginFrame(false);
        using var skin = new Skin(); var viewport = new TestViewport();
        var h = new HSlider { Theme = skin.Theme, Position = new(10, 10), Size = new(100, 20), Value = 50, Step = 2 };
        var v = new VSlider { Theme = skin.Theme, Position = new(150, 10), Size = new(20, 100), Value = 50, Step = 2 };
        viewport.AddChild(h); viewport.AddChild(v); using var tree = new SceneTree(viewport); h.GrabFocus();
        using var right = new InputEventKey { Keycode = Key.Right, Pressed = true }; viewport.PushInput(right, true); Check(h.Value == 52, "Horizontal right action increments the range step.");
        right.Echo = true; viewport.PushInput(right, true); Check(h.Value == 54, "Keyboard echo is accepted for directional stepping.");
        using var up = new InputEventKey { Keycode = Key.Up, Pressed = true }; viewport.PushInput(up, true); Check(h.Value == 54, "The perpendicular arrow does not change a horizontal slider.");
        h.LayoutDirection = LayoutDirection.RTL; h.Position = new(10, 10); viewport.PushInput(right, true); Check(h.Value == 52, "RTL reverses horizontal keyboard increments.");
        using var home = new InputEventKey { Keycode = Key.Home, Pressed = true }; using var end = new InputEventKey { Keycode = Key.End, Pressed = true };
        viewport.PushInput(home, true); Check(h.Value == 0, "Home uses the minimum endpoint."); h.Page = 10; viewport.PushInput(end, true); Check(h.Value == 90, "End uses the maximum request and inherited Page clamp."); h.Page = 0;
        using var wheel = Button(new(30, 20), button: MouseButton.WheelDown); viewport.PushInput(wheel, true); Check(h.Value == 88, "Wheel stepping uses Step regardless of RTL.");
        h.Scrollable = false; viewport.PushInput(wheel, true); Check(h.Value == 88, "Scrollable disables the wheel path.");
        h.Editable = false; viewport.PushInput(home, true); Check(h.Value == 88, "Noneditable sliders ignore keyboard changes."); h.Editable = true;
        v.GrabFocus(); viewport.PushInput(up, true); Check(v.Value == 52, "Vertical up increments the value."); viewport.PushInput(right, true); Check(v.Value == 52, "The perpendicular arrow does not change a vertical slider.");
        h.LayoutDirection = LayoutDirection.LTR; h.Position = new(10, 10); h.Value = 50; h.GrabFocus();
        using var joy = new InputEventJoypadButton { Device = 73, ButtonIndex = JoyButton.DpadRight, Pressed = true };
        using var joyRelease = new InputEventJoypadButton { Device = 73, ButtonIndex = JoyButton.DpadRight, Pressed = false };
        try
        {
            Input.Service.CompleteFrame(false);
            Input.ParseInputEvent(joy); viewport.PushInput(joy, true); Check(h.Value == 52, "The exact joypad transition applies its immediate step."); Input.Service.CompleteFrame(false);
            tree.ProcessFrame(.49); Check(h.Value == 52, "Joypad repeat waits for the initial half-second delay."); Input.Service.CompleteFrame(false);
            tree.ProcessFrame(.02); Check(h.Value == 54, "Joypad repeat starts after the delay."); Input.Service.CompleteFrame(false);
            tree.ProcessFrame(.05); Check(h.Value == 56, "Joypad repeat preserves its twenty-per-second interval."); Input.Service.CompleteFrame(false);
            tree.ProcessFrame(.3); Check(h.Value == 58, "A large process delta performs at most one repeat per frame."); Input.Service.CompleteFrame(false);
            Input.ParseInputEvent(joyRelease); viewport.PushInput(joyRelease, true); tree.ProcessFrame(.5); Check(h.Value == 58, "A directional release stops repeating before the next increment.");
        }
        finally { Input.ParseInputEvent(joyRelease); Input.Service.CompleteFrame(false); Input.Service.CompleteFrame(true); }
        using var axis = new InputEventJoypadMotion { Device = 74, Axis = JoyAxis.LeftX, AxisValue = 1 };
        using var heldAxis = new InputEventJoypadMotion { Device = 74, Axis = JoyAxis.LeftX, AxisValue = 1 };
        using var neutralAxis = new InputEventJoypadMotion { Device = 74, Axis = JoyAxis.LeftX, AxisValue = 0 };
        using var otherPress = new InputEventJoypadButton { Device = 75, ButtonIndex = JoyButton.DpadUp, Pressed = true };
        using var otherRelease = new InputEventJoypadButton { Device = 75, ButtonIndex = JoyButton.DpadUp, Pressed = false };
        try
        {
            Input.ParseInputEvent(axis); viewport.PushInput(axis, true); Check(h.Value == 60, "A fresh analog directional transition also applies the immediate step.");
            Input.ParseInputEvent(heldAxis); viewport.PushInput(heldAxis, true); Check(h.Value == 60, "Another held-axis event does not impersonate the event that created just-pressed state."); Input.Service.CompleteFrame(false);
            Input.ParseInputEvent(otherPress); Input.ParseInputEvent(otherRelease); tree.ProcessFrame(.6);
            Check(h.Value == 60 && Input.IsActionPressed("ui_right"), "Releasing any directional action stops the repeat even while another direction remains held.");
        }
        finally { Input.ParseInputEvent(neutralAxis); Input.ParseInputEvent(otherRelease); Input.Service.CompleteFrame(false); Input.Service.CompleteFrame(true); }
        var dragEnds = 0; h.DragEnded += _ => dragEnds++;
        for (var reason = 0; reason < 4; reason++)
        {
            h.Value = 10; h.GrabFocus(); Input.ParseInputEvent(joy); viewport.PushInput(joy, true); Check(h.Value == 12, "Lifecycle repeat regression starts with an active controller gesture."); Input.Service.CompleteFrame(false);
            try
            {
                switch (reason)
                {
                    case 0: h.Hide(); h.Show(); break;
                    case 1: h.Editable = false; h.Editable = true; break;
                    case 2: v.GrabFocus(); h.GrabFocus(); break;
                    case 3: viewport.RemoveChild(h); viewport.AddChild(h); h.GrabFocus(); break;
                }
                tree.ProcessFrame(.6); Check(h.Value == 12 && dragEnds == 0, "Visibility, editability, focus and tree transitions cancel controller repeat without manufacturing DragEnded.");
            }
            finally { Input.ParseInputEvent(joyRelease); Input.Service.CompleteFrame(false); Input.Service.CompleteFrame(true); }
        }
        h.Value = 10; h.GrabFocus(); Input.ParseInputEvent(joy); viewport.PushInput(joy, true);
        Input.ParseInputEvent(joyRelease); Input.Service.CompleteFrame(false); tree.ProcessFrame(.1);
        try { Input.ActionPress("ui_right"); tree.ProcessFrame(.6); Check(h.Value == 12, "A missed release edge still disables idle repeat while no orientation action is held."); }
        finally { Input.ActionRelease("ui_right"); Input.Service.CompleteFrame(false); Input.Service.CompleteFrame(true); }
        h.Value = 10; Input.ParseInputEvent(joy); viewport.PushInput(joy, true);
        try { InputMap.EraseAction("ui_right"); tree.ProcessFrame(.6); Check(h.Value == 12, "Removing the held action mapping safely cancels its repeat loop."); }
        finally { Input.ParseInputEvent(joyRelease); InputMap.LoadFromProjectSettings(); Input.Service.CompleteFrame(false); Input.Service.CompleteFrame(true); }
    }

    private static void VerifyFailuresLifetimeAndPacking()
    {
        using var skin = new Skin(); var root = new Node(); var slider = new HorizontalProbe { Theme = skin.Theme, Size = new(100, 20) }; root.AddChild(slider);
        using var tree = new SceneTree(root); using var press = Button(new(50, 10)); using var release = Button(new(50, 10), false); using var move = new InputEventMouseMotion { Position = new(95, 10), ButtonMask = MouseButtonMask.Left };
        var values = 0; slider.ValueChanged += _ => values++;
        Action failure = () => throw new ApplicationException("expected drag observer failure"); slider.DragStarted += failure;
        Reject<AggregateException>(() => slider.Send(press)); slider.DragStarted -= failure;
        Check(slider.Value == 50 && values == 1, "A failing DragStarted observer does not skip the committed click and forced value phase."); slider.Send(release);
        slider.Value = 0; Action disable = () => slider.Editable = false; slider.DragStarted += disable; slider.Send(press); slider.DragStarted -= disable;
        Check(slider.Value == 0, "Reentrant editability changes cancel the pending click before value mutation."); slider.Editable = true; slider.Send(move); Check(slider.Value == 0, "Canceled gestures cannot consume later motion.");
        Action hide = slider.Hide; slider.DragStarted += hide; slider.Send(press); slider.DragStarted -= hide; slider.Show(); slider.Send(move);
        Check(slider.Value == 0, "Visibility cancellation prevents stale dragging after the slider reappears.");
        var nested = false; using var nestedPress = Button(new(75, 10));
        Action reenter = () => { if (!nested) { nested = true; slider.Send(nestedPress); } }; slider.DragStarted += reenter; slider.Send(press); slider.DragStarted -= reenter;
        Check(slider.Value == 77, "A nested gesture keeps its own click result instead of being overwritten by the outer gesture."); slider.Send(release);
        slider.Value = 0; Action detach = () => root.RemoveChild(slider); slider.DragStarted += detach; slider.Send(press); slider.DragStarted -= detach;
        Check(slider.Tree is null && slider.Value == 0, "Detachment during DragStarted cancels the pending click."); root.AddChild(slider); slider.Send(move); Check(slider.Value == 0, "Reattachment cannot revive the old drag gesture.");
        var dying = new HorizontalProbe { Name = "Dying", Theme = skin.Theme, Size = new(100, 20) }; root.AddChild(dying); var deadValues = 0;
        dying.ValueChanged += _ => deadValues++; dying.DragStarted += dying.Dispose; dying.Send(press);
        Check(dying.IsDisposed && deadValues == 0, "Disposal during DragStarted does not use the disposed range or send a stale value event.");
        slider.Send(press); Action<bool> endFailure = _ => throw new ApplicationException("expected drag-end failure"); slider.DragEnded += endFailure;
        var endError = Capture(() => slider.Send(release)); slider.DragEnded -= endFailure; slider.Send(move);
        Check(endError is AggregateException endAggregate && endAggregate.Flatten().InnerExceptions is [{ } cause] && cause is ApplicationException { Message: "expected drag-end failure" },
            "Drag-end delivery reports the exact observer failure through the callback aggregate.");
        Check(slider.Value == 50, "A failed DragEnded observer still leaves the gesture inactive.");
        Check(Task.Run(() => Capture(() => slider.TickCount = 5)).Result is InvalidOperationException, "Attached slider mutation requires its owner thread.");
        using var source = new HSlider { TickCount = 7, TicksOnBorders = true, TicksPosition = Slider.TickPosition.Both, Editable = false, Scrollable = false, Value = 35, Step = 5 };
        var vertical = new VSlider { Name = "Vertical" }; source.AddChild(vertical); vertical.Owner = source;
        using var packed = new PackedScene(); packed.Pack(source); using var copy = (HSlider)packed.Instantiate();
        Check(copy.GetType() == typeof(HSlider) && copy.GetChild(0).GetType() == typeof(VSlider) && copy.TickCount == 7 && copy.TicksOnBorders && copy.TicksPosition == Slider.TickPosition.Both && !copy.Editable && !copy.Scrollable && copy.Value == 35 && copy.Step == 5,
            "Packing preserves concrete orientation and typed slider/range state.");
        var captureChecked = false; var probe = new CaptureProbe { DuringCapture = () => { Reject<InvalidOperationException>(() => source.TickCount = 1); Reject<InvalidOperationException>(() => source.Editable = true); captureChecked = true; } };
        source.AddChild(probe); probe.Owner = source; packed.Pack(source); Check(captureChecked, "Capture forbids slider mutation from custom property callbacks.");
        using var dead = new HSlider(); dead.Dispose(); Reject<ObjectDisposedException>(() => _ = dead.TickCount); Reject<ObjectDisposedException>(() => dead.Editable = false);
    }

    private static void VerifyWarmInput()
    {
        using var skin = new Skin(); var viewport = new TestViewport(); var slider = new HorizontalProbe { Theme = skin.Theme, Size = new(100, 20) };
        viewport.AddChild(slider); using var tree = new SceneTree(viewport); slider.GrabFocus();
        using var press = Button(new(20, 10)); using var move = new InputEventMouseMotion { Position = new(80, 10), ButtonMask = MouseButtonMask.Left };
        using var release = Button(new(80, 10), false); using var key = new InputEventKey { Keycode = Key.Left, Pressed = true };
        var starts = 0; var ends = 0; slider.DragStarted += () => starts++; slider.DragEnded += _ => ends++;
        void Cycle() { slider.Send(press); slider.Send(move); slider.Send(release); viewport.PushInput(key, true); }
        for (var pass = 0; pass < 64; pass++) Cycle();
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var pass = 0; pass < 64; pass++) Cycle(); var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0 && starts == 128 && ends == 128 && slider.Value == 83,
            $"Warmed reused drag events and viewport-routed keyboard actions allocate {allocated} bytes and retain complete gesture delivery.");
    }

    internal static InputEventMouseButton Button(Vector2 position, bool pressed = true, MouseButton button = MouseButton.Left) => new() { Position = position, ButtonIndex = button, Pressed = pressed };
    private static void Near(double actual, double expected, double tolerance, string message) => Check(Math.Abs(actual - expected) < tolerance, message + $" ({actual}/{expected})");
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 300, 200); }
    private sealed class HorizontalProbe : HSlider { internal void Send(InputEvent inputEvent) => OnGUIInput(inputEvent); }
    private sealed class CaptureProbe : Node
    {
        private static readonly PropertyDescriptor<CaptureProbe, int> ProbeProperty = new("Probe", node => { node.DuringCapture?.Invoke(); return 0; }, (node, _) => node.EnsureMutable(), _ => 0, stored: true);
        internal Action? DuringCapture;
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(ProbeProperty);
        protected override Func<Node> CreateSceneInstanceFactory() => CreateProbe;
        private static Node CreateProbe() => new CaptureProbe();
    }

    internal sealed class Skin : IDisposable
    {
        internal readonly Theme Theme = new();
        internal readonly StyleBoxFlat Track = new() { BGColor = Colors.Blue, AntiAliasing = false };
        internal readonly StyleBoxFlat Fill = new() { BGColor = Colors.Red, AntiAliasing = false };
        internal readonly StyleBoxFlat Highlight = new() { BGColor = Colors.Yellow, AntiAliasing = false };
        internal readonly ImageTexture Grabber = Solid(6, 8, Colors.White), Hovered = Solid(10, 12, Colors.Cyan), Disabled = Solid(4, 6, new(.5f, .5f, .5f, 1));
        internal readonly ImageTexture Tick;
        internal Skin()
        {
            Track.SetContentMarginAll(2);
            using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.SetPixel(0, 0, Colors.Magenta); image.SetPixel(1, 0, Colors.Cyan); image.SetPixel(0, 1, Colors.Yellow); image.SetPixel(1, 1, Colors.White); Tick = ImageTexture.CreateFromImage(image);
            Theme.SetStyleBox("slider", "Slider", Track); Theme.SetStyleBox("grabber_area", "Slider", Fill); Theme.SetStyleBox("grabber_area_highlight", "Slider", Highlight);
            Theme.SetIcon("grabber", "Slider", Grabber); Theme.SetIcon("grabber_highlight", "Slider", Hovered); Theme.SetIcon("grabber_disabled", "Slider", Disabled); Theme.SetIcon("tick", "Slider", Tick);
            Theme.SetConstant("center_grabber", "Slider", 0); Theme.SetConstant("grabber_offset", "Slider", 0); Theme.SetConstant("tick_offset", "Slider", 2);
        }
        private static ImageTexture Solid(int width, int height, Color color) { using var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8); image.Fill(color); return ImageTexture.CreateFromImage(image); }
        public void Dispose() { Theme.Dispose(); Track.Dispose(); Fill.Dispose(); Highlight.Dispose(); Grabber.Dispose(); Hovered.Dispose(); Disabled.Dispose(); Tick.Dispose(); }
    }
}
