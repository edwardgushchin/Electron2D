# Physics server and direct queries component

## Cold query geometry

CPU/GPU shape-query preparation borrows the using world before evaluating a cold
procedural resource. Caller-created direct-space views and both selected shape-pair
operands use the same numerical/lifetime guard. A supplier cannot release the
final world resource, remove or transfer a collider, or write numerical state.
Its original exception releases the guard for a later retry without changing
caller output. Pure input reads and nested queries retain their existing rules.

The GPU driver resolves cold geometry before selecting or replacing its retained
query cache, so a nested query completes before outer cache preparation starts.
The [public-only query-geometry fixture](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.QueryGeometry.cs)
exercises array/span shape overlap and contacts, motion fractions and rest queries
through selected and caller-created views, both resource-pair operand orders and
body-slot queries. Circle motion checks contain the analytical first-contact
fraction 0.2 within a bracket no wider than 1/256, matching eight binary refinements
(with 0.000001 float slack). Identity, errors and lifetime checks are exact.
Warmed measurements include eleven query routes and a complete moving physics
frame. Evidence and execution limits are recorded in
`bin/physics-query-borrow/2026-10-18/validation.json`.

This protects geometry construction; it does not change ordinary query-hook,
parameter-edit or shared World-wrapper ownership contracts. Complete numerical,
hardware/FPS, native-memory, network and foreign-platform acceptance remain open.


[Terminal world authoring](physics-registered-adapter.md#terminal-world-authoring) now shares common simulation/restore failure validation with numerical body preparation. Motion/role writes reject before authoring; valid healthy callbacks and failed-world cleanup retain their phase/lifetime rules.


[Polygon point containment and ray origins](physics-point-containment.md) now share strict unrounded CPU half-planes across direct queries, Area audio and directed query/proxy regions. Public scene/server CPU/GPU checks preserve filters, logical identities and warmed allocations.

Last updated: 2026-10-20

## Special query displacement

Direct-space IntersectShape, CastMotion, CollideShape and GetRestInfo retain the
represented resource-family dispatch under [ADR 0063](../decisions/physics.md#adr-0063)
and [ADR 0068](../decisions/physics.md#adr-0068). An infinite boundary ignores its
own requested displacement. A concave source ignores displacement except against
a world boundary. A stationary separation ray ignores counterpart displacement.
Two concave resources cannot create contacts from their private segment pieces.
Initial overlap/contact/rest still executes with the current margin; CastMotion
ignores initially overlapping logical slots. Boundary intersection/contact/rest
uses the finite operand endpoint, including departure from the solid half-plane;
finite entry casts retain their first-impact bracket.

CPU retains the query resource kind and fixture tags' represented kind across
private decomposition. Its reverse ray path tests the current counterpart instead
of constructing a swept region; the obsolete private extrusion helpers are removed.
Resident GPU geometry applies the same policy before piece tests. Independent
registered queries keep the policy in their own public-only implementation.
Body motion shares the stationary-ray counterpart correction. A circle exactly
centered on another CPU circle or capsule/segment core now receives a unit side
and matching surface points rather than a zero normal; no arbitrary side is compared across
backends in that degenerate configuration.

The --special-space-motion consumer checks all 64 ordered built-in families across
scene/server targets, two rigid poses, margins 0/.75, forward/reverse controls and
all four direct operations, including array/span overloads. Supplied-pose server
body motion independently checks separated witnesses. Positive initial concave/
boundary contacts prevent an implementation from dropping special resources.
Identity and logical slots are exact; fraction slack is .0002 with an eight-step
1/256 plus .000002 maximum bracket. Rest normals are unit within .001; circle
surface membership allows .02 scene units for float/native composition. Steady
frames use span results and a real moving body; array-return allocation is cold.
Evidence: bin/physics-special-space-motion/2026-10-20/.

The final source-bound cohort passes 120 processes: 57 JIT, 57 strict NativeAOT
and six root profiles. Twelve new matrix profiles measure 1536 full moving frames
at strict zero owner/all-thread managed bytes after 128 warmups each. Six DLL/PDB
sets match 3460 non-generated C# inputs, including vendor, and the linked root
consumer matches the separately qualified consumer. Fresh compilation/validation
of both shaders matches 51 tracked shader/tool inputs and both embedded blobs in
all four runtime/native payloads. No public declaration or wire format changes.
The later [coincident rounded-core fix](physics-contact-policy.md#coincident-rounded-cores)
now places unit-normal and surface-anchor construction in shared native primitives,
covering actual dynamics and removing this query-only correction. The cohort above
remains its earlier frozen query evidence.

This bounded correction does not establish complete custom/coupled motion,
network/native/platform or target hardware/window/performance acceptance. GPU
execution uses private software Vulkan while the power cable remains unchecked.

## Contact reporting write preflight

Common body writes check numerical borrowing before scene participation, selected
status hooks or pending state preparation. Configured reporting reads still use
the non-preparing retained-input path during owner hooks and actual Solve.
The [reporting threshold audit](physics-registered-adapter.md#complete-reporting-threshold-operations)
records scene/raw roles, publication timing, numeric/failure/history checks,
CPU/software-GPU/registered profiles and zero warmed managed bytes.

[Local activation recovery](physics-registered-adapter.md#failed-local-activation) restores the last committed selected flag after partial hook errors, before space query/history or active-step preparation. Metadata, skipped intervals and cleanup retain their non-preparing rules.

[World policy publication recovery](physics-registered-adapter.md#failed-world-policy-publication) retains complete current profiles after partial selected errors, retries before numerical preparation and preserves skipped-interval and cleanup behavior.

The [typed result family](../classes/PhysicsRayResult.md#constructor) now has public construction for ray, point, shape and rest results, plus reusable [motion result filling](../classes/PhysicsTestMotionResult.md#setcollision). One shared engine path validates collider RID/kind, logical shape resource and owner/solver/failure access, then samples the current weak object association. Supplied finite geometry is preserved; construction performs no collision calculation. Motion collisions additionally require different bodies in the same live space and a valid fraction bracket. Invalid writes preserve previous output; SetMotion clears collision state. The separate [public API consumer](../../examples/PhysicsResultConstruction/README.md) exercises both built-ins with zero warmed managed allocation. The [runtime probe](../../tests/Electron2D.Tests/PhysicsResultConstructionTests.cs) additionally verifies rejection across the complete result family while the CPU solver owns the world. This implements a required result boundary under ADR 0103; named registration and typed dispatch now execute; complete numerical-extension and custom geometry acceptance remain open.

[Per-body CCD](cpu-continuous-collision.md) now exposes shared Disabled/CastRay/CastShape policy through RigidBody and PhysicsServer. CPU checks solved trajectories before publication and retains force budgets and frame impulses across impact intervals; the [world adapter](physics-backends.md) also dispatches GPU worlds to their resident CCD path. Full-contract acceptance remains tracked separately.

[Resident body-motion queries](gpu-resident-motion-queries.md) now execute supplied-pose recovery and sweeps on GPU, with moving-mask/target-layer filtering, one-way/ray policies, explicit exclusions and center-aware hit velocity. CPU full-contour recovery and directed containment now avoid internal polygon seams. Public GPU body-motion/CharacterBody binding is covered by the [shared scene motion checks](physics-backends.md#public-scene-motion-conformance).

[Resident shape queries](gpu-resident-shape-queries.md) now execute intersections, contact pairs, deepest rest information and motion brackets over standalone leased geometry on GPU. CPU compound casts and directed-query containment now ignore internal decomposition seams. Public GPU query/world binding uses the same [world adapter](physics-backends.md). The common GPU query driver downloads predicted hit prefixes and any exact missing tails, preserving full caps and avoiding capacity-sized reads for sparse queries.


Process-wide service operations and events use static access to retained objects under [ADR 0095](../decisions/singleton-services.md#adr-0095). Native availability remains explicit through DisplayServer.IsAvailable and RenderingServer.IsAvailable. Independent project registries use ProjectSettingsRegistry; static ProjectSettings operations address only the runtime registry.

## Scope and owned types

Selected motion dispatch now borrows world lifetime for the whole query and validates its completed output before caller replacement or scene movement. Finite travel/contact values, ordered fractions, the requested moving RID, live enabled logical slots, same-space bodies, moving-mask/target-layer filtering, symmetric body/joint exceptions and explicit RID/object exclusions are checked. Collider object identity is sampled by the engine; supplied identity cannot retarget a result. Misses clear collision payload and must report completed motion. Same-space recursive motion rejects because the selected solver owns reusable query scratch; query failures retire borrowing without advancing or poisoning the world. Public registration remains open under ADR 0103.


[`RID`](../classes/RID.md) is the shared opaque identity value. [`PhysicsServer`](../classes/PhysicsServer.md) owns its physics resource registry. [`World`](../classes/World.md) exposes a SceneTree's registered space and [`PhysicsDirectSpaceState`](../classes/PhysicsDirectSpaceState.md). Mutable [`PhysicsRayQueryParameters`](../classes/PhysicsRayQueryParameters.md), [`PhysicsPointQueryParameters`](../classes/PhysicsPointQueryParameters.md) and [`PhysicsShapeQueryParameters`](../classes/PhysicsShapeQueryParameters.md) configure queries. Immutable [`PhysicsRayResult`](../classes/PhysicsRayResult.md), [`PhysicsPointResult`](../classes/PhysicsPointResult.md), [`PhysicsShapeResult`](../classes/PhysicsShapeResult.md) and [`PhysicsRestInfo`](../classes/PhysicsRestInfo.md) carry typed results. [`RayCast`](../classes/RayCast.md) and [`ShapeCast`](../classes/ShapeCast.md) are spatial nodes that sample the same direct view in the fixed physics lane. The server's [`BodyMode`](../classes/PhysicsServer.BodyMode.md) selects explicit body motion.

[`PhysicsTestMotionParameters`](../classes/PhysicsTestMotionParameters.md) and [`PhysicsTestMotionResult`](../classes/PhysicsTestMotionResult.md) form the typed server body-motion query. The scene [PhysicsBody](../classes/PhysicsBody.md) exposes the same sweep through `TestMove` and `MoveAndCollide`, returning [KinematicCollision](../classes/KinematicCollision.md).

`PhysicsDirectBodyState` and PhysicsBodyRuntime now use a versioned attachment
adapter for live state, mass/force application and contact projection. Common code
holds engine-valued mass profiles and contact values, with weak scene ownership;
concrete world/body references and fixture traversal belong to its selected CPU/GPU collider implementation.
The same adapter version qualifies queued callbacks across same-world reentry.
CPU attachments use Box2D.NET and GPU attachments use the resident store;
backend identity belongs to their selected World.

[Independent resident ray/point queries](gpu-resident-queries.md) execute on GPU for public GPU direct-space views; CPU spaces retain their solver queries. Shared CPU/GPU origin-inside tests now cover the complete logical compound slot before casting, avoiding false hits on internal polygon seams. Other collider slots remain eligible. PhysicsQueryTests covers the regression for body/Area queries and warmed zero managed allocation.

## Runtime flow

[PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks all six operations across alternating hit/miss worlds, copied/span results, exclusions, view replacement, empty destinations and owner/solver/freed/failed-world guards. Both CPU and GPU execute 64 warmed complete-query cycles at 0/0 owner/all-thread managed bytes. The no-device child executes the CPU query family without a renderer or graphics device. Query identity and lifetime are exact; the circle ray check allows .05 scene units for native/resident geometric rounding, and a cast must bracket the first new hit. These checks do not establish native allocator, foreign-platform or real-window FPS acceptance.

All six direct-space operations now dispatch through the owning PhysicsWorldBackend: ray, point, shape intersection, motion fractions, contact pairs and rest information. The retained public view validates lifetime, owner thread and solver/failure access and prepares pending authored edits before dispatch. Array/span overloads share the same implementation results and preserve caps, ordering, physical RID/object identity and untouched destination tails. CPUPhysicsWorldBackend owns native scans and their retained geometry/result scratch; GPUPhysicsWorldBackend owns resident-query projection and result scratch, using the existing PhysicsSpace GPU query driver and leased geometry. Reopening a disposed view uses the same selected implementation; different spaces retain independent scratch. Public named backend registration and guarded caller-created extension/exclusion hooks now execute under ADR 0103; their complete custom numerical/geometry acceptance remains separate.

A World lazily registers its selected backend space when physics is needed. Each CollisionObject owns a stable RID from construction to disposal; rebuilt backend geometry keeps that RID and its logical shape-owner index. The server also creates explicit spaces, bodies, Areas and shapes. A server collider may attach to an explicit space or the SceneTree space. Explicit spaces advance through `SpaceStep`; a SceneTree advances its own space in the fixed physics lane. Freeing a resource removes its registry entry without reusing the numeric RID.

Direct ray and point views prepare pending scene geometry/poses, then query current body, Area and explicit-server geometry through the selected implementation. Query masks inspect collider layers independent of those colliders' masks; optional flags include sensors or bodies, RID arrays exclude objects, and results retain both RID and scene object where one exists. Server-only results have a null scene collider; their sampled object identity follows any explicit object association. Ray hits choose nearest fraction with RID/index tie order. Point hits sort and deduplicate by RID/index before applying the result cap. A direct view cannot query during solver stepping or off the space owner thread.

Shape views use the same prepared fixtures and layer rules. A query Shape is borrowed by the server through a lazily allocated RID; its owner releases that identity on disposal, and edits invalidate server fixtures before the next query. An explicit server shape RID works without a managed resource reference. `IntersectShape` includes shapes touched along `Motion`, sorting and deduplicating by RID/index. `CastMotion` ignores initial overlaps and refines safe/unsafe fractions around the first new hit. `CollideShape` returns paired contact points; `GetRestInfo` returns the deepest contact's collider point, outward normal and point velocity. Both scene and server-only shapes participate, including compound convex and hollow segment pieces.

`BodyTestMotion` scans registered body fixtures with the moving mask against target layers, body RID and scene instance-ID exclusions, and a finite supplied starting pose. Four recovery attempts move an initially penetrating test pose out of contact; the sweep then refines the first impact in eight steps. A residual deep overlap reports a zero safe fraction. One-way fixture direction and margin gate pass-through and recovery. Areas are sensors and do not block. The actual scene/server body stays in place; the caller-owned result records safe/unsafe fractions, contact and travel. Separation rays participate in directed recovery, flag-controlled sweeps and ordinary directed dynamic response under ADR 0068.

Body-owned collision exception RIDs also exclude pairs from motion tests and fixed-step contacts when either side lists the other. The typed scene methods accept PhysicsBody references; server methods accept opaque RIDs and can store an inert value. An exception edit rebuilds the owner's fixtures before the next test or step, including for an already touching pair. Direct ray, point and shape queries continue to use their own explicit exclusion arrays.

An enabled RayCast derives global endpoints from its local target and node transform, uses its cached RID exception array, and calls the direct ray view during internal physics processing before the solver step. It retains the result until another automatic or forced sample. Disabling clears only the collision flag; a miss clears collider identity while leaving the previous point and normal readable. Forced sampling can run while disabled. Direct-parent exclusion, layer bits, Area/body flags and inside hits reuse the direct query contract. Index-based fixture scans avoid managed boxing in 64 warmed active ray frames on Linux/.NET 8.

An enabled ShapeCast borrows a Shape resource and converts its local target into global motion. It first computes safe/unsafe motion fractions, then queries contacts at the earliest impact pose, excluding each reported collider RID until the result cap is reached. Zero target samples the current pose. Forced sampling works while disabled; disabling clears only its collision flag. Shape, target, margin, cap and filters are stored in PackedScene, while exclusions and results are transient. A reused direct parameter object, RID scratch array and contact lists keep 64 warmed active scene frames free of managed allocation on Linux/.NET 8.

## Dependencies, invariants and limits

The server builds on existing Shape resources, SceneTree owner-thread rules and the existing per-World internal Box2D.NET spaces. Its shape creation families include the analytic world boundary; extension-defined custom geometry remains separate work; joint resource lifetime and kernels execute through the shared runtime. Canvas/navigation RIDs and independent/shared viewport worlds execute. Scene query debug drawing and the remaining server methods are recorded in the [contract audit](physics-contract-audit.md). Server-only colliders share query/solver state but are not represented in current scene Area object-event arrays or server-only Area field configuration. Query scans are linear in fixture count; warmed unchanged casts and rest queries allocate no managed memory, while per-call result arrays, native allocation, other platforms and large-world throughput have not been audited.

[PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) checks RID and ray/point behavior. [PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) checks resource/RID shape selection and four direct operations. [PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) checks body sweeps; [PhysicsCollisionExceptionTests](../../tests/Electron2D.Tests/PhysicsCollisionExceptionTests.cs) checks pair suppression in motion and regular solver contact. [RayCastTests](../../tests/Electron2D.Tests/RayCastTests.cs) and [ShapeCastTests](../../tests/Electron2D.Tests/ShapeCastTests.cs) check scene node timing and lifecycle. Physics debug-gizmo drawing and virtual tile collision-object results remain separate dependencies. [ADR 0063](../decisions/physics.md#adr-0063) defines the ownership and query contract.

Separation rays execute directed direct queries, current-counterpart reverse queries and special body-motion recovery. CollideSeparationRay defaults false, includes non-sliding rays only in the motion phase, and leaves recovery participation unconditional. Sliding rays participate automatically; CharacterBody snap explicitly enables all rays. Shared motion results reconstruct the collider-side point from the manifold. Ordinary CPU dynamic ray impulses now use directed alternative manifolds under [ADR 0068](../decisions/physics.md#adr-0068), without changing query visibility or motion flags.

The internal PhysicsSeparationRay kernel owns directed contact and current counterpart containment; immutable SeparationRayData in PhysicsFixtureTag carries exact local endpoints and slope policy across fixture rebuilds. Both are private runtime integration details shared with PhysicsSpace.

[Shape collision methods](../classes/Shape.md#collide) are independent resource-pair geometry operations under [ADR 0069](../decisions/physics.md#adr-0069). They do not create a direct-space query or consult scene filters. Their motion evaluates independently swept regions; concave terrain motion is ignored and rays ignore counterpart motion. Direct-space queries preserve the same represented-family precedence, including finite boundary endpoints.

[PhysicsDirectBodyState](../classes/PhysicsDirectBodyState.md) and RigidBody custom integration now expose live owner-thread fields and immutable solved contact values after native stepping. PhysicsBodyRuntime owns body callback/persistent-force configuration; PhysicsSpace snapshots callback entries, aggregates failures and resynchronizes scene state. Typed userdata adapters allocate at registration. Sixty-four warmed active callback frames allocate zero managed bytes on Linux/.NET 10; whole-step normal/tangent impulses are complete; virtual tile identities remain Partial under [ADR 0070](../decisions/physics.md#adr-0070).

[CollisionObject shape-owner groups](../classes/CollisionObject.md#createshapeowner) unify manual borrowed resources and direct collision children. Sorted group IDs are separate from append-order global logical shape slots; structural removals reindex later slots. Body/Area preparation uses slot resource revisions, owner pose/policy and shared native fixture construction. Arbitrary weak owner identity reaches KinematicCollision results. ShapeOwnerTests verifies query/motion/solver/sensor integration on explicitly selected CPU/GPU worlds and 64 warmed full frames with zero owner/all-thread managed bytes under [ADR 0071](../decisions/physics.md#adr-0071).

Logical shape-pair monitoring now uses a shared PhysicsShapePairTracker and committed transition queues. Native compound fixtures are deduplicated by RID and remote/local global slot indices; object/shape entry and departure ordering is preserved. Scene Area scans include server colliders with nullable payloads and explicit server Area monitorability. Callback failures continue later queued events; monitor policy changes inside in/out callbacks reject. ShapePairEventTests checks steady and active entry/exit paths without warmed managed allocation. [Physics monitoring decisions](../decisions/physics-monitoring.md) now own active ADRs 0055/0058; stable legacy anchors route there.

## Shared body mass profile

[Resident GPU mass](gpu-resident-mass.md) now reuses the same authoring calculation
without a CPU solver world. Rotated detached profiles and attached CPU fixtures use
the same authored unit basis; analytic asymmetric polygon/line tests cover this
consistency. Public GPU-world selection remains unconnected.

[RigidBody](../classes/RigidBody.md#centerofmassmode) stores automatic/custom local center and zero/explicit scene-unit inertia. Typed [PhysicsServer](../classes/PhysicsServer.md) mass methods execute for scene and server identities, using one-kilogram normalization instead of raw fixture density. The internal [PhysicsMass](../../src/Servers/Physics/PhysicsMass.cs) calculation validates before profile/native commit, measures native solid primitives independently of body density, applies parallel-axis inertia or the segment-only rod fallback, and refreshes native shape extents around the selected center. The body runtime retains configured values and resolved geometry separately from static inverse values. Mode/reset property-list callbacks run after commit; profile changes leave scene origin and velocity unchanged. [PhysicsMassProfileTests](../../tests/Electron2D.Tests/PhysicsMassProfileTests.cs) covers force response, shared lifecycle, numeric/thread/phase rejection and 64 warmed changes plus solver frames with zero managed allocation on Linux/.NET 10. Native allocation, other platforms and owner acceptance remain unverified. [ADR 0073](../decisions/physics-mass.md#adr-0073) owns the mass policy and typed parameter projection.

## Shared force state

The body runtime now owns one-step pending force/torque and routes scene/direct-view actions through all 13 typed [PhysicsServer](../classes/PhysicsServer.md) force/impulse methods. Detached impulses resolve the same mass primitives from reused resource proxies, without a world/body allocation. Pending values survive removal/static/dormant participation, respect the latest Sleeping assignment and are consumed once on eligible integration; omission discards them, while constant totals remain configured. Removal captures current dynamic velocity. Native fixture creation skips intermediate automatic mass updates so attach/rebuild cannot inject a center-shift velocity. [PhysicsServerForceTests](../../tests/Electron2D.Tests/PhysicsServerForceTests.cs) checks lifecycle, units, every primitive family, numeric/phase/thread rejection and 64 warmed attached/detached actions plus solver frames with zero managed allocation on Linux/.NET 10. Native allocation, other platforms and visual acceptance remain unverified. [ADR 0074](../decisions/physics-forces.md#adr-0074) owns this contract; axis velocity remains separate body-state work.

## Complete typed body parameters

[PhysicsServer](../classes/PhysicsServer.md) now connects all ten material/mass/field parameter capabilities to concrete typed getter/setter pairs. Signed coefficients use actual fixture material and rough/absorbent combines; per-body writes isolate borrowed resources and reload on material revision/disposal. The internal [runtime field policy](../../src/Servers/Physics/PhysicsBodyRuntime.Parameters.cs) scales selected gravity and resolves Combine/Replace signed damping before automatic motion, validating totals and preserving wakeup/omission/role semantics. Scene RigidBody descriptors remain bidirectional; CharacterBody and direct state report matching scaled gravity. [PhysicsBodyParameterTests](../../tests/Electron2D.Tests/PhysicsBodyParameterTests.cs) checks actual contact/field response, ownership and failure paths, with 64 warmed field/read/solver iterations at zero managed allocation on Linux/.NET 10. Native allocation, other platforms and owner acceptance remain unverified. [ADR 0076](../decisions/physics-mass.md#adr-0076) owns typed selector adaptation and current behavior.

## Server Area sensor receivers

Typed [PhysicsServer](../classes/PhysicsServer.md) body/Area monitor callbacks now use the existing exact overlap kernel for scene/server receiver identities. The internal [Area runtime](../classes/PhysicsAreaRuntime.md) stores optional delegates, logical pair snapshots and epochs; the [space scanner](../../src/Servers/Physics/PhysicsSpace.AreaMonitoring.cs) commits before callback dispatch, filters other layers directionally and retains monitorable gating. Payload includes other RID, stable scene InstanceID or zero, and both logical indices. Receiver reentry resets history while retaining registration; peer removal/free, reentrant removal and callback failures drain without stale entry replay. Scene snapshots remain independent. [PhysicsAreaMonitorTests](../../tests/Electron2D.Tests/PhysicsAreaMonitorTests.cs) checks 64 warmed entry/exit cycles with zero managed allocation on Linux/.NET 10. Native allocation, large-world performance, other platforms and owner acceptance remain unverified. [ADR 0077](../decisions/physics-monitoring.md#adr-0077) owns current semantics; [typed server Area fields](../classes/PhysicsServer.md#typed-area-fields-and-space-defaults) execute through their own shared profile.

Typed PhysicsServer Area field getter/setters now share scene/server profiles and address a space default Area by its Space RID. Bounded fields use current exact overlap and descending mixed priority; monitor flags/callbacks are independent. Initial space defaults sample ProjectSettings, then explicit live edits apply on the next nonzero step without changing other worlds. Point/default gravity, body damping/scaling, wakeup and failure recovery are verified by [PhysicsServerAreaFieldTests](../../tests/Electron2D.Tests/PhysicsServerAreaFieldTests.cs) under [ADR 0056](../decisions/physics-fields.md#adr-0056). Native allocation, other platforms and owner visual acceptance remain unverified.

Shared [scene/server joint resources](physics-joints.md) use the existing physics world and native body IDs. Stable joint identities, typed settings, pending connection lifetime and independent contact/motion exception contributions now execute under [ADR 0087](../decisions/physics-joints.md#adr-0087).

[Indexed PhysicsServer geometry](../classes/PhysicsServer.md#shape-slots) now shares the scene/server logical slots, effective local poses and native fixture/query path under [ADR 0088](../decisions/physics-shape-slots.md#adr-0088). Raw replacement/pose/disabled/one-way edits do not rewrite child configuration; group/child edits reclaim corresponding overrides. Shape free/replacement follows shared RID/view ownership and related-world phase guards. Effective poses also feed mass geometry. [PhysicsServerShapeSlotTests](../../tests/Electron2D.Tests/PhysicsServerShapeSlotTests.cs) verifies real geometry, body/Area lifetime and one-way contacts/motion on explicitly selected public CPU/GPU worlds. The [paired collider runner](physics-backends.md#paired-complete-collider-runner-2026-10-16) also checks serial/separate policies and zero owner/all-thread bytes in warmed indexed edit/step intervals.

[Physics world activity](../classes/PhysicsServer.md#activity) is now independent of scene scheduling under [ADR 0089](../decisions/physics-activity.md#adr-0089). SceneTree activates its lazily created world; explicit SpaceCreate defaults inactive and requires SpaceSetActive(true). Global/local false skips simulation, force consumption and solver callbacks without clearing native state or accumulating elapsed time. Queries/configuration/cleanup and scene callbacks/timers continue. [PhysicsActivityTests](../../tests/Electron2D.Tests/PhysicsActivityTests.cs) checks the profile and warmed allocation on Linux/.NET 10.

Point, shape-intersection and shape-contact queries also accept caller-owned spans. Their bounded outputs share ordering, deduplication, filtering and units with the copied-array overloads; only full contact pairs are written. Scratch buffers and copied exclusions are retained across repeated calls. `PhysicsShapeQueryTests` checks active/miss zero managed allocation after preparation and all output boundary cases. Character slide getters have reusable `KinematicCollision` destination overloads under the same ownership rule.

## Typed body state

All five transform/linear/angular/sleep/can-sleep state branches and axis velocity
now accept scene/server RIDs. Direct views reuse the same state paths. Raw kinematic
targets defer after their initial pose and traverse the shared subdivided solver
path; constant velocity affects contacts without moving the target. Sleep and
pending targets survive reentry. PhysicsServerStateTests verifies real CPU/GPU
motion, lifecycle, guards and 64 warmed state/read/step cycles with zero all-thread
managed bytes. [The server reference](../classes/PhysicsServer.md#body-state)
records units, errors, scene role policies and remaining verification limits.

WorldBoundaryShape participates analytically in both query argument positions,
including far-away half-plane contacts, normal/distance transforms and ray pairs.
The shared shape and body-motion matrices now include this geometry. Direct-space
shape queries retain initial overlap during motion; body-motion recovery/casts
retain their own directed-ray policy. Public independent-GPU binding remains open.


## Canvas association

PhysicsPointQueryParameters.CanvasInstanceID and the Body/Area attach/get server
operations share retained metadata in PhysicsColliderBackend. Scene canvas
notifications bind the nearest CanvasLayer instance or zero, including reparenting;
physics disable removal does not remove canvas membership. Raw server edits are
immediate and survive reattachment. Zero is an exact default-canvas selector, not
an all-canvas wildcard. Selection uses physical world coordinates even when a
CanvasLayer has a visual offset. Separate World identities still bound every query.

PhysicsCanvasTests now selects the actual public CPU/GPU owner for scene and raw
fixtures through the [paired runner](physics-backends.md#paired-complete-collider-runner-2026-10-16).
The legacy GPU bool profile remains a direct resident-store comparison with a CPU
oracle; its transfer counters describe that probe only. The common public profile
checks retained direct views and physical state after metadata edits, exact
filters and both managed allocation counters.

PhysicsCanvasTests exercises four overlapping raw colliders (two bodies and two
Areas), default and high-bit 64-bit associations, same-low-bits nonmatching keys,
layers, kinds, exclusions, stable caps, live edits, attachment, wrong-kind/freed RID,
thread and solver guards. Scene checks cover entry/exit, reparent, overrides,
sleep preservation, disabled participation, shared/independent worlds and Area
monitoring across canvas layers. The independent GPU query kernel now uses the
same exact selector and leaves ray queries unrestricted by canvas. A regression
check failed on the old zero-as-wildcard GPU path before this correction.

Linux/.NET 10 Release, Vulkan/NVIDIA GeForce RTX 3090 Ti, 128 warmup/128 samples:
the focused four-collider test changes one body's canvas then queries the complete
population. CPU p50/p95/p99 was 0.0003/0.0003/0.0004 ms. The GPU measurement, including
the public CPU oracle and exact result comparison, was 0.0644/0.2144/0.3443 ms,
with 184 B upload, 268 B readback and 0.0599 ms mean wait per query. Both intervals
allocated 0/0 owner/all-thread managed bytes. These small query costs do not claim
whole-step GPU advantage or window FPS. Evidence: `/tmp/e2d-canvas-cpu.log` and
`/tmp/e2d-canvas-gpu.log`.

Run `ELECTRON2D_TEST_PHYSICS_CANVAS=1` or
`ELECTRON2D_TEST_PHYSICS_CANVAS_GPU=1` with the Release test runner; the complete
collider and GPU suites also include the respective checks. Public independent-GPU
binding, picking and networking have separate acceptance records. Square-atlas tile collision generation and owner queries now execute through TileMapLayerTests; broader tile capabilities remain open. Native allocations, other devices/platforms and rendered acceptance were
not measured by these headless tests.

The complete collider suite and complete GPU suite passed after integration:
`ELECTRON2D_TEST_COLLIDER_BACKEND=1` and `ELECTRON2D_TEST_GPU_PHYSICS=1` with
`tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll`.
Logs: `/tmp/e2d-canvas-colliders.log`, `/tmp/e2d-canvas-full-gpu.log`.

[Object associations](physics-object-bindings.md) now supply typed Body/Area bindings and sampled weak identities across queries, motion, contacts and scene/server monitoring.

[Directional filter mutation](physics-filters.md) shares scene/server/tile metadata and query rules across CPU/GPU. It updates existing fixtures without rebuilding geometry; body assignments wake contact neighbors even for unchanged bits.

[Report-only contact verification](physics-report-only.md) now covers nonresponding kinematic/static and kinematic/kinematic pairs, current receiver caps, identity, lifecycle and restoration on both public backends. Existing contact storage and publication are reused; no CPU body-state mirror is added to GPU physics.

[Caller-created direct-space extensions](../components/physics-space-extensions.md) now execute all six typed query hooks through inherited public array/span/scalar operations. The library owns bound-space guards, nested exclusions, scratch lifetime and validated result publication. A separate public-only consumer computes real scene/raw/Area circle queries on CPU/GPU and checks lifecycle, failures and warmed allocation. Public registered factories and direct-body extensions now execute under ADR 0103; complete independent numerical/custom-geometry acceptance remains open.

[Caller-created body-state extensions](../components/physics-body-extensions.md) now execute the complete typed state/force/space/contact family. Body context is qualified by current attachment generation; nested hooks borrow body/world lifetime and preserve ordinary live mutation. Contact projection uses immutable engine-validated PhysicsBodyContact values. Registered server factories and factory-returned scene/server callback integration remain open under ADR 0103.

[Selected shape-pair queries](physics-shape-pairs.md) provide the internal resource/resource and body-slot operation family required by the complete server-extension bridge. Attached scene/raw bodies select their owner; detached bodies require no solver. Both built-ins use exact authored resource geometry and common result/lifetime guards.

The complete PhysicsMotionTests, PhysicsQueryTests and PhysicsShapeQueryTests now use explicitly selected public CPU/GPU worlds. The common suites retain scene/raw identities, view replacement and last-resource retirement, filters/exclusions, compound origin-inside and initial-overlap rules, hollow/segment/convex contact families, one-way recovery, motion fractions/travel and scene shape-owner payloads. Existing numerical bounds are unchanged. Warm gates include both owner/all-thread managed counters: 64 scene-motion, compound recovery/ray/cast/rest queries and 128 alternating span-query cycles require 0/0 bytes. Caller-owned World aliases may outlive the SceneTree; its colliders retire immediately and the view expires after the last World resource is released. Native allocations, foreign platforms, visual and final performance acceptance remain open.

## Compound geometric query verification

Built-in shape casts now target geometric boundaries in CPU conservative advancement and independent resident GPU query/motion sweeps, rather than a penetration-reduced solver target. Disk queries and linear-disk CCD share the CPU analytic fraction kernel; other represented proxies use geometric distance with 1e-5 scene-unit convergence tolerance. The internal CPU cast explicitly selects this query policy while native solver callers retain their existing policy. Sharp zero-radius contact retains the preceding finite normal. CastMotion keeps eight refinements: analytic first touch must lie between safe/unsafe with maximum width 1/256. The rejected disk 10/12 case returned .87109375/.875; the rejected rectangle .4 case returned .421875/.42578125. Ten circle/segment query combinations against rectangle, capsule, segment, convex and concave targets now verify rounded and sharp boundaries; each runs 128 warm queries at strict 0/0 managed bytes. Bit equality across backends is not required.

See [the shared compound checks](physics-registered-adapter.md#compound-slot-numerics) for independent-provider and CPU/GPU response, identity, Area, CCD and zero warm allocation evidence.

[Retained slot history](physics-snapshots.md#capture-after-topology-edits) now samples
private logical-slot lifetimes for contacts, overlaps and collided motion. Capture
rejects removal/reindex/readdition before replacing bytes while preserving sampled
public data and events. In-slot replacement stays valid. CPU/registered serial/
separate twelve-case verification executes; destination apply remaps tokens, which
never enter wire data. Current GPU/native and broader topology acceptance stay open.


[Borrowed motion output](physics-registered-adapter.md#borrowed-motion-output-2026-10-14) preserves selected scratch lifetime and owner scope while retaining caller-owned snapshots, error continuation and retirement failures. The inherited disposal preflight is a reviewed C# adaptation; full numerical/foreign/hardware acceptance stays open.

## Failed raw shape edits and warmed reuse

Attached raw Body/Area add, replacement, local pose, enabled state, removal and
clear operations synchronously rebuild the selected numerical attachment. Body
one-way edits use the same transaction. A failed rebuild restores the exact prior
logical slots, including their private history identities, then attempts numerical
restoration. Successful restoration rethrows the original error. If restoration
also fails, an AggregateException retains the original and restoration errors in
that order. Geometry remains dirty until preparation succeeds; a subsequent query
or step retries before publishing results. This does not turn an authoring error
into a failed numerical step. Scene slot edits retain their existing deferred
preparation, and freeing a shape commits resource retirement rather than restoring
its removed uses.

Clear uses a collider-local backup array, grows only for new slot capacity and
clears every saved reference in finally. CPU attachments retire all old fixtures
before rewriting their private reusable tags. Compound pieces retain logical
indices, their own weak source references and a shared current contour pose;
directed pieces refresh their ray data. World-local retired sensor shells retain
cleared visitor-array capacity, and world destruction releases both active and
retired sensor arrays. GPU sensor slots reuse empty query-lease wrappers while
releasing geometry on every clear/detach. Ordinary query leases retain terminal
disposal; only the owning collider cache can rebind its released wrapper. These
caches introduce no host simulation-state mirror.

OneWayContactData remains an immutable public record. Scene/raw slots reuse the
last resolved direction/margin value when unchanged. A changed resolved value
publishes a new record; previously borrowed values remain unchanged. Thus rotated
one-way authoring has a separate explicit publication allocation, while warmed
PhysicsFrame intervals retain their strict zero-managed-byte requirement.

The public-only `--shape-edits` consumer exercises both raw roles through the real
selected CPU, GPU or registered attachment. Fifteen successful operations cover
all seven shared edit kinds plus body one-way policy. Its registered provider
injects 45 failures, including partially changed numerical geometry, failed
restoration and another failed preparation retry. Queries check exact geometry
and logical indices, association checks retain full 64-bit canvas/object identity,
and subsequent physical frames check stable sensor effects. Retired geometry is
checked through weak references for both body and Area clears.

The allocation profile retains 128 warm and 128 measured edit/query/full-frame
cycles, moving a real body by 12.8 units within .01 units and delivering 128 force
callbacks. A second profile repeats rotating body/Area edits and compound queries
for all eight built-in shape families, including a twelve-vertex convex contour,
paired concave edges, infinite boundary and directed ray. Rays query a surface
entry rather than an invalid start-inside hit. Every measured physics step has a
strict 0/0 owner/all-thread managed-byte assertion. The complete rotating authoring
interval permits exactly one independently calibrated immutable policy object per
frame, with no other allocation. See the current validation receipt under
`bin/physics-shape-edit-failures/2026-10-16/`; native/driver budgets, arbitrary custom
solvers, foreign platforms and target-device/window performance remain open.

The final local run passed twelve fresh CoreCLR/NativeAOT processes across
CPU/software-GPU/registered serial/separate profiles, fifteen existing consumer
resource/owner/Area/geometry regressions and six CPU/software-GPU scene/shared/
portable-snapshot profiles. CPU used an absent Vulkan ICD; GPU used pinned
lavapipe, and sampled process maps loaded no hardware driver. The basic edited
interval fell from 430,080 CPU / 20,480 GPU bytes per 128 frames to 0/0; each
rotating profile measured 4,096 authoring bytes plus 0/0 physical-step bytes.
These are functional/allocation results, not a native-memory or GPU-speedup claim.

## Area observer registration transactions

AreaSetMonitorCallback and AreaSetAreaMonitorCallback share PhysicsAreaRuntime.
For attached receivers, registration first prepares all common/backend monitoring
capacity while the previous callbacks and pair histories are still current. Only
a successful preparation publishes the replacement and clears both histories,
including same-delegate and null assignments. Preparation needs collider/shape
capacity, not a prospective callback value. A failure propagates its original
exception without a second fallible rollback; the old delegates, logical pairs,
generation and already queued deliveries remain intact. Retained capacity may
have grown before failure. This is an authoring failure, not a started solve.

A successful registration still invalidates older queued entries immediately.
Current overlaps reenter at the next nonzero scan, while scene-owned snapshots
remain independent. Cross-receiver registration from an ordinary callback keeps
these epoch rules; own-receiver configuration and numerical-hook writes retain
their existing guards. Detached registration has no numerical capacity hook and
still commits immediately. Disposing/releasing owners retains the existing
callback/history cleanup.

The public-only `--area-callback-transactions` profile drives both callback lanes
on scene and raw receivers with enabled logical slots 0/2 and a disabled gap.
Two body peers include scene and assigned raw identities; a monitorable Area peer
supplies the other lane. It checks first publication, clear/same/new handlers,
zero-delta timing, independent scene snapshots, exact entry/exit counts and
successful versus failed publication while later receiver events are queued.
The registered implementation injects 24 capacity failures, including four weak
candidate-target checks. Failed replacements preserve both lanes, and physical
departure/reentry reaches the prior handler. The shared warm interval re-registers
all four observers and reads live overlap/shape state during 128 full frames,
retaining strict 0/0 owner/all-thread managed-byte gates, 128 force callbacks,
stable RID and 12.8 units of real motion within .01 units.

The receipt lives under `bin/physics-area-callback-transactions/2026-10-16/`.
This verifies observer publication through the actual selected implementation;
arbitrary numerical/custom geometry, native/driver budgets, foreign platforms,
separate-process networking and hardware/window performance remain separate gates.

The final local run passed twelve CoreCLR/NativeAOT serial/separate profiles on
CPU, software GPU and the registered implementation, six existing Area-monitor/
resource consumer regressions, and the complete Area contract selector on both
CPU and software GPU. Every new warm registration interval measured 0/0 managed
bytes. CPU used an absent Vulkan ICD; GPU used pinned lavapipe with no hardware
driver observed.

## Scene cast metadata and cached observations from groups

RayCast and ShapeCast options, borrowed query shape and RID exceptions permit the scene owner/current processing group. Capture, disposed nodes and foreign/ordinary worker mutations reject before edits, including equal assignments. Cached result getters permit the owner or any active group with caller coordination. Separate reads are not an atomic snapshot. CollisionResult remains an allocating caller-owned copy; its physical scene owner and sampled weak object identity remain distinct. Invalid options and result indices keep their existing exceptions; PackedScene stores options, without results or explicit exceptions.

Exception edits grow retained arrays only when capacity is insufficient and clear unused entries before copying the current set. Add/remove/clear and parent exclusion edits reuse warmed storage; empty RID tail slots do not exclude live colliders. Forced sampling remains synchronous and owner-only, including when disabled. Enabled automatic sampling in a SubThread group fails its numerical access guard; an InternalPhysicsProcess notification does not schedule a delayed replacement query. Later notification receivers still run and retain collected original errors. Games may keep automatic sampling in a main group, or disable the internal lane and sample on the owner before workers consume the held results.

This numerical boundary is present in the pinned reference: [PhysicsServer2DWrapMT.space_get_direct_state](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/physics_2d/physics_server_2d_wrap_mt.h#L114-L118) rejects non-main callers; [the concrete server](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_physics_server_2d.cpp#L278-L283) also checks the synchronized/unlocked interval. [RayCast sampling](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/2d/physics/ray_cast_2d.cpp#L184-L217) and [ShapeCast sampling](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/2d/physics/shape_cast_2d.cpp#L268-L318) call that view immediately. The prior general-propagation coverage description incorrectly treated worker cast sampling as missing behavior. It is a retained query restriction; cached logical observations and local options required the actual implementation change.

Public root/JIT/strict NativeAOT CPU/software-Vulkan/registered ordinary/pool-owner profiles exercise all options and exception operations, complete held results, matching selected owner queries, capture/packing, numeric notification errors and lifetime. Full warmed moving physics/query frames include repeated metadata and exception edits at a strict zero owner/all-thread managed budget. Native Wayland GPU/compatibility windows exercise worker observations and option edits, owner queries and hit/miss diagnostic pixels. Artifacts: `bin/physics-group-casts/2026-10-18/validation.json`. Native allocation totals, target hardware performance and foreign platforms retain their separate gates.

Final group-cast verification: 36 focused root/JIT/strict NativeAOT profiles measure 4608 complete moving physics/query frames with repeated option/exception edits at strict 0/0 owner/all-thread managed bytes. Twenty-four related profiles and eight new native profiles pass, with 16 retained Wayland captures. All 3264 authored inputs match five explicit Compile/PDB cohorts (2299 runtime, 786 root tests, 188 public consumer). Compiled API remains 14635 declarations/875 types, unchanged from main `13a2c225`. The same consumer reproduces the former cached-read owner rejection over that baseline runtime. One mixed CPU/GPU diagnostic suite initially ran with Vulkan disabled by the harness; the preserved setup failure is excluded from success counts and the corrected software-Vulkan rerun passes. General notification coverage remains Partial and the physical census stays 196 open.


[Complete axis velocity](physics-registered-adapter.md#complete-axis-velocity-operation)
shares scene/server current-motion projection, preserving unsynchronized perpendicular
impulses and inactive native future configuration. Exact configured values, bounded
live error, nine body owners/four direct modes, publication recovery and 1536 complete
warmed frames at 0/0 owner/all-thread managed bytes pass in the 58-profile
JIT/NativeAOT CPU/software-GPU/registered cohort. This closes the extension axis
operation; broader numerical, networking and hardware acceptance remain open.


## Selected logical query origins and ordering

The [direct-space view checks](physics-registered-adapter.md#complete-direct-space-view-operation)
verify six public query families over selected moving worlds. The independent
provider ignores complete initially intersecting logical slots, including all
query/target pieces, resolves equal ray hits by RID/index and orders contact rows
before caps. Rest contacts retain their own numerical geometry after sorting.
CPU/software GPU share the public assertions; every 128-frame warmed bracket
requires strict zero owner/all-thread managed bytes. Arbitrary custom/coupled
simulation and hardware performance acceptance remain separate.


[Special body-motion sources](physics-registered-adapter.md#special-source-motion)
now retain canonical geometry kind across private CPU pieces, resident GPU geometry
and independent provider parts. All-family public motion, recovery/residual and
live replacement checks retain logical indices and zero warmed frame allocations.
