using Electron2D;

internal static class PanelContainerTests
{
    internal static void Run()
    {
        VerifyGeometryAndBounds();
        VerifyCallbacks();
        VerifyContainerThemeFailure();
        VerifyReentrantThemeCacheAndDeferredBulk();
        VerifyPackingAndDefaults();
        VerifyDrawingOrder();
        Console.WriteLine("Panel defaults, content layout, eligibility, maximum propagation, callbacks and packing passed.");
    }

    private static void VerifyGeometryAndBounds()
    {
        using var style = new StyleBoxEmpty { ContentMarginLeft = 2, ContentMarginTop = 3, ContentMarginRight = 4, ContentMarginBottom = 5 };
        using var panel = new Panel(); panel.AddThemeStyleBoxOverride("panel", style);
        Check(panel.MouseFilter == MouseFilter.Stop && panel.GetMinimumSize() == Vector2.Zero, "Panel draws its style without adopting its margins as an intrinsic minimum.");
        var container = new FlagProbe { Size = new(50, 40) }; container.AddThemeStyleBoxOverride("panel", style);
        var a = new Control { Name = "A", CustomMinimumSize = new(10, 8) };
        var b = new Control { Name = "B", CustomMinimumSize = new(6, 4), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        var hidden = new Control { Name = "Hidden", Visible = false, CustomMinimumSize = new(1000, 1000), Position = new(7, 9) };
        var top = new Control { Name = "Top", TopLevel = true, CustomMinimumSize = new(1000, 1000), Position = new(11, 13) };
        container.AddChild(a); container.AddChild(new Node { Name = "NonControl" }); container.AddChild(b); container.AddChild(hidden); container.AddChild(top);
        Check(container.GetMinimumSize() == new Vector2(16, 16), "Panel minimum combines per-axis child maxima and style margins without hidden/top-level contributions.");
        Check(container.MouseFilter == MouseFilter.Stop && container.PropagateMaximumSize && !container.Horizontal().Contains(Control.SizeFlags.Expand) && !container.Vertical().Contains(Control.SizeFlags.Expand), "Panel container overrides pointer defaults and does not advertise expansion flags.");
        using var tree = new SceneTree(container); Flush(tree);
        Check(a.Position == new Vector2(2, 3) && a.Size == new Vector2(44, 32), "Filled content receives the complete shared inner rectangle.");
        Check(b.Position == new Vector2(21, 17) && b.Size == new Vector2(6, 4), "Shrink-center content aligns inside the same inner rectangle.");
        Check(hidden.Position == new Vector2(7, 9) && top.Position == new Vector2(11, 13), "Excluded controls retain their rectangles.");
        container.Hide(); Check(container.GetMinimumSize() == new Vector2(16, 16), "A hidden ancestor does not erase locally visible child minimums."); container.Show(); Flush(tree);
        a.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; a.SizeFlagsStretchRatio = 100; Flush(tree);
        Check(a.Size.X == 44 && b.Size.X == 6, "Stored expansion flags and ratios do not divide the shared content rectangle.");
        container.CustomMaximumSize = new(20, 18); Flush(tree);
        Check(a.Size == new Vector2(14, 10) && a.GetCombinedMaximumSize() == new Vector2(14, 10), "Propagated child maximum excludes the panel's style margins.");
        container.CustomMaximumSize = new(4, 5); Flush(tree);
        Check(a.Size == Vector2.Zero && b.Size == Vector2.Zero && a.GetCombinedMaximumSize() == Vector2.Zero, "A panel smaller than its margins gives finite zero-sized child bounds.");
        container.CustomMaximumSize = new(20, 18);
        Check(container.GetMinimumSize() == new Vector2(16, 16), "Relaxing the parent maximum immediately refreshes bound minimum queries."); Flush(tree);
        container.PropagateMaximumSize = false; container.CustomMaximumSize = new(4, 5); Flush(tree);
        Check(a.Size == new Vector2(10, 8) && a.GetCombinedMaximumSize() == new Vector2(-1, -1), "Disabling propagation preserves child minima even when the content allocation is zero.");
        container.PropagateMaximumSize = true; container.CustomMaximumSize = new(-1, -1); container.Size = new(50, 40); Flush(tree);
        style.SetContentMarginAll(6); Flush(tree);
        Check(a.Position == new Vector2(6, 6) && a.Size == new Vector2(38, 28) && container.GetMinimumSize() == new Vector2(22, 20), "Borrowed style mutation updates content offset, size and minimum through theme invalidation.");
        Check(Task.Run(() => Capture(() => container.GetMinimumSize())).Result is InvalidOperationException, "Attached panel queries retain owner-thread guards.");
    }

    private static void VerifyCallbacks()
    {
        using var style = new StyleBoxEmpty(); style.SetContentMarginAll(2);
        using var replacement = new StyleBoxEmpty(); replacement.SetContentMarginAll(6);
        var container = new PanelContainer { Size = new(40, 30) }; container.AddThemeStyleBoxOverride("panel", style);
        var a = new Control { Name = "A" }; var b = new Control { Name = "B" }; container.AddChild(a); container.AddChild(b);
        using var tree = new SceneTree(container); Flush(tree);
        Action fail = () => throw new ApplicationException("expected panel child resize failure"); a.Resized += fail;
        container.Size = new(50, 40); var error = Capture(() => tree.ProcessFrame(0)); a.Resized -= fail;
        Check(error is AggregateException && b.Size == new Vector2(46, 36), "A failed child fit preserves committed geometry and attempts later children."); Flush(tree);
        Action remove = () => container.RemoveChild(b); a.Resized += remove;
        container.Size = new(60, 40); Flush(tree); a.Resized -= remove;
        Check(b.Parent is null && !b.IsDisposed && a.Size == new Vector2(56, 36), "Removing a captured sibling during layout skips it without invalidating the current snapshot."); b.Dispose();
        Action themeFail = () => throw new ApplicationException("expected panel theme observer failure"); container.ThemeChanged += themeFail;
        error = Capture(() => container.AddThemeStyleBoxOverride("panel", replacement)); container.ThemeChanged -= themeFail;
        Check(error is not null, "A theme observer failure propagates after its override is committed."); Flush(tree);
        Check(a.Position == new Vector2(6, 6) && a.Size == new Vector2(48, 28), "Panel layout is still queued when the inherited theme-change callback fails.");
    }

    private static void VerifyContainerThemeFailure()
    {
        var root = new Node(); var box = new HBoxContainer { Name = "Box", Size = new(100, 20) }; var grid = new GridContainer { Name = "Grid", Columns = 2, Size = new(100, 20) };
        var boxFirst = new Control { Name = "A", CustomMinimumSize = new(10, 10) }; var boxSecond = new Control { Name = "B", CustomMinimumSize = new(10, 10) };
        var gridFirst = new Control { Name = "A", CustomMinimumSize = new(10, 10) }; var gridSecond = new Control { Name = "B", CustomMinimumSize = new(10, 10) };
        box.AddChild(boxFirst); box.AddChild(boxSecond); grid.AddChild(gridFirst); grid.AddChild(gridSecond); root.AddChild(box); root.AddChild(grid);
        using var tree = new SceneTree(root); Flush(tree);
        Check(boxSecond.Position.X == 14 && gridSecond.Position.X == 14, "Container theme-failure regression starts from the default four-pixel separation.");
        Action fail = () => throw new ApplicationException("expected container theme observer failure"); box.ThemeChanged += fail; grid.ThemeChanged += fail;
        var boxError = Capture(() => box.AddThemeConstantOverride("separation", 7));
        var gridError = Capture(() => grid.AddThemeConstantOverride("h_separation", 9));
        box.ThemeChanged -= fail; grid.ThemeChanged -= fail; Flush(tree);
        Check(boxError is not null && gridError is not null && boxSecond.Position.X == 17 && gridSecond.Position.X == 19,
            "Changed theme separation rearranges fixed-size box and grid children even when a theme observer throws.");
    }

    private static void VerifyReentrantThemeCacheAndDeferredBulk()
    {
        using var original = new ReentrantTheme(); original.SetColor("tint", "Control", Colors.Red);
        using var replacement = new Theme(); replacement.SetColor("tint", "Control", Colors.Green);
        using var style = new StyleBoxEmpty();
        var control = new Control { Theme = original }; control.AddThemeStyleBoxOverride("panel", style);
        using var tree = new SceneTree(control); Flush(tree);
        original.ReadAction = () => control.Theme = replacement;
        Check(control.GetThemeColor("tint") == Colors.Red && ReferenceEquals(control.Theme, replacement) && control.GetThemeColor("tint") == Colors.Green,
            "A reentrant theme replacement cannot put the old result back into the invalidated cache.");
        var changes = 0; control.ThemeChanged += () => changes++;
        control.BeginBulkThemeOverride();
        Task.Run(() => style.SetContentMarginAll(3)).GetAwaiter().GetResult(); tree.FlushDeferred();
        Check(changes == 0, "A deferred background override-resource change still honors bulk suppression when delivered.");
        control.EndBulkThemeOverride(); Check(changes == 1, "Ending the bulk override emits its single required notification.");
    }

    private static void VerifyPackingAndDefaults()
    {
        using var theme = new Theme(); using var style = new StyleBoxEmpty(); style.SetContentMarginAll(3);
        theme.SetStyleBox("panel", "PanelContainer", style);
        using var source = new PanelContainer { Name = "Container", Theme = theme, Size = new(40, 30) };
        var child = new Panel { Name = "Panel" }; source.AddChild(child); child.Owner = source;
        using var packed = new PackedScene(); packed.Pack(source);
        using var instance = (PanelContainer)packed.Instantiate();
        Check(instance.GetType() == typeof(PanelContainer) && instance.GetChild(0).GetType() == typeof(Panel) && instance.MouseFilter == MouseFilter.Stop && ((Panel)instance.GetChild(0)).MouseFilter == MouseFilter.Stop,
            "Packed panels preserve both exact runtime types and stopping pointer defaults.");
        Check(instance.GetMinimumSize() == new Vector2(6, 6), "Packed theme resources retain panel content-margin behavior.");
        var mouse = instance.GetPropertyList().Single(property => property.Name == nameof(Control.MouseFilter));
        Check(!instance.PropertyCanRevert(mouse), "PanelContainer descriptor default agrees with its constructor.");
        instance.MouseFilter = MouseFilter.Pass; instance.RevertProperty(mouse); Check(instance.MouseFilter == MouseFilter.Stop, "PanelContainer mouse filter reverts to Stop rather than Container.Pass.");
        instance.Dispose(); Reject<ObjectDisposedException>(() => instance.GetMinimumSize());
    }

    private static void VerifyDrawingOrder()
    {
        var order = new List<string>(); using var style = new DrawingStyle(order);
        using var panel = new DrawingPanel(order); panel.AddThemeStyleBoxOverride("panel", style); panel.Draw += _ => order.Add("event");
        panel.PrepareCanvas(); Check(order.SequenceEqual(new[] { "style", "event", "custom" }), "Panel decoration precedes the draw event and a custom OnDraw override without requiring a base call.");
        order.Clear(); using var container = new DrawingContainer(order); container.AddThemeStyleBoxOverride("panel", style); container.Draw += _ => order.Add("event");
        container.PrepareCanvas(); Check(order.SequenceEqual(new[] { "style", "event", "custom" }), "PanelContainer decoration also precedes custom draw callbacks.");
    }

    private static void Flush(SceneTree tree) { tree.ProcessFrame(0); tree.ProcessFrame(0); tree.ProcessFrame(0); }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class FlagProbe : PanelContainer
    {
        internal Control.SizeFlags[] Horizontal() => GetAllowedSizeFlagsHorizontal();
        internal Control.SizeFlags[] Vertical() => GetAllowedSizeFlagsVertical();
    }
    private sealed class DrawingStyle(List<string> order) : StyleBox
    {
        protected override void OnDraw(CanvasItem canvasItem, Rect2 rect) => order.Add("style");
    }
    private sealed class DrawingPanel(List<string> order) : Panel
    {
        protected override void OnDraw() => order.Add("custom");
    }
    private sealed class DrawingContainer(List<string> order) : PanelContainer
    {
        protected override void OnDraw() => order.Add("custom");
    }
    private sealed class ReentrantTheme : Theme
    {
        internal Action? ReadAction;
        public override Color GetColor(string name, string themeType)
        {
            var result = base.GetColor(name, themeType); var action = ReadAction; ReadAction = null; action?.Invoke(); return result;
        }
    }
}
