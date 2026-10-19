using Electron2D;

public static partial class BackendChecks
{
    public static void RunShapePairMatrix(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { ShapePairMatrixCore(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void ShapePairMatrixCore(PhysicsServer.Backend? backend, bool threaded)
    {
        var owner = Thread.CurrentThread;
        if (backend is null) PhysicsServerManager.RegisterServer("ShapePairMatrix", () => new Solver
        { StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "The pair matrix retains actual selected numerical threading") });
        using var world = backend is { } chosen ? new World(chosen) : new World("ShapePairMatrix");
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
            "The matrix uses the explicitly selected backend without startup fallback");
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        Shape[] shapes = [new CircleShape { Radius = 3 }, new RectangleShape { Size = new(6, 6) },
            new CapsuleShape { Radius = 1, Height = 6 }, new SegmentShape { A = new(-3, 0), B = new(3, 0) },
            new ConvexPolygonShape { Points = [new(-3, -3), new(3, -3), new(0, 3)] },
            new ConcavePolygonShape { Segments = [new(-3, 0), new(3, 0), new(0, -3), new(0, 3)] },
            new SeparationRayShape { Length = 8 }, new WorldBoundaryShape { Normal = Vector2.Up }];
        using var gap = new CircleShape { Radius = .25f };
        var raw = new RID[8]; var scene = new StaticBody[8]; var ids = new RID[8];
        try
        {
            for (var i = 0; i < shapes.Length; i++)
            {
                ids[i] = shapes[i].GetRID(); raw[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(raw[i], PhysicsServer.BodyMode.Static);
                PhysicsServer.BodySetCollisionLayer(raw[i], 0); PhysicsServer.BodySetCollisionMask(raw[i], 0);
                PhysicsServer.BodyAddShape(raw[i], gap.GetRID(), disabled: true); PhysicsServer.BodyAddShape(raw[i], ids[i], disabled: true);
                PhysicsServer.BodySetShapeAsOneWayCollision(raw[i], 1, true, 2, Vector2.Up); PhysicsServer.BodySetSpace(raw[i], world.Space);
                scene[i] = new StaticBody { CollisionLayer = 0, CollisionMask = 0 };
                scene[i].AddChild(new CollisionShape { Shape = gap, Disabled = true });
                scene[i].AddChild(new CollisionShape { Shape = shapes[i], Disabled = true }); root.AddChild(scene[i]);
            }
            var mover = new RigidBody
            {
                Position = new(50, -50),
                LinearVelocity = Vector2.Right,
                GravityScale = 0,
                CanSleep = false,
                CollisionLayer = 0,
                CollisionMask = 0,
                LinearDampMode = RigidBody.DampMode.Replace,
                LinearDamp = 0
            };
            mover.AddChild(new CollisionShape { Shape = gap }); root.AddChild(mover);
            var points = new Vector2[12]; var sentinel = new Vector2(701, 709); var failures = new List<string>();
            int[] capacities = [0, 1, 4];
            for (var a = 0; a < 8; a++) for (var b = 0; b < 8; b++)
                {
                    for (var variant = 0; variant < 2; variant++) foreach (var capacity in capacities) Exercise(a, b, variant, capacity, false, true);
                    Exercise(a, b, 0, 1, true, true);
                }
            Check(failures.Count == 0, "Shape-pair matrix failures: " + string.Join("; ", failures.Take(12)));
            PairMatrixSceneOwners(world, root, tree, shapes[0], gap);
            PairMatrixMotions(world, shapes, raw, scene);
            for (var i = 0; i < 128; i++) Frame(i); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame(i);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Shape-pair matrix allocated {own}/{all} B");
            Console.WriteLine($"Shape-pair matrix {backend?.ToString() ?? "registered"}, separate={threaded}: 64 ordered families, resource/raw/scene, rigid covariance/caps/boundaries, 128 moving frames {own}/{all} B.");

            void Frame(int frame)
            {
                var index = frame % 64; Exercise(index / 8, index % 8, frame / 64, 1, false, false);
                var before = mover.Position.X; tree.PhysicsFrame(.01);
                Check(MathF.Abs(mover.Position.X - before - .01f) < .0001f, "The pair matrix runs complete moving selected frames");
            }
            void Exercise(int a, int b, int variant, int capacity, bool miss, bool audit)
            {
                var (left, right) = PairMatrixPoses(a, b);
                var expected = !(a == b && a >= 5) && !miss;
                if (miss)
                {
                    left = a == 7 ? Transform.Identity : new(0, new(-30, -30));
                    right = b == 7 ? Transform.Identity : new(0, new(30, -30));
                    if (a != 7 && b != 7) right.Origin = new(30, 30);
                }
                var outer = variant == 0 ? Transform.Identity : new Transform(.37f, new(50, -20));
                left = outer * left; right = outer * right;
                PhysicsServer.BodySetTransform(raw[a], left); scene[a].GlobalTransform = left;
                for (var route = 0; route < 3; route++)
                {
                    Array.Fill(points, sentinel); var destination = points.AsSpan(0, 2 * capacity);
                    var hit = route == 0 ? PhysicsServer.ShapeCollide(world.Space, ids[a], left, Vector2.Zero, ids[b], right, Vector2.Zero, destination, out var count)
                        : PhysicsServer.BodyCollideShape(route == 1 ? raw[a] : scene[a].GetRID(), 1, ids[b], right, Vector2.Zero, destination, out count);
                    var valid = hit == expected && count >= 0 && count <= capacity && (hit || count == 0);
                    if (expected && capacity > 0 && (a is not (3 or 5) || b is not (3 or 5))) valid &= count > 0;
                    for (var i = 0; valid && i < count; i++) valid = PairMatrixBoundary(a, left, points[2 * i]) && PairMatrixBoundary(b, right, points[2 * i + 1]);
                    for (var i = count * 2; valid && i < points.Length; i++) valid = points[i] == sentinel;
                    if (valid) continue;
                    var message = $"{a}/{b}, route={route}, variant={variant}, cap={capacity}, miss={miss}: expected={expected}, hit={hit}, count={count}, points={points[0]}/{points[1]}";
                    if (!audit) throw new InvalidOperationException(message);
                    failures.Add(message); if (failures.Count <= 16) Console.WriteLine(message);
                }
            }
        }
        finally
        {
            foreach (var body in raw) if (body.IsValid()) PhysicsServer.FreeRID(body);
            foreach (var body in scene) body?.Dispose();
            foreach (var shape in shapes) shape.Dispose();
        }
    }

    private static void PairMatrixMotions(World world, Shape[] shapes, RID[] raw, StaticBody[] scene)
    {
        var output = new Vector2[8];
        for (var a = 0; a < 5; a++) for (var b = 0; b < 5; b++)
            {
                Check(PhysicsServer.ShapeCollide(world.Space, shapes[a].GetRID(), new(0, new(-12, 0)), new(24, 0),
                    shapes[b].GetRID(), new(Mathf.Pi / 2, new(0, -12)), new(0, 16), output, out _),
                    "Every finite pairing intersects its independently displaced swept regions");
                PhysicsServer.BodySetTransform(raw[a], Transform.Identity); scene[a].GlobalTransform = Transform.Identity;
                Check(PhysicsServer.BodyCollideShape(raw[a], 1, shapes[b].GetRID(), new(Mathf.Pi / 2, new(12, 0)), new(-24, 0), output, out _) &&
                    PhysicsServer.BodyCollideShape(scene[a].GetRID(), 1, shapes[b].GetRID(), new(Mathf.Pi / 2, new(12, 0)), new(-24, 0), output, out _),
                    "Raw and scene source slots retain the other shape's complete swept region");
            }
        Check(!Pair(7, Transform.Identity, new(0, 1000), 0, new(0, new(0, 2)), new(0, -10)) &&
            !Pair(0, new(0, new(0, 2)), new(0, -10), 7, Transform.Identity, new(0, 1000)),
            "A boundary ignores its own displacement and tests the finite endpoint in either operand order");
        Check(!Pair(5, Transform.Identity, new(100, 0), 0, new(0, new(100, 0)), Vector2.Zero) &&
            !Pair(0, new(0, new(100, 0)), Vector2.Zero, 5, Transform.Identity, new(100, 0)),
            "Concave terrain displacement is ignored in either operand order");
        var far = new Transform(0, new(0, 14));
        Check(Pair(6, Transform.Identity, new(0, 4), 0, far, Vector2.Zero) && Pair(0, far, Vector2.Zero, 6, Transform.Identity, new(0, 4)),
            "Positive axial ray displacement extends sensing in either operand order");
        Check(!Pair(6, Transform.Identity, new(0, -4), 0, far, new(0, -100)) &&
            !Pair(0, far, new(0, -100), 6, Transform.Identity, new(100, 0)),
            "Negative/perpendicular ray displacement and counterpart displacement do not extend sensing");
        Check(Pair(6, new(0, new(0, -6)), new(10, 0), 7, Transform.Identity, new(0, 1000)) && output[0].DistanceTo(new(10, 2)) < .002f && output[1].DistanceTo(new(10, 0)) < .002f,
            "Boundary precedence retains full finite ray endpoint displacement and point ownership");
        bool Pair(int a, Transform pa, Vector2 ma, int b, Transform pb, Vector2 mb) =>
            PhysicsServer.ShapeCollide(world.Space, shapes[a].GetRID(), pa, ma, shapes[b].GetRID(), pb, mb, output, out _);
    }

    private static void PairMatrixSceneOwners(World world, SubViewport root, SceneTree tree, Shape shape, Shape gap)
    {
        PhysicsBody[] owners = [new StaticBody(), new AnimatableBody(), new AnimatableBody { SyncToPhysics = false },
            new CharacterBody(), new RigidBody(), new RigidBody { Freeze = true, FreezeMode = RigidFreezeMode.Kinematic },
            new PhysicalBone { SimulatePhysics = true }];
        var contacts = new Vector2[4];
        try
        {
            foreach (var body in owners)
            {
                body.CollisionLayer = 0; body.CollisionMask = 0;
                body.AddChild(new CollisionShape { Shape = gap, Disabled = true }); body.AddChild(new CollisionShape { Shape = shape, Disabled = true });
                root.AddChild(body); var id = body.GetRID(); PhysicsServer.BodySetSpace(id, world.Space); PhysicsServer.BodySetGravityScale(id, 0);
                var requested = new Transform(.37f, new(100, 20)); body.GlobalTransform = requested;
                var logical = PhysicsServer.BodyGetTransform(id); var other = logical; other.Origin += Vector2.Right;
                var tick = PhysicsServer.SpaceGetTick(world.Space);
                Check(PhysicsServer.BodyGetSpace(id) == world.Space && PhysicsServer.BodyCollideShape(id, 1, shape.GetRID(), other, Vector2.Zero, contacts, out var count) && count > 0 &&
                    PairMatrixBoundary(0, logical, contacts[0]) && PairMatrixBoundary(0, other, contacts[1]),
                    "Every selected scene role queries its current logical body/slot pose");
                Check(PhysicsServer.BodyGetTransform(id) == logical && body.GlobalTransform == logical && PhysicsServer.SpaceGetTick(world.Space) == tick,
                    "A logical pair query neither moves the body nor consumes its pending simulation target");
                if (body is AnimatableBody { SyncToPhysics: true })
                {
                    Check(logical.Origin != requested.Origin, "Synchronized animation keeps its later target out of the current logical pose");
                    tree.PhysicsFrame(.01);
                    Check(body.GlobalPosition.DistanceTo(requested.Origin) < .001f, "The queued animation target still completes at the next physical interval");
                }
            }
        }
        finally { foreach (var body in owners) body.Dispose(); }
    }

    private static (Transform A, Transform B) PairMatrixPoses(int a, int b)
    {
        var left = Transform.Identity; var right = new Transform(Mathf.Pi / 2, new(.25f, .25f));
        if (a == 7 || b == 7)
        {
            var finite = new Transform(0, new(0, a == 6 || b == 6 ? -6 : 2));
            return a == 7 ? (Transform.Identity, finite) : (finite, Transform.Identity);
        }
        if (a == 6 || b == 6)
        {
            var ray = new Transform(-Mathf.Pi / 2, new(-6, 0)); var finite = new Transform(Mathf.Pi / 2, Vector2.Zero);
            return a == 6 ? (ray, finite) : (finite, ray);
        }
        return (left, right);
    }
    private static bool PairMatrixBoundary(int family, Transform pose, Vector2 point)
    {
        if (!point.IsFinite()) return false;
        var p = (point - pose.Origin).Rotated(-pose.Rotation);
        return family switch
        {
            0 => MathF.Abs(p.Length() - 3) < .002f,
            1 => MathF.Min(MathF.Abs(MathF.Abs(p.X) - 3), MathF.Abs(MathF.Abs(p.Y) - 3)) < .002f && MathF.Max(MathF.Abs(p.X), MathF.Abs(p.Y)) < 3.002f,
            2 => MathF.Abs(p.DistanceTo(new(0, Math.Clamp(p.Y, -2, 2))) - 1) < .002f,
            3 => Line(p, new(-3, 0), new(3, 0)) < .002f,
            4 => MathF.Min(Line(p, new(-3, -3), new(3, -3)), MathF.Min(Line(p, new(3, -3), new(0, 3)), Line(p, new(0, 3), new(-3, -3)))) < .002f,
            5 => MathF.Min(Line(p, new(-3, 0), new(3, 0)), Line(p, new(0, -3), new(0, 3))) < .002f,
            6 => Line(p, Vector2.Zero, new(0, 8)) < .002f,
            _ => MathF.Abs(p.Y) < .002f
        };
        static float Line(Vector2 p, Vector2 a, Vector2 b) { var edge = b - a; return p.DistanceTo(a + edge * Math.Clamp((p - a).Dot(edge) / edge.LengthSquared(), 0, 1)); }
    }
}
