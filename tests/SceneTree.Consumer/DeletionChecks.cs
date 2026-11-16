using Electron2D;

internal static class DeletionChecks
{
    internal static void Run()
    {
        using var root = new Node();
        using var tree = new SceneTree(root);
        using var detached = new Node();
        root.AddChild(detached);
        detached.QueueFree();
        root.RemoveChild(detached);
        tree.FinalizeLoop();
        Check(!detached.IsDisposed && !detached.IsQueuedForDeletion, "shutdown releases detached QueueFree request");
        using var firstRoot = new Node();
        using var secondRoot = new Node();
        using var first = new SceneTree(firstRoot);
        using var second = new SceneTree(secondRoot);
        using var moved = new Node();
        firstRoot.AddChild(moved); moved.QueueFree(); firstRoot.RemoveChild(moved);
        secondRoot.AddChild(moved); secondRoot.RemoveChild(moved);
        first.FlushDeferred();
        Check(!moved.IsDisposed && moved.IsQueuedForDeletion, "old attachment cannot delete transferred detached node");
        first.FinalizeLoop();
        Check(moved.IsQueuedForDeletion, "old shutdown preserves receiving tree request");
        second.FlushDeferred();
        Check(moved.IsDisposed && !moved.IsQueuedForDeletion, "receiving tree owns deletion after detach");
        Matrix();
        Console.WriteLine("SceneTree public deletion checks passed.");
    }
    private static void Matrix()
    {
        using var root = new Node(); using var tree = new SceneTree(root);
        using var otherRoot = new Node(); using var other = new SceneTree(otherRoot);
        Reject<ArgumentException>(() => tree.QueueDelete(tree));
        Reject<InvalidOperationException>(() => tree.QueueDelete(root));
        Reject<InvalidOperationException>(root.QueueFree);
        Reject<InvalidOperationException>(() => tree.QueueDelete(otherRoot));
        Reject<ArgumentNullException>(() => tree.QueueDelete(null!));
        using var retiredNode = new Node(); root.AddChild(retiredNode); retiredNode.QueueFree(); retiredNode.Dispose(); tree.FlushDeferred();
        Check(!retiredNode.IsQueuedForDeletion, "accepted QueueFree then Dispose releases mark");
        using var retiredPlain = new ElectronObject(); tree.QueueDelete(retiredPlain); retiredPlain.Dispose(); tree.FlushDeferred();
        Check(!retiredPlain.IsQueuedForDeletion, "accepted QueueDelete then Dispose releases mark");
        using var canceled = new Node(); root.AddChild(canceled);
        canceled.QueueFree(); canceled.QueueFree(); Check(canceled.CancelFree() && !canceled.CancelFree(), "idempotence/cancel");
        tree.FlushDeferred(); Check(!canceled.IsDisposed && !canceled.IsQueuedForDeletion, "canceled request skipped");
        canceled.QueueFree(); canceled.CancelFree(); canceled.QueueFree(); tree.FlushDeferred();
        Check(canceled.IsDisposed && !canceled.IsQueuedForDeletion, "stale generation cannot consume replacement");
        using var explicitNode = new Node(); root.AddChild(explicitNode);
        explicitNode.QueueFree(); tree.QueueDelete(explicitNode); tree.QueueDelete(explicitNode);
        explicitNode.CancelFree(); tree.FlushDeferred(); Check(explicitNode.IsDisposed && !explicitNode.IsQueuedForDeletion, "explicit duplicates survive node cancellation");
        using var plain = new ElectronObject(); using var resource = new Resource();
        tree.QueueDelete(plain); other.QueueDelete(plain); tree.QueueDelete(resource);
        tree.FlushDeferred(); other.FlushDeferred();
        Check(plain.IsDisposed && resource.IsDisposed && !plain.IsQueuedForDeletion, "plain/resource/multiple-tree explicit ownership");
        using var foreign = new Node(); root.AddChild(foreign); tree.QueueDelete(foreign);
        root.RemoveChild(foreign); otherRoot.AddChild(foreign);
        Reject<AggregateException>(tree.FlushDeferred);
        Check(!foreign.IsDisposed && !foreign.IsQueuedForDeletion, "foreign explicit refusal releases mark");
        using var later = new Node(); using var spawned = new Node(); root.AddChild(later); root.AddChild(spawned);
        var order = new List<string>();
        var failing = new Probe { Cleanup = () => { order.Add("delete"); spawned.QueueFree(); tree.Defer(() => order.Add("late")); throw new InvalidOperationException("controlled"); } };
        tree.Defer(() => { order.Add("deferred"); later.QueueFree(); tree.Defer(() => order.Add("nested")); throw new InvalidOperationException("controlled"); });
        tree.QueueDelete(failing);
        Reject<AggregateException>(tree.FlushDeferred);
        Check(later.IsDisposed && !spawned.IsDisposed && order.SequenceEqual(new[] { "deferred", "nested", "delete" }), "failures continue; deletion-created work waits");
        tree.FlushDeferred(); tree.ProcessFrame(0); tree.PhysicsFrame(0);
        Check(spawned.IsDisposed && order[^1] == "late", "later safe point drains deletion-created work");
        using var reissued = new Node(); root.AddChild(reissued);
        var cancelFromDeletion = new Probe { Cleanup = () => { Check(reissued.CancelFree(), "cancel captured request"); reissued.QueueFree(); } };
        tree.QueueDelete(cancelFromDeletion); reissued.QueueFree(); tree.FlushDeferred();
        Check(!reissued.IsDisposed && reissued.IsQueuedForDeletion, "reissued request waits for new FIFO position");
        tree.FlushDeferred(); Check(reissued.IsDisposed, "reissued request consumed next flush");
        using var neverAttached = new Node(); neverAttached.QueueFree(); root.AddChild(neverAttached); tree.FlushDeferred();
        Check(neverAttached.IsDisposed, "detached request publishes on entry");
        using var returning = new Node(); root.AddChild(returning); returning.QueueFree(); root.RemoveChild(returning);
        otherRoot.AddChild(returning); otherRoot.RemoveChild(returning); root.AddChild(returning);
        other.FinalizeLoop(); Check(returning.IsQueuedForDeletion, "intermediate shutdown preserves latest attachment");
        tree.FlushDeferred(); Check(returning.IsDisposed, "returning attachment executes exactly once");
        var physicsDelete = new Node(); root.AddChild(physicsDelete); physicsDelete.QueueFree(); tree.PhysicsFrame(0);
        Check(physicsDelete.IsDisposed, "physics safe point");
        var idleDelete = new Node(); root.AddChild(idleDelete); tree.QueueDelete(idleDelete); tree.ProcessFrame(0);
        Check(idleDelete.IsDisposed, "idle safe point");
        tree.QueueDelete(other); tree.FlushDeferred(); Check(other.IsDisposed, "another SceneTree is a valid explicit target");
        using var discarded = new ElectronObject(); tree.QueueDelete(discarded);
        tree.FinalizeLoop(); Check(!discarded.IsDisposed && !discarded.IsQueuedForDeletion, "shutdown discards explicit detached request");
        Reject<ObjectDisposedException>(() => tree.QueueDelete(discarded));
        using var raceRoot = new Node(); using var raceTree = new SceneTree(raceRoot);
        for (var i = 0; i < 128; i++)
        {
            var node = new Node(); raceRoot.AddChild(node);
            using var start = new ManualResetEventSlim();
            var queue = Task.Run(() => { start.Wait(); try { node.QueueFree(); } catch (ObjectDisposedException) { } });
            start.Set(); node.Dispose(); queue.GetAwaiter().GetResult(); raceTree.FlushDeferred();
            Check(node.IsDisposed && !node.IsQueuedForDeletion, "concurrent Dispose/QueueFree drains");
            var plainRace = new ElectronObject();
            using var explicitStart = new ManualResetEventSlim();
            var dispose = Task.Run(() => { explicitStart.Wait(); plainRace.Dispose(); });
            explicitStart.Set();
            try { raceTree.QueueDelete(plainRace); } catch (ObjectDisposedException) { }
            dispose.GetAwaiter().GetResult(); raceTree.FlushDeferred();
            Check(plainRace.IsDisposed && !plainRace.IsQueuedForDeletion, "concurrent Dispose/QueueDelete drains");
        }
    }
    private sealed class Probe : ElectronObject
    {
        internal Action? Cleanup;
        protected override void Dispose(bool disposing)
        {
            try { Cleanup?.Invoke(); } finally { base.Dispose(disposing); }
        }
    }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
