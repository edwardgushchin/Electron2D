using Electron2D;

internal static class DampedSpringJointTests
{
    internal static void Run()
    {
        VerifyDefaultsAndPacking();
        VerifyElasticForceAndLiveSettings();
        VerifyDampingAndAnchorTorque();
        VerifyMomentumAndEquilibrium();
        VerifyTransformedAnchorsAndSleep();
        VerifyLifecycleAndFailure();
        VerifyMultipleSpringFailure();
        var ordinary = MeasureKinematicIntervals(false);
        var subdivided = MeasureKinematicIntervals(true);
        Check(MathF.Abs(ordinary - subdivided) < 0.1f,
            "Kinematic subdivisions integrate spring impulses for the full duration once.");
        Console.WriteLine("Damped spring force, axial damping, anchor torque, lifecycle and allocation checks passed.");
    }

    private static RigidBody Body(string name, Shape shape, Vector2 position, float mass = 1)
    {
        var body = new RigidBody
        {
            Name = name,
            Position = position,
            Mass = mass,
            GravityScale = 0,
            CanSleep = false,
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace
        };
        body.AddChild(new CollisionShape { Shape = shape });
        return body;
    }

    private static void VerifyDefaultsAndPacking()
    {
        using var root = new Node();
        var spring = new DampedSpringJoint { Name = "Spring" };
        root.AddChild(spring); spring.Owner = root;
        Check(spring.Length == 50 && spring.RestLength == 0 && spring.Stiffness == 20 && spring.Damping == 1,
            "Spring defaults preserve the declared public profile.");
        spring.Length = -80; spring.RestLength = 40; spring.Stiffness = 12; spring.Damping = 3;
        spring.NodeA = "../First"; spring.NodeB = "../Second"; spring.DisableCollision = false;
        using var packed = new PackedScene(); packed.Pack(root);
        using var copy = packed.Instantiate();
        var restored = copy.GetNode<DampedSpringJoint>("Spring");
        Check(restored.Length == -80 && restored.RestLength == 40 && restored.Stiffness == 12 && restored.Damping == 3 &&
              restored.NodeA == "../First" && restored.NodeB == "../Second" && !restored.DisableCollision,
            "PackedScene restores the exact spring role, geometry, coefficients and inherited paths.");
        Reject<ArgumentOutOfRangeException>(() => spring.RestLength = -1);
        Reject<ArgumentOutOfRangeException>(() => spring.Stiffness = -1);
        Reject<ArgumentOutOfRangeException>(() => spring.Damping = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => spring.Length = float.MaxValue);
        Check(spring.RestLength == 40 && spring.Stiffness == 12 && spring.Damping == 3 && spring.Length == -80,
            "Rejected detached settings preserve the previous configuration.");
    }

    private static void VerifyElasticForceAndLiveSettings()
    {
        using var shape = new CircleShape { Radius = 6 };
        var root = new Node();
        var first = new StaticBody { Name = "First" };
        var body = Body("Second", shape, new(0, 100), mass: 2);
        var spring = new DampedSpringJoint
        {
            NodeA = "../First",
            NodeB = "../Second",
            Length = 100,
            RestLength = 50,
            Stiffness = 20,
            Damping = 0
        };
        root.AddChild(spring); root.AddChild(first); root.AddChild(body);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(0);
        Check(body.LinearVelocity == Vector2.Zero, "A zero-duration frame applies no spring impulse.");
        tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.LinearVelocity.Y + 1000f / 120f) < 0.001f,
            "Hooke force uses scene distance, stiffness, elapsed time and the body's mass.");
        body.LinearVelocity = Vector2.Zero;
        spring.RestLength = 0;
        tree.PhysicsFrame(1d / 60);
        Check(body.LinearVelocity.Y > 0, "Zero RestLength uses Length and pushes a compressed spring outward.");
        body.LinearVelocity = Vector2.Zero;
        spring.Length = 200;
        tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.LinearVelocity.Y) < 0.001f,
            "Live Length resamples the second anchor and the automatic relaxed length together.");
        spring.RestLength = 120;
        // Retire background-GC allocation contexts before measuring; affected runtimes can count their unused bytes.
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
        for (var frame = 0; frame < 40; frame++) tree.PhysicsFrame(1d / 120);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 120);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "A warmed active spring world step allocates no managed memory.");
        body.Freeze = true;
        var position = body.Position;
        tree.PhysicsFrame(1d / 60);
        Check(body.Position == position, "A frozen static body does not respond to the spring.");
        body.Freeze = false;
        body.CustomIntegrator = true;
        body.LinearVelocity = Vector2.Zero;
        tree.PhysicsFrame(1d / 60);
        Check(body.LinearVelocity != Vector2.Zero, "A custom integrator retains external spring response.");
    }

    private static void VerifyDampingAndAnchorTorque()
    {
        using var shape = new CircleShape { Radius = 6 };
        var root = new Node();
        var first = new StaticBody { Name = "First" };
        var body = Body("Second", shape, new(0, 100));
        body.LinearVelocity = new(30, 60);
        var spring = new DampedSpringJoint { NodeA = "../First", NodeB = "../Second", Length = 100, Stiffness = 0, Damping = 2 };
        root.AddChild(first); root.AddChild(body); root.AddChild(spring);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(0.05);
        Check(MathF.Abs(body.LinearVelocity.Y - 60 * MathF.Exp(-0.1f)) < 0.001f && MathF.Abs(body.LinearVelocity.X - 30) < 0.001f,
            "Pure damping follows exponential axial decay and leaves tangential velocity unchanged.");
        body.Position = new(20, 100); body.LinearVelocity = new(0, 60); body.AngularVelocity = 0; body.Inertia = 400;
        spring.Length = 101; spring.Length = 100;
        tree.PhysicsFrame(0.05);
        var impulse = -0.6f * (1 - MathF.Exp(-0.2f)) / 2;
        Check(MathF.Abs(body.LinearVelocity.Y - (60 + impulse * 100)) < 0.002f &&
              MathF.Abs(body.AngularVelocity - (-5 * impulse)) < 0.002f,
            "Off-center damping includes rotational inverse mass and applies anchor torque.");
    }

    private static void VerifyMomentumAndEquilibrium()
    {
        using var shape = new CircleShape { Radius = 6 };
        var root = new Node();
        var first = Body("First", shape, Vector2.Zero);
        var second = Body("Second", shape, new(0, 100), mass: 2);
        var spring = new DampedSpringJoint { NodeA = "../First", NodeB = "../Second", Length = 100, RestLength = 50, Damping = 0 };
        root.AddChild(first); root.AddChild(second); root.AddChild(spring);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(first.LinearVelocity.Y - 1000f / 60f) < 0.001f &&
              MathF.Abs(first.LinearVelocity.Y + 2 * second.LinearVelocity.Y) < 0.001f,
            "The same spring impulse reaches both bodies and preserves pair momentum.");
        spring.Damping = 8;
        for (var frame = 0; frame < 600; frame++) tree.PhysicsFrame(1d / 120);
        Check(MathF.Abs(first.Position.DistanceTo(second.Position) - 50) < 0.3f,
            "Axial damping settles the pair at the configured relaxed separation.");
        spring.RestLength = 150;
        for (var frame = 0; frame < 180; frame++) tree.PhysicsFrame(1d / 120);
        Check(first.Position.DistanceTo(second.Position) > 110,
            "Length places an anchor and does not impose a maximum stretch constraint.");
    }

    private static void VerifyLifecycleAndFailure()
    {
        using var shape = new CircleShape { Radius = 10 };
        var root = new Node();
        var first = new StaticBody { Name = "First" };
        first.AddChild(new CollisionShape { Shape = shape });
        var body = Body("Second", shape, new(0, 5)); body.MaxContactsReported = 2;
        var spring = new DampedSpringJoint { NodeA = "../First", NodeB = "../Second", Length = 5, Stiffness = 0, Damping = 0 };
        root.AddChild(first); root.AddChild(body); root.AddChild(spring);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        Check(body.GetContactCount() == 0, "The spring connection suppresses mutual collisions by default.");
        spring.DisableCollision = false;
        tree.PhysicsFrame(1d / 60);
        Check(body.GetContactCount() > 0, "A live spring collision policy creates real body contacts.");
        spring.DisableCollision = true;
        tree.PhysicsFrame(1d / 60);
        Check(body.GetContactCount() == 0, "Re-disabling spring collisions removes the active contact.");
        spring.NodeB = "../Missing";
        tree.PhysicsFrame(1d / 60);
        Check(!spring.Runtime.HasBackend, "An unresolved endpoint removes the filter joint and force generation.");
        spring.NodeB = "../Second";
        tree.PhysicsFrame(1d / 60);
        var id = spring.Runtime.Backend.ID;
        spring.Scale = new(2, 1); spring.Length = 6;
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
        Check(spring.Runtime.Backend.ID.Equals(id), "Invalid spring geometry retains the previous native connection.");
        spring.Scale = Vector2.One;
        tree.PhysicsFrame(1d / 60);
        var velocity = body.LinearVelocity;
        spring.Stiffness = float.MaxValue; spring.RestLength = 10_000_000;
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
        Check(body.LinearVelocity == velocity, "An overflowing spring impulse rejects before either native body is mutated.");
        spring.Stiffness = 20; spring.RestLength = 6;
        Reject<InvalidOperationException>(() => Task.Run(() => spring.Damping = 3).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => spring.Stiffness).GetAwaiter().GetResult());
        root.RemoveChild(body);
        Check(!spring.Runtime.HasBackend, "Body departure clears the spring's native connection before body destruction.");
        root.AddChild(body); tree.PhysicsFrame(1d / 60);
        Check(spring.Runtime.HasBackend, "Body reentry rebuilds the spring and its local anchors.");
        spring.RestLength = 30;
        body.NotifyLocalTransformChanges = true;
        var phaseRejected = false;
        Action<CanvasItem> guarded = _ =>
        {
            Reject<InvalidOperationException>(() => spring.Damping = 3);
            phaseRejected = true;
        };
        body.LocalTransformChanged += guarded;
        tree.PhysicsFrame(1d / 60);
        body.LocalTransformChanged -= guarded;
        Check(phaseRejected && spring.Damping == 0,
            "Mutation from in-step body synchronization rejects before changing spring state.");
        Action<CanvasItem> failing = _ => throw new InvalidOperationException("Spring body sync probe.");
        body.LocalTransformChanged += failing;
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
        body.LocalTransformChanged -= failing;
        body.NotifyLocalTransformChanges = false;
        tree.PhysicsFrame(1d / 60);
        Check(spring.Runtime.HasBackend, "A throwing body-sync callback leaves the spring and world reusable.");
    }

    private static void VerifyTransformedAnchorsAndSleep()
    {
        using var shape = new CircleShape { Radius = 6 };
        var root = new Node();
        var first = new StaticBody { Name = "First", Position = new(10, 20), Rotation = 0.6f };
        var body = Body("Second", shape, new(110, 20)); body.Rotation = 0.4f;
        var spring = new DampedSpringJoint
        {
            NodeA = "../First",
            NodeB = "../Second",
            Position = new(10, 20),
            Rotation = MathF.PI / 2,
            Length = -100,
            RestLength = 50,
            Damping = 0
        };
        root.AddChild(first); root.AddChild(body); root.AddChild(spring);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.LinearVelocity.X + 1000f / 60) < 0.001f && MathF.Abs(body.LinearVelocity.Y) < 0.001f,
            "Rotated signed anchor geometry creates a horizontal spring with correct force direction.");
        body.LinearVelocity = Vector2.Zero; body.AngularVelocity = 0;
        spring.Length = 0; spring.RestLength = 0;
        tree.PhysicsFrame(1d / 60);
        Check(body.LinearVelocity == Vector2.Zero && body.AngularVelocity == 0,
            $"Coincident zero-length anchors have no undefined force direction: {body.LinearVelocity}, {body.AngularVelocity}.");
        body.CanSleep = true; body.Sleeping = true;
        tree.PhysicsFrame(1d / 60);
        Check(body.Sleeping, "A relaxed spring does not wake an unchanged sleeping body.");
        spring.Length = 50; spring.RestLength = 100;
        tree.PhysicsFrame(1d / 60);
        Check(!body.Sleeping && body.LinearVelocity != Vector2.Zero,
            "A changed relaxed distance wakes a body when the spring develops a nonzero force.");
    }

    private static void VerifyMultipleSpringFailure()
    {
        using var shape = new CircleShape { Radius = 6 };
        var root = new Node();
        var first = new StaticBody { Name = "First" };
        var body = Body("Second", shape, new(0, 100));
        var valid = new DampedSpringJoint { NodeA = "../First", NodeB = "../Second", Length = 100, RestLength = 50, Damping = 0 };
        valid.Name = "Valid";
        var invalid = new DampedSpringJoint
        {
            Name = "Invalid",
            NodeA = "../First",
            NodeB = "../Second",
            Length = 100,
            RestLength = 10_000_000,
            Stiffness = float.MaxValue,
            Damping = 0
        };
        root.AddChild(first); root.AddChild(body); root.AddChild(valid); root.AddChild(invalid);
        using var tree = new SceneTree(root);
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
        Check(body.LinearVelocity == Vector2.Zero,
            "All spring impulses preflight before any are applied, including a later invalid spring.");
        valid.Stiffness = 2e38f; valid.RestLength = 200;
        invalid.Stiffness = 2e38f; invalid.RestLength = 200;
        Reject<AggregateException>(() => tree.PhysicsFrame(1));
        Check(body.LinearVelocity == Vector2.Zero,
            "Individually finite spring responses reject if their combined body velocity overflows.");
        valid.Stiffness = 20; valid.RestLength = 50;
        invalid.Stiffness = 0;
        tree.PhysicsFrame(1d / 60);
        Check(body.LinearVelocity.Y < 0, "A corrected sibling spring leaves the world reusable.");
    }

    private static float MeasureKinematicIntervals(bool subdivide)
    {
        using var shape = new CircleShape { Radius = 6 };
        var root = new Node();
        var first = new StaticBody { Name = "First" };
        var body = Body("Second", shape, new(0, 100));
        var spring = new DampedSpringJoint { NodeA = "../First", NodeB = "../Second", Length = 100, RestLength = 50, Damping = 0 };
        var platform = new AnimatableBody { Position = new(0, -1000) };
        platform.AddChild(new CollisionShape { Shape = shape });
        root.AddChild(first); root.AddChild(body); root.AddChild(spring); root.AddChild(platform);
        using var tree = new SceneTree(root);
        if (subdivide) platform.Position = new(100, -1000);
        tree.PhysicsFrame(1d / 60);
        return body.LinearVelocity.Y;
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
