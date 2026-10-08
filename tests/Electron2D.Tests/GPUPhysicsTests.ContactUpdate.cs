using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

internal static partial class GPUPhysicsTests
{
    internal static void VerifyContactUpdates()
    {
        // The second point must be checked independently of the first one.
        var pruning = new B2Manifold { pointCount = 2 };
        pruning.points[0].separation = -.01f; pruning.points[1].separation = .02f;
        B2Contacts.b2PruneSpeculativePoints(ref pruning);
        if (pruning.pointCount != 1 || pruning.points[0].separation != -.01f)
            throw new Exception("Speculative pruning must remove a separated second point.");
        using var gpu = new GPUPhysicsWorld();
        const int count = 129;
        using var scheduler = new PhysicsTaskScheduler(4);
        var definition = b2DefaultWorldDef(); definition.workerCount = 4;
        definition.enqueueTask = scheduler.Enqueue; definition.finishTask = scheduler.Finish;
        var id = b2CreateWorld(definition); var world = b2GetWorldFromId(id);
        scheduler.Bind(world);
        var inputs = new B2ContactSim[count]; var expected = new B2ContactSim[count]; var actual = new B2ContactSim[count];
        var expectedBits = new bool[count];
        var expectedCalls = new List<(int Kind, ulong A, ulong B, B2Vec2 Point, B2Vec2 Normal)>(count * 3);
        var actualCalls = new List<(int Kind, ulong A, ulong B, B2Vec2 Point, B2Vec2 Normal)>(count * 3);
        var calls = expectedCalls;
        var owner = Environment.CurrentManagedThreadId;
        void Callback(int kind, ulong a, ulong b, B2Vec2 point = default, B2Vec2 normal = default)
        {
            if (Environment.CurrentManagedThreadId != owner) throw new Exception("GPU contact callbacks must run on the owner.");
            calls.Add((kind, a, b, point, normal));
        }
        b2FrictionCallback friction = (a, fa, b, fb) => { Callback(0, fa, fb); return MathF.Abs(a - b) + .03f * (fa >> 40); };
        b2RestitutionCallback bounce = (a, fa, b, fb) => { Callback(1, fa, fb); return .1f + .2f * (a + b); };
        b2PreSolveFcn preSolve = (a, b, point, normal, _) =>
        {
            Callback(2, (uint)a.index1, (uint)b.index1, point, normal);
            return a.index1 % 4 == 1;
        };
        var shapes = new B2ShapeId[2 * count];
        try
        {
            for (var i = 0; i < count; i++)
            {
                var body = b2DefaultBodyDef(); body.type = i % 3 == 0 ? B2BodyType.b2_staticBody : B2BodyType.b2_dynamicBody;
                body.position = new(i * 4, 0);
                var a = b2CreateBody(id, body);
                body.type = B2BodyType.b2_dynamicBody;
                body.position = new(i * 4 + .05f, i % 11 == 0 ? 5 : 1.6f + (i % 7 - 3) * .004f);
                var angle = (i % 3 - 1) * .008f; body.rotation = new(MathF.Cos(angle), MathF.Sin(angle));
                var b = b2CreateBody(id, body);
                var shape = b2DefaultShapeDef();
                shape.enableHitEvents = i % 3 == 0;
                shape.material = new() { friction = .2f + i % 7 * .3f, restitution = i % 4 * .4f, rollingResistance = i % 5 * .05f, tangentSpeed = .25f, userMaterialId = (uint)(i % 4) | (3UL << 40) };
                var poly = b2MakeOffsetBox(.8f, .8f, new(.1f, 0), new(1, 0)); poly.radius = .03f;
                shapes[2 * i] = b2CreatePolygonShape(a, shape, poly);
                shape.enableHitEvents = i % 4 == 0;
                shape.material = new() { friction = .4f, restitution = .3f, rollingResistance = .07f, tangentSpeed = -.1f, userMaterialId = (uint)((i + 1) % 4) | (5UL << 40) };
                shapes[2 * i + 1] = b2CreatePolygonShape(b, shape, b2MakeBox(.8f, .8f));
                var sa = b2GetBodySim(world, world.bodies.data[a.index1 - 1]);
                sa.surfaceLinearVelocity = new(.2f, -.1f); sa.surfaceAngularVelocity = .4f;
                var old = CollisionReference(world.shapes.data[shapes[2 * i].index1 - 1], sa.transform,
                    world.shapes.data[shapes[2 * i + 1].index1 - 1], b2GetBodySim(world, world.bodies.data[b.index1 - 1]).transform);
                old.rollingImpulse = .02f;
                for (var j = 0; j < old.pointCount; j++) { old.points[j].normalImpulse = .4f + j; old.points[j].tangentImpulse = -.03f * (j + 1); }
                if (i % 4 == 0) old.pointCount = 0;
                inputs[i] = new()
                {
                    contactId = i,
                    shapeIdA = shapes[2 * i].index1 - 1,
                    shapeIdB = shapes[2 * i + 1].index1 - 1,
                    manifold = old,
                    simFlags = (uint)((i % 2 == 0 ? B2ContactSimFlags.b2_simTouchingFlag : 0) | (i % 3 == 0 ? B2ContactSimFlags.b2_simEnablePreSolveEvents : 0))
                };
                expected[i] = new(); actual[i] = new();
            }
            B2Arrays.b2Array_Reserve(ref world.contacts, count);
            var context = new B2StepContext { world = world };
            void CompareBatch()
            {
                for (var i = 0; i < count; i++) { expected[i].CopyFrom(inputs[i]); actual[i].CopyFrom(inputs[i]); }
                expectedCalls.Clear(); actualCalls.Clear(); calls = expectedCalls;
                for (var worker = 0; worker < world.workerCount; worker++)
                    B2BitSets.b2SetBitCountAndClear(ref world.taskContexts.data[worker].contactStateBitSet, count);
                context.contacts = expected; context.generatedManifolds = null!;
                b2CollideTask(0, count, 0, context);
                for (var i = 0; i < count; i++) expectedBits[i] = B2BitSets.b2GetBit(ref world.taskContexts.data[0].contactStateBitSet, i);
                calls = actualCalls;
                for (var worker = 0; worker < world.workerCount; worker++)
                    B2BitSets.b2SetBitCountAndClear(ref world.taskContexts.data[worker].contactStateBitSet, count);
                context.contacts = actual;
                gpu.UpdateContacts(context, count);
                for (var worker = 1; worker < world.workerCount; worker++)
                    B2BitSets.b2InPlaceUnion(ref world.taskContexts.data[0].contactStateBitSet, ref world.taskContexts.data[worker].contactStateBitSet);
                if (!context.generatedContactsUpdated) throw new Exception("GPU contacts must bypass the CPU update task.");
                for (var i = 0; i < count; i++)
                {
                    var e = expected[i]; var a = actual[i];
                    if (e.simFlags != a.simFlags || expectedBits[i] != B2BitSets.b2GetBit(ref world.taskContexts.data[0].contactStateBitSet, i))
                        throw new Exception($"GPU contact state differs at {i}: {e.simFlags:x}/{a.simFlags:x}.");
                    CompareManifold(e.manifold, a.manifold); CompareWarmStart(e.manifold, a.manifold);
                    Near(e.friction, a.friction); Near(e.restitution, a.restitution); Near(e.rollingResistance, a.rollingResistance); Near(e.tangentSpeed, a.tangentSpeed);
                    if (e.bodySimIndexA != a.bodySimIndexA || e.bodySimIndexB != a.bodySimIndexB || e.surfaceLinearA != a.surfaceLinearA || e.surfaceLinearB != a.surfaceLinearB)
                        throw new Exception("GPU contact body mirror differs.");
                    Near(e.invMassA, a.invMassA); Near(e.invMassB, a.invMassB); Near(e.invIA, a.invIA); Near(e.invIB, a.invIB);
                    Near(e.surfaceAngularA, a.surfaceAngularA); Near(e.surfaceAngularB, a.surfaceAngularB);
                }
                if (expectedCalls.Count != actualCalls.Count) throw new Exception("GPU callback coverage differs.");
                for (var i = 0; i < expectedCalls.Count; i++)
                {
                    var e = expectedCalls[i]; var a = actualCalls[i];
                    if (e.Kind != a.Kind || e.A != a.A || e.B != a.B) throw new Exception("GPU callback order/identity differs.");
                    Near(e.Point.X, a.Point.X); Near(e.Point.Y, a.Point.Y); Near(e.Normal.X, a.Normal.X); Near(e.Normal.Y, a.Normal.Y);
                }
            }
            foreach (var speculative in new[] { false, true })
                for (var mode = 0; mode < 3; mode++)
                    foreach (var hooks in new[] { false, true })
                    {
                        world.enableSpeculative = speculative;
                        world.frictionCallback = mode == 0 ? b2DefaultFrictionCallback : mode == 1 ? PhysicsSpace.CombineFriction : friction;
                        world.restitutionCallback = mode == 0 ? b2DefaultRestitutionCallback : mode == 1 ? PhysicsSpace.CombineBounce : bounce;
                        world.preSolveFcn = hooks ? preSolve : null!;
                        CompareBatch();
                    }
            void Edit(int tick)
            {
                var material = b2Shape_GetSurfaceMaterial(shapes[2]);
                material.friction = tick % 2 == 0 ? .1f : .8f; material.restitution = .6f;
                material.rollingResistance = .09f; material.tangentSpeed = -.2f;
                b2Shape_SetSurfaceMaterial(shapes[2], material);
                b2Shape_SetUserMaterial(shapes[4], tick % 2 == 0 ? 1UL : 2UL);
                b2Shape_SetFriction(shapes[6], material.friction);
                b2Shape_SetRestitution(shapes[8], material.friction);
                b2Shape_EnableHitEvents(shapes[10], tick % 2 == 0);
                b2Body_EnableHitEvents(b2Shape_GetBody(shapes[12]), tick % 2 == 0);
                CompareBatch();
            }
            world.frictionCallback = PhysicsSpace.CombineFriction; world.restitutionCallback = PhysicsSpace.CombineBounce;
            for (var i = 0; i < 32; i++) Edit(i);
            var before = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Edit(i);
            var bytes = GC.GetTotalAllocatedBytes(true) - before;
            if (bytes != 0) throw new Exception($"Warmed contact updates allocated {bytes} managed bytes.");
            context.Reset();
            if (context.generatedContactsUpdated) throw new Exception("Step reset must clear the GPU update marker.");
        }
        finally { scheduler.Drain(); b2DestroyWorld(id); }
        Console.WriteLine("GPU contact updates match CPU: material modes/live edits, contact states, mass-relative anchors, speculative pruning, callback order/veto, parallel publication/change bits and zero warmed bytes.");
    }
}
