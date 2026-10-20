using Electron2D;

public static partial class BackendChecks
{
    public static void RunHollowPairs(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread); var owner = Thread.CurrentThread;
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        PhysicsServerManager.RegisterServer("HollowPairs", () => new Solver
        { StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "Hollow pairs retain their selected numerical lane") });
        try
        {
            using (var world = backend is { } selected ? new World(selected) : new World("HollowPairs")) VerifyHollowResources(world);
            if (backend is null) { RunCustomMotion(threaded, hollowPeer: true); VerifyHollowDynamics(); }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void VerifyHollowResources(World world)
    {
        using var large = new CustomShape(Annulus.Create(5, 10)); using var small = new CustomShape(Annulus.Create(.25f, 1));
        var output = new Vector2[4]; var reversed = new Vector2[4];
        (Vector2 A, Vector2 At, Vector2 B, bool Hit)[] cases =
        [
            (Vector2.Zero, Vector2.Zero, Vector2.Zero, false),
            (Vector2.Zero, new(3.999f, 0), Vector2.Zero, false),
            (Vector2.Zero, new(4, 0), Vector2.Zero, true),
            (Vector2.Zero, new(4.5f, 0), Vector2.Zero, true),
            (Vector2.Zero, new(11, 0), Vector2.Zero, true),
            (Vector2.Zero, new(11.001f, 0), Vector2.Zero, false),
            (new(2, 0), new(1, 0), Vector2.Zero, false),
            (new(12, 0), new(1, 0), Vector2.Zero, true),
            (new(20, 0), new(15, 0), new(20, 0), true),
            (new(20, 0), new(0, 25), new(20, 0), false),
            (new(30, 0), new(15, 25), new(0, -15), true),
            (new(6, 0), new(3, 0), new(0, 6), true)
        ];
        foreach (var item in cases) Pair(item.A, item.At, item.B, item.Hit);
        var random = new Random(59173); var hits = 0;
        for (var i = 0; i < 512; i++)
        {
            Vector2 Next() => new(random.Next(-240, 241) / 10f, random.Next(-240, 241) / 10f);
            var a = Next(); var at = Next(); var b = Next();
            var hit = PhysicsServer.ShapeCollide(world.Space, large.GetRID(), Transform.Identity, a, small.GetRID(), new(0, at), b, output, out _);
            var near = Geometry.GetClosestPointsBetweenSegments(Vector2.Zero, a, at, at + b);
            var far = MathF.Max(MathF.Max(at.Length(), (at + b).Length()), MathF.Max(a.DistanceTo(at), a.DistanceTo(at + b)));
            Check(hit == (near[0].DistanceTo(near[1]) <= 11 && far >= 4), "Seeded hollow truth follows the full independent center-distance range");
            if (hit) hits++; Pair(a, at, b, hit);
        }
        Check(hits > 0 && hits < 512, "Hollow resource seeds exercise hits and misses");
        for (var i = 0; i < 128; i++) Cycle(); Collect();
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Cycle();
        own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(own == 0 && all == 0, $"Hollow resource pairs allocated {own}/{all} B");
        Console.WriteLine($"Hollow resources {world.PhysicsBackendName}: 12 exact / 512 seeded ({hits} hits), both orders, 128 warmed cycles {own}/{all} B.");

        void Cycle() => Pair(new(6, 0), new(3, 0), new(0, 6), true);
        void Pair(Vector2 a, Vector2 at, Vector2 b, bool expected)
        {
            var pose = new Transform(0, at);
            Check(large.CollideWithMotion(Transform.Identity, a, small, pose, b) == expected && small.CollideWithMotion(pose, b, large, Transform.Identity, a) == expected,
                "Standalone hollow regions move independently in either order");
            Check(PhysicsServer.ShapeCollide(world.Space, large.GetRID(), Transform.Identity, a, small.GetRID(), pose, b, output, out var count) == expected &&
                PhysicsServer.ShapeCollide(world.Space, small.GetRID(), pose, b, large.GetRID(), Transform.Identity, a, reversed, out var reverseCount) == expected && count == reverseCount,
                "Selected hollow pairs retain truth and contact count in both orders");
            if (!expected) return;
            Check(count == 1, "Hollow resource hit has a boundary pair");
            Boundary(output[0], Vector2.Zero, a, 5, 10); Boundary(output[1], at, b, .25f, 1);
            Boundary(reversed[1], Vector2.Zero, a, 5, 10); Boundary(reversed[0], at, b, .25f, 1);
        }
        static void Boundary(Vector2 point, Vector2 start, Vector2 motion, float inner, float outer)
        {
            var nearest = point.DistanceTo(Geometry.GetClosestPointToSegment(point, start, start + motion));
            var farthest = MathF.Max(point.DistanceTo(start), point.DistanceTo(start + motion));
            Check(nearest <= outer + .0001f && farthest >= inner - .0001f && (MathF.Abs(nearest - outer) < .0001f || MathF.Abs(farthest - inner) < .0001f),
                "Hollow contact belongs to its outer capsule or retained inner lens boundary");
        }
    }

    private static void VerifyHollowDynamics()
    {
        using var world = new World("HollowPairs"); using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        using var large = new CustomShape(Annulus.Create(5, 10)); using var small = new CustomShape(Annulus.Create(.25f, 1));
        var walls = new StaticBody[2]; var movers = new RigidBody[2];
        for (var i = 0; i < 2; i++)
        {
            walls[i] = new StaticBody { Position = new(i * 100, 0), CollisionLayer = 1, CollisionMask = 1 };
            movers[i] = new RigidBody
            {
                CollisionLayer = 1,
                CollisionMask = 1,
                GravityScale = 0,
                CanSleep = false,
                MaxContactsReported = 4,
                LinearDampMode = RigidBody.DampMode.Replace,
                LinearDamp = 0,
                AngularDampMode = RigidBody.DampMode.Replace,
                AngularDamp = 0
            };
            walls[i].AddChild(new CollisionShape { Shape = i == 0 ? large : small }); movers[i].AddChild(new CollisionShape { Shape = i == 0 ? small : large });
            if (i == 0) { root.AddChild(walls[i]); root.AddChild(movers[i]); } else { root.AddChild(movers[i]); root.AddChild(walls[i]); }
        }
        using var query = new PhysicsShapeQueryParameters { Shape = large, Margin = 0, Exclude = [movers[0].GetRID(), movers[1].GetRID()] }; var points = new Vector2[4];
        Check(world.DirectSpaceState.CollideShape(query, points) == 1 && MathF.Abs(points[0].Length() - 10) < .0001f && MathF.Abs(points[1].Length() - 10) < .0001f,
            "Coincident equal rings use outer surfaces when neither body fits inside the other's hole");
        for (var i = 0; i < 128; i++) Frame(i); Collect();
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame(i);
        own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(own == 0 && all == 0, $"Hollow dynamic frames allocated {own}/{all} B");
        Console.WriteLine($"Hollow dynamics: both body orders, inner/outer response, exact collider/shape identity and signed contact impulses, 128 frames {own}/{all} B.");

        void Frame(int configuration)
        {
            var inner = configuration % 2 == 0; var sign = configuration / 2 % 2 == 0 ? 1 : -1;
            var expected = new Vector2(sign * (inner ? -1 : 1), 0);
            for (var i = 0; i < 2; i++)
            {
                var moving = movers[i]; moving.Position = walls[i].Position + new Vector2(sign * (inner ? 4.75f : 10.25f), 0);
                moving.Rotation = 0; moving.AngularVelocity = 0; moving.LinearVelocity = -expected * 2;
            }
            tree.PhysicsFrame(.01);
            for (var i = 0; i < 2; i++)
            {
                var moving = movers[i]; var state = PhysicsServer.BodyGetDirectState(moving.GetRID())!;
                Check(state.GetContactCount() == 1 && state.GetContactCollider(0) == walls[i].GetRID() && state.GetContactColliderShape(0) == 0 && state.GetContactLocalShape(0) == 0,
                    "Hollow dynamics preserve collider and logical shape ownership");
                Check(state.GetContactLocalNormal(0).Dot(expected) > .999f && state.GetContactImpulse(0).Dot(expected) > 1.9f && moving.LinearVelocity.Dot(expected) > -.0001f,
                    "Hollow wall response opposes approach on either surface and in either body order");
                var distance = moving.Position.DistanceTo(walls[i].Position);
                Check(inner ? distance < 4.75f : distance > 10.25f, "Hollow correction resolves penetration toward its actual empty region");
            }
        }
    }
}
