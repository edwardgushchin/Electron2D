using Electron2D;

public static partial class BackendChecks
{
    public static void RunAxisVelocity(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            Solver? solver = null;
            if (backend is null) PhysicsServerManager.RegisterServer("AxisVelocity", () => solver = new());
            using var world = backend is { } selected ? new World(selected) : new World("AxisVelocity");
            using var circle = new CircleShape { Radius = 1 }; using var root = new SubViewport { World = world };
            var rigid = new RigidBody { GravityScale = 0, Mass = 2, Inertia = 4, CanSleep = false };
            PhysicsBody[] nodes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), rigid, new PhysicalBone()];
            for (var i = 0; i < nodes.Length; i++)
            {
                nodes[i].Position = new(i * 80, 0);
                nodes[i].AddChild(new CollisionShape { Shape = circle, Disabled = true });
                nodes[i].AddChild(new CollisionShape { Shape = circle }); root.AddChild(nodes[i]);
            }
            using var tree = new SceneTree(root);
            rigid.LinearVelocity = new(3, 4); rigid.ApplyCentralImpulse(new(0, 12)); rigid.ApplyTorqueImpulse(8);
            Check(PhysicsServer.BodyGetLinearVelocity(rigid.GetRID()).DistanceTo(new(3, 10)) < .0001f, "The selected solver receives the real impulse before axis projection");
            rigid.SetAxisVelocity(new(5, 0));
            var velocity = PhysicsServer.BodyGetLinearVelocity(rigid.GetRID());
            if (velocity.DistanceTo(new(5, 10)) >= .0001f)
                throw new InvalidOperationException($"Scene axis projection erased current perpendicular impulse motion: {velocity}.");
            Check(Math.Abs(PhysicsServer.BodyGetAngularVelocity(rigid.GetRID()) - 2) < .0001f, "Axis projection preserves current angular impulse motion");
            AxisVelocityInactive(root);
            AxisVelocityInputs(solver, world, tree, nodes, circle, threaded);
            Console.WriteLine($"Axis velocity {backend?.ToString() ?? "registered"}/{threaded}: immediate native scene impulse projection passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
