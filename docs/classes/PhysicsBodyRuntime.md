# PhysicsBodyRuntime

Last updated: 2026-10-08

**Declaration:** `internal sealed partial class PhysicsBodyRuntime`

**Source:** [PhysicsBodyRuntime.cs](../../src/Servers/Physics/PhysicsBodyRuntime.cs),
[forces](../../src/Servers/Physics/PhysicsBodyRuntime.Forces.cs),
[parameters](../../src/Servers/Physics/PhysicsBodyRuntime.Parameters.cs),
[surface velocity](../../src/Servers/Physics/PhysicsBodyRuntime.Surface.cs),
[direct-view adapter](../../src/Servers/Physics/PhysicsBodyRuntime.View.cs).
**Component:** [Physics server and queries](../components/physics-queries.md).

## Ownership and flow

PhysicsServer keeps one runtime per body RID. A scene owner is weakly referenced;
a server collider is retained until RID release. Owners resolve to the current
attachment. The runtime owns the cached backend world/body used by a direct view,
validates that the borrowed object is still its current view, and clears both
the view and cached backend references on detach. Reattachment on the same RID
and space cannot revive an old view.
The runtime shares mass/material/field profiles, pending and constant forces,
contact limits, callbacks and live state between scene and server operations.
It is an internal implementation; applications use PhysicsServer, PhysicsBody
subclasses and PhysicsDirectBodyState.

## Internal operations

| Operation/state | Contract |
| --- | --- |
| `Owners`, `Space`, `BodyID` | Resolve the live scene/server owner and current native attachment. |
| `GetTransform`/`SetTransform`, velocity/sleep pairs, `SetAxisVelocity`, `RestoreSceneState` | Shared typed state, static-support wakeup and role-specific retained configuration. |
| `EnsureMutable`, `GetView`, `InvalidateView`, `ValidateView` | Enforce world phase/current attachment identity; replace a disposed view and invalidate all older attachments. |
| `View*`, `CaptureViewContacts`, `CaptureViewContact` | Adapt backend live state and solved contacts to engine values without exposing backend handles to PhysicsDirectBodyState. |
| `ApplyMassProfile`, `SetMassProfile` | Share body shape/mass/center validation and scene projection. |
| `ApplyBeforeStep`, pending/constant force and torque | Consume eligible pending forces once; preserve configured totals. |
| `ApplyResolvedFields`, parameter operations | Combine world/Area gravity and damping with body policy. |
| `ApplyImpulse`, `AddForce`, velocity/mass access | Preserve dynamic-role, lock, finite-value and detached-state behavior. |
| `SetSurfaceVelocity(B2BodyId id, Vector2 linear, float angular)` | Set virtual global linear/angular contact velocity separately from integrated state and wake touching bodies. |

The surface helper rejects a locked solver. Its callers validate finite inputs
and owner access before mutation. StaticBody, AnimatableBody and raw static
colliders reuse the channel; shape rebuild and reentry preserve scene configuration.
The extra velocity affects point queries and CPU/GPU contact constraints but never
contributes to pose integration or actual kinematic subdivision distance.

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
