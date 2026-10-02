using Electron2D;

internal static class FlowContainerTests
{
    internal static void Run()
    {
        VerifyWrapping(); VerifyAlignments(); VerifyExpansion(); VerifyDirections(); VerifyPackingAndCallbacks(); VerifyEdges(); VerifyAllocations();
        Console.WriteLine("Flow wrapping, relative alignment, expansion, RTL/reverse, lifecycle, packing, guards and zero-allocation checks passed.");
    }
    private static Control Child(string name, Vector2 minimum, bool expand = false) => new()
    { Name = name, CustomMinimumSize = minimum, SizeFlagsHorizontal = expand ? Control.SizeFlags.ExpandFill : Control.SizeFlags.Fill };
    private static (FlowContainer Flow, Control A, Control B, Control C) Fixture(bool vertical = false)
    {
        var flow = new FlowContainer { Vertical = vertical, Size = vertical ? new(40, 40) : new(40, 40), HSeparation = 4, VSeparation = 3 };
        var a = Child("A", vertical ? new(10, 10) : new(10, 10));
        var b = Child("B", vertical ? new(12, 20) : new(20, 12));
        var c = Child("C", vertical ? new(8, 15) : new(15, 8));
        flow.AddChild(a); flow.AddChild(b); flow.AddChild(c); return (flow, a, b, c);
    }
    private static void VerifyWrapping()
    {
        using var defaults = new FlowContainer();
        Check(!defaults.Vertical && !defaults.ReverseFill && defaults.Alignment == AlignmentMode.Begin && defaults.LastWrapAlignment == FlowContainer.LastWrapAlignmentMode.Inherit && defaults.HSeparation == 4 && defaults.VSeparation == 4 && defaults.GetLineCount() == 0, "Flow defaults match the reference surface.");
        var (flow, a, b, c) = Fixture(); using var tree = new SceneTree(flow); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(flow.GetLineCount() == 2 && a.Position == Vector2.Zero && b.Position == new Vector2(14, 0) && c.Position == new Vector2(0, 15), "Horizontal flow wraps before the third child and preserves separation.");
        Check(a.Size == new Vector2(10, 12) && b.Size == new Vector2(20, 12) && flow.GetMinimumSize() == new Vector2(20, 23), "Cross fill and cached minimum extent follow line heights.");
        c.Hide(); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(flow.GetLineCount() == 1 && flow.GetMinimumSize() == new Vector2(20, 12), "Hidden children do not leave phantom lines.");
        c.Show(); c.TopLevel = true; tree.ProcessFrame(0); Check(flow.GetLineCount() == 1, "Top-level children are excluded.");
        c.TopLevel = false; tree.ProcessFrame(0); tree.ProcessFrame(0);
        flow.MoveChild(c, 0); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(c.Position == Vector2.Zero && a.Position == new Vector2(19, 0) && b.Position.Y == 13, "Changed child order updates wrap grouping.");
        flow.Size = new(80, 40); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(flow.GetLineCount() == 1, "Resize merges previously separate wraps.");
        using var empty = new FlowContainer(); using var emptyTree = new SceneTree(empty); emptyTree.ProcessFrame(0);
        Check(empty.GetLineCount() == 1 && empty.GetMinimumSize() == Vector2.Zero, "Empty visible sorting retains the reference's single implicit line.");
    }
    private static void VerifyAlignments()
    {
        var (flow, a, b, c) = Fixture(); using var tree = new SceneTree(flow); tree.ProcessFrame(0);
        var expected = new int[,] { { 0, 0, 9, 19 }, { 12, 3, 12, 22 }, { 25, 6, 15, 25 } };
        for (var alignment = 0; alignment < 3; alignment++)
            for (var last = 0; last < 4; last++)
            {
                flow.Alignment = (AlignmentMode)alignment; flow.LastWrapAlignment = (FlowContainer.LastWrapAlignmentMode)last;
                Check(a.Position.X == alignment * 3 && c.Position.X == expected[alignment, last], $"Relative last-line alignment profile {alignment}/{last}.");
            }
        flow.LastWrapAlignment = FlowContainer.LastWrapAlignmentMode.End; flow.Alignment = AlignmentMode.Begin;
        c.CustomMinimumSize = new(25, 8); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(c.Position.X == 0, "A final line classed filled does not use relative alignment.");
    }
    private static void VerifyExpansion()
    {
        var flow = new HFlowContainer { Size = new(60, 20), HSeparation = 0 };
        var a = Child("A", new(10, 8), true); var b = Child("B", new(10, 12), true);
        a.CustomMaximumSize = new(20, -1); b.SizeFlagsStretchRatio = 3;
        flow.AddChild(a); flow.AddChild(b); using var tree = new SceneTree(flow); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(a.Size.X == 20 && b.Size.X == 40 && b.Position.X == 20, "Capped weighted expansion redistributes remaining pixels.");
        a.SizeFlagsStretchRatio = 0; tree.ProcessFrame(0);
        Check(a.Size.X == 10 && b.Size.X == 50, "A zero weight receives no extra pixels.");
        a.SizeFlagsStretchRatio = -1; tree.ProcessFrame(0);
        Check(a.Size.X == 10 && b.Size.X == 50, "A signed nonpositive weight keeps the minimum without overallocating another child.");
        a.SizeFlagsStretchRatio = b.SizeFlagsStretchRatio = 1; a.CustomMaximumSize = new(-1, -1); flow.Size = new(31, 20); tree.ProcessFrame(0);
        Check(a.Size.X == 15 && b.Size.X == 15 && b.Position.X == 15, "Per-child truncation leaves one residual pixel rather than distributing it.");
        flow.Alignment = AlignmentMode.End; Check(a.Position.X == 1 && b.Position.X == 16, "Residual truncated pixels obey alignment.");
    }
    private static void VerifyDirections()
    {
        var (h, a, b, c) = Fixture(); using var tree = new SceneTree(h); tree.ProcessFrame(0);
        h.LayoutDirection = LayoutDirection.RTL; tree.ProcessFrame(0);
        Check(a.Position.X == 30 && b.Position.X == 6 && c.Position.X == 25, "RTL mirrors row allocations without reversing child groups.");
        h.ReverseFill = true; Check(a.Position.Y == 28 && c.Position.Y == 17, "Horizontal reverse fills rows upward.");
        var (v, va, vb, vc) = Fixture(true); using var verticalTree = new SceneTree(v); verticalTree.ProcessFrame(0);
        Check(va.Position == Vector2.Zero && vb.Position == new Vector2(0, 13) && vc.Position == new Vector2(16, 0), "Vertical flow wraps to another column.");
        v.LayoutDirection = LayoutDirection.RTL; verticalTree.ProcessFrame(0);
        Check(va.Position.X == 28 && vc.Position.X == 16, "Vertical RTL reverses columns.");
        v.ReverseFill = true; Check(va.Position.X == 0 && vc.Position.X == 16, "Vertical reverse combines with RTL as exclusive-or.");
        using var fixedH = new HFlowContainer(); using var fixedV = new VFlowContainer();
        Reject<InvalidOperationException>(() => fixedH.Vertical = false); Reject<InvalidOperationException>(() => fixedV.Vertical = true);
        Check(!fixedH.GetPropertyList().Any(p => p.Name == nameof(FlowContainer.Vertical)) && fixedV.Vertical, "Fixed subclasses omit writable orientation descriptors.");
    }
    private static void VerifyPackingAndCallbacks()
    {
        var (flow, a, b, c) = Fixture(); flow.Name = "Flow"; a.Owner = b.Owner = c.Owner = flow;
        using var tree = new SceneTree(flow); tree.ProcessFrame(0);
        using var packed = new PackedScene(); packed.Pack(flow); using var copy = packed.Instantiate();
        Check(copy is FlowContainer f && !f.Vertical && f.HSeparation == 4 && f.VSeparation == 3 && f.ChildCount == 3, "Packing restores exact generic flow identity and typed state.");
        foreach (var control in new FlowContainer[] { new HFlowContainer(), new VFlowContainer() })
        {
            using (control) { control.ReverseFill = true; packed.Pack(control); using var fixedCopy = packed.Instantiate(); Check(fixedCopy.GetType() == control.GetType() && ((FlowContainer)fixedCopy).ReverseFill, "Packing preserves fixed subclass identity."); }
        }
        Reject<ArgumentOutOfRangeException>(() => flow.Alignment = (AlignmentMode)3);
        Reject<ArgumentOutOfRangeException>(() => flow.LastWrapAlignment = (FlowContainer.LastWrapAlignmentMode)4);
        Check(Task.Run(() => Capture(() => _ = flow.Alignment)).Result is InvalidOperationException && Task.Run(() => Capture(() => flow.ReverseFill = true)).Result is InvalidOperationException, "Attached configuration is owner-thread affine.");
        a.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; tree.ProcessFrame(0);
        Action fail = () => throw new ApplicationException("expected flow child failure");
        a.Resized += fail; flow.Size = new(80, 50); Reject<AggregateException>(() => tree.ProcessFrame(0)); a.Resized -= fail;
        Check(c.Position.X == 65 && flow.GetLineCount() == 1, "Failed child resize does not prevent later placements or cache publication.");
        var changed = false; Action mutate = () => { if (changed) return; changed = true; flow.ReverseFill = true; };
        a.Resized += mutate; flow.Size = new(90, 52); flow.Alignment = AlignmentMode.End; a.Resized -= mutate;
        Check(changed && a.Position.Y == 40, "Immediate layout requests from callbacks settle without recursion.");
        using var disposed = new FlowContainer(); disposed.Dispose(); Reject<ObjectDisposedException>(() => disposed.GetLineCount()); Reject<ObjectDisposedException>(() => disposed.HSeparation = 0);
    }
    private static void VerifyAllocations()
    {
        var (flow, _, _, _) = Fixture(); using var tree = new SceneTree(flow); tree.ProcessFrame(0);
        for (var i = 0; i < 64; i++) { flow.Size = new(i % 2 == 0 ? 40 : 80, 40); tree.ProcessFrame(0); tree.ProcessFrame(0); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { flow.Size = new(i % 2 == 0 ? 40 : 80, 40); tree.ProcessFrame(0); tree.ProcessFrame(0); }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, $"Prepared wrap/merge cycles allocate {allocated} managed bytes.");
        before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) tree.ProcessFrame(0);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared idle flow frames allocate no managed memory.");
    }
    private static void VerifyEdges()
    {
        var (flow, a, b, c) = Fixture(); using var tree = new SceneTree(flow); tree.ProcessFrame(0);
        var beforeA = new Rect2(a.Position, a.Size); var beforeB = new Rect2(b.Position, b.Size);
        flow.HSeparation = int.MaxValue;
        Reject<AggregateException>(() => tree.ProcessFrame(0));
        Check(new Rect2(a.Position, a.Size) == beforeA && new Rect2(b.Position, b.Size) == beforeB, "Overflow preflight preserves all fitted rectangles.");
        flow.HSeparation = 4; tree.ProcessFrame(0);
        flow.PropagateMaximumSize = true; flow.CustomMaximumSize = new(80, 40); tree.ProcessFrame(0); tree.ProcessFrame(0);
        flow.CustomMaximumSize = new(20, 40); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(flow.GetLineCount() == 3 && b.Size.X <= 20, "Propagated bounds shrink eligible child minima and wrap groups.");
        flow.CustomMaximumSize = new(80, 40); flow.Size = new(80, 40); tree.ProcessFrame(0); tree.ProcessFrame(0);
        Check(flow.GetLineCount() == 1 && b.Size.X == 20, "Maximum growth releases old layout bounds.");
        using var oldTree = new SceneTree(new Node()); using var newTree = new SceneTree(new Node());
        var movable = new FlowContainer(); movable.AddChild(Child("Child", new(10, 10))); oldTree.Root.AddChild(movable); oldTree.Root.RemoveChild(movable); newTree.Root.AddChild(movable);
        oldTree.FlushDeferred(); Check(movable.GetLineCount() == 0, "Old membership sort cannot consume the new layout.");
        newTree.FlushDeferred(); Check(movable.GetLineCount() == 1, "Current membership retains its own pending sort.");
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception e) { return e; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
