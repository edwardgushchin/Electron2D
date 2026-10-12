using Electron2D;

public static partial class BackendChecks
{
    private enum LifecyclePhase { Initialize, Active, Begin, Solve, Sync, Statistics, State, Flush, End, Finish }

    public static void RunLifecycle()
    {
        LifecycleScope(false); LifecycleScope(true);
        foreach (var phase in new[] { LifecyclePhase.Solve, LifecyclePhase.Sync, LifecyclePhase.Statistics, LifecyclePhase.Flush, LifecyclePhase.End })
            LifecycleFailure(phase);
        foreach (var statistics in new[] { new PhysicsBackendStatistics(-1, 0, 0), new PhysicsBackendStatistics(0, -1, 0), new PhysicsBackendStatistics(0, 0, -1) })
            LifecycleFailure(LifecyclePhase.Statistics, statistics);
        Solver? rejected = null;
        PhysicsServerManager.RegisterServer("LifecycleInitializationFailure", () => rejected = new Solver { FailInit = true, FailFinish = true, FailDisposal = true });
        try { PhysicsServer.SpaceCreate("LifecycleInitializationFailure"); throw new InvalidOperationException("Expected initialization failure"); }
        catch (AggregateException error)
        {
            Check(error.Flatten().InnerExceptions.Count == 3 && rejected!.Finished && rejected.IsDisposed,
                "Initialization, finish and disposal failures all survive retirement");
        }
        Console.WriteLine("Registered lifecycle: explicit/scene order, activation, published statistics, errors, retirement and zero warm allocations passed.");
    }

    private static void LifecycleScope(bool scene)
    {
        Solver? solver = null; var phases = new List<LifecyclePhase>(16);
        var name = scene ? "LifecycleScene" : "LifecycleExplicit";
        PhysicsServerManager.RegisterServer(name, () => solver = new Solver
        {
            LifecycleHook = phase =>
        {
            phases.Add(phase);
            if (phase == LifecyclePhase.Initialize) Check(!PhysicsServer.SpaceIsActive(solver!.Space), "Initialization hook sees an inactive registered context");
        }
        });
        using var world = scene ? new World(name) : null;
        using var root = scene ? new SubViewport { World = world } : null;
        using var circle = new CircleShape { Radius = 2 };
        using var node = scene ? new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = Vector2.Right } : null;
        if (node is not null) { node.AddChild(new CollisionShape { Shape = circle }); root!.AddChild(node); }
        var space = world?.Space ?? PhysicsServer.SpaceCreate(name);
        Check(phases[0] == LifecyclePhase.Initialize && phases.Count(p => p == LifecyclePhase.Initialize) == 1 && PhysicsServer.SpaceIsActive(space) == scene,
            "Initialization precedes the automatic activation of a scene world; explicit spaces remain inactive");
        using var tree = root is null ? null : new SceneTree(root);
        var body = node?.GetRID() ?? PhysicsServer.BodyCreate();
        try
        {
            if (node is null)
            {
                PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetGravityScale(body, 0);
                PhysicsServer.BodySetCanSleep(body, false); PhysicsServer.BodySetLinearVelocity(body, Vector2.Right);
                PhysicsServer.BodySetSpace(body, space); PhysicsServer.SpaceSetActive(space, true);
            }
            Check(PhysicsServer.SpaceGetBackend(space) == PhysicsServer.Backend.Extension && phases.Count(p => p == LifecyclePhase.Active) == 1,
                "Actual scene/raw numerical ownership activates once");
            PhysicsServer.BodySetStateSyncCallback(body, _ => phases.Add(LifecyclePhase.State));
            var position = PhysicsServer.BodyGetTransform(body).Origin; phases.Clear(); Tick(0);
            Check(phases.Count == 0 && PhysicsServer.SpaceGetTick(space) == 0, "Zero time does not dispatch interval hooks");
            Cycle(); Check(MathF.Abs(PhysicsServer.BodyGetTransform(body).Origin.X - position.X - 1f / 120) < .0001f, "Selected solving publishes real scene-unit motion");
            Counts(1, 0, 1);
            phases.Clear(); PhysicsServer.SpaceSetActive(space, true); Check(phases.Count == 0, "Equal activation invokes no hook");
            solver!.LifecycleHook = phase => { phases.Add(phase); if (phase == LifecyclePhase.Active) throw new ApplicationException("Activation failure"); };
            Reject<ApplicationException>(() => PhysicsServer.SpaceSetActive(space, false));
            Check(PhysicsServer.SpaceIsActive(space), "Failed selected activation preserves the previous local policy");
            solver.LifecycleHook = phases.Add;
            PhysicsServer.SpaceSetActive(space, false); Counts(0, 0, 0); phases.Clear(); Tick(1d / 120);
            Check(phases.Count == 0 && solver!.Steps == 1, "Inactive ownership does not dispatch or erase its completed interval");
            PhysicsServer.SpaceSetActive(space, true); Counts(1, 0, 1);
            PhysicsServer.SetActive(false);
            try { phases.Clear(); Tick(1d / 120); Check(phases.Count == 0 && solver!.Steps == 1, "Global suspension retains selected activation and performs no solve"); Counts(1, 0, 1); }
            finally { PhysicsServer.SetActive(true); }
            Task.Run(() => Counts(1, 0, 1)).GetAwaiter().GetResult();
            Task.Run(() => Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetActive(space, false))).GetAwaiter().GetResult();
            solver!.LifecycleHook = phase =>
            {
                phases.Add(phase);
                if (phase == LifecyclePhase.Solve)
                {
                    Reject<InvalidOperationException>(() => Tick(0));
                    Reject<InvalidOperationException>(() => { if (tree is null) PhysicsServer.FreeRID(space); else tree.Dispose(); });
                }
            };
            Cycle(); solver.LifecycleHook = phases.Add;
            Collect(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Cycle();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Cycle();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0 && solver.Steps == 194, "Repeated lifecycle, cached statistics and full moving intervals allocate zero owner/all-thread bytes");
            Console.WriteLine($"Registered lifecycle scene={scene}: 128 active measured frames, owner/all={owner}/{all} B.");
            PhysicsServer.BodySetStateSyncCallback(body, null);
            if (node is null) PhysicsServer.BodySetSpace(body, default); else root!.RemoveChild(node);
            phases.Clear(); Tick(1d / 120);
            Check(phases.SequenceEqual([LifecyclePhase.Begin, LifecyclePhase.Solve, LifecyclePhase.Sync, LifecyclePhase.Statistics, LifecyclePhase.Flush, LifecyclePhase.End]),
                "An empty active world executes every selected interval phase without body callbacks");
            Counts(0, 0, 0);
        }
        finally
        {
            solver!.LifecycleHook = phases.Add;
            if (node is null) { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
            else { tree!.Dispose(); world!.Dispose(); }
        }
        Check(solver!.Finished && solver.IsDisposed && phases.Count(p => p == LifecyclePhase.Finish) == 1, "Finish and disposal retire the selected owner once");
        Counts(0, 0, 0);
        void Tick(double delta) { if (tree is null) PhysicsServer.SpaceStep(space, delta); else tree.PhysicsFrame(delta); }
        void Cycle()
        {
            phases.Clear(); Tick(1d / 120);
            ReadOnlySpan<LifecyclePhase> expected = [LifecyclePhase.Begin, LifecyclePhase.Solve, LifecyclePhase.Sync, LifecyclePhase.Statistics, LifecyclePhase.State, LifecyclePhase.Flush, LifecyclePhase.End];
            Check(phases.Count == expected.Length, "One complete selected interval has exactly the required phases");
            for (var i = 0; i < expected.Length; i++) Check(phases[i] == expected[i], "Numerical sync, completed statistics, body observers, flush and end-sync retain order");
            Check(solver!.EndFailure is null, "Successful end-sync receives no failure"); Counts(1, 0, 1);
        }
    }

    private static void LifecycleFailure(LifecyclePhase phase, PhysicsBackendStatistics? statistics = null)
    {
        Solver? solver = null; var name = "LifecycleFailure" + phase + (statistics?.ToString() ?? "");
        PhysicsServerManager.RegisterServer(name, () => solver = new());
        var space = PhysicsServer.SpaceCreate(name); var body = PhysicsServer.BodyCreate();
        using var circle = new CircleShape { Radius = 2 };
        try
        {
            PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false);
            PhysicsServer.BodySetSpace(body, space); PhysicsServer.SpaceSetActive(space, true); PhysicsServer.SpaceStep(space, 1d / 120);
            var steps = solver!.Steps; solver.StatisticsOverride = statistics;
            solver.LifecycleHook = current => { if (current == phase && statistics is null) throw new ApplicationException("Lifecycle failure"); };
            Reject<AggregateException>(() => PhysicsServer.SpaceStep(space, 1d / 120));
            Check(solver.Steps == steps + (phase == LifecyclePhase.Solve ? 0 : 1), "Failure never repeats a started numerical solve");
            Counts(1, 0, 1);
            solver.LifecycleHook = null; solver.StatisticsOverride = null;
            if (phase is LifecyclePhase.Flush or LifecyclePhase.End)
            {
                PhysicsServer.SpaceStep(space, 1d / 120);
                Check(solver.Steps == steps + 2, "Notification/finalization failures allow the next distinct interval");
            }
            else
            {
                Check(solver.EndFailure is not null, "End-sync receives the numerical failure");
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(space, 1d / 120));
                Reject<InvalidOperationException>(() => PhysicsServer.BodyGetTransform(body));
            }
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
        Check(solver!.Finished && solver.IsDisposed, "A failed owner still finishes and disposes"); Counts(0, 0, 0);
    }

    private static void Counts(int active, int pairs, int islands)
    {
        Check(PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.ActiveObjects) == active &&
            PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.CollisionPairs) == pairs &&
            PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.IslandCount) == islands, "Published process counts preserve all three selected values");
    }
}
