using Electron2D;

public static partial class BackendChecks
{
    public static void RunAreaMonitors(PhysicsServer.Backend? backend = null) => AreaMonitorChecks(backend);

    private static void AreaMonitorChecks(PhysicsServer.Backend? backend = null)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("AreaMonitors", () => solver = new());
        using var world = backend is { } selected ? new World(selected) : new World("AreaMonitors");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 3 };
        using var identity = new Node();
        var scene = new Area(); scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        var body = new StaticBody(); body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.AreaCreate(); var peer = PhysicsServer.AreaCreate(); var rawBody = PhysicsServer.BodyCreate();
        var bodyAdds = 0; var bodyRemoves = 0; var areaAdds = 0; var areaRemoves = 0;
        var sceneBodies = 0; var sceneAreas = 0;
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
            tree.PhysicsFrame(1d / 60);
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
            for (var i = 0; i < 64; i++) { PhysicsServer.AreaSetTransform(raw, i % 2 == 0 ? far : near); tree.PhysicsFrame(1d / 60); }
            Collect(); GC.WaitForPendingFinalizers(); var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
            var entries = bodyAdds + areaAdds; var exits = bodyRemoves + areaRemoves;
            for (var i = 0; i < 64; i++) { PhysicsServer.AreaSetTransform(raw, i % 2 == 0 ? far : near); tree.PhysicsFrame(1d / 60); }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(bodyAdds + areaAdds - entries == 128 && bodyRemoves + areaRemoves - exits == 128,
                "Warmed pose changes deliver actual body/Area entry and exit pairs");
            Check(owner == 0 && all == 0, $"Area monitoring complete-frame allocations: owner={owner}, all={all}");
            Check(Task.Run(() =>
            {
                Reject<InvalidOperationException>(() => PhysicsServer.AreaGetTransform(raw));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetTransform(raw, Transform.Identity));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetMonitorable(raw, false));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetMonitorCallback(raw, null));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetAreaMonitorCallback(raw, null));
            }).Wait(TimeSpan.FromSeconds(2)), "Attached Area access rejects other threads");
            Console.WriteLine($"Area monitor family on {backend?.ToString() ?? "registered"} passed: 128 entries/128 exits, owner={owner}, all={all}.");
        }
        finally
        {
            if (solver is not null) solver.IdentityHook = null;
            PhysicsServer.AreaSetMonitorCallback(scene.GetRID(), null); PhysicsServer.AreaSetAreaMonitorCallback(scene.GetRID(), null);
            PhysicsServer.AreaSetMonitorCallback(raw, null); PhysicsServer.AreaSetAreaMonitorCallback(raw, null);
            PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(peer); PhysicsServer.FreeRID(rawBody);
        }
        Reject<ArgumentException>(() => PhysicsServer.AreaGetTransform(raw));
        Reject<ArgumentException>(() => PhysicsServer.AreaSetTransform(raw, Transform.Identity));
        Reject<ArgumentException>(() => PhysicsServer.AreaSetMonitorable(raw, true));
        Reject<ArgumentException>(() => PhysicsServer.AreaSetMonitorCallback(raw, null));
        Reject<ArgumentException>(() => PhysicsServer.AreaSetAreaMonitorCallback(raw, null));
    }
}
