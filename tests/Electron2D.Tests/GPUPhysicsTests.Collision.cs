using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Manifolds;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

internal static partial class GPUPhysicsTests
{
    private static void VerifyManifolds(GPUPhysicsWorld gpu)
    {
        B2Contacts.b2InitializeContactRegisters();
        var activeFamilies = new int[9];
        foreach (var count in new[] { 1, 63, 64, 65, 4097 })
        {
            var world = new B2World { frictionCallback = B2Worlds.b2DefaultFrictionCallback, restitutionCallback = B2Worlds.b2DefaultRestitutionCallback, enableSpeculative = true }; var context = new B2StepContext { world = world };
            world.shapes.data = new B2Shape[2 * count]; world.shapes.count = world.shapes.capacity = 2 * count;
            world.bodies.data = new B2Body[2 * count]; world.bodies.count = world.bodies.capacity = 2 * count;
            world.contacts.capacity = count;
            world.solverSets.data = [new(), new(), new()]; world.solverSets.count = world.solverSets.capacity = 3;
            var sims = new B2BodySim[2 * count];
            world.solverSets.data[0].bodySims.data = sims; world.solverSets.data[0].bodySims.count = 2 * count;
            // Arena segments may include an unused tail; only the explicit live count is valid.
            context.contacts = new B2ContactSim[count + 7];
            var expected = new B2Manifold[count]; var random = new Random(921 + count);
            var types = new (B2ShapeType A, B2ShapeType B)[]
            {
                (B2ShapeType.b2_circleShape, B2ShapeType.b2_circleShape),
                (B2ShapeType.b2_capsuleShape, B2ShapeType.b2_circleShape),
                (B2ShapeType.b2_capsuleShape, B2ShapeType.b2_capsuleShape),
                (B2ShapeType.b2_polygonShape, B2ShapeType.b2_circleShape),
                (B2ShapeType.b2_polygonShape, B2ShapeType.b2_capsuleShape),
                (B2ShapeType.b2_polygonShape, B2ShapeType.b2_polygonShape),
                (B2ShapeType.b2_segmentShape, B2ShapeType.b2_circleShape),
                (B2ShapeType.b2_segmentShape, B2ShapeType.b2_capsuleShape),
                (B2ShapeType.b2_segmentShape, B2ShapeType.b2_polygonShape)
            };
            float Number(float scale) => (float)(random.NextDouble() * 2 - 1) * scale;
            for (var i = 0; i < count; i++)
            {
                var pair = types[i % types.Length]; var a = CollisionShape(pair.A, random); var b = CollisionShape(pair.B, random);
                a.bodyId = 2 * i; b.bodyId = 2 * i + 1;
                world.shapes.data[2 * i] = a; world.shapes.data[2 * i + 1] = b;
                var angleA = i % 11 == 0 ? 0 : Number(3); var angleB = i % 11 == 0 ? 0 : Number(3);
                var poseA = new B2Transform(new(Number(200), Number(200)), new(MathF.Cos(angleA), MathF.Sin(angleA)));
                var poseB = new B2Transform(poseA.p + new B2Vec2(Number(3), Number(3)), new(MathF.Cos(angleB), MathF.Sin(angleB)));
                sims[2 * i] = new() { transform = poseA }; sims[2 * i + 1] = new() { transform = poseB };
                world.bodies.data[2 * i] = new() { setIndex = 0, localIndex = 2 * i };
                world.bodies.data[2 * i + 1] = new() { setIndex = 0, localIndex = 2 * i + 1 };
                context.contacts[i] = new() { contactId = i, shapeIdA = 2 * i, shapeIdB = 2 * i + 1 };
                expected[i] = CollisionReference(a, poseA, b, poseB);
                if (i % 17 == 0)
                {
                    b.fatAABB = new() { lowerBound = new(300, 300), upperBound = new(400, 400) };
                    expected[i] = default;
                }
                if (expected[i].pointCount > 0) activeFamilies[i % types.Length]++;
                var old = expected[i];
                old.rollingImpulse = .02f * (i % 5 + 1);
                old.points[0].normalImpulse = .4f; old.points[0].tangentImpulse = -.03f;
                old.points[1].normalImpulse = .7f; old.points[1].tangentImpulse = .05f;
                switch (i % 5)
                {
                    case 0: old.pointCount = 0; break;
                    case 1: if (old.pointCount == 2) (old.points[0], old.points[1]) = (old.points[1], old.points[0]); break;
                    case 2: old.points[0].id ^= 0x8000; break;
                    case 3: old.points[1].id = old.points[0].id; break;
                }
                context.contacts[i].manifold = old;
            }
            gpu.GenerateManifolds(context, count);
            for (var i = 0; i < count; i++)
            {
                try
                {
                    CompareManifold(expected[i], context.generatedManifolds[i]);
                    var oracle = new B2ContactSim { manifold = context.contacts[i].manifold };
                    if (B2MathFunction.b2AABB_Overlaps(world.shapes.data[2 * i].fatAABB, world.shapes.data[2 * i + 1].fatAABB))
                        B2Contacts.b2UpdateContact(world, oracle, world.shapes.data[2 * i], sims[2 * i].transform, default,
                            world.shapes.data[2 * i + 1], sims[2 * i + 1].transform, default);
                    else oracle.manifold = default;
                    CompareWarmStart(oracle.manifold, context.generatedManifolds[i]);
                }
                catch (Exception ex) { throw new InvalidOperationException($"GPU manifold case {i}/{count}, {types[i % types.Length]} differs.", ex); }
            }
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            for (var i = 0; i < 8; i++) gpu.GenerateManifolds(context, count);
            var before = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 8; i++)
            {
                gpu.GenerateManifolds(context, count);
                if (gpu.UploadedGeometryCount != 0 || gpu.GeometryUploadBytes != 0)
                    throw new Exception("Unchanged manifolds must reuse resident geometry.");
            }
            var bytes = GC.GetTotalAllocatedBytes(true) - before;
            if (bytes != 0) throw new InvalidOperationException($"Prepared GPU manifolds allocated {bytes} managed bytes.");
            world.shapes.data[0].type = B2ShapeType.b2_chainSegmentShape;
            world.shapes.data[1].fatAABB = world.shapes.data[0].fatAABB;
            try { gpu.GenerateManifolds(context, count); throw new Exception("Unsupported chain geometry must fail explicitly."); }
            catch (NotSupportedException) { }
        }
        if (activeFamilies.Any(count => count == 0)) throw new InvalidOperationException("Every GPU shape pair family must produce an active contact in the conformance batch.");
        Console.WriteLine("GPU manifolds passed: nine shape pair families, rotated/offset shapes, speculative/empty contacts, features and warmed allocation.");
    }

    internal static void VerifyGeometryResidency()
    {
        using var gpu = new GPUPhysicsWorld();
        var id = b2CreateWorld(b2DefaultWorldDef());
        var world = b2GetWorldFromId(id);
        var definition = b2DefaultBodyDef(); definition.type = B2BodyType.b2_dynamicBody;
        var bodyA = b2CreateBody(id, definition); definition.position = new(.4f, .3f);
        var bodyB = b2CreateBody(id, definition);
        var shapeDef = b2DefaultShapeDef();
        var shapeA = b2CreateCircleShape(bodyA, shapeDef, new B2Circle { radius = .9f });
        var shapeB = b2CreateCircleShape(bodyB, shapeDef, new B2Circle { radius = .6f });
        var context = new B2StepContext { world = world, contacts = new B2ContactSim[3] };
        for (var i = 0; i < context.contacts.Count; i++) context.contacts[i] = new();
        void Check(int uploads)
        {
            var a = world.shapes.data[shapeA.index1 - 1]; var b = world.shapes.data[shapeB.index1 - 1];
            foreach (var contact in context.contacts) { contact.shapeIdA = shapeA.index1 - 1; contact.shapeIdB = shapeB.index1 - 1; }
            var poseA = b2GetBodySim(world, world.bodies.data[a.bodyId]).transform;
            var poseB = b2GetBodySim(world, world.bodies.data[b.bodyId]).transform;
            var expected = B2MathFunction.b2AABB_Overlaps(a.fatAABB, b.fatAABB) ? CollisionReference(a, poseA, b, poseB) : default;
            gpu.GenerateManifolds(context, context.contacts.Count);
            foreach (var actual in context.generatedManifolds.AsSpan(0, context.contacts.Count)) CompareManifold(expected, actual);
            var referenced = B2MathFunction.b2AABB_Overlaps(a.fatAABB, b.fatAABB) ? 2 : 0;
            if (gpu.ResidentGeometryCount + gpu.UploadedGeometryCount != referenced)
                throw new Exception("Geometry accounting must count each referenced slot once.");
            if (gpu.UploadedGeometryCount != uploads || gpu.GeometryUploadBytes != uploads * 160L)
                throw new Exception($"Geometry upload count differs: expected {uploads}, got {gpu.UploadedGeometryCount}.");
        }
        try
        {
            Check(2); Check(0); // Three pairs share the same two geometry slots.
            b2Body_SetTransform(bodyA, new(.1f, 0), new(1, 0)); Check(0);
            b2Shape_SetFriction(shapeA, .7f); Check(1);
            b2Shape_SetFilter(shapeA, new B2Filter { categoryBits = 8, maskBits = ulong.MaxValue }); Check(0);
            b2Shape_SetCircle(shapeA, new B2Circle { center = new(.2f, -.1f), radius = 1.1f }); Check(1);
            b2Shape_SetCapsule(shapeA, new B2Capsule(new(-.7f, 0), new(.7f, 0), .5f)); Check(1);
            b2Shape_SetCapsule(shapeA, new B2Capsule(new(0, 0), new(0, 0), .5f)); Check(0); // Rejected degenerate edit.
            b2Shape_SetSegment(shapeA, new B2Segment { point1 = new(-1, 0), point2 = new(1, 0) }); Check(1);
            var polygon = b2MakeBox(.7f, .5f);
            b2Shape_SetPolygon(shapeA, ref polygon); Check(1);
            b2Body_SetTransform(bodyA, new(.1f, 0), new(MathF.Cos(.3f), MathF.Sin(.3f))); Check(0);
            b2Shape_SetCircle(shapeA, new B2Circle { radius = .8f });
            b2Shape_SetPolygon(shapeA, ref polygon); Check(1); // Only the final edit reaches the GPU.
            b2Body_Disable(bodyA);
            b2Shape_SetCircle(shapeA, new B2Circle { radius = 1.2f });
            gpu.GenerateManifolds(context, 0);
            if (gpu.UploadedGeometryCount != 0) throw new Exception("An empty collision batch must not upload dirty shapes.");
            b2Body_Enable(bodyA); Check(1);
            b2Body_SetTransform(bodyB, new(20, 0), new(1, 0));
            b2Shape_SetCircle(shapeA, new B2Circle { radius = .7f }); Check(0);
            b2Body_SetTransform(bodyB, new(.4f, .3f), new(1, 0)); Check(1);

            var old = shapeA;
            b2DestroyShape(shapeA, true);
            shapeA = b2CreatePolygonShape(bodyA, shapeDef, polygon);
            if (shapeA.index1 != old.index1) throw new Exception("Geometry fixture must reuse its shape slot.");
            Check(1);
            var resets = gpu.GeometryCacheResetCount;
            for (var i = 0; i < 130; i++)
                b2CreateCircleShape(bodyA, shapeDef, new B2Circle { center = new(100 + i * 3, 0), radius = .5f });
            Check(2); // Growth invalidates the buffer, but untouched shapes stay lazy.
            if (gpu.GeometryCacheResetCount != resets + 1) throw new Exception("Geometry growth must invalidate prior GPU storage.");
            world.shapeGeometryChanged = null!;
            b2Shape_SetCircle(shapeA, new B2Circle { radius = .5f }); Check(2);
            if (gpu.GeometryCacheResetCount != resets + 2) throw new Exception("Lost geometry observer must invalidate the cache.");

            void Edit(int tick)
            {
                b2Shape_SetCircle(shapeA, new B2Circle { center = new(.2f, 0), radius = .5f });
                b2Shape_SetCircle(shapeA, new B2Circle { center = new(.1f, 0), radius = tick % 2 == 0 ? .7f : .9f });
                Check(1);
            }
            for (var i = 0; i < 32; i++) Edit(i);
            var before = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Edit(i);
            var bytes = GC.GetTotalAllocatedBytes(true) - before;
            if (bytes != 0) throw new Exception($"Warmed geometry edits allocated {bytes} managed bytes.");

            // Fail after one dirty shape was packed, before any dispatch. Its cache
            // must remain invalid and the next valid batch must still upload it.
            b2Shape_SetCircle(shapeA, new B2Circle { radius = 1 });
            var b = world.shapes.data[shapeB.index1 - 1]; b.type = B2ShapeType.b2_chainSegmentShape;
            try { gpu.GenerateManifolds(context, 3); throw new Exception("Unsupported geometry must reject the batch."); }
            catch (NotSupportedException) { }
            b.type = B2ShapeType.b2_circleShape; Check(1);
            var observer = world.shapeGeometryChanged;
            using (var other = new GPUPhysicsWorld())
            {
                other.GenerateManifolds(context, 3);
                if (world.shapeGeometryChanged == observer) throw new Exception("Other GPU hosts must claim their own geometry cache.");
                gpu.Dispose();
                if (world.shapeGeometryChanged is null) throw new Exception("Disposal must not detach another host's geometry observer.");
            }
            if (world.shapeGeometryChanged is not null) throw new Exception("GPU disposal must detach its geometry observer.");
        }
        finally
        {
            b2DestroyWorld(id);
            if (world.shapeGeometryChanged is not null) throw new Exception("World reset must detach its geometry observer.");
        }
        Console.WriteLine("Resident GPU geometry passed: shared slots, all shape edits, lazy uploads, movement, ID reuse, growth, observer/disposal/failure boundaries and 64 warmed edits with zero managed bytes.");
    }

    private static B2Shape CollisionShape(B2ShapeType type, Random random)
    {
        float Number(float scale) => (float)(random.NextDouble() * 2 - 1) * scale;
        var shape = new B2Shape { type = type, fatAABB = new() { lowerBound = new(-100, -100), upperBound = new(100, 100) } };
        var offset = new B2Vec2(Number(.5f), Number(.5f)); var radius = .05f + (float)random.NextDouble() * .9f;
        switch (type)
        {
            case B2ShapeType.b2_circleShape: shape.us.circle = new() { center = offset, radius = radius }; break;
            case B2ShapeType.b2_capsuleShape: shape.us.capsule = new(offset + new B2Vec2(-1, 0), offset + new B2Vec2(.5f, .4f), radius); break;
            case B2ShapeType.b2_segmentShape: shape.us.segment = new() { point1 = offset + new B2Vec2(-1, -.4f), point2 = offset + new B2Vec2(.8f, .5f) }; break;
            case B2ShapeType.b2_polygonShape:
                Span<B2Vec2> vertices = stackalloc B2Vec2[8]; var count = random.Next(3, 9);
                for (var i = 0; i < count; i++)
                {
                    var angle = i * MathF.Tau / count;
                    vertices[i] = offset + new B2Vec2(1.2f * MathF.Cos(angle), .75f * MathF.Sin(angle));
                }
                var hull = b2ComputeHull(vertices, count); shape.us.polygon = b2MakePolygon(in hull, random.Next(2) == 0 ? 0 : .1f);
                break;
        }
        return shape;
    }

    private static B2Manifold CollisionReference(B2Shape a, B2Transform poseA, B2Shape b, B2Transform poseB) => (a.type, b.type) switch
    {
        (B2ShapeType.b2_circleShape, B2ShapeType.b2_circleShape) => b2CollideCircles(a.us.circle, poseA, b.us.circle, poseB),
        (B2ShapeType.b2_capsuleShape, B2ShapeType.b2_circleShape) => b2CollideCapsuleAndCircle(a.us.capsule, poseA, b.us.circle, poseB),
        (B2ShapeType.b2_capsuleShape, B2ShapeType.b2_capsuleShape) => b2CollideCapsules(a.us.capsule, poseA, b.us.capsule, poseB),
        (B2ShapeType.b2_polygonShape, B2ShapeType.b2_circleShape) => b2CollidePolygonAndCircle(ref a.us.polygon, poseA, b.us.circle, poseB),
        (B2ShapeType.b2_polygonShape, B2ShapeType.b2_capsuleShape) => b2CollidePolygonAndCapsule(ref a.us.polygon, poseA, b.us.capsule, poseB),
        (B2ShapeType.b2_polygonShape, B2ShapeType.b2_polygonShape) => b2CollidePolygons(ref a.us.polygon, poseA, ref b.us.polygon, poseB),
        (B2ShapeType.b2_segmentShape, B2ShapeType.b2_circleShape) => b2CollideSegmentAndCircle(a.us.segment, poseA, b.us.circle, poseB),
        (B2ShapeType.b2_segmentShape, B2ShapeType.b2_capsuleShape) => b2CollideSegmentAndCapsule(a.us.segment, poseA, b.us.capsule, poseB),
        (B2ShapeType.b2_segmentShape, B2ShapeType.b2_polygonShape) => b2CollideSegmentAndPolygon(a.us.segment, poseA, ref b.us.polygon, poseB),
        _ => throw new NotSupportedException()
    };

    private static void CompareManifold(B2Manifold expected, B2Manifold actual)
    {
        if (expected.pointCount != actual.pointCount) throw new InvalidOperationException($"Point counts differ: CPU {expected.pointCount}, GPU {actual.pointCount}.");
        if (expected.pointCount == 0) return;
        Near(expected.normal.X, actual.normal.X); Near(expected.normal.Y, actual.normal.Y);
        for (var i = 0; i < expected.pointCount; i++)
        {
            var e = expected.points[i]; var a = actual.points[i];
            if (e.id != a.id) throw new InvalidOperationException($"Contact features differ: CPU {e.id}, GPU {a.id}.");
            Near(e.anchorA.X, a.anchorA.X); Near(e.anchorA.Y, a.anchorA.Y); Near(e.anchorB.X, a.anchorB.X); Near(e.anchorB.Y, a.anchorB.Y);
            Near(e.point.X, a.point.X); Near(e.point.Y, a.point.Y); Near(e.separation, a.separation);
        }
    }
}
