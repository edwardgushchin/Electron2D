# PhysicsDirectBodyStateExtension

Last updated: 2026-10-10

**Declaration:** `public abstract partial class PhysicsDirectBodyStateExtension : PhysicsDirectBodyState` · **Inherits:** [PhysicsDirectBodyState](PhysicsDirectBodyState.md) · **Component:** [Body-state extensions](../components/physics-body-extensions.md)

**Source:** [bound context](../../src/Servers/Physics/PhysicsDirectBodyStateExtension.cs), [complete hooks](../../src/Servers/Physics/PhysicsDirectBodyStateExtension.Hooks.cs)

## Description and example

Implements the complete live state/force/contact/space operation family for one current scene/raw body attachment. The protected constructor binds an engine body RID, prepares its selected attachment and samples its generation. It owns no body/world and does not replace the server's cached built-in view. Detach/reentry/transfer/free permanently retire this context; cached built-in view disposal alone does not.

The [complete public-only implementation](../../tests/PhysicsBodyExtension.Consumer/ControlledState.cs) supplies every required hook and changes a real impulse response. With its current body RID in scope:

```csharp
using PhysicsDirectBodyState state = new ControlledState(body);
state.ApplyCentralImpulse(new Vector2(20, 0));
Vector2 actual = state.LinearVelocity;
```

This fixture doubles that impulse. Its getter/force/contact hooks remain attached to real public physics state rather than inert stored values.

## API summary

| Declaration | Contract |
| --- | --- |
| `protected PhysicsDirectBodyStateExtension(RID body)` | Bind current live attached scene/raw body; Area, detached or invalid RID rejects. |
| `protected sealed override void ValidateDisposal()` | Reject borrowed-view disposal, preserving the library guard for subclasses. |
| `protected abstract float GetAngularVelocityCore()` | Implements the inherited AngularVelocity operation in the current borrowed context. |
| `protected abstract void SetAngularVelocityCore(float value)` | Implements the inherited AngularVelocity operation in the current borrowed context. |
| `protected abstract Vector2 GetLinearVelocityCore()` | Implements the inherited LinearVelocity operation in the current borrowed context. |
| `protected abstract void SetLinearVelocityCore(Vector2 value)` | Implements the inherited LinearVelocity operation in the current borrowed context. |
| `protected abstract Vector2 GetCenterOfMassCore()` | Implements the inherited CenterOfMass operation in the current borrowed context. |
| `protected abstract Vector2 GetCenterOfMassLocalCore()` | Implements the inherited CenterOfMassLocal operation in the current borrowed context. |
| `protected abstract float GetInverseMassCore()` | Implements the inherited InverseMass operation in the current borrowed context. |
| `protected abstract float GetInverseInertiaCore()` | Implements the inherited InverseInertia operation in the current borrowed context. |
| `protected abstract bool IsSleepingCore()` | Implements the inherited Sleeping operation in the current borrowed context. |
| `protected abstract void SetSleepStateCore(bool value)` | Implements the inherited Sleeping operation in the current borrowed context. |
| `protected abstract float GetStepCore()` | Implements the inherited Step operation in the current borrowed context. |
| `protected abstract Vector2 GetTotalGravityCore()` | Implements the inherited TotalGravity operation in the current borrowed context. |
| `protected abstract float GetTotalLinearDampCore()` | Implements the inherited TotalLinearDamp operation in the current borrowed context. |
| `protected abstract float GetTotalAngularDampCore()` | Implements the inherited TotalAngularDamp operation in the current borrowed context. |
| `protected abstract uint GetCollisionLayerCore()` | Implements the inherited CollisionLayer operation in the current borrowed context. |
| `protected abstract void SetCollisionLayerCore(uint value)` | Implements the inherited CollisionLayer operation in the current borrowed context. |
| `protected abstract uint GetCollisionMaskCore()` | Implements the inherited CollisionMask operation in the current borrowed context. |
| `protected abstract void SetCollisionMaskCore(uint value)` | Implements the inherited CollisionMask operation in the current borrowed context. |
| `protected abstract Transform GetTransformCore()` | Implements the inherited Transform operation in the current borrowed context. |
| `protected abstract void SetTransformCore(Transform value)` | Implements the inherited Transform operation in the current borrowed context. |
| `protected abstract Vector2 GetConstantForceCore()` | Implements the inherited GetConstantForce operation in the current borrowed context. |
| `protected abstract float GetConstantTorqueCore()` | Implements the inherited GetConstantTorque operation in the current borrowed context. |
| `protected abstract int GetContactCountCore()` | Implements the inherited GetContactCount operation in the current borrowed context. |
| `protected abstract PhysicsDirectSpaceState GetSpaceStateCore()` | Implements the inherited GetSpaceState operation in the current borrowed context. |
| `protected abstract Vector2 GetVelocityAtLocalPositionCore(Vector2 localPosition)` | Implements the inherited GetVelocityAtLocalPosition operation in the current borrowed context. |
| `protected abstract void SetConstantForceCore(Vector2 force)` | Implements the inherited SetConstantForce operation in the current borrowed context. |
| `protected abstract void SetConstantTorqueCore(float torque)` | Implements the inherited SetConstantTorque operation in the current borrowed context. |
| `protected abstract void AddConstantCentralForceCore(Vector2 force)` | Implements the inherited AddConstantCentralForce operation in the current borrowed context. |
| `protected abstract void AddConstantTorqueCore(float torque)` | Implements the inherited AddConstantTorque operation in the current borrowed context. |
| `protected abstract void AddConstantForceCore(Vector2 force, Vector2 position)` | Implements the inherited AddConstantForce operation in the current borrowed context. |
| `protected abstract void ApplyCentralForceCore(Vector2 force)` | Implements the inherited ApplyCentralForce operation in the current borrowed context. |
| `protected abstract void ApplyCentralImpulseCore(Vector2 impulse)` | Implements the inherited ApplyCentralImpulse operation in the current borrowed context. |
| `protected abstract void ApplyForceCore(Vector2 force, Vector2 position)` | Implements the inherited ApplyForce operation in the current borrowed context. |
| `protected abstract void ApplyImpulseCore(Vector2 impulse, Vector2 position)` | Implements the inherited ApplyImpulse operation in the current borrowed context. |
| `protected abstract void ApplyTorqueCore(float torque)` | Implements the inherited ApplyTorque operation in the current borrowed context. |
| `protected abstract void ApplyTorqueImpulseCore(float impulse)` | Implements the inherited ApplyTorqueImpulse operation in the current borrowed context. |
| `protected abstract void IntegrateForcesCore()` | Implements the inherited IntegrateForces operation in the current borrowed context. |
| `protected abstract PhysicsBodyContact GetContactCore(int contactIndex)` | Immutable complete engine-validated contact snapshot at the requested completed index. |

## Constructor, hook and lifetime contracts

All inherited public properties and force/impulse/contact/space methods reach these hooks after normal access/input validation. Getters return finite engine-unit values; inverse mass/inertia and step are nonnegative, transforms are rigid, signed damping remains permitted, and contact count is bounded by the body's authored report limit. GetSpaceStateCore must return a live matching-world direct-space view. Setters receive the same finite/unit-scale input and coordinate/unit semantics as the inherited operations. Nonfinite input rejects before user code; invalid hook output rejects before returning to the caller.

GetContactCore returns one [PhysicsBodyContact](PhysicsBodyContact.md) carrying every retained contact field and weak sampled association. The library validates contact index/count, observed body identity and attachment generation before projection. The complete contact getter family preserves stored values/indices/IDs across later collider edits/free, and weak target access becomes null after expiry. It does not resample the historical contact association as a current query would.

Nested hooks are allowed. Finally balances borrowed body/world/view context after successful operations and errors. While borrowed, body/world release or transfer, scene hierarchy/disposal, world binding, active recursive steps and checkpoint capture/restore reject before mutation. Ordinary live velocity/pose/force/filter writes remain available. The sealed disposal guard cannot be bypassed by overriding ValidateDisposal.

Hook implementations own numerical behavior, completed contact selection/full-step impulses and any internal replay state; library guards do not calculate custom physics. Caller-created view dispatch allocates no warmed managed memory at unchanged capacity. Construction, first static delegate initialization, user callbacks and structural edits may allocate.

## Protected method descriptions

### GetAngularVelocityCore

`protected abstract float GetAngularVelocityCore()`

Implements AngularVelocity with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### SetAngularVelocityCore

`protected abstract void SetAngularVelocityCore(float value)`

Implements AngularVelocity with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetLinearVelocityCore

`protected abstract Vector2 GetLinearVelocityCore()`

Implements LinearVelocity with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### SetLinearVelocityCore

`protected abstract void SetLinearVelocityCore(Vector2 value)`

Implements LinearVelocity with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetCenterOfMassCore

`protected abstract Vector2 GetCenterOfMassCore()`

Implements CenterOfMass with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetCenterOfMassLocalCore

`protected abstract Vector2 GetCenterOfMassLocalCore()`

Implements CenterOfMassLocal with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetInverseMassCore

`protected abstract float GetInverseMassCore()`

Implements InverseMass with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetInverseInertiaCore

`protected abstract float GetInverseInertiaCore()`

Implements InverseInertia with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### IsSleepingCore

`protected abstract bool IsSleepingCore()`

Implements Sleeping with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### SetSleepStateCore

`protected abstract void SetSleepStateCore(bool value)`

Implements Sleeping with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetStepCore

`protected abstract float GetStepCore()`

Implements Step with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetTotalGravityCore

`protected abstract Vector2 GetTotalGravityCore()`

Implements TotalGravity with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetTotalLinearDampCore

`protected abstract float GetTotalLinearDampCore()`

Implements TotalLinearDamp with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetTotalAngularDampCore

`protected abstract float GetTotalAngularDampCore()`

Implements TotalAngularDamp with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetCollisionLayerCore

`protected abstract uint GetCollisionLayerCore()`

Implements CollisionLayer with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### SetCollisionLayerCore

`protected abstract void SetCollisionLayerCore(uint value)`

Implements CollisionLayer with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetCollisionMaskCore

`protected abstract uint GetCollisionMaskCore()`

Implements CollisionMask with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### SetCollisionMaskCore

`protected abstract void SetCollisionMaskCore(uint value)`

Implements CollisionMask with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetTransformCore

`protected abstract Transform GetTransformCore()`

Implements Transform with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### SetTransformCore

`protected abstract void SetTransformCore(Transform value)`

Implements Transform with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetConstantForceCore

`protected abstract Vector2 GetConstantForceCore()`

Implements GetConstantForce with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetConstantTorqueCore

`protected abstract float GetConstantTorqueCore()`

Implements GetConstantTorque with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetContactCountCore

`protected abstract int GetContactCountCore()`

Implements GetContactCount with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetSpaceStateCore

`protected abstract PhysicsDirectSpaceState GetSpaceStateCore()`

Implements GetSpaceState with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetVelocityAtLocalPositionCore

`protected abstract Vector2 GetVelocityAtLocalPositionCore(Vector2 localPosition)`

Implements GetVelocityAtLocalPosition with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### SetConstantForceCore

`protected abstract void SetConstantForceCore(Vector2 force)`

Implements SetConstantForce with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### SetConstantTorqueCore

`protected abstract void SetConstantTorqueCore(float torque)`

Implements SetConstantTorque with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### AddConstantCentralForceCore

`protected abstract void AddConstantCentralForceCore(Vector2 force)`

Implements AddConstantCentralForce with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### AddConstantTorqueCore

`protected abstract void AddConstantTorqueCore(float torque)`

Implements AddConstantTorque with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### AddConstantForceCore

`protected abstract void AddConstantForceCore(Vector2 force, Vector2 position)`

Implements AddConstantForce with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### ApplyCentralForceCore

`protected abstract void ApplyCentralForceCore(Vector2 force)`

Implements ApplyCentralForce with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### ApplyCentralImpulseCore

`protected abstract void ApplyCentralImpulseCore(Vector2 impulse)`

Implements ApplyCentralImpulse with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### ApplyForceCore

`protected abstract void ApplyForceCore(Vector2 force, Vector2 position)`

Implements ApplyForce with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### ApplyImpulseCore

`protected abstract void ApplyImpulseCore(Vector2 impulse, Vector2 position)`

Implements ApplyImpulse with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### ApplyTorqueCore

`protected abstract void ApplyTorqueCore(float torque)`

Implements ApplyTorque with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### ApplyTorqueImpulseCore

`protected abstract void ApplyTorqueImpulseCore(float impulse)`

Implements ApplyTorqueImpulse with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### IntegrateForcesCore

`protected abstract void IntegrateForcesCore()`

Implements IntegrateForces with the full coordinate, unit, wake/sleep, force or field contract documented on [PhysicsDirectBodyState](PhysicsDirectBodyState.md). Input arguments are validated engine values; a return value is the current operation result. Execute synchronously on the owner outside solving, respecting the borrowed lifetime rules above.

### GetContactCore

`protected abstract PhysicsBodyContact GetContactCore(int contactIndex)`

Return the complete immutable captured contact at the supplied zero-based index. Its body/generation must match this view; collider identity may have expired since capture. The snapshot carries global positions/normal/velocities and full-step impulse in scene units.

## Verification and limits

The separate consumer exercises every hook through actual inherited API on CPU/GPU and CPU without display/GPU access. It verifies real force/contact/motion, .001 physical velocity/force tolerances, exact identities/snapshot values, invalid input/output, nesting/failure, release/binding/hierarchy/checkpoint/step guards, cached-view disposal, off-owner access, raw/scene transfer/reentry/free and previous-generation contacts. Full unchanged operation cycles warm for at least one second before 64 measured cycles with strict zero owner/all-thread managed bytes.

Registered backend factories do not yet provide these views to scene/server integration callbacks; class coverage stays Partial. Custom Shape and full server backend registration/operation remain open. Native allocation, foreign-platform and new full-step/window-performance acceptance are separate gates under [ADR 0103](../decisions/physics-extensions.md#adr-0103).
