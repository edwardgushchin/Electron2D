using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;

namespace Electron2D;

internal sealed partial class CPUPhysicsWorldBackend
{
    private readonly Dictionary<(ulong, ulong), OneWayPair> _oneWayPairs = [];
    private readonly List<(ulong, ulong)> _staleOneWayPairs = [];
    private readonly record struct OneWayPair(bool Allowed, long SeenStep);
    private long _contactStep;
    internal static bool PreSolveContact(B2ShapeId first, B2ShapeId second, B2Vec2 point,
        B2Vec2 normal, object context) => ((CPUPhysicsWorldBackend)context).AllowBodyContact(first, second, normal);

    private bool AllowBodyContact(B2ShapeId first, B2ShapeId second, B2Vec2 normal)
    {
        var firstTag = b2Shape_GetUserData(first).GetRef<PhysicsFixtureTag>();
        var secondTag = b2Shape_GetUserData(second).GetRef<PhysicsFixtureTag>();
        if (firstTag is not null && secondTag is not null &&
            PhysicsServer.Service.BodiesExcepted(firstTag.ColliderRID, secondTag.ColliderRID)) return false;
        var firstData = ContactOneWay(firstTag, secondTag);
        var secondData = ContactOneWay(secondTag, firstTag);
        if (firstData is null && secondData is null) return true;

        lock (_oneWayPairs)
        {
            var firstKey = OneWayShapeKey(first, firstTag);
            var secondKey = OneWayShapeKey(second, secondTag);
            var key = firstKey < secondKey ? (firstKey, secondKey) : (secondKey, firstKey);
            if (_oneWayPairs.TryGetValue(key, out var previous))
            {
                _oneWayPairs[key] = previous with { SeenStep = _contactStep };
                return previous.Allowed;
            }

            var allowed = (firstData is null || FacesContact(first, firstData, normal, firstSurface: true)) &&
                (secondData is null || FacesContact(second, secondData, normal, firstSurface: false));
            _oneWayPairs.Add(key, new(allowed, _contactStep));
            return allowed;
        }
    }

    private static OneWayContactData? ContactOneWay(PhysicsFixtureTag? target, PhysicsFixtureTag? source) =>
        source?.Kind == PhysicsShapeGeometry.ShapeKind.SeparationRay ? null : target?.OneWay;

    private static bool FacesContact(B2ShapeId shape, OneWayContactData data, B2Vec2 normal, bool firstSurface)
    {
        var local = data.LocalDirection;
        var direction = b2RotateVector(b2Body_GetRotation(b2Shape_GetBody(shape)), new(local.X, local.Y));
        var facing = normal.X * direction.X + normal.Y * direction.Y;
        return firstSurface ? facing < -1e-6f : facing > 1e-6f;
    }

    private static ulong PackShapeID(B2ShapeId shape) =>
        ((ulong)(uint)shape.index1 << 32) | ((ulong)shape.world0 << 16) | shape.generation;

    internal void PruneOneWayPairs()
    {
        _staleOneWayPairs.Clear();
        foreach (var pair in _oneWayPairs)
            if (pair.Value.SeenStep != _contactStep) _staleOneWayPairs.Add(pair.Key);
        foreach (var key in _staleOneWayPairs) _oneWayPairs.Remove(key);
    }

}
