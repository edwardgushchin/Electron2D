using Electron2D;

internal static class PhysicsAreaMonitorTests
{
    internal static void Run()
    {
        VerifyServerPairsAndFilters();
        VerifySceneObserversAndLifetime();
        VerifyResetAndCallbackFailures();
        VerifyRemovalDuringCallback();
        VerifyWarmTransitions();
        Console.WriteLine("Server Area monitoring, directional masks, pair IDs, lifetime and allocation checks passed.");
    }

    private static void VerifyServerPairsAndFilters()
    {
        var server = PhysicsServer.Service; using var tree = SceneSpace(out var space);
        var area = PhysicsServer.AreaCreate(); var body = PhysicsServer.BodyCreate(); var other = PhysicsServer.AreaCreate(); var shape = PhysicsServer.CircleShapeCreate();
        var events = new List<(PhysicsServer.AreaBodyStatus Status, RID Other, ulong ID, int Remote, int Local, bool Area)>();
        try
        {
            PhysicsServer.AreaAddShape(area, shape); PhysicsServer.AreaAddShape(area, shape, new(0, Vector2.One, 0, new(10, 0)));
            PhysicsServer.BodyAddShape(body, shape); PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.AreaAddShape(other, shape);
            PhysicsServer.AreaSetSpace(area, space); PhysicsServer.BodySetSpace(body, space); PhysicsServer.AreaSetSpace(other, space);
            PhysicsServer.AreaSetMonitorCallback(area, (status, rid, id, remote, local) => events.Add((status, rid, id, remote, local, false)));
            PhysicsServer.AreaSetAreaMonitorCallback(area, (status, rid, id, remote, local) => events.Add((status, rid, id, remote, local, true)));
            Check((int)PhysicsServer.AreaBodyStatus.Added == 0 && (int)PhysicsServer.AreaBodyStatus.Removed == 1 &&
                PhysicsServer.AreaGetCollisionLayer(area) == 1 && PhysicsServer.AreaGetCollisionMask(area) == 1 && PhysicsServer.AreaGetTransform(area) == Transform.Identity,
                "Status values, filter defaults and server pose getter.");
            tree.PhysicsFrame(1d / 60);
            Check(events.Count == 2 && events.All(e => e.Other == body && e.ID == 0 && !e.Area && e.Status == PhysicsServer.AreaBodyStatus.Added) &&
                events.Select(e => e.Local).Order().SequenceEqual(new[] { 0, 1 }), "Server body overlaps preserve both local logical pairs and zero instance ID.");
            events.Clear(); PhysicsServer.AreaSetMonitorable(other, true); tree.PhysicsFrame(1d / 60);
            Check(events.Count == 2 && events.All(e => e.Area && e.Other == other), "Server Areas default non-monitorable and opt into area callback lane.");
            events.Clear(); PhysicsServer.AreaSetCollisionMask(area, 0); tree.PhysicsFrame(1d / 60);
            Check(events.Count == 4 && events.All(e => e.Status == PhysicsServer.AreaBodyStatus.Removed), "Directional receiver mask removal emits retained body/Area pair exits.");
            events.Clear(); PhysicsServer.AreaSetCollisionLayer(other, 1u << 31); PhysicsServer.AreaSetCollisionMask(area, 1u << 31); tree.PhysicsFrame(1d / 60);
            Check(events.Count == 2 && events.All(e => e.Area && e.Other == other), "Bit 32 filters by other layer, not reciprocal masks.");
            PhysicsServer.AreaSetSpace(area, default); events.Clear(); PhysicsServer.AreaSetSpace(area, space); tree.PhysicsFrame(1d / 60);
            Check(events.Count == 2 && events.All(e => e.Status == PhysicsServer.AreaBodyStatus.Added), "Reentry clears receiver history and retains callback registration.");
            events.Clear(); PhysicsServer.FreeRID(other); other = default;
            Check(events.Count == 2 && events.All(e => e.Status == PhysicsServer.AreaBodyStatus.Removed), "Other Area free synchronously emits pair departure.");
        }
        finally
        {
            PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(body); if (other.IsValid()) PhysicsServer.FreeRID(other); PhysicsServer.FreeRID(shape);
        }
    }

    private static void VerifySceneObserversAndLifetime()
    {
        using var circle = new CircleShape(); var root = new Node(); var sceneArea = new Area { Name = "receiver" };
        var body = new StaticBody { Name = "body" }; Add(sceneArea, circle); Add(body, circle); root.AddChild(sceneArea); root.AddChild(body);
        using var tree = new SceneTree(root); var server = PhysicsServer.Service;
        var bodyRID = body.GetRID(); var bodyID = body.InstanceID;
        var callbackEntries = 0; var callbackExits = 0; var typedEntries = 0;
        sceneArea.BodyEntered += _ => typedEntries++;
        PhysicsServer.AreaSetMonitorCallback(sceneArea.GetRID(), (status, rid, id, remote, local) =>
        {
            Check(rid == bodyRID && id == bodyID && remote == 0 && local == 0, "Scene observer payload keeps RID/object ID/shape role.");
            if (status == PhysicsServer.AreaBodyStatus.Added) callbackEntries++; else callbackExits++;
        });
        tree.PhysicsFrame(1d / 60);
        Check(callbackEntries == 1 && typedEntries == 1 && sceneArea.GetOverlappingBodies().Contains(body),
            "External observers preserve scene-owned snapshots and object events.");
        root.RemoveChild(body); Check(callbackExits == 1, "Scene body departure reaches raw callback before another step.");
        root.AddChild(body); tree.PhysicsFrame(1d / 60); Check(callbackEntries == 2, "Scene reentry can enter again with stable identity.");
        PhysicsServer.AreaSetCollisionMask(sceneArea.GetRID(), 0); tree.PhysicsFrame(1d / 60);
        Check(sceneArea.CollisionMask == 0 && callbackExits == 2, "Scene filter projection has actual overlap effect.");
        PhysicsServer.AreaSetTransform(sceneArea.GetRID(), new(0.2f, Vector2.One, 0, new(40, 0)));
        Check(PhysicsServer.AreaGetTransform(sceneArea.GetRID()).Origin.IsEqualApprox(new(40, 0)), "Scene global pose getter/setter projection.");
        Check(Task.Run(() => Capture(() => PhysicsServer.AreaSetCollisionMask(sceneArea.GetRID(), 1))).Result is InvalidOperationException,
            "Attached receiver writes enforce owner thread.");
    }

    private static void VerifyResetAndCallbackFailures()
    {
        var server = PhysicsServer.Service; using var tree = SceneSpace(out var space); var area = PhysicsServer.AreaCreate();
        var body = PhysicsServer.BodyCreate(); var shape = PhysicsServer.CircleShapeCreate(); var added = 0; var removed = 0;
        try
        {
            PhysicsServer.AreaAddShape(area, shape); PhysicsServer.AreaAddShape(area, shape, new(0, Vector2.One, 0, new(10, 0)));
            PhysicsServer.BodyAddShape(body, shape); PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
            PhysicsServer.AreaSetSpace(area, space); PhysicsServer.BodySetSpace(body, space);
            Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int> observer = (status, _, _, _, _) => { if (status == PhysicsServer.AreaBodyStatus.Added) added++; else removed++; };
            PhysicsServer.AreaSetMonitorCallback(area, observer); tree.PhysicsFrame(1d / 60); Check(added == 2, "Initial callback enters all retained pairs.");
            PhysicsServer.AreaSetMonitorCallback(area, observer); tree.PhysicsFrame(1d / 60); Check(added == 4, "Re-registering even the same callback resets history.");
            PhysicsServer.AreaSetMonitorCallback(area, null); tree.PhysicsFrame(1d / 60); Check(added == 4 && removed == 0, "Clearing callback resets without synthetic exits.");
            var delivered = 0; var rejected = false;
            PhysicsServer.AreaSetMonitorCallback(area, (status, _, _, _, _) =>
            {
                delivered++;
                rejected = Capture(() => PhysicsServer.AreaSetCollisionMask(area, 0)) is InvalidOperationException &&
                    Capture(() => PhysicsServer.AreaSetMonitorCallback(area, null)) is InvalidOperationException;
                throw new ApplicationException("monitor");
            });
            Check(Capture(() => tree.PhysicsFrame(1d / 60)) is AggregateException && delivered == 2 && rejected,
                "Snapshots commit before callbacks; failures continue later pairs and reject recursive receiver config mutation.");
            tree.PhysicsFrame(1d / 60); Check(delivered == 2, "Committed callback failures do not replay transitions.");
            PhysicsServer.AreaSetMonitorCallback(area, (status, _, _, _, _) => { if (status == PhysicsServer.AreaBodyStatus.Removed) throw new ApplicationException("exit"); });
            Check(Capture(() => tree.PhysicsFrame(1d / 60)) is null, "Replacement observer sees fresh entry.");
            Check(Capture(() => PhysicsServer.FreeRID(body)) is AggregateException, "Free reports callback departure failures."); body = default;
            var bodyReplacement = PhysicsServer.BodyCreate(); PhysicsServer.FreeRID(bodyReplacement);
            PhysicsServer.AreaSetMonitorCallback(area, null);
            Reject<ArgumentException>(() => PhysicsServer.AreaGetCollisionMask(default));
            Reject<ArgumentException>(() => PhysicsServer.AreaSetMonitorCallback(shape, observer));
        }
        finally { if (body.IsValid()) PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(shape); }
    }

    private static void VerifyRemovalDuringCallback()
    {
        var server = PhysicsServer.Service; using var tree = SceneSpace(out var space);
        var area = PhysicsServer.AreaCreate(); var body = PhysicsServer.BodyCreate(); var shape = PhysicsServer.CircleShapeCreate();
        var entries = 0; var exits = 0; var freed = false;
        try
        {
            PhysicsServer.AreaAddShape(area, shape); PhysicsServer.AreaAddShape(area, shape, new(0, Vector2.One, 0, new(10, 0)));
            PhysicsServer.BodyAddShape(body, shape); PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
            PhysicsServer.AreaSetSpace(area, space); PhysicsServer.BodySetSpace(body, space);
            PhysicsServer.AreaSetMonitorCallback(area, (status, _, _, _, _) =>
            {
                if (status == PhysicsServer.AreaBodyStatus.Added)
                {
                    entries++; PhysicsServer.FreeRID(body); freed = true;
                }
                else exits++;
            });
            tree.PhysicsFrame(1d / 60);
            Check(entries == 1 && exits == 2, "Removal from a callback suppresses stale later entries and drains retained pair departures safely.");
        }
        finally { if (!freed) PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(shape); }
    }

    private static void VerifyWarmTransitions()
    {
        var server = PhysicsServer.Service; using var tree = SceneSpace(out var space); var area = PhysicsServer.AreaCreate(); var body = PhysicsServer.BodyCreate(); var shape = PhysicsServer.CircleShapeCreate();
        var entries = 0; var exits = 0;
        try
        {
            PhysicsServer.AreaAddShape(area, shape); PhysicsServer.BodyAddShape(body, shape); PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
            PhysicsServer.AreaSetSpace(area, space); PhysicsServer.BodySetSpace(body, space);
            PhysicsServer.AreaSetMonitorCallback(area, (status, _, _, _, _) => { if (status == PhysicsServer.AreaBodyStatus.Added) entries++; else exits++; });
            var inside = Transform.Identity; var outside = new Transform(0, Vector2.One, 0, new(100, 0));
            for (var pass = 0; pass < 64; pass++) Transition();
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var pass = 0; pass < 64; pass++) Transition();
            Check(GC.GetAllocatedBytesForCurrentThread() == before && entries >= 128 && exits >= 128,
                "Warmed active body pair entry/exit scans and callback delivery allocate zero managed bytes.");
            void Transition() { PhysicsServer.BodySetTransform(body, inside); tree.PhysicsFrame(1d / 60); PhysicsServer.BodySetTransform(body, outside); tree.PhysicsFrame(1d / 60); }
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(shape); }
    }

    private static SceneTree SceneSpace(out RID space)
    {
        var root = new Node(); var holder = new Area { CollisionLayer = 0, CollisionMask = 0, Monitoring = false, Monitorable = false };
        root.AddChild(holder); var tree = new SceneTree(root); space = holder.GetWorld()!.Space; return tree;
    }

    private static void Add(CollisionObject collider, Shape shape) => collider.ShapeOwnerAddShape(collider.CreateShapeOwner(null), shape);
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
