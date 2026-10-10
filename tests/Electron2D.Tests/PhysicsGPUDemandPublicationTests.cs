using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsGPUDemandPublicationTests
{
    internal static void Run()
    {
        Raw(); Mixed();
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
    private static void Collect() { GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); }
}
