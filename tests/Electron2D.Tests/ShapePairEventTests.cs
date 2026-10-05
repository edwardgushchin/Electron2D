using Electron2D;

internal static class ShapePairEventTests
{
    internal static void Run()
    {
        VerifyAreaBodyPairs();
        VerifyAreaPairsAndServerPayload();
        VerifyRigidPairs();
        VerifyCompoundAndFailure();
        VerifyWarmAllocation();
        Console.WriteLine("Area/body shape-pair events, ordering, identities and allocation checks passed.");
    }

    private static uint Add(CollisionObject obj, Shape shape, float x = 0)
    {
        var owner = obj.CreateShapeOwner(null); obj.ShapeOwnerAddShape(owner, shape);
        obj.ShapeOwnerSetTransform(owner, new(0, Vector2.One, 0, new(x, 0))); return owner;
    }

    private static void VerifyAreaBodyPairs()
    {
        using var sensor = new RectangleShape { Size = new(60, 60) };
        using var circle = new CircleShape { Radius = 5 };
        var root = new Node(); var area = new Area { Name = "Area" }; var body = new StaticBody { Name = "Body", CollisionMask = 0 };
        Add(area, sensor); Add(area, sensor, 2); var first = Add(body, circle); var second = Add(body, circle, 10);
        root.AddChild(area); root.AddChild(body);
        using var tree = new SceneTree(root);
        var events = new List<string>(); var pairs = new HashSet<(int, int)>();
        area.BodyEntered += _ => events.Add("object+"); area.BodyExited += _ => events.Add("object-");
        area.BodyShapeEntered += (rid, node, remote, local) =>
        {
            Check(rid == body.GetRID() && ReferenceEquals(node, body), "Area shape payload retains scene object and RID.");
            Check(body.ShapeFindOwner(remote) == first || body.ShapeFindOwner(remote) == second, "Other shape index is a global owner slot.");
            pairs.Add((remote, local)); events.Add("shape+");
        };
        area.BodyShapeExited += (_, _, remote, local) => { pairs.Remove((remote, local)); events.Add("shape-"); };
        tree.PhysicsFrame(1d / 60);
        Check(events.SequenceEqual(new[] { "object+", "shape+", "shape+", "shape+", "shape+" }) && pairs.Count == 4,
            "One object entry precedes four distinct logical shape pairs despite reciprocal mask zero.");
        events.Clear(); tree.PhysicsFrame(1d / 60); Check(events.Count == 0, "Unchanged pairs do not replay transitions.");
        body.ShapeOwnerSetTransform(second, new(0, Vector2.One, 0, new(200, 0))); tree.PhysicsFrame(1d / 60);
        Check(events.SequenceEqual(new[] { "shape-", "shape-" }) && area.OverlapsBody(body),
            "Leaving two pairs retains object overlap while another shape remains.");
        events.Clear(); root.RemoveChild(body);
        Check(events.SequenceEqual(new[] { "object-", "shape-", "shape-" }) && pairs.Count == 0,
            "Tree departure reports object exit before retained pair exits without another step.");
        body.Dispose();
    }

    private static void VerifyAreaPairsAndServerPayload()
    {
        using var box = new RectangleShape { Size = new(30, 30) };
        var root = new Node(); var area = new Area { Name = "Observer" }; var other = new Area { Name = "Other" };
        Add(area, box); Add(other, box); root.AddChild(area); root.AddChild(other);
        using var tree = new SceneTree(root);
        var sequence = new List<string>(); var entered = 0; var exited = 0;
        area.AreaEntered += _ => sequence.Add("object+"); area.AreaExited += _ => sequence.Add("object-");
        Action<RID, Area?, int, int> sceneVerifier = (rid, node, remote, local) =>
        { Check(rid == other.GetRID() && ReferenceEquals(node, other) && remote == 0 && local == 0, "Area/Area payload ordering."); sequence.Add("shape+"); };
        area.AreaShapeEntered += sceneVerifier;
        area.AreaShapeExited += (_, _, _, _) => sequence.Add("shape-");
        tree.PhysicsFrame(1d / 60); Check(sequence.SequenceEqual(new[] { "object+", "shape+" }), "Area object entry precedes pair entry.");
        sequence.Clear(); other.Monitorable = false; tree.PhysicsFrame(1d / 60);
        Check(sequence.SequenceEqual(new[] { "object-", "shape-" }), "Monitorable policy generates pair/object departures.");
        var server = PhysicsServer.Service; var body = PhysicsServer.BodyCreate(); var shape = PhysicsServer.RectangleShapeCreate();
        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(body, shape); PhysicsServer.BodySetSpace(body, area.GetWorld()!.Space);
        area.BodyShapeEntered += (rid, node, remote, local) =>
        { Check(rid == body && node is null && remote == 0 && local == 0, "Server body payload uses RID and null scene object."); entered++; };
        area.BodyShapeExited += (rid, node, _, _) => { Check(rid == body && node is null, "Server body departure payload."); exited++; };
        tree.PhysicsFrame(1d / 60); Check(entered == 1 && !area.HasOverlappingBodies(), "Server shape events do not fabricate scene object snapshots.");
        Action<RID, Entity?, int, int> failedExit = (_, _, _, _) => throw new InvalidOperationException("User departure handler failed.");
        area.BodyShapeExited += failedExit;
        Check(Capture(() => PhysicsServer.FreeRID(body)) is AggregateException && exited == 1,
            "Server free emits retained exit values despite a failed departure handler.");
        area.BodyShapeExited -= failedExit;
        Reject<ArgumentException>(() => PhysicsServer.BodyGetDirectState(body));
        var serverArea = PhysicsServer.AreaCreate(); PhysicsServer.AreaAddShape(serverArea, shape); PhysicsServer.AreaSetSpace(serverArea, area.GetWorld()!.Space);
        var serverAreaEntries = 0;
        area.AreaShapeEntered += (rid, node, _, _) => { if (rid == serverArea) { Check(node is null, "Server Area payload is nullable."); serverAreaEntries++; } };
        tree.PhysicsFrame(1d / 60); Check(serverAreaEntries == 0, "Server-created Areas default non-monitorable.");
        area.AreaShapeEntered -= sceneVerifier;
        root.RemoveChild(other); other.Dispose();
        PhysicsServer.AreaSetMonitorable(serverArea, true);
        tree.PhysicsFrame(1d / 60);
        Check(serverAreaEntries == 1, "A monitorable server Area emits its nullable RID pair payload.");
        PhysicsServer.FreeRID(serverArea); PhysicsServer.FreeRID(shape);
    }

    private static void VerifyRigidPairs()
    {
        using var circle = new CircleShape { Radius = 10 };
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        var root = new Node(); var body = new RigidBody { Name = "Body", MaxContactsReported = 8, ContactMonitor = true, CanSleep = false };
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100) };
        var left = Add(body, circle, -15); var right = Add(body, circle, 15); Add(floor, floorShape);
        root.AddChild(body); root.AddChild(floor); using var tree = new SceneTree(root);
        var objects = 0; var shapeEntries = 0; var shapeExits = 0; var locals = new HashSet<int>();
        body.BodyEntered += _ => objects++;
        body.BodyShapeEntered += (rid, node, remote, local) =>
        {
            Check(objects == 1 && rid == floor.GetRID() && ReferenceEquals(node, floor) && remote == 0, "Rigid pair entry follows object entry.");
            Check(body.ShapeFindOwner(local) == left || body.ShapeFindOwner(local) == right, "Rigid local global index resolves to its owner.");
            locals.Add(local); shapeEntries++;
        };
        body.BodyShapeExited += (_, _, _, _) => shapeExits++;
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(objects == 1 && shapeEntries == 2 && locals.SetEquals(new[] { 0, 1 }), "Two touching logical shapes emit two entries, with one object entry.");
        body.ShapeOwnerSetDisabled(right, true); tree.PhysicsFrame(1d / 60);
        Check(shapeExits == 1 && body.GetCollidingBodies().Contains(floor), "One pair exit does not drop the remaining monitored object.");
        root.RemoveChild(floor); Check(shapeExits == 2, "Body departure emits the remaining rigid pair exit."); floor.Dispose();
    }

    private static void VerifyCompoundAndFailure()
    {
        using var polygon = new ConvexPolygonShape
        { Points = Enumerable.Range(0, 16).Select(i => Vector2.FromAngle(i * Mathf.Tau / 16) * 30).ToArray() };
        using var sensor = new RectangleShape { Size = new(100, 100) };
        var root = new Node(); var area = new Area { Name = "Area" }; var body = new StaticBody { Name = "Body" };
        Add(area, sensor); Add(body, polygon); root.AddChild(area); root.AddChild(body); using var tree = new SceneTree(root);
        var entries = 0;
        area.BodyEntered += _ => throw new InvalidOperationException("User callback failed.");
        area.BodyShapeEntered += (_, _, _, _) =>
        {
            entries++;
            Reject<InvalidOperationException>(() => area.Monitoring = false);
            Reject<InvalidOperationException>(() => area.Monitorable = false);
        };
        Check(Capture(() => tree.PhysicsFrame(1d / 60)) is AggregateException && entries == 1,
            "A compound resource emits one logical pair and a failed object handler does not suppress the later shape event.");
        tree.PhysicsFrame(1d / 60); Check(entries == 1, "Committed pair transitions do not replay after a handler failure.");
    }

    private static void VerifyWarmAllocation()
    {
        using var shape = new CircleShape(); var root = new Node(); var area = new Area { Name = "Area" }; var body = new StaticBody { Name = "Body" };
        Add(area, shape); Add(body, shape); root.AddChild(area); root.AddChild(body); using var tree = new SceneTree(root);
        area.BodyShapeEntered += static (_, _, _, _) => { }; area.BodyShapeExited += static (_, _, _, _) => { };
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed steady pair monitoring allocates no managed bytes.");
        for (var frame = 0; frame < 64; frame++) { body.Position = new(frame % 2 == 0 ? 100 : 0, 0); tree.PhysicsFrame(1d / 60); }
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) { body.Position = new(frame % 2 == 0 ? 100 : 0, 0); tree.PhysicsFrame(1d / 60); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed pair entry/exit callbacks allocate no managed bytes.");
    }

    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { if (Capture(action) is not T) throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
