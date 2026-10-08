using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2BoardPhases;
using static Box2D.NET.B2Contacts;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

internal static partial class GPUPhysicsTests
{
    internal static void VerifyContactCreation()
    {
        using var gpu = new GPUPhysicsWorld();
        foreach (var count in new[] { 0, 1, 2, 63, 64, 65, 257 })
            VerifyContactCreation(gpu, count, count == 65);
        Console.WriteLine("GPU contact creation and resident adjacency match CPU identities, generations, callback-visible links, topology edits, sleeping sets, mixed pool edits and warmed allocation.");
    }

    private static void VerifyContactCreation(GPUPhysicsWorld gpu, int count, bool custom)
    {
        var owner = Environment.CurrentManagedThreadId;
        var expectedCalls = new List<(int, ulong, ulong, ulong)>(Math.Max(16, count * count * 2));
        var actualCalls = new List<(int, ulong, ulong, ulong)>(expectedCalls.Capacity);
        B2WorldId Create(List<(int, ulong, ulong, ulong)> calls)
        {
            B2World? callbackWorld = null;
            ulong Adjacency()
            {
                var w = callbackWorld!; ulong hash = 14695981039346656037;
                for (var i = 0; i < w.bodies.count; i++)
                {
                    hash = unchecked((hash ^ (uint)w.bodies.data[i].headContactKey) * 1099511628211);
                    hash = unchecked((hash ^ (uint)w.bodies.data[i].contactCount) * 1099511628211);
                }
                for (var i = 0; i < w.contacts.count; i++)
                    if (w.contacts.data[i].contactId >= 0)
                        for (var e = 0; e < 2; e++)
                        {
                            hash = unchecked((hash ^ (uint)w.contacts.data[i].edges[e].prevKey) * 1099511628211);
                            hash = unchecked((hash ^ (uint)w.contacts.data[i].edges[e].nextKey) * 1099511628211);
                        }
                return hash;
            }
            var definition = b2DefaultWorldDef();
            if (custom)
            {
                definition.frictionCallback = (a, ma, b, mb) =>
                {
                    if (Environment.CurrentManagedThreadId != owner) throw new Exception("Contact creation callbacks left the owner.");
                    calls.Add((0, ma, mb, Adjacency())); return MathF.Abs(a - b) + .1f;
                };
                definition.restitutionCallback = (a, ma, b, mb) => { calls.Add((1, ma, mb, Adjacency())); return .2f * (a + b); };
            }
            else if (count % 2 == 0)
            {
                definition.frictionCallback = PhysicsSpace.CombineFriction;
                definition.restitutionCallback = PhysicsSpace.CombineBounce;
            }
            var id = b2CreateWorld(definition); callbackWorld = b2GetWorldFromId(id);
            for (var i = 0; i < count; i++)
            {
                var body = b2DefaultBodyDef(); body.type = count <= 2 ? B2BodyType.b2_dynamicBody : (B2BodyType)(i % 3);
                body.position = new(i % 4 * .05f, 0); body.isAwake = i % 7 != 0;
                var bodyID = b2CreateBody(id, body);
                var shape = b2DefaultShapeDef(); shape.invokeContactCreation = true;
                shape.enableContactEvents = i % 2 == 0; shape.enablePreSolveEvents = i % 3 == 0;
                shape.material.friction = .2f + i % 5 * .1f; shape.material.restitution = i % 4 * .2f;
                shape.material.userMaterialId = ((ulong)(uint)(i + 1) << 32) | (uint)(i % 4);
                switch (i % 5)
                {
                    case 0: b2CreateCircleShape(bodyID, shape, new B2Circle { radius = 1 }); break;
                    case 1: b2CreateCapsuleShape(bodyID, shape, new B2Capsule(new(-.6f, 0), new(.6f, 0), .6f)); break;
                    case 2: b2CreateSegmentShape(bodyID, shape, new B2Segment { point1 = new(-1, 0), point2 = new(1, 0) }); break;
                    case 3: b2CreatePolygonShape(bodyID, shape, b2MakeBox(.8f, .8f)); break;
                    default:
                        b2CreateShape(bodyID, shape, new B2ChainSegment { ghost1 = new(-2, 0), segment = new() { point1 = new(-1, 0), point2 = new(1, 0) }, ghost2 = new(2, 0), chainId = -1 }, B2ShapeType.b2_chainSegmentShape);
                        break;
                }
            }
            return id;
        }
        var cpuID = Create(expectedCalls); var gpuID = Create(actualCalls);
        var cpu = b2GetWorldFromId(cpuID); var actual = b2GetWorldFromId(gpuID);
        void Compare()
        {
            var a = cpu.contactIdPool; var b = actual.contactIdPool;
            if (a.nextIndex != b.nextIndex || a.freeArray.count != b.freeArray.count || cpu.contacts.count != actual.contacts.count)
                throw new Exception("GPU contact pool counts differ.");
            for (var i = 0; i < a.freeArray.count; i++) if (a.freeArray.data[i] != b.freeArray.data[i]) throw new Exception("Contact free-stack order differs.");
            for (var i = 0; i < cpu.contacts.count; i++)
            {
                var e = cpu.contacts.data[i]; var c = actual.contacts.data[i];
                if (e.contactId != c.contactId || e.generation != c.generation || e.setIndex != c.setIndex || e.colorIndex != c.colorIndex ||
                    e.localIndex != c.localIndex || e.islandId != c.islandId || e.flags != c.flags || e.shapeIdA != c.shapeIdA || e.shapeIdB != c.shapeIdB)
                    throw new Exception($"GPU contact identity/state differs at {i}/{count}.");
                if (e.contactId < 0) continue;
                for (var edge = 0; edge < 2; edge++)
                    if (e.edges[edge].bodyId != c.edges[edge].bodyId || e.edges[edge].prevKey != c.edges[edge].prevKey || e.edges[edge].nextKey != c.edges[edge].nextKey)
                        throw new Exception("GPU contact body adjacency differs.");
                var es = b2GetContactSim(cpu, e); var cs = b2GetContactSim(actual, c);
                if (es.contactId != cs.contactId || es.generation != cs.generation || cs.generation != c.generation || es.shapeIdA != cs.shapeIdA || es.shapeIdB != cs.shapeIdB || es.simFlags != cs.simFlags ||
                    es.manifold.pointCount != cs.manifold.pointCount || es.bodySimIndexA != cs.bodySimIndexA || es.bodySimIndexB != cs.bodySimIndexB)
                    throw new Exception("GPU initial contact simulation differs.");
                Near(es.friction, cs.friction); Near(es.restitution, cs.restitution); Near(es.tangentSpeed, cs.tangentSpeed);
            }
            for (var i = 0; i < cpu.bodies.count; i++)
                if (cpu.bodies.data[i].contactCount != actual.bodies.data[i].contactCount || cpu.bodies.data[i].headContactKey != actual.bodies.data[i].headContactKey)
                    throw new Exception("GPU body contact heads/counts differ.");
            if (expectedCalls.Count != actualCalls.Count) throw new Exception("Contact material callback counts differ.");
            for (var i = 0; i < expectedCalls.Count; i++) if (expectedCalls[i] != actualCalls[i]) throw new Exception("Contact material callback order differs.");
            expectedCalls.Clear(); actualCalls.Clear();
        }
        static void Buffer(B2World world)
        {
            for (var i = 0; i < world.shapes.count; i++)
                if (world.shapes.data[i].proxyKey != -1) b2BufferMove(world.broadPhase, world.shapes.data[i].proxyKey);
        }
        static void Destroy(B2World world)
        {
            for (var i = 0; i < world.contacts.count; i++)
                if (world.contacts.data[i].contactId >= 0) b2DestroyContact(world, world.contacts.data[i], false);
        }
        void Query() { Buffer(cpu); Buffer(actual); b2UpdateBroadPhasePairs(cpu); b2UpdateBroadPhasePairs(actual); Compare(); gpu.ValidateContactLinks(actual); }
        void Churn() { Destroy(cpu); Destroy(actual); Query(); }
        try
        {
            // Attach after CPU-created contacts for one fixture; other worlds start on GPU.
            if (count == 64) { b2UpdateBroadPhasePairs(cpu); b2UpdateBroadPhasePairs(actual); Compare(); }
            gpu.EnableContactCreation(actual);
            Query();
            var sleeping = false;
            for (var i = 0; i < cpu.contacts.count; i++) sleeping |= cpu.contacts.data[i].setIndex == (int)B2SolverSetType.b2_disabledSet;
            if (count >= 63 && !sleeping) throw new Exception("The creation fixture must contain sleeping non-touching contacts.");
            for (var i = 0; i < 32; i++) { Churn(); B2ArenaAllocators.b2GrowArena(cpu.arena); B2ArenaAllocators.b2GrowArena(actual.arena); }
            var snapshots = gpu.ContactPoolSnapshotCount;
            var linkBytes = gpu.ContactLinkUploadBytes;
            Query();
            if (gpu.ContactLinkUploadBytes != linkBytes) throw new Exception("Unchanged adjacency must stay resident without uploads.");
            var before = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 16; i++) Churn();
            var bytes = GC.GetTotalAllocatedBytes(true) - before;
            if (bytes != 0 || gpu.ContactPoolSnapshotCount != snapshots)
                throw new Exception($"Warmed GPU contact creation allocated {bytes} bytes or reset its resident pool.");
            if (cpu.contacts.count > 0)
            {
                // CPU allocations remain legal mirror mutations; replay mixed frees/allocations in order.
                actual.createBroadPhaseContacts = null!;
                Churn();
                gpu.EnableContactCreation(actual);
                b2DestroyContact(cpu, cpu.contacts.data[0], false); b2DestroyContact(actual, actual.contacts.data[0], false);
                Query();
                // Losing the observer must recover the native final pool and generations.
                actual.contactIdPool.changed = null!;
                cpu.contacts.data[0].generation = actual.contacts.data[0].generation = uint.MaxValue;
                b2DestroyContact(cpu, cpu.contacts.data[0], false); b2DestroyContact(actual, actual.contacts.data[0], false);
                Query();
                if (actual.contacts.data[0].generation != 0) throw new Exception("GPU contact generation must preserve unsigned wrap.");
            }
            if (count == 2)
            {
                var contact = actual.contacts.data[0]; var sim = b2GetContactSim(actual, contact);
                var context = new B2StepContext { world = actual, contacts = new[] { sim } };
                gpu.GenerateManifolds(context, 1);
                var copied = new B2ContactSim(); copied.CopyFrom(sim);
                if (copied.generation != contact.generation) throw new Exception("Contact copies lost their identity generation.");
                sim.generation++;
                try { gpu.GenerateManifolds(context, 1); throw new Exception("GPU lookup accepted a stale contact generation."); }
                catch (InvalidOperationException ex) when (ex.Message.Contains("invalid manifold", StringComparison.Ordinal)) { }
                sim.generation--;
                gpu.GenerateManifolds(context, 1);
            }
            if (count == 63)
            {
                // Remove adjacent old links, recycle a body ID, change topology without pool growth.
                b2DestroyBody(b2MakeBodyId(cpu, 2)); b2DestroyBody(b2MakeBodyId(actual, 2));
                var bd = b2DefaultBodyDef(); bd.type = B2BodyType.b2_dynamicBody;
                var sd = b2DefaultShapeDef(); sd.invokeContactCreation = true;
                b2CreateCircleShape(b2CreateBody(cpuID, bd), sd, new B2Circle { radius = 1 });
                b2CreateCircleShape(b2CreateBody(gpuID, bd), sd, new B2Circle { radius = 1 });
                Query();
                b2Body_Disable(b2MakeBodyId(cpu, 5)); b2Body_Disable(b2MakeBodyId(actual, 5));
                Query();
                b2Body_Enable(b2MakeBodyId(cpu, 5)); b2Body_Enable(b2MakeBodyId(actual, 5));
                Query();
                b2Body_SetType(b2MakeBodyId(cpu, 5), B2BodyType.b2_staticBody);
                b2Body_SetType(b2MakeBodyId(actual, 5), B2BodyType.b2_staticBody);
                Query();
                actual.contactLinksChanged = null!;
                Destroy(cpu); Destroy(actual); Query();
            }
            if (count == 65)
            {
                // Force growth after an established resident pool.
                var bd = b2DefaultBodyDef(); bd.type = B2BodyType.b2_dynamicBody;
                var sd = b2DefaultShapeDef(); sd.invokeContactCreation = true;
                for (var i = 0; i < 100; i++)
                {
                    b2CreateCircleShape(b2CreateBody(cpuID, bd), sd, new B2Circle { radius = 1 });
                    b2CreateCircleShape(b2CreateBody(gpuID, bd), sd, new B2Circle { radius = 1 });
                }
                Query();
                if (gpu.ContactPoolSnapshotCount <= snapshots) throw new Exception("GPU contact growth must restore its pool snapshot.");
            }
            if (count == 257)
            {
                using (var other = new GPUPhysicsWorld())
                {
                    other.EnableContactCreation(actual);
                    var creator = actual.createBroadPhaseContacts; var observer = actual.contactIdPool.changed; var links = actual.contactLinksChanged;
                    gpu.Dispose();
                    if (actual.createBroadPhaseContacts != creator || actual.contactIdPool.changed != observer || actual.contactLinksChanged != links)
                        throw new Exception("Disposal must preserve another host's contact creator and observer.");
                }
                if (actual.createBroadPhaseContacts is not null || actual.contactIdPool.changed is not null || actual.contactLinksChanged is not null)
                    throw new Exception("The contact creator must detach on disposal.");
            }
        }
        finally
        {
            b2DestroyWorld(cpuID); b2DestroyWorld(gpuID);
            if (actual.createBroadPhaseContacts is not null || actual.contactLinksChanged is not null) throw new Exception("World teardown must clear the contact creator.");
        }
    }
}
