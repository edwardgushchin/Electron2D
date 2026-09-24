using Electron2D;

internal static class ControlRecursiveBehaviorTests
{
    internal static void Run()
    {
        var root = new TestViewport();
        var parent = new Control { Name = "parent", Size = new(80, 80), MouseFilter = ControlMouseFilter.Pass };
        var child = new Control { Name = "child", Size = new(30, 30), MouseFilter = ControlMouseFilter.Pass, FocusMode = ControlFocusMode.All };
        root.AddChild(parent);
        parent.AddChild(child);
        using var tree = new SceneTree(root);
        var parentInputs = 0;
        var childInputs = 0;
        var exits = 0;
        parent.GUIInput += _ => parentInputs++;
        child.GUIInput += _ => childInputs++;
        child.MouseExited += () => exits++;

        Check(child.GetMouseFilterWithOverride() == ControlMouseFilter.Pass &&
              child.GetFocusModeWithOverride() == ControlFocusMode.All, "Inherited root policies permit input.");
        Move(root);
        child.GrabFocus();
        Check(child.HasFocus(), "The inherited focus policy permits explicit focus.");
        parent.FocusBehaviorRecursive = ControlFocusBehaviorRecursive.Disabled;
        Check(!child.HasFocus() && child.GetFocusModeWithOverride() == ControlFocusMode.None,
            "Disabling an ancestor immediately releases descendant focus.");
        child.GrabFocus();
        Check(!child.HasFocus() && child.FindNextValidFocus() is null, "Disabled descendants cannot grab or navigate to focus.");
        parent.MouseBehaviorRecursive = ControlMouseBehaviorRecursive.Disabled;
        Check(child.GetMouseFilterWithOverride() == ControlMouseFilter.Ignore && exits == 1,
            "Disabling an ancestor immediately releases descendant hover.");
        var parentBefore = parentInputs;
        var childBefore = childInputs;
        Press(root);
        Check(parentInputs == parentBefore && childInputs == childBefore,
            "A disabled subtree does not receive pointer input after its earlier hover motion.");

        child.FocusBehaviorRecursive = ControlFocusBehaviorRecursive.Enabled;
        child.MouseBehaviorRecursive = ControlMouseBehaviorRecursive.Enabled;
        Check(child.GetFocusModeWithOverride() == ControlFocusMode.All &&
              child.GetMouseFilterWithOverride() == ControlMouseFilter.Pass,
            "Explicit child policies override disabled ancestors.");
        child.GrabFocus();
        Check(child.HasFocus(), "An explicitly enabled child can grab focus.");
        Press(root);
        Check(childInputs == childBefore + 1 && parentInputs == parentBefore,
            "An enabled child receives pointer input without bubbling through a disabled parent.");
        child.FocusBehaviorRecursive = ControlFocusBehaviorRecursive.Inherited;
        child.MouseBehaviorRecursive = ControlMouseBehaviorRecursive.Inherited;
        ReleaseOutside(root);
        Check(!child.HasFocus() && child.GetFocusModeWithOverride() == ControlFocusMode.None &&
              child.GetMouseFilterWithOverride() == ControlMouseFilter.Ignore && childInputs == childBefore + 1,
            "Returning to inheritance releases focus and invalidates pointer capture.");
        var neutral = new Node();
        var independent = new Control { FocusMode = ControlFocusMode.All, MouseFilter = ControlMouseFilter.Pass };
        parent.AddChild(neutral);
        neutral.AddChild(independent);
        Check(independent.GetFocusModeWithOverride() == ControlFocusMode.All &&
              independent.GetMouseFilterWithOverride() == ControlMouseFilter.Pass,
            "A non-Control parent ends recursive policy inheritance.");
        parent.FocusBehaviorRecursive = ControlFocusBehaviorRecursive.Inherited;
        parent.MouseBehaviorRecursive = ControlMouseBehaviorRecursive.Inherited;
        Check(child.GetFocusModeWithOverride() == ControlFocusMode.All &&
              child.GetMouseFilterWithOverride() == ControlMouseFilter.Pass,
            "A root inherited policy permits both input kinds again.");

        Reject<ArgumentOutOfRangeException>(() => parent.FocusBehaviorRecursive = (ControlFocusBehaviorRecursive)3);
        Reject<ArgumentOutOfRangeException>(() => parent.MouseBehaviorRecursive = (ControlMouseBehaviorRecursive)(-1));
        Check(parent.FocusBehaviorRecursive == ControlFocusBehaviorRecursive.Inherited &&
              parent.MouseBehaviorRecursive == ControlMouseBehaviorRecursive.Inherited,
            "Invalid policies leave stored state unchanged.");

        using var template = new Control
        {
            FocusBehaviorRecursive = ControlFocusBehaviorRecursive.Disabled,
            MouseBehaviorRecursive = ControlMouseBehaviorRecursive.Disabled
        };
        var enabled = new Control
        {
            FocusBehaviorRecursive = ControlFocusBehaviorRecursive.Enabled,
            MouseBehaviorRecursive = ControlMouseBehaviorRecursive.Enabled
        };
        template.AddChild(enabled);
        enabled.Owner = template;
        using var packed = new PackedScene();
        packed.Pack(template);
        using var copy = (Control)packed.Instantiate();
        var copyChild = (Control)copy.GetChild(0);
        Check(copy.FocusBehaviorRecursive == ControlFocusBehaviorRecursive.Disabled &&
              copy.MouseBehaviorRecursive == ControlMouseBehaviorRecursive.Disabled &&
              copyChild.FocusBehaviorRecursive == ControlFocusBehaviorRecursive.Enabled &&
              copyChild.MouseBehaviorRecursive == ControlMouseBehaviorRecursive.Enabled,
            "Packed controls retain parent and child recursive policies.");

        Console.WriteLine("Control recursive focus and pointer behavior checks passed.");
    }

    private static void Move(Viewport root)
    {
        using var motion = new InputEventMouseMotion { Position = new(5, 5) };
        root.PushInput(motion, inLocalCoordinates: true);
    }

    private static void Press(Viewport root)
    {
        using var press = new InputEventMouseButton { Position = new(5, 5), ButtonIndex = MouseButton.Left, Pressed = true };
        root.PushInput(press, inLocalCoordinates: true);
    }

    private static void ReleaseOutside(Viewport root)
    {
        using var release = new InputEventMouseButton { Position = new(95, 95), ButtonIndex = MouseButton.Left, Pressed = false };
        root.PushInput(release, inLocalCoordinates: true);
    }

    private sealed class TestViewport : Viewport { public override Rect GetVisibleRect() => new(0, 0, 100, 100); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
