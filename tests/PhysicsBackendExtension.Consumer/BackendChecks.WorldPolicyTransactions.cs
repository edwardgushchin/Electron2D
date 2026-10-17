using Electron2D;

public static partial class BackendChecks
{
    private static (Action<float> Set, Func<float> Get, float First, float Second)[] WorldPolicies(RID space) =>
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

    public static void RunWorldPolicyTransactions(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null; var cases = 0;
        if (backend is null) PhysicsServerManager.RegisterServer("WorldPolicyTransactions", () => solver = new());
        try
        {
            if (backend is null)
            {
                for (var policy = 0; policy < 9; policy++)
                    for (var stage = 0; stage < 2; stage++)
                        for (var route = 0; route < 7; route++) Failure(policy, stage != 0, route);
                SleepRecovery();
                BatchRecovery();
            }
            Healthy();
            Console.WriteLine($"World policy transactions {backend?.ToString() ?? "registered"}/{threaded}: {cases} publication cases; 128 full frames 0/0 owner/all-thread B.");
        }
        finally { ClearHooks(); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }

        World Create() => backend is { } selected ? new World(selected) : new World("WorldPolicyTransactions");
        void ClearHooks() { if (solver is not null) solver.WorldPolicyHook = solver.WorldPolicyPublishedHook = solver.StepHook = null; }
        void Failure(int index, bool after, int route)
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var circle = new CircleShape { Radius = 2 };
            var body = new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(6, 0) };
            body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body);
            using var tree = new SceneTree(root); var raw = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.BodySetSpace(raw, world.Space); PhysicsServer.BodySetGravityScale(raw, 0);
                var policy = WorldPolicies(world.Space)[index]; policy.Set(policy.First);
                using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
                using var map = new PhysicsSnapshotMap(world.Space); var snapshot = new PhysicsSnapshot();
                map.Bind(1, 1, body.GetRID()); map.Bind(2, 1, raw); map.Capture(snapshot);
                policy.Set(policy.Second);
                var failure = new ApplicationException("world-policy-publication");
                if (after) solver!.WorldPolicyPublishedHook = () => throw failure;
                else solver!.WorldPolicyHook = () => throw failure;
                Original(() => policy.Set(policy.First)); ClearHooks();
                Check(policy.Get() == policy.First, "Failed publication retains complete world authoring");
                var calls = solver!.WorldPolicyCalls; var steps = solver.Steps; var tick = PhysicsServer.SpaceGetTick(world.Space);
                Task.Run(() => Reject<InvalidOperationException>(() => policy.Set(policy.First))).GetAwaiter().GetResult();
                Check(solver.WorldPolicyCalls == calls, "Off-owner retry invokes no selected policy hook");
                solver.WorldPolicyHook = () => throw failure;
                Original(Prepare);
                Check(solver.Steps == steps && PhysicsServer.SpaceGetTick(world.Space) == tick,
                    "Repeated preparation failure preserves its tick without solving");
                ClearHooks();
                solver.StepHook = () => Check(solver.ReadWorldPolicy(index) == policy.First, "Solve receives the current world policy");
                Prepare();
                Check(solver.ReadWorldPolicy(index) == policy.First, "Recovery completes selected world policy publication");
                calls = solver.WorldPolicyCalls; policy.Set(policy.First);
                Check(solver.WorldPolicyCalls == calls, "Healthy equal assignment preserves numerical policy and quiet clocks");
                if (route != 2) tree.PhysicsFrame(.01);
                Check(solver.Steps == steps + 1, "Recovery never replays numerical Solve"); cases++;
                void Original(Action action)
                {
                    var errors = ReleaseErrors(action);
                    Check(errors.Length == 1 && ReferenceEquals(errors[0], failure), $"World policy error identity: index={index}, after={after}, route={route}");
                }
                void Prepare()
                {
                    switch (route)
                    {
                        case 0: policy.Set(policy.First); break;
                        case 1:
                            using (var query = new PhysicsPointQueryParameters()) world.DirectSpaceState.IntersectPoint(query, new PhysicsPointResult[2]);
                            break;
                        case 2: tree.PhysicsFrame(.01); break;
                        case 3: checkpoint.Capture(); break;
                        case 4: map.Capture(snapshot); break;
                        case 5: checkpoint.Restore(); break;
                        case 6: map.Apply(snapshot); break;
                    }
                }
            }
            finally { ClearHooks(); PhysicsServer.FreeRID(raw); }
        }
        void BatchRecovery()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var tree = new SceneTree(root); var policies = WorldPolicies(world.Space);
            foreach (var policy in policies) policy.Set(policy.First);
            var failure = new ApplicationException("batch-world-policy");
            solver!.WorldPolicyHook = () => throw failure;
            foreach (var policy in policies) Reject<ApplicationException>(() => policy.Set(policy.Second));
            var calls = solver.WorldPolicyCalls;
            tree.PhysicsFrame(0);
            PhysicsServer.SetActive(false);
            try { tree.PhysicsFrame(.01); }
            finally { PhysicsServer.SetActive(true); }
            Check(solver.WorldPolicyCalls == calls && solver.Steps == 0, "Skipped intervals do not prepare pending world policies");
            solver.WorldPolicyHook = () => { if (solver.WorldPolicyCalls - calls == 3) throw failure; };
            using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            Reject<ApplicationException>(() => world.DirectSpaceState.IntersectPoint(query, hits));
            Check(solver.WorldPolicyCalls - calls == 3 && solver.Steps == 0, "Batch recovery stops at the original preparation error");
            ClearHooks(); calls = solver.WorldPolicyCalls;
            world.DirectSpaceState.IntersectPoint(query, hits);
            Check(solver.WorldPolicyCalls - calls == 2, "Only the two incomplete policy families retry");
            for (var i = 0; i < policies.Length; i++) Check(solver.ReadWorldPolicy(i) == policies[i].Second, "Batch publication keeps every current profile value");
            solver.WorldPolicyHook = () => throw failure;
            Reject<ApplicationException>(() => policies[0].Set(policies[0].First)); calls = solver.WorldPolicyCalls;
            tree.Dispose(); world.Dispose();
            Check(solver.WorldPolicyCalls == calls, "Retirement discards pending policy without invoking the failed hook"); ClearHooks();
        }
        void SleepRecovery()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var circle = new CircleShape { Radius = 2 };
            var body = new RigidBody { GravityScale = 0, LinearVelocity = new(6, 0) };
            body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body);
            using var tree = new SceneTree(root);
            PhysicsServer.SpaceSetBodyTimeToSleep(world.Space, .01f);
            solver!.WorldPolicyHook = () => throw new ApplicationException("sleep-policy-publication");
            Reject<ApplicationException>(() => PhysicsServer.SpaceSetBodyLinearVelocitySleepThreshold(world.Space, 100)); ClearHooks();
            tree.PhysicsFrame(.02); tree.PhysicsFrame(.02);
            Check(body.Sleeping, "A retained sleep policy controls actual numerical sleep after recovery");
        }
        void Healthy()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            using var circle = new CircleShape { Radius = 2 };
            var body = new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(6, 0) };
            body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body);
            using var tree = new SceneTree(root); var raw = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.BodySetSpace(raw, world.Space); PhysicsServer.BodySetGravityScale(raw, 0);
                var policies = WorldPolicies(world.Space); var callback = 0;
                PhysicsServer.BodySetStateSyncCallback(raw, _ => Edit(callback++));
                for (var i = 0; i < 128; i++) Frame(i);
                Collect(); for (var i = 0; i < 128; i++) Frame(i);
                var before = callback; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame(i);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(callback - before == 128 && own == 0 && all == 0, $"Edited complete world policy frames: callbacks={callback - before}, bytes={own}/{all}");
                Check(body.Position.X > 0, "World policy frames include actual numerical motion");
                PhysicsServer.BodySetStateSyncCallback(raw, null);
                void Edit(int i) { foreach (var policy in policies) policy.Set((i & 1) == 0 ? policy.First : policy.Second); }
                void Frame(int i) { Edit(i); tree.PhysicsFrame(.01); }
            }
            finally { PhysicsServer.FreeRID(raw); }
        }
    }
}
