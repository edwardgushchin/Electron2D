using Electron2D;

internal sealed class ControlledState : PhysicsDirectBodyStateExtension
{
    private readonly RID _body;
    private PhysicsDirectBodyState _inner;
    private PhysicsDirectBodyState Inner => _inner.IsDisposed ? _inner = PhysicsServer.BodyGetDirectState(_body)! : _inner;
    internal ulong Seen;
    internal Action? OnRead;
    internal bool Fail, BadScalar, BadVector, BadTransform, BadCount;
    internal PhysicsDirectSpaceState? WrongSpace;
    internal PhysicsBodyContact Contact;
    internal bool HasContact;
    internal ControlledState(RID body) : base(body) { _body = body; _inner = PhysicsServer.BodyGetDirectState(body)!; }
    internal bool CaptureContact()
    {
        if (Inner.GetContactCount() == 0) return false;
        Contact = new(_body, Inner.GetContactLocalShape(0), Inner.GetContactCollider(0), Inner.GetContactColliderShape(0),
            Inner.GetContactLocalPosition(0), Inner.GetContactColliderPosition(0), Inner.GetContactLocalNormal(0),
            Inner.GetContactLocalVelocityAtPosition(0), Inner.GetContactColliderVelocityAtPosition(0), Inner.GetContactImpulse(0));
        HasContact = true; return true;
    }
    protected override float GetAngularVelocityCore() { Seen |= 1UL << 0; return Inner.AngularVelocity; }
    protected override void SetAngularVelocityCore(float value) { Seen |= 1UL << 1; Inner.AngularVelocity = value; }
    protected override Vector2 GetLinearVelocityCore() { Seen |= 1UL << 2; OnRead?.Invoke(); if (Fail) throw new InvalidOperationException("Expected hook failure."); if (BadVector) return new(float.NaN, 0); return Inner.LinearVelocity; }
    protected override void SetLinearVelocityCore(Vector2 value) { Seen |= 1UL << 3; Inner.LinearVelocity = value; }
    protected override Vector2 GetCenterOfMassCore() { Seen |= 1UL << 4; return Inner.CenterOfMass; }
    protected override Vector2 GetCenterOfMassLocalCore() { Seen |= 1UL << 5; return Inner.CenterOfMassLocal; }
    protected override float GetInverseMassCore() { Seen |= 1UL << 6; if (BadScalar) return -1; return Inner.InverseMass; }
    protected override float GetInverseInertiaCore() { Seen |= 1UL << 7; return Inner.InverseInertia; }
    protected override bool IsSleepingCore() { Seen |= 1UL << 8; return Inner.Sleeping; }
    protected override void SetSleepStateCore(bool value) { Seen |= 1UL << 9; Inner.Sleeping = value; }
    protected override float GetStepCore() { Seen |= 1UL << 10; return Inner.Step; }
    protected override Vector2 GetTotalGravityCore() { Seen |= 1UL << 11; return Inner.TotalGravity; }
    protected override float GetTotalLinearDampCore() { Seen |= 1UL << 12; return Inner.TotalLinearDamp; }
    protected override float GetTotalAngularDampCore() { Seen |= 1UL << 13; return Inner.TotalAngularDamp; }
    protected override uint GetCollisionLayerCore() { Seen |= 1UL << 14; return Inner.CollisionLayer; }
    protected override void SetCollisionLayerCore(uint value) { Seen |= 1UL << 15; Inner.CollisionLayer = value; }
    protected override uint GetCollisionMaskCore() { Seen |= 1UL << 16; return Inner.CollisionMask; }
    protected override void SetCollisionMaskCore(uint value) { Seen |= 1UL << 17; Inner.CollisionMask = value; }
    protected override Transform GetTransformCore() { Seen |= 1UL << 18; if (BadTransform) return new(0, new(2, 1), 0, Vector2.Zero); return Inner.Transform; }
    protected override void SetTransformCore(Transform value) { Seen |= 1UL << 19; Inner.Transform = value; }
    protected override Vector2 GetConstantForceCore() { Seen |= 1UL << 20; return Inner.GetConstantForce(); }
    protected override float GetConstantTorqueCore() { Seen |= 1UL << 21; return Inner.GetConstantTorque(); }
    protected override int GetContactCountCore() { Seen |= 1UL << 22; return BadCount ? 5000 : HasContact ? 1 : Inner.GetContactCount(); }
    protected override PhysicsDirectSpaceState GetSpaceStateCore() { Seen |= 1UL << 23; return WrongSpace ?? Inner.GetSpaceState(); }
    protected override Vector2 GetVelocityAtLocalPositionCore(Vector2 localPosition) { Seen |= 1UL << 24; return Inner.GetVelocityAtLocalPosition(localPosition); }
    protected override void SetConstantForceCore(Vector2 force) { Seen |= 1UL << 25; Inner.SetConstantForce(force); }
    protected override void SetConstantTorqueCore(float torque) { Seen |= 1UL << 26; Inner.SetConstantTorque(torque); }
    protected override void AddConstantCentralForceCore(Vector2 force) { Seen |= 1UL << 27; Inner.AddConstantCentralForce(force); }
    protected override void AddConstantTorqueCore(float torque) { Seen |= 1UL << 28; Inner.AddConstantTorque(torque); }
    protected override void AddConstantForceCore(Vector2 force, Vector2 position) { Seen |= 1UL << 29; Inner.AddConstantForce(force, position); }
    protected override void ApplyCentralForceCore(Vector2 force) { Seen |= 1UL << 30; Inner.ApplyCentralForce(force); }
    protected override void ApplyCentralImpulseCore(Vector2 impulse) { Seen |= 1UL << 31; Inner.ApplyCentralImpulse(impulse * 2); }
    protected override void ApplyForceCore(Vector2 force, Vector2 position) { Seen |= 1UL << 32; Inner.ApplyForce(force, position); }
    protected override void ApplyImpulseCore(Vector2 impulse, Vector2 position) { Seen |= 1UL << 33; Inner.ApplyImpulse(impulse, position); }
    protected override void ApplyTorqueCore(float torque) { Seen |= 1UL << 34; Inner.ApplyTorque(torque); }
    protected override void ApplyTorqueImpulseCore(float impulse) { Seen |= 1UL << 35; Inner.ApplyTorqueImpulse(impulse); }
    protected override void IntegrateForcesCore() { Seen |= 1UL << 36; Inner.IntegrateForces(); }
    protected override PhysicsBodyContact GetContactCore(int contactIndex) { Seen |= 1UL << 37; return Contact; }
}
