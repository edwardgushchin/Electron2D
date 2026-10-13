using Box2D.NET;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2MathFunction;

namespace Electron2D;

internal sealed partial class CPUPhysicsWorldBackend
{
    private static bool LinearDiskCast(in B2TOIInput input, out B2TOIOutput hit)
    {
        // A slop-reduced TOI target can discard a shallow disk intersection.
        hit = default;
        if (input.proxyA.isBoundary || input.proxyB.isBoundary || input.proxyA.count != 1 || input.proxyB.count != 1 ||
            input.sweepA.q1.c != input.sweepA.q2.c || input.sweepA.q1.s != input.sweepA.q2.s ||
            input.sweepB.q1.c != input.sweepB.q2.c || input.sweepB.q1.s != input.sweepB.q2.s) return false;
        var radius = (double)input.proxyA.radius + input.proxyB.radius;
        if (!(radius > 0)) return false;
        var a = b2TransformPoint(b2GetSweepTransform(input.sweepA, 0), input.proxyA.points[0]);
        var b = b2TransformPoint(b2GetSweepTransform(input.sweepB, 0), input.proxyB.points[0]);
        var da = b2TransformPoint(b2GetSweepTransform(input.sweepA, 1), input.proxyA.points[0]) - a;
        var db = b2TransformPoint(b2GetSweepTransform(input.sweepB, 1), input.proxyB.points[0]) - b;
        var point = b - a; var motion = db - da;
        var distance = (double)point.X * point.X + (double)point.Y * point.Y;
        hit = new() { state = B2TOIState.b2_toiStateSeparated, fraction = input.maxFraction };
        if (distance <= radius * radius)
        {
            hit.state = B2TOIState.b2_toiStateOverlapped; hit.fraction = 0; return true;
        }
        if (!DiskFraction(point, motion, radius, input.maxFraction, out var fraction)) return true;
        hit.state = B2TOIState.b2_toiStateHit; hit.fraction = (float)fraction;
        var normal = point + (float)fraction * motion; hit.normal = b2Normalize(normal);
        return true;
    }
    private static bool DiskFraction(in B2Vec2 point, in B2Vec2 motion, double radius, float maximum, out float fraction)
    {
        fraction = maximum;
        var distance = (double)point.X * point.X + (double)point.Y * point.Y;
        var length = (double)motion.X * motion.X + (double)motion.Y * motion.Y;
        var projection = (double)point.X * motion.X + (double)point.Y * motion.Y;
        var discriminant = projection * projection - length * (distance - radius * radius);
        if (length == 0 || projection >= 0 || discriminant < 0) return false;
        var time = (-projection - Math.Sqrt(discriminant)) / length;
        if (time < 0 || time > maximum) return false;
        fraction = (float)time; return true;
    }
}
