using Electron2D;
using System.Runtime.CompilerServices;

internal static class EventConnectionMembershipTests
{
    internal static void Run()
    {
        Membership(); PairMembership(); CapturedDelivery(); ConcurrentQueries(); RetiredReferences();
        Console.WriteLine("Event membership: live removal/clear/re-add, duplicates/fan-out, captured/deferred delivery, concurrency, retained references and zero warmed query bytes passed.");
    }

    private static void Membership()
    {
        EventHandlers<Action>? first = null, second = null; Action wrapper = null!;
        using var connection = EventConnection.Subscribe(h =>
        {
            wrapper = h; EventHandlers<Action>.Add(ref first, h); EventHandlers<Action>.Add(ref first, h); EventHandlers<Action>.Add(ref second, h);
        }, h => { first?.Remove(h); first?.Remove(h); second?.Remove(h); }, static () => { });
        Check(connection.IsConnected, "A wrapper retained by its publishers is connected");
        second!.Clear(); first!.Remove(wrapper); Check(connection.IsConnected, "One remaining duplicate retains membership");
        first.Remove(wrapper); Check(!connection.IsConnected, "Removing the last prepared publisher entry disconnects its query");
        EventHandlers<Action>.Add(ref second, wrapper); Check(connection.IsConnected, "Re-adding an active wrapper restores actual membership");
        second!.Clear(); Check(!connection.IsConnected, "Clearing a prepared publisher disconnects its query");
        EventHandlers<Action>.Add(ref first, wrapper); connection.Dispose(); EventHandlers<Action>.Add(ref first, wrapper);
        Check(!connection.IsConnected && connection.WasConnectedAtDisposal, "A disposed token stays terminal even if a custom accessor retains its wrapper");
        first!.Clear();
        Action? field = null;
        using var ordinary = EventConnection.Subscribe(h => field += h, h => field -= h, static () => { });
        field = null; Check(ordinary.IsConnected, "Unobserved custom accessors retain the documented logical-state query");
        EventHandlers<Action>.Add(ref first, ordinary.Dispose); first!.Clear();
        Check(ordinary.IsConnected, "Using a token Dispose method as an unrelated handler does not bind that token to the publisher");
    }

    private static void PairMembership()
    {
        EventHandlers<Action<int, int>>? source = null; var calls = 0;
        using var connection = EventConnection.Subscribe<int, int>(h => EventHandlers<Action<int, int>>.Add(ref source, h), h => source?.Remove(h),
            (first, second) => { Check(first == 3 && second == 4, "Captured pair payload stays exact"); calls++; });
        Check(connection.IsConnected, "Prepared two-argument wrappers are observed");
        using (var snapshot = source!.Acquire())
        {
            source.Clear(); Check(!connection.IsConnected, "Two-argument membership follows the current publisher"); snapshot[0](3, 4);
        }
        connection.Dispose();
        Check(calls == 1 && !connection.WasConnectedAtDisposal, "Removing an already detached token does not report an active scene disconnection");
    }

    private static void CapturedDelivery()
    {
        EventHandlers<Action>? source = null; var calls = 0; var queue = new Queue<Action>();
        using var immediate = EventConnection.Subscribe(h => EventHandlers<Action>.Add(ref source, h), h => source?.Remove(h), () => calls++);
        using (var snapshot = source!.Acquire())
        {
            source.Clear(); Check(!immediate.IsConnected, "A leased old snapshot is not current publisher membership");
            snapshot[0](); Check(calls == 1, "An already captured emission retains ordinary immutable-snapshot delivery");
        }
        using var deferred = EventConnection.Subscribe(h => EventHandlers<Action>.Add(ref source, h), h => source?.Remove(h), () => calls++, defer: queue.Enqueue);
        Emit(source!); source!.Clear(); Check(!deferred.IsConnected && queue.Count == 1, "Membership observes detach without cancelling already accepted deferred work");
        queue.Dequeue()(); Check(calls == 2, "A publisher clear preserves accepted deferred delivery");
        Action wrapper = null!;
        using var once = EventConnection.Subscribe(h => { wrapper = h; EventHandlers<Action>.Add(ref source, h); }, h => source?.Remove(h), () => calls++, oneShot: true, defer: queue.Enqueue);
        Emit(source!); Check(!once.IsConnected && queue.Count == 1, "A deferred one-shot reports disconnected before delivery");
        EventHandlers<Action>.Add(ref source, wrapper); Check(!once.IsConnected, "Re-adding a consumed wrapper does not revive it");
        queue.Dequeue()(); Check(calls == 3, "Consumed one-shot still delivers its accepted pending work once");
        source!.Clear();
        EventHandlers<Action>.Add(ref source, wrapper); once.Dispose(); source!.Clear();
        EventHandlers<Action<int>>? values = null; var valueCalls = 0;
        using var value = EventConnection.Subscribe<int>(h => EventHandlers<Action<int>>.Add(ref values, h), h => values?.Remove(h), _ => valueCalls++);
        for (var i = 0; i < 128; i++) Check(value.IsConnected, "Prepared query warmup");
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 1024; i++) Check(value.IsConnected, "Prepared query retains membership");
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0 && valueCalls == 0, "Prepared membership queries allocate zero owner/all-thread bytes and invoke no handler");
    }

    private static void ConcurrentQueries()
    {
        EventHandlers<Action>? source = null; Action wrapper = null!;
        using var connection = EventConnection.Subscribe(h => { wrapper = h; EventHandlers<Action>.Add(ref source, h); }, h => source?.Remove(h), static () => { });
        Exception? error = null; using var start = new ManualResetEventSlim();
        var writer = new Thread(() =>
        {
            try { start.Wait(); for (var i = 0; i < 4096; i++) { source!.Clear(); EventHandlers<Action>.Add(ref source, wrapper); } }
            catch (Exception failure) { error = failure; }
        });
        writer.Start(); start.Set();
        for (var i = 0; i < 4096; i++) _ = connection.IsConnected;
        Check(writer.Join(TimeSpan.FromSeconds(10)) && error is null, "Concurrent source mutation and membership queries finish without lock inversion");
        source!.Clear(); Check(!connection.IsConnected, "The completed final clear is observed");
    }

    private static void RetiredReferences()
    {
        var (disposed, first) = Retire(false); Collect(); Check(!first.IsAlive, "A retained disposed token releases observed publishers and unrelated handlers"); GC.KeepAlive(disposed);
        var (consumed, second) = Retire(true); Collect(); Check(!second.IsAlive, "A retained consumed token releases observed publishers and unrelated handlers"); GC.KeepAlive(consumed);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (EventConnection, WeakReference) Retire(bool once)
    {
        EventHandlers<Action>? source = null; var payload = new Payload(); var weak = new WeakReference(payload);
        EventHandlers<Action>.Add(ref source, payload.Touch);
        var connection = EventConnection.Subscribe(h => EventHandlers<Action>.Add(ref source, h), h => source?.Remove(h), static () => { }, oneShot: once);
        if (once) Emit(source!); else connection.Dispose();
        return (connection, weak);
    }
    private static void Emit(EventHandlers<Action> source) { using var snapshot = source.Acquire(); for (var i = 0; i < snapshot.Count; i++) snapshot[i](); }
    private static void Collect() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers(); }
    private sealed class Payload { internal void Touch() { } }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
