using Electron2D;

internal static partial class PhysicsBackendOwnershipTests
{
    private sealed class WrappedCollider(PhysicsColliderBackend owner, PhysicsSpace space, PhysicsColliderImplementation inner) : PhysicsColliderImplementation(owner, space)
    {
        internal int PolicyCalls, ImpulseCalls, IdentityCalls, PriorityCalls;
        internal ShapeInputHook? ShapeInputs;
        internal override int ShapeCount => inner.ShapeCount;
        internal override Vector2 LinearVelocity => inner.LinearVelocity;
        internal override float AngularVelocity => inner.AngularVelocity;
        internal override bool IsAwake => inner.IsAwake;
        internal override bool RotationLocked => inner.RotationLocked;
        internal override Vector2 CenterOfMass => inner.CenterOfMass;
        internal override Vector2 CenterOfMassLocal => inner.CenterOfMassLocal;
        internal override float InverseMass => inner.InverseMass;
        internal override float InverseInertia => inner.InverseInertia;
        internal override Transform PortablePose => inner.PortablePose;
        internal override void Attach(Vector2 position, float rotation, in PhysicsBodyConfiguration configuration) => inner.Attach(position, rotation, configuration);
        internal override void Detach() => inner.Detach();
        internal override void PrepareDefaultForces(PhysicsBodyRuntime runtime, PhysicsBody? scene) => inner.PrepareDefaultForces(runtime, scene);
        internal override bool HasMotionMode(PhysicsServer.BodyMode mode) => inner.HasMotionMode(mode);
        internal override void SetMotionMode(PhysicsServer.BodyMode mode) => inner.SetMotionMode(mode);
        internal override void SetContactReporting(bool enabled) => inner.SetContactReporting(enabled);
        internal override void RefreshIdentity() { IdentityCalls++; inner.RefreshIdentity(); }
        internal override void SetCollisionPriority(float value) { PriorityCalls++; inner.SetCollisionPriority(value); }
        internal override void PrepareContactPolicy() { PolicyCalls++; inner.PrepareContactPolicy(); }
        internal override void UpdateFilter(uint layer, uint mask, bool wakeBody) => inner.UpdateFilter(layer, mask, wakeBody);
        internal override void RebuildShapes(ReadOnlySpan<PhysicsColliderShape> slots, uint layer, uint mask, bool sensor, float density, float friction, float bounce)
        {
            ShapeInputs?.Invoke(Owner, slots, sensor);
            inner.RebuildShapes(slots, layer, mask, sensor, density, friction, bounce);
        }
        internal override (Vector2 Position, float Rotation) GetPose() => inner.GetPose();
        internal override Transform GetTransform() => inner.GetTransform();
        internal override Transform SampleJointLocalFrame(Vector2 point, float angle) => inner.SampleJointLocalFrame(point, angle);
        internal override (Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) GetSolverMotion() => inner.GetSolverMotion();
        internal override void SetPose(Vector2 position, float rotation) => inner.SetPose(position, rotation);
        internal override void SetTargetPose(Transform pose, double delta) => inner.SetTargetPose(pose, delta);
        internal override void SavePose() => inner.SavePose();
        internal override void RestorePose() => inner.RestorePose();
        internal override void SetLinearVelocity(Vector2 velocity) => inner.SetLinearVelocity(velocity);
        internal override void SetAngularVelocity(float velocity) => inner.SetAngularVelocity(velocity);
        internal override void ClearVelocity() => inner.ClearVelocity();
        internal override void SetAwake(bool awake) => inner.SetAwake(awake);
        internal override void SetCanSleep(bool canSleep) => inner.SetCanSleep(canSleep);
        internal override void SetRotationLocked(bool locked) => inner.SetRotationLocked(locked);
        internal override void SetGravityScale(float scale) => inner.SetGravityScale(scale);
        internal override void ApplyCentralForce(Vector2 force, bool wake) => inner.ApplyCentralForce(force, wake);
        internal override void ApplyTorque(float torque, bool wake) => inner.ApplyTorque(torque, wake);
        internal override PhysicsMass.Properties ApplyMassProfile(float mass, float inertia, Vector2? center) => inner.ApplyMassProfile(mass, inertia, center);
        internal override void WakeTouching() => inner.WakeTouching();
        internal override Vector2 GetPointVelocity(Vector2 offset) => inner.GetPointVelocity(offset);
        internal override Vector2 GetWorldPointVelocity(Vector2 point) => inner.GetWorldPointVelocity(point);
        internal override void ApplyImpulse(Vector2 impulse, float moment) { ImpulseCalls++; inner.ApplyImpulse(impulse, moment); }
        internal override void SetSurfaceVelocity(Vector2 linear, float angular) => inner.SetSurfaceVelocity(linear, angular);
        internal override void CaptureViewContacts(PhysicsDirectBodyState view, int limit) => inner.CaptureViewContacts(view, limit);
        internal override void ApplyPortableMotion(Transform pose, Vector2 linear, float angular, float sleepTime, bool canSleep, bool sleeping, Vector2 surface, float surfaceAngular, Vector2 force, float torque, Vector2 gravity, float linearDamp, float angularDamp, bool fieldsInitialized) => inner.ApplyPortableMotion(pose, linear, angular, sleepTime, canSleep, sleeping, surface, surfaceAngular, force, torque, gravity, linearDamp, angularDamp, fieldsInitialized);
        internal override void ValidatePortablePiece(int slot, int piece) => inner.ValidatePortablePiece(slot, piece);
    }
}
