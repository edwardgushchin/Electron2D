using Electron2D;

public static partial class BackendChecks
{
    private static void IdentityChecks()
    {
        var solvers = new List<Solver>();
        PhysicsServerManager.RegisterServer("Identity", () => { var s = new Solver(); solvers.Add(s); return s; });
        using var first = new World("Identity"); using var second = new World("Identity");
        var firstSpace = first.Space; var secondSpace = second.Space;
        using var circle = new CircleShape { Radius = 3 };
        using var owner = new Node(); using var replacement = new Node();
        var body = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        const ulong canvas = (1ul << 63) | 17, otherCanvas = (1ul << 40) | 17;
        using var point = new PhysicsPointQueryParameters { CanvasInstanceID = canvas, CollideWithAreas = true };
        var hits = new PhysicsPointResult[2];
        try
        {
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.AreaAddShape(area, circle.GetRID());
            PhysicsServer.BodyAttachCanvasInstanceID(body, canvas); PhysicsServer.AreaAttachCanvasInstanceID(area, canvas);
            PhysicsServer.BodyAttachObject(body, owner); PhysicsServer.AreaAttachObject(area, owner);
            PhysicsServer.BodySetCollisionPriority(body, 3);
            PhysicsServer.BodySetSpace(body, firstSpace); PhysicsServer.AreaSetSpace(area, firstSpace);
            Verify(solvers[0], canvas, owner.InstanceID, 3);
            Check(first.DirectSpaceState.IntersectPoint(point, hits) == 2 && hits.All(h => h.ColliderID == owner.InstanceID),
                "Detached authoring reaches the selected implementation before the first point query");
            var saved = hits[0];
            PhysicsServer.BodyAttachObject(body, replacement); PhysicsServer.AreaAttachObject(area, replacement);
            PhysicsServer.BodyAttachCanvasInstanceID(body, otherCanvas); PhysicsServer.AreaAttachCanvasInstanceID(area, otherCanvas);
            PhysicsServer.BodySetCollisionPriority(body, 7);
            Verify(solvers[0], otherCanvas, replacement.InstanceID, 7);
            Check(first.DirectSpaceState.IntersectPoint(point, hits) == 0 && saved.ColliderID == owner.InstanceID,
                "Live reassignment updates query membership and preserves sampled object identity");
            owner.Dispose();
            Check(saved.ColliderID == owner.InstanceID && saved.ColliderObject is null, "Sampled IDs survive disposal of their weak targets");
            Reject<ObjectDisposedException>(() => PhysicsServer.BodyAttachObject(body, owner));
            Reject<ObjectDisposedException>(() => PhysicsServer.AreaAttachObject(area, owner));
            point.CanvasInstanceID = otherCanvas;
            Check(first.DirectSpaceState.IntersectPoint(point, hits) == 2 && hits.All(h => h.ColliderID == replacement.InstanceID),
                "Reassigned object identity reaches public query output");
            PhysicsServer.BodySetSpace(body, secondSpace); PhysicsServer.AreaSetSpace(area, secondSpace);
            Verify(solvers[1], otherCanvas, replacement.InstanceID, 7);
            Check(solvers[0].Bodies.Count == 0 && second.DirectSpaceState.IntersectPoint(point, hits) == 2,
                "World transfer creates fresh attachments with retained identity and priority");
            PhysicsServer.BodyAttachObject(body, null); PhysicsServer.AreaAttachObject(area, null);
            Verify(solvers[1], otherCanvas, 0, 7);
            Collect(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) EditAndQuery(i);
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) EditAndQuery(i);
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Registered metadata edits/queries allocated {own}/{all} owner/all-thread B");
            Task.Run(() =>
            {
                Reject<InvalidOperationException>(() => PhysicsServer.BodyGetCanvasInstanceID(body));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaAttachObject(area, replacement));
            }).GetAwaiter().GetResult();
            PhysicsServer.BodySetSpace(body, default); PhysicsServer.AreaSetSpace(area, default);
            foreach (var failIdentity in new[] { true, false })
            {
                Action fail = () => throw new InvalidOperationException("Injected attachment metadata failure");
                solvers[0].IdentityHook = failIdentity ? _ => fail() : null; solvers[0].PriorityHook = failIdentity ? null : fail;
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetSpace(body, firstSpace));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetSpace(area, firstSpace));
                Check(!PhysicsServer.BodyGetSpace(body).IsValid() && !PhysicsServer.AreaGetSpace(area).IsValid() && solvers[0].Bodies.Count == 0,
                    "Failed initial metadata retires both attachments and preserves detached authoring");
                solvers[0].IdentityHook = null; solvers[0].PriorityHook = null;
            }
            solvers[0].IdentityHook = current =>
            {
                if (current == body)
                {
                    Reject<InvalidOperationException>(() => PhysicsServer.BodyAttachCanvasInstanceID(body, 0));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodyAttachObject(body, replacement));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetCollisionPriority(body, 9));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetSpace(body, secondSpace));
                }
                else
                {
                    Reject<InvalidOperationException>(() => PhysicsServer.AreaAttachCanvasInstanceID(area, 0));
                    Reject<InvalidOperationException>(() => PhysicsServer.AreaAttachObject(area, replacement));
                    Reject<InvalidOperationException>(() => PhysicsServer.AreaSetSpace(area, secondSpace));
                }
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(current));
            };
            PhysicsServer.BodySetSpace(body, firstSpace); PhysicsServer.AreaSetSpace(area, firstSpace);
            solvers[0].IdentityHook = null;
            Verify(solvers[0], point.CanvasInstanceID, 0, 2);
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(area); }
        using var root = new SubViewport { World = first };
        var layer = new CanvasLayer(); var scene = new StaticBody { CollisionPriority = 5 }; var sensor = new Area();
        scene.AddChild(new CollisionShape { Shape = circle }); sensor.AddChild(new CollisionShape { Shape = circle });
        layer.AddChild(scene); layer.AddChild(sensor); root.AddChild(layer);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(0);
        Check(solvers[0].Bodies[scene.GetRID()].ObjectID == scene.InstanceID && solvers[0].Bodies[scene.GetRID()].Priority == 5 &&
            solvers[0].Bodies[sensor.GetRID()].ObjectID == sensor.InstanceID, "Scene body/Area initial identity and body priority reach the same hooks");
        point.CanvasInstanceID = layer.InstanceID;
        Check(first.DirectSpaceState.IntersectPoint(point, hits) == 2, "Scene canvas membership reaches registered point queries");
        Console.WriteLine("Registered identity: detached/live/transfer/clear body+Area metadata, scene canvas, failed initial hooks and 0/0 B edits/queries passed.");

        void Verify(Solver solver, ulong expectedCanvas, ulong expectedObject, float expectedPriority)
        {
            Check(PhysicsServer.BodyGetCanvasInstanceID(body) == expectedCanvas && PhysicsServer.AreaGetCanvasInstanceID(area) == expectedCanvas &&
                PhysicsServer.BodyGetObjectInstanceID(body) == expectedObject && PhysicsServer.AreaGetObjectInstanceID(area) == expectedObject &&
                PhysicsServer.BodyGetCollisionPriority(body) == expectedPriority, "Public getters retain the complete authored metadata");
            Check(solver.Bodies[body].Canvas == expectedCanvas && solver.Bodies[area].Canvas == expectedCanvas &&
                solver.Bodies[body].ObjectID == expectedObject && solver.Bodies[area].ObjectID == expectedObject &&
                solver.Bodies[body].Priority == expectedPriority, "Registered body/Area attachments receive retained identity and body priority");
        }
        void EditAndQuery(int i)
        {
            point.CanvasInstanceID = (i & 1) == 0 ? canvas : otherCanvas;
            PhysicsServer.BodyAttachCanvasInstanceID(body, point.CanvasInstanceID); PhysicsServer.AreaAttachCanvasInstanceID(area, point.CanvasInstanceID);
            PhysicsServer.BodySetCollisionPriority(body, 2);
            Check(second.DirectSpaceState.IntersectPoint(point, hits) == 2, "Edited membership reaches both real shape queries");
        }
    }
}
