using Electron2D;

public static partial class BackendChecks
{
    public static void RunLongCapsuleMotion(PhysicsServer.Backend? backend = null, bool threaded = false, bool convexPairs = false)
    {
        if (convexPairs) CheckLongMotionOracle();
        var label = convexPairs ? "LongConvexMotion" : "LongCapsuleMotion";
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread); var owner = Thread.CurrentThread; var solved = 0;
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer(label, () => new Solver
        { StepHook = () => { Check((Thread.CurrentThread != owner) == threaded, "Long capsule retains its numerical lane"); solved++; } });
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World(label);
            Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null, "Long capsule keeps its requested implementation");
            using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
            using var disk = new CircleShape { Radius = 1 }; using var capsule = new CapsuleShape { Radius = 1, Height = 6 };
            using var segment = new SegmentShape { A = new(0, -2), B = new(0, 2) }; using var gap = new CircleShape { Radius = .01f };
            using var rectangle = new RectangleShape { Size = new(2, 6) }; using var triangle = new ConvexPolygonShape { Points = [new(-1, -2), new(2, -.5f), new(-.5f, 3)] };
            Shape[] elongated = [capsule, segment]; Shape[] shapes = [capsule, segment, rectangle, triangle];
            Vector2[][] cores = [[new(0, -2), new(0, 2)], [new(0, -2), new(0, 2)], [new(-1, -3), new(1, -3), new(1, 3), new(-1, 3)], [new(-1, -2), new(2, -.5f), new(-.5f, 3)]];
            var pairs = convexPairs ? 16 : 4; var frames = convexPairs ? 3072 : 512;
            var movingShapes = new Shape[pairs]; var targetShapes = new Shape[pairs];
            for (var k = 0; k < pairs; k++)
            {
                movingShapes[k] = convexPairs ? shapes[k / 4] : k >= 2 ? disk : elongated[k % 2];
                targetShapes[k] = convexPairs ? shapes[k % 4] : k >= 2 ? elongated[k % 2] : disk;
            }
            var rawSources = new RID[pairs]; var rawPeers = new RID[pairs];
            try
            {
                var sources = new StaticBody[pairs]; var primaries = new CollisionShape[pairs]; var peers = new StaticBody[pairs];
                for (var k = 0; k < pairs; k++)
                {
                    sources[k] = new StaticBody { Position = new(400000 + k * 50, -400000), CollisionLayer = 0 };
                    sources[k].AddChild(new CollisionShape { Shape = gap, Disabled = true }); primaries[k] = new CollisionShape { Shape = movingShapes[k] };
                    sources[k].AddChild(primaries[k]); root.AddChild(sources[k]);
                    peers[k] = new StaticBody { CollisionLayer = 0, CollisionMask = 0 }; peers[k].AddChild(new CollisionShape { Shape = gap, Disabled = true });
                    peers[k].AddChild(new CollisionShape { Shape = targetShapes[k] }); root.AddChild(peers[k]);
                    rawSources[k] = PhysicsServer.BodyCreate(); rawPeers[k] = PhysicsServer.BodyCreate();
                    foreach (var body in new[] { rawSources[k], rawPeers[k] })
                    {
                        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionLayer(body, 0);
                        PhysicsServer.BodyAddShape(body, gap.GetRID(), disabled: true);
                    }
                    PhysicsServer.BodyAddShape(rawSources[k], movingShapes[k].GetRID());
                    PhysicsServer.BodyAddShape(rawPeers[k], targetShapes[k].GetRID()); PhysicsServer.BodySetCollisionMask(rawPeers[k], 0);
                    PhysicsServer.BodySetTransform(rawSources[k], sources[k].Transform); PhysicsServer.BodySetSpace(rawSources[k], world.Space); PhysicsServer.BodySetSpace(rawPeers[k], world.Space);
                }
                using var ticker = new RigidBody { Position = new(100, -300), CollisionLayer = 0, CollisionMask = 0, GravityScale = 0, CanSleep = false, LinearVelocity = Vector2.Right, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
                ticker.AddChild(new CollisionShape { Shape = disk }); root.AddChild(ticker);
                using var query = new PhysicsShapeQueryParameters { Margin = 0 }; var hits = new PhysicsShapeResult[4];
                using var motion = new PhysicsTestMotionParameters { Margin = .05f }; using var result = new PhysicsTestMotionResult(); using var sceneResult = new KinematicCollision();
                for (var i = 0; i < frames; i++) Frame(i); Collect();
                var beforeSteps = solved; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < frames; i++) Frame(i);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(backend is not null || solved - beforeSteps == frames, "Long capsule executes each registered frame");
                Check(own == 0 && all == 0, $"Long capsule allocated {own}/{all} B");
                Console.WriteLine($"{label} {world.PhysicsBackendName}/{threaded}: {pairs} pairs; {frames} frames / {frames * 5} calls {own}/{all} B.");
                void Frame(int configuration)
                {
                    var sample = configuration % 8; var kind = configuration / 8 % pairs; var orientations = convexPairs ? 2 : 4;
                    var orientation = configuration / (8 * pairs) % orientations; var sign = configuration / (8 * pairs * orientations) % 2 == 0 ? 1 : -1;
                    var relative = convexPairs ? configuration / (32 * pairs * orientations) % 3 : 0;
                    var outer = orientation == 0 ? Transform.Identity : orientation == 1 && !convexPairs ? new Transform(Vector2.Down, Vector2.Left, new(70, -30)) :
                        orientation == 2 || convexPairs ? new Transform(new(.8f, .6f), new(-.6f, .8f), new(70, -30)) : new Transform(new(.6f, -.8f), new(.8f, .6f), new(-50, 80));
                    var rawTarget = configuration / (16 * pairs * orientations) % 2 == 1; var reverse = kind >= 2; var radius = kind % 2 == 0 ? 2d : 1d;
                    var moving = movingShapes[kind]; var target = targetShapes[kind];
                    var distance = sample % 2 == 0 ? 9999f : 30000; var length = sample == 6 ? distance / 2 : distance * 2.1f;
                    var lateral = sample == 0 ? 0 : sample == 1 ? 1 : sample == 2 ? 2.7f : sample == 3 ? (float)(radius + 1.5) : sample == 4 ? (float)(radius + 2.125) : sample == 5 ? -2.7f : .5f;
                    if (convexPairs) lateral = sample == 0 ? 0 : sample == 1 ? 1 : sample == 2 ? 2.7f : sample == 3 ? 4.5f : sample == 4 ? 8 : sample == 5 ? -2.7f : .5f;
                    var relativePose = relative == 0 ? Transform.Identity : relative == 1 ? new Transform(Vector2.Down, Vector2.Left, Vector2.Zero) : new Transform(new(.8f, .6f), new(-.6f, .8f), Vector2.Zero);
                    relativePose.Origin = new(sign * (sample == 7 ? -distance : distance), lateral);
                    var targetPose = outer * relativePose;
                    var source = sources[kind]; var primary = primaries[kind]; var peer = peers[kind];
                    for (var k = 0; k < pairs; k++)
                    {
                        peers[k].CollisionLayer = k == kind && !rawTarget ? 1u : 0u;
                        PhysicsServer.BodySetCollisionLayer(rawPeers[k], k == kind && rawTarget ? 1u : 0u);
                    }
                    var targetRID = rawTarget ? rawPeers[kind] : peer.GetRID();
                    if (rawTarget) PhysicsServer.BodySetTransform(targetRID, targetPose); else peer.Transform = targetPose;
                    query.Margin = convexPairs ? sample % 3 == 0 ? 0 : sample % 3 == 1 ? .05f : .2f : 0;
                    query.Shape = moving; query.Transform = outer; query.Motion = outer.BasisXform(new(sign * length, 0));
                    var elongatedPose = reverse ? targetPose : outer; var point = reverse ? outer.Origin : targetPose.Origin;
                    var expected = CapsuleEntry(point, reverse ? query.Motion : -query.Motion, elongatedPose * new Vector2(0, -2), elongatedPose * new Vector2(0, 2), radius);
                    Span<(double X, double Y)> hull = stackalloc (double, double)[32]; var hullCount = 0;
                    if (convexPairs) { hullCount = LongMotionHull(cores[kind / 4], outer, cores[kind % 4], targetPose, hull); expected = LongHullEntry(hull[..hullCount], query.Motion, (kind / 4 == 0 ? 1 : 0) + (kind % 4 == 0 ? 1 : 0)); }
                    var expectedHit = expected <= 1;
                    var queryExpected = convexPairs ? LongHullEntry(hull[..hullCount], query.Motion, (kind / 4 == 0 ? 1 : 0) + (kind % 4 == 0 ? 1 : 0) + (double)query.Margin) : expected;
                    var queryHit = queryExpected <= 1;
                    var cast = world.DirectSpaceState.CastMotion(query); Bracket(cast.SafeFraction, cast.UnsafeFraction, queryExpected);
                    var count = world.DirectSpaceState.IntersectShape(query, hits);
                    if (count != (queryHit ? 1 : 0) || queryHit && (hits[0].ColliderRID != targetRID || hits[0].ShapeIndex != 1)) throw new InvalidOperationException($"Long capsule overlap case={configuration}: {count}, expected={expected}.");
                    motion.From = outer; motion.Motion = query.Motion;
                    for (var route = 0; route < 3; route++)
                    {
                        var hit = route == 2 ? source.TestMove(outer, motion.Motion, sceneResult, motion.Margin) : PhysicsServer.BodyTestMotion(route == 0 ? rawSources[kind] : source.GetRID(), motion, result);
                        if (hit != expectedHit) throw new InvalidOperationException($"Long capsule hit case={configuration}, route={route}: {hit}, expected={expected}.");
                        var collider = route == 2 ? sceneResult.GetColliderRID() : result.GetColliderRID();
                        if (!hit) { Check(collider == default, "Long capsule miss clears prior contact"); continue; }
                        if (route != 2) Bracket(result.GetCollisionSafeFraction(), result.GetCollisionUnsafeFraction(), expected);
                        Check(collider == targetRID && (route == 2 ? ReferenceEquals(sceneResult.GetLocalShape(), primary) : result.GetCollisionLocalShape() == 1 && result.GetColliderShape() == 1), "Long capsule retains source and target slots");
                        var contact = route == 2 ? sceneResult.GetPosition() : result.GetCollisionPoint(); var normal = route == 2 ? sceneResult.GetNormal() : result.GetCollisionNormal();
                        var local = targetPose.AffineInverse() * contact;
                        if (convexPairs)
                        {
                            var core = cores[kind % 4]; var nearest = Geometry.GetClosestPointToSegment(local, core[0], core[1]); var distanceToCore = local.DistanceSquaredTo(nearest);
                            for (var edge = 1; edge < core.Length; edge++) { var candidate = Geometry.GetClosestPointToSegment(local, core[edge], core[(edge + 1) % core.Length]); var d = local.DistanceSquaredTo(candidate); if (d < distanceToCore) { distanceToCore = d; nearest = candidate; } }
                            Check(MathF.Abs(MathF.Sqrt(distanceToCore) - (kind % 4 == 0 ? 1 : 0)) < .015f && MathF.Abs(normal.Length() - 1) < .001f && normal.Dot(query.Motion.Normalized()) < .001f, "Long convex contact lies on its target surface with an opposing unit normal");
                        }
                        else
                        {
                            var center = reverse ? new Vector2(0, Math.Clamp(local.Y, -2, 2)) : Vector2.Zero;
                            Check(MathF.Abs(local.DistanceTo(center) - (target == segment ? 0 : 1)) < .015f && MathF.Abs(normal.Length() - 1) < .001f &&
                                (target == segment || normal.Dot(targetPose.BasisXform((local - center).Normalized())) > .995f), "Long capsule retains a target surface point and outward unit normal");
                        }
                    }
                    var before = ticker.Position.X; tree.PhysicsFrame(.01); Check(MathF.Abs(ticker.Position.X - before - .01f) < .0001f, "Long capsule includes actual selected physics");
                    void Bracket(float low, float high, double entry)
                    {
                        if (entry <= 1 ? low < 0 || low > entry + .000002 || high < entry - .000002 || high > 1 || low > high || high - low > 3f / 512 + .000002f : low != 1 || high != 1)
                            throw new InvalidOperationException($"Long capsule cast case={configuration}: {low}/{high}, expected={entry}.");
                    }
                }
            }
            finally
            {
                foreach (var body in rawSources) if (body.IsValid()) PhysicsServer.FreeRID(body);
                foreach (var body in rawPeers) if (body.IsValid()) PhysicsServer.FreeRID(body);
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
    private static double CapsuleEntry(Vector2 point, Vector2 motion, Vector2 first, Vector2 second, double radius)
    {
        var ex = (double)second.X - first.X; var ey = (double)second.Y - first.Y; var size = Math.Sqrt(ex * ex + ey * ey); ex /= size; ey /= size;
        var px = (double)point.X - first.X; var py = (double)point.Y - first.Y;
        var x = px * ey - py * ex; var y = px * ex + py * ey; var dx = (double)motion.X * ey - (double)motion.Y * ex; var dy = (double)motion.X * ex + (double)motion.Y * ey;
        var best = double.PositiveInfinity;
        if (dx != 0) for (var sign = -1; sign <= 1; sign += 2) { var t = (sign * radius - x) / dx; if (t >= 0 && y + t * dy >= 0 && y + t * dy <= size) best = Math.Min(best, t); }
        var speed = Math.Sqrt(dx * dx + dy * dy);
        for (var end = 0; end < 2; end++)
        {
            var cy = y - end * size; var along = (x * dx + cy * dy) / speed; var side = (x * dy - cy * dx) / speed;
            var discriminant = (radius - Math.Abs(side)) * (radius + Math.Abs(side));
            if (along >= 0 || discriminant < 0) continue;
            var t = (-along - Math.Sqrt(discriminant)) / speed; var at = y + t * dy;
            if (t >= 0 && (end == 0 ? at <= 0 : at >= size)) best = Math.Min(best, t);
        }
        return best;
    }
}
