using Electron2D;

public static partial class BackendChecks
{
    public static void RunJointLifecycle(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { JointLifecycleChecks(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void JointLifecycleChecks(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("JointLifecycle", () => solver = new());
        using var world = backend is { } selected ? new World(selected) : new World("JointLifecycle");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 3 };
        var scene = new RigidBody { GravityScale = 0, CanSleep = false };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        Joint[] nodes = [new PinJoint(), new GrooveJoint(), new DampedSpringJoint()];
        foreach (var node in nodes) root.AddChild(node);
        using var tree = new SceneTree(root);
        var ownerThread = Thread.CurrentThread;
        if (solver is not null) solver.StepHook = () => Check((Thread.CurrentThread != ownerThread) == threaded, "Lifecycle uses the actual selected numerical thread");
        var raw = PhysicsServer.BodyCreate();
        var joints = new[] { PhysicsServer.JointCreate(), PhysicsServer.JointCreate(), PhysicsServer.JointCreate() };
        var spare = PhysicsServer.JointCreate();
        var pending = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetTransform(raw, new(0, new Vector2(100, 0)));
            PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false); PhysicsServer.BodySetSpace(raw, world.Space);
            var all = joints.Concat(nodes.Select(node => node.GetRID())).ToArray();
            var guarded = false; var hooks = 0;
            if (solver is not null)
            {
                Action<RID> guard = id =>
                {
                    if (guarded) return;
                    guarded = true;
                    try
                    {
                        hooks++;
                        var role = PhysicsServer.JointGetType(id); var before = JointParameterState(id);
                        var attachment = solver.Joints[id];
                        Reject<InvalidOperationException>(() => PhysicsServer.JointClear(id));
                        Reject<InvalidOperationException>(() => MakeJoint(id, (int)role, scene.GetRID(), raw));
                        Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(id));
                        foreach (var node in nodes)
                        {
                            var sceneID = node.GetRID();
                            Reject<InvalidOperationException>(() => PhysicsServer.JointClear(sceneID));
                            Reject<InvalidOperationException>(node.Dispose);
                            Check(!node.IsDisposed && node.GetRID() == sceneID, "Rejected scene disposal preserves owner and RID");
                        }
                        for (var i = 0; i < 3; i++) Reject<InvalidOperationException>(() => MakeJoint(spare, i, scene.GetRID(), raw));
                        for (var i = 0; i < 3; i++) Reject<InvalidOperationException>(() => MakeJoint(spare, i, pending, raw));
                        Reject<InvalidOperationException>(() => PhysicsServer.JointMakePin(spare, Vector2.Zero, raw));
                        Check(PhysicsServer.JointGetType(id) == role && JointParameterState(id) == before &&
                            ReferenceEquals(solver.Joints[id], attachment), "Numerical hooks preserve joint role, profile and attachment");
                        Check(PhysicsServer.JointGetType(spare) == PhysicsServer.JointType.Empty, "Incoming endpoints reject before configuring an empty joint");
                    }
                    finally { guarded = false; }
                };
                solver.JointHook = solver.JointFactoryHook = solver.JointReleaseHook = guard;
            }
            for (var i = 0; i < all.Length; i++) MakeJoint(all[i], i % 3, scene.GetRID(), raw);
            if (solver is not null)
            {
                Check(hooks >= 6, "All scene/raw roles exercise initial hooks");
                foreach (var id in all) PhysicsServer.JointSetBias(id, .2f);
                for (var i = 0; i < all.Length; i++) MakeJoint(all[i], i % 3, scene.GetRID(), raw);
                solver.JointHook = solver.JointFactoryHook = solver.JointReleaseHook = null;
                JointLifecycleFailures(solver, scene.GetRID(), raw, spare);
            }
            PhysicsServer.JointMakePin(spare, Vector2.Zero, pending, raw);
            Check(PhysicsServer.JointGetType(spare) == PhysicsServer.JointType.Pin && (solver is null || !solver.Joints.ContainsKey(spare)), "Detached first endpoint retains a pending selected connection");
            PhysicsServer.BodySetSpace(pending, world.Space);
            Check(solver is null || solver.Joints.ContainsKey(spare), "Endpoint entry creates the pending selected attachment");
            PhysicsServer.FreeRID(pending); pending = default;
            Check(PhysicsServer.JointGetType(spare) == PhysicsServer.JointType.Empty && (solver is null || !solver.Joints.ContainsKey(spare)), "Endpoint release clears dependent role and numerical attachment while retaining joint RID");
            JointLifecycleIdentity(tree, world, scene.GetRID(), raw, all, solver);
            JointParameterMotion(tree, scene, raw, all, joints[2]);
            for (var i = 0; i < all.Length; i++) { MakeJoint(all[i], i % 3, scene.GetRID(), raw); SetJointParameters(all[i], 1); }
            JointLifecycleContracts(tree, world, scene, raw, nodes, joints, solver);
            for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
            GC.Collect(); GC.WaitForPendingFinalizers();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var total = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; total = GC.GetTotalAllocatedBytes(true) - total;
            Check(owner == 0 && total == 0, $"Joint lifecycle full frames allocated {owner}/{total} B");
            Console.WriteLine($"Joint lifecycle on {backend?.ToString() ?? "registered"}: identity/borrow guards, real spring and 64 full frames {owner}/{total} B passed.");
        }
        finally
        {
            if (solver is not null) solver.JointHook = solver.JointFactoryHook = solver.JointReleaseHook = null;
            foreach (var id in joints) PhysicsServer.FreeRID(id); PhysicsServer.FreeRID(spare); PhysicsServer.FreeRID(raw);
            if (pending.IsValid()) PhysicsServer.FreeRID(pending);
        }
    }

    private static void MakeJoint(RID id, int role, RID first, RID second)
    {
        switch (role)
        {
            case 0: PhysicsServer.JointMakePin(id, Vector2.Zero, first, second); break;
            case 1: PhysicsServer.JointMakeGroove(id, new(-10, 0), new(10, 0), Vector2.Zero, first, second); break;
            case 2: PhysicsServer.JointMakeDampedSpring(id, Vector2.Zero, new(100, 0), first, second); break;
            default: throw new ArgumentOutOfRangeException(nameof(role));
        }
    }
}
