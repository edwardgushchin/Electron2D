# GPUPhysicsWorldBackend

Last updated: 2026-10-11

**Declaration:** `internal sealed partial class GPUPhysicsWorldBackend : PhysicsWorldBackend` · **Source:** [GPUPhysicsWorldBackend.cs](../../src/Servers/Physics/GPUPhysicsWorldBackend.cs) · **Component:** [Physics backends](../components/physics-backends.md)

## Description and internal flow

CreateCollider delegates actual attachment retention to its owned resident store.
The store resolves native handles/publication caches independently of the selected
collider's concrete type. Selected wrappers execute body operations while GPU
queries, Areas, joints and replay retain actual storage ownership. See
[GPU collider ownership](../components/physics-backends.md#gpu-collider-ownership).

Owns one independent resident GPUPhysicsBodyStore, with no native CPU world or CPU workers. Creation applies existing sleep/contact/iteration/default-bias settings. The store keeps physical state and executes the resident pipeline. A failed constructor releases the created store before startup fallback can be considered.

GPUStore is the owned resident implementation. Kind and Requested remain GPU for its lifetime. EnsureAccess rejects disposal or terminal store failure. Step uses the common space boundary and GPU-owned solve/publication/failure phases. Dispose releases the resident store once; CPU-only native facets and diagnostic stage controls reject.

All six direct-space operations project results from the existing resident PhysicsSpace GPU query driver and geometry leases. The implementation retains its point/shape/contact result lists; Dispose clears their object references before releasing the store. Public views validate access before dispatch, including terminal world failure and empty destination calls.

CreateCollider constructs a fresh GPUPhysicsColliderImplementation for each body/Area attachment. The retained collider identity facade delegates all engine-unit body and shape operations to that concrete owner; reattachment never retargets its immutable world.

CreateJoint creates a fresh GPUPhysicsJointImplementation for each constraint attachment. Shared settings and physical RID stay in PhysicsJointRuntime. GPU world pins need no CPU anchor. Generic policy/setters/portable-frame/release operations dispatch to that attachment.

Source partial: [world policy](../../src/Servers/Physics/GPUPhysicsWorldBackend.Policy.cs).

## Joint input ownership

CreateJoint receives the retained common joint identity. Attachments consume [PhysicsJointConfiguration](PhysicsJointConfiguration.md) values rather than the mutable runtime. CPU retains native joint ownership by that identity, schedules actual spring phases and retires matching entries; GPU keeps resident execution. See [the shared input contract](../components/physics-backends.md#shared-joint-inputs-and-native-ownership).

## World policy, capacity and statistics

The selected owner now applies sleep/contact thresholds, solver iterations and world joint defaults. PhysicsSpace validates and retains authored values, then dispatches implementation work. CPU owns native threshold updates, waking, worker selection, sensor scratch, solver/arena capacity and native sensor/process-statistics traversal. GPU applies resident settings and epoch invalidation, prepares existing GPU publication/report scratch and reads completed store statistics; it creates no CPU motion-history mirror.

CPU capacity preparation keeps a conservative flag once any dynamic attachment can sleep. Worlds whose dynamic bodies have never enabled sleep skip repeated complete-body scans during attachment. Enabling sleep or changing a sleep-enabled static/kinematic body to a dynamic role prepares dormant capacity immediately. The flag never resets, so private checkpoint restores cannot introduce an unobserved sleeping role. Existing dormant budgets and physics semantics are retained.

[PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks typed policy, invalid values, independent worlds, CPU native settings, selected statistics, no GPU CPU-motion storage, live role changes and disposed-owner guards. Common sleep/contact/iteration/statistics, replay and network suites remain the behavioral acceptance boundary.

## Selected step phases

GPU step phases prepare authored policies and the required pre-step activity snapshot, enqueue motion/Area/joint/filter/report work, solve resident fields and synchronize consumer state/reports. ResultsReady becomes true after required publication completes. EndStep retains the started-submission failure marker without CPU replay. The [common phase/lifetime boundary](PhysicsWorldBackend.md#selected-step-phases) owns observer publication and ordered callbacks. Source: [GPU phases](../../src/Servers/Physics/GPUPhysicsWorldBackend.Step.cs).

Body-motion tests now dispatch through this selected owner using engine-unit inputs and RID exclusions. The [common contract and verification](PhysicsWorldBackend.md#body-motion-and-platform-lookup) records geometry, identity, lookup and allocation boundaries.

## Ownership and verification

The space owner thread controls lifecycle and stepping. [PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks fresh implementations, distinct physical stores, unchanged requested/actual diagnostics, real published motion, independent ticks, warmed full-step allocation, native lifetime, injected worker-cleanup failure and a no-device child process. It also checks all six direct-space operations, access/lifetime/failure guards and 64 warmed hit/miss query cycles at zero owner/all-thread allocation. Existing common physics, checkpoint and network checks remain separate acceptance evidence.

## Limits and decisions

Startup fallback is handled only by the internal creation factory. A started GPU interval never selects or executes a CPU replacement. Shared registrations, authored metadata, readback consumers and event delivery remain in PhysicsSpace; public registered extensions are still absent. Native allocator and foreign-platform execution remain separate gates; recorded workload-specific GPU/window measurements do not establish general superiority. See [ADR 0054](../decisions/physics-backends.md#adr-0054) and [ADR 0103](../decisions/physics-extensions.md#adr-0103).

## Area implementation ownership

[The Area partial](../../src/Servers/Physics/GPUPhysicsWorldBackend.Areas.cs) owns resident scene/raw overlap scans and sensor/audio result scratch. AreaContainsPoint uses current sensor capacity, canvas/mask filtering and logical RID projection. Scans commit generation-qualified observations to shared PhysicsSpace; Dispose clears retained geometry/result buffers before store release. Device field reduction is unchanged. [Contract and checks](../components/physics-backends.md#area-implementation-ownership) cover both selected backends and warmed allocation.

## Native simulation and replay ownership

SolverCheckpoint wraps the resident device checkpoint behind the selected PhysicsBackendCheckpoint contract. Portable one-way staging now belongs to this owner; current shared shape ownership/generation qualifies every record. ReadDebugContacts supplies capped scene-unit points. The resident solve equations and state ownership remain unchanged. [Contract and verification](../components/physics-backends.md#native-simulation-and-replay-ownership) describes replay/portable boundaries.

## Selected checkpoint ownership

PrepareCheckpoint and the selected history operation family borrow library lifetime. GPU reports/epochs/collider caches stay in its concrete checkpoint; common authoring uses neutral field values. Failed capture is replaceable, validation rejection preserves state, begun restore failure is terminal, and cleanup failure still retires the point. See [the executing contract](../components/physics-backends.md#selected-checkpoint-ownership).

## Selected fields, exceptions and portable observation

Resolved fields and exception invalidation dispatch through the selected collider/world; common server/replay/map code retains no GPU handle/state aliases. Portable preparation and bound-body observation use the selected owner and borrowed common inputs. GPU pair/batch scratch stays private; CPU preserves existing field/fixture preparation. See [the executing contract and limits](../components/physics-backends.md#selected-observation-and-collision-policy).

## Selected shape-pair operations

The internal resource/resource and body-slot/resource bridge now executes through the retained implementation with complete logical indices, composed global poses and validated nested caller-span results. Detached bodies use the same exact resource kernel without a solver; borrowed collider release is preflighted before registry cleanup. GPU scalar pose demand gathers sixteen pose bytes plus eight status bytes instead of complete motion/field/sleep state when no current cache exists. [The pair contract and checks](../components/physics-shape-pairs.md) distinguish this internal prerequisite from the pending complete public server-extension family.
