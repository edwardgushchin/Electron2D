using Electron2D;

public static partial class BackendChecks
{
    public static void RunSpecialSpaceMotion(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var prior = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { SpecialSpaceMotionCore(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, prior); }
    }
    private static void SpecialSpaceMotionCore(PhysicsServer.Backend? backend, bool threaded)
    {
        var owner = Thread.CurrentThread;
        if (backend is null) PhysicsServerManager.RegisterServer("SpecialSpaceMotion", () => new Solver
        { StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "Special queries preserve the actual numerical lane") });
        using var world = backend is { } selected ? new World(selected) : new World("SpecialSpaceMotion");
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null, "Special queries select their actual backend without fallback");
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        Shape[] shapes = [new CircleShape { Radius = 3 }, new RectangleShape { Size = new(6, 6) },
            new CapsuleShape { Radius = 3, Height = 8 }, new SegmentShape { A = new(0, -3), B = new(0, 3) },
            new ConvexPolygonShape { Points = [new(-3, -3), new(3, -3), new(3, 3), new(-3, 3)] },
            new ConcavePolygonShape { Segments = [new(-3, -3), new(-3, 3), new(0, -3), new(0, 3)] },
            new SeparationRayShape { Length = 6, SlideOnSlope = true }, new WorldBoundaryShape { Normal = Vector2.Right }];
        using var targetRay = new SeparationRayShape { Length = 10, SlideOnSlope = true };
        using var boundary = new WorldBoundaryShape { Normal = Vector2.Left }; using var gap = new CircleShape { Radius = .1f };
        var sources = new RID[8]; var raw = new RID[8]; var scene = new StaticBody[8]; var hits = new PhysicsShapeResult[2]; var points = new Vector2[16];
        using var query = new PhysicsShapeQueryParameters { Margin = 0 };
        using var bodyQuery = new PhysicsTestMotionParameters { CollideSeparationRay = true }; using var bodyResult = new PhysicsTestMotionResult();
        var view = world.DirectSpaceState; var failures = new List<string>();
        float[] right = [3, 3, 3, 0, 3, 0, 6, 0]; float[] left = [3, 3, 3, 0, 3, 3, 10, 0];
        try
        {
            for (var b = 0; b < 8; b++)
            {
                sources[b] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(sources[b], PhysicsServer.BodyMode.Static);
                PhysicsServer.BodyAddShape(sources[b], gap.GetRID(), disabled: true); PhysicsServer.BodyAddShape(sources[b], shapes[b].GetRID());
                PhysicsServer.BodySetCollisionLayer(sources[b], 0); PhysicsServer.BodySetTransform(sources[b], new(0, new(4000 + b * 100, 4000))); PhysicsServer.BodySetSpace(sources[b], world.Space);
                var shape = b == 7 ? boundary : b == 6 ? targetRay : shapes[b]; var local = new Transform(b == 6 ? Mathf.Pi / 2 : 0, new(2, 0));
                raw[b] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(raw[b], PhysicsServer.BodyMode.Static);
                PhysicsServer.BodyAddShape(raw[b], gap.GetRID(), disabled: true); PhysicsServer.BodyAddShape(raw[b], shape.GetRID(), local);
                PhysicsServer.BodySetCollisionLayer(raw[b], 1u << b); PhysicsServer.BodySetCollisionMask(raw[b], 0); PhysicsServer.BodySetSpace(raw[b], world.Space);
                scene[b] = new StaticBody { CollisionLayer = 1u << (b + 8), CollisionMask = 0 };
                scene[b].AddChild(new CollisionShape { Shape = gap, Disabled = true }); scene[b].AddChild(new CollisionShape { Shape = shape, Transform = local }); root.AddChild(scene[b]);
            }
            var mover = new RigidBody { Position = new(100, 100), GravityScale = 0, CanSleep = false, LinearVelocity = Vector2.Right, CollisionLayer = 0, CollisionMask = 0, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
            mover.AddChild(new CollisionShape { Shape = gap }); root.AddChild(mover);
            for (var a = 0; a < 8; a++) for (var b = 0; b < 8; b++)
                    for (var variant = 0; variant < 2; variant++) for (var route = 0; route < 2; route++)
                            foreach (var margin in new[] { 0f, .75f })
                            { Probe(a, b, variant, route, margin, false, false, true); Probe(a, b, variant, route, margin, true, false, true); }
            for (var a = 5; a < 8; a += 2) for (var b = 0; b < 8; b++)
                    for (var route = 0; route < 2; route++) Probe(a, b, 1, route, .75f, false, true, true);
            EndpointAndCollinear();
            foreach (var group in failures.GroupBy(x => x.Split(", variant=")[0])) Console.WriteLine($"Special query group {group.Key}: {group.Count()} cases; {group.First()}");
            Check(failures.Count == 0, "Special space motion failures: " + string.Join("; ", failures.Take(12)));
            for (var frame = 0; frame < 128; frame++) Frame(frame); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var frame = 0; frame < 128; frame++) Frame(frame);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Special space motion allocated {own}/{all} B");
            Console.WriteLine($"Special space motion {backend?.ToString() ?? "registered"}, separate={threaded}: 64 families, four operations, scene/server, 128 moving frames {own}/{all} B.");
            void EndpointAndCollinear()
            {
                for (var variant = 0; variant < 2; variant++) for (var route = 0; route < 2; route++)
                    {
                        var outer = variant == 0 ? Transform.Identity : new Transform(.37f, new(70, -30));
                        var targetPose = outer * new Transform(0, new(12, 0));
                        PhysicsServer.BodySetTransform(raw[7], targetPose); scene[7].Transform = targetPose;
                        query.CollisionMask = 1u << (7 + route * 8); query.Margin = .75f;
                        for (var family = 0; family < 7; family++)
                        {
                            query.Shape = shapes[family]; query.Transform = outer * new Transform(family == 6 ? -Mathf.Pi / 2 : 0, new(16, 0)); query.Motion = Vector2.Zero;
                            Check(view.IntersectShape(query, hits) == 1 && view.CollideShape(query, points) > 0 && view.GetRestInfo(query) is not null && view.CastMotion(query) == (1, 1), "Finite initial boundary contacts remain visible and do not create a new cast");
                            query.Motion = outer.BasisXform(new(-20, 0));
                            Check(view.IntersectShape(query, hits) == 0 && view.CollideShape(query, points) == 0 && view.GetRestInfo(query) is null && view.CastMotion(query) == (1, 1), "Boundary endpoint departure cannot retain the earlier overlap/contact/rest");
                        }
                        foreach (var target in new[] { 0, 2, 3 })
                        {
                            PhysicsServer.BodySetTransform(raw[target], targetPose); scene[target].Transform = targetPose;
                            query.CollisionMask = 1u << (target + route * 8); query.Shape = shapes[0]; query.Transform = outer * new Transform(0, new(14, 0)); query.Motion = Vector2.Zero;
                            var pairs = view.CollideShape(query, points); var rest = view.GetRestInfo(query);
                            if (pairs == 0 || rest is not { } info) throw new InvalidOperationException("A centered circle retains its capsule/core contact");
                            var local = outer.AffineInverse() * info.Point; var radius = target == 2 ? 3 : 0;
                            var onSurface = target == 0 ? MathF.Abs(info.Point.DistanceTo(outer * new Vector2(14, 0)) - 3) < .02f && info.Normal.Dot((info.Point - outer * new Vector2(14, 0)).Normalized()) > .999f : MathF.Abs(info.Normal.Dot(outer.BasisXform(Vector2.Right))) > .999f && MathF.Abs(MathF.Abs(local.X - 14) - radius) < .02f && MathF.Abs(local.Y) < .02f;
                            Check(MathF.Abs(info.Normal.Length() - 1) < .001f && onSurface,
                                "A centered circle has a unit contact side and corresponding counterpart surface point");
                            PhysicsServer.BodySetCollisionMask(sources[0], query.CollisionMask); bodyQuery.From = query.Transform; bodyQuery.Motion = Vector2.Zero; bodyQuery.Margin = .75f; bodyQuery.RecoveryAsCollision = true;
                            Check(PhysicsServer.BodyTestMotion(sources[0], bodyQuery, bodyResult) && MathF.Abs(bodyResult.GetCollisionNormal().Length() - 1) < .001f,
                                "Centered circle body recovery retains a unit contact side");
                            bodyQuery.RecoveryAsCollision = false;
                        }
                    }
            }
            void Frame(int frame)
            {
                var index = frame % 64; Probe(index / 8, index % 8, frame / 64, frame / 64, .75f, false, false, false);
                var before = mover.Position.X; tree.PhysicsFrame(.01);
                Check(MathF.Abs(mover.Position.X - before - .01f) < .0001f, "Special queries execute actual complete moving frames");
            }
            void Probe(int a, int b, int variant, int targetRoute, float margin, bool reverse, bool initial, bool audit)
            {
                var outer = variant == 0 ? Transform.Identity : new Transform(.37f, new(70, -30));
                var targetX = b == 6 ? 27 : 14; var targetPose = outer * new Transform(0, new(targetX - 2, 0));
                PhysicsServer.BodySetTransform(raw[b], targetPose); scene[b].Transform = targetPose;
                query.Shape = shapes[a]; query.CollisionMask = 1u << (b + targetRoute * 8); query.Margin = margin;
                var sourceX = initial ? a == 7 ? b == 6 ? 29 : 16 : b == 6 ? 22 : b == 7 ? 16 : 14 : 0;
                query.Transform = outer * new Transform(a == 6 ? -Mathf.Pi / 2 : 0, new(sourceX, 0)); query.Motion = outer.BasisXform(new(reverse ? -20 : 20, 0));
                var expected = initial ? !(a == 7 && b == 7 || a == 5 && b == 5) : !reverse && a != 7 && b != 6 && (a != 5 || b == 7);
                var expectedRID = targetRoute == 0 ? raw[b] : scene[b].GetRID();
                var count = view.IntersectShape(query, hits); var cast = view.CastMotion(query); var pairs = view.CollideShape(query, points); var rest = view.GetRestInfo(query);
                var valid = (count > 0) == expected && (pairs > 0) == expected && (rest is not null) == expected;
                valid &= count == 0 || count == 1 && hits[0].ColliderRID == expectedRID && hits[0].ShapeIndex == 1;
                if (rest is { } info)
                    valid &= info.ColliderRID == expectedRID && info.ShapeIndex == 1 && info.Point.IsFinite() && info.Normal.IsFinite() && MathF.Abs(info.Normal.Length() - 1) < .001f && info.LinearVelocity == Vector2.Zero;
                if (rest is { } onCircle && b == 0) valid &= MathF.Abs(onCircle.Point.DistanceTo(outer * new Vector2(14, 0)) - 3) < .02f;
                for (var i = 0; i < pairs * 2; i++) valid &= points[i].IsFinite();
                if (!expected || initial) valid &= cast == (1, 1);
                else
                {
                    var fraction = (14 - right[a] - left[b] - margin) / 20;
                    valid &= cast.SafeFraction >= 0 && cast.SafeFraction <= cast.UnsafeFraction && cast.UnsafeFraction <= 1 && cast.UnsafeFraction - cast.SafeFraction <= 1f / 256 + .000002f && cast.SafeFraction <= fraction + .0002f && cast.UnsafeFraction >= fraction - .0002f;
                }
                var bodyHit = false;
                if (!initial)
                {
                    PhysicsServer.BodySetCollisionMask(sources[a], query.CollisionMask);
                    bodyQuery.From = query.Transform; bodyQuery.Motion = query.Motion; bodyQuery.Margin = margin;
                    bodyHit = PhysicsServer.BodyTestMotion(sources[a], bodyQuery, bodyResult);
                    valid &= bodyHit == expected && (bodyHit ? bodyResult.GetColliderRID() == expectedRID && bodyResult.GetColliderShape() == 1 && bodyResult.GetCollisionLocalShape() == 1 : bodyResult.GetColliderRID() == default);
                }
                if (audit)
                {
                    var arrayHits = view.IntersectShape(query); var arrayPairs = view.CollideShape(query);
                    valid &= arrayHits.Length == count && arrayPairs.Length == pairs * 2;
                }
                if (valid) return;
                var message = $"{a}/{b}, variant={variant}, target={targetRoute}, margin={margin}, reverse={reverse}, initial={initial}: expected={expected}, hits={count}, pairs={pairs}, rest={rest?.Point}/{rest?.Normal}, body={bodyHit}, cast={cast}";
                if (!audit) throw new InvalidOperationException(message); failures.Add(message);
            }
        }
        finally
        {
            foreach (var id in sources) if (id.IsValid()) PhysicsServer.FreeRID(id); foreach (var id in raw) if (id.IsValid()) PhysicsServer.FreeRID(id); foreach (var body in scene) body?.Dispose(); foreach (var shape in shapes) shape.Dispose();
        }
    }
}
