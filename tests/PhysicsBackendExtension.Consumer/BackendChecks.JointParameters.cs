using Electron2D;

public static partial class BackendChecks
{
    public static void RunJointParameters(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { JointParameterChecks(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void JointParameterChecks(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("JointParameters", () => solver = new());
        using var world = backend is { } selected ? new World(selected) : new World("JointParameters");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 3 };
        var scene = new RigidBody { GravityScale = 0, CanSleep = false };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        Joint[] nodes = [new PinJoint(), new GrooveJoint(), new DampedSpringJoint()];
        foreach (var node in nodes) root.AddChild(node);
        using var tree = new SceneTree(root);
        var ownerThread = Thread.CurrentThread;
        if (solver is not null) solver.StepHook = () => Check((Thread.CurrentThread != ownerThread) == threaded, "Parameters use the actual selected numerical thread");
        var raw = PhysicsServer.BodyCreate();
        var joints = new[] { PhysicsServer.JointCreate(), PhysicsServer.JointCreate(), PhysicsServer.JointCreate() };
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetTransform(raw, new(0, new Vector2(100, 0)));
            PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false); PhysicsServer.BodySetSpace(raw, world.Space);
            var sceneIDs = nodes.Select(node => node.GetRID()).ToArray();
            var all = joints.Concat(sceneIDs).ToArray();
            var guarded = false; var hooks = 0;
            if (solver is not null) solver.JointHook = id =>
            {
                if (guarded) return;
                guarded = true;
                try { hooks++; RejectParameterWrites(id); RejectSceneJointWrites(nodes); }
                finally { guarded = false; }
            };
            foreach (var ids in new[] { joints, sceneIDs })
            {
                PhysicsServer.JointMakePin(ids[0], Vector2.Zero, scene.GetRID(), raw);
                PhysicsServer.JointMakeGroove(ids[1], new(-10, 0), new(10, 0), Vector2.Zero, scene.GetRID(), raw);
                PhysicsServer.JointMakeDampedSpring(ids[2], Vector2.Zero, new(100, 0), scene.GetRID(), raw);
            }
            if (solver is not null)
            {
                Check(hooks >= 6, "Every raw/scene role invokes its initial numerical attachment hook");
                solver.BodyHook = _ =>
                {
                    foreach (var id in all) RejectParameterWrites(id);
                    RejectSceneJointWrites(nodes);
                };
                try { PhysicsServer.BodySetLinearVelocity(raw, Vector2.Zero); }
                finally { solver.BodyHook = null; }
            }
            var jointHook = solver?.JointHook;
            for (var i = 0; i < 2; i++)
            {
                if (solver is not null) solver.JointHook = jointHook;
                foreach (var id in all) SetJointParameters(id, i);
                foreach (var node in nodes) Check(node.Bias == PhysicsServer.JointGetBias(node.GetRID()) && node.MaxBias == PhysicsServer.JointGetMaxBias(node.GetRID()) && node.MaxForce == PhysicsServer.JointGetMaxForce(node.GetRID()), "Scene descriptors share the typed server profile");
                ((PinJoint)nodes[0]).Softness = .25f;
                ((DampedSpringJoint)nodes[2]).Stiffness = .5f;
                if (solver is not null) solver.JointHook = null;
                tree.PhysicsFrame(1d / 60);
                foreach (var id in all) VerifyJointParameters(id, solver);
            }
            if (solver is not null) solver.JointHook = null;
            JointParameterLifetime(tree, world, raw, all, solver);
            JointParameterMotion(tree, scene, raw, all, joints[2]);
            foreach (var ids in new[] { joints, sceneIDs })
            {
                PhysicsServer.JointMakePin(ids[0], Vector2.Zero, scene.GetRID(), raw);
                PhysicsServer.JointMakeGroove(ids[1], new(-10, 0), new(10, 0), Vector2.Zero, scene.GetRID(), raw);
                PhysicsServer.JointMakeDampedSpring(ids[2], Vector2.Zero, new(100, 0), scene.GetRID(), raw);
            }
            JointParameterAccess(tree, world, raw, nodes, joints, solver);
            for (var i = 0; i < 64; i++) Cycle(i);
            GC.Collect(); GC.WaitForPendingFinalizers();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var total = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Cycle(i);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; total = GC.GetTotalAllocatedBytes(true) - total;
            Check(owner == 0 && total == 0, $"Edited joint parameter/full-frame cycles allocated {owner}/{total} B");
            void Cycle(int index) { foreach (var id in all) SetJointParameters(id, index & 1); tree.PhysicsFrame(1d / 60); }
            Console.WriteLine($"Joint parameter guards on {backend?.ToString() ?? "registered"} passed: numerical hooks, real capped spring response and 64 edited full frames {owner}/{total} B.");
        }
        finally { if (solver is not null) { solver.BodyHook = null; solver.JointHook = null; } foreach (var id in joints) PhysicsServer.FreeRID(id); PhysicsServer.FreeRID(raw); }
    }

    private static void RejectParameterWrites(RID id)
    {
        var before = JointParameterState(id);
        Reject<InvalidOperationException>(() => PhysicsServer.JointSetBias(id, .4f));
        Reject<InvalidOperationException>(() => PhysicsServer.JointSetBias(id, PhysicsServer.JointGetBias(id)));
        Reject<InvalidOperationException>(() => PhysicsServer.JointSetMaxBias(id, 7));
        Reject<InvalidOperationException>(() => PhysicsServer.JointSetMaxForce(id, 8));
        switch (PhysicsServer.JointGetType(id))
        {
            case PhysicsServer.JointType.Pin:
                Reject<InvalidOperationException>(() => PhysicsServer.PinJointSetSoftness(id, .2f));
                Reject<InvalidOperationException>(() => PhysicsServer.PinJointSetAngularLimitEnabled(id, true));
                Reject<InvalidOperationException>(() => PhysicsServer.PinJointSetAngularLimitLower(id, -.2f));
                Reject<InvalidOperationException>(() => PhysicsServer.PinJointSetAngularLimitUpper(id, .2f));
                Reject<InvalidOperationException>(() => PhysicsServer.PinJointSetMotorEnabled(id, true));
                Reject<InvalidOperationException>(() => PhysicsServer.PinJointSetMotorTargetVelocity(id, 2));
                Reject<InvalidOperationException>(() => PhysicsServer.PinJointSetMotorMaxTorque(id, 3));
                break;
            case PhysicsServer.JointType.DampedSpring:
                Reject<InvalidOperationException>(() => PhysicsServer.DampedSpringJointSetRestLength(id, 20));
                Reject<InvalidOperationException>(() => PhysicsServer.DampedSpringJointSetStiffness(id, 2));
                Reject<InvalidOperationException>(() => PhysicsServer.DampedSpringJointSetDamping(id, 3));
                break;
        }
        Check(JointParameterState(id) == before, "Rejected numerical parameter writes preserve all retained getters");
    }
}
