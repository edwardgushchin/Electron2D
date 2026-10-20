using Electron2D;

public static partial class BackendChecks
{
    public static void RunMotionRest(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { MotionRestCore(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void MotionRestCore(PhysicsServer.Backend? backend, bool threaded)
    {
        var owner = Thread.CurrentThread;
        if (backend is null) PhysicsServerManager.RegisterServer("MotionRest", () => new Solver
        { StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "Motion rest retains its actual numerical lane") });
        using var world = backend is { } selected ? new World(selected) : new World("MotionRest");
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
            "Motion rest uses the selected implementation without fallback");
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        using var circle = new CircleShape { Radius = 2 };
        using var boundary = new WorldBoundaryShape { Normal = Vector2.Left };
        using var mover = new CharacterBody(); mover.AddChild(new CollisionShape { Shape = circle }); root.AddChild(mover);
        var target = PhysicsServer.BodyCreate(); using var query = new PhysicsTestMotionParameters(); using var result = new PhysicsTestMotionResult();
        var failures = new List<string>();
        try
        {
            PhysicsServer.BodySetMode(target, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(target, boundary.GetRID());
            PhysicsServer.BodySetTransform(target, new(0, new Vector2(10, 0))); PhysicsServer.BodySetSpace(target, world.Space);
            tree.PhysicsFrame(.01);
            query.From = Transform.Identity; query.Motion = new(20, 0); query.Margin = 0;
            var hit = PhysicsServer.BodyTestMotion(mover.GetRID(), query, result);
            var expected = MathF.Max(0, 2 + 20 * result.GetCollisionUnsafeFraction() - 10 + .0001f);
            var depth = result.GetCollisionDepth();
            Console.WriteLine($"sweep: hit={hit}, unsafe={result.GetCollisionUnsafeFraction()}, depth={depth}, expected={expected}");
            if (!hit || MathF.Abs(depth - expected) > .003f) failures.Add("sweep depth must sample the unsafe pose and query margin");
            query.From = new(0, new Vector2(9, 0)); query.Motion = Vector2.Zero; query.Margin = .75f; query.RecoveryAsCollision = true;
            hit = PhysicsServer.BodyTestMotion(mover.GetRID(), query, result); depth = result.GetCollisionDepth();
            expected = MathF.Max(0, 2 + 9 + result.GetTravel().X - 10 + .75f);
            Console.WriteLine($"recovery: hit={hit}, travel={result.GetTravel()}, depth={depth}, expected={expected}");
            if (!hit || MathF.Abs(depth - expected) > .003f) failures.Add("recovery depth must sample after the completed correction");
            query.Motion = new(-20, 0); hit = PhysicsServer.BodyTestMotion(mover.GetRID(), query, result);
            Console.WriteLine($"departure: hit={hit}, collider={result.GetColliderRID()}, travel={result.GetTravel()}, remainder={result.GetRemainder()}");
            if (hit || result.GetColliderRID().IsValid() || result.GetTravel().X >= -20 || result.GetRemainder() != Vector2.Zero)
                failures.Add("departed recovery must clear the obsolete contact and retain full travel");
            Check(failures.Count == 0, "Motion rest failures: " + string.Join("; ", failures));
            MotionRestFamilies(root, tree, world, backend, threaded);
        }
        finally { PhysicsServer.FreeRID(target); }
    }
    private static void MotionRestFamilies(SubViewport root, SceneTree tree, World world, PhysicsServer.Backend? backend, bool threaded)
    {
        Shape[] shapes = [new CircleShape { Radius = 2 }, new RectangleShape { Size = new(4, 4) },
            new CapsuleShape { Radius = 2, Height = 8 }, new SegmentShape { A = new(0, -3), B = new(0, 3) },
            new ConvexPolygonShape { Points = [new(-2, -2), new(2, 0), new(-2, 2)] },
            new ConcavePolygonShape { Segments = [new(2, -3), new(2, 3)] },
            new SeparationRayShape { Length = 4, SlideOnSlope = true }, new WorldBoundaryShape { Normal = Vector2.Right }];
        using var planeX = new WorldBoundaryShape { Normal = Vector2.Left };
        using var planeY = new WorldBoundaryShape { Normal = Vector2.Up };
        using var peerCircle = new CircleShape { Radius = 2 }; using var gap = new CircleShape { Radius = .1f };
        var raw = new RID[8]; var scene = new StaticBody[8]; var parked = new Transform[8];
        var peers = new RID[6]; var nodes = new StaticBody[3]; var tags = new Node[3];
        using var query = new PhysicsTestMotionParameters { CollideSeparationRay = true, RecoveryAsCollision = true };
        using var result = new PhysicsTestMotionResult(); using var sceneResult = new KinematicCollision();
        using var ticker = new RigidBody
        {
            Position = new(500, 500),
            GravityScale = 0,
            CanSleep = false,
            CollisionLayer = 0,
            CollisionMask = 0,
            LinearVelocity = Vector2.Right,
            LinearDampMode = RigidBody.DampMode.Replace,
            LinearDamp = 0
        };
        ticker.AddChild(new CollisionShape { Shape = gap }); root.AddChild(ticker);
        float[] right = [2, 2, 2, 0, 2, 2, 4, 0];
        try
        {
            for (var i = 0; i < 8; i++)
            {
                parked[i] = new(0, new(1000 + i * 100, 1000));
                var local = new Transform(i == 6 ? -Mathf.Pi / 2 : 0, new(1, .5f));
                raw[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(raw[i], PhysicsServer.BodyMode.Static);
                PhysicsServer.BodyAddShape(raw[i], gap.GetRID(), disabled: true); PhysicsServer.BodyAddShape(raw[i], shapes[i].GetRID(), local);
                PhysicsServer.BodySetCollisionLayer(raw[i], 0); PhysicsServer.BodySetTransform(raw[i], parked[i]); PhysicsServer.BodySetSpace(raw[i], world.Space);
                scene[i] = new StaticBody { Transform = parked[i], CollisionLayer = 0 };
                scene[i].AddChild(new CollisionShape { Shape = gap, Disabled = true }); scene[i].AddChild(new CollisionShape { Shape = shapes[i], Transform = local }); root.AddChild(scene[i]);
            }
            Shape[] peerShapes = [planeX, peerCircle, planeY];
            for (var i = 0; i < 3; i++)
            {
                tags[i] = new Node(); root.AddChild(tags[i]);
                peers[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(peers[i], PhysicsServer.BodyMode.Static);
                PhysicsServer.BodyAddShape(peers[i], gap.GetRID(), disabled: true); PhysicsServer.BodyAddShape(peers[i], peerShapes[i].GetRID());
                PhysicsServer.BodyAttachObject(peers[i], tags[i]); PhysicsServer.BodySetCollisionMask(peers[i], 0); PhysicsServer.BodySetSpace(peers[i], world.Space);
                nodes[i] = new StaticBody { CollisionMask = 0 };
                nodes[i].AddChild(new CollisionShape { Shape = gap, Disabled = true }); nodes[i].AddChild(new CollisionShape { Shape = peerShapes[i] }); root.AddChild(nodes[i]);
                peers[i + 3] = nodes[i].GetRID();
            }
            tree.PhysicsFrame(.01);
            for (var configuration = 0; configuration < 64; configuration++) Exercise(configuration, true);
            for (var frame = 0; frame < 128; frame++) Frame(frame); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var frame = 0; frame < 128; frame++) Frame(frame);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Motion rest complete query/frame brackets allocated {own}/{all} B");
            Console.WriteLine($"Motion rest {backend?.ToString() ?? "registered"}, separate={threaded}: eight families, unsafe/recovered/departed poses, scene/raw metadata, surface velocity, deepest final contact, applied movement; 128 full frames {own}/{all} B.");
            void Frame(int frame)
            {
                Exercise(frame % 64, false);
                var x = ticker.Position.X; tree.PhysicsFrame(.01);
                Check(MathF.Abs(ticker.Position.X - x - .01f) < .0001f, "Motion rest frames execute the selected numerical step");
            }
            void Exercise(int configuration, bool apply)
            {
                var family = configuration % 8; var angle = configuration / 8 % 2 == 0 ? 0 : .37f;
                var role = configuration / 16 % 2; var margin = configuration / 32 == 0 ? 0 : .75f;
                var outer = new Transform(angle, angle == 0 ? Vector2.Zero : new(70, -30));
                var axis = outer.BasisXform(Vector2.Right); var up = outer.BasisXform(Vector2.Down);
                var id = peers[role * 3 + (family == 7 ? 1 : 0)];
                for (var i = 0; i < 6; i++)
                {
                    PhysicsServer.BodySetCollisionLayer(peers[i], peers[i] == id ? 2u : 0u);
                    PhysicsServer.BodySetTransform(peers[i], outer * new Transform(0, new(10, 0)));
                }
                var linear = outer.BasisXform(new(4, -2)); PhysicsServer.BodySetLinearVelocity(id, linear); PhysicsServer.BodySetAngularVelocity(id, 2.5f);
                PhysicsServer.BodySetCollisionMask(raw[family], 2); scene[family].CollisionMask = 2;
                query.Margin = margin;
                for (var phase = 0; phase < 3; phase++)
                {
                    // Keep the ray entry between refinement samples, including a nearby separated probe.
                    var fromX = phase == 0 ? family == 6 ? -4 : 0 : family == 7 ? 8 : 10 - right[family];
                    query.From = outer * new Transform(0, new(fromX, 0));
                    query.Motion = axis * (phase == 0 ? 20 : phase == 1 ? 0 : -20);
                    query.Margin = phase == 2 ? .75f : margin;
                    for (var route = 0; route < 3; route++)
                    {
                        var hit = route == 2 ? scene[family].TestMove(query.From, query.Motion, sceneResult, query.Margin, true) : PhysicsServer.BodyTestMotion(route == 0 ? raw[family] : scene[family].GetRID(), query, result);
                        var travel = route == 2 ? sceneResult.GetTravel() : result.GetTravel(); var remainder = route == 2 ? sceneResult.GetRemainder() : result.GetRemainder();
                        var collider = route == 2 ? sceneResult.GetColliderRID() : result.GetColliderRID(); var depth = route == 2 ? sceneResult.GetDepth() : result.GetCollisionDepth();
                        var expectedHit = phase != 2 && (phase != 0 || family != 7);
                        var valid = hit == expectedHit && PhysicsServer.BodyGetTransform(raw[family]) == parked[family] && scene[family].GlobalTransform == parked[family];
                        if (hit)
                        {
                            var point = route == 2 ? sceneResult.GetPosition() : result.GetCollisionPoint(); var normal = route == 2 ? sceneResult.GetNormal() : result.GetCollisionNormal();
                            var velocity = route == 2 ? sceneResult.GetColliderVelocity() : result.GetColliderVelocity();
                            var slot = route == 2 ? sceneResult.GetColliderShapeIndex() : result.GetColliderShape();
                            var objectID = route == 2 ? sceneResult.GetColliderID() : result.GetColliderID();
                            var expectedID = role == 0 ? tags[family == 7 ? 1 : 0].InstanceID : nodes[family == 7 ? 1 : 0].InstanceID;
                            var relative = point - PhysicsServer.BodyGetTransform(id).Origin;
                            var expectedVelocity = linear + new Vector2(-relative.Y, relative.X) * 2.5f;
                            var effectiveMargin = MathF.Max(margin, .0001f);
                            var expectedDepth = phase != 0 ? 1 + effectiveMargin + travel.Dot(axis) : effectiveMargin;
                            if (phase == 0 && route != 2) expectedDepth += right[family] + 1 + fromX + 20 * result.GetCollisionUnsafeFraction() - 10;
                            valid &= collider == id && slot == 1 && objectID == expectedID && (route == 2 || result.GetCollisionLocalShape() == 1);
                            valid &= MathF.Abs(normal.Length() - 1) < .001f && normal.Dot(-axis) > .999f && velocity.DistanceTo(expectedVelocity) < .01f;
                            valid &= family == 7 ? MathF.Abs(relative.Length() - 2) < .003f : MathF.Abs(relative.Dot(axis)) < .003f;
                            valid &= phase == 0 && route == 2 ? depth >= expectedDepth - .003f && depth <= expectedDepth + 20f / 256 + .003f : MathF.Abs(depth - expectedDepth) < .003f;
                            valid &= phase != 0 ? travel.Dot(axis) < -.01f && remainder.DistanceTo(query.Motion) < .003f : (travel + remainder).DistanceTo(query.Motion) < .003f;
                        }
                        else valid &= collider == default && depth == 0 && remainder == Vector2.Zero && (phase == 2 ? travel.Dot(axis) < -20 : travel.DistanceTo(query.Motion) < .003f);
                        if (!valid) throw new InvalidOperationException($"Motion rest family={family}, phase={phase}, route={route}, role={role}, angle={angle}, margin={margin}: hit={hit}, collider={collider}, depth={depth}, travel={travel}, remainder={remainder}");
                    }
                    if (apply)
                    {
                        var expectedTravel = sceneResult.GetTravel(); scene[family].GlobalTransform = query.From;
                        using var collision = scene[family].MoveAndCollide(query.Motion, safeMargin: query.Margin, recoveryAsCollision: true);
                        Check((collision is not null) == (phase != 2 && (phase != 0 || family != 7)) && scene[family].Position.DistanceTo(query.From.Origin + expectedTravel) < .003f,
                            "Applied movement consumes the final sampled contact and complete safe/recovery travel");
                        scene[family].Transform = parked[family];
                    }
                }
                // Different priority decay rates switch the deepest collider on the last recovery pass.
                var xPeer = peers[role * 3]; var yPeer = peers[role * 3 + 2];
                for (var i = 0; i < 6; i++) PhysicsServer.BodySetCollisionLayer(peers[i], peers[i] == xPeer || peers[i] == yPeer ? 4u : 0u);
                PhysicsServer.BodySetCollisionPriority(xPeer, 3); PhysicsServer.BodySetCollisionPriority(yPeer, 2);
                PhysicsServer.BodySetTransform(xPeer, outer * new Transform(0, new(10, 0))); PhysicsServer.BodySetTransform(yPeer, outer * new Transform(0, new(0, 10)));
                PhysicsServer.BodySetCollisionMask(raw[0], 4); query.From = outer * new Transform(0, new(9.19375f, 7.95f)); query.Motion = Vector2.Zero; query.Margin = .75f;
                Check(PhysicsServer.BodyTestMotion(raw[0], query, result), "Coupled recovery retains a final contact");
                var dx = 2.94375f + result.GetTravel().Dot(axis); var dy = 1.2f + result.GetTravel().Dot(up);
                Check(dy > dx + .02f && result.GetColliderRID() == yPeer && MathF.Abs(result.GetCollisionDepth() - dy) < .003f && result.GetCollisionNormal().Dot(-up) > .999f,
                    "Final recovery resamples every candidate and selects the actual deepest contact after correction");
                PhysicsServer.BodySetCollisionPriority(xPeer, 1); PhysicsServer.BodySetCollisionPriority(yPeer, 1);
            }
        }
        finally
        {
            foreach (var id in raw) if (id.IsValid()) PhysicsServer.FreeRID(id);
            for (var i = 0; i < 3; i++) if (peers[i].IsValid()) PhysicsServer.FreeRID(peers[i]);
            foreach (var body in scene) body?.Dispose(); foreach (var node in nodes) node?.Dispose(); foreach (var tag in tags) tag?.Dispose();
            foreach (var shape in shapes) shape.Dispose();
        }
    }

}
