using System.Diagnostics;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;

internal static class PhysicsCanvasTests
{
    private const ulong Canvas = (1ul << 63) | 17;
    private const ulong OtherCanvas = (1ul << 40) | 17;

    internal static void Run(bool resident = false)
    {
        if (!resident) SceneMembership();
        using var circle = new CircleShape { Radius = 5 };
        using var gpu = resident ? new Store() : null;
        var space = PhysicsServer.SpaceCreate();
        RID[] colliders = [PhysicsServer.BodyCreate(), PhysicsServer.BodyCreate(), PhysicsServer.AreaCreate(), PhysicsServer.AreaCreate()];
        var shapes = new Store.ShapeHandle[4];
        using var point = new PhysicsPointQueryParameters { CollideWithAreas = true };
        using var ray = PhysicsRayQueryParameters.Create(new(-10, 0), new(10, 0));
        var view = PhysicsServer.SpaceGetDirectState(space);
        var hits = new PhysicsPointResult[4]; var nativeHits = new Store.QueryHit[4];
        try
        {
            for (var i = 0; i < colliders.Length; i++)
            {
                var id = colliders[i]; var area = i >= 2; var canvas = (i & 1) == 0 ? 0 : Canvas;
                if (area)
                {
                    PhysicsServer.AreaAddShape(id, circle.GetRID()); PhysicsServer.AreaSetCollisionLayer(id, 2);
                    PhysicsServer.AreaSetCollisionMask(id, 0); PhysicsServer.AreaAttachCanvasInstanceID(id, canvas);
                    PhysicsServer.AreaSetSpace(id, space);
                    Check(PhysicsServer.AreaGetCanvasInstanceID(id) == canvas, "Area retains detached canvas metadata across attachment.");
                }
                else
                {
                    PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(id, circle.GetRID());
                    PhysicsServer.BodySetCollisionMask(id, 0); PhysicsServer.BodyAttachCanvasInstanceID(id, canvas);
                    PhysicsServer.BodySetSpace(id, space);
                    Check(PhysicsServer.BodyGetCanvasInstanceID(id) == canvas, "Body retains detached canvas metadata across attachment.");
                }
                if (gpu is not null)
                {
                    var body = gpu.Add(new(PhysicsServer.BodyMode.Static, Vector2.Zero, 0, Vector2.Zero, 0));
                    shapes[i] = gpu.AddShape(body, circle, layer: area ? 2u : 1u, mask: 0, sensor: area);
                    gpu.SetQueryIdentity(shapes[i], (ulong)id.GetID(), 0, canvas);
                }
            }
            foreach (var canvas in new[] { 0ul, Canvas, OtherCanvas, ulong.MaxValue })
                foreach (var areas in new[] { false, true }) foreach (var bodies in new[] { false, true })
                        foreach (var mask in new[] { 0u, 1u, 2u, uint.MaxValue })
                        {
                            point.CanvasInstanceID = canvas; point.CollideWithAreas = areas; point.CollideWithBodies = bodies; point.CollisionMask = mask;
                            var expected = canvas == 0 || canvas == Canvas ? ((bodies && (mask & 1) != 0 ? 1 : 0) + (areas && (mask & 2) != 0 ? 1 : 0)) : 0;
                            Check(Query() == expected, "Point membership uses exact 64-bit canvas, kind and one-sided layer filters.");
                            for (var i = 0; i < expected; i++)
                            {
                                var target = hits[i].ColliderRID;
                                var index = Array.IndexOf(colliders, target);
                                Check(index >= 0 && ((index & 1) == 0) == (canvas == 0), "Every result belongs to the selected canvas.");
                            }
                        }
            point.CanvasInstanceID = Canvas; point.CollideWithAreas = point.CollideWithBodies = true; point.CollisionMask = uint.MaxValue;
            point.Exclude = [colliders[1]];
            Check(Query() == 1 && hits[0].ColliderRID == colliders[3], "RID exclusions follow canvas selection.");
            point.Exclude = [];
            var one = view.IntersectPoint(point, 1);
            Check(one.Length == 1 && one[0].ColliderRID == colliders[1], "Array cap uses stable ordering after canvas filtering.");
            var tail = hits[3]; Check(view.IntersectPoint(point, hits.AsSpan(0, 1)) == 1 && hits[3].Equals(tail), "Span cap preserves the caller tail.");
            Check(view.IntersectRay(ray)?.ColliderRID == colliders[0], "Rays continue to cross canvas associations.");
            if (gpu is not null)
            {
                Span<int> counts = stackalloc int[1];
                gpu.Query([new(new(-10, 0), new(10, 0), Ray: true, Canvas: OtherCanvas)], [], counts, nativeHits);
                Check(counts[0] == 1 && nativeHits[0].Collider == (ulong)colliders[0].GetID(), "GPU rays ignore the point-only canvas selector.");
            }
            var backend = PhysicsServer.Service.BodyRuntime(colliders[1]).Backend;
            var bodyID = backend.BodyID; var fixture = backend.Shapes[0];
            PhysicsServer.BodyAttachCanvasInstanceID(colliders[1], OtherCanvas);
            gpu?.SetQueryIdentity(shapes[1], (ulong)colliders[1].GetID(), 0, OtherCanvas);
            Check(Query() == 1 && hits[0].ColliderRID == colliders[3], "Live metadata edits affect the next query without a step.");
            Check(backend.BodyID.Equals(bodyID) && backend.Shapes[0].Equals(fixture), "Canvas edits preserve body and fixture identities.");
            PhysicsServer.BodySetSpace(colliders[1], default); PhysicsServer.BodySetSpace(colliders[1], space);
            PhysicsServer.AreaSetSpace(colliders[3], default); PhysicsServer.AreaSetSpace(colliders[3], space);
            Check(PhysicsServer.BodyGetCanvasInstanceID(colliders[1]) == OtherCanvas, "Server detach and reattachment retain authored canvas association.");
            Check(PhysicsServer.AreaGetCanvasInstanceID(colliders[3]) == Canvas, "Area detach and reattachment retain authored canvas association.");
            Reject<ArgumentException>(() => PhysicsServer.BodyAttachCanvasInstanceID(colliders[2], 1));
            Reject<ArgumentException>(() => PhysicsServer.AreaGetCanvasInstanceID(colliders[0]));
            Reject<ArgumentException>(() => PhysicsServer.BodyGetCanvasInstanceID(space));
            Reject<ArgumentException>(() => PhysicsServer.AreaAttachCanvasInstanceID(circle.GetRID(), 1));
            Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.BodyAttachCanvasInstanceID(colliders[1], 0)).GetAwaiter().GetResult());
            var world = Box2D.NET.B2Worlds.b2GetWorldFromId(backend.Space!.WorldID); world.locked = true;
            try
            {
                Reject<InvalidOperationException>(() => PhysicsServer.BodyGetCanvasInstanceID(colliders[1]));
                Reject<InvalidOperationException>(() => PhysicsServer.AreaAttachCanvasInstanceID(colliders[3], 0));
            }
            finally { world.locked = false; }
            Check(PhysicsServer.BodyGetCanvasInstanceID(colliders[1]) == OtherCanvas && PhysicsServer.AreaGetCanvasInstanceID(colliders[3]) == Canvas, "Rejected writes preserve associations.");

            var times = new double[128];
            for (var i = 0; i < 128; i++) EditAndQuery(i);
            var upload = gpu?.UploadBytes ?? 0; var readback = gpu?.ReadbackBytes ?? 0; var wait = gpu?.WaitMS ?? 0;
            var all = GC.GetTotalAllocatedBytes(true); var own = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < times.Length; i++) { var start = Stopwatch.GetTimestamp(); EditAndQuery(i); times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Warmed canvas edits/queries allocate zero managed bytes: {own}/{all}.");
            Array.Sort(times);
            Console.WriteLine($"Canvas {(resident ? "GPU plus public CPU oracle" : "public CPU")}: 4 colliders, 128 warmup/128 edited point queries, p50/p95/p99={times[64]:F4}/{times[121]:F4}/{times[126]:F4} ms; {own}/{all} owner/all-thread managed B; upload={((gpu?.UploadBytes ?? 0) - upload) / 128}, readback={((gpu?.ReadbackBytes ?? 0) - readback) / 128} B/query, wait={((gpu?.WaitMS ?? 0) - wait) / 128:F4} ms.");
            PhysicsServer.FreeRID(colliders[3]);
            Reject<ArgumentException>(() => PhysicsServer.AreaGetCanvasInstanceID(colliders[3]));
            colliders[3] = default;
            point.Dispose(); Reject<ObjectDisposedException>(() => point.CanvasInstanceID = 0);
        }
        finally
        {
            foreach (var id in colliders) if (id.IsValid()) PhysicsServer.FreeRID(id);
            PhysicsServer.FreeRID(space);
        }
        Console.WriteLine("Physics canvas associations passed: scene/server identity, exact filtering, lifecycle, guards and warmed allocation.");

        int Query()
        {
            var count = view.IntersectPoint(point, hits);
            if (gpu is null) return count;
            Span<int> counts = stackalloc int[1]; Span<ulong> excluded = stackalloc ulong[point.ExclusionsArray.Length];
            for (var i = 0; i < excluded.Length; i++) excluded[i] = (ulong)point.ExclusionsArray[i].GetID();
            gpu.Query([new(point.Position, Mask: point.CollisionMask, Bodies: point.CollideWithBodies, Areas: point.CollideWithAreas, Limit: 4, Canvas: point.CanvasInstanceID, ExclusionCount: excluded.Length)], excluded, counts, nativeHits);
            Check(counts[0] == count, "CPU/GPU canvas membership agrees exactly.");
            for (var i = 0; i < count; i++) Check(nativeHits[i].Collider == (ulong)hits[i].ColliderRID.GetID() && nativeHits[i].LogicalShape == hits[i].ShapeIndex, "CPU/GPU logical identities agree exactly.");
            return count;
        }
        void EditAndQuery(int sample)
        {
            var canvas = (sample & 1) == 0 ? Canvas : OtherCanvas;
            PhysicsServer.BodyAttachCanvasInstanceID(colliders[1], canvas);
            gpu?.SetQueryIdentity(shapes[1], (ulong)colliders[1].GetID(), 0, canvas);
            Check(Query() == ((sample & 1) == 0 ? 2 : 1), "Measured edited query retains the full population and expected membership.");
        }
    }

    private static void SceneMembership()
    {
        using var shape = new CircleShape { Radius = 5 };
        var root = new SubViewport(); var first = new CanvasLayer { Name = "First", Offset = new(100, 0) }; var second = new CanvasLayer { Name = "Second" };
        root.AddChild(first); root.AddChild(second);
        var body = new RigidBody { GravityScale = 0, Sleeping = true }; body.AddChild(new CollisionShape { Shape = shape }); first.AddChild(body);
        var area = new Area(); area.AddChild(new CollisionShape { Shape = shape }); first.AddChild(area);
        var normal = new StaticBody(); normal.AddChild(new CollisionShape { Shape = shape }); root.AddChild(normal);
        using var tree = new SceneTree(root); using var point = new PhysicsPointQueryParameters { CollideWithAreas = true };
        var view = root.FindWorld()!.DirectSpaceState;
        Check(PhysicsServer.BodyGetCanvasInstanceID(body.GetRID()) == first.InstanceID && PhysicsServer.AreaGetCanvasInstanceID(area.GetRID()) == first.InstanceID, "Scene bodies and Areas publish CanvasLayer identity on entry.");
        Check(view.IntersectPoint(point).Length == 1, "Default point query excludes layered colliders in the same world.");
        point.CanvasInstanceID = first.InstanceID; Check(view.IntersectPoint(point).Length == 2, "Canvas filtering does not apply the visual layer transform to physics coordinates.");
        point.Position = new(100, 0); Check(view.IntersectPoint(point).Length == 0, "Canvas offset is not a physics transform."); point.Position = Vector2.Zero;
        body.Reparent(second); area.Reparent(second);
        Check(PhysicsServer.BodyGetCanvasInstanceID(body.GetRID()) == second.InstanceID && PhysicsServer.AreaGetCanvasInstanceID(area.GetRID()) == second.InstanceID, "Reparent refreshes body and Area associations.");
        Check(view.IntersectPoint(point).Length == 0, "Old canvas has no stale query members.");
        point.CanvasInstanceID = second.InstanceID; Check(view.IntersectPoint(point).Length == 2, "Reparented shapes remain in their physical world.");
        PhysicsServer.BodyAttachCanvasInstanceID(body.GetRID(), Canvas);
        Check(body.Sleeping && PhysicsServer.BodyGetCanvasInstanceID(body.GetRID()) == Canvas, "Explicit scene override retains sleep.");
        second.RemoveChild(body); Check(PhysicsServer.BodyGetCanvasInstanceID(body.GetRID()) == 0, "Canvas exit clears scene association."); first.AddChild(body);
        Check(PhysicsServer.BodyGetCanvasInstanceID(body.GetRID()) == first.InstanceID, "Reentry replaces a manual override with the current layer.");
        body.ProcessMode = ProcessMode.Disabled;
        Check(PhysicsServer.BodyGetCanvasInstanceID(body.GetRID()) == first.InstanceID, "Physics disable removal retains scene canvas membership.");
        Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.BodyAttachCanvasInstanceID(body.GetRID(), 0)).GetAwaiter().GetResult());
        body.ProcessMode = ProcessMode.Inherit;
        var peer = new SubViewport { Name = "Peer" }; root.AddChild(peer); peer.World = root.World;
        var peerBody = new StaticBody(); peerBody.AddChild(new CollisionShape { Shape = shape }); peer.AddChild(peerBody);
        point.CanvasInstanceID = 0;
        Check(view.IntersectPoint(point).Length == 2 && PhysicsServer.BodyGetCanvasInstanceID(peerBody.GetRID()) == 0, "Shared worlds retain one default canvas association across viewports.");
        peer.World = null;
        Check(view.IntersectPoint(point).Length == 1 && peer.FindWorld()!.DirectSpaceState.IntersectPoint(point).Length == 1, "Independent worlds isolate queries even when both canvas IDs are zero.");
        tree.PhysicsFrame(1d / 60);
        Check(area.GetOverlappingBodies().Contains(normal), "Canvas association does not filter physical Area overlaps across layers.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
