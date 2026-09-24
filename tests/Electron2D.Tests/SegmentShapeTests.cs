using Electron2D;

internal static class SegmentShapeTests
{
    internal static void Run()
    {
        VerifyEndpointsAndCopy();
        VerifyBodyAndAreaFixtures();
        VerifyRotatedAndTwoSidedResponse();
        VerifyDynamicSegment();
        VerifyMultipleSegmentMass();
        VerifyUnshapedBodyMass();
        VerifyPackedResource();
        Console.WriteLine("Segment shape terrain, two-sided contact, area and allocation checks passed.");
    }

    private static void VerifyRotatedAndTwoSidedResponse()
    {
        using var segment = new SegmentShape { A = new(-100, 0), B = new(100, 0) };
        using var circle = new CircleShape();
        var root = new Node();
        var wall = new StaticBody { Position = new(0, 100) };
        var wallCollision = new CollisionShape { Shape = segment };
        wall.AddChild(wallCollision);
        var body = new RigidBody
        {
            Position = new(0, 120),
            GravityScale = 0,
            LinearVelocity = new(0, -80),
            CanSleep = false
        };
        body.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(wall); root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 107 and < 114,
            "The segment also blocks a circle approaching from below.");

        wallCollision.Rotation = Mathf.Pi * 0.5f;
        body.GlobalPosition = new(-30, 100);
        body.LinearVelocity = new(80, 0);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.X is > -14 and < -7,
            "Rotating the collision child turns the segment into a two-sided vertical wall.");
    }

    private static void VerifyDynamicSegment()
    {
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        using var bodyShape = new SegmentShape { A = new(-20, 0), B = new(20, 0) };
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorShape });
        var body = new RigidBody { CanSleep = false };
        body.AddChild(new CollisionShape { Shape = bodyShape });
        root.AddChild(floor); root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 86 and < 94 && MathF.Abs(body.LinearVelocity.Y) < 2,
            "A dynamic body with a segment fixture collides with the static floor.");
        body.GravityScale = 0;
        body.GlobalPosition = Vector2.Zero;
        body.LinearVelocity = Vector2.Zero;
        body.ApplyTorqueImpulse(100);
        tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.AngularVelocity) > 0.1f,
            "A dynamic segment receives finite rod inertia and responds to torque.");
    }

    private static void VerifyUnshapedBodyMass()
    {
        var root = new Node();
        var body = new RigidBody();
        Reject<ArgumentOutOfRangeException>(() => body.Mass = 1e-40f);
        Check(body.Mass == 1, "A detached body also rejects a mass below the solver range.");
        root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y > 100,
            "An unshaped dynamic body retains its requested mass and falls without collision geometry.");
    }

    private static void VerifyMultipleSegmentMass()
    {
        using var segment = new SegmentShape { A = new(-20, 0), B = new(20, 0) };
        var root = new Node();
        var body = new RigidBody { GravityScale = 0, Mass = 2 };
        body.AddChild(new CollisionShape { Name = "Left", Position = new(-10, 0), Shape = segment });
        body.AddChild(new CollisionShape { Name = "Right", Position = new(10, 0), Shape = segment });
        root.AddChild(body);
        using var tree = new SceneTree(root);
        body.ApplyTorqueImpulse(100);
        tree.PhysicsFrame(1d / 60);
        Check(body.AngularVelocity is > 0.17f and < 0.25f,
            "Multiple segment fixtures distribute the requested mass and inertia by length and offset.");
        Reject<ArgumentOutOfRangeException>(() => body.Mass = 1e-40f);
        Check(body.Mass == 2, "A mass below the solver's reciprocal range rejects before mutation.");
    }

    private static void VerifyEndpointsAndCopy()
    {
        using var shape = new SegmentShape();
        var changes = 0;
        shape.Changed += _ => changes++;
        Check(shape.A == Vector2.Zero && shape.B == new Vector2(0, 10) &&
              shape.GetRect() == new Rect2(0, 0, 0, 10),
            "The segment defaults to a vertical ten-unit span with exact bounds.");
        shape.A = new(10, 20);
        shape.B = new(-10, -20);
        Check(shape.GetRect() == new Rect2(-10, -20, 20, 40) && changes == 2,
            "Reversed endpoints produce normalized local bounds and publish changes.");
        shape.A = new(10, 20); shape.B = new(-10, -20);
        Check(changes == 2, "Equal endpoint assignments do not publish changes.");
        using var copy = (SegmentShape)shape.Duplicate();
        shape.B = new(10, 20);
        Check(shape.GetRect() == new Rect2(10, 20, 0, 0) && copy.B == new Vector2(-10, -20),
            "Coincident endpoints retain point bounds; duplication owns independent endpoint state.");
        Reject<ArgumentOutOfRangeException>(() => shape.A = new(float.NaN, 0));
        Reject<ArgumentOutOfRangeException>(() => shape.B = new(float.PositiveInfinity, 0));
        Check(shape.A == new Vector2(10, 20) && shape.B == new Vector2(10, 20),
            "Invalid endpoints reject before mutation.");
        using var extreme = new SegmentShape { B = new(float.MaxValue / 2, 0) };
        Reject<ArgumentOutOfRangeException>(() => extreme.A = new(-float.MaxValue, 0));
        Check(extreme.A == Vector2.Zero, "Overflowing bounds reject before mutation.");
        copy.Dispose();
        Reject<ObjectDisposedException>(() => _ = copy.GetRect());
        Reject<ObjectDisposedException>(() => copy.A = Vector2.Zero);
    }

    private static void VerifyBodyAndAreaFixtures()
    {
        using var floorShape = new SegmentShape { A = new(-100, 0), B = new(100, 0) };
        using var bodyShape = new CircleShape();
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorShape });
        var body = new RigidBody { CanSleep = false, ContactMonitor = true, MaxContactsReported = 2 };
        body.AddChild(new CollisionShape { Shape = bodyShape });
        root.AddChild(floor); root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 87 and < 93 && body.GetContactCount() > 0 &&
              body.GetCollidingBodies().Contains(floor),
            "A rigid circle rests on a two-sided static segment fixture.");

        floorShape.Changed += _ => throw new InvalidOperationException("User subscriber failed.");
        Reject<InvalidOperationException>(() => floorShape.A = new(200, 0));
        Reject<InvalidOperationException>(() => floorShape.B = new(200, 0));
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y > 100 && body.GetContactCount() == 0,
            "A live segment edit removes the floor even when a prior Changed subscriber throws.");
        Reject<InvalidOperationException>(() => floorShape.A = new(-100, 0));
        Reject<InvalidOperationException>(() => floorShape.B = new(100, 0));
        body.GlobalPosition = new(0, 0);
        body.LinearVelocity = Vector2.Zero;
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 87 and < 93,
            "Restoring endpoints rebuilds the floor fixture for new contacts.");

        using var sensorShape = new SegmentShape { A = new(-20, 0), B = new(20, 0) };
        var area = new Area { Position = new(0, 90) };
        area.AddChild(new CollisionShape { Shape = sensorShape });
        var entered = 0; var exited = 0;
        area.BodyEntered += other => { if (ReferenceEquals(other, body)) entered++; };
        area.BodyExited += other => { if (ReferenceEquals(other, body)) exited++; };
        root.AddChild(area);
        tree.PhysicsFrame(1d / 60);
        Check(entered == 1 && area.GetOverlappingBodies().Contains(body),
            "A line segment can serve as an area sensor across a rigid body.");
        sensorShape.A = new(200, 0);
        sensorShape.B = new(200, 0);
        tree.PhysicsFrame(1d / 60);
        Check(exited == 1 && !area.GetOverlappingBodies().Contains(body),
            "A coincident-point segment keeps a valid fixture and ends the overlap.");

        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed segment contact and area scans allocate no managed memory.");
    }

    private static void VerifyPackedResource()
    {
        using var shape = new SegmentShape { A = new(-20, 4), B = new(30, -8) };
        using var root = new Node { Name = "Root" };
        var body = new StaticBody { Name = "Body" };
        var collision = new CollisionShape { Name = "Collision", Shape = shape };
        root.AddChild(body); body.AddChild(collision);
        body.Owner = root; collision.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root);
        using var copy = packed.Instantiate();
        Check(ReferenceEquals(copy.GetNode<CollisionShape>("Body/Collision").Shape, shape) &&
              shape.GetRect() == new Rect2(-20, -8, 50, 12),
            "PackedScene preserves the borrowed segment and its endpoint bounds.");
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
