using Electron2D;

internal static class ControlFocusNavigationTests
{
    internal static void Run()
    {
        var map = InputMap.Service;
        Check(InputMap.HasAction("ui_focus_next") && InputMap.HasAction("ui_focus_prev") && InputMap.HasAction("ui_right") &&
            InputMap.ActionGetEvents("ui_focus_next").Count != 0, "GUI navigation actions are registered by default.");

        var root = new TestViewport();
        var first = new Control { Name = "first", Position = new(0, 0), Size = new(10, 10), FocusMode = FocusMode.All };
        var skipped = new Control { Name = "skipped", Position = new(20, 0), Size = new(10, 10) };
        var second = new Control { Name = "second", Position = new(40, 0), Size = new(10, 10), FocusMode = FocusMode.All };
        var click = new Control { Name = "click", Position = new(60, 0), Size = new(10, 10), FocusMode = FocusMode.Click };
        root.AddChild(first); root.AddChild(skipped); root.AddChild(second); root.AddChild(click);
        using var tree = new SceneTree(root);

        Check(ReferenceEquals(first.FindNextValidFocus(), second) && ReferenceEquals(second.FindPrevValidFocus(), first),
            "Traversal skips nonkeyboard controls and wraps in scene order.");
        Check(ReferenceEquals(first.FindValidFocusNeighbor(Side.Right), second) &&
            ReferenceEquals(second.FindValidFocusNeighbor(Side.Left), first), "Directional traversal uses eligible rectangles.");
        first.FocusNext = "../click";
        Check(ReferenceEquals(first.FindNextValidFocus(), click), "Explicit sequential path may target Click mode.");
        first.FocusNext = string.Empty;
        first.FocusNeighborRight = "../click";
        Check(ReferenceEquals(first.FindValidFocusNeighbor(Side.Right), click), "Explicit directional path takes precedence.");
        first.FocusNeighborRight = string.Empty;
        first.FocusNext = "../missing";
        Check(first.FindNextValidFocus() is null, "An invalid explicit focus path does not select another control.");
        first.FocusNext = string.Empty;
        first.FocusNeighborRight = "../skipped";
        skipped.FocusNeighborRight = "../first";
        Check(ReferenceEquals(first.FindValidFocusNeighbor(Side.Right), second),
            "A cyclic, ineligible neighbor chain falls back to spatial search.");
        first.FocusNeighborRight = string.Empty;
        skipped.FocusNeighborRight = string.Empty;

        first.GrabFocus(hideFocus: true);
        using var tab = new InputEventKey { Pressed = true, Keycode = Key.Tab };
        root.PushInput(tab, inLocalCoordinates: true);
        Check(second.HasFocus() && !first.HasFocus() && second.HasFocus(ignoreHiddenFocus: true),
            "Tab moves focus and reveals its indicator.");
        using var shiftTab = new InputEventKey { Pressed = true, Keycode = Key.Tab, ShiftPressed = true };
        root.PushInput(shiftTab, inLocalCoordinates: true);
        Check(first.HasFocus(), "Shift+Tab reverses focus traversal.");
        using var right = new InputEventKey { Pressed = true, Keycode = Key.Right };
        root.PushInput(right, inLocalCoordinates: true);
        Check(second.HasFocus(), "Right arrow follows spatial focus.");
        using var dpadLeft = new InputEventJoypadButton { Pressed = true, ButtonIndex = JoyButton.DpadLeft, Device = 5 };
        root.PushInput(dpadLeft, inLocalCoordinates: true);
        Check(first.HasFocus(), "D-pad left moves GUI focus from a nonzero controller device.");
        using var dpadRight = new InputEventJoypadButton { Pressed = true, ButtonIndex = JoyButton.DpadRight, Device = 5 };
        root.PushInput(dpadRight, inLocalCoordinates: true);
        Check(second.HasFocus(), "D-pad right moves GUI focus through the default action.");
        second.ReleaseFocus();
        root.PushInput(tab, inLocalCoordinates: true);
        Check(second.HasFocus(), "Tab finds a focus target when the viewport has no owner.");

        second.GUIInput += e => { if (e is InputEventKey { Keycode: Key.Tab }) second.AcceptEvent(); };
        root.PushInput(tab, inLocalCoordinates: true);
        Check(second.HasFocus(), "A control can consume navigation input before viewport traversal.");

        second.Visible = false;
        Check(!second.HasFocus() && ReferenceEquals(first.FindNextValidFocus(), first), "Hidden targets are skipped and focused controls release focus.");
        Check(first.GetFocusNeighbor(Side.Right) == string.Empty, "Directional override defaults to an empty path.");
        Reject<ArgumentOutOfRangeException>(() => first.GetFocusNeighbor((Side)9));
        Reject<ArgumentNullException>(() => first.FocusNext = null!);

        using var packed = new PackedScene();
        var detached = new Control { FocusMode = FocusMode.All, FocusNext = "../next", FocusNeighborBottom = "../below" };
        packed.Pack(detached);
        using var copy = (Control)packed.Instantiate();
        Check(copy.FocusMode == FocusMode.All && copy.FocusNext == "../next" && copy.FocusNeighborBottom == "../below",
            "Focus policy and paths survive typed scene capture.");
        detached.Dispose();

        second.Visible = true;
        second.FocusNeighborRight = "../first";
        first.GrabFocus();
        Engine.Start(tree);
        try
        {
            using var stick = new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = 1f, Device = 9 };
            Input.ParseInputEvent(stick);
            Check(second.HasFocus(), "Left-stick right moves GUI focus through the active input loop.");
            using var heldStick = new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = 1f, Device = 9 };
            Input.ParseInputEvent(heldStick);
            Check(second.HasFocus(), "Holding the stick does not repeat a directional focus transition.");
            using var neutralStick = new InputEventJoypadMotion { Axis = JoyAxis.LeftX, Device = 9 };
            Input.ParseInputEvent(neutralStick);
            using var repeatedStick = new InputEventJoypadMotion { Axis = JoyAxis.LeftX, AxisValue = 1f, Device = 9 };
            Input.ParseInputEvent(repeatedStick);
            Check(first.HasFocus(), "Releasing and pressing the stick permits another focus transition.");
            Input.ParseInputEvent(neutralStick);
        }
        finally { Engine.Stop(); }
        Console.WriteLine("Control focus navigation checks passed.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 100, 100); }
}
