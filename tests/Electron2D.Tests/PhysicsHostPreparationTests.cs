using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsHostPreparationTests
{
    internal static void Run(bool includeGPU = true)
    {
        Verify(PhysicsServer.Backend.CPU);
        VerifyCCD(PhysicsServer.Backend.CPU);
        if (includeGPU) { Verify(PhysicsServer.Backend.GPU); VerifyCCD(PhysicsServer.Backend.GPU); VerifyCallbackBatch(); }
    }

    private static void Verify(PhysicsServer.Backend backend)
    {
        using var world = new World(backend);
        using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 1 };
        var scene = new RigidBody { Name = "Scene", Position = new(100, 0), Mass = 2, Inertia = 4, CanSleep = false };
        scene.AddChild(new CollisionShape { Shape = shape }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        var space = world.Space; var raw = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetMass(raw, 2); PhysicsServer.BodySetInertia(raw, 4); PhysicsServer.BodySetCanSleep(raw, false);
            PhysicsServer.BodyAddShape(raw, shape.GetRID()); PhysicsServer.BodySetTransform(raw, new(0, new(-100, 0))); PhysicsServer.BodySetSpace(raw, space);
            PhysicsServer.AreaSetGravity(space, 0); PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0);
            Step(); Step();
            using var sceneView = PhysicsServer.BodyGetDirectState(scene.GetRID())!;
            using var rawView = PhysicsServer.BodyGetDirectState(raw)!;

            Forces(new(120, 0), 240); Velocity(0, 0); Step(); Motion(1, 0, 1);
            Forces(default, 0); PhysicsServer.AreaSetGravity(space, 60);
            scene.GravityScale = 2; PhysicsServer.BodySetGravityScale(raw, 2);
            Velocity(0, 0); Step(); Motion(0, 2, 0);
            Near(sceneView.TotalGravity.Y, 120); Near(rawView.TotalGravity.Y, 120);

            PhysicsServer.AreaSetGravity(space, 0); PhysicsServer.AreaSetLinearDamp(space, 3); PhysicsServer.AreaSetAngularDamp(space, 4);
            scene.LinearDamp = 2; scene.AngularDamp = 6; PhysicsServer.BodySetLinearDamp(raw, 2); PhysicsServer.BodySetAngularDamp(raw, 6);
            scene.LinearDampMode = RigidBody.DampMode.Replace; scene.AngularDampMode = RigidBody.DampMode.Replace;
            PhysicsServer.BodySetLinearDampMode(raw, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(raw, RigidBody.DampMode.Replace);
            Velocity(60, 12); Step(); Motion(58, 0, 10.8f);
            Near(sceneView.TotalLinearDamp, 2); Near(rawView.TotalAngularDamp, 6);
            scene.LinearDampMode = RigidBody.DampMode.Combine; scene.AngularDampMode = RigidBody.DampMode.Combine;
            PhysicsServer.BodySetLinearDampMode(raw, RigidBody.DampMode.Combine); PhysicsServer.BodySetAngularDampMode(raw, RigidBody.DampMode.Combine);
            Velocity(60, 12); Step(); Motion(55, 0, 10);

            scene.CustomIntegrator = true; PhysicsServer.BodySetOmitForceIntegration(raw, true);
            Forces(new(120, 0), 240); PhysicsServer.AreaSetGravity(space, 60);
            Velocity(60, 12); Step(); Motion(60, 0, 12);
            scene.LockRotation = true; PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.RigidLinear);
            Velocity(0, 12); Step(); Motion(0, 0, 0);
            scene.LockRotation = false; PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Rigid);
            Velocity(0, 12); Step(); Motion(0, 0, 12);

            scene.Freeze = true; PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Static);
            var position = scene.Position; var rawPosition = PhysicsServer.BodyGetTransform(raw).Origin;
            Step(); Check(scene.Position == position && PhysicsServer.BodyGetTransform(raw).Origin == rawPosition, "Frozen/static poses stay fixed");
            scene.Freeze = false; PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Rigid);
            scene.CustomIntegrator = false; PhysicsServer.BodySetOmitForceIntegration(raw, false);
            scene.LinearDamp = scene.AngularDamp = 0; PhysicsServer.BodySetLinearDamp(raw, 0); PhysicsServer.BodySetAngularDamp(raw, 0);
            PhysicsServer.AreaSetGravity(space, 0); PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0);
            Velocity(0, 0); Step(); Motion(1, 0, 1);
            using (var checkpoint = PhysicsServer.SpaceCreateCheckpoint(space))
            {
                Forces(default, 0); Step();
                checkpoint.Restore(); Step(); Motion(2, 0, 2);
            }
            root.RemoveChild(scene); PhysicsServer.BodySetSpace(raw, default);
            root.AddChild(scene); PhysicsServer.BodySetSpace(raw, space);
            Reject<ObjectDisposedException>(() => _ = rawView.Transform); Reject<ObjectDisposedException>(() => _ = sceneView.Transform);
            Velocity(0, 0); Step(); Motion(1, 0, 1);

            Forces(default, 0); scene.CustomIntegrator = true; PhysicsServer.BodySetOmitForceIntegration(raw, true);
            var negativeZero = BitConverter.Int32BitsToSingle(int.MinValue);
            PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetGravityScale(raw, negativeZero);
            PhysicsServer.BodySetLinearDamp(raw, negativeZero); PhysicsServer.BodySetAngularDamp(raw, negativeZero);
            PhysicsServer.BodySetConstantForce(raw, new(negativeZero, negativeZero)); PhysicsServer.BodySetConstantTorque(raw, negativeZero);
            Check(BitConverter.SingleToInt32Bits(PhysicsServer.BodyGetGravityScale(raw)) == int.MinValue &&
                BitConverter.SingleToInt32Bits(PhysicsServer.BodyGetLinearDamp(raw)) == int.MinValue &&
                BitConverter.SingleToInt32Bits(PhysicsServer.BodyGetAngularDamp(raw)) == int.MinValue &&
                BitConverter.SingleToInt32Bits(PhysicsServer.BodyGetConstantForce(raw).X) == int.MinValue &&
                BitConverter.SingleToInt32Bits(PhysicsServer.BodyGetConstantTorque(raw)) == int.MinValue,
                "Skipping equivalent device policy writes preserves authored signed-zero values");
            using var currentScene = PhysicsServer.BodyGetDirectState(scene.GetRID())!;
            using var currentRaw = PhysicsServer.BodyGetDirectState(raw)!;
            foreach (var angle in new[] { -Mathf.Pi, -1.2f, 0f, Mathf.Pi / 2, Mathf.Pi - .00001f, Mathf.Pi })
            {
                scene.Rotation = angle; PhysicsServer.BodySetTransform(raw, new(angle, new(-100, 0)));
                Velocity(0, 2); Step();
                var a = PhysicsServer.BodyGetTransform(scene.GetRID()); var b = PhysicsServer.BodyGetTransform(raw);
                Basis(a); Basis(b);
                Check(a.IsEqualApprox(currentScene.Transform) && b.IsEqualApprox(currentRaw.Transform), "Scene, raw and direct views agree on the published transform");
            }
            Console.WriteLine($"Host preparation {backend}: live force/field/damping/omission edits, mode/lock/freeze, checkpoint, reentry and transform views passed.");
        }
        finally { PhysicsServer.FreeRID(raw); }

        void Step() => tree.PhysicsFrame(1d / 60);
        void Forces(Vector2 force, float torque)
        { scene.ConstantForce = force; scene.ConstantTorque = torque; PhysicsServer.BodySetConstantForce(raw, force); PhysicsServer.BodySetConstantTorque(raw, torque); }
        void Velocity(float linear, float angular)
        { scene.LinearVelocity = new(linear, 0); scene.AngularVelocity = angular; PhysicsServer.BodySetLinearVelocity(raw, new(linear, 0)); PhysicsServer.BodySetAngularVelocity(raw, angular); }
        void Motion(float x, float y, float angular)
        {
            Near(scene.LinearVelocity.X, x); Near(scene.LinearVelocity.Y, y); Near(scene.AngularVelocity, angular);
            var velocity = PhysicsServer.BodyGetLinearVelocity(raw); Near(velocity.X, x); Near(velocity.Y, y); Near(PhysicsServer.BodyGetAngularVelocity(raw), angular);
        }
    }
    private static void VerifyCCD(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var root = new SubViewport { World = world };
        using var ball = new CircleShape { Radius = 1 }; using var wallShape = new RectangleShape { Size = new(.2f, 300) };
        var body = new RigidBody { Name = "Projectile", GravityScale = 0, CanSleep = false };
        body.AddChild(new CollisionShape { Shape = ball }); root.AddChild(body);
        var wall = new StaticBody { Name = "Wall", Position = new(20, 50) };
        wall.AddChild(new CollisionShape { Shape = wallShape }); root.AddChild(wall);
        using var tree = new SceneTree(root); var raw = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodyAddShape(raw, ball.GetRID()); PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false);
            PhysicsServer.BodySetTransform(raw, new(0, new(0, 100))); PhysicsServer.BodySetSpace(raw, world.Space);
            PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
            tree.PhysicsFrame(.02);
            Check(PhysicsServer.SpaceGetBackend(world.Space) == backend, "CCD edits run through the requested public backend");
            foreach (var mode in new[] { CCDMode.CastRay, CCDMode.Disabled, CCDMode.CastShape })
            {
                body.Position = default; body.LinearVelocity = new(3000, 0); body.ContinuousCD = mode;
                PhysicsServer.BodySetTransform(raw, new(0, new(0, 100))); PhysicsServer.BodySetLinearVelocity(raw, new(3000, 0));
                PhysicsServer.BodySetContinuousCollisionDetectionMode(raw, mode);
                tree.PhysicsFrame(.02);
                var x = PhysicsServer.BodyGetTransform(raw).Origin.X;
                Check(mode == CCDMode.Disabled ? body.Position.X > 25 && x > 25 : body.Position.X < 20 && x < 20,
                    "A live CCD edit changes thin-wall crossing after a previously prepared step");
            }
            Console.WriteLine($"Host preparation {backend}: live CastRay/Disabled/CastShape edits change scene/raw trajectories.");
        }
        finally { PhysicsServer.FreeRID(raw); }
    }

    private static void VerifyCallbackBatch()
    {
        const int count = 32;
        var space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU); var bodies = new RID[count];
        var store = PhysicsServer.Service.GetSceneSpace(space).GPUStore!;
        var calls = 0; var expectedX = 0f; var expectedY = 1f;
        Action<PhysicsDirectBodyState> callback = state =>
        { calls++; Near(state.LinearVelocity.X, expectedX); Near(state.LinearVelocity.Y, expectedY); };
        try
        {
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.AreaSetGravity(space, 60);
            PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0);
            for (var i = 0; i < count; i++)
            {
                var body = bodies[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetCanSleep(body, false);
                PhysicsServer.BodySetSpace(body, space); PhysicsServer.BodySetForceIntegrationCallback(body, callback);
            }
            Step(); Step(); expectedX = 1;
            foreach (var body in bodies) PhysicsServer.BodyApplyCentralForce(body, new(60, 0));
            var ordinary = Step(); expectedY = 2;
            foreach (var body in bodies)
            { PhysicsServer.BodySetGravityScale(body, 2); PhysicsServer.BodyApplyCentralForce(body, new(60, 0)); }
            var edited = Step();
            Console.WriteLine($"GPU callback preparation: ordinary={ordinary}, changed policies={edited} submissions for {count} bodies.");
            Check(edited <= ordinary + 4, "Policy edits and pre-step consumers use a shared publication, without per-body GPU waits");
        }
        finally { foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
        long Step()
        {
            foreach (var body in bodies) PhysicsServer.BodySetLinearVelocity(body, Vector2.Zero);
            calls = 0; var before = store.SubmissionCount;
            PhysicsServer.SpaceStep(space, 1d / 60);
            Check(calls == count, "Every live integration callback executes once");
            return store.SubmissionCount - before;
        }
    }

    private static void Basis(Transform value)
    {
        Check(value.IsFinite(), "Published transform is finite");
        Near(value.X.LengthSquared(), 1, .000002f); Near(value.Y.LengthSquared(), 1, .000002f);
        Near(value.X.Dot(value.Y), 0, .000002f); Near(value.Determinant(), 1, .000002f);
    }
    private static void Near(float actual, float expected, float tolerance = .002f) =>
        Check(MathF.Abs(actual - expected) <= tolerance, $"Expected {expected}, got {actual}; tolerance {tolerance}");
}
