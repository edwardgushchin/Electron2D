using Electron2D;

public static partial class BackendChecks
{
    public static void RunMotionSelection(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread); var owner = Thread.CurrentThread; var solved = 0;
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("MotionSelection", () => new Solver
        { StepHook = () => { Check((Thread.CurrentThread != owner) == threaded, "Motion selection retains the numerical lane"); solved++; } });
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("MotionSelection");
            Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null, "Motion selection keeps its requested implementation");
            using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
            using var disk = new CircleShape { Radius = 1 }; using var line = new ConcavePolygonShape { Segments = [new(-1, 0), new(1, 0)] };
            using var floor = new RectangleShape { Size = new(8, .2f) }; using var sink = new CircleShape { Radius = 4 }; using var gap = new CircleShape { Radius = .01f };
            var raw = new RID[2]; var scene = new StaticBody[2]; var slots = new CollisionShape[2, 2]; var peers = new RID[2];
            using var sinkBody = new StaticBody { CollisionLayer = 2, CollisionMask = 0 }; sinkBody.AddChild(new CollisionShape { Shape = sink }); root.AddChild(sinkBody);
            using var peer = new RigidBody { CollisionLayer = 0, CollisionMask = 0, GravityScale = 0, CanSleep = false };
            peer.AddChild(new CollisionShape { Shape = gap, Disabled = true }); peer.AddChild(new CollisionShape { Shape = floor }); root.AddChild(peer); peers[1] = peer.GetRID();
            using var parameters = new PhysicsTestMotionParameters(); using var result = new PhysicsTestMotionResult(); using var sceneResult = new KinematicCollision();
            try
            {
                peers[0] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(peers[0], PhysicsServer.BodyMode.Static);
                PhysicsServer.BodyAddShape(peers[0], gap.GetRID(), disabled: true); PhysicsServer.BodyAddShape(peers[0], floor.GetRID());
                PhysicsServer.BodySetCollisionMask(peers[0], 0); PhysicsServer.BodySetSpace(peers[0], world.Space);
                for (var order = 0; order < 2; order++)
                {
                    raw[order] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(raw[order], PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionLayer(raw[order], 0); PhysicsServer.BodySetCollisionMask(raw[order], 2);
                    PhysicsServer.BodyAddShape(raw[order], gap.GetRID(), disabled: true);
                    scene[order] = new StaticBody { Position = new(1000 + order * 50, 1000), CollisionLayer = 0, CollisionMask = 2 };
                    scene[order].AddChild(new CollisionShape { Shape = gap, Disabled = true });
                    for (var slot = 0; slot < 2; slot++)
                    {
                        var ordinary = slot == order; Shape shape = ordinary ? disk : line; var pose = new Transform(0, new Vector2(ordinary ? 5 : -5, 0));
                        PhysicsServer.BodyAddShape(raw[order], shape.GetRID(), pose); slots[order, slot] = new CollisionShape { Shape = shape, Transform = pose }; scene[order].AddChild(slots[order, slot]);
                    }
                    root.AddChild(scene[order]); PhysicsServer.BodySetTransform(raw[order], scene[order].Transform); PhysicsServer.BodySetSpace(raw[order], world.Space);
                }
                for (var i = 0; i < 128; i++) Frame(i); Collect();
                var beforeSteps = solved; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame(i);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(backend is not null || solved - beforeSteps == 128, "Motion selection executes complete numerical frames");
                Check(own == 0 && all == 0, $"Motion selection allocated {own}/{all} B");
                Console.WriteLine($"Motion selection {world.PhysicsBackendName}/{threaded}: two source orders, final-source isolation, scene/raw peers, four modes, rotated custom center; 128 frames {own}/{all} B.");
                void Frame(int configuration)
                {
                    var role = configuration % 2; var mode = (PhysicsServer.BodyMode)(configuration / 2 % 4);
                    var outer = new Transform(configuration / 8 % 2 == 0 ? 0 : .37f, new Vector2(70, -30)); var margin = configuration / 16 % 2 == 0 ? .02f : .5f;
                    var target = peers[role]; var targetPose = outer * new Transform(0, new(5, 11.1f)); var center = new Vector2(2, -3);
                    for (var i = 0; i < 2; i++) PhysicsServer.BodySetCollisionLayer(peers[i], i == role ? 2u : 0u);
                    PhysicsServer.BodySetMode(target, mode); PhysicsServer.BodySetTransform(target, targetPose); PhysicsServer.BodySetCenterOfMass(target, center);
                    var linear = outer.BasisXform(new(3, -2)); var angular = mode == PhysicsServer.BodyMode.RigidLinear ? 0 : 2.5f;
                    PhysicsServer.BodySetLinearVelocity(target, linear); PhysicsServer.BodySetAngularVelocity(target, angular);
                    sinkBody.Transform = outer * new Transform(0, new(-5, 10));
                    for (var phase = 0; phase < 2; phase++)
                    {
                        parameters.From = outer * new Transform(0, phase == 0 ? Vector2.Zero : new(0, 10));
                        parameters.Motion = phase == 0 ? outer.BasisXform(new(0, 40)) : Vector2.Zero;
                        parameters.Margin = margin; parameters.RecoveryAsCollision = phase != 0;
                        for (var order = 0; order < 2; order++) for (var route = 0; route < 2; route++)
                            {
                                var hit = route == 0 ? PhysicsServer.BodyTestMotion(raw[order], parameters, result) : scene[order].TestMove(parameters.From, parameters.Motion, sceneResult, margin, phase != 0);
                                var local = phase == 0 ? order + 1 : 2 - order; var collider = route == 0 ? result.GetColliderRID() : sceneResult.GetColliderRID();
                                var actualSlot = route == 0 ? result.GetCollisionLocalShape() : ReferenceEquals(sceneResult.GetLocalShape(), slots[order, local - 1]) ? local : -1;
                                if (!hit || actualSlot != local || collider != (phase == 0 ? target : sinkBody.GetRID()))
                                    throw new InvalidOperationException($"Motion selection source phase={phase}, order={order}, route={route}, role={role}, mode={mode}: hit={hit}, slot={actualSlot}, collider={collider}.");
                                Check(route == 0 || ReferenceEquals(sceneResult.GetLocalShape(), slots[order, local - 1]), "Scene motion retains the selected physical source owner");
                                if (phase != 0) continue;
                                var point = route == 0 ? result.GetCollisionPoint() : sceneResult.GetPosition(); var velocity = route == 0 ? result.GetColliderVelocity() : sceneResult.GetColliderVelocity();
                                var arm = point - targetPose * center; var expectedVelocity = linear + new Vector2(-arm.Y, arm.X) * angular;
                                var normal = route == 0 ? result.GetCollisionNormal() : sceneResult.GetNormal(); var travel = route == 0 ? result.GetTravel() : sceneResult.GetTravel();
                                Check(velocity.DistanceTo(expectedVelocity) < .003f && normal.Dot(-outer.Y) > .999f, "Selected motion contact uses the peer's rotated center and current point velocity");
                                Check((outer.AffineInverse() * point).Y is > 10.997f and < 11.003f && travel.DistanceTo(outer.BasisXform(new(0, 10))) < .24f,
                                    "Final contact stays on the first ordinary source rather than the deeper ignored concave source");
                            }
                    }
                    tree.PhysicsFrame(.01);
                }
            }
            finally { foreach (var body in raw) if (body.IsValid()) PhysicsServer.FreeRID(body); if (peers[0].IsValid()) PhysicsServer.FreeRID(peers[0]); }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
