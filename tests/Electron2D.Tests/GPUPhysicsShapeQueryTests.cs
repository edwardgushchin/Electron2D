using System.Diagnostics;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Query = Electron2D.GPUPhysicsBodyStore.ShapeQuery;
using Mode = Electron2D.GPUPhysicsBodyStore.ShapeQueryMode;

internal static class GPUPhysicsShapeQueryTests
{
    internal static void Run()
    {
        CompareCPU(); SpecialGeometry(); GeometryAndFilters(); Publication(); Lifetime(); Measure();
        Console.WriteLine("Resident shape queries: CPU overlap/cast/contact/rest contracts, complete geometry, filters, lifetime and warmed allocation passed.");
    }
    private static int Query(Store store, Query query, Span<Store.ShapeQueryHit> hits, ReadOnlySpan<ulong> exclusions = default)
    { Span<int> counts = stackalloc int[1]; store.QueryShapes([query], exclusions, counts, hits); return counts[0]; }
    private static Store.ShapeHandle Target(Store store, Shape shape, Vector2 position = default, uint layer = 1, bool sensor = false)
    { var body = store.Add(new(PhysicsServer.BodyMode.Static, position, 0, default, 0)); return store.AddShape(body, shape, layer: layer, mask: 0, sensor: sensor); }
    private static void CompareCPU()
    {
        using var circle = new CircleShape { Radius = 10 }; using var box = new RectangleShape { Size = new(20, 20) };
        using var capsule = new CapsuleShape { Radius = 6, Height = 30 };
        Shape[] shapes = [circle, box, capsule];
        using var store = new Store(); var space = PhysicsServer.SpaceCreate(); var bodies = new RID[3];
        using var parameters = new PhysicsShapeQueryParameters();
        var leases = new Store.QueryGeometry[3]; Span<Store.ShapeQueryHit> hits = stackalloc Store.ShapeQueryHit[16];
        Vector2[] offsets = [new(0, -18), new(8, -17), new(0, -40), new(60, -40), new(0, -70), new(0, -30)];
        try
        {
            var direct = PhysicsServer.SpaceGetDirectState(space);
            for (var i = 0; i < shapes.Length; i++)
            {
                var position = new Vector2(i * 200, 0); var id = PhysicsServer.BodyCreate(); bodies[i] = id;
                PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(id, shapes[i].GetRID());
                PhysicsServer.BodySetTransform(id, new(0, position)); PhysicsServer.BodySetCollisionLayer(id, 1u << i); PhysicsServer.BodySetCollisionMask(id, 0); PhysicsServer.BodySetSpace(id, space);
                store.SetQueryIdentity(Target(store, shapes[i], position, 1u << i), (ulong)id.GetID(), 0); leases[i] = store.RetainQueryGeometry(shapes[i]);
            }
            for (var a = 0; a < 3; a++) for (var b = 0; b < 3; b++) for (var sample = 0; sample < offsets.Length; sample++)
                    {
                        var position = new Vector2(b * 200, 0) + offsets[sample]; var motion = sample >= 2 ? new Vector2(0, 80) : Vector2.Zero; var margin = sample == 5 ? 3 : 0;
                        parameters.Shape = shapes[a]; parameters.Transform = new(0, position); parameters.Motion = motion; parameters.Margin = margin; parameters.CollisionMask = 1u << b;
                        var query = new Query(leases[a], new(0, position), motion, margin, Mask: 1u << b, Limit: 16);
                        var expected = direct.IntersectShape(parameters); var count = Query(store, query, hits);
                        Check(count == expected.Length, $"Overlap {a}/{b}/{sample}: GPU={count}, CPU={expected.Length}");
                        if (count > 0) Check(hits[0].Collider == (ulong)bodies[b].GetID() && hits[0].LogicalShape == 0, "Logical shape query identity");
                        var fractions = direct.CastMotion(parameters); count = Query(store, query with { Mode = Mode.Cast }, hits);
                        var safe = count == 0 ? 1 : hits[0].SafeFraction; var unsafeFraction = count == 0 ? 1 : hits[0].UnsafeFraction;
                        Check(MathF.Abs(safe - fractions.SafeFraction) < .025f && MathF.Abs(unsafeFraction - fractions.UnsafeFraction) < .025f, $"Cast {a}/{b}/{sample}: GPU={safe}/{unsafeFraction}, CPU={fractions}");
                        if (count > 0) Check(safe >= 0 && unsafeFraction > safe && unsafeFraction - safe <= 1f / 256 + 1e-6f, "Eight-refinement bracket");
                        var contacts = direct.CollideShape(parameters); count = Query(store, query with { Mode = Mode.Contacts }, hits);
                        Check((count > 0) == (contacts.Length > 0), $"Contacts {a}/{b}/{sample}: GPU={count}, CPU={contacts.Length / 2}");
                        for (var i = 0; i < count; i++) Check(hits[i].QueryPoint.IsFinite() && hits[i].ColliderPoint.IsFinite() && MathF.Abs(hits[i].Normal.Length() - 1) < .001f, "Finite contact surface pair and unit normal");
                        var rest = direct.GetRestInfo(parameters); count = Query(store, query with { Mode = Mode.Rest }, hits);
                        Check((count > 0) == (rest is not null), "Rest membership");
                        if (rest is { } result)
                        {
                            // Contact feature order is internal; equal-depth faces need not choose the same endpoint or normal.
                            var depth = float.NegativeInfinity; for (var i = 0; i < contacts.Length; i += 2) depth = MathF.Max(depth, (contacts[i] - contacts[i + 1]).Dot(-result.Normal));
                            Check(MathF.Abs(hits[0].Depth - depth) <= 1.2f, $"Rest depth {a}/{b}/{sample}: {hits[0].Depth} vs {depth}");
                            var local = hits[0].ColliderPoint - new Vector2(b * 200, 0); var normal = hits[0].Normal;
                            if (b == 0) { Check(MathF.Abs(local.Length() - 10) < .01f, "Rest circle surface"); Near(normal, local.Normalized(), .001f, "Rest circle outward normal"); }
                            else if (b == 2)
                            {
                                var center = new Vector2(0, Math.Clamp(local.Y, -9, 9));
                                Check(MathF.Abs(local.DistanceTo(center) - 6) < .01f, "Rest capsule surface"); Near(normal, (local - center).Normalized(), .001f, "Rest capsule outward normal");
                            }
                            else Check(MathF.Abs(local.X) <= 10.01f && MathF.Abs(local.Y) <= 10.01f && MathF.Min(MathF.Abs(MathF.Abs(local.X) - 10), MathF.Abs(MathF.Abs(local.Y) - 10)) < .01f, "Rest box surface");
                            Near(hits[0].Velocity, result.LinearVelocity, .001f, "Static point velocity");
                        }
                    }
        }
        finally { foreach (var lease in leases) lease?.Dispose(); foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
    }
    private static void SpecialGeometry()
    {
        var polygon = new Vector2[12]; for (var i = 0; i < polygon.Length; i++) polygon[i] = new Vector2(20, 0).Rotated(-i * Mathf.Tau / polygon.Length);
        Shape[] shapes = [new CircleShape { Radius = 10 }, new ConvexPolygonShape { Points = polygon },
            new ConcavePolygonShape { Segments = [new(-15, -15), new(15, -15), new(15, -15), new(15, 15), new(15, 15), new(-15, 15)] },
            new SegmentShape { A = new(-20, 0), B = new(20, 0) }, new SeparationRayShape { Length = 20 }, new WorldBoundaryShape()];
        using var store = new Store(); var space = PhysicsServer.SpaceCreate(); var bodies = new RID[shapes.Length]; var leases = new Store.QueryGeometry[shapes.Length];
        using var parameters = new PhysicsShapeQueryParameters(); Span<Store.ShapeQueryHit> hits = stackalloc Store.ShapeQueryHit[16];
        Vector2[] positions = [new(0, -25), new(0, -10), new(15, -10), new(45, 0), new(0, 25), new(0, 10)];
        try
        {
            var direct = PhysicsServer.SpaceGetDirectState(space);
            for (var i = 0; i < shapes.Length; i++)
            {
                bodies[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(bodies[i], PhysicsServer.BodyMode.Static);
                PhysicsServer.BodyAddShape(bodies[i], shapes[i].GetRID()); PhysicsServer.BodySetCollisionLayer(bodies[i], 1u << i); PhysicsServer.BodySetSpace(bodies[i], space);
                store.SetQueryIdentity(Target(store, shapes[i], layer: 1u << i), (ulong)bodies[i].GetID(), 0); leases[i] = store.RetainQueryGeometry(shapes[i]);
            }
            for (var slide = 0; slide < 2; slide++)
            {
                ((SeparationRayShape)shapes[4]).SlideOnSlope = slide != 0;
                for (var a = 0; a < shapes.Length; a++) for (var b = 0; b < shapes.Length; b++) for (var sample = 0; sample < positions.Length; sample++)
                        {
                            var motion = sample is 0 or 2 ? new Vector2(0, 50) : sample == 3 ? new Vector2(-80, 0) : default;
                            var angle = sample == 2 ? .37f : 0; var actual = Box2D.NET.B2MathFunction.b2MakeRot(angle);
                            parameters.Shape = shapes[a]; parameters.Transform = new(angle, positions[sample]); parameters.Motion = motion; parameters.CollisionMask = 1u << b;
                            var q = new Query(leases[a], new(MathF.Atan2(actual.s, actual.c), positions[sample]), motion, Mask: 1u << b, Limit: 16);
                            var count = Query(store, q, hits); var expected = direct.IntersectShape(parameters).Length;
                            Check(count == expected, $"Special overlap {a}/{b}/{sample}: {count} vs {expected}");
                            count = Query(store, q with { Mode = Mode.Contacts }, hits); var contacts = direct.CollideShape(parameters);
                            Check((count > 0) == (contacts.Length > 0), $"Special contact {a}/{b}/{sample}: {count} vs {contacts.Length / 2}");
                            count = Query(store, q with { Mode = Mode.Rest }, hits);
                            Check((count > 0) == (direct.GetRestInfo(parameters) is not null), $"Special rest {a}/{b}/{sample}");
                            if (count > 0) Check(hits[0].Normal.IsFinite() && MathF.Abs(hits[0].Normal.Length() - 1) < .001f, "Special normal is a finite unit direction");
                            count = Query(store, q with { Mode = Mode.Cast }, hits); var cast = direct.CastMotion(parameters); var safe = count == 0 ? 1 : hits[0].SafeFraction;
                            Check(MathF.Abs(safe - cast.SafeFraction) <= .035f, $"Special cast {a}/{b}/{sample}: {safe} vs {cast.SafeFraction}");
                        }
            }
        }
        finally { foreach (var lease in leases) lease?.Dispose(); foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); foreach (var shape in shapes) shape.Dispose(); }
    }
    private static void GeometryAndFilters()
    {
        using var store = new Store(); using var circle = new CircleShape { Radius = 10 }; using var probe = store.RetainQueryGeometry(circle);
        ulong[] identities = [100, 3, 7, 3];
        for (var i = 0; i < identities.Length; i++) store.SetQueryIdentity(Target(store, circle, layer: 1u << 31, sensor: i == 2), identities[i], i == 3 ? 1 : 0);
        Span<Store.ShapeQueryHit> hits = stackalloc Store.ShapeQueryHit[4];
        var query = new Query(probe, new(0, new(0, -18)), Mask: 1u << 31, Areas: true, Limit: 2);
        Check(Query(store, query, hits) == 2 && hits[0].Collider == 3 && hits[0].LogicalShape == 0 && hits[1].Collider == 3 && hits[1].LogicalShape == 1, "Caps follow logical order, retaining distinct collider slots.");
        Check(Query(store, query with { Bodies = false }, hits) == 1 && hits[0].Collider == 7, "Sensor-only filter");
        Check(Query(store, query with { ExclusionStart = 1, ExclusionCount = 2 }, hits, [0, 3, 7]) == 1 && hits[0].Collider == 100, "Explicit exclusions, high layer bit and reciprocal mask zero");
        hits.Fill(new() { Collider = 555 }); Check(Query(store, query with { Pose = new(0, new(1000, 0)) }, hits) == 0 && hits[0].Collider == 555, "Miss preserves caller tails");
    }
    private static void Publication()
    {
        using var store = new Store(); using var circle = new CircleShape { Radius = 10 }; using var probe = store.RetainQueryGeometry(circle);
        var body = store.Add(new(PhysicsServer.BodyMode.Kinematic, new(100, 0), 0, new(10, 20), 2, CenterOfMass: new(2, 3)));
        var shape = store.AddShape(body, circle); store.SetQueryIdentity(shape, 7, 0);
        Span<Store.ShapeQueryHit> hits = stackalloc Store.ShapeQueryHit[4];
        var query = new Query(probe, new(0, new(100, -18)), Mode: Mode.Rest, Limit: 1);
        Check(Query(store, query, hits) == 1, "Surface rest hit");
        var offset = hits[0].ColliderPoint - new Vector2(102, 3);
        Near(hits[0].Velocity, new Vector2(10, 20) + 2 * new Vector2(-offset.Y, offset.X), .001f, "Point velocity includes angular surface motion about custom COM");
        store.SetShapeFilter(shape, 1, 0, true);
        Check(Query(store, query with { Bodies = false, Areas = true }, hits) == 1 && hits[0].Velocity == Vector2.Zero, "Area rest points have no body velocity");
        store.SetShapeFilter(shape, 1, 0, false);
        var deeper = Target(store, circle, new(100, -8)); store.SetQueryIdentity(deeper, 99, 0);
        Check(Query(store, query, hits) == 1 && hits[0].Collider == 99 && hits[0].Depth > 9.9f, "Rest chooses deeper geometry over a lower logical identity");
        Span<Query> batch = [query, query with { Mode = Mode.Intersect, Limit = 2 }, query with { Limit = 0 }];
        Span<int> counts = stackalloc int[4]; counts.Fill(777); hits.Fill(new() { Collider = 777 });
        store.QueryShapes(batch, [], counts, hits);
        Check(counts[0] == 1 && counts[1] == 2 && counts[2] == 0 && counts[3] == 777 && hits[0].Collider == 99 && hits[1].Collider == 7 && hits[2].Collider == 99 && hits[3].Collider == 777, "Mixed mode/cap batches preserve segment offsets and unwritten tails");
        Span<Store.QueryHit> points = stackalloc Store.QueryHit[2];
        store.Query([new(new(100, 0), Limit: 2)], [], counts, points);
        Check(counts[0] == 2 && Query(store, query, hits) == 1 && hits[0].Collider == 99, "Ray/point and shape queries can alternate shared buffers with different record strides");
    }
    private static void Lifetime()
    {
        using var store = new Store(); using var target = new RectangleShape { Size = new(10, 10) }; using var circle = new CircleShape { Radius = 2 };
        Target(store, target); using var probe = store.RetainQueryGeometry(circle); Span<Store.ShapeQueryHit> hits = stackalloc Store.ShapeQueryHit[1];
        var query = new Query(probe, new(0, new(0, -12)), Limit: 1);
        Check(Query(store, query, hits) == 0, "Standalone query shape is not a world collider");
        circle.Radius = 10; Check(Query(store, query, hits) == 1, "Shared query resource edits upload new geometry");
        Check(store.Count == 1 && store.ShapeCount == 1, "Query geometry does not create simulation bodies or shapes");
        Reject<ArgumentOutOfRangeException>(() => Query(store, query with { Margin = -1 }, new Store.ShapeQueryHit[1]));
        Reject<ArgumentException>(() => Query(store, query with { Pose = new(0, new(2, 1), 0, default) }, new Store.ShapeQueryHit[1]));
        Reject<ArgumentException>(() => Query(store, query with { Limit = 2 }, new Store.ShapeQueryHit[1]));
        Reject<InvalidOperationException>(() => Task.Run(() => Query(store, query, new Store.ShapeQueryHit[1])).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(probe.Dispose).GetAwaiter().GetResult());
        probe.Dispose(); Reject<ObjectDisposedException>(() => Query(store, query, new Store.ShapeQueryHit[1]));
        using var next = store.RetainQueryGeometry(circle); Check(Query(store, query with { Geometry = next }, hits) == 1, "Lease reuse reloads query geometry without stale handles");
        circle.Dispose(); Reject<ObjectDisposedException>(() => Query(store, query with { Geometry = next }, new Store.ShapeQueryHit[1]));
        store.Dispose(); next.Dispose(); Reject<ObjectDisposedException>(() => Query(store, query, new Store.ShapeQueryHit[1]));
    }
    private static void Measure()
    {
        using var store = new Store(); using var circle = new CircleShape { Radius = 2 }; using var probe = store.RetainQueryGeometry(circle);
        const int population = 65536, batch = 256;
        for (var i = 0; i < population; i++) Target(store, circle, new(i % 256 * 10, i / 256 * 10));
        var queries = new Query[batch]; var counts = new int[batch]; var hits = new Store.ShapeQueryHit[batch];
        for (var i = 0; i < batch; i++) queries[i] = new(probe, new(0, new(i * 10, 997)), Mode: Mode.Rest, Limit: 1);
        for (var i = 0; i < 96; i++) store.QueryShapes(queries, [], counts, hits);
        var samples = new double[128]; var upload = store.UploadBytes; var download = store.ReadbackBytes; var wait = store.WaitMS;
        var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < samples.Length; i++) { var start = Stopwatch.GetTimestamp(); store.QueryShapes(queries, [], counts, hits); samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, $"Warm shape queries allocate zero managed bytes: {owner}/{all}");
        for (var i = 0; i < batch; i++) { Check(counts[i] == 1, "Complete workload query result"); Near(hits[i].ColliderPoint, new(i * 10, 998), .001f, "Complete workload point"); }
        Array.Sort(samples);
        Console.WriteLine($"Resident shape queries: {population} shapes, {batch} rest queries/batch, 96 warmup/128 samples, p50/p95/p99={samples[64]:F4}/{samples[121]:F4}/{samples[126]:F4} ms, wait={(store.WaitMS - wait) / 128:F4}; {owner}/{all} owner/all-thread B, upload={(store.UploadBytes - upload) / 128}, readback={(store.ReadbackBytes - download) / 128} B/batch; {store.Driver}, {store.DeviceName}.");
    }
    private static void Near(Vector2 a, Vector2 b, float tolerance, string message) => Check(a.IsFinite() && a.DistanceTo(b) <= tolerance, $"{message}: {a} vs {b}");
    private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}"); }
}
