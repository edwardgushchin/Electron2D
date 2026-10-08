using Electron2D;
using static Box2D.NET.B2WheelJoints;
using static Box2D.NET.B2Worlds;

internal static class GrooveJointTests
{
    internal static void Run()
    {
        VerifySlidingAndRotation();
        VerifyGeometryChangesAndPacking();
        VerifyRotatedAndDegenerateGrooves();
        Console.WriteLine("Groove joint finite sliding, free rotation, geometry and lifecycle checks passed.");
    }

    private static void VerifySlidingAndRotation()
    {
        using var circle = new CircleShape { Radius = 6 };
        var root = new Node();
        var rail = new StaticBody { Name = "Rail" };
        var slider = new RigidBody
        {
            Name = "Slider",
            Position = new(0, 25),
            GravityScale = 0,
            CanSleep = false,
            LinearVelocity = new(80, 120),
            AngularVelocity = 2
        };
        slider.AddChild(new CollisionShape { Shape = circle });
        var groove = new GrooveJoint { Name = "Groove", NodeA = "../Rail", NodeB = "../Slider" };
        root.AddChild(groove); root.AddChild(rail); root.AddChild(slider);
        using var tree = new SceneTree(root);
        Check(groove.Length == 50 && groove.InitialOffset == 25 && groove.DisableCollision,
            "The detached groove defaults survive scene entry.");
        for (var frame = 0; frame < 40; frame++) tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(slider.GlobalPosition.X) < 4 && slider.GlobalPosition.Y is > 45 and < 55,
            "The wheel constraint removes sideways velocity and stops at the far groove endpoint.");
        Check(slider.GlobalRotation > 0.5f, "Sliding does not lock the second body's rotation.");
        Check(b2WheelJoint_GetUpperLimit(groove.Runtime.Backend.ID) == 0.5f,
            "The finite 50-unit groove maps to a half-meter solver limit.");

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, $"A warmed active groove step allocates no managed bytes: {allocated}.");

        slider.LinearVelocity = new(0, -120);
        for (var frame = 0; frame < 40; frame++) tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(slider.GlobalPosition.X) < 4 && slider.GlobalPosition.Y is > -5 and < 5,
            "Reverse motion stops at the near groove endpoint without escaping sideways.");
    }

    private static void VerifyGeometryChangesAndPacking()
    {
        using var bounds = new GrooveJoint { Length = -10_000_000f, InitialOffset = 10_000_000f };
        Reject<ArgumentOutOfRangeException>(() => bounds.Length = 10_000_001f);
        Reject<ArgumentOutOfRangeException>(() => bounds.InitialOffset = -10_000_001f);
        Check(bounds.Length == -10_000_000f && bounds.InitialOffset == 10_000_000f,
            "The documented solver extent boundary is accepted and larger edits roll back.");
        using var circle = new CircleShape { Radius = 6 };
        var root = new Node();
        var rail = new StaticBody { Name = "Rail" };
        var slider = new RigidBody { Name = "Slider", Position = new(0, 25), GravityScale = 0, CanSleep = false };
        slider.AddChild(new CollisionShape { Shape = circle });
        var groove = new GrooveJoint
        {
            Name = "Groove",
            NodeA = "../Rail",
            NodeB = "../Slider",
            Length = 50,
            InitialOffset = 25,
            DisableCollision = false
        };
        root.AddChild(rail); root.AddChild(slider); root.AddChild(groove);
        rail.Owner = root; slider.Owner = root; groove.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root);
        using (var copy = packed.Instantiate())
        {
            var restored = copy.GetNode<GrooveJoint>("Groove");
            Check(restored.NodeA == "../Rail" && restored.NodeB == "../Slider" &&
                  restored.Length == 50 && restored.InitialOffset == 25 && !restored.DisableCollision,
                "PackedScene restores the exact groove role, paths, geometry and collision policy.");
        }

        using var tree = new SceneTree(root);
        slider.LinearVelocity = new(0, 120);
        for (var frame = 0; frame < 40; frame++) tree.PhysicsFrame(1d / 60);
        var originalID = groove.Runtime.Backend.ID;
        groove.Length = 20;
        Check(groove.Runtime.Backend.ID.Equals(originalID) &&
              MathF.Abs(b2WheelJoint_GetUpperLimit(groove.Runtime.Backend.ID) - 0.2f) < 0.00001f,
            "Live groove length updates the limit without dropping body-local anchors.");
        for (var frame = 0; frame < 40; frame++) tree.PhysicsFrame(1d / 60);
        Check(slider.GlobalPosition.Y is > 16 and < 24,
            "Narrowing the groove pulls the same slider anchor inside the new far endpoint.");

        groove.InitialOffset = 75;
        tree.PhysicsFrame(1d / 60);
        Check(!groove.Runtime.Backend.ID.Equals(originalID) && !b2Joint_IsValid(originalID),
            "Changing the second-body offset replaces the old native constraint.");
        for (var frame = 0; frame < 40; frame++) tree.PhysicsFrame(1d / 60);
        Check(slider.GlobalPosition.Y < -20,
            "An offset beyond the far endpoint pulls the sampled body anchor into the finite groove.");
        Reject<ArgumentOutOfRangeException>(() => groove.Length = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => groove.InitialOffset = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => groove.Length = float.MaxValue);
        Reject<ArgumentOutOfRangeException>(() => groove.InitialOffset = float.MaxValue);
        Check(groove.Length == 20 && groove.InitialOffset == 75,
            "Nonfinite geometry rejects before mutating stored values.");
        Reject<InvalidOperationException>(() => Task.Run(() => groove.Length = 30).GetAwaiter().GetResult());
        Check(groove.Length == 20, "Off-owner geometry edits leave the constraint unchanged.");
        var previousID = groove.Runtime.Backend.ID;
        groove.Scale = new(2, 1);
        groove.InitialOffset = 70;
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
        Check(groove.Runtime.Backend.ID.Equals(previousID) && groove.Length == 20,
            "Unrepresentable groove geometry preserves the previous constraint and stored length.");
        groove.Scale = Vector2.One;
        groove.InitialOffset = 75;
        tree.PhysicsFrame(1d / 60);
        Check(groove.Runtime.HasBackend, "Corrected geometry reconnects after a failed step.");
        root.RemoveChild(slider);
        Check(!groove.Runtime.HasBackend, "Body exit destroys the groove before its backend body.");
        root.AddChild(slider);
        tree.PhysicsFrame(1d / 60);
        Check(groove.Runtime.HasBackend, "Body reentry reconnects the groove.");
    }

    private static void VerifyRotatedAndDegenerateGrooves()
    {
        using var circle = new CircleShape { Radius = 6 };
        var root = new Node();
        var rail = new StaticBody { Name = "Rail" };
        var slider = new RigidBody
        {
            Name = "Slider",
            Position = new(-25, 0),
            GravityScale = 0,
            CanSleep = false,
            LinearVelocity = new(-120, 60),
            AngularVelocity = 2
        };
        slider.AddChild(new CollisionShape { Shape = circle });
        var groove = new GrooveJoint
        {
            NodeA = "../Rail",
            NodeB = "../Slider",
            Rotation = MathF.PI / 2f
        };
        root.AddChild(rail); root.AddChild(slider); root.AddChild(groove);
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 40; frame++) tree.PhysicsFrame(1d / 60);
        Check(slider.GlobalPosition.X is > -55 and < -45 && MathF.Abs(slider.GlobalPosition.Y) < 4 &&
              slider.GlobalRotation > 0.5f,
            "A rotated groove constrains motion along its transformed axis while rotation remains free.");

        groove.Length = 0;
        slider.LinearVelocity = new(0, 0);
        for (var frame = 0; frame < 80; frame++) tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(slider.GlobalPosition.X) < 4 && MathF.Abs(slider.GlobalPosition.Y) < 4,
            "A zero-length groove constrains the anchor to one point.");

        groove.Length = -50;
        slider.LinearVelocity = new(100, 0);
        for (var frame = 0; frame < 50; frame++) tree.PhysicsFrame(1d / 60);
        Check(slider.GlobalPosition.X is > 45 and < 55 && MathF.Abs(slider.GlobalPosition.Y) < 4,
            "A negative length reverses the finite groove interval.");
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
