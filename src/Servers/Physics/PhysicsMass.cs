using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal static class PhysicsMass
{
    internal const float InertiaScale = PhysicsSpace.MetersPerUnit * PhysicsSpace.MetersPerUnit;

    internal static void Validate(float mass, float inertia, Vector2? center)
    {
        if (!float.IsFinite(mass) || mass <= 0 || !float.IsFinite(1f / mass))
            throw new ArgumentOutOfRangeException(nameof(mass));
        if (!float.IsFinite(inertia) || inertia < 0 ||
            (inertia > 0 && !float.IsFinite(1f / (inertia * InertiaScale))))
            throw new ArgumentOutOfRangeException(nameof(inertia));
        if (center is { } value && (!value.IsFinite() || !float.IsFinite(value.LengthSquared())))
            throw new ArgumentOutOfRangeException(nameof(center));
    }

    internal static B2MassData Apply(B2BodyId body, IReadOnlyList<B2ShapeId> shapes,
        float mass, float inertia, Vector2? customCenter)
    {
        Validate(mass, inertia, customCenter);
        var weight = 0d;
        var centerX = 0d;
        var centerY = 0d;
        var segments = false;
        for (var pass = 0; pass < 2; pass++)
        {
            for (var index = 0; index < shapes.Count; index++)
            {
                var sample = Measure(shapes[index], segments);
                weight += sample.mass;
                centerX += (double)sample.mass * sample.center.X;
                centerY += (double)sample.mass * sample.center.Y;
            }
            if (weight > 0) break;
            segments = true;
        }
        if (weight > 0) { centerX /= weight; centerY /= weight; }
        if (customCenter is { } center)
        {
            var native = Shape.ToBackend(center);
            centerX = native.X; centerY = native.Y;
        }
        var moment = (double)inertia * InertiaScale;
        if (inertia == 0 && weight > 0)
        {
            for (var index = 0; index < shapes.Count; index++)
            {
                var sample = Measure(shapes[index], segments);
                if (sample.mass == 0) continue;
                var dx = sample.center.X - centerX;
                var dy = sample.center.Y - centerY;
                moment += mass * (sample.mass / weight) * (sample.rotationalInertia + dx * dx + dy * dy);
            }
        }
        var result = new B2MassData(mass, new((float)centerX, (float)centerY), (float)moment);
        if (!double.IsFinite(weight) || !float.IsFinite(result.center.X) || !float.IsFinite(result.center.Y) ||
            !float.IsFinite(result.center.X * result.center.X + result.center.Y * result.center.Y) ||
            !float.IsFinite(result.rotationalInertia) || (inertia == 0 && !float.IsFinite(result.rotationalInertia / InertiaScale)) || result.rotationalInertia < 0 ||
            (moment > 0 && (!float.IsFinite(1f / result.rotationalInertia))))
            throw new ArgumentOutOfRangeException(nameof(mass), "Mass geometry exceeds the finite solver range.");
        var world = b2GetWorldFromId(b2Body_GetWorld(body));
        var minExtent = B2_HUGE;
        var maxExtent = 0f;
        for (var index = 0; index < shapes.Count; index++)
        {
            var extent = b2ComputeShapeExtent(b2GetShape(world, shapes[index]), result.center);
            minExtent = MathF.Min(minExtent, extent.minExtent);
            maxExtent = MathF.Max(maxExtent, extent.maxExtent);
        }
        if (!float.IsFinite(minExtent) || !float.IsFinite(maxExtent))
            throw new ArgumentOutOfRangeException(nameof(customCenter), "Center-relative shape extents exceed the solver range.");
        var applied = result;
        if (b2Body_GetType(body) != B2BodyType.b2_dynamicBody)
        {
            applied.mass = 0; applied.rotationalInertia = 0;
        }
        b2Body_SetMassData(body, applied);
        var simulation = PhysicsBodyRuntime.Simulation(body);
        simulation.minExtent = minExtent; simulation.maxExtent = maxExtent;
        return result;
    }

    private static B2MassData Measure(B2ShapeId shape, bool segments)
    {
        if (b2Shape_IsSensor(shape)) return default;
        var type = b2Shape_GetType(shape);
        if (segments)
        {
            if (type != B2ShapeType.b2_segmentShape) return default;
            var segment = b2Shape_GetSegment(shape);
            var dx = (double)segment.point2.X - segment.point1.X;
            var dy = (double)segment.point2.Y - segment.point1.Y;
            var squaredLength = dx * dx + dy * dy;
            return new((float)Math.Sqrt(squaredLength),
                new((float)(((double)segment.point1.X + segment.point2.X) / 2),
                    (float)(((double)segment.point1.Y + segment.point2.Y) / 2)), (float)(squaredLength / 12));
        }
        var data = type switch
        {
            B2ShapeType.b2_circleShape => b2ComputeCircleMass(b2Shape_GetCircle(shape), 1),
            B2ShapeType.b2_capsuleShape => b2ComputeCapsuleMass(b2Shape_GetCapsule(shape), 1),
            B2ShapeType.b2_polygonShape => b2ComputePolygonMass(b2Shape_GetPolygon(shape), 1),
            _ => default
        };
        if (data.mass > 0) data.rotationalInertia /= data.mass;
        return data;
    }
}
