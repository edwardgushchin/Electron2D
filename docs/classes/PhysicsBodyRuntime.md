# PhysicsBodyRuntime

Last updated: 2026-10-09

**Declaration:** `internal sealed partial class PhysicsBodyRuntime`

**Source:** [PhysicsBodyRuntime.cs](../../src/Servers/Physics/PhysicsBodyRuntime.cs),
[forces](../../src/Servers/Physics/PhysicsBodyRuntime.Forces.cs),
[parameters](../../src/Servers/Physics/PhysicsBodyRuntime.Parameters.cs),
[direct-view adapter](../../src/Servers/Physics/PhysicsBodyRuntime.View.cs).
**Component:** [Physics server and queries](../components/physics-queries.md).

## Ownership and flow

PhysicsServer keeps one runtime per body RID. A scene owner is weakly referenced;
a server collider is retained until RID release. Owners resolve to the current
attachment. The runtime retains an attachment adapter and its monotonically increasing version
for each cached direct view. It validates the current view, world and attachment
version and clears the cached adapter on detach. It contains no vendor body/world
IDs, mass records or contact traversal. Reattachment on the same RID
and space cannot revive an old view.
The runtime shares mass/material/field profiles, pending and constant forces,
contact limits, callbacks and live state between scene and server operations.
It is an internal implementation; applications use PhysicsServer, PhysicsBody
subclasses and PhysicsDirectBodyState.

## Internal operations

| Operation/state | Contract |
| --- | --- |
| `Owners`, `Space`, `Backend` | Resolve the live scene/server owner and its attachment adapter. |
| `GetTransform`/`SetTransform`, velocity/sleep pairs, `SetAxisVelocity`, `RestoreSceneState` | Shared typed state, static-support wakeup and role-specific retained configuration. |
| `EnsureMutable`, `GetView`, `InvalidateView`, `ValidateView` | Enforce world phase/current attachment identity; replace a disposed view and invalidate all older attachments. |
| `View*`, `CaptureViewContacts` | Forward live state/contact capture through the validated attachment adapter; retain no concrete solver references. |
| `ApplyMassProfile`, `SetMassProfile`, `SetAttachedMassProfile`, `MassProperties` | Share profile validation and scene projection; retain mass, inertia and center in scene units. Detached calls reuse PhysicsMass.Geometry; attached compilation belongs to the adapter. |
| `ApplyBeforeStep`, pending/constant force and torque | Consume eligible pending forces once; preserve configured totals. |
| `ApplyResolvedFields`, parameter operations | Combine world/Area gravity and damping with scene or server authored body policy through one finite-validated update path. |
| `ApplyImpulse`, `AddForce`, velocity/mass access | Preserve dynamic-role, lock, finite-value and detached-state behavior. |

Virtual surface velocity is applied by PhysicsColliderBackend and preserves the
existing scene/server stored values, role policy and wake behavior. It does not
contribute to pose integration or actual kinematic subdivision distance.

Resolved gravity and damping live once in this runtime for both RigidBody and raw
server bodies. RigidBody retains authored settings; its GetGravity and direct-state
field views read these resolved values. Custom integration skips automatic field
forces but still observes changed fields and wakes an eligible dynamic body.
Frozen, disabled-static and inactive physical-bone roles retain their motion gates.
Validation completes before publishing field values or applying motion changes.

## Verification

PhysicsBodyStateTests covers current-view identity, repeated same-space scene/server
reattachment, caller-disposed view replacement, stale writes, callbacks and warmed
allocation. PhysicsParallelTests exercises contact publication for 288 bodies over
256 fixed steps with retained workers. The state/surface/contact suites also pass
through the existing GPU stage host; this is not independent GPU acceptance.

PhysicsSurfaceVelocityTests covers surface contacts, query snapshots, character
carry, inherited target motion, owner/phase/lifecycle rejection, packing and warmed
allocation. Existing mass/force/field/direct-state suites cover the other runtime
profiles through their public APIs. Native allocation and foreign-device acceptance
remain separate from the checked Linux/.NET 10/Vulkan behavior.

The [typed state partial](../../src/Servers/Physics/PhysicsBodyRuntime.State.cs)
shares transform, linear/angular velocity, sleep, automatic sleep policy and axis
projection across scene/server owners and direct views. Non-rigid scene contact
velocity is distinct from character movement input and restored at attachment.
PhysicsServerStateTests covers CPU/GPU behavior and warmed allocation.

ContactLimit accepts zero through 4095 for both scene and raw owners. The common
limit constant keeps their validation aligned; reconfiguration clears retained point
counts before the next solve. Selected object/shape monitoring follows direct-state
point selection. PhysicsContactImpulseTests covers boundary and replacement behavior.

Queued callback entries also use adapter identity plus attachment version, so
removal/reentry on the same RID and world cannot consume the prior attachment's
callback. PhysicsBodyStateTests explicitly replaces both a later scene body and a
raw server body during an earlier callback, then verifies only their next-frame
callbacks execute and both older direct views stay invalid. World stepping,
geometry/query adapters and independent GPU binding remain separate open work.
