using Electron2D;
using Mode = Electron2D.PhysicsServer.BodyMode;

public static partial class BackendChecks
{
    public static void RunGravityPublication(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("GravityPublication", () => solver = new() { UsePublishedGravityScale = true });
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("GravityPublication");
            using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 1 };
            RigidBody[] nodes = [new RigidBody(), new PhysicalBone()];
            foreach (var node in nodes) root.AddChild(node);
            StaticBody[] surfaces = [new StaticBody(), new AnimatableBody()];
            foreach (var surface in surfaces) root.AddChild(surface);
            using var tree = new SceneTree(root); var ids = nodes.Select(node => node.GetRID()).ToArray();
            PhysicsServer.AreaSetGravity(world.Space, 10); PhysicsServer.AreaSetGravityVector(world.Space, Vector2.Down);
            PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
            using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            var views = new PhysicsDirectBodyState[ids.Length]; var cases = 0;
            for (var i = 0; i < ids.Length; i++)
            {
                PhysicsServer.BodyAddShape(ids[i], circle.GetRID()); PhysicsServer.BodySetCollisionMask(ids[i], 0);
                PhysicsServer.BodySetCanSleep(ids[i], false); PhysicsServer.BodySetLinearDampMode(ids[i], RigidBody.DampMode.Replace);
                PhysicsServer.BodySetAngularDampMode(ids[i], RigidBody.DampMode.Replace);
                Configure(i, Mode.Rigid, false); views[i] = PhysicsServer.BodyGetDirectState(ids[i])!;
            }
            Check(world.PhysicsBackendName == (backend?.ToString() ?? "GravityPublication") && world.PhysicsFallbackReason is null,
                "Gravity publication uses the requested numerical owner without fallback");
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space); var snapshot = new PhysicsSnapshot(65536);
            if (solver is not null)
                for (var i = 0; i < ids.Length; i++)
                    foreach (var mode in new[] { Mode.Rigid, Mode.RigidLinear, Mode.Static, Mode.Kinematic })
                        for (var operation = 0; operation < 6; operation++)
                            foreach (var path in new[] { 3, 0, 1, 2, 4, 5, 6, 7, 8 })
                                for (var failures = 1; failures <= 2; failures++)
                                {
                                    Configure(i, mode, operation >= 4);
                                    var scale = operation < 2 ? 3 : 2; var omitted = operation is 2 or 3; var published = omitted ? 0 : scale;
                                    Change(i, operation); checkpoint.Capture();
                                    using var map = new PhysicsSnapshotMap(world.Space);
                                    for (var j = 0; j < ids.Length; j++) map.Bind((ulong)j + 1, 1, ids[j]);
                                    for (var j = 0; j < surfaces.Length; j++) map.Bind((ulong)(ids.Length + j) + 1, 1, surfaces[j].GetRID());
                                    map.Capture(snapshot); Configure(i, mode, operation >= 4); query.Position = PhysicsServer.BodyGetTransform(ids[i]).Origin;
                                    var original = new ApplicationException("gravity-publication"); var retry = new ArithmeticException("gravity-retry"); var calls = 0;
                                    solver.GravityHook = (id, value) =>
                                    {
                                        if (id != ids[i]) return;
                                        if (++calls > failures) return;
                                        Check(PhysicsServer.BodyGetGravityScale(id) == scale && PhysicsServer.BodyIsOmittingForceIntegration(id) == omitted,
                                            "Gravity hook reads committed authoring without numerical preparation");
                                        Reject<InvalidOperationException>(() => PhysicsServer.BodySetGravityScale(id, scale));
                                        if (i == 1) Reject<InvalidOperationException>(() => _ = views[i].InverseMass);
                                        solver.Bodies[id].PublishedGravityScale = 99;
                                        if (calls == 1) throw original; throw retry;
                                    };
                                    var tick = PhysicsServer.SpaceGetTick(world.Space); var errors = ReleaseErrors(() => Change(i, operation));
                                    Check(errors.Length == 1 && ReferenceEquals(errors[0], original) && calls == 1,
                                        "A gravity/omission write preserves the original selected error");
                                    Check(PhysicsServer.BodyGetGravityScale(ids[i]) == scale && PhysicsServer.BodyIsOmittingForceIntegration(ids[i]) == omitted && calls == 1,
                                        "Committed gravity/omission reads do not repair numerical publication");
                                    if (failures == 2)
                                    {
                                        errors = ReleaseErrors(Touch);
                                        Check(errors.Length == 1 && ReferenceEquals(errors[0], retry) && calls == 2 && PhysicsServer.SpaceGetTick(world.Space) == tick,
                                            "Repeated gravity preparation retains its error and tick");
                                    }
                                    Touch();
                                    if (path == 3)
                                    {
                                        var actual = PhysicsServer.BodyGetLinearVelocity(ids[i]).Y;
                                        var expected = !omitted && mode is Mode.Rigid or Mode.RigidLinear ? scale : 0;
                                        Check(MathF.Abs(actual - expected) < .002f, $"Gravity publication leaked numerical velocity {actual}; expected {expected}");
                                    }
                                    Check(solver.Bodies[ids[i]].PublishedGravityScale == published && calls == failures + 1,
                                        "Numerical preparation republishes the current effective gravity scale");
                                    Check(ReferenceEquals(PhysicsServer.BodyGetDirectState(ids[i]), views[i]), "Gravity repair preserves the cached body view");
                                    if (path != 3) Check(PhysicsServer.SpaceGetTick(world.Space) == tick, "Gravity preparation does not simulate");
                                    solver.GravityHook = null; cases++;
                                    void Touch()
                                    {
                                        if (path == 0) _ = views[i].InverseMass;
                                        else if (path == 1) _ = PhysicsServer.BodyGetDirectState(ids[i]);
                                        else if (path == 2) _ = world.DirectSpaceState.IntersectPoint(query, hits);
                                        else if (path == 3) tree.PhysicsFrame(.1);
                                        else if (path == 4) PhysicsServer.BodyApplyCentralImpulse(ids[i], Vector2.Zero);
                                        else if (path == 5) checkpoint.Capture();
                                        else if (path == 6) map.Capture(snapshot);
                                        else if (path == 7) map.Apply(snapshot);
                                        else checkpoint.Restore();
                                    }
                                }
            if (solver is not null)
            {
                for (var i = 0; i < ids.Length; i++)
                    foreach (var operation in new[] { 0, 2, 4 })
                    {
                        Configure(i, Mode.Rigid, operation >= 4); var old = solver.Bodies[ids[i]]; var oldView = views[i]; var calls = 0;
                        var failure = new ApplicationException("retired-gravity");
                        solver.GravityHook = (id, _) =>
                        {
                            if (id != ids[i] || !ReferenceEquals(solver.Bodies[id], old)) return;
                            calls++; old.PublishedGravityScale = 99; throw failure;
                        };
                        Check(ReferenceEquals(ReleaseErrors(() => Change(i, operation))[0], failure), "Retirement starts with a failed selected gravity request");
                        PhysicsServer.BodySetSpace(ids[i], default); Check(calls == 1, "Retirement does not retry discarded gravity publication");
                        Reject<InvalidOperationException>(() => _ = oldView.InverseMass);
                        PhysicsServer.BodySetSpace(ids[i], world.Space); views[i] = PhysicsServer.BodyGetDirectState(ids[i])!;
                        Check(calls == 1 && solver.Bodies[ids[i]].PublishedGravityScale == (nodes[i].CustomIntegrator ? 0 : nodes[i].GravityScale),
                            "Reattachment reconstructs current authoring without replaying the retired request");
                        solver.GravityHook = null;
                    }
                for (var direct = 0; direct < 2; direct++)
                {
                    Configure(0, Mode.Rigid, false); var calls = 0; var failure = new ApplicationException("superseded-gravity");
                    solver.GravityHook = (id, _) => { if (id == ids[0] && ++calls == 1) { solver.Bodies[id].PublishedGravityScale = 99; throw failure; } };
                    Check(ReferenceEquals(ReleaseErrors(() => nodes[0].GravityScale = 3)[0], failure), "Replacement starts with pending gravity");
                    if (direct == 0) nodes[0].GravityScale = 4; else PhysicsServer.BodySetGravityScale(ids[0], 4);
                    Check(nodes[0].GravityScale == 4 && solver.Bodies[ids[0]].PublishedGravityScale == 4 && calls == (direct == 0 ? 2 : 3),
                        "An explicit replacement preserves scene/RID preparation order and publishes the new value");
                    solver.GravityHook = null;
                }
            }
            foreach (var node in nodes)
            {
                Reject<ArgumentOutOfRangeException>(() => node.GravityScale = float.NaN);
                Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetGravityScale(node.GetRID(), float.PositiveInfinity));
            }
            Task.Run(() =>
            {
                foreach (var node in nodes)
                {
                    Reject<InvalidOperationException>(() => node.GravityScale = 2);
                    Reject<InvalidOperationException>(() => node.CustomIntegrator = true);
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetGravityScale(node.GetRID(), 2));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetOmitForceIntegration(node.GetRID(), true));
                }
            }).GetAwaiter().GetResult();
            Configure(0, Mode.Rigid, false); var callbacks = 0;
            PhysicsServer.BodySetForceIntegrationCallback(ids[0], _ =>
            {
                nodes[0].CustomIntegrator = true; nodes[0].LinearDamp = 1; nodes[0].AngularDamp = 2;
                nodes[0].LinearDampMode = RigidBody.DampMode.Combine; nodes[0].AngularDampMode = RigidBody.DampMode.Combine;
                nodes[0].PhysicsMaterialOverride = null; surfaces[0].PhysicsMaterialOverride = null; callbacks++;
            });
            try { tree.PhysicsFrame(.1); }
            finally { PhysicsServer.BodySetForceIntegrationCallback(ids[0], null); }
            Check(callbacks == 1 && nodes[0].CustomIntegrator && nodes[0].LinearDamp == 1 && nodes[0].AngularDamp == 2,
                "Ordinary completed-step callbacks retain parameter writes on a healthy world");
            foreach (var node in nodes)
            {
                node.LinearDamp = 0; node.AngularDamp = 0;
                node.LinearDampMode = RigidBody.DampMode.Replace; node.AngularDampMode = RigidBody.DampMode.Replace;
            }
            var owner = Environment.CurrentManagedThreadId; var numericalReads = 0;
            if (solver is not null) solver.StepHook = () =>
            {
                Check((Environment.CurrentManagedThreadId != owner) == threaded, "Gravity is observed on the captured numerical lane");
                foreach (var id in ids)
                    Check(solver.Bodies[id].PublishedGravityScale == (PhysicsServer.BodyIsOmittingForceIntegration(id) ? 0 : PhysicsServer.BodyGetGravityScale(id)),
                        "The selected numerical owner consumes the complete effective gravity publication");
                numericalReads++;
            };
            for (var frame = 0; frame < 128; frame++) Frame(frame);
            Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); var beforeReads = numericalReads;
            for (var frame = 0; frame < 128; frame++) Frame(frame);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"128 gravity/omission full frames allocated {own}/{all} owner/all-thread B");
            Check(solver is null || numericalReads == beforeReads + 128, "Every measured frame observes both effective native gravity values");
            if (solver is not null)
            {
                solver.StepHook = null; solver.FailSolve = true; Reject<AggregateException>(() => tree.PhysicsFrame(.1));
                var before = nodes.Select(node => (node.GravityScale, node.CustomIntegrator, node.LinearDamp, node.AngularDamp,
                    node.LinearDampMode, node.AngularDampMode, node.PhysicsMaterialOverride, node.Mass)).ToArray();
                var accepted = 0;
                foreach (var node in nodes)
                {
                    (string Name, Action Write)[] writes = [
                        ("GravityScale", () => node.GravityScale = 2), ("CustomIntegrator", () => node.CustomIntegrator = true),
                        ("LinearDamp", () => node.LinearDamp = 1), ("AngularDamp", () => node.AngularDamp = 1),
                        ("LinearDampMode", () => node.LinearDampMode = RigidBody.DampMode.Combine),
                        ("AngularDampMode", () => node.AngularDampMode = RigidBody.DampMode.Combine),
                        ("PhysicsMaterialOverride", () => node.PhysicsMaterialOverride = null), ("Mass", () => node.Mass = 2)];
                    foreach (var (name, write) in writes)
                    {
                        Exception? failure = null; try { write(); } catch (Exception error) { failure = error; }
                        if (failure is InvalidOperationException) continue;
                        accepted++; Console.WriteLine($"Failed world accepted {node.GetType().Name}.{name}");
                    }
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetGravityScale(node.GetRID(), 2));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetOmitForceIntegration(node.GetRID(), true));
                }
                foreach (var surface in surfaces)
                {
                    Exception? failure = null; try { surface.PhysicsMaterialOverride = null; } catch (Exception error) { failure = error; }
                    if (failure is InvalidOperationException) continue;
                    accepted++; Console.WriteLine($"Failed world accepted {surface.GetType().Name}.PhysicsMaterialOverride");
                }
                Check(accepted == 0, $"Failed world accepted {accepted} scene parameter writes");
                for (var i = 0; i < nodes.Length; i++)
                    Check(before[i] == (nodes[i].GravityScale, nodes[i].CustomIntegrator, nodes[i].LinearDamp, nodes[i].AngularDamp,
                        nodes[i].LinearDampMode, nodes[i].AngularDampMode, nodes[i].PhysicsMaterialOverride, nodes[i].Mass),
                        "Terminal guards reject before changing any scene parameter");
                Check(PhysicsServer.SpaceGetBackend(world.Space) == PhysicsServer.Backend.Extension && PhysicsServer.SpaceGetBackendFallbackReason(world.Space) is null,
                    "A numerical failure retains the selected implementation without fallback");
            }
            Console.WriteLine($"Gravity publication {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: {cases} partial failure cases, 128 changed full frames {own}/{all} B passed.");
            void Frame(int frame)
            {
                var mode = (Mode)(frame & 3); var scale = (frame & 8) == 0 ? 3 : -2; var omitted = (frame & 4) != 0;
                for (var i = 0; i < ids.Length; i++)
                {
                    Configure(i, mode, false);
                    if ((i + frame) % 2 == 0) { nodes[i].GravityScale = scale; nodes[i].CustomIntegrator = omitted; }
                    else { PhysicsServer.BodySetGravityScale(ids[i], scale); PhysicsServer.BodySetOmitForceIntegration(ids[i], omitted); }
                }
                tree.PhysicsFrame(.1);
                for (var i = 0; i < ids.Length; i++)
                {
                    var expected = !omitted && mode is Mode.Rigid or Mode.RigidLinear ? scale : 0;
                    Check(MathF.Abs(PhysicsServer.BodyGetLinearVelocity(ids[i]).Y - expected) < .002f &&
                        PhysicsServer.BodyGetGravityScale(ids[i]) == scale && PhysicsServer.BodyIsOmittingForceIntegration(ids[i]) == omitted,
                        "Changed signed gravity and omission produce the actual role-dependent motion");
                    if (mode is Mode.Rigid or Mode.RigidLinear)
                        Check(MathF.Abs(views[i].TotalGravity.Y - 10 * scale) < .002f, "Reported fields retain configured gravity even when default integration is omitted");
                    query.Position = PhysicsServer.BodyGetTransform(ids[i]).Origin;
                    Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == ids[i] && hits[0].ShapeIndex == 0,
                        "Gravity publication preserves actual shape and collider identity");
                }
            }

            void Configure(int i, Mode mode, bool omitted)
            {
                if (solver is not null) solver.GravityHook = null;
                nodes[i].CustomIntegrator = false; nodes[i].GravityScale = 2;
                PhysicsServer.BodySetMode(ids[i], mode); PhysicsServer.BodySetTransform(ids[i], new(0, new(i * 100, 0)));
                PhysicsServer.BodySetLinearVelocity(ids[i], Vector2.Zero); PhysicsServer.BodySetAngularVelocity(ids[i], 0);
                nodes[i].CustomIntegrator = omitted;
            }
            void Change(int i, int operation)
            {
                if (operation == 0) nodes[i].GravityScale = 3;
                else if (operation == 1) PhysicsServer.BodySetGravityScale(ids[i], 3);
                else if (operation == 2) nodes[i].CustomIntegrator = true;
                else if (operation == 3) PhysicsServer.BodySetOmitForceIntegration(ids[i], true);
                else if (operation == 4) nodes[i].CustomIntegrator = false;
                else PhysicsServer.BodySetOmitForceIntegration(ids[i], false);
            }
        }
        finally { if (solver is not null) { solver.GravityHook = null; solver.StepHook = null; } ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
