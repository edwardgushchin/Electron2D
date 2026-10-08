using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2BoardPhases;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

internal static partial class GPUPhysicsTests
{
    internal static void VerifyContactRemovals(bool islandChanges = false)
    {
        using var gpu = new GPUPhysicsWorld();
        foreach (var count in new[] { 0, 1, 2, 63, 64, 65, 129, 257 }) VerifyContactRemovals(gpu, count, islandChanges);
        Console.WriteLine("GPU contact removal preserves body links, free-ID order, graph slots, begin/end events, callback-visible topology and warmed allocation.");
    }

    private static void VerifyContactRemovals(GPUPhysicsWorld gpu, int count, bool islandChanges)
    {
        var owner = Environment.CurrentManagedThreadId;
        var expectedCalls = new List<(int, int, int, int, int, int)>(Math.Max(16, count * count));
        var actualCalls = new List<(int, int, int, int, int, int)>(expectedCalls.Capacity);
        B2Vec2 Position(int i, bool apart) => count == 257 ? new(2 * i, apart && i % 3 != 0 ? 20 : .7f) :
            new(apart && (i % 3 != 0 || count is 1 or 64) ? 100 + 4 * i : (i % 4) * .1f, 0);
        B2WorldId Create(List<(int, int, int, int, int, int)> calls)
        {
            var def = b2DefaultWorldDef(); def.gravity = new(0, 0);
            var id = b2CreateWorld(def); var world = b2GetWorldFromId(id);
            if (count == 65)
                world.preSolveFcn = (a, b, _, _, _) =>
                {
                    if (Environment.CurrentManagedThreadId != owner) throw new Exception("Removal callbacks left the owner.");
                    var ba = world.bodies.data[world.shapes.data[a.index1 - 1].bodyId];
                    var bb = world.bodies.data[world.shapes.data[b.index1 - 1].bodyId];
                    calls.Add((a.index1, b.index1, ba.headContactKey, ba.contactCount, bb.headContactKey, bb.contactCount));
                    return true;
                };
            var bd = b2DefaultBodyDef(); var sd = b2DefaultShapeDef();
            sd.enableContactEvents = true; sd.enablePreSolveEvents = true; sd.invokeContactCreation = true;
            var ground = b2CreateBody(id, bd);
            if (count == 257) b2CreatePolygonShape(ground, sd, b2MakeBox(2 * count + 1, .25f));
            else b2CreateCircleShape(ground, sd, new B2Circle { radius = 1 });
            bd.type = B2BodyType.b2_dynamicBody; bd.enableSleep = false;
            for (var i = 0; i < count; i++)
            {
                bd.position = Position(i, false);
                b2CreateCircleShape(b2CreateBody(id, bd), sd, new B2Circle { radius = count == 257 ? .5f : 1 });
            }
            return id;
        }
        var cpuID = Create(expectedCalls); var gpuID = Create(actualCalls);
        var cpu = b2GetWorldFromId(cpuID); var actual = b2GetWorldFromId(gpuID);
        var cpuContext = new B2StepContext { world = cpu }; var gpuContext = new B2StepContext { world = actual };
        gpu.EnableContactCreation(actual); actual.generateManifolds = gpu.UpdateContacts;
        if (islandChanges) { gpu.EnableIslandChanges(actual); gpu.EnableIslandSplitting(actual); }
        void Move(B2World w, bool apart)
        {
            for (var i = 0; i < count; i++) b2Body_SetTransform(b2MakeBodyId(w, i + 1), Position(i, apart), new(1, 0));
        }
        static void Collide(B2World world, B2StepContext context)
        {
            b2Array_Clear(ref world.contactBeginEvents);
            b2Array_Clear(ref world.contactEndEvents[world.endEventArrayIndex]);
            world.locked = true;
            try { b2UpdateBroadPhasePairs(world); b2Collide(context); }
            finally { world.locked = false; }
            B2ArenaAllocators.b2GrowArena(world.arena);
        }
        void Compare()
        {
            var a = cpu.contactIdPool; var b = actual.contactIdPool;
            if (a.nextIndex != b.nextIndex || a.freeArray.count != b.freeArray.count) throw new Exception("Removal pool counts differ.");
            for (var i = 0; i < a.freeArray.count; i++) if (a.freeArray.data[i] != b.freeArray.data[i]) throw new Exception("GPU freed IDs out of order.");
            for (var i = 0; i < cpu.contacts.count; i++)
            {
                var e = cpu.contacts.data[i]; var c = actual.contacts.data[i];
                if (e.contactId != c.contactId || e.generation != c.generation || e.setIndex != c.setIndex || e.colorIndex != c.colorIndex ||
                    e.localIndex != c.localIndex || e.flags != c.flags || e.islandId != c.islandId)
                    throw new Exception($"Removal contact/graph state differs at {i}/{count}.");
                if (e.contactId < 0) continue;
                for (var side = 0; side < 2; side++)
                    if (e.edges[side].bodyId != c.edges[side].bodyId || e.edges[side].prevKey != c.edges[side].prevKey || e.edges[side].nextKey != c.edges[side].nextKey)
                        throw new Exception("Removal body links differ.");
            }
            for (var i = 0; i < cpu.bodies.count; i++)
                if (cpu.bodies.data[i].headContactKey != actual.bodies.data[i].headContactKey || cpu.bodies.data[i].contactCount != actual.bodies.data[i].contactCount)
                    throw new Exception("Removal body head/count differs.");
            var beginA = cpu.contactBeginEvents; var beginB = actual.contactBeginEvents;
            var endA = cpu.contactEndEvents[cpu.endEventArrayIndex]; var endB = actual.contactEndEvents[actual.endEventArrayIndex];
            if (beginA.count != beginB.count || endA.count != endB.count) throw new Exception("Removal event counts differ.");
            for (var i = 0; i < beginA.count; i++)
                if (beginA.data[i].contactId.index1 != beginB.data[i].contactId.index1 || beginA.data[i].contactId.generation != beginB.data[i].contactId.generation ||
                    beginA.data[i].shapeIdA.index1 != beginB.data[i].shapeIdA.index1 || beginA.data[i].shapeIdB.index1 != beginB.data[i].shapeIdB.index1)
                    throw new Exception("Removal begin-event order differs.");
            for (var i = 0; i < endA.count; i++)
                if (endA.data[i].contactId.index1 != endB.data[i].contactId.index1 || endA.data[i].contactId.generation != endB.data[i].contactId.generation ||
                    endA.data[i].shapeIdA.index1 != endB.data[i].shapeIdA.index1 || endA.data[i].shapeIdB.index1 != endB.data[i].shapeIdB.index1)
                    throw new Exception("Removal end-event order differs.");
            if (expectedCalls.Count != actualCalls.Count) throw new Exception("Removal callback counts differ.");
            for (var i = 0; i < expectedCalls.Count; i++) if (expectedCalls[i] != actualCalls[i]) throw new Exception("Callbacks saw different pre-removal topology.");
            expectedCalls.Clear(); actualCalls.Clear();
            gpu.ValidateContactLinks(actual);
            CompareIslands(cpu, actual);
        }
        void Frame(bool apart)
        {
            Move(cpu, apart); Move(actual, apart); Collide(cpu, cpuContext); Collide(actual, gpuContext); Compare();
            if (islandChanges && apart && count > 0)
            {
                // Force disconnected components back apart so every cycle must merge again.
                B2Islands.b2SplitIsland(cpu, cpu.bodies.data[1].islandId);
                B2Islands.b2SplitIsland(actual, actual.bodies.data[1].islandId);
                B2ArenaAllocators.b2GrowArena(cpu.arena); Compare();
            }
        }
        try
        {
            Frame(false);
            if (islandChanges && count == 63 && gpu.IslandGraphReadbackRetries == 0) throw new Exception("Dense initial graph must exercise compact-output growth.");
            for (var i = 0; i < 32; i++) { Frame(true); Frame(false); }
            var snapshots = gpu.ContactPoolSnapshotCount; var links = gpu.ContactLinkUploadBytes; var poolBytes = gpu.ContactPoolUploadBytes;
            var removed = gpu.RemovedContactCount; var changes = gpu.IslandChangeCount; var merges = gpu.MergedIslandCount;
            var graphSnapshots = gpu.IslandGraphSnapshotCount; var graphRetries = gpu.IslandGraphReadbackRetries;
            var before = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 16; i++) { Frame(true); Frame(false); }
            var bytes = GC.GetTotalAllocatedBytes(true) - before;
            if (bytes != 0 || gpu.ContactPoolSnapshotCount != snapshots || gpu.ContactLinkUploadBytes != links || gpu.ContactPoolUploadBytes != poolBytes)
                throw new Exception($"Warmed GPU removal allocated {bytes} bytes or reuploaded retained contact topology.");
            if (islandChanges && (gpu.IslandGraphSnapshotCount != graphSnapshots || gpu.IslandGraphReadbackRetries != graphRetries))
                throw new Exception("Warmed island changes must retain the graph and compact-output capacity.");
            if (count >= 1 && gpu.RemovedContactCount == removed) throw new Exception("The fixture must remove contacts on GPU.");
            if (islandChanges && count > 0 && (gpu.IslandChangeCount == changes || count is > 1 and < 257 && gpu.MergedIslandCount == merges))
                throw new Exception("The island fixture must change membership and merge on GPU.");
            // External CPU destruction remains coherent with the resident GPU graph.
            if (count > 2)
            {
                b2DestroyBody(b2MakeBodyId(cpu, count)); b2DestroyBody(b2MakeBodyId(actual, count));
                Collide(cpu, cpuContext); Collide(actual, gpuContext); Compare();
                if (islandChanges && count == 65)
                {
                    B2BodyId Replace(B2WorldId id)
                    {
                        var bd = b2DefaultBodyDef(); bd.type = B2BodyType.b2_dynamicBody; bd.position = new(1000, 0);
                        var body = b2CreateBody(id, bd); var sd = b2DefaultShapeDef(); sd.enableContactEvents = true; sd.enablePreSolveEvents = true;
                        b2CreateCircleShape(body, sd, new B2Circle { radius = 1 }); return body;
                    }
                    var a = Replace(cpuID); var b = Replace(gpuID);
                    var ja = SplitJoint(cpuID, b2MakeBodyId(cpu, 1), a, 0); var jb = SplitJoint(gpuID, b2MakeBodyId(actual, 1), b, 0);
                    Frame(false); // Native authoring merge and recycled body/island ID reach the resident graph.
                    B2Joints.b2DestroyJoint(ja, true); B2Joints.b2DestroyJoint(jb, true);
                    foreach (var type in new[] { B2BodyType.b2_staticBody, B2BodyType.b2_kinematicBody, B2BodyType.b2_dynamicBody })
                    {
                        b2Body_SetType(a, type); b2Body_SetType(b, type); Frame(true); Frame(false);
                    }
                    b2Body_Disable(a); b2Body_Disable(b); Collide(cpu, cpuContext); Collide(actual, gpuContext); Compare();
                    b2Body_Enable(a); b2Body_Enable(b); Frame(true); Frame(false);
                    var savedSplit = actual.splitIsland; actual.splitIsland = null!;
                    Frame(true); Frame(false); actual.splitIsland = savedSplit; // CPU split publication uses the same journal.
                    var oldSnapshots = gpu.IslandGraphSnapshotCount;
                    actual.islandGraphChanged = null!; Frame(true);
                    if (gpu.IslandGraphSnapshotCount != oldSnapshots + 1) throw new Exception("Lost graph observer must force a complete resynchronization.");
                    // Capacity growth also replaces GPU storage, so its next use must snapshot.
                    oldSnapshots = gpu.IslandGraphSnapshotCount;
                    for (var i = 0; i < 260; i++) { Replace(cpuID); Replace(gpuID); }
                    Frame(false);
                    if (gpu.IslandGraphSnapshotCount != oldSnapshots + 1) throw new Exception("Growing graph buffers must restore all retained records.");
                }
            }
        }
        finally
        {
            cpu.integrateBodyStage = static (_, _) => throw new Exception("A recycled world retained integration.");
            cpu.solveConstraints = static _ => throw new Exception("A recycled world retained solving.");
            cpu.reusableStepContext.states = Array.Empty<B2BodyState>();
            actual.reusableStepContext.generatedManifoldOwner = gpu;
            b2DestroyWorld(cpuID); b2DestroyWorld(gpuID);
            if (cpu.integrateBodyStage is not null || cpu.solveConstraints is not null || actual.generateManifolds is not null ||
                actual.destroyDisjointContact is not null || actual.finishContactRemovals is not null ||
                actual.islandGraphChanged is not null || actual.beginIslandChanges is not null || actual.changeContactIsland is not null || actual.finishIslandChanges is not null ||
                cpu.reusableStepContext.states is not null || actual.reusableStepContext.generatedManifoldOwner is not null)
                throw new Exception("World reset must detach every GPU stage callback.");
        }
    }
}
