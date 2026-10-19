using Box2D.NET;
using static Box2D.NET.B2Manifolds;
using static Box2D.NET.B2MathFunction;

internal static class PhysicsRoundedCoreTests
{
    internal static void Run()
    {
        var count = 0;
        foreach (var angle in new[] { 0f, .57f })
            foreach (var offset in new[] { 0f, 1e-8f, .01f })
                foreach (var origin in new[] { new B2Vec2(0, 0), new B2Vec2(10, -20) })
                    foreach (var capsule in new[] { false, true })
                    {
                        var pose = new B2Transform(origin, new(MathF.Cos(angle), MathF.Sin(angle)));
                        var core = new B2Vec2(.4f, .7f); var other = core + new B2Vec2(offset, 0);
                        var circle = new B2Circle { center = other, radius = 3 };
                        var manifold = capsule
                            ? b2CollideCapsuleAndCircle(new(core + new B2Vec2(0, -2), core + new B2Vec2(0, 2), 2), pose, circle, pose)
                            : b2CollideCircles(new B2Circle { center = core, radius = 2 }, pose, circle, pose);
                        var normal = manifold.normal;
                        var expected = .5f * (b2TransformPoint(pose, core) + 2 * normal + b2TransformPoint(pose, other) - 3 * normal);
                        Check(manifold.pointCount == 1 && MathF.Abs(b2Length(normal) - 1) < 1e-5f, "Rounded cores retain one unit-normal contact");
                        Check(MathF.Abs(manifold.points[0].separation - (offset - 5)) < 1e-5f, "Rounded core separation retains both radii");
                        Check(b2Length(manifold.points[0].point - expected) < 1e-5f &&
                            b2Length(origin + manifold.points[0].anchorA - expected) < 1e-5f &&
                            b2Length(origin + manifold.points[0].anchorB - expected) < 1e-5f, "Rounded surface midpoint and both anchors use the same unit normal");
                        if (capsule) Check(MathF.Abs(b2Dot(normal, b2RotateVector(pose.q, new(0, 1)))) < 1e-5f, "Coincident capsule core chooses a perpendicular normal");
                        count++;
                    }
        Console.WriteLine($"Rounded core primitives: {count} exact/near-zero/separated, transformed circle/capsule manifolds passed.");
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
