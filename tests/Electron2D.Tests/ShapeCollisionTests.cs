using Electron2D;

internal static class ShapeCollisionTests
{
    internal static void Run()
    {
        VerifyStaticAndContacts();
        VerifyIndependentMotion();
        VerifyFamiliesAndContour();
        VerifyRays();
        VerifyCapAndValidation();
        VerifyAllocationAndThreads();
        Console.WriteLine("Standalone shape collision, independent motion, boundary pairs and allocation checks passed.");
    }

    private static Transform At(float x, float y, float rotation = 0) => new(rotation, Vector2.One, 0, new(x, y));

    private static void VerifyStaticAndContacts()
    {
        using var first = new CircleShape();
        using var second = new CircleShape();
        Check(!first.Collide(Transform.Identity, second, At(21, 0)), "Separated resources do not collide.");
        Check(first.Collide(Transform.Identity, second, At(20, 0)), "Exact circle touching counts as collision.");
        var contacts = first.CollideAndGetContacts(Transform.Identity, second, At(15, 0));
        Check(contacts.Length == 2 && contacts[0].IsEqualApprox(new(10, 0)) &&
            contacts[1].IsEqualApprox(new(5, 0)) && (contacts[1] - contacts[0]).IsEqualApprox(new(-5, 0)),
            $"Circle boundary order and depth: {string.Join(';', contacts)}.");
        var reversed = second.CollideAndGetContacts(At(15, 0), first, Transform.Identity);
        Check(reversed.Length == 2 && reversed[0].IsEqualApprox(contacts[1]) && reversed[1].IsEqualApprox(contacts[0]),
            "Swapping resources swaps their boundary points.");
        contacts[0] = new(999, 999);
        Check(first.CollideAndGetContacts(Transform.Identity, second, At(15, 0))[0].IsEqualApprox(new(10, 0)),
            "Returned arrays are caller-owned.");
        Check(first.Collide(Transform.Identity, first, At(15, 0)), "One resource may be queried at two poses.");
        using var box = new RectangleShape();
        Check(box.CollideAndGetContacts(Transform.Identity, box, At(15, 0)) is { Length: 4 },
            "Overlapping parallel faces produce two contact pairs.");
        Check(box.Collide(Transform.Identity, box, At(20, 0)) &&
            box.CollideAndGetContacts(Transform.Identity, box, At(20, 0)).Length == 0,
            "Exact edge touching can collide without a separating pair.");
        Check(first.Collide(Transform.Identity, box, Transform.Identity), "Filled containment intersects.");
        using var cornerProbe = new CircleShape { Radius = 3 };
        Check(!cornerProbe.Collide(At(13, 13), box, Transform.Identity) &&
            cornerProbe.Collide(At(12, 12), box, Transform.Identity),
            "Rounded corners test their vertex axis, not just polygon face intervals.");
    }

    private static void VerifyIndependentMotion()
    {
        using var circle = new CircleShape { Radius = 2 };
        Check(circle.CollideWithMotion(Transform.Identity, new(30, 0), circle, At(20, 0), new(30, 0)),
            "Parallel equal motions intersect through overlapping independently swept regions.");
        var contacts = circle.CollideWithMotionAndGetContacts(Transform.Identity, new(30, 0), circle, At(20, 0), new(30, 0));
        Check(contacts.Length > 0 && contacts.Length % 2 == 0 &&
            (contacts[1] - contacts[0]).Length() > 0,
            "Motion contacts describe a swept-region penetration, not an earliest-time impact.");
        Check(circle.CollideWithMotion(At(0, 0, Mathf.Pi / 2), new(30, 0), circle, At(20, 0), Vector2.Zero),
            "Motion is in global axes even when a resource pose is rotated.");
        Check(!circle.CollideWithMotion(Transform.Identity, new(-30, 0), circle, At(20, 0), new(30, 0)),
            "Separated swept regions do not intersect.");
        Check(circle.CollideWithMotion(Transform.Identity, new(40, 0), circle, At(20, -20), new(0, 40)),
            "Both independently swept regions may meet away from either initial shape.");
        using var segment = new SegmentShape { A = new(-10, 0), B = new(10, 0) };
        using var terrain = new ConcavePolygonShape { Segments = [new(-10, 0), new(10, 0)] };
        Check(!circle.CollideWithMotion(At(0, 20), Vector2.Zero, terrain, Transform.Identity, new(0, 20)) &&
            !terrain.CollideWithMotion(Transform.Identity, new(0, 20), circle, At(0, 20), Vector2.Zero),
            "Concave terrain motion is ignored in either operand position.");
        Check(circle.CollideWithMotion(At(0, 20), Vector2.Zero, segment, Transform.Identity, new(0, 20)),
            "A concrete segment's motion contributes its swept region.");
    }

    private static void VerifyFamiliesAndContour()
    {
        using var circle = new CircleShape { Radius = 3 };
        using var capsule = new CapsuleShape { Radius = 3, Height = 10 };
        using var segment = new SegmentShape { A = new(-10, 0), B = new(10, 0) };
        using var rectangle = new RectangleShape { Size = new(10, 10) };
        using var convex = new ConvexPolygonShape { Points = [new(-5, -5), new(5, -5), new(5, 5), new(-5, 5)] };
        using var concave = new ConcavePolygonShape { Segments = [new(-10, 0), new(10, 0)] };
        foreach (var shape in new Shape[] { circle, capsule, segment, rectangle, convex, concave })
        {
            Check(circle.Collide(At(0, 2), shape, Transform.Identity) &&
                shape.Collide(Transform.Identity, circle, At(0, 2)), $"Static contact with {shape.GetType().Name}.");
            Check(circle.CollideAndGetContacts(At(0, 2), shape, Transform.Identity).Length > 0,
                $"Boundary contact with {shape.GetType().Name}.");
        }
        var families = new Shape[] { circle, capsule, segment, rectangle, convex, concave };
        foreach (var first in families)
            foreach (var second in families)
                Check(first.Collide(Transform.Identity, second, Transform.Identity) ==
                    !(first is ConcavePolygonShape && second is ConcavePolygonShape),
                    $"Origin contact pair {first.GetType().Name}/{second.GetType().Name}.");
        concave.Segments = [new(-20, -20), new(20, -20), new(20, -20), new(20, 20),
            new(20, 20), new(-20, 20), new(-20, 20), new(-20, -20)];
        Check(!circle.Collide(Transform.Identity, concave, Transform.Identity), "Hollow terrain containment is not intersection.");
        Check(!concave.Collide(Transform.Identity, concave, At(5, 0)), "Two concave resources do not collide.");
        convex.Points = [];
        Check(!circle.Collide(Transform.Identity, convex, Transform.Identity), "Empty geometry does not collide.");
        var points = Enumerable.Range(0, 24).Select(i => Vector2.FromAngle(i * Mathf.Tau / 24) * 50).ToArray();
        convex.Points = points;
        var outer = circle.CollideAndGetContacts(At(48, 0), convex, Transform.Identity);
        Check(outer.Length >= 2 && outer[1].X > 49 && MathF.Abs(outer[1].Y) < 5,
            $"A large convex resource returns its external boundary, not a fixture partition: {string.Join(';', outer)}.");
        Check(convex.CollideWithMotion(Transform.Identity, new(100, 0), circle, At(120, 0), Vector2.Zero),
            "The whole arbitrarily sized convex contour contributes its sweep.");
        convex.Changed += _ => throw new InvalidOperationException("User changed callback failed.");
        Reject<InvalidOperationException>(() => convex.Points = []);
        Check(!circle.Collide(Transform.Identity, convex, Transform.Identity), "Queries see committed geometry after callback failure.");
        segment.A = segment.B;
        Check(circle.Collide(At(10, 0), segment, Transform.Identity), "A zero-length segment contributes its point fallback.");
    }

    private static void VerifyRays()
    {
        using var ray = new SeparationRayShape { Length = 30 };
        using var box = new RectangleShape { Size = new(20, 10) };
        var contacts = ray.CollideAndGetContacts(Transform.Identity, box, At(0, 25));
        Check(contacts.Length == 2 && contacts[0].IsEqualApprox(new(0, 30)) && contacts[1].IsEqualApprox(new(0, 20)),
            "Ray contacts preserve the directed endpoint/surface pair.");
        Check(!ray.Collide(At(0, 23), box, At(0, 25)) && !ray.Collide(Transform.Identity, ray, Transform.Identity),
            "Ray containment and ray-ray pairs do not intersect.");
        ray.Length = 10;
        Check(ray.CollideWithMotion(Transform.Identity, new(0, 20), box, At(0, 25), new(200, 0)) &&
            !ray.CollideWithMotion(Transform.Identity, new(200, 0), box, At(0, 25), Vector2.Zero),
            "Only positive axial ray motion extends it; counterpart motion is ignored.");
        Check(!box.CollideWithMotion(At(-20, 5), new(20, 0), ray, Transform.Identity, Vector2.Zero),
            "Standalone reverse ray contact ignores ordinary counterpart motion.");
        ray.Length = 30;
        ray.SlideOnSlope = true;
        contacts = ray.CollideAndGetContacts(Transform.Identity, box, At(0, 25, 0.3f));
        Check((contacts[1] - contacts[0]).Normalized().X > 0.2f, "Ray slide policy follows the rotated surface normal.");
        using var polygon = new ConvexPolygonShape
        {
            Points = Enumerable.Range(0, 24).Select(i => Vector2.FromAngle(i * Mathf.Tau / 24) * 10).ToArray()
        };
        ray.SlideOnSlope = false;
        Check(ray.CollideAndGetContacts(Transform.Identity, polygon, At(0, 25)) is { Length: 2 } &&
            !ray.Collide(At(0, 25), polygon, At(0, 25)), "Full convex ray clipping rejects whole-shape containment.");
    }

    private static void VerifyCapAndValidation()
    {
        using var circle = new CircleShape { Radius = 10 };
        using var terrain = new ConcavePolygonShape
        {
            Segments = Enumerable.Range(0, 24).SelectMany(i => new Vector2[] { new(-20, 0.1f * i), new(20, 0.1f * i) }).ToArray()
        };
        Check(circle.CollideAndGetContacts(Transform.Identity, terrain, Transform.Identity) is { Length: 32 },
            "The result is capped at sixteen pairs.");
        terrain.Segments = terrain.Segments.Reverse().ToArray();
        var capped = circle.CollideAndGetContacts(Transform.Identity, terrain, Transform.Identity);
        Check(capped.Where((_, index) => index % 2 == 1).Max(point => point.Y) < 1.51f,
            "The cap replaces shallow pairs with deeper contacts, independent of traversal direction.");
        Reject<ArgumentNullException>(() => circle.Collide(Transform.Identity, null!, Transform.Identity));
        Reject<ArgumentException>(() => circle.Collide(new(0, new(2, 1), 0, Vector2.Zero), terrain, Transform.Identity));
        Reject<ArgumentOutOfRangeException>(() => circle.CollideWithMotion(Transform.Identity, new(float.NaN, 0), terrain, Transform.Identity, Vector2.Zero));
        Reject<ArgumentException>(() => circle.Collide(At(float.PositiveInfinity, 0), terrain, Transform.Identity));
        terrain.Dispose();
        Reject<ObjectDisposedException>(() => circle.Collide(Transform.Identity, terrain, Transform.Identity));
        Check(circle.Collide(Transform.Identity, circle, At(15, 0)), "A rejected query leaves later calls usable.");
        circle.Dispose();
        Reject<ObjectDisposedException>(() => circle.Collide(Transform.Identity, circle, Transform.Identity));
    }

    private static void VerifyAllocationAndThreads()
    {
        using var first = new CircleShape();
        using var second = new RectangleShape();
        for (var index = 0; index < 64; index++) first.CollideWithMotion(Transform.Identity, new(30, 0), second, At(20, 0), new(30, 0));
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 64; index++) first.CollideWithMotion(Transform.Identity, new(30, 0), second, At(20, 0), new(30, 0));
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed active standalone collision tests allocate no managed bytes.");
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 64; index++) first.CollideWithMotionAndGetContacts(Transform.Identity, Vector2.Zero, second, At(100, 0), Vector2.Zero);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed empty contact arrays allocate no managed bytes.");
        Check(Task.Run(() => first.Collide(Transform.Identity, second, At(5, 0))).GetAwaiter().GetResult(),
            "Independent geometry can be queried on another thread without a physics world.");
        using var contour = new ConvexPolygonShape
        { Points = Enumerable.Range(0, 24).Select(i => Vector2.FromAngle(i * Mathf.Tau / 24) * 50).ToArray() };
        for (var index = 0; index < 64; index++) contour.CollideWithMotion(Transform.Identity, new(100, 0), second, At(80, 0), Vector2.Zero);
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 64; index++) contour.CollideWithMotion(Transform.Identity, new(100, 0), second, At(80, 0), Vector2.Zero);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed full-contour swept tests allocate no managed bytes.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
