# GPUPhysicsColliderImplementation

Last updated: 2026-10-14

**Declaration:** `internal sealed partial class GPUPhysicsColliderImplementation : PhysicsColliderImplementation` · **Source:** [GPUPhysicsColliderImplementation.cs](../../src/Servers/Physics/GPUPhysicsColliderImplementation.cs) · **Component:** [Physics backends](../components/physics-backends.md#runtime-flow-and-ownership)

An explicitly requested compact pose has an independent epoch-qualified cache. It can satisfy scalar pose access without treating velocity, fields or sleep as current. Local replay captures/restores this cache; portable motion application invalidates it. Scene presentation remains owned by the scene transform.

## Responsibility and flow

The one RebuildShapes span consumes neutral PhysicsColliderShape values projected
from all scene/server bodies and Areas. Disabled/disposed slots retain their
logical indices without producing fixtures. Complete source preflight occurs
before mutation; this owner retains concrete geometry validation and storage.
See [shared inputs](../components/physics-backends.md#shared-collider-shape-inputs).

The actual resident store retains this concrete attachment by common collider
identity. Its immutable store reference supplies device operations even when the
selected implementation is a wrapper. Native handles, shape leases and publication
caches are absent from the common facade. Detach retires only its own table entry;
a delayed old detach preserves a replacement. RefreshIdentity and
SetCollisionPriority publish live shared edits to resident query/solver records.

Owns one resident body handle, shape handles/query leases, sensor/mask policy, constant/surface metadata and epoch-qualified state/publication/replay caches. Implements all engine-unit collider operations through the independent store. Existing query projection and common frame publication can address its retained identity facade; no CPU body/world cache is created. Detach attempts every shape lease, resident removal and common lookup retirement, then clears handles and aggregates failures.

Common resource RID, object/canvas association, collision priority and monotonically increasing AttachmentVersion stay in [PhysicsColliderBackend](PhysicsColliderBackend.md). Library guards retain owner-thread, solver and failed-world access rules. Factory selection uses the actual implementation, including CPU startup fallback; native handles never become public resource identity.

Joint local-frame sampling now uses this same selected body attachment. CPU keeps native local-point/rotation math without a decoded-angle round trip; GPU samples its current typed pose and validates the existing anchor extent.

## Lifecycle and verification

A failed attachment attempts cleanup and leaves the common facade detached. Retiring a concrete implementation clears its handles; a disposed view cannot retarget to a new attachment. [PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks invalid creation followed by reuse, four body/Area world transfers, native retirement, stable physical/object IDs, current motion/mass/force and query geometry. Existing shared physics, GPU publication, private/portable checkpoint and network checks establish separate behavior limits.

## Limits and decisions

Only internal CPU/GPU implementations are currently constructed. Public backend registration, server/direct-state extension contexts and custom geometry remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103); this type is not a public plugin API. Native allocator, foreign-platform and real-window FPS acceptance remain separate gates.

World-point platform velocity now dispatches through the current selected attachment. Common lookup uses the shared scene/raw RID registry and checks current world ownership, including transfer/detach/free. CPU keeps direct world-point native conversion; GPU keeps resident center-aware surface motion. See [motion ownership and checks](PhysicsWorldBackend.md#body-motion-and-platform-lookup).

## Selected integration preparation

[Integration preparation](../../src/Servers/Physics/GPUPhysicsColliderImplementation.Integration.cs) consumes eligible pending force/torque through resident sparse edits and skips them under omission. Constants, fields and damping remain device integration policies; no additional host simulation path is introduced. [Contract and verification](../components/physics-backends.md#selected-body-integration-preparation).

Explicit sleep first synchronizes pending authored integration/constant policies
and flushes their older wake work, then writes the sleep command. A later authored
change can still wake the receiver; an older deferred policy update cannot override
the explicit sleep. The shared scene/raw ownership fixture exercises this order.

## Procedural resource geometry

Runtime geometry decisions now use ShapeGeometry's represented primitive rather than concrete resource classes. Caller-defined Shape resources retain the same full-contour, infinite-boundary, directed-ray and zero-geometric-mass policies. Compound CPU contour metadata borrows any Shape with a large convex payload. Scene/raw geometry construction is preflighted before retiring existing CPU/GPU fixtures, so a failing protected hook leaves preparation retryable. See [procedural shape verification](../components/physics-shapes.md#caller-defined-shape-resources).

## Selected fields, exceptions and portable observation

Resolved fields and exception invalidation dispatch through the selected collider/world; common server/replay/map code retains no GPU handle/state aliases. Portable preparation and bound-body observation use the selected owner and borrowed common inputs. GPU pair/batch scratch stays private; CPU preserves existing field/fixture preparation. See [the executing contract and limits](../components/physics-backends.md#selected-observation-and-collision-policy).

## Selected shape-pair operations

The internal resource/resource and body-slot/resource bridge now executes through the retained implementation with complete logical indices, composed global poses and validated nested caller-span results. Detached bodies use the same exact resource kernel without a solver; borrowed collider release is preflighted before registry cleanup. GPU scalar pose demand gathers sixteen pose bytes plus eight status bytes instead of complete motion/field/sleep state when no current cache exists. [The pair contract and checks](../components/physics-shape-pairs.md) distinguish this internal prerequisite from the pending complete public server-extension family.

## Value-only integration preparation

[Force and policy inputs](../components/physics-backends.md#value-only-body-integration-inputs) now separate engine-owned authoring/command state from numerical preparation. Default forces use PhysicsBodyForceConfiguration; CPU fields and GPU parameters use PhysicsBodyIntegrationConfiguration. Pending command retirement and GPU dirty flags stay in the binding layer; numerical methods receive finite authored values and return complete field/preparation results. CPU/GPU/no-device, replay, parameter and full operation checks remain the acceptance boundaries; public registration stays open.

[Custom collision geometry](../components/physics-custom-geometry.md) now provides copied canonical data, exact mass/bounds, diagnostic segments and standalone pair callbacks to registered implementations. Built-in compilation rejects unsupported custom geometry before mutation. Portable schema covers the custom authoring payload; detached force preparation resolves exact custom mass while automatic getters retain cached-resolution semantics.

Scene field eligibility survives temporary static/frozen solver roles through a retained integration-policy bit. Numerical Area/default reduction stays on the device; public scene/direct-state fields and zero-allocation full frames are checked by CollisionDisableModeTests on explicit CPU/GPU worlds. See [resident body field eligibility](../components/gpu-resident-bodies.md#storage-transfers-and-waits).

Prepared integration now retains the ResolveStaticFields flag in its concrete
GPU attachment. An unchanged step compares that local flag instead of reading a
validated store policy for every collider. Fresh attachment, mode/lock/gravity
edits and local replay still invalidate preparation; dirty/forced requests always
prepare. Only successful ordinary preparation records the flag. This is one
host metadata boolean per attachment, not a simulation-state mirror or readback.
PhysicsHostPreparationTests retains the CPU/GPU edit, freeze/role, checkpoint,
reentry, CCD and callback assertions and now directly checks changed static-field
demand plus dirty/forced preparation.

## Authored sensor pose publication (2026-10-14)

Ordinary mixed-world completion no longer requests complete GPU snapshots for
scene Areas. Sensor poses are authored static inputs; their collider reuses the
existing pose cache across numerical epochs. Initial scene attachment and explicit
pose writes populate it. Selected device reads and restored snapshots replace its
values. Body caches retain the existing epoch policy, and explicit portable capture
still reads complete sensor state when needed. No new host solver or pose array is
introduced; resident geometry remains authoritative for actual sensing and queries.

PhysicsGPUDemandPublicationTests exercises 64 scene sensors, scene/raw bodies and
a raw sensor through 128 warm/128 measured edited full frames at strict 0/0 owner/
all-thread managed bytes. The completed observer readback drops by exactly 4,096
bytes per step, equal to 64 unused 64-byte sensor records. Public pose reads, moved/
rotated scene and raw sensing, raw detach/reattach, resident point queries, local
checkpoint and portable Area restoration retain identities and avoid duplicate
user effects. Serial/separate numerical profiles retain these same operations.
The fixture reports full-frame p50/p95/p99, uploads, readback, mean wait and submission
count. Its CPU-only Vulkan values are transfer/function evidence, not hardware
speedup, native allocator totals, real-window FPS or final performance acceptance.

## Compact callback activity (2026-10-14)

AcceptGPUActivity retains a separate active flag and epoch; IsAwake can use it without reading a full Snapshot. Full-state invalidation also invalidates local activity, including explicit sleep writes without a space epoch change. AcceptGPUState refreshes activity from solved sleep. ReplayState captures/restores Active, ActivityValid and ActivityEpoch alongside existing pose/full-state caches.


[Mixed quiet observer publication](../components/gpu-body-publication.md#mixed-quiet-scene-observers-2026-10-14)
selects current incremental or complete state by estimated transfer cost. Cached
mode/sleep is a hint only; scalar values and wake/private/portable continuation
retain authoritative device reads. Existing API and coverage states remain unchanged;
hardware, native, foreign and full acceptance remain open.
