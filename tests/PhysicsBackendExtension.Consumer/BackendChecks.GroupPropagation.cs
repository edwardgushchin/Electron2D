using Electron2D;

public static partial class BackendChecks
{
    private const int PropagationNotice = 9103;
    private sealed class PropagationProbe : Node
    {
        internal Action? Callback, Capturing;
        internal int Notices, LastThread;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what != PropagationNotice) return;
            Notices++; LastThread = Environment.CurrentManagedThreadId; Callback?.Invoke();
        }
        protected override Func<Node> CreateSceneInstanceFactory() { Capturing?.Invoke(); return Create; }
        private static Node Create() => new PropagationProbe();
    }
    public static void RunGroupPropagation(PhysicsServer.Backend? backend, bool threaded, bool poolOwner = false)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("GroupPropagation", static () => new Solver());
        try
        {
            if (poolOwner) { var task = WorkerThreadPool.AddTask(() => GroupPropagation(backend, threaded)); WorkerThreadPool.WaitForTaskCompletion(task); }
            else GroupPropagation(backend, threaded);
            using var source = new PropagationProbe(); using var packed = new PackedScene(); var captures = 0;
            source.Callback = () => { captures++; Reject<InvalidOperationException>(() => source.Name = "Captured"); };
            source.Capturing = () => source.PropagateNotification(PropagationNotice); packed.Pack(source); source.Capturing = null;
            Check(captures > 0, "Pure custom notification remains observable during capture while the callback cannot mutate the source");
            source.Dispose(); Reject<ObjectDisposedException>(() => source.PropagateNotification(PropagationNotice));
        }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver); }
    }
    private static void GroupPropagation(PhysicsServer.Backend? backend, bool threaded)
    {
        using var world = backend is { } selected ? new World(selected) : new World("GroupPropagation");
        using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 1 };
        var a = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
        var b = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
        root.AddChild(a); root.AddChild(b);
        var body = new RigidBody { GravityScale = 0, CanSleep = false, CollisionMask = 0 }; var collision = new CollisionShape { Shape = shape };
        var polygon = new CollisionPolygon { Position = new(10, 0), Polygon = [new(-1, -1), new(1, -1), new(0, 1)] };
        body.AddChild(collision); body.AddChild(polygon); a.AddChild(body);
        Node[] roles = [body, collision, polygon, new Area { CollisionLayer = 0 }, new StaticBody { CollisionLayer = 0 }, new CharacterBody { CollisionLayer = 0 }, new AnimatableBody { CollisionLayer = 0 }, new PinJoint(), new RayCast { Enabled = false }, new ShapeCast { Enabled = false }, new PhysicalBone { AutoConfigureJoint = false }, new Bone(), new Control(), new Camera(), new MultiMeshInstance(), new TileMapLayer(), new CPUParticles(), new Electron2D.Timer()];
        var probes = new PropagationProbe[roles.Length];
        for (var i = 0; i < roles.Length; i++) { if (roles[i].Parent is null) a.AddChild(roles[i]); probes[i] = new(); roles[i].AddChild(probes[i]); }
        body.MoveChild(probes[0], 0);
        var foreign = new PropagationProbe { ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread }; var hidden = new PropagationProbe(); foreign.AddChild(hidden); a.AddChild(foreign);
        var last = new PropagationProbe(); a.AddChild(last); var other = new PropagationProbe(); b.AddChild(other);
        var collector = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread }; root.AddChild(collector);
        using var tree = new SceneTree(root); var ownerID = body.InstanceID; var rid = body.GetRID(); var shapeOwners = body.GetShapeOwners();
        using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[2]; using var together = new Barrier(2);
        var cold = true; var phase = 0; var inCallback = false; var sequence = 0;
        for (var i = 0; i < probes.Length; i++) { var index = i; probes[i].Callback = () => { Check(inCallback && probes[index].LastThread == a.ThreadID && sequence++ == index, "Reserved roles preserve synchronous parent-first custom delivery"); }; }
        last.Callback = () => Check(inCallback && sequence++ == probes.Length, "Later own-group siblings run after a rejected foreign branch");
        a.Write = () =>
        {
            inCallback = true; sequence = 0;
            try
            {
                if (cold)
                {
                    var error = CaptureCopy(() => a.PropagateNotification(PropagationNotice));
                    Check(error is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Any(e => e is InvalidOperationException) && foreign.Notices == 0 && hidden.Notices == 0 && sequence == probes.Length + 1,
                        "Foreign explicit-group notification branch rejects without callbacks while later siblings continue");
                    Guard();
                }
                else a.PropagateNotification(PropagationNotice);
                phase++; body.Position = new(phase * .01f, 20);
                body.PropagateNotification(Node.NotificationResetPhysicsInterpolation);
            }
            finally { inCallback = false; }
        };
        b.Write = () => { if (cold && !together.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Propagation groups did not overlap"); other.PropagateNotification(PropagationNotice); };
        collector.Write = () =>
        {
            query.Position = collision.GlobalPosition;
            Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderObject == body && hits[0].ColliderRID == rid && body.InstanceID == ownerID, "Owner query observes actual moving body identity after the notification stage");
        };
        tree.PhysicsFrame(.01); Check(a.ThreadID != b.ThreadID, "Independent group notifications execute concurrently"); cold = false;
        a.RemoveChild(foreign); foreign.Dispose();
        tree.ProcessFrame(.01);
        var failure = new ApplicationException("Expected propagated callback failure"); var normal = probes[0].Callback; var throwOnce = true;
        probes[0].Callback = () => { normal!(); if (throwOnce) { throwOnce = false; throw failure; } };
        var tick = PhysicsServer.SpaceGetTick(world.Space); var failed = CaptureCopy(() => tree.PhysicsFrame(.01));
        Check(failed is AggregateException errors && errors.Flatten().InnerExceptions.Contains(failure) && PhysicsServer.SpaceGetTick(world.Space) == tick + 1 && last.Notices > 2, "Callback failure joins workers, continues later descendants and preserves the selected physics step");
        probes[0].Callback = normal;
        for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01); Collect(); for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
        tick = PhysicsServer.SpaceGetTick(world.Space); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
        own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(own == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128, $"Group propagation complete physics/query intervals allocated {own}/{all} B");
        Check(body.GetShapeOwners().SequenceEqual(shapeOwners) && world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null, "Manual notification guards preserve geometry owners and selected backend identity");
        using var outsideDone = new ManualResetEventSlim(); var task = WorkerThreadPool.AddTask(() => { try { Reject<InvalidOperationException>(() => a.PropagateNotification(PropagationNotice)); } finally { outsideDone.Set(); } }, highPriority: true);
        Check(outsideDone.Wait(TimeSpan.FromSeconds(5)), "An ordinary job runs before owner assistance"); WorkerThreadPool.WaitForTaskCompletion(task);
        Console.WriteLine($"Group propagation {world.PhysicsBackendName}/{(threaded ? "separate" : "owner")}: ordered synchronous callbacks, foreign/capture/lifetime/error guards, stable geometry and 128 complete physics/query intervals at {own}/{all} B passed.");
        void Guard()
        {
            if (!together.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Propagation groups did not overlap");
            Reject<InvalidOperationException>(() => other.PropagateNotification(PropagationNotice));
            var saved = probes[0].Callback; var entered = false; var count = probes[0].Notices;
            probes[0].Callback = () => { if (entered) return; entered = true; probes[0].PropagateNotification(PropagationNotice); };
            probes[0].PropagateNotification(PropagationNotice); probes[0].Callback = saved;
            Check(entered && probes[0].Notices == count + 2, "Recursive propagation runs synchronously outside common gates");
            Reject<InvalidOperationException>(() => a.RemoveChild(roles[3])); Reject<InvalidOperationException>(probes[0].Dispose);

            foreach (var geometry in new Node[] { collision, polygon })
            {
                foreach (var notification in new[] { Node.NotificationParented, Node.NotificationUnparented })
                {
                    var error = CaptureCopy(() => geometry.PropagateNotification(notification));
                    Check(error is AggregateException aggregate && aggregate.Flatten().InnerExceptions.All(e => e is InvalidOperationException) && ReferenceEquals(geometry.Parent, body), "Unsupported physical reparent notification fails before altering geometry ownership");
                }
            }
            collision.Position = new(30, 0); polygon.Position = new(50, 0);
        }
    }
}
