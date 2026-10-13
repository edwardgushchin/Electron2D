using Electron2D;

public static partial class BackendChecks
{
    public static void RunAngularCompound(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            if (backend is null) PhysicsServerManager.RegisterServer("AngularCompound", () => new Solver());
            CheckMassCenter();
            foreach (var mode in new[] { CCDMode.Disabled, CCDMode.CastRay, CCDMode.CastShape }) CheckAngularImpact(mode);
            CheckAngularImpact(CCDMode.CastShape, movingPeer: true);
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        Console.WriteLine($"Angular compound {backend?.ToString() ?? "registered"}, threaded={threaded}: mass-center motion, intermediate arc, contact identity and 0/0 warm frames passed.");

        World CreateWorld()
        {
            var world = backend is { } selected ? new World(selected) : new World("AngularCompound");
            Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
                "Angular motion uses the selected backend without startup fallback");
            return world;
        }
        void CheckMassCenter()
        {
            using var world = CreateWorld(); using var shape = new CircleShape { Radius = 1 };
            using var root = new SubViewport { World = world };
            var body = MakeBody(); body.CollisionMask = 0;
            body.AddChild(new CollisionShape { Name = "Right", Shape = shape, Position = new(8, 0) });
            body.AddChild(new CollisionShape { Name = "Left", Shape = shape, Position = new(-2, 0) }); root.AddChild(body);
            using var tree = new SceneTree(root);
            body.AngularVelocity = 10; body.LinearVelocity = new(2, -3);
            using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            var center = state.Transform.Origin + state.CenterOfMass; var local = state.CenterOfMassLocal;
            Check(local.DistanceTo(new(3, 0)) < .001f, "Automatic mass retains the asymmetric logical-slot center");
            for (var i = 1; i <= 32; i++)
            {
                tree.PhysicsFrame(.01);
                var expected = center + new Vector2(2, -3) * (.01f * i);
                var actual = state.Transform.Origin + state.CenterOfMass;
                Check(actual.DistanceTo(expected) < .002f &&
                    body.Position.DistanceTo(expected - local.Rotated(body.Rotation)) < .002f,
                    $"Linear velocity translates the mass center while the origin follows rotation: {actual}/{expected}");
                var offset = new Vector2(5, -2); var arm = offset - state.CenterOfMass;
                Check(state.GetVelocityAtLocalPosition(offset).DistanceTo(body.LinearVelocity + new Vector2(-arm.Y, arm.X) * body.AngularVelocity) < .002f,
                    "Point velocity uses the global-axis offset from the body origin and its rotated mass offset");
            }
        }
        void CheckAngularImpact(CCDMode mode, bool movingPeer = false)
        {
            const float angularSpeed = 30;
            const float armLength = 40;
            using var world = CreateWorld(); using var disk = new CircleShape { Radius = .25f };
            using var root = new SubViewport { World = world };
            var body = MakeBody(); body.Mass = 2; body.ContinuousCD = mode; body.MaxContactsReported = 8;
            body.AddChild(new CollisionShape { Name = "Disabled", Shape = disk, Disabled = true });
            body.AddChild(new CollisionShape { Name = "Right", Shape = disk, Position = new(armLength, 0) });
            body.AddChild(new CollisionShape { Name = "Left", Shape = disk, Position = new(-armLength, 0) }); root.AddChild(body);
            using var tree = new SceneTree(root);
            var target = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.BodySetMode(target, movingPeer ? PhysicsServer.BodyMode.Rigid : PhysicsServer.BodyMode.Static);
                PhysicsServer.BodyAddShape(target, disk.GetRID(), new(0, movingPeer ? new Vector2(-5, 0) : Vector2.Zero));
                // Clear ordinary quarter-step poses and their speculative contact margin.
                var point = new Vector2(armLength, 0).Rotated(.225f);
                var targetPose = new Transform(0, point + (movingPeer ? new Vector2(5, 0) : Vector2.Zero));
                PhysicsServer.BodySetTransform(target, targetPose);
                if (movingPeer)
                {
                    PhysicsServer.BodySetMass(target, 10); PhysicsServer.BodySetInertia(target, 100); PhysicsServer.BodySetCenterOfMass(target, Vector2.Zero);
                    PhysicsServer.BodySetGravityScale(target, 0); PhysicsServer.BodySetCanSleep(target, false);
                    PhysicsServer.BodySetLinearDampMode(target, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(target, 0);
                    PhysicsServer.BodySetAngularDampMode(target, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(target, 0);
                }
                PhysicsServer.BodySetSpace(target, world.Space);
                PhysicsServer.BodySetFriction(target, 0); PhysicsServer.BodySetFriction(body.GetRID(), 0);
                var callbacks = 0;
                PhysicsServer.BodySetStateSyncCallback(body.GetRID(), _ => callbacks++);
                using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
                Reset(); Step();
                Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(body.GetRID()) == mode,
                    "Angular response retains the authored CCD mode");
                if (mode == CCDMode.Disabled)
                {
                    Check(state.GetContactCount() == 0 && MathF.Abs(body.AngularVelocity - angularSpeed) < .01f,
                        $"Discrete control misses the clear-sample pure rotational arc: mode={mode}, omega={body.AngularVelocity}, contacts={state.GetContactCount()}");
                    return;
                }
                if (mode == CCDMode.CastRay)
                {
                    Check(float.IsFinite(body.AngularVelocity) && body.Position.IsFinite(), "Approximate ray motion retains finite state; ordinary contacts may intercept the arc");
                    return;
                }
                Check(state.GetContactCount() > 0 && body.AngularVelocity < .8f * angularSpeed && body.AngularVelocity > -1,
                    $"Full-shape CCD observes the intermediate angular arc and resolves its impulse: omega={body.AngularVelocity}, contacts={state.GetContactCount()}");
                var impulse = Vector2.Zero;
                for (var i = 0; i < state.GetContactCount(); i++)
                {
                    Check(state.GetContactCollider(i) == target && state.GetContactLocalShape(i) == 1 && state.GetContactColliderShape(i) == 0,
                        "Angular CCD retains exact scene/raw peer and nonzero logical shape indices");
                    impulse += state.GetContactImpulse(i);
                }
                Check((body.LinearVelocity * 2 - impulse).Length() < .02f && impulse.Length() > 1,
                    "The completed angular frame publishes the full momentum-changing contact impulse");
                if (movingPeer)
                {
                    Check((10 * (PhysicsServer.BodyGetLinearVelocity(target) - new Vector2(-10, 0)) + impulse).Length() < .02f,
                        "Coupled translating/rotating bodies publish equal opposite momentum changes");
                    Check(MathF.Abs(PhysicsServer.BodyGetAngularVelocity(target) + 10) > .1f &&
                        PhysicsServer.BodyGetContinuousCollisionDetectionMode(target) == CCDMode.Disabled,
                        "The rotating CCD-disabled counterpart receives an angular impulse while retaining its authored mode");
                }
                Collect();
                for (var i = 0; i < 128; i++) { Reset(); Step(); }
                var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) { Reset(); Step(); }
                Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all,
                    "Warmed reset/rotating-contact complete frames allocate zero owner/all-thread managed bytes");
                Check(state.GetContactCount() > 0 && body.AngularVelocity < .8f * angularSpeed, "Measured frames retain the real angular collision workload");
                Console.WriteLine($"Angular CastShape, movingPeer={movingPeer}: 128 warmup/128 measured full frames, omega={body.AngularVelocity:F6}, contacts={state.GetContactCount()}, 0/0 owner/all-thread managed B.");
                void Reset()
                {
                    body.Transform = Transform.Identity; body.LinearVelocity = Vector2.Zero; body.AngularVelocity = angularSpeed;
                    if (movingPeer)
                    {
                        PhysicsServer.BodySetTransform(target, targetPose); PhysicsServer.BodySetLinearVelocity(target, new(-10, 0));
                        PhysicsServer.BodySetAngularVelocity(target, -10);
                    }
                }
                void Step()
                {
                    var before = callbacks; var tick = PhysicsServer.SpaceGetTick(world.Space);
                    tree.PhysicsFrame(.02);
                    Check(callbacks == before + 1 && PhysicsServer.SpaceGetTick(world.Space) == tick + 1,
                        "Angular impact intervals retain one completed frame, callback and world tick");
                }
            }
            finally { PhysicsServer.FreeRID(target); }
        }
        static RigidBody MakeBody() => new()
        {
            GravityScale = 0,
            CanSleep = false,
            LinearDamp = 0,
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDamp = 0,
            AngularDampMode = RigidBody.DampMode.Replace
        };
    }
}
