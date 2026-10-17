using Electron2D;

public static partial class BackendChecks
{
    public static void RunActivationTransactions(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null; var cases = 0;
        if (backend is null) PhysicsServerManager.RegisterServer("ActivationTransactions", () => solver = new());
        try
        {
            if (backend is null)
            {
                foreach (var scene in new[] { false, true })
                    foreach (var committed in new[] { false, true })
                        foreach (var after in new[] { false, true })
                            for (var route = 0; route < 8; route++) Failure(scene, committed, after, route);
                CallbackFailure(); StartupFailure(); PendingRetirement();
            }
            Healthy();
            Console.WriteLine($"Activation transactions {backend?.ToString() ?? "registered"}/{threaded}: {cases} publication cases; 128 full frames 0/0 owner/all-thread B.");
        }
        finally { ClearHooks(); PhysicsServer.SetActive(true); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }

        World Create() => backend is { } selected ? new World(selected) : new World("ActivationTransactions");
        void ClearHooks() { if (solver is not null) solver.ActivePublishedHook = solver.WorldPolicyHook = solver.StepHook = null; }
        void Failure(bool scene, bool committed, bool after, int route)
        {
            using var world = scene ? Create() : null; using var root = scene ? new SubViewport { World = world } : null;
            using var circle = new CircleShape { Radius = 2 };
            var node = scene ? new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(6, 0) } : null;
            if (node is not null) { node.AddChild(new CollisionShape { Shape = circle }); root!.AddChild(node); }
            using var tree = root is null ? null : new SceneTree(root);
            var space = world?.Space ?? PhysicsServer.SpaceCreate("ActivationTransactions");
            var body = node?.GetRID() ?? PhysicsServer.BodyCreate();
            try
            {
                if (node is null)
                {
                    PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid);
                    PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetGravityScale(body, 0);
                    PhysicsServer.BodySetCanSleep(body, false); PhysicsServer.BodySetLinearVelocity(body, new(6, 0));
                    PhysicsServer.BodySetSpace(body, space);
                }
                PhysicsServer.SpaceSetActive(space, committed);
                using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(space);
                using var map = new PhysicsSnapshotMap(space); var snapshot = new PhysicsSnapshot(); map.Bind(1, 1, body); map.Capture(snapshot);
                using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[2];
                var failure = new ApplicationException("selected-activation");
                if (after) solver!.ActivePublishedHook = () => throw failure;
                else solver!.WorldPolicyHook = () => throw failure;
                Original(() => PhysicsServer.SpaceSetActive(space, !committed)); ClearHooks();
                var calls = solver!.WorldPolicyCalls; var steps = solver.Steps; var tick = PhysicsServer.SpaceGetTick(space);
                Check(PhysicsServer.SpaceIsActive(space) == committed, "Activation failure preserves the committed flag");
                _ = PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.ActiveObjects);
                Task.Run(() => Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetActive(space, committed))).GetAwaiter().GetResult();
                Tick(0); PhysicsServer.SetActive(false);
                try { Tick(.01); } finally { PhysicsServer.SetActive(true); }
                Check(solver.WorldPolicyCalls == calls && solver.Steps == steps && PhysicsServer.SpaceGetTick(space) == tick,
                    "Metadata, off-owner attempts and skipped intervals do not publish pending activation");
                solver.ActivePublishedHook = () =>
                {
                    var entered = solver.WorldPolicyCalls;
                    Check(PhysicsServer.SpaceIsActive(space) == committed, "Selected activation sees the prior committed flag");
                    Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetActive(space, committed));
                    Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetActive(space, !committed));
                    Reject<InvalidOperationException>(Query);
                    Check(solver.WorldPolicyCalls == entered, "Borrowed activation and pending query reject before nested publication");
                    throw failure;
                };
                Original(Prepare);
                Check(solver.Steps == steps && PhysicsServer.SpaceGetTick(space) == tick, "Preparation errors neither start Solve nor advance a tick");
                ClearHooks(); Prepare();
                var expected = route == 1 ? !committed : committed;
                Check(PhysicsServer.SpaceIsActive(space) == expected && solver.SelectedActive == expected,
                    "Recovery publishes the explicit choice or restores the last committed activation");
                calls = solver.WorldPolicyCalls; PhysicsServer.SpaceSetActive(space, expected);
                Check(solver.WorldPolicyCalls == calls, "Completed equal activation invokes no selected hook");
                if (!expected)
                {
                    var oldTick = PhysicsServer.SpaceGetTick(space); Tick(.01);
                    Check(PhysicsServer.SpaceGetTick(space) == oldTick, "Restored inactivity remains a skipped interval");
                    PhysicsServer.SpaceSetActive(space, true);
                }
                var position = PhysicsServer.BodyGetTransform(body).Origin; steps = solver.Steps; tick = PhysicsServer.SpaceGetTick(space);
                Tick(.01);
                Check(PhysicsServer.BodyGetTransform(body).Origin.X > position.X && solver.Steps == steps + 1 && PhysicsServer.SpaceGetTick(space) == tick + 1,
                    "A recovered active world performs one real moving interval"); cases++;

                void Tick(double dt) { if (tree is null) PhysicsServer.SpaceStep(space, dt); else tree.PhysicsFrame(dt); }
                void Query() => PhysicsServer.SpaceGetDirectState(space)!.IntersectPoint(point, hits);
                void Original(Action action)
                {
                    var errors = ReleaseErrors(action);
                    Check(errors.Length == 1 && ReferenceEquals(errors[0], failure), $"Activation error identity: scene={scene}, active={committed}, after={after}, route={route}");
                }
                void Prepare()
                {
                    switch (route)
                    {
                        case 0: PhysicsServer.SpaceSetActive(space, committed); break;
                        case 1: PhysicsServer.SpaceSetActive(space, !committed); break;
                        case 2: Query(); break;
                        case 3: Tick(.01); if (!committed) Query(); break;
                        case 4: checkpoint.Capture(); break;
                        case 5: checkpoint.Restore(); break;
                        case 6: map.Capture(snapshot); break;
                        case 7: map.Apply(snapshot); break;
                    }
                }
            }
            finally
            {
                ClearHooks();
                if (node is null) PhysicsServer.FreeRID(body);
                if (world is null) PhysicsServer.FreeRID(space);
            }
        }
        void CallbackFailure()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            var body = new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(6, 0) }; root.AddChild(body);
            using var tree = new SceneTree(root); var calls = 0;
            PhysicsServer.BodySetStateSyncCallback(body.GetRID(), _ => { calls++; PhysicsServer.SpaceSetActive(world.Space, false); });
            var failure = new ApplicationException("post-step-activation"); solver!.ActivePublishedHook = () => throw failure;
            var errors = ReleaseErrors(() => tree.PhysicsFrame(.01));
            Check(errors.Length == 1 && ReferenceEquals(errors[0], failure) && PhysicsServer.SpaceGetTick(world.Space) == 1 && calls == 1,
                "Post-step activation failure preserves the completed interval and original callback error");
            ClearHooks(); PhysicsServer.BodySetStateSyncCallback(body.GetRID(), null); var position = body.Position;
            tree.PhysicsFrame(.01);
            Check(body.Position.X > position.X && PhysicsServer.SpaceGetTick(world.Space) == 2 && calls == 1,
                "Next interval restores activation without replaying the prior callback or Solve");
        }
        void StartupFailure()
        {
            Solver? rejected = null; var failure = new ApplicationException("startup-activation");
            PhysicsServerManager.RegisterServer("StartupActivationTransactions", () => rejected = new Solver
            { ActivePublishedHook = () => throw failure, FailFinish = true, FailDisposal = true });
            var errors = ReleaseErrors(() => { using var world = new World("StartupActivationTransactions"); _ = world.Space; });
            Check(errors.Length == 3 && errors.Any(error => ReferenceEquals(error, failure)) && rejected!.Finished && rejected.IsDisposed && rejected.WorldPolicyCalls == 1,
                "Failed initial activation preserves all errors and retires without repair publication");
        }
        void PendingRetirement()
        {
            using var world = Create(); _ = world.Space;
            solver!.ActivePublishedHook = () => throw new ApplicationException("retiring-activation");
            Reject<ApplicationException>(() => PhysicsServer.SpaceSetActive(world.Space, false)); var calls = solver.WorldPolicyCalls;
            world.Dispose(); Check(solver.IsDisposed && solver.WorldPolicyCalls == calls, "Retirement discards activation debt without retry"); ClearHooks();
        }
        void Healthy()
        {
            using var world = Create(); using var root = new SubViewport { World = world };
            var body = new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(6, 0) }; root.AddChild(body);
            using var tree = new SceneTree(root); var callbacks = 0;
            PhysicsServer.BodySetStateSyncCallback(body.GetRID(), _ =>
            {
                PhysicsServer.SpaceSetActive(world.Space, false); PhysicsServer.SpaceSetActive(world.Space, true); callbacks++;
            });
            for (var i = 0; i < 128; i++) Frame();
            Collect(); for (var i = 0; i < 128; i++) Frame();
            var before = callbacks; var tick = PhysicsServer.SpaceGetTick(world.Space); var position = body.Position;
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && callbacks - before == 128 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128 && body.Position.X > position.X,
                $"Changing activation full frames: callbacks={callbacks - before}, bytes={own}/{all}");
            PhysicsServer.BodySetStateSyncCallback(body.GetRID(), null);
            void Frame()
            {
                PhysicsServer.SpaceSetActive(world.Space, false); tree.PhysicsFrame(.01);
                PhysicsServer.SpaceSetActive(world.Space, true); tree.PhysicsFrame(.01); tree.PhysicsFrame(0);
                PhysicsServer.SetActive(false); try { tree.PhysicsFrame(.01); } finally { PhysicsServer.SetActive(true); }
            }
        }
    }
}
