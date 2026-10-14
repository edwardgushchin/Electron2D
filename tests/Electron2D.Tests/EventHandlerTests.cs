using Electron2D;
using System.Runtime.CompilerServices;

internal static class EventHandlerTests
{
    internal static void Run()
    {
        DelegateSemantics(); SnapshotMutations(); ConcurrentSnapshots(); PreparedFamily(); PublisherLifetime();
    }

    private static void DelegateSemantics()
    {
        EventHandlers<Action<int>>? handlers = null;
        Action<int>? native = null;
        Action<int>[] atoms = [static _ => { }, static _ => { }, static _ => { }];
        var random = new Random(510);
        for (var step = 0; step < 1024; step++)
        {
            Action<int>? value = null;
            for (var i = 0; i < random.Next(1, 5); i++) value += atoms[random.Next(atoms.Length)];
            if (random.Next(2) == 0) { native += value; EventHandlers<Action<int>>.Add(ref handlers, value); }
            else { native -= value; handlers?.Remove(value); }
            using var snapshot = handlers?.Acquire() ?? default;
            var expected = native?.GetInvocationList() ?? [];
            Check(snapshot.Count == expected.Length, "Combined delegate removal retains native multiplicity.");
            for (var i = 0; i < expected.Length; i++) Check(snapshot[i].Equals(expected[i]), "Combined delegate removal retains last matching subsequence and order.");
        }
        handlers?.Clear();
        using var cleared = handlers?.Acquire() ?? default;
        Check(cleared.Count == 0, "Clear removes the current handler list.");
    }

    private static void SnapshotMutations()
    {
        using var body = new RigidBody();
        var order = new List<int>(); var nested = false; var mutate = true;
        Action<RigidBody> late = _ => order.Add(4);
        Action<RigidBody> second = _ => order.Add(3);
        body.SleepingStateChanged += _ =>
        {
            order.Add(nested ? 2 : 1);
            if (!mutate) return;
            mutate = false; body.SleepingStateChanged -= second; body.SleepingStateChanged += late;
            nested = true; body.RaiseSleepingStateChanged(); nested = false;
        };
        body.SleepingStateChanged += second;
        body.RaiseSleepingStateChanged();
        Check(order.SequenceEqual([1, 2, 4, 3]), "Reentrant emission sees changed handlers while outer emission retains its snapshot.");
        order.Clear(); body.RaiseSleepingStateChanged();
        Check(order.SequenceEqual([1, 4]), "Later emission observes the final handler list.");
        Action<RigidBody> failure = _ => throw new ArithmeticException("handler snapshot");
        body.SleepingStateChanged += failure; body.SleepingStateChanged += second;
        order.Clear();
        try { body.RaiseSleepingStateChanged(); throw new InvalidOperationException("Missing handler error."); }
        catch (ArithmeticException) { }
        Check(order.SequenceEqual([1, 4]), "Ordinary physical event stops within its snapshot on error.");
        body.SleepingStateChanged -= failure;
        order.Clear(); body.RaiseSleepingStateChanged();
        Check(order.SequenceEqual([1, 4, 3]), "Error releases the snapshot and permits later delivery.");
    }

    private static void ConcurrentSnapshots()
    {
        EventHandlers<Action>? store = null;
        Action first = static () => { }; Action last = static () => { };
        EventHandlers<Action>.Add(ref store, first); EventHandlers<Action>.Add(ref store, last);
        using var held = store!.Acquire();
        Parallel.For(0, 256, _ =>
        {
            Action transient = () => GC.KeepAlive(_);
            EventHandlers<Action>.Add(ref store, transient);
            using (var current = store!.Acquire())
                Check(current.Count >= 2 && current[0] == first, "Concurrent mutation publishes complete ordered lists.");
            store.Remove(transient);
        });
        Check(held.Count == 2 && held[0] == first && held[1] == last, "Concurrent mutations preserve a leased snapshot.");
        using var final = store.Acquire();
        Check(final.Count == 2 && final[0] == first && final[1] == last, "Concurrent exact removals retain permanent handlers.");
    }

    private static void PublisherLifetime()
    {
        using var area = new Area(); using var body = new RigidBody();
        var reference = Attach(area, body);
        area.Dispose(); body.Dispose();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        Check(!reference.IsAlive, "Disposed publishers release every physical event family subscriber.");
        GC.KeepAlive(area); GC.KeepAlive(body);
    }

    private static void PreparedFamily()
    {
        using var world = new World(PhysicsServer.Backend.CPU);
        using var root = new SubViewport { World = world };
        var area = new Area(); var peer = new Area { Name = "Peer" }; var body = new RigidBody();
        root.AddChild(area); root.AddChild(peer); root.AddChild(body);
        using var tree = new SceneTree(root); using var input = new InputEventMouseMotion();
        var areaPair = new PhysicsShapePair(peer.GetRID(), peer.BorrowIdentity(), true, 2, 3);
        var bodyPair = new PhysicsShapePair(body.GetRID(), body.BorrowIdentity(), false, 4, 5);
        (string Name, Func<Action, EventConnection> Subscribe, Action Emit)[] cases =
        [
            ("AreaEntered", call => EventConnection.Subscribe<Area>(h => area.AreaEntered += h, h => area.AreaEntered -= h, _ => call(), oneShot: true), () => area.RaiseOverlap(new(areaPair, true, true))),
            ("AreaExited", call => EventConnection.Subscribe<Area>(h => area.AreaExited += h, h => area.AreaExited -= h, _ => call(), oneShot: true), () => area.RaiseOverlap(new(areaPair, false, true))),
            ("Area.BodyEntered", call => EventConnection.Subscribe<Entity>(h => area.BodyEntered += h, h => area.BodyEntered -= h, _ => call(), oneShot: true), () => area.RaiseOverlap(new(bodyPair, true, true))),
            ("Area.BodyExited", call => EventConnection.Subscribe<Entity>(h => area.BodyExited += h, h => area.BodyExited -= h, _ => call(), oneShot: true), () => area.RaiseOverlap(new(bodyPair, false, true))),
            ("AreaShapeEntered", call => EventConnection.Subscribe<RID, Area?, int, int>(h => area.AreaShapeEntered += h, h => area.AreaShapeEntered -= h, (_, _, _, _) => call(), oneShot: true), () => area.RaiseOverlap(new(areaPair, true, false))),
            ("AreaShapeExited", call => EventConnection.Subscribe<RID, Area?, int, int>(h => area.AreaShapeExited += h, h => area.AreaShapeExited -= h, (_, _, _, _) => call(), oneShot: true), () => area.RaiseOverlap(new(areaPair, false, false))),
            ("Area.BodyShapeEntered", call => EventConnection.Subscribe<RID, Entity?, int, int>(h => area.BodyShapeEntered += h, h => area.BodyShapeEntered -= h, (_, _, _, _) => call(), oneShot: true), () => area.RaiseOverlap(new(bodyPair, true, false))),
            ("Area.BodyShapeExited", call => EventConnection.Subscribe<RID, Entity?, int, int>(h => area.BodyShapeExited += h, h => area.BodyShapeExited -= h, (_, _, _, _) => call(), oneShot: true), () => area.RaiseOverlap(new(bodyPair, false, false))),
            ("RigidBody.BodyEntered", call => EventConnection.Subscribe<Node>(h => body.BodyEntered += h, h => body.BodyEntered -= h, _ => call(), oneShot: true), () => body.RaiseContact(new(bodyPair, true, true))),
            ("RigidBody.BodyExited", call => EventConnection.Subscribe<Node>(h => body.BodyExited += h, h => body.BodyExited -= h, _ => call(), oneShot: true), () => body.RaiseContact(new(bodyPair, false, true))),
            ("RigidBody.BodyShapeEntered", call => EventConnection.Subscribe<RID, Node, int, int>(h => body.BodyShapeEntered += h, h => body.BodyShapeEntered -= h, (_, _, _, _) => call(), oneShot: true), () => body.RaiseContact(new(bodyPair, true, false))),
            ("RigidBody.BodyShapeExited", call => EventConnection.Subscribe<RID, Node, int, int>(h => body.BodyShapeExited += h, h => body.BodyShapeExited -= h, (_, _, _, _) => call(), oneShot: true), () => body.RaiseContact(new(bodyPair, false, false))),
            ("SleepingStateChanged", call => EventConnection.Subscribe<RigidBody>(h => body.SleepingStateChanged += h, h => body.SleepingStateChanged -= h, _ => call(), oneShot: true), body.RaiseSleepingStateChanged),
            ("InputEvent", call => EventConnection.Subscribe<Node, InputEvent, int>(h => body.InputEvent += h, h => body.InputEvent -= h, (_, _, _) => call(), oneShot: true), () => Pointer(0)),
            ("MouseEntered", call => EventConnection.Subscribe(h => body.MouseEntered += h, h => body.MouseEntered -= h, call, oneShot: true), () => Pointer(1)),
            ("MouseExited", call => EventConnection.Subscribe(h => body.MouseExited += h, h => body.MouseExited -= h, call, oneShot: true), () => Pointer(2)),
            ("MouseShapeEntered", call => EventConnection.Subscribe<int>(h => body.MouseShapeEntered += h, h => body.MouseShapeEntered -= h, _ => call(), oneShot: true), () => Pointer(3)),
            ("MouseShapeExited", call => EventConnection.Subscribe<int>(h => body.MouseShapeExited += h, h => body.MouseShapeExited -= h, _ => call(), oneShot: true), () => Pointer(4)),
        ];
        foreach (var item in cases)
        {
            // Prepare dispatch/JIT without consuming the measured subscriptions.
            using (var warm = item.Subscribe(static () => { })) item.Emit();
            var count = 0; Action increment = () => count++;
            var tokens = new EventConnection[128];
            for (var i = 0; i < tokens.Length; i++) tokens[i] = item.Subscribe(increment);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            item.Emit();
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
            Check(ownerBytes == 0 && allBytes == 0 && count == 128, $"Prepared {item.Name} one-shot removal/delivery: {ownerBytes}/{allBytes} B, {count} callbacks.");
            item.Emit(); Check(count == 128 && tokens.All(t => !t.IsConnected), "Every physical family consumes its wrappers once.");
            foreach (var token in tokens) token.Dispose();
        }
        Console.WriteLine("All 18 physical event accessors: 128 first one-shot callbacks each at 0/0 B.");
        void Pointer(int kind)
        {
            List<Exception>? errors = null;
            body.DispatchPhysicsPointer(kind, root, input, 4, ref errors);
            Check(errors is null, "Pointer event handlers completed without errors.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Attach(Area area, RigidBody body)
    {
        var target = new byte[4096];
        area.AreaEntered += _ => GC.KeepAlive(target); area.AreaExited += _ => GC.KeepAlive(target);
        area.BodyEntered += _ => GC.KeepAlive(target); area.BodyExited += _ => GC.KeepAlive(target);
        area.AreaShapeEntered += (_, _, _, _) => GC.KeepAlive(target); area.AreaShapeExited += (_, _, _, _) => GC.KeepAlive(target);
        area.BodyShapeEntered += (_, _, _, _) => GC.KeepAlive(target); area.BodyShapeExited += (_, _, _, _) => GC.KeepAlive(target);
        body.BodyEntered += _ => GC.KeepAlive(target); body.BodyExited += _ => GC.KeepAlive(target);
        body.BodyShapeEntered += (_, _, _, _) => GC.KeepAlive(target); body.BodyShapeExited += (_, _, _, _) => GC.KeepAlive(target);
        body.SleepingStateChanged += _ => GC.KeepAlive(target);
        body.InputEvent += (_, _, _) => GC.KeepAlive(target);
        body.MouseEntered += () => GC.KeepAlive(target); body.MouseExited += () => GC.KeepAlive(target);
        body.MouseShapeEntered += _ => GC.KeepAlive(target); body.MouseShapeExited += _ => GC.KeepAlive(target);
        return new WeakReference(target);
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
