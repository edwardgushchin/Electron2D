using Electron2D;

internal static class EventConnectionPhysicsTests
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread,
            Environment.GetEnvironmentVariable("ELECTRON2D_TEST_THREADED_COLLIDER") == "1");
        DeferredZero(); Immediate(); DeferredPair(); HigherArities();
        using var world = new World(backend); using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 1 }; using var sensorShape = new CircleShape { Radius = 5 };
        var body = new PublisherBody { Position = new(100, 0), CanSleep = false, GravityScale = 0, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
        body.AddChild(new CollisionShape { Name = "DisabledSlot", Shape = shape, Disabled = true }); body.AddChild(new CollisionShape { Name = "ActiveSlot", Shape = shape }); var area = new Area { Monitoring = true };
        area.AddChild(new CollisionShape { Shape = sensorShape }); root.AddChild(body); root.AddChild(area); using var tree = new SceneTree(root);
        Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Actual selected physical backend without fallback.");
        var pulses = 0; var values = 0; var pairs = 0; var triples = 0; var quadruples = 0; var enters = 0; var exits = 0; var shapeEnters = 0; var shapeExits = 0;
        using var pulse = EventConnection.Subscribe(h => body.Pulse += h, h => body.Pulse -= h, () => pulses++);
        var deferredPulses = 0;
        using var deferredPulse = EventConnection.Subscribe(h => body.Pulse += h, h => body.Pulse -= h, () => deferredPulses++, defer: tree.Defer);
        using var value = EventConnection.Subscribe<PublisherBody>(h => body.Value += h, h => body.Value -= h, sender => { Check(ReferenceEquals(sender, body), "One-argument source identity."); values++; });
        using var pair = EventConnection.Subscribe<PublisherBody, int>(h => body.Pair += h, h => body.Pair -= h,
            (sender, tick) => { Check(ReferenceEquals(sender, body) && tick == body.Ticks, "Two-argument source and current tick."); pairs++; });
        using var enter = EventConnection.Subscribe<Entity>(h => area.BodyEntered += h, h => area.BodyEntered -= h, other => { Check(ReferenceEquals(other, body), "Area entered body identity."); enters++; });
        using var exit = EventConnection.Subscribe<Entity>(h => area.BodyExited += h, h => area.BodyExited -= h, other => { Check(ReferenceEquals(other, body), "Area exited body identity."); exits++; });
        using var triple = EventConnection.Subscribe<PublisherBody, int, Vector2>(h => body.Triple += h, h => body.Triple -= h,
            (sender, tick, position) => { Check(ReferenceEquals(sender, body) && tick == body.Ticks && position == body.Position, "Three-argument values."); triples++; });
        using var quadruple = EventConnection.Subscribe<PublisherBody, int, Vector2, float>(h => body.Quadruple += h, h => body.Quadruple -= h,
            (sender, tick, position, mass) => { Check(ReferenceEquals(sender, body) && tick == body.Ticks && position == body.Position && mass == body.Mass, "Four-argument values."); quadruples++; });
        using var shapeEnter = EventConnection.Subscribe<RID, Entity?, int, int>(h => area.BodyShapeEntered += h, h => area.BodyShapeEntered -= h,
            (rid, other, otherShape, localShape) => { ShapePayload(rid, other, otherShape, localShape); shapeEnters++; });
        using var shapeExit = EventConnection.Subscribe<RID, Entity?, int, int>(h => area.BodyShapeExited += h, h => area.BodyShapeExited -= h,
            (rid, other, otherShape, localShape) => { ShapePayload(rid, other, otherShape, localShape); shapeExits++; });
        var queue = new Queue<Action>(); var onceCalls = 0;
        using var once = EventConnection.Subscribe<RID, Entity?, int, int>(h => area.BodyShapeEntered += h, h => area.BodyShapeEntered -= h,
            (rid, other, otherShape, localShape) => { ShapePayload(rid, other, otherShape, localShape); onceCalls++; }, oneShot: true, defer: queue.Enqueue);
        Frame(0); Frame(1); Check(!once.IsConnected && onceCalls == 0 && queue.Count == 1, "Real shape one-shot captures once before deferred delivery.");
        while (queue.TryDequeue(out var action)) action(); Check(onceCalls == 1, "Actual RID, owner and flattened indices survive deferred dispatch.");
        root.PhysicsObjectPicking = true; area.InputPickable = false; body.InputPickable = true; body.Position = Vector2.Zero;
        var picked = 0;
        using (var picking = EventConnection.Subscribe<Node, InputEvent, int>(h => body.InputEvent += h, h => body.InputEvent -= h,
            (viewport, input, slot) => { Check(ReferenceEquals(viewport, root) && input is InputEventMouseMotion motion && !input.IsDisposed && motion.Position == new Vector2(.25f, .25f) && slot == 1, "Picking preserves viewport, live borrowed event and logical shape."); picked++; }))
        {
            using var mouse = new InputEventMouseMotion { Position = new(.25f, .25f), GlobalPosition = new(.25f, .25f) };
            root.PushInput(mouse, true); Check(picked == 0, "Typed picking waits for the physical pass."); tree.PhysicsFrame(.01); Check(picked == 1, "Actual three-argument physical input reaches its typed token.");
        }
        root.PhysicsObjectPicking = false; body.InputPickable = false; Frame(1);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        for (var i = 0; i < 128; i++) Frame(i);
        var first = (pulses, values, pairs, triples, quadruples, enters, exits, shapeEnters, shapeExits); var firstDeferred = deferredPulses; var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame(i);
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Check(ownerBytes == 0 && allBytes == 0, "Warmed full physics frames with immediate typed event connections allocate zero owner/all-thread bytes.");
        Check(deferredPulses - firstDeferred == 128, "The real scene safe point retains every deferred zero-argument physical callback.");
        Check(pulses - first.pulses == 128 && values - first.values == 128 && pairs - first.pairs == 128 && triples - first.triples == 128 && quadruples - first.quadruples == 128 && shapeEnters - first.shapeEnters == 64 && shapeExits - first.shapeExits == 64 && enters - first.enters == 64 && exits - first.exits == 64,
            "Every measured frame simulates and publishes all event arities; overlap events retain exact alternating transitions.");
        pulse.Dispose(); deferredPulse.Dispose(); value.Dispose(); pair.Dispose(); triple.Dispose(); quadruple.Dispose(); enter.Dispose(); exit.Dispose(); shapeEnter.Dispose(); shapeExit.Dispose();
        var counts = (pulses, values, pairs, triples, quadruples, enters, exits, shapeEnters, shapeExits); Frame(0); Frame(1); Check(counts == (pulses, values, pairs, triples, quadruples, enters, exits, shapeEnters, shapeExits), "Disposed physical subscriptions do not deliver later events.");
        var calls = 0;
        using (var failure = EventConnection.Subscribe<PublisherBody, int>(h => body.Pair += h, h => body.Pair -= h, (_, _) => { calls++; throw new ArithmeticException(); }))
        {
            Reject<AggregateException>(() => tree.PhysicsFrame(.01)); Check(calls == 1, "An immediate connection failure is collected without replaying its body callback.");
        }
        tree.PhysicsFrame(.01); Check(calls == 1 && !body.IsDisposed, "The next physical step remains usable after unsubscribing the failing handler.");
        Console.WriteLine($"Event connections {backend}: arities 0 through 4, actual Area body/shape transitions, identities, errors and 128 warm/128 measured full frames at {ownerBytes}/{allBytes} B passed.");
        void ShapePayload(RID rid, Entity? other, int otherShape, int localShape)
        { Check(rid == body.GetRID() && ReferenceEquals(other, body) && otherShape == 1 && localShape == 0, "Shape event preserves RID, owner and stable disabled-first indices."); }
        void Frame(int i) { body.Position = new(i % 2 == 0 ? 0 : 100, 0); body.LinearVelocity = new(1, 0); tree.PhysicsFrame(.01); Check(body.Position.X > (i % 2 == 0 ? 0 : 100), "The event fixture retains actual body integration."); }
    }

    private static void DeferredZero()
    {
        using var source = new PublisherBody(); var queue = new Queue<Action>(); var calls = 0;
        using var connection = EventConnection.Subscribe(h => source.Pulse += h, h => source.Pulse -= h, () => calls++, defer: queue.Enqueue);
        source.Raise(); source.Raise(); source.Raise(); Check(calls == 0 && queue.Count == 3, "Each zero-argument emission creates a separate queue entry.");
        while (queue.TryDequeue(out var action)) action(); Check(calls == 3, "A zero-argument batch retains every emission.");
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        for (var i = 0; i < 128; i++) Cycle(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Cycle(); var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Console.WriteLine($"Deferred zero-argument bracket: {ownerBytes}/{allBytes} B for 128 prepared emissions/queue drains.");
        Check(ownerBytes == 0 && allBytes == 0 && calls == 259, "Prepared deferred zero-argument delivery has no per-emission delegate allocation.");
        source.Raise(); connection.Dispose(); while (queue.TryDequeue(out var action)) action(); Check(calls == 259, "Cancellation leaves a queued callback inert.");
        void Cycle() { source.Raise(); Check(queue.Count == 1, "Deferred scheduling occurs for each emission."); while (queue.TryDequeue(out var action)) action(); }
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

    private static void HigherArities()
    {
        using var source = new PublisherBody(); var queue = new Queue<Action>(); var values = new List<int>();
        using var triple = EventConnection.Subscribe<PublisherBody, int, Vector2>(h => source.Triple += h, h => source.Triple -= h,
            (sender, tick, position) => { Check(ReferenceEquals(sender, source) && position == Vector2.Zero, "Deferred triple identity/value."); values.Add(tick); }, defer: queue.Enqueue);
        source.Raise(); source.Raise(); Check(values.Count == 0 && queue.Count == 2, "Triple deferred captures are distinct.");
        while (queue.TryDequeue(out var action)) action(); Check(values.SequenceEqual([1, 2]), "Triple payload order.");
        source.Raise(); triple.Dispose(); while (queue.TryDequeue(out var action)) action(); Check(values.Count == 2, "Triple disposal cancels pending delivery.");
        using var quad = EventConnection.Subscribe<PublisherBody, int, Vector2, float>(h => source.Quadruple += h, h => source.Quadruple -= h,
            (sender, tick, position, mass) => { Check(ReferenceEquals(sender, source) && position == Vector2.Zero && mass == 1, "Quadruple snapshot."); values.Add(tick); }, oneShot: true, defer: queue.Enqueue);
        source.Raise(); source.Raise(); Check(!quad.IsConnected && queue.Count == 1, "Quadruple one-shot detaches before scheduling.");
        while (queue.TryDequeue(out var action)) action(); Check(values.SequenceEqual([1, 2, 4]), "Quadruple deferred first payload.");
        var calls = 0; using var recursive = EventConnection.Subscribe<PublisherBody, int, Vector2>(h => source.Triple += h, h => source.Triple -= h,
            (_, _, _) => { calls++; source.Raise(); }, oneShot: true);
        source.Raise(); Check(calls == 1 && !recursive.IsConnected, "Triple one-shot is recursion-safe.");
        var nextTick = source.Ticks + 1; var reusableCalls = 0;
        using var reusable = EventConnection.Subscribe<PublisherBody, int, Vector2, float>(h => source.Quadruple += h, h => source.Quadruple -= h,
            (_, tick, position, mass) => { Check(tick == nextTick++ && position == Vector2.Zero && mass == 1, "Reusable quadruple captures ordered values."); reusableCalls++; }, defer: queue.Enqueue);
        source.Raise(); source.Raise(); while (queue.TryDequeue(out var action)) action(); Check(reusableCalls == 2, "Reusable quadruple dispatches each captured emission.");
        source.Raise(); reusable.Dispose(); while (queue.TryDequeue(out var action)) action(); Check(reusableCalls == 2, "Pending reusable quadruple cancels.");
        using var cancelled = EventConnection.Subscribe<PublisherBody, int, Vector2>(h => source.Triple += h, h => source.Triple -= h,
            (_, _, _) => throw new InvalidOperationException("cancelled triple"), oneShot: true, defer: queue.Enqueue);
        source.Raise(); cancelled.Dispose(); while (queue.TryDequeue(out var action)) action();
        Reject<ArgumentNullException>(() => EventConnection.Subscribe<int, int, int>(null!, _ => { }, (_, _, _) => { }));
        Reject<ArgumentNullException>(() => EventConnection.Subscribe<int, int, int>(_ => { }, null!, (_, _, _) => { }));
        Reject<ArgumentNullException>(() => EventConnection.Subscribe<int, int, int, int>(null!, _ => { }, (_, _, _, _) => { }));
        Reject<ArgumentNullException>(() => EventConnection.Subscribe<int, int, int, int>(_ => { }, null!, (_, _, _, _) => { }));
        Reject<ArgumentNullException>(() => EventConnection.Subscribe<int, int, int>(_ => { }, _ => { }, null!));
        Reject<ArgumentNullException>(() => EventConnection.Subscribe<int, int, int, int>(_ => { }, _ => { }, null!));
        Action<int, int, int>? partial = null;
        Reject<ArithmeticException>(() => EventConnection.Subscribe<int, int, int>(h => { partial += h; throw new ArithmeticException(); }, h => partial -= h, (_, _, _) => { }));
        Check(partial is null, "Triple partial attachment rolls back.");
        Action<int, int, int, int>? retained = null; var retainedCalls = 0;
        var removalFailure = EventConnection.Subscribe<int, int, int, int>(h => retained += h, _ => throw new ArithmeticException(), (_, _, _, _) => retainedCalls++);
        Reject<ArithmeticException>(removalFailure.Dispose); retained?.Invoke(1, 2, 3, 4); removalFailure.Dispose();
        Check(!removalFailure.IsConnected && retainedCalls == 0, "Quadruple failed removal keeps the retained wrapper terminal and inert.");
    }

    private sealed class PublisherBody : RigidBody
    {
        internal event Action? Pulse; internal event Action<PublisherBody>? Value; internal event Action<PublisherBody, int>? Pair; internal event Action<PublisherBody, int, Vector2>? Triple; internal event Action<PublisherBody, int, Vector2, float>? Quadruple; internal int Ticks;
        internal void Raise() { Ticks++; Pulse?.Invoke(); Value?.Invoke(this); Pair?.Invoke(this, Ticks); Triple?.Invoke(this, Ticks, Position); Quadruple?.Invoke(this, Ticks, Position, Mass); }
        protected override void IntegrateForces(PhysicsDirectBodyState state) => Raise();
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
