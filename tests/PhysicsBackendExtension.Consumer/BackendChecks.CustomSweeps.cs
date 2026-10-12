using Electron2D;

public static partial class BackendChecks
{
    private static void VerifyCustomSweeps(PhysicsServer.Backend? backend = null)
    {
        if (backend is null) PhysicsServerManager.RegisterServer("CustomSweeps", () => new Solver());
        using var world = backend is { } selected ? new World(selected) : new World("CustomSweeps");
        Check(PhysicsServer.SpaceGetBackend(world.Space) == (backend ?? PhysicsServer.Backend.Extension), "Custom resource pairs use the selected implementation");
        using var ring = new CustomShape(Annulus.Create(5, 10)); using var circle = new CircleShape { Radius = 1 };
        (Vector2 RingMotion, Vector2 CircleAt, Vector2 CircleMotion, bool Hit)[] cases =
        [
            (new(20, 0), new(15, 0), new(20, 0), true),
            (new(30, 0), new(15, 25), new(0, -15), true),
            (new(20, 0), new(0, 25), new(20, 0), false),
            (new(2, 0), new(1, 0), Vector2.Zero, false),
            (new(12, 0), new(1, 0), Vector2.Zero, true),
            (Vector2.Zero, new(4, 0), Vector2.Zero, true),
            (Vector2.Zero, new(3.999f, 0), Vector2.Zero, false),
            (Vector2.Zero, new(11, 0), Vector2.Zero, true),
            (Vector2.Zero, new(11.001f, 0), Vector2.Zero, false),
            (new(-20, 0), new(-15, 0), new(-20, 0), true),
            (new(0, 20), new(0, 15), new(0, 20), true),
            (new(6, 0), new(3, 0), new(0, 6), true)
        ];
        var output = new Vector2[4]; var reversed = new Vector2[4];
        foreach (var item in cases)
        {
            var pose = new Transform(0, item.CircleAt);
            Check(ring.CollideWithMotion(Transform.Identity, item.RingMotion, circle, pose, item.CircleMotion) == item.Hit &&
                circle.CollideWithMotion(pose, item.CircleMotion, ring, Transform.Identity, item.RingMotion) == item.Hit,
                "Custom regions sweep independently in both argument orders");
            var pairs = ring.CollideWithMotionAndGetContacts(Transform.Identity, item.RingMotion, circle, pose, item.CircleMotion);
            if (item.Hit) Boundaries(pairs[0], pairs[1], item);
            else Check(pairs.Length == 0, "Separated outer regions and retained common inner hole have no contacts");
            Check(PhysicsServer.ShapeCollide(world.Space, ring.GetRID(), Transform.Identity, item.RingMotion,
                circle.GetRID(), pose, item.CircleMotion, output, out var count) == item.Hit, "Selected custom pair retains independent truth");
            Check(PhysicsServer.ShapeCollide(world.Space, circle.GetRID(), pose, item.CircleMotion,
                ring.GetRID(), Transform.Identity, item.RingMotion, reversed, out var reverseCount) == item.Hit && count == reverseCount,
                "Selected custom reversal retains truth and count");
            if (item.Hit)
            {
                Check(count == 1 && output[0] == reversed[1] && output[1] == reversed[0], "Reversal preserves sampled boundary ownership");
                Boundaries(output[0], output[1], item);
            }
        }
        var random = new Random(43127); var hits = 0;
        for (var i = 0; i < 512; i++)
        {
            Vector2 Next() => new(random.Next(-240, 241) / 10f, random.Next(-240, 241) / 10f);
            var item = (RingMotion: Next(), CircleAt: Next(), CircleMotion: Next(), Hit: true);
            var pose = new Transform(0, item.CircleAt);
            var hit = PhysicsServer.ShapeCollide(world.Space, ring.GetRID(), Transform.Identity, item.RingMotion,
                circle.GetRID(), pose, item.CircleMotion, output, out var count);
            Check(circle.CollideWithMotion(pose, item.CircleMotion, ring, Transform.Identity, item.RingMotion) == hit,
                "Seeded selected and reversed standalone sweep truth agree");
            if (!hit) continue;
            hits++; Check(count == 1, "Seeded custom collision supplies one boundary pair"); Boundaries(output[0], output[1], item);
        }
        Check(hits > 0 && hits < 512, "Seeded matrix exercises both collisions and misses");
        for (var i = 0; i < 128; i++) Cycle();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Cycle();
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, $"Independent custom swept pairs allocated {owner}/{all} owner/all-thread bytes");
        Console.WriteLine($"Custom independent sweeps {backend?.ToString() ?? "registered"}: 12 exact cases, 512 seeded cases ({hits} hits), both argument orders and 128 warmed selected/standalone cycles 0/0 B passed.");
        void Cycle()
        {
            var item = cases[0]; var pose = new Transform(0, item.CircleAt);
            Check(ring.CollideWithMotion(Transform.Identity, item.RingMotion, circle, pose, item.CircleMotion), "Warm standalone sweep truth");
            Check(PhysicsServer.ShapeCollide(world.Space, ring.GetRID(), Transform.Identity, item.RingMotion,
                circle.GetRID(), pose, item.CircleMotion, output, out var count) && count == 1, "Warm selected sweep truth/contact");
        }
        static void Boundaries(Vector2 ringPoint, Vector2 circlePoint, (Vector2 RingMotion, Vector2 CircleAt, Vector2 CircleMotion, bool Hit) item)
        {
            var ringDistance = ringPoint.DistanceTo(Geometry.GetClosestPointToSegment(ringPoint, Vector2.Zero, item.RingMotion));
            var holeDistance = Math.Max(ringPoint.Length(), ringPoint.DistanceTo(item.RingMotion));
            var circleDistance = circlePoint.DistanceTo(Geometry.GetClosestPointToSegment(circlePoint, item.CircleAt, item.CircleAt + item.CircleMotion));
            Check(Math.Abs(ringDistance - 10) < .0001f || Math.Abs(holeDistance - 5) < .0001f,
                "Custom contact lies on the outer capsule or remaining inner lens boundary");
            Check(Math.Abs(circleDistance - 1) < .0001f, "Counterpart contact lies on its independently swept capsule boundary");
        }
    }
}
