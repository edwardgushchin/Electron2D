using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;

namespace Electron2D;

internal sealed partial class CPUPhysicsColliderImplementation
{
    internal override Transform PortablePose
    {
        get
        {
            var value = b2GetBodyTransformQuick(_world!, _body!);
            return new(new(value.q.c, value.q.s), new(-value.q.s, value.q.c), ToScene(value.p));
        }
    }
    internal float PortableSleepTime => _body!.sleepTime;
    internal override void ApplyPortableMotion(Transform pose, Vector2 linear, float angular, float sleepTime, bool canSleep, bool sleeping,
        Vector2 surface, float surfaceAngular, Vector2 force, float torque, Vector2 gravity, float linearDamp, float angularDamp, bool fieldsInitialized)
    {
        b2Body_SetTransform(BodyID, PhysicsShapeBackend.ToBackend(pose.Origin), new B2Rot(pose.X.X, pose.X.Y));
        SetLinearVelocity(linear); SetAngularVelocity(angular); SetSurfaceVelocity(surface, surfaceAngular);
    }
    internal void ApplyPortableSleep(float time, bool canSleep, bool sleeping)
    {
        SetCanSleep(canSleep); SetAwake(!sleeping); _body!.sleepTime = time;
    }
    internal B2ShapeId PortableCPUShape(int slot, int piece)
    {
        var index = 0;
        for (var i = 0; i < _shapes.Count; i++)
        {
            var shape = _shapes[i];
            if (b2Shape_GetUserData(shape).GetRef<PhysicsFixtureTag>()?.ShapeIndex == slot && index++ == piece) return shape;
        }
        throw new InvalidDataException("Snapshot references an unavailable collision piece.");
    }
    internal override void ValidatePortablePiece(int slot, int piece)
    {
        var shape = PortableCPUShape(slot, piece);
        PhysicsSnapshotReader.Require(piece == 0 || b2Shape_GetUserData(shape).GetRef<PhysicsFixtureTag>()?.Compound is null);
    }
}
