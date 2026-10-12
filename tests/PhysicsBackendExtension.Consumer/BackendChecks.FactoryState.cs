using Electron2D;

public static partial class BackendChecks
{
    private sealed class OwnedState : PhysicsDirectBodyStateExtension
    {
        private readonly Solver solver;
        private readonly RID body;
        internal OwnedState(Solver owner, RID id) : base(id) { solver = owner; body = id; }
        private Body Data => solver.Bodies[body];
        internal int ContactReads;
        internal bool BadContactCount;
        internal Action? ContactHook;
        protected override float GetAngularVelocityCore() => PhysicsServer.BodyGetAngularVelocity(body);
        protected override void SetAngularVelocityCore(float value) => PhysicsServer.BodySetAngularVelocity(body, value);
        protected override Vector2 GetLinearVelocityCore() => PhysicsServer.BodyGetLinearVelocity(body);
        protected override void SetLinearVelocityCore(Vector2 value) => PhysicsServer.BodySetLinearVelocity(body, value);
        protected override Vector2 GetCenterOfMassCore() => Data.Center;
        protected override Vector2 GetCenterOfMassLocalCore() => Data.LocalCenter;
        protected override float GetInverseMassCore() => Data.InvMass;
        protected override float GetInverseInertiaCore() => Data.InvInertia;
        protected override bool IsSleepingCore() => PhysicsServer.BodyGetSleeping(body);
        protected override void SetSleepStateCore(bool value) => PhysicsServer.BodySetSleeping(body, value);
        protected override float GetStepCore() => solver.Delta;
        protected override Vector2 GetTotalGravityCore() => Data.Fields.Gravity;
        protected override float GetTotalLinearDampCore() => Data.Fields.LinearDamp;
        protected override float GetTotalAngularDampCore() => Data.Fields.AngularDamp;
        protected override uint GetCollisionLayerCore() => PhysicsServer.BodyGetCollisionLayer(body);
        protected override void SetCollisionLayerCore(uint value) => PhysicsServer.BodySetCollisionLayer(body, value);
        protected override uint GetCollisionMaskCore() => PhysicsServer.BodyGetCollisionMask(body);
        protected override void SetCollisionMaskCore(uint value) => PhysicsServer.BodySetCollisionMask(body, value);
        protected override Transform GetTransformCore() => PhysicsServer.BodyGetTransform(body);
        protected override void SetTransformCore(Transform value) => PhysicsServer.BodySetTransform(body, value);
        protected override Vector2 GetConstantForceCore() => PhysicsServer.BodyGetConstantForce(body);
        protected override float GetConstantTorqueCore() => PhysicsServer.BodyGetConstantTorque(body);
        protected override int GetContactCountCore()
        { ContactHook?.Invoke(); ContactReads++; return BadContactCount ? -1 : Data.ContactPeer.IsValid() ? Math.Min(1, Data.ReportedLimit) : 0; }
        protected override PhysicsDirectSpaceState GetSpaceStateCore() => PhysicsServer.SpaceGetDirectState(solver.Space);
        protected override Vector2 GetVelocityAtLocalPositionCore(Vector2 localPosition) => Data.PointVelocity(Data.Center + localPosition);
        protected override void SetConstantForceCore(Vector2 force) => PhysicsServer.BodySetConstantForce(body, force);
        protected override void SetConstantTorqueCore(float torque) => PhysicsServer.BodySetConstantTorque(body, torque);
        protected override void AddConstantCentralForceCore(Vector2 force) => PhysicsServer.BodyAddConstantCentralForce(body, force);
        protected override void AddConstantTorqueCore(float torque) => PhysicsServer.BodyAddConstantTorque(body, torque);
        protected override void AddConstantForceCore(Vector2 force, Vector2 position) => PhysicsServer.BodyAddConstantForce(body, force, position);
        protected override void ApplyCentralForceCore(Vector2 force) => PhysicsServer.BodyApplyCentralForce(body, force);
        protected override void ApplyCentralImpulseCore(Vector2 impulse) => PhysicsServer.BodyApplyCentralImpulse(body, impulse);
        protected override void ApplyForceCore(Vector2 force, Vector2 position) => PhysicsServer.BodyApplyForce(body, force, position);
        protected override void ApplyImpulseCore(Vector2 impulse, Vector2 position) => PhysicsServer.BodyApplyImpulse(body, impulse, position);
        protected override void ApplyTorqueCore(float torque) => PhysicsServer.BodyApplyTorque(body, torque);
        protected override void ApplyTorqueImpulseCore(float impulse) => PhysicsServer.BodyApplyTorqueImpulse(body, impulse);
        protected override void IntegrateForcesCore()
        {
            LinearVelocity = (LinearVelocity + TotalGravity * Step) * MathF.Max(0, 1 - Step * TotalLinearDamp);
            AngularVelocity *= MathF.Max(0, 1 - Step * TotalAngularDamp);
        }
        protected override PhysicsBodyContact GetContactCore(int contactIndex)
        {
            var data = Data; var peer = solver.Bodies[data.ContactPeer];
            return new(body, 0, peer.RID, 0, data.ContactPoint, data.ContactPoint, data.ContactNormal,
                data.PointVelocity(data.ContactPoint), peer.PointVelocity(data.ContactPoint), data.ContactImpulse, data.ContactDepth);
        }
    }
}
