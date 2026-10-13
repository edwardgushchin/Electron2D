using Electron2D;

public static partial class BackendChecks
{
    public static void RunSpringAnchors(bool threaded = false, PhysicsServer.Backend? backend = null)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            if (backend is null) PhysicsServerManager.RegisterServer("SpringAnchors", static () => new Solver());
            for (var registered = 0; registered < 2; registered++)
            {
                using var world = registered == 0 ? new World(PhysicsServer.Backend.CPU) :
                    backend is { } selected ? new World(selected) : new World("SpringAnchors");
                Check(world.PhysicsBackend == (registered == 0 ? PhysicsServer.Backend.CPU : backend ?? PhysicsServer.Backend.Extension) &&
                    world.PhysicsFallbackReason is null, "Spring anchors use the selected owner without startup fallback");
                using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 2 };
                var anchor = new StaticBody { Position = new(-10, 0), CollisionLayer = 0, CollisionMask = 0 };
                var body = new RigidBody
                {
                    Mass = 2,
                    Inertia = 2,
                    GravityScale = 0,
                    CanSleep = false,
                    CollisionLayer = 0,
                    CollisionMask = 0,
                    LinearDampMode = RigidBody.DampMode.Replace,
                    AngularDampMode = RigidBody.DampMode.Replace,
                    LinearDamp = 0,
                    AngularDamp = 0
                };
                anchor.AddChild(new CollisionShape { Shape = circle }); body.AddChild(new CollisionShape { Shape = circle });
                root.AddChild(anchor); root.AddChild(body); var scene = new DampedSpringJoint(); root.AddChild(scene);
                using var tree = new SceneTree(root); var raw = PhysicsServer.JointCreate();
                try
                {
                    foreach (var joint in new[] { raw, scene.GetRID() })
                        for (var reverse = 0; reverse < 2; reverse++)
                        {
                            Reset();
                            if (reverse == 0) PhysicsServer.JointMakeDampedSpring(joint, new(-10, 2), new(0, 2), anchor.GetRID(), body.GetRID());
                            else PhysicsServer.JointMakeDampedSpring(joint, new(0, 2), new(-10, 2), body.GetRID(), anchor.GetRID());
                            PhysicsServer.DampedSpringJointSetRestLength(joint, 5); PhysicsServer.DampedSpringJointSetStiffness(joint, 2);
                            PhysicsServer.DampedSpringJointSetDamping(joint, 0); PhysicsServer.JointSetMaxForce(joint, float.MaxValue);
                            tree.PhysicsFrame(.001);
                            Check(MathF.Abs(body.LinearVelocity.X + .005f) < .00005f && MathF.Abs(body.AngularVelocity - .01f) < .0001f,
                                "An off-center spring applies its real anchor impulse and angular moment in scene units");
                            Reset(); PhysicsServer.JointSetMaxForce(joint, 1); tree.PhysicsFrame(.001);
                            Check(MathF.Abs(body.LinearVelocity.X + .0005f) < .00001f && MathF.Abs(body.AngularVelocity - .001f) < .00002f,
                                "The shared force cap bounds the same linear impulse and its off-center moment");
                            Reset(); PhysicsServer.JointSetMaxForce(joint, float.MaxValue);
                            PhysicsServer.DampedSpringJointSetStiffness(joint, 0); PhysicsServer.DampedSpringJointSetDamping(joint, 4);
                            body.AngularVelocity = 3; tree.PhysicsFrame(.001);
                            Check(body.LinearVelocity.X > .011f && body.LinearVelocity.X < .013f && body.AngularVelocity > 2.974f && body.AngularVelocity < 2.978f,
                                "Axial damping observes anchor point velocity and effective linear/angular mass");
                            Check(body.LinearVelocity.LengthSquared() + body.AngularVelocity * body.AngularVelocity < 9,
                                "Pure spring damping reduces complete linear plus rotational kinetic energy");
                            Reset(); PhysicsServer.DampedSpringJointSetDamping(joint, 10000); body.AngularVelocity = 3; tree.PhysicsFrame(.001);
                            Check(MathF.Abs(body.LinearVelocity.X - 1.2f) < .002f && MathF.Abs(body.AngularVelocity - .6f) < .002f &&
                                body.LinearVelocity.LengthSquared() + body.AngularVelocity * body.AngularVelocity < 2,
                                "Strong damping removes axial point motion without reversal or kinetic energy growth");
                            PhysicsServer.JointClear(joint);
                        }
                }
                finally { PhysicsServer.FreeRID(raw); }

                void Reset()
                { body.Position = Vector2.Zero; body.Rotation = 0; body.LinearVelocity = Vector2.Zero; body.AngularVelocity = 0; }
            }
            Console.WriteLine($"Spring anchors CPU/{backend?.ToString() ?? "registered"}, threaded={threaded}: scene/raw and both endpoint orders retain torque, capped impulse and stable rotational damping.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
