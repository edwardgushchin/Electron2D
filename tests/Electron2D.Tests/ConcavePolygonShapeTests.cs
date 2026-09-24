using Electron2D;

internal static class ConcavePolygonShapeTests
{
    internal static void Run()
    {
        VerifySegmentsAndCopy();
        VerifyHollowBodyAndArea();
        VerifyDynamicConcaveMass();
        VerifyPackedResource();
        Console.WriteLine("Concave segment pairs, hollow contacts, area and allocation checks passed.");
    }

    private static void VerifySegmentsAndCopy()
    {
        using var shape = new ConcavePolygonShape();
        Check(shape.Segments.Length == 0 && shape.GetRect() == default,
            "An empty concave contour has no segments and empty bounds.");
        var changes = 0;
        shape.Changed += _ => changes++;
        var input = new[] { new Vector2(10, 20), new Vector2(-10, -20),
            new Vector2(40, -30), new Vector2(40, 10) };
        shape.Segments = input;
        input[0] = Vector2.Zero;
        var read = shape.Segments;
        read[1] = Vector2.Zero;
        Check(shape.Segments[0] == new Vector2(10, 20) && shape.Segments[1] == new Vector2(-10, -20) &&
              shape.GetRect() == new Rect2(-10, -30, 50, 50) && changes == 1,
            "Paired endpoints, off-center bounds and array ownership are preserved.");
        shape.Segments = shape.Segments;
        Check(changes == 2, "Equal segment-array assignments still publish a resource change.");
        Reject<ArgumentNullException>(() => shape.Segments = null!);
        Reject<ArgumentException>(() => shape.Segments = [new(0, 0)]);
        Reject<ArgumentException>(() => shape.Segments = [new(0, 0), new(float.NaN, 1)]);
        Reject<ArgumentException>(() => shape.Segments = [new(-float.MaxValue, 0), new(float.MaxValue, 0)]);
        Reject<ArgumentException>(() => shape.Segments = [new(-float.MaxValue, 0), new(-float.MaxValue, 0),
            new(float.MaxValue, 0), new(float.MaxValue, 0)]);
        Check(changes == 2 && shape.Segments.Length == 4,
            "Odd, nonfinite and overflowing pairs reject before mutation.");
        using var duplicate = (ConcavePolygonShape)shape.Duplicate();
        shape.Segments = [];
        Check(shape.GetRect() == default && duplicate.Segments.Length == 4 &&
              duplicate.GetRect() == new Rect2(-10, -30, 50, 50),
            "Clearing a source does not change an independently duplicated contour.");
        duplicate.Dispose();
        Reject<ObjectDisposedException>(() => _ = duplicate.GetRect());
    }

    private static void VerifyHollowBodyAndArea()
    {
        using var terrain = new ConcavePolygonShape
        {
            Segments = [new(-80, 0), new(80, 0), new(-80, 0), new(-80, -60), new(80, 0), new(80, -60)]
        };
        using var circle = new CircleShape();
        var root = new Node();
        var ground = new StaticBody { Position = new(0, 100) };
        ground.AddChild(new CollisionShape { Shape = terrain });
        var body = new RigidBody { CanSleep = false, ContactMonitor = true, MaxContactsReported = 4 };
        body.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(ground); root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 87 and < 93 && body.GetContactCount() > 0 &&
              body.GetCollidingBodies().Contains(ground),
            "A collection of independent terrain edges supports one body-level contact.");

        using var sensor = new ConcavePolygonShape
        {
            Segments = [new(-40, -40), new(40, -40), new(40, -40), new(40, 40),
                new(40, 40), new(-40, 40), new(-40, 40), new(-40, -40)]
        };
        var area = new Area { Position = new(0, 90) };
        area.AddChild(new CollisionShape { Shape = sensor });
        var enters = 0; var exits = 0;
        area.BodyEntered += other => { if (ReferenceEquals(other, body)) enters++; };
        area.BodyExited += other => { if (ReferenceEquals(other, body)) exits++; };
        root.AddChild(area);
        tree.PhysicsFrame(1d / 60);
        Check(enters == 0 && !area.GetOverlappingBodies().Contains(body),
            "A body wholly inside the hollow contour is not an overlap.");
        area.Position = new(0, 50);
        tree.PhysicsFrame(1d / 60);
        Check(enters == 1 && area.GetOverlappingBodies().Contains(body),
            "Touching one edge of the contour reports one body entry.");
        area.Position = new(0, 90);
        tree.PhysicsFrame(1d / 60);
        Check(exits == 1 && !area.GetOverlappingBodies().Contains(body),
            "Moving the hollow contour around the body reports one exit.");

        sensor.Segments = [Vector2.Zero, Vector2.Zero];
        tree.PhysicsFrame(1d / 60);
        Check(enters == 2 && area.GetOverlappingBodies().Contains(body),
            "A coincident endpoint pair uses a valid point fixture for sensing.");
        sensor.Segments = [new(200, 0), new(200, 0)];
        tree.PhysicsFrame(1d / 60);
        Check(exits == 2 && !area.GetOverlappingBodies().Contains(body),
            "Moving the point fixture away ends its overlap.");

        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed concave terrain contact and area scans allocate no managed memory.");

        terrain.Changed += _ => throw new InvalidOperationException("Earlier user listener failed.");
        Reject<InvalidOperationException>(() => terrain.Segments = [new(200, 0), new(200, 40)]);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y > 100 && body.GetContactCount() == 0,
            "Live pair edits rebuild fixtures even after a user Changed listener throws.");
    }

    private static void VerifyPackedResource()
    {
        using var contour = new ConcavePolygonShape
        {
            Segments = [new(-40, 0), new(0, -20), new(0, -20), new(40, 0)]
        };
        using var root = new Node { Name = "Root" };
        var body = new StaticBody { Name = "Body" };
        var collision = new CollisionShape { Name = "Collision", Shape = contour };
        root.AddChild(body); body.AddChild(collision);
        body.Owner = root; collision.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root);
        using var copy = packed.Instantiate();
        Check(ReferenceEquals(copy.GetNode<CollisionShape>("Body/Collision").Shape, contour) &&
              contour.GetRect() == new Rect2(-40, -20, 80, 20),
            "PackedScene retains the borrowed concave contour and its exact bounds.");
    }

    private static void VerifyDynamicConcaveMass()
    {
        using var contour = new ConcavePolygonShape
        {
            Segments = [new(-20, 0), new(20, 0), new(20, 0), new(20, -20)]
        };
        var root = new Node();
        var body = new RigidBody { Mass = 2 };
        body.AddChild(new CollisionShape { Shape = contour });
        root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y > 100,
            "A dynamic body with several zero-area pairs retains mass and responds to gravity.");
        body.GravityScale = 0;
        body.ApplyTorqueImpulse(100);
        tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.AngularVelocity) > 0.01f,
            "Paired segments contribute finite rotational inertia when the body is dynamic.");
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
