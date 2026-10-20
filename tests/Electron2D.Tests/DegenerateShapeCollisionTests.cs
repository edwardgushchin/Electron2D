using Electron2D;

internal static class DegenerateShapeCollisionTests
{
    private readonly record struct Case(Shape First, Transform From, Vector2 Motion,
        Shape Second, Transform OtherFrom, Vector2 OtherMotion, bool Hit);

    internal static void Run()
    {
        using var segment = new SegmentShape { A = new(-2, 0), B = new(2, 0) };
        using var rod = new CapsuleShape { Radius = 0, Height = 4 };
        using var point = new SegmentShape { A = new(-.1f, 0), B = new(.1f, 0) };
        using var dot = new CapsuleShape { Height = 0 };
        using var terrain = new ConcavePolygonShape { Segments = [new(-2, 0), new(2, 0)] };
        var shapes = new Shape[] { segment, rod, point, dot, terrain };
        var cases = new List<Case>();
        // A nonzero segment's tangent is a required separator when both cores have no area.
        Check(!segment.Collide(Transform.Identity, segment, new(0, new(8, 0))),
            "Disjoint collinear segments must not collide.");
        for (var turn = 0; turn < 4; turn++)
        {
            var basis = turn switch
            {
                0 => Transform.Identity,
                1 => new Transform(Vector2.Down, Vector2.Left, new(16, -8)),
                2 => new Transform(Vector2.Left, Vector2.Up, new(-16, 8)),
                _ => new Transform(Vector2.Up, Vector2.Right, new(8, 16))
            };
            foreach (var first in shapes)
                foreach (var second in shapes)
                    for (var x = -8; x <= 8; x += 2)
                        for (var y = -4; y <= 4; y += 2)
                        {
                            var from = basis;
                            var other = basis * new Transform(0, new(x, y));
                            var hit = !(first is ConcavePolygonShape && second is ConcavePolygonShape) &&
                                Crosses(Core(first, from), Core(second, other));
                            cases.Add(new(first, from, Vector2.Zero, second, other, Vector2.Zero, hit));
                        }
            // Independently swept collinear intervals, including endpoint touching and a gap.
            foreach (var x in new[] { 6, 8, 10, 12 })
                cases.Add(new(segment, basis, basis.BasisXform(new(2, 0)), segment,
                    basis * new Transform(0, new(x, 0)), basis.BasisXform(new(-2, 0)), x <= 8));
            // Ignored concave motion cannot fill a gap; point sweeps must still meet spatially.
            cases.Add(new(terrain, basis, basis.BasisXform(new(20, 0)), segment,
                basis * new Transform(0, new(8, 0)), Vector2.Zero, false));
            cases.Add(new(point, basis, basis.BasisXform(new(4, 0)), dot,
                basis * new Transform(0, new(8, 0)), basis.BasisXform(new(-2, 0)), false));
            cases.Add(new(point, basis, basis.BasisXform(new(6, 0)), dot,
                basis * new Transform(0, new(8, 0)), basis.BasisXform(new(-2, 0)), true));
        }
        foreach (var sample in cases)
        {
            Exercise(sample);
            var contacts = sample.First.CollideWithMotionAndGetContacts(sample.From, sample.Motion,
                sample.Second, sample.OtherFrom, sample.OtherMotion);
            Check(sample.Hit || contacts.Length == 0, "A rejected degenerate pair must return empty contacts.");
            Check(contacts.Length % 2 == 0 && contacts.All(p => p.IsFinite()), "Boundary pairs remain finite and paired.");
            if (sample.Motion == Vector2.Zero && sample.OtherMotion == Vector2.Zero)
            {
                Check(sample.First.Collide(sample.From, sample.Second, sample.OtherFrom) == sample.Hit,
                    "Static and zero-motion predicates agree.");
                Check(sample.First.CollideAndGetContacts(sample.From, sample.Second, sample.OtherFrom).SequenceEqual(contacts),
                    "Static and zero-motion contact snapshots agree.");
            }
        }
        for (var i = 0; i < 4; i++) foreach (var sample in cases) Exercise(sample);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        foreach (var sample in cases)
        {
            Exercise(sample);
            if (!sample.Hit)
                Check(sample.First.CollideWithMotionAndGetContacts(sample.From, sample.Motion,
                    sample.Second, sample.OtherFrom, sample.OtherMotion).Length == 0, "Warmed misses remain empty.");
        }
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, $"Prepared degenerate resource queries allocated {owner}/{all} owner/all-thread managed bytes.");
        Task.Run(() => { foreach (var sample in cases) Exercise(sample); }).GetAwaiter().GetResult();
        Console.WriteLine($"Degenerate shape pairs: {cases.Count} exact static/swept cases, both orders, all four public methods, worker queries; warm {owner}/{all} B, no world/device.");
    }

    private static void Exercise(Case sample)
    {
        var hit = sample.First.CollideWithMotion(sample.From, sample.Motion, sample.Second, sample.OtherFrom, sample.OtherMotion);
        var reverse = sample.Second.CollideWithMotion(sample.OtherFrom, sample.OtherMotion, sample.First, sample.From, sample.Motion);
        if (hit != sample.Hit || reverse != sample.Hit)
            throw new InvalidOperationException($"Degenerate {sample.First.ClassName}/{sample.Second.ClassName} at {sample.From.Origin}/{sample.OtherFrom.Origin}, motions {sample.Motion}/{sample.OtherMotion}: {hit}/{reverse}, expected {sample.Hit}.");
    }

    private static (Vector2 A, Vector2 B) Core(Shape shape, Transform pose) => shape switch
    {
        SegmentShape line => line.A.DistanceTo(line.B) <= .5f ? (pose.Origin, pose.Origin) : (pose * line.A, pose * line.B),
        CapsuleShape capsule => (pose * new Vector2(0, -capsule.Height / 2), pose * new Vector2(0, capsule.Height / 2)),
        ConcavePolygonShape => (pose * new Vector2(-2, 0), pose * new Vector2(2, 0)),
        _ => throw new InvalidOperationException()
    };

    private static bool Crosses((Vector2 A, Vector2 B) first, (Vector2 A, Vector2 B) second)
    {
        static double Side(Vector2 a, Vector2 b, Vector2 p) =>
            ((double)b.X - a.X) * ((double)p.Y - a.Y) - ((double)b.Y - a.Y) * ((double)p.X - a.X);
        static bool Between(float a, float b, float c, float d) => MathF.Max(a, b) >= MathF.Min(c, d) && MathF.Max(c, d) >= MathF.Min(a, b);
        return Between(first.A.X, first.B.X, second.A.X, second.B.X) &&
            Between(first.A.Y, first.B.Y, second.A.Y, second.B.Y) &&
            Side(first.A, first.B, second.A) * Side(first.A, first.B, second.B) <= 0 &&
            Side(second.A, second.B, first.A) * Side(second.A, second.B, first.B) <= 0;
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
