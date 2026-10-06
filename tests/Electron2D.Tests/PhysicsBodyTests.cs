using Electron2D;

internal static class PhysicsBodyTests
{
    internal static void Run()
    {
        Box2DSIMDTests.Run();
        PhysicsParallelTests.Run();
        VerifyDefaultsAndValidation();
        VerifyCircleAndShapeChanges();
        VerifyPackedPhysicsNodes();
        VerifyMotionSettingsAndRecovery();
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        using var boxShape = new RectangleShape { Size = new(20, 20) };
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorShape });
        var body = new RigidBody();
        body.AddChild(new CollisionShape { Shape = boxShape });
        root.AddChild(floor); root.AddChild(body);

        using (var tree = new SceneTree(root))
        {
            for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
            Check(body.GlobalPosition.Y > 75 && body.GlobalPosition.Y < 85,
                "A dynamic rectangle falls and rests on the static floor.");
            Check(MathF.Abs(body.LinearVelocity.Y) < 2,
                "Contact resolution stops the falling body.");
            body.Mass = 1e-30f;
            Reject<ArgumentOutOfRangeException>(() => body.Mass = float.MaxValue);
            Check(body.Mass == 1e-30f, "An unrepresentable geometry-derived inertia leaves the previous body state intact.");
            body.Mass = 1;
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            Check(allocated == 0, "A warmed resting physics frame allocates no managed memory.");

            body.CanSleep = false;
            for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
            allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
            Check(GC.GetAllocatedBytesForCurrentThread() - allocatedBefore == 0,
                "A warmed active contact frame allocates no managed memory.");
            body.CanSleep = true;

            body.ApplyCentralImpulse(new(0, -200));
            tree.PhysicsFrame(1d / 60);
            Check(body.GlobalPosition.Y < 78 && body.LinearVelocity.Y < 0,
                "A central impulse moves the attached body upward.");
            for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
            Check(body.GlobalPosition.Y > 75 && body.GlobalPosition.Y < 85,
                "The body returns to the floor after the impulse.");

            floor.CollisionLayer = 0;
            for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
            Check(body.GlobalPosition.Y > 100,
                "Clearing the floor collision layer removes its response on the next step.");
            allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            Check(allocated == 0,
                "A warmed moving-body physics frame allocates no managed memory.");
        }

        Check(body.IsDisposed && floor.IsDisposed && !floorShape.IsDisposed && !boxShape.IsDisposed,
            "The tree owns bodies, while collision-shape resources remain caller-owned.");
        Console.WriteLine("Physics body gravity, collision, impulse, filtering and lifetime checks passed.");
    }

    private static void VerifyDefaultsAndValidation()
    {
        using var rectangle = new RectangleShape();
        using var circle = new CircleShape();
        Check(rectangle.Size == new Vector2(20, 20) && rectangle.GetRect() == new Rect2(-10, -10, 20, 20) &&
              circle.Radius == 10 && circle.GetRect() == new Rect2(-10, -10, 20, 20),
            "Rectangle and circle resources retain their pinned dimensions and local bounds.");
        Reject<ArgumentOutOfRangeException>(() => rectangle.Size = new(0, 20));
        Reject<ArgumentOutOfRangeException>(() => rectangle.Size = new(float.NaN, 20));
        Reject<ArgumentOutOfRangeException>(() => circle.Radius = -1);
        Reject<ArgumentOutOfRangeException>(() => circle.Radius = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => circle.Radius = float.MaxValue);
        using var duplicate = (CircleShape)circle.Duplicate();
        Check(duplicate.Radius == circle.Radius && !ReferenceEquals(duplicate, circle),
            "Shape duplication owns independent typed geometry.");

        using var body = new RigidBody();
        Check(body.Mass == 1 && body.GravityScale == 1 && body.LinearVelocity == Vector2.Zero &&
              body.AngularVelocity == 0 && body.CanSleep && !body.Sleeping && !body.Freeze &&
              body.CollisionLayer == 1 && body.CollisionMask == 1,
            "Detached body defaults preserve the initial dynamic state and layer one.");
        body.SetCollisionLayerValue(32, true);
        body.SetCollisionMaskValue(1, false);
        Check(body.GetCollisionLayerValue(32) && !body.GetCollisionMaskValue(1),
            "Layer and mask bit helpers address the complete one-based 32-bit range.");
        Reject<ArgumentOutOfRangeException>(() => body.GetCollisionLayerValue(0));
        Reject<ArgumentOutOfRangeException>(() => body.SetCollisionMaskValue(33, true));
        Reject<ArgumentOutOfRangeException>(() => body.Mass = 0);
        body.LinearDamp = -1;
        Check(body.LinearDamp == -1, "Signed damping remains available for the area/body field contract.");
        body.LinearDamp = 0;
        Reject<ArgumentOutOfRangeException>(() => body.GravityScale = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => body.LinearVelocity = new(float.NaN, 0));
        Reject<InvalidOperationException>(() => body.ApplyCentralImpulse(new(0, 1)));
        Check(body.Mass == 1 && body.GravityScale == 1 && body.LinearVelocity == Vector2.Zero,
            "Invalid detached writes do not alter body state.");
        using var unattached = new CollisionShape();
        Check(unattached.GetConfigurationWarnings().Length >= 2,
            "A collision shape without a body or resource reports both missing prerequisites.");
    }

    private static void VerifyCircleAndShapeChanges()
    {
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        using var circleShape = new CircleShape();
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorShape });
        var body = new RigidBody { CanSleep = false };
        var collider = new CollisionShape { Shape = circleShape };
        body.AddChild(collider);
        root.AddChild(floor); root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y > 75 && body.GlobalPosition.Y < 85,
            "A dynamic circle rests on a rectangular static floor.");
        circleShape.Radius = 20;
        body.Sleeping = false;
        for (var frame = 0; frame < 90; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y > 65 && body.GlobalPosition.Y < 75,
            "Changing a borrowed shape resource rebuilds contact geometry before the next step.");
        collider.Disabled = true;
        body.Sleeping = false;
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.Y > 100,
            "Disabling a child collision shape removes its fixture while the body keeps moving.");
    }

    private static void VerifyPackedPhysicsNodes()
    {
        using var shape = new RectangleShape { Size = new(24, 12) };
        using var root = new Node { Name = "PhysicsRoot" };
        var body = new RigidBody { Name = "Body", Mass = 2, GravityScale = 0.5f };
        var collider = new CollisionShape { Name = "Collider", Shape = shape };
        root.AddChild(body); body.AddChild(collider);
        body.Owner = root; collider.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root);
        using var copy = packed.Instantiate();
        var copiedBody = copy.GetNode<RigidBody>("Body");
        var copiedCollider = copy.GetNode<CollisionShape>("Body/Collider");
        Check(copiedBody.Mass == 2 && copiedBody.GravityScale == 0.5f &&
              ReferenceEquals(copiedCollider.Shape, shape),
            "Packed scenes restore the exact physics node roles and borrowed shape resource.");
    }

    private static void VerifyMotionSettingsAndRecovery()
    {
        var root = new Node();
        var body = new RigidBody
        {
            GravityScale = 0,
            LinearVelocity = new(60, 0),
            CanSleep = false,
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace
        };
        root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.GlobalPosition.X - 60) < 1 && MathF.Abs(body.GlobalPosition.Y) < 0.01f,
            "Zero gravity and an initial velocity move an attached body by scene units per second.");

        body.Freeze = true;
        var frozen = body.GlobalPosition;
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition == frozen, "A frozen body holds its position across fixed steps.");
        body.Freeze = false;
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.X > frozen.X + 55,
            "Unfreezing restores dynamic motion and the stored velocity.");

        body.LockRotation = true;
        body.AngularVelocity = 2;
        var rotation = body.GlobalRotation;
        for (var frame = 0; frame < 30; frame++) tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.GlobalRotation - rotation) < 0.001f,
            "Rotation locking constrains angular motion.");
        body.LockRotation = false;
        body.AngularVelocity = 2;
        for (var frame = 0; frame < 30; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GlobalRotation > rotation + 0.5f,
            "Unlocking restores angular motion.");

        body.Scale = new(2, 1);
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
        body.Scale = Vector2.One;
        var priorX = body.GlobalPosition.X;
        tree.PhysicsFrame(1d / 60);
        Check(body.GlobalPosition.X > priorX,
            "A rejected non-unit physics transform leaves the world reusable after correction.");
        Reject<InvalidOperationException>(() => Task.Run(() => body.Mass = 2).GetAwaiter().GetResult());

        root.RemoveChild(body);
        Reject<InvalidOperationException>(() => body.ApplyCentralForce(new(0, 1)));
        root.AddChild(body);
        tree.PhysicsFrame(1d / 60);
        Check(body.Tree == tree, "A detached body can re-enter its scene physics world.");
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
