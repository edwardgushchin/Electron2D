using Electron2D;

internal static class UndoRedoTests
{
    private static readonly PropertyDescriptor<ColorRect, Color> Fill = new("Color", n => n.Color, (n, v) => n.Color = v);
    internal static void Run()
    {
        OrderingAndMetadata(); Merging(); References(); RetirementErrorsAndScale(); ErrorsAndGuards(); SceneAndResourceAuthoring(); WarmPlayback();
        Console.WriteLine("UndoRedo: typed ordering/metadata, nested/record-only actions, 800ms merges/forced operations, conditional aliases/retirement, failures/reentry/owner guards, scene/resource persistence and 128 warm Undo/Redo cycles=0 managed bytes passed.");
    }
    private static void OrderingAndMetadata()
    {
        using var history = new UndoRedo(); var log = new List<int>(); var notifications = 0; history.VersionChanged += () => notifications++;
        Check(history.GetVersion() == 1 && history.GetCurrentAction() == -1 && history.GetCurrentActionName() == "" && history.GetHistoryCount() == 0 && !history.Undo() && !history.Redo(), "Empty history contract.");
        Reject<InvalidOperationException>(() => history.AddDoMethod(() => { }));
        history.CreateAction("ordered"); history.AddDoMethod(() => log.Add(1)); history.AddDoMethod(() => log.Add(2)); history.AddUndoMethod(() => log.Add(3)); history.AddUndoMethod(() => log.Add(4));
        Reject<InvalidOperationException>(() => history.GetHistoryCount()); history.CommitAction(); history.Undo(); history.Redo();
        Check(log.SequenceEqual([1, 2, 3, 4, 1, 2]) && history.GetVersion() == 2 && notifications == 3 && history.GetActionName(0) == "ordered", "Registration order and version transitions.");
        history.CreateAction("reverse", backwardUndoOps: true); history.AddUndoMethod(() => log.Add(5)); history.AddUndoMethod(() => log.Add(6)); history.CreateAction("nested"); history.AddDoMethod(() => log.Add(7)); history.CommitAction(); Check(log[^1] == 2, "Inner commit stays pending."); history.CommitAction(false); history.Undo(); Check(log.TakeLast(2).SequenceEqual([6, 5]), "Backward undo and execute=false.");
        history.Redo(); Check(log[^1] == 7 && history.GetCurrentActionName() == "reverse", "Redo executes recorded do once, using outer name.");
        history.MaxSteps = 1; history.CreateAction("trim"); history.CommitAction(); Check(history.GetHistoryCount() == 1 && history.GetCurrentAction() == 0 && history.GetActionName(0) == "trim", "Positive MaxSteps trim after commit.");
        history.ClearHistory(false); var version = history.GetVersion(); var events = notifications; history.ClearHistory(); Check(history.GetVersion() == version + 1 && notifications == events + 1, "Clear optional version/event behavior.");
        Reject<ArgumentOutOfRangeException>(() => history.GetActionName(-1)); Reject<ArgumentOutOfRangeException>(() => history.CreateAction("bad", (UndoRedo.MergeMode)3));
    }
    private static void Merging()
    {
        long clock = 1000; using var history = new UndoRedo(() => clock); using var color = new ColorRect(); var forced = 0; var forcedUndo = 0;
        history.CreateAction("paint", UndoRedo.MergeMode.Ends); history.StartForceKeepInMergeEnds(); history.AddDoMethod(() => forced++); history.AddUndoMethod(() => forcedUndo++); history.EndForceKeepInMergeEnds(); history.AddDoProperty(color, Fill, Colors.Red); history.AddUndoProperty(color, Fill, Colors.White); history.CommitAction();
        clock += 799; history.CreateAction("paint", UndoRedo.MergeMode.Ends); history.AddDoProperty(color, Fill, Colors.Blue); history.AddUndoProperty(color, Fill, Colors.Red); history.StartForceKeepInMergeEnds(); history.AddUndoMethod(() => forcedUndo += 10); history.EndForceKeepInMergeEnds(); history.CommitAction();
        Check(history.GetHistoryCount() == 1 && history.GetVersion() == 2 && color.Color == Colors.Blue && forced == 2, "Ends latest do/oldest undo and forced do survive.");
        history.Undo(); Check(color.Color == Colors.White && forcedUndo == 11, "Ends retained undo plus explicitly forced newer undo."); history.Redo(); Check(color.Color == Colors.Blue && forced == 3, "Merged redo includes forced earlier do.");
        clock += 800; history.CreateAction("paint", UndoRedo.MergeMode.Ends); history.CommitAction(); Check(history.GetHistoryCount() == 2, "Exactly 800ms does not merge.");
        clock += 1; history.CreateAction("paint", UndoRedo.MergeMode.All, true); history.CommitAction(); Check(history.GetHistoryCount() == 3, "Different backward policy prevents merge.");
        using var all = new UndoRedo(() => clock); var total = 0;
        all.CreateAction("sum", UndoRedo.MergeMode.All); all.AddDoMethod(() => total += 1); all.AddUndoMethod(() => total -= 1); all.CommitAction();
        clock++; all.CreateAction("sum", UndoRedo.MergeMode.All); all.AddDoMethod(() => total += 2); all.AddUndoMethod(() => total -= 2); all.CommitAction();
        Check(total == 3 && all.GetHistoryCount() == 1 && all.GetVersion() == 2, "All merge only executes new do suffix."); all.Undo(); Check(total == 0, "All undo includes both edits."); all.Redo(); Check(total == 3, "All redo includes complete do list.");
        long wrap = long.MaxValue - 5; using var wrapped = new UndoRedo(() => wrap); wrapped.CreateAction("wrap"); wrapped.CommitAction(); wrap = long.MinValue + 9; wrapped.CreateAction("wrap", UndoRedo.MergeMode.All); wrapped.CommitAction(); Check(wrapped.GetHistoryCount() == 1, "Monotonic signed counter wrap uses unsigned elapsed arithmetic.");
    }
    private sealed class Owned : ElectronObject
    {
        internal int Cleanups;
        protected override void Dispose(bool disposing) { if (disposing) Cleanups++; base.Dispose(disposing); }
    }
    private static void References()
    {
        using var history = new UndoRedo(); var created = new Owned(); var removed = new Owned();
        history.CreateAction("add"); history.AddDoReference(created); history.AddDoReference(created); history.CommitAction(false); history.Undo(); history.CreateAction("branch"); Check(created.IsDisposed && created.Cleanups == 1 && !history.HasRedo(), "Discarded redo disposes aliased do-owned object exactly once."); history.CommitAction();
        history.CreateAction("delete"); history.AddUndoReference(removed); history.CommitAction(false); history.ClearHistory(false); Check(removed.IsDisposed && removed.Cleanups == 1, "Applied retirement disposes undo-owned objects.");
        var live = new Owned(); history.CreateAction("keep applied"); history.AddDoReference(live); history.CommitAction(false); history.ClearHistory(false); Check(!live.IsDisposed, "Applied do references transfer retention back to caller."); live.Dispose();
        var restored = new Owned(); history.CreateAction("keep restored"); history.AddUndoReference(restored); history.CommitAction(false); history.Undo(); history.ClearHistory(false); Check(!restored.IsDisposed, "Unapplied undo references release without disposal."); restored.Dispose();
        long clock = 0; using var merged = new UndoRedo(() => clock); var shared = new Owned(); merged.CreateAction("ref", UndoRedo.MergeMode.Ends); merged.AddDoReference(shared); merged.CommitAction(false); clock++; merged.CreateAction("ref", UndoRedo.MergeMode.Ends); merged.AddDoReference(shared); merged.CommitAction(false); Check(!shared.IsDisposed, "Replacing ownership claim retains another registered alias."); merged.Undo(); merged.ClearHistory(false); Check(shared.Cleanups == 1, "Final alias matching-side retirement disposes.");
        var pendingDo = new Owned(); var pendingUndo = new Owned(); var pending = new UndoRedo(); pending.CreateAction("pending"); pending.AddDoReference(pendingDo); pending.AddUndoReference(pendingUndo); pending.Dispose(); Check(pendingDo.IsDisposed && !pendingUndo.IsDisposed, "Unexecuted pending action ownership."); pendingUndo.Dispose();
        using var resource = new Gradient(); history.CreateAction("resources"); Reject<ArgumentException>(() => history.AddDoReference(resource)); Reject<ArgumentException>(() => history.AddUndoReference(resource)); history.CommitAction(false); Check(!resource.IsDisposed, "Resources never transfer history ownership.");
    }
    private sealed class FailingOwned : ElectronObject
    {
        internal int Attempts;
        protected override void Dispose(bool disposing) { if (disposing) { Attempts++; throw new InvalidOperationException("cleanup"); } base.Dispose(disposing); }
    }
    private static void RetirementErrorsAndScale()
    {
        using var history = new UndoRedo(); var bad = new FailingOwned(); var good = new Owned();
        history.CreateAction("owned"); history.AddUndoReference(bad); history.AddUndoReference(good); history.CommitAction(false);
        Reject<AggregateException>(() => history.ClearHistory()); Check(good.IsDisposed && bad.Attempts == 1 && history.GetHistoryCount() == 0 && history.GetCurrentAction() == -1, "Cleanup failures attempt every owner and publish empty state.");
        var log = new List<int>(); var one = new Owned(); var two = new Owned(); one.Disposed += _ => log.Add(1); two.Disposed += _ => log.Add(2);
        history.CreateAction("one"); history.AddDoReference(one); history.CommitAction(false); history.CreateAction("two"); history.AddDoReference(two); history.CommitAction(false); history.Undo(); history.Undo(); history.CreateAction("branch"); history.CommitAction(false); Check(log.SequenceEqual([1, 2]), "Redo ownership retires in original action order.");
        history.ClearHistory(false); for (var i = 0; i < 4096; i++) { history.CreateAction("bulk"); history.CommitAction(false); }
        history.MaxSteps = 1; history.CreateAction("limit"); history.CommitAction(false); Check(history.GetHistoryCount() == 1 && history.GetCurrentAction() == 0, "Large prefix trims in one storage movement."); history.ClearHistory(false);
        long tick = 0; var live = new Owned(); var pending = new UndoRedo(() => tick); pending.CreateAction("merge"); pending.AddDoReference(live); pending.CommitAction(false); tick++; pending.CreateAction("merge", UndoRedo.MergeMode.Ends); pending.Dispose(); Check(!live.IsDisposed, "Disposing unfinished merge preserves already-applied do-owned object."); live.Dispose();
    }
    private static void ErrorsAndGuards()
    {
        using var history = new UndoRedo(); using var color = new ColorRect(); var reached = 0; var notified = 0;
        history.VersionChanged += () => throw new InvalidOperationException("observer"); history.VersionChanged += () => notified++;
        history.CreateAction("failure"); history.AddDoMethod(() => throw new ArgumentException("operation")); history.AddDoMethod(() => reached++); history.AddUndoMethod(() => reached--);
        var errors = Reject<AggregateException>(() => history.CommitAction()); Check(errors.InnerExceptions.Count == 2 && reached == 1 && notified == 1 && history.GetCurrentAction() == 0 && history.GetVersion() == 2 && !history.IsCommittingAction(), "Failure continuation and final state publication.");
        Reject<AggregateException>(() => history.Undo()); Check(reached == 0 && history.GetCurrentAction() == -1 && history.GetVersion() == 1, "Undo still moves cursor after observer failure.");
        using var guarded = new UndoRedo(); var checkedGuards = 0;
        guarded.CreateAction("guards"); guarded.AddDoMethod(() => { Check(guarded.IsCommittingAction(), "Commit flag."); Reject<InvalidOperationException>(() => guarded.Undo()); Reject<InvalidOperationException>(() => guarded.CreateAction("nested dispatch")); Reject<InvalidOperationException>(guarded.Dispose); Reject<InvalidOperationException>(() => guarded.MaxSteps = 2); checkedGuards++; }); guarded.CommitAction(); Check(checkedGuards == 1 && !guarded.IsDisposed, "History guard rejects destructive callback reentry.");
        Reject<InvalidOperationException>(() => Task.Run(() => guarded.HasUndo()).GetAwaiter().GetResult()); Reject<InvalidOperationException>(() => Task.Run(guarded.Dispose).GetAwaiter().GetResult());
        using var target = new ColorRect(); guarded.CreateAction("disposed target"); guarded.AddDoProperty(target, Fill, Colors.Red); target.Dispose(); guarded.CommitAction();
        using var readOnly = new UndoRedo(); readOnly.CreateAction("readonly"); var descriptor = new PropertyDescriptor<ColorRect, Color>("ReadOnly", c => c.Color); Reject<InvalidOperationException>(() => readOnly.AddDoProperty(color, descriptor, Colors.Blue)); readOnly.CommitAction(false);
        using var validates = new UndoRedo(); var invalid = new PropertyDescriptor<ColorRect, Color>("Guard", c => c.Color, (c, v) => c.Color = v, validator: (_, v) => v.R >= 0); validates.CreateAction("validation"); validates.AddDoProperty(color, invalid, new Color(-1, 0, 0)); Check(color.Color == Colors.White, "Registration does not set."); Reject<AggregateException>(() => validates.CommitAction()); Check(color.Color == Colors.White && validates.HasUndo(), "Playback validator failure does not mutate target.");
    }
    private static void SceneAndResourceAuthoring()
    {
        var root = new Node(); var color = new ColorRect { Name = "Paint", Color = Colors.White }; root.AddChild(color); color.Owner = root; using var tree = new SceneTree(root); using var history = new UndoRedo(); using var gradient = new Gradient();
        var offsets = new PropertyDescriptor<Gradient, float[]>("Offsets", g => g.Offsets, (g, v) => g.Offsets = v);
        history.CreateAction("author"); history.AddDoProperty(color, Fill, Colors.Red); history.AddUndoProperty(color, Fill, Colors.White); history.AddDoProperty(gradient, offsets, new[] { 0f, .25f, 1f }); history.AddUndoProperty(gradient, offsets, new[] { 0f, 1f }); history.CommitAction();
        Check(color.Color == Colors.Red && gradient.GetPointCount() == 3, "Real scene and resource edit."); history.Undo(); Check(color.Color == Colors.White && gradient.GetPointCount() == 2, "Real scene and resource undo."); history.Redo(); using var packed = new PackedScene(); packed.Pack(root); using var copy = (Node)packed.Instantiate(); Check(((ColorRect)copy.GetNode("Paint")).Color == Colors.Red, "Replayed state becomes authored scene data.");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-undo-" + Guid.NewGuid() + ".e2dscene"); try { ResourceSaver.Save(packed, path); using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var restored = loaded.Instantiate(); Check(((ColorRect)restored.GetNode("Paint")).Color == Colors.Red, "Replayed typed state persists to actual file."); } finally { File.Delete(path); }
    }
    private static void WarmPlayback()
    {
        using var history = new UndoRedo(); using var color = new ColorRect(); var notifications = 0; history.VersionChanged += () => notifications++;
        history.CreateAction("warm"); history.AddDoProperty(color, Fill, Colors.Red); history.AddUndoProperty(color, Fill, Colors.White); history.CommitAction(); for (var i = 0; i < 128; i++) { history.Undo(); history.Redo(); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) { history.Undo(); history.Redo(); }
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before; Check(bytes == 0 && color.Color == Colors.Red && notifications == 513, "128 warm typed playback and event cycles bytes=" + bytes);
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static T Reject<T>(Action action) where T : Exception { try { action(); } catch (T e) { return e; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
