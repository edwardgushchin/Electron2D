using Electron2D;

internal static class EventConnectionPhysicsTests
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread,
            Environment.GetEnvironmentVariable("ELECTRON2D_TEST_THREADED_COLLIDER") == "1");
        Immediate(); DeferredPair();
        using var world = new World(backend); using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 1 }; using var sensorShape = new CircleShape { Radius = 5 };
        var body = new PublisherBody { Position = new(100, 0), CanSleep = false, GravityScale = 0, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
        body.AddChild(new CollisionShape { Shape = shape }); var area = new Area { Monitoring = true };
        area.AddChild(new CollisionShape { Shape = sensorShape }); root.AddChild(body); root.AddChild(area); using var tree = new SceneTree(root);
        Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Actual selected physical backend without fallback.");
        var pulses = 0; var values = 0; var pairs = 0; var enters = 0; var exits = 0;
        using var pulse = EventConnection.Subscribe(h => body.Pulse += h, h => body.Pulse -= h, () => pulses++);
        using var value = EventConnection.Subscribe<PublisherBody>(h => body.Value += h, h => body.Value -= h, sender => { Check(ReferenceEquals(sender, body), "One-argument source identity."); values++; });
        using var pair = EventConnection.Subscribe<PublisherBody, int>(h => body.Pair += h, h => body.Pair -= h,
            (sender, tick) => { Check(ReferenceEquals(sender, body) && tick == body.Ticks, "Two-argument source and current tick."); pairs++; });
        using var enter = EventConnection.Subscribe<Entity>(h => area.BodyEntered += h, h => area.BodyEntered -= h, other => { Check(ReferenceEquals(other, body), "Area entered body identity."); enters++; });
        using var exit = EventConnection.Subscribe<Entity>(h => area.BodyExited += h, h => area.BodyExited -= h, other => { Check(ReferenceEquals(other, body), "Area exited body identity."); exits++; });
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        for (var i = 0; i < 128; i++) Frame(i);
        var first = (pulses, values, pairs, enters, exits); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame(i);
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Check(ownerBytes == 0 && allBytes == 0, "Warmed full physics frames with immediate typed event connections allocate zero owner/all-thread bytes.");
        Check(pulses - first.pulses == 128 && values - first.values == 128 && pairs - first.pairs == 128 && enters - first.enters == 64 && exits - first.exits == 64,
            "Every measured frame simulates and publishes all event arities; overlap events retain exact alternating transitions.");
        pulse.Dispose(); value.Dispose(); pair.Dispose(); enter.Dispose(); exit.Dispose();
        var counts = (pulses, values, pairs, enters, exits); Frame(0); Frame(1); Check(counts == (pulses, values, pairs, enters, exits), "Disposed physical subscriptions do not deliver later events.");
        var calls = 0;
        using (var failure = EventConnection.Subscribe<PublisherBody, int>(h => body.Pair += h, h => body.Pair -= h, (_, _) => { calls++; throw new ArithmeticException(); }))
        {
            Reject<AggregateException>(() => tree.PhysicsFrame(.01)); Check(calls == 1, "An immediate connection failure is collected without replaying its body callback.");
        }
        tree.PhysicsFrame(.01); Check(calls == 1 && !body.IsDisposed, "The next physical step remains usable after unsubscribing the failing handler.");
        Console.WriteLine($"Event connections {backend}: all arities, actual Area transitions, identities, errors and 128 warm/128 measured full frames at {ownerBytes}/{allBytes} B passed.");
        void Frame(int i) { body.Position = new(i % 2 == 0 ? 0 : 100, 0); body.LinearVelocity = new(1, 0); tree.PhysicsFrame(.01); Check(body.Position.X > (i % 2 == 0 ? 0 : 100), "The event fixture retains actual body integration."); }
    }

    private static void Immediate()
    {
        var source = new PublisherBody(); var pulses = 0; var values = 0; var pairs = 0;
        using var pulse = EventConnection.Subscribe(h => source.Pulse += h, h => source.Pulse -= h, () => pulses++);
        using var value = EventConnection.Subscribe<PublisherBody>(h => source.Value += h, h => source.Value -= h, sender => { Check(ReferenceEquals(sender, source), "Immediate argument identity."); values++; });
        using var pair = EventConnection.Subscribe<PublisherBody, int>(h => source.Pair += h, h => source.Pair -= h, (sender, tick) => { Check(ReferenceEquals(sender, source) && tick == source.Ticks, "Immediate pair values."); pairs++; });
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        for (var i = 0; i < 128; i++) source.Raise(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) source.Raise(); var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Console.WriteLine($"Immediate event bracket: {ownerBytes}/{allBytes} B for 128 emissions of each arity.");
        Check(ownerBytes == 0 && allBytes == 0 && pulses == 256 && values == 256 && pairs == 256, "Prepared immediate connections have no captured per-emission allocation.");
        source.Dispose();
    }

    private static void DeferredPair()
    {
        using var source = new PublisherBody(); var queue = new Queue<Action>(); var observed = new List<int>();
        using var pair = EventConnection.Subscribe<PublisherBody, int>(h => source.Pair += h, h => source.Pair -= h,
            (sender, tick) => { Check(ReferenceEquals(sender, source), "Deferred source identity."); observed.Add(tick); }, defer: queue.Enqueue);
        source.Raise(); source.Raise(); Check(observed.Count == 0 && queue.Count == 2, "Reusable deferred events retain each emission.");
        while (queue.TryDequeue(out var action)) action(); Check(observed.SequenceEqual([1, 2]), "Deferred value capture and ordering.");
        source.Raise(); pair.Dispose(); while (queue.TryDequeue(out var action)) action(); Check(observed.Count == 2, "Disposal cancels pending reusable pairs.");
        using var once = EventConnection.Subscribe<PublisherBody, int>(h => source.Pair += h, h => source.Pair -= h,
            (_, tick) => observed.Add(tick), oneShot: true, defer: queue.Enqueue);
        source.Raise(); source.Raise(); Check(!once.IsConnected && queue.Count == 1, "One-shot detaches before scheduling.");
        while (queue.TryDequeue(out var action)) action(); Check(observed.SequenceEqual([1, 2, 4]), "Pending one-shot delivers its captured first value.");
        using var cancelled = EventConnection.Subscribe<PublisherBody, int>(h => source.Pair += h, h => source.Pair -= h,
            (_, _) => throw new InvalidOperationException("cancelled callback"), oneShot: true, defer: queue.Enqueue);
        source.Raise(); cancelled.Dispose(); while (queue.TryDequeue(out var action)) action();
        var calls = 0; using var recursive = EventConnection.Subscribe<PublisherBody, int>(h => source.Pair += h, h => source.Pair -= h,
            (_, _) => { calls++; source.Raise(); }, oneShot: true);
        source.Raise(); Check(calls == 1 && !recursive.IsConnected, "Immediate pair one-shot remains recursion-safe.");
    }

    private sealed class PublisherBody : RigidBody
    {
        internal event Action? Pulse; internal event Action<PublisherBody>? Value; internal event Action<PublisherBody, int>? Pair; internal int Ticks;
        internal void Raise() { Ticks++; Pulse?.Invoke(); Value?.Invoke(this); Pair?.Invoke(this, Ticks); }
        protected override void IntegrateForces(PhysicsDirectBodyState state) => Raise();
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
