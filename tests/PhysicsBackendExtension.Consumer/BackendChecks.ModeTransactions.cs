using Electron2D;
using Mode = Electron2D.PhysicsServer.BodyMode;

public static partial class BackendChecks
{
    public static void RunModeTransactions(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("ModeTransactions", () => solver = new());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("ModeTransactions");
            using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 2 };
            PhysicsBody[] nodes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone()];
            for (var i = 0; i < nodes.Length; i++) { nodes[i].Position = new(i * 100, 0); nodes[i].CollisionMask = 0; root.AddChild(nodes[i]); }
            using var tree = new SceneTree(root); var raw = PhysicsServer.BodyCreate();
            RID[] ids = [.. nodes.Select(node => node.GetRID()), raw]; var views = new PhysicsDirectBodyState[ids.Length];
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1]; var faults = 0;
            try
            {
                PhysicsServer.BodySetTransform(raw, new(0, new(500, 0))); PhysicsServer.BodySetCollisionMask(raw, 0);
                for (var i = 0; i < ids.Length; i++)
                {
                    PhysicsServer.BodyAddShape(ids[i], circle.GetRID(), disabled: true); PhysicsServer.BodyAddShape(ids[i], circle.GetRID());
                    PhysicsServer.BodySetMode(ids[i], Mode.Rigid); PhysicsServer.BodySetSpace(ids[i], world.Space);
                    PhysicsServer.BodySetGravityScale(ids[i], 0); PhysicsServer.BodySetCanSleep(ids[i], false); PhysicsServer.BodySetMass(ids[i], 2);
                    PhysicsServer.BodySetLinearDampMode(ids[i], RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(ids[i], 0);
                    PhysicsServer.BodySetAngularDampMode(ids[i], RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(ids[i], 0);
                    views[i] = PhysicsServer.BodyGetDirectState(ids[i])!;
                }
                tree.PhysicsFrame(.01);
                for (var i = 0; i < ids.Length; i++)
                    foreach (var mode in new[] { Mode.Static, Mode.Kinematic, Mode.Rigid, Mode.RigidLinear })
                    {
                        if (solver is null) continue;
                        for (var stage = 0; stage < 5; stage++)
                            for (var path = 0; path < 4; path++)
                                for (var failures = 1; failures <= 2; failures++)
                                {
                                    Reset(i); if (mode == Mode.Rigid) PhysicsServer.BodySetMode(ids[i], Mode.RigidLinear);
                                    point.Position = PhysicsServer.BodyGetTransform(ids[i]).Origin;
                                    var attempts = 0; var apply = new ApplicationException("mode-apply"); var retry = new ArithmeticException("mode-retry");
                                    Action<RID> hook = id =>
                                    {
                                        if (id != ids[i]) return;
                                        if (++attempts > failures) { ClearHooks(); return; }
                                        var body = solver.Bodies[id]; body.Mode = mode == Mode.Static ? Mode.Rigid : Mode.Static;
                                        body.Locked = mode != Mode.RigidLinear; body.Velocity = new(99, 0); body.Angular = 9; body.Awake = false;
                                        Check(PhysicsServer.BodyGetMode(id) == mode, "Committed mode remains readable during a failing hook");
                                        Reject<InvalidOperationException>(() => PhysicsServer.BodySetMode(id, mode));
                                        Reject<InvalidOperationException>(() => _ = views[i].InverseMass);
                                        if (attempts == 1) throw apply; throw retry;
                                    };
                                    Install(stage, hook);
                                    var tick = PhysicsServer.SpaceGetTick(world.Space);
                                    var errors = ReleaseErrors(() => PhysicsServer.BodySetMode(ids[i], mode));
                                    Check(errors.Length == 1 && ReferenceEquals(errors[0], apply) && attempts == 1 && PhysicsServer.BodyGetMode(ids[i]) == mode,
                                        "A failed mode transition retains its original error and committed mode");
                                    if (failures == 2)
                                    {
                                        errors = ReleaseErrors(() => Recover(path));
                                        Check(errors.Length == 1 && ReferenceEquals(errors[0], retry) && PhysicsServer.SpaceGetTick(world.Space) == tick,
                                            "Failed mode preparation remains pending before numerical publication");
                                    }
                                    Recover(path); ClearHooks();
                                    Prepared(i, mode);
                                    Check(attempts == failures + 1 && PhysicsServer.SpaceGetTick(world.Space) == tick + (path == 1 ? 1UL : 0), "A mode repairs only at explicit preparation and advances only a successful step");
                                    Check(Math.Abs(PhysicsServer.BodyGetLinearVelocity(ids[i]).X - (mode is Mode.Rigid or Mode.RigidLinear ? 6 : 0)) < .001f &&
                                        Math.Abs(PhysicsServer.BodyGetAngularVelocity(ids[i])) < .001f,
                                        $"Repair velocity {i}/{mode}/{stage}/{path}/{failures}: {PhysicsServer.BodyGetLinearVelocity(ids[i])}, {PhysicsServer.BodyGetAngularVelocity(ids[i])}");
                                    Check(mode < Mode.Rigid || !PhysicsServer.BodyGetSleeping(ids[i]), "Dynamic mode recovery restores its committed wake policy");
                                    faults++;
                                    void Recover(int lane)
                                    {
                                        if (lane == 0) _ = world.DirectSpaceState.IntersectPoint(point, hits);
                                        else if (lane == 1) tree.PhysicsFrame(.01);
                                        else if (lane == 2) _ = views[i].InverseMass;
                                        else if (i < nodes.Length && nodes[i] is RigidBody rigid) _ = rigid.LinearVelocity;
                                        else _ = PhysicsServer.BodyGetLinearVelocity(ids[i]);
                                    }
                                }
                    }
                if (solver is not null)
                    for (var i = 0; i < ids.Length; i++)
                        foreach (var mode in new[] { Mode.Static, Mode.Kinematic, Mode.Rigid, Mode.RigidLinear })
                            for (var stage = 0; stage < 5; stage++)
                                for (var lane = 0; lane < (i < nodes.Length ? 2 : 1); lane++)
                                {
                                    Reset(i); if (mode == Mode.Rigid) PhysicsServer.BodySetMode(ids[i], Mode.RigidLinear);
                                    var attempts = 0; var failure = new ApplicationException("retired-mode");
                                    Install(stage, id =>
                                    {
                                        if (id != ids[i]) return; attempts++;
                                        var body = solver.Bodies[id]; body.Mode = mode == Mode.Static ? Mode.Rigid : Mode.Static;
                                        body.Velocity = new(99, 0); body.Angular = 9; throw failure;
                                    });
                                    var errors = ReleaseErrors(() => PhysicsServer.BodySetMode(ids[i], mode));
                                    Check(errors.Length == 1 && ReferenceEquals(errors[0], failure), "The retirement control has pending mode preparation");
                                    var old = views[i];
                                    if (lane == 0) PhysicsServer.BodySetSpace(ids[i], default); else root.RemoveChild(nodes[i]);
                                    Check(attempts == 1 && !PhysicsServer.BodyGetSpace(ids[i]).IsValid(), "Retirement discards failed attachment debt without repairing it");
                                    Reject<InvalidOperationException>(() => _ = old.InverseMass); ClearHooks();
                                    if (lane == 0) PhysicsServer.BodySetSpace(ids[i], world.Space); else root.AddChild(nodes[i]);
                                    views[i] = PhysicsServer.BodyGetDirectState(ids[i])!; Prepared(i, mode);
                                    Check(Math.Abs(PhysicsServer.BodyGetLinearVelocity(ids[i]).X - (mode is Mode.Rigid or Mode.RigidLinear ? 6 : 0)) < .001f &&
                                        Math.Abs(PhysicsServer.BodyGetAngularVelocity(ids[i])) < .001f, "Reattachment preserves authored motion instead of sampling the failed owner");
                                    faults++;
                                }
                for (var i = 0; i < ids.Length; i++)
                    foreach (var before in new[] { Mode.Static, Mode.Kinematic, Mode.Rigid, Mode.RigidLinear })
                        foreach (var after in new[] { Mode.Static, Mode.Kinematic, Mode.Rigid, Mode.RigidLinear })
                        {
                            PhysicsServer.BodySetMode(ids[i], before); PhysicsServer.BodySetMode(ids[i], after); Prepared(i, after);
                        }
                if (solver is not null)
                    for (var i = 3; i <= 4; i++)
                        for (var option = 0; option < 3; option++)
                        {
                            var rigid = (RigidBody)nodes[i]; rigid.Freeze = false; rigid.LockRotation = false; rigid.FreezeMode = RigidFreezeMode.Static;
                            if (option == 2) rigid.Freeze = true;
                            Reset(i); var failure = new ApplicationException("scene-reclaim"); var attempts = 0;
                            solver.ModeHook = id => { if (id != ids[i]) return; attempts++; solver.Bodies[id].Mode = Mode.Static; throw failure; };
                            var errors = ReleaseErrors(() => PhysicsServer.BodySetMode(ids[i], Mode.RigidLinear));
                            Check(errors.Length == 1 && ReferenceEquals(errors[0], failure), "Scene reclaim starts with incomplete direct mode publication");
                            errors = ReleaseErrors(Change);
                            Check(errors.Length == 1 && ReferenceEquals(errors[0], failure) && attempts == 2 && !rigid.LockRotation &&
                                rigid.Freeze == (option == 2) && rigid.FreezeMode == RigidFreezeMode.Static && PhysicsServer.BodyGetMode(ids[i]) == Mode.RigidLinear,
                                "Scene role edits repair the old direct profile before changing configured role");
                            ClearHooks(); Change(); Prepared(i, i == 4 ? Mode.Static : option == 0 ? Mode.RigidLinear : option == 1 ? Mode.Static : Mode.Kinematic, i != 4, option == 0);
                            rigid.Freeze = false; rigid.LockRotation = false; rigid.FreezeMode = RigidFreezeMode.Static; faults++;
                            void Change() { if (option == 0) rigid.LockRotation = true; else if (option == 1) rigid.Freeze = true; else rigid.FreezeMode = RigidFreezeMode.Kinematic; }
                        }
                for (var frame = 0; frame < 128; frame++) Frame(frame);
                Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var frame = 0; frame < 128; frame++) Frame(frame);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0, $"128 complete changed-mode frames allocated {own}/{all} owner/all-thread bytes");
                Console.WriteLine($"Mode transactions {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: 96 transitions, {faults} partial failures, shape/view identity and 128 changed-mode frames {own}/{all} B passed.");
            }
            finally { ClearHooks(); PhysicsServer.FreeRID(raw); }
            void ClearHooks() { if (solver is not null) solver.ModeHook = solver.RotationHook = solver.BodyHook = solver.SurfaceHook = solver.ShapeHook = null; }
            void Install(int stage, Action<RID> hook)
            {
                if (stage == 0) solver!.ModeHook = hook;
                else if (stage == 1) solver!.RotationHook = hook;
                else if (stage == 2) solver!.BodyHook = hook; else if (stage == 3) solver!.SurfaceHook = hook; else solver!.ShapeHook = hook;
            }
            void Reset(int i)
            {
                ClearHooks(); PhysicsServer.BodySetMode(ids[i], Mode.Rigid);
                PhysicsServer.BodySetLinearVelocity(ids[i], new(6, 0)); PhysicsServer.BodySetAngularVelocity(ids[i], 2);
            }
            void Prepared(int i, Mode mode, bool geometry = true, bool? locked = null)
            {
                point.Position = PhysicsServer.BodyGetTransform(ids[i]).Origin;
                var count = world.DirectSpaceState.IntersectPoint(point, hits);
                Check(geometry ? count == 1 && hits[0].ColliderRID == ids[i] && hits[0].ShapeIndex == 1 : count == 0,
                    "Mode preparation preserves numerical geometry and logical disabled gaps");
                Check(PhysicsServer.BodyGetMode(ids[i]) == mode && ReferenceEquals(PhysicsServer.BodyGetDirectState(ids[i]), views[i]) &&
                    Math.Abs(views[i].InverseMass - (mode is Mode.Rigid or Mode.RigidLinear ? .5f : 0)) < .0001f,
                    "Mode, direct-view identity and physical mass agree after preparation");
                if (solver is not null) Check(solver.Bodies[ids[i]].Mode == mode && solver.Bodies[ids[i]].Locked == (locked ?? mode == Mode.RigidLinear),
                    "The selected numerical owner has the complete mode and rotation policy");
            }
            void Frame(int frame)
            {
                var mode = (Mode)(frame & 3);
                foreach (var id in ids) { PhysicsServer.BodySetMode(id, mode); PhysicsServer.BodySetLinearVelocity(id, default); PhysicsServer.BodySetAngularVelocity(id, 0); }
                tree.PhysicsFrame(.01);
                for (var i = 0; i < ids.Length; i++) Prepared(i, mode);
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
