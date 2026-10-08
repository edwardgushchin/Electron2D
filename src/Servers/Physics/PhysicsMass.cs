using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
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

    internal static void AppendGeometry(Shape shape, Transform pose, List<B2ShapeProxy> proxies)
    {
        if (shape.IsDisposed || shape is SeparationRayShape) return;
        if (!pose.IsFinite() || !pose.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(pose.Skew))
            throw new ArgumentException("Mass geometry requires finite unit-scale poses.", nameof(pose));
        var start = proxies.Count;
        PhysicsShapeBackend.AppendQueryProxies(shape, proxies);
        var transform = new B2Transform(PhysicsShapeBackend.ToBackend(pose.Origin), b2MakeRot(pose.Rotation));
        for (var index = start; index < proxies.Count; index++)
        {
            var proxy = proxies[index];
            for (var point = 0; point < proxy.count; point++) proxy.points[point] = b2TransformPoint(transform, proxy.points[point]);
            proxies[index] = proxy;
        }
    }

    internal static B2MassData Calculate(IReadOnlyList<B2ShapeProxy> proxies, float mass, float inertia, Vector2? customCenter)
    {
        Validate(mass, inertia, customCenter);
        var weight = 0d;
        var centerX = 0d;
        var centerY = 0d;
        var segments = false;
        for (var pass = 0; pass < 2; pass++)
        {
            for (var index = 0; index < proxies.Count; index++)
            {
                var sample = Measure(proxies[index], segments);
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
            var native = PhysicsShapeBackend.ToBackend(center);
            centerX = native.X; centerY = native.Y;
        }
        var moment = (double)inertia * InertiaScale;
        if (inertia == 0 && weight > 0)
        {
            for (var index = 0; index < proxies.Count; index++)
            {
                var sample = Measure(proxies[index], segments);
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
        return result;
    }

    internal static B2MassData Apply(B2BodyId body, IReadOnlyList<B2ShapeId> shapes,
        float mass, float inertia, Vector2? customCenter, List<B2ShapeProxy> proxies)
    {
        proxies.Clear();
        var world = b2GetWorldFromId(b2Body_GetWorld(body));
        for (var index = 0; index < shapes.Count; index++)
            if (!b2Shape_IsSensor(shapes[index])) proxies.Add(b2MakeShapeDistanceProxy(b2GetShape(world, shapes[index])));
        var result = Calculate(proxies, mass, inertia, customCenter);
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
        // Fixture edits defer mass; the validated custom profile completes that pending update.
        b2GetBodyFullId(world, body).flags &= ~(uint)B2BodyFlags.b2_dirtyMass;
        var simulation = PhysicsBodyRuntime.Simulation(body);
        simulation.minExtent = minExtent; simulation.maxExtent = maxExtent;
        return result;
    }

    private static B2MassData Measure(B2ShapeProxy proxy, bool segments)
    {
        if (segments)
        {
            if (proxy.count != 2 || proxy.radius != 0) return default;
            var a = proxy.points[0]; var b = proxy.points[1];
            var dx = (double)b.X - a.X; var dy = (double)b.Y - a.Y;
            var squaredLength = dx * dx + dy * dy;
            return new((float)Math.Sqrt(squaredLength),
                new((float)(((double)a.X + b.X) / 2), (float)(((double)a.Y + b.Y) / 2)), (float)(squaredLength / 12));
        }
        var data = default(B2MassData);
        if (proxy.count == 1) data = b2ComputeCircleMass(new B2Circle { center = proxy.points[0], radius = proxy.radius }, 1);
        else if (proxy.count == 2 && proxy.radius > 0)
            data = b2ComputeCapsuleMass(new B2Capsule { center1 = proxy.points[0], center2 = proxy.points[1], radius = proxy.radius }, 1);
        else if (proxy.count > 2)
        {
            var hull = new B2Hull { count = proxy.count, points = proxy.points };
            var polygon = b2MakePolygon(in hull, proxy.radius);
            data = b2ComputePolygonMass(polygon, 1);
        }
        if (data.mass > 0) data.rotationalInertia /= data.mass;
        return data;
    }
}
