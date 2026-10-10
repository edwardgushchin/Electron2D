using System.Diagnostics;
using Electron2D;
using Body = Electron2D.GPUPhysicsBodyStore.BodyHandle;
using ShapeID = Electron2D.GPUPhysicsBodyStore.ShapeHandle;
using Pair = Electron2D.GPUPhysicsBodyStore.ShapePair;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class GPUPhysicsSpatialTests
{
    internal static void Run()
    {
        VerifyGeometryAndLifecycle();
        VerifyPairs();
        VerifyFailure();
        VerifyResidency(64);
        VerifyResidency(256);
    }

    private static Body Add(GPUPhysicsBodyStore store, Vector2 position = default, Mode mode = Mode.Rigid,
        Vector2 velocity = default, float rotation = 0) => store.Add(new(mode, position, rotation, velocity, 0));

    private static void VerifyGeometryAndLifecycle()
    {
        using var store = new GPUPhysicsBodyStore();
        var body = Add(store, new(40, 60), velocity: new(2, -3), rotation: MathF.PI / 2);
        using var circle = new CircleShape { Radius = 2 };
        using var capsule = new CapsuleShape { Radius = 2, Height = 12 };
        using var segment = new SegmentShape { A = new(-3, 2), B = new(6, -4) };
        using var box = new RectangleShape { Size = new(8, 12) };
        var points = new Vector2[13];
        for (var i = 0; i < 12; i++) points[i] = new Vector2(MathF.Cos(i * MathF.Tau / 12), MathF.Sin(i * MathF.Tau / 12)) * 8;
        points[^1] = points[0];
        using var convex = new ConvexPolygonShape { Points = points };
        Vector2[] segments = [new(-8, -6), new(8, -6), new(8, -6), new(8, 6), new(8, 6), new(-8, 6)];
        using var concave = new ConcavePolygonShape { Segments = segments };
        using var ray = new SeparationRayShape { Length = 14, SlideOnSlope = true };
        Shape[] resources = [circle, capsule, segment, box, convex, concave, ray];
        Vector2[][] vertices = [[Vector2.Zero], [new(0, -4), new(0, 4)], [segment.A, segment.B],
            [new(-4, -6), new(4, -6), new(4, 6), new(-4, 6)], points, segments, [Vector2.Zero, new(0, 14)]];
        float[] radii = [2, 2, 0, 0, 0, 0, 0];
        var local = new Transform(MathF.PI / 2, new(3, 5));
        var handles = new ShapeID[resources.Length];
        for (var i = 0; i < resources.Length; i++) handles[i] = store.AddShape(body, resources[i], local);
        Check(store.FindPairs() == 0, "Shapes on one body never collide with each other.");
        for (var i = 0; i < handles.Length; i++)
            Bounds(store.ReadShapeBounds(handles[i]), BoundsOf(vertices[i], radii[i], new Transform(MathF.PI / 2, new(40, 60)) * local), "All authored shape families preserve rotated bounds");
        var uploads = store.GeometryUploadBytes;
        var shapeUploads = store.ShapeUploadBytes;
        store.Step(0.5f, Vector2.Zero);
        Bounds(store.ReadShapeBounds(handles[0]), new(34, 59.5f, 4, 4), "GPU motion updates bounds without CPU poses");
        Check(store.GeometryUploadBytes == uploads && store.ShapeUploadBytes == shapeUploads, "Motion reuses geometry and attachments.");
        store.SetShapePose(handles[0], Transform.Identity);
        Bounds(store.ReadShapeBounds(handles[0]), new(39, 56.5f, 4, 4), "Local pose edits update bounds");

        var another = Add(store, new(50, 60));
        var shared = store.AddShape(another, circle);
        store.FindPairs();
        Check(store.GeometryUploadBytes == uploads, "A second borrower does not upload shared geometry again.");
        Action<Resource> brokenObserver = _ => throw new InvalidOperationException("Observer failed after resource mutation.");
        circle.Changed += brokenObserver;
        Reject<InvalidOperationException>(() => circle.Radius = 5);
        circle.Changed -= brokenObserver;
        Bounds(store.ReadShapeBounds(handles[0]), new(36, 53.5f, 10, 10), "Revision survives a throwing resource observer");
        Bounds(store.ReadShapeBounds(shared), new(45, 55, 10, 10), "All borrowers observe one geometry revision");
        Check(store.GeometryUploadBytes - uploads == 48 + 16, "One radius revision uploads one geometry record and one vertex.");
        circle.Dispose();
        Check(store.ReadShapeBounds(shared) is null && store.ReadShapeBounds(handles[0]) is null, "Disposed borrowed geometry disappears from bounds and pairs.");
        Reject<ObjectDisposedException>(() => store.AddShape(another, circle));
        store.RemoveShape(handles[1]); store.RemoveShape(handles[0]); store.RemoveShape(handles[^1]);
        store.Remove(body);
        Check(store.ShapeCount == 1, "Body deletion removes every attachment.");
        Reject<ArgumentException>(() => store.ReadShapeBounds(handles[1]));
        var replacementBody = Add(store, new(90, 100));
        Check(replacementBody.Index == body.Index, "Test exercises body slot reuse.");
        var replacement = store.AddShape(replacementBody, box);
        Bounds(store.ReadShapeBounds(replacement), new(86, 94, 8, 12), "Reused body slots inherit no old attachment pose");
        store.RemoveShape(replacement);
        using var large = new CircleShape { Radius = 7 };
        var recreated = store.AddShape(replacementBody, large);
        Check(recreated.Index == replacement.Index && recreated.Generation != replacement.Generation, "Reused shape slots change generation.");
        Bounds(store.ReadShapeBounds(recreated), new(83, 93, 14, 14), "Coalesced remove/recreate replaces geometry and shape generations");
        Reject<ArgumentException>(() => store.SetShapeFilter(replacement, 1, 1, false));
        using var foreign = new GPUPhysicsBodyStore();
        var foreignShape = foreign.AddShape(Add(foreign), large);
        Reject<ArgumentException>(() => store.RemoveShape(foreignShape));
        Reject<ArgumentException>(() => store.SetShapePose(recreated, new(0, new(2, 1), 0, Vector2.Zero)));
        Task.Run(() => Reject<InvalidOperationException>(() => store.FindPairs())).GetAwaiter().GetResult();
        // Exercise resource/vertex slot churn across publication and device capacity growth.
        for (var i = 0; i < 70; i++) store.AddShape(Add(store, new(i * 20, 200)), box);
        Bounds(store.ReadShapeBounds(recreated), new(83, 93, 14, 14), "Device geometry growth retains prior slots");
        var submissions = store.BroadPhaseSubmissionCount;
        store.FindPairs();
        Check(store.BroadPhaseSubmissionCount == submissions, "Unchanged pair reads do not resubmit work.");
        Console.WriteLine("Resident geometry: seven families, resource sharing/revision/disposal, motion, reuse and growth passed.");
    }

    private static void VerifyPairs()
    {
        using var store = new GPUPhysicsBodyStore();
        using var circle = new CircleShape { Radius = 1.5f };
        const int count = 96;
        var bodies = new Body[count]; var shapes = new ShapeID[count];
        var positions = new Vector2[count]; var modes = new Mode[count];
        var layers = new uint[count]; var masks = new uint[count]; var sensors = new bool[count];
        var random = new Random(8192);
        for (var i = 0; i < count; i++)
        {
            bodies[i] = Add(store); shapes[i] = store.AddShape(bodies[i], circle);
        }
        Check(store.FindPairs() == count * (count - 1) / 2 && store.PairCapacityRetries == 1, "Dense batch grows and returns every pair without truncation.");
        var pairs = new Pair[count * (count - 1) / 2];
        Check(store.ReadPairs(pairs) == pairs.Length && pairs.Distinct().Count() == pairs.Length, "Dense pairs are unique.");
        Reject<ArgumentException>(() => store.ReadPairs(new Pair[1]));
        // A randomized oracle exercises masks, directional sensors, static/kinematic roles,
        // both query orientations, moved leaves, and a full tree resort after 32 refits.
        for (var i = 0; i < count; i++)
        {
            store.Remove(bodies[i]);
            modes[i] = (Mode)(i % 4); sensors[i] = i % 5 == 0;
            layers[i] = i % 7 == 0 ? 0 : 1u << (i % 2 == 0 ? 31 : 0);
            masks[i] = i % 3 == 0 ? uint.MaxValue : 1u << (i % 2 == 0 ? 31 : 0);
            bodies[i] = Add(store, mode: modes[i]);
            shapes[i] = store.AddShape(bodies[i], circle, layer: layers[i], mask: masks[i], sensor: sensors[i]);
        }
        for (var frame = 0; frame < 36; frame++)
        {
            for (var i = 0; i < count; i++)
            {
                positions[i] = new((float)random.NextDouble() * 25, (float)random.NextDouble() * 25);
                store.SetPose(bodies[i], positions[i], 0);
            }
            var expected = new HashSet<Pair>();
            for (var i = 0; i < count; i++)
                for (var j = i + 1; j < count; j++)
                {
                    var eligible = sensors[i] || sensors[j]
                        ? (sensors[i] && (masks[i] & layers[j]) != 0) || (sensors[j] && (masks[j] & layers[i]) != 0)
                        : (modes[i] >= Mode.Rigid || modes[j] >= Mode.Rigid) && ((masks[i] & layers[j]) != 0 || (masks[j] & layers[i]) != 0);
                    if (eligible && MathF.Abs(positions[i].X - positions[j].X) <= 3 && MathF.Abs(positions[i].Y - positions[j].Y) <= 3)
                        expected.Add(shapes[i].Index < shapes[j].Index ? new(shapes[i], shapes[j]) : new(shapes[j], shapes[i]));
                }
            var actualCount = store.ReadPairs(pairs);
            Check(actualCount == expected.Count, "Randomized pair count agrees with brute-force authored AABB oracle.");
            for (var i = 0; i < actualCount; i++) Check(expected.Remove(pairs[i]), "Every canonical pair is expected and appears once.");
        }
        for (var i = 2; i < count; i++) store.Remove(bodies[i]);
        store.SetPose(bodies[0], Vector2.Zero, 0); store.SetPose(bodies[1], new(3, 0), 0);
        store.SetShapeFilter(shapes[0], 1u << 31, 0, false);
        store.SetShapeFilter(shapes[1], 0, 1u << 31, true);
        Check(store.FindPairs() == 1, "Touching static/kinematic sensor pair uses the sensor mask with zero sensor layer.");
        store.SetShapeFilter(shapes[1], 0, 0, true);
        Check(store.FindPairs() == 0, "Sparse filter edits remove prior pairs.");
        store.Remove(bodies[0]); store.Remove(bodies[1]);
        Check(store.FindPairs() == 0 && store.ShapeCount == 0, "An empty resident tree drops all historical pairs.");
        Console.WriteLine("Resident broad phase: dense complete pairs, randomized masks/sensors/roles, tree refits/resorts and empty teardown passed.");
    }

    private static void VerifyFailure()
    {
        using var store = new GPUPhysicsBodyStore();
        using var shape = new CircleShape { Radius = float.MaxValue / 2 };
        var body = Add(store, new(float.MaxValue, 0));
        store.AddShape(body, shape, sensor: true);
        Reject<InvalidOperationException>(() => store.FindPairs());
        Reject<InvalidOperationException>(() => store.Step(0.01f, Vector2.Zero));
        Reject<InvalidOperationException>(() => store.AddShape(body, shape));
        Console.WriteLine("Invalid device bounds invalidate the store without CPU replay.");
    }

    private static void VerifyResidency(int side)
    {
        const int warmup = 384, samples = 256;
        var count = side * side;
        var expectedPairs = 2 * side * (side - 1) + 2 * (side - 1) * (side - 1);
        using var store = new GPUPhysicsBodyStore();
        using var box = new RectangleShape { Size = new(2.2f, 2.2f) };
        var bodies = new Body[count];
        for (var i = 0; i < count; i++)
        {
            bodies[i] = Add(store, new(i % side * 2, i / side * 2), velocity: new(0.25f, 0.5f));
            store.AddShape(bodies[i], box);
        }
        for (var i = 0; i < warmup; i++) { store.Step(1f / 120, Vector2.Zero); Check(store.FindPairs() == expectedPairs, "Moving grid pair count"); }
        var times = new double[samples];
        var upload = store.UploadBytes; var readback = store.ReadbackBytes; var uniforms = store.UniformBytes;
        var geometry = store.GeometryUploadBytes; var shapes = store.ShapeUploadBytes; var wait = store.WaitMS;
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < samples; i++)
        {
            var start = Stopwatch.GetTimestamp();
            store.Step(1f / 120, Vector2.Zero);
            Check(store.FindPairs() == expectedPairs, "Moving grid pair count");
            times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        var waitMS = (store.WaitMS - wait) / samples;
        Check(allocated == 0, "Resident integration and broad phase allocate zero warmed managed bytes.");
        Check(store.GeometryUploadBytes == geometry && store.ShapeUploadBytes == shapes, "Unchanged geometry and shape attachments never reupload during moving simulation.");
        Check(store.UploadBytes - upload == 16 * samples && store.ReadbackBytes - readback == 16 * samples,
            "Integration plus broad phase only exchanges status/count, with no poses, bounds or pair downloads.");
        var uniformPerTick = (store.UniformBytes - uniforms) / (double)samples;
        var pairs = new Pair[expectedPairs];
        var readStart = Stopwatch.GetTimestamp();
        store.ReadPairs(pairs);
        var readMS = Stopwatch.GetElapsedTime(readStart).TotalMilliseconds;
        var unique = new HashSet<Pair>();
        foreach (var pair in pairs)
        {
            var x = Math.Abs(pair.A.Index % side - pair.B.Index % side);
            var y = Math.Abs(pair.A.Index / side - pair.B.Index / side);
            Check(pair.A.Index < pair.B.Index && x <= 1 && y <= 1 && unique.Add(pair), "Every grid neighbor pair is canonical, adjacent and unique.");
        }
        Array.Sort(times);
        Console.WriteLine($"Resident integration+broad phase only: {count} bodies/shapes, {expectedPairs} pairs, {warmup} warmup, {samples} samples; p50={times[samples / 2]:F4} ms, p95={times[(int)(samples * 0.95)]:F4} ms, wait={waitMS:F4} ms/tick, {allocated} B/tick; 16 B buffer upload + {uniformPerTick:F0} B mean uniforms + 16 B readback/tick; geometry uploads/tick=0, pair uploads/downloads/tick=0; explicit pair read={readMS:F4} ms (outside samples), driver={store.Driver}, device={store.DeviceName}, runtime={Environment.Version}.");
        foreach (var body in bodies) store.Remove(body);
        Check(store.FindPairs() == 0 && store.Count == 0 && store.ShapeCount == 0, "Full grid retirement removes body attachments and pairs.");
    }

    private static Rect2 BoundsOf(Vector2[] points, float radius, Transform pose)
    {
        var lower = new Vector2(float.MaxValue, float.MaxValue); var upper = -lower;
        foreach (var point in points) { var world = pose * point; lower = lower.Min(world); upper = upper.Max(world); }
        return new(lower - Vector2.One * radius, upper - lower + Vector2.One * (2 * radius));
    }
    private static void Bounds(Rect2? actual, Rect2 expected, string message) => Check(actual is { } bounds &&
        bounds.Position.DistanceTo(expected.Position) < 0.0001f && bounds.Size.DistanceTo(expected.Size) < 0.0001f, message);
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
