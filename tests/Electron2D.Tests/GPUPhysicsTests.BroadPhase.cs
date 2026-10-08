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
        VerifyTreeCategory();
        foreach (var count in new[] { 0, 1, 2, 63, 64, 65, 257, 4097 })
            VerifyBroadPhase(gpu, count, false);
        VerifyBroadPhase(gpu, 257, true);
        Console.WriteLine("GPU tree, resident pairs and filter journals match CPU order: sparse/dense trees, motion/rebuild, filters/joints, late binding, ID reuse, zero unchanged uploads and warmed managed bytes.");
    }

    internal static void VerifyTreeCategory()
    {
        var tree = B2DynamicTrees.b2DynamicTree_Create();
        try
        {
            var changes = 0;
            var proxy = B2DynamicTrees.b2DynamicTree_CreateProxy(tree, new B2AABB { lowerBound = new(-1, -1), upperBound = new(1, 1) }, 1, 37);
            tree.proxyChanged = (id, topology) => { if (id != 37 || !topology) throw new Exception("Proxy category notification lost its leaf identity."); changes++; };
            B2DynamicTrees.b2DynamicTree_SetCategoryBits(tree, proxy, 1UL << 63);
            if (changes != 1 || B2DynamicTrees.b2DynamicTree_GetCategoryBits(tree, proxy) != 1UL << 63)
                throw new Exception("Changing a category must accept an allocated leaf with a nonzero user ID.");
        }
        finally { B2DynamicTrees.b2DynamicTree_Destroy(tree); }
        if (tree.proxyChanged is not null) throw new Exception("Destroying a tree must detach the proxy observer.");
        Console.WriteLine("Dynamic-tree category leaf guard and observer lifetime passed.");
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
        var vetoAll = false;
        var jointID = new B2JointId();
        world.customFilterFcn = (a, b, _) =>
        {
            if (owner != Environment.CurrentManagedThreadId) throw new Exception("GPU pair filters left the owner thread.");
            filters.Add((a.index1, b.index1));
            return !vetoAll && (a.index1 + b.index1) % 7 != 0;
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
            if (dense && gpu.BroadPhaseCandidateCount != expected.Count)
                throw new Exception("Built-in filtering must remove all rejected pairs before GPU readback.");
            if (dense && passes == 0 && expected.Count != count * (count - 1) / 2 - 2)
                throw new Exception("The dense fixture must exclude exactly the two joint-veto pairs.");
            passes++;
        };
        try
        {
            gpu.FindBroadPhasePairs(world);
            for (var i = 0; i < count; i++)
            {
                var bodyDef = b2DefaultBodyDef();
                bodyDef.type = dense || count <= 2 ? B2BodyType.b2_dynamicBody : (B2BodyType)(i % 3);
                bodyDef.position = dense ? new(0, 0) : count > 1000 ? new(i * 4, 0) : new(i % 8 * .4f, i / 8 * .4f);
                var body = b2CreateBody(id, bodyDef); bodies.Add(body);
                var shapeDef = b2DefaultShapeDef(); shapeDef.invokeContactCreation = true;
                shapeDef.enableCustomFiltering = !dense; shapeDef.isSensor = !dense && count > 2 && i % 17 == 0;
                shapeDef.filter.categoryBits = dense || count <= 2 ? 1UL : i % 11 == 0 ? 0UL : 1UL << (i % 64);
                shapeDef.filter.maskBits = dense || i % 5 == 0 ? ulong.MaxValue : 0xaaaaaaaaaaaaaaaaUL;
                shapeDef.filter.groupIndex = dense ? 0 : i % 9 < 2 ? 3 : i % 9 < 4 ? -3 : 0;
                shapes.Add(b2CreateCircleShape(body, shapeDef, new B2Circle { radius = .5f }));
                if (!dense && count > 2 && i % 13 == 0)
                    shapes.Add(b2CreateCircleShape(body, shapeDef, new B2Circle { center = new(.1f, 0), radius = .4f }));
            }
            if (count > 8)
            {
                var joint = B2Joints.b2DefaultRevoluteJointDef();
                joint.@base.bodyIdA = bodies[2]; joint.@base.bodyIdB = bodies[5]; joint.@base.collideConnected = false;
                jointID = B2Joints.b2CreateRevoluteJoint(id, joint);
                // A later allowed edge must not hide an earlier veto in either list.
                foreach (var (a, b, collide) in new[] { (2, 6, true), (2, 7, false), (2, 8, true), (5, 7, true) })
                {
                    joint.@base.bodyIdA = bodies[a]; joint.@base.bodyIdB = bodies[b]; joint.@base.collideConnected = collide;
                    B2Joints.b2CreateRevoluteJoint(id, joint);
                }
            }
            if (count == 65 && !dense)
            {
                // The initial GPU table must include contacts created by a CPU world.
                var oracle = world.findBroadPhasePairs;
                world.findBroadPhasePairs = null!;
                b2UpdateBroadPhasePairs(world);
                world.findBroadPhasePairs = oracle;
                foreach (var shape in shapes) b2BufferMove(world.broadPhase, world.shapes.data[shape.index1 - 1].proxyKey);
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
            if (count > 8)
            {
                B2Joints.b2Joint_SetCollideConnected(jointID, true);
                b2UpdateBroadPhasePairs(world);
                B2Joints.b2Joint_SetCollideConnected(jointID, false);
                foreach (var shape in shapes) b2BufferMove(world.broadPhase, world.shapes.data[shape.index1 - 1].proxyKey);
                b2UpdateBroadPhasePairs(world);
                B2Joints.b2DestroyJoint(jointID, true);
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
            if (dense)
            {
                // Swap two live contact IDs, with repeated destroy/create before
                // the next GPU query. The journal must publish only final keys.
                B2Contact Find(int a, int b) => world.contacts.data.Take(world.contacts.count).First(c => c.contactId >= 0 &&
                    ((c.shapeIdA == a && c.shapeIdB == b) || (c.shapeIdA == b && c.shapeIdB == a)));
                var a = Find(0, 1); var b = Find(0, 2);
                var idA = a.contactId; var idB = b.contactId;
                for (var iteration = 0; iteration < 3; iteration++)
                {
                    B2Contacts.b2DestroyContact(world, Find(0, 1), false);
                    B2Contacts.b2DestroyContact(world, Find(0, 2), false);
                    B2Contacts.b2CreateContact(world, world.shapes.data[0], world.shapes.data[1]);
                    B2Contacts.b2CreateContact(world, world.shapes.data[0], world.shapes.data[2]);
                }
                if (Find(0, 1).contactId != idB || Find(0, 2).contactId != idA)
                    throw new Exception("The contact fixture did not recycle IDs across pair identities.");
                foreach (var shape in shapes) b2BufferMove(world.broadPhase, world.shapes.data[shape.index1 - 1].proxyKey);
                b2UpdateBroadPhasePairs(world);

                var snapshots = gpu.PairTableSnapshotCount; var rebuilds = gpu.PairTableRebuildCount;
                // Removing all contacts crosses the tombstone budget. Rebuild from
                // resident keys, then recreate without uploading a full CPU snapshot.
                for (var i = 0; i < world.contacts.count; i++)
                    if (world.contacts.data[i].contactId >= 0) B2Contacts.b2DestroyContact(world, world.contacts.data[i], false);
                for (var pass = 0; pass < 2; pass++)
                {
                    foreach (var shape in shapes) b2BufferMove(world.broadPhase, world.shapes.data[shape.index1 - 1].proxyKey);
                    b2UpdateBroadPhasePairs(world);
                }
                if (gpu.PairTableSnapshotCount != snapshots || gpu.PairTableRebuildCount <= rebuilds)
                    throw new Exception("Pair-table tombstones must rehash on GPU without a full snapshot.");
            }
            // Retained capacities and unchanged topology, measured across all managed threads.
            for (var tick = 0; tick < 40; tick++)
            {
                foreach (var shape in shapes) b2BufferMove(world.broadPhase, world.shapes.data[shape.index1 - 1].proxyKey);
                b2UpdateBroadPhasePairs(world);
                B2ArenaAllocators.b2GrowArena(world.arena);
            }
            var uploadedBefore = gpu.PairTableUploadBytes;
            var snapshotsBefore = gpu.PairTableSnapshotCount;
            var treeUploadBefore = gpu.TreeUploadBytes;
            var treeRefitsBefore = gpu.TreeRefitCount;
            var filterUploadBefore = gpu.FilterUploadBytes;
            var before = GC.GetTotalAllocatedBytes(true);
            for (var tick = 0; tick < 16; tick++)
            {
                foreach (var shape in shapes) b2BufferMove(world.broadPhase, world.shapes.data[shape.index1 - 1].proxyKey);
                b2UpdateBroadPhasePairs(world);
            }
            var bytes = GC.GetTotalAllocatedBytes(true) - before;
            if (bytes != 0) throw new Exception($"Warmed CPU/GPU pair oracle allocated {bytes} bytes ({count}/{dense}).");
            if (gpu.PairTableUploadBytes != uploadedBefore || gpu.PairTableSnapshotCount != snapshotsBefore)
                throw new Exception("Unchanged contact topology must reuse the GPU pair table without uploads.");
            if (gpu.TreeUploadBytes != treeUploadBefore || gpu.TreeRefitCount != treeRefitsBefore)
                throw new Exception("Unchanged proxies must reuse the GPU tree without uploads or refitting.");
            if (gpu.FilterUploadBytes != filterUploadBefore)
                throw new Exception("Unchanged filters and joints must remain resident without uploads.");
            if (count == 65)
            {
                // Keep broad-phase candidates observable on every tick by vetoing
                // their creation. Move a high-key proxy queried by lower-key bodies.
                vetoAll = true;
                for (var i = 0; i < world.contacts.count; i++)
                    if (world.contacts.data[i].contactId >= 0) B2Contacts.b2DestroyContact(world, world.contacts.data[i], false);
                void Move(int tick)
                {
                    b2Body_SetTransform(bodies[62], new(2, tick % 2 == 0 ? 0 : 3.2f), new(1, 0));
                    foreach (var shape in shapes) b2BufferMove(world.broadPhase, world.shapes.data[shape.index1 - 1].proxyKey);
                    b2UpdateBroadPhasePairs(world);
                }
                for (var tick = 0; tick < 32; tick++) Move(tick);
                treeUploadBefore = gpu.TreeUploadBytes;
                var treeSnapshots = gpu.TreeSnapshotCount; var treeRebuilds = gpu.TreeRebuildCount;
                var treeUpdates = gpu.TreeUpdatedProxies;
                before = GC.GetTotalAllocatedBytes(true);
                for (var tick = 0; tick < 64; tick++) Move(tick);
                bytes = GC.GetTotalAllocatedBytes(true) - before;
                if (bytes != 0) throw new Exception($"Warmed GPU tree movement allocated {bytes} managed bytes.");
                if (gpu.TreeSnapshotCount != treeSnapshots || gpu.TreeUpdatedProxies - treeUpdates != 64 ||
                    gpu.TreeUploadBytes - treeUploadBefore != 64 * 32 || gpu.TreeRebuildCount <= treeRebuilds)
                    throw new Exception("GPU refit/rebuild must consume one changed proxy per tick without a full snapshot.");

                if (gpu.FilterUploadBytes != filterUploadBefore)
                    throw new Exception("Moving bodies must not reupload stable shape/joint metadata.");

                // The bullet lane enlarges the tree directly, bypassing broad-phase wrappers.
                var movingShape = world.shapes.data[world.bodies.data[bodies[62].index1 - 1].headShapeId];
                var tree = world.broadPhase.trees[(int)B2_PROXY_TYPE(movingShape.proxyKey)];
                var proxy = B2_PROXY_ID(movingShape.proxyKey);
                var bounds = tree.nodes[proxy].aabb;
                bounds.lowerBound = new(bounds.lowerBound.X - 3, bounds.lowerBound.Y - 3);
                bounds.upperBound = new(bounds.upperBound.X + 3, bounds.upperBound.Y + 3);
                B2DynamicTrees.b2DynamicTree_EnlargeProxy(tree, proxy, bounds);
                foreach (var shape in shapes) b2BufferMove(world.broadPhase, world.shapes.data[shape.index1 - 1].proxyKey);
                b2UpdateBroadPhasePairs(world);
                var category = tree.nodes[proxy].categoryBits;
                B2DynamicTrees.b2DynamicTree_SetCategoryBits(tree, proxy, 0);
                foreach (var shape in shapes) b2BufferMove(world.broadPhase, world.shapes.data[shape.index1 - 1].proxyKey);
                b2UpdateBroadPhasePairs(world);
                B2DynamicTrees.b2DynamicTree_SetCategoryBits(tree, proxy, category);
                foreach (var shape in shapes) b2BufferMove(world.broadPhase, world.shapes.data[shape.index1 - 1].proxyKey);
                b2UpdateBroadPhasePairs(world);
            }
            if (count == 65)
            {
                void Flush()
                {
                    foreach (var shape in shapes)
                    {
                        var key = world.shapes.data[shape.index1 - 1].proxyKey;
                        if (key != -1) b2BufferMove(world.broadPhase, key);
                    }
                    b2UpdateBroadPhasePairs(world);
                }
                // Mask/group-only changes do not recreate proxies. Repeated edits
                // before a query must upload the final value exactly once.
                void Refilter(int tick)
                {
                    var value = b2Shape_GetFilter(shapes[2]);
                    value.maskBits = 0; value.groupIndex = -3;
                    b2Shape_SetFilter(shapes[2], value);
                    value.maskBits = tick % 2 == 0 ? ulong.MaxValue : 0xaaaaaaaaaaaaaaaaUL;
                    value.groupIndex = tick % 2 == 0 ? 3 : 0;
                    b2Shape_SetFilter(shapes[2], value);
                    Flush();
                }
                for (var tick = 0; tick < 32; tick++) Refilter(tick);
                var snapshots = gpu.FilterSnapshotCount;
                var updates = gpu.FilterUpdatedShapes;
                filterUploadBefore = gpu.FilterUploadBytes;
                before = GC.GetTotalAllocatedBytes(true);
                for (var tick = 0; tick < 64; tick++) Refilter(tick);
                bytes = GC.GetTotalAllocatedBytes(true) - before;
                if (bytes != 0 || gpu.FilterSnapshotCount != snapshots || gpu.FilterUpdatedShapes - updates != 64 ||
                    gpu.FilterUploadBytes - filterUploadBefore != 64 * 48)
                    throw new Exception($"Refiltering must upload one final shape per tick with zero warmed bytes (allocated {bytes}).");

                // Disabled shapes still receive edits and become valid on enable.
                b2Body_Disable(bodies[1]);
                var disabled = b2Shape_GetFilter(shapes[2]); disabled.groupIndex = -7;
                b2Shape_SetFilter(shapes[2], disabled); Flush();
                b2Body_Enable(bodies[1]); Flush();
                b2Body_SetType(bodies[1], B2BodyType.b2_dynamicBody); Flush();

                // Reused slots must take the new sensor flag and body identity.
                var removed = shapes[^1];
                b2DestroyShape(removed, true); shapes.RemoveAt(shapes.Count - 1);
                Flush();
                var shapeDef = b2DefaultShapeDef(); shapeDef.invokeContactCreation = true;
                shapeDef.isSensor = true;
                var sensor = b2CreateCircleShape(bodies[2], shapeDef, new B2Circle { radius = .9f }); shapes.Add(sensor);
                if (sensor.index1 != removed.index1) throw new Exception("Sensor fixture must reuse the destroyed shape ID.");
                Flush();
                b2DestroyShape(sensor, true); shapes.RemoveAt(shapes.Count - 1);
                shapeDef.isSensor = false;
                shapes.Add(b2CreateCircleShape(bodies[5], shapeDef, new B2Circle { radius = .9f })); Flush();

                b2Body_SetTransform(bodies[5], new(.8f, 0), new(1, 0));
                b2Body_SetTransform(bodies[8], new(.8f, 0), new(1, 0));
                var joint = B2Joints.b2DefaultRevoluteJointDef();
                joint.@base.bodyIdA = bodies[2]; joint.@base.bodyIdB = bodies[5];
                var middle = B2Joints.b2CreateRevoluteJoint(id, joint);
                joint.@base.bodyIdB = bodies[7];
                var head = B2Joints.b2CreateRevoluteJoint(id, joint); Flush();
                B2Joints.b2DestroyJoint(middle, true); Flush();
                joint.@base.bodyIdA = bodies[5]; joint.@base.bodyIdB = bodies[8];
                var recycled = B2Joints.b2CreateRevoluteJoint(id, joint);
                if (recycled.index1 != middle.index1) throw new Exception("Joint fixture must reuse the removed ID.");
                Flush();
                void ToggleJoint(int tick)
                {
                    B2Joints.b2Joint_SetCollideConnected(recycled, tick % 2 == 0);
                    Flush();
                }
                for (var tick = 0; tick < 32; tick++) ToggleJoint(tick);
                snapshots = gpu.FilterSnapshotCount; filterUploadBefore = gpu.FilterUploadBytes;
                var jointsBefore = gpu.FilterUpdatedJoints;
                updates = gpu.FilterUpdatedShapes;
                var endpointShapes = world.bodies.data[bodies[5].index1 - 1].shapeCount + world.bodies.data[bodies[8].index1 - 1].shapeCount;
                before = GC.GetTotalAllocatedBytes(true);
                for (var tick = 0; tick < 64; tick++) ToggleJoint(tick);
                bytes = GC.GetTotalAllocatedBytes(true) - before;
                if (bytes != 0 || gpu.FilterSnapshotCount != snapshots || gpu.FilterUpdatedJoints - jointsBefore != 64 ||
                    gpu.FilterUpdatedShapes - updates != 64 * endpointShapes || gpu.FilterUploadBytes - filterUploadBefore != 64 * (32 + 48 * endpointShapes))
                    throw new Exception($"Joint edits must update only their adjacency metadata with zero warmed bytes (allocated {bytes}).");
                B2Joints.b2DestroyJoint(head, true); Flush();
                b2DestroyBody(bodies[5]);
                shapes.RemoveAll(shape => !b2Shape_IsValid(shape)); Flush();

                // Growth, lost observers and epoch wrap must replace the complete
                // snapshot rather than trusting previously retained markers.
                snapshots = gpu.FilterSnapshotCount;
                for (var i = 0; i < 130; i++)
                    shapes.Add(b2CreateCircleShape(bodies[2], shapeDef, new B2Circle { center = new(100 + i * 2, 0), radius = .5f }));
                Flush();
                if (gpu.FilterSnapshotCount != snapshots + 1) throw new Exception("Filter buffer growth must restore all live records.");
                world.shapeFilterChanged = null!;
                Refilter(0);
                if (gpu.FilterSnapshotCount != snapshots + 2) throw new Exception("Lost filter observers require a new snapshot.");
                typeof(GPUPhysicsWorld).GetField("_filterEpoch", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(gpu, -1);
                Flush();
                if (gpu.FilterSnapshotCount != snapshots + 3) throw new Exception("Moved epoch wrap must reset stale markers.");
            }
            if (dense)
            {
                var idA = world.contacts.data.Take(world.contacts.count).First(c => c.contactId >= 0 &&
                    ((c.shapeIdA == 0 && c.shapeIdB == 1) || (c.shapeIdA == 1 && c.shapeIdB == 0))).contactId;
                var idB = world.contacts.data.Take(world.contacts.count).First(c => c.contactId >= 0 &&
                    ((c.shapeIdA == 0 && c.shapeIdB == 2) || (c.shapeIdA == 2 && c.shapeIdB == 0))).contactId;
                void Churn(int tick)
                {
                    var first = tick % 2 == 0 ? idA : idB; var second = tick % 2 == 0 ? idB : idA;
                    B2Contacts.b2DestroyContact(world, world.contacts.data[first], false);
                    B2Contacts.b2DestroyContact(world, world.contacts.data[second], false);
                    B2Contacts.b2CreateContact(world, world.shapes.data[0], world.shapes.data[1]);
                    B2Contacts.b2CreateContact(world, world.shapes.data[0], world.shapes.data[2]);
                    foreach (var shape in shapes) b2BufferMove(world.broadPhase, world.shapes.data[shape.index1 - 1].proxyKey);
                    b2UpdateBroadPhasePairs(world);
                }
                for (var tick = 0; tick < 32; tick++) Churn(tick);
                uploadedBefore = gpu.PairTableUploadBytes; snapshotsBefore = gpu.PairTableSnapshotCount;
                var slotsBefore = gpu.PairTableUpdatedSlots;
                before = GC.GetTotalAllocatedBytes(true);
                for (var tick = 0; tick < 64; tick++) Churn(tick);
                bytes = GC.GetTotalAllocatedBytes(true) - before;
                if (bytes != 0) throw new Exception($"Warmed resident pair churn allocated {bytes} managed bytes.");
                if (gpu.PairTableSnapshotCount != snapshotsBefore || gpu.PairTableUpdatedSlots - slotsBefore != 128 ||
                    gpu.PairTableUploadBytes - uploadedBefore != 128 * 16)
                    throw new Exception("Active pair churn must upload exactly two final 16-byte slot updates per tick.");
            }
        }
        finally
        {
            world.findBroadPhasePairs = null!;
            b2DestroyWorld(id);
            if (world.shapeFilterChanged is not null || world.jointFilterChanged is not null)
                throw new Exception("Destroying a world must detach its filter observers.");
        }
    }
}
