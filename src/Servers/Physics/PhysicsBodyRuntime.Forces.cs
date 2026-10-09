namespace Electron2D;

internal sealed partial class PhysicsBodyRuntime
{
    internal Vector2 PendingForce;
    internal float PendingTorque;

    internal bool Dynamic
    {
        get
        {
            if (Space is not null) return Backend.IsDynamic;
            var owners = Owners;
            return owners.Scene?.RequestedBodyMode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear ||
                owners.Server?.Mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
        }
    }

    internal bool RotationLocked => Space is not null ? Backend.RotationLocked :
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
        var geometry = _massGeometry ??= new();
        geometry.Clear();
        if (owners.Scene is { } body)
        {
            PhysicsServerCollider.ValidateTransform(body.GlobalTransform);
            for (var index = 0; index < body.ShapeSlots.Count; index++)
            {
                var slot = body.ShapeSlots[index];
                if (slot.Active) geometry.Append(slot.Shape, slot.Transform, false);
            }
        }
        else owners.Server!.AppendMassGeometry(geometry);
        var mass = owners.Scene is RigidBody massBody ? massBody.Mass : Mass;
        var inertia = owners.Scene is RigidBody inertiaBody ? inertiaBody.Inertia : Inertia;
        var center = owners.Scene is RigidBody centerBody ? centerBody.CustomMassCenter : CustomCenter;
        MassProperties = geometry.Calculate(mass, inertia, center);
    }

    internal Vector2 CenterOffset
    {
        get
        {
            var center = MassProperties.Center;
            var owners = Owners;
            var rotation = Space is not null ? Backend.GetPose().Rotation :
                owners.Scene?.GlobalRotation ?? owners.Server!.GetTransform().Rotation;
            return center.Rotated(rotation);
        }
    }

    internal Vector2 GetLinearVelocity()
    {
        if (Space is not null) return Backend.LinearVelocity;
        var owners = Owners;
        return owners.Scene switch { RigidBody rigid => rigid.LinearVelocity, StaticBody surface => surface.ConstantLinearVelocity, not null => _surfaceLinear, _ => owners.Server!.GetLinearVelocity() };
    }

    internal float GetAngularVelocity() => Space is not null ? Backend.AngularVelocity :
        Owners.Scene switch { RigidBody rigid => rigid.AngularVelocity, StaticBody surface => surface.ConstantAngularVelocity, not null => _surfaceAngular, _ => Owners.Server!.GetAngularVelocity() };

    internal void ApplyImpulse(Vector2 impulse, float moment)
    {
        var inverseMass = Dynamic ? 1 / MassProperties.Mass : 0;
        var inverseInertia = Dynamic && !RotationLocked && MassProperties.Inertia > 0 ?
            1 / MassProperties.Inertia : 0;
        var velocity = GetLinearVelocity() + impulse * inverseMass;
        var angular = GetAngularVelocity() + moment * inverseInertia;
        Finite(velocity); Finite(angular);
        if (!Dynamic) return;
        if (Space is not null) Backend.ApplyImpulse(impulse, moment);
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
        if (Space is not null) Backend.SetAwake(true);
        else SetSleeping(false);
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
