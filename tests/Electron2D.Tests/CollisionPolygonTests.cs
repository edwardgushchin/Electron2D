using Electron2D;

internal static class CollisionPolygonTests
{
    internal static void Run()
    {
        VerifyDefaultsAndValidation();
        VerifyBodyAndLiveGeometry();
        VerifyDynamicBody();
        VerifyConcaveAreaAndHollowMode();
        VerifyOneWayAndPacking();
        Console.WriteLine("Collision polygon solids, hollow edges, one-way contact, packing and allocation passed.");
    }

    private static void VerifyDefaultsAndValidation()
    {
        using var polygon = new CollisionPolygon();
        Check(polygon.BuildMode == CollisionPolygonBuildMode.Solids && polygon.Polygon.Length == 0 &&
              !polygon.Disabled && !polygon.OneWayCollision && polygon.OneWayCollisionDirection == Vector2.Down,
            "A detached polygon defaults to empty solid geometry without one-way contact.");
        Check(polygon.GetConfigurationWarnings().Length >= 2,
            "A detached empty polygon reports its missing parent and contour.");
        Vector2[] square = [new(0, 0), new(40, 0), new(40, 40), new(0, 40)];
        polygon.Polygon = square;
        square[0] = new(99, 99);
        var read = polygon.Polygon;
        read[1] = new(99, 99);
        Check(polygon.Polygon[0] == Vector2.Zero && polygon.Polygon[1] == new Vector2(40, 0),
            "The node owns a copy of its local contour.");
        Reject<ArgumentNullException>(() => polygon.Polygon = null!);
        Reject<ArgumentException>(() => polygon.Polygon = [new(float.NaN, 0)]);
        Reject<ArgumentException>(() => polygon.Polygon = [new(-float.MaxValue, 0), new(float.MaxValue, 0)]);
        Reject<ArgumentOutOfRangeException>(() => polygon.BuildMode = (CollisionPolygonBuildMode)7);
        Check(polygon.Polygon.Length == 4 && polygon.BuildMode == CollisionPolygonBuildMode.Solids,
            "Invalid input rejects without replacing a valid contour or mode.");

        polygon.Polygon = [new(0, 0), new(40, 40), new(0, 40), new(40, 0)];
        Check(polygon.Polygon.Length == 4 && polygon.GetConfigurationWarnings().Any(w => w.Contains("cannot produce", StringComparison.Ordinal)),
            "A self-crossing solid contour remains editable but has no collision fixtures.");
        polygon.BuildMode = CollisionPolygonBuildMode.Segments;
        Check(!polygon.GetConfigurationWarnings().Any(w => w.Contains("cannot produce", StringComparison.Ordinal)),
            "The same contour can still supply hollow edges.");
        polygon.OneWayCollisionDirection = new(300, 400);
        Check((polygon.OneWayCollisionDirection - new Vector2(0.6f, 0.8f)).Length() < 1e-6f,
            "One-way direction is normalized.");
        var direction = polygon.OneWayCollisionDirection;
        Reject<ArgumentOutOfRangeException>(() => polygon.OneWayCollisionDirection = new(float.PositiveInfinity, 0));
        Check(polygon.OneWayCollisionDirection == direction, "Invalid one-way direction preserves state.");
    }

    private static void VerifyBodyAndLiveGeometry()
    {
        using var circle = new CircleShape();
        using var spare = new RectangleShape { Size = new(10, 10) };
        var root = new Node();
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Name = "Spare", Shape = spare, Position = new(500, 0) });
        var polygon = new CollisionPolygon
        {
            Name = "Contour",
            Polygon = [new(-100, -10), new(100, -10), new(100, 10), new(-100, 10)]
        };
        floor.AddChild(polygon);
        var body = new RigidBody
        {
            Name = "Body",
            CanSleep = false,
            ContactMonitor = true,
            MaxContactsReported = 4
        };
        body.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(floor); root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 76 and < 84 && body.GetContactCount() > 0 &&
              body.GetCollidingBodies().Contains(floor),
            "A direct polygon owner and a sibling CollisionShape both supply body fixtures.");

        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed solid polygon contact allocates no managed memory on the stepping thread.");

        polygon.Scale = new(2, 1);
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
        polygon.Scale = Vector2.One;
        tree.PhysicsFrame(1d / 60);
        Check(body.GetContactCount() > 0,
            "A rejected polygon transform leaves prior fixtures usable after correction.");

        polygon.Disabled = true;
        for (var frame = 0; frame < 45; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y > 120, "Disabling the polygon removes its fixtures on the next step.");
        polygon.Disabled = false;
        tree.EditedSceneRoot = root;
        void FailWarning(SceneTree _, Node node)
        {
            if (ReferenceEquals(node, polygon)) throw new InvalidOperationException("Observer failed.");
        }
        tree.NodeConfigurationWarningChanged += FailWarning;
        Reject<InvalidOperationException>(() => polygon.Polygon =
            [new(200, -10), new(400, -10), new(400, 10), new(200, 10)]);
        tree.NodeConfigurationWarningChanged -= FailWarning;
        body.GlobalPosition = Vector2.Zero;
        body.LinearVelocity = Vector2.Zero;
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y > 120,
            "Changing vertices rebuilds only the polygon's fixtures even after a warning observer throws.");

        polygon.Polygon = [new(-100, -10), new(100, -10), new(100, 10), new(-100, 10)];
        floor.RemoveChild(polygon);
        body.GlobalPosition = Vector2.Zero;
        body.LinearVelocity = Vector2.Zero;
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y > 120, "Removing the child unregisters its direct geometry slot.");
        floor.AddChild(polygon);
        body.GlobalPosition = Vector2.Zero;
        body.LinearVelocity = Vector2.Zero;
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 76 and < 84,
            "Reentering the owner restores the polygon's live fixtures.");
    }

    private static void VerifyDynamicBody()
    {
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorShape });
        var body = new RigidBody { CanSleep = false, Mass = 2 };
        var polygon = new CollisionPolygon
        {
            Polygon = [new(-10, -10), new(10, -10), new(10, 10), new(-10, 10)]
        };
        body.AddChild(polygon);
        root.AddChild(floor); root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 76 and < 84,
            "An owned solid polygon contributes mass-bearing fixtures to a dynamic body.");
        polygon.BuildMode = CollisionPolygonBuildMode.Segments;
        body.GravityScale = 0;
        body.GlobalPosition = Vector2.Zero;
        body.LinearVelocity = Vector2.Zero;
        body.ApplyTorqueImpulse(100);
        tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.AngularVelocity) > 0.01f,
            "An owned hollow polygon retains positive body mass and responds to torque.");
    }

    private static void VerifyConcaveAreaAndHollowMode()
    {
        using var circle = new CircleShape { Radius = 5 };
        var root = new Node();
        var area = new Area();
        var polygon = new CollisionPolygon
        {
            Polygon = [new(0, 0), new(100, 0), new(100, 20), new(20, 20), new(20, 100), new(0, 100)]
        };
        area.AddChild(polygon);
        var body = new RigidBody { Position = new(10, 60), GravityScale = 0, CanSleep = false };
        body.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(area); root.AddChild(body);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        Check(area.GetOverlappingBodies().Contains(body),
            "Convex parts of an L contour detect a body in its vertical arm.");
        body.GlobalPosition = new(60, 60);
        tree.PhysicsFrame(1d / 60);
        Check(!area.GetOverlappingBodies().Contains(body),
            "The missing corner of a concave solid polygon has no sensor fixture.");

        polygon.BuildMode = CollisionPolygonBuildMode.Segments;
        body.GlobalPosition = new(10, 60);
        tree.PhysicsFrame(1d / 60);
        Check(!area.GetOverlappingBodies().Contains(body),
            "The segment mode is hollow even when it closes a concave contour.");
        body.GlobalPosition = new(2, 60);
        tree.PhysicsFrame(1d / 60);
        Check(area.GetOverlappingBodies().Contains(body),
            "Touching an edge of the hollow contour activates the sensor.");
        polygon.OneWayCollision = true;
        Check(polygon.GetConfigurationWarnings().Any(w => w.Contains("no effect", StringComparison.Ordinal)),
            "An Area child warns that one-way body response is inapplicable to its sensor.");
        body.GlobalPosition = new(-2, 60);
        tree.PhysicsFrame(1d / 60);
        Check(area.GetOverlappingBodies().Contains(body),
            "Area sensing still detects the edge from the other side.");
    }

    private static void VerifyOneWayAndPacking()
    {
        using var circle = new CircleShape();
        var root = new Node();
        var wall = new StaticBody { Name = "Wall", Position = new(0, 100) };
        var polygon = new CollisionPolygon
        {
            Name = "Contour",
            BuildMode = CollisionPolygonBuildMode.Segments,
            Polygon = [new(-100, 0), new(100, 0)],
            OneWayCollision = true
        };
        wall.AddChild(polygon); root.AddChild(wall);
        wall.Owner = root; polygon.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root);
        using var copy = packed.Instantiate();
        var restored = copy.GetNode<CollisionPolygon>("Wall/Contour");
        polygon.Polygon = [];
        Check(restored.BuildMode == CollisionPolygonBuildMode.Segments && restored.Polygon.Length == 2 &&
              restored.OneWayCollision && restored.OneWayCollisionDirection == Vector2.Down && !restored.Disabled,
            "PackedScene restores the exact polygon node and copied physics properties.");

        var body = new RigidBody { Position = new(0, 120), GravityScale = 0, CanSleep = false, LinearVelocity = new(0, -80) };
        body.AddChild(new CollisionShape { Shape = circle });
        copy.AddChild(body);
        using var tree = new SceneTree(copy);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y < 60,
            "A restored segment polygon allows traversal from its pass-through side.");
        body.GlobalPosition = new(0, 80);
        body.LinearVelocity = new(0, 80);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 86 and < 94,
            "The restored polygon blocks contact from its solid side.");
        restored.OneWayCollisionDirection = Vector2.Up;
        body.GlobalPosition = new(0, 120);
        body.LinearVelocity = new(0, -80);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 106 and < 114,
            "Changing a polygon's one-way direction reverses its solid side on the next step.");
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
