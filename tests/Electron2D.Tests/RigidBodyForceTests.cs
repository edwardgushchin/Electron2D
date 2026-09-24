using Electron2D;

internal static class RigidBodyForceTests
{
    internal static void Run()
    {
        VerifyDetachedStateAndValidation();
        VerifyPositionedAndRotationalActions();
        VerifyPersistentForcesAndPacking();
        VerifyAxisVelocityAndWarmFrames();
        Console.WriteLine("Rigid-body positioned force, torque, persistent force and axis checks passed.");
    }

    private static void VerifyDetachedStateAndValidation()
    {
        using var body = new RigidBody();
        Check(body.ConstantForce == Vector2.Zero && body.ConstantTorque == 0,
            "Constant force and torque default to zero.");
        body.Sleeping = true;
        body.AddConstantCentralForce(new(10, 0));
        Check(body.ConstantForce == new Vector2(10, 0) && !body.Sleeping,
            "Adding a persistent central force stores it and wakes a detached body.");
        body.AddConstantForce(new(0, 20), new(5, 0));
        Check(body.ConstantForce == new Vector2(10, 20) && body.ConstantTorque == 100,
            "An offset persistent force accumulates both force and the origin-relative moment.");
        body.AddConstantTorque(-40);
        Check(body.ConstantTorque == 60, "Persistent torque accumulates without changing force.");
        body.ConstantForce = new(3, 4);
        body.ConstantTorque = -2;
        Check(body.ConstantForce == new Vector2(3, 4) && body.ConstantTorque == -2,
            "Property assignments replace accumulated values independently.");
        Reject<ArgumentOutOfRangeException>(() => body.ConstantForce = new(float.NaN, 0));
        Reject<ArgumentOutOfRangeException>(() => body.ConstantTorque = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => body.AddConstantForce(new(0, float.MaxValue),
            new(float.MaxValue, 0)));
        Reject<ArgumentOutOfRangeException>(() => body.AddConstantTorque(float.NaN));
        Check(body.ConstantForce == new Vector2(3, 4) && body.ConstantTorque == -2,
            "Invalid persistent writes reject before changing either total.");
        Reject<InvalidOperationException>(() => body.ApplyForce(new(1, 0)));
        Reject<InvalidOperationException>(() => body.ApplyImpulse(new(1, 0)));
        Reject<InvalidOperationException>(() => body.ApplyTorque(1));
        Reject<InvalidOperationException>(() => body.ApplyTorqueImpulse(1));
        Reject<ArgumentOutOfRangeException>(() => body.ApplyForce(new(float.NaN, 0)));
        Reject<ArgumentOutOfRangeException>(() => body.ApplyImpulse(new(1, 0), new(float.NaN, 0)));
    }

    private static void VerifyPositionedAndRotationalActions()
    {
        using var circle = new CircleShape();
        var root = new Node();
        var body = new RigidBody
        {
            Position = new(100, 0),
            GravityScale = 0,
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace,
            CanSleep = false
        };
        var collider = new CollisionShape { Shape = circle, Position = new(10, 0) };
        body.AddChild(collider);
        root.AddChild(body);
        using var tree = new SceneTree(root);
        body.AddConstantForce(new(0, 10), new(10, 0));
        Check(MathF.Abs(body.ConstantTorque) < 0.001f,
            "An offset constant force uses the updated shifted center of mass before the first step.");
        body.ConstantForce = Vector2.Zero;
        body.ConstantTorque = 0;
        body.ApplyForce(new(0, 1000), new(10, 0));
        tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.LinearVelocity.Y - 16.6667f) < 0.2f && MathF.Abs(body.AngularVelocity) < 0.01f,
            "A force at the shifted center of mass uses scene-to-meter units without torque.");
        body.LinearVelocity = Vector2.Zero;
        body.AngularVelocity = 0;
        body.ApplyForce(new(0, 1000));
        tree.PhysicsFrame(1d / 60);
        Check(body.AngularVelocity < -0.1f,
            "The default origin offset produces the expected signed torque for an asymmetric shape.");

        body.LinearVelocity = Vector2.Zero;
        body.AngularVelocity = 0;
        body.ApplyCentralImpulse(new(0, 100));
        tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.LinearVelocity.Y - 100) < 1 && MathF.Abs(body.AngularVelocity) < 0.01f,
            "A central impulse uses current mass geometry without inducing torque.");

        body.LinearVelocity = Vector2.Zero;
        body.AngularVelocity = 0;
        body.ApplyImpulse(new(0, 100));
        tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.LinearVelocity.Y - 100) < 1 && body.AngularVelocity < -0.1f,
            "A positioned impulse changes both linear and angular velocity once.");
        body.AngularVelocity = 0;
        body.ApplyTorque(1000);
        tree.PhysicsFrame(1d / 60);
        Check(body.AngularVelocity is > 0.2f and < 0.5f,
            "Torque uses squared scene-to-meter units during the current fixed step.");
        body.AngularVelocity = 0;
        body.ApplyTorqueImpulse(1000);
        tree.PhysicsFrame(1d / 60);
        Check(body.AngularVelocity is > 15 and < 25,
            "Angular impulse uses squared scene-to-meter units independently of step duration.");
        body.ApplyCentralImpulse();
        Reject<ArgumentOutOfRangeException>(() => body.ApplyForce(new(0, float.MaxValue),
            new(float.MaxValue, 0)));
        Reject<ArgumentOutOfRangeException>(() => body.ApplyImpulse(new(0, float.MaxValue),
            new(float.MaxValue, 0)));
        Reject<ArgumentOutOfRangeException>(() => body.ApplyTorque(float.NaN));
        Reject<ArgumentOutOfRangeException>(() => body.ApplyTorqueImpulse(float.NaN));
        collider.Scale = new(2, 1);
        Reject<InvalidOperationException>(() => body.ApplyForce(new(0, 1)));
        collider.Scale = Vector2.One;
        tree.PhysicsFrame(1d / 60);
        Check(body.LinearVelocity.IsFinite() && float.IsFinite(body.AngularVelocity),
            "Invalid positioned and angular actions leave the world reusable.");
    }

    private static void VerifyPersistentForcesAndPacking()
    {
        using var circle = new CircleShape();
        var root = new Node();
        var body = new RigidBody
        {
            GravityScale = 0,
            CanSleep = false,
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace
        };
        body.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(body);
        using (var tree = new SceneTree(root))
        {
            body.ConstantForce = new(100, 0);
            for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
            Check(body.LinearVelocity.X > 80 && body.GlobalPosition.X > 35,
                "A constant central force accelerates on every fixed step.");
            body.ConstantForce = Vector2.Zero;
            var velocity = body.LinearVelocity.X;
            for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
            Check(MathF.Abs(body.LinearVelocity.X - velocity) < 1,
                "Clearing constant force stops acceleration while retaining velocity.");

            body.AddConstantForce(new(0, 100), new(10, 0));
            Check(body.ConstantForce == new Vector2(0, 100) && body.ConstantTorque == 1000,
                "Adding an offset constant force retains its moment separately.");
            body.AddConstantTorque(500);
            Check(body.ConstantTorque == 1500, "Constant torque adds to the moment of a positioned force.");
            tree.PhysicsFrame(1d / 60);
            Check(body.AngularVelocity > 0, "Persistent torque rotates an active body.");
            body.ConstantTorque = 0;
            body.ConstantForce = Vector2.Zero;
            body.AngularVelocity = 0;
            body.LinearVelocity = Vector2.Zero;
            body.Freeze = true;
            body.ConstantForce = new(100, 0);
            var frozen = body.GlobalPosition;
            for (var frame = 0; frame < 10; frame++) tree.PhysicsFrame(1d / 60);
            Check(body.GlobalPosition == frozen, "Frozen bodies retain but do not apply persistent force.");
            body.Freeze = false;
            tree.PhysicsFrame(1d / 60);
            Check(body.LinearVelocity.X > 0, "Unfreezing resumes the stored constant force.");
        }

        using var packedRoot = new Node { Name = "Root" };
        var packedBody = new RigidBody { Name = "Body", ConstantForce = new(3, 4), ConstantTorque = 7 };
        packedRoot.AddChild(packedBody);
        packedBody.Owner = packedRoot;
        using var packed = new PackedScene(); packed.Pack(packedRoot);
        using var copy = packed.Instantiate();
        var restored = copy.GetNode<RigidBody>("Body");
        Check(restored.ConstantForce == new Vector2(3, 4) && restored.ConstantTorque == 7,
            "PackedScene restores persistent force and torque without a backend dependency.");
    }

    private static void VerifyAxisVelocityAndWarmFrames()
    {
        using (var body = new RigidBody { LinearVelocity = new(12, 34) })
        {
            body.SetAxisVelocity(new(0, -5));
            Check(body.LinearVelocity == new Vector2(12, -5),
                "Axis velocity replaces the parallel component and preserves perpendicular speed.");
            body.SetAxisVelocity(Vector2.Zero);
            Check(body.LinearVelocity == new Vector2(12, -5), "A zero axis leaves velocity unchanged.");
            Reject<ArgumentOutOfRangeException>(() => body.SetAxisVelocity(new(float.NaN, 0)));
            Check(body.LinearVelocity == new Vector2(12, -5), "Invalid axis input preserves velocity.");
        }

        using var circle = new CircleShape();
        var root = new Node();
        var moving = new RigidBody
        {
            GravityScale = 0,
            CanSleep = false,
            ConstantForce = new(1, 0),
            ConstantTorque = 2,
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace
        };
        moving.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(moving);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed active constant force and torque frames allocate no managed memory.");
        Check(moving.GlobalTransform.Scale.IsEqualApprox(Vector2.One) &&
              Mathf.IsZeroApprox(moving.GlobalTransform.Skew),
            "Repeated solver rotation preserves the unit-scale physics transform.");
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
