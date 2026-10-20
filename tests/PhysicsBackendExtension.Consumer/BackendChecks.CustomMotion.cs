using Electron2D;

public static partial class BackendChecks
{
    public static void RunCustomMotion(bool threaded = false, bool hollowPeer = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread); var owner = Thread.CurrentThread; var solved = 0;
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        PhysicsServerManager.RegisterServer("CustomMotion", () => new Solver
        { StepHook = () => { Check((Thread.CurrentThread != owner) == threaded, "Custom motion retains its numerical lane"); solved++; } });
        try
        {
            using var world = new World("CustomMotion"); using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
            using var ring = new CustomShape(Annulus.Create(5, 10)); using Shape disk = hollowPeer ? new CustomShape(Annulus.Create(.25f, 1)) : new CircleShape { Radius = 1 }; using var gap = new CircleShape { Radius = .01f };
            Shape[] shapes = [ring, disk]; var targets = new RID[2]; var sources = new RID[2]; var scene = new StaticBody[2]; var primary = new CollisionShape[2];
            using var ticker = new RigidBody { Position = new(100, -300), CollisionLayer = 0, CollisionMask = 0, GravityScale = 0, CanSleep = false, LinearVelocity = Vector2.Right, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
            ticker.AddChild(new CollisionShape { Shape = disk }); root.AddChild(ticker);
            using var query = new PhysicsShapeQueryParameters { Margin = 0 }; var hits = new PhysicsShapeResult[4]; var points = new Vector2[8];
            using var motion = new PhysicsTestMotionParameters { Margin = .05f }; using var result = new PhysicsTestMotionResult(); using var sceneResult = new KinematicCollision();
            try
            {
                for (var kind = 0; kind < 2; kind++)
                {
                    targets[kind] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(targets[kind], PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionLayer(targets[kind], 0); PhysicsServer.BodySetCollisionMask(targets[kind], 0);
                    PhysicsServer.BodyAddShape(targets[kind], gap.GetRID(), disabled: true); PhysicsServer.BodyAddShape(targets[kind], shapes[1 - kind].GetRID());
                    PhysicsServer.BodySetTransform(targets[kind], new(0, new Vector2(kind == 0 ? .5f : 0, 0))); PhysicsServer.BodySetSpace(targets[kind], world.Space);
                    sources[kind] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(sources[kind], PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionLayer(sources[kind], 0);
                    PhysicsServer.BodyAddShape(sources[kind], gap.GetRID(), disabled: true); PhysicsServer.BodyAddShape(sources[kind], shapes[kind].GetRID());
                    scene[kind] = new StaticBody { Position = new(1000 + kind * 50, 1000), CollisionLayer = 0 };
                    scene[kind].AddChild(new CollisionShape { Shape = gap, Disabled = true }); primary[kind] = new CollisionShape { Shape = shapes[kind] }; scene[kind].AddChild(primary[kind]); root.AddChild(scene[kind]);
                    PhysicsServer.BodySetTransform(sources[kind], scene[kind].Transform); PhysicsServer.BodySetSpace(sources[kind], world.Space);
                }
                for (var i = 0; i < 128; i++) Frame(i); Collect();
                var beforeSteps = solved; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame(i);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(solved - beforeSteps == 128 && own == 0 && all == 0, $"Custom motion steps={solved - beforeSteps}, allocated {own}/{all} B");
                Console.WriteLine($"Custom motion hollowPeer={hollowPeer}/{threaded}: both hollow-source orders, empty hole, inner/outer entry, initial-overlap recovery/flag control and surface contacts; 128 frames / 971 calls {own}/{all} B.");
                void Frame(int configuration)
                {
                    var phase = configuration % 5; var kind = configuration / 5 % 2; var sign = configuration / 10 % 2 == 0 ? 1 : -1;
                    var target = targets[kind]; var targetX = kind == 0 ? .5f : 0; var fromX = kind == 0 ? 0 : .5f;
                    if (phase == 3) fromX = targetX + sign * 15; if (phase == 4) fromX = targetX + sign * 4.5f;
                    var dx = phase is 0 or 4 ? 0 : phase == 1 ? sign : phase == 2 ? sign * 10 : -sign * 10;
                    var expectedHit = phase >= 2; var expectedFraction = phase == 2 ? (4 - sign * (fromX - targetX)) / 10 : phase == 3 ? .4f : 1;
                    for (var i = 0; i < 2; i++) PhysicsServer.BodySetCollisionLayer(targets[i], kind == i ? 1u : 0u);
                    query.Shape = shapes[kind]; query.Transform = new(0, new Vector2(fromX, 0)); query.Motion = new(dx, 0);
                    var cast = world.DirectSpaceState.CastMotion(query);
                    if (phase is 0 or 1 or 4 ? cast != (1f, 1f) : MathF.Abs(cast.SafeFraction - expectedFraction) > .000002f || MathF.Abs(cast.UnsafeFraction - expectedFraction) > .000002f)
                        throw new InvalidOperationException($"Custom motion cast kind={kind}, phase={phase}, sign={sign}: {cast}, expected={expectedFraction}.");
                    var count = world.DirectSpaceState.IntersectShape(query, hits); var contacts = world.DirectSpaceState.CollideShape(query, points); var rest = world.DirectSpaceState.GetRestInfo(query);
                    Check(count == (expectedHit ? 1 : 0) && contacts == count && rest.HasValue == expectedHit, "Hollow-source shape operations retain actual overlap truth");
                    if (expectedHit)
                    {
                        var info = rest ?? throw new InvalidOperationException("A hollow-source hit needs rest information.");
                        Check(hits[0].ColliderRID == target && hits[0].ShapeIndex == 1 && info.ColliderRID == target && info.ShapeIndex == 1, "Hollow-source queries preserve target identity and disabled slot gaps");
                        var sourceCenter = new Vector2(fromX + (phase == 4 ? 0 : dx * expectedFraction), 0); var targetCenter = new Vector2(targetX, 0);
                        var sourceRadius = kind == 0 ? phase == 3 ? 10 : 5 : 1; var targetRadius = kind == 0 ? 1 : phase == 3 ? 10 : 5;
                        Check(MathF.Abs(points[0].DistanceTo(sourceCenter) - sourceRadius) < .0001f && MathF.Abs(points[1].DistanceTo(targetCenter) - targetRadius) < .0001f && info.Point.DistanceTo(points[1]) < .0001f,
                            "Hollow-source contact pairs stay on the correct inner or outer surfaces");
                        var radial = (info.Point - targetCenter).Normalized(); var outward = kind == 1 && phase != 3 ? -radial : radial;
                        Check(info.Normal.Dot(outward) > .999f, "Hollow-source rest normal follows its actual target surface");
                    }
                    {
                        motion.From = query.Transform; motion.Motion = query.Motion; motion.RecoveryAsCollision = phase == 4;
                        for (var route = 0; route < 3; route++)
                        {
                            var hit = route == 2 ? scene[kind].TestMove(motion.From, motion.Motion, sceneResult, motion.Margin, motion.RecoveryAsCollision) : PhysicsServer.BodyTestMotion(route == 0 ? sources[kind] : scene[kind].GetRID(), motion, result);
                            Check(hit == expectedHit && (route == 2 ? sceneResult.GetColliderRID() : result.GetColliderRID()) == (expectedHit ? target : default), "Hollow-source body queries retain the same geometric truth");
                            if (!hit) continue;
                            Check(route == 2 ? ReferenceEquals(sceneResult.GetLocalShape(), primary[kind]) : result.GetCollisionLocalShape() == 1 && result.GetColliderShape() == 1 && MathF.Abs(result.GetCollisionUnsafeFraction() - expectedFraction) < .000002f,
                                "Hollow-source body motion retains first entry and logical source ownership");
                            if (phase == 4)
                            {
                                // Four 40% corrections of (.5 penetration + .05 margin - .0025 slack).
                                var expectedTravel = new Vector2(-sign * .476544f, 0);
                                var travel = route == 2 ? sceneResult.GetTravel() : result.GetTravel();
                                Check(travel.DistanceTo(expectedTravel) < .0001f, "Hollow-source initial recovery follows the analytic inner-wall correction.");
                                motion.RecoveryAsCollision = false;
                                var withoutRecoveryHit = route == 2 ? scene[kind].TestMove(motion.From, motion.Motion, sceneResult, motion.Margin) : PhysicsServer.BodyTestMotion(route == 0 ? sources[kind] : scene[kind].GetRID(), motion, result);
                                Check(!withoutRecoveryHit && (route == 2 ? sceneResult.GetColliderRID() : result.GetColliderRID()) == default &&
                                    (route == 2 ? sceneResult.GetTravel() : result.GetTravel()).DistanceTo(travel) < .0001f,
                                    "Disabling recovery collision retains correction while clearing the hollow-source contact.");
                                motion.RecoveryAsCollision = true;
                            }
                        }
                    }
                    var before = ticker.Position.X; tree.PhysicsFrame(.01); Check(MathF.Abs(ticker.Position.X - before - .01f) < .0001f, "Custom motion measures real selected physics steps");
                }
            }
            finally { foreach (var id in sources) if (id.IsValid()) PhysicsServer.FreeRID(id); foreach (var id in targets) if (id.IsValid()) PhysicsServer.FreeRID(id); }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
