using Electron2D;
using Mode = Electron2D.PhysicsServer.BodyMode;
using DampMode = Electron2D.RigidBody.DampMode;

public static partial class BackendChecks
{
    public static void RunMotionTransactions(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("MotionTransactions", () => solver = new());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("MotionTransactions");
            using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 2 };
            PhysicsBody[] nodes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone()];
            for (var i = 0; i < nodes.Length; i++) { nodes[i].Position = new(i * 100, 0); root.AddChild(nodes[i]); }
            using var tree = new SceneTree(root); var raw = PhysicsServer.BodyCreate(); RID[] ids = [.. nodes.Select(node => node.GetRID()), raw];
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1]; var failures = 0;
            try
            {
                using (var detached = new RigidBody())
                {
                    var id = detached.GetRID(); detached.Sleeping = true; PhysicsServer.BodySetLinearVelocity(id, new(7, 8));
                    Check(!PhysicsServer.BodyGetSleeping(id) && detached.LinearVelocity == new Vector2(7, 8), "Detached linear authoring wakes configured dynamic bodies");
                    detached.Sleeping = true; PhysicsServer.BodySetAngularVelocity(id, 3);
                    Check(!PhysicsServer.BodyGetSleeping(id) && detached.AngularVelocity == 3, "Detached angular authoring wakes configured dynamic bodies");
                }
                for (var i = 0; i < ids.Length; i++)
                {
                    PhysicsServer.BodyAddShape(ids[i], circle.GetRID(), disabled: true); PhysicsServer.BodyAddShape(ids[i], circle.GetRID());
                    PhysicsServer.BodySetSpace(ids[i], world.Space); PhysicsServer.BodySetGravityScale(ids[i], 0); PhysicsServer.BodySetCanSleep(ids[i], false);
                    PhysicsServer.BodySetLinearDamp(ids[i], 0); PhysicsServer.BodySetAngularDamp(ids[i], 0);
                    PhysicsServer.BodySetLinearDampMode(ids[i], DampMode.Replace); PhysicsServer.BodySetAngularDampMode(ids[i], DampMode.Replace);
                    PhysicsServer.BodySetTransform(ids[i], new(0, new(i * 100, 0)));
                }
                tree.PhysicsFrame(.01);
                for (var i = 0; i < ids.Length; i++)
                {
                    Reset(i, Mode.Rigid, 0); PhysicsServer.BodyApplyTorqueImpulse(ids[i], 5);
                    var angular = PhysicsServer.BodyGetAngularVelocity(ids[i]); PhysicsServer.BodySetLinearVelocity(ids[i], new(7, 8));
                    Check(Math.Abs(PhysicsServer.BodyGetAngularVelocity(ids[i]) - angular) < .001f, "A linear write preserves angular motion added through the selected solver");
                    Reset(i, Mode.Rigid, 1); PhysicsServer.BodyApplyCentralImpulse(ids[i], new(8, 6));
                    var linear = PhysicsServer.BodyGetLinearVelocity(ids[i]); PhysicsServer.BodySetAngularVelocity(ids[i], 3);
                    Check((PhysicsServer.BodyGetLinearVelocity(ids[i]) - linear).Length() < .001f, "An angular write preserves linear motion added through the selected solver");
                }
                for (var i = 0; i < ids.Length; i++)
                    foreach (var mode in new[] { Mode.Static, Mode.Kinematic, Mode.Rigid, Mode.RigidLinear })
                        for (var op = 0; op < 7; op++)
                        {
                            var dynamic = mode is Mode.Rigid or Mode.RigidLinear;
                            if (op is 3 or 4 && !dynamic) continue;
                            for (var stage = 0; stage < 5; stage++)
                            {
                                if (!Applicable(op, stage, dynamic)) continue;
                                for (var lane = 0; lane < 6; lane++)
                                    for (var count = 1; count <= 2; count++)
                                    {
                                        Reset(i, mode, op); var held = PhysicsServer.BodyGetDirectState(ids[i])!; var attempts = 0;
                                        var first = new ApplicationException("motion-apply"); var second = new ArithmeticException("motion-prepare");
                                        if (solver is not null) Install(stage, id =>
                                        {
                                            if (id != ids[i]) return;
                                            if (++attempts > count) { Clear(); return; }
                                            var body = solver.Bodies[id];
                                            if (stage == 0) body.Velocity = new(99, 0);
                                            else if (stage == 1) body.Angular = 99;
                                            else if (stage == 2) { body.Surface = new(99, 0); body.SurfaceAngular = 99; }
                                            else if (stage == 3) { body.Awake = op == 3; if (op == 3) { body.Velocity = default; body.Angular = 0; } }
                                            else body.CanSleep = op != 5;
                                            Reject<InvalidOperationException>(() => PhysicsServer.BodySetLinearVelocity(id, default));
                                            if (attempts == 1) throw first; throw second;
                                        });
                                        var tick = PhysicsServer.SpaceGetTick(world.Space);
                                        if (solver is null) Write(i, mode, op, lane == 1, held);
                                        else
                                        {
                                            var errors = ReleaseErrors(() => Write(i, mode, op, lane == 1, held));
                                            Check(errors.Length == 1 && ReferenceEquals(errors[0], first), "Partial motion publication preserves its original error");
                                            if (op is 5 or 6) Check(PhysicsServer.BodyGetCanSleep(ids[i]) == (op == 5) && attempts == 1,
                                                "CanSleep authoring commits before selected publication and remains a non-preparing read");
                                            if (count == 2)
                                            {
                                                errors = ReleaseErrors(Recover);
                                                Check(errors.Length == 1 && ReferenceEquals(errors[0], second) && PhysicsServer.SpaceGetTick(world.Space) == tick,
                                                    "A repeated motion publication error retains pending input without advancing the tick");
                                            }
                                        }
                                        Recover(); Clear(); Verify(i, mode, op); Query(i);
                                        Check(PhysicsServer.SpaceGetTick(world.Space) == tick + (lane == 3 ? 1UL : 0), "Only a successful step advances the tick");
                                        if (solver is not null) Check(attempts == count + 1, "The complete requested write retries at the selected preparation boundary");
                                        failures += solver is null ? 0 : 1;
                                        PhysicsServer.BodySetSleeping(ids[i], false); PhysicsServer.BodySetLinearVelocity(ids[i], default); PhysicsServer.BodySetAngularVelocity(ids[i], 0);
                                        void Recover()
                                        {
                                            if (lane == 0) _ = PhysicsServer.BodyGetLinearVelocity(ids[i]);
                                            else if (lane == 1) _ = held.LinearVelocity;
                                            else if (lane == 2) Query(i);
                                            else if (lane == 3) tree.PhysicsFrame(.01);
                                            else if (lane == 4 || solver is null) _ = PhysicsServer.BodyGetDirectState(ids[i]);
                                            else { using var owned = new OwnedState(solver, ids[i]); }
                                        }
                                    }
                            }
                        }
                var primarySolver = solver;
                using (var other = backend is { } choice ? new World(choice) : new World("MotionTransactions"))
                {
                    _ = other.Space; solver = primarySolver;
                    for (var i = 0; i < ids.Length; i++)
                        foreach (var mode in new[] { Mode.Static, Mode.Kinematic, Mode.Rigid, Mode.RigidLinear })
                            for (var op = 0; op < 7; op++)
                            {
                                var dynamic = mode is Mode.Rigid or Mode.RigidLinear;
                                if (op is 3 or 4 && !dynamic) continue;
                                for (var stage = 0; stage < 5; stage++)
                                {
                                    if (!Applicable(op, stage, dynamic)) continue;
                                    for (var lane = 0; lane < (i < nodes.Length ? 3 : 2); lane++)
                                    {
                                        Reset(i, mode, op); var old = PhysicsServer.BodyGetDirectState(ids[i])!; var attempts = 0;
                                        var failure = new ApplicationException("retired-motion");
                                        if (solver is not null) Install(stage, id =>
                                        {
                                            if (id != ids[i]) return; attempts++;
                                            var body = solver.Bodies[id];
                                            if (stage == 0) body.Velocity = new(99, 0);
                                            else if (stage == 1) body.Angular = 99;
                                            else if (stage == 2) { body.Surface = new(99, 0); body.SurfaceAngular = 99; }
                                            else if (stage == 3) body.Awake = op == 3; else body.CanSleep = op != 5;
                                            throw failure;
                                        });
                                        if (solver is null) Write(i, mode, op, false);
                                        else
                                        {
                                            var errors = ReleaseErrors(() => Write(i, mode, op, false));
                                            Check(errors.Length == 1 && ReferenceEquals(errors[0], failure), "Retirement starts with an incomplete motion write");
                                        }
                                        if (lane == 2) root.RemoveChild(nodes[i]);
                                        else PhysicsServer.BodySetSpace(ids[i], lane == 0 ? default : other.Space);
                                        if (solver is not null) Check(attempts == 1, "Retirement does not repair the attachment being discarded");
                                        Reject<InvalidOperationException>(() => _ = old.LinearVelocity); Clear(); Verify(i, mode, op);
                                        if (lane == 2) root.AddChild(nodes[i]); else PhysicsServer.BodySetSpace(ids[i], world.Space);
                                        Verify(i, mode, op); Query(i);
                                        Check(!ReferenceEquals(old, PhysicsServer.BodyGetDirectState(ids[i])), "Retained motion receives a new qualified body view");
                                        PhysicsServer.BodySetSleeping(ids[i], false); PhysicsServer.BodySetLinearVelocity(ids[i], default); PhysicsServer.BodySetAngularVelocity(ids[i], 0);
                                        failures += solver is null ? 0 : 1;
                                    }
                                }
                            }
                }
                if (solver is not null)
                    for (var i = 0; i < ids.Length; i++)
                        for (var op = 0; op < 2; op++)
                        {
                            Reset(i, Mode.Rigid, op);
                            if (op == 0) PhysicsServer.BodyApplyTorqueImpulse(ids[i], 5); else PhysicsServer.BodyApplyCentralImpulse(ids[i], new(8, 6));
                            var linear = op == 0 ? new Vector2(7, 8) : PhysicsServer.BodyGetLinearVelocity(ids[i]);
                            var angular = op == 0 ? PhysicsServer.BodyGetAngularVelocity(ids[i]) : 3;
                            var attempts = 0; var failure = new ApplicationException("retained-untouched-motion");
                            solver.AwakeHook = id => { if (id != ids[i]) return; attempts++; solver.Bodies[id].Awake = false; throw failure; };
                            var errors = ReleaseErrors(() => Write(i, Mode.Rigid, op, false));
                            Check(errors.Length == 1 && ReferenceEquals(errors[0], failure), "A requested write can fail after the selected solver supplied other motion");
                            PhysicsServer.BodySetSpace(ids[i], default); Check(attempts == 1, "Retirement does not replay a failed wake"); Clear();
                            PhysicsServer.BodySetSpace(ids[i], world.Space);
                            Check((PhysicsServer.BodyGetLinearVelocity(ids[i]) - linear).Length() < .001f && Math.Abs(PhysicsServer.BodyGetAngularVelocity(ids[i]) - angular) < .001f,
                                "Retirement overlays requested channels while retaining untouched selected motion");
                            PhysicsServer.BodySetLinearVelocity(ids[i], default); PhysicsServer.BodySetAngularVelocity(ids[i], 0); failures++;
                        }
                for (var role = 0; role < 4; role++)
                {
                    using PhysicsBody native = role switch { 0 => new RigidBody(), 1 => new StaticBody(), 2 => new AnimatableBody(), _ => new CharacterBody() };
                    native.Position = new(1000 + role * 100, 0); root.AddChild(native); var id = native.GetRID();
                    PhysicsServer.BodyAddShape(id, circle.GetRID(), disabled: true); PhysicsServer.BodyAddShape(id, circle.GetRID());
                    PhysicsServer.BodySetGravityScale(id, 0); PhysicsServer.BodySetLinearDamp(id, 0); PhysicsServer.BodySetAngularDamp(id, 0);
                    PhysicsServer.BodySetLinearDampMode(id, DampMode.Replace); PhysicsServer.BodySetAngularDampMode(id, DampMode.Replace);
                    try
                    {
                        for (var op = 0; op < 7; op++)
                        {
                            if (op is 3 or 4 && role != 0) continue;
                            for (var stage = 0; stage < 5; stage++)
                            {
                                if (!Applicable(op, stage, role == 0)) continue;
                                for (var retire = 0; retire < 2; retire++)
                                {
                                    Clear(); PhysicsServer.BodySetSleeping(id, false); PhysicsServer.BodySetCanSleep(id, op == 6);
                                    PhysicsServer.BodySetLinearVelocity(id, new(3, 4)); PhysicsServer.BodySetAngularVelocity(id, 2);
                                    if (op is 4 or 6 && role == 0) PhysicsServer.BodySetSleeping(id, true);
                                    var held = PhysicsServer.BodyGetDirectState(id)!; var attempts = 0;
                                    var first = new ApplicationException("native-motion-apply"); var second = new ArithmeticException("native-motion-prepare");
                                    if (solver is not null) Install(stage, rid =>
                                    {
                                        if (rid != id) return;
                                        if (++attempts > 2) { Clear(); return; }
                                        var body = solver.Bodies[id];
                                        if (stage == 0) body.Velocity = new(99, 0);
                                        else if (stage == 1) body.Angular = 99;
                                        else if (stage == 2) { body.Surface = new(99, 0); body.SurfaceAngular = 99; }
                                        else if (stage == 3) body.Awake = op == 3; else body.CanSleep = op != 5;
                                        if (attempts == 1) throw first; throw second;
                                    });
                                    if (solver is null) WriteNative();
                                    else
                                    {
                                        var errors = ReleaseErrors(WriteNative);
                                        Check(errors.Length == 1 && ReferenceEquals(errors[0], first), "Native scene writes preserve their original selected error");
                                        if (op is 5 or 6) Check(PhysicsServer.BodyGetCanSleep(id) == (op == 5) && attempts == 1, "Native CanSleep input remains non-preparing after failure");
                                    }
                                    if (retire != 0)
                                    {
                                        root.RemoveChild(native);
                                        if (solver is not null) Check(attempts == 1, "Native scene retirement discards pending publication without repairing it");
                                        Reject<InvalidOperationException>(() => _ = held.LinearVelocity); Clear(); root.AddChild(native);
                                    }
                                    else if (solver is not null)
                                    {
                                        var errors = ReleaseErrors(() => { if (native is StaticBody && stage == 2 && op is 0 or 1) WriteNative(); else _ = held.LinearVelocity; });
                                        Check(errors.Length == 1 && ReferenceEquals(errors[0], second), "Native scene equal-property/view access retries the same incomplete write");
                                    }
                                    point.Position = native.GlobalPosition;
                                    Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == id && hits[0].ShapeIndex == 1,
                                        "Native scene queries retain identity and logical slots after motion recovery");
                                    var expectedLinear = op switch { 0 => new Vector2(7, 8), 2 => new Vector2(3, 9), 3 or 4 => default, 6 when role == 0 => default, _ => new Vector2(3, 4) };
                                    var expectedAngular = op is 3 or 4 || op == 6 && role == 0 ? 0 : op == 1 ? 3 : 2;
                                    Check((PhysicsServer.BodyGetLinearVelocity(id) - expectedLinear).Length() < .001f && Math.Abs(PhysicsServer.BodyGetAngularVelocity(id) - expectedAngular) < .001f &&
                                        PhysicsServer.BodyGetCanSleep(id) == (op == 5) && (role != 0 || PhysicsServer.BodyGetSleeping(id) == (op == 3)),
                                        "Native scene motion and sleep preserve the requested values and unaffected channels");
                                    Clear(); failures += solver is null ? 0 : 1;
                                    void WriteNative()
                                    {
                                        if (native is RigidBody rigid && op != 2)
                                        {
                                            if (op == 0) rigid.LinearVelocity = new(7, 8);
                                            else if (op == 1) rigid.AngularVelocity = 3;
                                            else if (op is 3 or 4) rigid.Sleeping = op == 3; else rigid.CanSleep = op == 5;
                                        }
                                        else if (native is StaticBody surface && stage == 2 && op is 0 or 1)
                                        {
                                            if (op == 0) surface.ConstantLinearVelocity = new(7, 8); else surface.ConstantAngularVelocity = 3;
                                        }
                                        else if (op == 0) PhysicsServer.BodySetLinearVelocity(id, new(7, 8));
                                        else if (op == 1) PhysicsServer.BodySetAngularVelocity(id, 3);
                                        else if (op == 2) PhysicsServer.BodySetAxisVelocity(id, new(0, 9));
                                        else PhysicsServer.BodySetCanSleep(id, op == 5);
                                    }
                                }
                            }
                        }
                    }
                    finally { Clear(); root.RemoveChild(native); }
                }
                foreach (var id in ids)
                {
                    PhysicsServer.BodySetMode(id, Mode.Rigid); PhysicsServer.BodySetCanSleep(id, false);
                    PhysicsServer.BodySetLinearVelocity(id, new(3, 4)); PhysicsServer.BodySetAngularVelocity(id, 2);
                }
                tree.PhysicsFrame(.01);
                using (var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space))
                using (var map = new PhysicsSnapshotMap(world.Space))
                {
                    for (var i = 0; i < ids.Length; i++) map.Bind((ulong)(100 + i), 1, ids[i]);
                    var snapshot = new PhysicsSnapshot(65536); map.Capture(snapshot);
                    for (var portable = 0; portable < 2; portable++)
                        for (var i = 0; i < ids.Length; i++)
                            foreach (var op in new[] { 0, 1, 3 })
                            {
                                checkpoint.Restore(); var attempts = 0; var failure = new ApplicationException("superseded-motion");
                                if (solver is not null) solver.AwakeHook = id =>
                                {
                                    if (id != ids[i]) return; attempts++; solver.Bodies[id].Awake = op == 3; throw failure;
                                };
                                if (solver is null) Write(i, Mode.Rigid, op, false);
                                else
                                {
                                    var errors = ReleaseErrors(() => Write(i, Mode.Rigid, op, false));
                                    Check(errors.Length == 1 && ReferenceEquals(errors[0], failure), "A snapshot may supersede an incomplete motion request");
                                }
                                if (portable == 0) checkpoint.Restore();
                                else
                                {
                                    if (solver is not null)
                                    {
                                        var tick = PhysicsServer.SpaceGetTick(world.Space); var errors = ReleaseErrors(() => map.Apply(snapshot));
                                        Check(errors.Length == 1 && ReferenceEquals(errors[0], failure) && attempts == 2 && PhysicsServer.SpaceGetTick(world.Space) == tick,
                                            "Portable preparation retains pre-existing publication errors without applying incoming state");
                                        solver.AwakeHook = id => { if (id == ids[i]) attempts++; };
                                    }
                                    map.Apply(snapshot);
                                }
                                Check((PhysicsServer.BodyGetLinearVelocity(ids[i]) - new Vector2(3, 4)).Length() < .001f && Math.Abs(PhysicsServer.BodyGetAngularVelocity(ids[i]) - 2) < .001f &&
                                    !PhysicsServer.BodyGetSleeping(ids[i]) && (solver is null || attempts == (portable == 0 ? 1 : 3)),
                                    "Restored state supersedes requested publication after the applicable local/portable preparation boundary");
                                Clear(); failures += solver is null ? 0 : 1;
                            }
                    for (var i = 0; i < ids.Length; i++)
                        for (var kind = 0; kind < 2; kind++)
                        {
                            if (kind == 1 && solver is null) continue;
                            checkpoint.Restore(); var attempts = 0; var failure = new ApplicationException("superseded-pose-mode");
                            if (kind == 1) PhysicsServer.BodySetMode(ids[i], Mode.Static);
                            if (solver is not null)
                            {
                                Action<RID> hook = id =>
                                {
                                    if (id != ids[i]) return; attempts++;
                                    if (kind == 0) solver.Bodies[id].Position = new(99, 99); else solver.Bodies[id].Mode = Mode.Kinematic;
                                    throw failure;
                                };
                                if (kind == 0) solver.PoseHook = hook; else solver.ModeHook = hook;
                                var errors = ReleaseErrors(WritePoseMode);
                                Check(errors.Length == 1 && ReferenceEquals(errors[0], failure), "Snapshots may supersede other incomplete body publication");
                            }
                            else WritePoseMode();
                            checkpoint.Restore(); Query(i);
                            Check(PhysicsServer.BodyGetMode(ids[i]) == Mode.Rigid && PhysicsServer.BodyGetLinearVelocity(ids[i]).DistanceTo(new(3, 4)) < .001f &&
                                (solver is null || attempts == 1), "Restored body pose/mode does not republish discarded preparation debt");
                            Clear(); failures += solver is null ? 0 : 1;
                            void WritePoseMode() { if (kind == 0) PhysicsServer.BodySetTransform(ids[i], new(0, new(99, 99))); else PhysicsServer.BodySetMode(ids[i], Mode.Rigid); }
                        }
                }
                using (var sceneArea = new Area { Position = new(2000, 0) })
                {
                    root.AddChild(sceneArea); var rawArea = PhysicsServer.AreaCreate(); RID[] areas = [sceneArea.GetRID(), rawArea];
                    try
                    {
                        for (var i = 0; i < areas.Length; i++)
                        {
                            PhysicsServer.AreaAddShape(areas[i], circle.GetRID()); PhysicsServer.AreaSetSpace(areas[i], world.Space);
                            PhysicsServer.AreaSetTransform(areas[i], new(0, new(2000 + i * 100, 0)));
                        }
                        tree.PhysicsFrame(.01); using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
                        for (var i = 0; i < areas.Length; i++)
                        {
                            var attempts = 0; var failure = new ApplicationException("superseded-area-pose");
                            if (solver is not null) solver.PoseHook = id => { if (id != areas[i]) return; attempts++; solver.Bodies[id].Position = new(99, 99); throw failure; };
                            if (solver is null) PhysicsServer.AreaSetTransform(areas[i], new(0, new(99, 99)));
                            else
                            {
                                var errors = ReleaseErrors(() => { PhysicsServer.AreaSetTransform(areas[i], new(0, new(99, 99))); Query(0); });
                                Check(errors.Length == 1 && ReferenceEquals(errors[0], failure), "An Area pose may fail before an authoritative rewind");
                            }
                            checkpoint.Restore(); point.CollideWithAreas = true; point.Position = new(2000 + i * 100, 0);
                            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == areas[i] && (solver is null || attempts == 1),
                                "Restored Area pose discards superseded preparation without another selected pose write");
                            point.CollideWithAreas = false; Clear(); failures += solver is null ? 0 : 1;
                        }
                    }
                    finally { Clear(); point.CollideWithAreas = false; PhysicsServer.FreeRID(rawArea); root.RemoveChild(sceneArea); }
                }
                foreach (var id in ids) { PhysicsServer.BodySetLinearVelocity(id, default); PhysicsServer.BodySetAngularVelocity(id, 0); }
                for (var frame = 0; frame < 128; frame++) Frame();
                Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var frame = 0; frame < 128; frame++) Frame();
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0, $"Changed-motion full frames allocated {own}/{all} owner/all-thread bytes");
                Console.WriteLine($"Motion transactions {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: {failures} failure cases and 128 changed-motion full frames {own}/{all} B passed.");
            }
            finally { Clear(); PhysicsServer.FreeRID(raw); }
            void Clear() { if (solver is not null) solver.BodyHook = solver.AngularHook = solver.SurfaceHook = solver.AwakeHook = solver.CanSleepHook = solver.PoseHook = solver.ModeHook = null; }
            void Install(int stage, Action<RID> hook)
            {
                if (stage == 0) solver!.BodyHook = hook;
                else if (stage == 1) solver!.AngularHook = hook;
                else if (stage == 2) solver!.SurfaceHook = hook;
                else if (stage == 3) solver!.AwakeHook = hook; else solver!.CanSleepHook = hook;
            }
            bool Applicable(int op, int stage, bool dynamic) => op switch
            {
                0 or 2 => stage == 0 || stage == (dynamic ? 3 : 2),
                1 => stage == 1 || stage == (dynamic ? 3 : 2),
                3 or 4 => stage == 3,
                _ => stage == 4 || op == 6 && dynamic && stage == 3
            };
            void Reset(int i, Mode mode, int op)
            {
                Clear(); PhysicsServer.BodySetMode(ids[i], Mode.Rigid); PhysicsServer.BodySetSleeping(ids[i], false);
                PhysicsServer.BodySetTransform(ids[i], new(0, new(i * 100, 0))); PhysicsServer.BodySetMode(ids[i], mode);
                PhysicsServer.BodySetCanSleep(ids[i], op == 6); PhysicsServer.BodySetLinearVelocity(ids[i], new(3, 4));
                PhysicsServer.BodySetAngularVelocity(ids[i], mode == Mode.RigidLinear ? 0 : 2);
                if (op is 4 or 6 && mode is Mode.Rigid or Mode.RigidLinear) PhysicsServer.BodySetSleeping(ids[i], true);
            }
            void Write(int i, Mode mode, int op, bool scene, PhysicsDirectBodyState? view = null)
            {
                if (scene && i is 3 or 4 && op != 2 && (mode is Mode.Rigid or Mode.RigidLinear || op is not (3 or 4)))
                {
                    var rigid = (RigidBody)nodes[i];
                    if (op == 0) rigid.LinearVelocity = new(7, 8);
                    else if (op == 1) rigid.AngularVelocity = mode == Mode.RigidLinear ? 0 : 3;
                    else if (op is 3 or 4) rigid.Sleeping = op == 3; else rigid.CanSleep = op == 5;
                    return;
                }
                if (scene && view is not null && op is 0 or 1 or 3 or 4)
                {
                    if (op == 0) view.LinearVelocity = new(7, 8);
                    else if (op == 1) view.AngularVelocity = mode == Mode.RigidLinear ? 0 : 3;
                    else view.Sleeping = op == 3;
                    return;
                }
                if (op == 0) PhysicsServer.BodySetLinearVelocity(ids[i], new(7, 8));
                else if (op == 1) PhysicsServer.BodySetAngularVelocity(ids[i], mode == Mode.RigidLinear ? 0 : 3);
                else if (op == 2) PhysicsServer.BodySetAxisVelocity(ids[i], new(0, 9));
                else if (op is 3 or 4) PhysicsServer.BodySetSleeping(ids[i], op == 3);
                else PhysicsServer.BodySetCanSleep(ids[i], op == 5);
            }
            void Verify(int i, Mode mode, int op)
            {
                var linear = op switch { 0 => new Vector2(7, 8), 2 => new Vector2(3, 9), 3 or 4 or 6 => default, _ => new Vector2(3, 4) };
                if (op == 6 && mode is Mode.Static or Mode.Kinematic) linear = new(3, 4);
                var angular = mode == Mode.RigidLinear || op is 3 or 4 || op == 6 && mode is Mode.Rigid or Mode.RigidLinear ? 0 : op == 1 ? 3 : 2;
                if ((PhysicsServer.BodyGetLinearVelocity(ids[i]) - linear).Length() >= .001f || Math.Abs(PhysicsServer.BodyGetAngularVelocity(ids[i]) - angular) >= .001f ||
                    PhysicsServer.BodyGetCanSleep(ids[i]) != (op == 5) || mode is Mode.Rigid or Mode.RigidLinear && PhysicsServer.BodyGetSleeping(ids[i]) != (op == 3))
                    throw new InvalidOperationException($"Prepared motion disagrees: {i}/{mode}/{op}, {PhysicsServer.BodyGetLinearVelocity(ids[i])}/{PhysicsServer.BodyGetAngularVelocity(ids[i])}/{PhysicsServer.BodyGetSleeping(ids[i])}/{PhysicsServer.BodyGetCanSleep(ids[i])}");
            }
            void Query(int i)
            {
                point.Position = new(i * 100, 0);
                Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == ids[i] && hits[0].ShapeIndex == 1, "Prepared motion retains RID and logical disabled gaps in real queries");
            }
            void Frame()
            {
                foreach (var id in ids)
                {
                    PhysicsServer.BodySetCanSleep(id, true); PhysicsServer.BodySetSleeping(id, true); PhysicsServer.BodySetCanSleep(id, false);
                    PhysicsServer.BodySetSleeping(id, false); PhysicsServer.BodySetLinearVelocity(id, new(.1f, 0)); PhysicsServer.BodySetAngularVelocity(id, 0);
                }
                tree.PhysicsFrame(.01);
                for (var i = 0; i < ids.Length; i++) Query(i);
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
