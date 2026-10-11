# PhysicsColliderImplementation

Last updated: 2026-10-11

**Declaration:** `internal abstract class PhysicsColliderImplementation` · **Source:** [PhysicsColliderImplementation.cs](../../src/Servers/Physics/PhysicsColliderImplementation.cs) · **Component:** [Physics backends](../components/physics-backends.md#runtime-flow-and-ownership)

## Responsibility and flow

RebuildShapes receives one borrowed ReadOnlySpan of
[PhysicsColliderShape](PhysicsColliderShape.md) inputs for scene/raw bodies and
Areas. Logical disabled slots and exact resource/transform/one-way data survive
projection; private scene/server slot stores never cross this implementation
boundary. Native preparation still preflights before fixture removal.

RefreshIdentity and SetCollisionPriority receive shared object/canvas and priority
edits through the selected implementation. GPU publishes resident query/solver
metadata; CPU reads shared identity and priority directly. Common collider code
has no concrete CPU/GPU casts or native handle aliases.

PrepareContactPolicy belongs to the selected implementation. CPU refreshes current
native shape policy; GPU keeps its policy publication in geometry rebuilding.
The common facade has no CPU ID/shape aliases or concrete CPU cast. The wrapped
CPU fixture exercises actual policy/impulse hooks, Areas, CCD and portable state.

The selected world creates this engine-unit operation boundary for one current collider attachment. The owner identity facade and physics space are captured once; reattachment constructs another implementation. It dispatches body/Area creation and release, shape rebuild/filtering, pose/motion/sleep/lock/force/mass/surface/contact operations and portable pose/motion/piece validation. CPU/GPU-specific diagnostic and checkpoint facets remain concrete internal operations.

Common resource RID, object/canvas association, collision priority and monotonically increasing AttachmentVersion stay in [PhysicsColliderBackend](PhysicsColliderBackend.md). Library guards retain owner-thread, solver and failed-world access rules. Factory selection uses the actual implementation, including CPU startup fallback; native handles never become public resource identity.

Joint local-frame sampling now uses this same selected body attachment. CPU keeps native local-point/rotation math without a decoded-angle round trip; GPU samples its current typed pose and validates the existing anchor extent.

## Lifecycle and verification

A failed attachment attempts cleanup and leaves the common facade detached. Retiring a concrete implementation clears its handles; a disposed view cannot retarget to a new attachment. [PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks invalid creation followed by reuse, four body/Area world transfers, native retirement, stable physical/object IDs, current motion/mass/force and query geometry. Existing shared physics, GPU publication, private/portable checkpoint and network checks establish separate behavior limits.

## Limits and decisions

Only internal CPU/GPU implementations are currently constructed. Public backend registration, server/direct-state extension contexts and custom geometry remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103); this type is not a public plugin API. Native allocator, foreign-platform and real-window FPS acceptance remain separate gates.

## Selected integration preparation

PrepareDefaultForces consumes eligible pending forces through the selected collider after common role/activity guards. CPU owns native omission/gravity and persistent-force preparation; GPU publishes eligible pending forces while resident policy handles constants/fields. [Contract and checks](../components/physics-backends.md#selected-body-integration-preparation).

## Selected fields, exceptions and portable observation

Resolved fields and exception invalidation dispatch through the selected collider/world; common server/replay/map code retains no GPU handle/state aliases. Portable preparation and bound-body observation use the selected owner and borrowed common inputs. GPU pair/batch scratch stays private; CPU preserves existing field/fixture preparation. See [the executing contract and limits](../components/physics-backends.md#selected-observation-and-collision-policy).

| Internal operation | Contract |
| --- | --- |
| `PhysicsBodyFields ReadResolvedFields(in PhysicsBodyFields current)` | Return complete current observer fields in engine units; CPU preserves its completed common values, GPU samples its qualified resident cache. Common publication validates all fields atomically. |
| `void InvalidateCollisionExceptions()` | Notify this attachment's actual world owner of changed shared exception authoring, without exposing native pair handles. |

## Value-only integration preparation

[Force and policy inputs](../components/physics-backends.md#value-only-body-integration-inputs) now separate engine-owned authoring/command state from numerical preparation. Default forces use PhysicsBodyForceConfiguration; CPU fields and GPU parameters use PhysicsBodyIntegrationConfiguration. Pending command retirement and GPU dirty flags stay in the binding layer; numerical methods receive finite authored values and return complete field/preparation results. CPU/GPU/no-device, replay, parameter and full operation checks remain the acceptance boundaries; public registration stays open.
