using Electron2D;

internal static class BoxContainerTests
{
    internal static void Run()
    {
        var root = new Node(); var box = new HBoxContainer { Name = "Box", Size = new(100, 40) }; root.AddChild(box);
        var a = new Control { Name = "A", CustomMinimumSize = new(10, 8), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var b = new Control { Name = "B", CustomMinimumSize = new(20, 12), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsStretchRatio = 3 };
        box.AddChild(a); box.AddChild(b); using var tree = new SceneTree(root); tree.ProcessFrame(0);
        Check(a.Size == new Vector2(24, 40) && b.Size == new Vector2(72, 40) && b.Position == new Vector2(28, 0), "Weighted horizontal expansion includes minima and spacing.");
        Check(box.GetMinimumSize() == new Vector2(34, 12) && box.PropagateMaximumSize && box.MouseFilter == MouseFilter.Pass, "Container defaults and minimum sums.");
        a.CustomMaximumSize = new(15, -1); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(a.Size.X == 15 && b.Size.X == 81, "Capped child redistributes surplus.");
        a.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter; b.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd; box.Alignment = AlignmentMode.Center;
        tree.ProcessFrame(0); Check(a.Position.X == 33 && b.Position.X == 47, "Group center alignment with fixed minima.");
        box.LayoutDirection = LayoutDirection.RTL; tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(b.Position.X == 33 && a.Position.X == 57, "RTL reverses horizontal child ordering.");
        a.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter; b.SizeFlagsVertical = Control.SizeFlags.ShrinkEnd;
        box.FitChildInRect(a, new(0, 0, 50, 40)); box.FitChildInRect(b, new(0, 0, 50, 40));
        Check(a.Position.Y == 16 && b.Position.Y == 28, "Cross-axis center/end shrink.");
        a.Rotation = .5f; a.Scale = new(2, 3); box.FitChildInRect(a, new(0, 0, 50, 40)); Check(a.Rotation == 0 && a.Scale == Vector2.One, "Fit resets child visual transform.");
        b.Hide(); tree.ProcessFrame(0); Check(box.GetMinimumSize() == a.GetBoundMinimumSize().Ceil(), "Hidden children stop minimum participation."); b.Show();
        using var outsider = new Control(); Reject<ArgumentException>(() => box.FitChildInRect(outsider, new(0, 0, 10, 10)));
        Reject<InvalidOperationException>(() => box.Vertical = false);
        Reject<ArgumentOutOfRangeException>(() => box.Alignment = (AlignmentMode)3);
        a.SizeFlagsHorizontal = (Control.SizeFlags)32; Check((int)a.SizeFlagsHorizontal == 32, "Unrecognized source flag bits retain value.");
        Reject<ArgumentOutOfRangeException>(() => a.SizeFlagsStretchRatio = float.NaN);
        Check(Task.Run(() => Capture(() => a.SizeFlagsHorizontal = Control.SizeFlags.Fill)).Result is InvalidOperationException, "Flag owner guard.");
        var spacer = box.AddSpacer(true); Check(ReferenceEquals(box.GetChild(0), spacer) && spacer.SizeFlagsHorizontal == Control.SizeFlags.ExpandFill, "Spacer is an actual weighted child.");
        box.RemoveChild(spacer); spacer.Dispose();
        var endSpacer = box.AddSpacer(false); var collision = new Control { Name = $"Spacer{box.ChildCount + 1}" }; box.AddChild(collision);
        var anotherSpacer = box.AddSpacer(false); Check(anotherSpacer.Name != collision.Name, "Spacer names avoid existing siblings.");
        box.RemoveChild(endSpacer); endSpacer.Dispose(); box.RemoveChild(collision); collision.Dispose(); box.RemoveChild(anotherSpacer); anotherSpacer.Dispose();
        a.SetAnchorsPreset(LayoutPreset.FullRect); box.FitChildInRect(a, new(0, 0, 50, 40));
        Check(a.AnchorLeft == 0 && a.AnchorTop == 0 && a.AnchorRight == 0 && a.AnchorBottom == 0, "Container fit resets all anchors in one rectangle operation.");
        var flags = 0; a.SizeFlagsChanged += () => flags++; a.SizeFlagsVertical = Control.SizeFlags.Fill; a.SizeFlagsVertical = Control.SizeFlags.Fill; Check(flags == 1, "Flag equal assignments are silent.");
        var range = new TextureProgressBar { Name = "Progress" }; root.AddChild(range); Check(range.SizeFlagsVertical == Control.SizeFlags.Fill, "Progress inherited default restored.");
        using var bare = new RangeProbe(); Check(bare.SizeFlagsVertical == Control.SizeFlags.ShrinkBegin, "Range inherited default restored.");
        VerifyCallbacksPackingAndWarmSort();
        VerifyDeferredBatchReuse();
        VerifyMembershipSort();
        VerifyBoundsAndWeights();
        Console.WriteLine("Container fit, box allocation, flags, RTL, callbacks, packing and allocation checks passed.");
    }
    private static void VerifyCallbacksPackingAndWarmSort()
    {
        var root = new Node(); var box = new VBoxContainer { Name = "Box", Size = new(40, 100) }; root.AddChild(box);
        var a = new Control { Name = "A", CustomMinimumSize = new(5, 10), SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var b = new Control { Name = "B", CustomMinimumSize = new(6, 10), SizeFlagsVertical = Control.SizeFlags.ExpandFill }; box.AddChild(a); box.AddChild(b);
        using var tree = new SceneTree(root); var pre = 0; var sort = 0; box.PreSortChildren += () => pre++; box.SortChildren += () => sort++;
        tree.ProcessFrame(0); Check(a.Size == new Vector2(40, 48) && b.Position.Y == 52 && pre == 1 && sort == 1, "Deferred vertical layout and phase events.");
        box.QueueSort(); box.QueueSort(); tree.ProcessFrame(0); Check(pre == 2 && sort == 2, "Sort requests coalesce.");
        Action failure = () => throw new ApplicationException("expected"); box.PreSortChildren += failure; box.QueueSort(); Reject<AggregateException>(() => tree.ProcessFrame(0)); box.PreSortChildren -= failure;
        Check(sort == 3, "Pre-sort failure continues arrangement and sort event.");
        root.RemoveChild(box); box.QueueSort(); root.AddChild(box); tree.ProcessFrame(0); Check(sort == 4, "Detach/reentry resumes membership sorting.");
        a.Owner = root; b.Owner = root; box.Owner = root; using var packed = new PackedScene(); packed.Pack(root); using var copy = packed.Instantiate();
        Check(copy.GetNodeOrNull("Box") is VBoxContainer clone && clone.Vertical && clone.GetChild(0) is Control child && child.SizeFlagsVertical == Control.SizeFlags.ExpandFill, "Exact fixed orientation and flags pack.");
        var reentered = false; Action reentrant = () => { if (reentered) return; reentered = true; box.Alignment = AlignmentMode.End; };
        a.Resized += reentrant; box.Size = new(42, 102); tree.ProcessFrame(0); a.Resized -= reentrant;
        Check(reentered, "Child resize can synchronously request another alignment pass without corrupting scratch slots.");
        for (var pass = 0; pass < 64; pass++) { box.Size = pass % 2 == 0 ? new(40, 100) : new(42, 102); tree.ProcessFrame(0); }
        var before = GC.GetAllocatedBytesForCurrentThread(); long resize = 0, process = 0;
        for (var pass = 0; pass < 64; pass++) { var phase = GC.GetAllocatedBytesForCurrentThread(); box.Size = pass % 2 == 0 ? new(40, 100) : new(42, 102); resize += GC.GetAllocatedBytesForCurrentThread() - phase; phase = GC.GetAllocatedBytesForCurrentThread(); tree.ProcessFrame(0); process += GC.GetAllocatedBytesForCurrentThread() - phase; }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, $"Warmed resize/deferred layout allocated {allocated} managed bytes (resize {resize}, process {process}).");
    }
    private static void VerifyDeferredBatchReuse()
    {
        using var tree = new SceneTree(new Node()); var first = 0; var second = 0;
        Action later = () => second++; Action early = () => { first++; tree.Defer(later); };
        tree.Defer(early); tree.FlushDeferred(); Check(first == 1 && second == 0, "Captured batch excludes work scheduled by a callback.");
        tree.FlushDeferred(); Check(second == 1, "Reentrant work executes in the next captured batch.");
        Task.Run(() => tree.Defer(later)).Wait(); tree.FlushDeferred(); Check(second == 2, "Cross-thread enqueue remains synchronized.");
        for (var pass = 0; pass < 64; pass++) { tree.Defer(early); tree.FlushDeferred(); tree.FlushDeferred(); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 64; pass++) { tree.Defer(early); tree.FlushDeferred(); tree.FlushDeferred(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared captured batches reuse queue storage without allocation.");
    }
    private static void VerifyMembershipSort()
    {
        var root = new Node(); var box = new HBoxContainer { Name = "Box" }; root.AddChild(box);
        using var oldTree = new SceneTree(root); using var newTree = new SceneTree(new Node());
        var sorts = 0; box.SortChildren += () => sorts++;
        root.RemoveChild(box); newTree.Root.AddChild(box);
        oldTree.FlushDeferred(); Check(sorts == 0, "Stale captured sort cannot run in a new tree membership.");
        newTree.FlushDeferred(); Check(sorts == 1, "Current membership retains its queued sort after stale delivery.");
    }
    private static void VerifyBoundsAndWeights()
    {
        var box = new HBoxContainer { Size = new(31.9f, 10), Separation = 1 };
        var a = new Control { Name = "A", CustomMinimumSize = new(1.2f, 2.1f), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var b = new Control { Name = "B", CustomMinimumSize = new(2.2f, 3.1f), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.AddChild(a); box.AddChild(b); using var tree = new SceneTree(box); tree.ProcessFrame(0);
        Check(box.GetMinimumSize() == new Vector2(6, 4) && a.Size.X == 15 && b.Size.X == 15, "Fractional minima ceil and primary size floors to pixels.");
        a.SizeFlagsStretchRatio = -1; b.SizeFlagsStretchRatio = 3; tree.ProcessFrame(0);
        Check(a.Size.X == 2 && b.Size.X == 28, "Negative weight refits to minimum while positive peer consumes surplus.");
        a.SizeFlagsStretchRatio = 0; b.SizeFlagsStretchRatio = 0; tree.ProcessFrame(0);
        Check(a.Size.X == 2 && b.Size.X == 28, "Zero total skips refit while final expanding child consumes the source boundary remainder.");
        b.TopLevel = true; box.QueueSort(); tree.ProcessFrame(0); Check(box.GetMinimumSize() == new Vector2(2, 3), "Top-level controls do not participate in box minimum or arrangement."); b.TopLevel = false;
        a.SizeFlagsHorizontal = Control.SizeFlags.Fill; b.SizeFlagsHorizontal = Control.SizeFlags.Fill; box.Separation = -2; tree.ProcessFrame(0);
        Check(box.GetMinimumSize().X == 3 && b.Position.X == 0, "Signed separation permits overlap.");
        a.CustomMinimumSize = new(10, 3); a.GrowHorizontal = GrowDirection.Begin;
        box.FitChildInRect(a, new(4, 0, 2, 10)); Check(a.Position.X == -4 && a.Size.X == 10, "Fill allocations below minimum apply inherited growth in one rectangle reflow.");
        box.CustomMaximumSize = new(12, -1); a.CustomMinimumSize = new(8, 3); b.CustomMinimumSize = new(8, 3); box.Separation = 4; tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(a.GetCombinedMaximumSize().X == 12 && b.GetCombinedMaximumSize().X == 0 && b.Size.X == 0, "Remaining primary maximum propagates through later children.");
        box.RemoveChild(b); Check(b.GetCombinedMaximumSize().X == -1, "Removal clears allocated parent maximum cache."); b.Dispose();
        a.SetAnchorsPreset(LayoutPreset.FullRect); var previousPosition = a.Position;
        Reject<ArgumentException>(() => box.FitChildInRect(a, new(float.MaxValue, 0, float.MaxValue, 10)));
        Check(a.AnchorRight == 1 && a.AnchorBottom == 1 && a.Position == previousPosition, "Invalid overflowing fit leaves anchors and rectangle intact.");
        using var range = new RangeProbe(); using var progress = new TextureProgressBar();
        var rangeFlag = range.GetPropertyList().Single(property => property.Name == nameof(Control.SizeFlagsVertical));
        var progressFlag = progress.GetPropertyList().Single(property => property.Name == nameof(Control.SizeFlagsVertical));
        Check(!range.PropertyCanRevert(rangeFlag) && !progress.PropertyCanRevert(progressFlag), "Initial inherited flag values equal their stored defaults.");
        range.SizeFlagsVertical = Control.SizeFlags.Expand; progress.SizeFlagsVertical = Control.SizeFlags.Expand;
        range.RevertProperty(rangeFlag); progress.RevertProperty(progressFlag);
        Check(range.SizeFlagsVertical == Control.SizeFlags.ShrinkBegin && progress.SizeFlagsVertical == Control.SizeFlags.Fill, "Inherited stored flag defaults agree with constructors.");
    }
    private sealed class RangeProbe : Electron2D.Range { }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
