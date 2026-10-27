using System.Runtime.CompilerServices;
using Electron2D;
using Monitor = Electron2D.Performance.Monitor;

internal static class DeferredMemoryContract
{
    private static double Peak => Performance.GetMonitor(Monitor.MemoryMessageBufferMax);
    private static readonly Action Noop = static () => { };
    private static readonly Action<int> Set = static _ => { };

    internal static void Run()
    {
        Check((int)Monitor.MemoryMessageBufferMax == 6 && Peak == 0, "Fresh process identity/empty storage.");
        Noop(); Set(0);
        using (var tree = new SceneTree(new Node()))
        {
            var before = GC.GetAllocatedBytesForCurrentThread(); tree.Defer(Noop);
            var firstArray = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(Peak == firstArray && firstArray > 0, $"First queue buffer equals its actual managed allocation: {Peak}/{firstArray}.");
            for (var i = 0; i < 3; i++) tree.Defer(Noop);
            Check(Peak == firstArray, "Unused reserved capacity is already counted.");
            before = GC.GetAllocatedBytesForCurrentThread(); tree.Defer(Noop);
            var secondArray = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(Peak == secondArray && secondArray > firstArray, "Growth replaces the old backing allocation.");
            tree.FlushDeferred(); Check(Peak == secondArray, "Drain retains capacity and peak.");
            tree.Defer(() => tree.Defer(Noop)); tree.FlushDeferred();
            Check(Peak == secondArray + firstArray, "Captured and receiving buffers both contribute.");
            var queues = Peak;
            before = GC.GetAllocatedBytesForCurrentThread(); tree.SetDeferred(Set, 1);
            var slot = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(Peak > queues && Peak <= queues + slot, "Typed invocation storage contributes, bookkeeping is excluded.");
            tree.FlushDeferred();
            var referenced = new byte[1024 * 1024]; Action captured = () => GC.KeepAlive(referenced);
            var beforeCapture = Peak; tree.Defer(captured);
            Check(Peak == beforeCapture, "Caller-owned callback and referenced graph are excluded.");
            tree.FlushDeferred();
            var beforeValue = Peak; tree.SetDeferred<LargeValue>(static _ => { }, default);
            Check(Peak >= beforeValue + 2048, "Inline typed payload storage contributes its actual size.");
            tree.FlushDeferred(); var prepared = Peak;
            for (var i = 0; i < 128; i++) Cycle(tree);
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 256; i++) Cycle(tree);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0 && Peak == prepared, $"Prepared enqueue/drain/reads: {owner}/{all} owner/all-thread bytes.");
            Task.Run(() =>
            {
                for (var i = 0; i < 128; i++) _ = Peak;
                var start = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 256; i++) if (Peak != prepared) throw new InvalidOperationException("Worker peak changed.");
                Check(GC.GetAllocatedBytesForCurrentThread() == start, "Worker reads allocate nothing.");
                for (var i = 0; i < 65; i++) tree.SetDeferred(Set, i);
            }).GetAwaiter().GetResult();
            tree.FlushDeferred();
            var completed = 0;
            tree.Defer(() =>
            {
                Task.Run(() => tree.SetDeferred<int>(_ => completed++, 1)).GetAwaiter().GetResult();
                throw new InvalidOperationException("Expected callback failure.");
            });
            try { tree.FlushDeferred(); throw new Exception("Missing deferred failure."); }
            catch (AggregateException) { }
            Check(completed == 1, "Worker typed delivery survives captured-buffer callback failure.");
            var group = new Node { ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread };
            tree.Root.AddChild(group);
            for (var i = 0; i < 257; i++) group.CallDeferredThreadGroup<Node>(static _ => { });
            var groupPeak = Peak; group.Dispose();
            group = new Node { ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread }; tree.Root.AddChild(group);
            for (var i = 0; i < 257; i++) group.CallDeferredThreadGroup<Node>(static _ => { });
            Check(Peak == groupPeak, "Retired group queue storage is released while typed slots are reused.");
            group.Dispose();
        }
        var retired = LargeTree(); var retiredPeak = Peak; retired.Dispose();
        using (var replacement = LargeTree()) Check(Peak == retiredPeak, "Disposed trees do not accumulate into later peaks.");
        GC.KeepAlive(retired);
        var weak = AbandonTree(); var collectedPeak = Peak;
        for (var i = 0; i < 8 && weak.IsAlive; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Check(!weak.IsAlive, "Diagnostics must not retain an undisposed tree.");
        using (var replacement = LargeTree()) Check(Peak == collectedPeak, "Collected tree storage does not accumulate into later peaks.");
        using (var first = LargeTree())
        using (var second = LargeTree()) Check(Peak == 2 * collectedPeak, "Independent live trees contribute together.");
        Check(Peak == 2 * collectedPeak, "Teardown retains the process peak.");
        Console.WriteLine($"Deferred memory: real growth/replacement, dual buffers, typed/group storage, worker/error delivery, disposed/collected owners and 256 zero-allocation cycles passed; peak={Peak} bytes.");
    }

    [InlineArray(256)]
    private struct LargeValue { private long _element0; }

    private static void Cycle(SceneTree tree)
    {
        tree.Defer(Noop); tree.SetDeferred(Set, 1); tree.FlushDeferred(); _ = Peak;
    }
    private static SceneTree LargeTree()
    {
        var tree = new SceneTree(new Node());
        for (var i = 0; i < 8193; i++) tree.Defer(Noop);
        return tree;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AbandonTree() => new(LargeTree());
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
