using Electron2D;

internal static class AreaTests
{
    internal static void Run()
    {
        VerifyBodyAndAreaSnapshots();
        VerifyMovingBodyPassesThrough();
        VerifyShapeEditsAndPacking();
        VerifyCallbackMutationAndFailure();
        Console.WriteLine("Physics area monitoring, filtering, lifecycle and allocation checks passed.");
    }

    private static void VerifyMovingBodyPassesThrough()
    {
        using var region = new RectangleShape { Size = new(80, 20) };
        using var circle = new CircleShape { Radius = 5 };
        var root = new Node();
        var area = new Area { Position = new(0, 50) };
        area.AddChild(new CollisionShape { Shape = region });
        var body = new RigidBody();
        body.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(area);
        root.AddChild(body);
        var entered = 0; var exited = 0;
        area.BodyEntered += value => { Check(ReferenceEquals(value, body), "Moving body enters the sensor."); entered++; };
        area.BodyExited += value => { Check(ReferenceEquals(value, body), "Moving body exits the sensor."); exited++; };
        using var tree = new SceneTree(root);
        for (var frame = 0; frame < 60; frame++) tree.PhysicsFrame(1d / 60);
        Check(entered == 1 && exited == 1 && body.GlobalPosition.Y > 100 &&
              !area.OverlapsBody(body),
            "A dynamic body passes through a sensor without collision response.");
    }

    private static void VerifyBodyAndAreaSnapshots()
    {
        using var region = new RectangleShape { Size = new(80, 80) };
        using var circle = new CircleShape { Radius = 5 };
        var root = new Node();
        var area = new Area { Name = "MainArea" };
        area.AddChild(new CollisionShape { Shape = region });
        area.AddChild(new CollisionShape { Shape = circle, Name = "InnerCircle" });
        var otherArea = new Area { Name = "OtherArea", Monitoring = false, CollisionMask = 0 };
        otherArea.AddChild(new CollisionShape { Shape = circle });
        var body = new StaticBody { CollisionMask = 0 };
        body.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(area);
        root.AddChild(otherArea);
        root.AddChild(body);
        var bodyEnters = 0; var bodyExits = 0; var areaEnters = 0; var areaExits = 0;
        area.BodyEntered += value => { Check(ReferenceEquals(value, body), "Body event carries the detected node."); bodyEnters++; };
        area.BodyExited += value => { Check(ReferenceEquals(value, body), "Body exit carries the detected node."); bodyExits++; };
        area.AreaEntered += value => { Check(ReferenceEquals(value, otherArea), "Area event carries the detected node."); areaEnters++; };
        area.AreaExited += value => { Check(ReferenceEquals(value, otherArea), "Area exit carries the detected node."); areaExits++; };

        using var tree = new SceneTree(root);
        Check(!area.HasOverlappingBodies() && !area.HasOverlappingAreas(),
            "Area snapshots start empty before the first physics step.");
        tree.PhysicsFrame(0);
        Check(bodyEnters == 0, "A zero-delta frame does not scan overlaps.");
        tree.PhysicsFrame(1d / 60);
        Check(bodyEnters == 1 && areaEnters == 1 && area.GetOverlappingBodies().Length == 1 &&
              area.GetOverlappingAreas().Length == 1 && area.OverlapsBody(body) && area.OverlapsArea(otherArea),
            "An area sees a static body with mask zero and another area once across multiple shapes.");
        Check(!otherArea.HasOverlappingBodies() && !otherArea.HasOverlappingAreas() &&
              !area.OverlapsBody(null) && !area.OverlapsArea(new Node()),
            "Monitoring is directional and unrelated nodes do not overlap.");
        Reject<InvalidOperationException>(() => Task.Run(area.HasOverlappingBodies).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => area.Monitoring = false).GetAwaiter().GetResult());
        Check(area.Monitoring, "Off-thread monitoring edits leave owner-thread state unchanged.");
        area.Monitorable = false;
        tree.PhysicsFrame(1d / 60);
        Check(area.OverlapsBody(body) && area.OverlapsArea(otherArea),
            "An area's own monitorability does not disable its monitoring.");
        area.Monitorable = true;
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Check(allocated == 0,
            "A warmed steady overlap scan allocates no managed memory: " + allocated);

        otherArea.CollisionLayer = 2;
        tree.PhysicsFrame(1d / 60);
        Check(areaExits == 1 && !area.OverlapsArea(otherArea), "The monitoring mask filters the other area's layer.");
        area.CollisionMask = 3;
        tree.PhysicsFrame(1d / 60);
        Check(areaEnters == 2 && area.OverlapsArea(otherArea), "Changing the area's mask updates the next snapshot.");
        otherArea.Monitorable = false;
        tree.PhysicsFrame(1d / 60);
        Check(areaExits == 2, "A nonmonitorable area disappears from other monitoring areas.");
        otherArea.Monitorable = true;
        tree.PhysicsFrame(1d / 60);
        Check(areaEnters == 3, "Restoring monitorability produces a new entry.");

        area.Monitoring = false;
        tree.PhysicsFrame(1d / 60);
        Check(bodyExits == 1 && areaExits == 3 && !area.HasOverlappingBodies() && !area.HasOverlappingAreas(),
            "Disabling monitoring clears both snapshots and reports exits.");
        area.Monitoring = true;
        tree.PhysicsFrame(1d / 60);
        Check(bodyEnters == 2 && areaEnters == 4, "Reenabling monitoring reports current overlaps.");

        body.Position = new(200, 0);
        otherArea.Position = new(200, 0);
        tree.PhysicsFrame(1d / 60);
        Check(bodyExits == 2 && areaExits == 4 && !area.HasOverlappingBodies() && !area.HasOverlappingAreas(),
            "Moving shapes away produces one exit per object.");
        allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - allocatedBefore == 0,
            "A warmed empty overlap scan allocates no managed memory.");

        body.Position = Vector2.Zero;
        tree.PhysicsFrame(1d / 60);
        root.RemoveChild(body);
        Check(!area.OverlapsBody(body) && bodyExits == 3,
            "Removing a body clears its overlap without another physics step.");
        body.Dispose();
    }

    private static void VerifyShapeEditsAndPacking()
    {
        using var region = new RectangleShape { Size = new(20, 20) };
        using var circle = new CircleShape { Radius = 5 };
        using var packedRoot = new Node { Name = "Root" };
        var area = new Area { Name = "Region", Monitoring = false, Monitorable = false, CollisionMask = 3 };
        var collider = new CollisionShape { Name = "Shape", Shape = region };
        area.AddChild(collider);
        packedRoot.AddChild(area);
        area.Owner = packedRoot;
        collider.Owner = packedRoot;
        Check(collider.GetConfigurationWarnings().Length == 0,
            "A collision shape accepts an Area as its direct collision owner.");
        using var packed = new PackedScene();
        packed.Pack(packedRoot);
        using var copy = packed.Instantiate();
        var restored = copy.GetNode<Area>("Region");
        Check(!restored.Monitoring && !restored.Monitorable && restored.CollisionMask == 3 &&
              ReferenceEquals(copy.GetNode<CollisionShape>("Region/Shape").Shape, region),
            "Packed scenes restore area flags, filters and borrowed shape identity.");

        Action<Resource> changedFailure = _ => throw new InvalidOperationException("User shape callback failure.");
        region.Changed += changedFailure;

        var root = new Node();
        var liveArea = new Area();
        var liveCollider = new CollisionShape { Shape = region };
        liveArea.AddChild(liveCollider);
        var body = new StaticBody { Position = new(30, 0) };
        body.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(liveArea);
        root.AddChild(body);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        Check(!liveArea.OverlapsBody(body), "Separated shapes do not count as overlapping.");
        Reject<InvalidOperationException>(() => region.Size = new(80, 80));
        region.Changed -= changedFailure;
        tree.PhysicsFrame(1d / 60);
        Check(liveArea.OverlapsBody(body),
            "Editing borrowed area geometry updates detection even when an earlier change subscriber throws.");
        liveCollider.Disabled = true;
        tree.PhysicsFrame(1d / 60);
        Check(!liveArea.OverlapsBody(body), "Disabling an area shape removes its overlap.");
        liveCollider.Disabled = false;
        liveArea.Scale = new(2, 1);
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
        liveArea.Scale = Vector2.One;
        tree.PhysicsFrame(1d / 60);
        Check(liveArea.OverlapsBody(body), "An invalid area transform leaves the world reusable after correction.");
    }

    private static void VerifyCallbackMutationAndFailure()
    {
        using var region = new RectangleShape { Size = new(80, 80) };
        using var circle = new CircleShape();
        var root = new Node();
        var area = new Area();
        area.AddChild(new CollisionShape { Shape = region });
        var body = new StaticBody();
        body.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(area);
        root.AddChild(body);
        var entered = 0; var exited = 0;
        var areaEntered = 0;
        area.BodyEntered += _ => { entered++; if (entered == 1) root.RemoveChild(body); };
        area.BodyExited += _ => exited++;
        area.AreaEntered += _ => areaEntered++;
        using (var tree = new SceneTree(root))
        {
            tree.PhysicsFrame(1d / 60);
            Check(entered == 1 && exited == 1 && !area.OverlapsBody(body),
                "An overlap callback can remove a body and deliver its exit without stale state.");
            root.AddChild(body);
            var otherArea = new Area { Name = "OtherArea" };
            otherArea.AddChild(new CollisionShape { Shape = region });
            root.AddChild(otherArea);
            area.BodyEntered += _ => throw new InvalidOperationException("User callback failure.");
            Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
            Check(entered == 2 && areaEntered == 1 && area.OverlapsBody(body) && area.OverlapsArea(otherArea),
                "A throwing overlap callback leaves committed snapshots and later events available.");
            tree.PhysicsFrame(1d / 60);
            Check(entered == 2, "A failed callback is not replayed on the next stable frame.");
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
