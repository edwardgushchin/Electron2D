using Electron2D;

public static partial class BackendChecks
{
    public static void RunPriorityGeometry(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("PrioritySolver", () => new Solver());
        try
        {
            Raw(false); Raw(true); Scene(); Finite(); OneWay(); Rigid();
            Console.WriteLine($"Priority geometry {backend?.ToString() ?? "registered"}, threaded={threaded}: weighted recovery, capacity, policy and zero warm frames passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        World Create() => backend is { } selected ? new World(selected) : new World("PrioritySolver");
        void Raw(bool rays)
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var circle = new CircleShape { Radius = 5 }; using var ray = new SeparationRayShape { Length = 5 };
            using var leftShape = new WorldBoundaryShape { Normal = Vector2.Right, Distance = -3 };
            using var bottomShape = new WorldBoundaryShape { Normal = Vector2.Up, Distance = -3 };
            var left = new StaticBody { Name = "Left", CollisionMask = 0 }; left.AddChild(new CollisionShape { Shape = leftShape }); root.AddChild(left);
            var bottom = new StaticBody { Name = "Bottom", CollisionMask = 0 }; bottom.AddChild(new CollisionShape { Shape = bottomShape }); root.AddChild(bottom);
            var sensor = new Area { CollisionPriority = float.MaxValue }; sensor.AddChild(new CollisionShape { Shape = circle }); root.AddChild(sensor);
            using var tree = new SceneTree(root); using var query = new PhysicsTestMotionParameters { RecoveryAsCollision = true };
            using var result = new PhysicsTestMotionResult(); var body = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Kinematic);
                PhysicsServer.BodyAddShape(body, circle.GetRID(), disabled: true);
                PhysicsServer.BodyAddShape(body, rays ? ray.GetRID() : circle.GetRID());
                if (rays) PhysicsServer.BodyAddShape(body, ray.GetRID(), new(MathF.PI / 2, Vector2.Zero));
                PhysicsServer.BodySetTransform(body, new(0, new Vector2(500, 500))); PhysicsServer.BodySetSpace(body, world.Space);
                foreach (var pair in new[] { (1f, 1f), (9f, 1f), (1f, 9f), (90f, 10f), (float.MaxValue, float.MaxValue),
                    (float.MaxValue, 1f), (5e-6f, 5e-6f), (1e-7f, 1e-7f), (float.Epsilon, float.Epsilon) })
                {
                    Set(pair.Item1, pair.Item2); Recover();
                    var total = (double)pair.Item1 + pair.Item2; var factor = total < .00001f ? 1 : 2 / total;
                    var excess = 2.076;
                    var expected = new Vector2((float)(excess * (1 - Math.Pow(1 - .4 * pair.Item1 * factor, 4))),
                        (float)(-excess * (1 - Math.Pow(1 - .4 * pair.Item2 * factor, 4))));
                    Near(result.GetTravel(), expected, pair.Item1 < 1e-6f ? 1e-7f : .002f, "Priority analytic recovery");
                }
                Task.Run(() => Reject<InvalidOperationException>(() => left.CollisionPriority = 2)).GetAwaiter().GetResult();
                Reject<ArgumentException>(() => PhysicsServer.BodyGetCollisionPriority(sensor.GetRID()));
                Set(1, 9); Recover(); var before = result.GetTravel();
                foreach (var invalid in new[] { 0f, -1, float.NaN, float.PositiveInfinity })
                    Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetCollisionPriority(left.GetRID(), invalid));
                Recover(); Near(result.GetTravel(), before, .0001f, "Invalid priority preserves recovery");
                query.RecoveryAsCollision = false; Check(!PhysicsServer.BodyTestMotion(body, query, result), "Recovery flag suppresses the report, retaining travel");
                Near(result.GetTravel(), before, .0001f, "Recovery travel survives report policy"); query.RecoveryAsCollision = true;
                query.ExcludeBodies = [left.GetRID()]; Recover(); Check(MathF.Abs(result.GetTravel().X) < .0001f && result.GetTravel().Y < -1.7f, "Body exclusions precede priority normalization");
                query.ExcludeBodies = []; query.ExcludeObjects = [bottom.InstanceID]; Recover(); Check(result.GetTravel().X > 1.7f && MathF.Abs(result.GetTravel().Y) < .0001f, "Object exclusions precede priority normalization");
                query.ExcludeObjects = [];
                PhysicsServer.BodyAddCollisionException(body, left.GetRID()); Recover(); Check(MathF.Abs(result.GetTravel().X) < .0001f, "Exceptions remove weighted planes");
                PhysicsServer.BodyRemoveCollisionException(body, left.GetRID());
                var frame = 0;
                Warm(() => { Set((frame++ & 1) == 0 ? 9 : 1, 1); tree.PhysicsFrame(.01); Recover(); }, rays ? "ray query/full frame" : "circle query/full frame");
                Near(PhysicsServer.BodyGetTransform(body).Origin, new(500, 500), 0, "Supplied-pose recovery never mutates the body");
                root.RemoveChild(left); left.CollisionPriority = 7; root.AddChild(left);
                Check(PhysicsServer.BodyGetCollisionPriority(left.GetRID()) == 7, "Detached edits survive reentry");
                PhysicsServer.BodySetSpace(body, default); PhysicsServer.BodySetCollisionPriority(body, 3); PhysicsServer.BodySetSpace(body, world.Space);
                Check(PhysicsServer.BodyGetCollisionPriority(body) == 3, "Server detach preserves priority");
                Set(1, 1); Recover();
                if (!rays) Capacity();
                void Capacity()
                {
                    leftShape.Distance = bottomShape.Distance = -10;
                    using var shallow = new WorldBoundaryShape { Normal = Vector2.Up, Distance = -4.5f };
                    using var deep = new WorldBoundaryShape { Normal = Vector2.Right, Distance = -3 };
                    var walls = new RID[41];
                    try
                    {
                        for (var i = 0; i < walls.Length; i++)
                        {
                            var rid = walls[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(rid, PhysicsServer.BodyMode.Static);
                            PhysicsServer.BodyAddShape(rid, (i == 40 ? deep : shallow).GetRID()); PhysicsServer.BodySetSpace(rid, world.Space);
                        }
                        Warm(() =>
                        {
                            tree.PhysicsFrame(.01); Recover(); Near(new(result.GetTravel().X, 0), new(1.8069504f, 0), .002f, "Deep plane survives the 32-point cap");
                            Check(result.GetTravel().Y is > -.577f and < -.57f, "Coincident shallow planes stay bounded");
                        }, "41 contacts/capacity");
                    }
                    finally { foreach (var rid in walls) if (rid.IsValid()) PhysicsServer.FreeRID(rid); }
                }
                void Set(float a, float b) { left.CollisionPriority = a; PhysicsServer.BodySetCollisionPriority(bottom.GetRID(), b); }
                void Recover()
                {
                    Check(PhysicsServer.BodyTestMotion(body, query, result), "Recovery reports a boundary contact");
                    Check(result.GetColliderRID() == left.GetRID() || result.GetColliderRID() == bottom.GetRID() || leftShape.Distance == -10,
                        "Recovery retains a real collider identity");
                    Check(result.GetCollisionLocalShape() is 1 or 2 && result.GetColliderShape() == 0, "Logical indices skip disabled slots");
                    Check(result.GetCollisionNormal().IsFinite() && result.GetCollisionDepth() > 0, "Recovery publishes finite geometry");
                }
            }
            finally { PhysicsServer.FreeRID(body); }
        }
        void Scene()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var circle = new CircleShape { Radius = 5 };
            using var planeX = new WorldBoundaryShape { Normal = Vector2.Right, Distance = -3 };
            using var planeY = new WorldBoundaryShape { Normal = Vector2.Up, Distance = -3 };
            var left = new StaticBody { Name = "Left", CollisionPriority = 9 }; left.AddChild(new CollisionShape { Shape = planeX }); root.AddChild(left);
            var bottom = new StaticBody { Name = "Bottom" }; bottom.AddChild(new CollisionShape { Shape = planeY }); root.AddChild(bottom);
            var actor = new CharacterBody { MotionMode = CharacterMotionMode.Floating }; actor.AddChild(new CollisionShape { Shape = circle }); root.AddChild(actor);
            var sleeper = new RigidBody { Position = new(500, 500), GravityScale = 0 }; sleeper.AddChild(new CollisionShape { Shape = circle }); root.AddChild(sleeper);
            using var tree = new SceneTree(root); sleeper.Sleeping = true;
            using var hit = actor.MoveAndCollide(Vector2.Zero, testOnly: true, recoveryAsCollision: true);
            Check(hit is not null && actor.Position == Vector2.Zero, "Test-only weighted motion preserves authored pose"); var expected = hit!.GetTravel();
            Check(expected.X > 2 && expected.Y is < -.5f and > -.7f, "Scene recovery obeys obstacle priority");
            actor.MoveAndSlide(); Near(actor.Position, expected, .002f, "Character uses shared recovery");
            actor.Position = Vector2.Zero; using var applied = actor.MoveAndCollide(Vector2.Zero, recoveryAsCollision: true);
            Near(actor.Position, expected, .002f, "Applied movement retains weighted travel");
            var shapeRID = PhysicsServer.BodyGetShape(sleeper.GetRID(), 0); var ticks = 0;
            Warm(() => { sleeper.CollisionPriority = (ticks++ & 1) + 1; tree.PhysicsFrame(.01); Check(sleeper.Sleeping, "Priority-only edits preserve sleep"); }, "sleeping scene/live priority");
            Check(PhysicsServer.BodyGetShape(sleeper.GetRID(), 0) == shapeRID, "Priority preserves shape identity");
        }
        void Finite()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var circle = new CircleShape { Radius = 5 }; using var rectangle = new RectangleShape { Size = new(10, 10) };
            using var capsule = new CapsuleShape { Radius = 5, Height = 10 }; using var segment = new SegmentShape { A = new(-5, 0), B = new(5, 0) };
            using var convex = new ConvexPolygonShape { Points = [new(-5, -5), new(5, -5), new(5, 5), new(-5, 5)] };
            using var concave = new ConcavePolygonShape { Segments = [new(-5, 0), new(5, 0)] };
            using var leftShape = new WorldBoundaryShape { Normal = Vector2.Right, Distance = -3 };
            var left = new StaticBody(); left.AddChild(new CollisionShape { Shape = leftShape }); root.AddChild(left);
            using var tree = new SceneTree(root); using var query = new PhysicsTestMotionParameters { RecoveryAsCollision = true };
            using var result = new PhysicsTestMotionResult(); var body = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Kinematic); PhysicsServer.BodySetSpace(body, world.Space);
                foreach (var shape in new Shape[] { circle, rectangle, capsule, segment, convex, concave })
                {
                    PhysicsServer.BodyAddShape(body, shape.GetRID());
                    Check(PhysicsServer.BodyTestMotion(body, query, result) && result.GetTravel().X > 1.7f, "Every finite source family recovers actual overlap");
                    var travel = result.GetTravel();
                    left.CollisionPriority = 9; Check(PhysicsServer.BodyTestMotion(body, query, result), "Scaled single-obstacle priority retains collision");
                    Near(result.GetTravel(), travel, .0001f, "Single-obstacle normalization is invariant under priority scaling");
                    PhysicsServer.BodyRemoveShape(body, 0); left.CollisionPriority = 1;
                }
            }
            finally { PhysicsServer.FreeRID(body); }
        }
        void OneWay()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 5 };
            using var floorShape = new RectangleShape { Size = new(100, 2) };
            var floor = new StaticBody { CollisionPriority = float.MaxValue }; var slot = new CollisionShape { Shape = floorShape, OneWayCollision = true, OneWayCollisionMargin = 2 };
            floor.AddChild(slot); root.AddChild(floor); using var tree = new SceneTree(root);
            var body = PhysicsServer.BodyCreate(); using var query = new PhysicsTestMotionParameters { RecoveryAsCollision = true, From = new(0, new Vector2(0, -5)) };
            using var result = new PhysicsTestMotionResult();
            try
            {
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Kinematic); PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetSpace(body, world.Space);
                Check(PhysicsServer.BodyTestMotion(body, query, result) && result.GetTravel().Y < -.8f, "Accepted one-way shallow recovery retains priority");
                query.From = new(0, new Vector2(0, 5)); Check(!PhysicsServer.BodyTestMotion(body, query, result), "Back-face recovery rejects before priority");
                query.From = new(0, Vector2.Zero); Check(!PhysicsServer.BodyTestMotion(body, query, result), "One-way depth beyond its margin rejects recovery");
                query.From = new(0, new Vector2(0, 10)); query.Motion = new(0, -20); Check(!PhysicsServer.BodyTestMotion(body, query, result), "Upward motion passes the one-way platform");
                query.From = new(0, new Vector2(0, -10)); query.Motion = new(0, 20); Check(PhysicsServer.BodyTestMotion(body, query, result), "Downward motion reaches one-way geometry");
            }
            finally { PhysicsServer.FreeRID(body); }
        }
        void Rigid()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 5 };
            using var material = new PhysicsMaterial { Friction = 0, Bounce = 0 };
            var wall = new StaticBody { Position = new(20, 0), PhysicsMaterialOverride = material }; wall.AddChild(new CollisionShape { Shape = circle }); root.AddChild(wall);
            var body = new RigidBody
            {
                Position = new(9.9f, 0),
                GravityScale = 0,
                Mass = 2,
                LinearVelocity = new(20, 0),
                MaxContactsReported = 8,
                PhysicsMaterialOverride = material,
                ContactMonitor = true,
                LinearDampMode = RigidBody.DampMode.Replace,
                LinearDamp = 0
            }; body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body);
            var entries = 0; body.BodyEntered += _ => entries++;
            using var tree = new SceneTree(root); using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
            using var history = PhysicsServer.SpaceCreateCheckpoint(world.Space); var index = 0;
            Warm(() =>
            {
                wall.CollisionPriority = (index & 1) == 0 ? 1 : float.MaxValue; body.CollisionPriority = (index++ & 1) == 0 ? float.MaxValue : 1;
                history.Capture(); var before = entries; tree.PhysicsFrame(.01);
                Check(entries == before + 1 && state.GetContactCount() > 0, "Priority preserves unique rigid contact entry");
                var impulse = Vector2.Zero; for (var i = 0; i < state.GetContactCount(); i++) impulse += state.GetContactImpulse(i);
                Near(body.LinearVelocity, Vector2.Zero, .002f, "Priority does not alter rigid response");
                Near(impulse, new(-40, 0), .003f, "Reported impulse matches rigid momentum");
                Check(MathF.Abs(state.InverseMass - .5f) < .0001f, "Priority does not alter physical mass");
                history.Restore();
            }, "rigid momentum/contact replay");
        }
        static void Near(Vector2 actual, Vector2 expected, float tolerance, string label)
        { if (actual.DistanceTo(expected) > tolerance) throw new InvalidOperationException($"{label}: {actual} expected {expected}"); }
        static void Warm(Action frame, string label)
        {
            frame(); for (var i = 0; i < 128; i++) frame(); Collect();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) frame();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Priority {label} allocated {owner}/{all} B"); Console.WriteLine($"Priority {label}: 128 full frames {owner}/{all} B.");
        }
    }
}
