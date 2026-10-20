using Electron2D;

public static partial class BackendChecks
{
    private sealed class LongMotionDisk : Shape
    {
        private readonly ShapeGeometry _geometry = ShapeGeometry.Circle(1);
        protected override ShapeGeometry GetGeometryCore() => _geometry;
    }
    public static void RunLongDiskMotion(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread); var owner = Thread.CurrentThread; var solved = 0;
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("LongDiskMotion", () => new Solver
        { StepHook = () => { Check((Thread.CurrentThread != owner) == threaded, "Long disk motion retains its numerical lane"); solved++; } });
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("LongDiskMotion");
            Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null, "Long disk motion keeps its requested implementation");
            using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
            using var disk = new CircleShape { Radius = 1 }; using var procedural = new LongMotionDisk(); using var gap = new CircleShape { Radius = .01f };
            Shape[] sources = [disk, procedural]; var movers = new RID[2]; var scene = new StaticBody[2]; var primary = new CollisionShape[2]; var peers = new RID[2];
            using var peer = new StaticBody { CollisionLayer = 0, CollisionMask = 0 }; peer.AddChild(new CollisionShape { Shape = gap, Disabled = true });
            peer.AddChild(new CollisionShape { Shape = disk }); root.AddChild(peer); peers[1] = peer.GetRID();
            using var ticker = new RigidBody { Position = new(100, -300), CollisionLayer = 0, CollisionMask = 0, GravityScale = 0, CanSleep = false, LinearVelocity = Vector2.Right, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
            ticker.AddChild(new CollisionShape { Shape = disk }); root.AddChild(ticker);
            using var shapeQuery = new PhysicsShapeQueryParameters { Margin = 0 }; var hits = new PhysicsShapeResult[4];
            using var motion = new PhysicsTestMotionParameters { Margin = .05f }; using var result = new PhysicsTestMotionResult(); using var sceneResult = new KinematicCollision();
            try
            {
                peers[0] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(peers[0], PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionLayer(peers[0], 0);
                PhysicsServer.BodySetCollisionMask(peers[0], 0); PhysicsServer.BodyAddShape(peers[0], gap.GetRID(), disabled: true); PhysicsServer.BodyAddShape(peers[0], procedural.GetRID()); PhysicsServer.BodySetSpace(peers[0], world.Space);
                for (var kind = 0; kind < 2; kind++)
                {
                    movers[kind] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(movers[kind], PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionLayer(movers[kind], 0);
                    PhysicsServer.BodyAddShape(movers[kind], gap.GetRID(), disabled: true); PhysicsServer.BodyAddShape(movers[kind], sources[kind].GetRID());
                    PhysicsServer.BodyAddShape(movers[kind], sources[kind].GetRID(), new(0, new Vector2(-5, 8)));
                    scene[kind] = new StaticBody { Position = new(400000 + kind * 50, -400000), CollisionLayer = 0 };
                    scene[kind].AddChild(new CollisionShape { Shape = gap, Disabled = true }); primary[kind] = new CollisionShape { Shape = sources[kind] }; scene[kind].AddChild(primary[kind]);
                    scene[kind].AddChild(new CollisionShape { Shape = sources[kind], Position = new(-5, 8) }); root.AddChild(scene[kind]);
                    PhysicsServer.BodySetTransform(movers[kind], scene[kind].Transform); PhysicsServer.BodySetSpace(movers[kind], world.Space);
                }
                using var ray = new SeparationRayShape { Length = 1, SlideOnSlope = true };
                PhysicsServer.BodySetCollisionLayer(peers[0], 1); PhysicsServer.BodySetTransform(peers[0], new(0, new Vector2(9999, 0)));
                shapeQuery.Shape = ray; shapeQuery.Transform = new(-Mathf.Pi / 2, Vector2.Zero); shapeQuery.Motion = new(29997, 0);
                var rayCast = world.DirectSpaceState.CastMotion(shapeQuery); var rayEntry = 9997f / 29997;
                Check(rayCast.SafeFraction <= rayEntry + .000002f && rayCast.UnsafeFraction >= rayEntry - .000002f && rayCast.UnsafeFraction < 1,
                    "Long directed point-to-disk sweeps preserve the same first entry");
                for (var i = 0; i < 128; i++) Frame(i); Collect();
                var beforeSteps = solved; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame(i);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(backend is not null || solved - beforeSteps == 128, "Long disk motion executes each registered frame");
                Check(own == 0 && all == 0, $"Long disk motion allocated {own}/{all} B");
                Console.WriteLine($"Long disk motion {world.PhysicsBackendName}/{threaded}: builtin/procedural compound sources, scene/raw peers, long/oblique/tangent/miss controls; 128 frames / 640 calls {own}/{all} B.");
                void Frame(int configuration)
                {
                    var sample = configuration % 6; var kind = configuration / 6 % 2; var role = configuration / 12 % 2;
                    var outer = configuration / 24 % 2 == 0 ? Transform.Identity : new Transform(Vector2.Down, Vector2.Left, new(70, -30));
                    var sign = configuration / 48 % 2 == 0 ? 1 : -1;
                    var distance = sample == 0 ? 9999f : sample is 1 or 2 ? 30000 : sample == 3 ? 12345 : 10000;
                    var length = sample == 0 ? 29997f : sample == 1 ? 41100 : sample == 2 ? 65100 : sample == 3 ? 24566.55f : 20000;
                    var lateral = sample == 1 ? .7f : sample == 2 ? 1.5f : sample == 3 ? -.7f : sample == 4 ? 2.125f : sample == 5 ? 2 : 0;
                    var expectedHit = sample != 4; var expected = expectedHit ? (float)((distance - Math.Sqrt(4 - (double)lateral * lateral)) / length) : 1;
                    var target = peers[role]; var targetCenter = outer * new Vector2(sign * distance, lateral);
                    for (var i = 0; i < 2; i++) PhysicsServer.BodySetCollisionLayer(peers[i], i == role ? 1u : 0u);
                    PhysicsServer.BodySetTransform(target, new(0, targetCenter));
                    shapeQuery.Shape = sources[kind]; shapeQuery.Transform = outer; shapeQuery.Motion = outer.BasisXform(new(sign * length, 0));
                    var cast = world.DirectSpaceState.CastMotion(shapeQuery); Bracket(cast.SafeFraction, cast.UnsafeFraction);
                    var count = world.DirectSpaceState.IntersectShape(shapeQuery, hits);
                    Check(count == (expectedHit ? 1 : 0) && (!expectedHit || hits[0].ColliderRID == target && hits[0].ShapeIndex == 1), "Long disk overlap sweep retains actual hit truth and target identity");
                    motion.From = outer; motion.Motion = shapeQuery.Motion;
                    for (var route = 0; route < 3; route++)
                    {
                        var hit = route == 2 ? scene[kind].TestMove(outer, motion.Motion, sceneResult, motion.Margin) : PhysicsServer.BodyTestMotion(route == 0 ? movers[kind] : scene[kind].GetRID(), motion, result);
                        if (hit != expectedHit) throw new InvalidOperationException($"Long disk body hit sample={sample}, role={role}, kind={kind}, route={route}, sign={sign}: {hit}.");
                        var collider = route == 2 ? sceneResult.GetColliderRID() : result.GetColliderRID();
                        if (!hit) { Check(collider == default, "Long disk misses clear previous collider identity"); continue; }
                        if (route != 2) Bracket(result.GetCollisionSafeFraction(), result.GetCollisionUnsafeFraction());
                        Check(collider == target && (route == 2 ? ReferenceEquals(sceneResult.GetLocalShape(), primary[kind]) : result.GetCollisionLocalShape() == 1 && result.GetColliderShape() == 1), "Long disk compound motion selects the actual primary source and target slot");
                        var point = route == 2 ? sceneResult.GetPosition() : result.GetCollisionPoint(); var normal = route == 2 ? sceneResult.GetNormal() : result.GetCollisionNormal();
                        Check(MathF.Abs(point.DistanceTo(targetCenter) - 1) < .01f && MathF.Abs(normal.Length() - 1) < .001f && normal.Dot((point - targetCenter).Normalized()) > .995f,
                            "Long disk body contact stays on the target surface with its outward normal");
                    }
                    var before = ticker.Position.X; tree.PhysicsFrame(.01); Check(MathF.Abs(ticker.Position.X - before - .01f) < .0001f, "Long disk measurements include actual selected simulation");
                    void Bracket(float low, float high)
                    {
                        if (expectedHit ? low < 0 || low > expected + .000002f || high < expected - .000002f || high > 1 || low > high || high - low > 3f / 512 + .000002f : low != 1 || high != 1)
                            throw new InvalidOperationException($"Long disk cast sample={sample}, kind={kind}, role={role}, sign={sign}: {low}/{high}, expected={expected}.");
                    }
                }
            }
            finally { foreach (var body in movers) if (body.IsValid()) PhysicsServer.FreeRID(body); if (peers[0].IsValid()) PhysicsServer.FreeRID(peers[0]); }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
