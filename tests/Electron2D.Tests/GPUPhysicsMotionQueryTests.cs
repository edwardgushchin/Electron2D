using System.Diagnostics;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Query = Electron2D.GPUPhysicsBodyStore.MotionQuery;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class GPUPhysicsMotionQueryTests
{
    internal static void Run()
    {
        CompareCPU(); ShapesAndRays(); CompoundRays(); FiltersAndLifetime(); PointVelocityAndBatch(); Measure();
        Console.WriteLine("Resident body motion: CPU recovery/sweep/one-way semantics, identity, reciprocal filters, exceptions, unchanged poses and warmed allocation passed.");
    }
    private static Store.BodyHandle Body(Store s, Vector2 position = default, Mode mode = Mode.Static) => s.Add(new(mode, position, 0, default, 0));
    private static Store.MotionQueryResult Test(Store s, Query query, ReadOnlySpan<ulong> bodies = default, ReadOnlySpan<ulong> objects = default)
    { Span<Store.MotionQueryResult> result = stackalloc Store.MotionQueryResult[1]; s.TestMotion([query], bodies, objects, result); return result[0]; }
    private static void CompareCPU()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 10 }; using var floor = new RectangleShape { Size = new(200, 20) };
        var space = PhysicsServer.SpaceCreate(); var cpuMover = PhysicsServer.BodyCreate(); var cpuFloor = PhysicsServer.BodyCreate();
        var mover = Body(s, new(500, 500)); var obstacle = Body(s, new(0, 100));
        s.SetQueryIdentity(s.AddShape(mover, circle), (ulong)cpuMover.GetID(), 0);
        var target = s.AddShape(obstacle, floor); s.SetQueryIdentity(target, (ulong)cpuFloor.GetID(), 0);
        using var parameters = new PhysicsTestMotionParameters(); using var result = new PhysicsTestMotionResult();
        try
        {
            PhysicsServer.BodySetMode(cpuMover, Mode.Static); PhysicsServer.BodyAddShape(cpuMover, circle.GetRID()); PhysicsServer.BodySetTransform(cpuMover, new(0, new(500, 500))); PhysicsServer.BodySetSpace(cpuMover, space);
            PhysicsServer.BodySetMode(cpuFloor, Mode.Static); PhysicsServer.BodyAddShape(cpuFloor, floor.GetRID()); PhysicsServer.BodySetTransform(cpuFloor, new(0, new(0, 100))); PhysicsServer.BodySetSpace(cpuFloor, space);
            Vector2[] positions = [Vector2.Zero, new(0, 50), new(0, 82), new(0, 95), new(0, 130), new(200, 50)];
            foreach (var oneWay in new[] { false, true }) foreach (var margin in new[] { .08f, 3f })
                {
                    s.SetShapeOneWay(target, new(oneWay, Vector2.Down, margin)); PhysicsServer.BodySetShapeAsOneWayCollision(cpuFloor, 0, oneWay, margin);
                    foreach (var position in positions) foreach (var motion in new[] { Vector2.Zero, new Vector2(0, 120), new Vector2(0, -100) }) foreach (var report in new[] { false, true })
                            {
                                parameters.From = new(0, position); parameters.Motion = motion; parameters.RecoveryAsCollision = report;
                                var expected = PhysicsServer.BodyTestMotion(cpuMover, parameters, result);
                                var actual = Test(s, new(mover, parameters.From, motion, RecoveryAsCollision: report));
                                var label = $"oneWay={oneWay}, margin={margin}, from={position}, motion={motion}, recovery={report}";
                                Check(actual.Collided == expected, $"Motion membership {label}: {actual.Collided} vs {expected}");
                                Near(actual.Travel, result.GetTravel(), .8f, $"Travel {label}"); Near(actual.Remainder, result.GetRemainder(), .8f, $"Remainder {label}");
                                Check(MathF.Abs(actual.SafeFraction - result.GetCollisionSafeFraction()) < .008f && MathF.Abs(actual.UnsafeFraction - result.GetCollisionUnsafeFraction()) < .008f, $"Fractions {label}");
                                if (expected)
                                {
                                    Check(actual.Collider == (ulong)result.GetColliderRID().GetID() && actual.LocalShape == 0 && actual.ColliderShape == 0, "Collider/local logical identities");
                                    Near(actual.Normal, result.GetCollisionNormal(), .001f, "Collider outward normal");
                                    Near(actual.Point, result.GetCollisionPoint(), .1f, "Collider surface point");
                                    Check(MathF.Abs(actual.Depth - result.GetCollisionDepth()) < 1, "Depth tolerance covers the one-unit contact advance.");
                                }
                                else Check(actual.Collider == 0 && actual.Object == 0 && actual.Point == Vector2.Zero && actual.Normal == Vector2.Zero && actual.Velocity == Vector2.Zero && actual.Depth == 0, "Miss clears every collision field.");
                            }
                }
            Span<Store.Snapshot> snapshots = stackalloc Store.Snapshot[2]; s.Read([mover, obstacle], snapshots);
            Near(snapshots[0].Position, new(500, 500), 0, "Tests do not teleport their owner"); Near(snapshots[1].Position, new(0, 100), 0, "Tests do not move targets");
            Check(PhysicsServer.BodyGetTransform(cpuMover).Origin == new Vector2(500, 500), "CPU query preserves its owner pose.");
        }
        finally { PhysicsServer.FreeRID(cpuMover); PhysicsServer.FreeRID(cpuFloor); PhysicsServer.FreeRID(space); }
    }
    private static void ShapesAndRays()
    {
        var contour = new Vector2[12]; for (var i = 0; i < contour.Length; i++) contour[i] = new Vector2(20, 0).Rotated(-i * Mathf.Tau / contour.Length);
        Shape[] shapes = [new RectangleShape { Size = new(20, 16) }, new CapsuleShape { Radius = 5, Height = 24 },
            new SegmentShape { A = new(-9, 0), B = new(9, 0) }, new ConvexPolygonShape { Points = contour },
            new ConcavePolygonShape { Segments = [new(-10, -10), new(10, -10), new(10, -10), new(10, 10)] },
            new SeparationRayShape { Length = 20 }];
        using var floor = new RectangleShape { Size = new(200, 20) }; using var slope = new SegmentShape { A = new(-100, 20), B = new(100, -20) };
        using var s = new Store(); var space = PhysicsServer.SpaceCreate(); var cpuFloor = PhysicsServer.BodyCreate(); var obstacle = Body(s, new(0, 100));
        var target = s.AddShape(obstacle, floor); PhysicsServer.BodySetMode(cpuFloor, Mode.Static); PhysicsServer.BodyAddShape(cpuFloor, floor.GetRID()); PhysicsServer.BodySetTransform(cpuFloor, new(0, new(0, 100))); PhysicsServer.BodySetSpace(cpuFloor, space);
        using var parameters = new PhysicsTestMotionParameters(); using var result = new PhysicsTestMotionResult();
        try
        {
            for (var family = 0; family < shapes.Length; family++)
            {
                var mover = Body(s, new(500, 500)); var own = s.AddShape(mover, shapes[family], new(.13f, new(3, -2)));
                var cpuMover = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(cpuMover, Mode.Static); PhysicsServer.BodyAddShape(cpuMover, shapes[family].GetRID(), new(.13f, new(3, -2))); PhysicsServer.BodySetTransform(cpuMover, new(0, new(500, 500))); PhysicsServer.BodySetSpace(cpuMover, space);
                try
                {
                    for (var slide = 0; slide < (family == 5 ? 2 : 1); slide++)
                    {
                        if (shapes[family] is SeparationRayShape ray) ray.SlideOnSlope = slide != 0;
                        foreach (var collide in new[] { false, true }) foreach (var recovery in new[] { false, true }) foreach (var height in new[] { 0f, 75f, 95f })
                                {
                                    const float angle = .2f; var actual = Box2D.NET.B2MathFunction.b2MakeRot(angle);
                                    parameters.From = new(angle, new(0, height)); parameters.Motion = new(0, 120); parameters.RecoveryAsCollision = recovery; parameters.CollideSeparationRay = collide;
                                    var expected = PhysicsServer.BodyTestMotion(cpuMover, parameters, result);
                                    var hit = Test(s, new(mover, new(MathF.Atan2(actual.s, actual.c), new(0, height)), parameters.Motion, RecoveryAsCollision: recovery, CollideSeparationRay: collide));
                                    var label = $"family={family}, slide={slide}, ray={collide}, recovery={recovery}, height={height}";
                                    Check(hit.Collided == expected, $"Geometry membership {label}: {hit.Collided} vs {expected}");
                                    Near(hit.Travel, result.GetTravel(), 1.2f, $"Geometry travel {label}");
                                    if (hit.Collided) Check(hit.Normal.IsFinite() && MathF.Abs(hit.Normal.Length() - 1) < .001f && hit.Point.IsFinite(), "Geometry motion contact remains finite and unit length");
                                }
                    }
                }
                finally { s.Remove(mover); PhysicsServer.FreeRID(cpuMover); }
            }
            s.RemoveShape(target); s.AddShape(obstacle, slope); PhysicsServer.BodySetShape(cpuFloor, 0, slope.GetRID());
            using var circle = new CircleShape { Radius = 10 }; var actor = Body(s); s.AddShape(actor, circle);
            var hitSlope = Test(s, new(actor, new(0, new(-80, 106)), new(100, 0)));
            Check(hitSlope.Collided && hitSlope.Normal.X < -.1f && hitSlope.Normal.Y < -.9f && hitSlope.Travel.X < 5, "An inward tangent sweep cannot tunnel through a slope");
        }
        finally { PhysicsServer.FreeRID(cpuFloor); PhysicsServer.FreeRID(space); foreach (var shape in shapes) shape.Dispose(); }
    }
    private static void CompoundRays()
    {
        var points = new Vector2[12]; for (var i = 0; i < points.Length; i++) points[i] = new Vector2(30, 0).Rotated(i * Mathf.Tau / points.Length);
        using var polygon = new ConvexPolygonShape { Points = points }; using var ray = new SeparationRayShape { Length = 70, SlideOnSlope = true };
        foreach (var reverse in new[] { false, true })
        {
            using var s = new Store(); var space = PhysicsServer.SpaceCreate(); var cpuMover = PhysicsServer.BodyCreate(); var cpuTarget = PhysicsServer.BodyCreate();
            var mover = Body(s); var target = Body(s); s.AddShape(mover, reverse ? polygon : ray); s.AddShape(target, reverse ? ray : polygon);
            using var parameters = new PhysicsTestMotionParameters { RecoveryAsCollision = true, CollideSeparationRay = true }; using var result = new PhysicsTestMotionResult();
            try
            {
                PhysicsServer.BodySetMode(cpuMover, Mode.Static); PhysicsServer.BodySetMode(cpuTarget, Mode.Static);
                PhysicsServer.BodyAddShape(cpuMover, (reverse ? (Shape)polygon : ray).GetRID()); PhysicsServer.BodyAddShape(cpuTarget, (reverse ? (Shape)ray : polygon).GetRID());
                PhysicsServer.BodySetSpace(cpuMover, space); PhysicsServer.BodySetSpace(cpuTarget, space);
                foreach (var x in new[] { -11f, 3f, 12f }) foreach (var y in new[] { -50f, -5f, 5f }) foreach (var motion in new[] { Vector2.Zero, new Vector2(0, 10), new Vector2(10, 0) })
                        {
                            parameters.From = new(0, new(x, y)); parameters.Motion = motion;
                            var expected = PhysicsServer.BodyTestMotion(cpuMover, parameters, result);
                            var actual = Test(s, new(mover, parameters.From, motion, RecoveryAsCollision: true, CollideSeparationRay: true));
                            var label = $"compound/ray reverse={reverse}, from={parameters.From.Origin}, motion={motion}";
                            Check(actual.Collided == expected, $"{label}: GPU={actual.Collided}, CPU={expected}"); Near(actual.Travel, result.GetTravel(), 1, label);
                            if (MathF.Abs(y) < 10) Check(!actual.Collided && actual.Travel == motion, "A ray origin inside the whole contour cannot enter an internal partition seam.");
                        }
            }
            finally { PhysicsServer.FreeRID(cpuMover); PhysicsServer.FreeRID(cpuTarget); PhysicsServer.FreeRID(space); }
        }
    }
    private static void PointVelocityAndBatch()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 10 }; using var floor = new RectangleShape { Size = new(200, 20) };
        var mover = Body(s); s.AddShape(mover, circle);
        var target = s.Add(new(Mode.Kinematic, new(0, 100), 0, new(10, 20), 2, CenterOfMass: new(2, 3)));
        s.SetQueryIdentity(s.AddShape(target, floor), 12, 4);
        var query = new Query(mover, Transform.Identity, new(0, 120)); var hit = Test(s, query);
        var offset = hit.Point - new Vector2(2, 103);
        Near(hit.Velocity, new Vector2(10, 20) + 2 * new Vector2(-offset.Y, offset.X), .001f, "Collider point velocity includes surface rotation about custom COM");
        Span<Query> batch = [query, query with { BodyExclusionCount = 1 }, query with { From = new(0, new(0, 82)), Motion = Vector2.Zero }];
        Span<Store.MotionQueryResult> outputs = stackalloc Store.MotionQueryResult[4]; outputs.Fill(new() { Collider = 777 });
        s.TestMotion(batch, [12], [], outputs);
        Check(outputs[0].Collided && !outputs[1].Collided && outputs[1].Collider == 0 && outputs[1].Travel == query.Motion && !outputs[2].Collided && outputs[2].Travel.Y < 0 && outputs[3].Collider == 777, "Batch hit/miss/recovery results clear collision fields and preserve the caller tail");
    }
    private static void FiltersAndLifetime()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 10 }; using var floor = new RectangleShape { Size = new(200, 20) };
        var a = Body(s, new(500, 500)); var b = Body(s, new(0, 100));
        var own = s.AddShape(a, circle, new(0, new(-500, 0))); s.SetQueryIdentity(own, 1, 3);
        own = s.AddShape(a, circle); s.SetQueryIdentity(own, 1, 9);
        var target = s.AddShape(b, floor); s.SetQueryIdentity(target, 99, 7, objectID: 1234);
        var q = new Query(a, Transform.Identity, new(0, 120)); var result = Test(s, q);
        Check(result.Collided && result.Collider == 99 && result.Object == 1234 && result.LocalShape == 9 && result.ColliderShape == 7, "Own local poses and logical/object identities");
        Check(!Test(s, q with { BodyExclusionStart = 1, BodyExclusionCount = 1 }, [0, 99]).Collided, "Explicit collider exclusion range");
        Check(!Test(s, q with { ObjectExclusionStart = 1, ObjectExclusionCount = 1 }, objects: [0, 1234]).Collided, "Explicit object exclusion range");
        s.SetCollisionException(b, a, true); Check(!Test(s, q).Collided, "Reverse directed exception excludes body motion");
        s.SetCollisionException(b, a, false); s.SetCollisionException(a, b, true); Check(!Test(s, q).Collided, "Forward exception excludes body motion"); s.SetCollisionException(a, b, false);
        var joint = s.AddJoint(new(PhysicsServer.JointType.Pin, a, b, Transform.Identity, Transform.Identity));
        Check(Test(s, q).Collided, "Joint collision vetoes do not change explicit body-query exceptions"); s.RemoveJoint(joint);
        s.SetShapeFilter(target, 1, 0, false); Check(!Test(s, q).Collided, "Reciprocal target mask is required"); s.SetShapeFilter(target, 1, 1, true);
        Check(!Test(s, q).Collided, "Sensors never block body motion"); s.SetShapeFilter(target, 1, 1, false);
        s.SetQueryIdentity(target, 99, 7); Check(Test(s, q with { ObjectExclusionCount = 1 }, objects: [0]).Collided, "Object zero cannot exclude a server-only collider");
        Reject<ArgumentOutOfRangeException>(() => Test(s, q with { Margin = -1 }));
        Reject<ArgumentOutOfRangeException>(() => Test(s, q with { BodyExclusionCount = 1 }));
        Reject<ArgumentException>(() => Test(s, q with { From = new(0, new(2, 1), 0, default) }));
        Reject<InvalidOperationException>(() => Task.Run(() => Test(s, q)).GetAwaiter().GetResult());
        s.Remove(b); var next = Body(s, new(0, 100)); target = s.AddShape(next, floor); s.SetQueryIdentity(target, 100, 8);
        Check(Test(s, q).Collider == 100 && Test(s, q).ColliderBodyGeneration == next.Generation, "Reuse does not retain old exclusions or identity");
        s.Remove(a); Reject<ArgumentException>(() => Test(s, q));
        var empty = Body(s); var miss = Test(s, new(empty, Transform.Identity, new(9, 10)));
        Check(!miss.Collided && miss.Travel == new Vector2(9, 10) && miss.SafeFraction == 1 && miss.UnsafeFraction == 1, "An owner without shapes travels freely");
        s.Dispose(); Reject<ObjectDisposedException>(() => Test(s, q));
    }
    private static void Measure()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 2 }; using var floor = new RectangleShape { Size = new(8, 4) };
        const int population = 65536, batch = 256;
        for (var i = 0; i < population; i++) s.AddShape(Body(s, new(i % 256 * 20, i / 256 * 20)), floor);
        var mover = Body(s, new(-1000, 0)); s.AddShape(mover, circle);
        var queries = new Query[batch]; var results = new Store.MotionQueryResult[batch];
        for (var i = 0; i < batch; i++) queries[i] = new(mover, new(0, new(i * 20, 990)), new(0, 12));
        for (var i = 0; i < 96; i++) s.TestMotion(queries, [], [], results);
        var times = new double[128]; var up = s.UploadBytes; var down = s.ReadbackBytes; var wait = s.WaitMS;
        var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < times.Length; i++) { var start = Stopwatch.GetTimestamp(); s.TestMotion(queries, [], [], results); times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, $"Warmed body motion allocates zero managed bytes: {owner}/{all}");
        for (var i = 0; i < batch; i++) Check(results[i].Collided && results[i].Normal.Y < -.99f && results[i].Travel.Y is > 5 and < 7, "Every full-population motion result stops at its floor");
        Array.Sort(times); Console.WriteLine($"Resident body motion: {population} obstacles + 1 mover, {batch} supplied-pose tests/batch, 96 warmup/128 samples; p50/p95/p99={times[64]:F4}/{times[121]:F4}/{times[126]:F4} ms, wait={(s.WaitMS - wait) / 128:F4} ms, {owner}/{all} owner/all-thread managed B, upload={(s.UploadBytes - up) / 128}, readback={(s.ReadbackBytes - down) / 128} B/batch; {s.Driver}, {s.DeviceName}.");
    }
    private static void Near(Vector2 a, Vector2 b, float tolerance, string message) => Check(a.IsFinite() && a.DistanceTo(b) <= tolerance, $"{message}: {a} vs {b}");
    private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}"); }
}
