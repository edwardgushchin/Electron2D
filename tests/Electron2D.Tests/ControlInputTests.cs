using Electron2D;

internal static class ControlInputTests
{
    internal static void Run()
    {
        var root = new TestViewport();
        var parent = new Control { Name = "parent", Position = new(10, 10), Size = new(60, 50) };
        var child = new Probe { Position = new(5, 5), Size = new(20, 20), MouseFilter = ControlMouseFilter.Pass, FocusMode = ControlFocusMode.Click, InputEnabled = true };
        var overlay = new Control { Name = "overlay", Position = new(15, 15), Size = new(20, 20), MouseFilter = ControlMouseFilter.Ignore, ZIndex = 2 };
        var observer = new Probe { UnhandledInputEnabled = true };
        root.AddChild(parent); parent.AddChild(child); root.AddChild(overlay); root.AddChild(observer);
        using var tree = new SceneTree(root);
        var order = new List<string>();
        InputEvent? borrowed = null;
        child.InputAction = e => order.Add("input");
        child.GUIAction = e =>
        {
            order.Add("child"); borrowed = e;
            if (e is InputEventMouse mouse)
            {
                var released = mouse is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false };
                Check(mouse.Position == (released ? new Vector2(75, 75) : new Vector2(2, 3)),
                    "GUI coordinates are local to the child.");
                Check(mouse.GlobalPosition == (released ? new Vector2(90, 90) : new Vector2(17, 18)),
                    "GUI global coordinates are in the default canvas.");
            }
        };
        parent.GUIInput += e => { order.Add("parent"); if (e is InputEventMouse mouse) Check(mouse.Position == (mouse is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } ? new Vector2(80, 80) : new Vector2(7, 8)), "Parent receives its own local copy."); };
        observer.UnhandledAction = _ => order.Add("unhandled");
        child.FocusEntered += () => order.Add("focus");
        child.FocusExited += () => order.Add("blur");

        using var press = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = new(17, 18) };
        root.PushInput(press, inLocalCoordinates: true);
        Check(order.SequenceEqual(["input", "focus", "child", "parent"]), "Input precedes GUI routing; Stop parent consumes the event.");
        Check(child.HasFocus() && !child.HasFocus(ignoreHiddenFocus: true), "Pointer focus is hidden but receives keyboard input.");
        Check(borrowed!.IsDisposed && !press.IsDisposed && press.Position == new Vector2(17, 18), "GUI copies are temporary and the source stays borrowed.");

        order.Clear();
        using var key = new InputEventKey { Pressed = true };
        root.PushInput(key, inLocalCoordinates: true);
        Check(order.SequenceEqual(["input", "child", "unhandled"]), "Focused GUI key input precedes unhandled input.");
        order.Clear();
        child.Accept = true;
        root.PushInput(key, inLocalCoordinates: true);
        Check(order.SequenceEqual(["input", "child"]), "AcceptEvent stops the remaining input stages.");
        child.Accept = false;

        order.Clear();
        using var release = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = new(90, 90) };
        child.CheckMousePosition = false;
        root.PushInput(release, inLocalCoordinates: true);
        Check(order.SequenceEqual(["input", "child", "parent"]), "A release outside the rectangle reaches the pressed control.");

        order.Clear();
        using var wheel = new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown, Pressed = true, Position = new(17, 18) };
        root.PushInput(wheel, inLocalCoordinates: true);
        Check(order.SequenceEqual(["input", "child", "parent", "unhandled"]), "Wheel events pass a Stop parent by default.");
        order.Clear();
        parent.MouseForcePassScrollEvents = false;
        root.PushInput(wheel, inLocalCoordinates: true);
        Check(order.SequenceEqual(["input", "child", "parent"]), "Disabling wheel pass consumes at the Stop parent.");

        order.Clear();
        child.RejectHits = true;
        root.PushInput(press, inLocalCoordinates: true);
        Check(order.SequenceEqual(["input", "parent"]), "Custom hit testing can leave the pointer to a parent control.");
        child.RejectHits = false;

        order.Clear();
        child.Visible = false;
        Check(order.SequenceEqual(["blur"]) && !child.HasFocus(), "Hiding a focused control releases focus.");
        order.Clear();
        root.PushInput(key, inLocalCoordinates: true);
        Check(order.SequenceEqual(["input", "unhandled"]), "Hidden control receives no focused input.");

        order.Clear();
        overlay.MouseFilter = ControlMouseFilter.Stop;
        overlay.GUIInput += _ => order.Add("overlay");
        using var topPress = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = new(17, 18) };
        root.PushInput(topPress, inLocalCoordinates: true);
        Check(order.SequenceEqual(["input", "overlay"]), "Higher Z control wins hit testing.");

        overlay.MouseFilter = ControlMouseFilter.Ignore;
        child.Visible = true;
        order.Clear();
        child.Fail = true;
        Reject<AggregateException>(() => root.PushInput(press, inLocalCoordinates: true));
        Check(order.Contains("parent") && borrowed!.IsDisposed, "A GUI callback failure does not skip parent delivery or leak local input.");
        child.Fail = false;
        Reject<ArgumentOutOfRangeException>(() => child.MouseFilter = (ControlMouseFilter)9);
        Reject<ArgumentOutOfRangeException>(() => child.FocusMode = (ControlFocusMode)3);
        using var settings = new Control { MouseForcePassScrollEvents = false };
        var settingsChild = new Control { FocusMode = ControlFocusMode.Click };
        settings.AddChild(settingsChild);
        settingsChild.Owner = settings;
        using var packed = new PackedScene();
        packed.Pack(settings);
        using var copy = (Control)packed.Instantiate();
        Check(!copy.MouseForcePassScrollEvents && ((Control)copy.GetChild(0)).FocusMode == ControlFocusMode.Click,
            "Packed controls retain GUI input and focus policy.");
        FocusContract();
        CanvasLayerMouseCoordinates();
        Console.WriteLine("Control GUI input routing checks passed.");
    }

    private static void CanvasLayerMouseCoordinates()
    {
        var root = new TestViewport();
        var layer = new CanvasLayer { Transform = new Transform(0f, new Vector2(20f, 10f)) };
        var control = new Control { Position = new Vector2(5f, 8f), Size = new Vector2(20f, 20f) };
        root.AddChild(layer);
        layer.AddChild(control);
        using var tree = new SceneTree(root);
        var calls = 0;
        InputEvent? delivered = null;
        control.GUIInput += inputEvent =>
        {
            calls++;
            delivered = inputEvent;
            Check(inputEvent is InputEventMouse mouse && mouse.Position == new Vector2(3f, 4f) &&
                mouse.GlobalPosition == new Vector2(8f, 12f),
                "GUI mouse coordinates separate Control-local position from CanvasLayer-global position.");
        };
        using var source = new InputEventMouseMotion
        {
            Position = new Vector2(28f, 22f),
            GlobalPosition = new Vector2(999f, 999f),
        };
        root.PushInput(source, inLocalCoordinates: true);
        Check(calls == 1 && delivered!.IsDisposed && source.GlobalPosition == new Vector2(999f, 999f),
            "CanvasLayer GUI dispatch owns its corrected copy and leaves the borrowed source unchanged.");

        layer.Transform = new Transform(new Vector2(1e-20f, 0f), Vector2.Down, Vector2.Zero);
        control.Scale = new Vector2(1e20f, 1f);
        control.Size = new Vector2(float.MaxValue, 20f);
        using var extreme = new InputEventMouseMotion { Position = new Vector2(1e20f, 10f) };
        Reject<AggregateException>(() => root.PushInput(extreme, inLocalCoordinates: true));
        Check(calls == 1 && !extreme.IsDisposed,
            "Overflow in derived CanvasLayer-global coordinates rejects delivery without taking the source.");
    }

    private static void FocusContract()
    {
        using var detached = new TestViewport();
        Check(detached.GetGUIFocusOwner() is null, "Detached viewport has no focus owner.");
        detached.ReleaseGUIFocus();

        var neutralRoot = new Node();
        var neutralControl = new Control { FocusMode = ControlFocusMode.Click };
        neutralRoot.AddChild(neutralControl);
        using (var neutralTree = new SceneTree(neutralRoot))
        {
            neutralControl.GrabFocus();
            Check(!neutralControl.HasFocus(), "A Control without a viewport cannot hold invisible GUI focus.");
        }

        var root = new TestViewport();
        var order = new List<string>();
        var first = new FocusProbe("first", order) { Name = "first", FocusMode = ControlFocusMode.Click };
        var second = new FocusProbe("second", order) { Name = "second", FocusMode = ControlFocusMode.Click };
        root.AddChild(first);
        root.AddChild(second);
        using var tree = new SceneTree(root);
        root.GUIFocusChanged += control => order.Add("viewport:" + control.Name);
        first.FocusEntered += () => order.Add("first:entered");
        first.FocusExited += () => order.Add("first:exited");
        second.FocusEntered += () => order.Add("second:entered");
        second.FocusExited += () => order.Add("second:exited");

        first.GrabFocus();
        Check(ReferenceEquals(root.GetGUIFocusOwner(), first) &&
            order.SequenceEqual(["viewport:first", "first:enter-notification", "first:entered"]),
            "Focus gain notifies the viewport before the control.");
        order.Clear();
        first.GrabFocus(hideFocus: true);
        Check(order.Count == 0 && first.HasFocus() && !first.HasFocus(ignoreHiddenFocus: true),
            "Changing focus visibility does not repeat notifications.");

        second.GrabFocus();
        Check(ReferenceEquals(root.GetGUIFocusOwner(), second) &&
            order.SequenceEqual(["first:exit-notification", "first:exited", "viewport:second", "second:enter-notification", "second:entered"]),
            "Focus transfer exits the old owner before entering the new one.");
        order.Clear();
        root.ReleaseGUIFocus();
        Check(root.GetGUIFocusOwner() is null &&
            order.SequenceEqual(["second:exit-notification", "second:exited"]),
            "Explicit viewport release clears focus without a viewport change event.");
        order.Clear();
        root.ReleaseGUIFocus();
        Check(order.Count == 0, "Releasing empty focus does nothing.");

        first.GrabFocus();
        order.Clear();
        first.FailExitNotification = true;
        first.FocusExited += () => throw new ApplicationException("exit");
        root.GUIFocusChanged += _ => throw new ApplicationException("viewport");
        second.FocusEntered += () => throw new ApplicationException("enter");
        var failure = Catch<AggregateException>(() => second.GrabFocus());
        Check(failure.InnerExceptions.Count == 4 && ReferenceEquals(root.GetGUIFocusOwner(), second) &&
            order.SequenceEqual(["first:exit-notification", "first:exited", "viewport:second", "second:enter-notification", "second:entered"]),
            "Callback failures leave committed focus and deliver later stages.");
        Reject<InvalidOperationException>(() => Task.Run(root.GetGUIFocusOwner).GetAwaiter().GetResult());
        root.ReleaseGUIFocus();

        var reentrantRoot = new TestViewport();
        var reentrantOrder = new List<string>();
        var reentrant = new FocusProbe("reentrant", reentrantOrder) { FocusMode = ControlFocusMode.Click };
        reentrantRoot.AddChild(reentrant);
        using var reentrantTree = new SceneTree(reentrantRoot);
        reentrant.FocusEntered += () => reentrantOrder.Add("entered");
        reentrant.FocusExited += () => reentrantOrder.Add("exited");
        var releaseFromViewport = true;
        reentrantRoot.GUIFocusChanged += _ =>
        {
            reentrantOrder.Add("viewport");
            if (releaseFromViewport) reentrantRoot.ReleaseGUIFocus();
        };
        reentrant.GrabFocus();
        Check(reentrantRoot.GetGUIFocusOwner() is null &&
            reentrantOrder.SequenceEqual(["viewport", "reentrant:exit-notification", "exited"]),
            "Reentrant viewport release prevents a stale focus-enter notification.");
        releaseFromViewport = false;
        reentrantOrder.Clear();
        reentrant.EnterNotificationAction = reentrantRoot.ReleaseGUIFocus;
        reentrant.GrabFocus();
        Check(reentrantRoot.GetGUIFocusOwner() is null &&
            reentrantOrder.SequenceEqual(["viewport", "reentrant:enter-notification", "reentrant:exit-notification", "exited"]),
            "Reentrant notification release prevents a stale focus-enter event.");
    }

    private sealed class TestViewport : Viewport { public override Rect GetVisibleRect() => new(0, 0, 100, 100); }
    private sealed class FocusProbe(string name, List<string> order) : Control
    {
        internal bool FailExitNotification;
        internal Action? EnterNotificationAction;
        protected override void OnNotification(int what)
        {
            if (what == NotificationFocusEnter) order.Add(name + ":enter-notification");
            if (what == NotificationFocusExit) order.Add(name + ":exit-notification");
            base.OnNotification(what);
            if (what == NotificationFocusEnter) EnterNotificationAction?.Invoke();
            if (what == NotificationFocusExit && FailExitNotification) throw new ApplicationException("exit notification");
        }
    }
    private sealed class Probe : Control
    {
        internal Action<InputEvent>? InputAction;
        internal Action<InputEvent>? UnhandledAction;
        internal Action<InputEvent>? GUIAction;
        internal bool Accept;
        internal bool Fail;
        internal bool CheckMousePosition = true;
        internal bool RejectHits;
        protected override bool HasPoint(Vector2 point) => !RejectHits && base.HasPoint(point);
        protected override void OnInput(InputEvent inputEvent) => InputAction?.Invoke(inputEvent);
        protected override void OnUnhandledInput(InputEvent inputEvent) => UnhandledAction?.Invoke(inputEvent);
        protected override void OnGUIInput(InputEvent inputEvent)
        {
            if (inputEvent is InputEventMouse mouse && CheckMousePosition) Check(mouse.Position == new Vector2(2, 3), "Virtual GUI input is localized.");
            GUIAction?.Invoke(inputEvent);
            if (Accept) AcceptEvent();
            if (Fail) throw new ApplicationException("GUI callback");
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static T Catch<T>(Action action) where T : Exception { try { action(); } catch (T error) { return error; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
