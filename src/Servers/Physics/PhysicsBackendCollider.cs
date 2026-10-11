namespace Electron2D;

/// <summary>Defines one numerical collider attachment without scene, runtime or native handles.</summary>
public abstract class PhysicsBackendCollider
{
    private int _claimed;
    internal void Claim() { if (Interlocked.CompareExchange(ref _claimed, 1, 0) != 0) throw new InvalidOperationException("A collider implementation can belong to only one attachment."); }
    /// <summary>Supplies the selected implementation operation: shape count.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract int ShapeCount { get; }
    /// <summary>Supplies the selected implementation operation: attach.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void Attach(Vector2 position, float rotation, in PhysicsBodyConfiguration configuration);
    /// <summary>Supplies the selected implementation operation: detach.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void Detach();
    /// <summary>Supplies the selected implementation operation: prepare default forces.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void PrepareDefaultForces(in PhysicsBodyForceConfiguration configuration);
    /// <summary>Supplies the selected implementation operation: has motion mode.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract bool HasMotionMode(PhysicsServer.BodyMode mode);
    /// <summary>Supplies the selected implementation operation: set motion mode.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetMotionMode(PhysicsServer.BodyMode mode);
    /// <summary>Supplies the selected implementation operation: set contact reporting.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetContactReporting(bool enabled);
    /// <summary>Supplies the selected implementation operation: prepare contact policy.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void PrepareContactPolicy();
    /// <summary>Supplies the selected implementation operation: prepare integration.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void PrepareIntegration(in PhysicsBodyIntegrationConfiguration configuration, double delta);
    /// <summary>Supplies the selected implementation operation: set area fields.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetAreaFields(in PhysicsAreaFieldConfiguration configuration);
    /// <summary>Supplies the selected implementation operation: set area monitoring.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetAreaMonitoring(bool monitoring, bool monitorable);
    /// <summary>Supplies the selected implementation operation: invalidate collision exceptions.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void InvalidateCollisionExceptions();
    /// <summary>Supplies the selected implementation operation: read resolved fields.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract PhysicsBodyFields ReadResolvedFields(in PhysicsBodyFields current);
    /// <summary>Supplies the selected implementation operation: refresh identity.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void RefreshIdentity(ulong objectID, ulong canvasID);
    /// <summary>Supplies the selected implementation operation: set collision priority.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetCollisionPriority(float value);
    /// <summary>Supplies the selected implementation operation: update filter.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void UpdateFilter(uint layer, uint mask, bool wakeBody);
    /// <summary>Supplies the selected implementation operation: rebuild shapes.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void RebuildShapes(ReadOnlySpan<PhysicsColliderShape> slots, uint layer, uint mask, bool sensor, float density, float friction, float bounce);
    /// <summary>Supplies the selected implementation operation: get pose.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract (Vector2 Position, float Rotation) GetPose();
    /// <summary>Supplies the selected implementation operation: get transform.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract Transform GetTransform();
    /// <summary>Supplies the selected implementation operation: sample joint local frame.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract Transform SampleJointLocalFrame(Vector2 point, float angle);
    /// <summary>Supplies the selected implementation operation: get solver motion.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract (Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) GetSolverMotion();
    /// <summary>Supplies the selected implementation operation: linear velocity.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract Vector2 LinearVelocity { get; }
    /// <summary>Supplies the selected implementation operation: angular velocity.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract float AngularVelocity { get; }
    /// <summary>Supplies the selected implementation operation: is awake.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract bool IsAwake { get; }
    /// <summary>Supplies the selected implementation operation: set pose.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetPose(Vector2 position, float rotation);
    /// <summary>Supplies the selected implementation operation: set target pose.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetTargetPose(Transform pose, double delta);
    /// <summary>Supplies the selected implementation operation: save pose.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SavePose();
    /// <summary>Supplies the selected implementation operation: restore pose.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void RestorePose();
    /// <summary>Supplies the selected implementation operation: set linear velocity.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetLinearVelocity(Vector2 velocity);
    /// <summary>Supplies the selected implementation operation: set angular velocity.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetAngularVelocity(float velocity);
    /// <summary>Supplies the selected implementation operation: clear velocity.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void ClearVelocity();
    /// <summary>Supplies the selected implementation operation: set awake.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetAwake(bool awake);
    /// <summary>Supplies the selected implementation operation: set can sleep.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetCanSleep(bool canSleep);
    /// <summary>Supplies the selected implementation operation: set rotation locked.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetRotationLocked(bool locked);
    /// <summary>Supplies the selected implementation operation: set gravity scale.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetGravityScale(float scale);
    /// <summary>Supplies the selected implementation operation: apply central force.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void ApplyCentralForce(Vector2 force, bool wake);
    /// <summary>Supplies the selected implementation operation: apply torque.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void ApplyTorque(float torque, bool wake);
    /// <summary>Supplies the selected implementation operation: rotation locked.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract bool RotationLocked { get; }
    /// <summary>Supplies the selected implementation operation: center of mass.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract Vector2 CenterOfMass { get; }
    /// <summary>Supplies the selected implementation operation: center of mass local.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract Vector2 CenterOfMassLocal { get; }
    /// <summary>Supplies the selected implementation operation: inverse mass.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract float InverseMass { get; }
    /// <summary>Supplies the selected implementation operation: inverse inertia.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract float InverseInertia { get; }
    /// <summary>Supplies the selected implementation operation: apply mass profile.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract PhysicsMassProperties ApplyMassProfile(float mass, float inertia, Vector2? center);
    /// <summary>Supplies the selected implementation operation: wake touching.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void WakeTouching();
    /// <summary>Supplies the selected implementation operation: get point velocity.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract Vector2 GetPointVelocity(Vector2 offset);
    /// <summary>Supplies the selected implementation operation: get world point velocity.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract Vector2 GetWorldPointVelocity(Vector2 point);
    /// <summary>Supplies the selected implementation operation: apply impulse.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void ApplyImpulse(Vector2 impulse, float moment);
    /// <summary>Supplies the selected implementation operation: set surface velocity.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetSurfaceVelocity(Vector2 linear, float angular);
    /// <summary>Supplies the selected implementation operation: read contacts.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract int ReadContacts(Span<PhysicsBodyContact> contacts);
    /// <summary>Supplies the selected implementation operation: portable pose.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract Transform PortablePose { get; }
    /// <summary>Supplies the selected implementation operation: apply portable motion.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void ApplyPortableMotion(Transform pose, Vector2 linear, float angular, float sleepTime, bool canSleep, bool sleeping,
        Vector2 surface, float surfaceAngular, Vector2 force, float torque, Vector2 gravity, float linearDamp, float angularDamp, bool fieldsInitialized);
    /// <summary>Supplies the selected implementation operation: validate portable piece.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void ValidatePortablePiece(int slot, int piece);
}
