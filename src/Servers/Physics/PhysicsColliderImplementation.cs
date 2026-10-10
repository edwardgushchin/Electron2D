namespace Electron2D;

/// <summary>Owns one concrete collider attachment created by the selected world implementation.</summary>
internal abstract class PhysicsColliderImplementation(PhysicsColliderBackend owner, PhysicsSpace space)
{
    protected PhysicsColliderBackend Owner { get; } = owner;
    protected PhysicsSpace Space { get; } = space;
    internal bool IsDynamic => HasMotionMode(PhysicsServer.BodyMode.Rigid);
    internal abstract int ShapeCount { get; }
    internal abstract void Attach(Vector2 position, float rotation, in PhysicsBodyConfiguration configuration);
    internal abstract void Detach();
    internal abstract bool HasMotionMode(PhysicsServer.BodyMode mode);
    internal abstract void SetMotionMode(PhysicsServer.BodyMode mode);
    internal abstract void SetContactReporting(bool enabled);
    internal abstract void UpdateFilter(uint layer, uint mask, bool wakeBody);
    internal abstract void RebuildShapes(IReadOnlyList<CollisionObject.ShapeSlot> slots, uint layer, uint mask, bool sensor, float density, float friction, float bounce);
    internal abstract void RebuildShapes(IReadOnlyList<PhysicsServerCollider.ShapeSlot> slots, uint layer, uint mask, bool sensor, float density, float friction, float bounce);
    internal abstract (Vector2 Position, float Rotation) GetPose();
    internal abstract Transform GetTransform();
    internal abstract (Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) GetSolverMotion();
    internal abstract Vector2 LinearVelocity { get; }
    internal abstract float AngularVelocity { get; }
    internal abstract bool IsAwake { get; }
    internal abstract void SetPose(Vector2 position, float rotation);
    internal abstract void SetTargetPose(Transform pose, double delta);
    internal abstract void SavePose();
    internal abstract void RestorePose();
    internal abstract void SetLinearVelocity(Vector2 velocity);
    internal abstract void SetAngularVelocity(float velocity);
    internal abstract void ClearVelocity();
    internal abstract void SetAwake(bool awake);
    internal abstract void SetCanSleep(bool canSleep);
    internal abstract void SetRotationLocked(bool locked);
    internal abstract void SetGravityScale(float scale);
    internal abstract void ApplyCentralForce(Vector2 force, bool wake);
    internal abstract void ApplyTorque(float torque, bool wake);
    internal abstract bool RotationLocked { get; }
    internal abstract Vector2 CenterOfMass { get; }
    internal abstract Vector2 CenterOfMassLocal { get; }
    internal abstract float InverseMass { get; }
    internal abstract float InverseInertia { get; }
    internal abstract PhysicsMass.Properties ApplyMassProfile(float mass, float inertia, Vector2? center);
    internal abstract void WakeTouching();
    internal abstract Vector2 GetPointVelocity(Vector2 offset);
    internal abstract void ApplyImpulse(Vector2 impulse, float moment);
    internal abstract void SetSurfaceVelocity(Vector2 linear, float angular);
    internal abstract void CaptureViewContacts(PhysicsDirectBodyState view, int limit);
    internal abstract Transform PortablePose { get; }
    internal abstract void ApplyPortableMotion(Transform pose, Vector2 linear, float angular, float sleepTime, bool canSleep, bool sleeping,
        Vector2 surface, float surfaceAngular, Vector2 force, float torque, Vector2 gravity, float linearDamp, float angularDamp, bool fieldsInitialized);
    internal abstract void ValidatePortablePiece(int slot, int piece);
}
