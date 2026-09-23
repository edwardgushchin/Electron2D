using Electron2D;

internal static class CanvasTransformNotificationTests
{
    internal static void Run()
    {
        Delivery(); BoundariesAndFailures(); FramePhases();
        using var custom = new Placement(); using var child = new Entity { Position = Vector2.One }; custom.AddChild(child);
        Check(child.GetGlobalTransform().Origin == Vector2.One && child.GetGlobalTransform().Origin == Vector2.One && custom.Queries == 1, "Global queries reuse the resolved parent transform.");
        custom.Move(new(2, 3)); Check(child.GetGlobalTransform().Origin == new Vector2(3, 4) && custom.Queries == 2, "Custom placement invalidation reaches descendants while detached.");
        Console.WriteLine("Canvas transform invalidation, queued delivery, force, reentry and frame phases passed.");
    }

    private static void Delivery()
    {
        var root = new Node(); var parent = new Entity(); var child = new Entity(); root.AddChild(parent); parent.AddChild(child);
        var local = 0; var global = new List<CanvasItem>();
        parent.LocalTransformChanged += _ => local++; parent.TransformChanged += n => global.Add(n); child.TransformChanged += n => global.Add(n);
        Check(!parent.NotifyLocalTransformChanges && !parent.NotifyTransformChanges, "Policies default off.");
        parent.NotifyLocalTransformChanges = true; parent.NotifyTransformChanges = true; child.NotifyTransformChanges = true;
        parent.Position = Vector2.One; Check(local == 0 && global.Count == 0, "Detached changes do not notify.");
        Reject<InvalidOperationException>(parent.ForceUpdateTransform);
        using var tree = new SceneTree(root);
        Check(global.Count == 0, "Entry is queued.");
        var initial = new Entity { Name = "Initial" }; root.AddChild(initial); var initialCalls = 0; initial.TransformChanged += _ => initialCalls++;
        initial.ForceUpdateTransform(); initial.Position = Vector2.One; initial.ForceUpdateTransform();
        Check(initialCalls == 1, "Entry queues once even with global notifications disabled; later changes do not.");
        Check(parent.GetPropertyList().All(p => p.Name is not (nameof(CanvasItem.NotifyTransformChanges) or nameof(CanvasItem.NotifyLocalTransformChanges))), "Notification policies are not stored scene properties.");
        parent.ForceUpdateTransform(); parent.ForceUpdateTransform();
        Check(global.SequenceEqual(new[] { parent }), "Force consumes only this item's entry, once.");
        child.ForceUpdateTransform(); global.Clear();
        child.GetGlobalTransform();
        parent.Position = new(2, 3); parent.Position = new(3, 4); parent.Position = parent.Position;
        Check(local == 3 && global.Count == 0, "Equal local assignments notify synchronously; globals coalesce.");
        parent.NotifyTransformChanges = false; parent.ForceUpdateTransform(); child.ForceUpdateTransform();
        Check(global.SequenceEqual(new[] { parent, child }), "Disabling does not cancel a queued notification.");
        global.Clear(); parent.Position = new(4, 5); parent.ForceUpdateTransform(); child.ForceUpdateTransform();
        Check(global.Count == 0, "Consuming a notification does not resolve global invalidation.");
        parent.NotifyTransformChanges = true; parent.Position = new(5, 6); parent.ForceUpdateTransform();
        Check(global.SequenceEqual(new[] { parent }), "Enabling resolves invalidation so a later change can queue.");
        global.Clear(); parent.NotifyTransformChanges = false; parent.NotifyLocalTransformChanges = false;
        parent.Notify(CanvasItem.NotificationTransformChanged); parent.Notify(CanvasItem.NotificationLocalTransformChanged);
        Check(global.Count == 1 && local == 6, "Typed events project manual notifications independently of automatic policy.");
        global.Clear(); parent.NotifyLocalTransformChanges = parent.NotifyTransformChanges = true; child.GetGlobalTransform();
        Action<CanvasItem> forceThenFail = n => { n.ForceUpdateTransform(); throw new ApplicationException("local observer"); };
        parent.LocalTransformChanged += forceThenFail;
        Reject<ApplicationException>(() => parent.Position = new(9, 10)); parent.LocalTransformChanged -= forceThenFail; child.ForceUpdateTransform();
        Check(global.SequenceEqual(new[] { parent, child }) && child.GetGlobalTransform().Origin == new Vector2(9, 10), "Global invalidation precedes local callbacks and survives their failure.");
        global.Clear();
        var capture = root.BeginSceneCapture();
        try { Reject<InvalidOperationException>(parent.ForceUpdateTransform); Reject<InvalidOperationException>(() => parent.NotifyTransformChanges = true); }
        finally { Node.EndSceneCapture(capture); }
        Task.Run(() =>
        {
            Reject<InvalidOperationException>(parent.ForceUpdateTransform); Reject<InvalidOperationException>(() => _ = parent.GetGlobalTransform());
            Reject<InvalidOperationException>(() => _ = parent.NotifyTransformChanges); Reject<InvalidOperationException>(() => parent.NotifyLocalTransformChanges = true);
        }).GetAwaiter().GetResult();
        tree.Dispose(); Reject<ObjectDisposedException>(parent.ForceUpdateTransform);
        var rollback = new FailedEntry(); Reject<AggregateException>(() => new SceneTree(rollback));
        Check(rollback.Tree is null && rollback.TransformQueueEntry.List is null, "Failed activation cancels every pending entry.");
        rollback.Fail = false; using var recovered = new SceneTree(rollback); rollback.ForceUpdateTransform();
    }

    private static void BoundariesAndFailures()
    {
        var root = new Entity(); var child = new Entity(); var top = new Entity { Name = "Top", TopLevel = true }; var neutral = new Node(); var independent = new Entity();
        root.AddChild(child); root.AddChild(top); root.AddChild(neutral); neutral.AddChild(independent);
        using var tree = new SceneTree(root); tree.ProcessFrame(0);
        var log = new List<string>();
        foreach (var item in new[] { root, child, top, independent }) item.NotifyTransformChanges = true;
        root.TransformChanged += _ => { log.Add("root"); root.GetGlobalTransform(); };
        child.TransformChanged += _ => { log.Add("child"); child.GetGlobalTransform(); };
        top.TransformChanged += _ => log.Add("top"); independent.TransformChanged += _ => log.Add("independent");
        root.Position = Vector2.One; root.ForceUpdateTransform(); Check(log.SequenceEqual(new[] { "root" }), "Force does not flush descendants.");
        tree.ProcessFrame(0); Check(log.SequenceEqual(new[] { "root", "child" }), "Propagation stops at TopLevel and neutral nodes."); log.Clear();
        root.Position = new(2, 2); root.RemoveChild(child); tree.ProcessFrame(0); Check(log.SequenceEqual(new[] { "root" }), "Exit cancels pending child delivery.");
        root.AddChild(child); child.ForceUpdateTransform(); Check(log.Last() == "child", "Reentry queues fresh initial delivery."); log.Clear();
        Action<CanvasItem> fail = _ => throw new ApplicationException("transform observer"); root.TransformChanged += fail;
        root.Position = new(3, 3); Reject<AggregateException>(() => tree.ProcessFrame(0));
        Check(log.SequenceEqual(new[] { "root", "child" }), "Failed delivery does not skip later pending items."); root.TransformChanged -= fail; log.Clear();
        child.TransformChanged += fail; child.Position = Vector2.One; Reject<ApplicationException>(child.ForceUpdateTransform); child.ForceUpdateTransform(); child.TransformChanged -= fail;
        var reentered = false;
        Action<CanvasItem> again = n => { if (reentered) return; reentered = true; n.GetGlobalTransform(); child.Position = new(2, 2); child.ForceUpdateTransform(); };
        child.TransformChanged += again; child.Position = new(3, 3); child.ForceUpdateTransform(); child.TransformChanged -= again;
        Check(reentered, "Removing before dispatch permits explicit reentrant forcing.");
        child.TransformChanged += _ => { Reject<InvalidOperationException>(tree.Dispose); Reject<InvalidOperationException>(() => tree.ProcessFrame(0)); };
        child.Position = new(4, 4); child.ForceUpdateTransform();
        var skipped = new Entity { Name = "Skipped", NotifyTransformChanges = true }; root.AddChild(skipped);
        var deadCalls = 0; skipped.TransformChanged += _ => deadCalls++; skipped.Dispose(); tree.ProcessFrame(0); Check(deadCalls == 0, "Disposal cancels queued delivery.");
        log.Clear();
        // Use an observer-free item to isolate queue allocation from test bookkeeping.
        var plain = new Entity { Name = "Plain", NotifyTransformChanges = true }; root.AddChild(plain); plain.ForceUpdateTransform();
        for (var i = 0; i < 1000; i++) { plain.GetGlobalTransform(); plain.Position = new(i, 0); plain.ForceUpdateTransform(); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) { plain.GetGlobalTransform(); plain.Position = new(i, 0); plain.ForceUpdateTransform(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warm force and queue operations allocate zero managed bytes.");
        root.Position = new(20, 0);
        var removed = new Entity { Name = "Removed" }; root.AddChild(removed); var removedCalls = 0; removed.TransformChanged += _ => removedCalls++;
        Action<CanvasItem> remove = _ => removed.Dispose(); root.TransformChanged += remove;
        tree.ProcessFrame(0); root.TransformChanged -= remove;
        Check(removedCalls == 0 && removed.IsDisposed, "Callback removal cancels a later entry in the current delivery batch.");
    }

    private static void FramePhases()
    {
        var root = new Node(); var probe = new Probe { ProcessEnabled = true, PhysicsProcessEnabled = true, NotifyTransformChanges = true }; root.AddChild(probe);
        using var tree = new SceneTree(root); tree.ProcessFrame(0); var log = new List<string>();
        probe.TransformChanged += n => { log.Add("transform"); n.GetGlobalTransform(); };
        tree.ProcessFrameStarted += _ => log.Add("process-start"); tree.PhysicsFrameStarted += _ => log.Add("physics-start");
        probe.ProcessAction = () => { log.Add("process"); probe.Position = new(3, 3); };
        probe.PhysicsAction = () => { log.Add("physics"); probe.Position = new(4, 4); };
        probe.GetGlobalTransform(); probe.Position = Vector2.One; tree.ProcessFrame(0);
        Check(log.SequenceEqual(new[] { "process-start", "transform", "process", "transform" }), "Idle flushes before and after node processing."); log.Clear();
        probe.Position = new(2, 2); tree.PhysicsFrame(0);
        Check(log.SequenceEqual(new[] { "transform", "physics-start", "physics", "transform" }), "Physics flushes before the frame event and after processing."); log.Clear();
        probe.ProcessAction = null;
        tree.Defer(() => { log.Add("deferred"); probe.Position = new(5, 5); }); tree.ProcessFrame(0);
        Check(log.SequenceEqual(new[] { "process-start", "deferred", "transform" }), "Deferred changes flush before queued deletion."); log.Clear();
        tree.CreateTimer(0).Timeout += _ => { log.Add("timer"); probe.Position = new(6, 6); probe.QueueFree(); }; tree.ProcessFrame(0);
        Check(log.SequenceEqual(new[] { "process-start", "timer", "transform" }) && probe.IsDisposed, "Timer changes deliver before deletion.");
        var looping = new Entity { NotifyTransformChanges = true }; root.AddChild(looping); var calls = 0;
        looping.TransformChanged += n => { Check(++calls < 10, "Tail reentry waits for the next flush."); n.GetGlobalTransform(); looping.Position += Vector2.One; };
        tree.ProcessFrame(0); Check(calls == 3, "A tail reentry waits for the next of three idle delivery phases."); looping.Dispose();
        log.Clear(); var a = new Probe { Name = "A", NotifyTransformChanges = true, ProcessEnabled = true }; var b = new Entity { Name = "B" }; root.AddChild(a); root.AddChild(b);
        var aCalls = 0;
        a.TransformChanged += n => { log.Add(++aCalls == 1 ? "first" : "again"); n.GetGlobalTransform(); if (aCalls == 1) a.Position = Vector2.One; };
        b.TransformChanged += _ => log.Add("second"); a.ProcessAction = () => log.Add("node");
        tree.ProcessFrame(0);
        Check(log.SequenceEqual(new[] { "process-start", "first", "second", "again", "node" }), "An append behind a pending successor is reached in the current pass.");
    }

    private sealed class Placement : CanvasItem
    {
        private Transform _local = Transform.Identity;
        internal int Queries;
        public override Transform GetTransform() { Queries++; return _local; }
        internal void Move(Vector2 position) { _local.Origin = position; NotifyLocalTransformChanged(); }
    }
    private sealed class FailedEntry : Entity
    {
        internal bool Fail = true;
        protected override void OnReady() { if (Fail) throw new ApplicationException("ready"); }
    }
    private sealed class Probe : Entity
    {
        internal Action? ProcessAction, PhysicsAction;
        protected override void OnProcess(double delta) => ProcessAction?.Invoke();
        protected override void OnPhysicsProcess(double delta) => PhysicsAction?.Invoke();
    }
    private static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
