using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsGPUDensePublicationTests
{
    internal static void Run()
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, Environment.GetEnvironmentVariable("ELECTRON2D_TEST_GPU_PUBLICATION_THREAD") == "1");
        try { Verify(); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void Verify()
    {
        const int count = 512, samples = 64, spacing = 8;
        using var world = new World(PhysicsServer.Backend.GPU);
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 1 };
        using var material = new PhysicsMaterial { Friction = 0, Bounce = 0 };
        using var rectangle = new RectangleShape { Size = new(count * spacing + spacing, 2) };
        var floor = new StaticBody { Position = new((count - 1) * spacing / 2, 2), PhysicsMaterialOverride = material };
        floor.AddChild(new CollisionShape { Shape = rectangle }); root.AddChild(floor);
        var bodies = new RigidBody[count]; var entered = 0; var contacted = new bool[count]; string? mismatch = null;
        for (var i = 0; i < count; i++)
        {
            var body = new RigidBody { Position = new(i * spacing, 0), LinearVelocity = new(1, 0), CanSleep = false, ContactMonitor = true, MaxContactsReported = 2, PhysicsMaterialOverride = material };
            var slot = i; body.BodyEntered += other => { if (other != floor) mismatch ??= $"receiver={slot}, other={other.GetType().Name}/{other.InstanceID}, floor={floor.InstanceID}, receiver pose={bodies[slot].Position}"; else contacted[slot] = true; entered++; }; body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body); bodies[i] = body;
        }
        using var tree = new SceneTree(root);
        PhysicsServer.AreaSetGravity(world.Space, 60);
        PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
        var store = PhysicsServer.Service.GetSceneSpace(world.Space).GPUStore!;
        var coldBytes = GC.GetAllocatedBytesForCurrentThread();
        Frame(); coldBytes = GC.GetAllocatedBytesForCurrentThread() - coldBytes;
        Console.WriteLine($"First positive scene frame: owner managed bytes={coldBytes}.");
        for (var i = 1; i < 128; i++) Frame();
        Console.WriteLine($"Dense scene receivers: entered={entered}, unique={contacted.Count(value => value)}, first mismatch={mismatch}, poses={bodies[0].Position}/{bodies[1].Position}/{bodies[^1].Position}, floor={floor.Position}.");
        Check(mismatch is null, "Dense contact events retain the actual floor identity");
        Check(contacted.All(value => value), "Every dense scene receiver observes its actual floor contact");
        Measure(false, true);
        var saved = bodies[0].GlobalTransform;
        using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
        using var map = new PhysicsSnapshotMap(world.Space);
        for (var i = 0; i < count; i++) map.Bind((ulong)i + 1, 1, bodies[i].GetRID());
        map.Bind(count + 1, 1, floor.GetRID());
        var packet = new PhysicsSnapshot(2 << 20); map.Capture(packet);
        Future(); checkpoint.Restore(); Restored();
        Future(); map.Apply(packet); Restored();
        foreach (var body in bodies) { body.LinearVelocity = Vector2.Zero; body.AngularVelocity = 0; }
        for (var i = 0; i < 128; i++) Frame();
        Measure(false, false);
        PhysicsServer.AreaSetGravity(world.Space, 90); Frame();
        Check(PhysicsServer.BodyGetDirectState(bodies[0].GetRID())!.TotalGravity == new Vector2(0, 90), "A zero-motion cost prediction still observes changed fields");
        var edited = bodies[1].Position + new Vector2(1, 0); bodies[1].Position = edited; Frame();
        Check(PhysicsServer.BodyGetDirectState(bodies[1].GetRID())!.Transform.Origin.DistanceTo(edited) < .001f, "A zero-motion prediction still observes an authored pose within .001 scene units");
        foreach (var body in bodies) body.Sleeping = true;
        for (var i = 0; i < 128; i++) Frame();
        Measure(true, false);
        var beforeWake = bodies[0].Position.X;
        bodies[0].ApplyCentralImpulse(new(2, 0)); Frame();
        Check(!bodies[0].Sleeping && bodies[0].Position.X > beforeWake + .02f, "A sparse observer wakes with current selected motion");
        Console.WriteLine("Pure scene GPU publication: dense contacts, sparse sleep, wake and local/portable restoration passed.");

        void Frame() => tree.PhysicsFrame(1d / 60);
        void Future() { bodies[0].ApplyCentralImpulse(new(2, 0)); Frame(); }
        void Restored()
        {
            Check(bodies[0].GlobalTransform == saved && bodies[0].LinearVelocity.X == 1, "Correction restores a selected scene observation without stale publication history");
            Frame();
        }
        void Measure(bool sleeping, bool moving)
        {
            var changes = store.ChangePublicationCount; var reports = store.ReportReadCount;
            var read = store.ReadbackBytes; var upload = store.UploadBytes; var submissions = store.SubmissionCount;
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < samples; i++) Frame();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            changes = store.ChangePublicationCount - changes;
            Console.WriteLine($"Pure scene GPU publication sleeping={sleeping}, moving={moving}: {count} bodies + floor, {samples} full frames; changes={changes}, reports={store.ReportReadCount - reports}, {owner}/{all} B; upload/readback={(store.UploadBytes - upload) / samples}/{(store.ReadbackBytes - read) / samples} B/frame, submissions={(store.SubmissionCount - submissions) / samples}.");
            Check(owner == 0 && all == 0, "Warmed pure scene full frames allocate zero managed bytes");
            Check(changes == (moving ? 0 : samples), "Pure scenes select dense body/contact completion or sparse changed-state history by actual receiver cost");
            if (!sleeping) Check(store.ReportReadCount - reports == samples, "Each dense frame publishes its capped contact snapshot");
            foreach (var body in bodies)
            {
                Check(body.Sleeping == sleeping && body.Position.IsFinite() && MathF.Abs(body.Position.Y) < .4f && body.LinearVelocity.IsFinite(), "Every scene body retains finite current motion, bounded floor penetration and sleep");
                if (!sleeping)
                {
                    var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
                    Check(state.Transform.Origin == body.GlobalPosition, "Scene and direct-state poses share the same current observation");
                    Check(state.GetContactCount() is > 0 and <= 2 && state.GetContactCollider(0) == floor.GetRID(), "Selected completion retains actual contact caps and floor identity");
                }
            }
        }
    }
}
