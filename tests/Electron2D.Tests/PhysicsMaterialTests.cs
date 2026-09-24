using Electron2D;

internal static class PhysicsMaterialTests
{
    internal static void Run()
    {
        using var material = new PhysicsMaterial();
        Check(material.Friction == 1 && material.Bounce == 0 && !material.Rough && !material.Absorbent,
            "Physics material defaults match the reference surface.");
        var changes = 0;
        material.Changed += _ => changes++;
        material.Friction = 0.25f;
        material.Bounce = 0.8f;
        material.Rough = true;
        material.Absorbent = true;
        Check(changes == 4, "Every material property emits change notification.");
        Reject<ArgumentOutOfRangeException>(() => material.Friction = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => material.Bounce = float.PositiveInfinity);
        Check(changes == 4 && material.Friction == 0.25f && material.Bounce == 0.8f,
            "Rejected nonfinite material edits preserve state and do not notify.");
        using var copy = (PhysicsMaterial)material.Duplicate();
        Check(copy.Friction == 0.25f && copy.Bounce == 0.8f && copy.Rough && copy.Absorbent,
            "Material duplication retains independent surface settings.");

        using (var geometry = new RectangleShape())
        using (var root = new Node { Name = "Root" })
        {
            var body = new StaticBody { Name = "Floor", PhysicsMaterialOverride = material };
            body.AddChild(new CollisionShape { Shape = geometry, Name = "Collider" });
            root.AddChild(body);
            body.Owner = root;
            body.GetNode<CollisionShape>("Collider").Owner = root;
            using var packed = new PackedScene();
            packed.Pack(root);
            using var instance = packed.Instantiate();
            Check(ReferenceEquals(instance.GetNode<StaticBody>("Floor").PhysicsMaterialOverride, material),
                "PackedScene retains borrowed material identity.");
            material.ResourceLocalToScene = true;
            using var localInstance = packed.Instantiate();
            var localMaterial = localInstance.GetNode<StaticBody>("Floor").PhysicsMaterialOverride;
            Check(localMaterial is not null && !ReferenceEquals(localMaterial, material) &&
                  localMaterial.Friction == material.Friction && localMaterial.Bounce == material.Bounce &&
                  localMaterial.Rough == material.Rough && localMaterial.Absorbent == material.Absorbent,
                "A scene-local material duplicates all surface state for the new scene instance.");
            material.ResourceLocalToScene = false;
        }

        Check(Bounces(0.8f, false, 0.8f) && !Bounces(0.8f, true, 0.8f) &&
              !Bounces(-0.8f, false, 0.8f),
            "Bounce adds and clamps normally; absorbent or negative bounce subtracts.");
        Check(BouncesAtLowSpeed(), "A low-speed impact still uses the configured bounce.");
        var ordinary = SlideDistance(rough: false, floorRough: false);
        var rough = SlideDistance(rough: true, floorRough: false);
        var negative = SlideDistance(rough: false, floorRough: false, bodyFriction: -1);
        var bothRough = SlideDistance(rough: true, floorRough: true);
        var lowRough = SlideDistance(rough: false, floorRough: true);
        Check(rough + 5 < ordinary && MathF.Abs(rough - negative) < 1 && bothRough + 5 < lowRough,
            "Rough or negative friction takes precedence; two rough surfaces use the higher value.");
        VerifyLiveEditAndDisposal();
        Console.WriteLine("Physics material mixing, live updates, resource and scene-copy checks passed.");
    }

    private static bool Bounces(float floorBounce, bool absorbent, float bodyBounce)
    {
        using var rectangle = new RectangleShape { Size = new(200, 20) };
        using var circle = new CircleShape();
        using var floorMaterial = new PhysicsMaterial { Bounce = floorBounce, Absorbent = absorbent };
        using var bodyMaterial = new PhysicsMaterial { Bounce = bodyBounce };
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100), PhysicsMaterialOverride = floorMaterial };
        floor.AddChild(new CollisionShape { Shape = rectangle });
        var body = new RigidBody { Position = new(0, 50), PhysicsMaterialOverride = bodyMaterial, LockRotation = true };
        body.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(floor);
        root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 35; frame++)
        {
            tree.PhysicsFrame(1d / 60);
            if (body.LinearVelocity.Y < -50) return true;
        }
        return false;
    }

    private static bool BouncesAtLowSpeed()
    {
        using var rectangle = new RectangleShape { Size = new(200, 20) };
        using var circle = new CircleShape();
        using var floorMaterial = new PhysicsMaterial { Bounce = 1 };
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100), PhysicsMaterialOverride = floorMaterial };
        floor.AddChild(new CollisionShape { Shape = rectangle });
        var body = new RigidBody { Position = new(0, 79), LinearVelocity = new(0, 10), LockRotation = true };
        body.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(floor);
        root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 8; frame++)
        {
            tree.PhysicsFrame(1d / 60);
            if (body.LinearVelocity.Y < -5) return true;
        }
        return false;
    }

    private static float SlideDistance(bool rough, bool floorRough, float bodyFriction = 1)
    {
        using var rectangle = new RectangleShape { Size = new(500, 20) };
        using var box = new RectangleShape { Size = new(20, 20) };
        using var floorMaterial = new PhysicsMaterial { Friction = 0.1f, Rough = floorRough };
        using var bodyMaterial = new PhysicsMaterial { Friction = bodyFriction, Rough = rough };
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100), PhysicsMaterialOverride = floorMaterial };
        floor.AddChild(new CollisionShape { Shape = rectangle });
        var body = new RigidBody
        {
            Position = new(-100, 80),
            LinearVelocity = new(100, 0),
            PhysicsMaterialOverride = bodyMaterial,
            LockRotation = true
        };
        body.AddChild(new CollisionShape { Shape = box });
        root.AddChild(floor);
        root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        return body.Position.X + 100;
    }

    private static void VerifyLiveEditAndDisposal()
    {
        using var shape = new RectangleShape { Size = new(200, 20) };
        using var circle = new CircleShape();
        using var floorMaterial = new PhysicsMaterial();
        Action<Resource> changedFailure = _ => throw new InvalidOperationException("Subscriber failure.");
        floorMaterial.Changed += changedFailure;
        floorMaterial.Disposed += _ => throw new InvalidOperationException("Subscriber failure.");
        var root = new Node();
        var floor = new StaticBody { Position = new(0, 100), PhysicsMaterialOverride = floorMaterial };
        floor.AddChild(new CollisionShape { Shape = shape });
        var body = new RigidBody { Position = new(0, 50), LockRotation = true };
        body.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(floor);
        root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 90; frame++) tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.LinearVelocity.Y) < 2, "The default material does not bounce a settled body.");
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed resting material contacts allocate no managed memory.");
        body.CanSleep = false;
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed active material contacts allocate no managed memory.");
        Reject<InvalidOperationException>(() => floorMaterial.Bounce = 1);
        floorMaterial.Changed -= changedFailure;
        body.GlobalPosition = new(0, 50);
        body.LinearVelocity = Vector2.Zero;
        var bounced = false;
        for (var frame = 0; frame < 35; frame++)
        {
            tree.PhysicsFrame(1d / 60);
            bounced |= body.LinearVelocity.Y < -50;
        }
        Check(bounced, "A live material edit changes the next contact's bounce.");
        Check(ReferenceEquals(floor.PhysicsMaterialOverride, floorMaterial), "Live edits preserve material ownership.");
        Reject<InvalidOperationException>(floorMaterial.Dispose);
        tree.PhysicsFrame(1d / 60);
        Check(floor.PhysicsMaterialOverride is null, "Disposing a borrowed material restores default surface state.");
        Reject<ObjectDisposedException>(() => floor.PhysicsMaterialOverride = floorMaterial);
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
