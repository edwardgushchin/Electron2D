using Electron2D;
using System.Runtime.InteropServices;

internal static class PhysicsPointGravityRangeTests
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        AllocationWarmupCheck.Verify(Collect);
        using var trace = PhysicsAllocationTrace.Open(backend);
        PhysicsPointGravityFailureTests.Run(backend);
        WideCoordinates(backend, trace);
        using var world = new World(backend); using var region = new CircleShape { Radius = 20 }; using var circle = new CircleShape { Radius = 1 };
        using var root = new SubViewport { World = world };
        var area = new Area { GravityPoint = true, GravitySpaceOverride = Area.SpaceOverride.Replace, Monitoring = false };
        Add(area, region); root.AddChild(area);
        var rigid = new RigidBody { Name = "Rigid", CanSleep = false, CollisionMask = 0 }; Add(rigid, circle); root.AddChild(rigid);
        var character = new CharacterBody { Name = "Character", CollisionMask = 0 }; Add(character, circle); root.AddChild(character);
        using var tree = new SceneTree(root); Check(PhysicsServer.SpaceGetBackend(world.Space) == backend, "Explicit field backend.");
        if (backend == PhysicsServer.Backend.GPU)
        {
            var device = PhysicsServer.Service.GetSceneSpace(world.Space).GPUStore!;
            Console.WriteLine($"Point field compute device: {device.Driver}; {device.DeviceName}.");
        }
        var body = PhysicsServer.BodyCreate(); var shape = PhysicsServer.CircleShapeCreate(); var rawArea = PhysicsServer.AreaCreate(); var regionShape = PhysicsServer.CircleShapeCreate();
        try
        {
            PhysicsServer.ShapeSetData(shape, circle); PhysicsServer.BodyAddShape(body, shape); PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid);
            PhysicsServer.BodySetCollisionMask(body, 0); PhysicsServer.BodySetCanSleep(body, false); PhysicsServer.BodySetSpace(body, world.Space);
            PhysicsServer.ShapeSetData(regionShape, region); PhysicsServer.AreaAddShape(rawArea, regionShape); PhysicsServer.AreaSetSpace(rawArea, world.Space);
            PhysicsServer.AreaSetGravityPoint(rawArea, true);
            var cases = new (Vector2 Center, float Gravity, float UnitDistance, Vector2 Expected)[]
            {
                (new(1e-25f, 0), 10, 0, new(10, 0)),
                (new(1e-25f, 0), -10, -1, new(-10, 0)),
                (new(1e-25f, 0), 10, 1e-25f, new(10, 0)),
                (new(1e20f, 0), 10, 1e20f, new(10, 0)),
                (new(1e20f, 1e20f), 10, 1e20f, new(3.5355339f, 3.5355339f)),
                (new(float.MaxValue, float.MaxValue), 10, 0, new(7.0710678f, 7.0710678f)),
                (new(1e-30f, 0), 1e-30f, 1e-10f, new(1e10f, 0)),
                (new(1e-25f, 0), 0, float.MaxValue, Vector2.Zero),
                (Vector2.Zero, float.MaxValue, float.MaxValue, Vector2.Zero)
            };
            for (var profile = 0; profile < 3; profile++)
            {
                area.GravitySpaceOverride = profile == 0 ? Area.SpaceOverride.Replace : Area.SpaceOverride.Disabled;
                PhysicsServer.AreaSetGravitySpaceOverride(rawArea, profile == 1 ? Area.SpaceOverride.Replace : Area.SpaceOverride.Disabled);
                PhysicsServer.AreaSetGravityPoint(world.Space, profile == 2);
                PhysicsServer.AreaSetGravity(world.Space, 0);
                foreach (var sample in cases)
                {
                    area.GravityPointCenter = sample.Center; area.Gravity = sample.Gravity; area.GravityPointUnitDistance = sample.UnitDistance;
                    PhysicsServer.AreaSetGravityVector(rawArea, sample.Center); PhysicsServer.AreaSetGravity(rawArea, sample.Gravity); PhysicsServer.AreaSetGravityPointUnitDistance(rawArea, sample.UnitDistance);
                    if (profile == 2) { PhysicsServer.AreaSetGravityVector(world.Space, sample.Center); PhysicsServer.AreaSetGravity(world.Space, sample.Gravity); PhysicsServer.AreaSetGravityPointUnitDistance(world.Space, sample.UnitDistance); }
                    Reset(rigid, body); tree.PhysicsFrame(.001);
                    Near(rigid.GetGravity(), sample.Expected, "Scene rigid field"); Near(character.GetGravity(), sample.Expected, "Character field");
                    Near(PhysicsServer.BodyGetDirectState(body)!.TotalGravity, sample.Expected, "Raw body field");
                    Check(rigid.Position.IsFinite() && rigid.LinearVelocity.IsFinite() && PhysicsServer.BodyGetTransform(body).Origin.IsFinite(), "The complete solver interval remains finite.");
                }
            }
            area.GravitySpaceOverride = Area.SpaceOverride.Replace; PhysicsServer.AreaSetGravitySpaceOverride(rawArea, Area.SpaceOverride.Disabled);
            area.GravityPointCenter = new(1e20f, 0); area.Gravity = 10; area.GravityPointUnitDistance = 1e20f;
            if (backend == PhysicsServer.Backend.CPU)
            {
                area.GravityPointCenter = new(1e-25f, 0); area.Gravity = float.MaxValue;
                Reset(rigid, body); var prior = rigid.GetGravity();
                var rejected = false;
                try { tree.PhysicsFrame(.001); } catch (AggregateException) { rejected = true; }
                Check(rejected && rigid.GetGravity() == prior && rigid.GetGravity().IsFinite(), "Genuinely unrepresentable force rejects before field-cache publication.");
                area.GravityPointCenter = new(1e20f, 0); area.Gravity = 10;
            }
            Collect();
            for (var i = 0; i < 128; i++) { Reset(rigid, body); tree.PhysicsFrame(.001); }
            var generation0 = GC.CollectionCount(0); var generation1 = GC.CollectionCount(1); var generation2 = GC.CollectionCount(2);
            var tag = trace?.Tag(34) ?? 0; trace?.Begin(tag);
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            long ownerBytes, allBytes;
            try
            {
                for (var i = 0; i < 128; i++) { trace?.MarkFrame(i); Reset(rigid, body); tree.PhysicsFrame(.001); }
                trace?.MarkFrame(128);
            }
            finally
            {
                ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; allBytes = GC.GetTotalAllocatedBytes(true) - all;
                trace?.End();
            }
            if (ownerBytes != 0 || allBytes != 0)
            {
                trace?.ReportFrames(owner, ownerBytes);
                throw new InvalidOperationException($"Point gravity reset {backend}: 128 warmed complete frames allocated owner/all={ownerBytes}/{allBytes} B; GC={GC.CollectionCount(0) - generation0}/{GC.CollectionCount(1) - generation1}/{GC.CollectionCount(2) - generation2}, trace={tag}, runtime={RuntimeInformation.FrameworkDescription}, serverGC={System.Runtime.GCSettings.IsServerGC}.");
            }
            Console.WriteLine($"Point gravity range: backend={backend}, profiles=3, cases={cases.Length}, receivers=3, warmFrames=128, ownerBytes={ownerBytes}, allBytes={allBytes}.");
            if (backend == PhysicsServer.Backend.GPU)
            {
                var prior = rigid.GetGravity();
                area.GravityPointCenter = new(1e-25f, 0); area.GravityPointUnitDistance = 1e20f; area.Gravity = float.MaxValue;
                Reset(rigid, body);
                Reject<AggregateException>(() => tree.PhysicsFrame(.001));
                Check(rigid.GetGravity() == prior && world.PhysicsBackend == PhysicsServer.Backend.GPU && world.RequestedPhysicsBackend == PhysicsServer.Backend.GPU && world.PhysicsFallbackReason is null,
                    "Started point-force overflow preserves published fields and backend without fallback.");
                Reject<AggregateException>(() => tree.PhysicsFrame(.001));
                Reject<InvalidOperationException>(() => area.Gravity = 0);
                Console.WriteLine("GPU point-force overflow is terminal, preserves selection/publication and permits final resource retirement.");
            }
        }
        finally { PhysicsServer.FreeRID(rawArea); PhysicsServer.FreeRID(regionShape); PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(shape); }
    }
    private static void Collect()
    {
        // Finish background collection before its context retirement can change the allocation counters.
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
    }

    private static void WideCoordinates(PhysicsServer.Backend backend, PhysicsAllocationTrace? trace)
    {
        foreach (var sign in new[] { 1f, -1f })
        {
            using var world = new World(backend); using var root = new SubViewport { World = world };
            var rigid = new RigidBody { Name = "WideRigid", Position = new(-sign * 2e38f, 0), CanSleep = false, CollisionMask = 0 };
            var character = new CharacterBody { Name = "WideCharacter", Position = rigid.Position, CollisionMask = 0 };
            root.AddChild(rigid); root.AddChild(character); using var tree = new SceneTree(root);
            var body = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetCanSleep(body, false); PhysicsServer.BodySetSpace(body, world.Space);
                PhysicsServer.AreaSetGravityPoint(world.Space, true);
                PhysicsServer.BodySetTransform(body, new(0, rigid.Position));
                PhysicsServer.AreaSetGravityVector(world.Space, new(sign * 2e38f, 0)); PhysicsServer.AreaSetGravity(world.Space, 10);
                foreach (var unit in new[] { 0f, 2e38f })
                {
                    Console.WriteLine($"Wide point case: backend={backend}, sign={sign}, unit={unit}.");
                    PhysicsServer.AreaSetGravityPointUnitDistance(world.Space, unit); tree.PhysicsFrame(.001);
                    var expected = new Vector2(sign * (unit == 0 ? 10 : 2.5f), 0);
                    Near(rigid.GetGravity(), expected, "Wide scene point offset"); Near(character.GetGravity(), expected, "Wide character point offset");
                    Near(PhysicsServer.BodyGetDirectState(body)!.TotalGravity, expected, "Wide raw point offset");
                    Check(rigid.Position.IsFinite() && PhysicsServer.BodyGetTransform(body).Origin.IsFinite(), "Wide finite positions survive the complete interval.");
                }
                Collect();
                for (var i = 0; i < 128; i++) tree.PhysicsFrame(.001);
                var generation0 = GC.CollectionCount(0); var generation1 = GC.CollectionCount(1); var generation2 = GC.CollectionCount(2);
                var tag = trace?.Tag(sign > 0 ? 32UL : 33UL) ?? 0; trace?.Begin(tag);
                var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                long ownerBytes, allBytes;
                try
                {
                    for (var i = 0; i < 128; i++) { trace?.MarkFrame(i); tree.PhysicsFrame(.001); }
                    trace?.MarkFrame(128);
                }
                finally
                {
                    ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; allBytes = GC.GetTotalAllocatedBytes(true) - all;
                    trace?.End();
                }
                if (ownerBytes != 0 || allBytes != 0)
                {
                    trace?.ReportFrames(owner, ownerBytes);
                    throw new InvalidOperationException($"Wide point gravity {backend}, sign={sign}: 128 warmed complete frames allocated owner/all={ownerBytes}/{allBytes} B; GC={GC.CollectionCount(0) - generation0}/{GC.CollectionCount(1) - generation1}/{GC.CollectionCount(2) - generation2}, trace={tag}, runtime={RuntimeInformation.FrameworkDescription}, serverGC={System.Runtime.GCSettings.IsServerGC}.");
                }
                Console.WriteLine($"Wide point warm gate: backend={backend}, frames=128, ownerBytes={ownerBytes}, allBytes={allBytes}.");
            }
            finally { PhysicsServer.FreeRID(body); }
        }
        if (backend == PhysicsServer.Backend.CPU)
        {
            var fields = new PhysicsAreaFields(10, new(0, -2e38f)) { GravityPoint = true };
            var pose = new Transform(new(0, 1), new(-1, 0), new(2e38f, 2e38f));
            Near(fields.ComputeGravity(pose, new(1e38f, -2e38f)), new(6, 8), "Rotated/translated point coordinates normalize before narrowing.");
            fields.GravityPointUnitDistance = 1e38f;
            Near(fields.ComputeGravity(pose, new(1e38f, -2e38f)), new(.24f, .32f), "Rotated/translated point falloff preserves finite components.");
        }
        Console.WriteLine($"Wide point coordinates: backend={backend}, directions=2, policies=2, receivers=3; affineChecks={(backend == PhysicsServer.Backend.CPU ? 2 : 0)}.");
    }
    private static void Reset(RigidBody rigid, RID body)
    {
        rigid.Position = Vector2.Zero; rigid.LinearVelocity = Vector2.Zero;
        PhysicsServer.BodySetTransform(body, Transform.Identity); PhysicsServer.BodySetLinearVelocity(body, Vector2.Zero);
    }
    private static void Add(CollisionObject body, Shape shape) { var owner = body.CreateShapeOwner(null); body.ShapeOwnerAddShape(owner, shape); }
    private static void Near(Vector2 actual, Vector2 expected, string message)
    {
        var tolerance = MathF.Max(.002f, MathF.Max(MathF.Abs(expected.X), MathF.Abs(expected.Y)) * .0002f);
        Check(actual.IsFinite() && MathF.Abs(actual.X - expected.X) <= tolerance && MathF.Abs(actual.Y - expected.Y) <= tolerance, message + $": {actual} vs {expected}");
    }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
