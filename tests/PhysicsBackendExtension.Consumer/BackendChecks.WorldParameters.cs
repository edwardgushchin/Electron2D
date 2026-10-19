using Electron2D;

public static partial class BackendChecks
{
    public static void RunWorldParameters(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("WorldParameters", () => solver = new());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("WorldParameters");
            var space = world.Space; var policies = WorldPolicies(space); var owner = Thread.CurrentThread;
            Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null, "World parameters retain the requested selected backend");
            float[] expected = [ProjectSettings.GetWithOverride(ProjectSettings.Physics2DSleepThresholdLinear), ProjectSettings.GetWithOverride(ProjectSettings.Physics2DSleepThresholdAngular),
                ProjectSettings.GetWithOverride(ProjectSettings.Physics2DTimeBeforeSleep), ProjectSettings.GetWithOverride(ProjectSettings.Physics2DContactRecycleRadius),
                ProjectSettings.GetWithOverride(ProjectSettings.Physics2DContactMaxSeparation), ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultContactBias),
                ProjectSettings.GetWithOverride(ProjectSettings.Physics2DContactMaxAllowedPenetration), ProjectSettings.GetWithOverride(ProjectSettings.Physics2DSolverIterations),
                ProjectSettings.GetWithOverride(ProjectSettings.Physics2DDefaultConstraintBias)];
            var reads = 0; var callbacks = 0; var queries = 0; var solves = 0; var invalid = 0;
            ReadValues();
            for (var i = 0; i < policies.Length; i++)
            {
                var policy = policies[i]; policy.Set(policy.First); expected[i] = policy.First;
                Reject<ArgumentOutOfRangeException>(() => policy.Set(-1)); invalid++;
                if (i != 7)
                {
                    Reject<ArgumentOutOfRangeException>(() => policy.Set(float.NaN)); Reject<ArgumentOutOfRangeException>(() => policy.Set(float.PositiveInfinity)); invalid += 2;
                    policy.Set(0); Check(policy.Get() == 0, "Zero is retained for every float-valued world policy");
                }
                else { Reject<ArgumentOutOfRangeException>(() => PhysicsServer.SpaceSetSolverIterations(space, 0)); invalid++; PhysicsServer.SpaceSetSolverIterations(space, int.MaxValue); Check(PhysicsServer.SpaceGetSolverIterations(space) == int.MaxValue, "Positive iteration requests are not clamped"); }
                policy.Set(policy.First); Check(policy.Get() == expected[i], "Invalid policy candidates preserve the accepted value");
            }
            foreach (var i in new[] { 5, 8 }) { var p = policies[i]; Reject<ArgumentOutOfRangeException>(() => p.Set(1.01f)); invalid++; p.Set(1); Check(p.Get() == 1, "Unit bias endpoint is retained"); p.Set(p.First); }
            using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 2 };
            var scene = new RigidBody { GravityScale = 0, CanSleep = false }; scene.AddChild(new CollisionShape { Shape = shape, Disabled = true }); scene.AddChild(new CollisionShape { Shape = shape }); root.AddChild(scene);
            using var tree = new SceneTree(root); var raw = PhysicsServer.BodyCreate();
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1]; RID[] bodies = [scene.GetRID(), raw];
            try
            {
                PhysicsServer.BodyAddShape(raw, shape.GetRID(), disabled: true); PhysicsServer.BodyAddShape(raw, shape.GetRID()); PhysicsServer.BodySetSpace(raw, space);
                foreach (var id in bodies)
                {
                    PhysicsServer.BodySetGravityScale(id, 0); PhysicsServer.BodySetCanSleep(id, false); PhysicsServer.BodySetCollisionMask(id, 0);
                    PhysicsServer.BodySetLinearDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(id, 0); PhysicsServer.BodySetAngularDamp(id, 0);
                }
                var stale = PhysicsServer.SpaceCreate(PhysicsServer.Backend.CPU); PhysicsServer.FreeRID(stale);
                foreach (var bad in new[] { raw, shape.GetRID(), stale, default })
                    foreach (var policy in WorldPolicies(bad)) { Reject<ArgumentException>(() => policy.Get()); Reject<ArgumentException>(() => policy.Set(policy.First)); }
                Task.Run(() => { foreach (var p in policies) { Reject<InvalidOperationException>(() => p.Get()); Reject<InvalidOperationException>(() => p.Set(p.First)); } }).GetAwaiter().GetResult();
                if (solver is not null)
                {
                    var rejected = 0;
                    solver.StepHook = () =>
                    {
                        Check((Thread.CurrentThread != owner) == threaded, "Solve uses the actual captured numerical thread");
                        foreach (var p in policies) { try { _ = p.Get(); } catch (InvalidOperationException) { rejected++; } }
                        foreach (var p in policies) Reject<InvalidOperationException>(() => p.Set(p.First));
                        Reject<InvalidOperationException>(() => world.DirectSpaceState.IntersectPoint(point, hits));
                    };
                    tree.PhysicsFrame(.01); solver.StepHook = null;
                    if (rejected != 0) throw new InvalidOperationException($"World parameter reads rejected during Solve: {rejected}/9.");
                    var statusActive = false; var statusReads = 0;
                    solver.StatusHook = () =>
                    {
                        Check(!statusActive, "Configured reads do not reenter the failure hook"); statusActive = true;
                        try { ReadValues(); statusReads++; } finally { statusActive = false; }
                    };
                    ReadValues(); solver.StatusHook = null; Check(statusReads == 9, "Each ordinary getter retains a single failure guard");
                    solver.StepHook = () =>
                    {
                        Check((Thread.CurrentThread != owner) == threaded, "Warm Solve retains the captured numerical thread");
                        solver.StatusHook = static () => throw new InvalidOperationException("World input getter reentered numerical failure status.");
                        try { ReadValues(); for (var i = 0; i < policies.Length; i++) Check(solver.ReadWorldPolicy(i) == expected[i], "Selected Solve consumes the complete current policy profile"); solves++; }
                        finally { solver.StatusHook = null; }
                    };
                }
                foreach (var id in bodies) PhysicsServer.BodySetStateSyncCallback(id, state =>
                {
                    Check(Thread.CurrentThread == owner && PhysicsServer.SpaceIsFlushingQueries(space), "World input callbacks retain owner and notification phase");
                    ReadValues(); point.Position = state.Transform.Origin;
                    Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == id && hits[0].ShapeIndex == 1, "World policy frames preserve actual motion and logical shape identity"); callbacks++; queries++;
                });
                for (var i = 0; i < 64; i++) Frame(i); Collect();
                var beforeReads = reads; var beforeCallbacks = callbacks; var beforeQueries = queries; var beforeSolves = solves;
                var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame(i);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                var requiredReads = solver is null ? 4608 : 5760;
                Check(own == 0 && all == 0 && reads - beforeReads == requiredReads && callbacks - beforeCallbacks == 256 && queries - beforeQueries == 256 && solves - beforeSolves == (solver is null ? 0 : 128), "Complete changed world-policy frames retain every read/callback/query and zero owner/all-thread managed bytes");
                Console.WriteLine($"World parameters {backend?.ToString() ?? "registered"}/{threaded}: 9 typed policies, {invalid} numeric rejections; 128 changed full frames, {requiredReads} configured reads, 256 callbacks, 256 queries 0/0 B passed.");
                void Frame(int index)
                {
                    for (var i = 0; i < policies.Length; i++) { var p = policies[i]; p.Set((index & 1) == 0 ? p.First : p.Second); expected[i] = (index & 1) == 0 ? p.First : p.Second; }
                    ReadValues();
                    for (var i = 0; i < bodies.Length; i++) { PhysicsServer.BodySetTransform(bodies[i], new(0, new Vector2(i * 100, 0))); PhysicsServer.BodySetLinearVelocity(bodies[i], new(6, 0)); }
                    var tick = PhysicsServer.SpaceGetTick(space); tree.PhysicsFrame(.01); Check(PhysicsServer.SpaceGetTick(space) == tick + 1, "Changed world-policy frames advance exactly one tick");
                    for (var i = 0; i < bodies.Length; i++) Check(PhysicsServer.BodyGetTransform(bodies[i]).Origin.DistanceTo(new(i * 100 + .06f, 0)) < .0002f, "World input reads preserve the complete actual selected motion");
                    ReadValues();
                }
            }
            finally { if (solver is not null) solver.StepHook = solver.StatusHook = null; PhysicsServer.FreeRID(raw); }
            void ReadValues()
            {
                var calls = solver?.WorldPolicyCalls;
                for (var i = 0; i < policies.Length; i++) Check(policies[i].Get() == expected[i], "All nine getters expose retained world authoring");
                Check(solver?.WorldPolicyCalls == calls, "Readonly world inputs do not publish numerical policies"); reads += 9;
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
