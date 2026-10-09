using Electron2D;
using Info = Electron2D.PhysicsServer.ProcessInfo;
using Monitor = Electron2D.Performance.Monitor;

internal static class PhysicsStatisticsTests
{
    internal static void Run()
    {
        Check((int)Info.ActiveObjects == 0 && (int)Info.CollisionPairs == 1 && (int)Info.IslandCount == 2, "Process selectors");
        Check((int)Monitor.PhysicsActiveObjects == 17 && (int)Monitor.PhysicsIslandCount == 19, "Monitor identities");
        Check(ReferenceEquals(Engine.GetSingleton<PhysicsServer>(nameof(PhysicsServer)), PhysicsServer.Service) &&
            ReferenceEquals(Engine.GetSingleton<Performance>(nameof(Performance)), Performance.Service), "Retained service identity");
        Reject<InvalidOperationException>(() => Performance.Service.Dispose());
        Reject<InvalidOperationException>(() => Engine.UnregisterSingleton(nameof(Performance)));
        Reject<InvalidOperationException>(() => Engine.UnregisterSingleton(nameof(PhysicsServer)));
        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.GetProcessInfo((Info)99));
        Reject<ArgumentOutOfRangeException>(() => Performance.GetMonitor((Monitor)99));
        if (Environment.GetEnvironmentVariable("ELECTRON2D_STATISTICS_CPU_ONLY") == "1")
        {
            Verify(PhysicsServer.Backend.CPU);
            VerifySensorPairs(PhysicsServer.Backend.CPU);
            Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "CPU statistics need no window or renderer");
            return;
        }
        foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU }) { Verify(backend); VerifySensorPairs(backend); }
        VerifySharedWorld();
        VerifyFailure();
        VerifyHostThread();
        Check(PhysicsServer.GetProcessInfo(Info.ActiveObjects) == 0, "All test worlds released");
    }

    private static void Verify(PhysicsServer.Backend backend)
    {
        var space = PhysicsServer.SpaceCreate(backend); var floor = PhysicsServer.BodyCreate();
        var a = PhysicsServer.BodyCreate(); var b = PhysicsServer.BodyCreate();
        var joint = PhysicsServer.JointCreate(); var sensor = PhysicsServer.AreaCreate();
        var kinematic = PhysicsServer.BodyCreate();
        using var circle = new CircleShape { Radius = 5 };
        using var ground = new RectangleShape { Size = new(200, 20) };
        using var zone = new RectangleShape { Size = new(200, 50) };
        try
        {
            PhysicsServer.SpaceSetActive(space, true);
            PhysicsServer.AreaSetGravity(space, 100); PhysicsServer.AreaSetLinearDamp(space, 0);
            PhysicsServer.BodySetMode(floor, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAddShape(floor, ground.GetRID()); PhysicsServer.BodySetTransform(floor, new(0, new Vector2(0, 20)));
            PhysicsServer.BodySetSpace(floor, space);
            foreach (var body in new[] { a, b })
            {
                PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetCanSleep(body, false);
                PhysicsServer.BodySetTransform(body, new(0, new Vector2(body == a ? -30 : 30, 5)));
                PhysicsServer.BodySetSpace(body, space);
            }
            Counts(0, 0, "No step has published"); Step(space, 30); Counts(2, 2, "Two supported independent bodies");
            var pairs = PhysicsServer.GetProcessInfo(Info.CollisionPairs); Check(pairs >= 2, "Physical collision candidates");
            Check(Task.Run(() => Performance.GetMonitor(Monitor.PhysicsActiveObjects)).Result == 2, "Cross-thread cached read");
            PhysicsServer.JointMakePin(joint, new(0, 5), a, b); Step(space); Counts(2, 1, "Joint joins active islands");
            PhysicsServer.JointClear(joint); Step(space, 16);
            Check(PhysicsServer.GetProcessInfo(Info.ActiveObjects) == 2 && PhysicsServer.GetProcessInfo(Info.IslandCount) is >= 1 and <= 2, "Removed joint retains valid backend groups; splitting may be deferred");
            PhysicsServer.BodySetSleeping(a, true); PhysicsServer.BodySetSleeping(b, true);
            Step(space); Counts(0, 0, "Sleeping components are not active");
            PhysicsServer.BodyApplyCentralImpulse(a, new(1, 0)); Step(space); Counts(1, 1, "Scoped wake");
            PhysicsServer.BodySetLinearVelocity(floor, new(1, 0)); Step(space); Counts(2, 2, "Moving static surface wakes bodies but is not active itself");
            PhysicsServer.BodySetMode(kinematic, PhysicsServer.BodyMode.Kinematic); PhysicsServer.BodySetSpace(kinematic, space);
            Step(space); Counts(3, 2, "Kinematic body counts without a dynamic constraint island");
            PhysicsServer.BodySetSpace(kinematic, default);
            PhysicsServer.AreaAddShape(sensor, zone.GetRID()); PhysicsServer.AreaSetSpace(sensor, space);
            Step(space); Counts(2, 2, "Sensor does not create solver islands");
            Check(PhysicsServer.GetProcessInfo(Info.CollisionPairs) > pairs, "Sensor pairs participate in diagnostics");
            PhysicsServer.BodySetLinearVelocity(floor, default); Step(space);
            PhysicsServer.SpaceSetActive(space, false); Counts(0, 0, "Local suspension omits the world");
            PhysicsServer.SpaceSetActive(space, true); Counts(2, 2, "Reactivation retains the last sample");
            PhysicsServer.SetActive(false); PhysicsServer.BodySetSleeping(a, true); Step(space); Counts(2, 2, "Global suspension retains the sample");
            PhysicsServer.SetActive(true); PhysicsServer.SpaceStep(space, 0); Counts(2, 2, "Zero delta is inert");
            Step(space); Counts(1, 1, "Next step publishes pending sleep");
            var calls = 0;
            PhysicsServer.BodySetStateSyncCallback(b, _ => { calls++; Counts(1, 1, "Callbacks see committed sample"); throw new InvalidOperationException("observer"); });
            Reject<AggregateException>(() => Step(space)); Check(calls == 1, "Throwing observer ran once");
            PhysicsServer.BodySetStateSyncCallback(b, null);
            Step(space, 64);
            var stepBytes = GC.GetTotalAllocatedBytes(true);
            Step(space, 64);
            Check(GC.GetTotalAllocatedBytes(true) == stepBytes, "Completed steps with sensors and statistics allocate no managed bytes");
            var store = PhysicsServer.Service.GetSceneSpace(space).GPUStore;
            var submissions = store?.SubmissionCount; var download = store?.ReadbackBytes;
            for (var i = 0; i < 256; i++) { _ = PhysicsServer.GetProcessInfo(Info.CollisionPairs); _ = Performance.GetMonitor(Monitor.PhysicsIslandCount); }
            var before = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 1024; i++) { _ = PhysicsServer.GetProcessInfo(Info.CollisionPairs); _ = Performance.GetMonitor(Monitor.PhysicsIslandCount); }
            Check(GC.GetTotalAllocatedBytes(true) == before, "Cached diagnostic reads allocate no managed bytes");
            Check(store?.SubmissionCount == submissions && store?.ReadbackBytes == download, "Reads issue no GPU work");
            PhysicsServer.BodySetSpace(a, default); PhysicsServer.BodySetSpace(b, default);
            PhysicsServer.BodySetSpace(floor, default); PhysicsServer.AreaSetSpace(sensor, default);
            Step(space); Counts(0, 0, "Empty positive step clears samples"); Check(PhysicsServer.GetProcessInfo(Info.CollisionPairs) == 0, "Empty pairs");
            Console.WriteLine($"Physics statistics passed on {backend}: active bodies, pairs, islands, sleep, joints, sensors, pause, callbacks and zero-allocation steps/reads.");
        }
        finally
        {
            PhysicsServer.SetActive(true);
            PhysicsServer.FreeRID(joint); PhysicsServer.FreeRID(sensor); PhysicsServer.FreeRID(kinematic);
            PhysicsServer.FreeRID(a); PhysicsServer.FreeRID(b); PhysicsServer.FreeRID(floor); PhysicsServer.FreeRID(space);
        }
    }
    private static void VerifySensorPairs(PhysicsServer.Backend backend)
    {
        var space = PhysicsServer.SpaceCreate(backend); var a = PhysicsServer.AreaCreate(); var b = PhysicsServer.AreaCreate();
        using var shape = new CircleShape { Radius = 10 };
        try
        {
            PhysicsServer.SpaceSetActive(space, true);
            PhysicsServer.AreaAddShape(a, shape.GetRID()); PhysicsServer.AreaAddShape(b, shape.GetRID());
            PhysicsServer.AreaSetSpace(a, space); PhysicsServer.AreaSetSpace(b, space);
            Step(space); Counts(0, 0, "Sensor-only world");
            Check(PhysicsServer.GetProcessInfo(Info.CollisionPairs) == 1, "Symmetric sensor pair counted once");
            PhysicsServer.AreaSetCollisionMask(a, 0); Step(space);
            Check(PhysicsServer.GetProcessInfo(Info.CollisionPairs) == 1, "Asymmetric sensor pair still counted");
            PhysicsServer.AreaSetCollisionMask(b, 0); Step(space);
            Check(PhysicsServer.GetProcessInfo(Info.CollisionPairs) == 0, "Filtered sensor pair omitted");
            PhysicsServer.AreaSetCollisionMask(a, 1); Step(space);
            Check(PhysicsServer.GetProcessInfo(Info.CollisionPairs) == 1, "Opposite directional sensor pair restored");
            PhysicsServer.AreaSetShapeDisabled(b, 0, true); Step(space);
            Check(PhysicsServer.GetProcessInfo(Info.CollisionPairs) == 0, "Disabled shape omitted");
        }
        finally { PhysicsServer.FreeRID(a); PhysicsServer.FreeRID(b); PhysicsServer.FreeRID(space); }
    }
    private static void VerifySharedWorld()
    {
        using var cpu = new World(PhysicsServer.Backend.CPU); using var gpu = new World(PhysicsServer.Backend.GPU);
        using var root = new Node(); var first = new SubViewport { Name = "First", World = cpu };
        var shared = new SubViewport { Name = "Shared", World = cpu }; var other = new SubViewport { Name = "Other", World = gpu };
        first.AddChild(new RigidBody { GravityScale = 0, CanSleep = false }); other.AddChild(new RigidBody { GravityScale = 0, CanSleep = false });
        root.AddChild(first); root.AddChild(shared); root.AddChild(other);
        using (var tree = new SceneTree(root))
        {
            tree.PhysicsFrame(1d / 60); Counts(2, 0, "Shared world counts once, independent worlds sum");
            PhysicsServer.SpaceSetActive(cpu.Space, false); Counts(1, 0, "Independent local activation");
        }
    }
    private static void VerifyFailure()
    {
        var space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU, allowCPUFallback: true); var body = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.BodySetSpace(body, space); Step(space); Counts(1, 0, "Initial sample");
            Reject<AggregateException>(() => PhysicsServer.SpaceStep(space, float.MaxValue));
            Counts(1, 0, "Failed interval retains its last published sample");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
    }
    private static void VerifyHostThread()
    {
        using var ready = new ManualResetEventSlim(); using var finish = new ManualResetEventSlim();
        var worker = Task.Run(() =>
        {
            var space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.CPU); var body = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.SpaceSetActive(space, true); PhysicsServer.BodySetSpace(body, space); Step(space);
                ready.Set(); finish.Wait();
            }
            finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
        });
        try { Check(ready.Wait(TimeSpan.FromSeconds(10)), "Foreign owner published"); Counts(1, 0, "Reads do not enter another world's owner lane"); }
        finally { finish.Set(); worker.GetAwaiter().GetResult(); }
        Counts(0, 0, "Foreign world removal drops its contribution");
    }
    private static void Step(RID space, int count = 1) { for (var i = 0; i < count; i++) PhysicsServer.SpaceStep(space, 1d / 60); }
    private static void Counts(int active, int islands, string message)
    {
        var actual = PhysicsServer.GetProcessInfo(Info.ActiveObjects); var groups = PhysicsServer.GetProcessInfo(Info.IslandCount);
        Check(actual == active && groups == islands, $"{message}: expected {active}/{islands}, got {actual}/{groups}");
        Check(Performance.GetMonitor(Monitor.PhysicsActiveObjects) == actual && Performance.GetMonitor(Monitor.PhysicsIslandCount) == groups &&
            Performance.GetMonitor(Monitor.PhysicsCollisionPairs) == PhysicsServer.GetProcessInfo(Info.CollisionPairs), "Performance reports the same samples");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}"); }
}
