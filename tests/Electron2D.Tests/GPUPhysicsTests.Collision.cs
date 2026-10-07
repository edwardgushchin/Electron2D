using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Manifolds;

internal static partial class GPUPhysicsTests
{
    private static void VerifyManifolds(GPUPhysicsWorld gpu)
    {
        var activeFamilies = new int[9];
        foreach (var count in new[] { 1, 63, 64, 65, 4097 })
        {
            var world = new B2World(); var context = new B2StepContext { world = world };
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
            }
            gpu.GenerateManifolds(context, count);
            for (var i = 0; i < count; i++)
            {
                try { CompareManifold(expected[i], context.generatedManifolds[i]); }
                catch (Exception ex) { throw new InvalidOperationException($"GPU manifold case {i}/{count}, {types[i % types.Length]} differs.", ex); }
            }
            var before = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 8; i++) gpu.GenerateManifolds(context, count);
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
