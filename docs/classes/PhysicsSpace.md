# PhysicsSpace

Last updated: 2026-10-10

**Declaration:** `internal sealed partial class PhysicsSpace : IDisposable`

**Source:** [PhysicsSpace.cs](../../src/Servers/Physics/PhysicsSpace.cs), [PhysicsSpace.BodyState.cs](../../src/Servers/Physics/PhysicsSpace.BodyState.cs), [PhysicsSpace.Kinematic.cs](../../src/Servers/Physics/PhysicsSpace.Kinematic.cs) · **Component:** [Physics server and queries](../components/physics-queries.md)

## Description and internal flow

One owner-thread CPU or independent GPU world shared by scene bodies/Areas/joints and caller-owned colliders. SceneTree and PhysicsServer host stepping use this same simulation lane; public consumers use [PhysicsServer](PhysicsServer.md#activity) and World. The fixed scene lane owns each interval; its internal [task scheduler](PhysicsTaskScheduler.md) parallelizes backend work only.

| State/operation | Contract |
| --- | --- |
| `PhysicsSpace(Backend backend = CPU, bool allowCPUFallback = false)` | Selected CPU world or resident GPU store with sampled defaults; initially inactive. CPU alone owns the retained task scheduler. |
| `RID`, `WorldID`, body/Area/server-collider/joint lists | Stable server identity and current native generation/membership. |
| `bool IsActive { get; }`, `SetActive(bool active)` | Local interval policy, owner/solver guard; SceneTree sets true on registration. |
| `EnsureQueryAccess()`, `EnsureReleaseAccess()`, `PrepareForQuery()` | Owner/lifetime/solver guard and pending fixture/pose preparation, including inactive worlds. |
| `EnsureWorldBindingChange()`, `EnsureWorldRelease()` | Binding changes reject a failed GPU world; releasing its last resource still permits cleanup. Both preserve owner/solver/live-body-callback guards. |
| `Step(double delta)` | Gate on local/global activity/nonzero delta; prepare fields/body states/joints, solve native intervals, capture state and dispatch callbacks/events. |
| `EnableGPUIntegration()`, `EnableGPUSolver()` | Internal development entries for numeric integration, GPU tree construction/refit/traversal/built-in resident pair filters/resident contact lookup, GPU contact identity allocation/initialization, adjacency construction and disjoint-contact removal and resident contact-driven island merging/unlinking with parallel ordered contact lists and compact publication and disconnected-island splitting, resident shape geometry/manifold generation, complete GPU contact updates, collision-batch contact/joint coloring and constraint preparation/solving with fused body-pose, sleep-eligibility and fast-body finalization. GPU callbacks run on the owner; generated geometry and feature-matched warm-start state remain on GPU for constraint preparation, while retained workers publish ordinary contact mirrors and custom material/pre-solve callbacks plus graph/events remain on the owner. CPU query/CCD tree mirrors, publication ranking, user callbacks, mirror/event publication, external authoring removal, authoring graph coloring/island changes and island sleep/set transfer/CCD remain CPU; chain manifolds are not yet supported by the GPU entry. |
| `LastStep`, cached body-state callback list | Last actual interval and generation-aware delivery; skipped intervals retain data. |
| `Add` / `Remove` scene/server objects | Native membership, dependent joint/monitor lifetime and identity. |
| `GetJointWorldBody()` | Hidden shape-free world anchor for single-body server pin. |
| `CreateCheckpoint()` | Capture a reusable same-world CPU/GPU rewind point including attached observer state; restore validates configuration and silently resets physics poses. See [local world checkpoints](../components/physics-space-checkpoints.md). |
| `Dispose()` | Destroy joints before bodies/world, join retained workers, detach caller configuration and invalidate views. |

An internally enabled GPU-stage failure releases solver scratch/lock ownership,
marks the space failed and rejects later stepping/queries rather than replaying
a partially committed interval on CPU. Disposal remains available. The independent GPU store uses the same fail-closed policy. Public startup
selection and fallback are described in [physics backends](../components/physics-backends.md).

## Invariants and verification

Local control and native queries require the owner outside solving. Global activity is sampled atomically at each world interval boundary. Inactive intervals leave state, force queues, contact/sensor snapshots and handles intact; resume does not replay skipped time. Running intervals finish queued callbacks; post-solver control affects later intervals. Scene scheduling remains separate. Structural changes may allocate; prepared frames and activity switches reuse storage.

PhysicsActivityTests checks defaults, actual native motion/spring, skipped callbacks/forces, queries/configuration, related scheduling, failure/thread/lifetime boundaries and zero managed bytes over 64 warmed policy/solver cycles on Linux/.NET 10. Physics body/joint/shape/monitor suites cover the shared kernels and lifecycle. The [backend performance report](../components/box2d-performance.md) measures a fixed large-world kernel on Linux x64. Other platforms, native allocations and owner acceptance remain unverified. [ADR 0089](../decisions/physics-activity.md#adr-0089) owns the gate; the [physics decision index](../decisions/index.md) routes its kernels.

Large intervals select up to four workers once fixtures/body modes are prepared and at least 256 backend bodies remain awake. Small intervals use the direct serial path without creating threads; already created workers remain parked until needed or disposed. Collision and solver jobs complete before scene transforms, state capture and owner callbacks. A world without callbacks or requested/previously captured contacts skips the body-callback snapshot entirely. Persistent views that only read live body fields do not trigger that snapshot. A previous contact snapshot is cleared on the next step after its cap becomes zero. When any receiver needs that phase, the complete body order is retained so an earlier callback can enable a later receiver within the same frame. One-way pair history uses a world-local lock inside pre-solve.

Solver preparation gives only the three shared sets whole-world capacities. Dormant island slots start with 16 bodies, 32 contacts, four joints and one island, then retain the capacities exercised by their island topology. This avoids a whole-world copy in every dormant slot. New larger island topologies need warmup outside the prepared measurement interval; repeated sleep/wake reuses their retained buffers. PhysicsSolverStorageTests checks linear dormant capacity for 65,536 independent fragments, repeated zero-byte sleep/wake and disposal of spare arrays.

Membership preparation skips full monitor scans for uninstrumented additions to worlds without scene/server Areas; solver preparation uses the retained worst-case sleep bound before rescanning. Contact departure cleanup traverses the configured snapshot/monitor subjects, preserving body order, and tail removals avoid a full membership search. Configuration changes rebuild that subject list; disposal clears it.

Broad-phase pair queries retain their peak requested/overflow count between intervals. Dense query results therefore reuse the arena buffer after preparation instead of repeatedly allocating overflow pair objects when the number of moving proxies drops. PhysicsSolverStorageTests includes a 96-body overlapping query exceeding the original 16-pairs-per-proxy estimate.

Movement events reserve the rounded whole-world body capacity during membership preparation. Later wake propagation increases the used event count without exact-size array growth each interval. The maximum sleeping-wall test checks that capacity before launch.

State/query access also rejects a locked underlying solver before mutation. Raw
server kinematic targets are prepared before common path subdivision and consumed
after the completed interval. Scene and server state tests exercise this on CPU/GPU.

## Contact impulse publication

[PhysicsSpace.ContactImpulses.cs](../../src/Servers/Physics/PhysicsSpace.ContactImpulses.cs)
keeps a reusable feature map only for frames subdivided into multiple native calls.
Reported fixture pairs are canonicalized, including swapped feature bytes and
impulse direction; per-solve epochs prevent double counting both reporters. The
map indexes retained records and per-body encounter lists. Records sum global-axis
vectors, retain last geometry and greatest depth, and preserve transient contacts
that separate before the final interval. A single native
interval reads current-epoch manifold totals directly; sleeping/stale solves return
zero. All accumulation precedes worker snapshot collection and public callbacks.
PhysicsContactImpulseTests checks momentum, raw/scene roles, shared pairs, sleeping
frames and zero warmed allocation on CPU/GPU. Unseen larger topology can grow the
retained map; it is not an unlimited preallocated contact store.

After an internally enabled GPU backend fails, body/area/joint teardown preserves
owner/stepping guards but skips individual raw graph destruction and partial-motion
capture. Managed bindings/views are released; the failed space reclaims raw storage
in bulk. Queries and further simulation remain rejected. See the
[GPU island graph failure contract](../components/gpu-physics.md#gpu-contact-driven-island-graph-2026-10-08).


## Joint correction default

ConstraintDefaultBias captures ProjectSettings.Physics2DDefaultConstraintBias with active feature overrides at construction (default 0.2). SetConstraintDefaultBias validates a finite [0,1] value under the normal owner/phase/error guard, then updates attached zero-bias joints and wakes their connected bodies. Explicit nonzero joint bias is preserved. The typed PhysicsServer accessors project this value. It governs joint recovery, not the separately pending contact-bias setting.


## Complete convex motion geometry

Body-motion recovery, initial penetration and impact geometry use a complete convex
contour when a shape exceeds the backend piece limit. Directed queries reject ray
origins inside the complete contour or its swept region before selecting pieces.
The fixture tag borrows the contour weakly; shared resource collision scratch stays
allocation-free after warmup. PhysicsMotionTests covers both polygon roles and
both directed-containment directions. See [body-motion verification](../components/gpu-resident-motion-queries.md).


## Continuous collision policy

The shared public CCDMode now supplies the scene/server setting and resident GPU
configuration. CPU worlds inspect solved motion before publication and preserve
remaining tick time and nominal force/joint budgets through impact continuations.
The runtime stores policy across attachments and roles; independent GPU world
binding remains open. See [implementation and verification](../components/cpu-continuous-collision.md).

Analytic world boundaries are handled outside finite broad-phase bounds. The CPU
hosted GPU-stage experiment leaves boundary/ray custom manifold publication on the
host; independent GPUPhysicsBodyStore uses device plane contacts instead.
[WorldBoundaryTests](../../tests/Electron2D.Tests/WorldBoundaryTests.cs) checks both
paths, with [measurements and limits](../components/physics-shapes.md#infinite-world-boundaries).

SleepSettings captures the three project sleep defaults. SetSleepSettings validates
owner/phase and all values before publication; changed values update native angular/time
fields and existing body linear thresholds, restart timers and wake dynamics. New
attachments read the current policy. The shared [sleep report](../components/physics-sleep.md)
records exact threshold/delay semantics and scene/server verification.

World contact settings capture validated project bias/slack and feed native contact preparation with the outer tick duration. Typed edits wake dynamic bodies without rebuilding fixtures. See [contact policy](../components/physics-contact-policy.md).

SolverIterations captures the typed project setting when the world is created and
feeds both scalar overflow and colored SIMD contacts/joints. SetSolverIterations
validates before changing native policy and shares WakeDynamicBodies with contact
settings. The internal raw backend default remains one for isolated diagnostics;
Electron2D world creation explicitly applies its captured setting (default sixteen).

ContactSettings also retains sampled contact recycle radius and maximum separation.
Publication converts scene distances to native units and wakes dynamics without
rebuilding fixtures. The CPU updater stores true body-local boundary anchors,
validates their current normal/tangential separation and performs one-use geometric
impulse matching after preferring stable features. Fresh geometry and event identities
remain independent of the history limits.

Body-motion candidates capture each obstacle's CollisionPriority. Recovery keeps
up to 32 deepest accepted contact planes and applies normalized weighted projection
over four attempts; compound convex fixtures contribute their full contour once.
The fixed stack buffer avoids managed allocation. [Collision priority](../components/physics-contact-policy.md#collision-priority)
retains one-way/exclusion rules, query identity and motion/reporting semantics.


ObjectTreeChanged publishes scene monitor visibility changes for external Node bindings independently of physical body lifetime. Raw Area callbacks retain physical pair semantics. See [object associations](../components/physics-object-bindings.md).

The independent path is implemented by the GPU partials beside this source: scene/server
publication, selected contact reports, Area geometry queries, body motion and authored
exceptions. Its observable host caches and waits are documented in the backend component.

## Statistics publication

[PhysicsSpace.Statistics.cs](../../src/Servers/Physics/PhysicsSpace.Statistics.cs)
retains an internal `Statistics(Active, Pairs, Islands)` value. The private
`SensorStatisticsQuery` value carries native tree-query state without allocations.
CPU publishes after body capture; GPU captures the final resident counters before
query/report work and publishes after completed body capture. Empty active positive
steps clear the sample. The PhysicsServer registry gate protects publication and
aggregation. Local activation uses a volatile flag so diagnostics can read it
without entering a foreign owner's solver. See [Physics statistics](../components/physics-statistics.md).

## Contact diagnostic snapshots

The [diagnostic partial](../../src/Servers/Physics/PhysicsSpace.DebugContacts.cs)
retains the configured limit, point buffer/count and monotonically changing revision.
Internal owner-thread `SetDebugContacts` prepares storage, while `DebugContacts`
borrows the current span. Positive active steps clear the previous sample, then
publish penetrating surface samples from the latest manifold batch. CPU uses its
retained pre-solve midpoint/separation; GPU supplies a bounded compact result.
Disabled/sleeping/sensor pairs do not become markers. Reading a failed world still
rejects, while disabling is permitted for release. These internal operations do not
expand the public PhysicsServer API. See [diagnostics](../components/physics-debug.md).

## Conditional GPU body publication

GPU preparation scans existing callback/contact-snapshot predicates and pending
force/torque. Only consumers needing pre-step activity retain the initial full
publication and ApplyBeforeStep phase. Connected wakes still publish before device
simulation; completion continues to publish final state. A per-world publication
epoch qualifies attachment caches against selected intermediate reads, so a final
state equal to earlier publication never revives an intermediate cached pose.

GPU steps now populate the existing eight optional ProfileMS/ProfileBytes slots:
attachments; policies/consumers/pre-publication/fields; motion/joint preparation/wakes;
simulation/debug; post-publication/reports; scene/server completion;
contacts/Areas/views; callbacks/events. Mean profiling does not add GPU fences.
See [public-world measurement and limits](../components/physics-backends.md#conditional-body-publication).

The consumer scan now also synchronizes changed authored body policies before
publishing pre-step state. This batches their wakes and prevents per-body readbacks
when callbacks consume pending forces. Diagnostic ForceGPUParameterRefresh and
DecodeGPUTransforms retain same-build controls; they are internal test settings.
See [host preparation and callback regression](../components/gpu-host-preparation.md).

## Portable correction

`PhysicsSnapshotMap` borrows a world, validates complete network identity bindings
and compatible authoring, then replaces portable physical/observer state silently.
The world retains reusable one-way translation scratch and disposes its registered
maps. A correction execution error shares the failed-world guard with local replay.
CPU correction disables warm start for one following interval; local checkpoints
also preserve that pending policy. See [portable snapshots](../components/physics-snapshots.md).
