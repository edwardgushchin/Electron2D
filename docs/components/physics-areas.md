# Physics areas component

Last updated: 2026-10-16

## Scope and owned types

[`Area`](../classes/Area.md) is the sensor branch of [`CollisionObject`](../classes/CollisionObject.md). Direct [`CollisionShape`](../classes/CollisionShape.md) children borrow circle, capsule, segment, convex polygon, concave segment collection, separation ray or rectangle geometry and become nonresponding backend fixtures. A SceneTree's internal physics space owns the backend body and the fixed-step overlap scan.

## Runtime flow

Direct CollisionPolygon children contribute owned solid convex pieces or closed hollow edge sensors alongside borrowed CollisionShape children. Polygon one-way settings do not change Area overlap detection.

Body callbacks and the four-substep backend world advance before areas scan. An area's mask tests each candidate's layer, regardless of the candidate's mask. Another area also needs to be monitorable. Backend bounding boxes reject separated shape pairs; backend shape distance checks candidates exactly. Multiple overlapping shape pairs produce one object-level result. Snapshots and entered/exited transitions are committed once per nonzero fixed step. Event delivery runs after backend stepping so handlers can change the scene; removal of an object clears other areas' snapshots immediately.

Before that backend step, each RigidBody resolves currently overlapping area fields by greatest priority first. Gravity, linear damping and angular damping have independent Disabled/Combine/CombineReplace/Replace/ReplaceCombine modes. An unstopped channel includes the sampled world default; body gravity scale and damping modes apply afterward. CharacterBody resolves the same gravity channel for its inherited `GetGravity()` query but does not automatically change its caller-owned `Velocity`. Point gravity transforms the area's local point and supports constant strength or inverse-square falloff. Field participation does not require `Monitoring` or `Monitorable` and does not wait for the event snapshot.

## Dependencies, invariants and limits

An Area owns a stable [RID](../classes/RID.md). A [direct ray or point query](physics-queries.md) includes its sensor fixtures only when `CollideWithAreas` is enabled; the query's layer mask tests this area's layer even when its own mask is zero. Scene and server Areas share the selected CPU/GPU space for direct queries and logical shape-pair observations; typed object-level events project associated live scene owners.

An Area uses nonresponding sensor fixtures; a child CollisionShape's one-way body-contact setting does not filter either approach side. The child warns about the ineffective setting, while its scene properties remain packable.

The current profile accepts unit global scale and zero skew while active. Shape/resource, monitoring and field edits take effect on the next step. Layer/mask edits update existing sensor geometry immediately for queries; overlap snapshots and events update on the next step. Borrowed resources remain caller-owned; tree exit and disposal release backend handles. The CPU pairwise scan has quadratic candidate growth; add a spatial candidate index when measured large-scene cost requires it. Square-atlas TileMapLayer virtual bodies now retain their layer owner and logical shape/RID payloads through overlap snapshots and events on both public backends. Other tile capabilities retain separate coverage records. Spatial audio point queries use scene Area shapes and collision layers independently of monitoring flags; see [audio playback](audio-playback.md#spatial-2d-playback).

[AreaTests](../../tests/Electron2D.Tests/AreaTests.cs) now checks monitoring, event timing, failed callbacks, packing and lifecycle on explicitly selected public CPU/GPU worlds. Its steady and empty full-frame brackets retain 64 warmup/64 samples and require zero owner/all-thread managed bytes. The [paired runner](physics-backends.md#paired-complete-collider-runner-2026-10-16) supplies serial/separate policies. [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks field modes, point falloff, sampled defaults, damping and warmed RigidBody frames; [CharacterBodyTests](../../tests/Electron2D.Tests/CharacterBodyTests.cs) checks world/Area gravity selection on a kinematic character. Native allocator counts, other platforms and owner visual acceptance remain unverified. [ADRs 0055 and 0056](../decisions/physics.md#adr-0056) record the execution boundary.

Separation-ray overlap uses exact endpoints and the directed query kernel rather than a GJK segment overlap. Containment and ray-ray pairs are rejected. This also governs field-area selection. [SeparationRayShapeTests](../../tests/Electron2D.Tests/SeparationRayShapeTests.cs) checks directed Area entry and containment exit under [ADR 0068](../decisions/physics.md#adr-0068).

Scene Area field reduction now includes server-created dynamic/kinematic bodies through the shared shape-overlap/mask reducer. Dynamic server gravity/damping uses the same selected field, omission policy and field-change wakeup; server-only Area field configuration now shares the same profile; raw monitoring observers execute independently under ADR 0077. PhysicsBodyStateTests checks this integration.

[CollisionObject shape-owner groups](../classes/CollisionObject.md#createshapeowner) unify manual borrowed resources and direct collision children. Sorted group IDs are separate from append-order global logical shape slots; structural removals reindex later slots. Body/Area preparation uses slot resource revisions, owner pose/policy and shared native fixture construction. Arbitrary weak owner identity reaches KinematicCollision results. ShapeOwnerTests verifies query/motion/solver/sensor integration on explicitly selected CPU/GPU worlds and 64 warmed full frames with zero owner/all-thread managed bytes under [ADR 0071](../decisions/physics.md#adr-0071).

Logical shape-pair monitoring now uses a shared PhysicsShapePairTracker and committed transition queues. Native compound fixtures are deduplicated by RID and remote/local global slot indices; object/shape entry and departure ordering is preserved. Scene Area scans include server colliders with nullable payloads and explicit server Area monitorability. Callback failures continue later queued events; monitor policy changes inside in/out callbacks reject. ShapePairEventTests checks steady and active entry/exit paths without warmed managed allocation. [Physics monitoring decisions](../decisions/physics-monitoring.md) now own active ADRs 0055/0058; stable legacy anchors route there.

Typed PhysicsServer Area field getter/setters now share scene/server profiles and address a space default Area by its Space RID. Bounded fields use current exact overlap and descending mixed priority; monitor flags/callbacks are independent. Initial space defaults sample ProjectSettings, then explicit live edits apply on the next nonzero step without changing other worlds. Point/default gravity, body damping/scaling, wakeup and failure recovery are verified by [PhysicsServerAreaFieldTests](../../tests/Electron2D.Tests/PhysicsServerAreaFieldTests.cs) under [ADR 0056](../decisions/physics-fields.md#adr-0056). Native allocation, other platforms and owner visual acceptance remain unverified.

Scene/server membership and monitor configuration prepare object/pair/change storage from the existing fixture inventory. Body creation also prepares dormant solver-set identities and buffers, so first sleep after a long run can reuse them. Prepared graph storage is four contacts per rounded body-capacity slot; unusual denser topologies can exceed that tested budget and are outside the sandbox measurement. Cache capacity grows during explicit construction/configuration and remains owned by the world until disposal.

CPU field/overlap/audio geometry and retained priority scratch now belong to
CPUPhysicsWorldBackend. GPU resident Area scans and audio scratch belong to
GPUPhysicsWorldBackend; shared PhysicsSpace commits observer histories and dispatches
events after solving. [The shared CPU/GPU fixture](../../tests/Electron2D.Tests/PhysicsAreaBackendTests.cs)
checks mixed scene/raw fields, logical slots, masks, audio, disabled geometry, transfer,
reentry, free and 64 warmed step/audio/read cycles at zero owner/all-thread bytes.

[Registered Area fields](physics-registered-adapter.md#registered-area-fields) now exercise the complete typed profile/default shortcut through the selected hooks, including independent override/point/mask response and zero warmed edited full frames. Numerical-hook writes reject before mutation; authored reads and ordinary Area callback edits retain their guards and overlap history. Generic solver/extension acceptance remains open.


[Selected Area monitor publication](physics-registered-adapter.md#area-pose-and-monitor-publication) verifies pose, monitorability and both callbacks on CPU/GPU/registered owners. Scene and server observers share the own-receiver configuration guard while retaining independent histories. Full registered geometry and final network/performance acceptance remain open.

Prepared replacement bodies acquire the layer maps before any prior-body departure callback. Every old body and owned shape is retired even if a handler throws; completed replacement ownership is retained and errors escape after cleanup. UpdateInternals marks failed remaining work for retry. Exit/disposal also attempts navigation, drawing, diagnostic and runtime-data cleanup after physics failures. TileMapLayerTests checks replacement, disabling, exit/reentry and disposal with throwing shape-departure handlers on CPU/GPU, including actual navigation-region retirement.

Generated tile bodies retain TileMapLayer owner identity and individual RIDs through Area/rigid object and shape events, point/ray/shape/rest queries, scene casts and scene/server motion results. Object snapshots/events deduplicate multiple bodies for one layer; shape payloads retain remote/local logical indices. Generated server geometry has no scene shape-owner node. Explicit CPU/GPU tests check canvas-qualified point queries, reparenting, sampled weak owner lifetime and 64 warmed full-frame/span-query cycles at zero owner/all-thread managed bytes. Native allocations, other platforms and visual acceptance remain unverified.

The complete PhysicsAreaFieldTests, PhysicsServerAreaFieldTests and PhysicsAreaMonitorTests now execute on explicitly selected CPU/GPU worlds. They retain all five gravity/damping modes, priorities, point transforms/falloff, default-world shortcuts, signed damping, directional masks, logical pair identity, callback failure/removal and lifecycle assertions. Initial unchanged child registration preserves a sleeping body; changed fields still wake it. Sixty-four measured moving, sleeping and edited-field frames and 64 monitor transition cycles (128 frames) require zero owner/all-thread managed bytes. CPU field validation failure can be corrected; a started GPU field-reduction failure is terminal, preserves the selected backend and last published scene field snapshot, and permits resource retirement. Native allocations, other platforms, visual and final performance acceptance remain open.

## Point-gravity numeric range

Point reduction preserves constant-strength behavior for a nonzero finite offset,
including offsets whose square underflows a float. Positive unit distance retains
inverse-square falloff without forming overflowing/underflowing float squares.
The CPU reducer computes the distance, ratio and final components with wider
intermediates; directional gravity remains the authored, unnormalized vector.
Exactly coincident point/body positions and zero configured strength yield zero.
A genuinely unrepresentable resulting force still rejects before field-cache
publication, and CPU callers can correct its configuration for the next interval.

The resident GPU kernel scales the direction and combines mantissas/exponents
for each output component. It guards the [GLSL reconstruction limits](https://docs.vulkan.org/glsl/latest/chapters/builtinfunctions.html) for ldexp
and reports nonfinite resolved fields through the existing failed-step policy.
This remains device work: no CPU field mirror, transfer or wait is added.
The revised module now executes the numeric-range cases and terminal point-force
overflow through a CPU Vulkan adapter; [software validation](physics-backends.md#software-vulkan-functional-checks)
records the actual driver and profile. The hardware guard below now adds physical GPU numeric-range execution; broad GPU performance acceptance remains open.

PhysicsPointGravityRangeTests runs nine independently specified cases through
scene Areas, raw server Areas and default-space point fields, each with RigidBody,
CharacterBody and raw body receivers. It checks full finite solver intervals,
signed/constant/inverse-square/zero behavior and recoverable real overflow.
Tolerance is max(0.002 scene acceleration units, 0.0002 of the largest expected
component), rather than CPU/GPU bitwise agreement. The CPU path also measures
128 warmed reset/full-field frames with zero owner/all-thread managed bytes.
It is connected to the shared backend and complete CPU suites; native allocation,
other drivers/platforms, performance and full-goal acceptance remain open.


The CPU point reducer also widens the affine center transformation and subtraction
from body position before measuring/normalizing the offset. Finite authored point
and body coordinates may have a difference larger than a float while still yielding
a finite constant or inverse-square force; this is not a reason to reject the field.
Only final acceleration components are narrowed, preserving the existing real
nonfinite-result guard and directional-vector behavior. The resident GPU kernel retains its ordinary finite-offset path and scales
authored point/pose/body coordinates before their affine sum when that intermediate
overflows. It performs normalization and falloff on-device with no new publication
mirror, transfer or wait.

The same point test now covers default-space centers/body positions at opposite
2e38 scene coordinates, both directions and constant/inverse-square policies, with
RigidBody, CharacterBody and raw body receivers. Two CPU affine checks additionally
exercise rotation/translation producing a virtual center outside the float range,
using independent 3:4 direction and falloff expectations. A separate 128-frame
wide-coordinate warm bracket per direction requires zero owner/all-thread allocation.
Each direction starts its own world so the field check does not request a physically
unrepresentable one-tick kinematic transition between opposite extreme poses. These are
field and finite-state checks; they do not establish collision resolution precision
at huge coordinates or hardware GPU/performance acceptance.

## Hardware point-field guards, 2026-10-14

On NVIDIA GeForce RTX 3090 Ti/Vulkan the old infinity-sentinel branch could produce a weak finite field for an actually unrepresentable point force. Adding a temporary diagnostic changed that result; ordinary compiler-sensitive field arithmetic did not provide a reliable error marker. The opposite-coordinate test also failed its valid field. Temporary diagnostics and status probes were removed before delivery.

The resident point-field reducer preserves guarded float operation order with GLSL precise intermediates and propagates reconstruction validity separately from the value. Values beyond the finite output range do not enter subsequent arithmetic as infinity sentinels. A later Replace/ReplaceCombine contribution resets validity with the channel; Combine retains an invalid earlier contribution. Final invalid fields fail before body-field publication or solver motion through the existing eight-byte status read and terminal GPU-world policy. Mantissa/exponent normalization avoids reciprocals outside the normal range for large/tiny coordinates. No CPU mirror, buffer growth, new readback or wait is introduced.

The [GLSL precise rules](https://registry.khronos.org/OpenGL/specs/gl/GLSLangSpec.4.60.html) preserve the marked operation order through optimization; the compiled modules retain NoContraction decorations. Field finiteness examines IEEE bit ranges. The existing field result/cache ownership, publication, failure retirement and no-CPU-replay policies remain unchanged.

PhysicsPointGravityFailureTests verifies 18 independent source/sign/axis combinations on CPU/GPU: scene Area, raw server Area and default-space fields; positive/negative strengths; X/Y/diagonal centers. It also checks the exact maximum finite component and an earlier overflowing contribution discarded by lower-priority Replace, then verifies that Combine retains the failure. CPU recovery, GPU terminal rejection, unchanged last fields, selected backend and resource retirement are asserted. PhysicsPointGravityRangeTests still exercises nine independent range cases × three profiles × three receiver roles, opposite finite 2e38 coordinates and 128 warmed full frames with zero owner/all-thread managed bytes. Serial and separate numerical scheduling passed the same device point-range/failure profile. The default harness now includes CPU/GPU point-range tests. Native allocation totals, other hardware/OS backends, collision accuracy at huge coordinates and general performance acceptance remain separate gates.

The executable candidate comparison selected this failure contract over KTX loading (ready container primitives and BC consumers, but additional format/container semantics remain) and GPU timestamps (no actual timestamp producer in the pinned SDL GPU API). The public field workflow was already claimed and its real hardware failure stopped the full runtime suite; correcting it restores that executable contract and its dependent tests.
