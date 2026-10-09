using System.Diagnostics;
using System.Runtime.CompilerServices;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;

internal static class PhysicsObjectBindingTests
{
    private sealed class Target : ElectronObject { }
    internal static void Run(bool gpu = false)
    {
        if (gpu) { GPU(); return; }
        Queries(); Monitoring();
        Console.WriteLine("Physics object binding passed: weak snapshots, query/motion/contact identities, scene membership, callbacks, guards and allocation.");
    }
    private static void Queries()
    {
        using var shape = new CircleShape { Radius = 10 };
        using var first = new Target(); using var second = new Target();
        var space = PhysicsServer.SpaceCreate(); var body = PhysicsServer.BodyCreate(); var mover = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        using var point = new PhysicsPointQueryParameters(); using var ray = PhysicsRayQueryParameters.Create(new(-30, 0), new(30, 0));
        using var query = new PhysicsShapeQueryParameters { Shape = shape };
        using var parameters = new PhysicsTestMotionParameters { From = new(0, new(-30, 0)), Motion = new(40, 0) };
        using var result = new PhysicsTestMotionResult();
        try
        {
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(body, shape.GetRID());
            Check(PhysicsServer.BodyGetObjectInstanceID(body) == 0, "Raw bodies start unassigned.");
            PhysicsServer.BodyAttachObject(body, first); PhysicsServer.BodySetSpace(body, space);
            PhysicsServer.BodyAddShape(mover, shape.GetRID()); PhysicsServer.BodySetTransform(mover, new(0, new(-30, 0))); PhysicsServer.BodySetSpace(mover, space);
            PhysicsServer.AreaAddShape(area, shape.GetRID()); PhysicsServer.AreaAttachObject(area, second); PhysicsServer.AreaSetSpace(area, space);
            var direct = PhysicsServer.SpaceGetDirectState(space);
            var pointHit = direct.IntersectPoint(point).Single(); var rayHit = direct.IntersectRay(ray)!.Value;
            var shapeHit = direct.IntersectShape(query).Single(); var restHit = direct.GetRestInfo(query)!.Value;
            foreach (var identity in new[] { pointHit.ColliderID, rayHit.ColliderID, shapeHit.ColliderID, restHit.ColliderID }) Check(identity == first.InstanceID, "Every direct query publishes assigned identity.");
            Check(pointHit.Collider is null && pointHit.ColliderObject == first && rayHit.ColliderObject == first && shapeHit.ColliderObject == first && restHit.ColliderObject == first, "Physical scene convenience stays null for raw bodies; attached object is available in every result.");
            Check(PhysicsServer.BodyTestMotion(mover, parameters, result) && result.GetColliderID() == first.InstanceID && result.GetCollider() == first, "Motion snapshot captures bound object.");
            parameters.ExcludeObjects = [first.InstanceID]; Check(!PhysicsServer.BodyTestMotion(mover, parameters, result), "Object exclusions use assigned identity."); parameters.ExcludeObjects = [];
            Check(PhysicsServer.BodyTestMotion(mover, parameters, result), "Removing exclusion restores motion hit.");
            var backend = PhysicsServer.Service.BodyRuntime(body).Backend; var native = backend.BodyID; var fixture = backend.Shapes[0];
            PhysicsServer.BodyAttachObject(body, second);
            Check(pointHit.ColliderObject == first && rayHit.ColliderObject == first && shapeHit.ColliderObject == first && restHit.ColliderObject == first && result.GetCollider() == first, "Rebinding cannot rewrite old query/motion snapshots.");
            Check(direct.IntersectPoint(point)[0].ColliderObject == second && native.Equals(backend.BodyID) && fixture.Equals(backend.Shapes[0]), "New association is immediate and preserves solver identity.");
            point.CollideWithBodies = false; point.CollideWithAreas = true;
            Check(direct.IntersectPoint(point).Single().ColliderObject == second && PhysicsServer.AreaGetObjectInstanceID(area) == second.InstanceID, "Area association uses the same path.");
            PhysicsServer.BodySetSpace(body, default); PhysicsServer.BodySetSpace(body, space);
            Check(PhysicsServer.BodyGetObjectInstanceID(body) == second.InstanceID, "Raw association survives reattachment.");
            first.Dispose(); Check(pointHit.ColliderID == first.InstanceID && pointHit.ColliderObject is null && result.GetCollider() is null, "Disposal invalidates borrowed targets while sampled IDs survive.");
            Reject<ObjectDisposedException>(() => PhysicsServer.BodyAttachObject(body, first));
            Reject<ArgumentException>(() => PhysicsServer.BodyAttachObject(area, second));
            Reject<ArgumentException>(() => PhysicsServer.AreaGetObjectInstanceID(body));
            Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.AreaAttachObject(area, null)).GetAwaiter().GetResult());
            var world = Box2D.NET.B2Worlds.b2GetWorldFromId(backend.Space!.WorldID); world.locked = true;
            try { Reject<InvalidOperationException>(() => PhysicsServer.BodyAttachObject(body, null)); }
            finally { world.locked = false; }
            Check(PhysicsServer.BodyGetObjectInstanceID(body) == second.InstanceID, "Rejected writes preserve association.");
            PhysicsServer.BodyAttachObject(body, null); Check(PhysicsServer.BodyGetObjectInstanceID(body) == 0, "Null clears association.");
            var weak = BindTemporary(body); var id = PhysicsServer.BodyGetObjectInstanceID(body);
            point.CollideWithBodies = true; point.CollideWithAreas = false;
            var retained = direct.IntersectPoint(point).Single();
            for (var i = 0; i < 8 && Alive(weak); i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
            Check(!Alive(weak) && retained.ColliderObject is null && retained.ColliderID == id && PhysicsServer.BodyGetObjectInstanceID(body) == id, "Bindings, fixtures and saved results do not root targets.");
        }
        finally { PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(mover); PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
        Reject<ArgumentException>(() => PhysicsServer.BodyGetObjectInstanceID(body));
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<ElectronObject> BindTemporary(RID body)
    { var target = new Target(); PhysicsServer.BodyAttachObject(body, target); return new(target); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Alive(WeakReference<ElectronObject> weak) => weak.TryGetTarget(out _);

    private static void Monitoring()
    {
        using var shape = new CircleShape { Radius = 10 }; using var sensorShape = new CircleShape { Radius = 40 };
        var root = new SubViewport(); var owner = new Entity { Name = "Owner" }; var replacement = new Entity { Name = "Replacement" };
        var sensor = new Area { Name = "Sensor", CollisionMask = 2, CollisionLayer = 8 }; sensor.AddChild(new CollisionShape { Shape = sensorShape });
        var receiver = new RigidBody { Name = "Receiver", GravityScale = 0, Position = new(18, 0), CollisionLayer = 4, CollisionMask = 2, ContactMonitor = true, MaxContactsReported = 8 };
        receiver.AddChild(new CollisionShape { Shape = shape });
        root.AddChild(owner); root.AddChild(replacement); root.AddChild(sensor); root.AddChild(receiver);
        using var tree = new SceneTree(root);
        Check(PhysicsServer.BodyGetObjectInstanceID(receiver.GetRID()) == receiver.InstanceID && PhysicsServer.AreaGetObjectInstanceID(sensor.GetRID()) == sensor.InstanceID, "Scene associations initially identify their nodes.");
        var body = PhysicsServer.BodyCreate(); var second = PhysicsServer.BodyCreate();
        var enters = 0; var exits = 0; var contactEnters = 0; var contactExits = 0;
        var raw = new List<(PhysicsServer.AreaBodyStatus Status, RID RID, ulong ID)>(1024);
        sensor.BodyEntered += _ => enters++; sensor.BodyExited += _ => exits++;
        receiver.BodyEntered += _ => contactEnters++; receiver.BodyExited += _ => contactExits++;
        PhysicsServer.AreaSetMonitorCallback(sensor.GetRID(), (status, rid, id, _, _) => raw.Add((status, rid, id)));
        try
        {
            foreach (var id in new[] { body, second })
            {
                PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionLayer(id, 2); PhysicsServer.BodySetCollisionMask(id, 4);
                PhysicsServer.BodyAddShape(id, shape.GetRID()); PhysicsServer.BodyAttachObject(id, owner);
                if (id == second) PhysicsServer.BodySetTransform(id, new(0, new(0, 22)));
                PhysicsServer.BodySetSpace(id, root.FindWorld()!.Space);
            }
            tree.PhysicsFrame(1d / 60);
            Check(enters == 1 && sensor.GetOverlappingBodies().Single() == owner && sensor.OverlapsBody(owner), "Several raw bodies deduplicate one spatial object association.");
            Check(contactEnters == 1 && receiver.GetCollidingBodies().Contains(owner), "Rigid monitoring reports a bound Entity that is not a PhysicsBody.");
            var rayNode = new RayCast { Name = "Ray", Enabled = false, Position = new(-30, 0), TargetPosition = new(60, 0), CollisionMask = 2 };
            var castNode = new ShapeCast { Name = "Cast", Enabled = false, Position = new(-30, 0), TargetPosition = new(60, 0), Shape = shape, CollisionMask = 2 };
            root.AddChild(rayNode); root.AddChild(castNode); rayNode.ForceRaycastUpdate(); castNode.ForceShapecastUpdate();
            using var movement = new KinematicCollision();
            Check(receiver.TestMove(new(0, new(-30, 0)), new(40, 0), movement) && movement.GetCollider() == owner && movement.GetColliderID() == owner.InstanceID, "Scene movement snapshots carry assigned identity.");
            Check(rayNode.GetCollider() == owner && castNode.GetCollider(0) == owner && castNode.CollisionResult[0].ColliderID == owner.InstanceID, "Scene ray and shape queries expose the assigned object.");
            var state = PhysicsServer.BodyGetDirectState(receiver.GetRID())!;
            Check(state.GetContactCount() > 0 && state.GetContactColliderID(0) == owner.InstanceID && state.GetContactColliderObject<Entity>(0) == owner && state.GetContactColliderObject(0) is null, "Direct contacts retain assigned and physical scene roles separately.");
            var rawCount = raw.Count;
            Action<Entity> failingExit = _ => throw new InvalidOperationException("exit observer"); sensor.BodyExited += failingExit;
            Reject<AggregateException>(() => root.RemoveChild(owner)); sensor.BodyExited -= failingExit;
            Check(exits == 1 && contactExits == 1 && !sensor.OverlapsBody(owner), "Bound node departure emits immediate scene exits without deleting raw bodies.");
            Reject<InvalidOperationException>(() => Task.Run(owner.Dispose).GetAwaiter().GetResult());
            Check(!owner.IsDisposed, "Detached associated nodes retain the bound physics owner's disposal guard.");
            tree.PhysicsFrame(1d / 60); Check(raw.Count == rawCount, "Scene departure does not fabricate server physical-pair exits.");
            root.AddChild(owner);
            Check(enters == 2 && contactEnters == 2 && sensor.OverlapsBody(owner), "Bound node reentry emits immediate scene entries for retained pairs.");
            PhysicsServer.BodyAttachObject(body, replacement);
            Check(state.GetContactColliderObject<Entity>(0) == owner, "Post-step rebind leaves captured contact ownership unchanged.");
            Check(rayNode.GetCollider() == owner && castNode.GetCollider(0) == owner && castNode.CollisionResult[0].ColliderID == owner.InstanceID && movement.GetCollider() == owner, "Scene query caches are not retargeted by rebinding.");
            rayNode.ForceRaycastUpdate(); castNode.ForceShapecastUpdate();
            Check(rayNode.GetCollider() == replacement && castNode.GetCollider(0) == replacement, "Explicit scene resampling observes the replacement object.");
            tree.PhysicsFrame(1d / 60);
            Check(sensor.GetOverlappingBodies().Length == 2 && raw.Any(x => x.Status == PhysicsServer.AreaBodyStatus.Removed && x.RID == body && x.ID == owner.InstanceID) && raw.Any(x => x.Status == PhysicsServer.AreaBodyStatus.Added && x.RID == body && x.ID == replacement.InstanceID), "Rebinding preserves old/new IDs through physical callback transitions.");
            PhysicsServer.FreeRID(second); second = default;
            Check(!sensor.GetOverlappingBodies().Contains(owner), "Free removes only that body's remaining owner contribution.");
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[8]; var direct = root.FindWorld()!.DirectSpaceState;
            var times = new double[128];
            void Tick(int i)
            {
                PhysicsServer.BodyAttachObject(body, (i & 1) == 0 ? owner : replacement);
                tree.PhysicsFrame(1d / 60);
                direct.IntersectPoint(point, hits);
            }
            for (var i = 0; i < 128; i++) Tick(i);
            raw.Clear();
            var all = GC.GetTotalAllocatedBytes(true); var own = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 128; i++) { var start = Stopwatch.GetTimestamp(); Tick(i); times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Warmed binding/step/query path allocates zero bytes: {own}/{all}.");
            Array.Sort(times); Console.WriteLine($"Object binding: 128 warmup/128 owner changes + full scene steps + point queries; p50/p95/p99={times[64]:F4}/{times[121]:F4}/{times[126]:F4} ms; {own}/{all} owner/all-thread managed B.");
        }
        finally
        {
            if (second.IsValid()) PhysicsServer.FreeRID(second);
            PhysicsServer.FreeRID(body);
        }
    }

    private static void GPU()
    {
        using var store = new Store(); using var shape = new CircleShape { Radius = 10 };
        var body = store.Add(new(PhysicsServer.BodyMode.Static, Vector2.Zero, 0, Vector2.Zero, 0));
        var handle = store.AddShape(body, shape); var hits = new Store.QueryHit[1]; Span<int> counts = stackalloc int[1];
        using var geometry = store.RetainQueryGeometry(shape); var shapeHits = new Store.ShapeQueryHit[1];
        foreach (var id in new[] { 0ul, 7ul, (1ul << 40) | 7, ulong.MaxValue })
        {
            store.SetQueryIdentity(handle, 99, 3, objectID: id);
            store.Query([new(Vector2.Zero, Limit: 1)], [], counts, hits);
            Check(counts[0] == 1 && hits[0].ObjectID == id && hits[0].Collider == 99 && hits[0].LogicalShape == 3, "GPU point hit preserves every object-ID bit.");
            foreach (var mode in Enum.GetValues<Store.ShapeQueryMode>())
            {
                var cast = mode == Store.ShapeQueryMode.Cast;
                store.QueryShapes([new(geometry, new(0, cast ? new(-30, 0) : Vector2.Zero), Motion: cast ? new(40, 0) : Vector2.Zero, Mode: mode, Limit: 1)], [], counts, shapeHits);
                Check(counts[0] == 1 && shapeHits[0].ObjectID == id, "GPU shape intersection/contact/rest/cast captures the complete object ID.");
            }
            var retained = hits[0];
            store.SetQueryIdentity(handle, 99, 3, objectID: 88);
            store.Query([new(new(-30, 0), new(30, 0), Ray: true)], [], counts, hits);
            Check(hits[0].ObjectID == 88 && retained.ObjectID == id, "GPU ray hit and earlier point snapshot retain distinct sampled IDs.");
        }
        Console.WriteLine("GPU query object identities passed: ray/point records remain 64 B; complete shape results use 96 B.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
