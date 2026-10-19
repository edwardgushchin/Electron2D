# Directional physics filters

Body/Area filter getters now use [prepared collider metadata access](physics-registered-adapter.md#prepared-collider-metadata-reads) during selected numerical/status hooks. Reads preserve authored bits, including inactive PhysicalBone filters, while mutations keep their existing numerical-borrow guards.

Last updated: 2026-10-19

## Filter setter operation audit

The complete Body/Area layer/mask setter family now has a bidirectional audit
against 22 pinned declarations: public and required implementation getters/setters,
both CollisionObject properties and all four one-based bit helpers. Nine pinned
source receipts verify defaults of one, uint32 native boundaries, required/const
metadata, absent deprecation and bit numbers 1 through 32. Typed unsigned values
preserve all category bits. Existing prepared getter operations remain complete;
only the four outstanding implementation setters change coverage state.

The common runtime keeps previously committed authoring after failed numerical
publication and repairs complete effective filters before a later query or step.
The public-only `--filter-transactions` profile covers eight scene/raw roles,
40 ordinary edits and 160 registered partial-failure cases. Its selected hook now
observes full proposed layer/mask arguments, owner threading and the Body/Area
wake distinction. Forty successful edit/restore pairs invoke the selected update
80 times; each measured 128-frame bracket requires 2,048 selected updates and
128 actual numerical hooks on the captured serial/separate lane. Prepared reads
retain configured values while an inactive PhysicalBone receives zero effective
bits. Geometry rebuilds remain zero, and resource/RID/canvas/slot identity stays
stable.

A zero-layer RigidBody with its own matching mask receives a real circle impulse
against a zero-mask static peer and reports logical shape index 2. Rejecting both
masks instead gives free motion and no contacts. Velocity/position comparisons
use .01/.001 scene-unit bounds for float integration; a greater-than-five impulse
separates the response from the six-unit incoming motion. The physical case resets
pose/velocity explicitly. Its initial checkpoint-based reset was rejected by the
existing fixed-configuration GPU guard after configuration authoring; no runtime
history contract or verifier was changed.

Fresh JIT/strict Linux x64 NativeAOT CPU/no-device, software-GPU and registered
serial/separate filter and prepared-input profiles execute the finished sources.
Factory-view and shape-policy companions retain the original borrowed FilterHook
behavior while the new argument probe is unused. Root CPU/GPU filter checks add
all bits, directional impulse/CCD, actual sensor/contact transitions, callback and
lifetime guards and body/neighbor wake on equal assignments. Generated tile-body
checks retain defaults, bit 32, immediate query/sensor changes and tile/RID/shape
owner identity. Tile unchanged-frame allocation also has its existing owner-only
measurement; this is separate from strict owner/all-thread brackets.

Evidence: `bin/physics-filter-operation-audit/2026-10-19/validation.json` and its
contract/source/profile receipts. Four setter rows become Implemented. The unique
physics ledger retains 171 open declarations (140 Partial, 14 Unimplemented,
17 Blocked). Runtime C# and public API are unchanged. The ordinary frozen
`8912effe` runtime success remains qualified separately from these fresh test
cohorts. Complete arbitrary registered numerics, inherited/extension/network,
native memory, foreign platforms and target-GPU/window performance remain open.

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


Ordinary direct-body-state extension writes may forward BodySetCollisionLayer/Mask for scene and raw bodies. These operations use the common body parameter-write guard, retain raw release/borrowed-geometry validation, and reject numerical-hook writes before mutation. Filter metadata updates preserve their existing query, wake and next-step observation semantics; the public factory-view fixture exercises both siblings and body roles.

## Failed updates and effective sensing

Scene properties, their one-bit helpers and static server setters now publish
configured layer/mask bits only after the selected numerical update succeeds.
The internal scene forwarding hook receives proposed values explicitly, so
borrowed authored getters retain the previous committed values for scene and raw
objects alike. Unchanged body assignments still request body/neighbor wakeup.

A failed UpdateFilter leaves a pending bit on its collider and its world. The
next numerical query or active positive-duration step resubmits current effective
bits, using wakeBody=false, before publishing query output or beginning the solver
interval. Another failure propagates and remains pending. Filter repair changes
existing metadata without recompiling shapes or advancing a failed interval.
Successful shape rebuilding and detach clear collider pending state. A world bit
skips repair traversal on ordinary frames; no filter/state array mirror, new GPU
readback or ordinary per-frame population scan is introduced by this repair.

Effective body filters also govern CPU scene-Area and external server-Area scans.
An inactive PhysicalBone keeps configured public bits while its effective layer
is zero, so neither sensing path reports it. Direct queries, motion and resident
GPU sensing already use that effective policy.

The public-only `--filter-transactions` profile covers StaticBody, AnimatableBody,
CharacterBody, RigidBody, inactive PhysicalBone, scene Area, raw body and raw Area.
Forty ordinary edits use direct properties, bit helpers and static facade setters.
The registered provider injects 160 partial numerical failures across both fields,
entry paths and query/step retry routes. Authored bits and associations remain
committed, retries propagate the original failure, failed preparation does not
advance the world tick, logical slots 0/2 retain a disabled gap, and sensor history
does not replay. A separate scene Area also checks its empty follower snapshot
and zero entry events alongside the raw observer. A shape hook counts rebuilds and requires zero throughout that provider.

Real point/motion queries and exact departure/reentry counts verify successful
edits on each selected implementation. Local history restores after registered
failure repair; after accepted authoring changes the profile recaptures its local
checkpoint under the existing fixed-configuration contract before silent replay.
The warm bracket contains 128 full frames after 128 warm frames, with sixteen
filter assignments and fourteen queries per frame, strict 0/0 owner/all-thread
managed bytes, 128 force callbacks, stable RID and 12.8 units of actual movement
within .01 units. Native/driver budgets, arbitrary private side effects of custom
solvers, foreign platforms and target-device/window performance remain open.
Evidence is retained under `bin/physics-filter-transactions/2026-10-16/`.
