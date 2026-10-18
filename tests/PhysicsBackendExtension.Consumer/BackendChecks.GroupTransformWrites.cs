using Electron2D;

public static partial class BackendChecks
{
    private static readonly Action<Entity>[] GroupTransformEdits =
    [
        n => n.Transform = new(.2f, new(1.2f, .8f), .05f, new(2, 3)), n => n.Position = new(5, 7), n => n.GlobalPosition = new(20, 30),
        n => n.Rotation = .3f, n => n.RotationDegrees = -25, n => n.GlobalRotation = .4f, n => n.GlobalRotationDegrees = 15,
        n => n.Scale = new(1.1f, .9f), n => n.GlobalScale = new(1.2f, 1.4f), n => n.Skew = .1f, n => n.GlobalSkew = -.2f,
        n => n.GlobalTransform = new(.12f, new(1.05f, .95f), .04f, new(13, 17)), n => n.Translate(new(2, 3)), n => n.GlobalTranslate(new(3, 2)),
        n => n.Rotate(.07f), n => n.ApplyScale(new(.9f, 1.1f)), n => n.MoveLocalX(.25f), n => n.MoveLocalX(.25f, true),
        n => n.MoveLocalY(-.25f), n => n.MoveLocalY(-.25f, true), n => n.LookAt(new(40, 50)),
        n => n.GlobalTransform = new(0, Vector2.One, 0, new(100, 120))
    ];
    private sealed class ReentrantTransform : Entity
    {
        internal bool Rewrite;
        public override Transform GetTransform()
        {
            var value = base.GetTransform();
            if (Rewrite) { Rewrite = false; Position += new Vector2(1, 2); }
            return value;
        }
    }
    private sealed class TransformWriter : Entity
    {
        internal Action? Write;
        internal int ThreadID;
        internal bool RejectChanges;
        internal int Validations;
        protected override void OnPhysicsProcess(double delta) { ThreadID = Environment.CurrentManagedThreadId; Write!(); }
        protected override void OnProcess(double delta) { ThreadID = Environment.CurrentManagedThreadId; Write!(); }
        protected override void ValidateMutation() { base.ValidateMutation(); Validations++; if (RejectChanges) throw new InvalidOperationException("Expected transform veto"); }
    }
    public static void RunGroupTransformWrites(PhysicsServer.Backend? backend, bool threaded, bool poolOwner = false)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("GroupTransformWrites", static () => new Solver());
        try
        {
            VerifyTransformBorrowRollback(backend);
            if (poolOwner) { var task = WorkerThreadPool.AddTask(() => GroupTransformWrites(backend, threaded, true)); WorkerThreadPool.WaitForTaskCompletion(task); }
            else GroupTransformWrites(backend, threaded, false);
        }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver); }
    }
    private static void GroupTransformWrites(PhysicsServer.Backend? backend, bool threaded, bool poolOwner)
    {
        using var world = backend is { } selected ? new World(selected) : new World("GroupTransformWrites");
        using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = .5f };
        var first = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
        var second = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
        var baseline = new Entity(); root.AddChild(baseline); root.AddChild(first); root.AddChild(second);
        Entity[] Build(Node parent, uint layer)
        {
            var rigid = new RigidBody { GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0, CollisionLayer = layer, CollisionMask = 0 };
            var circle = new CollisionShape { Shape = shape }; rigid.AddChild(circle);
            var polygon = new CollisionPolygon { Polygon = [new(-1, -1), new(1, -1), new(0, 1)] }; rigid.AddChild(polygon);
            Entity[] members = [new ReentrantTransform(), new Sprite(), new MeshInstance(), new Bone(), new StaticBody { CollisionLayer = 0 }, new CharacterBody { CollisionLayer = 0 }, new Area { CollisionLayer = 0 }, rigid, circle, polygon, new AnimatableBody { SyncToPhysics = false, CollisionLayer = 0 }];
            foreach (var member in members) if (member.Parent is null) parent.AddChild(member);
            foreach (var member in members) { member.NotifyLocalTransformChanges = true; member.NotifyTransformChanges = true; }
            return members;
        }
        var expectedNodes = Build(baseline, 0); var a = Build(first, 1); var b = Build(second, 2);
        var syncA = new AnimatableBody { CollisionLayer = 0 }; var syncB = new AnimatableBody { CollisionLayer = 0 }; first.AddChild(syncA); second.AddChild(syncB);
        var owner = Environment.CurrentManagedThreadId; var globalA = new int[a.Length]; var globalB = new int[b.Length];
        var trace = new Transform[a.Length * GroupTransformEdits.Length]; var counts = new int[a.Length]; var countA = new int[a.Length]; var countB = new int[a.Length];
        for (var i = 0; i < a.Length; i++)
        {
            var index = i; expectedNodes[i].LocalTransformChanged += _ => counts[index]++; a[i].LocalTransformChanged += _ => countA[index]++; b[i].LocalTransformChanged += _ => countB[index]++;
            a[i].TransformChanged += _ => { Check(Environment.CurrentManagedThreadId == owner, "First group global notifications deliver on the owner"); globalA[index]++; };
            b[i].TransformChanged += _ => { Check(Environment.CurrentManagedThreadId == owner, "Second group global notifications deliver on the owner"); globalB[index]++; };
        }
        var collect = new GroupTransformReader { ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread, ProcessThreadGroupOrder = -1 };
        root.AddChild(collect); using var tree = new SceneTree(root); tree.PhysicsInterpolation = true;
        using var query = new PhysicsPointQueryParameters { Position = new(100, 120), CollisionMask = 1 }; var hits = new PhysicsPointResult[4];
        var rid = ((RigidBody)a[7]).GetRID(); var identity = a[7].InstanceID; var cold = true; var failed = false;
        using var together = new Barrier(2);
        var expectedError = new ApplicationException("Expected local transform observer error"); var throwOnce = true;
        first.Write = () => Write(first, a, countA, syncA); second.Write = () => Write(second, b, countB, syncB);
        collect.Read = () =>
        {
            if (!failed) Check(world.DirectSpaceState.IntersectPoint(query, hits) == 2 && hits[0].ColliderRID == rid && hits[1].ColliderRID == rid,
                "Owner collection publishes worker body and both child shape transforms before numerical integration");
            ((RigidBody)a[7]).LinearVelocity = new(4, 0); ((RigidBody)b[7]).LinearVelocity = new(0, 5);
        };
        Prepare(); tree.PhysicsFrame(.01); Check(first.ThreadID != second.ThreadID && syncA.GlobalPosition.DistanceTo(new(70, 80)) < .0001f && syncB.GlobalPosition.DistanceTo(new(70, 80)) < .0001f, "Independent writers run concurrently and synchronized targets publish at the fixed step"); cold = false;
        Prepare(); tree.ProcessFrame(.01); Check(a[7].GlobalPosition.DistanceTo(new(100, 120)) < .0001f, "Idle worker transforms publish without an automatic numerical step");
        Action<CanvasItem> fail = _ => { if (throwOnce) { throwOnce = false; a[1].Position = new(25, 30); throw expectedError; } };
        Prepare(); a[0].LocalTransformChanged += fail; failed = true; var tick = PhysicsServer.SpaceGetTick(world.Space); var error = CaptureCopy(() => tree.PhysicsFrame(.01)); failed = false; a[0].LocalTransformChanged -= fail;
        Check(error is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(expectedError) && PhysicsServer.SpaceGetTick(world.Space) == tick + 1,
            "Local observer errors preserve the committed pose and still release every world before the actual step");
        Check(a[0].GlobalTransform.IsEqualApprox(a[0].Transform) && a[0].Position == new Vector2(2, 3) && a[1].Position == new Vector2(25, 30), "Observer reentry can mutate another own node and neither committed cache is stranded by its original error");
        using var outsideDone = new ManualResetEventSlim();
        var outside = WorkerThreadPool.AddTask(() =>
        {
            try { foreach (var edit in GroupTransformEdits) Reject<InvalidOperationException>(() => edit(a[0])); }
            finally { outsideDone.Set(); }
        }, highPriority: true);
        Check(outsideDone.Wait(TimeSpan.FromSeconds(5)), "An ordinary job completes its write guards without scene-owner help"); WorkerThreadPool.WaitForTaskCompletion(outside);
        for (var i = 0; i < 128; i++) Frame(); Collect(); for (var i = 0; i < 128; i++) Frame();
        tick = PhysicsServer.SpaceGetTick(world.Space); var ownerBytes = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame(); ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBytes; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(ownerBytes == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128, $"Worker transform writes full physics frames {ownerBytes}/{all} B");
        Check(world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null, "Transform publication preserves actual selected physics");
        Console.WriteLine($"Group transform writes {world.PhysicsBackendName}/{(threaded ? "separate" : "owner")}/{(poolOwner ? "pool-host" : "ordinary-host")}: complete spatial edit traces, callbacks, parallel shape/body publication and 128 complete physics/query intervals at 0/0 B passed.");
        void Prepare()
        {
            for (var i = 0; i < a.Length; i++)
            {
                expectedNodes[i].Transform = a[i].Transform = b[i].Transform = Transform.Identity;
                counts[i] = countA[i] = countB[i] = globalA[i] = globalB[i] = 0;
            }
            for (var i = 0; i < a.Length; i++)
                for (var j = 0; j < GroupTransformEdits.Length; j++) { GroupTransformEdits[j](expectedNodes[i]); trace[i * GroupTransformEdits.Length + j] = expectedNodes[i].GlobalTransform; }
        }
        void Write(TransformWriter writer, Entity[] nodes, int[] observed, AnimatableBody synchronized)
        {
            if (cold) Check(together.SignalAndWait(TimeSpan.FromSeconds(5)), "Independent transform callbacks do not hold a scene-wide lock");
            for (var i = 0; i < nodes.Length; i++)
                for (var j = 0; j < GroupTransformEdits.Length; j++)
                {
                    if (failed && ReferenceEquals(writer, first) && i == 0 && j == 0)
                    {
                        try { GroupTransformEdits[j](nodes[i]); } catch (ApplicationException caught) { Check(ReferenceEquals(caught, expectedError), "Immediate original observer exception propagates"); throw; }
                    }
                    GroupTransformEdits[j](nodes[i]);
                    Check(nodes[i].GlobalTransform.IsEqualApprox(trace[i * GroupTransformEdits.Length + j]), "Every own-group spatial setter/helper has the owner-side result");
                }
            for (var i = 0; i < observed.Length; i++) Check(observed[i] == counts[i], "Synchronous local notifications retain the owner trace");
            if (cold) { Guard(writer, nodes); var previous = synchronized.GlobalTransform; synchronized.GlobalPosition = new(70, 80); Check(synchronized.GlobalTransform == previous, "Synchronized worker targets preserve presentation until the numerical step"); }
        }
        void Guard(TransformWriter writer, Entity[] nodes)
        {
            Reject<InvalidOperationException>(() => baseline.Position = baseline.Position);
            Reject<InvalidOperationException>(() => ((RigidBody)nodes[7]).LinearVelocity = Vector2.One);
            Reject<InvalidOperationException>(() => world.DirectSpaceState.IntersectPoint(query, hits));
            Reject<InvalidOperationException>(() => tree.PhysicsFrame(.01));
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceCreateCheckpoint(world.Space));
            var reentrant = (ReentrantTransform)nodes[0]; var prior = reentrant.Transform; var global = reentrant.GlobalTransform;
            reentrant.Position = reentrant.Position; reentrant.Rewrite = true;
            Check(reentrant.GlobalTransform.IsEqualApprox(global) && reentrant.GlobalPosition.DistanceTo(global.Origin + new Vector2(1, 2)) < .0001f,
                "Reentrant getters return their observation without caching it across a later invalidation");
            reentrant.Transform = prior;
            var rootPose = writer.Transform; var bodyPosition = nodes[7].GlobalPosition;
            writer.Position += new Vector2(1, 2);
            Check(nodes[7].GlobalPosition.DistanceTo(bodyPosition + new Vector2(1, 2)) < .0001f, "An explicit group owner may move its complete physical subtree");
            writer.Transform = rootPose;
            var validations = writer.Validations; writer.Position = writer.Position; Check(writer.Validations == validations + 1, "A direct setter validates before its commit without a second virtual veto after mutation");
            writer.RejectChanges = true; var before = writer.Transform;
            Reject<InvalidOperationException>(() => writer.Position = writer.Position); Check(writer.Transform == before, "Derived validation rejects before changing pose"); writer.RejectChanges = false;
            Reject<ArgumentOutOfRangeException>(() => nodes[0].Position = new(float.NaN, 0));
        }

        void Frame()
        {
            Prepare(); tree.PhysicsFrame(.01); query.Position = a[7].GlobalPosition;
            Check(a[7].InstanceID == identity && ((RigidBody)a[7]).GetRID() == rid && MathF.Abs(a[7].GlobalPosition.X - 100.04f) < .0001f &&
                world.DirectSpaceState.IntersectPoint(query, hits) == 2, "Full worker edit/barrier/physics/query interval keeps actual movement and identity");
            for (var i = 0; i < a.Length; i++) Check(globalA[i] > 0 && globalB[i] > 0, "Concurrent queue insertion retains every own-group global notification");
            query.Position = new(100, 120);
        }
    }
    private static void VerifyTransformBorrowRollback(PhysicsServer.Backend? backend)
    {
        Solver? probe = null;
        PhysicsServerManager.RegisterServer("TransformBorrowProbe", () => probe = new Solver());
        using var firstWorld = backend is { } selected ? new World(selected) : new World("GroupTransformWrites");
        using var secondWorld = new World("TransformBorrowProbe");
        using var root = new SubViewport { World = firstWorld };
        var runs = 0; var writer = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, Write = () => runs++ }; root.AddChild(writer);
        using var tree = new SceneTree(root); _ = root.FindWorld(); _ = firstWorld.Space;
        var secondViewport = new SubViewport { World = secondWorld }; root.AddChild(secondViewport); _ = secondViewport.FindWorld(); _ = secondWorld.Space;
        var attempts = 0; using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
        probe!.PointHook = () =>
        {
            attempts++; var error = CaptureCopy(() => tree.ProcessFrame(.01));
            if (error is not AggregateException aggregate || !aggregate.Flatten().InnerExceptions.Any(e => e is InvalidOperationException))
                throw new InvalidOperationException("A later busy world must reject the stage after earlier space acquisition: " + error, error);
        };
        try { Check(secondWorld.DirectSpaceState.IntersectPoint(query, hits) == 0, "The real provider query completes after rejecting nested scene authoring"); }
        finally { probe.PointHook = null; }
        Check(attempts == 1 && runs == 0, "A failed acquisition starts no scene worker");
        Check(firstWorld.DirectSpaceState.IntersectPoint(query, hits) == 0, "Earlier space borrows roll back after later acquisition failure");
        tree.PhysicsFrame(.01);
        Check(runs == 1 && PhysicsServer.SpaceGetTick(firstWorld.Space) == 1 && PhysicsServer.SpaceGetTick(secondWorld.Space) == 1,
            "Both spaces remain usable for a subsequent joined stage and complete numerical steps");
    }
}
