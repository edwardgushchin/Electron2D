using System.Diagnostics;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Query = Electron2D.GPUPhysicsBodyStore.WorldQuery;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class GPUPhysicsQueryTests
{
    internal static void Run()
    {
        CompareCPU(); OrderingAndFilters(); BoundaryAndBatch(); Lifetime(); AdaptiveReadback(); Measure();
        Console.WriteLine("Resident world queries: public CPU ray/point geometry, stable logical caps, masks/exclusions/canvas, lifetime and warmed allocation passed.");
    }
    private static Store.BodyHandle Body(Store s, Vector2 position = default, float rotation = 0) => s.Add(new(Mode.Static, position, rotation, Vector2.Zero, 0));
    private static int Execute(Store s, Query q, Span<Store.QueryHit> hits, ReadOnlySpan<ulong> excluded = default)
    { Span<int> count = stackalloc int[1]; s.Query([q], excluded, count, hits); return count[0]; }
    private static Vector2[] Polygon(int count)
    { var result = new Vector2[count]; for (var i = 0; i < count; i++) result[i] = new Vector2(12, 0).Rotated(Mathf.Tau * i / count); return result; }
    private static void CompareCPU()
    {
        var clockwise = Polygon(12); Array.Reverse(clockwise);
        Shape[] shapes = [new CircleShape { Radius = 5 }, new RectangleShape { Size = new(20, 12) }, new CapsuleShape { Radius = 4, Height = 20 },
            new SegmentShape { A = new(-9, 0), B = new(9, 0) }, new ConvexPolygonShape { Points = Polygon(12) },
            new ConcavePolygonShape { Segments = [new(-9, -8), new(9, -8), new(9, -8), new(9, 8)] }, new SeparationRayShape { Length = 15 },
            new SegmentShape { A = Vector2.Zero, B = new(.2f, 0) }, new CapsuleShape { Radius = 5, Height = 10.4f },
            new ConvexPolygonShape { Points = clockwise }];
        using var s = new Store(); var space = PhysicsServer.SpaceCreate(); var bodies = new RID[shapes.Length]; var random = new Random(313);
        using var ray = new PhysicsRayQueryParameters(); using var point = new PhysicsPointQueryParameters();
        try
        {
            var view = PhysicsServer.SpaceGetDirectState(space);
            Span<Store.QueryHit> actual = stackalloc Store.QueryHit[1]; var expectedPoint = new PhysicsPointResult[1];
            for (var family = 0; family < shapes.Length; family++)
            {
                var position = new Vector2(family * 200, 80); const float angle = .37f;
                var id = PhysicsServer.BodyCreate(); bodies[family] = id; PhysicsServer.BodySetMode(id, Mode.Static);
                PhysicsServer.BodySetCollisionLayer(id, 1u << family); PhysicsServer.BodySetCollisionMask(id, 0);
                PhysicsServer.BodySetTransform(id, new(angle, position)); PhysicsServer.BodyAddShape(id, shapes[family].GetRID()); PhysicsServer.BodySetSpace(id, space);
                // Compare queries at the same actual pose; CPU uses a deterministic approximate sine/cosine.
                var rotation = Box2D.NET.B2MathFunction.b2MakeRot(angle);
                var body = Body(s, position, MathF.Atan2(rotation.s, rotation.c)); var shape = s.AddShape(body, shapes[family], layer: 1u << family, mask: 0); s.SetQueryIdentity(shape, (ulong)id.GetID(), 0);
                for (var sample = 0; sample < 64; sample++)
                {
                    var y = (float)(random.NextDouble() * 36 - 18); var direction = (sample & 1) == 0 ? 1 : -1;
                    var from = position + new Vector2(-40 * direction, y).Rotated(angle); var to = position + new Vector2(40 * direction, y).Rotated(angle);
                    ray.From = from; ray.To = to; ray.CollisionMask = 1u << family;
                    var expected = view.IntersectRay(ray); var count = Execute(s, new(from, to, Ray: true, Mask: ray.CollisionMask), actual);
                    Check(count == (expected is null ? 0 : 1), $"Ray membership family={family}, sample={sample}");
                    if (expected is { } hit)
                    {
                        Check(actual[0].Collider == (ulong)hit.ColliderRID.GetID() && actual[0].LogicalShape == hit.ShapeIndex, "Ray logical identity");
                        Near(actual[0].Position, hit.Position, .004f, "Ray position"); Near(actual[0].Normal, hit.Normal, .001f, "Ray outward normal");
                    }
                    point.Position = position + new Vector2((float)(random.NextDouble() * 30 - 15), y).Rotated(angle); point.CollisionMask = ray.CollisionMask;
                    var points = view.IntersectPoint(point, expectedPoint); count = Execute(s, new(point.Position, Mask: point.CollisionMask, Limit: 1), actual);
                    // CPU GJK and GPU half-planes may classify opposite sides within float roundoff at an edge.
                    if (count != points)
                    {
                        Check(shapes[family] is ConvexPolygonShape, $"Point membership family={family}, sample={sample}");
                        var local = (point.Position - position).Rotated(-MathF.Atan2(rotation.s, rotation.c));
                        var contour = shapes[family].GetGeometry().Points; var distance = float.NegativeInfinity;
                        var winding = family == 9 ? -1 : 1;
                        for (var edge = 0; edge < contour.Length; edge++)
                        {
                            var d = contour[(edge + 1) % contour.Length] - contour[edge];
                            distance = MathF.Max(distance, winding * (local - contour[edge]).Dot(new Vector2(d.Y, -d.X).Normalized()));
                        }
                        Check(MathF.Abs(distance) <= .001f && count == (distance <= 0 ? 1 : 0), $"Point disagreement must be edge roundoff only: {distance} scene units.");
                    }
                    if (count > 0) Check(actual[0].Collider == (ulong)id.GetID() && actual[0].LogicalShape == 0, "Point logical identity");
                }
                if (family is 0 or 1 or 2 or 4 or 8 or 9)
                {
                    var from = position; var to = position + new Vector2(0, -40).Rotated(angle); ray.From = from; ray.To = to;
                    ray.HitFromInside = false;
                    Check(view.IntersectRay(ray) is null, $"Public CPU skips the complete logical filled shape when starting inside, family={family}.");
                    Check(Execute(s, new(from, to, Ray: true, Mask: ray.CollisionMask), actual) == 0, "GPU skips inside starts");
                    ray.HitFromInside = true; var cpu = view.IntersectRay(ray)!.Value;
                    Check(Execute(s, new(from, to, Ray: true, Mask: ray.CollisionMask, HitFromInside: true), actual) == 1 && actual[0].Normal == Vector2.Zero, "Inside hit has zero normal");
                    Near(actual[0].Position, cpu.Position, 0, "Inside hit is the origin"); ray.HitFromInside = false;
                }
            }
            Check(s.BroadPhaseSubmissionCount == 0 && s.PairCapacityRetries == 0, "World queries never enumerate simulation pairs.");
        }
        finally { foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); foreach (var shape in shapes) shape.Dispose(); }
    }
    private static void OrderingAndFilters()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 10 };
        var handles = new Store.ShapeHandle[5]; ulong[] keys = [100, (1ul << 40) + 3, 9, 9, 42]; int[] logical = [0, 1, 7, 7, 0];
        for (var i = 0; i < handles.Length; i++)
        { handles[i] = s.AddShape(Body(s), circle, layer: 1u << 31, mask: 0, sensor: i == 4); s.SetQueryIdentity(handles[i], keys[i], logical[i], 70ul); }
        Span<Store.QueryHit> hits = stackalloc Store.QueryHit[4];
        Check(Execute(s, new(Vector2.Zero, Areas: true, Limit: 4), hits) == 0, "Canvas zero selects the default canvas rather than all canvases.");
        var count = Execute(s, new(Vector2.Zero, Mask: 1u << 31, Areas: true, Limit: 4, Canvas: 70), hits);
        Check(count == 4 && hits[0].Collider == 9 && hits[1].Collider == 42 && hits[2].Collider == 100 && hits[3].Collider == keys[1], "Point order/dedup uses unsigned 64-bit collider key and logical index.");
        Check(Execute(s, new(Vector2.Zero, Mask: 1u << 31, Areas: true, Limit: 2, Canvas: 70), hits) == 2 && hits[0].Collider == 9 && hits[1].Collider == 42, "Cap follows logical order.");
        s.SetQueryIdentity(handles[4], 42, 0, 80);
        Check(Execute(s, new(Vector2.Zero, Mask: 1u << 31, Areas: true, Limit: 4, Canvas: 80), hits) == 1 && hits[0].Collider == 42, "Canvas filter uses authored association.");
        s.SetQueryIdentity(handles[4], 42, 0, 70);
        Check(Execute(s, new(Vector2.Zero, Mask: 1u << 31, Areas: true, Bodies: false, Limit: 4, Canvas: 70), hits) == 1 && hits[0].Collider == 42, "Area-only query");
        Check(Execute(s, new(Vector2.Zero, Mask: 1u << 31, Limit: 4, ExclusionStart: 1, ExclusionCount: 2, Canvas: 70), hits, [0, 9, 100]) == 1 && hits[0].Collider == keys[1], "Exclusions address logical collider keys and ignore reciprocal mask.");
        count = Execute(s, new(new(-30, 0), new(30, 0), Ray: true, Mask: 1u << 31, Areas: true), hits);
        Check(count == 1 && hits[0].Collider == 9, "Equal-distance ray tie follows logical identity.");
        s.SetQueryIdentity(handles[1], 1, 9, 70); Check(Execute(s, new(Vector2.Zero, Mask: 1u << 31, Limit: 4, Canvas: 70), hits) == 3 && hits[0].Collider == 1, "Identity-only edits do not require moving the body.");
        hits.Fill(new() { Collider = 777 }); Check(Execute(s, new(new(100, 100), Limit: 4), hits) == 0 && hits[0].Collider == 777 && hits[3].Collider == 777, "Miss leaves destination tail unchanged.");
    }
    private static void BoundaryAndBatch()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 5 };
        using var capsule = new CapsuleShape { Radius = 4, Height = 20 };
        using var segment = new SegmentShape { A = new(-10, 0), B = new(10, 0) };
        using var box = new RectangleShape { Size = new(10, 10) };
        var body = Body(s); s.AddShape(body, circle, layer: 1); s.AddShape(body, capsule, layer: 2);
        s.AddShape(body, segment, layer: 4); s.AddShape(body, box, layer: 8);
        Query[] queries = [new(new(-10, 5), new(10, 5), Ray: true, Mask: 1), // tangent circle
            new(new(-10, 5.01f), new(10, 5.01f), Ray: true, Mask: 1),
            new(new(-4.00001f, 0), new(-3.99999f, 0), Ray: true, Mask: 2), // tiny capsule side crossing
            new(new(0, -20), new(0, 20), Ray: true, Mask: 2), // rounded cap
            new(new(0, -10), new(0, 10), Ray: true, Mask: 4),
            new(new(0, 10), new(0, -10), Ray: true, Mask: 4),
            new(Vector2.Zero, Mask: 4, Limit: 2), // hollow segment has no interior
            new(Vector2.Zero, Ray: true, HitFromInside: true), // zero ray
            new(new(-5, 0), new(10, 0), Ray: true, Mask: 8), // filled boundary counts as inside
            new(new(-5, 0), new(10, 0), Ray: true, Mask: 8, HitFromInside: true),
            new(Vector2.Zero, Limit: 0), new(Vector2.Zero, Limit: 4)];
        int[] expected = [1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 3];
        var counts = new int[queries.Length + 1]; counts[^1] = 777;
        var hits = new Store.QueryHit[15]; hits.AsSpan().Fill(new() { Collider = 777 });
        s.Query(queries, [], counts, hits);
        for (var i = 0; i < queries.Length; i++) Check(counts[i] == expected[i], $"Batched boundary query {i}: {counts[i]} vs {expected[i]}");
        Near(hits[0].Position, new(0, 5), .001f, "Tangent point"); Near(hits[0].Normal, new(0, 1), .001f, "Tangent normal");
        Near(hits[2].Position, new(-4, 0), .000001f, "Tiny ray crossing"); Near(hits[3].Position, new(0, -10), .001f, "Capsule tip");
        Near(hits[4].Normal, new(0, -1), 0, "Segment front normal"); Near(hits[5].Normal, new(0, 1), 0, "Segment back normal");
        Check(hits[1].Collider == 777 && hits[6].Collider == 777 && hits[^1].Collider == 777 && counts[^1] == 777, "Mixed caps and misses preserve every unfilled tail.");
        var moving = s.Add(new(Mode.Rigid, new(100, 0), 0, new(100, 0), 0)); s.AddShape(moving, circle, layer: 16);
        s.Step(.5f, default); Check(Execute(s, new(new(150, 0), Mask: 16, Limit: 1), hits) == 1, "Queries read integrated GPU motion.");
        s.SetPose(moving, new(8, 0), 0); Execute(s, new(new(8, 0), Mask: 16, Limit: 1), hits);
        Check(s.FindContacts() > 0, "Queries cannot consume dirty geometry needed by subsequent simulation contacts.");
        s.SetPose(moving, new(100, 0), 0); Execute(s, new(new(100, 0), Mask: 16, Limit: 1), hits);
        Check(s.FindContacts() == 0, "Query tree updates invalidate simulation pairs/contacts after separation.");
    }
    private static void Lifetime()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 5 }; var body = Body(s); var shape = s.AddShape(body, circle); s.SetQueryIdentity(shape, 50, 0);
        Span<Store.QueryHit> hits = stackalloc Store.QueryHit[4]; Check(Execute(s, new(Vector2.Zero, Limit: 4), hits) == 1, "Initial hit");
        var spatial = s.QuerySpatialSubmissionCount; Execute(s, new(Vector2.Zero, Limit: 4), hits); Check(s.QuerySpatialSubmissionCount == spatial, "Unchanged geometry reuses device tree.");
        s.SetPose(body, new(50, 0), 0); Check(Execute(s, new(Vector2.Zero, Limit: 4), hits) == 0 && Execute(s, new(new(50, 0), Limit: 4), hits) == 1, "Pending pose is visible without simulation.");
        circle.Radius = 20; Check(Execute(s, new(new(65, 0), Limit: 4), hits) == 1, "Shared resource edits refresh geometry.");
        s.SetShapePose(shape, new(0, new(40, 0))); Check(Execute(s, new(new(50, 0), Limit: 4), hits) == 0, "Local shape pose edit");
        s.FindPairs(); var broad = s.BroadPhaseSubmissionCount; Execute(s, new(new(90, 0), Limit: 4), hits); Check(s.BroadPhaseSubmissionCount == broad, "Query after simulation pair work reuses conservative bounds.");
        s.RemoveShape(shape); var next = s.AddShape(body, circle); s.SetQueryIdentity(next, 60, 1); Check(Execute(s, new(new(50, 0), Limit: 4), hits) == 1 && hits[0].Collider == 60 && hits[0].ShapeGeneration == next.Generation, "Reuse does not inherit old identity.");
        Reject<ArgumentException>(() => s.SetQueryIdentity(shape, 1, 0));
        Reject<ArgumentOutOfRangeException>(() => Execute(s, new(new(float.NaN, 0), Limit: 1), new Store.QueryHit[1]));
        Reject<ArgumentOutOfRangeException>(() => Execute(s, new(Vector2.Zero, ExclusionCount: 2, Limit: 1), new Store.QueryHit[1], [1]));
        Reject<ArgumentException>(() => Execute(s, new(Vector2.Zero, Limit: 2), new Store.QueryHit[1]));
        Reject<InvalidOperationException>(() => Task.Run(() => Execute(s, new(Vector2.Zero, Limit: 1), new Store.QueryHit[1])).GetAwaiter().GetResult());
        for (var i = 0; i < 100; i++) s.AddShape(Body(s, new(1000 + i * 100, 0)), circle);
        Check(Execute(s, new(new(50, 0), Limit: 4), hits) == 1 && hits[0].Collider == 60, "Growth preserves logical mapping and live data.");
        circle.Dispose(); Check(Execute(s, new(new(50, 0), Limit: 4), hits) == 0, "Disposed geometry disappears.");
        s.Dispose(); Reject<ObjectDisposedException>(() => Execute(s, new(Vector2.Zero, Limit: 1), new Store.QueryHit[1]));
    }
    private static void AdaptiveReadback()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 2 };
        const int population = 256, limit = 512;
        for (var i = 0; i < population; i++) s.SetQueryIdentity(s.AddShape(Body(s), circle), (ulong)i + 1, 0);
        s.SetQueryIdentity(s.AddShape(Body(s, new(100, 0)), circle), 1000, 0);
        Query[] queries = [new(new(100, 0), Limit: limit), new(default, Limit: 0), new(new(1000, 0), Limit: limit)];
        var counts = new int[4]; counts[^1] = 777;
        var hits = new Store.QueryHit[2 * limit + 1]; hits.AsSpan().Fill(new() { Collider = 777 });
        s.Query(queries, [], counts, hits);
        var read = s.ReadbackBytes; var submissions = s.SubmissionCount;
        s.Query(queries, [], counts, hits);
        Check(counts[0] == 1 && counts[1] == 0 && counts[2] == 0 && counts[3] == 777, "Sparse batch retains counts and zero-limit segments");
        Check(s.ReadbackBytes - read == 8 + 3 * 4 + 2 * 64 && s.SubmissionCount - submissions == 1,
            "Sparse queries read one predicted hit per nonempty segment with one fence, independent of reserved capacity");
        queries[0] = new(default, Limit: limit); queries[2] = queries[0];
        read = s.ReadbackBytes; submissions = s.SubmissionCount; var searches = s.QuerySubmissionCount;
        s.Query(queries, [], counts, hits);
        Check(counts[0] == population && counts[2] == population && s.SubmissionCount - submissions == 2 && s.QuerySubmissionCount == searches + 1,
            "Unexpected dense results fetch missing tails once without repeating the search");
        Check(s.ReadbackBytes - read == 8 + 3 * 4 + 2 * population * 64, "Overflow downloads only actual missing results");
        for (var i = 0; i < population; i++) Check(hits[i].Collider == (ulong)i + 1 && hits[limit + i].Collider == (ulong)i + 1, "Packed transfer preserves both logical result segments");
        Check(hits[population].Collider == 777 && hits[limit - 1].Collider == 777 && hits[limit + population].Collider == 777 && hits[^1].Collider == 777,
            "Prefix and overflow copies leave all unused caller tails unchanged");
        read = s.ReadbackBytes; submissions = s.SubmissionCount;
        s.Query(queries, [], counts, hits);
        Check(s.SubmissionCount == submissions + 1 && s.ReadbackBytes - read == 8 + 3 * 4 + 2 * population * 64, "Dense predictions retain one fence without downloading cap padding");
        queries[0] = new(new(1000, 0), Limit: limit); queries[2] = queries[0];
        s.Query(queries, [], counts, hits); read = s.ReadbackBytes; s.Query(queries, [], counts, hits);
        Check(s.ReadbackBytes - read == 8 + 3 * 4 + 2 * 64, "Readback shrinks again after sparse results");
        queries[0] = new(default, Limit: limit); queries[2] = new(default, Limit: 7);
        s.Query(queries, [], counts, hits);
        Check(counts[0] == population && counts[2] == 7 && hits[limit + 6].Collider == 7, "Changed caps clamp the prediction and keep output offsets");
        using var probe = s.RetainQueryGeometry(circle);
        Store.ShapeQuery[] shapeQueries = [new(probe, Transform.Identity, Limit: limit), new(probe, Transform.Identity, Limit: 0), new(probe, new(0, new(100, 0)), Limit: limit)];
        var shapeHits = new Store.ShapeQueryHit[2 * limit + 1]; shapeHits.AsSpan().Fill(new() { Collider = 777 });
        s.QueryShapes(shapeQueries, [], counts, shapeHits);
        Check(counts[0] == population && counts[1] == 0 && counts[2] == 1 && shapeHits[limit].Collider == 1000 && shapeHits[^1].Collider == 777,
            "Shape queries reuse adaptive storage with their different output stride");
        s.Query(queries, [], counts, hits);
        Check(counts[0] == population && counts[2] == 7 && hits[limit + 6].Collider == 7, "Point query tails remain correct after shape-query reuse");
        for (var i = 0; i < 32; i++) s.Query(queries, [], counts, hits);
        var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 32; i++)
        {
            queries[0] = queries[0] with { From = (i & 1) == 0 ? new(1000, 0) : Vector2.Zero };
            s.Query(queries, [], counts, hits);
        }
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, "Repeated sparse/dense readback and overflow allocate zero managed bytes after capacity warmup");
    }
    private static void Measure()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 2 };
        const int population = 65536, batch = 256; var queries = new Query[batch]; var counts = new int[batch]; var hits = new Store.QueryHit[batch];
        for (var i = 0; i < population; i++) s.AddShape(Body(s, new(i % 256 * 10, i / 256 * 10)), circle);
        for (var i = 0; i < batch; i++) { var x = i * 10; queries[i] = new(new(x - 4, 1000), new(x + 4, 1000), Ray: true); }
        for (var i = 0; i < 96; i++) s.Query(queries, [], counts, hits);
        var samples = new double[128]; var upload = s.UploadBytes; var download = s.ReadbackBytes; var wait = s.WaitMS; var spatial = s.QuerySpatialSubmissionCount;
        var allAllocated = GC.GetTotalAllocatedBytes(true); var gc0 = GC.CollectionCount(0); var gc1 = GC.CollectionCount(1); var gc2 = GC.CollectionCount(2);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < samples.Length; i++) { var start = Stopwatch.GetTimestamp(); s.Query(queries, [], counts, hits); samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated; allAllocated = GC.GetTotalAllocatedBytes(true) - allAllocated;
        Check(allocated == 0 && allAllocated == 0, $"Warmed query batches allocate zero managed bytes: owner={allocated}, all={allAllocated}.");
        Check(GC.CollectionCount(0) == gc0 && GC.CollectionCount(1) == gc1 && GC.CollectionCount(2) == gc2, "Warmed query batches cause no GC collections.");
        for (var i = 0; i < batch; i++) { Check(counts[i] == 1, "Every query in the complete population returns a hit."); Near(hits[i].Position, new(i * 10 - 2, 1000), .001f, "Every selected position"); }
        Check(s.BroadPhaseSubmissionCount == 0 && s.QuerySpatialSubmissionCount == spatial, "Warm reads neither rebuild static geometry nor enumerate body pairs.");
        Array.Sort(samples);
        Console.WriteLine($"Resident ray queries: {population} shapes, {batch} rays/batch, 96 warmup/128 samples; p50/p95/p99={samples[64]:F4}/{samples[121]:F4}/{samples[126]:F4} ms; wait={(s.WaitMS - wait) / 128:F4}; {allocated}/{allAllocated} owner/all-thread managed B, upload={(s.UploadBytes - upload) / 128}, readback={(s.ReadbackBytes - download) / 128} B/batch; {s.Driver}, {s.DeviceName}.");
    }
    private static void Near(Vector2 a, Vector2 b, float tolerance, string message) => Check(a.IsFinite() && a.DistanceTo(b) <= tolerance, $"{message}: {a} vs {b}");
    private static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
