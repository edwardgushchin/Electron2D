using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    private List<B2ShapeProxy>? _massProxies;
    internal bool IsDynamic => HasMotionMode(PhysicsServer.BodyMode.Rigid);
    internal bool RotationLocked => GPU is not null ? GPUState.RotationLocked : b2Body_GetMotionLocks(BodyID).angularZ;
    internal Vector2 CenterOfMass => GPU is not null ? CenterOfMassLocal.Rotated(GPUState.Rotation) : ToScene(b2Body_GetWorldCenterOfMass(BodyID) - b2Body_GetPosition(BodyID));
    internal Vector2 CenterOfMassLocal => GPU is { } gpu ? gpu.GetMassProperties(GPUHandle).Center : ToScene(b2Body_GetLocalCenterOfMass(BodyID));
    internal float InverseMass => GPU is { } gpu ? IsDynamic ? 1 / gpu.GetMassProperties(GPUHandle).Mass : 0 : Simulation(BodyID).invMass;
    internal float InverseInertia => RotationLocked ? 0 : GPU is { } gpu ? IsDynamic && gpu.GetMassProperties(GPUHandle).Inertia is > 0 and var inertia ? 1 / inertia : 0 : Simulation(BodyID).invInertia * PhysicsMass.InertiaScale;

    internal static B2BodySim Simulation(B2BodyId id)
    {
        var world = b2GetWorldFromId(b2Body_GetWorld(id));
        return b2GetBodySim(world, b2GetBodyFullId(world, id));
    }
    internal PhysicsMass.Properties ApplyMassProfile(float mass, float inertia, Vector2? center)
    {
        if (GPU is { } gpu) { gpu.SetMassProfile(GPUHandle, new(mass, inertia, center)); Space!.InvalidateGPUStates(); return gpu.GetMassProperties(GPUHandle); }
        var data = PhysicsMass.Apply(BodyID, _shapes, mass, inertia, center, _massProxies ??= []);
        return new(data.mass, data.rotationalInertia / PhysicsMass.InertiaScale, ToScene(data.center));
    }
    internal void WakeTouching() { if (GPU is { } gpu) { gpu.WakeConnected(GPUHandle); Space!.InvalidateGPUStates(); } else b2Body_WakeTouching(BodyID); }
    internal void ClearTransientForces() { var sim = Simulation(BodyID); sim.force = default; sim.torque = 0; }
    internal Vector2 GetPointVelocity(Vector2 offset)
    {
        if (GPU is not null)
        {
            var arm = offset - CenterOfMass;
            var value = LinearVelocity + new Vector2(-arm.Y, arm.X) * AngularVelocity;
            PhysicsBodyRuntime.Finite(value); return value;
        }
        var point = b2Body_GetPosition(BodyID) + PhysicsShapeBackend.ToBackend(offset);
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y)) throw new ArgumentOutOfRangeException(nameof(offset));
        var result = ToScene(b2Body_GetWorldPointVelocity(BodyID, point));
        PhysicsBodyRuntime.Finite(result); return result;
    }
    internal void ApplyImpulse(Vector2 impulse, float moment)
    {
        if (GPU is { } gpu) { gpu.ApplyImpulse(GPUHandle, impulse, moment); Space!.InvalidateGPUStates(); return; }
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
