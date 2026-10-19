using Electron2D;

public static partial class BackendChecks
{
    public static void RunAreaMonitors(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { AreaMonitorChecks(backend, threaded); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void AreaMonitorChecks(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("AreaMonitors", () => solver = new());
        using var world = backend is { } selected ? new World(selected) : new World("AreaMonitors");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 3 };
        using var identity = new Node();
        var parent = new Entity(); root.AddChild(parent);
        var scene = new Area(); scene.AddChild(new CollisionShape { Shape = circle }); parent.AddChild(scene);
        var body = new StaticBody(); body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.AreaCreate(); var peer = PhysicsServer.AreaCreate(); var rawBody = PhysicsServer.BodyCreate();
        var bodyAdds = 0; var bodyRemoves = 0; var areaAdds = 0; var areaRemoves = 0;
        var sceneBodies = 0; var sceneAreas = 0;
        var ownerThread = Environment.CurrentManagedThreadId; var numericalReads = 0;
        var scenePose = Transform.Identity; var rawPose = Transform.Identity;
        var sceneID = scene.GetRID(); var probeWrites = true;
        RID[] areaIDs = [sceneID, raw, peer];
        Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int> bodyCallback = (status, rid, id, remote, local) =>
        {
            Check((rid == body.GetRID() && id == body.InstanceID || rid == rawBody && id == identity.InstanceID) && remote == 0 && local == 0,
                "Body callbacks preserve RID, assigned instance and logical slot roles");
            if (status == PhysicsServer.AreaBodyStatus.Added) bodyAdds++; else bodyRemoves++;
        };
        Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int> areaCallback = (status, rid, id, remote, local) =>
        {
            Check((rid == scene.GetRID() && id == scene.InstanceID || rid == peer && id == 0) && remote == 0 && local == 0,
                "Area callbacks preserve scene and unassigned raw identities");
            if (status == PhysicsServer.AreaBodyStatus.Added) areaAdds++; else areaRemoves++;
        };
        try
        {
            foreach (var id in new[] { raw, peer }) PhysicsServer.AreaAddShape(id, circle.GetRID());
            PhysicsServer.BodyAddShape(rawBody, circle.GetRID()); PhysicsServer.BodySetMode(rawBody, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAttachObject(rawBody, identity); PhysicsServer.BodySetCollisionMask(rawBody, 0);
            PhysicsServer.BodySetSpace(rawBody, world.Space); PhysicsServer.AreaSetSpace(peer, world.Space);
            var before = new Transform(.2f, new Vector2(10, 0)); PhysicsServer.AreaSetTransform(raw, before);
            Check(PhysicsServer.AreaGetTransform(raw) == before, "Detached Area pose retains full transform");
            foreach (var invalid in new[] { new Transform(0, new Vector2(2, 1), 0, Vector2.Zero),
                new Transform(float.NaN, Vector2.Zero), new Transform(0, Vector2.One, .2f, Vector2.Zero) })
                Reject<ArgumentException>(() => PhysicsServer.AreaSetTransform(raw, invalid));
            Check(PhysicsServer.AreaGetTransform(raw) == before, "Invalid poses preserve authored state");
            PhysicsServer.AreaSetMonitorable(raw, true);
            if (solver is not null) solver.IdentityHook = _ =>
            {
                Reject<InvalidOperationException>(() => scene.Monitoring = !scene.Monitoring);
                Reject<InvalidOperationException>(() => scene.Monitorable = !scene.Monitorable);
                Reject<InvalidOperationException>(() => scene.Monitoring = scene.Monitoring);
                Reject<InvalidOperationException>(() => scene.Monitorable = scene.Monitorable);
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetTransform(raw, Transform.Identity));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetMonitorable(raw, false));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetMonitorCallback(raw, null));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetAreaMonitorCallback(raw, null));
                Check(PhysicsServer.AreaGetTransform(raw) == before && scene.Monitoring && scene.Monitorable,
                    "Rejected numerical authoring preserves scene/raw values");
            };
            PhysicsServer.AreaSetSpace(raw, world.Space);
            if (solver is not null) solver.IdentityHook = null;
            PhysicsServer.AreaSetTransform(raw, Transform.Identity);
            PhysicsServer.AreaSetMonitorCallback(raw, bodyCallback); PhysicsServer.AreaSetAreaMonitorCallback(raw, areaCallback);
            PhysicsServer.AreaSetMonitorCallback(scene.GetRID(), (status, _, _, _, _) => { if (status == PhysicsServer.AreaBodyStatus.Added) sceneBodies++; });
            PhysicsServer.AreaSetAreaMonitorCallback(scene.GetRID(), (status, _, _, _, _) =>
            {
                if (status != PhysicsServer.AreaBodyStatus.Added) return;
                sceneAreas++;
                Reject<InvalidOperationException>(() => scene.Monitoring = !scene.Monitoring);
                Reject<InvalidOperationException>(() => scene.Monitorable = !scene.Monitorable);
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetTransform(scene.GetRID(), Transform.Identity));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetMonitorCallback(scene.GetRID(), null));
                scene.Monitoring = scene.Monitoring; scene.Monitorable = scene.Monitorable;
                Check(PhysicsServer.AreaGetTransform(scene.GetRID()) == Transform.Identity, "Callback pose reads remain available");
            });
            tree.PhysicsFrame(0); Check(bodyAdds == 0 && areaAdds == 0, "Zero step does not publish transitions");
            Check(world.PhysicsBackendName == (backend?.ToString() ?? "AreaMonitors"), "Area monitoring uses the requested actual implementation");
            if (solver is not null) solver.StepHook = () =>
            {
                Check((Environment.CurrentManagedThreadId != ownerThread) == threaded, "Area pose reads execute on the captured numerical lane");
                ReadPoses(); numericalReads++;
                if (!probeWrites) return;
                foreach (var id in areaIDs)
                {
                    Reject<InvalidOperationException>(() => PhysicsServer.AreaSetTransform(id, PhysicsServer.AreaGetTransform(id)));
                    Reject<InvalidOperationException>(() => PhysicsServer.AreaSetTransform(id, new(.7f, Vector2.One)));
                }
                Reject<InvalidOperationException>(() => PhysicsServer.BodyGetTransform(body.GetRID()));
                if (threaded) Reject<InvalidOperationException>(() => { _ = scene.GlobalTransform; });
                probeWrites = false;
            };
            tree.PhysicsFrame(1d / 60);
            if (solver is not null)
            {
                Check(numericalReads == 1, "The selected numerical hook actually reads all three configured Area poses");
                solver.StatusHook = ReadPoses;
                _ = PhysicsServer.AreaGetTransform(raw); solver.StatusHook = null;
            }
            Check(bodyAdds == 2 && areaAdds == 1 && sceneBodies == 2 && sceneAreas == 1 && scene.GetOverlappingBodies().Contains(body),
                "Selected owner publishes directional body pairs, default raw monitorability and mandatory scene snapshots");
            PhysicsServer.AreaSetMonitorable(peer, true); tree.PhysicsFrame(1d / 60);
            Check(areaAdds == 2 && sceneAreas == 2, "Live raw monitorability enters the peer once");
            scene.Monitoring = false; tree.PhysicsFrame(1d / 60);
            Check(scene.GetOverlappingBodies().Length == 0 && sceneBodies == 2 && sceneAreas == 2,
                "Raw observers retain history while scene snapshots stop monitoring");
            PhysicsServer.AreaSetMonitorCallback(raw, bodyCallback); tree.PhysicsFrame(1d / 60);
            Check(bodyAdds == 4 && areaAdds == 4 && bodyRemoves == 0 && areaRemoves == 0,
                "Registering the same callback resets both pair histories without synthetic exits");
            var adds = bodyAdds + areaAdds;
            PhysicsServer.AreaSetSpace(raw, default);
            Check(bodyAdds + areaAdds == adds && bodyRemoves + areaRemoves == 0, "Receiver detach clears its own history silently");
            PhysicsServer.AreaSetSpace(raw, world.Space); tree.PhysicsFrame(1d / 60);
            Check(bodyAdds == 6 && areaAdds == 6, "Reentry retains callbacks, identity and authored monitoring");
            PhysicsServer.AreaSetMonitorCallback(scene.GetRID(), null); PhysicsServer.AreaSetAreaMonitorCallback(scene.GetRID(), null);
            var failures = 0;
            Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int> fail = (_, _, _, _, _) => { failures++; throw new ApplicationException("Area observer failure"); };
            PhysicsServer.AreaSetMonitorCallback(raw, fail); PhysicsServer.AreaSetAreaMonitorCallback(raw, fail);
            Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
            Check(failures == 4, "All committed body and Area transitions survive observer errors");
            tree.PhysicsFrame(1d / 60); Check(failures == 4, "Observer errors do not replay committed transitions");
            PhysicsServer.AreaSetMonitorCallback(raw, bodyCallback); PhysicsServer.AreaSetAreaMonitorCallback(raw, areaCallback);
            tree.PhysicsFrame(1d / 60);
            foreach (var wrong in new[] { default, rawBody, circle.GetRID(), world.Space })
            {
                Reject<ArgumentException>(() => PhysicsServer.AreaGetTransform(wrong));
                Reject<ArgumentException>(() => PhysicsServer.AreaSetTransform(wrong, Transform.Identity));
                Reject<ArgumentException>(() => PhysicsServer.AreaSetMonitorable(wrong, true));
                Reject<ArgumentException>(() => PhysicsServer.AreaSetMonitorCallback(wrong, null));
                Reject<ArgumentException>(() => PhysicsServer.AreaSetAreaMonitorCallback(wrong, null));
            }
            var near = Transform.Identity; var far = new Transform(.2f, new Vector2(30, 0));
            for (var i = 0; i < 64; i++) EditAndStep(i);
            Collect(); GC.WaitForPendingFinalizers(); var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
            var entries = bodyAdds + areaAdds; var exits = bodyRemoves + areaRemoves;
            var beforeReads = numericalReads;
            for (var i = 0; i < 64; i++) EditAndStep(i);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(bodyAdds + areaAdds - entries == 128 && bodyRemoves + areaRemoves - exits == 128,
                "Warmed pose changes deliver actual body/Area entry and exit pairs");
            Check(owner == 0 && all == 0, $"Area monitoring complete-frame allocations: owner={owner}, all={all}");
            Check(solver is null || numericalReads == beforeReads + 64, "Every measured frame reads three prepared poses inside the actual selected Solve");
            Check(Task.Run(() =>
            {
                foreach (var id in areaIDs) Reject<InvalidOperationException>(() => PhysicsServer.AreaGetTransform(id));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetTransform(raw, Transform.Identity));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetMonitorable(raw, false));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetMonitorCallback(raw, null));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetAreaMonitorCallback(raw, null));
            }).Wait(TimeSpan.FromSeconds(2)), "Attached Area access rejects other threads");
            Console.WriteLine($"Area monitor family on {backend?.ToString() ?? "registered"}, separate={threaded} passed: 128 entries/128 exits, owner={owner}, all={all}.");
            if (solver is not null)
            {
                solver.StepHook = null; ClearCallbacks(); solver.FailSolve = true;
                Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
                foreach (var id in areaIDs) Reject<InvalidOperationException>(() => PhysicsServer.AreaGetTransform(id));
            }

            void EditAndStep(int i)
            {
                parent.Transform = new(.3f + (i & 1) * .1f, new(.1f, .2f));
                scene.Transform = new(-.1f, new(.2f, -.1f)); scenePose = scene.GlobalTransform;
                rawPose = i % 2 == 0 ? far : near; PhysicsServer.AreaSetTransform(raw, rawPose);
                tree.PhysicsFrame(1d / 60);
            }
        }
        finally
        {
            if (solver is not null) { solver.IdentityHook = null; solver.StepHook = null; solver.StatusHook = null; }
            if (solver?.FailSolve != true) ClearCallbacks();
            PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(peer); PhysicsServer.FreeRID(rawBody);
        }
        void ReadPoses()
        {
            foreach (var id in areaIDs)
            {
                var actual = PhysicsServer.AreaGetTransform(id);
                var expected = id == sceneID ? scenePose : id == raw ? rawPose : Transform.Identity;
                Check(actual.IsEqualApprox(expected) && actual.IsEqualApprox(solver!.Bodies[id].Pose), "Configured scene/raw Area pose agrees with the selected prepared global pose");
            }
        }
        void ClearCallbacks()
        {
            PhysicsServer.AreaSetMonitorCallback(scene.GetRID(), null); PhysicsServer.AreaSetAreaMonitorCallback(scene.GetRID(), null);
            PhysicsServer.AreaSetMonitorCallback(raw, null); PhysicsServer.AreaSetAreaMonitorCallback(raw, null);
        }
        Reject<ArgumentException>(() => PhysicsServer.AreaGetTransform(raw));
        Reject<ArgumentException>(() => PhysicsServer.AreaSetTransform(raw, Transform.Identity));
        Reject<ArgumentException>(() => PhysicsServer.AreaSetMonitorable(raw, true));
        Reject<ArgumentException>(() => PhysicsServer.AreaSetMonitorCallback(raw, null));
        Reject<ArgumentException>(() => PhysicsServer.AreaSetAreaMonitorCallback(raw, null));
    }
}
