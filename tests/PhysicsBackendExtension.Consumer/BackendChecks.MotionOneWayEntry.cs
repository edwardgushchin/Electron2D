using Electron2D;

public static partial class BackendChecks
{
    public static void RunMotionOneWayEntry(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            var owner = Thread.CurrentThread;
            if (backend is null) PhysicsServerManager.RegisterServer("OneWayEntry", () => new Solver
            { StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "One-way entry retains the selected numerical lane.") });
            using var world = backend is { } selected ? new World(selected) : new World("OneWayEntry");
            Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
                "One-way entry selects the requested implementation without fallback.");
            using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
            using var circle = new CircleShape { Radius = 1 };
            using var platform = new RectangleShape { Size = new(10, 2) };
            var mover = PhysicsServer.BodyCreate(); var target = PhysicsServer.BodyCreate();
            using var parameters = new PhysicsTestMotionParameters { Margin = .08f };
            using var result = new PhysicsTestMotionResult();
            try
            {
                PhysicsServer.BodySetMode(mover, PhysicsServer.BodyMode.Static);
                PhysicsServer.BodySetCollisionLayer(mover, 0); PhysicsServer.BodySetCollisionMask(mover, 4);
                PhysicsServer.BodyAddShape(mover, circle.GetRID());
                PhysicsServer.BodySetTransform(mover, new(0, new(100, 100))); PhysicsServer.BodySetSpace(mover, world.Space);
                PhysicsServer.BodySetMode(target, PhysicsServer.BodyMode.Static);
                PhysicsServer.BodySetCollisionLayer(target, 4); PhysicsServer.BodySetCollisionMask(target, 0);
                PhysicsServer.BodyAddShape(target, platform.GetRID());
                PhysicsServer.BodySetShapeAsOneWayCollision(target, 0, true, 1);
                PhysicsServer.BodySetTransform(target, new(0, new(5, 1))); PhysicsServer.BodySetSpace(target, world.Space);
                tree.PhysicsFrame(.01);
                var failures = 0;
                foreach (var entry in new[]
                {
                    (Name: "tangent-front-corner", From: new Vector2(-3, -.5f), Motion: new Vector2(5, 0), Hit: true),
                    (Name: "receding-front-corner", From: new Vector2(-3, .5f), Motion: new Vector2(5, -2), Hit: true),
                    (Name: "tangent-back-side", From: new Vector2(-3, 1.5f), Motion: new Vector2(5, 0), Hit: false)
                })
                {
                    parameters.From = new(0, entry.From); parameters.Motion = entry.Motion;
                    var hit = PhysicsServer.BodyTestMotion(mover, parameters, result);
                    Console.WriteLine($"One-way entry {backend?.ToString() ?? "registered"}/{threaded} {entry.Name}: hit={hit}, expected={entry.Hit}, travel={result.GetTravel()}, normal={result.GetCollisionNormal()}");
                    if (hit != entry.Hit) failures++;
                }
                Check(failures == 0, "Body motion must choose one-way entry from the actual contact side, including tangent and receding corner approach.");
                using (var boundary = new WorldBoundaryShape { Normal = Vector2.Left })
                {
                    var wall = PhysicsServer.BodyCreate();
                    try
                    {
                        PhysicsServer.BodySetMode(wall, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionLayer(wall, 4);
                        PhysicsServer.BodyAddShape(wall, boundary.GetRID()); PhysicsServer.BodySetTransform(wall, new(0, new(12, 0)));
                        PhysicsServer.BodySetSpace(wall, world.Space); parameters.From = new(0, new(-3, 1.5f)); parameters.Motion = new(20, 0);
                        Check(PhysicsServer.BodyTestMotion(mover, parameters, result) && result.GetColliderRID() == wall,
                            "A rejected one-way side must not hide a later valid wall.");
                        PhysicsServer.BodySetShapeAsOneWayCollision(wall, 0, true, 1, new(1, -1));
                        parameters.ExcludeBodies = [target]; parameters.From = new(0, new(9, -3)); parameters.Motion = new(5, 10);
                        Check(PhysicsServer.BodyTestMotion(mover, parameters, result) && result.GetColliderRID() == wall,
                            "An oblique one-way direction admits a real plane-front impact even with receding projected motion.");
                        parameters.ExcludeBodies = [];
                    }
                    finally { PhysicsServer.FreeRID(wall); }
                }
                PhysicsServer.BodySetCollisionPriority(target, float.Epsilon);
                parameters.From = new(0, new(1, -.5f)); parameters.Motion = new(5, 0);
                Check(PhysicsServer.BodyTestMotion(mover, parameters, result) && result.GetCollisionSafeFraction() == 0,
                    "A tangent initial overlap remains blocked when a subnormal priority rounds recovery to zero.");
                parameters.Motion = new(0, -5);
                Check(!PhysicsServer.BodyTestMotion(mover, parameters, result) && result.GetTravel().DistanceTo(parameters.Motion) < .001f,
                    "A receding initial overlap remains free to leave the one-way surface.");
                PhysicsServer.BodySetCollisionPriority(target, 1);
                PhysicsServer.BodySetShapeAsOneWayCollision(target, 0, false, 1);
                parameters.From = new(0, new(-3, 1.5f)); parameters.Motion = new(5, 0);
                Check(PhysicsServer.BodyTestMotion(mover, parameters, result), "Disabling one-way policy restores the same side collision.");
                PhysicsServer.BodySetShapeAsOneWayCollision(target, 0, true, 1);
                MotionOneWayEntryMatrix(root, tree, world, backend, threaded);
            }
            finally { PhysicsServer.FreeRID(mover); PhysicsServer.FreeRID(target); }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
    private sealed record EntryProfile(Shape Source, Vector2[] SourceCore, float SourceRadius,
        Shape Target, Vector2[] TargetCore, float TargetRadius, Transform TargetPose, Vector2 From, Vector2 Motion, bool Hit = true);
    private sealed record EntryCase(string Name, RID Raw, StaticBody Scene, RID Target, Transform Parked,
        Transform From, Vector2 Motion, bool Hit, float Fraction, Vector2 Normal);

    private static void MotionOneWayEntryMatrix(SubViewport root, SceneTree tree, World world, PhysicsServer.Backend? backend, bool threaded)
    {
        using var circle = new CircleShape { Radius = 1 };
        using var capsule = new CapsuleShape { Radius = 1, Height = 4 };
        using var segment = new SegmentShape { A = new(-1, 0), B = new(1, 0) };
        using var box = new RectangleShape { Size = new(2, 2) };
        using var triangle = new ConvexPolygonShape { Points = [new(-1, -1), new(1, 1), new(-1, 1)] };
        using var roundTarget = new CircleShape { Radius = 2 };
        using var capTarget = new CapsuleShape { Radius = 2, Height = 6 };
        using var flatTarget = new RectangleShape { Size = new(10, 2) };
        using var edgeTarget = new SegmentShape { A = Vector2.Zero, B = new(10, 0) };
        using var hollowTarget = new ConcavePolygonShape { Segments = [Vector2.Zero, new(10, 0)] };
        using var polygonTarget = new ConvexPolygonShape { Points = [new(-5, -1), new(5, -1), new(5, 1), new(-5, 1)] };
        Vector2[] disk = [Vector2.Zero], cap = [new(0, -1), new(0, 1)], line = [new(-1, 0), new(1, 0)];
        Vector2[] square = [new(-1, -1), new(1, -1), new(1, 1), new(-1, 1)];
        Vector2[] rectangle = [new(-5, -1), new(5, -1), new(5, 1), new(-5, 1)];
        EntryProfile[] profiles =
        [
            new(circle, disk, 1, flatTarget, rectangle, 0, new(0, new(5, 1)), new(-3, -.5f), new(5, 0)),
            new(circle, disk, 1, flatTarget, rectangle, 0, new(0, new(5, 1)), new(-3, .5f), new(5, -2)),
            new(circle, disk, 1, flatTarget, rectangle, 0, new(0, new(5, 1)), new(-3, 1.5f), new(5, 0), false),
            new(capsule, cap, 1, flatTarget, rectangle, 0, new(0, new(5, 1)), new(-3, -1.5f), new(5, 0)),
            new(circle, disk, 1, polygonTarget, rectangle, 0, new(0, new(5, 1)), new(-3, -.5f), new(5, 0)),
            new(circle, disk, 1, capTarget, cap, 2, new(0, new(0, 3)), new(-4, -.5f), new(6, 0)),
            new(circle, disk, 1, roundTarget, disk, 2, new(0, new(0, 2)), new(-4, -.5f), new(6, 0)),
            new(circle, disk, 1, roundTarget, disk, 2, new(0, new(0, 2)), new(-4, .5f), new(6, -2)),
            new(segment, line, 0, roundTarget, disk, 2, new(0, new(0, 2)), new(-4, .5f), new(6, 0)),
            new(box, square, 0, roundTarget, disk, 2, new(0, new(0, 2)), new(-4, -.5f), new(6, 0)),
            new(triangle, triangle.Points, 0, roundTarget, disk, 2, new(0, new(0, 2)), new(-4, -.5f), new(6, 0)),
            new(circle, disk, 1, roundTarget, disk, 2, new(0, new(0, 2)), new(-4, 3.5f), new(6, 0), false),
            new(circle, disk, 1, edgeTarget, [Vector2.Zero, new(10, 0)], 0, Transform.Identity, new(-3, -.5f), new(5, 0)),
            new(circle, disk, 1, hollowTarget, [Vector2.Zero, new(10, 0)], 0, Transform.Identity, new(-3, -.5f), new(5, 0))
        ];
        CheckLongMotionOracle();
        Check(Math.Abs(EntryFraction([(3d, .5d)], new(5, 0), 1) - (3 - Math.Sqrt(.75)) / 5) < 1e-12,
            "The point-core oracle reproduces the analytic disk first touch.");
        var cases = new List<EntryCase>(); var owned = new List<RID>();
        using var parameters = new PhysicsTestMotionParameters { Margin = .08f };
        using var result = new PhysicsTestMotionResult(); using var sceneResult = new KinematicCollision();
        using var ticker = new RigidBody
        {
            Position = new(-500, -500),
            GravityScale = 0,
            CanSleep = false,
            CollisionLayer = 0,
            CollisionMask = 0,
            LinearVelocity = Vector2.Right,
            LinearDampMode = RigidBody.DampMode.Replace,
            LinearDamp = 0
        };
        ticker.AddChild(new CollisionShape { Shape = circle }); root.AddChild(ticker);
        try
        {
            Span<(double X, double Y)> hull = stackalloc (double, double)[32];
            for (var pose = 0; pose < 4; pose++) for (var profile = 0; profile < profiles.Length; profile++)
                {
                    var p = profiles[profile]; var index = cases.Count;
                    var frame = new Transform(pose switch { 0 => 0, 1 => Mathf.Pi / 2, 2 => -.7f, _ => 2.4f }, new Vector2(200, index * 64));
                    var from = frame * new Transform(0, p.From); var motion = frame.BasisXform(p.Motion);
                    var targetPose = frame * p.TargetPose;
                    var parked = new Transform(from.Rotation, new Vector2(2000, index * 64));
                    var raw = PhysicsServer.BodyCreate(); owned.Add(raw);
                    PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionLayer(raw, 0); PhysicsServer.BodySetCollisionMask(raw, 4);
                    PhysicsServer.BodyAddShape(raw, p.Source.GetRID()); PhysicsServer.BodySetTransform(raw, parked); PhysicsServer.BodySetSpace(raw, world.Space);
                    var node = new StaticBody { Transform = parked, CollisionLayer = 0, CollisionMask = 4 };
                    node.AddChild(new CollisionShape { Shape = p.Source }); root.AddChild(node);
                    var target = PhysicsServer.BodyCreate(); owned.Add(target);
                    PhysicsServer.BodySetMode(target, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionLayer(target, 4); PhysicsServer.BodySetCollisionMask(target, 0);
                    PhysicsServer.BodyAddShape(target, p.Target.GetRID()); PhysicsServer.BodySetShapeAsOneWayCollision(target, 0, true, 1);
                    PhysicsServer.BodySetTransform(target, targetPose); PhysicsServer.BodySetSpace(target, world.Space);
                    var count = LongMotionHull(p.SourceCore, from, p.TargetCore, targetPose, hull);
                    var fraction = EntryFraction(hull[..count], motion, p.SourceRadius + p.TargetRadius);
                    Check(double.IsFinite(fraction) && fraction >= 0 && fraction <= 1, $"The independent rounded-hull oracle finds geometric contact: profile={profile}, pose={pose}, fraction={fraction}.");
                    var normal = EntryNormal(hull[..count], motion, fraction);
                    var hit = p.Hit;
                    Check(hit ? normal.Dot(targetPose.Y) < -.1f : normal.Dot(targetPose.Y) > -.0001f,
                        "The independent normal agrees with the authored front/side/back case within world-coordinate rounding.");
                    cases.Add(new($"one-way profile={profile}, pose={pose}", raw, node, target, parked, from, motion, hit, (float)fraction, normal));
                }
            foreach (var entry in cases)
            {
                Evaluate(entry);
                entry.Scene.GlobalTransform = entry.From;
                using var collision = entry.Scene.MoveAndCollide(entry.Motion);
                Check((collision is not null) == entry.Hit, entry.Name);
                if (collision is not null) Check(collision.GetColliderRID() == entry.Target, entry.Name);
                entry.Scene.GlobalTransform = entry.Parked;
            }
            for (var i = 0; i < 128; i++) Frame(i);
            Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var total = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 512; i++) Frame(i);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; total = GC.GetTotalAllocatedBytes(true) - total;
            Check(owner == 0 && total == 0, $"One-way entry complete frames allocate {owner}/{total} bytes.");
            Console.WriteLine($"One-way entry {backend?.ToString() ?? "registered"}/{threaded}: {cases.Count} rounded/flat corner profiles, four poses, raw/scene/applied motion, double first-touch oracle; 512 warmed full frames {owner}/{total} B.");
            void Frame(int i)
            {
                var x = ticker.Position.X; tree.PhysicsFrame(1d / 60);
                Check(MathF.Abs(ticker.Position.X - x - 1f / 60) < .0001f, "The one-way query bracket includes an actual selected physical step.");
                Evaluate(cases[i % cases.Count]);
            }
            void Evaluate(EntryCase entry)
            {
                parameters.From = entry.From; parameters.Motion = entry.Motion;
                var hit = PhysicsServer.BodyTestMotion(entry.Raw, parameters, result);
                var sceneHit = entry.Scene.TestMove(entry.From, entry.Motion, sceneResult);
                if (hit != entry.Hit || sceneHit != entry.Hit)
                    throw new InvalidOperationException($"{entry.Name}: expected={entry.Hit}, raw={hit}, scene={sceneHit}, normal={result.GetCollisionNormal()}, oracleNormal={entry.Normal}, pass={PhysicsServer.BodyGetTransform(entry.Target).Y}");
                Check(PhysicsServer.BodyGetTransform(entry.Raw) == entry.Parked && entry.Scene.GlobalTransform == entry.Parked, entry.Name);
                if (!hit)
                {
                    Check(result.GetColliderRID() == default && result.GetTravel().DistanceTo(entry.Motion) < .002f, entry.Name);
                    return;
                }
                Check(result.GetColliderRID() == entry.Target && sceneResult.GetColliderRID() == entry.Target &&
                    result.GetCollisionSafeFraction() <= entry.Fraction + .0001f && result.GetCollisionUnsafeFraction() >= entry.Fraction - .0001f &&
                    result.GetCollisionUnsafeFraction() - result.GetCollisionSafeFraction() <= .0051f &&
                    result.GetCollisionNormal().Dot(entry.Normal) > .95f, entry.Name);
            }
        }
        finally
        {
            foreach (var entry in cases) entry.Scene.Dispose();
            foreach (var id in owned) PhysicsServer.FreeRID(id);
        }
    }

    private static double EntryFraction(ReadOnlySpan<(double X, double Y)> hull, Vector2 motion, double radius)
    {
        if (hull.Length != 1) return LongHullEntry(hull, motion, radius);
        var point = hull[0]; var speed = Math.Sqrt((double)motion.X * motion.X + (double)motion.Y * motion.Y);
        var along = (point.X * motion.X + point.Y * motion.Y) / speed;
        var side = (point.X * motion.Y - point.Y * motion.X) / speed;
        var discriminant = (radius - Math.Abs(side)) * (radius + Math.Abs(side));
        return discriminant < 0 ? double.PositiveInfinity : (along - Math.Sqrt(discriminant)) / speed;
    }

    private static Vector2 EntryNormal(ReadOnlySpan<(double X, double Y)> hull, Vector2 motion, double fraction)
    {
        var x = motion.X * fraction; var y = motion.Y * fraction; var best = double.PositiveInfinity; var nx = 0d; var ny = 0d;
        for (var i = 0; i < hull.Length; i++)
        {
            var a = hull[i]; var b = hull[(i + 1) % hull.Length]; var dx = b.X - a.X; var dy = b.Y - a.Y;
            var size = dx * dx + dy * dy; var t = size == 0 ? 0 : Math.Clamp(((x - a.X) * dx + (y - a.Y) * dy) / size, 0, 1);
            var vx = x - a.X - t * dx; var vy = y - a.Y - t * dy; var distance = vx * vx + vy * vy;
            if (distance < best) { best = distance; nx = vx; ny = vy; }
        }
        var length = Math.Sqrt(best);
        return new((float)(nx / length), (float)(ny / length));
    }
}
