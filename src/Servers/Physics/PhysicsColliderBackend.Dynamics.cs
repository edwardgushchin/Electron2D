namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    internal bool IsDynamic => Attached.IsDynamic;
    internal bool RotationLocked => Attached.RotationLocked;
    internal Vector2 CenterOfMass => Attached.CenterOfMass;
    internal Vector2 CenterOfMassLocal => Attached.CenterOfMassLocal;
    internal float InverseMass => Attached.InverseMass;
    internal float InverseInertia => Attached.InverseInertia;
    internal PhysicsMass.Properties ApplyMassProfile(float mass, float inertia, Vector2? center) => Attached.ApplyMassProfile(mass, inertia, center);
    internal void WakeTouching() => Attached.WakeTouching();
    internal void ClearTransientForces() => CPU.ClearTransientForces();
    internal Vector2 GetPointVelocity(Vector2 offset) => Attached.GetPointVelocity(offset);
    internal void ApplyImpulse(Vector2 impulse, float moment) => Attached.ApplyImpulse(impulse, moment);
    internal void ApplyFieldMotion(bool changed, bool omitted, float linearFactor, float angularFactor, Vector2 acceleration) =>
        CPU.ApplyFieldMotion(changed, omitted, linearFactor, angularFactor, acceleration);
}
