using Electron2D;

public static partial class BackendChecks
{
    public static void RunQueuedDeletion(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("DeletionTickets", static () => new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("DeletionTickets");
            Check(world.PhysicsBackendName == (backend?.ToString() ?? "DeletionTickets") && world.PhysicsFallbackReason is null, "Queued deletion uses the selected world without fallback");
            DeletionOrder(world); DeletionBoundary(world); DeletionFamily(world); DeletionRaces(world); DeletionReferences(world); DeletionWarmFrames(world);
            Console.WriteLine($"Queued deletion {world.PhysicsBackendName}/{(threaded ? "separate" : "owner")}: ticket order, captured boundary, lifetime, transfer, concurrency and warm full frames passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private sealed class DeletionBody : RigidBody
    {
        internal int Ticks, Integrations;
        internal Action? Exit;
        protected override void OnPhysicsProcess(double delta) { base.OnPhysicsProcess(delta); Ticks++; }
        protected override void IntegrateForces(PhysicsDirectBodyState state) { base.IntegrateForces(state); Integrations++; }
        protected override void OnExitTree() { base.OnExitTree(); Exit?.Invoke(); }
    }

    private static DeletionBody CreateDeletionBody(CircleShape circle, float x)
    {
        var body = new DeletionBody { GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0, Position = new(x, 0), LinearVelocity = new(1, 0) };
        body.AddChild(new CollisionShape { Shape = circle }); return body;
    }

    private static void DeletionOrder(World world)
    {
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        var order = new List<int>(); var a = new Node(); var b = new Node(); root.AddChild(a); root.AddChild(b);
        var child = new Node(); a.AddChild(child); child.Disposed += _ => order.Add(3);
        a.Disposed += _ => order.Add(1); b.Disposed += _ => order.Add(2);
        a.QueueFree(); Check(!child.IsQueuedForDeletion, "A parent's request does not mark every descendant queued"); a.QueueFree(); b.QueueFree(); Check(a.CancelFree() && !a.CancelFree(), "Cancellation consumes exactly the current request");
        a.QueueFree(); tree.Defer(() => order.Add(4)); tree.FlushDeferred();
        Check(order.SequenceEqual([4, 2, 3, 1]), "A canceled old entry cannot consume a newly appended request ahead of its FIFO position");
        Check(!a.IsQueuedForDeletion && !b.IsQueuedForDeletion, "Completed nodes report no pending cancellable request");
        Reject<ObjectDisposedException>(a.QueueFree); Reject<ObjectDisposedException>(() => a.CancelFree());
        var explicitNode = new Node(); root.AddChild(explicitNode); tree.QueueDelete(explicitNode);
        Check(!explicitNode.CancelFree(), "The explicit deletion API retains its independent uncancellable contract");
        tree.FlushDeferred(); Check(explicitNode.IsDisposed, "Node cancellation cannot cancel an explicit QueueDelete");
        Reject<InvalidOperationException>(root.QueueFree);
        using var detached = new Node(); detached.QueueFree(); detached.CancelFree(); detached.QueueFree(); root.AddChild(detached);
        tree.FlushDeferred(); Check(detached.IsDisposed && !detached.IsQueuedForDeletion, "A detached reissued request follows its attachment");
    }

    private static void DeletionBoundary(World world)
    {
        using var circle = new CircleShape { Radius = 1 };
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        var a = CreateDeletionBody(circle, 100); var b = CreateDeletionBody(circle, 0); root.AddChild(a); root.AddChild(b);
        var rid = b.GetRID(); var retired = 0; b.Disposed += _ => retired++;
        a.SetBlockSignals(true); a.Exit = () => { Check(b.CancelFree(), "The later request is pending when the first body exits"); b.QueueFree(); };
        a.QueueFree(); b.QueueFree(); tree.PhysicsFrame(.01);
        Check(a.IsDisposed && !b.IsDisposed && b.IsQueuedForDeletion && b.Ticks == 1 && b.Integrations == 1 && b.Position.X > 0,
            "A new request during deletion waits for the next captured batch while the physical body remains live");
        using var point = new PhysicsPointQueryParameters { Position = b.GlobalPosition };
        Span<PhysicsPointResult> hit = new PhysicsPointResult[1];
        Check(world.DirectSpaceState.IntersectPoint(point, hit) == 1 && hit[0].ColliderID == b.InstanceID && PhysicsServer.BodyGetSpace(rid).IsValid(),
            "The deferred body retains its actual query identity and server space");
        tree.PhysicsFrame(.01);
        Check(b.IsDisposed && retired == 1 && b.Ticks == 2 && b.Integrations == 2 && !b.IsQueuedForDeletion, "The next full step runs before the new deletion, which retires once");
        Reject<ArgumentException>(() => PhysicsServer.BodyGetSpace(rid));

        var failing = new Node(); var next = new Node(); root.AddChild(failing); root.AddChild(next);
        failing.TreeExiting += _ => throw new ArithmeticException("Expected queued departure failure");
        failing.QueueFree(); next.QueueFree();
        Reject<AggregateException>(tree.FlushDeferred);
        Check(failing.IsDisposed && next.IsDisposed, "A failing departure does not stop later captured deletions");
        var retry = new Node(); root.AddChild(retry); retry.QueueFree(); tree.FlushDeferred(); Check(retry.IsDisposed, "A failed batch returns its prepared queue for later work");
    }

    private static void DeletionFamily(World world)
    {
        using var root = new SubViewport { World = world }; using var destination = new Node();
        using var tree = new SceneTree(root);
        Node[] family = [new Node(), new Entity(), new Control(), new StaticBody(), new AnimatableBody(), new RigidBody(), new CharacterBody(), new PhysicalBone(), new Area(), new CollisionShape()];
        foreach (var node in family)
        {
            root.AddChild(node); node.QueueFree(); Check(node.IsQueuedForDeletion && node.CancelFree() && !node.IsQueuedForDeletion, "Inherited cancellation applies to every neutral/canvas/physical role");
        }
        tree.FlushDeferred(); Check(family.All(n => !n.IsDisposed), "Canceled family entries remain alive");
        foreach (var node in family) node.QueueFree();
        tree.FlushDeferred(); Check(family.All(n => n.IsDisposed && !n.IsQueuedForDeletion), "All inherited roles retire exactly their current requests");
        var moved = new Node(); root.AddChild(moved); moved.QueueFree(); root.RemoveChild(moved);
        using var other = new SceneTree(destination); destination.AddChild(moved);
        Check(moved.CancelFree(), "The transferred request remains cancellable"); moved.QueueFree(); tree.FlushDeferred();
        Check(!moved.IsDisposed && moved.IsQueuedForDeletion && ReferenceEquals(moved.Tree, other), "An old owner cannot consume the new owner's reissued request");
        other.FlushDeferred(); Check(moved.IsDisposed && !moved.IsQueuedForDeletion, "The destination retires the transferred request");
        var committed = new DeletionBody(); root.AddChild(committed); var cancellations = 0;
        committed.Exit = () => { if (committed.CancelFree()) cancellations++; committed.QueueFree(); };
        committed.QueueFree(); tree.FlushDeferred(); tree.FlushDeferred();
        Check(committed.IsDisposed && !committed.IsQueuedForDeletion && cancellations == 0, "Requeueing inside a committed departure cannot cancel destruction or leave a disposed node queued");
    }

    private static void DeletionRaces(World world)
    {
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        for (var iteration = 0; iteration < 128; iteration++)
        {
            var node = new Node(); var retired = 0; node.Disposed += _ => retired++; root.AddChild(node);
            var producer = Task.Run(() =>
            {
                for (var i = 0; i < 128; i++)
                {
                    try { node.QueueFree(); node.CancelFree(); }
                    catch (ObjectDisposedException) { break; }
                }
            });
            while (!producer.IsCompleted) { tree.FlushDeferred(); Thread.Yield(); }
            producer.GetAwaiter().GetResult();
            if (!node.IsDisposed) node.QueueFree(); tree.FlushDeferred();
            Check(node.IsDisposed && !node.IsQueuedForDeletion && retired == 1, "Concurrent publication, cancellation and owner claims retire exactly once without stranded flags");
        }
    }

    private static void DeletionWarmFrames(World world)
    {
        using var circle = new CircleShape { Radius = 1 };
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        var body = CreateDeletionBody(circle, 0); root.AddChild(body);
        using var point = new PhysicsPointQueryParameters(); var hit = new PhysicsPointResult[1];
        var deferred = 0; Action callback = () => deferred++;
        for (var i = 0; i < 128; i++) Frame();
        Collect(); var before = (body.Ticks, body.Integrations, deferred); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame();
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Check(ownerBytes == 0 && allBytes == 0 && body.Ticks - before.Ticks == 128 && body.Integrations - before.Integrations == 128 && deferred - before.deferred == 128,
            $"Prepared canceled deletion, deferred FIFO, integration and point query in 128 full frames: {ownerBytes}/{allBytes} B");
        Console.WriteLine($"Queued deletion warm frames {world.PhysicsBackendName}: 128 full frames, owner/all-thread={ownerBytes}/{allBytes} B.");
        void Frame()
        {
            body.QueueFree(); body.QueueFree(); Check(body.CancelFree(), "A warm request is canceled once");
            body.QueueFree(); Check(body.CancelFree(), "A reissued warm request has its own identity");
            tree.Defer(callback); var x = body.Position.X; tree.PhysicsFrame(.01);
            Check(!body.IsDisposed && !body.IsQueuedForDeletion && body.Position.X > x, "A canceled body still physically integrates");
            point.Position = body.GlobalPosition;
            Check(world.DirectSpaceState.IntersectPoint(point, hit) == 1 && hit[0].ColliderID == body.InstanceID, "Each warm frame queries the actual surviving body");
        }
    }

    private static void DeletionReferences(World world)
    {
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        var deleted = QueueDeletionReference(tree, root, false); tree.FlushDeferred();
        Collect(); Check(!deleted.IsAlive, "A returned deletion queue releases its retired node reference");
        var pending = QueueDeletionReference(tree, root, true);
        Collect(); Check(pending.IsAlive, "An accepted generic request retains its target before closure");
        tree.Dispose(); Collect(); Check(!pending.IsAlive, "Finalization discards pending generic targets without retaining them in either prepared queue");
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference QueueDeletionReference(SceneTree tree, Node root, bool generic)
    {
        if (generic) { var resource = new Resource(); tree.QueueDelete(resource); return new WeakReference(resource); }
        var node = new Node(); root.AddChild(node); node.QueueFree(); return new WeakReference(node);
    }
}
