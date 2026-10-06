using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

internal static class PhysicsParallelTests
{
    internal static void Run()
    {
        if (!OperatingSystem.IsBrowser()) VerifyScheduler();
        var owner = Environment.CurrentManagedThreadId;
        using var circle = new CircleShape { Radius = 6 };
        using var floorShape = new RectangleShape { Size = new(800, 18) };
        var root = new Node();
        var floor = new StaticBody { Position = new(400, 600) };
        floor.AddChild(new CollisionShape { Shape = floorShape, OneWayCollision = true });
        root.AddChild(floor);
        var bodies = new List<ProbeBody>();
        for (var i = 0; i < 288; i++)
        {
            var body = new ProbeBody(owner) { Name = "Particle" + i, CanSleep = false, Position = new(20 + i % 24 * 30, 310 + i / 24 * 18), ContactMonitor = true, MaxContactsReported = 4 };
            body.AddChild(new CollisionShape { Shape = circle });
            body.BodyEntered += _ => { if (Environment.CurrentManagedThreadId != owner) throw new Exception("Contact event left its owner thread."); };
            root.AddChild(body); bodies.Add(body);
        }
        using var tree = new SceneTree(root);
        for (var i = 0; i < 256; i++) tree.PhysicsFrame(1d / 144);
        var world = b2GetWorldFromId(bodies[0].Space!.WorldID);
        Check(world.workerCount == (OperatingSystem.IsBrowser() ? 1 : Math.Min(4, Environment.ProcessorCount)), "Large world selects retained workers.");
        Check(bodies.All(body => body.Position.IsFinite() && body.Position.Y < 610 && body.Calls == 256), "Parallel contacts and owner integration remain valid.");
        for (var i = bodies.Count - 1; i >= 32; i--) bodies[i].Freeze = true;
        tree.PhysicsFrame(1d / 144);
        Check(world.workerCount == 1, "Small awake population returns to the serial solver.");
        foreach (var body in bodies) body.Freeze = false;
        tree.PhysicsFrame(1d / 144);
        Check(world.workerCount == (OperatingSystem.IsBrowser() ? 1 : Math.Min(4, Environment.ProcessorCount)), "Wakeup restores parallel solving.");
    }

    private static void VerifyScheduler()
    {
        using var scheduler = new PhysicsTaskScheduler(3);
        var definition = b2DefaultWorldDef();
        definition.workerCount = 3; definition.enqueueTask = scheduler.Enqueue; definition.finishTask = scheduler.Finish;
        var id = b2CreateWorld(definition);
        scheduler.Bind(b2GetWorldFromId(id));
        try
        {
            var values = new int[97];
            static void Fill(int start, int end, uint worker, object context) { var values = (int[])context; for (var i = start; i < end; i++) values[i]++; }
            for (var i = 0; i < 160; i++) scheduler.Finish(scheduler.Enqueue(Fill, values.Length, 16, values, null!), null!);
            Check(values.All(value => value == 160), "Non-divisible ranges and recycled jobs cover every item once.");
            var worker = new B2WorkerContext { context = new B2StepContext(), workerIndex = 1 };
            var failed = scheduler.Enqueue(static (_, _, _, _) => throw new ArithmeticException("probe"), 3, 1, worker, null!);
            try { scheduler.Finish(failed, null!); throw new Exception("Worker failure was ignored."); }
            catch (InvalidOperationException ex) when (ex.InnerException is ArithmeticException) { }
            Check(worker.context.workerFailure is ArithmeticException && B2Atomics.b2AtomicLoadU32(ref worker.context.atomicSyncBits) == uint.MaxValue,
                "Solver failure publishes cancellation to waiting workers.");
            scheduler.Finish(scheduler.Enqueue(Fill, values.Length, 16, values, null!), null!);
            Check(values.All(value => value == 161), "All failed task ranges finish and storage remains reusable.");
        }
        finally { b2DestroyWorld(id); }
    }

    private sealed class ProbeBody(int owner) : RigidBody
    {
        internal int Calls;
        protected override void IntegrateForces(PhysicsDirectBodyState state)
        {
            if (Environment.CurrentManagedThreadId != owner) throw new Exception("Force integration left its owner thread.");
            Calls++;
        }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
