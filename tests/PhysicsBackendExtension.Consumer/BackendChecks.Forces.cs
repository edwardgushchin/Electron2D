using Electron2D;

public static partial class BackendChecks
{
    public static void RunForces(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { ForceChecks(backend, threaded); ForceOperationChecks(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void ForceChecks(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("ForceChecks", () => solver = new());
        using var world = backend is { } selected ? new World(selected) : new World("ForceChecks");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 3 };
        var scene = new RigidBody { Mass = 2, Inertia = 8, GravityScale = 0, CanSleep = false };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        var ownerThread = Thread.CurrentThread;
        if (solver is not null) solver.StepHook = () => Check((Thread.CurrentThread != ownerThread) == threaded, "Forces use the actual captured numerical thread");
        var raw = PhysicsServer.BodyCreate();
        const double delta = 1d / 60;
        try
        {
            PhysicsServer.AreaSetGravity(world.Space, 0); PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetTransform(raw, new(0, new Vector2(100, 0)));
            PhysicsServer.BodySetSpace(raw, world.Space);
            var bodies = new[] { scene.GetRID(), raw };
            foreach (var body in bodies)
            {
                PhysicsServer.BodySetMass(body, 2); PhysicsServer.BodySetInertia(body, 8); PhysicsServer.BodySetCenterOfMass(body, new(2, -1));
                PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false);
                PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(body, RigidBody.DampMode.Replace);
                PhysicsServer.BodySetLinearDamp(body, 0); PhysicsServer.BodySetAngularDamp(body, 0);
            }
            for (var route = 0; route < 3; route++)
            {
                foreach (var body in bodies)
                {
                    Reset(body);
                    var state = PhysicsServer.BodyGetDirectState(body)!;
                    if (route == 0)
                    {
                        PhysicsServer.BodyApplyCentralImpulse(body, new(2, 4)); PhysicsServer.BodyApplyImpulse(body, new(6, -2), new(3, -4));
                        PhysicsServer.BodyApplyTorqueImpulse(body, -4);
                        PhysicsServer.BodyApplyCentralForce(body, new(4, -8)); PhysicsServer.BodyApplyForce(body, new(12, 4), new(-2, 3)); PhysicsServer.BodyApplyTorque(body, 16);
                    }
                    else if (route == 1 && body == scene.GetRID())
                    {
                        scene.ApplyCentralImpulse(new(2, 4)); scene.ApplyImpulse(new(6, -2), new(3, -4)); scene.ApplyTorqueImpulse(-4);
                        scene.ApplyCentralForce(new(4, -8)); scene.ApplyForce(new(12, 4), new(-2, 3)); scene.ApplyTorque(16);
                    }
                    else
                    {
                        state.ApplyCentralImpulse(new(2, 4)); state.ApplyImpulse(new(6, -2), new(3, -4)); state.ApplyTorqueImpulse(-4);
                        state.ApplyCentralForce(new(4, -8)); state.ApplyForce(new(12, 4), new(-2, 3)); state.ApplyTorque(16);
                    }
                    Motion(body, new(4, 1), 3.5f, "Immediate impulse uses mass, rotated center and inertia");
                }
                tree.PhysicsFrame(delta);
                foreach (var body in bodies) Motion(body, new(4 + 8f / 60, 1 - 2f / 60), 3.5f - 1f / 60,
                    "Central/positioned force and torque integrate once in scene units");
                var velocity = PhysicsServer.BodyGetLinearVelocity(raw); var angular = PhysicsServer.BodyGetAngularVelocity(raw);
                tree.PhysicsFrame(delta);
                foreach (var body in bodies) Motion(body, velocity, angular, "Consumed transient force does not leak into the next tick");
                foreach (var body in bodies)
                {
                    var state = PhysicsServer.BodyGetDirectState(body)!;
                    var center = PhysicsServer.BodyGetTransform(body).BasisXform(new Vector2(2, -1));
                    var position = center + new Vector2(1, 2);
                    if (route == 0)
                    {
                        PhysicsServer.BodySetConstantForce(body, new(6, 8)); PhysicsServer.BodySetConstantTorque(body, 12);
                        PhysicsServer.BodyAddConstantCentralForce(body, new(2, -4)); PhysicsServer.BodyAddConstantForce(body, new(4, 2), position); PhysicsServer.BodyAddConstantTorque(body, -2);
                    }
                    else if (route == 1 && body == scene.GetRID())
                    {
                        scene.ConstantForce = new(6, 8); scene.ConstantTorque = 12; scene.AddConstantCentralForce(new(2, -4));
                        scene.AddConstantForce(new(4, 2), position); scene.AddConstantTorque(-2);
                    }
                    else
                    {
                        state.SetConstantForce(new(6, 8)); state.SetConstantTorque(12); state.AddConstantCentralForce(new(2, -4));
                        state.AddConstantForce(new(4, 2), position); state.AddConstantTorque(-2);
                    }
                    Check(PhysicsServer.BodyGetConstantForce(body) == new Vector2(12, 6) && MathF.Abs(PhysicsServer.BodyGetConstantTorque(body) - 4) < .00001f &&
                        state.GetConstantForce() == new Vector2(12, 6) && MathF.Abs(state.GetConstantTorque() - 4) < .00001f,
                        "Constant totals share scene/server/direct-state identity and capture the positioned moment once");
                }
                for (var step = 1; step <= 2; step++)
                {
                    tree.PhysicsFrame(delta);
                    foreach (var body in bodies) Motion(body, velocity + new Vector2(.1f, .05f) * step, angular + step / 120f,
                        "Persistent totals repeat without recomputing their captured moment after rotation");
                }
                foreach (var body in bodies)
                {
                    PhysicsServer.BodySetConstantForce(body, Vector2.Zero); PhysicsServer.BodySetConstantTorque(body, 0);
                    if (route == 1 && body == scene.GetRID()) scene.SetAxisVelocity(new(5, 0));
                    else PhysicsServer.BodySetAxisVelocity(body, new(5, 0));
                    Motion(body, new(5, velocity.Y + .1f), angular + 1f / 60, "Axis velocity preserves the perpendicular component");
                    var before = PhysicsServer.BodyGetLinearVelocity(body);
                    PhysicsServer.BodySetAxisVelocity(body, Vector2.Zero); Check(PhysicsServer.BodyGetLinearVelocity(body) == before, "Zero axis retains motion");
                }
            }
            ForceLifetimeChecks(tree, world, bodies);
            for (var i = 0; i < 64; i++) Cycle();
            GC.Collect(); GC.WaitForPendingFinalizers();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Cycle();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Edited scene/raw force, impulse, axis and full-frame cycles allocate {owner}/{all} managed bytes");
            Console.WriteLine($"Force family on {backend?.ToString() ?? "registered"}: rotated-center scene/server/direct response, transient/persistent totals and 128 edited full frames: {owner}/{all} B passed.");

            void Reset(RID body)
            {
                PhysicsServer.BodySetConstantForce(body, Vector2.Zero); PhysicsServer.BodySetConstantTorque(body, 0);
                PhysicsServer.BodySetTransform(body, new(MathF.PI / 2, body == scene.GetRID() ? Vector2.Zero : new Vector2(100, 0)));
                PhysicsServer.BodySetLinearVelocity(body, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(body, 0);
            }
            void Cycle() { Edit(); tree.PhysicsFrame(delta); }
            void Edit()
            {
                foreach (var body in bodies)
                {
                    PhysicsServer.BodyApplyCentralImpulse(body, new(.002f, 0)); PhysicsServer.BodyApplyImpulse(body, new(.001f, 0), new(1, 2)); PhysicsServer.BodyApplyTorqueImpulse(body, .0008f);
                    PhysicsServer.BodyApplyCentralForce(body, new(.02f, 0)); PhysicsServer.BodyApplyForce(body, new(.01f, 0), new(1, 2)); PhysicsServer.BodyApplyTorque(body, .008f);
                    PhysicsServer.BodySetConstantForce(body, new(.01f, 0)); PhysicsServer.BodySetConstantTorque(body, .004f);
                    PhysicsServer.BodyAddConstantCentralForce(body, new(.001f, 0)); PhysicsServer.BodyAddConstantForce(body, new(.001f, 0), new(1, 2));
                    PhysicsServer.BodyAddConstantTorque(body, .0004f);
                    var state = PhysicsServer.BodyGetDirectState(body)!;
                    Check(PhysicsServer.BodyGetConstantForce(body) == state.GetConstantForce() && PhysicsServer.BodyGetConstantTorque(body) == state.GetConstantTorque(), "Warm facade/direct totals agree");
                    state.ApplyCentralImpulse(new(.002f, 0)); state.ApplyImpulse(new(.001f, 0), new(1, 2)); state.ApplyTorqueImpulse(.0008f);
                    state.ApplyCentralForce(new(.02f, 0)); state.ApplyForce(new(.01f, 0), new(1, 2)); state.ApplyTorque(.008f);
                    state.SetConstantForce(new(.01f, 0)); state.SetConstantTorque(.004f); state.AddConstantCentralForce(new(.001f, 0));
                    state.AddConstantForce(new(.001f, 0), new(1, 2)); state.AddConstantTorque(.0004f);
                    PhysicsServer.BodySetAxisVelocity(body, new(.01f, 0));
                }
                scene.ApplyCentralImpulse(new(.002f, 0)); scene.ApplyImpulse(new(.001f, 0), new(1, 2)); scene.ApplyTorqueImpulse(.0008f);
                scene.ApplyCentralForce(new(.02f, 0)); scene.ApplyForce(new(.01f, 0), new(1, 2)); scene.ApplyTorque(.008f);
                scene.ConstantForce = new(.01f, 0); scene.ConstantTorque = .004f; scene.AddConstantCentralForce(new(.001f, 0));
                scene.AddConstantForce(new(.001f, 0), new(1, 2)); scene.AddConstantTorque(.0004f); scene.SetAxisVelocity(new(.01f, 0));
            }
        }
        finally { PhysicsServer.FreeRID(raw); }

        static void Motion(RID body, Vector2 linear, float angular, string reason)
        {
            Check(PhysicsServer.BodyGetLinearVelocity(body).DistanceTo(linear) < .0001f && MathF.Abs(PhysicsServer.BodyGetAngularVelocity(body) - angular) < .0001f, reason);
        }
    }
}
