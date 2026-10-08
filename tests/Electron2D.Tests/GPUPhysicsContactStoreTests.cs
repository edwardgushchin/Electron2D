using System.Diagnostics;
using Electron2D;
using Body = Electron2D.GPUPhysicsBodyStore.BodyHandle;
using Point = Electron2D.GPUPhysicsBodyStore.ContactPoint;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class GPUPhysicsContactStoreTests
{
    internal static void Run()
    {
        VerifyPrimitivesAndEdits();
        VerifyFamilies();
        VerifyConcaveAndRays();
        VerifyFailure();
        VerifyResidency(64);
        VerifyResidency(256);
    }
    private static Body Add(GPUPhysicsBodyStore store, Vector2 position = default, float rotation = 0, Vector2 velocity = default) =>
        store.Add(new(Mode.Rigid, position, rotation, velocity, 0));

    private static void VerifyPrimitivesAndEdits()
    {
        using var store = new GPUPhysicsBodyStore();
        using var circle = new CircleShape { Radius = 1 };
        var a = Add(store); var b = Add(store, new(1.5f, 0));
        var sa = store.AddShape(a, circle); var sb = store.AddShape(b, circle);
        var points = new Point[8];
        Check(store.ReadContacts(points) == 1, "Circles produce one contact.");
        Near(new(points[0].Normal.X, points[0].Normal.Y), Vector2.Right, 0.00001f, "Circle normal points from A to B");
        Check(MathF.Abs(points[0].Normal.Z + 0.5f) < 0.00001f && points[0].Anchors == new System.Numerics.Vector4(1, 0, -1, 0), "Circle boundary anchors and signed depth.");
        var submissions = store.ContactSubmissionCount;
        store.FindContacts(); Check(store.ContactSubmissionCount == submissions, "Unchanged contacts stay resident.");
        store.SetPose(b, new(1.9f, 1.9f), 0);
        Check(store.FindPairs() == 1 && store.FindContacts() == 0, "Rounded corners reject an AABB false positive.");
        store.SetPose(b, new(2.25f, 0), 0);
        Check(store.FindContacts() == 0 && store.ReadContacts(points, 0.5f) == 1 && MathF.Abs(points[0].Normal.Z - 0.25f) < 0.00001f,
            "Speculative distance expands broad phase and retains positive separation.");
        store.SetShapeFilter(sb, 1, uint.MaxValue, true);
        Check(store.FindContacts(0.5f) == 0, "Sensors use exact overlap, without speculative contact events.");
        store.SetPose(b, new(1.5f, 0), 0);
        Check(store.ReadContacts(points, 0.5f) == 1 && points[0].Normal.W == 1, "Sensor contacts retain their role.");
        circle.Radius = 2;
        Check(store.ReadContacts(points) == 1 && MathF.Abs(points[0].Normal.Z + 2.5f) < 0.00001f, "Live shared geometry updates resident contacts.");
        store.SetPose(b, Vector2.Zero, 0);
        Check(store.ReadContacts(points) == 1 && float.IsFinite(points[0].Normal.X) && points[0].Normal.Z == -4, "Coincident circles retain a finite unit separating direction.");
        Reject<ArgumentOutOfRangeException>(() => store.FindContacts(float.NaN));
        Reject<ArgumentOutOfRangeException>(() => store.FindContacts(-1));
        Reject<ArgumentException>(() => store.ReadContacts([]));
        store.Remove(b); Check(store.FindContacts() == 0, "Body retirement removes contacts.");
        b = Add(store, new(1, 0)); sb = store.AddShape(b, circle);
        Check(store.ReadContacts(points) == 1 && points[0].GenerationB == sb.Generation && points[0].GenerationA == sa.Generation,
            "Contact identity follows reused shape generations.");
        using var box = new RectangleShape { Size = new(8, 12) };
        store.RemoveShape(sa); store.RemoveShape(sb);
        var boxA = store.AddShape(a, box); store.AddShape(b, box); store.SetPose(b, new(7, 0), 0);
        Check(store.ReadContacts(points) == 2 && points.Take(2).All(p => MathF.Abs(p.Normal.Z + 1) < 0.00001f), "Overlapping box faces retain two boundary points.");
        var features = points.Take(2).Select(p => (p.FeatureA, p.FeatureB)).Order().ToArray();
        store.SetPose(a, new(100, 30), 0); store.SetPose(b, new(107, 30), 0);
        Check(store.ReadContacts(points) == 2 && features.SequenceEqual(points.Take(2).Select(p => (p.FeatureA, p.FeatureB)).Order()), "Translation preserves contact features.");
        store.SetShapePose(boxA, new(0.2f, Vector2.Zero));
        Check(store.ReadContacts(points) > 0, "Rotated local placement refreshes contact geometry.");
        store.SetShapePose(boxA, Transform.Identity); store.SetPose(a, Vector2.Zero, 0); store.SetPose(b, new(8.25f, 12.25f), 0);
        Check(store.ReadContacts(points, 0.5f) == 1 && MathF.Abs(points[0].Normal.Z - MathF.Sqrt(0.125f)) < 0.0001f,
            "Separated polygon corners produce one speculative point with Euclidean separation.");
        box.Dispose(); Check(store.FindContacts() == 0, "Resource disposal removes resident contacts.");
        Console.WriteLine("Resident contacts: analytic primitives, speculative/sensor boundary, edits, identity, features and lifetime passed.");
    }

    private static void VerifyFamilies()
    {
        using var store = new GPUPhysicsBodyStore();
        var contour = new Vector2[12];
        for (var i = 0; i < contour.Length; i++) contour[i] = new Vector2(MathF.Cos(i * MathF.Tau / 12), MathF.Sin(i * MathF.Tau / 12)) * 5;
        Shape[] resources = [new CircleShape { Radius = 3 }, new CapsuleShape { Radius = 2, Height = 12 },
            new SegmentShape { A = new(-6, 0), B = new(6, 0) }, new RectangleShape { Size = new(8, 10) },
            new ConvexPolygonShape { Points = contour }, new ConvexPolygonShape { Points = contour.Reverse().ToArray() },
            new SeparationRayShape { Length = 12 }, new SeparationRayShape { Length = 12, SlideOnSlope = true },
            new ConcavePolygonShape { Segments = [new(-8, -5), new(8, -5), new(8, -5), new(8, 5)] }];
        try
        {
            var expected = new List<bool>(); var poses = new List<(Transform A, Transform B)>();
            var random = new Random(9248);
            foreach (var first in resources)
                foreach (var second in resources)
                    for (var sample = 0; sample < 20; sample++)
                    {
                        var index = expected.Count; var origin = new Vector2(index % 32 * 64, index / 32 * 64);
                        var pa = new Transform((float)random.NextDouble() * MathF.Tau, origin);
                        var pb = new Transform((float)random.NextDouble() * MathF.Tau, origin + new Vector2((float)random.NextDouble() * 24 - 12, (float)random.NextDouble() * 24 - 12));
                        store.AddShape(Add(store, pa.Origin, pa.Rotation), first);
                        store.AddShape(Add(store, pb.Origin, pb.Rotation), second);
                        expected.Add(first.Collide(pa, second, pb)); poses.Add((pa, pb));
                    }
            var points = new Point[store.FindContacts()]; store.ReadContacts(points);
            var touched = new bool[expected.Count];
            var features = new HashSet<(uint, uint, uint, uint, uint, uint)>();
            foreach (var point in points)
            {
                Check(point.ShapeA % 2 == 0 && point.ShapeB == point.ShapeA + 1, "Separate test pairs never cross their cells.");
                var index = (int)(point.ShapeA / 2); touched[index] = true;
                Check(features.Add((point.ShapeA, point.ShapeB, point.FeatureA, point.FeatureB, point.PieceA, point.PieceB)), "Contact features uniquely identify each point for history lookup.");
                Invariant(point, poses[index].A, poses[index].B, 0.001f);
            }
            for (var i = 0; i < expected.Count; i++) Check(expected[i] == touched[i], $"Pair {i} agrees with standalone collision region: expected={expected[i]}, actual={touched[i]}.");
            Console.WriteLine($"Resident contacts: {expected.Count} rotated primitive/full-contour cases agree with resource collision regions and anchor invariants.");
        }
        finally { foreach (var resource in resources) resource.Dispose(); }
    }

    private static void VerifyConcaveAndRays()
    {
        using var store = new GPUPhysicsBodyStore();
        using var terrain = new ConcavePolygonShape { Segments = [new(-20, -20), new(20, -20), new(20, -20), new(20, 20), new(20, 20), new(-20, 20), new(-20, 20), new(-20, -20)] };
        using var circle = new CircleShape { Radius = 2 };
        var a = Add(store); var b = Add(store);
        store.AddShape(a, terrain); var sb = store.AddShape(b, circle);
        Check(store.FindPairs() == 1 && store.FindContacts() == 0, "A hollow concave region has no filled interior.");
        var segments = new Vector2[400];
        for (var i = 0; i < 200; i++) { segments[2 * i] = new(-10, (i % 10) * 0.1f); segments[2 * i + 1] = new(10, (i % 10) * 0.1f); }
        terrain.Segments = segments;
        Check(store.FindContacts() == 200 && store.ContactCapacityRetries == 1, "All concave piece contacts survive output-capacity recovery.");
        var points = new Point[200]; store.ReadContacts(points);
        Check(points.Select(p => p.PieceA).Distinct().Count() == 200, "Every concave segment retains its piece identity.");
        store.RemoveShape(sb);
        using var ray = new SeparationRayShape { Length = 16 };
        store.AddShape(b, ray);
        terrain.Segments = [new(-10, 4), new(10, 8), new(-10, 10), new(10, 14)];
        foreach (var slide in new[] { false, true })
        {
            ray.SlideOnSlope = slide;
            Check(store.ReadContacts(points) == 1, "Directed ray retains only the nearest concave crossing.");
            var expected = terrain.CollideAndGetContacts(Transform.Identity, ray, Transform.Identity);
            Check(expected.Length == 2, "Reference directed contact exists.");
            Near(new(points[0].Anchors.X, points[0].Anchors.Y), expected[0], 0.0001f, "Directed target anchor");
            Near(new(points[0].Anchors.Z, points[0].Anchors.W), expected[1], 0.0001f, "Directed ray anchor");
            Invariant(points[0], Transform.Identity, Transform.Identity, 0.0001f);
        }
        store.SetPose(b, new(0, 20), 0); Check(store.FindContacts() == 0, "A ray pointing away from the terrain misses.");
        Console.WriteLine("Resident contacts: hollow concave terrain, complete piece output and directed ray slope/nearest-hit semantics passed.");
    }

    private static void VerifyFailure()
    {
        using var store = new GPUPhysicsBodyStore();
        using var capsule = new CapsuleShape { Radius = 1, Height = float.MaxValue / 4 };
        store.AddShape(Add(store), capsule); store.AddShape(Add(store), capsule);
        Reject<InvalidOperationException>(() => store.FindContacts());
        Reject<InvalidOperationException>(() => store.Step(0.01f, Vector2.Zero));
        Console.WriteLine("Nonfinite contact intermediates fail closed without CPU replay.");
    }

    private static void VerifyResidency(int side)
    {
        const int warmup = 384, samples = 256;
        var count = side * side; var expectedPairs = 2 * side * (side - 1) + 2 * (side - 1) * (side - 1);
        using var store = new GPUPhysicsBodyStore();
        using var box = new RectangleShape { Size = new(2.2f, 2.2f) };
        var bodies = new Body[count];
        for (var i = 0; i < count; i++)
        {
            bodies[i] = Add(store, new(i % side * 2, i / side * 2), velocity: new(0.25f, 0.5f));
            store.AddShape(bodies[i], box);
        }
        for (var i = 0; i < warmup; i++) { store.Step(1f / 120, Vector2.Zero); Check(store.FindContacts() == 2 * expectedPairs, "Grid contact count"); }
        var times = new double[samples]; var upload = store.UploadBytes; var readback = store.ReadbackBytes;
        var uniforms = store.UniformBytes; var wait = store.WaitMS; var geometry = store.GeometryUploadBytes; var shapes = store.ShapeUploadBytes;
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < samples; i++)
        {
            var start = Stopwatch.GetTimestamp(); store.Step(1f / 120, Vector2.Zero);
            Check(store.FindContacts() == 2 * expectedPairs, "Grid contact count"); times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated; var waitMS = (store.WaitMS - wait) / samples;
        Check(allocated == 0 && store.UploadBytes - upload == 20 * samples && store.ReadbackBytes - readback == 20 * samples,
            "Resident integration/broad/narrow stages exchange status only and allocate zero warmed managed bytes.");
        Check(store.GeometryUploadBytes == geometry && store.ShapeUploadBytes == shapes, "Moving contacts need no geometry/pose/pair/contact uploads.");
        var uniformBytes = (store.UniformBytes - uniforms) / (double)samples;
        var points = new Point[2 * expectedPairs]; var startRead = Stopwatch.GetTimestamp(); store.ReadContacts(points);
        var readMS = Stopwatch.GetElapsedTime(startRead).TotalMilliseconds;
        startRead = Stopwatch.GetTimestamp(); store.ReadContacts(points);
        var warmReadMS = Stopwatch.GetElapsedTime(startRead).TotalMilliseconds;
        var snapshots = new GPUPhysicsBodyStore.Snapshot[count];
        startRead = Stopwatch.GetTimestamp(); store.Read(bodies, snapshots);
        var poseReadMS = Stopwatch.GetElapsedTime(startRead).TotalMilliseconds;
        startRead = Stopwatch.GetTimestamp(); store.Read(bodies, snapshots);
        var warmPoseReadMS = Stopwatch.GetElapsedTime(startRead).TotalMilliseconds;
        var minDepth = float.MaxValue; var maxDepth = float.MinValue;
        foreach (var point in points)
        {
            var x = Math.Abs((int)(point.ShapeA % side) - (int)(point.ShapeB % side)); var y = Math.Abs((int)(point.ShapeA / side) - (int)(point.ShapeB / side));
            var delta = snapshots[point.ShapeB].Position - snapshots[point.ShapeA].Position;
            var expectedSeparation = MathF.Max(MathF.Abs(delta.X), MathF.Abs(delta.Y)) - 2.2f;
            // Floating-point integration can change relative spacing across exponent boundaries.
            // Test the actual device configuration analytically, without loosening contact accuracy.
            Check(x <= 1 && y <= 1 && MathF.Abs(point.Normal.Z - expectedSeparation) < 0.0001f && point.ShapeA < point.ShapeB,
                "Every grid contact matches the actual adjacent rectangle poses within 0.0001 scene units.");
            minDepth = MathF.Min(minDepth, -point.Normal.Z); maxDepth = MathF.Max(maxDepth, -point.Normal.Z);
        }
        Array.Sort(times);
        Console.WriteLine($"Resident integration+broad+narrow only: {count} bodies, {expectedPairs} pairs, {points.Length} points, {warmup} warmup, {samples} samples; p50={times[samples / 2]:F4} ms, p95={times[(int)(samples * 0.95)]:F4} ms, p99={times[(int)(samples * 0.99)]:F4} ms, wait={waitMS:F4} ms/tick, {allocated} B/tick; 20 B upload + {uniformBytes:F0} B mean uniforms + 20 B readback/tick; contact read first/warm={readMS:F4}/{warmReadMS:F4} ms, diagnostic pose read first/warm={poseReadMS:F4}/{warmPoseReadMS:F4} ms, depth range={minDepth:F6}..{maxDepth:F6} (outside samples); {store.Driver}, {store.DeviceName}, .NET {Environment.Version}.");
    }
    private static void Invariant(Point point, Transform a, Transform b, float tolerance)
    {
        var normal = new Vector2(point.Normal.X, point.Normal.Y);
        var pa = a * new Vector2(point.Anchors.X, point.Anchors.Y); var pb = b * new Vector2(point.Anchors.Z, point.Anchors.W);
        Check(normal.IsFinite() && pa.IsFinite() && pb.IsFinite() && MathF.Abs(normal.Length() - 1) < 0.0001f &&
            MathF.Abs((pb - pa).Dot(normal) - point.Normal.Z) < tolerance && point.Normal.Z <= tolerance, "Finite unit normal, signed depth and body-local anchors remain consistent.");
    }
    private static void Near(Vector2 actual, Vector2 expected, float tolerance, string message) => Check(actual.DistanceTo(expected) <= tolerance, message);
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
