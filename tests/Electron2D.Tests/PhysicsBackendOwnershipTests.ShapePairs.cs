using Electron2D;

internal static partial class PhysicsBackendOwnershipTests
{
    private delegate bool PairQueryHook(Span<Vector2> contacts, out int count);
    internal static void ShapePairs(PhysicsServer.Backend backend)
    {
        ViewBackend? owner = null;
        using var world = new World(new WorldRuntime(false, backend, factory: space => owner = new(space, backend)));
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 5 };
        using var small = new CircleShape { Radius = 1 };
        using var other = new CircleShape { Radius = 5 };
        var scene = new StaticBody { Position = new(20, 10), Rotation = .5f, CollisionLayer = 0, CollisionMask = 0 };
        scene.AddChild(new CollisionShape { Name = "Disabled", Shape = small, Disabled = true });
        scene.AddChild(new CollisionShape { Name = "Offset", Shape = circle, Position = new(10, 0), Rotation = .25f });
        Check(PhysicsServer.Service.BodyCollideShapeCore(scene.GetRID(), 1, other.GetRID(), new(0, scene.GlobalTransform * new Vector2(10, 0)), Vector2.Zero, [], out _),
            "Detached scene body pair requires no physics space");
        root.AddChild(scene);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate(); var detached = PhysicsServer.BodyCreate();
        var first = PhysicsServer.Service.GetSceneSpace(world.Space);
        var output = new Vector2[8]; var expected = new Vector2[8];
        try
        {
            PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodySetCollisionLayer(raw, 0); PhysicsServer.BodySetCollisionMask(raw, 0);
            PhysicsServer.BodySetTransform(raw, scene.GlobalTransform);
            PhysicsServer.BodyAddShape(raw, small.GetRID(), disabled: true);
            PhysicsServer.BodyAddShape(raw, circle.GetRID(), new(.25f, new(10, 0))); PhysicsServer.BodySetSpace(raw, world.Space);
            PhysicsServer.BodyAddShape(detached, circle.GetRID(), new(.25f, new(10, 0))); PhysicsServer.BodySetTransform(detached, scene.GlobalTransform);
            tree.PhysicsFrame(1d / 60);
            var bodyPose = scene.GlobalTransform * new Transform(.25f, new(10, 0));
            var pose = new Transform(0, bodyPose.Origin + new Vector2(8, 0));
            Check(PhysicsShapeCollision.Evaluate(circle, bodyPose, Vector2.Zero, other, pose, Vector2.Zero, expected, out var expectedCount), "Expected rotated pair intersects");
            foreach (var body in new[] { scene.GetRID(), raw })
            {
                Check(PhysicsServer.Service.BodyCollideShapeCore(body, 1, other.GetRID(), pose, Vector2.Zero, output, out var count) && count == expectedCount,
                    "Selected body pair tests the requested logical slot and composed global pose");
                Check(output.AsSpan(0, count * 2).SequenceEqual(expected.AsSpan(0, count * 2)), "Scene/raw pair point ordering matches original resource geometry");
                Check(PhysicsServer.Service.BodyCollideShapeCore(body, 0, other.GetRID(), new(0, scene.GlobalPosition), Vector2.Zero, [], out count) && count == 0,
                    "Explicit body pair uses disabled logical geometry for hit-only requests");
            }
            Check(PhysicsServer.Service.BodyCollideShapeCore(detached, 0, other.GetRID(), pose, Vector2.Zero, output, out _) &&
                owner!.BodyPairQueries == 4, "Detached pair requires no solver and does not select a world's owner");
            Check(first.CollideShapes(circle.GetRID(), bodyPose, Vector2.Zero, other.GetRID(), new(0, bodyPose.Origin + new Vector2(20, 0)), new(-15, 0), output, out var pairCount) && pairCount > 0,
                "Selected resource pair retains independent motion-volume semantics");
            using var boundary = new WorldBoundaryShape { Normal = Vector2.Up, Distance = 0 };
            Check(first.CollideShapes(boundary.GetRID(), Transform.Identity, Vector2.Zero, circle.GetRID(), new(0, new(0, 1)), Vector2.Zero, output, out pairCount) && pairCount > 0,
                "Selected pair preserves infinite boundary contacts");
            using var ray = new SeparationRayShape { Length = 30 };
            using var box = new RectangleShape { Size = new(20, 10) };
            Check(first.CollideShapes(ray.GetRID(), Transform.Identity, Vector2.Zero, box.GetRID(), new(0, new(0, 25)), Vector2.Zero, output, out pairCount) && pairCount == 1,
                "Selected pair preserves directed separation-ray point order");
            using var terrain = new ConcavePolygonShape
            { Segments = Enumerable.Range(0, 24).SelectMany(i => new Vector2[] { new(-20, .1f * i), new(20, .1f * i) }).ToArray() };
            var larger = new Vector2[48];
            Check(first.CollideShapes(circle.GetRID(), Transform.Identity, Vector2.Zero, terrain.GetRID(), Transform.Identity, Vector2.Zero, larger, out pairCount) && pairCount == 24,
                "Internal pair capacity is supplied by the caller, independently of the resource array's sixteen-pair cap");
            PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetGravityScale(raw, 0);
            PhysicsServer.BodySetCanSleep(raw, false); PhysicsServer.BodySetLinearVelocity(raw, new(1, 0));
            tree.PhysicsFrame(1d / 60);
            var store = first.GPUStore; var readback = store?.ReadbackBytes ?? 0; var submissions = store?.SubmissionCount ?? 0;
            var wait = store?.WaitMS ?? 0; var readMark = System.Diagnostics.Stopwatch.GetTimestamp();
            var hit = first.BodyCollideShape(raw, 1, other.GetRID(), pose, Vector2.Zero, output, out _);
            var readMS = System.Diagnostics.Stopwatch.GetElapsedTime(readMark).TotalMilliseconds;
            Check(hit && (store is null || store.ReadbackBytes - readback == 8 + 16 && store.SubmissionCount - submissions == 1),
                $"Unobserved resident body pair reads eight-byte status plus one sixteen-byte pose: hit={hit}, bytes={(store?.ReadbackBytes ?? 0) - readback}");
            Console.WriteLine($"{backend}: one dynamic body pair: {readMS:F4} ms, readback {(store?.ReadbackBytes ?? 0) - readback} B, wait {(store?.WaitMS ?? 0) - wait:F4} ms, submissions {(store?.SubmissionCount ?? 0) - submissions}.");
            PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Static);
            GuardAndStage();
            owner!.PairHook = null;
            var times = new double[64];
            Collect(); for (var i = 0; i < 64; i++) Cycle();
            var upload = store?.UploadBytes ?? 0; readback = store?.ReadbackBytes ?? 0; wait = store?.WaitMS ?? 0;
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++)
            {
                var mark = System.Diagnostics.Stopwatch.GetTimestamp(); Cycle(); times[i] = System.Diagnostics.Stopwatch.GetElapsedTime(mark).TotalMilliseconds;
            }
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Selected shape/body pair intervals allocate {own}/{all} owner/all-thread bytes");
            Array.Sort(times);
            Console.WriteLine($"{backend}: 2 attached static bodies, dt=1/60, full step + 3 pair queries, 64 warmup/64 samples: p50/p95/p99 {times[31]:F4}/{times[60]:F4}/{times[63]:F4} ms; mean upload/readback {((store?.UploadBytes ?? 0) - upload) / 64d:F1}/{((store?.ReadbackBytes ?? 0) - readback) / 64d:F1} B; wait {((store?.WaitMS ?? 0) - wait) / 64d:F4} ms.");
            Console.WriteLine($"{backend}: scene/raw/detached logical shape pairs, boundary/ray/concave motion, selected hooks, atomic nested results and guards; 64 warm full-step/pair cycles: {own}/{all} B.");
            void Cycle()
            {
                tree.PhysicsFrame(1d / 60);
                _ = first.CollideShapes(circle.GetRID(), bodyPose, Vector2.Zero, other.GetRID(), pose, Vector2.Zero, output, out _);
                _ = PhysicsServer.Service.BodyCollideShapeCore(raw, 1, other.GetRID(), pose, Vector2.Zero, output, out _);
                _ = PhysicsServer.Service.BodyCollideShapeCore(scene.GetRID(), 1, other.GetRID(), pose, Vector2.Zero, output, out _);
            }
            void GuardAndStage()
            {
                var area = PhysicsServer.AreaCreate(); PhysicsServer.AreaSetSpace(area, world.Space);
                try
                {
                    Reject<ArgumentException>(() => first.BodyCollideShape(area, 0, other.GetRID(), pose, Vector2.Zero, output, out _));
                    Reject<ArgumentOutOfRangeException>(() => PhysicsServer.Service.BodyCollideShapeCore(raw, 9, other.GetRID(), pose, Vector2.Zero, output, out _));
                    Reject<ArgumentException>(() => first.CollideShapes(circle.GetRID(), bodyPose, Vector2.Zero, other.GetRID(), pose, Vector2.Zero, new Vector2[3], out _));
                    Reject<ArgumentOutOfRangeException>(() => first.CollideShapes(circle.GetRID(), bodyPose, new(float.NaN, 0), other.GetRID(), pose, Vector2.Zero, output, out _));
                    var alternate = PhysicsServer.SpaceCreate(backend);
                    try { Reject<ArgumentException>(() => PhysicsServer.Service.GetSceneSpace(alternate).BodyCollideShape(raw, 1, other.GetRID(), pose, Vector2.Zero, output, out _)); }
                    finally { PhysicsServer.FreeRID(alternate); }
                    owner!.PairHook = (Span<Vector2> points, out int count) =>
                    {
                        RequireBorrowRejection(() => first.Step(1d / 60));
                        RequireBorrowRejection(() => root.World = null);
                        RequireBorrowRejection(() => PhysicsServer.FreeRID(raw));
                        RequireBorrowRejection(() => PhysicsServer.FreeRID(area));
                        Check(PhysicsServer.BodyGetSpace(raw) == world.Space && PhysicsServer.AreaGetSpace(area) == world.Space,
                            "Rejected free preserves both body/Area registrations and memberships");
                        RequireBorrowRejection(() => root.RemoveChild(scene));
                        RequireBorrowRejection(() => PhysicsServer.BodySetSpace(raw, default));
                        var saved = owner.PairHook; owner.PairHook = null;
                        try { Check(first.CollideShapes(circle.GetRID(), bodyPose, Vector2.Zero, other.GetRID(), pose, Vector2.Zero, expected, out var nested) && nested > 0, "Nested shape-pair uses a separate frame"); }
                        finally { owner.PairHook = saved; }
                        points[0] = new(1, 2); points[1] = new(3, 4); count = 1; return true;
                    };
                    Array.Fill(output, new(77, 88));
                    Check(first.BodyCollideShape(raw, 1, other.GetRID(), pose, Vector2.Zero, output, out var count) && count == 1 && output[0] == new Vector2(1, 2) && output[2] == new Vector2(77, 88),
                        "Actual selected hook output commits only its used prefix");
                    foreach (var mode in new[] { 0, 1, 2, 3 })
                    {
                        owner.PairHook = (Span<Vector2> points, out int n) =>
                        {
                            points[0] = new(9, 9); points[1] = new(float.NaN, 0);
                            if (mode == 0) throw new InvalidOperationException("Expected pair failure");
                            n = mode == 1 ? 99 : 1; return mode != 2;
                        };
                        Array.Fill(output, new(77, 88));
                        RequireBorrowRejection(() => first.CollideShapes(circle.GetRID(), bodyPose, Vector2.Zero, other.GetRID(), pose, Vector2.Zero, output, out _));
                        Check(output.All(p => p == new Vector2(77, 88)), "Hook failure/count/miss/nonfinite validation preserves all caller output");
                    }
                    owner.PairHook = null;
                    Check(!first.CollideShapes(circle.GetRID(), bodyPose, Vector2.Zero, other.GetRID(), new(0, new(1000, 1000)), Vector2.Zero, output, out var missed) && missed == 0 && output.All(p => p == new Vector2(77, 88)),
                        "A genuine selected miss preserves the caller buffer and publishes zero pairs");
                }
                finally { PhysicsServer.FreeRID(area); }
            }
        }
        finally { PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(detached); }
    }
}
