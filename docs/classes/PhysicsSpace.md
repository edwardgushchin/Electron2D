# PhysicsSpace

Cold world-query geometry now uses the [shared using-world borrow](../components/physics-queries.md#cold-query-geometry) before user suppliers run. Numerical writes, collider retirement/transfer and final world release reject; original supplier failures can retry. Existing pure input reads and nested query behavior are retained.

Last updated: 2026-10-18

## Failed local activation

A failed selected activation leaves the prior public flag committed. Equal writes retry incomplete publication; live space queries, local/portable history and active positive intervals restore that committed policy before numerical use. Explicit writes may instead commit a different supplied choice. Metadata reads and skipped intervals remain non-preparing. Post-solver failure keeps its completed tick and callbacks; retirement discards pending activation. See [activation recovery](../components/physics-registered-adapter.md#failed-local-activation) for exercised scope and limits.

## Failed world policy publication

World sleep/contact profiles, solver iterations and default constraint bias retain accepted input when selected publication fails. An equal setter retries its complete current family; live queries, checkpoint/portable preparation and active positive steps finish incomplete families before numerical use. Completed families do not replay when a later family fails. Authored getters remain non-preparing; skipped steps and retirement do not publish pending settings. Local restore validates fixed configuration before this preparation. See [world policy recovery](../components/physics-registered-adapter.md#failed-world-policy-publication) for execution evidence and limits.

## Terminal world access

Terminal simulation or local/portable restore failure rejects attached motion/role writes before changing authored values, including equal assignments. Numerical scene sleep and direct-profile motion reads use the same common failure gate; ordinary native authored velocity/policy and constant-surface reads remain non-preparing. The gate does not impose public-query phase rules on otherwise valid force/state callbacks. Detachment and cleanup remain available. See [terminal world authoring](../components/physics-registered-adapter.md#terminal-world-authoring) for the public regression scope and limits.

**Declaration:** `internal sealed partial class PhysicsSpace : IDisposable`

**Source:** [PhysicsSpace.cs](../../src/Servers/Physics/PhysicsSpace.cs), [PhysicsSpace.BodyState.cs](../../src/Servers/Physics/PhysicsSpace.BodyState.cs), [PhysicsSpace.Step.cs](../../src/Servers/Physics/PhysicsSpace.Step.cs) · **Component:** [Physics server and queries](../components/physics-queries.md)

[GPU publication for current consumers](../components/gpu-demand-publication.md) now keeps unobserved raw bodies on device. Explicit getters select one current snapshot; scene consumers, live raw views, contact receivers and kinematic completion retain fresh data. Portable preparation batches its explicit state demand. Cached values are qualified by the completed interval epoch; callback ordering and failure/lifetime guards remain unchanged.

`ReadBodyTransforms` validates the complete body sequence and retains staging, selected handles, input indices and compact pose scratch. CPU and scene/static projection use the scalar path; GPU raw dynamic projection reads one 16-byte pose per request. Output commits only after successful reads; retained body references are cleared in `finally`. Empty requests still run the common space guards. Source: [PhysicsSpace.BodyTransforms.cs](../../src/Servers/Physics/PhysicsSpace.BodyTransforms.cs).

The [common step boundary](../../src/Servers/Physics/PhysicsSpace.Step.cs) now dispatches owner phases for preparation, solve, result synchronization, body completion, contacts and Areas. Shared callback/event ordering and always-released step guards replace separate CPU/GPU outer-step implementations. The selected owner retains actual numerical work, completion readiness and GPU failure policy.

Scene and raw colliders use the same public space-assignment transition. Actual scene attachments can differ from their render-world descriptor; direct changes retain this space's existing driver. Partial additions are removed before a failed transfer restores its old attachment. See [common space bindings](../components/physics-registered-adapter.md#common-space-bindings).

## Pending collider metadata

Object/canvas/priority authoring and reverse node membership commit before selected publication. Initial factory/attachment hooks already borrow the target world and node. Failed live publication queues complete metadata repair before live queries/nonzero steps; repeated preparation failure keeps the tick unchanged. Failed initial creation retires partial bindings. See [the contract and executable limits](../components/physics-registered-adapter.md#failed-collider-metadata-publication).

## Description and internal flow

Selected initialization/activation/query-flush/end-sync now use the retained
owner's lifecycle. Active policy commits after owner work; shared observers drain
even if the selected flush throws. Empty positive active steps reach that same
owner. Implementation hooks borrow context and nested observer flushing preserves
its enclosing phase. See [lifecycle](../components/physics-backends.md#selected-lifecycle-and-activity).

One owner-thread world shared by scene bodies/Areas/joints and caller-owned colliders. Authored sleep/contact/iteration/default-joint policy stays here; application, capacity preparation and completed statistics dispatch through the selected owner. It retains one selected [PhysicsWorldBackend](PhysicsWorldBackend.md), which owns the native CPU world/workers or the resident GPU store and full interval dispatch. SceneTree and PhysicsServer host stepping use this same simulation lane; public consumers use [PhysicsServer](PhysicsServer.md#activity) and World. The fixed scene lane owns each interval; its internal [task scheduler](PhysicsTaskScheduler.md) parallelizes backend work only.

| State/operation | Contract |
| --- | --- |
| `PhysicsSpace(RID rid, bool separateSolverThread = false)`, `InitializeBackend(...)` | Registered immutable identity and sampled settings precede selected-owner construction; initialization completes before activation. CPU alone owns the retained task scheduler and native policy/capacity/statistics implementation. |
| `RID`, `WorldID`, body/Area/server-collider/joint lists | Stable server identity and current native generation/membership. |
| `bool IsActive { get; }`, `SetActive(bool active)` | Local interval policy is retained by the selected owner and commits after its hook succeeds; owner/solver/recursive guards remain. SceneTree sets true after initialization. |
| `EnsureQueryAccess()`, `EnsureReleaseAccess()`, `PrepareForQuery()` | Owner/lifetime/solver guard and pending fixture/pose preparation, including inactive worlds. |
| `EnsureWorldBindingChange()`, `EnsureWorldRelease()` | Binding changes reject a failed GPU world; releasing its last resource still permits cleanup. Both preserve owner/solver guards and reject release/binding changes during every physics callback phase. |
| `Step(double delta)` | Gate on local/global activity/nonzero delta; reject recursive callback intervals; prepare fields/body states/joints, solve native intervals, capture state and dispatch callbacks/events. |
| `EnableGPUIntegration()`, `EnableGPUSolver()` | Internal development entries for numeric integration, GPU tree construction/refit/traversal/built-in resident pair filters/resident contact lookup, GPU contact identity allocation/initialization, adjacency construction and disjoint-contact removal and resident contact-driven island merging/unlinking with parallel ordered contact lists and compact publication and disconnected-island splitting, resident shape geometry/manifold generation, complete GPU contact updates, collision-batch contact/joint coloring and constraint preparation/solving with fused body-pose, sleep-eligibility and fast-body finalization. GPU callbacks run on the owner; generated geometry and feature-matched warm-start state remain on GPU for constraint preparation, while retained workers publish ordinary contact mirrors and custom material/pre-solve callbacks plus graph/events remain on the owner. CPU query/CCD tree mirrors, publication ranking, user callbacks, mirror/event publication, external authoring removal, authoring graph coloring/island changes and island sleep/set transfer/CCD remain CPU; chain manifolds are not yet supported by the GPU entry. |
| `LastStep`, cached body-state callback list | Last actual interval and generation-aware delivery; skipped intervals retain data. |
| `Add` / `Remove` scene/server objects | Native membership, dependent joint/monitor lifetime and identity. |
| `CreateCheckpoint()` | Capture a reusable same-world CPU/GPU rewind point including attached observer state; restore validates configuration and silently resets physics poses. See [local world checkpoints](../components/physics-space-checkpoints.md). |
| `Dispose()` | Attempt every checkpoint/map, joint, body/Area/collider and query-cache release; clear registrations and finally release the selected implementation. Aggregate cleanup failures after all attempts, including native world destruction. |

An internally enabled GPU-stage failure releases solver scratch/lock ownership,
marks the space failed and rejects later stepping/queries rather than replaying
a partially committed interval on CPU. Disposal remains available. The independent GPU store uses the same fail-closed policy. Public startup
selection and fallback are described in [physics backends](../components/physics-backends.md).

Selected motion dispatch now borrows world lifetime for the whole query and validates its completed output before caller replacement or scene movement. Finite travel/contact values, ordered fractions, the requested moving RID, live enabled logical slots, same-space bodies, moving-mask/target-layer filtering, symmetric body/joint exceptions and explicit RID/object exclusions are checked. Collider object identity is sampled by the engine; supplied identity cannot retarget a result. Misses clear collision payload and must report completed motion. Same-space recursive motion rejects because the selected solver owns reusable query scratch; query failures retire borrowing without advancing or poisoning the world. Public registration remains open under ADR 0103.

TestBodyMotion prepares authored topology and delegates engine-unit inputs to the selected world owner. TryGetBodyPointMotion resolves the current scene/raw body by RID, verifies world ownership and delegates world-point velocity to its selected attachment. Neither operation carries native shape lists through common code; platform lookup does not scan world bodies. See [motion ownership](PhysicsWorldBackend.md#body-motion-and-platform-lookup).

## Invariants and verification

[PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) verifies fresh selected implementations, independent ticks, 512-body warmed full-step allocation, native destruction despite an injected worker-cleanup failure and no-device startup/fallback. Shared body/shape/joint/query paths and registration/extension work retain their own gates.

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

[CPUPhysicsWorldBackend.ContactImpulses.cs](../../src/Servers/Physics/CPUPhysicsWorldBackend.ContactImpulses.cs)
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
`CPUPhysicsWorldBackend.SensorStatisticsQuery` value carries native tree-query state without allocations.
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
Disabled geometry and sensor pairs do not become markers; report-only kinematics
also contribute current penetrating samples without native constraint membership.
Reading a failed world still
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

## Report-only contacts

The [report-only partial](../../src/Servers/Physics/CPUPhysicsWorldBackend.ReportOnly.cs)
queries CPU static/kinematic trees after each internal interval only for receivers
with a positive contact cap. Kinematic subdivision also covers report-only
receivers without dynamic participants; earlier observations remain in the outer
frame snapshot while bounded diagnostics retain the latest interval.
The CPU owner's private `ReportOnlyQuery` struct borrows that implementation, the receiver's current
`B2BodyId`, and one live `B2Shape` for the synchronous tree callback. It owns no
resources and never escapes the owner-thread capture interval. The callback
validates pair masks, body/joint vetoes and contact policy, then projects an
existing manifold into retained frame contacts with zero impulse. The same pair
contributes once to statistics and bounded diagnostics without joining native
constraint or sleep islands. Infinite boundaries also query their explicit proxy
list.

Partial internal use inside the CPU owner: `new ReportOnlyQuery { Space = this, Receiver = backend.BodyID, Shape = shape }`
borrows already prepared attachments; callers must not retain it across a step.
`PhysicsReportOnlyTests` exercises both receiver orders, masks/exceptions/joints,
quiet lifecycle, geometry, snapshots and allocation limits through public API.
See [the complete contract](../components/physics-report-only.md).

Caller-created direct-space extension hooks borrow a query context. The space rejects recursive active stepping, checkpoint capture/restore, world binding changes and world release until the outer hook/validation completes; nested queries and ordinary live reads remain available. This uses a balanced query-depth guard in finally and preserves the existing observer callback rules. See [direct-space extensions](../components/physics-space-extensions.md).

[Caller-created body-state extensions](../components/physics-body-extensions.md) now execute the complete typed state/force/space/contact family. Body context is qualified by current attachment generation; nested hooks borrow body/world lifetime and preserve ordinary live mutation. Contact projection uses immutable engine-validated PhysicsBodyContact values. Registered server factories and factory-returned scene/server callback integration remain open under ADR 0103.

## Area implementation ownership

Concrete scene/raw Area overlap scans and audio containment now dispatch through PhysicsWorldBackend. CPU owns priority/field scratch and pre-step body preparation; GPU owns resident sensor/audio scratch. CommitAreaScan and QueueMonitorChanges retain shared logical histories and ordered user callbacks. DefaultGravity retains the sampled/current CPU subtraction baseline across checkpoint and portable restore. [Contract and verification](../components/physics-backends.md#area-implementation-ownership) covers masks, fields, disabled slots, transfer/reentry/free and zero warmed managed allocation.

## Native simulation and replay ownership

Native discrete/CCD/kinematic scheduling, one-way episodes, contact impulse/motion histories and report-only geometry/statistics now belong to CPUPhysicsWorldBackend. Common Checkpoint uses one selected PhysicsBackendCheckpoint for private solver history and retains shared authoring/observer/pose metadata. Portable one-way capture/restore and diagnostic point reads reach the selected owner. CallbackBodies and ContactBodies are internal typed subjects, not public mutable collections. [Contract and checks](../components/physics-backends.md#native-simulation-and-replay-ownership).

## Registered initialization

Explicit and lazy scene worlds now use PhysicsServer.CreateRegisteredSpace. It allocates and registers an immutable space RID before invoking the selected implementation factory outside the registry lock. Metadata/settings exist first; ordinary query access rejects until an implementation is retained. The initialization phase can then construct guarded direct-space extensions and query the real selected world. Activation, stepping, attachment changes, recursive initialization and release reject until it completes. Explicit spaces publish inactive; scene spaces become active after successful initialization.

Failure retires owned/direct-state/space registrations and releases any implementation retained by that space. A foreign implementation is rejected without disposing its original world. Initialization and cleanup errors are aggregated after cleanup attempts. [Ownership tests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.Initialization.cs) exercise live RID lookup, registry-lock independence, all six query hooks, transfer rejection, real motion after publication and CPU/GPU native retirement. This is the internal construction transaction; public registered factories and their complete operation bridge remain open.

## Borrowed selected checkpoint lifetime

Selected checkpoint operations borrow the same world context as implementation hooks. Body/Area/joint addition and removal reject while an extension context is borrowed, including raw server transfer; normal observer delivery retains its existing membership rules. Preparation, capture/validate/restore and release use the selected owner. See [ownership and checks](../components/physics-backends.md#selected-checkpoint-ownership).

## Selected fields, exceptions and portable observation

Resolved fields and exception invalidation dispatch through the selected collider/world; common server/replay/map code retains no GPU handle/state aliases. Portable preparation and bound-body observation use the selected owner and borrowed common inputs. GPU pair/batch scratch stays private; CPU preserves existing field/fixture preparation. See [the executing contract and limits](../components/physics-backends.md#selected-observation-and-collision-policy).

## Retained named selection

Existing CPU/GPU creation resolves built-in entries through the retained internal [PhysicsBackendRegistry](PhysicsBackendRegistry.md). A captured selection can retain its requested token and actual registration across lazy creation and shared World aliases. The common initialization transaction invokes the selected factory outside registry locks; internal name diagnostics distinguish a custom registration or built-in fallback. Public named registration and the complete operation bridge remain open. See [selection behavior and checks](../components/physics-backends.md#named-implementation-selection).

## Selected shape-pair operations

The internal resource/resource and body-slot/resource bridge now executes through the retained implementation with complete logical indices, composed global poses and validated nested caller-span results. Detached bodies use the same exact resource kernel without a solver; borrowed collider release is preflighted before registry cleanup. GPU scalar pose demand gathers sixteen pose bytes plus eight status bytes instead of complete motion/field/sleep state when no current cache exists. [The pair contract and checks](../components/physics-shape-pairs.md) distinguish this internal prerequisite from the pending complete public server-extension family.

`CollideShapes(RID first, Transform firstPose, Vector2 firstMotion, RID second, Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count)` and `BodyCollideShape(RID body, int index, RID shape, Transform pose, Vector2 motion, Span<Vector2> contacts, out int count)` return hit state and paired global points through the selected owner. Odd capacities, invalid rigid poses/motion and foreign body context reject before dispatch. Every borrowed frame retires in finally, including nested calls and hook failures.

## Factory contact publication

A factory-returned cached body extension now projects its contact count/values on the world owner after native capture and before scene/raw observer delivery. Complete validated snapshots commit atomically; cached getters retain immutable data and do not reinvoke contact hooks. Independent caller-created views keep ordinary guarded direct hook dispatch. Qualified publication permits immutable PhysicsBodyContact identity construction while ordinary solver queries/mutation stay blocked. Bad receivers retain their prior published state while other callbacks and committed ticks continue. [The executing contract and verification](../components/physics-body-extensions.md#selected-contact-publication) retain public registration/native/platform/performance limits.

[Registered numerical adapter](../components/physics-registered-adapter.md) now connects a public value/RID-only implementation to real scene/raw motion, fields/forces, joints, queries, contact/Area publication, private/portable history and terminal failure. Its independent circle/spring test solver owns its own state and does not wrap a built-in. Public registered factories and the typed family now execute in a separate consumer; arbitrary custom geometry and unexercised operation acceptance remain open.

Portable continuation validation is shared by capture and apply. Shape availability, nonnegative indices, distinct colliders and unordered uniqueness precede selected piece validation. Capture reports malformed implementation continuation as InvalidOperationException; incoming malformed continuation reports InvalidDataException before mutation.

GPU publication reuses a complete snapshot only when its publication epoch matches and every live collider cache is still valid. A cache invalidated independently by sleep edits must still publish. Internal diagnostic controls force repeated publication/count-first transfer for equivalent-work measurements.


The existing borrowed-extension hierarchy guard also rejects object/canvas/priority binding changes and raw collider release/transfer, including partial initial attachments. This prevents recursive metadata publication before authoring changes; qualified immutable result construction retains its current access scope.


Membership removal now attempts every dependent joint, state capture, numerical detach and contact/Area observer cleanup before reporting errors. Internal Cleanup/FinishCleanup collect errors while preserving a lone exception type/stack. A failed numerical detach cannot leave a collider outside common membership but alive in the selected solver. [Resource retirement](../components/physics-registered-adapter.md#resource-retirement) records complete scope and limits.


[Selected diagnostic preparation](../components/physics-registered-adapter.md#diagnostic-capacity-and-publication) passes the contact-point cap to the numerical owner before solving, including registered implementations. Zero disables capture and remains available for failed-world cleanup; synchronized point/count reads retain the bounded shared snapshot. Full numerical and platform acceptance remain separate.

The public GPU adapter retains one rebindable query-geometry lease. Alternating live resources reuses its wrapper and retires the previous reference; past resources are not accumulated. ConvexPolygonShapeTests exercises 128 warmed overlap/contact/rest/motion cycles between shared and standalone geometry on CPU/GPU with zero owner/all-thread bytes, preserving current contour results and initial-overlap fractions. GPUPhysicsShapeQueryTests checks successful replacement, null/disposed/thread rejection with preserved previous geometry, and retired lease failure. Geometry uploads and query waits retain their existing separate costs.

A captured [separate numerical lane](../components/physics-thread.md) now dispatches only Solve after owner preparation and joins before all observations/callbacks. Its private exact-thread borrow authorizes prepared readonly inputs, while public queries, authoring and lifetime remain owner-bound. Disposal joins the retained lane before attachments; CPU/provider checks pass and GPU/native acceptance remains open.

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

CaptureGPUActivity reuses uint scratch sized for resident slots and updates collider activity only. PublishGPUCompletion selects raw callback receivers individually; a callback on one body no longer publishes unrelated raw states. Full callback descriptors preserve late registration. Newly enabled receivers publish fresh fields before GetView and retain active-before-step eligibility.


[Mixed quiet observer publication](../components/gpu-body-publication.md#mixed-quiet-scene-observers-2026-10-14)
selects current incremental or complete state by estimated transfer cost. Cached
mode/sleep is a hint only; scalar values and wake/private/portable continuation
retain authoritative device reads. Existing API and coverage states remain unchanged;
hardware, native, foreign and full acceptance remain open.

## Pending observer publication

Monitoring capacity is prepared from current colliders/shapes before an Area
callback replacement commits. A failed preparation does not advance the receiver
generation, so its already queued events retain delivery. Successful registration
still retires older queued epochs and reenters on the next nonzero scan. See
[observer transactions](../components/physics-queries.md#area-observer-registration-transactions).

## Filter repair before numerical work

A world-local pending flag bypasses filter repair traversal on ordinary frames.
After a failed update, query preparation and Step repair flagged colliders from
current effective owner bits. Step does this before BeginStep and the stepping
phase, preserving existing built-in query guards and leaving a failed repair's
tick/publication unchanged. Another failure retains the flag. [Filter verification](../components/physics-filters.md#failed-updates-and-effective-sensing)
checks both retry routes and all scene/raw roles.

Shared ShapeSetData now retains this world's numerical borrowing throughout all
participating rebuilds, rollback and rejected-copy retirement, even while another
world's provider hook runs. Committed old-view retirement follows borrow release.
[Mixed-world checks](../components/physics-shape-data.md#shared-cpugpuregistered-replacement)
exercise scene/raw authoring and lifetime guards without adding a frame-loop phase.

[Shared owned-shape release](../components/physics-shape-data.md#shared-shape-release) now borrows resource membership and every
participating world through removal and private-view retirement. Status failure
preserves live data; committed cleanup attempts all users, retains exact errors
and unregisters the RID. Public-only checks retain unrelated slots, exact Area
entry/exit histories and zero warmed managed bytes. Wider acceptance stays open.
