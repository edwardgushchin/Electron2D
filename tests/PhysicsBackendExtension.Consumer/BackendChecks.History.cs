using Electron2D;

public static partial class BackendChecks
{
    public static void RunHistory(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { RunHistoryCore(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void RunHistoryCore(PhysicsServer.Backend? backend, bool threaded)
    {
        var ownerThread = Thread.CurrentThread;
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("History", () => solver = new() { StepHook = () => Check((Thread.CurrentThread != ownerThread) == threaded, "Selected Solve retains the requested numerical thread") });
        using var world = backend is { } selected ? new World(selected) : new World("History");
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
            "History exercises the requested numerical owner without fallback");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 2 };
        var scene = new RigidBody { GravityScale = 0, CanSleep = true, CollisionLayer = 0, CollisionMask = 0 };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate(); RID extra = default;
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetCollisionLayer(raw, 0); PhysicsServer.BodySetCollisionMask(raw, 0);
            PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetSpace(raw, world.Space);
            PhysicsServer.BodySetSleeping(raw, true);
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            PhysicsServer.BodySetCanSleep(raw, false); PhysicsServer.BodyApplyCentralImpulse(raw, new(6, 0));
            tree.PhysicsFrame(1d / 60); checkpoint.Restore();
            Check(PhysicsServer.BodyGetSleeping(raw) && PhysicsServer.BodyGetCanSleep(raw) && PhysicsServer.BodyGetLinearVelocity(raw) == Vector2.Zero,
                "Private history restores numerical sleep state as well as pose and velocity");
            if (solver is not null) Check(!solver.Bodies[raw].Awake && solver.Bodies[raw].CanSleep, "The independent numerical attachment agrees with restored sleep policy");

            extra = PhysicsServer.BodyCreate(); PhysicsServer.BodySetGravityScale(extra, 0); PhysicsServer.BodySetSpace(extra, world.Space);
            checkpoint.Capture(); var tick = checkpoint.Tick;
            PhysicsServer.BodySetLinearVelocity(extra, new(3, 4)); tree.PhysicsFrame(1d / 60); checkpoint.Restore();
            Check(PhysicsServer.SpaceGetTick(world.Space) == tick && PhysicsServer.BodyGetLinearVelocity(extra) == Vector2.Zero,
                "A reusable checkpoint captures increased current topology");
            PhysicsServer.FreeRID(extra); extra = default; checkpoint.Capture();
            PhysicsServer.BodySetSleeping(raw, false); tree.PhysicsFrame(1d / 60); checkpoint.Restore();
            Check(PhysicsServer.BodyGetSleeping(raw), "Recapture ignores retired entries beyond the current prefix");

            foreach (var body in new[] { scene.GetRID(), raw })
            {
                PhysicsServer.BodySetCanSleep(body, false); PhysicsServer.BodySetSleeping(body, false);
                PhysicsServer.BodySetMass(body, 2); PhysicsServer.BodySetInertia(body, 2);
                PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(body, 0);
                PhysicsServer.BodySetAngularDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(body, 0);
                PhysicsServer.BodySetLinearVelocity(body, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(body, 0);
                PhysicsServer.BodyApplyCentralForce(body, new(12, 0)); PhysicsServer.BodyApplyTorque(body, 6);
            }
            checkpoint.Capture(); tree.PhysicsFrame(1d / 60);
            var expectedPose = PhysicsServer.BodyGetTransform(raw); var expectedVelocity = PhysicsServer.BodyGetLinearVelocity(raw);
            var expectedAngular = PhysicsServer.BodyGetAngularVelocity(raw);
            Check(MathF.Abs(expectedVelocity.X - .1f) < .001f && MathF.Abs(expectedAngular - .05f) < .001f,
                "Prepared replay has an actual force/torque response in engine units");
            PhysicsServer.BodyApplyCentralForce(raw, new(500, 0)); PhysicsServer.BodyApplyTorque(raw, 100);
            tree.PhysicsFrame(1d / 30); checkpoint.Restore(); tree.PhysicsFrame(1d / 60);
            Check(PhysicsServer.BodyGetTransform(raw).Origin.DistanceTo(expectedPose.Origin) < .001f &&
                PhysicsServer.BodyGetLinearVelocity(raw).DistanceTo(expectedVelocity) < .001f &&
                MathF.Abs(PhysicsServer.BodyGetAngularVelocity(raw) - expectedAngular) < .001f,
                "Pending commands are restored once, without replaying commands from the discarded future");

            foreach (var body in new[] { scene.GetRID(), raw })
            {
                PhysicsServer.BodySetCollisionLayer(body, 1); PhysicsServer.BodySetCollisionMask(body, 1);
                PhysicsServer.BodySetMaxContactsReported(body, 4);
                PhysicsServer.BodySetLinearVelocity(body, Vector2.Zero); PhysicsServer.BodySetAngularVelocity(body, 0);
            }
            PhysicsServer.BodySetTransform(scene.GetRID(), Transform.Identity); PhysicsServer.BodySetTransform(raw, new(0, new Vector2(3, 0)));
            tree.PhysicsFrame(1d / 60);
            var state = PhysicsServer.BodyGetDirectState(raw)!;
            var count = state.GetContactCount(); Check(count > 0, "The history fixture has actual numerical contact observations");
            var contact = state.GetContactCollider(0); var point = state.GetContactLocalPosition(0); var impulse = state.GetContactImpulse(0);
            checkpoint.Capture();
            PhysicsServer.BodySetTransform(raw, new(0, new Vector2(100, 0))); tree.PhysicsFrame(1d / 30);
            Check(state.GetContactCount() == 0, "The discarded future removes the contact");
            checkpoint.Restore();
            Check(state.GetContactCount() == count && state.GetContactCollider(0) == contact &&
                state.GetContactLocalPosition(0).DistanceTo(point) < .001f && state.GetContactImpulse(0).DistanceTo(impulse) < .001f,
                "Private replay immediately restores published contact identities and values");
            if (solver is not null)
            {
                using var uncached = new OwnedState(solver, raw);
                Check(uncached.GetContactCount() > 0 && uncached.GetContactCollider(0) == contact && MathF.Abs(uncached.Step - 1f / 60) < .0000001f,
                    "Implementation-local contacts and step duration also restore for caller-created views");
                PrivateEpisodeReplay(tree, checkpoint, solver, scene.GetRID(), raw);
            }

            foreach (var body in new[] { scene.GetRID(), raw }) { PhysicsServer.BodySetCollisionLayer(body, 0); PhysicsServer.BodySetCollisionMask(body, 0); }
            PhysicsServer.BodySetCanSleep(raw, true); PhysicsServer.BodySetSleeping(raw, true); checkpoint.Capture();

            for (var i = 0; i < 128; i++) Cycle();
            Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Cycle();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"History full-step/capture/restore cycles allocated {own}/{all} B");
            Console.WriteLine($"History on {backend?.ToString() ?? "registered"}: sleep, grow/shrink recapture, pending commands/contact observations and 128 full-step/capture/restore cycles {own}/{all} B passed.");
            if (solver is not null)
            {
                var history = solver.HistoryReference!; checkpoint.Dispose(); Collect();
                Check(!history.TryGetTarget(out _), "A live disposed public wrapper releases the independent history object");
                GC.KeepAlive(checkpoint);
                var failed = FailedHistoryRelease(solver, world.Space); history = solver.HistoryReference!; Collect();
                Check(failed.IsDisposed && !history.TryGetTarget(out _), "A failed release also drops the independent history reference");
                GC.KeepAlive(failed);
                Console.WriteLine("Registered history release: live public wrappers retain neither successful nor failed provider histories.");
            }
            void Cycle() { checkpoint.Capture(); PhysicsServer.BodySetSleeping(raw, false); tree.PhysicsFrame(1d / 60); checkpoint.Restore(); }
        }
        finally { if (extra.IsValid()) PhysicsServer.FreeRID(extra); PhysicsServer.FreeRID(raw); }
        BarePortableHistory(backend);
    }

    private static void BarePortableHistory(PhysicsServer.Backend? backend)
    {
        using var world = backend is { } selected ? new World(selected) : new World("History");
        using var root = new SubViewport { World = world }; using var tree = new SceneTree(root); _ = root.FindWorld();
        var body = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false);
            PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(body, 0);
            PhysicsServer.BodySetLinearVelocity(body, new(3, 0)); PhysicsServer.BodySetSpace(body, world.Space);
            using var map = new PhysicsSnapshotMap(world.Space); map.Bind(1, 1, body);
            var snapshot = new PhysicsSnapshot(); map.Capture(snapshot); map.Apply(snapshot);
            Check(PhysicsServer.SpaceGetTick(world.Space) == 0 && PhysicsServer.BodyGetLinearVelocity(body) == new Vector2(3, 0),
                "A shapeless world applies portable history before its first simulation interval");
            for (var i = 0; i < 128; i++) Cycle();
            Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Cycle();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Shapeless portable history allocated {own}/{all} B");
            tree.PhysicsFrame(1d / 60);
            var tick = PhysicsServer.SpaceGetTick(world.Space); var position = PhysicsServer.BodyGetTransform(body).Origin;
            Check(tick == 1 && MathF.Abs(position.X - .05f) < .0001f,
                $"The restored shapeless body advances with its captured velocity: tick={tick}, position={position}");
            Console.WriteLine($"Shapeless {backend?.ToString() ?? "registered"} history before first step: apply, actual motion and 128 capture/apply cycles {own}/{all} B.");
            void Cycle() { map.Capture(snapshot); PhysicsServer.BodySetLinearVelocity(body, new(6, 0)); map.Apply(snapshot); }
        }
        finally { PhysicsServer.FreeRID(body); }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static PhysicsCheckpoint FailedHistoryRelease(Solver solver, RID space)
    {
        var checkpoint = PhysicsServer.SpaceCreateCheckpoint(space); solver.FailHistoryRelease = true;
        try
        {
            try { checkpoint.Dispose(); }
            catch (InvalidOperationException error) when (error.Message == "Expected independent history release failure.") { return checkpoint; }
            throw new Exception("Expected independent history release failure");
        }
        finally { solver.FailHistoryRelease = false; }
    }

    private static void PrivateEpisodeReplay(SceneTree tree, PhysicsCheckpoint checkpoint, Solver solver, RID first, RID second)
    {
        PhysicsServer.BodySetTransform(first, Transform.Identity); PhysicsServer.BodySetTransform(second, new(0, new Vector2(3, 0)));
        PhysicsServer.BodySetLinearVelocity(first, new(60, 0)); PhysicsServer.BodySetLinearVelocity(second, Vector2.Zero);
        solver.OneWays.Clear(); solver.OneWays.Add(new(first, 0, 0, second, 0, 0, false));
        checkpoint.Capture(); tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(PhysicsServer.BodyGetLinearVelocity(first).X - 60) < .001f, "Captured rejected episode permits real pass-through");
        solver.OneWays[0] = solver.OneWays[0] with { Allowed = true }; checkpoint.Restore();
        tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(PhysicsServer.BodyGetLinearVelocity(first).X - 60) < .001f && solver.Pairs == 0,
            "Private history restores the numerical one-way decision before the next collision step");
        checkpoint.Restore(); solver.OneWays[0] = solver.OneWays[0] with { Allowed = true }; tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(PhysicsServer.BodyGetLinearVelocity(first).X - 30) < .001f && solver.Pairs == 1,
            "The same restored physical state responds differently to an explicitly changed episode");
        for (var i = 0; i < 128; i++) Cycle();
        Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Cycle();
        own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(own == 0 && all == 0, $"Episode/contact history cycles allocated {own}/{all} B");
        Console.WriteLine($"Registered private episode/contact history: 128 capture/changed-decision/full-step/restore cycles {own}/{all} B.");
        solver.OneWays.Clear();
        void Cycle()
        {
            checkpoint.Restore(); checkpoint.Capture();
            solver.OneWays[0] = solver.OneWays[0] with { Allowed = true }; tree.PhysicsFrame(1d / 60); checkpoint.Restore();
        }
    }
}
