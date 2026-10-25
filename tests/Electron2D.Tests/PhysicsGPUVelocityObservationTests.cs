using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsGPUVelocityObservationTests
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
        using var world = new World(PhysicsServer.Backend.GPU);
        using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 1 };
        var scene = new RigidBody { CanSleep = false, Mass = 1, Inertia = 1, LinearDamp = 0, AngularDamp = 0, LinearVelocity = new(2, 1), AngularVelocity = .25f };
        scene.AddChild(new CollisionShape { Shape = shape }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        var rawA = PhysicsServer.BodyCreate(); var rawB = PhysicsServer.BodyCreate();
        try
        {
            var bodies = new[] { scene.GetRID(), rawA, rawB };
            for (var i = 1; i < bodies.Length; i++)
            {
                var body = bodies[i]; PhysicsServer.BodyAddShape(body, shape.GetRID());
                PhysicsServer.BodySetMass(body, 1); PhysicsServer.BodySetInertia(body, 1); PhysicsServer.BodySetCanSleep(body, false);
                PhysicsServer.BodySetTransform(body, new(0, new Vector2(i * 100, 0)));
                PhysicsServer.BodySetLinearVelocity(body, new(2, 1)); PhysicsServer.BodySetAngularVelocity(body, .25f);
                PhysicsServer.BodySetSpace(body, world.Space);
            }
            PhysicsServer.AreaSetGravity(world.Space, 0); PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
            var views = bodies.Select(body => PhysicsServer.BodyGetDirectState(body)!).ToArray();
            var linear = new Vector2[bodies.Length]; var angular = new float[bodies.Length];
            var store = PhysicsServer.Service.GetSceneSpace(world.Space).GPUStore!;
            Frame();
            for (var i = 0; i < 128; i++) { Observe(); AuthorAndRead(); Frame(); }
            Observe();
            var submissions = store.SubmissionCount; var readback = store.ReadbackBytes;
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            const int samples = 32;
            for (var i = 0; i < samples; i++) AuthorAndRead();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Console.WriteLine($"Velocity observations after wake/force: {bodies.Length} scene/raw receivers, {samples} rounds, submissions={store.SubmissionCount - submissions}, readback={store.ReadbackBytes - readback}, managed={owner}/{all} B.");
            Check(owner == 0 && all == 0, "Warmed scene/raw velocity observations allocate zero managed bytes");
            Check(store.SubmissionCount == submissions && store.ReadbackBytes == readback, "Wake/force authoring preserves current velocity observations without a device completion");
            Frame();
            for (var i = 0; i < bodies.Length; i++)
            {
                Check(PhysicsServer.BodyGetLinearVelocity(bodies[i]).DistanceTo(linear[i] + new Vector2(samples * 3f / 60, 0)) < .0001f,
                    "Queued forces integrate once at 1/60 s; velocity tolerance .0001 scene units/s bounds float summation");
                Check(MathF.Abs(PhysicsServer.BodyGetAngularVelocity(bodies[i]) - angular[i] - samples / 60f) < .0001f,
                    "Queued torque integrates once with unit inertia within .0001 radians/s");
            }
            foreach (var body in bodies) { PhysicsServer.BodySetLinearVelocity(body, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(body, 0); }
            Frame(); Observe();
            for (var i = 0; i < 16; i++)
            {
                Frame(); submissions = store.SubmissionCount;
                AuthorAndRead(stationary: true);
                Check(store.SubmissionCount == submissions, "Device-confirmed unchanged publication also qualifies velocity after a later wake");
            }
            Observe(); submissions = store.SubmissionCount;
            PhysicsServer.BodySetLinearVelocity(rawA, new(19, -2));
            Check(PhysicsServer.BodyGetLinearVelocity(rawB) == linear[2] && store.SubmissionCount == submissions,
                "Another body's authored velocity does not invalidate an unchanged velocity observation");
            Check(PhysicsServer.BodyGetLinearVelocity(rawA) == new Vector2(19, -2), "Authored velocity invalidates its own observation");
            PhysicsServer.BodySetAngularVelocity(rawA, 3);
            Check(PhysicsServer.BodyGetAngularVelocity(rawA) == 3, "Authored angular velocity is immediately visible");
            PhysicsServer.BodyApplyCentralImpulse(rawA, new(2, 0));
            Check(PhysicsServer.BodyGetLinearVelocity(rawA) == new Vector2(21, -2), "An impulse invalidates its receiver's velocity");
            PhysicsServer.BodySetSleeping(rawA, true);
            Check(PhysicsServer.BodyGetLinearVelocity(rawA) == Vector2.Zero && PhysicsServer.BodyGetAngularVelocity(rawA) == 0,
                "Explicit sleep clears cached motion even with automatic sleep disabled");
            PhysicsServer.BodyApplyCentralForce(rawA, new(1, 0));
            Check(PhysicsServer.BodyGetLinearVelocity(rawA) == Vector2.Zero && !PhysicsServer.BodyGetSleeping(rawA),
                "Velocity-only reuse does not suppress current wake observation");
            Frame();
            using var map = new PhysicsSnapshotMap(world.Space);
            for (var i = 0; i < bodies.Length; i++) map.Bind((ulong)i + 1, 1, bodies[i]);
            var packet = new PhysicsSnapshot(65536); map.Capture(packet);
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space); Observe();
            var saved = linear.ToArray(); var savedAngular = angular.ToArray();
            Future(); checkpoint.Restore(); Restored();
            Future(); map.Apply(packet); Restored();
            Console.WriteLine("Velocity observation invalidation, wake, numerical advancement and private/portable correction passed.");

            void Frame() => tree.PhysicsFrame(1d / 60);
            void Observe()
            {
                for (var i = 0; i < bodies.Length; i++) { linear[i] = PhysicsServer.BodyGetLinearVelocity(bodies[i]); angular[i] = PhysicsServer.BodyGetAngularVelocity(bodies[i]); }
            }
            void AuthorAndRead(bool stationary = false)
            {
                for (var i = 0; i < bodies.Length; i++)
                {
                    PhysicsServer.BodyApplyCentralForce(bodies[i], stationary ? Vector2.Zero : new(3, 0)); PhysicsServer.BodyApplyTorque(bodies[i], stationary ? 0 : 1);
                    Check(PhysicsServer.BodyGetLinearVelocity(bodies[i]) == linear[i] && PhysicsServer.BodyGetAngularVelocity(bodies[i]) == angular[i] &&
                        views[i].LinearVelocity == linear[i] && views[i].AngularVelocity == angular[i],
                        "Force/wake authoring preserves scene/raw/direct velocity until positive simulation");
                }
            }
            void Future() { PhysicsServer.BodyApplyCentralImpulse(rawA, new(7, 0)); Frame(); Observe(); }
            void Restored()
            {
                for (var i = 0; i < bodies.Length; i++)
                    Check(PhysicsServer.BodyGetLinearVelocity(bodies[i]) == saved[i] && PhysicsServer.BodyGetAngularVelocity(bodies[i]) == savedAngular[i],
                        "Correction restores velocity observation values and invalidation history");
            }
        }
        finally { PhysicsServer.FreeRID(rawA); PhysicsServer.FreeRID(rawB); }
    }
}
