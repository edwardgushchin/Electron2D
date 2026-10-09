using System.Diagnostics;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;

internal static class PhysicsCollisionPriorityTests
{
    internal static void MeasurePipeline(string path)
    {
        var start = Stopwatch.GetTimestamp(); using var device = new GPUPhysicsDevice();
        Console.WriteLine($"Compute device startup: {Stopwatch.GetElapsedTime(start).TotalMilliseconds:F2} ms.");
        var code = System.IO.File.ReadAllBytes(path); start = Stopwatch.GetTimestamp();
        var pipeline = ShaderCompiler.CreateComputePipeline(device.Device, code);
        if (pipeline == 0) throw GPUPhysicsDevice.Failure("compile the diagnostic pipeline");
        try { Console.WriteLine($"Compute pipeline {System.IO.Path.GetFileName(path)}: {Stopwatch.GetElapsedTime(start).TotalMilliseconds:F2} ms; {device.Driver}/{device.DeviceName}."); }
        finally { SDL3.SDL.ReleaseGPUComputePipeline(device.Device, pipeline); }
    }
    internal static void Run(bool resident = false)
    {
        if (!resident) { SceneAndServer(); SceneMovement(); }
        using var rig = new Rig(resident);
        foreach (var pair in new[] { (1f, 1f), (9f, 1f), (1f, 9f), (90f, 10f), (float.MaxValue, float.MaxValue),
            (float.MaxValue, 1f), (5e-6f, 5e-6f), (1e-7f, 1e-7f), (float.Epsilon, float.Epsilon) })
        {
            rig.Set(pair.Item1, pair.Item2); var travel = rig.Test();
            var sum = (double)pair.Item1 + pair.Item2; var normalization = sum < .00001f ? 1 : 2 / sum;
            // A circle penetrates two orthogonal planes by 2 u. Each plane independently
            // removes 40% of its weighted excess depth in each of four recovery passes.
            var excess = 2 + .08 * .95;
            var expected = new Vector2((float)(excess * (1 - Math.Pow(1 - .4 * pair.Item1 * normalization, 4))),
                (float)(-excess * (1 - Math.Pow(1 - .4 * pair.Item2 * normalization, 4))));
            Near(travel, expected, pair.Item1 < 1e-6f ? 1e-7f : .002f, "Weighted orthogonal recovery");
        }
        rig.Set(9, 1); var strongLeft = rig.Test(); rig.Set(1, 9); var strongBottom = rig.Test();
        Check(strongLeft.X > strongBottom.X + 1 && strongLeft.Y > strongBottom.Y + 1, "High-priority obstacle receives greater separation.");
        foreach (var invalid in new[] { 0f, -1, float.NaN, float.PositiveInfinity })
            Reject<ArgumentOutOfRangeException>(() => rig.Set(invalid, 9));
        Near(rig.Test(), strongBottom, .0001f, "Invalid edit preserves recovery");
        rig.VerifyLifetime(); rig.VerifyCapacity(); Measure(rig);
        Console.WriteLine($"Collision priority passed on {(resident ? "independent GPU" : "public CPU")}.");
    }

    private sealed class Rig : IDisposable
    {
        internal readonly Store? GPU;
        private readonly Store.BodyHandle _mover, _left, _bottom;
        private readonly RID _space, _body, _wall, _floor;
        private readonly CircleShape _circle = new() { Radius = 5 };
        private readonly WorldBoundaryShape _leftShape = new() { Normal = Vector2.Right, Distance = -3 };
        private readonly WorldBoundaryShape _bottomShape = new() { Normal = Vector2.Up, Distance = -3 };
        private readonly PhysicsTestMotionParameters _query = new() { RecoveryAsCollision = true };
        private readonly PhysicsTestMotionResult _result = new();
        internal Rig(bool resident)
        {
            if (resident)
            {
                GPU = new();
                _mover = GPU.Add(new(Mode.Static, new(500, 500), 0, default, 0)); GPU.AddShape(_mover, _circle);
                _left = GPU.Add(new(Mode.Static, default, 0, default, 0)); GPU.AddShape(_left, _leftShape);
                _bottom = GPU.Add(new(Mode.Static, default, 0, default, 0)); GPU.AddShape(_bottom, _bottomShape);
                Check(GPU.GetCollisionPriority(_left) == 1, "GPU default priority.");
            }
            else
            {
                _space = PhysicsServer.SpaceCreate();
                _body = Add(_circle, new(500, 500)); _wall = Add(_leftShape, default); _floor = Add(_bottomShape, default);
                Check(PhysicsServer.BodyGetCollisionPriority(_wall) == 1, "Server default priority.");
            }
        }
        private RID Add(Shape shape, Vector2 position)
        {
            var body = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(body, Mode.Static); PhysicsServer.BodyAddShape(body, shape.GetRID());
            PhysicsServer.BodySetTransform(body, new(0, position)); PhysicsServer.BodySetSpace(body, _space); return body;
        }
        internal void Set(float left, float bottom)
        {
            if (GPU is { } gpu) { gpu.SetCollisionPriority(_left, left); gpu.SetCollisionPriority(_bottom, bottom); }
            else { PhysicsServer.BodySetCollisionPriority(_wall, left); PhysicsServer.BodySetCollisionPriority(_floor, bottom); }
        }
        internal Vector2 Test()
        {
            if (GPU is { } gpu)
            {
                Span<Store.MotionQueryResult> result = stackalloc Store.MotionQueryResult[1];
                gpu.TestMotion([new(_mover, Transform.Identity, Vector2.Zero, RecoveryAsCollision: true)], [], [], result);
                Check(result[0].Collided, "GPU recovery reports an actual boundary contact."); return result[0].Travel;
            }
            Check(PhysicsServer.BodyTestMotion(_body, _query, _result), "CPU recovery reports an actual boundary contact."); return _result.GetTravel();
        }
        internal void VerifyLifetime()
        {
            Task.Run(() => Reject<InvalidOperationException>(() => Set(2, 3))).GetAwaiter().GetResult();
            if (GPU is { } gpu)
            {
                var submitted = gpu.SubmissionCount; gpu.SetCollisionPriority(_left, gpu.GetCollisionPriority(_left)); Test();
                Check(gpu.SubmissionCount == submitted + 1, "Equal priority adds no authoring submission.");
                var rebuilds = gpu.QuerySpatialSubmissionCount; Set(4, 5); Test();
                Check(gpu.QuerySpatialSubmissionCount == rebuilds, "Priority-only edits reuse the spatial tree.");
                gpu.SetCollisionPriority(_left, 7); var weighted = Test();
                gpu.SetMode(_left, Mode.Kinematic); gpu.SetVelocity(_left, new(2, 3), 4);
                gpu.SetMode(_left, Mode.Rigid); gpu.SetMode(_left, Mode.Static);
                Near(Test(), weighted, .0001f, "Device priority survives role and surface edits");
                Check(gpu.GetCollisionPriority(_left) == 7, "Role and velocity edits preserve priority.");
                Span<Store.Snapshot> states = stackalloc Store.Snapshot[1]; gpu.Read([_mover], states);
                Check(states[0].Position == new Vector2(500, 500), "Supplied-pose query preserves resident pose.");
                var sleeper = gpu.Add(new(Mode.Rigid, new(500, 500), 0, default, 0, Sleeping: true));
                gpu.Read([sleeper], states); gpu.SetCollisionPriority(sleeper, 3); gpu.Read([sleeper], states);
                Check(states[0].Sleeping, "Priority-only metadata preserves device sleep."); gpu.Remove(sleeper);
                var removed = gpu.Add(new(Mode.Static, default, 0, default, 0)); gpu.Remove(removed);
                Reject<ArgumentException>(() => gpu.SetCollisionPriority(removed, 3));
            }
            else
            {
                PhysicsServer.BodySetSpace(_wall, default); PhysicsServer.BodySetCollisionPriority(_wall, 7);
                PhysicsServer.BodySetMode(_wall, Mode.Kinematic); PhysicsServer.BodySetSpace(_wall, _space);
                Check(PhysicsServer.BodyGetCollisionPriority(_wall) == 7, "Detach and role edits preserve priority.");
                Check(PhysicsServer.BodyGetTransform(_body).Origin == new Vector2(500, 500), "Supplied-pose query preserves CPU pose.");
            }
        }
        internal void VerifyCapacity()
        {
            _leftShape.Distance = _bottomShape.Distance = -10;
            using var shallow = new WorldBoundaryShape { Normal = Vector2.Up, Distance = -4.5f };
            using var deep = new WorldBoundaryShape { Normal = Vector2.Right, Distance = -3 };
            Span<RID> bodies = stackalloc RID[41]; Span<Store.BodyHandle> handles = stackalloc Store.BodyHandle[41];
            var count = 0;
            try
            {
                for (; count < 41; count++)
                {
                    var shape = count == 40 ? deep : shallow;
                    if (GPU is { } gpu) { handles[count] = gpu.Add(new(Mode.Static, default, 0, default, 0)); gpu.AddShape(handles[count], shape); }
                    else bodies[count] = Add(shape, default);
                }
                var travel = Test();
                // Forty coincident shallow planes exceed scratch capacity. The deeper
                // perpendicular plane must still produce its four-pass correction.
                Near(new(travel.X, 0), new(1.8069504f, 0), .002f, "Deep recovery survives the 32-point capacity");
                Check(travel.Y is > -.577f and < -.57f, "Bounded coincident planes recover their shallow overlap.");
            }
            finally
            {
                for (var i = 0; i < count; i++) { if (GPU is { } gpu) gpu.Remove(handles[i]); else PhysicsServer.FreeRID(bodies[i]); }
                _leftShape.Distance = _bottomShape.Distance = -3;
            }
        }
        public void Dispose()
        {
            GPU?.Dispose();
            if (_space.IsValid()) { PhysicsServer.FreeRID(_body); PhysicsServer.FreeRID(_wall); PhysicsServer.FreeRID(_floor); PhysicsServer.FreeRID(_space); }
            _query.Dispose(); _result.Dispose(); _circle.Dispose(); _leftShape.Dispose(); _bottomShape.Dispose();
        }
    }

    private static void SceneAndServer()
    {
        using var root = new Node(); var body = new RigidBody { Name = "Body", GravityScale = 0 }; var area = new Area { Name = "Area" };
        root.AddChild(body); root.AddChild(area); body.Owner = area.Owner = root;
        Check(body.CollisionPriority == 1 && area.CollisionPriority == 1, "Inherited defaults.");
        PhysicsServer.BodySetCollisionPriority(body.GetRID(), 7); Check(body.CollisionPriority == 7, "Server edits project into the scene.");
        body.CollisionPriority = 3; area.CollisionPriority = 11;
        Check(PhysicsServer.BodyGetCollisionPriority(body.GetRID()) == 3, "Scene edits project into the server.");
        Reject<ArgumentException>(() => PhysicsServer.BodyGetCollisionPriority(area.GetRID()));
        foreach (var value in new[] { 0f, -1, float.NaN, float.PositiveInfinity }) Reject<ArgumentOutOfRangeException>(() => body.CollisionPriority = value);
        using (var packed = new PackedScene())
        {
            packed.Pack(root); using var copy = packed.Instantiate();
            Check(copy.GetNode<RigidBody>("Body").CollisionPriority == 3 && copy.GetNode<Area>("Area").CollisionPriority == 11, "Scene storage retains inherited priority.");
        }
        using var shape = new CircleShape { Radius = 5 }; body.AddChild(new CollisionShape { Shape = shape });
        using var tree = new SceneTree(root); tree.PhysicsFrame(.01); body.Sleeping = true;
        var fixture = body.BackendShapes[0]; var rid = body.GetRID(); body.CollisionPriority = 9;
        Check(body.Sleeping && body.GetRID() == rid && body.BackendShapes[0].Equals(fixture), "Live priority preserves sleep, RID and fixture.");
        Task.Run(() => Reject<InvalidOperationException>(() => _ = body.CollisionPriority)).GetAwaiter().GetResult();
        var guarded = false; body.NotifyLocalTransformChanges = true;
        body.LocalTransformChanged += _ => { Reject<InvalidOperationException>(() => body.CollisionPriority = 2); guarded = true; };
        body.LinearVelocity = new(1, 0); body.Sleeping = false; tree.PhysicsFrame(.01); Check(guarded, "In-step priority mutation is rejected.");
        var detached = PhysicsServer.BodyCreate(); PhysicsServer.FreeRID(detached); Reject<ArgumentException>(() => PhysicsServer.BodySetCollisionPriority(detached, 1));
    }

    private static void SceneMovement()
    {
        using var leftShape = new WorldBoundaryShape { Normal = Vector2.Right, Distance = -3 };
        using var bottomShape = new WorldBoundaryShape { Normal = Vector2.Up, Distance = -3 };
        using var circle = new CircleShape { Radius = 5 }; using var root = new Node();
        var left = new StaticBody { Name = "Left", CollisionPriority = 9 }; left.AddChild(new CollisionShape { Shape = leftShape }); root.AddChild(left);
        var bottom = new StaticBody { Name = "Bottom" }; bottom.AddChild(new CollisionShape { Shape = bottomShape }); root.AddChild(bottom);
        var area = new Area { Name = "Sensor", CollisionPriority = float.MaxValue }; area.AddChild(new CollisionShape { Shape = circle }); root.AddChild(area);
        var actor = new CharacterBody { Name = "Actor", MotionMode = CharacterMotionMode.Floating }; actor.AddChild(new CollisionShape { Shape = circle }); root.AddChild(actor);
        using var tree = new SceneTree(root);
        using var hit = actor.MoveAndCollide(Vector2.Zero, testOnly: true, recoveryAsCollision: true);
        Check(hit is not null && actor.Position == Vector2.Zero, "Test-only recovery retains pose and ignores high-priority sensors.");
        var expected = hit!.GetTravel(); Check(expected.X > 2 && expected.Y is < -.5f and > -.7f, "Scene motion uses obstacle priority.");
        actor.MoveAndSlide(); Near(actor.Position, expected, .002f, "Character recovery uses the shared policy");
        actor.Position = Vector2.Zero; using var applied = actor.MoveAndCollide(Vector2.Zero, recoveryAsCollision: true);
        Near(actor.Position, expected, .002f, "Ordinary move applies priority recovery");
    }

    private static void Measure(Rig rig)
    {
        for (var i = 0; i < 128; i++) { rig.Set((i & 1) + 1, 1); rig.Test(); }
        var samples = new long[128]; var uploads = rig.GPU?.UploadBytes ?? 0; var reads = rig.GPU?.ReadbackBytes ?? 0; var waits = rig.GPU?.WaitMS ?? 0;
        var before = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < samples.Length; i++) { var start = Stopwatch.GetTimestamp(); rig.Set((i & 1) + 1, 1); rig.Test(); samples[i] = Stopwatch.GetTimestamp() - start; }
        var allocated = GC.GetTotalAllocatedBytes(true) - before; Array.Sort(samples); var ms = 1000d / Stopwatch.Frequency;
        Console.WriteLine($"Priority {(rig.GPU is null ? "CPU" : "GPU")}: 128 warmup/128 edited recovery queries, p50/p95/p99={samples[64] * ms:F4}/{samples[121] * ms:F4}/{samples[126] * ms:F4} ms; {allocated} all-thread managed B.");
        if (rig.GPU is { } gpu) Console.WriteLine($"Priority traffic/query: upload={(gpu.UploadBytes - uploads) / 128} B, readback={(gpu.ReadbackBytes - reads) / 128} B, wait={(gpu.WaitMS - waits) / 128:F4} ms; {gpu.Driver}/{gpu.DeviceName}.");
        Check(allocated == 0, "Warmed edits and motion recovery allocate zero managed bytes.");
    }
    private static void Near(Vector2 actual, Vector2 expected, float tolerance, string message) => Check(actual.DistanceTo(expected) <= tolerance, $"{message}: {actual} vs {expected} (tolerance {tolerance} u).");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action operation) where T : Exception { try { operation(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
