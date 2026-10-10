using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class CPUPhysicsColliderImplementation
{
    private List<B2ShapeProxy>? _massProxies;
    internal override bool RotationLocked => b2Body_GetMotionLocks(BodyID).angularZ;
    internal override Vector2 CenterOfMass => ToScene(b2Body_GetWorldCenterOfMass(BodyID) - b2Body_GetPosition(BodyID));
    internal override Vector2 CenterOfMassLocal => ToScene(b2Body_GetLocalCenterOfMass(BodyID));
    internal override float InverseMass => Simulation(BodyID).invMass;
    internal override float InverseInertia => RotationLocked ? 0 : Simulation(BodyID).invInertia * PhysicsMass.InertiaScale;

    internal static B2BodySim Simulation(B2BodyId id)
    {
        var world = b2GetWorldFromId(b2Body_GetWorld(id));
        return b2GetBodySim(world, b2GetBodyFullId(world, id));
    }
    internal override PhysicsMass.Properties ApplyMassProfile(float mass, float inertia, Vector2? center)
    {
        var data = PhysicsMass.Apply(BodyID, _shapes, mass, inertia, center, _massProxies ??= []);
        return new(data.mass, data.rotationalInertia / PhysicsMass.InertiaScale, ToScene(data.center));
    }
    internal override void WakeTouching() { b2Body_WakeTouching(BodyID); }
    internal void ClearTransientForces() { var sim = Simulation(BodyID); sim.force = default; sim.torque = 0; }
    internal override Vector2 GetPointVelocity(Vector2 offset)
    {
        var point = b2Body_GetPosition(BodyID) + PhysicsShapeBackend.ToBackend(offset);
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y)) throw new ArgumentOutOfRangeException(nameof(offset));
        var result = ToScene(b2Body_GetWorldPointVelocity(BodyID, point));
        PhysicsBodyRuntime.Finite(result); return result;
    }
    internal override Vector2 GetWorldPointVelocity(Vector2 point)
    {
        var velocity = ToScene(b2Body_GetWorldPointVelocity(BodyID, PhysicsShapeBackend.ToBackend(point)));
        PhysicsBodyRuntime.Finite(velocity); return velocity;
    }
    internal override void ApplyImpulse(Vector2 impulse, float moment)
    {
        var nativeImpulse = PhysicsShapeBackend.ToBackend(impulse);
        var sim = Simulation(BodyID);
        var candidate = b2Body_GetLinearVelocity(BodyID) + nativeImpulse * sim.invMass;
        PhysicsBodyRuntime.Finite(ToScene(candidate));
        if (!RotationLocked) PhysicsBodyRuntime.Finite(b2Body_GetAngularVelocity(BodyID) + moment * PhysicsMass.InertiaScale * sim.invInertia);
        b2Body_ApplyLinearImpulseToCenter(BodyID, nativeImpulse, true);
        if (!RotationLocked) b2Body_ApplyAngularImpulse(BodyID, moment * PhysicsMass.InertiaScale, true);
    }
    internal void ApplyFieldMotion(bool changed, bool omitted, float linearFactor, float angularFactor, Vector2 acceleration)
    {
        var active = IsDynamic && !omitted && (changed || IsAwake);
        var linear = default(B2Vec2); var angular = 0f; var force = default(B2Vec2);
        if (active)
        {
            linear = b2Body_GetLinearVelocity(BodyID) * linearFactor;
            angular = b2Body_GetAngularVelocity(BodyID) * angularFactor;
            if (acceleration != Vector2.Zero) force = PhysicsShapeBackend.ToBackend(acceleration) * b2Body_GetMass(BodyID);
            if (!float.IsFinite(linear.X) || !float.IsFinite(linear.Y) || !float.IsFinite(angular) || !float.IsFinite(force.X) || !float.IsFinite(force.Y))
                throw new InvalidOperationException("The resolved body field would produce nonfinite motion.");
        }
        if (changed && IsDynamic) SetAwake(true);
        if (!active) return;
        if (linearFactor != 1) b2Body_SetLinearVelocity(BodyID, linear);
        if (angularFactor != 1) b2Body_SetAngularVelocity(BodyID, angular);
        if (force.X != 0 || force.Y != 0) b2Body_ApplyForceToCenter(BodyID, force, false);
    }
}
