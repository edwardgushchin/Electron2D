using Electron2D;

public static partial class BackendChecks
{
    public static void RunSleep(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { SleepChecks(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void SleepChecks(PhysicsServer.Backend? backend, bool threaded)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("Sleep", () => solver = new());
        using var world = backend is { } selected ? new World(selected) : new World("Sleep");
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
            "Sleep uses the requested implementation without fallback");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 2 };
        var scene = new RigidBody { GravityScale = 0, CanSleep = true, CollisionLayer = 0, CollisionMask = 0 };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        if (solver is not null)
        {
            var owner = Thread.CurrentThread;
            solver.StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "Sleep executes on the actual captured numerical thread");
        }
        var raw = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetCollisionLayer(raw, 0); PhysicsServer.BodySetCollisionMask(raw, 0);
            PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetTransform(raw, new(0, new Vector2(100, 0)));
            PhysicsServer.BodySetSpace(raw, world.Space);
            foreach (var body in new[] { scene.GetRID(), raw })
            {
                PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(body, 0);
                PhysicsServer.BodySetAngularDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(body, 0);
            }
            SetPolicy(2, .25f, .125f);
            PhysicsServer.BodySetAngularVelocity(scene.GetRID(), .125f); PhysicsServer.BodySetAngularVelocity(raw, -.125f);
            for (var i = 0; i < 4; i++) Step();
            Check(!scene.Sleeping && !PhysicsServer.BodyGetSleeping(raw), "Exact quiet-duration equality remains awake");
            Step(); Check(scene.Sleeping && PhysicsServer.BodyGetSleeping(raw), "Both scene/raw bodies sleep after the strict quiet duration");

            PhysicsServer.SpaceSetBodyTimeToSleep(world.Space, .25f);
            Check(!PhysicsServer.BodyGetSleeping(raw), "Changed sleep policy wakes immediately without a new tick");
            PhysicsServer.BodySetLinearVelocity(raw, new(2, 0)); for (var i = 0; i < 12; i++) Step();
            Check(!PhysicsServer.BodyGetSleeping(raw), "Linear threshold equality prevents sleep");
            PhysicsServer.BodySetLinearVelocity(raw, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(raw, -.25f);
            for (var i = 0; i < 12; i++) Step(); Check(!PhysicsServer.BodyGetSleeping(raw), "Angular threshold equality prevents sleep");
            PhysicsServer.BodySetAngularVelocity(raw, 0); PhysicsServer.BodySetCanSleep(raw, false);
            for (var i = 0; i < 12; i++) Step(); Check(!PhysicsServer.BodyGetSleeping(raw), "CanSleep false prevents automatic sleep");
            PhysicsServer.BodySetCanSleep(raw, true); SetPolicy(0, .25f, 0); Step();
            Check(!PhysicsServer.BodyGetSleeping(raw), "Zero linear threshold disables automatic sleep");
            SetPolicy(2, 0, 0); Step(); Check(!PhysicsServer.BodyGetSleeping(raw), "Zero angular threshold disables automatic sleep");
            SetPolicy(2, .25f, 0); tree.PhysicsFrame(0); Check(!PhysicsServer.BodyGetSleeping(raw), "Zero time never advances sleep");
            Step(); Check(PhysicsServer.BodyGetSleeping(raw), "Zero delay sleeps on the first positive quiet interval");
            PhysicsServer.BodyApplyCentralImpulse(raw, new(1, 0)); Check(!PhysicsServer.BodyGetSleeping(raw), "An explicit impulse wakes numerical sleep");
            PhysicsServer.BodySetLinearVelocity(raw, Vector2.Zero); SetPolicy(2, .25f, .125f);
            Step(); Step(); PhysicsServer.SpaceSetActive(world.Space, false);
            for (var i = 0; i < 12; i++) Step(); PhysicsServer.SpaceSetActive(world.Space, true);
            Step(); Check(!PhysicsServer.BodyGetSleeping(raw), "Inactive intervals do not age quiet time");
            Step(); Step(); Check(PhysicsServer.BodyGetSleeping(raw), "Reactivated world resumes only actual quiet intervals");
            SetPolicy(2, .25f, .5f); Step(); Step();
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            for (var i = 0; i < 20; i++) Step(); Check(PhysicsServer.BodyGetSleeping(raw), "The discarded future reaches sleep");
            checkpoint.Restore(); Check(!PhysicsServer.BodyGetSleeping(raw), "Checkpoint restores the earlier awake phase");
            for (var i = 0; i < 14; i++) Step(); Check(!PhysicsServer.BodyGetSleeping(raw), "Quiet-time replay preserves the strict half-second boundary");
            Step(); Check(PhysicsServer.BodyGetSleeping(raw), "Restored quiet clock reaches sleep at the same later interval");
            for (var i = 0; i < 128; i++) Cycle();
            Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Cycle();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Sleep/wake/replay full frames allocated {own}/{all} B");
            SleepGroups(tree, world.Space, scene.GetRID(), raw, circle, solver);
            Console.WriteLine($"Automatic sleep on {backend?.ToString() ?? "registered"}: strict thresholds/duration, scene/raw, pause, wake, quiet-clock replay and 128 cycles {own}/{all} B passed.");
            void Cycle() { checkpoint.Restore(); for (var i = 0; i < 15; i++) Step(); }
            void Step() => tree.PhysicsFrame(.03125);
            void SetPolicy(float linear, float angular, float time)
            {
                PhysicsServer.SpaceSetBodyLinearVelocitySleepThreshold(world.Space, linear);
                PhysicsServer.SpaceSetBodyAngularVelocitySleepThreshold(world.Space, angular);
                PhysicsServer.SpaceSetBodyTimeToSleep(world.Space, time);
            }
        }
        finally { PhysicsServer.FreeRID(raw); }
    }

    private static void SleepGroups(SceneTree tree, RID space, RID first, RID second, CircleShape circle, Solver? solver)
    {
        var joint = PhysicsServer.JointCreate(); RID ground = default; RID[] members = [first, second];
        try
        {
            PhysicsServer.SpaceSetBodyTimeToSleep(space, .125f);
            foreach (var body in members)
            {
                PhysicsServer.BodySetTransform(body, Transform.Identity); PhysicsServer.BodySetLinearVelocity(body, Vector2.Zero);
                PhysicsServer.BodySetAngularVelocity(body, 0); PhysicsServer.BodySetCanSleep(body, true);
                PhysicsServer.BodySetSleeping(body, false);
            }
            PhysicsServer.JointMakePin(joint, Vector2.Zero, first, second);
            Steps(6); Check(BothSleeping(), "A quiet pin-connected dynamic group sleeps together");
            PhysicsServer.BodyApplyCentralImpulse(first, new(8, 0)); Steps(1);
            Check(!PhysicsServer.BodyGetSleeping(first) && !PhysicsServer.BodyGetSleeping(second), "Impulse wakes the connected joint group");
            foreach (var body in members) { PhysicsServer.BodySetLinearVelocity(body, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(body, 0); }
            PhysicsServer.BodySetCanSleep(first, false); Steps(12);
            Check(!PhysicsServer.BodyGetSleeping(second), "CanSleep false on one member prevents collective group sleep");
            PhysicsServer.BodySetCanSleep(first, true); Steps(6);
            Check(BothSleeping(), $"Removing the group sleep veto permits later sleep: a={PhysicsServer.BodyGetLinearVelocity(first)}/{PhysicsServer.BodyGetCanSleep(first)}/{solver?.Bodies[first].SleepTime}, b={PhysicsServer.BodyGetLinearVelocity(second)}/{PhysicsServer.BodyGetCanSleep(second)}/{solver?.Bodies[second].SleepTime}");
            PhysicsServer.JointClear(joint);

            foreach (var body in members)
            {
                PhysicsServer.BodySetCollisionLayer(body, 1); PhysicsServer.BodySetCollisionMask(body, 1);
                PhysicsServer.BodySetSleeping(body, false);
            }
            PhysicsServer.BodySetTransform(first, Transform.Identity); PhysicsServer.BodySetTransform(second, new(0, new Vector2(4, 0)));
            Steps(8); Check(BothSleeping(), "A quiet touching contact group sleeps without zero-impulse wake churn");
            PhysicsServer.BodyApplyCentralImpulse(first, new(8, 0)); Steps(1);
            Check(!PhysicsServer.BodyGetSleeping(second), "Contact group receives physical wake propagation");

            ground = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(ground, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAddShape(ground, circle.GetRID()); PhysicsServer.BodySetSpace(ground, space);
            PhysicsServer.BodySetTransform(ground, Transform.Identity);
            PhysicsServer.BodySetTransform(first, new(0, new Vector2(-4, 0))); PhysicsServer.BodySetTransform(second, new(0, new Vector2(4, 0)));
            foreach (var body in members) { PhysicsServer.BodySetLinearVelocity(body, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(body, 0); PhysicsServer.BodySetSleeping(body, false); }
            PhysicsServer.BodySetCanSleep(first, false); Steps(8);
            Check(!PhysicsServer.BodyGetSleeping(first) && PhysicsServer.BodyGetSleeping(second),
                "A shared static support does not merge independent dynamic sleep groups");
            PhysicsServer.BodySetTransform(ground, new(0, new Vector2(1000, 0))); PhysicsServer.BodySetCanSleep(first, true);
            PhysicsServer.BodySetTransform(first, Transform.Identity); PhysicsServer.BodySetTransform(second, new(0, new Vector2(4, 0)));
            PhysicsServer.JointMakePin(joint, new(2, 0), first, second);
            for (var i = 0; i < 128; i++) Cycle();
            Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Cycle();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Contact/joint sleep-group transitions allocated {own}/{all} B");
            Console.WriteLine($"Contact/joint sleep groups: 128 full sleep/wake cycles {own}/{all} B.");
            void Cycle()
            {
                PhysicsServer.BodySetTransform(first, Transform.Identity); PhysicsServer.BodySetTransform(second, new(0, new Vector2(4, 0)));
                foreach (var body in members) { PhysicsServer.BodySetLinearVelocity(body, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(body, 0); }
                Steps(6); Check(BothSleeping(), "Measured contact/joint group reaches actual sleep");
                PhysicsServer.BodyApplyCentralImpulse(first, new(8, 0)); Steps(1);
                Check(!PhysicsServer.BodyGetSleeping(first) && !PhysicsServer.BodyGetSleeping(second), "Measured contact/joint group actually wakes");
            }
        }
        finally { PhysicsServer.FreeRID(joint); if (ground.IsValid()) PhysicsServer.FreeRID(ground); }
        bool BothSleeping() => PhysicsServer.BodyGetSleeping(first) && PhysicsServer.BodyGetSleeping(second);
        void Steps(int count) { for (var i = 0; i < count; i++) tree.PhysicsFrame(.03125); }
    }
}
