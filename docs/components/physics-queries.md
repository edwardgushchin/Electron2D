# Physics server and direct queries component

Last updated: 2026-10-10

The [typed result family](../classes/PhysicsRayResult.md#constructor) now has public construction for ray, point, shape and rest results, plus reusable [motion result filling](../classes/PhysicsTestMotionResult.md#setcollision). One shared engine path validates collider RID/kind, logical shape resource and owner/solver/failure access, then samples the current weak object association. Supplied finite geometry is preserved; construction performs no collision calculation. Motion collisions additionally require different bodies in the same live space and a valid fraction bracket. Invalid writes preserve previous output; SetMotion clears collision state. The separate [public API consumer](../../examples/PhysicsResultConstruction/README.md) exercises both built-ins with zero warmed managed allocation. The [runtime probe](../../tests/Electron2D.Tests/PhysicsResultConstructionTests.cs) additionally verifies rejection across the complete result family while the CPU solver owns the world. This implements a required result boundary under ADR 0103; manager registration, custom geometry and extension dispatch remain open.

[Per-body CCD](cpu-continuous-collision.md) now exposes shared Disabled/CastRay/CastShape policy through RigidBody and PhysicsServer. CPU checks solved trajectories before publication and retains force budgets and frame impulses across impact intervals; the [world adapter](physics-backends.md) also dispatches GPU worlds to their resident CCD path. Full-contract acceptance remains tracked separately.

[Resident body-motion queries](gpu-resident-motion-queries.md) now execute supplied-pose recovery and sweeps on GPU, with reciprocal masks, one-way/ray policies, explicit exclusions and center-aware hit velocity. CPU full-contour recovery and directed containment now avoid internal polygon seams. Public GPU body-motion/CharacterBody binding is covered by the [shared scene motion checks](physics-backends.md#public-scene-motion-conformance).

[Resident shape queries](gpu-resident-shape-queries.md) now execute intersections, contact pairs, deepest rest information and motion brackets over standalone leased geometry on GPU. CPU compound casts and directed-query containment now ignore internal decomposition seams. Public GPU query/world binding uses the same [world adapter](physics-backends.md). The common GPU query driver downloads predicted hit prefixes and any exact missing tails, preserving full caps and avoiding capacity-sized reads for sparse queries.


Process-wide service operations and events use static access to retained objects under [ADR 0095](../decisions/singleton-services.md#adr-0095). Native availability remains explicit through DisplayServer.IsAvailable and RenderingServer.IsAvailable. Independent project registries use ProjectSettingsRegistry; static ProjectSettings operations address only the runtime registry.

## Scope and owned types

[`RID`](../classes/RID.md) is the shared opaque identity value. [`PhysicsServer`](../classes/PhysicsServer.md) owns its physics resource registry. [`World`](../classes/World.md) exposes a SceneTree's registered space and [`PhysicsDirectSpaceState`](../classes/PhysicsDirectSpaceState.md). Mutable [`PhysicsRayQueryParameters`](../classes/PhysicsRayQueryParameters.md), [`PhysicsPointQueryParameters`](../classes/PhysicsPointQueryParameters.md) and [`PhysicsShapeQueryParameters`](../classes/PhysicsShapeQueryParameters.md) configure queries. Immutable [`PhysicsRayResult`](../classes/PhysicsRayResult.md), [`PhysicsPointResult`](../classes/PhysicsPointResult.md), [`PhysicsShapeResult`](../classes/PhysicsShapeResult.md) and [`PhysicsRestInfo`](../classes/PhysicsRestInfo.md) carry typed results. [`RayCast`](../classes/RayCast.md) and [`ShapeCast`](../classes/ShapeCast.md) are spatial nodes that sample the same direct view in the fixed physics lane. The server's [`BodyMode`](../classes/PhysicsServer.BodyMode.md) selects explicit body motion.

[`PhysicsTestMotionParameters`](../classes/PhysicsTestMotionParameters.md) and [`PhysicsTestMotionResult`](../classes/PhysicsTestMotionResult.md) form the typed server body-motion query. The scene [PhysicsBody](../classes/PhysicsBody.md) exposes the same sweep through `TestMove` and `MoveAndCollide`, returning [KinematicCollision](../classes/KinematicCollision.md).

`PhysicsDirectBodyState` and PhysicsBodyRuntime now use a versioned attachment
adapter for live state, mass/force application and contact projection. Common code
holds engine-valued mass profiles and contact values, with weak scene ownership;
concrete world/body references and fixture traversal belong to its selected CPU/GPU collider implementation.
The same adapter version qualifies queued callbacks across same-world reentry.
CPU attachments use Box2D.NET and GPU attachments use the resident store;
backend identity belongs to their selected World.

[Independent resident ray/point queries](gpu-resident-queries.md) execute on GPU for public GPU direct-space views; CPU spaces retain their solver queries. CPU origin-inside tests now cover the complete logical compound slot before casting, avoiding false hits on internal polygon seams. Other collider slots remain eligible. PhysicsQueryTests covers the regression for body/Area queries and warmed zero managed allocation.

## Runtime flow

[PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks all six operations across alternating hit/miss worlds, copied/span results, exclusions, view replacement, empty destinations and owner/solver/freed/failed-world guards. Both CPU and GPU execute 64 warmed complete-query cycles at 0/0 owner/all-thread managed bytes. The no-device child executes the CPU query family without a renderer or graphics device. Query identity and lifetime are exact; the circle ray check allows .05 scene units for native/resident geometric rounding, and a cast must bracket the first new hit. These checks do not establish native allocator, foreign-platform or real-window FPS acceptance.

All six direct-space operations now dispatch through the owning PhysicsWorldBackend: ray, point, shape intersection, motion fractions, contact pairs and rest information. The retained public view validates lifetime, owner thread and solver/failure access and prepares pending authored edits before dispatch. Array/span overloads share the same implementation results and preserve caps, ordering, physical RID/object identity and untouched destination tails. CPUPhysicsWorldBackend owns native scans and their retained geometry/result scratch; GPUPhysicsWorldBackend owns resident-query projection and result scratch, using the existing PhysicsSpace GPU query driver and leased geometry. Reopening a disposed view uses the same selected implementation; different spaces retain independent scratch. This internal boundary does not yet provide public extension construction, registration or scoped exclusion hooks under ADR 0103.

A World lazily registers its selected backend space when physics is needed. Each CollisionObject owns a stable RID from construction to disposal; rebuilt backend geometry keeps that RID and its logical shape-owner index. The server also creates explicit spaces, bodies, Areas and shapes. A server collider may attach to an explicit space or the SceneTree space. Explicit spaces advance through `SpaceStep`; a SceneTree advances its own space in the fixed physics lane. Freeing a resource removes its registry entry without reusing the numeric RID.

Direct ray and point views prepare pending scene geometry/poses, then query current body, Area and explicit-server geometry through the selected implementation. Query masks inspect collider layers independent of those colliders' masks; optional flags include sensors or bodies, RID arrays exclude objects, and results retain both RID and scene object where one exists. Server-only results have a null scene collider; their sampled object identity follows any explicit object association. Ray hits choose nearest fraction with RID/index tie order. Point hits sort and deduplicate by RID/index before applying the result cap. A direct view cannot query during solver stepping or off the space owner thread.

Shape views use the same prepared fixtures and layer rules. A query Shape is borrowed by the server through a lazily allocated RID; its owner releases that identity on disposal, and edits invalidate server fixtures before the next query. An explicit server shape RID works without a managed resource reference. `IntersectShape` includes shapes touched along `Motion`, sorting and deduplicating by RID/index. `CastMotion` ignores initial overlaps and refines safe/unsafe fractions around the first new hit. `CollideShape` returns paired contact points; `GetRestInfo` returns the deepest contact's collider point, outward normal and point velocity. Both scene and server-only shapes participate, including compound convex and hollow segment pieces.

`BodyTestMotion` scans registered body fixtures with reciprocal collision filters, body RID and scene instance-ID exclusions, and a finite supplied starting pose. Four recovery attempts move an initially penetrating test pose out of contact; the sweep then refines the first impact in eight steps. A residual deep overlap reports a zero safe fraction. One-way fixture direction and margin gate pass-through and recovery. Areas are sensors and do not block. The actual scene/server body stays in place; the caller-owned result records safe/unsafe fractions, contact and travel. Separation rays participate in directed recovery, flag-controlled sweeps and ordinary directed dynamic response under ADR 0068.

Body-owned collision exception RIDs also exclude pairs from motion tests and fixed-step contacts when either side lists the other. The typed scene methods accept PhysicsBody references; server methods accept opaque RIDs and can store an inert value. An exception edit rebuilds the owner's fixtures before the next test or step, including for an already touching pair. Direct ray, point and shape queries continue to use their own explicit exclusion arrays.

An enabled RayCast derives global endpoints from its local target and node transform, uses its cached RID exception array, and calls the direct ray view during internal physics processing before the solver step. It retains the result until another automatic or forced sample. Disabling clears only the collision flag; a miss clears collider identity while leaving the previous point and normal readable. Forced sampling can run while disabled. Direct-parent exclusion, layer bits, Area/body flags and inside hits reuse the direct query contract. Index-based fixture scans avoid managed boxing in 64 warmed active ray frames on Linux/.NET 8.

An enabled ShapeCast borrows a Shape resource and converts its local target into global motion. It first computes safe/unsafe motion fractions, then queries contacts at the earliest impact pose, excluding each reported collider RID until the result cap is reached. Zero target samples the current pose. Forced sampling works while disabled; disabling clears only its collision flag. Shape, target, margin, cap and filters are stored in PackedScene, while exclusions and results are transient. A reused direct parameter object, RID scratch array and contact lists keep 64 warmed active scene frames free of managed allocation on Linux/.NET 8.

## Dependencies, invariants and limits

The server builds on existing Shape resources, SceneTree owner-thread rules and the existing per-World internal Box2D.NET spaces. Its shape creation families include the analytic world boundary; extension-defined custom geometry remains separate work; joint resource lifetime and kernels execute through the shared runtime. Canvas/navigation RIDs and independent/shared viewport worlds execute. Scene query debug drawing and the remaining server methods are recorded in the [contract audit](physics-contract-audit.md). Server-only colliders share query/solver state but are not represented in current scene Area object-event arrays or server-only Area field configuration. Query scans are linear in fixture count; warmed unchanged casts and rest queries allocate no managed memory, while per-call result arrays, native allocation, other platforms and large-world throughput have not been audited.

[PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) checks RID and ray/point behavior. [PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) checks resource/RID shape selection and four direct operations. [PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) checks body sweeps; [PhysicsCollisionExceptionTests](../../tests/Electron2D.Tests/PhysicsCollisionExceptionTests.cs) checks pair suppression in motion and regular solver contact. [RayCastTests](../../tests/Electron2D.Tests/RayCastTests.cs) and [ShapeCastTests](../../tests/Electron2D.Tests/ShapeCastTests.cs) check scene node timing and lifecycle. Physics debug-gizmo drawing and virtual tile collision-object results remain separate dependencies. [ADR 0063](../decisions/physics.md#adr-0063) defines the ownership and query contract.

Separation rays execute directed direct queries, reverse sweeps and special body-motion recovery. CollideSeparationRay defaults false, includes non-sliding rays only in the motion phase, and leaves recovery participation unconditional. Sliding rays participate automatically; CharacterBody snap explicitly enables all rays. Shared motion results reconstruct the collider-side point from the manifold. Ordinary CPU dynamic ray impulses now use directed alternative manifolds under [ADR 0068](../decisions/physics.md#adr-0068), without changing query visibility or motion flags.

The internal PhysicsSeparationRay kernel owns directed contact and swept primitive composition; immutable SeparationRayData in PhysicsFixtureTag carries exact local endpoints and slope policy across fixture rebuilds. Both are private runtime integration details shared with PhysicsSpace.

[Shape collision methods](../classes/Shape.md#collide) are independent resource-pair geometry operations under [ADR 0069](../decisions/physics.md#adr-0069). They do not create a direct-space query or consult scene filters. Their motion evaluates independently swept regions; concave terrain motion is ignored and rays ignore counterpart motion. These rules differ from a direct query's swept-shape wrapper.

[PhysicsDirectBodyState](../classes/PhysicsDirectBodyState.md) and RigidBody custom integration now expose live owner-thread fields and immutable solved contact values after native stepping. PhysicsBodyRuntime owns body callback/persistent-force configuration; PhysicsSpace snapshots callback entries, aggregates failures and resynchronizes scene state. Typed userdata adapters allocate at registration. Sixty-four warmed active callback frames allocate zero managed bytes on Linux/.NET 10; whole-step normal/tangent impulses are complete; virtual tile identities remain Partial under [ADR 0070](../decisions/physics.md#adr-0070).

[CollisionObject shape-owner groups](../classes/CollisionObject.md#createshapeowner) unify manual borrowed resources and direct collision children. Sorted group IDs are separate from append-order global logical shape slots; structural removals reindex later slots. Body/Area preparation uses slot resource revisions, owner pose/policy and shared native fixture construction. Arbitrary weak owner identity reaches KinematicCollision results. ShapeOwnerTests verifies query/motion/solver/sensor integration and warmed zero managed allocation under [ADR 0071](../decisions/physics.md#adr-0071).

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

Typed [PhysicsServer](../classes/PhysicsServer.md) body/Area monitor callbacks now use the existing exact overlap kernel for scene/server receiver identities. The internal [Area runtime](../../src/Servers/Physics/PhysicsAreaRuntime.cs) stores optional delegates, logical pair snapshots and epochs; the [space scanner](../../src/Servers/Physics/PhysicsSpace.AreaMonitoring.cs) commits before callback dispatch, filters other layers directionally and retains monitorable gating. Payload includes other RID, stable scene InstanceID or zero, and both logical indices. Receiver reentry resets history while retaining registration; peer removal/free, reentrant removal and callback failures drain without stale entry replay. Scene snapshots remain independent. [PhysicsAreaMonitorTests](../../tests/Electron2D.Tests/PhysicsAreaMonitorTests.cs) checks 64 warmed entry/exit cycles with zero managed allocation on Linux/.NET 10. Native allocation, large-world performance, other platforms and owner acceptance remain unverified. [ADR 0077](../decisions/physics-monitoring.md#adr-0077) owns current semantics; server Area fields retain their next separate slice.

Typed PhysicsServer Area field getter/setters now share scene/server profiles and address a space default Area by its Space RID. Bounded fields use current exact overlap and descending mixed priority; monitor flags/callbacks are independent. Initial space defaults sample ProjectSettings, then explicit live edits apply on the next nonzero step without changing other worlds. Point/default gravity, body damping/scaling, wakeup and failure recovery are verified by [PhysicsServerAreaFieldTests](../../tests/Electron2D.Tests/PhysicsServerAreaFieldTests.cs) under [ADR 0056](../decisions/physics-fields.md#adr-0056). Native allocation, other platforms and owner visual acceptance remain unverified.

Shared [scene/server joint resources](physics-joints.md) use the existing physics world and native body IDs. Stable joint identities, typed settings, pending connection lifetime and independent contact/motion exception contributions now execute under [ADR 0087](../decisions/physics-joints.md#adr-0087).

[Indexed PhysicsServer geometry](../classes/PhysicsServer.md#shape-slots) now shares the scene/server logical slots, effective local poses and native fixture/query path under [ADR 0088](../decisions/physics-shape-slots.md#adr-0088). Raw replacement/pose/disabled/one-way edits do not rewrite child configuration; group/child edits reclaim corresponding overrides. Shape free/replacement follows shared RID/view ownership and related-world phase guards. Effective poses also feed mass geometry. [PhysicsServerShapeSlotTests](../../tests/Electron2D.Tests/PhysicsServerShapeSlotTests.cs) verifies real geometry, body/Area lifetime and one-way contacts/motion on Linux/.NET 10.

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

[Caller-created direct-space extensions](../components/physics-space-extensions.md) now execute all six typed query hooks through inherited public array/span/scalar operations. The library owns bound-space guards, nested exclusions, scratch lifetime and validated result publication. A separate public-only consumer computes real scene/raw/Area circle queries on CPU/GPU and checks lifecycle, failures and warmed allocation. Registered-backend factories, direct-body extensions and custom geometry remain open under ADR 0103.
