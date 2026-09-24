using Electron2D;

internal static class ControlHoverTests
{
    internal static void Run()
    {
        var root = new TestViewport();
        var parent = new Probe("parent") { Name = "parent", Position = new(10, 10), Size = new(60, 50) };
        var left = new Probe("left") { Name = "left", Size = new(20, 20), MouseFilter = ControlMouseFilter.Pass };
        var right = new Probe("right") { Name = "right", Position = new(25, 0), Size = new(20, 20), MouseFilter = ControlMouseFilter.Pass };
        root.AddChild(parent); parent.AddChild(left); parent.AddChild(right);
        using var tree = new SceneTree(root);
        var signals = new List<string>();
        foreach (var probe in new[] { parent, left, right })
        {
            var current = probe;
            current.MouseEntered += () => signals.Add(current.Label + "+");
            current.MouseExited += () => signals.Add(current.Label + "-");
        }

        Move(root, new(15, 15));
        Check(signals.SequenceEqual(["parent+", "left+"]) &&
              parent.Notifications.Contains(Control.NotificationMouseEnter) &&
              left.Notifications.Contains(Control.NotificationMouseEnterSelf),
            "Hover enters ancestors before the direct control and sends self notification.");

        signals.Clear();
        Move(root, new(40, 15));
        Check(signals.SequenceEqual(["left-", "right+"]) &&
              left.Notifications.Contains(Control.NotificationMouseExitSelf),
            "Moving between siblings keeps the shared parent hovered.");

        signals.Clear();
        right.MouseFilter = ControlMouseFilter.Ignore;
        Check(signals.SequenceEqual(["right-"]) && parent.Notifications.Last() == Control.NotificationMouseEnterSelf,
            "Ignoring a hovered control immediately exposes its parent.");

        signals.Clear();
        tree.ClearGUIHover();
        Check(signals.SequenceEqual(["parent-"]), "Leaving the viewport clears hover exactly once.");

        Move(root, new(15, 15));
        signals.Clear();
        parent.Visible = false;
        Check(signals.SequenceEqual(["left-", "parent-"]), "Hiding an ancestor releases its hover chain.");

        Check((int)Control.CursorShape.Help == (int)Input.CursorShape.Help &&
              left.GetCursorShape(new(2, 3)) == Control.CursorShape.Arrow,
            "Control cursor values share the native shape identities.");
        left.MouseDefaultCursorShape = Control.CursorShape.IBeam;
        Check(left.GetCursorShape(new(2, 3)) == Control.CursorShape.IBeam &&
              left.GetCursorShape(new(12, 3)) == Control.CursorShape.PointingHand,
            "A local-position override falls back to the stored cursor shape.");
        Reject<ArgumentException>(() => left.GetCursorShape(new(float.NaN, 0)));
        Reject<ArgumentOutOfRangeException>(() => left.MouseDefaultCursorShape = (Control.CursorShape)17);

        using var template = new Control { MouseDefaultCursorShape = Control.CursorShape.Help };
        using var packed = new PackedScene(); packed.Pack(template);
        using var copy = (Control)packed.Instantiate();
        Check(copy.MouseDefaultCursorShape == Control.CursorShape.Help,
            "Packed controls retain the cursor policy.");

        var failureRoot = new TestViewport();
        var failureParent = new Probe("failureParent") { Name = "failureParent", Size = new(50, 50) };
        var failureChild = new Probe("failureChild") { Name = "failureChild", Size = new(25, 25), MouseFilter = ControlMouseFilter.Pass };
        failureRoot.AddChild(failureParent);
        failureParent.AddChild(failureChild);
        using var failureTree = new SceneTree(failureRoot);
        var childEntered = false;
        failureParent.MouseEntered += () => throw new InvalidOperationException("hover subscriber failed");
        failureChild.MouseEntered += () => childEntered = true;
        Reject<AggregateException>(() => Move(failureRoot, new(5, 5)));
        Check(childEntered && failureChild.Notifications.Contains(Control.NotificationMouseEnterSelf),
            "A failed ancestor subscriber does not skip child hover or direct-target notifications.");
        Console.WriteLine("Control hover, notifications, cursor lookup and packed state passed.");
    }

    private static void Move(Viewport viewport, Vector2 position)
    {
        using var motion = new InputEventMouseMotion { Position = position };
        viewport.PushInput(motion, inLocalCoordinates: true);
    }

    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 100, 100); }

    private sealed class Probe(string label) : Control
    {
        internal string Label { get; } = label;
        internal List<int> Notifications { get; } = [];
        protected override CursorShape OnGetCursorShape(Vector2 atPosition) =>
            atPosition.X > 10 ? CursorShape.PointingHand : base.OnGetCursorShape(atPosition);
        protected override void OnNotification(int what)
        {
            if (what is NotificationMouseEnter or NotificationMouseExit or NotificationMouseEnterSelf or NotificationMouseExitSelf)
                Notifications.Add(what);
            base.OnNotification(what);
        }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
