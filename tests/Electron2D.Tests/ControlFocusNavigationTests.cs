using Electron2D;

internal static class ControlFocusNavigationTests
{
    internal static void Run()
    {
        var map = InputMap.Instance;
        Check(map.HasAction("ui_focus_next") && map.HasAction("ui_focus_prev") && map.HasAction("ui_right") &&
            map.ActionGetEvents("ui_focus_next").Count != 0, "GUI navigation actions are registered by default.");

        var root = new TestViewport();
        var first = new Control { Name = "first", Position = new(0, 0), Size = new(10, 10), FocusMode = ControlFocusMode.All };
        var skipped = new Control { Name = "skipped", Position = new(20, 0), Size = new(10, 10) };
        var second = new Control { Name = "second", Position = new(40, 0), Size = new(10, 10), FocusMode = ControlFocusMode.All };
        var click = new Control { Name = "click", Position = new(60, 0), Size = new(10, 10), FocusMode = ControlFocusMode.Click };
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
        var detached = new Control { FocusMode = ControlFocusMode.All, FocusNext = "../next", FocusNeighborBottom = "../below" };
        packed.Pack(detached);
        using var copy = (Control)packed.Instantiate();
        Check(copy.FocusMode == ControlFocusMode.All && copy.FocusNext == "../next" && copy.FocusNeighborBottom == "../below",
            "Focus policy and paths survive typed scene capture.");
        detached.Dispose();
        Console.WriteLine("Control focus navigation checks passed.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private sealed class TestViewport : Viewport { public override Rect GetVisibleRect() => new(0, 0, 100, 100); }
}
