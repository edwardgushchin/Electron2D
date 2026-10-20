using Electron2D;

public static partial class BackendChecks
{
    public static void RunLongCapsuleMotion(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread); var owner = Thread.CurrentThread; var solved = 0;
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("LongCapsuleMotion", () => new Solver
        { StepHook = () => { Check((Thread.CurrentThread != owner) == threaded, "Long capsule retains its numerical lane"); solved++; } });
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("LongCapsuleMotion");
            Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null, "Long capsule keeps its requested implementation");
            using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
            using var disk = new CircleShape { Radius = 1 }; using var capsule = new CapsuleShape { Radius = 1, Height = 6 };
            using var segment = new SegmentShape { A = new(0, -2), B = new(0, 2) }; using var gap = new CircleShape { Radius = .01f };
            Shape[] elongated = [capsule, segment];
            var rawSources = new RID[4]; var rawPeers = new RID[4];
            try
            {
                var sources = new StaticBody[4]; var primaries = new CollisionShape[4]; var peers = new StaticBody[4];
                for (var k = 0; k < 4; k++)
                {
                    sources[k] = new StaticBody { Position = new(400000 + k * 50, -400000), CollisionLayer = 0 };
                    sources[k].AddChild(new CollisionShape { Shape = gap, Disabled = true }); primaries[k] = new CollisionShape { Shape = k >= 2 ? disk : elongated[k % 2] };
                    sources[k].AddChild(primaries[k]); root.AddChild(sources[k]);
                    peers[k] = new StaticBody { CollisionLayer = 0, CollisionMask = 0 }; peers[k].AddChild(new CollisionShape { Shape = gap, Disabled = true });
                    peers[k].AddChild(new CollisionShape { Shape = k >= 2 ? elongated[k % 2] : disk }); root.AddChild(peers[k]);
                    rawSources[k] = PhysicsServer.BodyCreate(); rawPeers[k] = PhysicsServer.BodyCreate();
                    foreach (var body in new[] { rawSources[k], rawPeers[k] })
                    {
                        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionLayer(body, 0);
                        PhysicsServer.BodyAddShape(body, gap.GetRID(), disabled: true);
                    }
                    PhysicsServer.BodyAddShape(rawSources[k], (k >= 2 ? disk : elongated[k % 2]).GetRID());
                    PhysicsServer.BodyAddShape(rawPeers[k], (k >= 2 ? elongated[k % 2] : disk).GetRID()); PhysicsServer.BodySetCollisionMask(rawPeers[k], 0);
                    PhysicsServer.BodySetTransform(rawSources[k], sources[k].Transform); PhysicsServer.BodySetSpace(rawSources[k], world.Space); PhysicsServer.BodySetSpace(rawPeers[k], world.Space);
                }
                using var ticker = new RigidBody { Position = new(100, -300), CollisionLayer = 0, CollisionMask = 0, GravityScale = 0, CanSleep = false, LinearVelocity = Vector2.Right, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
                ticker.AddChild(new CollisionShape { Shape = disk }); root.AddChild(ticker);
                using var query = new PhysicsShapeQueryParameters { Margin = 0 }; var hits = new PhysicsShapeResult[4];
                using var motion = new PhysicsTestMotionParameters { Margin = .05f }; using var result = new PhysicsTestMotionResult(); using var sceneResult = new KinematicCollision();
                for (var i = 0; i < 512; i++) Frame(i); Collect();
                var beforeSteps = solved; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 512; i++) Frame(i);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(backend is not null || solved - beforeSteps == 512, "Long capsule executes each registered frame");
                Check(own == 0 && all == 0, $"Long capsule allocated {own}/{all} B");
                Console.WriteLine($"Long capsule {world.PhysicsBackendName}/{threaded}: both orders, four poses; 512 frames / 2560 calls {own}/{all} B.");
                void Frame(int configuration)
                {
                    var sample = configuration % 8; var kind = configuration / 8 % 4; var orientation = configuration / 32 % 4; var sign = configuration / 128 % 2 == 0 ? 1 : -1;
                    var outer = orientation == 0 ? Transform.Identity : orientation == 1 ? new Transform(Vector2.Down, Vector2.Left, new(70, -30)) :
                        orientation == 2 ? new Transform(new(.8f, .6f), new(-.6f, .8f), new(70, -30)) : new Transform(new(.6f, -.8f), new(.8f, .6f), new(-50, 80));
                    var rawTarget = configuration / 256 % 2 == 1; var reverse = kind >= 2; var radius = kind % 2 == 0 ? 2d : 1d;
                    var moving = reverse ? disk : elongated[kind % 2]; var target = reverse ? elongated[kind % 2] : disk;
                    var distance = sample % 2 == 0 ? 9999f : 30000; var length = sample == 6 ? distance / 2 : distance * 2.1f;
                    var lateral = sample == 0 ? 0 : sample == 1 ? 1 : sample == 2 ? 2.7f : sample == 3 ? (float)(radius + 1.5) : sample == 4 ? (float)(radius + 2.125) : sample == 5 ? -2.7f : .5f;
                    var targetPose = outer; targetPose.Origin = outer * new Vector2(sign * (sample == 7 ? -distance : distance), lateral);
                    var source = sources[kind]; var primary = primaries[kind]; var peer = peers[kind];
                    for (var k = 0; k < 4; k++)
                    {
                        peers[k].CollisionLayer = k == kind && !rawTarget ? 1u : 0u;
                        PhysicsServer.BodySetCollisionLayer(rawPeers[k], k == kind && rawTarget ? 1u : 0u);
                    }
                    var targetRID = rawTarget ? rawPeers[kind] : peer.GetRID();
                    if (rawTarget) PhysicsServer.BodySetTransform(targetRID, targetPose); else peer.Transform = targetPose;
                    query.Shape = moving; query.Transform = outer; query.Motion = outer.BasisXform(new(sign * length, 0));
                    var elongatedPose = reverse ? targetPose : outer; var point = reverse ? outer.Origin : targetPose.Origin;
                    var expected = CapsuleEntry(point, reverse ? query.Motion : -query.Motion, elongatedPose * new Vector2(0, -2), elongatedPose * new Vector2(0, 2), radius);
                    var expectedHit = expected <= 1;
                    var cast = world.DirectSpaceState.CastMotion(query); Bracket(cast.SafeFraction, cast.UnsafeFraction);
                    var count = world.DirectSpaceState.IntersectShape(query, hits);
                    if (count != (expectedHit ? 1 : 0) || expectedHit && (hits[0].ColliderRID != targetRID || hits[0].ShapeIndex != 1)) throw new InvalidOperationException($"Long capsule overlap case={configuration}: {count}, expected={expected}.");
                    motion.From = outer; motion.Motion = query.Motion;
                    for (var route = 0; route < 3; route++)
                    {
                        var hit = route == 2 ? source.TestMove(outer, motion.Motion, sceneResult, motion.Margin) : PhysicsServer.BodyTestMotion(route == 0 ? rawSources[kind] : source.GetRID(), motion, result);
                        if (hit != expectedHit) throw new InvalidOperationException($"Long capsule hit case={configuration}, route={route}: {hit}, expected={expected}.");
                        var collider = route == 2 ? sceneResult.GetColliderRID() : result.GetColliderRID();
                        if (!hit) { Check(collider == default, "Long capsule miss clears prior contact"); continue; }
                        if (route != 2) Bracket(result.GetCollisionSafeFraction(), result.GetCollisionUnsafeFraction());
                        Check(collider == targetRID && (route == 2 ? ReferenceEquals(sceneResult.GetLocalShape(), primary) : result.GetCollisionLocalShape() == 1 && result.GetColliderShape() == 1), "Long capsule retains source and target slots");
                        var contact = route == 2 ? sceneResult.GetPosition() : result.GetCollisionPoint(); var normal = route == 2 ? sceneResult.GetNormal() : result.GetCollisionNormal();
                        var local = targetPose.AffineInverse() * contact; var center = reverse ? new Vector2(0, Math.Clamp(local.Y, -2, 2)) : Vector2.Zero;
                        Check(MathF.Abs(local.DistanceTo(center) - (target == segment ? 0 : 1)) < .015f && MathF.Abs(normal.Length() - 1) < .001f &&
                            (target == segment || normal.Dot(targetPose.BasisXform((local - center).Normalized())) > .995f), "Long capsule retains a target surface point and outward unit normal");
                    }
                    var before = ticker.Position.X; tree.PhysicsFrame(.01); Check(MathF.Abs(ticker.Position.X - before - .01f) < .0001f, "Long capsule includes actual selected physics");
                    void Bracket(float low, float high)
                    {
                        if (expectedHit ? low < 0 || low > expected + .000002 || high < expected - .000002 || high > 1 || low > high || high - low > 3f / 512 + .000002f : low != 1 || high != 1)
                            throw new InvalidOperationException($"Long capsule cast case={configuration}: {low}/{high}, expected={expected}.");
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
