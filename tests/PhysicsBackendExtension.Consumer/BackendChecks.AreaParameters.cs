using Electron2D;
using Fields = Electron2D.PhysicsAreaFieldConfiguration;

public static partial class BackendChecks
{
    public static void RunAreaParameters(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        var solvers = new List<Solver>();
        if (backend is null) PhysicsServerManager.RegisterServer("AreaParameters", () => { var s = new Solver(); solvers.Add(s); return s; });
        try
        {
            using var world = backend is { } chosen ? new World(chosen) : new World("AreaParameters");
            using var other = backend is { } chosenOther ? new World(chosenOther) : new World("AreaParameters");
            var space = world.Space; var otherSpace = other.Space;
            using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 8 };
            var scene = new Area(); scene.AddChild(new CollisionShape { Shape = shape }); root.AddChild(scene);
            var body = new RigidBody { CanSleep = false }; body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
            var raw = PhysicsServer.AreaCreate(); var wrongBody = PhysicsServer.BodyCreate();
            RID[] ids = [scene.GetRID(), raw, space];
            (Action<RID, Fields> Set, Func<Fields, Fields, Fields> Merge, Action<RID> Get)[] operations =
            [
            ((id, p) => PhysicsServer.AreaSetGravitySpaceOverride(id, p.GravityMode), (a, b) => a with { GravityMode = b.GravityMode }, id => { _ = PhysicsServer.AreaGetGravitySpaceOverride(id); }),
            ((id, p) => PhysicsServer.AreaSetGravity(id, p.Gravity), (a, b) => a with { Gravity = b.Gravity }, id => { _ = PhysicsServer.AreaGetGravity(id); }),
            ((id, p) => PhysicsServer.AreaSetGravityVector(id, p.GravityVector), (a, b) => a with { GravityVector = b.GravityVector }, id => { _ = PhysicsServer.AreaGetGravityVector(id); }),
            ((id, p) => PhysicsServer.AreaSetGravityPoint(id, p.GravityPoint), (a, b) => a with { GravityPoint = b.GravityPoint }, id => { _ = PhysicsServer.AreaGetGravityPoint(id); }),
            ((id, p) => PhysicsServer.AreaSetGravityPointUnitDistance(id, p.PointUnitDistance), (a, b) => a with { PointUnitDistance = b.PointUnitDistance }, id => { _ = PhysicsServer.AreaGetGravityPointUnitDistance(id); }),
            ((id, p) => PhysicsServer.AreaSetLinearDampSpaceOverride(id, p.LinearMode), (a, b) => a with { LinearMode = b.LinearMode }, id => { _ = PhysicsServer.AreaGetLinearDampSpaceOverride(id); }),
            ((id, p) => PhysicsServer.AreaSetLinearDamp(id, p.LinearDamp), (a, b) => a with { LinearDamp = b.LinearDamp }, id => { _ = PhysicsServer.AreaGetLinearDamp(id); }),
            ((id, p) => PhysicsServer.AreaSetAngularDampSpaceOverride(id, p.AngularMode), (a, b) => a with { AngularMode = b.AngularMode }, id => { _ = PhysicsServer.AreaGetAngularDampSpaceOverride(id); }),
            ((id, p) => PhysicsServer.AreaSetAngularDamp(id, p.AngularDamp), (a, b) => a with { AngularDamp = b.AngularDamp }, id => { _ = PhysicsServer.AreaGetAngularDamp(id); }),
            ((id, p) => PhysicsServer.AreaSetPriority(id, p.Priority), (a, b) => a with { Priority = b.Priority }, id => { _ = PhysicsServer.AreaGetPriority(id); }),
            ];
            var ownerThread = Environment.CurrentManagedThreadId; var hooks = 0; var probeWrites = true;
            var failures = 0; var isolated = 0; var solver = solvers.FirstOrDefault();
            try
            {
                var defaults = new Fields(ProjectSettings.Get(ProjectSettings.Physics2DDefaultGravityVector),
                    ProjectSettings.Get(ProjectSettings.Physics2DDefaultGravity), false, 0,
                    ProjectSettings.Get(ProjectSettings.Physics2DDefaultLinearDamp), ProjectSettings.Get(ProjectSettings.Physics2DDefaultAngularDamp),
                    Area.SpaceOverride.Disabled, Area.SpaceOverride.Disabled, Area.SpaceOverride.Disabled, -1);
                Check(Read(ids[0]) == new Fields(new(0, 1), 980, false, 0, .1f, 1, 0, 0, 0, 0) &&
                    Read(raw) == new Fields(new(0, -1), 9.80665f, false, 0, .1f, 1, 0, 0, 0, 0) &&
                    Read(space) == defaults && Read(otherSpace) == defaults, "Scene/raw/default-space initial field profiles retain distinct pinned values");
                foreach (var id in ids)
                    for (var profile = 0; profile < 5; profile++)
                        foreach (var operation in operations)
                        {
                            var before = Read(id); var target = Profile(profile);
                            operation.Set(id, target); isolated++;
                            Check(Read(id) == operation.Merge(before, target), "One typed setter changes exactly its parameter and preserves its siblings");
                        }
                Check(scene.GravityDirection == scene.GravityPointCenter && scene.GravityDirection == Read(ids[0]).GravityVector &&
                    scene.Gravity == Read(ids[0]).Gravity && scene.Priority == Read(ids[0]).Priority, "Scene properties share typed server authoring and direction/point vector storage");
                Check(Read(otherSpace) == defaults, "Default-space edits neither change another space nor sampled project defaults");
                Check(ProjectSettings.Get(ProjectSettings.Physics2DDefaultGravityVector) == defaults.GravityVector &&
                    ProjectSettings.Get(ProjectSettings.Physics2DDefaultGravity) == defaults.Gravity &&
                    ProjectSettings.Get(ProjectSettings.Physics2DDefaultLinearDamp) == defaults.LinearDamp &&
                    ProjectSettings.Get(ProjectSettings.Physics2DDefaultAngularDamp) == defaults.AngularDamp, "Existing space edits preserve all project default settings");
                foreach (var id in ids)
                {
                    var before = Read(id);
                    foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                    {
                        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetGravity(id, invalid));
                        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetGravityPointUnitDistance(id, invalid));
                        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetLinearDamp(id, invalid));
                        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetAngularDamp(id, invalid));
                        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetGravityVector(id, new(invalid, 1)));
                        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetGravityVector(id, new(1, invalid)));
                    }
                    foreach (var invalid in new[] { (Area.SpaceOverride)(-1), (Area.SpaceOverride)5 })
                    {
                        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetGravitySpaceOverride(id, invalid));
                        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetLinearDampSpaceOverride(id, invalid));
                        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.AreaSetAngularDampSpaceOverride(id, invalid));
                    }
                    Check(Read(id) == before, "All rejected finite/mode inputs preserve the complete accepted profile");
                    foreach (var priority in new[] { int.MinValue, int.MaxValue })
                    {
                        var boundary = Profile(0) with
                        {
                            Priority = priority,
                            Gravity = -float.MaxValue,
                            PointUnitDistance = -float.MaxValue,
                            GravityVector = new(float.MaxValue, -float.MaxValue),
                            LinearDamp = -float.MaxValue,
                            AngularDamp = float.MaxValue
                        };
                        Write(id, boundary); Check(Read(id) == boundary, "Finite signed field and full integer priority boundaries remain stored without clamping");
                    }
                    Write(id, Profile(0));
                }
                var stale = PhysicsServer.AreaCreate(); PhysicsServer.FreeRID(stale);
                var staleSpace = PhysicsServer.SpaceCreate(); PhysicsServer.FreeRID(staleSpace);
                using var retired = new Area(); var staleScene = retired.GetRID(); retired.Dispose();
                foreach (var wrong in new[] { default, wrongBody, shape.GetRID(), stale, staleSpace, staleScene })
                    foreach (var operation in operations)
                    {
                        Reject<ArgumentException>(() => operation.Get(wrong));
                        Reject<ArgumentException>(() => operation.Set(wrong, Profile(0)));
                    }
                PhysicsServer.AreaAddShape(raw, shape.GetRID()); PhysicsServer.AreaSetSpace(raw, space);
                using var tree = new SceneTree(root);
                Check(world.PhysicsBackendName == (backend?.ToString() ?? "AreaParameters"), "Parameter tests use the actual requested implementation");
                foreach (var id in ids.Take(2))
                {
                    var before = Read(id);
                    PhysicsServer.AreaSetSpace(id, default); Check(Read(id) == before, "Detach retains every Area field");
                    PhysicsServer.AreaSetSpace(id, otherSpace); Check(Read(id) == before && Read(otherSpace) == defaults, "World transfer preserves Area input and independent defaults");
                    PhysicsServer.AreaSetSpace(id, space); Check(Read(id) == before, "Reentry retains every typed field with the same RID");
                }
                if (solver is not null) solver.StepHook = () =>
                {
                    Check((Environment.CurrentManagedThreadId != ownerThread) == threaded, "Parameter reads execute on the captured numerical lane");
                    ReadSelected(); hooks++;
                    if (!probeWrites) return;
                    foreach (var id in ids)
                        foreach (var operation in operations) Reject<InvalidOperationException>(() => operation.Set(id, Read(id)));
                    probeWrites = false;
                };
                tree.PhysicsFrame(.01); if (solver is not null) Check(hooks == 1, "Actual selected Solve reads all thirty parameter inputs");
                if (solver is not null)
                    foreach (var id in ids)
                        for (var failCount = 1; failCount <= 2; failCount++)
                        {
                            var attempts = 0; var target = Profile(failures + 1);
                            var tick = PhysicsServer.SpaceGetTick(space);
                            Exception[] errors = [new ApplicationException("Area profile preparation"), new ArithmeticException("Area profile retry")];
                            void Publish()
                            {
                                attempts++; Check(Read(id) == target, "Preparation hooks read complete common authoring");
                                if (attempts > failCount) return;
                                if (id == space) solver.Fields = target with { Gravity = 999 };
                                else solver.Bodies[id].AreaFields = target with { Gravity = 999 };
                                throw errors[attempts - 1];
                            }
                            if (id == space) solver.DefaultFieldsHook = Publish;
                            else solver.AreaFieldsHook = current => { if (current == id) Publish(); };
                            Write(id, target);
                            Check(Read(id) == target && attempts == 0, "Authored setters/getters defer selected field publication");
                            tree.PhysicsFrame(0); Check(attempts == 0, "Zero interval does not publish fields");
                            for (var attempt = 0; attempt < failCount; attempt++)
                            {
                                var actual = ReleaseErrors(() => tree.PhysicsFrame(.01));
                                Check(actual.Length == 1 && ReferenceEquals(actual[0], errors[attempt]) && PhysicsServer.SpaceGetTick(space) == tick && Read(id) == target,
                                    "Partial field preparation preserves the original error, common profile and solver tick");
                            }
                            tree.PhysicsFrame(.01); Check(attempts == failCount + 1 && PhysicsServer.SpaceGetTick(space) == tick + 1, "A complete retry prepares all fields before one actual Solve");
                            ReadSelected(); solver.AreaFieldsHook = null; solver.DefaultFieldsHook = null; failures++;
                        }
                for (var i = 0; i < 128; i++) Frame(i);
                Collect(); GC.WaitForPendingFinalizers(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); var beforeHooks = hooks;
                for (var i = 0; i < 128; i++) Frame(i);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0 && (solver is null || hooks == beforeHooks + 128), $"Complete parameter frames allocated {own}/{all} owner/all-thread B");
                Task.Run(() =>
                {
                    foreach (var id in ids)
                        foreach (var operation in operations)
                        {
                            Reject<InvalidOperationException>(() => operation.Get(id));
                            Reject<InvalidOperationException>(() => operation.Set(id, Profile(0)));
                        }
                }).GetAwaiter().GetResult();
                Console.WriteLine($"Area parameter operations {backend?.ToString() ?? "registered"}, separate={threaded}: {isolated} isolated setters, {failures} preparation cases; 128 edited full frames {own}/{all} B passed.");

                void Frame(int i)
                {
                    for (var j = 0; j < ids.Length; j++) { var value = Profile(i + j); Write(ids[j], value); Check(Read(ids[j]) == value, "Every edited parameter reaches shared authoring"); }
                    body.Position = Vector2.Zero; body.LinearVelocity = Vector2.Zero; tree.PhysicsFrame(1d / 60);
                    Check(body.LinearVelocity.IsFinite(), "Complete field publication produces finite actual body motion");
                }
            }
            finally
            {
                if (solver is not null) { solver.StepHook = null; solver.AreaFieldsHook = null; solver.DefaultFieldsHook = null; }
                PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(wrongBody);
            }
            void Write(RID id, Fields p) { foreach (var operation in operations) operation.Set(id, p); }
            void ReadSelected()
            {
                Check(Read(ids[0]) == solver!.Bodies[ids[0]].AreaFields && Read(raw) == solver.Bodies[raw].AreaFields && Read(space) == solver.Fields,
                    "All thirty configured parameters agree with selected SetAreaFields/PrepareWorld profiles");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        static Fields Profile(int i) => new(new(2 + (i & 1), -3), 4 + (i & 1), (i & 1) != 0, (i & 1) == 0 ? -2 : 2,
            -.1f, .2f, (Area.SpaceOverride)(i % 5), (Area.SpaceOverride)((i + 1) % 5), (Area.SpaceOverride)((i + 2) % 5), i % 7 - 3);
        static Fields Read(RID id) => new(PhysicsServer.AreaGetGravityVector(id), PhysicsServer.AreaGetGravity(id), PhysicsServer.AreaGetGravityPoint(id),
            PhysicsServer.AreaGetGravityPointUnitDistance(id), PhysicsServer.AreaGetLinearDamp(id), PhysicsServer.AreaGetAngularDamp(id),
            PhysicsServer.AreaGetGravitySpaceOverride(id), PhysicsServer.AreaGetLinearDampSpaceOverride(id), PhysicsServer.AreaGetAngularDampSpaceOverride(id), PhysicsServer.AreaGetPriority(id));
    }
}
