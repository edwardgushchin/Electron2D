using Electron2D;

public static partial class BackendChecks
{
    public static void RunCurvedAnnulus(bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            PhysicsServerManager.RegisterServer("CurvedAnnulus", () => new Solver());
            Arc(inner: true, pinned: false); Arc(inner: false, pinned: false); Arc(inner: true, pinned: true);
            MovingRing();
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        Console.WriteLine($"Curved annulus, threaded={threaded}: inner/outer arcs, pin motor, moving counterpart, frame impulses and 0/0 warm frames passed.");

        void Arc(bool inner, bool pinned)
        {
            using var world = new World("CurvedAnnulus");
            PhysicsServer.SpaceSetContactDefaultBias(world.Space, 1); PhysicsServer.SpaceSetContactMaxAllowedPenetration(world.Space, 0);
            using var ring = new CustomShape(Annulus.Create(4, 5)); using var disk = new CircleShape { Radius = .25f };
            using var root = new SubViewport { World = world };
            var body = Driver(); body.Mass = 2; body.MaxContactsReported = 8;
            var arm = inner ? 1.9f : 2f; var origin = new Vector2(inner ? 1.9f : 7.1f, 0);
            body.AddChild(new CollisionShape { Name = "Disabled", Shape = disk, Disabled = true });
            body.AddChild(new CollisionShape { Name = "Right", Shape = disk, Position = new(arm, 0) });
            body.AddChild(new CollisionShape { Name = "Left", Shape = disk, Position = new(-arm, 0) }); root.AddChild(body);
            using var tree = new SceneTree(root);
            var obstacle = PhysicsServer.BodyCreate(); var joint = default(RID);
            try
            {
                PhysicsServer.BodySetMode(obstacle, PhysicsServer.BodyMode.Static);
                PhysicsServer.BodyAddShape(obstacle, disk.GetRID(), disabled: true); PhysicsServer.BodyAddShape(obstacle, ring.GetRID());
                PhysicsServer.BodySetSpace(obstacle, world.Space);
                PhysicsServer.BodySetFriction(obstacle, 0); PhysicsServer.BodySetFriction(body.GetRID(), 0);
                var callbacks = 0; PhysicsServer.BodySetStateSyncCallback(body.GetRID(), _ => callbacks++);
                using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
                if (!pinned)
                {
                    body.ContinuousCD = CCDMode.Disabled; Reset(); Step();
                    Check(state.GetContactCount() == 0 && MathF.Abs(body.AngularVelocity - 20) < .001f,
                        "The discrete clear-endpoint control passes through the intermediate annulus arc");
                }
                body.ContinuousCD = CCDMode.CastShape; Reset();
                if (pinned)
                {
                    joint = PhysicsServer.JointCreate(); PhysicsServer.JointMakePin(joint, origin, body.GetRID());
                    PhysicsServer.PinJointSetMotorTargetVelocity(joint, -20); PhysicsServer.PinJointSetMotorMaxTorque(joint, .5f);
                    PhysicsServer.PinJointSetMotorEnabled(joint, true);
                }
                using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
                Step(); Verify();
                var expected = body.Transform; var velocity = body.LinearVelocity; var angular = body.AngularVelocity; var frameImpulse = Impulse();
                var delivered = callbacks; checkpoint.Restore();
                Check(callbacks == delivered && state.GetContactCount() == 0, "Custom private restore retires the discarded contact future without a duplicate game callback");
                Step(); Verify();
                Check(body.Position.DistanceTo(expected.Origin) < .002f && MathF.Abs(body.Rotation - expected.Rotation) < .002f &&
                    body.LinearVelocity.DistanceTo(velocity) < .002f && MathF.Abs(body.AngularVelocity - angular) < .002f && Impulse().DistanceTo(frameImpulse) < .02f,
                    "Restored custom geometry, contact and motor state replay the same physical interval within numerical tolerances");
                Collect();
                for (var i = 0; i < 128; i++) { Reset(); Step(); }
                var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) { Reset(); Step(); }
                Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all,
                    "Warmed curved custom contacts and pin-motor full frames allocate zero owner/all-thread managed bytes");
                Verify();
                void Reset() { body.Transform = new(-.4f, origin); body.LinearVelocity = Vector2.Zero; body.AngularVelocity = pinned ? 0 : 20; }
                void Step()
                {
                    var before = callbacks; var tick = PhysicsServer.SpaceGetTick(world.Space); tree.PhysicsFrame(.04);
                    Check(callbacks == before + 1 && PhysicsServer.SpaceGetTick(world.Space) == tick + 1,
                        "Curved intervals publish one callback and completed outer tick");
                }
                Vector2 Impulse()
                {
                    var value = Vector2.Zero;
                    for (var i = 0; i < state.GetContactCount(); i++) value += state.GetContactImpulse(i);
                    return value;
                }
                void Verify()
                {
                    Check(state.GetContactCount() > 0, $"Curved annulus must hit its intermediate {(inner ? "inner" : "outer")} boundary, pinned={pinned}");
                    var impulse = Vector2.Zero;
                    for (var i = 0; i < state.GetContactCount(); i++)
                    {
                        Check(state.GetContactCollider(i) == obstacle && state.GetContactColliderShape(i) == 1 &&
                            state.GetContactLocalShape(i) == (inner ? 1 : 2), "Custom arc contacts preserve both disabled index gaps");
                        var point = state.GetContactLocalPosition(i); var value = state.GetContactImpulse(i);
                        Check(MathF.Abs(point.Length() - (inner ? 4 : 5)) < .005f && value.Dot(point) * (inner ? -1 : 1) > 0,
                            $"Inner/outer contact points and impulse directions follow the actual custom boundary: inner={inner}, pinned={pinned}, point={point}, impulse={value}");
                        impulse += value;
                    }
                    Check(impulse.Length() > .01f, "A curved custom boundary produces a physical impulse");
                    var solvedCenter = body.GlobalTransform * new Vector2(inner ? arm : -arm, 0);
                    Check(inner ? solvedCenter.Length() <= 3.755f : solvedCenter.Length() >= 5.245f,
                        "Completed circular geometry stays on the allowed side of the custom boundary");
                    if (!pinned) Check((2 * body.LinearVelocity - impulse).Length() < .02f, "The custom frame impulse equals the full body momentum change");
                    else
                    {
                        var position = body.GlobalTransform * new Vector2(arm, 0);
                        Check(body.Position.DistanceTo(origin) < .1f && body.Rotation > -.4f && position.Length() <= 3.755f,
                            $"A motor starting from rest hits the hole while retaining its pin anchor: {body.Position}/{body.Rotation}");
                        Check(PhysicsServer.PinJointGetMotorMaxTorque(joint) == .5f, "CCD intervals preserve the authored motor torque cap");
                    }
                }
            }
            finally { if (joint.IsValid()) PhysicsServer.FreeRID(joint); PhysicsServer.FreeRID(obstacle); }
        }

        void MovingRing()
        {
            using var world = new World("CurvedAnnulus");
            PhysicsServer.SpaceSetContactDefaultBias(world.Space, 1); PhysicsServer.SpaceSetContactMaxAllowedPenetration(world.Space, 0);
            using var ring = new CustomShape(Annulus.Create(4, 5)); using var disk = new CircleShape { Radius = .25f };
            using var root = new SubViewport { World = world };
            var body = Driver(); body.Mass = 2; body.ContinuousCD = CCDMode.CastShape; body.MaxContactsReported = 8;
            body.AddChild(new CollisionShape { Shape = disk }); root.AddChild(body);
            using var tree = new SceneTree(root); var obstacle = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.BodyAddShape(obstacle, ring.GetRID(), new(0, new Vector2(2, 0)));
                PhysicsServer.BodySetMass(obstacle, 1000); PhysicsServer.BodySetInertia(obstacle, 1000);
                PhysicsServer.BodySetCenterOfMass(obstacle, Vector2.Zero); PhysicsServer.BodySetGravityScale(obstacle, 0);
                PhysicsServer.BodySetCanSleep(obstacle, false);
                PhysicsServer.BodySetLinearDampMode(obstacle, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(obstacle, 0);
                PhysicsServer.BodySetAngularDampMode(obstacle, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(obstacle, 0);
                PhysicsServer.BodySetSpace(obstacle, world.Space);
                PhysicsServer.BodySetFriction(obstacle, 0); PhysicsServer.BodySetFriction(body.GetRID(), 0);
                using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
                var callbacks = 0; PhysicsServer.BodySetStateSyncCallback(body.GetRID(), _ => callbacks++);
                Reset(); using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
                Step(); Verify();
                var expected = body.Transform; var velocity = body.LinearVelocity; var targetPose = PhysicsServer.BodyGetTransform(obstacle);
                var targetVelocity = PhysicsServer.BodyGetLinearVelocity(obstacle); var delivered = callbacks;
                checkpoint.Restore(); Check(callbacks == delivered && state.GetContactCount() == 0, "Moving-ring private restore discards contacts without duplicated callbacks");
                Step(); Verify();
                Check(body.Position.DistanceTo(expected.Origin) < .002f && body.LinearVelocity.DistanceTo(velocity) < .002f &&
                    PhysicsServer.BodyGetTransform(obstacle).Origin.DistanceTo(targetPose.Origin) < .002f &&
                    PhysicsServer.BodyGetLinearVelocity(obstacle).DistanceTo(targetVelocity) < .002f,
                    "Both moving custom participants replay their restored motion");
                Collect();
                for (var i = 0; i < 128; i++) { Reset(); Step(); }
                var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) { Reset(); Step(); }
                Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all,
                    "Warmed rotating custom counterpart full frames allocate zero owner/all-thread managed bytes");
                Verify();
                void Reset()
                {
                    body.Transform = new(0, new Vector2(-3.8f, 0)); body.LinearVelocity = Vector2.Zero; body.AngularVelocity = 0;
                    PhysicsServer.BodySetTransform(obstacle, new(-.4f, new Vector2(-2, 0)));
                    PhysicsServer.BodySetLinearVelocity(obstacle, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(obstacle, 20);
                }
                void Step()
                {
                    var before = callbacks; var tick = PhysicsServer.SpaceGetTick(world.Space); tree.PhysicsFrame(.04);
                    Check(callbacks == before + 1 && PhysicsServer.SpaceGetTick(world.Space) == tick + 1,
                        "Moving custom contact publishes one callback and completed outer tick");
                }
                void Verify()
                {
                    Check(state.GetContactCount() > 0, "A stationary protected circle detects the rotating counterpart's intermediate inner arc");
                    var impulse = Vector2.Zero;
                    for (var i = 0; i < state.GetContactCount(); i++)
                    {
                        Check(state.GetContactCollider(i) == obstacle && state.GetContactLocalShape(i) == 0 && state.GetContactColliderShape(i) == 0,
                            "Moving custom contact retains scene/raw identities");
                        impulse += state.GetContactImpulse(i);
                    }
                    Check(impulse.Length() > .01f && (2 * body.LinearVelocity - impulse).Length() < .02f &&
                        (1000 * PhysicsServer.BodyGetLinearVelocity(obstacle) + impulse).Length() < .02f,
                        "Both custom-body frame momentum changes match opposite full contact impulses");
                    Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(obstacle) == CCDMode.Disabled,
                        "Counterpart rotation does not change its configured discrete policy");
                }
            }
            finally { PhysicsServer.FreeRID(obstacle); }
        }

        static RigidBody Driver() => new()
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
