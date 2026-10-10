using Electron2D;
using static Box2D.NET.B2Joints;

internal sealed class PinJointTests(PhysicsServer.Backend backend)
{
    internal static void Run(PhysicsServer.Backend backend = PhysicsServer.Backend.CPU) => new PinJointTests(backend).RunCore();
    private void RunCore()
    {
        VerifyConstraintAndLifecycle();
        VerifyMotorLimitsAndPacking();
        VerifyConnectedCollisionPolicy();
        VerifyLateAttachment();
        Console.WriteLine("Pin joint solver, lifecycle, limits, motor and packing checks passed.");
    }

    private void VerifyConstraintAndLifecycle()
    {
        using var circle = new CircleShape();
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld };
        var anchor = new StaticBody { Name = "Anchor" };
        var bob = new RigidBody { Name = "Bob", Position = new(100, 0), CanSleep = false };
        bob.AddChild(new CollisionShape { Shape = circle });
        var pin = new PinJoint { Name = "Pin", NodeA = "../Anchor", NodeB = "../Bob" };
        root.AddChild(pin); root.AddChild(anchor); root.AddChild(bob);
        using var tree = new SceneTree(root);
        Check(pin.GetConfigurationWarnings().Length == 0, "Both late-entering bodies resolve before the first step.");
        for (var frame = 0; frame < 40; frame++) tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(bob.GlobalPosition.Length() - 100) < 4 && bob.GlobalPosition.Y > 10,
            "Gravity swings the body around the fixed pin without stretching the anchor.");
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0,
            $"A warmed moving pin joint allocates no managed memory per fixed step: {allocated} bytes.");

        pin.NodeB = "../Missing";
        tree.PhysicsFrame(1d / 60);
        Check(pin.GetConfigurationWarnings().Length != 0, "A missing endpoint removes the active constraint.");
        pin.NodeB = "../Bob";
        tree.PhysicsFrame(1d / 60);
        Check(pin.GetConfigurationWarnings().Length == 0, "Restoring a path rebuilds the pin.");

        root.RemoveChild(bob);
        Check(pin.GetConfigurationWarnings().Length != 0, "Body exit invalidates the joint before backend body destruction.");
        root.AddChild(bob);
        tree.PhysicsFrame(1d / 60);
        Check(pin.GetConfigurationWarnings().Length == 0, "Body reentry reconnects the joint.");
        pin.NodeA = "../Bob";
        tree.PhysicsFrame(1d / 60);
        Check(pin.GetConfigurationWarnings().Length != 0, "A self-connection remains unconfigured.");
        pin.NodeA = "../Anchor";
        tree.PhysicsFrame(1d / 60);
        Check(pin.GetConfigurationWarnings().Length == 0, "A corrected self-connection recovers.");
        bob.Name = "Other";
        tree.PhysicsFrame(1d / 60);
        Check(!pin.Runtime.HasBackend, "Renaming an active endpoint invalidates its stored path.");
        bob.Name = "Bob";
        tree.PhysicsFrame(1d / 60);
        Check(pin.Runtime.HasBackend, "Restoring the endpoint name reconnects the path.");
        var previousID = pin.Runtime.Backend.ID;
        pin.Scale = new(2, 1);
        pin.NodeB = "../Missing";
        pin.NodeB = "../Bob";
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
        Check(pin.Runtime.HasBackend && (backend != PhysicsServer.Backend.CPU || pin.Runtime.Backend.ID.Equals(previousID)), "Invalid joint geometry preserves the previous backend constraint.");
        pin.Scale = Vector2.One;
        tree.PhysicsFrame(1d / 60);
        Check(pin.Runtime.HasBackend && pin.GetConfigurationWarnings().Length == 0,
            "Invalid joint geometry can be corrected on the next step.");
        var radiusBefore = bob.GlobalPosition.Length();
        pin.Position = new(300, 0);
        for (var frame = 0; frame < 10; frame++) tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(bob.GlobalPosition.Length() - radiusBefore) < 4,
            "A moved joint node does not recreate an already-attached anchor.");
        Reject<InvalidOperationException>(() => Task.Run(() => pin.DisableCollision = false).GetAwaiter().GetResult());
        Check(pin.DisableCollision, "Off-owner mutation rejects before changing joint configuration.");
    }

    private void VerifyMotorLimitsAndPacking()
    {
        using var circle = new CircleShape();
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld };
        var anchor = new StaticBody { Name = "Anchor" };
        var bob = new RigidBody { Name = "Bob", GravityScale = 0, CanSleep = false };
        bob.AddChild(new CollisionShape { Shape = circle });
        var pin = new PinJoint
        {
            Name = "Pin",
            NodeA = "../Anchor",
            NodeB = "../Bob",
            AngularLimitLower = -0.4f,
            AngularLimitUpper = 0.4f,
            AngularLimitEnabled = true,
            MotorTargetVelocity = 3,
            MotorEnabled = true,
            DisableCollision = false,
            MotorMaxTorque = 10f
        };
        root.AddChild(anchor); root.AddChild(bob); root.AddChild(pin);
        anchor.Owner = root; bob.Owner = root; pin.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root);
        using (var copy = packed.Instantiate())
        {
            var copied = copy.GetNode<PinJoint>("Pin");
            Check(copied.NodeA == "../Anchor" && copied.NodeB == "../Bob" &&
                  copied.AngularLimitEnabled && copied.AngularLimitLower == -0.4f &&
                  copied.AngularLimitUpper == 0.4f && copied.MotorEnabled &&
                  copied.MotorTargetVelocity == 3 && copied.MotorMaxTorque == 10f && !copied.DisableCollision,
                "Packed scenes restore the concrete joint and all stored pin settings.");
        }

        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(bob.GlobalRotation > 0.2f && bob.GlobalRotation < 0.55f,
            $"The motor rotates the body and the enabled upper angular limit stops it: {bob.GlobalRotation}.");
        pin.MotorEnabled = false;
        pin.AngularLimitEnabled = false;
        bob.AngularVelocity = -2;
        for (var frame = 0; frame < 30; frame++) tree.PhysicsFrame(1d / 60);
        Check(bob.GlobalRotation < 0, "Disabling limits permits rotation beyond the former lower bound.");
        bob.AngularVelocity = 0;
        pin.MotorMaxTorque = 0;
        pin.MotorEnabled = true;
        var stopped = bob.GlobalRotation;
        for (var frame = 0; frame < 20; frame++) tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(bob.GlobalRotation - stopped) < 0.01f, "Zero motor torque cannot drive the body.");
        pin.MotorMaxTorque = 10;
        for (var frame = 0; frame < 20; frame++) tree.PhysicsFrame(1d / 60);
        Check(bob.GlobalRotation > stopped + 0.2f, "Live motor torque updates the active constraint.");
        Reject<ArgumentOutOfRangeException>(() => pin.MotorTargetVelocity = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => pin.MotorMaxTorque = -1);
        Reject<ArgumentOutOfRangeException>(() => pin.AngularLimitLower = float.PositiveInfinity);
        pin.AngularLimitLower = -MathF.PI;
        Reject<ArgumentOutOfRangeException>(() => pin.AngularLimitEnabled = true);
        Check(!pin.AngularLimitEnabled, "Unsupported solver limits reject before enabling the constraint.");
    }

    private void VerifyConnectedCollisionPolicy()
    {
        using var circle = new CircleShape { Radius = 20 };
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld };
        var anchor = new StaticBody { Name = "Anchor" };
        anchor.AddChild(new CollisionShape { Shape = circle });
        var body = new RigidBody { Name = "Body", Position = new(10, 0), GravityScale = 0, MaxContactsReported = 2, CanSleep = false };
        body.AddChild(new CollisionShape { Shape = circle });
        var pin = new PinJoint { NodeA = "../Anchor", NodeB = "../Body" };
        root.AddChild(anchor); root.AddChild(body); root.AddChild(pin);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 4; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GetContactCount() == 0, "Connected-body contacts are disabled by default.");
        pin.DisableCollision = false;
        for (var frame = 0; frame < 4; frame++) tree.PhysicsFrame(1d / 60);
        if (backend == PhysicsServer.Backend.CPU) Check(b2Joint_GetCollideConnected(pin.Runtime.Backend.ID), "Enabled connected collisions reach the solver joint.");
        Check(body.GetContactCount() > 0, "Enabling connected collisions creates solver contacts.");
        pin.DisableCollision = true;
        for (var frame = 0; frame < 4; frame++) tree.PhysicsFrame(1d / 60);
        Check(body.GetContactCount() == 0, "Disabling connected collisions removes existing contacts.");
    }

    private void VerifyLateAttachment()
    {
        using var circle = new CircleShape();
        using var selectedWorld = new World(backend); using var root = new SubViewport { World = selectedWorld };
        var anchor = new StaticBody { Name = "Anchor" };
        var pin = new PinJoint { NodeA = "../Anchor", NodeB = "../Late" };
        root.AddChild(anchor); root.AddChild(pin);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 4; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "An unchanged unresolved joint does not repeatedly allocate while polling paths.");
        var late = new RigidBody { Name = "Late", GravityScale = 0 };
        late.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(late);
        tree.PhysicsFrame(1d / 60);
        Check(pin.Runtime.HasBackend, "A body added after the joint enters connects on the next step.");
        root.RemoveChild(late);
        late.Name = "Other";
        root.AddChild(late);
        tree.PhysicsFrame(1d / 60);
        Check(!pin.Runtime.HasBackend, "A path that no longer resolves leaves the joint inactive.");
        late.Name = "Late";
        tree.PhysicsFrame(1d / 60);
        Check(pin.Runtime.HasBackend, "A matching node rename reconnects an unresolved joint.");
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
