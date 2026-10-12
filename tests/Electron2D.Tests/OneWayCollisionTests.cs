using Electron2D;

internal sealed class OneWayCollisionTests(PhysicsServer.Backend backend)
{
    internal static void Run(PhysicsServer.Backend backend = PhysicsServer.Backend.CPU) => new OneWayCollisionTests(backend).RunCore();
    private void RunCore()
    {
        _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
        VerifyPropertiesAndPacking();
        VerifyApproachSidesAndLiveChanges();
        VerifyRotatedDirection();
        VerifyKinematicPlatform();
        VerifyDynamicPlatform();
        VerifyAreaIgnoresOneWay();
        Console.WriteLine($"One-way body contacts, direction, packing, Area and 0/0 warm owner/all-thread byte checks passed on {backend}.");
    }

    private void VerifyPropertiesAndPacking()
    {
        using var segment = new SegmentShape { A = new(-100, 0), B = new(100, 0) };
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld };
        var wall = new StaticBody { Name = "Wall" };
        var collision = new CollisionShape { Name = "Collision", Shape = segment };
        root.AddChild(wall); wall.AddChild(collision);
        wall.Owner = root; collision.Owner = root;
        Check(!collision.OneWayCollision && collision.OneWayCollisionDirection == Vector2.Down,
            "The one-way flag is disabled and points down by default.");
        collision.OneWayCollisionDirection = new(300, 400);
        Check((collision.OneWayCollisionDirection - new Vector2(0.6f, 0.8f)).Length() < 1e-6f,
            "The assigned direction is normalized.");
        collision.OneWayCollisionDirection = new(float.MaxValue, float.MaxValue);
        Check(collision.OneWayCollisionDirection.IsFinite() && collision.OneWayCollisionDirection.Length() > 0.99f,
            "Large finite directions remain normalized.");
        var previous = collision.OneWayCollisionDirection;
        Reject<ArgumentOutOfRangeException>(() => collision.OneWayCollisionDirection = new(float.NaN, 1));
        Check(collision.OneWayCollisionDirection == previous, "Invalid direction leaves state unchanged.");
        collision.OneWayCollision = true;
        using var packed = new PackedScene(); packed.Pack(root);
        using var copy = packed.Instantiate();
        var restored = copy.GetNode<CollisionShape>("Wall/Collision");
        Check(restored.OneWayCollision && restored.OneWayCollisionDirection == previous &&
              ReferenceEquals(restored.Shape, segment), "PackedScene retains one-way state and borrowed geometry.");
    }

    private void VerifyApproachSidesAndLiveChanges()
    {
        using var platformShape = new SegmentShape { A = new(-100, 0), B = new(100, 0) };
        using var bodyShape = new CircleShape();
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld };
        var platform = new StaticBody { Position = new(0, 100) };
        var collision = new CollisionShape { Shape = platformShape, OneWayCollision = true };
        platform.AddChild(collision);
        var body = new RigidBody
        {
            Position = new(0, 120),
            GravityScale = 0,
            CanSleep = false,
            LinearVelocity = new(0, -80),
            ContactMonitor = true,
            MaxContactsReported = 8
        };
        body.AddChild(new CollisionShape { Shape = bodyShape });
        root.AddChild(platform); root.AddChild(body);
        using var tree = new SceneTree(root); Check(PhysicsServer.SpaceGetBackend(selectedWorld.Space) == backend, "One-way fixture uses the explicitly selected backend.");
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y < 60 && body.GetContactCount() == 0 && body.GetCollidingBodies().Length == 0,
            "A body travelling upward crosses from below without a reported contact.");

        body.GlobalPosition = new(0, 80);
        body.LinearVelocity = new(0, 80);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 86 and < 94 && MathF.Abs(body.LinearVelocity.Y) < 2 &&
              body.GetContactCount() > 0 && body.GetCollidingBodies().Contains(platform),
            "The same platform stops a body travelling downward from above.");

        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread(); var allBefore = GC.GetTotalAllocatedBytes(true);
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - before; var allBytes = GC.GetTotalAllocatedBytes(true) - allBefore;
        Check(ownerBytes == 0 && allBytes == 0, $"Warm active one-way contacts allocated {ownerBytes}/{allBytes} owner/all-thread bytes.");

        collision.OneWayCollision = false;
        body.GlobalPosition = new(0, 120);
        body.LinearVelocity = new(0, -80);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 106 and < 114,
            "Disabling one-way response makes the same segment two-sided on the next step.");

        collision.OneWayCollision = true;
        collision.OneWayCollisionDirection = Vector2.Zero;
        body.GlobalPosition = new(0, 80);
        body.LinearVelocity = new(0, 80);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y > 120,
            "A zero one-way direction selects no contact side.");
    }

    private void VerifyRotatedDirection()
    {
        using var wallShape = new SegmentShape { A = new(-100, 0), B = new(100, 0) };
        using var bodyShape = new CircleShape();
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld };
        var wall = new StaticBody { Position = new(100, 0) };
        var collision = new CollisionShape { Shape = wallShape, Rotation = Mathf.Pi / 2, OneWayCollision = true };
        wall.AddChild(collision);
        var body = new RigidBody { Position = new(80, 0), GravityScale = 0, CanSleep = false, LinearVelocity = new(80, 0) };
        body.AddChild(new CollisionShape { Shape = bodyShape });
        root.AddChild(wall); root.AddChild(body);
        using var tree = new SceneTree(root); Check(PhysicsServer.SpaceGetBackend(selectedWorld.Space) == backend, "One-way fixture uses the explicitly selected backend.");
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.X > 140, "Rotating the child turns the pass-through direction with the wall.");
        body.GlobalPosition = new(120, 0);
        body.LinearVelocity = new(-80, 0);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.X is > 106 and < 114,
            "The rotated wall blocks approach from its solid side.");

        collision.OneWayCollisionDirection = Vector2.Up;
        body.GlobalPosition = new(80, 0);
        body.LinearVelocity = new(80, 0);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.X is > 86 and < 94,
            "Changing the local direction reverses the solid side on the next step.");

        collision.Rotation = 0;
        collision.OneWayCollisionDirection = Vector2.Down;
        wall.Rotation = Mathf.Pi / 2;
        body.GlobalPosition = new(80, 0);
        body.LinearVelocity = new(80, 0);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.X > 140,
            "Rotating the owning body turns the pass-through direction too.");
    }

    private void VerifyAreaIgnoresOneWay()
    {
        using var sensorShape = new SegmentShape { A = new(-100, 0), B = new(100, 0) };
        using var bodyShape = new CircleShape();
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld };
        var area = new Area { Position = new(0, 100) };
        var collision = new CollisionShape { Shape = sensorShape, OneWayCollision = true };
        area.AddChild(collision);
        var body = new RigidBody { Position = new(0, 120), GravityScale = 0, CanSleep = false, LinearVelocity = new(0, -80) };
        body.AddChild(new CollisionShape { Shape = bodyShape });
        root.AddChild(area); root.AddChild(body);
        Check(collision.GetConfigurationWarnings().Any(warning => warning.Contains("no effect", StringComparison.Ordinal)),
            "An Area child reports that one-way contact response is inapplicable to a sensor.");
        using var tree = new SceneTree(root); Check(PhysicsServer.SpaceGetBackend(selectedWorld.Space) == backend, "One-way fixture uses the explicitly selected backend.");
        var entered = 0;
        area.BodyEntered += _ => entered++;
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(entered == 1 && body.GlobalPosition.Y < 60,
            "An Area still senses a body crossing from the pass-through side without collision response.");
    }

    private void VerifyKinematicPlatform()
    {
        using var platformShape = new SegmentShape { A = new(-100, 0), B = new(100, 0) };
        using var bodyShape = new CircleShape();
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld };
        var platform = new AnimatableBody { Position = new(0, 100) };
        platform.AddChild(new CollisionShape { Shape = platformShape, OneWayCollision = true });
        var body = new RigidBody { Position = new(0, 120), GravityScale = 0, CanSleep = false, LinearVelocity = new(0, -80) };
        body.AddChild(new CollisionShape { Shape = bodyShape });
        root.AddChild(platform); root.AddChild(body);
        using var tree = new SceneTree(root); Check(PhysicsServer.SpaceGetBackend(selectedWorld.Space) == backend, "One-way fixture uses the explicitly selected backend.");
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y < 60, "A kinematic platform lets a body pass upward from below.");
        body.GlobalPosition = new(0, 80);
        body.LinearVelocity = new(0, 80);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 86 and < 94,
            "The same kinematic platform stops approach from above.");
    }

    private void VerifyDynamicPlatform()
    {
        using var platformShape = new SegmentShape { A = new(-100, 0), B = new(100, 0) };
        using var bodyShape = new CircleShape();
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld };
        var platform = new RigidBody
        {
            Name = "Platform",
            Position = new(0, 100),
            GravityScale = 0,
            Mass = 1000,
            CanSleep = false,
            LockRotation = true
        };
        platform.AddChild(new CollisionShape { Shape = platformShape, OneWayCollision = true });
        var body = new RigidBody { Name = "Rider", Position = new(0, 80), GravityScale = 0, CanSleep = false, LinearVelocity = new(0, 80) };
        body.AddChild(new CollisionShape { Shape = bodyShape });
        root.AddChild(platform); root.AddChild(body);
        using var tree = new SceneTree(root); Check(PhysicsServer.SpaceGetBackend(selectedWorld.Space) == backend, "One-way fixture uses the explicitly selected backend.");
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y is > 86 and < 96,
            "A dynamic one-way owner resolves contact from its selected side.");
        platform.GlobalPosition = new(0, 100);
        platform.LinearVelocity = Vector2.Zero;
        body.GlobalPosition = new(0, 120);
        body.LinearVelocity = new(0, -80);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y < 60,
            "A dynamic one-way owner also permits crossing from below.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}.");
    }
}
