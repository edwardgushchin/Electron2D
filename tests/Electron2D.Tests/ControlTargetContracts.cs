using Electron2D;

internal static partial class ControlTargetContracts
{
    internal static void Run()
    {
        Order(new SubViewport { Size = new(100, 100) });
        CustomClip();
        Stationary();
        Routing();
        PreparedFrames();
        HitCallbackOrder();
        Console.WriteLine("Control target: scene order, canvas layers, TopLevel, viewport isolation, local HasPoint clipping and stationary geometry/visibility passed.");
    }

    private static void HitCallbackOrder()
    {
        var root = new SubViewport { Size = new(100, 100) };
        var lower = new ThrowingHit { Size = new(40, 40) };
        var layer = new CanvasLayer { Layer = 2 };
        var upper = new Control { Size = new(40, 40) };
        root.AddChild(layer); layer.AddChild(upper); root.AddChild(lower);
        using var tree = new SceneTree(root);
        using var motion = new InputEventMouseMotion { Position = new(5, 5) };
        root.PushInput(motion, true);
        Check(lower.Calls == 0, "A winning upper layer prevents unrelated lower HasPoint callbacks.");
        upper.Visible = false;
        try { root.PushInput(motion, true); throw new Exception("Expected HasPoint failure."); }
        catch (AggregateException) { }
        lower.Visible = false; root.PushInput(motion, true);
    }

    private sealed class ThrowingHit : Control
    {
        internal int Calls;
        protected override bool HasPoint(Vector2 point) { Calls++; throw new InvalidOperationException("HasPoint failure."); }
    }

    private static void Stationary()
    {
        var root = new SubViewport { Size = new(100, 100) };
        var control = new Control { Size = new(20, 20) };
        root.AddChild(control);
        using var tree = new SceneTree(root);
        var entered = 0; var exited = 0;
        control.MouseEntered += () => entered++;
        control.MouseExited += () => exited++;
        using var motion = new InputEventMouseMotion { Position = new(5, 5) };
        root.PushInput(motion, inLocalCoordinates: true);
        Check(entered == 1, "Initial pointer enters.");
        control.Position = new(30, 30); tree.ProcessFrame(0);
        Check(exited == 1, "Geometry changes release stationary hover next frame.");
        control.Position = Vector2.Zero; tree.ProcessFrame(0);
        Check(entered == 2, "Geometry changes restore stationary hover next frame.");
        control.Visible = false; tree.ProcessFrame(0);
        control.Visible = true; tree.ProcessFrame(0);
        Check(entered == 3 && exited == 2, "Hide/show retains the last pointer position.");
        control.Dispose(); tree.ProcessFrame(0);
        var replacement = new Control { Size = new(20, 20) };
        var replacementEntered = false;
        replacement.MouseEntered += () => replacementEntered = true;
        root.AddChild(replacement); tree.ProcessFrame(0);
        Check(replacementEntered, "Disposal retires hover and permits stationary entry into a replacement.");
        replacement.Visible = false;
        var reentrant = new Control { Size = new(20, 20) };
        var transitions = 0;
        reentrant.MouseEntered += () => { transitions++; reentrant.Visible = false; };
        root.AddChild(reentrant); tree.ProcessFrame(0);
        Check(transitions == 1, "A reentrant hide during hover entry settles without repeating entry.");
        tree.ProcessFrame(0);
        var failing = new Control { Size = new(20, 20) };
        failing.MouseEntered += () => throw new ApplicationException("hover entry failure");
        root.AddChild(failing);
        try { tree.ProcessFrame(0); throw new InvalidOperationException("Missing hover callback failure."); }
        catch (AggregateException) { }
        tree.ProcessFrame(0);
        failing.Dispose(); tree.ProcessFrame(0);
    }

    private static void CustomClip()
    {
        var viewport = new SubViewport { Size = new(100, 100) };
        var parent = new Shape { Position = new(20, 20), Size = new(40, 40), ClipContents = true, MouseFilter = MouseFilter.Ignore };
        var child = new Control { Size = new(40, 40) };
        viewport.AddChild(parent); parent.AddChild(child);
        using var tree = new SceneTree(viewport);
        var hits = 0;
        child.GUIInput += _ => hits++;
        using var outside = new InputEventMouseMotion { Position = new(25, 25) };
        using var inside = new InputEventMouseMotion { Position = new(35, 25) };
        viewport.PushInput(outside, inLocalCoordinates: true);
        Check(hits == 0, "A clipping HasPoint override excludes a child inside the rectangle but outside the shape.");
        viewport.PushInput(inside, inLocalCoordinates: true);
        Check(hits == 1 && parent.Local == new Vector2(15, 5), "Clipping uses local override coordinates even for an Ignore parent.");
        child.TopLevel = true;
        child.Position = new(20, 20);
        viewport.PushInput(outside, inLocalCoordinates: true);
        Check(hits == 2, "TopLevel ends custom clipping inheritance.");
    }

    private sealed class Shape : Control
    {
        internal Vector2 Local;
        protected override bool HasPoint(Vector2 point) { Local = point; return point.X >= 10 && base.HasPoint(point); }
    }

    private static void Order(Viewport viewport)
    {
        var outer = new SubViewport { Size = new(100, 100) };
        outer.AddChild(viewport);
        var first = new Control { Size = new(40, 40), ZIndex = 100 };
        var second = new Control { Size = new(40, 40), ZIndex = -100, ShowBehindParent = true };
        viewport.AddChild(first); viewport.AddChild(second);
        using var tree = new SceneTree(outer);
        var hits = new List<string>();
        first.GUIInput += _ => hits.Add("first");
        second.GUIInput += _ => hits.Add("second");
        void Expect(string expected)
        {
            hits.Clear();
            using var input = new InputEventMouseMotion { Position = new(5, 5) };
            viewport.PushInput(input, inLocalCoordinates: true);
            Check(string.Join(",", hits) == expected, $"Expected {expected}, received {string.Join(",", hits)}.");
        }
        Expect("second");
        first.MoveToFront(); Expect("first");
        first.ZIndex = -200; second.ZIndex = 200; Expect("first");
        first.MouseFilter = MouseFilter.Ignore; Expect("second");
        first.MouseFilter = MouseFilter.Stop;
        var independent = new Control { TopLevel = true, Size = new(40, 40), MouseFilter = MouseFilter.Pass };
        second.AddChild(independent); independent.GUIInput += _ => hits.Add("top");
        // The later GUI root wins even against children of an earlier root.
        var normal = new Control { Size = new(40, 40) };
        second.AddChild(normal); normal.GUIInput += _ => hits.Add("normal");
        Expect("first");
        second.MoveToFront(); Expect("top");
        independent.Visible = false; Expect("normal");
        independent.Visible = true;
        var layer = new CanvasLayer { Layer = 2 };
        viewport.AddChild(layer);
        var layered = new Control { Size = new(40, 40), ZIndex = -4096 };
        layer.AddChild(layered); layered.GUIInput += _ => hits.Add("layer");
        Expect("layer"); layer.Layer = -2; Expect("top");
        using var outside = new InputEventMouseMotion { Position = new(5, 5) };
        hits.Clear(); outer.PushInput(outside, inLocalCoordinates: true);
        Check(hits.Count == 0, "A viewport's controls cannot become another viewport's targets.");
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
