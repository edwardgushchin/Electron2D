using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;

namespace Electron2D;

internal sealed partial class PhysicsBodyRuntime
{
    internal Vector2 PendingForce;
    internal float PendingTorque;

    internal bool Dynamic
    {
        get
        {
            if (Space is not null) return b2Body_GetType(BodyID) == B2BodyType.b2_dynamicBody;
            var owners = Owners;
            return owners.Scene?.RequestedBodyType == B2BodyType.b2_dynamicBody ||
                owners.Server?.Mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
        }
    }

    internal bool RotationLocked => Space is not null ? b2Body_GetMotionLocks(BodyID).angularZ :
        Owners.Scene is RigidBody rigid ? rigid.LockRotation : Owners.Server?.Mode == PhysicsServer.BodyMode.RigidLinear;

    internal void PrepareForceAccess(bool prepareGeometry)
    {
        var owners = Owners;
        owners.Scene?.Tree?.EnsureOwnerThread();
        owners.Scene?.EnsurePhysicsParticipationChange();
        EnsureMutable();
        if (!prepareGeometry) return;
        if (Space is not null)
        {
            if (owners.Scene is { } scene) scene.PrepareBackend(); else owners.Server!.PrepareBackend();
            return;
        }
        MassProxies.Clear();
        if (owners.Scene is { } body)
        {
            PhysicsServerCollider.ValidateTransform(body.GlobalTransform);
            for (var index = 0; index < body.ShapeSlots.Count; index++)
            {
                var slot = body.ShapeSlots[index];
                if (slot.Active) PhysicsMass.AppendGeometry(slot.Shape, slot.Transform, MassProxies);
            }
        }
        else owners.Server!.AppendMassGeometry(MassProxies);
        var mass = owners.Scene is RigidBody massBody ? massBody.Mass : Mass;
        var inertia = owners.Scene is RigidBody inertiaBody ? inertiaBody.Inertia : Inertia;
        var center = owners.Scene is RigidBody centerBody ? centerBody.CustomMassCenter : CustomCenter;
        MassData = PhysicsMass.Calculate(MassProxies, mass, inertia, center);
    }

    internal Vector2 CenterOffset
    {
        get
        {
            var center = new Vector2(MassData.center.X * PhysicsSpace.UnitsPerMeter, MassData.center.Y * PhysicsSpace.UnitsPerMeter);
            var owners = Owners;
            var rotation = Space is not null ? b2Rot_GetAngle(b2Body_GetRotation(BodyID)) :
                owners.Scene?.GlobalRotation ?? owners.Server!.GetTransform().Rotation;
            return center.Rotated(rotation);
        }
    }

    internal Vector2 GetLinearVelocity()
    {
        if (Space is not null)
        {
            var value = b2Body_GetLinearVelocity(BodyID);
            return new(value.X * PhysicsSpace.UnitsPerMeter, value.Y * PhysicsSpace.UnitsPerMeter);
        }
        var owners = Owners;
        return owners.Scene is RigidBody rigid ? rigid.LinearVelocity : owners.Server?.GetLinearVelocity() ?? Vector2.Zero;
    }

    internal float GetAngularVelocity() => Space is not null ? b2Body_GetAngularVelocity(BodyID) :
        Owners.Scene is RigidBody rigid ? rigid.AngularVelocity : Owners.Server?.GetAngularVelocity() ?? 0;

    internal void ApplyImpulse(Vector2 impulse, float moment)
    {
        var inverseMass = Dynamic ? 1 / MassData.mass : 0;
        var inverseInertia = Dynamic && !RotationLocked && MassData.rotationalInertia > 0 ?
            PhysicsMass.InertiaScale / MassData.rotationalInertia : 0;
        var velocity = GetLinearVelocity() + impulse * inverseMass;
        var angular = GetAngularVelocity() + moment * inverseInertia;
        Finite(velocity); Finite(angular);
        if (!Dynamic) return;
        if (Space is not null)
        {
            var id = BodyID;
            var nativeImpulse = Shape.ToBackend(impulse);
            var sim = Simulation(id);
            var nativeVelocity = b2Body_GetLinearVelocity(id) + nativeImpulse * sim.invMass;
            Finite(new Vector2(nativeVelocity.X * PhysicsSpace.UnitsPerMeter, nativeVelocity.Y * PhysicsSpace.UnitsPerMeter));
            if (!RotationLocked) Finite(b2Body_GetAngularVelocity(id) + moment * PhysicsMass.InertiaScale * sim.invInertia);
            b2Body_ApplyLinearImpulseToCenter(id, nativeImpulse, true);
            if (!RotationLocked) b2Body_ApplyAngularImpulse(id, moment * PhysicsMass.InertiaScale, true);
        }
        else
        {
            var owners = Owners;
            if (owners.Scene is RigidBody rigid) { rigid.LinearVelocity = velocity; rigid.AngularVelocity = angular; }
            else { owners.Server!.SetLinearVelocity(velocity); owners.Server.SetAngularVelocity(angular); }
            Wake();
        }
    }

    internal void AddForce(Vector2 force, float moment)
    {
        var total = PendingForce + force;
        var torque = PendingTorque + moment;
        Finite(total); Finite(torque);
        PendingForce = total; PendingTorque = torque;
        Wake();
    }

    internal void Wake()
    {
        if (!Dynamic) return;
        if (Space is not null) b2Body_SetAwake(BodyID, true);
        else if (Owners.Scene is RigidBody rigid) rigid.Sleeping = false;
    }

    internal Vector2 GetConstantForce() => Owners.Scene is RigidBody rigid ? rigid.ConstantForce : ConstantForce;
    internal float GetConstantTorque() => Owners.Scene is RigidBody rigid ? rigid.ConstantTorque : ConstantTorque;

    internal void SetConstants(Vector2 force, float torque, bool wake)
    {
        Finite(force); Finite(torque);
        if (Owners.Scene is RigidBody rigid) rigid.SetConstantTotals(force, torque);
        else { ConstantForce = force; ConstantTorque = torque; }
        if (wake) Wake();
    }

    internal static void Finite(Vector2 value) { if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); }
    internal static void Finite(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
}
