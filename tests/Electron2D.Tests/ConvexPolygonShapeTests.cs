using Electron2D;

internal static class ConvexPolygonShapeTests
{
    internal static void Run()
    {
        VerifyPointsAndCloud();
        VerifyLargeHullFixtures();
        VerifyRotatedFixture();
        VerifyPackedResource();
        VerifySharedAndCopiedGeometry();
        Console.WriteLine("Convex polygon hull, compound fixtures, area, packing and allocation checks passed.");
    }

    private static void VerifySharedAndCopiedGeometry()
    {
        using var polygon = new ConvexPolygonShape { Points = RegularPolygon(12, 20) };
        using var duplicate = (ConvexPolygonShape)polygon.Duplicate();
        using var root = new SubViewport();
        using var tree = new SceneTree(root);
        var first = new StaticBody { Name = "First" };
        var second = new StaticBody { Name = "Second", Position = new(100, 0) };
        var copied = new StaticBody { Name = "Copy", Position = new(200, 0) };
        first.AddChild(new CollisionShape { Shape = polygon });
        second.AddChild(new CollisionShape { Shape = polygon });
        copied.AddChild(new CollisionShape { Shape = duplicate });
        root.AddChild(first); root.AddChild(second); root.AddChild(copied);
        var direct = first.GetWorld()!.DirectSpaceState;
        using var point = new PhysicsPointQueryParameters();
        var hits = new PhysicsPointResult[4];
        // Probe well inside radius 20 and well outside radius 5, independent of contact slop.
        foreach (var x in new[] { 15, 115, 215 })
        {
            point.Position = new(x, 0);
            Check(direct.IntersectPoint(point, hits) > 0, "Shared and duplicated polygons compile their complete contours.");
        }
        polygon.Points = RegularPolygon(12, 5);
        Reject<ArgumentException>(() => polygon.Points = [new(0, 0), new(0.001f, 0), new(0, 0.001f)]);
        foreach (var x in new[] { 15, 115, 215 })
        {
            point.Position = new(x, 0);
            Check((direct.IntersectPoint(point, hits) > 0) == (x == 215),
                "An edit updates both shared fixtures, a rejected contour keeps that edit, and the duplicate stays independent.");
        }
        foreach (var x in new[] { 3, 103 })
        {
            point.Position = new(x, 0);
            Check(direct.IntersectPoint(point, hits) > 0, "Rejected geometry preserves the smaller compiled fixtures instead of clearing them.");
        }
        using var query = new PhysicsShapeQueryParameters { Shape = polygon, Transform = new(0, new Vector2(-15, 0)) };
        var results = new PhysicsShapeResult[4];
        Check(direct.IntersectShape(query, results) == 0, "Query compilation uses the current small contour.");
        query.Shape = duplicate;
        Check(direct.IntersectShape(query, results) > 0, "Duplicate query compilation retains the original large contour.");
        for (var i = 0; i < 128; i++) { query.Shape = i % 2 == 0 ? polygon : duplicate; direct.IntersectShape(query, results); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++) { query.Shape = i % 2 == 0 ? polygon : duplicate; direct.IntersectShape(query, results); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed compiled polygon reuse allocates zero managed bytes.");
    }

    private static void VerifyPointsAndCloud()
    {
        using var shape = new ConvexPolygonShape();
        Check(shape.Points.Length == 0 && shape.GetRect() == default,
            "An empty convex polygon has no vertices and empty bounds.");
        var changes = 0;
        shape.Changed += _ => changes++;
        var clockwise = new[] { new Vector2(-10, -10), new Vector2(-10, 10),
            new Vector2(10, 10), new Vector2(10, -10) };
        shape.Points = clockwise;
        clockwise[0] = Vector2.Zero;
        var copyOut = shape.Points;
        copyOut[1] = Vector2.Zero;
        Check(shape.Points[0] == new Vector2(-10, -10) && shape.Points[1] == new Vector2(-10, 10) &&
              shape.GetRect() == new Rect2(-10, -10, 20, 20) && changes == 1,
            "Clockwise points are stored exactly and neither input nor output arrays alias resource state.");
        shape.Points = shape.Points;
        Check(changes == 2, "A repeated Points assignment publishes a resource change.");
        Reject<ArgumentNullException>(() => shape.Points = null!);
        Reject<ArgumentException>(() => shape.Points = [new(0, 0), new(10, 0), new(4, 4), new(10, 10), new(0, 10)]);
        var pentagon = RegularPolygon(5, 20);
        Reject<ArgumentException>(() => shape.Points = [pentagon[0], pentagon[2], pentagon[4], pentagon[1], pentagon[3]]);
        Reject<ArgumentException>(() => shape.Points = [new(0, 0), new(float.NaN, 1), new(1, 1)]);
        Reject<ArgumentException>(() => shape.Points = [new(-float.MaxValue, 0), new(float.MaxValue, 0), new(0, 1)]);
        Reject<ArgumentException>(() => shape.Points = [new(0, 0), new(1, 1)]);
        Check(changes == 2 && shape.Points.Length == 4,
            "Invalid convex contours reject before changing geometry.");

        shape.SetPointCloud([new(0, 0), new(10, 0), new(5, 5), new(10, 10), new(0, 10), new(4, 6)]);
        var hull = shape.Points;
        Check(hull.Length == 5 && hull[0] == hull[^1] &&
              shape.GetRect() == new Rect2(0, 0, 10, 10) && changes == 3,
            "Point-cloud assignment removes interior points and retains the closed hull contour.");
        Reject<ArgumentException>(() => shape.SetPointCloud([new(0, 0), new(1, 1), new(2, 2)]));
        Check(changes == 3, "A collinear cloud rejects without replacing the previous hull.");
        using var duplicate = (ConvexPolygonShape)shape.Duplicate();
        shape.Points = [];
        Check(shape.GetRect() == default && duplicate.Points.Length == 5 && !ReferenceEquals(shape.Points, duplicate.Points),
            "Clearing Points removes geometry while Resource duplication remains independent.");
        duplicate.Dispose();
        Reject<ObjectDisposedException>(() => _ = duplicate.GetRect());
    }

    private static void VerifyLargeHullFixtures()
    {
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        using var polygon = new ConvexPolygonShape { Points = RegularPolygon(12, 20) };
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorShape });
        var body = new RigidBody { CanSleep = false, ContactMonitor = true, MaxContactsReported = 8 };
        body.AddChild(new CollisionShape { Shape = polygon });
        root.AddChild(floor); root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 67 and < 74 && body.GetContactCount() > 0 &&
              body.GetCollidingBodies().Contains(floor),
            "A twelve-vertex solid hull spans multiple backend fixtures but collides as one body.");

        using var sensor = new ConvexPolygonShape { Points = RegularPolygon(12, 30) };
        var area = new Area { Position = new(0, 70) };
        area.AddChild(new CollisionShape { Shape = sensor });
        var enters = 0; var exits = 0;
        area.BodyEntered += other => { if (ReferenceEquals(other, body)) enters++; };
        area.BodyExited += other => { if (ReferenceEquals(other, body)) exits++; };
        root.AddChild(area);
        tree.PhysicsFrame(1d / 60);
        Check(enters == 1 && area.GetOverlappingBodies().Contains(body),
            "A compound convex sensor reports its overlapping body once.");
        using var probeShape = new CircleShape { Radius = 2 };
        var probe = new StaticBody { Name = "LowerProbe", Position = new(0, 45) };
        probe.AddChild(new CollisionShape { Shape = probeShape });
        root.AddChild(probe);
        tree.PhysicsFrame(1d / 60);
        Check(area.GetOverlappingBodies().Contains(probe),
            "The far side of a twelve-vertex hull uses the later compound fixture.");
        sensor.Points = [];
        tree.PhysicsFrame(1d / 60);
        Check(exits == 1 && !area.GetOverlappingBodies().Contains(body),
            "Clearing a live polygon removes every compound fixture and exits the overlap.");

        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed multi-fixture polygon contact and area scans allocate no managed memory.");

        polygon.Changed += _ => throw new InvalidOperationException("User callback failure.");
        Reject<InvalidOperationException>(() => polygon.Points = RegularPolygon(12, 10));
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 77 and < 84,
            "A failed earlier Changed handler cannot hide a live compound fixture rebuild.");
        Reject<InvalidOperationException>(() => polygon.Points = []);
        body.Sleeping = false;
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y > 100 && body.GetContactCount() == 0,
            "Clearing a body polygon removes all compound fixtures while unshaped motion continues.");
    }

    private static void VerifyRotatedFixture()
    {
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        using var shape = new ConvexPolygonShape
        {
            Points = [new(-30, -10), new(30, -10), new(30, 10), new(-30, 10)]
        };
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorShape });
        var body = new RigidBody { CanSleep = false, LockRotation = true };
        body.AddChild(new CollisionShape { Shape = shape, Rotation = Mathf.Pi * 0.5f });
        root.AddChild(floor); root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 57 and < 64,
            "A locally rotated polygon fixture rests on its long axis above the floor.");
    }

    private static void VerifyPackedResource()
    {
        using var polygon = new ConvexPolygonShape { Points = RegularPolygon(10, 16) };
        using var root = new Node { Name = "Root" };
        var body = new RigidBody { Name = "Body" };
        var collision = new CollisionShape { Name = "Collision", Shape = polygon };
        root.AddChild(body); body.AddChild(collision);
        body.Owner = root; collision.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root);
        using var copy = packed.Instantiate();
        Check(ReferenceEquals(copy.GetNode<CollisionShape>("Body/Collision").Shape, polygon) &&
              polygon.Points.Length == 10,
            "PackedScene preserves a borrowed large convex polygon resource.");
    }

    private static Vector2[] RegularPolygon(int count, float radius)
    {
        var result = new Vector2[count];
        for (var index = 0; index < count; index++)
        {
            var angle = 2 * MathF.PI * index / count;
            result[index] = new(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
        }
        return result;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
