using Electron2D;

public static partial class BackendChecks
{
    public static void RunMotionMatrix(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { MotionMatrixCore(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
    private static void MotionMatrixCore(PhysicsServer.Backend? backend, bool threaded)
    {
        var owner = Thread.CurrentThread;
        if (backend is null) PhysicsServerManager.RegisterServer("MotionMatrix", () => new Solver
        { StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "The motion matrix retains its actual numerical lane") });
        using var world = backend is { } selected ? new World(selected) : new World("MotionMatrix");
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null, "The motion matrix selects its actual backend without fallback");
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        Shape[] shapes = [new CircleShape { Radius = 3 }, new RectangleShape { Size = new(6, 6) },
            new CapsuleShape { Radius = 3, Height = 8 }, new SegmentShape { A = new(0, -3), B = new(0, 3) },
            new ConvexPolygonShape { Points = [new(-3, -3), new(3, -3), new(3, 3), new(-3, 3)] },
            new ConcavePolygonShape { Segments = [new(-3, -3), new(-3, 3), new(0, -3), new(0, 3)] },
            new SeparationRayShape { Length = 6, SlideOnSlope = true }, new WorldBoundaryShape { Normal = Vector2.Right }];
        using var boundary = new WorldBoundaryShape { Normal = Vector2.Left }; using var gap = new CircleShape { Radius = .1f };
        var source = new RID[8]; var target = new RID[8]; var scene = new StaticBody[8];
        using var query = new PhysicsTestMotionParameters { CollideSeparationRay = true }; using var result = new PhysicsTestMotionResult(); using var sceneResult = new KinematicCollision();
        try
        {
            for (var i = 0; i < 8; i++)
            {
                source[i] = PhysicsServer.BodyCreate(); target[i] = PhysicsServer.BodyCreate();
                foreach (var id in new[] { source[i], target[i] })
                { PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(id, gap.GetRID(), disabled: true); }
                var local = SourceLocal(i);
                PhysicsServer.BodyAddShape(source[i], shapes[i].GetRID(), local); PhysicsServer.BodySetCollisionLayer(source[i], 0);
                PhysicsServer.BodySetTransform(source[i], new(0, new(3000 + i * 100, 3000))); PhysicsServer.BodySetSpace(source[i], world.Space);
                PhysicsServer.BodyAddShape(target[i], (i == 7 ? boundary : shapes[i]).GetRID(), TargetLocal(i));
                PhysicsServer.BodySetCollisionLayer(target[i], 1u << i); PhysicsServer.BodySetCollisionMask(target[i], 0); PhysicsServer.BodySetSpace(target[i], world.Space);
                scene[i] = new StaticBody { Position = new(-3000 - i * 100, 3000), CollisionLayer = 0 };
                scene[i].AddChild(new CollisionShape { Shape = gap, Disabled = true }); scene[i].AddChild(new CollisionShape { Shape = shapes[i], Transform = local }); root.AddChild(scene[i]);
            }
            var mover = new RigidBody { Position = new(100, 100), GravityScale = 0, CanSleep = false, LinearVelocity = Vector2.Right, CollisionLayer = 0, CollisionMask = 0, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
            mover.AddChild(new CollisionShape { Shape = gap }); root.AddChild(mover);
            float[] right = [3, 3, 3, 0, 3, 0, 6, 0]; float[] left = [3, 3, 3, 0, 3, 3, 6, 0];
            var failures = new List<string>();
            for (var a = 0; a < 8; a++) for (var b = 0; b < 8; b++)
                    for (var variant = 0; variant < 2; variant++) foreach (var margin in new[] { 0f, .75f })
                        { Exercise(a, b, variant, margin, false, true); Exercise(a, b, variant, margin, true, true); }
            foreach (var group in failures.GroupBy(x => x.Split(", route=")[0])) Console.WriteLine($"Failure group {group.Key}: {group.Count()} cases; {group.First()}");
            Check(failures.Count == 0, "Motion matrix failures: " + string.Join("; ", failures.Take(12)));
            RecoveryAndReplacement();
            for (var i = 0; i < 128; i++) Frame(i); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame(i);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Motion matrix allocated {own}/{all} B");
            Console.WriteLine($"Motion matrix {backend?.ToString() ?? "registered"}, separate={threaded}: 64 ordered families, raw/scene/TestMove, margin/covariance/brackets, 128 complete moving frames {own}/{all} B.");
            void RecoveryAndReplacement()
            {
                foreach (var family in new[] { 5, 7 })
                {
                    PhysicsServer.BodySetTransform(target[0], new(0, new(18, 0)));
                    PhysicsServer.BodySetCollisionMask(source[family], 1); scene[family].CollisionMask = 1;
                    query.From = new(0, new(family == 5 ? 18 : 19, 0)); query.Margin = 0; query.Motion = Vector2.Zero;
                    query.RecoveryAsCollision = true;
                    Check(PhysicsServer.BodyTestMotion(source[family], query, result) && result.GetColliderRID() == target[0] && result.GetCollisionLocalShape() == 1 && result.GetColliderShape() == 1 && result.GetTravel().X < -1,
                        "Ignored source displacement still permits ordinary initial recovery");
                    var recovery = result.GetTravel(); query.RecoveryAsCollision = false;
                    Check(!PhysicsServer.BodyTestMotion(source[family], query, result) && result.GetColliderRID() == default && result.GetTravel().DistanceTo(recovery) < .001f,
                        "Recovery-only misses retain recovery displacement and clear contact identity");
                    query.Motion = new(-30, 0);
                    Check(PhysicsServer.BodyTestMotion(source[family], query, result) && result.GetCollisionSafeFraction() == 0 && result.GetRemainder() == query.Motion,
                        "A deep residual overlap still stops the requested interval for an ignored source displacement");
                    Check(scene[family].TestMove(query.From, query.Motion, sceneResult, 0) && sceneResult.GetRemainder() == query.Motion,
                        "Scene residual-overlap stopping retains the same special source semantics");
                }
                PhysicsServer.BodySetTransform(target[5], new(0, new(18, 0))); PhysicsServer.BodySetCollisionMask(source[5], 1u << 5); scene[5].CollisionMask = 1u << 5;
                query.From = new(0, new(19, 0)); query.Motion = Vector2.Zero; query.Margin = .75f; query.RecoveryAsCollision = true;
                Check(!PhysicsServer.BodyTestMotion(source[5], query, result) && result.GetTravel() == Vector2.Zero && result.GetColliderRID() == default && !scene[5].TestMove(query.From, Vector2.Zero, sceneResult, .75f, true),
                    "Two concave resources cannot manufacture recovery contacts from their private edge pieces");
                var node = (CollisionShape)scene[5].GetChild(1);
                PhysicsServer.BodySetShape(source[5], 1, shapes[0].GetRID()); node.Shape = shapes[0];
                query.From = new(0, new(-1, 0)); query.Motion = new(30, 0); query.Margin = 0; query.RecoveryAsCollision = false;
                Check(PhysicsServer.BodyTestMotion(source[5], query, result) && result.GetColliderRID() == target[5] && scene[5].TestMove(query.From, query.Motion, sceneResult, 0),
                    "Live concave-to-circle source replacement updates represented kind before the next query");
                PhysicsServer.BodySetShape(source[5], 1, shapes[5].GetRID()); node.Shape = shapes[5];
                Check(!PhysicsServer.BodyTestMotion(source[5], query, result) && !scene[5].TestMove(query.From, query.Motion, sceneResult, 0), "Restored concave sources retain the current ignored displacement policy");
                PhysicsServer.BodySetShape(target[5], 1, boundary.GetRID());
                Check(PhysicsServer.BodyTestMotion(source[5], query, result) && result.GetColliderRID() == target[5] && scene[5].TestMove(query.From, query.Motion, sceneResult, 0),
                    "Live target replacement preserves finite concave displacement against a world boundary");
                PhysicsServer.BodySetShape(target[5], 1, shapes[5].GetRID());
                Check(!PhysicsServer.BodyTestMotion(source[5], query, result), "Restoring a concave target restores two-concave exclusion");
            }
            void Frame(int frame)
            {
                var index = frame % 64; Exercise(index / 8, index % 8, frame / 64, .75f, false, false);
                var before = mover.Position.X; tree.PhysicsFrame(.01);
                Check(MathF.Abs(mover.Position.X - before - .01f) < .0001f, "The motion matrix advances actual selected numerical frames");
            }
            void Exercise(int a, int b, int variant, float margin, bool miss, bool audit)
            {
                var outer = variant == 0 ? Transform.Identity : new Transform(.37f, new(70, -30));
                var targetPose = outer * new Transform(0, new(18, 0)); PhysicsServer.BodySetTransform(target[b], targetPose);
                PhysicsServer.BodySetCollisionMask(source[a], 1u << b); scene[a].CollisionMask = 1u << b;
                query.From = outer * new Transform(0, new(-1, 0)); query.Motion = outer.BasisXform(new(miss ? -30 : 30, 0)); query.Margin = margin; query.RecoveryAsCollision = false;
                var expected = !miss && a != 7 && b != 6 && (a != 5 || b == 7);
                var fraction = (20 - right[a] - left[b]) / 30;
                for (var route = 0; route < 3; route++)
                {
                    var hit = route == 2 ? scene[a].TestMove(query.From, query.Motion, sceneResult, margin) : PhysicsServer.BodyTestMotion(route == 0 ? source[a] : scene[a].GetRID(), query, result);
                    var travel = route == 2 ? sceneResult.GetTravel() : result.GetTravel(); var remainder = route == 2 ? sceneResult.GetRemainder() : result.GetRemainder();
                    var valid = hit == expected && (travel + remainder).DistanceTo(query.Motion) < .001f;
                    if (hit)
                    {
                        var id = route == 2 ? sceneResult.GetColliderRID() : result.GetColliderRID();
                        var slot = route == 2 ? sceneResult.GetColliderShapeIndex() : result.GetColliderShape();
                        var point = route == 2 ? sceneResult.GetPosition() : result.GetCollisionPoint();
                        var normal = route == 2 ? sceneResult.GetNormal() : result.GetCollisionNormal();
                        var velocity = route == 2 ? sceneResult.GetColliderVelocity() : result.GetColliderVelocity();
                        valid &= MathF.Abs(normal.Length() - 1) < .001f && normal.Dot(outer.BasisXform(Vector2.Left)) > .99f && velocity == Vector2.Zero;
                        valid &= id == target[b] && slot == 1 && (route == 2 || result.GetCollisionLocalShape() == 1) && MotionMatrixBoundary(b, targetPose * TargetLocal(b), point);
                        var distance = outer.BasisXform(Vector2.Right).Dot(travel);
                        valid &= MathF.Abs(distance - 30 * fraction) < .13f;
                        if (route != 2)
                        {
                            var low = result.GetCollisionSafeFraction(); var high = result.GetCollisionUnsafeFraction();
                            valid &= low >= 0 && low <= high && high <= 1 && high - low <= 3f / 512 + .000002f && low <= fraction + .0002f && high >= fraction - .0002f;
                        }
                    }
                    else valid &= travel.DistanceTo(query.Motion) < .001f && remainder == Vector2.Zero && (route == 2 ? sceneResult.GetColliderRID() : result.GetColliderRID()) == default;
                    if (valid) continue;
                    var message = $"{a}/{b}, route={route}, variant={variant}, margin={margin}, miss={miss}: expected={expected}, hit={hit}, travel={travel}, point={(route == 2 ? sceneResult.GetPosition() : result.GetCollisionPoint())}, normal={(route == 2 ? sceneResult.GetNormal() : result.GetCollisionNormal())}, velocity={(route == 2 ? sceneResult.GetColliderVelocity() : result.GetColliderVelocity())}, low={result.GetCollisionSafeFraction()}, high={result.GetCollisionUnsafeFraction()}, expectedFraction={fraction}";
                    if (!audit) throw new InvalidOperationException(message);
                    failures.Add(message); if (failures.Count <= 16) Console.WriteLine(message);
                }
            }
        }
        finally
        {
            foreach (var id in source) if (id.IsValid()) PhysicsServer.FreeRID(id);
            foreach (var id in target) if (id.IsValid()) PhysicsServer.FreeRID(id);
            foreach (var body in scene) body?.Dispose(); foreach (var shape in shapes) shape.Dispose();
        }
        static Transform SourceLocal(int family) => new(family == 6 ? -Mathf.Pi / 2 : 0, new(1, 0));
        static Transform TargetLocal(int family) => new(family == 6 ? Mathf.Pi / 2 : 0, new(2, 0));
    }
    private static bool MotionMatrixBoundary(int family, Transform pose, Vector2 point)
    {
        var p = (point - pose.Origin).Rotated(-pose.Rotation);
        if (!p.IsFinite()) return false;
        return family switch
        {
            0 => MathF.Abs(p.Length() - 3) < .01f,
            1 or 4 => MathF.Min(MathF.Abs(MathF.Abs(p.X) - 3), MathF.Abs(MathF.Abs(p.Y) - 3)) < .01f && MathF.Max(MathF.Abs(p.X), MathF.Abs(p.Y)) < 3.01f,
            2 => MathF.Abs(p.DistanceTo(new(0, Math.Clamp(p.Y, -1, 1))) - 3) < .01f,
            3 => MathF.Abs(p.X) < .01f && MathF.Abs(p.Y) < 3.01f,
            5 => MathF.Min(MathF.Abs(p.X), MathF.Abs(p.X + 3)) < .01f && MathF.Abs(p.Y) < 3.01f,
            6 => MathF.Abs(p.X) < .01f && p.Y is > -.01f and < 6.01f,
            _ => MathF.Abs(p.X) < .01f
        };
    }
}
