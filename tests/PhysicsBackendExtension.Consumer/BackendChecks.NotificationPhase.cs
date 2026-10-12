using Electron2D;

public static partial class BackendChecks
{
    public static void RunNotificationPhase(PhysicsServer.Backend? backend = null)
    {
        Solver? factory = null;
        if (backend is null) PhysicsServerManager.RegisterServer("NotificationPhase", () => factory = new());
        using var world = backend is { } selected ? new World(selected) : new World("NotificationPhase");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 3 };
        using var sensorShape = new CircleShape { Radius = 12 };
        using var nestedRay = new PhysicsRayQueryParameters { From = new(200, 0), To = new(240, 0) };
        var body = new NotificationBody { GravityScale = 0, CanSleep = false, ContactMonitor = true, MaxContactsReported = 4 };
        body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body);
        var wall = new StaticBody { Name = "Wall", Position = new(4, 0) };
        wall.AddChild(new CollisionShape { Shape = circle }); root.AddChild(wall);
        var area = new Area { Name = "Sensor" }; area.AddChild(new CollisionShape { Shape = sensorShape }); root.AddChild(area);
        using var tree = new SceneTree(root);
        var space = world.Space; var solver = factory;
        var raw = PhysicsServer.BodyCreate(); var rawArea = PhysicsServer.AreaCreate();
        var force = 0; var script = 0; var sync = 0; var contacts = 0; var areas = 0; var sleeps = 0; var flushes = 0;
        var nestedSpace = default(RID); Solver? nested = null; var nest = false; var throwBody = false; var throwArea = false;
        var guardReentry = true;
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false);
            PhysicsServer.BodySetTransform(raw, new(0, new Vector2(0, 60))); PhysicsServer.BodySetSpace(raw, space);
            PhysicsServer.AreaAddShape(rawArea, sensorShape.GetRID()); PhysicsServer.AreaSetSpace(rawArea, space);
            PhysicsServer.AreaSetTransform(rawArea, new(0, new Vector2(0, 60)));
            PhysicsServer.BodySetForceIntegrationCallback(body.GetRID(), state => { Phase(); force++; state.LinearVelocity = Vector2.Zero; if (throwBody) throw new InvalidOperationException("Body notification failure"); });
            PhysicsServer.BodySetForceIntegrationCallback(raw, state => { Phase(); force++; state.LinearVelocity = Vector2.Zero; });
            body.IntegrationObserver = () => { Phase(); script++; };
            PhysicsServer.BodySetStateSyncCallback(body.GetRID(), _ => { Phase(); sync++; });
            PhysicsServer.BodySetStateSyncCallback(raw, _ => { Phase(); sync++; });
            body.BodyEntered += _ => { Phase(); contacts++; };
            body.BodyExited += _ => { Phase(); contacts++; };
            body.SleepingStateChanged += _ => { Phase(); sleeps++; };
            area.BodyEntered += _ => { Phase(); areas++; if (throwArea) throw new InvalidOperationException("Area notification failure"); };
            area.BodyExited += _ => { Phase(); areas++; };
            PhysicsServer.AreaSetMonitorCallback(rawArea, (_, _, _, _, _) => { Phase(); areas++; });
            if (backend is null)
            {
                Check(solver is not null, "The registered implementation is actually initialized");
                using var unbound = new Solver(); Reject<InvalidOperationException>(() => _ = unbound.IsFlushingQueries);
                nestedSpace = PhysicsServer.SpaceCreate("NotificationPhase"); nested = factory!; PhysicsServer.SpaceSetActive(nestedSpace, true);
                nested.StepHook = () => Check(!nested.IsFlushingQueries && PhysicsServer.SpaceIsFlushingQueries(space), "Other-world solve keeps independent delivery status");
                nested.FlushHook = () => Check(nested.IsFlushingQueries && solver!.IsFlushingQueries, "Nested other-world flushing retains both active scopes");
                solver!.StepHook = () => Check(!solver.IsFlushingQueries && !PhysicsServer.SpaceIsFlushingQueries(space), "Numerical solve is outside notification delivery");
                solver.FlushHook = () =>
                {
                    Phase(); flushes++;
                    Check(world.DirectSpaceState.IntersectRay(nestedRay) is null && solver.IsFlushingQueries,
                        "Nested ordinary queries preserve ongoing notification status");
                    if (nest) PhysicsServer.SpaceStep(nestedSpace, 1d / 120);
                };
                solver.SyncHook = () => Check(!solver.IsFlushingQueries && !PhysicsServer.SpaceIsFlushingQueries(space), "End synchronization runs after delivery");
                nest = true;
            }
            Tick(0); Check(force >= 2 && script > 0 && sync >= 2 && contacts > 0 && areas > 0, "Real scene/raw body, contact and Area callbacks execute");
            for (var i = 0; i < 128; i++) Tick(i);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Tick(i);
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
            Check(ownerBytes == 0 && allBytes == 0, "Warm status reads, notifications and full frames allocate no managed memory");
            var beforeSync = sync; throwBody = true;
            Reject<AggregateException>(() => Tick(0)); throwBody = false;
            Check(sync >= beforeSync + 2 && !PhysicsServer.SpaceIsFlushingQueries(space), "Body failure preserves later observers and clears delivery status");
            Tick(1); throwArea = true; Reject<AggregateException>(() => Tick(0)); throwArea = false;
            Check(!PhysicsServer.SpaceIsFlushingQueries(space), "Area failure clears delivery status"); Tick(1);
            PhysicsServer.BodySetForceIntegrationCallback(body.GetRID(), null);
            body.CanSleep = true;
            if (solver is not null)
            {
                var regular = solver.StepHook;
                solver.StepHook = () => { regular?.Invoke(); solver.Bodies[body.GetRID()].Awake = false; };
                tree.PhysicsFrame(1d / 120);
                solver.StepHook = () => { regular?.Invoke(); solver.Bodies[body.GetRID()].Awake = true; };
                tree.PhysicsFrame(1d / 120); solver.StepHook = regular;
            }
            else
            {
                PhysicsServer.SpaceSetBodyTimeToSleep(space, .02f);
                for (var i = 0; i < 8; i++) tree.PhysicsFrame(1d / 120);
                PhysicsServer.SpaceSetBodyTimeToSleep(space, .25f);
                tree.PhysicsFrame(1d / 120);
            }
            Check(sleeps >= 2 && !PhysicsServer.SpaceIsFlushingQueries(space), "Sleep/wake delivery uses the completed notification phase");
            var foreignThread = new Thread(() =>
            {
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceIsFlushingQueries(space));
                if (solver is not null) Reject<InvalidOperationException>(() => _ = solver.IsFlushingQueries);
            }); foreignThread.Start(); foreignThread.Join();
            Reject<ArgumentException>(() => PhysicsServer.SpaceIsFlushingQueries(raw));
            Reject<ArgumentException>(() => PhysicsServer.SpaceIsFlushingQueries(default));
            if (solver is not null)
            {
                var regular = solver.FlushHook;
                solver.FlushHook = () => { Phase(); throw new InvalidOperationException("Selected flush failure"); };
                Reject<AggregateException>(() => Tick(0));
                Check(!solver.IsFlushingQueries, "Selected flush failure clears the same shared flag"); solver.FlushHook = regular; Tick(1);
                nest = false;
            }
            Console.WriteLine($"Notification phase backend={world.PhysicsBackend} frames=64 bytes={ownerBytes}/{allBytes} force={force} script={script} sync={sync} contacts={contacts} areas={areas} sleeps={sleeps} flushes={flushes}.");
        }
        finally
        {
            if (solver is not null) { solver.StepHook = null; solver.FlushHook = null; solver.SyncHook = null; }
            body.IntegrationObserver = null;
            PhysicsServer.BodySetForceIntegrationCallback(body.GetRID(), null); PhysicsServer.BodySetStateSyncCallback(body.GetRID(), null);
            PhysicsServer.BodySetForceIntegrationCallback(raw, null); PhysicsServer.BodySetStateSyncCallback(raw, null);
            PhysicsServer.FreeRID(rawArea); PhysicsServer.FreeRID(raw);
            if (nestedSpace.IsValid()) PhysicsServer.FreeRID(nestedSpace);
        }
        if (solver is not null)
        {
            tree.Dispose(); world.Dispose();
            Reject<ObjectDisposedException>(() => _ = solver.IsFlushingQueries);
            Reject<ArgumentException>(() => PhysicsServer.SpaceIsFlushingQueries(space));
            var failedSpace = PhysicsServer.SpaceCreate("NotificationPhase"); var failed = factory!; PhysicsServer.SpaceSetActive(failedSpace, true);
            failed.FailSolve = true; var terminalFlushes = 0;
            failed.FlushHook = () => { Check(failed.IsFlushingQueries && PhysicsServer.SpaceIsFlushingQueries(failedSpace), "Terminal failed solve retains notification status for cleanup"); terminalFlushes++; };
            try
            {
                Reject<AggregateException>(() => PhysicsServer.SpaceStep(failedSpace, 1d / 120));
                Check(terminalFlushes == 1 && !failed.IsFlushingQueries && !PhysicsServer.SpaceIsFlushingQueries(failedSpace), "Terminal completion clears status without hiding failure");
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(failedSpace, 1d / 120));
            }
            finally { failed.FlushHook = null; PhysicsServer.FreeRID(failedSpace); }
        }
        void Phase()
        {
            Check(PhysicsServer.SpaceIsFlushingQueries(space) && (solver is null || solver.IsFlushingQueries), "Every observer sees the same active world delivery flag");
            if (guardReentry)
            {
                guardReentry = false;
                Reject<InvalidOperationException>(() => tree.PhysicsFrame(1d / 120));
                Check(PhysicsServer.SpaceIsFlushingQueries(space), "Rejected reentry preserves the active delivery phase");
            }
        }
        void Tick(int i)
        {
            Check(!PhysicsServer.SpaceIsFlushingQueries(space) && (solver is null || !solver.IsFlushingQueries), "Delivery status is false before stepping");
            body.GlobalPosition = (i & 1) == 0 ? Vector2.Zero : new(60, 0);
            PhysicsServer.BodySetTransform(raw, new(0, (i & 1) == 0 ? new Vector2(0, 60) : new Vector2(60, 60)));
            tree.PhysicsFrame(1d / 120);
            Check(!PhysicsServer.SpaceIsFlushingQueries(space) && (solver is null || !solver.IsFlushingQueries), "Delivery status clears after each completed frame");
        }
    }

    private sealed class NotificationBody : RigidBody
    {
        internal Action? IntegrationObserver;
        protected override void IntegrateForces(PhysicsDirectBodyState state) => IntegrationObserver?.Invoke();
    }
}
