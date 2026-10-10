# PhysicsWorldBackend

Last updated: 2026-10-10

**Declaration:** `internal abstract class PhysicsWorldBackend : IDisposable` · **Source:** [PhysicsWorldBackend.cs](../../src/Servers/Physics/PhysicsWorldBackend.cs) · **Component:** [Physics backends](../components/physics-backends.md)

## Description and internal flow

Owns selection metadata and full interval dispatch for one internal implementation. PhysicsSpace constructs it after validating sampled world settings. Create makes a fresh CPUPhysicsWorldBackend or GPUPhysicsWorldBackend; optional startup fallback constructs an actual CPU implementation and retains the GPU request and failure message. It never changes a running implementation.

Kind, Requested and FallbackReason describe immutable selection. EnsureAccess performs implementation-specific idle/failure/lifetime checks. Step dispatches the complete CPU/GPU interval; StepNative is the CPU discrete/CCD endpoint. WorldID, Tasks and diagnostic stage controls are internal CPU-only facets and reject for a resident GPU implementation. GPUStore is present only for the resident implementation. Dispose releases owned solver resources.

IntersectRay, CollectPointHits, CollectShapeHits, CastMotion, CollectShapeContacts and GetRestInfo dispatch every direct-space query to the same selected owner. Public access guards and output projection stay in PhysicsDirectSpaceState; concrete implementations own retained query scratch. These internal operations do not yet expose the public extension context under ADR 0103.

CreateCollider constructs a fresh selected CPU/GPU PhysicsColliderImplementation for each body/Area attachment. The retained collider identity facade delegates all engine-unit body and shape operations to that concrete owner; reattachment never retargets its immutable world.

CreateJoint creates a fresh selected CPU/GPU PhysicsJointImplementation for each constraint attachment. Shared settings and physical RID stay in PhysicsJointRuntime. Generic policy/setters/portable-frame/release operations dispatch to that attachment.

## World policy, capacity and statistics

The selected owner now applies sleep/contact thresholds, solver iterations and world joint defaults. PhysicsSpace validates and retains authored values, then dispatches implementation work. CPU owns native threshold updates, waking, worker selection, sensor scratch, solver/arena capacity and native sensor/process-statistics traversal. GPU applies resident settings and epoch invalidation, prepares existing GPU publication/report scratch and reads completed store statistics; it creates no CPU motion-history mirror.

CPU capacity preparation keeps a conservative flag once any dynamic attachment can sleep. Worlds whose dynamic bodies have never enabled sleep skip repeated complete-body scans during attachment. Enabling sleep or changing a sleep-enabled static/kinematic body to a dynamic role prepares dormant capacity immediately. The flag never resets, so private checkpoint restores cannot introduce an unobserved sleeping role. Existing dormant budgets and physics semantics are retained.

[PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks typed policy, invalid values, independent worlds, CPU native settings, selected statistics, no GPU CPU-motion storage, live role changes and disposed-owner guards. Common sleep/contact/iteration/statistics, replay and network suites remain the behavioral acceptance boundary.

## Selected step phases

`PhysicsWorldBackend.Step` now enters one common `PhysicsSpace.ExecuteStep` boundary.
The selected owner executes BeginStep, world/field preparation, body/motion
preparation, Solve, SyncResults, body completion, contact collection, Area scanning
and EndStep. CPU owns its native interval/worker/one-way finalization flow; GPU owns
resident submissions, required state/report publication and the started-interval
failure marker. Concrete solver helpers remain internal implementation details.

Shared code retains topology preparation, scene contact/sleep queues, captured body
views, statistics publication and ordered user callback/event dispatch. The owner
advances the world tick only after actual solve; CPU results become dispatchable
then, while GPU waits for required publication to finish. EndStep is attempted on
failed preparation as well as solved intervals, and nested finally releases the
space step guard before user code. Invalid authored geometry publishes no solved
tick; a user callback exception preserves a completed usable world and later
callbacks are still attempted. A started GPU execution failure remains terminal.

Typed internal membership lists avoid boxed interface enumerators in warmed
owner phases. They are not public mutable collections; existing owner/topology
rules continue to protect them.

[PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs)
checks solved tick/view publication, callback order, recursive-step and borrowed
release rejection, continued simulation after a user callback error, rejected
geometry and scene recovery on both implementations. Common physics, GPU demand,
checkpoint, portable and network checks retain their separate acceptance scope.


## Body motion and platform lookup

TestBodyMotion accepts engine-unit pose/motion, physical RID and typed exclusions,
then dispatches to the selected world owner. Common server/space code no longer
passes native fixture lists or selects a solver. CPU owns candidate collection,
priority-weighted recovery, one-way/directed-ray/full-contour geometry and sweep
refinement in [its motion partial](../../src/Servers/Physics/CPUPhysicsWorldBackend.Motion.cs).
GPU dispatches the existing resident query pipeline with its retained geometry leases.
Both preserve supplied-pose testing without moving the live body and retain logical
shape/object identity and reusable hit/miss result semantics.

Platform point velocity resolves the current scene/raw body through the shared RID
registry and verifies its current attachment belongs to the queried world. There
is no complete-body scan or newly created runtime view. The selected collider
returns world-point surface/center motion; CPU keeps direct native world-point
conversion to avoid a decoded-pose subtraction/addition round trip. Invalid,
Area, foreign, detached and freed identities return empty velocity/layer values.

[PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs)
checks selected hit/miss sweeps, filtering/exclusions, unchanged pose, scene/raw
surface velocities within .002 scene units and native-converted miss travel within .0001 units, world transfer/stale identities,
off-owner rejection and 64 warmed motion/lookup cycles at zero owner/all-thread managed bytes on both implementations, including a CPU child without a graphics device.

## Ownership and verification

The space owner thread controls lifecycle and stepping. [PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks fresh implementations, distinct physical stores, unchanged requested/actual diagnostics, real published motion, independent ticks, warmed full-step allocation, native lifetime, injected worker-cleanup failure and a no-device child process. It also checks all six direct-space operations, access/lifetime/failure guards and 64 warmed hit/miss query cycles at zero owner/all-thread allocation. Existing common physics, checkpoint and network checks remain separate acceptance evidence.

## Limits and decisions

PhysicsSpace retains membership, authoring, observer state and event/callback ordering. This internal factory is currently limited to the two built-ins. It is not public backend registration and does not implement custom geometry or registered server factories. Caller-created body-state extension hooks now execute; factory-returned callback integration remains open. Caller-created direct-space extension views now execute all six typed query hooks and scoped exclusions; factory-returned integration remains open. Native allocation and foreign-platform execution remain unverified. Recorded GPU/window measurements are workload-specific evidence; broader device/profile acceptance remains separate. See [ADR 0054](../decisions/physics-backends.md#adr-0054) and [ADR 0103](../decisions/physics-extensions.md#adr-0103).

[Caller-created body-state extensions](../components/physics-body-extensions.md) now execute the complete typed state/force/space/contact family. Body context is qualified by current attachment generation; nested hooks borrow body/world lifetime and preserve ordinary live mutation. Contact projection uses immutable engine-validated PhysicsBodyContact values. Registered server factories and factory-returned scene/server callback integration remain open under ADR 0103.

## Area implementation ownership

AreaContainsPoint dispatches spatial audio containment to the selected owner; ScanAreas now owns concrete scene/raw geometry scans as well as phase selection. Common PhysicsSpace commits logical observer history and queues callbacks. [The ownership contract and shared CPU/GPU checks](../components/physics-backends.md#area-implementation-ownership) describe fields, masks, logical slots, scratch lifetime and zero warmed allocation.
