using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsGPUMixedPublicationTests
{
    internal static void Run()
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var threaded = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_GPU_PUBLICATION_THREAD") == "1";
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { Verify(); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void Verify()
    {
        const int count = 512;
        using var world = new World(PhysicsServer.Backend.GPU);
        using var root = new SubViewport { World = world };
        var bodies = new RigidBody[count];
        for (var i = 0; i < count; i++)
        {
            bodies[i] = new RigidBody { Name = "Dormant" + i, Position = new(i * 2, 0), GravityScale = 0, Sleeping = true };
            root.AddChild(bodies[i]);
        }
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.AreaSetGravity(world.Space, 0); PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
            PhysicsServer.BodySetCanSleep(raw, false); PhysicsServer.BodySetLinearVelocity(raw, new(60, 0)); PhysicsServer.BodySetSpace(raw, world.Space);
            var store = PhysicsServer.Service.GetSceneSpace(world.Space).GPUStore!;
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            for (var i = 0; i < 128; i++) Frame();
            Check(store.Count == count + 1, "Mixed publication retains all 513 numerical bodies");
            var before = PhysicsServer.BodyGetTransform(raw).Origin;
            var times = new double[128];
            var read = store.ReadbackBytes; var up = store.UploadBytes; var submits = store.SubmissionCount; var wait = store.WaitMS;
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++)
            {
                var start = System.Diagnostics.Stopwatch.GetTimestamp(); Frame();
                times[i] = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            var bytes = (store.ReadbackBytes - read) / 128;
            Array.Sort(times);
            Console.WriteLine($"Mixed quiet GPU publication, threaded={ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread)}: {count} scene + 1 active raw body, 128 warm/128 measured full frames, {owner}/{all} B; readback={bytes}, upload={(store.UploadBytes - up) / 128} B/frame, submissions={(store.SubmissionCount - submits) / 128}, wait={(store.WaitMS - wait) / 128:F4} ms/frame; full p50/p95/p99={times[63]:F4}/{times[120]:F4}/{times[125]:F4} ms.");
            Check(owner == 0 && all == 0, $"Mixed quiet full frames allocate {owner}/{all} B");
            Check(bytes < 1024, $"Mixed quiet publication downloads {bytes} B/frame for 512 unchanged scene observers");
            var after = PhysicsServer.BodyGetTransform(raw).Origin;
            Check(MathF.Abs(after.X - before.X - 128) < .02f && PhysicsServer.BodyGetLinearVelocity(raw) == new Vector2(60, 0),
                "Unobserved server motion retains its complete actual trajectory");
            bodies[0].ApplyCentralImpulse(new(60, 0)); tree.PhysicsFrame(1d / 60);
            Check(!bodies[0].Sleeping && bodies[0].LinearVelocity.X > 59 && bodies[0].Position.X > .99f,
                "A quiet observer wakes with current motion and fields");
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            using var map = new PhysicsSnapshotMap(world.Space);
            for (var i = 0; i < count; i++) map.Bind((ulong)i + 1, 1, bodies[i].GetRID());
            map.Bind(count + 1, 1, raw);
            var packet = new PhysicsSnapshot(1 << 20); map.Capture(packet);
            var saved = bodies[0].GlobalTransform; var savedRaw = PhysicsServer.BodyGetTransform(raw);
            Future(); checkpoint.Restore(); Restored();
            Future(); map.Apply(packet); Restored();
            foreach (var body in bodies) { body.CanSleep = false; body.ApplyCentralImpulse(new(60, 0)); }
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
            read = store.ReadbackBytes;
            owner = GC.GetAllocatedBytesForCurrentThread(); all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(1d / 60);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            var activeBytes = (store.ReadbackBytes - read) / 128;
            Check(owner == 0 && all == 0 && activeBytes < 33000, $"Active mixed full publication: {owner}/{all} B, {activeBytes} readback B/frame");
            for (var i = 0; i < count; i++)
                Check(!bodies[i].Sleeping && bodies[i].LinearVelocity.X > 59, "Every awakened scene observer retains complete actual motion");
            Console.WriteLine($"Mixed active GPU publication: same {count} scene + 1 raw body, 128 warm/128 measured full frames, {owner}/{all} B, {activeBytes} readback B/frame; capacity survives waking every observer.");
            Console.WriteLine("Mixed quiet publication: wake, complete pose/velocity, private/portable correction and continued real steps passed.");

            void Frame()
            {
                tree.PhysicsFrame(1d / 60);
                for (var i = 0; i < count; i++)
                    Check(bodies[i].Sleeping && bodies[i].Position == new Vector2(i * 2, 0) && bodies[i].LinearVelocity == Vector2.Zero,
                        "Quiet scene identities, sleep and motion remain observable on every full frame");
            }
            void Future() { bodies[0].Position = new(400, 30); bodies[0].ApplyCentralImpulse(new(5, 2)); tree.PhysicsFrame(1d / 60); }
            void Restored()
            {
                Check(bodies[0].GlobalTransform == saved && PhysicsServer.BodyGetTransform(raw) == savedRaw && bodies[0].LinearVelocity.X > 59,
                    "Correction restores mixed cached and resident state without stale publication");
                tree.PhysicsFrame(1d / 60);
                Check(bodies[0].Position.X > saved.Origin.X + .9f && PhysicsServer.BodyGetTransform(raw).Origin.X > savedRaw.Origin.X + .9f,
                    "Both scene and raw owners continue after correction");
            }
        }
        finally { PhysicsServer.FreeRID(raw); }
    }
}
