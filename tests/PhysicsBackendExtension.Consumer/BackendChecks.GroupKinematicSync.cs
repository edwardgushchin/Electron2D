using Electron2D;

public static partial class BackendChecks
{
    public static void RunGroupKinematicSync(PhysicsServer.Backend? backend, bool threaded, bool poolOwner = false)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads);
        var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            if (poolOwner)
            {
                var task = WorkerThreadPool.AddTask(() => GroupKinematicSync(backend, threaded, true));
                WorkerThreadPool.WaitForTaskCompletion(task);
            }
            else GroupKinematicSync(backend, threaded, false);
            using var capture = new PropagationProbe();
            var source = new AnimatableBody { Name = "Platform", SyncToPhysics = false };
            capture.AddChild(source); source.Owner = capture;
            using var packed = new PackedScene(); var captures = 0;
            capture.Capturing = () =>
            {
                captures++;
                Check(!source.SyncToPhysics, "Capture may observe the configured synchronization policy");
                Reject<InvalidOperationException>(() => source.SyncToPhysics = false);
                Reject<InvalidOperationException>(() => source.SyncToPhysics = true);
            };
            packed.Pack(capture); capture.Capturing = null;
            using var copy = packed.Instantiate();
            Check(captures > 0 && copy.GetNode<AnimatableBody>("Platform").SyncToPhysics == false,
                "Equal and changed capture writes reject before stored configuration changes");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads);
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver);
        }
    }

    private static void GroupKinematicSync(PhysicsServer.Backend? backend, bool threaded, bool poolOwner)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("GroupKinematicSync", () => solver = new Solver());
        using var world = backend is { } selected ? new World(selected) : new World("GroupKinematicSync");
        using var root = new SubViewport { World = world };
        using var shape = new RectangleShape { Size = new(120, 20) };
        using var riderShape = new RectangleShape { Size = new(20, 20) };
        var writer = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread };
        var other = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread };
        var body = new AnimatableBody { Position = new(0, 100), CollisionLayer = 1, CollisionMask = 2 };
        body.AddChild(new CollisionShape { Shape = shape }); writer.AddChild(body);
        var rider = new RigidBody { Position = new(0, 80), CollisionLayer = 2, CollisionMask = 1, CanSleep = false, LockRotation = true };
        rider.AddChild(new CollisionShape { Shape = riderShape });
        root.AddChild(writer); root.AddChild(other); root.AddChild(rider);
        using var tree = new SceneTree(root);
        var owner = Environment.CurrentManagedThreadId;
        var policy = body.GetPropertyList().OfType<PropertyDescriptor<AnimatableBody, bool>>().Single(p => p.Name == nameof(AnimatableBody.SyncToPhysics));
        var rid = body.GetRID(); var identity = body.InstanceID;
        var mode = 0; var localCalls = 0; var cold = true;
        using var together = new Barrier(2);
        var expectedFailure = new ApplicationException("Expected synchronized-target observer failure");
        var fail = false;
        body.LocalTransformChanged += _ =>
        {
            Check(Environment.CurrentManagedThreadId == writer.ThreadID && body.SyncToPhysics,
                "Local kinematic notifications observe synchronization immediately on their current group");
            localCalls++;
            if (fail) { fail = false; throw expectedFailure; }
        };
        writer.Write = () =>
        {
            if (cold)
            {
                Check(together.SignalAndWait(TimeSpan.FromSeconds(5)), "Independent kinematic groups run outside a scene-wide lock");
                Reject<InvalidOperationException>(() => body.SetIndexed(policy, true));
            }
            if (mode == 0) return;
            var prior = body.GlobalPosition;
            Check(body.SyncToPhysics, "Own-group synchronization reads remain available");
            body.SyncToPhysics = true;
            body.GlobalPosition = prior + new Vector2(.125f, 0);
            Check(body.GlobalPosition == prior, "A synchronized target is not presented early");
            body.SetThreadSafe<AnimatableBody, bool>(static (node, value) => node.SyncToPhysics = value, false);
            Check(!body.SyncToPhysics && body.GlobalPosition == prior + new Vector2(.125f, 0),
                "Disabling synchronization presents the pending target in the same worker callback");
            body.CallThreadSafe<AnimatableBody>(static node => node.SyncToPhysics = true);
            body.GlobalPosition = prior + new Vector2(.25f, 0);
            Check(body.SyncToPhysics && body.GlobalPosition == prior + new Vector2(.125f, 0),
                "Reenabling synchronization stages the next target independently");
        };
        var move = writer.Write;
        other.Write = () =>
        {
            if (!cold) return;
            Check(together.SignalAndWait(TimeSpan.FromSeconds(5)), "A second current group participates independently");
            Reject<InvalidOperationException>(() => _ = body.SyncToPhysics);
            Reject<InvalidOperationException>(() => body.SyncToPhysics = true);
            Reject<InvalidOperationException>(() => body.SyncToPhysics = false);
        };
        tree.PhysicsFrame(.01); cold = false;
        for (var i = 0; i < 90; i++) tree.PhysicsFrame(1d / 60);
        Check(rider.GlobalPosition.Y is > 77 and < 83, "A resting dynamic rider is supported before changing worker policy");
        mode = 1;
        var tick = PhysicsServer.SpaceGetTick(world.Space);
        tree.PhysicsFrame(0);
        Check(body.GlobalPosition == new Vector2(.125f, 100) && PhysicsServer.SpaceGetTick(world.Space) == tick,
            "A zero-duration frame retains the synchronized target while committing the disabled presentation");
        mode = 0; tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.DistanceTo(new(.25f, 100)) < .02f,
            "The owner consumes a target staged by a worker on the next nonzero fixed step");
        mode = 1; tree.ProcessFrame(.01);
        Check(body.GlobalPosition.DistanceTo(new(.375f, 100)) < .02f,
            "Idle workers can switch presentation policy without advancing physics");
        mode = 0; tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.DistanceTo(new(.5f, 100)) < .02f, "Idle-authored targets reach the same numerical world");

        writer.Write = () => { body.GlobalPosition += new Vector2(.25f, 0); };
        var beforeFailure = body.GlobalPosition;
        fail = true;
        var error = CaptureCopy(() => tree.PhysicsFrame(1d / 60));
        Check(error is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Any(e => ReferenceEquals(e, expectedFailure)),
            "The original worker notification error reaches the frame caller");
        writer.Write = static () => { };
        tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.DistanceTo(beforeFailure + new Vector2(.25f, 0)) < .02f,
            "A throwing local observer does not erase its valid pending target");
        mode = 1; writer.Write = move;
        using var query = new PhysicsPointQueryParameters { CollisionMask = 1 };
        var hits = new PhysicsPointResult[2];
        void Frame()
        {
            tree.PhysicsFrame(1d / 60);
            query.Position = body.GlobalPosition;
            Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == rid &&
                  hits[0].ColliderID == identity && body.InstanceID == identity && body.GetRID() == rid,
                "Full numerical frames and direct queries preserve the selected body and collision identity");
        }
        for (var i = 0; i < 128; i++) Frame();
        Collect(); for (var i = 0; i < 128; i++) Frame();
        tick = PhysicsServer.SpaceGetTick(world.Space);
        var measuredPosition = body.GlobalPosition;
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame();
        ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBytes; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(ownerBytes == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128,
            $"Worker kinematic synchronization complete physics/query frames {ownerBytes}/{all} B");
        Check(body.GlobalPosition.DistanceTo(measuredPosition + new Vector2(32, 0)) < .05f,
            "The measured interval advances every worker-authored target through the numerical backend");
        Check(localCalls > 128 && (poolOwner || writer.ThreadID != owner && other.ThreadID != owner),
            "Real processing workers execute the synchronous kinematic callbacks");
        Check(rider.GlobalPosition.X > 5, "Worker-authored kinematic motion retains actual dynamic contact response");
        Check(world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null,
            "Kinematic policy edits preserve the explicitly selected backend");
        writer.Write = static () => { };
        if (solver is not null)
        {
            var numericalCalls = 0;
            solver.StepHook = () =>
            {
                numericalCalls++;
                Check((Environment.CurrentManagedThreadId != owner) == threaded, "The registered numerical hook uses its captured solver thread");
                Reject<InvalidOperationException>(() => body.SyncToPhysics = true);
                Reject<InvalidOperationException>(() => body.SyncToPhysics = false);
            };
            try { tree.PhysicsFrame(1d / 60); }
            finally { solver.StepHook = null; }
            Check(numericalCalls > 0 && body.SyncToPhysics, "A numerical borrow rejects even equal synchronization writes");
        }
        var forceCalls = 0;
        PhysicsServer.BodySetForceIntegrationCallback(rider.GetRID(), _ =>
        {
            forceCalls++;
            Check(Environment.CurrentManagedThreadId == owner, "Public body callbacks retain scene-owner delivery");
            body.SyncToPhysics = false;
            body.SyncToPhysics = true;
        });
        try { tree.PhysicsFrame(1d / 60); }
        finally { PhysicsServer.BodySetForceIntegrationCallback(rider.GetRID(), null); }
        Check(forceCalls > 0 && body.SyncToPhysics, "Completed body callbacks may author synchronization on the owner");
        using var outsideDone = new ManualResetEventSlim();
        var outside = WorkerThreadPool.AddTask(() =>
        {
            try
            {
                Reject<InvalidOperationException>(() => _ = body.SyncToPhysics);
                Reject<InvalidOperationException>(() => body.SyncToPhysics = true);
            }
            finally { outsideDone.Set(); }
        }, highPriority: true);
        Check(outsideDone.Wait(TimeSpan.FromSeconds(5)), "An unrelated worker rejects access without waiting on the scene owner");
        WorkerThreadPool.WaitForTaskCompletion(outside);
        using var dead = new AnimatableBody(); dead.Dispose();
        Reject<ObjectDisposedException>(() => _ = dead.SyncToPhysics);
        Reject<ObjectDisposedException>(() => dead.SyncToPhysics = true);
        Console.WriteLine($"Group kinematic sync {world.PhysicsBackendName}/{(threaded ? "separate" : "owner")}/{(poolOwner ? "pool-host" : "ordinary-host")}: pending/idle/zero-step targets, observer failure, selected contacts, identities and 128 full physics/query frames at {ownerBytes}/{all} B passed.");
    }
}
