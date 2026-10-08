using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;

namespace Electron2D;

internal sealed partial class PhysicsBodyRuntime
{
    private PhysicsSpace? _viewSpace;
    private B2BodyId _viewID;
    private B2World? _viewWorld;
    private B2Body? _viewBody;

    internal PhysicsDirectBodyState GetView(PhysicsSpace space, B2BodyId id)
    {
        if (View is null || View.IsDisposed || _viewSpace != space || _viewID != id)
        {
            var world = B2Worlds.b2GetWorldFromId(space.WorldID);
            var body = b2GetBodyFullId(world, id);
            var view = new PhysicsDirectBodyState(this, space);
            _viewSpace = space; _viewID = id; _viewWorld = world; _viewBody = body;
            View = view;
        }
        return View;
    }

    internal void InvalidateView()
    {
        View = null;
        _viewSpace = null; _viewID = default; _viewWorld = null; _viewBody = null;
    }

    internal void ValidateView(PhysicsDirectBodyState view, PhysicsSpace space)
    {
        try
        {
            if (!ReferenceEquals(View, view) || Space != space)
                throw new ObjectDisposedException(nameof(PhysicsDirectBodyState), "The backend attachment ended.");
        }
        catch (ArgumentException) { throw new ObjectDisposedException(nameof(PhysicsDirectBodyState), "The body was released."); }
    }

    private static Vector2 ViewToScene(B2Vec2 value) => new(value.X * PhysicsSpace.UnitsPerMeter, value.Y * PhysicsSpace.UnitsPerMeter);
    internal float ViewAngularVelocity => b2Body_GetAngularVelocity(_viewID);
    internal Vector2 ViewLinearVelocity => ViewToScene(b2Body_GetLinearVelocity(_viewID));
    internal Vector2 ViewCenterOfMass => ViewToScene(b2Body_GetWorldCenterOfMass(_viewID) - b2Body_GetPosition(_viewID));
    internal Vector2 ViewCenterOfMassLocal => ViewToScene(b2Body_GetLocalCenterOfMass(_viewID));
    internal float ViewInverseMass => Simulation(_viewID).invMass;
    internal float ViewInverseInertia => b2Body_GetMotionLocks(_viewID).angularZ ? 0 : Simulation(_viewID).invInertia * PhysicsMass.InertiaScale;
    internal bool ViewSleeping => _viewBody!.setIndex != (int)B2SolverSetType.b2_awakeSet;
    internal Transform ViewTransform
    {
        get
        {
            var pose = b2GetBodyTransformQuick(_viewWorld!, _viewBody!);
            return new(b2Rot_GetAngle(pose.q), Vector2.One, 0, ViewToScene(pose.p));
        }
    }

    internal Vector2 GetViewPointVelocity(Vector2 offset)
    {
        Finite(offset);
        var point = b2Body_GetPosition(_viewID) + Shape.ToBackend(offset);
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y)) throw new ArgumentOutOfRangeException(nameof(offset));
        var result = ViewToScene(b2Body_GetWorldPointVelocity(_viewID, point));
        Finite(result); return result;
    }

    internal void SetViewConstantForce(Vector2 force)
    {
        Finite(force);
        if (Owners.Scene is RigidBody rigid) rigid.ConstantForce = force;
        else ConstantForce = force;
        b2Body_SetAwake(_viewID, true);
    }

    internal void SetViewConstantTorque(float torque)
    {
        Finite(torque);
        if (Owners.Scene is RigidBody rigid) rigid.ConstantTorque = torque;
        else ConstantTorque = torque;
        b2Body_SetAwake(_viewID, true);
    }

    internal void CaptureViewContacts(PhysicsDirectBodyState view)
    {
        var limit = ContactLimit;
        if (limit == 0 || _viewSpace!.CaptureFrameContacts(this, view, _viewID, limit)) return;
        var world = _viewWorld!;
        for (var key = _viewBody!.headContactKey; key != B2Constants.B2_NULL_INDEX;)
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
                    _viewSpace.SolvedContactImpulse(simulation, point), collider, edge == 0, ownTag, otherTag, limit);
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
            own.ShapeIndex, other.ShapeIndex, ViewToScene(local), ViewToScene(remote),
            new(first ? -normal.X : normal.X, first ? -normal.Y : normal.Y),
            ViewToScene(_viewSpace!.SolvedPointVelocity(_viewID, local)), ViewToScene(_viewSpace.SolvedPointVelocity(collider, remote)),
            ViewToScene(first ? -impulse : impulse), depth, other.SceneOwner));
    }
}
