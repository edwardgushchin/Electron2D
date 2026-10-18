# Collision shapes component

## One-way slot operation audit

The current five-argument `BodySetShapeAsOneWayCollision` and its registered
`RebuildShapes` projection have complete operation-level accounting under ADRs
0065, 0071, 0088 and 0103. Both pinned declarations and their owners have no
`deprecated` metadata. The header's guarded four-argument compatibility binding
is excluded; no alias or reduced direction contract is exposed.

The operation changes exactly one global logical slot's enabled flag, finite
nonnegative scene-unit margin and finite normalized local direction. Null means
down; zero remains zero, and very large finite components normalize safely.
Effective slot/body rotations carry the direction into the numerical world.
Borrowed resource identity, sibling policy and logical indices remain intact.
Disabled slots retain authoring and reenable with the selected policy. Invalid
values, indices, wrong/stale RIDs, foreign-thread access and writes from a borrowed
selected rebuild reject before mutation.

Raw attached edits rebuild immediately. Apply/restoration failure retains both
original errors, restores prior logical authoring and retries numerical debt before
a query. Scene edits defer numerical preparation and retain committed authoring
across repeated selected failures. A manual-group one-way edit or a changed child
one-way property reclaims the full stored group policy, without changing the child
or group fields when only the raw slot override is edited.

The public-only `--one-way-inputs` profile now checks raw bodies, manual three-slot
groups and CollisionShape-owned slots. Actual selected motion queries verify both
approaches, rotated direction, zero direction, two-sided response after disabling,
margin-controlled initial recovery and exact collider/local logical indices.
The 40-unit approach sweeps begin 20 units from the target. Initial recovery
starts with .5 scene units of overlap and compares zero/one-unit policy margins
against a .001-unit query margin, keeping expected outcomes away from skin-depth
thresholds. Direction projection uses ordinary approximate vector comparison for
trigonometric rounding; logical identities and allocation counters remain exact.
Registered owners also verify the immutable transformed values and unaffected
siblings, equal normalized-write reuse, borrowed reentry and exact apply/rollback/
deferred error identity. Each role warms 128 and measures 128 full unchanged-policy
query/physics intervals at strict 0/0 owner/all-thread managed bytes. The existing
nine changed-policy preparation brackets retain their separate authoring boundary.
The root filter-contract group adds real built-in contact/polygon/exception checks.

Evidence: `bin/physics-one-way-slot-audit/2026-10-19/validation.json`. One registered
setter row becomes Implemented; the unique physics ledger has 179 open declarations:
148 Partial, 14 Unimplemented and 17 Blocked. Runtime code and public API are
unchanged by this audit. Arbitrary registered solver response, complete numerical
extension, native/foreign/network and target-GPU/window performance acceptance
remain open; these are distinct from the audited setter's input/lifecycle contract.

## One-way input preparation

Scene body dirtying captures every resolved immutable `OneWayContactData` value
while authoring changes are applied. Numerical shape preparation reuses those
values, preserving any earlier record retained by a selected implementation.
The indexed traversal uses the existing slot cache and includes manual groups,
CollisionShape and CollisionPolygon children. Area sensors retain their existing
two-sided policy. Unchanged values reuse the same record.

Previously, changing the local pose of a two-slot manual one-way group allocated
8192 owner/all-thread bytes across 128 physical frames, with zero bytes in the
measured property edits. Lazy policy capture had crossed into numerical preparation.
The shared body dirtying path now moves capture to authoring: changed input costs
32 managed bytes per logical slot on measured Linux x64, while each physical frame requires
strict zero owner/all-thread bytes. This does not promise zero allocation for
structural authoring or changed immutable input publication.

The public-only `--one-way-inputs` fixture deliberately performs no query between
an edit and its step. Manual two-slot groups, borrowed circle children and solid
polygon children each alternate local pose, margin and passing direction. Every
case warms 128 and measures 128 complete frames with an independently moving body;
registered owners also verify current transformed values and unchanged retained
records. JIT and strict NativeAOT profiles cover CPU without an available GPU,
software Vulkan and a registered implementation, with serial/separate numerical
policy. The root filter-contract group retains actual one-way traversal, polygon
response and collision-exception checks. Adjacent geometry-borrowing and indexed
shape-edit profiles protect the common preparation paths.

Evidence: `bin/physics-one-way-inputs/2026-10-18/validation.json`. No public signature,
ADR decision or coverage state changes. Complete numerical-extension, native
allocation, foreign-platform, network and target-GPU/window performance acceptance
remain open; software Vulkan establishes functional execution only.

## Indexed slot operation audit

The Body/Area replace, local-pose, enable/disable, remove and clear operations now
have complete operation-level accounting through their registered `RebuildShapes`
projection under ADRs 0063, 0071, 0088 and 0103. The pinned ten public operations and
ten implementation counterparts have no deprecated metadata. Their existing
runtime paths preserve resource identity, logical indices, finite unit-scale
zero-skew poses, validation and using-world/resource borrowing.

Raw attached edits rebuild immediately. A failed application restores authored
slots and history; failed restoration preserves both errors and queues numerical
retry. Scene indexed edits commit common authoring and defer numerical preparation.
A failed selected rebuild preserves that authoring and retries before a live query
or physical use. Replacement retains the owner and per-slot policy; pose/disabled
overrides leave group and child configuration intact. Removal reindexes across
owner boundaries. Clear retains empty owners and the child/resource association;
a later child resource edit repopulates its existing group at a new logical index.

The public-only `--shape-edits` consumer now exercises six edits for each scene
Body/Area role, two failed preparation attempts per edit on the registered owner,
exact query geometry/indices, weak owner identity and child repopulation. Each role
warms and measures 128 edited query/full frames at zero owner/all-thread managed
bytes. The existing raw fixture retains 15 ordinary operations and 45 registered
failure cases, plus all eight represented built-in geometry families. Fresh JIT
and strict NativeAOT CPU/no-device, software-Vulkan and registered serial/separate
profiles also exercise cold supplier borrowing through `--geometry-inputs`; the
root slot-consumer group checks real query, mass, one-way, lifetime and event paths.
Evidence: `bin/physics-slot-edit-audit/2026-10-18/validation.json`.

Ten extension rows close; no runtime API or solver implementation changes here.
One-way policy, other extension operations and full numerical/native/platform/
network/hardware performance acceptance retain their independent gates. Structural
authoring may allocate under ADR 0088; the explicit immutable one-way reauthoring
boundary described below remains separate from zero-allocation physical steps.

## Body/Area append contract audit

`PhysicsServer.BodyAddShape` and `AreaAddShape`, together with their registered
implementation projections, now have complete operation-level accounting under
ADRs 0063, 0071, 0088 and 0103. The existing code already supplies the contract;
this audit changes documentation and coverage. It adds no runtime API or behavior.

The pinned declarations and their owners have no deprecated metadata. Both take
an owner RID, shape RID, local transform defaulting to identity and `disabled=false`;
nullable C# transforms select that same identity. Accepted physics poses remain
finite translation/rotation with unit scale and zero skew under ADR 0071. An append
adds one logical index regardless of native decomposition, retains existing slots
and resource identity, and creates no scene child. Scene raw appends create a
transient null-owner group; their numerical preparation is deferred. Raw attached
appends rebuild immediately, restore original logical/history inputs on failure
and retain exact apply/rollback errors, with dirty retry before numerical use.

Unconfigured built-in RIDs may be appended and stay numerically inactive until
successful configuration. Disabled and disposed resources retain their accepted
logical slot semantics. Built-ins execute eight represented kinds and reject
unsupported enabled Custom input before mutation; registered owners receive all
nine canonical kinds through their complete borrowed `RebuildShapes` input span.
The engine owns RID/slot metadata and selected implementations own actual geometry.

Fifty-two fresh public-only JIT/strict NativeAOT profiles on the frozen `02b3328e`
authored source cohort cover CPU without an available GPU, software Vulkan and a
registered solver, serial/separate policy, eight-kind pending initialization,
nine-kind procedural imports, ordinary and injected append/edit rollback, shared
scene/raw Body/Area membership, and both mixed-world creation orders. Existing
warm full-frame gates retain strict zero owner/all-thread managed bytes. Explicit
immutable one-way reauthoring is measured separately at 32 managed bytes per edit;
its physical steps remain zero. Append/resource construction is cold work, and
this audit makes no new native-allocation claim. The evidence report at
`bin/physics-shape-append-audit/2026-10-18/validation.json`
records artifacts and qualification. The independent fixture's algorithms are test
implementations; this audit does not certify arbitrary third-party solvers.

The four append declarations are Implemented. Other slot/extension operations,
full numerical algorithms, native memory, foreign platforms, network/hardware
performance and complete-goal acceptance retain their own open gates.


Last updated: 2026-10-18

## Raw shape initialization

The eight built-in PhysicsServer shape factories return unconfigured RIDs. ShapeGetType works immediately. ShapeGetData and using that RID or a pending borrowed view as a collision-query source throw InvalidOperationException until ShapeSetData succeeds. Ordinary Shape constructors and CustomShapeCreate still supply initialized data.

Body/Area add operations accept pending shapes and keep their logical indices, transforms and disabled policy. Those slots supply no geometry, mass contribution, sensors or contacts until configuration. Disposed borrowed resources also produce inactive numerical descriptors while retaining logical slots and empty RID getters, including after disable/re-enable. Replacement operations require configured data. Selected query-result publication rejects disabled or unconfigured scene/raw slots before changing caller output. The first successful ShapeSetData activates enabled scene/raw users with stable RID/index/owner identity; a failed first configuration restores the pending state and can retry. Data copies, old-view retirement and shared-world borrowing keep their existing guarantees.

Private checkpoints preserve pending slots. Portable authoring compatibility distinguishes pending and configured data, so applying an old pending snapshot after configuration rejects before mutation. The encoded snapshot layout and hashes for live configured shapes stay unchanged. This is headless authoring/simulation behavior; hardware GPU, rendered output, native allocator and full physics acceptance remain separate gates.


## Scope and owned types

[`Shape`](../classes/Shape.md) is the abstract managed geometry resource. [`CircleShape`](../classes/CircleShape.md), [`CapsuleShape`](../classes/CapsuleShape.md), [`SegmentShape`](../classes/SegmentShape.md), [`SeparationRayShape`](../classes/SeparationRayShape.md), [`WorldBoundaryShape`](../classes/WorldBoundaryShape.md), [`ConvexPolygonShape`](../classes/ConvexPolygonShape.md), [`ConcavePolygonShape`](../classes/ConcavePolygonShape.md) and [`RectangleShape`](../classes/RectangleShape.md) provide concrete fixtures. [`CollisionShape`](../classes/CollisionShape.md) borrows one Shape as a direct `PhysicsBody` or `Area` child. [`CollisionPolygon`](../classes/CollisionPolygon.md) is a sibling scene child that owns its generated solid or hollow geometry; [`PolygonBuildMode`](../classes/PolygonBuildMode.md) chooses its construction mode. None of these public types expose backend handles.

## Runtime flow

[Geometry input borrowing](physics-backends.md#geometry-input-borrowing) protects the using world before cold procedural resources run, through selected scene/raw Body/Area compilation. Numerical and binding writes reject; original supplier failures release the guard for retry. Scene Area revision capture uses indexed slot traversal, matching bodies and avoiding a boxed enumerator on warmed shape edits.

Every built-in resource supplies cached immutable [ShapeGeometry](../classes/ShapeGeometry.md)
data with exact scene-unit endpoints, radius and contours. Resource classes contain
no backend handles. The selected collider owner compiles its body/Area attachments:
the CPU implementation uses PhysicsShapeBackend, GPU keeps resident geometry, and
a registered implementation reads the same public immutable payload. Mass and
direct queries dispatch through that selected owner. Standalone Shape collision
retains its separate resource-only whole-contour kernel. Compiled polygon
validation runs during authoring. The independent registered consumer now has
[rectangle and capsule numerics](physics-registered-adapter.md#registered-capsule-numerics);
other provider geometry and complete acceptance remain open.

Circle radius defaults to 10 scene units, capsule radius/full height to 10/30, segment endpoints to (0, 0)/(0, 10), and rectangle size to (20, 20). Capsule height and radius stay linked; MidHeight sets their central separation. The capsule accepts finite nonnegative dimensions, including zero and line/circle limits. Circle and rectangle require finite positive dimensions; segment endpoints and their difference must be finite and may coincide. These shapes report local bounds, publish geometry changes and support independent resource duplication; equal capsule radius/height writes do not publish, while an equal MidHeight write does. A CollisionShape starts with no resource, `Disabled=false`, `OneWayCollision=false` and local direction `(0, 1)`; assigning a live resource or changing either flag updates its direct collision owner's fixtures before the next physics step. Local position and rotation offset the fixture; scale/skew are rejected while active. The parent body or area owns its backend fixtures, while the caller owns the Shape resource.

A convex polygon starts empty, accepts convex perimeter arrays or a point cloud, and can append several solid fixtures beyond eight vertices while preserving one public resource. A concave polygon stores independent endpoint pairs and appends hollow line fixtures; a body fully inside closed edges does not overlap them. A body child can mark all its fixtures one-way, choose a normalized local pass-through direction and set a finite recovery-depth margin; the physics world keeps each pair's initial contact side until separation. An Area child keeps the flag for scene state but remains a two-sided sensor. Shape `Changed` and `Disposed` notifications mark the attached owner's fixture set dirty. Shape setters validate geometry before mutation; rebuild validates active transforms before destroying old fixtures. Configuration warnings report a missing direct collision owner, a missing/disposed resource, or an ineffective Area one-way setting. A detached CollisionShape is still a normal Entity and can be packed into a scene with its borrowed resource association.

A direct CollisionPolygon copies its local vertices and owns generated resources. Solids mode decomposes a valid contour into convex parts, while Segments mode closes the vertex list into hollow edges. Empty, insufficient or undecomposable contours contribute no geometry and report warnings; finite input validation rejects before mutation. Mode, contour, disabled and transform changes mark the same body or Area fixture owner dirty. Polygon one-way body contacts reuse the current pre-solve side selection; its margin also gates typed body motion recovery; an Area still senses from both sides. The node packs its typed contour and rebuilds its owned geometry after scene instantiation.

A [PhysicsShapeQueryParameters](../classes/PhysicsShapeQueryParameters.md) may borrow any concrete Shape for direct overlap, motion and contact queries. The borrowed RID is allocated lazily, remains stable through geometry edits, and is released by the owning Shape's disposal. A server collider may also use that RID; edits mark its fixtures dirty before the next direct query. Compound convex pieces and hollow paired edges preserve the same direct shape-owner identity. Standalone Shape operations consume the same source geometry through their separate resource-only collision kernel.

## Dependencies and verification

Shapes use Core Vector2/Rect2/Resource contracts and the Entity scene hierarchy; backend geometry compilation resides in PhysicsShapeBackend. [PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) checks defaults, validation, bounds, duplication, live circle resizing, disabled fixtures, PackedScene restoration and borrowed lifetime; [CapsuleShapeTests](../../tests/Electron2D.Tests/CapsuleShapeTests.cs) checks coupled dimensions, rotation, body/area fixtures and warmed allocations. [SegmentShapeTests](../../tests/Electron2D.Tests/SegmentShapeTests.cs) checks two-sided terrain, dynamic and sensor fixtures, mass and warmed allocations. [ConvexPolygonShapeTests](../../tests/Electron2D.Tests/ConvexPolygonShapeTests.cs) checks hulls, compound fixtures, dynamic contacts, area sensing and warmed allocations. [ConcavePolygonShapeTests](../../tests/Electron2D.Tests/ConcavePolygonShapeTests.cs) checks paired edges, hollow contact and sensors. [OneWayCollisionTests](../../tests/Electron2D.Tests/OneWayCollisionTests.cs) checks initial contact side, rotation, live changes, packing, Area sensing and warmed allocation. [PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) checks one-way margins on borrowed and owned scene shapes. [AreaTests](../../tests/Electron2D.Tests/AreaTests.cs) checks direct area ownership and sensor edits. The capsule and segment sub-slop fixture adaptations are defined in [ADRs 0059 and 0061](../decisions/physics.md#adr-0061); [ADR 0065](../decisions/physics.md#adr-0065) defines one-way body contacts. Debug drawing and custom solver bias retain their open rows in [coverage](../coverage/index.md).

[CollisionPolygonTests](../../tests/Electron2D.Tests/CollisionPolygonTests.cs) checks the direct scene polygon's solid/segment modes, concave and hollow sensing, one-way contact, errors, packing and warmed allocation. [ADR 0066](../decisions/physics.md#adr-0066) defines its owned geometry and direct slot. Debug drawing and custom solver bias retain their open rows in [coverage](../coverage/index.md).

The geometry extraction passes the expanded 28-suite collider group and the full
current GPU stage-host suite. The new shared/copy polygon regression checks live
fixture and query updates, rejected-edit preservation and 128 warmed queries with
zero managed allocation. This is current-backend preservation, not acceptance of
an independent GPU implementation or a throughput claim.

## Decision

- [0054: First Box2D-backed scene-body profile](../decisions/physics-backends.md#adr-0054)
- [0055: Directional scene area monitoring](../decisions/physics.md#adr-0055)
- [0059: Capsule collision geometry](../decisions/physics.md#adr-0059)
- [0061: Segment collision geometry](../decisions/physics.md#adr-0061)
- [0062: Compound convex fixtures](../decisions/physics.md#adr-0062)
- [0064: Hollow paired-segment resource](../decisions/physics.md#adr-0064)
- [0065: One-way scene-body contacts](../decisions/physics.md#adr-0065)
- [0066: Direct scene collision polygons](../decisions/physics.md#adr-0066)

SeparationRayShape defaults to length 20 and SlideOnSlope false, reports padded drawing bounds, duplicates independently and contributes a zero-density fixture with exact directed metadata. Query/body-motion and Area sensing use the special kernel under [ADR 0068](../decisions/physics.md#adr-0068); ray/point queries exclude it. CPU body fixtures now supply directed manifolds before ordinary material/constraint/history processing. Areas retain sensors; rays have no geometric mass or rod inertia. Full convex targets reject origin containment and select only the earliest entry partition. [SeparationRayShapeTests](../../tests/Electron2D.Tests/SeparationRayShapeTests.cs) preserves queries and character motion; [SeparationRayDynamicsTests](../../tests/Electron2D.Tests/SeparationRayDynamicsTests.cs) checks directed/reversed six-family contacts, slope policy, friction/bounce, coupled mass/impulses, sleep, live edits, one-way/CCD, scene events and zero all-thread managed allocation across 128 warmed active frames. The old CPU-hosted GPU experiment retains these custom manifolds on the host and uploads the resulting constraints; this does not establish independent GPU acceptance. Native allocation, other platforms and rendered acceptance remain unverified.

Standalone Shape methods test two resources without registering server RIDs or creating a world. The internal PhysicsShapeCollision kernel uses original scene-unit support geometry, independently swept convex regions, separating axes and clipped boundary supports. Full convex contours retain their external boundary rather than fixture partitions; concave/ray pairs retain their pinned special-motion rules. Its private per-thread buffers reuse proxy and hull capacity without retaining resources. [ShapeCollisionTests](../../tests/Electron2D.Tests/ShapeCollisionTests.cs) verifies active/full-contour/empty-contact calls without warmed managed allocation, under [ADR 0069](../decisions/physics.md#adr-0069).

[CollisionObject shape-owner groups](../classes/CollisionObject.md#createshapeowner) unify manual borrowed resources and direct collision children. Sorted group IDs are separate from append-order global logical shape slots; structural removals reindex later slots. Body/Area preparation uses slot resource revisions, owner pose/policy and shared native fixture construction. Arbitrary weak owner identity reaches KinematicCollision results. ShapeOwnerTests verifies query/motion/solver/sensor integration on explicitly selected CPU/GPU worlds and 64 warmed full frames with zero owner/all-thread managed bytes under [ADR 0071](../decisions/physics.md#adr-0071).

Directed-ray microbenchmark on Linux/.NET 10 (1/60 s, one awake supported body,
one static floor, 128 warmup and 128 measured whole public SpaceStep calls):

| Path | p50 / p95 / p99, ms | Managed bytes over 128 steps |
| --- | --- | --- |
| CPU, dummy video | 0.0047 / 0.0047 / 0.0049 | 0 |
| CPU host with GPU stages, Vulkan/RTX 3090 Ti | 0.2317 / 0.2520 / 0.2680 | 0 |

Logs: `/tmp/electron2d-ray-collider.log` and `/tmp/electron2d-ray-gpu-suite.log`.
The stage measurement includes host-directed contact generation and constraint
upload; it does not measure the independent GPU backend or establish large-world
speedups. Scene rendering, native allocations and networking are outside this probe.

## Infinite world boundaries

WorldBoundaryShape stores the analytic solid side `Normal.Dot(point) <= Distance`.
Nonunit normal magnitude remains authored data; both backends normalize the equation
for geometry. It contributes no geometric mass/inertia. Its finite GetRect is an
editing marker, never a collision envelope. PhysicsServer.WorldBoundaryShapeCreate
and ShapeGetType complete creation/type/data access for the eight built-in kinds;
Custom remains an enum identity without extension geometry support.

CPU primitives carry a plane tag, a separate unbounded broad-phase registration,
analytic support manifolds and conservative translating/rotating TOI. The unbounded
registry compares plane equations each step, renewing candidates when rotation
changes the half-plane despite an unchanged finite bookkeeping AABB. Independent
GPU storage carries the same kind and plane parameters; shaders handle unbounded
tree traversal, free-side pruning, contacts, queries and CCD. No CPU pose/geometry
mirror is used for resident simulation. The old CPU-hosted GPU-stage experiment
continues to publish custom boundary/ray manifolds on the host; that path is separate.

WorldBoundaryTests checks explicitly selected public CPU/GPU scene/server worlds
and independent store response at X=1,000,000,
queries up to X=4,000,000, contacts against all seven other built-in families,
nonunit equations, resource motion precedence, shape/type
lifetime, sensors, sleep/edit/removal, and both CCD modes including a rotating plane.
GPUPhysicsShapeQueryTests and GPUPhysicsMotionQueryTests include boundaries in the
shared geometry matrices. The regression moving a query plane away from an already
overlapping ray preserves the direct query's initial intersection.

Linux/.NET 10, Release, 1/60 s, one awake body plus static plane, 128 warmup and 128
measured complete step calls; GPU device Vulkan / NVIDIA GeForce RTX 3090 Ti:

| Path | p50 / p95 / p99, ms | All-thread managed bytes over 128 steps |
| --- | --- | --- |
| Public CPU, dummy video | 0.0044 / 0.0045 / 0.0048 | 0 |
| CPU host with GPU stages | 0.2200 / 0.2513 / 0.4571 | 0 |
| Independent resident GPU | 1.9938 / 2.5606 / 3.5468 | 0 |

Resident per-step traffic: 160 B buffer upload, 14,324 B uniforms, 160 B status
readback; included device waits average 1.1224 ms. No pose readback occurs in the
measured loop. Logs: `/tmp/electron2d-boundary-cpu.log`,
`/tmp/electron2d-boundary-stages.log`, `/tmp/electron2d-boundary-resident.log`.
These are tiny-world overhead/allocation probes, not large-world speedup or 60 FPS
acceptance. Native allocation, window rendering, network behavior and other platforms
remain separate acceptance gates. Public CPU/GPU binding and the common boundary
suite now execute; see the [current record](cpu-continuous-collision.md#public-cpugpu-conformance-2026-10-10).

## Solver policy and storage

All built-in shapes inherit stored CustomSolverBias. Policy edits use a separate
epoch, retaining geometry/fixture IDs, mass and contact history. Concrete copy hooks
publish geometry revisions; convex in-place copies refresh the compiled hull through
the Points setter. All eight shapes round-trip through the built-in resource file
registry. See [contact correction](physics-contact-policy.md).

[Shape.Draw and scene diagnostics](physics-debug.md) now record built-in geometry
through the existing retained canvas with finite color, project outlines, typed RIDs
and ownership/error checks. CPU/GPU physics do not need device state readback for this
drawing. CollisionShape stores DebugColor and refreshes on geometry revisions/disposal.

## Caller-defined shape resources

Shape.GetGeometryCore now accepts exact immutable [ShapeGeometry](../classes/ShapeGeometry.md). Factories cover all eight current primitive/contour families, copy contour input and preserve infinite boundary semantics. Cached data and CPU contour partitions are qualified by resource geometry revision; a procedural Shape can change primitive kind while retaining its RID. GetRect, Draw, standalone collisions, scene/raw attachments, Area observations, mass, direct queries, contact projection, checkpoints and portable schemas consume the same data.

[The separate public consumer](../../tests/ShapeExtension.Consumer/ShapeExtension.Consumer.csproj) exercises this path on CPU/GPU/no-device, including 64 warmed measured full step/query/checkpoint/snapshot cycles with zero managed allocations. Its draw profile checks live geometry edits using actual window pixel readback under both renderers. Native allocator, foreign-platform and full workload/FPS acceptance remain separate. This capability supplies exact supported geometry; it does not approximate or register unsupported collision algorithms.

Standalone special-pair rules, full-contour resource contacts, CPU compound metadata, directed-ray query policy and mass exclusions now dispatch on represented geometry kind. They do not require a concrete resource class. CPU/GPU rebuilds demand every live candidate's immutable data before retiring old fixtures; throwing or null construction hooks fail preparation before the solver tick and permit a later retry.

[Custom geometry](physics-custom-geometry.md) now executes with an external annulus/circle implementation: canonical data, exact mass, standalone/selected queries, body contacts, Area observations, server mutation and private/portable replay. Generic custom format file codecs/editor authoring, unexercised extension operations, native/foreign acceptance and final performance remain open.

Geometry edits now preflight all scene/raw borrowers before private data or convex
compilation-cache mutation. This includes disabled slots and equal assignments.
The protected Shape.EnsureGeometryChangeAccess authoring helper runs selected
status hooks on the owner before commit without resource locks. Coordinated worker
geometry writes remain valid in quiescent worlds and never call owner-bound status
hooks; numerical/step borrowing rejects worker writes before mutation. EmitGeometryChanged publishes
the edit with owner/phase checks and no repeated fallible status hook. Procedural
subclasses must preflight before changing private fields; publication cannot undo
such private writes. Ordinary post-solver edits remain available. The public-only
--shape-geometry profile exercises 16 authoring paths across all eight built-ins,
CustomShape and a procedural resource, with immutable data/RID preservation on
rejection, actual point-query changes and observer failure. Warm frames measure
stable geometry plus guard calls; array copies and accepted immutable geometry
creation are authoring allocations. Full numerical-extension acceptance remains open.

[Shared shape factories and data lifetime](physics-registered-adapter.md#shared-shape-construction-and-data-lifetime) now execute through the
public-only registered/CPU/GPU consumer. Replacement errors preserve original
geometry, continue every rollback/retirement and retain all failures; incomplete
borrowers retry during guarded preparation. Ten formerly unmapped extension rows
project to the common resource role as Partial. Full numerical-extension and final
physical/network/performance acceptance remain open.

The complete existing capsule, two-sided segment, convex and hollow concave test suites now run explicitly selected CPU/GPU worlds. They exercise linked/degenerate geometry, rotation and solver response, Area transitions, resource edits including throwing Changed callbacks, shared/independent copies, mass/inertia and real ray queries after PackedScene instantiation. RigidFreezeModeTests also assigns the selected World after loading; public BodyGetMode replaces internal role probes. Existing numeric tolerances are retained: these checks assert supported poses, physical response and exact identities rather than identical backend trajectories. Warm brackets measure 64 full scene frames or 128 query cycles at zero owner/all-thread managed bytes on Linux/.NET 10. Native allocation, other platforms, broad-world performance and owner visual acceptance remain unverified.

The ordinary full runner executes the complete WorldBoundaryTests on CPU and GPU;
`ELECTRON2D_TEST_CONTINUOUS_PHYSICS=cpu|gpu` selects the same cases. Scene and raw
spaces assert the actual backend. The 128 warmup/128 active-step all-thread zero
allocation gate and all numerical/error bounds remain unchanged. Native allocations,
foreign platforms and final performance acceptance stay open.

The continuous-contract entry now accepts separate numerical execution without
being redirected to the general CPU suite. Both CPU/GPU profiles pass the same
CCD, infinite-boundary and directed-ray cases on software Vulkan, with explicit
0/0 owner/all-thread warm allocation gates. See [current software conformance](cpu-continuous-collision.md#software-vulkan-continuous-conformance-2026-10-14).
Hardware performance, native allocation and foreign-platform acceptance remain open.

The [registered segment consumer](physics-registered-adapter.md#registered-segment-numerics)
now verifies actual two-sided lines, rod mass, queries, contacts and CCD behind the
existing extension boundary. It preserves empty point interiors and accepted short
midpoint fixtures. General provider contours and final acceptance remain open.

[Registered convex numerics](physics-registered-adapter.md#registered-convex-numerics)
now consume every accepted perimeter vertex through the public immutable payload.
They retain logical shape identity across whole contours while CPU may partition
native fixtures, and verify winding, asymmetric mass and live empty replacement.
Other provider families and final numerical acceptance remain open.

[Registered concave numerics](physics-registered-adapter.md#registered-concave-numerics)
now preserve hollow paired-edge material, logical slot identity, short/point limits,
queries and rod mass through the public immutable geometry payload. Standalone
concave motion remains ignored in either operand; two concave resources do not
collide. Wider provider and final acceptance gates remain open.


[Registered infinite-boundary numerics](../components/physics-registered-adapter.md#registered-boundary-numerics)
now exercise analytic planes in the independent public-only provider, all six
query families and reverse query source, Area/contacts, zero geometric mass,
material/surface impulses, private replay, sleep/lifecycle and translating/rotating
CCD. Twenty full-frame brackets per CPU/GPU/provider serial/separate profile
require strict 0/0 warmed managed bytes. CPU authored body/query/joint angles now
use ordinary sine/cosine; the far rotated query retains its .005-unit bound rather
than absorbing the former approximate-angle drift. Directed-ray, arbitrary/coupled,
complete inherited/extension/network/native/platform and hardware-performance
acceptance remain open; coverage states are unchanged.


[Registered directed-ray numerics](../components/physics-registered-adapter.md#registered-directed-numerics)
now execute surface entry, axis/slope separation, zero geometric mass, all query
families and reverse source, raw recovery and character snap, material/surface
impulses, silent replay and linear/angular CCD through an independent public-only
provider. Twenty-three full-frame brackets per CPU/GPU/provider serial/separate
profile require 128 warmup/128 measured frames at 0/0 managed bytes. Directional
response and stable long-ray/circle roots are corrected in the shared consumer.
Arbitrary custom/coupled, complete inherited/extension/network/native/platform
and hardware-performance acceptance remain open; coverage states are unchanged.


[Registered contact numerics](../components/physics-registered-adapter.md#registered-contact-numerics)
now exercises actual world/shape bias and penetration slack, complete contact/joint
sweeps, full physical impulses and identity-preserving live policy through the
public-only provider. Seven source families, dense contacts, CCD durations and
contact/pin convergence use nineteen complete 128-warmup/128-measured brackets
at zero managed allocation. Registered contact-cache, arbitrary coupled/custom,
full inherited/extension/network and native/platform/performance acceptance remain
open.
