using Electron2D;

public static partial class BackendChecks
{
    private static void WorldPolicyChecks()
    {
        Solver? solver = null;
        PhysicsServerManager.RegisterServer("WorldPolicy", () => solver = new());
        using var world = new World("WorldPolicy"); var space = world.Space;
        (Action<float> Set, Func<float> Get, float First, float Second)[] policies =
        [
            (v => PhysicsServer.SpaceSetBodyLinearVelocitySleepThreshold(space, v), () => PhysicsServer.SpaceGetBodyLinearVelocitySleepThreshold(space), 3, 4),
            (v => PhysicsServer.SpaceSetBodyAngularVelocitySleepThreshold(space, v), () => PhysicsServer.SpaceGetBodyAngularVelocitySleepThreshold(space), .2f, .3f),
            (v => PhysicsServer.SpaceSetBodyTimeToSleep(space, v), () => PhysicsServer.SpaceGetBodyTimeToSleep(space), .75f, .8f),
            (v => PhysicsServer.SpaceSetContactRecycleRadius(space, v), () => PhysicsServer.SpaceGetContactRecycleRadius(space), 2, 3),
            (v => PhysicsServer.SpaceSetContactMaxSeparation(space, v), () => PhysicsServer.SpaceGetContactMaxSeparation(space), 3, 4),
            (v => PhysicsServer.SpaceSetContactDefaultBias(space, v), () => PhysicsServer.SpaceGetContactDefaultBias(space), .4f, .5f),
            (v => PhysicsServer.SpaceSetContactMaxAllowedPenetration(space, v), () => PhysicsServer.SpaceGetContactMaxAllowedPenetration(space), .3f, .4f),
            (v => PhysicsServer.SpaceSetSolverIterations(space, (int)v), () => PhysicsServer.SpaceGetSolverIterations(space), 7, 8),
            (v => PhysicsServer.SpaceSetConstraintDefaultBias(space, v), () => PhysicsServer.SpaceGetConstraintDefaultBias(space), .4f, .5f)
        ];
        foreach (var policy in policies) { policy.Set(policy.First); Check(policy.Get() == policy.First, "World policies retain authored values"); }
        var raw = PhysicsServer.BodyCreate();
        using var circle = new CircleShape { Radius = 3 };
        using var root = new SubViewport { World = world };
        var scene = new RigidBody { GravityScale = 0, CanSleep = false };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID());
            solver!.IdentityHook = _ => Borrowed();
            PhysicsServer.BodySetSpace(raw, space); solver.IdentityHook = null;
            using var tree = new SceneTree(root);
            solver.IdentityHook = _ => Borrowed();
            PhysicsServer.BodyAttachCanvasInstanceID(raw, 17); solver.IdentityHook = null;
            solver.WorldPolicyHook = Borrowed;
            foreach (var policy in policies) policy.Set(policy.Second);
            PhysicsServer.SpaceSetActive(space, false); PhysicsServer.SpaceSetActive(space, true);
            solver.WorldPolicyHook = null;
            var callbacks = 0;
            PhysicsServer.BodySetStateSyncCallback(raw, _ =>
            {
                foreach (var policy in policies) policy.Set(policy.First);
                PhysicsServer.SpaceSetActive(space, true); callbacks++;
            });
            tree.PhysicsFrame(1d / 60);
            Check(callbacks == 1 && policies.All(p => p.Get() == p.First), "Ordinary post-solver callbacks can edit world policies");
            PhysicsServer.BodySetStateSyncCallback(raw, null);
            Task.Run(() => { foreach (var policy in policies) Reject<InvalidOperationException>(() => policy.Set(policy.Second)); }).GetAwaiter().GetResult();
            Collect(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) EditAndStep(i);
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) EditAndStep(i);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"World policy edited full frames allocated {own}/{all} owner/all-thread B");
            Console.WriteLine("Registered world policies: initial/live/nested borrow rejection, ordinary callback edits and 0/0 B full frames passed.");
            void EditAndStep(int i)
            {
                foreach (var policy in policies) policy.Set((i & 1) == 0 ? policy.First : policy.Second);
                PhysicsServer.SpaceSetActive(space, false); PhysicsServer.SpaceSetActive(space, true);
                tree.PhysicsFrame(1d / 60);
            }
        }
        finally { solver!.IdentityHook = null; solver.WorldPolicyHook = null; PhysicsServer.FreeRID(raw); }
        void Borrowed()
        {
            var calls = solver!.WorldPolicyCalls;
            foreach (var policy in policies)
            {
                var before = policy.Get();
                Reject<InvalidOperationException>(() => policy.Set(before == policy.First ? policy.Second : policy.First));
                Reject<InvalidOperationException>(() => policy.Set(before));
                Check(policy.Get() == before, "Borrowed world writes reject before changing authored values");
            }
            var active = PhysicsServer.SpaceIsActive(space);
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetActive(space, !active));
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetActive(space, active));
            Check(PhysicsServer.SpaceIsActive(space) == active && solver.WorldPolicyCalls == calls,
                "Borrowed activation and policy edits invoke no nested selected hooks");
        }
    }
}
