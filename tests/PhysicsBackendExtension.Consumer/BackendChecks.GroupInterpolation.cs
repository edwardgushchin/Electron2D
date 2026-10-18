using Electron2D;

public static partial class BackendChecks
{
    private sealed class InterpolationProbe : Entity
    {
        internal int Resets;
        internal Action? Reset, Capturing;
        internal bool Veto;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationResetPhysicsInterpolation) { Resets++; Reset?.Invoke(); }
        }
        protected override void ValidateMutation() { base.ValidateMutation(); if (Veto) throw new InvalidOperationException("Interpolation veto"); }
        protected override Func<Node> CreateSceneInstanceFactory() { Capturing?.Invoke(); return Create; }
        private static Node Create() => new InterpolationProbe();
    }
    public static void RunGroupInterpolation(PhysicsServer.Backend? backend, bool threaded, bool poolOwner = false)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, poolOwner ? 3 : 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("GroupInterpolation", static () => new Solver());
        try
        {
            if (poolOwner) { var task = WorkerThreadPool.AddTask(() => GroupInterpolation(backend, threaded, true)); WorkerThreadPool.WaitForTaskCompletion(task); }
            else GroupInterpolation(backend, threaded, false);
            using var probe = new InterpolationProbe(); using var packed = new PackedScene();
            probe.Capturing = () => { Reject<InvalidOperationException>(probe.ResetPhysicsInterpolation); Reject<InvalidOperationException>(() => probe.PhysicsInterpolationMode = probe.PhysicsInterpolationMode); };
            packed.Pack(probe); probe.Capturing = null;
            probe.Veto = true; Reject<InvalidOperationException>(probe.ResetPhysicsInterpolation); Reject<InvalidOperationException>(() => probe.PhysicsInterpolationMode = PhysicsInterpolationMode.On); probe.Veto = false;
            probe.ResetPhysicsInterpolation(); Check(probe.Resets == 0, "Detached reset is silent");
            Reject<ArgumentOutOfRangeException>(() => probe.PhysicsInterpolationMode = (PhysicsInterpolationMode)99);
            Check(probe.PhysicsInterpolationMode == PhysicsInterpolationMode.Inherit, "Invalid policy preserves the stored value");
            probe.Dispose(); Reject<ObjectDisposedException>(probe.ResetPhysicsInterpolation); Reject<ObjectDisposedException>(() => probe.PhysicsInterpolationMode = PhysicsInterpolationMode.On);
        }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver); }
    }
    private static void GroupInterpolation(PhysicsServer.Backend? backend, bool threaded, bool poolOwner)
    {
        using var world = backend is { } selected ? new World(selected) : new World("GroupInterpolation");
        using var root = new SubViewport { World = world, Size = new(128, 80) }; using var shape = new CircleShape { Radius = 1 };
        var a = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
        var b = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
        root.AddChild(a); root.AddChild(b);
        var first = Members(a, shape, 1); var second = Members(b, shape, 2);
        var collector = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread }; root.AddChild(collector);
        using var tree = new SceneTree(root); tree.PhysicsInterpolation = true;
        var firstProbe = (InterpolationProbe)first[0]; var secondProbe = (InterpolationProbe)second[0];
        var body = (RigidBody)first[5]; var rid = body.GetRID(); var identity = body.InstanceID;
        using var query = new PhysicsPointQueryParameters { CollisionMask = 1 }; var hits = new PhysicsPointResult[2];
        using var together = new Barrier(2); var cold = true; var observed = 0;
        a.Write = () => Write(a, first); b.Write = () => Write(b, second);
        collector.Write = () =>
        {
            observed++; query.Position = body.GlobalPosition;
            Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == rid && hits[0].ColliderObject == body,
                "Owner collection sees the same body identity after interpolation metadata and resets");
        };
        firstProbe.Reset = () => Check(Environment.CurrentManagedThreadId == a.ThreadID, "First reset runs immediately on its own group");
        secondProbe.Reset = () => Check(Environment.CurrentManagedThreadId == b.ThreadID, "Second reset runs immediately on its own group");
        tree.PhysicsFrame(.01); Check(a.ThreadID != b.ThreadID, "Two independent groups actually overlap"); cold = false;
        tree.ProcessFrame(.01);
        var foreign = new InterpolationProbe { ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread }; var later = new InterpolationProbe();
        a.AddChild(foreign); a.AddChild(later); var nested = new InterpolationProbe(); foreign.AddChild(nested);
        a.Write = () =>
        {
            var before = later.Resets; var foreignBefore = foreign.Resets; var nestedBefore = nested.Resets;
            var error = CaptureCopy(a.ResetPhysicsInterpolation);
            Check(error is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Any(e => e is InvalidOperationException) && later.Resets == before + 1 && foreign.Resets == foreignBefore && nested.Resets == nestedBefore,
                "A foreign group branch rejects without callbacks while later own-group descendants reset");
            a.PhysicsInterpolationMode = PhysicsInterpolationMode.Off;
            error = CaptureCopy(() => a.PhysicsInterpolationMode = PhysicsInterpolationMode.On);
            Check(error is AggregateException && a.PhysicsInterpolationMode == PhysicsInterpolationMode.On && foreign.IsPhysicsInterpolatedAndEnabled(),
                "Policy commits across inherited foreign branches even when their reset notifications reject");
        };
        tree.PhysicsFrame(.01); a.RemoveChild(foreign); foreign.Dispose(); a.RemoveChild(later); later.Dispose();
        var failure = new ApplicationException("Expected interpolation observer failure"); var failOnce = true;
        firstProbe.Reset = () => { if (failOnce) { failOnce = false; throw failure; } };
        a.Write = a.ResetPhysicsInterpolation; var tick = PhysicsServer.SpaceGetTick(world.Space);
        var failedFrame = CaptureCopy(() => tree.PhysicsFrame(.01));
        Check(failedFrame is AggregateException errors && errors.Flatten().InnerExceptions.Contains(failure) && PhysicsServer.SpaceGetTick(world.Space) == tick + 1,
            "Observer errors preserve identity, join workers, publish histories and release the selected numerical space");
        var reentered = false; firstProbe.Reset = () => { if (!reentered) { reentered = true; firstProbe.PhysicsInterpolationMode = PhysicsInterpolationMode.Off; firstProbe.ResetPhysicsInterpolation(); } };
        tree.PhysicsFrame(.01); Check(reentered, "A callback may change its policy and recursively reset outside the queue gate");
        firstProbe.Reset = null; secondProbe.Reset = null; a.Write = () => Write(a, first);
        using var outsideDone = new ManualResetEventSlim();
        var outside = WorkerThreadPool.AddTask(() =>
        {
            try { Reject<InvalidOperationException>(body.ResetPhysicsInterpolation); Reject<InvalidOperationException>(() => body.PhysicsInterpolationMode = body.PhysicsInterpolationMode); }
            finally { outsideDone.Set(); }
        }, highPriority: true);
        Check(outsideDone.Wait(TimeSpan.FromSeconds(5)), "An ordinary worker runs without scene-owner helper permission"); WorkerThreadPool.WaitForTaskCompletion(outside);
        for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01); Collect(); for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
        tick = PhysicsServer.SpaceGetTick(world.Space); var ownerBytes = GC.GetAllocatedBytesForCurrentThread(); var allBytes = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
        ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBytes; allBytes = GC.GetTotalAllocatedBytes(true) - allBytes;
        Check(ownerBytes == 0 && allBytes == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128, $"Own-group policy/reset full physics/query intervals allocated {ownerBytes}/{allBytes} B");
        Check(observed > 384 && body.InstanceID == identity && body.GetRID() == rid && world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null,
            "Full-step histories preserve stable identities and the selected backend");
        Console.WriteLine($"Group interpolation {world.PhysicsBackendName}/{(threaded ? "separate" : "owner")}/{(poolOwner ? "pool-host" : "ordinary-host")}: immediate resets, policy/inheritance, foreign branches, callback errors/reentry, camera scheduling and 128 complete physics/query intervals at {ownerBytes}/{allBytes} B passed.");
        void Write(TransformWriter writer, Node[] own)
        {
            if (cold) Guard(ReferenceEquals(writer, a) ? second : first);
            ((RigidBody)own[5]).Position += new Vector2(.01f, 0);
            foreach (var node in own)
            {
                node.PhysicsInterpolationMode = PhysicsInterpolationMode.Off; Check(!node.IsPhysicsInterpolated(), "Own-group Off commits immediately");
                node.PhysicsInterpolationMode = PhysicsInterpolationMode.On; Check(node.IsPhysicsInterpolatedAndEnabled(), "Own-group On commits immediately");
                node.PhysicsInterpolationMode = PhysicsInterpolationMode.Inherit; node.ResetPhysicsInterpolation();
            }
            var probe = (InterpolationProbe)own[0]; var count = probe.Resets; writer.ResetPhysicsInterpolation(); Check(probe.Resets == count + 1, "Recursive reset reaches inheriting descendants immediately");
        }
        void Guard(Node[] other)
        {
            if (!together.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Group interpolation overlap");
            Reject<InvalidOperationException>(other[0].ResetPhysicsInterpolation); Reject<InvalidOperationException>(() => other[0].PhysicsInterpolationMode = PhysicsInterpolationMode.Off);
            Reject<InvalidOperationException>(() => body.LinearVelocity = Vector2.One);
        }
        static Node[] Members(TransformWriter parent, Shape shape, uint layer)
        {
            var body = new RigidBody { Position = new(layer * 20, 20), GravityScale = 0, CanSleep = false, CollisionLayer = layer, CollisionMask = 0 };
            var collision = new CollisionShape { Shape = shape }; body.AddChild(collision);
            Node[] nodes = [new InterpolationProbe(), new Node(), new Control(), new StaticBody { CollisionLayer = 0 }, new Area { CollisionLayer = 0 }, body, collision,
                new CollisionPolygon { Polygon = [new(-1, -1), new(1, -1), new(0, 1)] }, new CharacterBody { CollisionLayer = 0 }, new AnimatableBody { CollisionLayer = 0 },
                new Camera { AnchorMode = AnchorMode.FixedTopLeft, LimitEnabled = false }, new CPUParticles(), new MultiMeshInstance(), new TileMapLayer(), new Bone()];
            foreach (var node in nodes) if (node.Parent is null) parent.AddChild(node);
            return nodes;
        }
    }
}
