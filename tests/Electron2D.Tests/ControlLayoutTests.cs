using Electron2D;

internal static class ControlLayoutTests
{
    internal static void Run()
    {
        Check(typeof(Control).BaseType == typeof(CanvasItem), "Control is a canvas sibling of Entity.");
        var viewport = new TestViewport();
        var parent = new Control { Position = new(10, 20), Size = new(100, 80) };
        var child = new Control { Position = new(5, 6), Size = new(20, 10) };
        var sprite = new Sprite { Position = new(2, 3) };
        viewport.AddChild(parent); parent.AddChild(child); child.AddChild(sprite);
        child.Owner = parent; sprite.Owner = parent;
        child.SetAnchor(Side.Right, 1);
        child.SetOffset(Side.Right, -5);
        var order = new List<string>();
        child.ItemRectChanged += _ => { Check(child.Size.X == parent.Size.X - 10, "Rectangle is committed before callbacks."); order.Add("rect"); };
        child.Resized += () => order.Add("resized");
        using (var tree = new SceneTree(viewport))
        {
            Check(parent.GetParentAreaSize() == new Vector2(200, 150) && child.GetParentControl() == parent,
                "Viewport and direct parent provide layout areas.");
            Check(child.Position == new Vector2(5, 6) && child.Size == new Vector2(90, 10), "Anchors resolve on attachment.");
            Check(sprite.GlobalPosition == new Vector2(17, 29), "Spatial children inherit a control transform.");
            Reject<InvalidOperationException>(() => Task.Run(() => _ = child.GetTransform()).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(() => _ = sprite.GetTransform()).GetAwaiter().GetResult());
            order.Clear();
            parent.Size = new(140, 80);
            Check(child.Size == new Vector2(130, 10) && order.SequenceEqual(new[] { "rect", "resized" }),
                "Parent resize updates descendants and delivers ordered notifications.");
            parent.SetAnchor(Side.Right, 1);
            parent.SetOffset(Side.Right, -10);
            viewport.VisibleSize = new(240, 150);
            viewport.NotifySizeChanged();
            Check(parent.Size.X == 220 && child.Size.X == 210, "Viewport resize cascades through controls.");
            using var packed = new PackedScene(); packed.Pack(parent);
            using var copy = (Control)packed.Instantiate();
            var copiedChild = (Control)copy.GetChild(0);
            Check(copy.AnchorRight == 1 && copy.OffsetRight == -10 && copiedChild.AnchorRight == 1 && copiedChild.OffsetRight == -5,
                "Packed controls retain anchors and offsets, including nested controls.");
            child.PivotOffset = new(10, 0);
            child.RotationDegrees = 90;
            Near(child.GetTransform().Origin, new(15, -4));
            child.GlobalPosition = new(40, 50);
            Near(child.GlobalPosition, new(40, 50));
            child.Scale = Vector2.Zero;
            Check(child.Scale.X > 0 && child.Scale.Y > 0, "Zero scale has an invertible replacement.");
            Action fail = () => throw new InvalidOperationException("resized callback");
            child.Resized += fail;
            Reject<InvalidOperationException>(() => parent.Size = new(230, 80));
            Check(parent.Size.X == 230 && child.Size.X == 220, "A failed resize callback sees committed layout.");
            child.Resized -= fail;
            var movable = new Control { Name = "movable", Position = new(3, 4), Size = new(5, 6) };
            var destination = new Control { Name = "destination", Position = new(60, 30), Size = new(40, 30) };
            parent.AddChild(movable); viewport.AddChild(destination);
            var beforeMove = movable.GlobalPosition;
            movable.Reparent(destination);
            Near(movable.GlobalPosition, beforeMove);
            using var singular = new Entity { Transform = new Transform(Vector2.Zero, Vector2.Down, Vector2.Zero) };
            viewport.AddChild(singular);
            Reject<InvalidOperationException>(() => movable.Reparent(singular));
            Check(movable.Parent == destination, "Singular destination rejects before hierarchy mutation.");
            Reject<ArgumentOutOfRangeException>(() => child.SetAnchor((Side)10, 0));
            Reject<ArgumentOutOfRangeException>(() => child.SetOffset(Side.Left, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => child.Size = new(-1, 0));
            Reject<InvalidOperationException>(() => Task.Run(() => child.Position = Vector2.Zero).GetAwaiter().GetResult());
        }
        VerifyAnchorPresets();
        VerifyMinimumSize();
        Check(child.IsDisposed && parent.IsDisposed, "Tree disposal releases controls.");
        Console.WriteLine("Control layout checks passed.");
    }

    private static void VerifyAnchorPresets()
    {
        var expected = new (float Left, float Top, float Right, float Bottom)[]
        {
            (0, 0, 0, 0), (1, 0, 1, 0), (0, 1, 0, 1), (1, 1, 1, 1),
            (0, .5f, 0, .5f), (.5f, 0, .5f, 0), (1, .5f, 1, .5f), (.5f, 1, .5f, 1),
            (.5f, .5f, .5f, .5f), (0, 0, 0, 1), (0, 0, 1, 0), (1, 0, 1, 1),
            (0, 1, 1, 1), (.5f, 0, .5f, 1), (0, .5f, 1, .5f), (0, 0, 1, 1)
        };
        using var viewport = new TestViewport();
        var parent = new Control { Size = new(120, 80) };
        viewport.AddChild(parent);
        using var tree = new SceneTree(viewport);
        for (var index = 0; index < expected.Length; index++)
        {
            var item = new Control { Name = $"preset{index}", Position = new(12, 18), Size = new(20, 10) };
            parent.AddChild(item);
            item.SetAnchorsPreset((ControlLayoutPreset)index);
            var anchors = expected[index];
            Check((item.AnchorLeft, item.AnchorTop, item.AnchorRight, item.AnchorBottom) == anchors,
                $"Preset {index} must use the pinned four-anchor arrangement.");
            Check(item.Position == new Vector2(12, 18) && item.Size == new Vector2(20, 10),
                $"Preset {index} must preserve the current rectangle by default.");
        }

        var stretch = new Control { Name = "stretch", Position = new(12, 18), Size = new(20, 10) };
        parent.AddChild(stretch);
        stretch.SetAnchorsPreset(ControlLayoutPreset.FullRect, keepOffsets: true);
        Check(stretch.OffsetLeft == 12 && stretch.OffsetTop == 18 && stretch.OffsetRight == 32 && stretch.OffsetBottom == 28,
            "Keep-offset mode must preserve all four offsets.");
        Check(stretch.Position == new Vector2(12, 18) && stretch.Size == new Vector2(140, 90),
            "Full-rectangle anchors must resolve against the live parent area.");
        parent.Size = new(150, 110);
        Check(stretch.Size == new Vector2(170, 120), "Preset anchors must follow a later parent resize.");
        stretch.SetAnchorsPreset(ControlLayoutPreset.TopLeft);
        Check(stretch.Position == new Vector2(12, 18) && stretch.Size == new Vector2(170, 120),
            "Changing an existing preset must preserve the current rectangle by default.");
        var before = (stretch.AnchorLeft, stretch.AnchorTop, stretch.AnchorRight, stretch.AnchorBottom);
        Reject<ArgumentOutOfRangeException>(() => stretch.SetAnchorsPreset((ControlLayoutPreset)16));
        Check(before == (stretch.AnchorLeft, stretch.AnchorTop, stretch.AnchorRight, stretch.AnchorBottom),
            "An invalid preset must not mutate any anchor.");
    }

    private static void VerifyMinimumSize()
    {
        using var viewport = new TestViewport();
        var host = new Control { Name = "host", Size = new(100, 80) };
        viewport.AddChild(host);
        var child = new MinimumControl { Name = "minimum", Position = new(10, 12), Size = new(20, 10) };
        host.AddChild(child);
        using var tree = new SceneTree(viewport);
        Check(child.GetMinimumSize() == Vector2.Zero && child.GetCombinedMinimumSize() == Vector2.Zero,
            "A base control has no intrinsic or custom minimum size.");
        var events = new List<string>();
        child.Resized += () => events.Add("resized");
        child.MinimumSizeChanged += () => events.Add("minimum");
        child.CustomMinimumSize = new(40, 30);
        Check(child.GetCombinedMinimumSize() == new Vector2(40, 30) && child.Size == new Vector2(20, 10),
            "The new bound is queryable before its deferred layout update.");
        tree.FlushDeferred();
        Check(child.Position == new Vector2(10, 12) && child.Size == new Vector2(40, 30),
            "Default growth keeps the leading edges and enlarges the rectangle.");
        Check(events.SequenceEqual(new[] { "resized", "minimum" }),
            "Resize notifications precede minimum-size change delivery.");
        events.Clear();
        child.CustomMinimumSize = new(50, 35);
        child.CustomMinimumSize = new(60, 40);
        tree.FlushDeferred();
        Check(child.Size == new Vector2(60, 40) && events.SequenceEqual(new[] { "resized", "minimum" }),
            "Multiple changes coalesce to the latest minimum and one signal.");
        events.Clear();
        child.CustomMinimumSize = new(60, 40);
        child.UpdateMinimumSize();
        tree.FlushDeferred();
        Check(events.Count == 0, "An unchanged minimum does not raise another signal.");

        child.SetIntrinsicMinimum(new(75, 15));
        Check(child.GetMinimumSize() == new Vector2(75, 15) && child.GetCombinedMinimumSize() == new Vector2(75, 40),
            "Intrinsic and custom minima combine independently per component.");
        tree.FlushDeferred();
        Check(child.Size == new Vector2(75, 40) && events.SequenceEqual(new[] { "resized", "minimum" }),
            "A derived control can invalidate its intrinsic minimum.");
        events.Clear();
        child.Visible = false;
        child.CustomMinimumSize = new(90, 50);
        tree.FlushDeferred();
        Check(child.Size == new Vector2(75, 40) && events.Count == 0,
            "A hidden control defers minimum-size layout and notifications.");
        child.Visible = true;
        tree.FlushDeferred();
        Check(child.Size == new Vector2(90, 50) && events.SequenceEqual(new[] { "resized", "minimum" }),
            "Showing the control applies its pending minimum-size change.");
        Reject<ArgumentOutOfRangeException>(() => child.CustomMinimumSize = new(float.NaN, 1));
        Check(child.CustomMinimumSize == new Vector2(90, 50), "Invalid custom minima preserve the prior value.");
        Reject<ArgumentOutOfRangeException>(() => child.GrowHorizontal = (ControlGrowDirection)3);
        Check(child.GrowHorizontal == ControlGrowDirection.End, "Invalid growth modes preserve the prior value.");

        var shifts = new[] { 1f, 0f, .5f };
        for (var index = 0; index < shifts.Length; index++)
        {
            var direction = (ControlGrowDirection)index;
            var item = new Control { Name = $"grow{index}", Position = new(10, 12), Size = new(20, 10) };
            host.AddChild(item);
            item.GrowHorizontal = direction;
            item.GrowVertical = direction;
            item.CustomMinimumSize = new(40, 30);
            tree.FlushDeferred();
            Check(item.Size == new Vector2(40, 30) && item.Position == new Vector2(10 - 20 * shifts[index], 12 - 20 * shifts[index]),
                $"Growth direction {direction} must expand from the selected edges.");
        }

        var packable = new Control { Name = "packable", CustomMinimumSize = new(25, 15), GrowHorizontal = ControlGrowDirection.Both };
        host.AddChild(packable);
        using var packed = new PackedScene();
        packed.Pack(packable);
        using var copy = (Control)packed.Instantiate();
        Check(copy.CustomMinimumSize == new Vector2(25, 15) && copy.GrowHorizontal == ControlGrowDirection.Both,
            "Minimum size and growth policy survive packed-scene storage.");

        var entering = new Control { Name = "entering", Size = new(10, 10) };
        entering.SetAnchor(Side.Right, 1);
        entering.SetOffset(Side.Right, -10);
        entering.Resized += () => entering.CustomMinimumSize = new(120, 20);
        host.AddChild(entering);
        tree.FlushDeferred();
        Check(entering.Size == new Vector2(120, 20),
            "A minimum-size change during entry resize must survive the initial layout snapshot.");
    }

    private sealed class MinimumControl : Control
    {
        private Vector2 _minimum;
        protected override Vector2 OnGetMinimumSize() => _minimum;
        internal void SetIntrinsicMinimum(Vector2 value) { _minimum = value; UpdateMinimumSize(); }
    }

    private sealed class TestViewport : Viewport
    {
        internal Vector2 VisibleSize = new(200, 150);
        public override Rect GetVisibleRect() => new(Vector2.Zero, VisibleSize);
    }

    private static void Near(Vector2 actual, Vector2 expected) => Check(actual.IsEqualApprox(expected), $"Expected {expected}, got {actual}.");
    private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
