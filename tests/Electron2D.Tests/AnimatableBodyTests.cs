using Electron2D;

internal sealed class AnimatableBodyTests(PhysicsServer.Backend backend)
{
    internal static void Run(PhysicsServer.Backend backend = PhysicsServer.Backend.CPU) => new AnimatableBodyTests(backend).RunCore();

    private void RunCore()
    {
        VerifySynchronizedPlatformMotion();
        VerifyModesAndSceneState();
        Console.WriteLine($"Animatable-body kinematic motion, synchronization and scene checks passed on {backend}.");
    }

    private void VerifySynchronizedPlatformMotion()
    {
        using var platformShape = new RectangleShape { Size = new(120, 20) };
        using var riderShape = new RectangleShape { Size = new(20, 20) };
        using var world = new World(backend); var root = new SubViewport { World = world };
        var platform = new AnimatableBody { Position = new(0, 100) };
        platform.AddChild(new CollisionShape { Shape = platformShape });
        var rider = new RigidBody { Position = new(0, 80), CanSleep = false, LockRotation = true };
        rider.AddChild(new CollisionShape { Shape = riderShape });
        root.AddChild(platform); root.AddChild(rider);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 90; frame++) tree.PhysicsFrame(1d / 60);
        Check(platform.SyncToPhysics && platform.GlobalPosition == new Vector2(0, 100) &&
              rider.GlobalPosition.Y is > 77 and < 83,
            "The default kinematic platform supports a resting dynamic body.");

        platform.Position = new(1, 100);
        Check(platform.GlobalPosition.X == 0,
            "Synchronized assignment waits for the fixed step before presenting motion.");
        tree.PhysicsFrame(0);
        Check(platform.GlobalPosition.X == 0, "Zero elapsed time retains the pending kinematic target.");
        tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(platform.GlobalPosition.X - 1) < 0.02f,
            "The kinematic body reaches its target after a fixed step.");
        for (var frame = 2; frame <= 60; frame++)
        {
            platform.Position = new(frame, 100);
            tree.PhysicsFrame(1d / 60);
        }
        Check(MathF.Abs(platform.GlobalPosition.X - 60) < 0.2f && rider.GlobalPosition.X > 5,
            "Platform velocity transfers through contact without the platform being pushed by the rider.");
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetTotalAllocatedBytes(true);
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetTotalAllocatedBytes(true) - before == 0,
            "Warmed stationary kinematic contact frames allocate no managed memory.");
        for (var frame = 61; frame <= 124; frame++)
        {
            platform.Position = new(frame, 100);
            tree.PhysicsFrame(1d / 60);
        }
        before = GC.GetTotalAllocatedBytes(true);
        for (var frame = 125; frame <= 188; frame++)
        {
            platform.Position = new(frame, 100);
            tree.PhysicsFrame(1d / 60);
        }
        Check(GC.GetTotalAllocatedBytes(true) - before == 0,
            "Warmed moving kinematic contact frames allocate no managed memory.");

        platform.SyncToPhysics = false;
        platform.Position = new(189, 100);
        Check(platform.GlobalPosition.X == 189,
            "Disabling synchronization presents a caller's movement immediately.");
        tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(platform.GlobalPosition.X - 189) < 0.02f,
            "The unsynchronized visual transform stays at its target after backend motion.");
    }

    private void VerifyModesAndSceneState()
    {
        using var geometry = new CapsuleShape();
        using var world = new World(backend); var root = new SubViewport { World = world };
        var body = new AnimatableBody();
        body.AddChild(new CollisionShape { Shape = geometry });
        root.AddChild(body);
        using var tree = new SceneTree(root);
        var delivered = 0;
        body.LocalTransformChanged += _ => delivered++;
        body.Rotation = 0.25f;
        Check(body.Rotation == 0 && delivered == 1,
            "Synchronized rotation is deferred and notifies once with the previous pose.");
        tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.Rotation - 0.25f) < 0.01f && delivered == 1,
            "The solved rotation appears after the step without another local notification.");
        Reject<InvalidOperationException>(() => body.Scale = new(2, 2));
        Check(body.Scale.IsEqualApprox(Vector2.One),
            "Unsupported active scale rejects and restores the previous transform.");
        Reject<InvalidOperationException>(() => body.Skew = 0.2f);
        Check(Mathf.IsZeroApprox(body.Skew),
            "Unsupported active skew rejects and restores the previous transform.");
        Action<CanvasItem> failingListener = _ => throw new InvalidOperationException("User callback failure.");
        body.LocalTransformChanged += failingListener;
        Reject<InvalidOperationException>(() => body.Position = new(10, 0));
        Check(body.GlobalPosition == Vector2.Zero,
            "Callback failure leaves the synchronized target pending and the scene at the old pose.");
        tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.GlobalPosition.X - 10) < 0.02f,
            "The pending target is applied once despite an earlier user callback failure.");
        body.LocalTransformChanged -= failingListener;

        body.SyncToPhysics = false;
        body.Position = new(11, 0);
        Check(body.GlobalPosition.X == 11 && delivered == 2,
            "Unsynchronized movement is immediate and does not emit a local transform notification.");
        body.SyncToPhysics = true;
        body.Position = new(12, 0);
        Check(body.GlobalPosition.X == 11,
            "Re-enabling synchronization restores the deferred presentation policy.");
        body.SyncToPhysics = false;
        Check(body.GlobalPosition.X == 12,
            "Disabling synchronization with a pending target presents that target immediately.");
        tree.PhysicsFrame(1d / 60);
        body.SyncToPhysics = true;
        Reject<InvalidOperationException>(() => Task.Run(() => _ = body.SyncToPhysics).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => body.SyncToPhysics = false).GetAwaiter().GetResult());
        Check(body.SyncToPhysics, "Off-thread access preserves the owner-thread mode.");
        root.RemoveChild(body);
        body.Position = new(20, 0);
        root.AddChild(body);
        body.Position = new(21, 0);
        Check(body.GlobalPosition.X == 20, "Re-entering a tree restores deferred movement.");
        tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.GlobalPosition.X - 21) < 0.02f,
            "The re-entered body's pending target reaches the solver.");

        using var surface = new PhysicsMaterial { Friction = 0.5f };
        using var packedRoot = new Node { Name = "Root" };
        var packedBody = new AnimatableBody { Name = "Body", SyncToPhysics = false, PhysicsMaterialOverride = surface };
        var collision = new CollisionShape { Name = "Collision", Shape = geometry };
        packedRoot.AddChild(packedBody); packedBody.AddChild(collision);
        packedBody.Owner = packedRoot; collision.Owner = packedRoot;
        using var packed = new PackedScene(); packed.Pack(packedRoot);
        using var copy = packed.Instantiate();
        var restored = copy.GetNode<AnimatableBody>("Body");
        Check(!restored.SyncToPhysics && ReferenceEquals(restored.PhysicsMaterialOverride, surface) &&
              ReferenceEquals(copy.GetNode<CollisionShape>("Body/Collision").Shape, geometry),
            "PackedScene restores the exact kinematic type, synchronization policy, material and shape.");
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
