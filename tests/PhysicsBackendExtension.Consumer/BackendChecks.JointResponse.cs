using Electron2D;

public static partial class BackendChecks
{
    public static void RunJointResponse(PhysicsServer.Backend? backend = null)
    {
        if (backend is null) PhysicsServerManager.RegisterServer("JointResponse", static () => new Solver());
        using var world = backend is { } selected ? new World(selected) : new World("JointResponse");
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
            "Joint response runs on the explicitly selected implementation without fallback");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 2 };
        var scene = new RigidBody { GravityScale = 0, CanSleep = false, CollisionLayer = 0, CollisionMask = 0 };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        var pin = new PinJoint(); var groove = new GrooveJoint(); root.AddChild(pin); root.AddChild(groove);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate();
        RID[] joints = [PhysicsServer.JointCreate(), PhysicsServer.JointCreate(), pin.GetRID(), groove.GetRID()];
        RID[] bodies = [scene.GetRID(), raw];
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetSpace(raw, world.Space);
            foreach (var b in bodies)
            {
                PhysicsServer.BodySetCollisionLayer(b, 0); PhysicsServer.BodySetCollisionMask(b, 0);
                PhysicsServer.BodySetMass(b, 2); PhysicsServer.BodySetInertia(b, 2); PhysicsServer.BodySetGravityScale(b, 0);
                PhysicsServer.BodySetCanSleep(b, false);
                PhysicsServer.BodySetLinearDampMode(b, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(b, 0);
                PhysicsServer.BodySetAngularDampMode(b, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(b, 0);
            }
            for (var owner = 0; owner < 2; owner++)
            {
                var id = joints[owner * 2]; Reset();
                PhysicsServer.JointMakePin(id, Vector2.Zero, bodies[0], raw);
                PhysicsServer.JointSetMaxForce(id, 6); PhysicsServer.JointSetMaxBias(id, 0);
                PhysicsServer.BodySetLinearVelocity(raw, new(6, 3));
                Step();
                var a = PhysicsServer.BodyGetLinearVelocity(bodies[0]); var b = PhysicsServer.BodyGetLinearVelocity(raw);
                Check(PhysicsServer.BodyGetDirectState(raw)!.TotalLinearDamp == 0 && PhysicsServer.BodyGetDirectState(raw)!.TotalAngularDamp == 0,
                    "Replace damping isolates joint response from default world drag");
                Check(a.Length() > .045f && a.Length() < .051f && a.X > 0 && a.Y > 0 && (a + b - new Vector2(6, 3)).Length() < .001f,
                    $"Pin anchor response uses one coupled linear force budget and preserves pair momentum: a={a}, b={b}");
                ResetMotion(); PhysicsServer.BodySetTransform(raw, new(0, new Vector2(2, 1))); Step();
                Check(PhysicsServer.BodyGetTransform(raw).Origin.DistanceTo(new(2, 1)) < .001f, "Zero correction-speed cap disables positional pin recovery");
                PhysicsServer.JointSetMaxForce(id, float.MaxValue); ResetMotion(); PhysicsServer.BodySetLinearVelocity(raw, new(6, 3)); Step();
                var hard = (PhysicsServer.BodyGetLinearVelocity(raw) - PhysicsServer.BodyGetLinearVelocity(bodies[0])).Length();
                PhysicsServer.PinJointSetSoftness(id, 10); ResetMotion(); PhysicsServer.BodySetLinearVelocity(raw, new(6, 3)); Step();
                var soft = (PhysicsServer.BodyGetLinearVelocity(raw) - PhysicsServer.BodyGetLinearVelocity(bodies[0])).Length();
                Check(hard < .001f && soft > 1 && soft < new Vector2(6, 3).Length(),
                    "Pin softness changes positional compliance rather than angular spring behavior");
                PhysicsServer.PinJointSetSoftness(id, 0); PhysicsServer.JointSetMaxBias(id, 5);
                ResetMotion(); PhysicsServer.BodySetTransform(raw, new(0, new Vector2(2, 1)));
                for (var i = 0; i < 16; i++) Step();
                Check(PhysicsServer.BodyGetTransform(raw).Origin.DistanceTo(PhysicsServer.BodyGetTransform(bodies[0]).Origin) < 2,
                    "Pin bias produces observable positional recovery on each solver architecture");
                PhysicsServer.JointSetBias(id, 0); PhysicsServer.JointSetMaxBias(id, 10000);
                PhysicsServer.SpaceSetConstraintDefaultBias(world.Space, .05f);
                ResetMotion(); PhysicsServer.BodySetTransform(raw, new(0, new Vector2(2, 1))); for (var i = 0; i < 4; i++) Step();
                var lowBias = PhysicsServer.BodyGetTransform(raw).Origin.DistanceTo(PhysicsServer.BodyGetTransform(bodies[0]).Origin);
                PhysicsServer.SpaceSetConstraintDefaultBias(world.Space, .4f);
                ResetMotion(); PhysicsServer.BodySetTransform(raw, new(0, new Vector2(2, 1))); for (var i = 0; i < 4; i++) Step();
                Check(PhysicsServer.BodyGetTransform(raw).Origin.DistanceTo(PhysicsServer.BodyGetTransform(bodies[0]).Origin) < lowBias - .05f,
                    "Zero joint bias inherits live world correction policy");
                PhysicsServer.SpaceSetConstraintDefaultBias(world.Space, .2f);

                Reset(); PhysicsServer.BodySetMode(bodies[0], PhysicsServer.BodyMode.Static);
                PhysicsServer.JointMakePin(id, new(0, 2), bodies[0], raw); PhysicsServer.JointSetMaxBias(id, 0);
                PhysicsServer.JointSetMaxForce(id, float.MaxValue); PhysicsServer.BodySetLinearVelocity(raw, new(6, 0)); Step();
                var lever = new Vector2(0, 2).Rotated(PhysicsServer.BodyGetTransform(raw).Rotation);
                var spin = PhysicsServer.BodyGetAngularVelocity(raw);
                var pointVelocity = PhysicsServer.BodyGetLinearVelocity(raw) + new Vector2(-lever.Y, lever.X) * spin;
                Check(spin > 1 && pointVelocity.Length() < .5f, "Off-center pin couples linear and angular motion at the actual anchor");

                Reset(); PhysicsServer.JointMakePin(id, Vector2.Zero, bodies[0]);
                PhysicsServer.JointSetMaxBias(id, 0); PhysicsServer.JointSetMaxForce(id, 6);
                PhysicsServer.BodySetLinearVelocity(bodies[0], new(4, 3)); Step();
                var groundChange = new Vector2(4, 3) - PhysicsServer.BodyGetLinearVelocity(bodies[0]);
                Check(groundChange.Length() > .045f && groundChange.Length() < .051f,
                    "Single-body pin executes against its fixed world anchor with the same force cap");

                Reset(); PhysicsServer.JointMakePin(id, Vector2.Zero, bodies[0], raw);
                PhysicsServer.PinJointSetMotorTargetVelocity(id, 3); PhysicsServer.PinJointSetMotorMaxTorque(id, .0006f);
                PhysicsServer.PinJointSetMotorEnabled(id, true); PhysicsServer.JointSetMaxForce(id, 6); Step();
                Check(MathF.Abs(PhysicsServer.BodyGetAngularVelocity(raw) - .05f) < .001f &&
                    MathF.Abs(PhysicsServer.BodyGetAngularVelocity(bodies[0]) + .05f) < .001f,
                    "Pin motor transfers opposite angular impulses under the additional torque cap");
                PhysicsServer.PinJointSetMotorMaxTorque(id, 10); PhysicsServer.JointSetMaxForce(id, .3f); ResetMotion(); Step();
                Check(MathF.Abs(PhysicsServer.BodyGetAngularVelocity(raw) - .0025f) < .0005f,
                    "General pin force cap also bounds the pure-angular channel");
                PhysicsServer.PinJointSetMotorEnabled(id, false); PhysicsServer.JointSetMaxForce(id, 10000);
                PhysicsServer.JointSetMaxBias(id, 2); PhysicsServer.PinJointSetAngularLimitLower(id, -.2f);
                PhysicsServer.PinJointSetAngularLimitUpper(id, .2f); PhysicsServer.PinJointSetAngularLimitEnabled(id, true);
                ResetMotion(); PhysicsServer.BodySetTransform(raw, new(.5f, Vector2.Zero));
                for (var i = 0; i < 12; i++) Step();
                var relative = PhysicsServer.BodyGetAngularVelocity(raw) - PhysicsServer.BodyGetAngularVelocity(bodies[0]);
                var angle = PhysicsServer.BodyGetTransform(raw).Rotation - PhysicsServer.BodyGetTransform(bodies[0]).Rotation;
                Check(angle < .4f && angle >= -.21f && MathF.Abs(relative) <= 2.01f, "Pin angular stops recover with their correction-speed cap");
                PhysicsServer.PinJointSetMotorEnabled(id, true);
                for (var i = 0; i < 60; i++)
                {
                    Step(); angle = PhysicsServer.BodyGetTransform(raw).Rotation - PhysicsServer.BodyGetTransform(bodies[0]).Rotation;
                    Check(angle >= -.26f && angle <= .26f, "Motor-driven pin respects stops within one 3 rad/s outer-tick travel allowance");
                }

                id = joints[owner * 2 + 1]; Reset(); PhysicsServer.BodySetMode(bodies[0], PhysicsServer.BodyMode.Static);
                PhysicsServer.BodySetTransform(raw, new(0, new Vector2(10, 0)));
                PhysicsServer.JointMakeGroove(id, Vector2.Zero, new(20, 0), new(10, 0), bodies[0], raw);
                PhysicsServer.BodySetLinearVelocity(raw, new(6, 3)); PhysicsServer.BodySetAngularVelocity(raw, 2); Step();
                b = PhysicsServer.BodyGetLinearVelocity(raw);
                Check(MathF.Abs(b.X - 6) < .01f && b.Y < .1f && MathF.Abs(PhysicsServer.BodyGetAngularVelocity(raw) - 2) < .001f,
                    $"Groove removes perpendicular drift while permitting axial motion and free rotation: v={b}, w={PhysicsServer.BodyGetAngularVelocity(raw)}");
                ResetMotion(); PhysicsServer.BodySetTransform(raw, new(0, new Vector2(23, 2)));
                PhysicsServer.BodySetLinearVelocity(raw, new(4, 3)); PhysicsServer.JointSetMaxForce(id, 6); PhysicsServer.JointSetMaxBias(id, 0); Step();
                b = PhysicsServer.BodyGetLinearVelocity(raw);
                var change = new Vector2(4, 3) - b;
                Check(change.X > 0 && change.Y > 0 && change.Length() > .045f && change.Length() < .051f,
                    "Groove endpoint and perpendicular rows share one vector force budget");
                foreach (var retreat in new[] { (At: 23f, Velocity: -4f), (At: -3f, Velocity: 4f) })
                {
                    ResetMotion(); PhysicsServer.BodySetTransform(raw, new(0, new Vector2(retreat.At, 0)));
                    PhysicsServer.BodySetLinearVelocity(raw, new(retreat.Velocity, 0)); Step();
                    Check(PhysicsServer.BodyGetLinearVelocity(raw).DistanceTo(new(retreat.Velocity, 0)) < .001f,
                        "A unilateral groove endpoint does not oppose retreat into its interval");
                }
                foreach (var end in new[] { new Vector2(-20, 0), Vector2.Zero })
                {
                    ResetMotion(); PhysicsServer.JointMakeGroove(id, Vector2.Zero, end, Vector2.Zero, bodies[0], raw);
                    PhysicsServer.JointSetMaxBias(id, 100); PhysicsServer.JointSetMaxForce(id, float.MaxValue);
                    PhysicsServer.BodySetTransform(raw, new(0, new Vector2(2, 1))); PhysicsServer.BodySetAngularVelocity(raw, 2);
                    for (var i = 0; i < 8; i++) Step();
                    Check(PhysicsServer.BodyGetTransform(raw).Origin.Length() < 2 && MathF.Abs(PhysicsServer.BodyGetAngularVelocity(raw) - 2) < .001f,
                        "Reversed and zero-length guides retain endpoint recovery and free rotation");
                }
            }
            foreach (var warm in joints)
            {
                Reset(); var isGroove = warm == joints[1] || warm == joints[3];
                if (isGroove) { PhysicsServer.BodySetMode(bodies[0], PhysicsServer.BodyMode.Static); PhysicsServer.JointMakeGroove(warm, Vector2.Zero, new(20, 0), Vector2.Zero, bodies[0], raw); }
                else PhysicsServer.JointMakePin(warm, Vector2.Zero, bodies[0], raw);
                PhysicsServer.JointSetMaxForce(warm, 6); PhysicsServer.JointSetMaxBias(warm, 5);
                for (var i = 0; i < 128; i++) Cycle(i);
                Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Cycle(i);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0, $"Joint response full-frame cycles allocated {own}/{all} B");
                using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
                Step(); var pose = PhysicsServer.BodyGetTransform(raw); var velocity = PhysicsServer.BodyGetLinearVelocity(raw);
                checkpoint.Restore(); Step();
                Check(PhysicsServer.BodyGetTransform(raw).Origin.DistanceTo(pose.Origin) < .001f &&
                    PhysicsServer.BodyGetLinearVelocity(raw).DistanceTo(velocity) < .001f, "Active pin/groove checkpoint restores the next interval's response");
                Console.WriteLine($"Joint response {backend?.ToString() ?? "registered"}, {PhysicsServer.JointGetType(warm)}: 128 edited full frames {own}/{all} B and private replay passed.");
                void Cycle(int i) { ResetMotion(); PhysicsServer.BodySetTransform(raw, new(0, isGroove ? new Vector2(23, 2) : new Vector2(2, 1))); PhysicsServer.JointSetBias(warm, (i & 1) == 0 ? .1f : .2f); Step(); }
            }
            Console.WriteLine($"Joint response on {backend?.ToString() ?? "registered"}: scene/server anchors, motor/stops, force/compliance/default bias, off-center/ground pins and finite free-rotation grooves passed.");
            void Reset()
            {
                foreach (var j in joints) PhysicsServer.JointClear(j);
                foreach (var b in bodies) PhysicsServer.BodySetMode(b, PhysicsServer.BodyMode.Rigid);
                ResetMotion();
            }
            void ResetMotion()
            {
                foreach (var b in bodies) { PhysicsServer.BodySetTransform(b, Transform.Identity); PhysicsServer.BodySetLinearVelocity(b, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(b, 0); }
            }
            void Step() => tree.PhysicsFrame(1d / 60);
        }
        finally { PhysicsServer.FreeRID(joints[0]); PhysicsServer.FreeRID(joints[1]); PhysicsServer.FreeRID(raw); }
    }
}
