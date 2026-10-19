using Electron2D;
using Mode = Electron2D.PhysicsServer.BodyMode;

public static partial class BackendChecks
{
    public static void RunMassReset(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("MassReset", () => solver = new());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("MassReset");
            using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 1 };
            PhysicsBody[] nodes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone()];
            for (var i = 0; i < nodes.Length; i++) root.AddChild(nodes[i]);
            using var tree = new SceneTree(root); var raw = PhysicsServer.BodyCreate();
            RID[] ids = [.. nodes.Select(node => node.GetRID()), raw]; var views = new PhysicsDirectBodyState[ids.Length];
            using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1]; var faults = 0; var numericalReads = 0;
            var owner = Environment.CurrentManagedThreadId; var probeWrites = true;
            try
            {
                foreach (var id in ids)
                {
                    PhysicsServer.BodySetCollisionMask(id, 0); PhysicsServer.BodySetGravityScale(id, 0); PhysicsServer.BodySetCanSleep(id, false);
                    PhysicsServer.BodySetLinearDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(id, 0);
                    PhysicsServer.BodySetAngularDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(id, 0);
                    PhysicsServer.BodyAddShape(id, circle.GetRID(), new(0, new(10000, 0)), disabled: true);
                    PhysicsServer.BodyAddShape(id, circle.GetRID(), new(0, new(8, 0))); PhysicsServer.BodyAddShape(id, circle.GetRID(), new(0, new(-2, 0)));
                    PhysicsServer.BodySetSpace(id, world.Space);
                }
                for (var i = 0; i < ids.Length; i++) { Configure(i, Mode.Rigid); views[i] = PhysicsServer.BodyGetDirectState(ids[i])!; }
                Check(world.PhysicsBackendName == (backend?.ToString() ?? "MassReset") && world.PhysicsFallbackReason is null,
                    "Mass profiles use the actual selected implementation without fallback");
                using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space); var snapshot = new PhysicsSnapshot(65536);
                var area = PhysicsServer.AreaCreate(); var stale = PhysicsServer.BodyCreate(); PhysicsServer.FreeRID(stale);
                try
                {
                    foreach (var wrong in new[] { default, area, circle.GetRID(), world.Space, stale }) Reject<ArgumentException>(() => PhysicsServer.BodyResetMassProperties(wrong));
                }
                finally { PhysicsServer.FreeRID(area); }
                Task.Run(() => { foreach (var id in ids) Reject<InvalidOperationException>(() => PhysicsServer.BodyResetMassProperties(id)); }).GetAwaiter().GetResult();
                for (var i = 0; i < ids.Length; i++)
                    foreach (var mode in new[] { Mode.Static, Mode.Kinematic, Mode.Rigid, Mode.RigidLinear })
                    {
                        Configure(i, mode); PhysicsServer.BodyResetMassProperties(ids[i]); Verify(i, mode, true);
                        PhysicsServer.BodySetSpace(ids[i], default); PhysicsServer.BodySetInertia(ids[i], 8); PhysicsServer.BodySetCenterOfMass(ids[i], new(1, -2));
                        PhysicsServer.BodyResetMassProperties(ids[i]); PhysicsServer.BodySetSpace(ids[i], world.Space);
                        views[i] = PhysicsServer.BodyGetDirectState(ids[i])!; Verify(i, mode, true);
                    }
                for (var i = 0; i < ids.Length; i++)
                {
                    Configure(i, Mode.Rigid); PhysicsServer.BodyResetMassProperties(ids[i]);
                    PhysicsServer.BodySetShapeTransform(ids[i], 1, new(0, new(10, 0)));
                    Check(PhysicsServer.BodyGetCenterOfMass(ids[i]).DistanceTo(new(4, 0)) < .002f && MathF.Abs(PhysicsServer.BodyGetInertia(ids[i]) - 73) < .002f,
                        "Automatic reset follows changed active geometry");
                    PhysicsServer.BodySetShapeTransform(ids[i], 1, new(0, new(8, 0))); Verify(i, Mode.Rigid, true);
                    PhysicsServer.BodyClearShapes(ids[i]); PhysicsServer.BodyResetMassProperties(ids[i]);
                    Check(PhysicsServer.BodyGetMass(ids[i]) == 2 && PhysicsServer.BodyGetInertia(ids[i]) == 0 && PhysicsServer.BodyGetCenterOfMass(ids[i]) == Vector2.Zero &&
                        MathF.Abs(views[i].InverseMass - .5f) < .0001f && views[i].InverseInertia == 0,
                        "Unshaped reset retains configured mass with zero automatic inertia/center");
                    PhysicsServer.BodyAddShape(ids[i], circle.GetRID(), new(0, new(10000, 0)), disabled: true);
                    PhysicsServer.BodyAddShape(ids[i], circle.GetRID(), new(0, new(8, 0))); PhysicsServer.BodyAddShape(ids[i], circle.GetRID(), new(0, new(-2, 0)));
                    Verify(i, Mode.Rigid, true);
                }
                circle.Radius = 2;
                foreach (var id in ids) Check(MathF.Abs(PhysicsServer.BodyGetInertia(id) - 54) < .002f, "Resolved automatic inertia follows ordinary shared resource edits");
                circle.Radius = 1;
                if (solver is not null)
                    for (var i = 0; i < ids.Length; i++)
                        foreach (var mode in new[] { Mode.Static, Mode.Kinematic, Mode.Rigid, Mode.RigidLinear })
                            for (var operation = 0; operation < 4; operation++)
                                for (var path = 0; path < 9; path++)
                                    for (var failures = 1; failures <= 2; failures++)
                                    {
                                        Configure(i, mode); checkpoint.Capture();
                                        using var map = new PhysicsSnapshotMap(world.Space);
                                        for (var j = 0; j < ids.Length; j++) map.Bind((ulong)j + 1, 1, ids[j]);
                                        map.Capture(snapshot); query.Position = PhysicsServer.BodyGetTransform(ids[i]).Origin + new Vector2(8, 0);
                                        var oldView = views[i]; var original = new ApplicationException("mass-profile-apply"); var retry = new ArithmeticException("mass-profile-retry"); var calls = 0;
                                        solver.MassHook = id =>
                                        {
                                            if (id != ids[i]) return;
                                            if (++calls > failures) { solver.MassHook = null; return; }
                                            Check(PhysicsServer.BodyGetMass(id) == 2 && PhysicsServer.BodyGetInertia(id) == 8 && PhysicsServer.BodyGetCenterOfMass(id) == new Vector2(1, -2),
                                                "A partial mass hook reads preceding authoring without recursive preparation");
                                            Reject<InvalidOperationException>(() => PhysicsServer.BodyResetMassProperties(id));
                                            Reject<InvalidOperationException>(() => _ = oldView.InverseMass);
                                            _ = solver.Bodies[id].PartMass(99, 3, new Vector2(50, 51));
                                            if (calls == 1) throw original; throw retry;
                                        };
                                        var tick = PhysicsServer.SpaceGetTick(world.Space); var errors = ReleaseErrors(() => Change(i, operation));
                                        Check(errors.Length == 1 && ReferenceEquals(errors[0], original) && calls == 1,
                                            "Mass/center/inertia/reset writes preserve the original selected publication failure");
                                        Check(PhysicsServer.BodyGetMass(ids[i]) == 2 && PhysicsServer.BodyGetInertia(ids[i]) == 8 &&
                                            PhysicsServer.BodyGetCenterOfMass(ids[i]) == new Vector2(1, -2) && calls == 1,
                                            "Failed mass publication keeps authoring and configured reads do not repair");
                                        if (failures == 2)
                                        {
                                            errors = ReleaseErrors(Touch);
                                            Check(errors.Length == 1 && ReferenceEquals(errors[0], retry) && calls == 2 && PhysicsServer.SpaceGetTick(world.Space) == tick,
                                                "A repeated preparation failure preserves its error, debt and public tick");
                                        }
                                        Touch(); Verify(i, mode, false);
                                        Check(ReferenceEquals(PhysicsServer.BodyGetDirectState(ids[i]), oldView), "Mass repair preserves the existing direct-state view");
                                        if (path != 3) Check(PhysicsServer.SpaceGetTick(world.Space) == tick, "Numerical mass preparation does not simulate");
                                        if (path == 4) Check(MathF.Abs(PhysicsServer.BodyGetLinearVelocity(ids[i]).X - (mode is Mode.Rigid or Mode.RigidLinear ? 1 : 0)) < .002f,
                                            "Impulse preparation uses restored mass before applying the request");
                                        faults++;
                                        void Touch()
                                        {
                                            if (path == 0) _ = oldView.InverseMass;
                                            else if (path == 1) _ = PhysicsServer.BodyGetDirectState(ids[i]);
                                            else if (path == 2) _ = world.DirectSpaceState.IntersectPoint(query, hits);
                                            else if (path == 3) tree.PhysicsFrame(.01);
                                            else if (path == 4) PhysicsServer.BodyApplyCentralImpulse(ids[i], new(2, 0));
                                            else if (path == 5) checkpoint.Capture();
                                            else if (path == 6) map.Capture(snapshot);
                                            else if (path == 7) map.Apply(snapshot);
                                            else checkpoint.Restore();
                                        }
                                    }
                if (solver is not null)
                    for (var i = 0; i < ids.Length; i++)
                    {
                        Configure(i, Mode.Rigid); var old = solver.Bodies[ids[i]]; var calls = 0; var failure = new ApplicationException("retired-mass"); var oldView = views[i];
                        solver.MassHook = id => { if (id == ids[i] && ReferenceEquals(old, solver.Bodies[id])) { calls++; old.Mass = 99; throw failure; } };
                        Check(ReferenceEquals(ReleaseErrors(() => PhysicsServer.BodyResetMassProperties(ids[i]))[0], failure), "The retirement control has incomplete selected mass");
                        PhysicsServer.BodySetSpace(ids[i], default); Check(calls == 1, "Retirement does not retry the discarded selected mass profile");
                        Reject<InvalidOperationException>(() => _ = oldView.InverseMass);
                        PhysicsServer.BodySetSpace(ids[i], world.Space); views[i] = PhysicsServer.BodyGetDirectState(ids[i])!;
                        Verify(i, Mode.Rigid, false); Check(calls == 1, "A fresh attachment reconstructs the preceding accepted mass profile"); solver.MassHook = null;
                    }
                for (var i = 3; i <= 4; i++)
                {
                    Configure(i, Mode.Rigid); var rigid = (RigidBody)nodes[i]; var failure = new ApplicationException("mass-list-notification");
                    Action<ElectronObject> fail = _ => throw failure; rigid.PropertyListChanged += fail;
                    try { Check(ReferenceEquals(ReleaseErrors(() => PhysicsServer.BodyResetMassProperties(ids[i]))[0], failure), "The mode notification reports after profile commit"); }
                    finally { rigid.PropertyListChanged -= fail; }
                    Verify(i, Mode.Rigid, true);
                }
                if (solver is not null) solver.StepHook = () =>
                {
                    Check((Environment.CurrentManagedThreadId != owner) == threaded, "Mass input checks run on the captured numerical lane");
                    foreach (var id in ids) Check(PhysicsServer.BodyGetMass(id) == 2 && MathF.Abs(solver.Bodies[id].Inertia - 51) < .002f && solver.Bodies[id].LocalCenter.DistanceTo(new(3, 0)) < .002f,
                        "All six automatic profiles reach the selected numerical owner");
                    numericalReads++;
                    if (!probeWrites) return;
                    foreach (var id in ids)
                    {
                        Reject<InvalidOperationException>(() => PhysicsServer.BodyResetMassProperties(id));
                        Reject<InvalidOperationException>(() => PhysicsServer.BodyGetInertia(id));
                        Reject<InvalidOperationException>(() => PhysicsServer.BodyGetCenterOfMass(id));
                    }
                    probeWrites = false;
                };
                for (var frame = 0; frame < 128; frame++) Frame(frame);
                Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); var beforeReads = numericalReads;
                for (var frame = 0; frame < 128; frame++) Frame(frame);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0, $"128 complete mass-reset frames allocated {own}/{all} owner/all-thread B");
                Check(solver is null || numericalReads == beforeReads + 128, "Every measured frame observes six actual automatic mass profiles in Solve");
                if (solver is not null)
                {
                    solver.StepHook = null; solver.FailSolve = true; Reject<AggregateException>(() => tree.PhysicsFrame(.01));
                    foreach (var id in ids)
                    {
                        Reject<InvalidOperationException>(() => PhysicsServer.BodyResetMassProperties(id));
                        Reject<InvalidOperationException>(() => PhysicsServer.BodyGetMass(id));
                    }
                    Check(PhysicsServer.SpaceGetBackend(world.Space) == PhysicsServer.Backend.Extension && PhysicsServer.SpaceGetBackendFallbackReason(world.Space) is null,
                        "A failed numerical world does not repair by switching implementation");
                }
                Console.WriteLine($"Mass reset {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: six owners, 24 role/reset/reentry cases, {faults} partial failure cases, 128 impulse/full-frame/geometry cycles {own}/{all} B passed.");

                void TouchShape(int i)
                {
                    for (var slot = 1; slot <= 2; slot++)
                    {
                        query.Position = PhysicsServer.BodyGetTransform(ids[i]) * new Vector2(slot == 1 ? 8 : -2, 0);
                        Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == ids[i] && hits[0].ShapeIndex == slot,
                            "Mass reset preserves both actual logical fixtures and the disabled gap");
                    }
                }
                void Frame(int frame)
                {
                    var mode = (Mode)(frame & 3);
                    for (var i = 0; i < ids.Length; i++)
                    {
                        Configure(i, mode); PhysicsServer.BodyResetMassProperties(ids[i]); Verify(i, mode, true);
                        PhysicsServer.BodyApplyCentralImpulse(ids[i], new(2, 0)); PhysicsServer.BodyApplyTorqueImpulse(ids[i], 51);
                    }
                    tree.PhysicsFrame(.01);
                    for (var i = 0; i < ids.Length; i++)
                    {
                        Verify(i, mode, true); TouchShape(i);
                        Check(MathF.Abs(PhysicsServer.BodyGetLinearVelocity(ids[i]).X - (mode is Mode.Rigid or Mode.RigidLinear ? 1 : 0)) < .002f &&
                            MathF.Abs(PhysicsServer.BodyGetAngularVelocity(ids[i]) - (mode == Mode.Rigid ? 1 : 0)) < .002f,
                            "Actual impulse response uses reset automatic mass/inertia and physical role");
                    }
                }
            }
            finally { if (solver is not null) { solver.MassHook = null; solver.StepHook = null; } PhysicsServer.FreeRID(raw); }
            void Configure(int i, Mode mode)
            {
                if (solver is not null) solver.MassHook = null;
                PhysicsServer.BodySetMode(ids[i], mode); PhysicsServer.BodySetTransform(ids[i], new(0, new(i * 100, 0)));
                PhysicsServer.BodySetLinearVelocity(ids[i], Vector2.Zero); PhysicsServer.BodySetAngularVelocity(ids[i], 0);
                PhysicsServer.BodySetMass(ids[i], 2); PhysicsServer.BodySetInertia(ids[i], 8); PhysicsServer.BodySetCenterOfMass(ids[i], new(1, -2));
            }
            void Change(int i, int operation)
            {
                if (operation == 0) PhysicsServer.BodySetMass(ids[i], 3);
                else if (operation == 1) PhysicsServer.BodySetInertia(ids[i], 9);
                else if (operation == 2) PhysicsServer.BodySetCenterOfMass(ids[i], new(2, -1));
                else PhysicsServer.BodyResetMassProperties(ids[i]);
            }
            void Verify(int i, Mode mode, bool automatic)
            {
                var center = automatic ? new Vector2(3, 0) : new Vector2(1, -2); var inertia = automatic ? 51 : 8;
                Check(PhysicsServer.BodyGetMass(ids[i]) == 2 && MathF.Abs(PhysicsServer.BodyGetInertia(ids[i]) - inertia) < .002f &&
                    PhysicsServer.BodyGetCenterOfMass(ids[i]).DistanceTo(center) < .002f && views[i].CenterOfMassLocal.DistanceTo(center) < .002f,
                    "Configured mass and automatic/custom profile metadata remain coherent");
                Check(MathF.Abs(views[i].InverseMass - (mode is Mode.Rigid or Mode.RigidLinear ? .5f : 0)) < .0001f &&
                    MathF.Abs(views[i].InverseInertia - (mode == Mode.Rigid ? 1f / inertia : 0)) < .0001f,
                    "Selected numerical inverse mass and inertia use the repaired profile and current role");
                if (automatic && i is 3 or 4) Check(((RigidBody)nodes[i]).Inertia == 0 && ((RigidBody)nodes[i]).CenterOfMassMode == RigidCenterOfMassMode.Auto &&
                    ((RigidBody)nodes[i]).CenterOfMass == Vector2.Zero, "Reset clears stored rigid/bone overrides while retaining computed metadata");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
