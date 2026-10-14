using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsGPUDemandPublicationTests
{
    internal static void Run()
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_GPU_PUBLICATION_THREAD") == "1") ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, true);
        try { Raw(); Mixed(); Areas(); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
    private static void Raw()
    {
        var space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU);
        var internalSpace = PhysicsServer.Service.GetSceneSpace(space); var store = internalSpace.GPUStore!;
        const int count = 512;
        var bodies = new RID[count];
        try
        {
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.AreaSetGravity(space, 0); PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0);
            for (var i = 0; i < count; i++)
            {
                var body = bodies[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetCanSleep(body, false);
                PhysicsServer.BodySetLinearVelocity(body, new(60, 0)); PhysicsServer.BodySetSpace(body, space);
            }
            Collect(); for (var i = 0; i < 64; i++) PhysicsServer.SpaceStep(space, 1d / 60);
            var read = store.ReadbackBytes; var publications = store.ChangePublicationCount;
            var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) PhysicsServer.SpaceStep(space, 1d / 60);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            var bytes = (store.ReadbackBytes - read) / 64;
            Check(owner == 0 && all == 0 && store.ChangePublicationCount == publications && bytes < 1024,
                $"Unobserved raw worlds must keep state on device: {owner}/{all} B, {bytes} B readback/step");
            read = store.ReadbackBytes;
            Near(PhysicsServer.BodyGetTransform(bodies[0]).Origin.X, 128);
            Near(PhysicsServer.BodyGetLinearVelocity(bodies[0]).X, 60);
            Check(store.ReadbackBytes - read <= 128, "Repeated getters for one body share one selected snapshot");
            using (var view = PhysicsServer.BodyGetDirectState(bodies[1])!)
            {
                PhysicsServer.AreaSetGravity(space, 60); PhysicsServer.SpaceStep(space, 1d / 60);
                read = store.ReadbackBytes;
                Near(view.TotalGravity.Y, 60); Near(view.LinearVelocity.Y, 1);
                Check(store.ReadbackBytes == read, "Retained raw body view consumes current batched completion without another read");
            }
            read = store.ReadbackBytes; PhysicsServer.SpaceStep(space, 1d / 60);
            Check(store.ReadbackBytes - read < 1024, "Disposed view no longer requires selected completion");
            PhysicsServer.BodySetMode(bodies[2], PhysicsServer.BodyMode.Kinematic);
            PhysicsServer.BodySetTransform(bodies[2], new(0, new(200, 0))); PhysicsServer.SpaceStep(space, 1d / 60);
            Near(PhysicsServer.BodyGetTransform(bodies[2]).Origin.X, 200);
            PhysicsServer.BodySetTransform(bodies[2], new(0, new(201, 0))); PhysicsServer.SpaceStep(space, 1d / 60);
            Near(PhysicsServer.BodyGetLinearVelocity(bodies[2]).X, 60);
            PhysicsServer.BodySetSpace(bodies[0], default);
            Check(PhysicsServer.BodyGetTransform(bodies[0]).Origin.X > 128 && PhysicsServer.BodyGetLinearVelocity(bodies[0]).Y > 1,
                "Detachment captures the latest previously unobserved device state");
            Console.WriteLine($"Raw GPU demand publication: {count} bodies, 64 full warmed steps, {owner}/{all} owner/all-thread B, {bytes} readback B/step; selected getters, fields, views, kinematic completion and detach passed.");
        }
        finally { foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
    }
    private static void Mixed()
    {
        using var world = new World(PhysicsServer.Backend.GPU); using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 5 };
        var scene = new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(60, 0), Name = "Observed" };
        scene.AddChild(new CollisionShape { Shape = shape }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        var bodies = new RID[512]; var store = PhysicsServer.Service.GetSceneSpace(world.Space).GPUStore!;
        try
        {
            for (var i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetCanSleep(body, false); PhysicsServer.BodySetSpace(body, world.Space);
            }
            Collect(); for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
            var read = store.ReadbackBytes; var publications = store.ChangePublicationCount;
            var before = scene.Position.X;
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            var bytes = (store.ReadbackBytes - read) / 64;
            Check(owner == 0 && all == 0 && scene.Position.X > before + 50 && store.ChangePublicationCount == publications && bytes < 2048,
                $"Mixed worlds publish actual scene consumers without raw-body mirrors: {owner}/{all} B, {bytes} readback B/step");
            using var view = PhysicsServer.BodyGetDirectState(bodies[0])!;
            var later = 0;
            PhysicsServer.BodySetForceIntegrationCallback(bodies[1], _ => PhysicsServer.BodySetStateSyncCallback(bodies[2], _ => later++));
            tree.PhysicsFrame(1d / 60);
            Check(later == 1 && scene.Position.X > before + 50, "Switching to global callback publication preserves later registration and scene completion");
            PhysicsServer.BodySetForceIntegrationCallback(bodies[1], null); PhysicsServer.BodySetStateSyncCallback(bodies[2], null);
            PhysicsServer.BodySetTransform(bodies[0], new(0, new(99, 0))); _ = view.Transform;
            PhysicsServer.BodySetTransform(bodies[0], new(0, new(100, 0))); tree.PhysicsFrame(1d / 60);
            Check(view.Transform.Origin.X == 100, "Selected completion discards an intermediate getter cache after authored edits");
            Console.WriteLine($"Mixed GPU demand publication: 1 scene + {bodies.Length} raw bodies, 64 warmed full steps, {owner}/{all} owner/all-thread B, {bytes} readback B/step; scene, raw view and callback transitions passed.");
        }
        finally { foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body); }
    }
    private static void Near(float value, float expected) => Check(Math.Abs(value - expected) < .02f, "Prescribed constant motion/field agrees within .02 scene units (or per second)");
    private static void Areas()
    {
        using var world = new World(PhysicsServer.Backend.GPU); using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 2 };
        var mover = new RigidBody { GravityScale = 0, CanSleep = false }; mover.AddChild(new CollisionShape { Shape = shape }); root.AddChild(mover);
        var areas = new Area[64];
        for (var i = 0; i < areas.Length; i++)
        {
            var area = areas[i] = new Area { Name = $"Area{i}", Position = new(i * 100, 0), Monitoring = false };
            area.AddChild(new CollisionShape { Shape = shape }); root.AddChild(area);
        }
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate(); PhysicsServer.BodySetSpace(raw, world.Space);
        var rawArea = PhysicsServer.AreaCreate(); PhysicsServer.AreaAddShape(rawArea, shape.GetRID());
        PhysicsServer.AreaSetTransform(rawArea, new(0, new(8000, 0))); PhysicsServer.AreaSetSpace(rawArea, world.Space);
        _ = PhysicsServer.AreaGetTransform(rawArea);
        var store = PhysicsServer.Service.GetSceneSpace(world.Space).GPUStore!;
        var samples = new double[128];
        try
        {
            Collect(); GC.WaitForPendingFinalizers();
            for (var i = 0; i < 128; i++) Frame();
            var read = store.ReadbackBytes; var publications = store.ChangePublicationCount;
            var upload = store.UploadBytes; var wait = store.WaitMS; var submissions = store.SubmissionCount;
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++)
            {
                var started = System.Diagnostics.Stopwatch.GetTimestamp(); Frame();
                samples[i] = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            }
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
            var bytes = (store.ReadbackBytes - read) / 128;
            Array.Sort(samples);
            Console.WriteLine($"GPU Area demand: 64 sensors + scene/raw bodies, 128 full edited frames, {ownerBytes}/{allBytes} B, {bytes} readback B/step.");
            Console.WriteLine($"GPU Area cost: p50/p95/p99={samples[64]:F4}/{samples[121]:F4}/{samples[126]:F4} ms; upload={(store.UploadBytes - upload) / 128} B/step, wait={(store.WaitMS - wait) / 128:F4} ms/step, submissions={(store.SubmissionCount - submissions) / 128}; software Vulkan only.");
            Check(ownerBytes == 0 && allBytes == 0 && bytes < 1024 && store.ChangePublicationCount == publications,
                "Authored Area poses do not require a complete state readback at every mixed-world completion.");
            read = store.ReadbackBytes;
            foreach (var area in areas) Check(PhysicsServer.AreaGetTransform(area.GetRID()).Origin == area.GlobalPosition, "Area transform reads retain current authored poses.");
            Check(store.ReadbackBytes == read, "Unchanged Area transform getters do not trigger individual device reads.");
            areas[0].Monitoring = true;
            var entered = 0; var exited = 0;
            areas[0].BodyEntered += other => { Check(ReferenceEquals(other, mover), "Moved Area retains its collider identity."); entered++; };
            areas[0].BodyExited += _ => exited++;
            mover.Position = Vector2.Zero; tree.PhysicsFrame(.01);
            areas[0].Position = new(50, 0); tree.PhysicsFrame(.01);
            areas[0].Position = Vector2.Zero; areas[0].Rotation = .3f; tree.PhysicsFrame(.01);
            Check(entered == 2 && exited == 1 && PhysicsServer.AreaGetTransform(areas[0].GetRID()).IsEqualApprox(areas[0].GlobalTransform),
                "Area pose edits still reach GPU sensing and public transform reads.");
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            areas[0].Position = new(70, 0); tree.PhysicsFrame(.01);
            var beforeRestore = entered + exited;
            checkpoint.Restore();
            Check(entered + exited == beforeRestore && PhysicsServer.AreaGetTransform(areas[0].GetRID()).IsEqualApprox(areas[0].GlobalTransform),
                "Area pose cache follows restored device/scene state without duplicate effects.");
            var rawPose = new Transform(.4f, new Vector2(9000, 0)); PhysicsServer.AreaSetTransform(rawArea, rawPose);
            read = store.ReadbackBytes;
            Check(PhysicsServer.AreaGetTransform(rawArea).IsEqualApprox(rawPose) && store.ReadbackBytes == read, "Raw Area edits expose authored pose without an observer readback.");
            PhysicsServer.AreaSetSpace(rawArea, default); PhysicsServer.AreaSetSpace(rawArea, world.Space);
            Check(PhysicsServer.AreaGetTransform(rawArea).IsEqualApprox(rawPose), "Raw Area transfer preserves the latest authored pose.");
            using var point = new PhysicsPointQueryParameters { Position = rawPose.Origin, CollideWithBodies = false, CollideWithAreas = true };
            var hits = new PhysicsPointResult[8]; var hitCount = world.DirectSpaceState.IntersectPoint(point, hits);
            var sawRawArea = false; for (var i = 0; i < hitCount; i++) sawRawArea |= hits[i].ColliderRID == rawArea;
            Check(sawRawArea && hitCount == 1, "Resident point queries consume the edited raw Area pose.");
            var rawEntered = 0;
            PhysicsServer.AreaSetMonitorCallback(rawArea, (status, rid, _, _, _) =>
            { if (status == PhysicsServer.AreaBodyStatus.Added) { Check(rid == mover.GetRID(), "Raw sensing retains the moved body's identity."); rawEntered++; } });
            rawPose = new(.7f, Vector2.Zero); PhysicsServer.AreaSetTransform(rawArea, rawPose); tree.PhysicsFrame(.01);
            Check(rawEntered == 1, "Raw Area monitoring uses the edited sensor pose after numerical completion.");
            using var map = new PhysicsSnapshotMap(world.Space); var snapshot = new PhysicsSnapshot();
            map.Bind(1, 1, mover.GetRID()); map.Bind(2, 1, raw); map.Bind(3, 1, rawArea);
            for (var i = 0; i < areas.Length; i++) map.Bind((ulong)(100 + i), 1, areas[i].GetRID());
            map.Capture(snapshot); var areaPose = areas[0].GlobalTransform;
            areas[0].Position = new(80, 0); PhysicsServer.AreaSetTransform(rawArea, new(0, new(9100, 0))); tree.PhysicsFrame(.01);
            beforeRestore = entered + exited; var beforeRawRestore = rawEntered; map.Apply(snapshot);
            Check(entered + exited == beforeRestore && rawEntered == beforeRawRestore && areas[0].GlobalTransform.IsEqualApprox(areaPose) &&
                PhysicsServer.AreaGetTransform(areas[0].GetRID()).IsEqualApprox(areaPose) && PhysicsServer.AreaGetTransform(rawArea).IsEqualApprox(rawPose),
                "Portable Area restoration replaces cached poses without replaying gameplay effects.");
            void Frame() { mover.Position = mover.Position.X == 0 ? new(10000, 0) : Vector2.Zero; tree.PhysicsFrame(.01); }
        }
        finally { PhysicsServer.FreeRID(rawArea); PhysicsServer.FreeRID(raw); }
    }
    private static void Collect() { GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); }
}
