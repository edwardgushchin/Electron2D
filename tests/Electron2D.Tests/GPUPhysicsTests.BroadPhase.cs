using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2BoardPhases;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

internal static partial class GPUPhysicsTests
{
    internal static void RunBroadPhase()
    {
        using var gpu = new GPUPhysicsWorld();
        VerifyBroadPhase(gpu);
        VerifyWorld(true, pairFailure: true);
    }

    private static void VerifyBroadPhase(GPUPhysicsWorld gpu)
    {
        foreach (var count in new[] { 0, 1, 63, 64, 65, 257, 4097 })
            VerifyBroadPhase(gpu, count, false);
        VerifyBroadPhase(gpu, 257, true);
        Console.WriteLine("GPU tree traversal matches CPU pair and custom-filter order: dispatch edges, mixed proxies, dense overflow, filters, joints, reuse and zero warmed managed bytes.");
    }

    private static void VerifyBroadPhase(GPUPhysicsWorld gpu, int count, bool dense)
    {
        var definition = b2DefaultWorldDef(); definition.gravity = new();
        var id = b2CreateWorld(definition);
        var world = b2GetWorldFromId(id);
        var shapes = new List<B2ShapeId>(); var bodies = new List<B2BodyId>();
        var expected = new List<(int Move, int A, int B)>();
        var expectedFilters = new List<(int A, int B)>();
        var actualFilters = new List<(int A, int B)>();
        var filters = expectedFilters;
        var owner = Environment.CurrentManagedThreadId;
        var passes = 0;
        world.customFilterFcn = (a, b, _) =>
        {
            if (owner != Environment.CurrentManagedThreadId) throw new Exception("GPU pair filters left the owner thread.");
            filters.Add((a.index1, b.index1));
            return (a.index1 + b.index1) % 7 != 0;
        };
        world.findBroadPhasePairs = w =>
        {
            var bp = w.broadPhase;
            expected.Clear(); expectedFilters.Clear(); actualFilters.Clear();
            filters = expectedFilters;
            B2Atomics.b2AtomicStoreInt(ref bp.movePairIndex, 0);
            b2FindPairsTask(0, bp.moveArray.count, 0, w);
            for (var i = 0; i < bp.moveArray.count; i++)
                for (var pair = bp.moveResults[i].pairList; pair is not null; pair = pair.next)
                    expected.Add((i, pair.shapeIndexA, pair.shapeIndexB));
            B2Atomics.b2AtomicStoreInt(ref bp.movePairIndex, 0);
            filters = actualFilters;
            gpu.FindBroadPhasePairs(w);
            var cursor = 0;
            for (var i = 0; i < bp.moveArray.count; i++)
                for (var pair = bp.moveResults[i].pairList; pair is not null; pair = pair.next)
                {
                    if (cursor >= expected.Count || expected[cursor++] != (i, pair.shapeIndexA, pair.shapeIndexB))
                        throw new Exception($"GPU pair/order differs at {count}/{dense}, pass {passes}, move {i}.");
                }
            if (cursor != expected.Count || expectedFilters.Count != actualFilters.Count)
                throw new Exception("GPU pair or filter callback coverage differs.");
            for (var i = 0; i < expectedFilters.Count; i++)
                if (expectedFilters[i] != actualFilters[i]) throw new Exception("GPU filter callback order differs.");
            if (passes == 0 && dense && expected.Count <= 16 * count)
                throw new Exception("The dense fixture must exceed the original per-move arena estimate.");
            if (passes == 0 && dense && gpu.BroadPhaseRetryCount != 1)
                throw new Exception("Dense GPU output must grow and retry without dropping pairs.");
            passes++;
        };
        try
        {
            gpu.FindBroadPhasePairs(world);
            for (var i = 0; i < count; i++)
            {
                var bodyDef = b2DefaultBodyDef();
                bodyDef.type = dense ? B2BodyType.b2_dynamicBody : (B2BodyType)(i % 3);
                bodyDef.position = dense ? new(0, 0) : count > 1000 ? new(i * 4, 0) : new(i % 8 * .4f, i / 8 * .4f);
                var body = b2CreateBody(id, bodyDef); bodies.Add(body);
                var shapeDef = b2DefaultShapeDef(); shapeDef.invokeContactCreation = true;
                shapeDef.enableCustomFiltering = !dense; shapeDef.isSensor = !dense && i % 17 == 0;
                shapeDef.filter.categoryBits = dense ? 1UL : i % 11 == 0 ? 0UL : 1UL << (i % 64);
                shapeDef.filter.maskBits = dense || i % 5 == 0 ? ulong.MaxValue : 0xaaaaaaaaaaaaaaaaUL;
                shapeDef.filter.groupIndex = dense ? 0 : i % 9 < 2 ? 3 : i % 9 < 4 ? -3 : 0;
                shapes.Add(b2CreateCircleShape(body, shapeDef, new B2Circle { radius = .5f }));
                if (!dense && i % 13 == 0)
                    shapes.Add(b2CreateCircleShape(body, shapeDef, new B2Circle { center = new(.1f, 0), radius = .4f }));
            }
            if (count > 5 && !dense)
            {
                var joint = B2Joints.b2DefaultRevoluteJointDef();
                joint.@base.bodyIdA = bodies[2]; joint.@base.bodyIdB = bodies[5]; joint.@base.collideConnected = false;
                B2Joints.b2CreateRevoluteJoint(id, joint);
            }
            b2UpdateBroadPhasePairs(world);
            if (count == 0 && (passes != 0 || gpu.BroadPhaseCandidateCount != 0))
                throw new Exception("An empty world must not dispatch a pair query.");

            // Existing contacts, both moved and one moved, all three proxy types.
            for (var pass = 0; pass < 4; pass++)
            {
                for (var i = pass; i < shapes.Count; i += pass + 1)
                    b2BufferMove(world.broadPhase, world.shapes.data[shapes[i].index1 - 1].proxyKey);
                b2UpdateBroadPhasePairs(world);
            }
            if (count > 5 && !dense)
            {
                // Destroy a moved proxy, reuse its slots, rebuild, refilter and teleport.
                var removed = shapes[^1];
                b2BufferMove(world.broadPhase, world.shapes.data[removed.index1 - 1].proxyKey);
                b2DestroyShape(removed, true); shapes.RemoveAt(shapes.Count - 1);
                var shapeDef = b2DefaultShapeDef(); shapeDef.invokeContactCreation = true;
                shapes.Add(b2CreateCircleShape(bodies[^1], shapeDef, new B2Circle { radius = .8f }));
                b2Shape_SetFilter(shapes[2], new B2Filter { categoryBits = 1UL << 63, maskBits = ulong.MaxValue, groupIndex = 3 });
                b2Body_SetTransform(bodies[3], new(.25f, .25f), new(1, 0));
                b2BroadPhase_RebuildTrees(world.broadPhase);
                b2UpdateBroadPhasePairs(world);
            }
            // Retained capacities and unchanged topology, measured across all managed threads.
            for (var tick = 0; tick < 40; tick++)
            {
                foreach (var shape in shapes) b2BufferMove(world.broadPhase, world.shapes.data[shape.index1 - 1].proxyKey);
                b2UpdateBroadPhasePairs(world);
                B2ArenaAllocators.b2GrowArena(world.arena);
            }
            var before = GC.GetTotalAllocatedBytes(true);
            for (var tick = 0; tick < 16; tick++)
            {
                foreach (var shape in shapes) b2BufferMove(world.broadPhase, world.shapes.data[shape.index1 - 1].proxyKey);
                b2UpdateBroadPhasePairs(world);
            }
            var bytes = GC.GetTotalAllocatedBytes(true) - before;
            if (bytes != 0) throw new Exception($"Warmed CPU/GPU pair oracle allocated {bytes} bytes ({count}/{dense}).");
        }
        finally
        {
            world.findBroadPhasePairs = null!;
            b2DestroyWorld(id);
        }
    }
}
