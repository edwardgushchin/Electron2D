namespace Electron2D;

internal sealed partial class RegisteredPhysicsWorldBackend
{
    private sealed class Collider(RegisteredPhysicsWorldBackend world, PhysicsColliderBackend owner, PhysicsSpace space, PhysicsBackendCollider implementation)
        : PhysicsColliderImplementation(owner, space)
    {
        internal override int ShapeCount { get { world.Enter(); try { return CountResult(implementation.ShapeCount); } finally { world.Exit(); } } }
        internal override Vector2 LinearVelocity { get { world.Enter(); try { return VectorResult(implementation.LinearVelocity); } finally { world.Exit(); } } }
        internal override float AngularVelocity { get { world.Enter(); try { return ScalarResult(implementation.AngularVelocity); } finally { world.Exit(); } } }
        internal override bool IsAwake { get { world.Enter(); try { return implementation.IsAwake; } finally { world.Exit(); } } }
        internal override bool RotationLocked { get { world.Enter(); try { return implementation.RotationLocked; } finally { world.Exit(); } } }
        internal override Vector2 CenterOfMass { get { world.Enter(); try { return VectorResult(implementation.CenterOfMass); } finally { world.Exit(); } } }
        internal override Vector2 CenterOfMassLocal { get { world.Enter(); try { return VectorResult(implementation.CenterOfMassLocal); } finally { world.Exit(); } } }
        internal override float InverseMass { get { world.Enter(); try { return NonnegativeResult(implementation.InverseMass); } finally { world.Exit(); } } }
        internal override float InverseInertia { get { world.Enter(); try { return NonnegativeResult(implementation.InverseInertia); } finally { world.Exit(); } } }
        internal override Transform PortablePose { get { world.Enter(); try { return TransformResult(implementation.PortablePose); } finally { world.Exit(); } } }
        internal override void Attach(Vector2 position, float rotation, in PhysicsBodyConfiguration configuration)
        {
            world.Enter();
            try
            {
                implementation.Attach(position, rotation, configuration);
                implementation.RefreshIdentity(Owner.ObjectIdentity.ID, Owner.CanvasInstanceID);
                implementation.SetCollisionPriority(Owner.CollisionPriority);
            }
            finally { world.Exit(); }
        }
        internal override void PrepareDefaultForces(in PhysicsBodyForceConfiguration configuration) { world.Enter(); try { implementation.PrepareDefaultForces(configuration); } finally { world.Exit(); } }
        internal override bool HasMotionMode(PhysicsServer.BodyMode mode) { world.Enter(); try { return implementation.HasMotionMode(mode); } finally { world.Exit(); } }
        internal override void SetMotionMode(PhysicsServer.BodyMode mode) { world.Enter(); try { implementation.SetMotionMode(mode); } finally { world.Exit(); } }
        internal override void SetContactReporting(bool enabled) { world.Enter(); try { implementation.SetContactReporting(enabled); } finally { world.Exit(); } }
        internal override void PrepareContactPolicy() { world.Enter(); try { implementation.PrepareContactPolicy(); } finally { world.Exit(); } }
        internal override void InvalidateCollisionExceptions() { world._exceptionsDirty = true; world.Enter(); try { implementation.InvalidateCollisionExceptions(); } finally { world.Exit(); } }
        internal override PhysicsBodyFields ReadResolvedFields(in PhysicsBodyFields current) { world.Enter(); try { return FieldResult(implementation.ReadResolvedFields(current)); } finally { world.Exit(); } }
        internal override void SetCollisionPriority(float value) { world.Enter(); try { implementation.SetCollisionPriority(value); } finally { world.Exit(); } }
        internal override void UpdateFilter(uint layer, uint mask, bool wakeBody) { world.Enter(); try { implementation.UpdateFilter(layer, mask, wakeBody); } finally { world.Exit(); } }
        internal override void RebuildShapes(ReadOnlySpan<PhysicsColliderShape> slots, uint layer, uint mask, bool sensor, float density, float friction, float bounce) { world.Enter(); try { implementation.RebuildShapes(slots, layer, mask, sensor, density, friction, bounce); } finally { world.Exit(); } }
        internal override (Vector2 Position, float Rotation) GetPose() { world.Enter(); try { return PoseResult(implementation.GetPose()); } finally { world.Exit(); } }
        internal override Transform GetTransform() { world.Enter(); try { return TransformResult(implementation.GetTransform()); } finally { world.Exit(); } }
        internal override Transform SampleJointLocalFrame(Vector2 point, float angle) { world.Enter(); try { return TransformResult(implementation.SampleJointLocalFrame(point, angle)); } finally { world.Exit(); } }
        internal override (Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) GetSolverMotion() { world.Enter(); try { return MotionResult(implementation.GetSolverMotion()); } finally { world.Exit(); } }
        internal override void SetPose(Vector2 position, float rotation) { world.Enter(); try { implementation.SetPose(position, rotation); } finally { world.Exit(); } }
        internal override void SetTargetPose(Transform pose, double delta) { world.Enter(); try { implementation.SetTargetPose(pose, delta); } finally { world.Exit(); } }
        internal override void SavePose() { world.Enter(); try { implementation.SavePose(); } finally { world.Exit(); } }
        internal override void RestorePose() { world.Enter(); try { implementation.RestorePose(); } finally { world.Exit(); } }
        internal override void SetLinearVelocity(Vector2 velocity) { world.Enter(); try { implementation.SetLinearVelocity(velocity); } finally { world.Exit(); } }
        internal override void SetAngularVelocity(float velocity) { world.Enter(); try { implementation.SetAngularVelocity(velocity); } finally { world.Exit(); } }
        internal override void ClearVelocity() { world.Enter(); try { implementation.ClearVelocity(); } finally { world.Exit(); } }
        internal override void SetAwake(bool awake) { world.Enter(); try { implementation.SetAwake(awake); } finally { world.Exit(); } }
        internal override void SetCanSleep(bool canSleep) { world.Enter(); try { implementation.SetCanSleep(canSleep); } finally { world.Exit(); } }
        internal override void SetRotationLocked(bool locked) { world.Enter(); try { implementation.SetRotationLocked(locked); } finally { world.Exit(); } }
        internal override void SetGravityScale(float scale) { world.Enter(); try { implementation.SetGravityScale(scale); } finally { world.Exit(); } }
        internal override void ApplyCentralForce(Vector2 force, bool wake) { world.Enter(); try { implementation.ApplyCentralForce(force, wake); } finally { world.Exit(); } }
        internal override void ApplyTorque(float torque, bool wake) { world.Enter(); try { implementation.ApplyTorque(torque, wake); } finally { world.Exit(); } }
        internal override PhysicsMass.Properties ApplyMassProfile(float mass, float inertia, Vector2? center) { world.Enter(); try { return MassResult(implementation.ApplyMassProfile(mass, inertia, center)); } finally { world.Exit(); } }
        internal override void WakeTouching() { world.Enter(); try { implementation.WakeTouching(); } finally { world.Exit(); } }
        internal override Vector2 GetPointVelocity(Vector2 offset) { world.Enter(); try { return VectorResult(implementation.GetPointVelocity(offset)); } finally { world.Exit(); } }
        internal override Vector2 GetWorldPointVelocity(Vector2 point) { world.Enter(); try { return VectorResult(implementation.GetWorldPointVelocity(point)); } finally { world.Exit(); } }
        internal override void ApplyImpulse(Vector2 impulse, float moment) { world.Enter(); try { implementation.ApplyImpulse(impulse, moment); } finally { world.Exit(); } }
        internal override void SetSurfaceVelocity(Vector2 linear, float angular) { world.Enter(); try { implementation.SetSurfaceVelocity(linear, angular); } finally { world.Exit(); } }
        internal override void ApplyPortableMotion(Transform pose, Vector2 linear, float angular, float sleepTime, bool canSleep, bool sleeping,
        Vector2 surface, float surfaceAngular, Vector2 force, float torque, Vector2 gravity, float linearDamp, float angularDamp, bool fieldsInitialized)
        { world.Enter(); try { implementation.ApplyPortableMotion(pose, linear, angular, sleepTime, canSleep, sleeping, surface, surfaceAngular, force, torque, gravity, linearDamp, angularDamp, fieldsInitialized); } finally { world.Exit(); } }
        internal override void ValidatePortablePiece(int slot, int piece) { world.Enter(); try { implementation.ValidatePortablePiece(slot, piece); } finally { world.Exit(); } }
        private PhysicsBodyContact[] _contacts = [];
        internal void PrepareIntegration(in PhysicsBodyIntegrationConfiguration configuration, double delta)
        { world.Enter(); try { implementation.PrepareIntegration(configuration, delta); } finally { world.Exit(); } }
        internal void SetAreaFields(in PhysicsAreaFieldConfiguration configuration)
        { world.Enter(); try { implementation.SetAreaFields(configuration); } finally { world.Exit(); } }
        internal void SetAreaMonitoring(bool monitoring, bool monitorable)
        { world.Enter(); try { implementation.SetAreaMonitoring(monitoring, monitorable); } finally { world.Exit(); } }
        internal override void RefreshIdentity()
        { world.Enter(); try { implementation.RefreshIdentity(Owner.ObjectIdentity.ID, Owner.CanvasInstanceID); } finally { world.Exit(); } }
        internal override void Detach()
        {
            world.Enter();
            try { implementation.Detach(); }
            finally { world.Exit(); if (world._colliders.TryGetValue(Owner, out var current) && ReferenceEquals(current, this)) world._colliders.Remove(Owner); world._exceptionsDirty = true; }
        }
        internal override void CaptureViewContacts(PhysicsDirectBodyState view, int limit)
        {
            if (_contacts.Length < limit) Array.Resize(ref _contacts, limit);
            Space.BeginContactResultPublication(); world.Enter();
            try
            {
                var count = implementation.ReadContacts(_contacts.AsSpan(0, limit));
                if ((uint)count > (uint)limit) throw new InvalidOperationException("A collider contact count exceeded its supplied capacity.");
                for (var i = 0; i < count; i++)
                {
                    var c = _contacts[i];
                    if (c.BodyRID != Owner.RID || c.BodyAttachment != Owner.AttachmentVersion) throw new InvalidOperationException("A collider contact belongs to a different attachment.");
                    var remote = PhysicsServer.Service.CaptureResultCollider(c.ColliderRID, c.ColliderShape, bodyOnly: true, contactResult: true);
                    if (!ReferenceEquals(remote.Space, Space) || !PhysicsServer.Service.IsColliderShapeEnabled(Owner.RID, c.LocalShape, contactResult: true) ||
                        !PhysicsServer.Service.IsColliderShapeEnabled(c.ColliderRID, c.ColliderShape, contactResult: true))
                        throw new InvalidOperationException("A collider contact belongs to an inactive shape or different space.");
                }
                for (var i = 0; i < count; i++)
                {
                    var c = _contacts[i]; var slot = view.SelectContactSlot(0, limit);
                    view.StoreContact(slot, new(c.ColliderRID, c.ColliderID, c.LocalShape, c.ColliderShape, c.LocalPosition, c.ColliderPosition,
                        c.Normal, c.LocalVelocity, c.ColliderVelocity, c.Impulse, 0, c.ColliderOwner, c.Identity));
                }
            }
            finally { Array.Clear(_contacts); world.Exit(); Space.EndContactResultPublication(); }
        }
    }
}
