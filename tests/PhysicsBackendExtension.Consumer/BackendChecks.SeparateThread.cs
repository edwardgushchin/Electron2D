using Electron2D;

public static partial class BackendChecks
{
    public static void RunSeparateThread()
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, true);
        try
        {
            var owner = Environment.CurrentManagedThreadId;
            Solver? solver = null;
            PhysicsServerManager.RegisterServer("ThreadedConsumer", () =>
            {
                Check(Environment.CurrentManagedThreadId == owner, "Provider factory retains owner-thread creation");
                return solver = new();
            });
            using var world = new World("ThreadedConsumer");
            using var shape = new CircleShape { Radius = 2 };
            using var root = new SubViewport { World = world };
            var body = new RigidBody
            {
                GravityScale = 0,
                CanSleep = false,
                LinearVelocity = new(30, 0),
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
            using var tree = new SceneTree(root);
            var bodyRID = body.GetRID(); var spaceRID = world.Space;
            Thread? worker = null; var callbacks = 0; var statusReads = 0;
            solver!.StepHook = () =>
            {
                worker = Thread.CurrentThread;
                Check(Environment.CurrentManagedThreadId != owner, "Independent provider Solve runs on its borrowed numerical lane");
                Check(PhysicsServer.BodyGetMass(bodyRID) == 1 && ReferenceEquals(solver.ReadGeometry(shape.GetRID()), shape.GetGeometryData()),
                    "Prepared authored values and cached geometry remain readable in a numerical hook");
                Check(!solver.IsBodyExcludedFromMotionTest(bodyRID) && !solver.IsObjectExcludedFromMotionTest(body.InstanceID) &&
                    solver.ReadExceptions(bodyRID).Length == 0, "The complete readonly exclusion family remains scoped to prepared inputs");
                Reject<InvalidOperationException>(() => _ = solver.IsFlushingQueries);
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetMass(bodyRID, 1));
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetDirectState(spaceRID));
                Reject<InvalidOperationException>(() => tree.Dispose());
            };
            solver.StatusHook = () => { if (Environment.CurrentManagedThreadId != owner) statusReads++; };
            solver.SyncHook = () => Check(Environment.CurrentManagedThreadId == owner, "Provider synchronization returns to owner");
            solver.FinishHook = () => Check(Environment.CurrentManagedThreadId == owner, "Provider retirement remains on owner");
            PhysicsServer.BodySetStateSyncCallback(bodyRID, _ =>
            { Check(Environment.CurrentManagedThreadId == owner, "Game callbacks remain on owner"); callbacks++; });
            tree.PhysicsFrame(1d / 60);
            Check(worker is not null && statusReads > 0 && callbacks == 1 && Math.Abs(body.Position.X - .5) < .001,
                "The engine joins real independent numerical state before publication");
            solver.StepHook = null; solver.StatusHook = null;
            Collect();
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
            var current = GC.GetAllocatedBytesForCurrentThread(); var total = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
            Check(GC.GetAllocatedBytesForCurrentThread() == current && GC.GetTotalAllocatedBytes(true) == total,
                "Threaded independent full-frame publication retains zero owner/all-thread managed bytes");
            var attempts = 0; var steps = solver.Steps;
            solver.StepHook = () => { attempts++; throw new ArithmeticException("worker interval probe"); };
            try { tree.PhysicsFrame(1d / 60); throw new Exception("A worker solve failure was ignored"); }
            catch (AggregateException error) when (ContainsArithmetic(error)) { }
            Check(attempts == 1 && solver.Steps == steps, "A failed independent numerical interval has no successful solve or hidden replay");
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetDirectState(spaceRID));
            tree.Dispose(); root.Dispose(); world.Dispose();
            Check(worker is not null && !worker.IsAlive && solver.Finished && solver.IsDisposed,
                "Failed world teardown joins the worker and retires the selected provider");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        Console.WriteLine("Independent separate numerical thread: readonly inputs, owner publication, failure/no-replay, retirement and 0/0 warmed managed bytes passed.");
    }

    private static bool ContainsArithmetic(Exception error) => error is ArithmeticException ||
        error is AggregateException aggregate && aggregate.InnerExceptions.Any(ContainsArithmetic);
}
