using Electron2D;

public static partial class BackendChecks
{
    public static void RunAngularGeometry(PhysicsServer.Backend backend, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            for (var kind = 0; kind < 4; kind++)
            {
                foreach (var mode in new[] { CCDMode.Disabled, CCDMode.CastRay, CCDMode.CastShape }) Verify(kind, mode, false);
                Verify(kind, CCDMode.CastShape, true);
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        Console.WriteLine($"Angular geometry {backend}, threaded={threaded}: rectangle/capsule/polygon pieces/compound slots, all CCD modes and coupled impulses passed.");

        void Verify(int kind, CCDMode mode, bool movingPeer)
        {
            using var world = new World(backend);
            Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Angular geometry uses the requested implementation.");
            using var shape = CreateShape(kind); using var disk = new CircleShape { Radius = .25f };
            using var root = new SubViewport { World = world };
            var body = new RigidBody
            {
                GravityScale = 0,
                CanSleep = false,
                Mass = 2,
                Inertia = 20000,
                CenterOfMassMode = RigidCenterOfMassMode.Custom,
                CenterOfMass = Vector2.Zero,
                ContinuousCD = mode,
                MaxContactsReported = 8,
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            body.AddChild(new CollisionShape { Name = "Disabled", Shape = disk, Disabled = true });
            if (kind == 3)
            {
                body.AddChild(new CollisionShape { Name = "Left", Shape = shape, Position = new(-80, 0) });
                body.AddChild(new CollisionShape { Name = "Right", Shape = shape, Position = new(80, 0) });
            }
            else body.AddChild(new CollisionShape { Name = "Arm", Shape = shape, Rotation = kind == 1 ? MathF.PI / 2 : 0 });
            root.AddChild(body); using var tree = new SceneTree(root);
            var target = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.BodySetMode(target, movingPeer ? PhysicsServer.BodyMode.Rigid : PhysicsServer.BodyMode.Static);
                PhysicsServer.BodyAddShape(target, disk.GetRID(), disabled: true);
                PhysicsServer.BodyAddShape(target, disk.GetRID(), new(0, movingPeer ? new Vector2(-5, 0) : Vector2.Zero));
                var point = new Vector2(160, 0).Rotated(.225f);
                var targetPose = new Transform(0, point + (movingPeer ? new Vector2(5, 0) : Vector2.Zero));
                PhysicsServer.BodySetTransform(target, targetPose);
                if (movingPeer)
                {
                    PhysicsServer.BodySetMass(target, 10); PhysicsServer.BodySetInertia(target, 100);
                    PhysicsServer.BodySetCenterOfMass(target, Vector2.Zero);
                    PhysicsServer.BodySetGravityScale(target, 0); PhysicsServer.BodySetCanSleep(target, false);
                    PhysicsServer.BodySetLinearDampMode(target, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(target, 0);
                    PhysicsServer.BodySetAngularDampMode(target, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(target, 0);
                }
                PhysicsServer.BodySetSpace(target, world.Space);
                PhysicsServer.BodySetFriction(target, 0); PhysicsServer.BodySetFriction(body.GetRID(), 0);
                using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
                var callbacks = 0; PhysicsServer.BodySetStateSyncCallback(body.GetRID(), _ => callbacks++);
                Frame();
                Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(body.GetRID()) == mode,
                    "Completed motion retains the requested per-body CCD mode.");
                if (mode == CCDMode.Disabled)
                {
                    Check(state.GetContactCount() == 0 && MathF.Abs(body.AngularVelocity - 30) < .01f,
                        $"Discrete geometry control misses the intermediate arc: kind={kind}, omega={body.AngularVelocity}, contacts={state.GetContactCount()}.");
                    return;
                }
                if (mode == CCDMode.CastRay)
                {
                    Check(body.Position.IsFinite() && float.IsFinite(body.AngularVelocity), "Approximate rotational ray control remains finite.");
                    return;
                }
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
                for (var i = 0; i < 128; i++) Frame();
                var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame();
                var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
                Check(ownerBytes == 0 && allBytes == 0, "Prepared active angular geometry full frames allocate zero managed bytes.");
                Console.WriteLine($"Angular geometry kind={kind}, movingPeer={movingPeer}: omega={body.AngularVelocity:F6}, contacts={state.GetContactCount()}, 128 warm/128 measured frames, {ownerBytes}/{allBytes} B.");

                void Frame()
                {
                    body.Transform = Transform.Identity; body.LinearVelocity = Vector2.Zero; body.AngularVelocity = 30;
                    PhysicsServer.BodySetTransform(target, targetPose);
                    if (movingPeer) { PhysicsServer.BodySetLinearVelocity(target, new(-10, 0)); PhysicsServer.BodySetAngularVelocity(target, -10); }
                    var tick = PhysicsServer.SpaceGetTick(world.Space); var before = callbacks;
                    tree.PhysicsFrame(.02);
                    Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1 && callbacks == before + 1, "One completed angular geometry frame publishes one tick and one state callback.");
                    if (mode == CCDMode.CastShape) VerifyImpact();
                }
                void VerifyImpact()
                {
                    if (state.GetContactCount() == 0 || body.AngularVelocity is <= -1 or >= 24)
                        throw new InvalidOperationException($"Full-shape CCD resolves intermediate geometry: kind={kind}, movingPeer={movingPeer}, omega={body.AngularVelocity}, contacts={state.GetContactCount()}.");
                    var impulse = Vector2.Zero;
                    for (var i = 0; i < state.GetContactCount(); i++)
                    {
                        Check(state.GetContactCollider(i) == target && state.GetContactLocalShape(i) == (kind == 3 ? 2 : 1) && state.GetContactColliderShape(i) == 1,
                            "Angular geometry contacts preserve scene/raw RID and disabled-first logical indices across native pieces.");
                        impulse += state.GetContactImpulse(i);
                    }
                    Check(impulse.Length() > 1 && (2 * body.LinearVelocity - impulse).Length() < .02f,
                        "Full frame impulse matches the scene body's momentum change.");
                    if (movingPeer)
                    {
                        Check((10 * (PhysicsServer.BodyGetLinearVelocity(target) - new Vector2(-10, 0)) + impulse).Length() < .02f,
                            "Coupled angular geometry publishes equal opposite linear momentum changes.");
                        Check(MathF.Abs(PhysicsServer.BodyGetAngularVelocity(target) + 10) > .1f,
                            "The moving offset collider receives an angular impulse.");
                    }
                }
            }
            finally { PhysicsServer.FreeRID(target); }
        }
        static Shape CreateShape(int kind)
        {
            if (kind == 0) return new RectangleShape { Size = new(320, .5f) };
            if (kind == 1) return new CapsuleShape { Radius = .25f, Height = 320 };
            if (kind == 3) return new RectangleShape { Size = new(160, .5f) };
            var points = new Vector2[12];
            for (var i = 0; i < points.Length; i++) { var angle = i * MathF.Tau / points.Length; points[i] = new(160 * MathF.Cos(angle), 4 * MathF.Sin(angle)); }
            return new ConvexPolygonShape { Points = points };
        }
    }
}
