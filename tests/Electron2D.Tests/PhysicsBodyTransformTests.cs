using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsBodyTransformTests
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 2 };
        var scene = new RigidBody { Position = new(4000, 0), GravityScale = 0, CanSleep = false };
        scene.AddChild(new CollisionShape { Shape = shape }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        var space = world.Space; var data = PhysicsServer.Service.GetSceneSpace(space); var gpu = data.GPUStore;
        var bodies = new RID[128]; var otherSpace = PhysicsServer.SpaceCreate(); var foreign = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        try
        {
            PhysicsServer.AreaSetGravity(space, 0); PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0);
            for (var i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodyAddShape(body, shape.GetRID());
                PhysicsServer.BodySetTransform(body, new(.25f, new(i * 25, 100)));
                PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false);
                PhysicsServer.BodySetLinearVelocity(body, new(60, 0)); PhysicsServer.BodySetSpace(body, space);
            }
            PhysicsServer.BodySetMode(bodies[^1], PhysicsServer.BodyMode.Static);
            PhysicsServer.BodySetSpace(foreign, otherSpace); PhysicsServer.AreaSetSpace(area, space);
            tree.PhysicsFrame(1d / 60);
            RID[] mixed = [bodies[3], scene.GetRID(), bodies[^1], bodies[3], bodies[0]];
            var sentinel = new Transform(.75f, new(-123, -456)); var results = new Transform[mixed.Length + 1];
            Array.Fill(results, sentinel);
            var read = gpu?.ReadbackBytes ?? 0; var submissions = gpu?.SubmissionCount ?? 0;
            PhysicsServer.BodyGetTransform(space, mixed, results);
            Check(results[^1] == sentinel && results[0] == results[3] && results[1] == scene.GlobalTransform,
                "Duplicates, input order, scene presentation and untouched destination tail");
            if (gpu is not null) Check(gpu.ReadbackBytes - read == 8 + 3 * 16 && gpu.SubmissionCount - submissions == 1,
                "Only three requested dynamic raw poses use one compact read and status fence");
            read = gpu?.ReadbackBytes ?? 0;
            for (var i = 0; i < mixed.Length; i++) Check(results[i] == PhysicsServer.BodyGetTransform(mixed[i]), "Batch matches scalar pose exactly");
            Check((gpu?.ReadbackBytes ?? 0) == read && PhysicsServer.Service.BodyRuntime(bodies[3]).View is null,
                "Pose cache serves scalar reads without full state publication or creating a direct view");
            Check(PhysicsServer.BodyGetLinearVelocity(bodies[3]).DistanceTo(new(60, 0)) < .001f && !PhysicsServer.BodyGetSleeping(bodies[3]),
                "Pose cache does not replace velocity or sleeping state");

            var stale = PhysicsServer.BodyCreate(); PhysicsServer.FreeRID(stale);
            foreach (var invalid in new[] { default(RID), stale, foreign, area })
            {
                Array.Fill(results, sentinel); read = gpu?.ReadbackBytes ?? 0;
                Reject<ArgumentException>(() => PhysicsServer.BodyGetTransform(space, new[] { bodies[0], invalid }, results));
                Check(results.All(value => value == sentinel) && (gpu?.ReadbackBytes ?? 0) == read, "Invalid batch is rejected before publication or destination writes");
            }
            Reject<ArgumentException>(() => PhysicsServer.BodyGetTransform(space, mixed, new Transform[1]));
            Reject<ArgumentException>(() => PhysicsServer.BodyGetTransform(default, Array.Empty<RID>(), Array.Empty<Transform>()));
            var repeated = Enumerable.Repeat(bodies[0], 513).ToArray(); var repeatedResults = new Transform[repeated.Length];
            PhysicsServer.BodyGetTransform(space, repeated, repeatedResults);
            Check(repeatedResults.All(value => value == repeatedResults[0]), "Request count may exceed the resident body population");
            var pose = new Transform(-.4f, new(6000, 200)); PhysicsServer.BodySetTransform(bodies[0], pose);
            PhysicsServer.BodyGetTransform(space, bodies, repeatedResults);
            Check(repeatedResults[0].Origin.DistanceTo(pose.Origin) < .001f, "Queued authored edits precede compact pose reads");
            using (var checkpoint = PhysicsServer.SpaceCreateCheckpoint(space))
            {
                tree.PhysicsFrame(1d / 60); PhysicsServer.BodyGetTransform(space, bodies, repeatedResults);
                checkpoint.Restore(); Check(PhysicsServer.BodyGetTransform(bodies[0]).Origin.DistanceTo(pose.Origin) < .001f, "Checkpoint restores compact cache with its state epoch");
            }
            using (var map = new PhysicsSnapshotMap(space))
            {
                for (var i = 0; i < bodies.Length; i++) map.Bind((ulong)i + 1, 1, bodies[i]);
                map.Bind(129, 1, scene.GetRID()); map.Bind(130, 1, area);
                var snapshot = new PhysicsSnapshot(); map.Capture(snapshot);
                PhysicsServer.BodySetTransform(bodies[0], new(0, new(7000, 300))); PhysicsServer.BodyGetTransform(space, bodies, repeatedResults);
                map.Apply(snapshot); Check(PhysicsServer.BodyGetTransform(bodies[0]).Origin.DistanceTo(pose.Origin) < .001f, "Portable state application invalidates the compact pose cache");
            }
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) { tree.PhysicsFrame(1d / 60); PhysicsServer.BodyGetTransform(space, bodies, repeatedResults); }
            var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) { tree.PhysicsFrame(1d / 60); PhysicsServer.BodyGetTransform(space, bodies, repeatedResults); }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Full step and caller-owned batch allocate {owner}/{all} owner/all-thread bytes");
            var rejected = false;
            var thread = new Thread(() => { try { PhysicsServer.BodyGetTransform(space, Array.Empty<RID>(), Array.Empty<Transform>()); } catch (InvalidOperationException) { rejected = true; } });
            thread.Start(); thread.Join(); Check(rejected, "Even an empty batch checks the space owner thread");
            var stepping = typeof(PhysicsSpace).GetField("_stepping", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            stepping.SetValue(data, true);
            try { Reject<InvalidOperationException>(() => PhysicsServer.BodyGetTransform(space, Array.Empty<RID>(), Array.Empty<Transform>())); }
            finally { stepping.SetValue(data, false); }
            Console.WriteLine($"{backend}: mixed body poses, compact transfer, guards, rollback/portable apply and 128 bodies/64 full warmed ticks: {owner}/{all} owner/all-thread B passed.");
        }
        finally
        {
            foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
            PhysicsServer.FreeRID(foreign); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(otherSpace);
        }
        if (backend == PhysicsServer.Backend.GPU) FailedWorld();
    }
    private static void FailedWorld()
    {
        var space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU); var body = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetSpace(body, space); PhysicsServer.SpaceSetActive(space, true);
            PhysicsServer.AreaSetGravity(space, 980);
            Reject<AggregateException>(() => PhysicsServer.SpaceStep(space, float.MaxValue));
            var sentinel = new Transform(0, new(42, 43)); var output = new[] { sentinel };
            Reject<InvalidOperationException>(() => PhysicsServer.BodyGetTransform(space, new[] { body }, output));
            Reject<InvalidOperationException>(() => PhysicsServer.BodyGetTransform(space, Array.Empty<RID>(), Array.Empty<Transform>()));
            Check(output[0] == sentinel && PhysicsServer.SpaceGetBackend(space) == PhysicsServer.Backend.GPU, "Failed GPU batch preserves output and selected backend");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
    }
}
