namespace Electron2D;

public abstract partial class PhysicsDirectBodyStateExtension
{
    /// <summary>Implements AngularVelocity for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.AngularVelocity"/>.</remarks>
    protected abstract float GetAngularVelocityCore();
    internal float ReadAngularVelocity() => Read(static owner => Result(owner.GetAngularVelocityCore()));

    /// <summary>Implements AngularVelocity for the current borrowed attachment.</summary>
    /// <param name="value">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.AngularVelocity"/>.</remarks>
    protected abstract void SetAngularVelocityCore(float value);
    internal void WriteAngularVelocity(float value) => Write(value, static (owner, argument) => owner.SetAngularVelocityCore(argument));

    /// <summary>Implements LinearVelocity for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.LinearVelocity"/>.</remarks>
    protected abstract Vector2 GetLinearVelocityCore();
    internal Vector2 ReadLinearVelocity() => Read(static owner => Result(owner.GetLinearVelocityCore()));

    /// <summary>Implements LinearVelocity for the current borrowed attachment.</summary>
    /// <param name="value">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.LinearVelocity"/>.</remarks>
    protected abstract void SetLinearVelocityCore(Vector2 value);
    internal void WriteLinearVelocity(Vector2 value) => Write(value, static (owner, argument) => owner.SetLinearVelocityCore(argument));

    /// <summary>Implements CenterOfMass for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.CenterOfMass"/>.</remarks>
    protected abstract Vector2 GetCenterOfMassCore();
    internal Vector2 ReadCenterOfMass() => Read(static owner => Result(owner.GetCenterOfMassCore()));

    /// <summary>Implements CenterOfMassLocal for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.CenterOfMassLocal"/>.</remarks>
    protected abstract Vector2 GetCenterOfMassLocalCore();
    internal Vector2 ReadCenterOfMassLocal() => Read(static owner => Result(owner.GetCenterOfMassLocalCore()));

    /// <summary>Implements InverseMass for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.InverseMass"/>.</remarks>
    protected abstract float GetInverseMassCore();
    internal float ReadInverseMass() => Read(static owner => Result(owner.GetInverseMassCore(), true));

    /// <summary>Implements InverseInertia for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.InverseInertia"/>.</remarks>
    protected abstract float GetInverseInertiaCore();
    internal float ReadInverseInertia() => Read(static owner => Result(owner.GetInverseInertiaCore(), true));

    /// <summary>Implements Sleeping for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.Sleeping"/>.</remarks>
    protected abstract bool IsSleepingCore();
    internal bool ReadSleeping() => Read(static owner => owner.IsSleepingCore());

    /// <summary>Implements Sleeping for the current borrowed attachment.</summary>
    /// <param name="value">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.Sleeping"/>.</remarks>
    protected abstract void SetSleepStateCore(bool value);
    internal void WriteSleeping(bool value) => Write(value, static (owner, argument) => owner.SetSleepStateCore(argument));

    /// <summary>Implements Step for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.Step"/>.</remarks>
    protected abstract float GetStepCore();
    internal float ReadStep() => Read(static owner => Result(owner.GetStepCore(), true));

    /// <summary>Implements TotalGravity for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.TotalGravity"/>.</remarks>
    protected abstract Vector2 GetTotalGravityCore();
    internal Vector2 ReadTotalGravity() => Read(static owner => Result(owner.GetTotalGravityCore()));

    /// <summary>Implements TotalLinearDamp for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.TotalLinearDamp"/>.</remarks>
    protected abstract float GetTotalLinearDampCore();
    internal float ReadTotalLinearDamp() => Read(static owner => Result(owner.GetTotalLinearDampCore()));

    /// <summary>Implements TotalAngularDamp for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.TotalAngularDamp"/>.</remarks>
    protected abstract float GetTotalAngularDampCore();
    internal float ReadTotalAngularDamp() => Read(static owner => Result(owner.GetTotalAngularDampCore()));

    /// <summary>Implements CollisionLayer for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.CollisionLayer"/>.</remarks>
    protected abstract uint GetCollisionLayerCore();
    internal uint ReadCollisionLayer() => Read(static owner => owner.GetCollisionLayerCore());

    /// <summary>Implements CollisionLayer for the current borrowed attachment.</summary>
    /// <param name="value">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.CollisionLayer"/>.</remarks>
    protected abstract void SetCollisionLayerCore(uint value);
    internal void WriteCollisionLayer(uint value) => Write(value, static (owner, argument) => owner.SetCollisionLayerCore(argument));

    /// <summary>Implements CollisionMask for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.CollisionMask"/>.</remarks>
    protected abstract uint GetCollisionMaskCore();
    internal uint ReadCollisionMask() => Read(static owner => owner.GetCollisionMaskCore());

    /// <summary>Implements CollisionMask for the current borrowed attachment.</summary>
    /// <param name="value">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.CollisionMask"/>.</remarks>
    protected abstract void SetCollisionMaskCore(uint value);
    internal void WriteCollisionMask(uint value) => Write(value, static (owner, argument) => owner.SetCollisionMaskCore(argument));

    /// <summary>Implements Transform for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.Transform"/>.</remarks>
    protected abstract Transform GetTransformCore();
    internal Transform ReadTransform() => Read(static owner => Result(owner.GetTransformCore()));

    /// <summary>Implements Transform for the current borrowed attachment.</summary>
    /// <param name="value">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.Transform"/>.</remarks>
    protected abstract void SetTransformCore(Transform value);
    internal void WriteTransform(Transform value) => Write(value, static (owner, argument) => owner.SetTransformCore(argument));

    /// <summary>Implements GetConstantForce for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.GetConstantForce"/>.</remarks>
    protected abstract Vector2 GetConstantForceCore();
    internal Vector2 InvokeGetConstantForce() => Read(static owner => Result(owner.GetConstantForceCore()));

    /// <summary>Implements GetConstantTorque for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.GetConstantTorque"/>.</remarks>
    protected abstract float GetConstantTorqueCore();
    internal float InvokeGetConstantTorque() => Read(static owner => Result(owner.GetConstantTorqueCore()));

    /// <summary>Implements GetContactCount for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.GetContactCount"/>.</remarks>
    protected abstract int GetContactCountCore();
    internal int InvokeGetContactCount() => Read(static owner => owner.ResultContactCount(owner.GetContactCountCore()));

    /// <summary>Implements GetSpaceState for the current borrowed attachment.</summary>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.GetSpaceState"/>.</remarks>
    protected abstract PhysicsDirectSpaceState GetSpaceStateCore();
    internal PhysicsDirectSpaceState InvokeGetSpaceState() => Read(static owner => owner.ResultSpace(owner.GetSpaceStateCore()));

    /// <summary>Implements GetVelocityAtLocalPosition for the current borrowed attachment.</summary>
    /// <param name="localPosition">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <returns>The current engine-unit value described by the inherited operation.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.GetVelocityAtLocalPosition"/>.</remarks>
    protected abstract Vector2 GetVelocityAtLocalPositionCore(Vector2 localPosition);
    internal Vector2 InvokeGetVelocityAtLocalPosition(Vector2 localPosition) => Read(localPosition, static (owner, argument) => Result(owner.GetVelocityAtLocalPositionCore(argument)));

    /// <summary>Implements SetConstantForce for the current borrowed attachment.</summary>
    /// <param name="force">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.SetConstantForce"/>.</remarks>
    protected abstract void SetConstantForceCore(Vector2 force);
    internal void InvokeSetConstantForce(Vector2 force) => Write(force, static (owner, argument) => owner.SetConstantForceCore(argument));

    /// <summary>Implements SetConstantTorque for the current borrowed attachment.</summary>
    /// <param name="torque">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.SetConstantTorque"/>.</remarks>
    protected abstract void SetConstantTorqueCore(float torque);
    internal void InvokeSetConstantTorque(float torque) => Write(torque, static (owner, argument) => owner.SetConstantTorqueCore(argument));

    /// <summary>Implements AddConstantCentralForce for the current borrowed attachment.</summary>
    /// <param name="force">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.AddConstantCentralForce"/>.</remarks>
    protected abstract void AddConstantCentralForceCore(Vector2 force);
    internal void InvokeAddConstantCentralForce(Vector2 force) => Write(force, static (owner, argument) => owner.AddConstantCentralForceCore(argument));

    /// <summary>Implements AddConstantTorque for the current borrowed attachment.</summary>
    /// <param name="torque">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.AddConstantTorque"/>.</remarks>
    protected abstract void AddConstantTorqueCore(float torque);
    internal void InvokeAddConstantTorque(float torque) => Write(torque, static (owner, argument) => owner.AddConstantTorqueCore(argument));

    /// <summary>Implements AddConstantForce for the current borrowed attachment.</summary>
    /// <param name="force">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <param name="position">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.AddConstantForce"/>.</remarks>
    protected abstract void AddConstantForceCore(Vector2 force, Vector2 position);
    internal void InvokeAddConstantForce(Vector2 force, Vector2 position) => Write((force, position), static (owner, argument) => owner.AddConstantForceCore(argument.Item1, argument.Item2));

    /// <summary>Implements ApplyCentralForce for the current borrowed attachment.</summary>
    /// <param name="force">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.ApplyCentralForce"/>.</remarks>
    protected abstract void ApplyCentralForceCore(Vector2 force);
    internal void InvokeApplyCentralForce(Vector2 force) => Write(force, static (owner, argument) => owner.ApplyCentralForceCore(argument));

    /// <summary>Implements ApplyCentralImpulse for the current borrowed attachment.</summary>
    /// <param name="impulse">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.ApplyCentralImpulse"/>.</remarks>
    protected abstract void ApplyCentralImpulseCore(Vector2 impulse);
    internal void InvokeApplyCentralImpulse(Vector2 impulse) => Write(impulse, static (owner, argument) => owner.ApplyCentralImpulseCore(argument));

    /// <summary>Implements ApplyForce for the current borrowed attachment.</summary>
    /// <param name="force">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <param name="position">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.ApplyForce"/>.</remarks>
    protected abstract void ApplyForceCore(Vector2 force, Vector2 position);
    internal void InvokeApplyForce(Vector2 force, Vector2 position) => Write((force, position), static (owner, argument) => owner.ApplyForceCore(argument.Item1, argument.Item2));

    /// <summary>Implements ApplyImpulse for the current borrowed attachment.</summary>
    /// <param name="impulse">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <param name="position">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.ApplyImpulse"/>.</remarks>
    protected abstract void ApplyImpulseCore(Vector2 impulse, Vector2 position);
    internal void InvokeApplyImpulse(Vector2 impulse, Vector2 position) => Write((impulse, position), static (owner, argument) => owner.ApplyImpulseCore(argument.Item1, argument.Item2));

    /// <summary>Implements ApplyTorque for the current borrowed attachment.</summary>
    /// <param name="torque">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.ApplyTorque"/>.</remarks>
    protected abstract void ApplyTorqueCore(float torque);
    internal void InvokeApplyTorque(float torque) => Write(torque, static (owner, argument) => owner.ApplyTorqueCore(argument));

    /// <summary>Implements ApplyTorqueImpulse for the current borrowed attachment.</summary>
    /// <param name="impulse">Validated engine-unit value; matches the inherited operation contract.</param>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.ApplyTorqueImpulse"/>.</remarks>
    protected abstract void ApplyTorqueImpulseCore(float impulse);
    internal void InvokeApplyTorqueImpulse(float impulse) => Write(impulse, static (owner, argument) => owner.ApplyTorqueImpulseCore(argument));

    /// <summary>Implements IntegrateForces for the current borrowed attachment.</summary>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.IntegrateForces"/>.</remarks>
    protected abstract void IntegrateForcesCore();
    internal void InvokeIntegrateForces() => Write(0, static (owner, argument) => owner.IntegrateForcesCore());

    /// <summary>Implements contacts for the current borrowed attachment.</summary>
    /// <param name="contactIndex">Zero-based index in the completed contact snapshot.</param>
    /// <returns>An immutable contact captured with live validated identities; retain it across later edits.</returns>
    /// <remarks>Called on the world owner outside solving, with generation/lifetime guards and a borrowed hook context.
    /// The inherited operation validates input/output; implementation errors unwind context in finally. See <see cref="PhysicsDirectBodyState.GetContactCollider(int)"/>.</remarks>
    protected abstract PhysicsBodyContact GetContactCore(int contactIndex);

}
