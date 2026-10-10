# Directional physics filters

Last updated: 2026-10-10

## Public behavior

[CollisionObject](../classes/CollisionObject.md) and the static
[PhysicsServer](../classes/PhysicsServer.md) layer/mask operations share the same
32-bit authoring. BodyGetCollisionLayer/Mask resolve scene bodies, caller-owned
bodies and generated tile bodies while attached, detached or geometry-disabled.
An inactive PhysicalBone retains configured filters while its effective follower
fixtures use zero. RID, shape resource, owner and logical indices remain stable.

Either endpoint can admit a physical pair by including the opposite layer in its
mask. Each dynamic endpoint receives normal/friction impulse and positional
correction only if its own mask includes the other layer. Its actual mass/inertia
and independent force/joint response are unchanged. A nonresponding endpoint still
contributes its actual linear/angular point velocity. Exceptions, joint vetoes,
one-way geometry and sensor roles continue to apply.

Body motion and each moving endpoint's CCD test its mask against target layers;
the target's mask cannot enable or veto that direction. Direct queries retain the
query-mask/target-layer rule. Area monitoring remains directional from the
observing area. Zero category bits can still admit a responding body through its
mask; bit 32 is preserved in both implementations.

## Mutation and ownership

Filter assignments update existing fixture metadata without recompiling geometry.
CPU may reinsert a conservative tree proxy and retire old contacts; GPU updates
resident shape filter records. Queries observe changes immediately. Contacts,
overlap snapshots and transitions publish on the next step. Repeated assignment
of unchanged body bits wakes the body and current contact neighbors without
replaying contact events. Area assignments keep their sensor policy.

Scene setters, server getters and server setters use existing owner/solver/failed
world guards. Invalid or wrong-kind RIDs reject. Callback failures retain committed
filter changes and finish later eligible callbacks; the next step remains usable.
Tile-body runtime edits preserve the TileMapLayer association. A later authoring
rebuild can reassert TileSet policy.

## CPU and GPU execution

CPU conservative tree bits include both category and mask bits. Actual pair
admission uses either matching direction, while scalar/vector contact constraints
and graph caches use response-qualified effective masses. Continuous collision
uses the moving direction. The older stage GPU diagnostic also receives those
qualified masses.

Independent GPU broad phase retains asymmetric pairs. Resident constraint rows
encode response flags alongside the existing row role; warm starts, colored
updates, Jacobi gather and position correction preserve directional response.
Incident lists include only responding endpoints. No body-state mirror or new
readback is introduced by this filter policy.

## Verification and limits

`ELECTRON2D_TEST_FILTERS=1` runs PhysicsFilterTests on explicitly selected CPU and
GPU worlds: all bits, scene/server view agreement, four body roles, attachment and
disable/reentry, immediate point/motion queries, sensor/contact transitions,
one-sided velocity/position response in both creation orders with/without a pin,
moving passive endpoints, CastRay/CastShape, unchanged neighbor wake, callback
failure and invalid/off-thread access. TileMapLayerTests covers generated body
filters and stable geometry/owner projections.

Exact assertions cover bits, identity, counts and lifetime. Passive linear/angular
velocity tolerances are 0.001 scene units/s and rad/s; passive position allows 0.01
scene units over four 1/60 s steps for float integration. CCD checks distinguish
stopping before a 0.2-unit wall at x=20 from a free 60-unit path; free speed allows
0.1 units/s rounding at 3000 units/s. Responding-body checks require a material
velocity change rather than CPU/GPU bit equality.

After a blocking collection and 96 warmup edits, 128 raw-body edit/query/full-step
cycles, 128 four-role scene cycles and 128 active contact-toggle cycles with
independent sensor membership each measure zero owner/all-thread managed
bytes on both backends. This capacity-prepared fixture is an allocation check;
it is not a throughput benchmark or a native-allocation measurement.

Kinematic/static and kinematic/kinematic report-only pairs now execute under the
shared contact pipeline when either endpoint reports; two static bodies remain
ineligible. [Report-only verification](physics-report-only.md) covers the boundary. Full physics conformance, large scenes, network topology, renderer FPS,
native allocations and additional device/platform acceptance retain their gates.
[ADR 0102](../decisions/physics-filters.md#adr-0102) owns the policy.

## Recorded checks

The full default Release runner passes after updating reciprocal-mask fixtures.
Explicit CPU/GPU tile, CCD/boundary/ray, joint and public-world suites pass. The
public-world failure test rejects the new filter reads/writes after a terminal
GPU step, releases its resources and checks CPU/no-device startup plus observable
fallback in a separate process. Resident motion-query tests cover both moving-mask
rejection and a zero target mask.

The separate-process network check passes with CPU authority, GPU prediction and
a late CPU client, impaired delivery, lifecycle/ownership, confirmed events and
convergence. Its measured warm groups contain 61 server steps, 27 GPU predicted
and 109 GPU replay steps, plus 61 late CPU predicted and 163 replay steps. All
owner/all-thread warm step and replay counters are zero. Cold allocation counters
remain separate. These scenarios preserve networking behavior; exhaustive network
and performance acceptance is still open.

Release runtime build, both formatting checks, compiled API/coverage, wiki tests
and generation/check, production identity scan and diff validation pass. Raw logs
and network reports are retained in ignored `bin/physics-filter-validation/2026-10-10/`.

Resident CCD policy tests now reject both pair directions explicitly in the
masked pass-through fixture. The response/friction fixture keeps the wall mask
zero while the moving body admits its layer, verifying that this reverse mask
does not veto continuous physical response.

The randomized resident spatial oracle now admits either mask direction for
ordinary body pairs, while retaining directional sensor checks and role/AABB
conditions. Its complete expected pair set still checks canonical identity and
uniqueness across 36 authored motion frames.
