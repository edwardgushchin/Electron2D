using Electron2D;

public static partial class BackendChecks
{
    public static void RunExceptionTransactions(bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null; var cases = 0;
        PhysicsServerManager.RegisterServer("ExceptionTransactions", () => solver = new());
        try
        {
            for (var role = 0; role < 3; role++)
                for (var scene = 0; scene < 2; scene++)
                    foreach (var after in new[] { true, false })
                        foreach (var requested in new[] { true, false })
                            for (var trigger = 0; trigger < 6; trigger++) Run(role, scene != 0, after, requested, trigger);
            Console.WriteLine($"Exception transactions/{threaded}: {cases} role/route/before-after/recovery cases passed.");
            ExceptionRetirement(threaded);
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }

        void Run(int role, bool scene, bool after, bool requested, int trigger)
        {
            using var world = new World("ExceptionTransactions"); using var root = new SubViewport { World = world };
            var first = new RigidBody { GravityScale = 0, CanSleep = false }; var second = new StaticBody(); root.AddChild(first); root.AddChild(second);
            Joint? node = scene ? role switch { 0 => new PinJoint(), 1 => new GrooveJoint(), _ => new DampedSpringJoint() } : null;
            if (node is not null) root.AddChild(node);
            using var tree = new SceneTree(root); var joint = node?.GetRID() ?? PhysicsServer.JointCreate(); var owner = Thread.CurrentThread;
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            try
            {
                MakeJoint(joint, role, first.GetRID(), second.GetRID()); Set(!requested);
                using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
                using var map = new PhysicsSnapshotMap(world.Space); var snapshot = new PhysicsSnapshot();
                map.Bind(1, 1, first.GetRID()); map.Bind(2, 1, second.GetRID()); map.Bind(3, 1, joint);
                var failure = new ApplicationException("joint-collision-publication"); var calls = 0; var expected = requested; var hookConsistent = true; var queryGuard = true;
                solver!.StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "Recovered collision policy uses the actual numerical thread");
                Action<RID> hook = id =>
                {
                    if (id != joint) return; calls++;
                    hookConsistent &= PhysicsServer.JointIsDisabledCollisionsBetweenBodies(joint) == expected && Listed() == expected;
                    Reject<InvalidOperationException>(() => Set(expected));
                    Exception? queryError = null;
                    try { world.DirectSpaceState.IntersectPoint(point, hits); } catch (Exception error) { queryError = error; }
                    queryGuard &= queryError is InvalidOperationException;
                    throw failure;
                };
                Install();
                var errors = ReleaseErrors(() => Set(requested));
                Check(errors.Length == 1 && ReferenceEquals(errors[0], failure), "Collision publication preserves its original error");
                if (!(PhysicsServer.JointIsDisabledCollisionsBetweenBodies(joint) == requested && Listed() == requested))
                    throw new InvalidOperationException("Failed joint collision publication lost its common contribution.");
                Check(calls == 1 && hookConsistent && queryGuard, "Borrowed publication reads retain the request, reject live queries and perform no repair");
                var tick = PhysicsServer.SpaceGetTick(world.Space); tree.PhysicsFrame(0);
                PhysicsServer.SpaceSetActive(world.Space, false); tree.PhysicsFrame(.01); PhysicsServer.SpaceSetActive(world.Space, true);
                Check(calls == 1 && PhysicsServer.SpaceGetTick(world.Space) == tick, "Zero/inactive frames preserve pending policy without solving or repair");
                expected = trigger == 5 ? !requested : requested;
                errors = ReleaseErrors(Attempt);
                Check(errors.Length == 1 && ReferenceEquals(errors[0], failure) && PhysicsServer.SpaceGetTick(world.Space) == tick,
                    "Repeated preparation failure preserves error and completed tick");
                Check(PhysicsServer.JointIsDisabledCollisionsBetweenBodies(joint) == expected && Listed() == expected, "Repeated errors retain the latest complete common request");
                solver.JointHook = solver.JointCollisionHook = null;
                Attempt();
                Check(PhysicsServer.JointIsDisabledCollisionsBetweenBodies(joint) == expected && solver.Joints[joint].Settings.DisableCollision == expected && Listed() == expected,
                    "Successful retry publishes the retained collision policy without losing identity");
                if (trigger != 4) tree.PhysicsFrame(.01);
                Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1, "Recovery advances exactly one actual solver interval");
                cases++;

                void Set(bool value) { if (node is null) PhysicsServer.JointDisableCollisionsBetweenBodies(joint, value); else node.DisableCollision = value; }
                bool Listed() => solver!.ReadExceptions(first.GetRID()).Contains(second.GetRID());
                void Install() { if (after) solver.JointHook = hook; else solver.JointCollisionHook = hook; }
                void Attempt()
                {
                    switch (trigger)
                    {
                        case 0: Set(expected); break;
                        case 1: Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0, "Repaired query has no unrelated geometry"); break;
                        case 2: checkpoint.Capture(); break;
                        case 3: map.Capture(snapshot); break;
                        case 4: tree.PhysicsFrame(.01); break;
                        default: Set(expected); break;
                    }
                }
            }
            finally
            {
                solver!.JointHook = solver.JointCollisionHook = null; solver.StepHook = null;
                if (node is null) PhysicsServer.FreeRID(joint); else node.Dispose();
            }
        }
    }
    private static void ExceptionRetirement(bool threaded)
    {
        Solver? solver = null; var cases = 0;
        PhysicsServerManager.RegisterServer("ExceptionRetirement", () => solver = new());
        for (var role = 0; role < 3; role++)
            for (var after = 0; after < 2; after++)
                for (var route = 0; route < 4; route++) Run(role, after != 0, route);
        Console.WriteLine($"Exception retirement/{threaded}: {cases} pending clear/free/departure/scene and independent-union cases passed.");

        void Run(int role, bool after, int route)
        {
            using var world = new World("ExceptionRetirement"); using var root = new SubViewport { World = world };
            var first = new RigidBody { GravityScale = 0 }; var second = new StaticBody(); root.AddChild(first); root.AddChild(second);
            Joint? node = route == 3 ? role switch { 0 => new PinJoint(), 1 => new GrooveJoint(), _ => new DampedSpringJoint() } : null;
            if (node is not null) root.AddChild(node);
            using var tree = new SceneTree(root); var joint = node?.GetRID() ?? PhysicsServer.JointCreate(); var other = PhysicsServer.JointCreate(); var alive = true;
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            try
            {
                MakeJoint(joint, role, first.GetRID(), second.GetRID()); MakeJoint(other, 0, first.GetRID(), second.GetRID());
                PhysicsServer.JointDisableCollisionsBetweenBodies(joint, false);
                var failure = new ApplicationException("retired-collision-publication"); var calls = 0;
                Action<RID> hook = id => { if (id == joint) { calls++; throw failure; } };
                if (after) solver!.JointHook = hook; else solver!.JointCollisionHook = hook;
                Check(ReleaseErrors(() => PhysicsServer.JointDisableCollisionsBetweenBodies(joint, true)).Any(e => ReferenceEquals(e, failure)), "Retirement begins with a real incomplete collision publication");
                Check(Listed(), "Independent active joints contribute their common union");
                if (route == 0) PhysicsServer.JointClear(joint);
                else if (route == 1)
                {
                    var invalidation = new ApplicationException("collision-invalidation-cleanup"); var detach = new ApplicationException("collision-detach-cleanup");
                    solver.ExceptionWakeHook = id => { if (id == first.GetRID()) throw invalidation; };
                    solver.JointReleaseHook = id => { if (id == joint) throw detach; };
                    alive = false; var errors = ReleaseErrors(() => PhysicsServer.FreeRID(joint));
                    Check(errors.Length == 2 && errors.Any(e => ReferenceEquals(e, invalidation)) && errors.Any(e => ReferenceEquals(e, detach)), "Failed common invalidation still detaches and reports both original cleanup errors");
                }
                else if (route == 2) PhysicsServer.BodySetSpace(first.GetRID(), default);
                else { node!.Dispose(); alive = false; }
                Check(calls == 1, "Retirement never retries a selected policy on the discarded attachment");
                solver.JointHook = solver.JointCollisionHook = solver.ExceptionWakeHook = solver.JointReleaseHook = null;
                if (route == 2)
                {
                    Check(!Listed(), "Inactive connections contribute no pair edge"); PhysicsServer.BodySetSpace(first.GetRID(), world.Space);
                    Check(solver.Joints[joint].Settings.DisableCollision, "Reentry publishes the retained requested flag to a fresh attachment");
                    PhysicsServer.JointClear(joint);
                }
                Check(Listed() && world.DirectSpaceState.IntersectPoint(point, hits) == 0 && calls == 1, "Retirement preserves the other contribution and removes pending publication work");
                PhysicsServer.BodyAddCollisionException(first.GetRID(), second.GetRID()); PhysicsServer.JointDisableCollisionsBetweenBodies(other, false);
                Check(Listed(), "An explicit exception remains after the final active-joint edge retires");
                PhysicsServer.BodyRemoveCollisionException(first.GetRID(), second.GetRID()); Check(!Listed(), "No duplicate or stolen joint edge remains after cleanup"); cases++;
            }
            finally
            {
                solver!.JointHook = solver.JointCollisionHook = solver.ExceptionWakeHook = solver.JointReleaseHook = null;
                PhysicsServer.FreeRID(other);
                if (alive) { if (node is null) PhysicsServer.FreeRID(joint); else node.Dispose(); }
            }
            bool Listed() => solver!.ReadExceptions(first.GetRID()).Contains(second.GetRID());
        }
    }

}
