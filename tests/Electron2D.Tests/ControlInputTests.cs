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
            if (e is InputEventMouse mouse) Check(mouse.Position == (mouse is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } ? new Vector2(75, 75) : new Vector2(2, 3)), "GUI coordinates are local to the child.");
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
        Reject<ArgumentOutOfRangeException>(() => child.FocusMode = (ControlFocusMode)2);
        using var settings = new Control { MouseForcePassScrollEvents = false };
        var settingsChild = new Control { FocusMode = ControlFocusMode.Click };
        settings.AddChild(settingsChild);
        settingsChild.Owner = settings;
        using var packed = new PackedScene();
        packed.Pack(settings);
        using var copy = (Control)packed.Instantiate();
        Check(!copy.MouseForcePassScrollEvents && ((Control)copy.GetChild(0)).FocusMode == ControlFocusMode.Click,
            "Packed controls retain GUI input and focus policy.");
        Console.WriteLine("Control GUI input routing checks passed.");
    }

    private sealed class TestViewport : Viewport { public override Rect GetVisibleRect() => new(0, 0, 100, 100); }
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
}
