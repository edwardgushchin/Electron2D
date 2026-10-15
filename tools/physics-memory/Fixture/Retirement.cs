using System.Runtime.CompilerServices;
using System.Text.Json;
using Electron2D;

internal static class Retirement
{
    internal static void Run(string[] args)
    {
        if (args.Length != 5 || args[0] is not ("cpu" or "gpu")) throw new ArgumentException("Expected backend, bodies, history, cycles, separate-thread boolean.");
        var count = int.Parse(args[1]); var history = int.Parse(args[2]); var cycles = int.Parse(args[3]); var threaded = bool.Parse(args[4]);
        if (count < 4 || count > 65536 || count % 4 != 0 || history is < 1 or > 32 || cycles is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(args));
        var backend = args[0] == "cpu" ? PhysicsServer.Backend.CPU : PhysicsServer.Backend.GPU;
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        var roots = new MemoryRoots();
        Ready("baseline", 0, 0, false);
        for (var cycle = 1; cycle <= cycles; cycle++)
        {
            var failed = Cycle(backend, count, history, cycle, roots, Ready);
            Ready($"cycle-{cycle}-retired", 0, cycle, failed);
        }
        Thread.Sleep(250);
        Ready("settled", 0, cycles, false);
        GC.KeepAlive(roots);

        void Ready(string phase, int bodies, int cycle, bool failed)
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Phase = phase,
                Cycle = cycle,
                FailedStepVerified = failed,
                SeparateThread = threaded,
                Bodies = bodies,
                Checkpoints = roots.Checkpoints.Length,
                ProcessMemory = ProcessMemory.Capture()
            }));
            if (Console.ReadLine() != "next") throw new IOException("Memory probe disconnected.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Cycle(PhysicsServer.Backend backend, int count, int history, int cycle, MemoryRoots roots, Action<string, int, int, bool> ready)
    {
        var space = PhysicsServer.SpaceCreate(backend); var bodies = new RID[count]; var joint = default(RID);
        using var shape = new CircleShape { Radius = 2 };
        var failed = false;
        try
        {
            if (PhysicsServer.SpaceGetBackend(space) != backend || PhysicsServer.SpaceGetBackendFallbackReason(space) is not null)
                throw new InvalidOperationException("Retirement requires the selected backend without fallback.");
            PhysicsServer.SpaceSetActive(space, true);
            for (var i = 0; i < count; i++)
            {
                var body = bodies[i] = PhysicsServer.BodyCreate();
                PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false);
                PhysicsServer.BodySetTransform(body, new(0, new(i / 2 % 32 * 12, i / 64 * 12 + i % 2 * 3.5f)));
                PhysicsServer.BodySetSpace(body, space);
            }
            joint = PhysicsServer.JointCreate(); PhysicsServer.JointMakePin(joint, Vector2.Zero, bodies[0]);
            PhysicsServer.SpaceStep(space, 1d / 120);
            roots.Map = new(space); roots.Snapshot = new();
            for (var i = 0; i < count; i++) roots.Map.Bind((ulong)i + 1, 1, bodies[i]);
            roots.Map.Bind(1000000, 1, joint);
            roots.Checkpoints = new PhysicsCheckpoint[history];
            for (var i = 0; i < history; i++) roots.Checkpoints[i] = PhysicsServer.SpaceCreateCheckpoint(space);
            for (var step = 0; step < 8; step++)
            {
                PhysicsServer.SpaceStep(space, 1d / 120);
                foreach (var point in roots.Checkpoints) { point.Capture(); point.Restore(); }
                roots.Map.Capture(roots.Snapshot); roots.Map.Apply(roots.Snapshot);
            }
            ready($"cycle-{cycle}-live", count, cycle, false);
            if (backend == PhysicsServer.Backend.GPU && cycle % 2 == 0)
            {
                PhysicsServer.AreaSetGravity(space, 980);
                PhysicsServer.BodySetGravityScale(bodies[^1], 1);
                try { PhysicsServer.SpaceStep(space, float.MaxValue); }
                catch (AggregateException) { failed = true; }
                if (!failed || PhysicsServer.SpaceGetBackend(space) != backend) throw new InvalidOperationException("The begun GPU failure was not preserved.");
                Reject(() => PhysicsServer.SpaceStep(space, 1d / 120));
                Reject(() => PhysicsServer.BodyGetCollisionLayer(bodies[0]));
            }
        }
        finally
        {
            // Retire the source while histories are held, then release every caller-owned identity.
            PhysicsServer.FreeRID(space);
            foreach (var point in roots.Checkpoints) point.Dispose();
            roots.Map?.Dispose();
            foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
            if (joint.IsValid()) PhysicsServer.FreeRID(joint);
            roots.Map = null; roots.Snapshot = null; roots.Checkpoints = [];
        }
        return failed;
    }

    private static void Reject(Action action)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("A failed world must reject further simulation and state reads.");
    }
}
