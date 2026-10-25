using Electron2D;

public static partial class BackendChecks
{
    public static void RunGroupWorlds(PhysicsServer.Backend? backend, bool threaded, bool poolOwner = false)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads);
        var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var oldEngine = ProjectSettings.Get(ProjectSettings.Physics2DEngine);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        var factories = 0;
        if (backend is null) PhysicsServerManager.RegisterServer("GroupWorlds", () => { Interlocked.Increment(ref factories); return new Solver(); });
        ProjectSettings.Set(ProjectSettings.Physics2DEngine, backend?.ToString() ?? "GroupWorlds");
        try
        {
            if (poolOwner) { var job = WorkerThreadPool.AddTask(Run); WorkerThreadPool.WaitForTaskCompletion(job); }
            else Run();
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads);
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver);
            ProjectSettings.Set(ProjectSettings.Physics2DEngine, oldEngine);
        }
        void Run() { ColdWorlds(false); ColdWorlds(true); ActiveWorlds(); }
        World Create() => backend is { } selected ? new World(selected) : new World("GroupWorlds");
        void ColdWorlds(bool viewport)
        {
            using Node root = viewport ? new SubViewport() : new Node();
            var a = Writer(); var b = Writer(); root.AddChild(a); root.AddChild(b);
            using var tree = new SceneTree(root); using var together = new Barrier(2);
            using var foreign = new SubViewport(); using var foreignTree = new SceneTree(foreign);
            World? foreignFirst = null, foreignSecond = null;
            World? first = null, second = null; var before = factories;
            a.Write = () => { first = Read(a); foreignFirst = foreign.FindWorld(); };
            b.Write = () => { second = Read(b); foreignSecond = foreign.World; };
            tree.PhysicsFrame(.01);
            Check(a.ThreadID != b.ThreadID && ReferenceEquals(first, second), "Parallel first reads publish one shared world wrapper");
            Check(ReferenceEquals(foreignFirst, foreignSecond) && !ReferenceEquals(first, foreignFirst), "Coordinated active groups may observe a different tree without sharing its driver");
            Check(factories == before && first!.RequestedPhysicsBackendName == (backend?.ToString() ?? "GroupWorlds"), "Cold logical lookup retains selection without constructing a numerical implementation");
            var old = first!; var canvas = old.Canvas; old.Dispose();
            tree.PhysicsFrame(.01);
            Check(ReferenceEquals(first, second) && !ReferenceEquals(old, first) && first!.Canvas == canvas, "Parallel reads reopen one bound wrapper with the same runtime identity");
            a.Write = b.Write = null;
            var space = first!.Space;
            Check(first.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension), "Only the scene owner initializes the selected implementation");
            using var alias = (World)first.Duplicate(); tree.Dispose();
            Reject<ArgumentException>(() => _ = alias.Canvas); Reject<ArgumentException>(() => PhysicsServer.SpaceGetTick(space));
            World Read(TransformWriter writer)
            {
                if (!together.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException("World readers did not overlap");
                var world = writer.GetWorld()!;
                if (root is Viewport view) Check(ReferenceEquals(world, view.FindWorld()) && ReferenceEquals(world, view.World), "Viewport property and finder share the active world");
                Reject<InvalidOperationException>(() => _ = world.Space);
                return world;
            }
        }
        void ActiveWorlds()
        {
            using var one = Create(); using var two = Create(); var unusedSpace = two.Space;
            using var root = new Node();
            var left = new SubViewport { World = one }; var right = new SubViewport { World = two };
            var a = Writer(); var b = Writer(); left.AddChild(a); right.AddChild(b);
            using var shape = new CircleShape { Radius = 2 };
            var body = new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(12, 0), LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
            body.AddChild(new CollisionShape { Shape = shape }); a.AddChild(body);
            root.AddChild(left); root.AddChild(right); using var tree = new SceneTree(root);
            var rid = body.GetRID(); var id = body.InstanceID;
            using var together = new Barrier(2); var cold = true; var observations = 0;
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            World expected = one; a.Write = () => Read(a); b.Write = () => Read(b);
            tree.PhysicsFrame(.01); cold = false;
            Check(a.ThreadID != b.ThreadID, "World lookup runs in two actual concurrent groups");
            one.Dispose(); expected = left.FindWorld()!; tree.PhysicsFrame(.01);
            right.World = expected; tree.PhysicsFrame(.01);
            Check(ReferenceEquals(right.World, expected) && right.World!.Space != unusedSpace, "Owner replacement commits before group observations");
            var failure = new ApplicationException("Expected world observer failure"); var fail = true;
            a.Write = () => { Read(a); if (fail) { fail = false; throw failure; } };
            var before = observations; var tick = PhysicsServer.SpaceGetTick(expected.Space);
            var error = CaptureCopy(() => tree.PhysicsFrame(.01));
            Check(error is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(failure) && observations == before + 2 && PhysicsServer.SpaceGetTick(expected.Space) == tick + 1, "Reader failure preserves later groups and releases the complete numerical interval");
            a.Write = () => Read(a);
            for (var i = 0; i < 128; i++) Frame(); Collect(); for (var i = 0; i < 128; i++) Frame();
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread(); var allBytes = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBytes; allBytes = GC.GetTotalAllocatedBytes(true) - allBytes;
            Check(ownerBytes == 0 && allBytes == 0 && body.GetRID() == rid && body.InstanceID == id, $"Grouped world observations/full physics/query frames allocated {ownerBytes}/{allBytes} B and preserve identity");
            using var done = new ManualResetEventSlim();
            var job = WorkerThreadPool.AddTask(() =>
            {
                try { Reject<InvalidOperationException>(() => body.GetWorld()); Reject<InvalidOperationException>(() => left.FindWorld()); Reject<InvalidOperationException>(() => _ = right.World); }
                finally { done.Set(); }
            }, highPriority: true);
            Check(done.Wait(TimeSpan.FromSeconds(5)), "An ordinary worker completes without owner assistance"); WorkerThreadPool.WaitForTaskCompletion(job);
            using var detached = new Entity(); Check(detached.GetWorld() is null, "Detached canvas item has no selected world");
            detached.Dispose(); Reject<ObjectDisposedException>(() => detached.GetWorld());
            Console.WriteLine($"Group worlds {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}/{(poolOwner ? "pool" : "ordinary")}: concurrent lazy fallback/viewport publication, wrapper reopening, owner startup/replacement, failure recovery and 128 complete physics/query frames at {ownerBytes}/{allBytes} B passed.");
            void Read(TransformWriter writer)
            {
                if (cold && !together.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Active world readers did not overlap");
                Check(ReferenceEquals(body.GetWorld(), expected) && ReferenceEquals(left.FindWorld(), expected) && ReferenceEquals(left.World, expected), "Any active group observes the actual borrowed world across group boundaries");
                _ = right.FindWorld()!.Canvas; _ = writer.GetWorld()!.Canvas;
                if (cold)
                {
                    Reject<InvalidOperationException>(() => left.World = expected);
                    Reject<InvalidOperationException>(() => expected.DirectSpaceState.IntersectPoint(point, hits));
                    Reject<InvalidOperationException>(() => left.GetViewportRID());
                }
                Interlocked.Increment(ref observations);
            }
            void Frame()
            {
                var before = body.Position.X; var tick = PhysicsServer.SpaceGetTick(expected.Space);
                tree.PhysicsFrame(.01); point.Position = body.Position;
                Check(MathF.Abs(body.Position.X - before - .12f) < .002f && PhysicsServer.SpaceGetTick(expected.Space) == tick + 1 &&
                    expected.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == rid, "Grouped observations preserve real motion, one step per shared world and query identity");
            }
        }
        static TransformWriter Writer() => new() { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
    }
}
