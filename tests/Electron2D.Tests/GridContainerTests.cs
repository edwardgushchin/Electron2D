using Electron2D;

internal static class GridContainerTests
{
    internal static void Run()
    {
        VerifyDefaultsAndMinimum();
        VerifyPlacementAndEligibility();
        VerifyExpansionAndBounds();
        VerifyMaximumPropagationAndOverflow();
        VerifySparseColumns();
        VerifyPackingAndGuards();
        VerifyCallbacksAndAllocations();
        Console.WriteLine("Grid columns, track bounds, RTL, sparse layout, packing, callbacks and allocation checks passed.");
    }

    private static void VerifyDefaultsAndMinimum()
    {
        using var grid = new GridContainer();
        Check(grid.Columns == 1 && grid.HSeparation == 4 && grid.VSeparation == 4 && grid.GetMinimumSize() == Vector2.Zero,
            "An empty grid has one column, four-pixel gaps and zero intrinsic minimum.");
        Check(grid.MouseFilter == MouseFilter.Pass && grid.PropagateMaximumSize, "Grid preserves container defaults.");
        Reject<ArgumentOutOfRangeException>(() => grid.Columns = 0);
        Reject<ArgumentOutOfRangeException>(() => grid.Columns = -1);
        Check(grid.Columns == 1, "Invalid columns leave configuration unchanged.");
        grid.Columns = 2;
        grid.AddChild(Cell("A", new(10.9f, 8.9f)));
        grid.AddChild(Cell("B", new(20.9f, 12.9f)));
        grid.AddChild(Cell("C", new(30.9f, 6.9f)));
        grid.AddChild(Cell("D", new(15.9f, 18.9f)));
        Check(grid.GetMinimumSize() == new Vector2(54, 34), "Track minimums truncate each bound minimum before row/column maxima and sums.");
        grid.HSeparation = -3; grid.VSeparation = -2;
        Check(grid.GetMinimumSize() == new Vector2(47, 28), "Signed gaps participate in intrinsic minimum without clamping.");
        grid.Hide();
        Check(grid.GetMinimumSize() == new Vector2(47, 28), "Intrinsic measurement uses locally visible children even when the grid is hidden.");
    }

    private static void VerifyPlacementAndEligibility()
    {
        var grid = new GridContainer { Columns = 2, Size = new(100, 80) };
        var a = Cell("A", new(10, 8)); var b = Cell("B", new(20, 12));
        var c = Cell("C", new(30, 6)); var d = Cell("D", new(15, 18));
        var hidden = Cell("Hidden", new(200, 200)); hidden.Hide(); hidden.Position = new(3, 5);
        var top = Cell("Top", new(300, 300)); top.TopLevel = true; top.Position = new(7, 9);
        grid.AddChild(a); grid.AddChild(new Node { Name = "NonControl" }); grid.AddChild(hidden);
        grid.AddChild(b); grid.AddChild(top); grid.AddChild(c); grid.AddChild(d);
        using var tree = new SceneTree(grid); tree.ProcessFrame(0);
        Check(grid.GetMinimumSize() == new Vector2(54, 34), "Hidden, top-level and non-control children do not create grid holes.");
        Check(a.Position == Vector2.Zero && a.Size == new Vector2(30, 12) && b.Position == new Vector2(34, 0) && b.Size == new Vector2(20, 12),
            "First row uses column widths and row height in child order.");
        Check(c.Position == new Vector2(0, 16) && d.Position == new Vector2(34, 16) && c.Size == new Vector2(30, 18),
            "Second row reuses the first row's column tracks.");
        Check(hidden.Position == new Vector2(3, 5) && top.Position == new Vector2(7, 9), "Ignored controls retain their rectangles.");
        b.Hide(); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(c.Position == new Vector2(19, 0) && d.Position == new Vector2(0, 12), "Hiding a child compacts all following cells in row-major order.");
        grid.MoveChild(d, 0); tree.ProcessFrame(0);
        Check(d.Position == Vector2.Zero && a.Position == new Vector2(34, 0) && c.Position == new Vector2(0, 22), "Child order changes rearrange cells.");
        grid.HSeparation = -3; grid.VSeparation = -2; tree.ProcessFrame(0);
        Check(a.Position == new Vector2(27, 0) && c.Position == new Vector2(0, 16), "Signed gaps deliberately overlap neighboring tracks.");
        c.TopLevel = true; tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(a.Position == new Vector2(12, 0) && grid.GetMinimumSize() == new Vector2(22, 18), "Making a child top-level automatically removes its track contribution.");
        c.TopLevel = false; tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(a.Position == new Vector2(27, 0) && c.Position == new Vector2(0, 16), "Returning a child to the parent canvas automatically restores row-major placement.");
    }

    private static void VerifyExpansionAndBounds()
    {
        var grid = new GridContainer { Columns = 2, Size = new(101, 85) };
        var a = Cell("A", new(10, 8), true); var b = Cell("B", new(20, 12), true);
        var c = Cell("C", new(30, 6), true); var d = Cell("D", new(15, 18), true);
        a.SizeFlagsStretchRatio = 100; b.SizeFlagsStretchRatio = 0; c.SizeFlagsStretchRatio = -3;
        grid.AddChild(a); grid.AddChild(b); grid.AddChild(c); grid.AddChild(d);
        using var tree = new SceneTree(grid); tree.ProcessFrame(0);
        Check(a.Size == new Vector2(49, 41) && b.Size == new Vector2(48, 41) && c.Size == new Vector2(49, 40) && d.Size == new Vector2(48, 40),
            "Expanded tracks share space uniformly, ignore child stretch ratios and receive leading remainder pixels.");
        Check(b.Position == new Vector2(53, 0) && c.Position == new Vector2(0, 45), "Track offsets include distributed remainder pixels.");
        b.SizeFlagsHorizontal = Control.SizeFlags.Fill; c.SizeFlagsVertical = Control.SizeFlags.Fill;
        tree.ProcessFrame(0);
        Check(b.Size.X == 48 && c.Size.Y == 40, "Any expanding cell expands its entire track, including ordinary fill peers.");
        grid.LayoutDirection = LayoutDirection.RTL; tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(a.Position.X == 52 && c.Position.X == 52 && b.Position.X == 0 && d.Position.X == 0,
            "RTL mirrors occupied columns while keeping remainder ownership and row order.");

        var minimumGrid = new GridContainer { Columns = 3, HSeparation = 0, Size = new(100, 10) };
        var large = Cell("Large", new(60, 10), true); var small = Cell("Small", new(10, 10), true); var last = Cell("Last", new(10, 10), true);
        minimumGrid.AddChild(large); minimumGrid.AddChild(small); minimumGrid.AddChild(last);
        using var minimumTree = new SceneTree(minimumGrid); minimumTree.ProcessFrame(0);
        Check(large.Size.X == 60 && small.Size.X == 20 && last.Size.X == 20 && last.Position.X == 80, "Min refit fixes the largest minimum before redistributing remaining space.");

        var maximumGrid = new GridContainer { Columns = 2, HSeparation = 0, VSeparation = 0, Size = new(100, 20) };
        var capped = Cell("Capped", new(10, 10), true); capped.CustomMaximumSize = new(20, -1);
        var free = Cell("Free", new(10, 10), true);
        var wider = Cell("Wider", new(10, 10), true); wider.CustomMaximumSize = new(30, -1);
        var freeLast = Cell("FreeLast", new(10, 10), true);
        maximumGrid.AddChild(capped); maximumGrid.AddChild(free); maximumGrid.AddChild(wider); maximumGrid.AddChild(freeLast);
        using var maximumTree = new SceneTree(maximumGrid); maximumTree.ProcessFrame(0);
        Check(capped.Size.X == 20 && wider.Size.X == 30 && free.Size.X == 70 && free.Position.X == 30,
            "A track uses the largest member maximum, while each fitted child retains its own cap.");

        var rows = new GridContainer { VSeparation = 0, Size = new(20, 100) };
        var shortRow = Cell("Short", new(10, 10), true); shortRow.CustomMaximumSize = new(-1, 20);
        var longRow = Cell("Long", new(10, 10), true); longRow.CustomMaximumSize = new(-1, 80);
        rows.AddChild(shortRow); rows.AddChild(longRow); using var rowTree = new SceneTree(rows); rowTree.ProcessFrame(0);
        Check(shortRow.Size.Y == 20 && longRow.Size.Y == 80 && longRow.Position.Y == 20,
            "A capped expanded row advances by its final height, preventing overlap with the next row.");

        var remainder = new GridContainer { Columns = 3, HSeparation = 1, Size = new(26, 5) };
        var fixedCell = Cell("Fixed", new(5, 5)); var expanding = Cell("Expanding", new(1, 5), true); var expandingLast = Cell("Last", new(1, 5), true);
        remainder.AddChild(fixedCell); remainder.AddChild(expanding); remainder.AddChild(expandingLast);
        using var remainderTree = new SceneTree(remainder); remainderTree.ProcessFrame(0);
        Check(expanding.Size.X == 10 && expandingLast.Size.X == 9 && expanding.Position.X == 6 && expandingLast.Position.X == 17,
            "Remainder distribution skips fixed tracks and starts at the first expanded track.");
    }

    private static void VerifyMaximumPropagationAndOverflow()
    {
        var root = new Node();
        var grid = new GridContainer { Name = "Grid", Columns = 2, Size = new(40, 30), CustomMaximumSize = new(40, 30) };
        var destination = new Control { Name = "Destination", PropagateMaximumSize = true, CustomMaximumSize = new(100, 100) };
        root.AddChild(grid); root.AddChild(destination);
        var a = Cell("A", new(10, 10), true); var b = Cell("B", new(10, 10), true);
        var c = Cell("C", new(10, 10), true); var d = Cell("D", new(10, 10), true);
        grid.AddChild(a); grid.AddChild(b); grid.AddChild(c); grid.AddChild(d);
        using var tree = new SceneTree(root); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(a.GetCombinedMaximumSize() == new Vector2(40, 30) && b.GetCombinedMaximumSize() == new Vector2(18, 30) &&
            c.GetCombinedMaximumSize() == new Vector2(40, 13) && d.GetCombinedMaximumSize() == new Vector2(18, 13),
            "Grid maximum propagation subtracts each cell's column and row offsets independently.");
        grid.RemoveChild(d); destination.AddChild(d);
        Check(d.GetCombinedMaximumSize() == new Vector2(100, 100), "Leaving a grid clears allocated maximum caches before another parent supplies bounds.");

        var bounded = new GridContainer { Size = new(20, 10), CustomMaximumSize = new(20, -1) };
        var constrained = Cell("Constrained", new(30, 10)); bounded.AddChild(constrained);
        using var boundedTree = new SceneTree(bounded); boundedTree.ProcessFrame(0); boundedTree.ProcessFrame(0);
        Check(constrained.Size.X == 20 && constrained.GetCombinedMaximumSize().X == 20, "A propagated maximum initially constrains a larger intrinsic minimum.");
        bounded.CustomMaximumSize = new(80, -1);
        for (var pass = 0; pass < 4; pass++) boundedTree.ProcessFrame(0);
        Check(bounded.GetMinimumSize().X == 30 && constrained.Size.X == 30 && constrained.GetCombinedMaximumSize().X == 80,
            "Raising a grid maximum invalidates old allocated child bounds and restores the intrinsic minimum without manual sorting or resizing.");
        for (var pass = 0; pass < 64; pass++)
        {
            bounded.CustomMaximumSize = new(pass % 2 == 0 ? 20 : 80, -1);
            for (var frame = 0; frame < 4; frame++) boundedTree.ProcessFrame(0);
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 64; pass++)
        {
            bounded.CustomMaximumSize = new(pass % 2 == 0 ? 20 : 80, -1);
            for (var frame = 0; frame < 4; frame++) boundedTree.ProcessFrame(0);
        }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed maximum invalidation and deferred grid refits allocate zero managed bytes.");

        var overflow = new GridContainer { Columns = 3, HSeparation = 0, Size = new(90, 10) };
        var first = Cell("First", new(1, 1), true); var middle = Cell("Middle", new(1, 1), true); var last = Cell("Last", new(1, 1), true);
        overflow.AddChild(first); overflow.AddChild(middle); overflow.AddChild(last);
        using var overflowTree = new SceneTree(overflow); overflowTree.ProcessFrame(0); overflowTree.ProcessFrame(0);
        var firstRect = new Rect2(first.Position, first.Size); var middleRect = new Rect2(middle.Position, middle.Size); var lastRect = new Rect2(last.Position, last.Size);
        var failure = Capture(() => overflow.HSeparation = int.MaxValue);
        Check(failure is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Any(error => error is OverflowException),
            "Overflowing theme separation fails during immediate theme reflow before deferred cell fitting.");
        Check(new Rect2(first.Position, first.Size) == firstRect && new Rect2(middle.Position, middle.Size) == middleRect && new Rect2(last.Position, last.Size) == lastRect,
            "A rejected overflowing layout leaves every previously fitted child rectangle unchanged.");
        overflow.HSeparation = 0; overflowTree.ProcessFrame(0);
    }

    private static void VerifySparseColumns()
    {
        var grid = new GridContainer { Columns = 4, Size = new(40, 10) };
        var child = Cell("Cell", Vector2.Zero, true); grid.AddChild(child);
        using var tree = new SceneTree(grid); tree.ProcessFrame(0);
        Check(child.Size.X == 10 && grid.GetMinimumSize() == Vector2.Zero, "Unused columns share expansion without adding visible separation or minimum width.");
        grid.Columns = int.MaxValue; grid.Size = new(100, 10); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(child.Size.X == 1 && grid.Columns == int.MaxValue, "Huge sparse column counts distribute the first remainder pixel without materializing empty tracks.");
        child.CustomMinimumSize = new(5, 2); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(child.Size.X == 5 && grid.GetMinimumSize() == new Vector2(5, 2), "Sparse expansion still refits occupied minimums.");
        grid.LayoutDirection = LayoutDirection.RTL; tree.ProcessFrame(0);
        Check(child.Position.X == 95, "Sparse RTL placement starts at the far edge of the container.");
    }

    private static void VerifyPackingAndGuards()
    {
        var grid = new GridContainer { Name = "Grid", Columns = 3, HSeparation = -2, VSeparation = 9 };
        var child = Cell("Cell", new(10, 12), true); grid.AddChild(child); child.Owner = grid;
        using var tree = new SceneTree(grid);
        using var packed = new PackedScene(); packed.Pack(grid); using var instance = packed.Instantiate();
        Check(instance.GetType() == typeof(GridContainer) && instance is GridContainer copy && copy.Columns == 3 && copy.HSeparation == -2 && copy.VSeparation == 9 && copy.GetChild(0) is Control copiedChild && copiedChild.SizeFlagsHorizontal == Control.SizeFlags.ExpandFill,
            "Packing preserves exact grid identity, configuration and inherited child size flags.");
        foreach (var name in new[] { nameof(GridContainer.Columns), nameof(GridContainer.HSeparation), nameof(GridContainer.VSeparation) })
        {
            var descriptor = grid.GetPropertyList().Single(property => property.Name == name);
            Check(descriptor is PropertyDescriptor<GridContainer, int> && grid.PropertyCanRevert(descriptor), "Grid stores one typed descriptor for each configuration property.");
            grid.RevertProperty(descriptor);
        }
        Check(grid.Columns == 1 && grid.HSeparation == 4 && grid.VSeparation == 4, "Descriptor reverts match constructor defaults.");
        Check(Task.Run(() => Capture(() => grid.Columns = 2)).Result is InvalidOperationException &&
            Task.Run(() => Capture(() => grid.HSeparation = 2)).Result is InvalidOperationException &&
            Task.Run(() => Capture(() => grid.VSeparation = 2)).Result is InvalidOperationException &&
            Task.Run(() => Capture(() => _ = grid.Columns)).Result is InvalidOperationException &&
            Task.Run(() => Capture(() => _ = grid.HSeparation)).Result is InvalidOperationException &&
            Task.Run(() => Capture(() => _ = grid.VSeparation)).Result is InvalidOperationException,
            "Grid configuration reads and writes require the scene owner thread.");
        var captureChecked = false;
        var probe = new CaptureProbe
        {
            Name = "Probe",
            DuringCapture = () =>
        {
            Reject<InvalidOperationException>(() => grid.Columns = 2);
            Reject<InvalidOperationException>(() => grid.HSeparation = -1);
            Reject<InvalidOperationException>(() => grid.VSeparation = -1);
            captureChecked = true;
        }
        };
        grid.AddChild(probe); probe.Owner = grid; packed.Pack(grid);
        Check(captureChecked && grid.Columns == 1 && grid.HSeparation == 4 && grid.VSeparation == 4, "Capture rejects all grid configuration mutation before changing state.");
        using var disposed = new GridContainer(); disposed.Dispose();
        Reject<ObjectDisposedException>(() => _ = disposed.Columns); Reject<ObjectDisposedException>(() => disposed.Columns = 2);
        Reject<ObjectDisposedException>(() => _ = disposed.HSeparation); Reject<ObjectDisposedException>(() => disposed.HSeparation = 0);
        Reject<ObjectDisposedException>(() => _ = disposed.VSeparation); Reject<ObjectDisposedException>(() => disposed.VSeparation = 0);
    }

    private static void VerifyCallbacksAndAllocations()
    {
        var grid = new GridContainer { Columns = 2, Size = new(100, 40) };
        var a = Cell("A", new(10, 10), true); var b = Cell("B", new(10, 10), true);
        grid.AddChild(a); grid.AddChild(b); using var tree = new SceneTree(grid); tree.ProcessFrame(0); tree.ProcessFrame(0);
        var sorts = 0; grid.SortChildren += () => sorts++;
        grid.Columns = 2; grid.HSeparation = 4; grid.VSeparation = 4; tree.ProcessFrame(0);
        Check(sorts == 0, "Equal configuration assignments do not schedule sorting.");
        Action fail = () => throw new ApplicationException("expected grid callback failure");
        a.Resized += fail; grid.Size = new(102, 40); Reject<AggregateException>(() => tree.ProcessFrame(0)); a.Resized -= fail;
        Check(b.Size.X == 49 && b.Position.X == 53 && sorts == 1, "A failing child callback does not prevent later child placement or the sort event.");
        grid.PreSortChildren += fail; grid.Size = new(104, 40); Reject<AggregateException>(() => tree.ProcessFrame(0)); grid.PreSortChildren -= fail;
        Check(a.Size.X == 50 && b.Size.X == 50 && sorts == 2, "Pre-sort failure still executes the grid arrangement and sort event.");

        var changed = false;
        Action mutate = () => { if (changed) return; changed = true; grid.Columns = 1; b.Hide(); };
        a.Resized += mutate; grid.Size = new(106, 42); tree.ProcessFrame(0); tree.ProcessFrame(0); a.Resized -= mutate;
        Check(changed && a.Size == new Vector2(106, 42) && grid.GetMinimumSize() == new Vector2(10, 10),
            "Configuration and child visibility changes during placement reach a fresh arrangement pass.");
        b.Show(); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(b.Position == new Vector2(0, 23) && b.Size == new Vector2(106, 19), "Restored child enters the next row with distributed height.");
        for (var pass = 0; pass < 64; pass++)
        {
            grid.Columns = pass % 2 + 1; grid.Size = pass % 2 == 0 ? new(100, 40) : new(102, 42); tree.ProcessFrame(0);
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 64; pass++)
        {
            grid.Columns = pass % 2 + 1; grid.Size = pass % 2 == 0 ? new(100, 40) : new(102, 42); tree.ProcessFrame(0);
        }
        var activeBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(activeBytes == 0, $"Prepared active grid resize/column cycles allocated {activeBytes} managed bytes.");
        tree.ProcessFrame(0); before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 64; pass++) tree.ProcessFrame(0);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared idle grid frames allocate zero managed bytes.");
    }

    private static Control Cell(string name, Vector2 minimum, bool expand = false) => new()
    {
        Name = name,
        CustomMinimumSize = minimum,
        SizeFlagsHorizontal = expand ? Control.SizeFlags.ExpandFill : Control.SizeFlags.Fill,
        SizeFlagsVertical = expand ? Control.SizeFlags.ExpandFill : Control.SizeFlags.Fill
    };
    private sealed class CaptureProbe : Node
    {
        private static readonly PropertyDescriptor<CaptureProbe, int> ProbeProperty = new("Probe", node => { node.DuringCapture?.Invoke(); return 0; }, (node, _) => node.EnsureMutable(), _ => 0, stored: true);
        internal Action? DuringCapture { get; init; }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(ProbeProperty);
        protected override Func<Node> CreateSceneInstanceFactory() => CreateProbe;
        private static Node CreateProbe() => new CaptureProbe();
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
