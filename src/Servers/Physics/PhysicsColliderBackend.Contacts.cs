using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;

namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    internal void CaptureViewContacts(PhysicsDirectBodyState view, int limit)
    {
        if (limit == 0 || Space!.CaptureFrameContacts(this, view, BodyID, limit)) return;
        var world = _world!;
        for (var key = _body!.headContactKey; key != B2Constants.B2_NULL_INDEX;)
        {
            var contact = world.contacts.data[key >> 1]; var edge = key & 1;
            key = contact.edges[edge].nextKey;
            if ((contact.flags & (uint)B2ContactFlags.b2_contactTouchingFlag) == 0) continue;
            var own = world.shapes.data[edge == 0 ? contact.shapeIdA : contact.shapeIdB];
            var other = world.shapes.data[edge == 0 ? contact.shapeIdB : contact.shapeIdA];
            var ownTag = own.userData.GetRef<PhysicsFixtureTag>(); var otherTag = other.userData.GetRef<PhysicsFixtureTag>();
            if (ownTag is null || otherTag is null) continue;
            var simulation = B2Contacts.b2GetContactSim(world, contact);
            ref var manifold = ref simulation.manifold;
            var collider = b2MakeBodyId(world, other.bodyId);
            for (var i = 0; i < manifold.pointCount; i++)
            {
                ref readonly var point = ref manifold.points[i];
                CaptureViewContact(view, manifold.normal, point.point, point.separation, -point.separation,
                    Space.SolvedContactImpulse(simulation, point), collider, edge == 0, ownTag, otherTag, limit);
            }
        }
    }

    internal void CaptureViewContact(PhysicsDirectBodyState view, B2Vec2 normal, B2Vec2 point, float separation,
        float depth, B2Vec2 impulse, B2BodyId collider, bool first, PhysicsFixtureTag own, PhysicsFixtureTag other, int limit)
    {
        var slot = view.SelectContactSlot(depth, limit);
        if (slot < 0) return;
        var a = point - normal * (separation * 0.5f);
        var b = point + normal * (separation * 0.5f);
        var local = first ? a : b;
        var remote = first ? b : a;
        view.StoreContact(slot, new(other.ColliderRID, other.SceneObject?.InstanceID ?? 0,
            own.ShapeIndex, other.ShapeIndex, ToScene(local), ToScene(remote),
            new(first ? -normal.X : normal.X, first ? -normal.Y : normal.Y),
            ToScene(Space!.SolvedPointVelocity(BodyID, local)), ToScene(Space.SolvedPointVelocity(collider, remote)),
            ToScene(first ? -impulse : impulse), depth, other.SceneOwner));
    }
}
