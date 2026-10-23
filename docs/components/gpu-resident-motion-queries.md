# Resident GPU body-motion queries

Last updated: 2026-10-23

The selected GPU world owner now receives engine-unit body motion and typed RID exclusions from common server/space code, then invokes this resident driver. CPU fixture lists no longer pass through that boundary. Platform point velocity resolves current body RID/world membership and uses the selected resident collider attachment. See [shared ownership checks](../classes/PhysicsWorldBackend.md#body-motion-and-platform-lookup).

## Boundary

GPUPhysicsBodyStore.TestMotion tests a registered body's shapes at a supplied pose,
without changing its live transform, advancing simulation or creating a CPU world.
A batch reads current GPU geometry, tree, poses and center-aware point velocities.
The CPU uploads authored queries, attached shape tokens, incident explicit
exceptions and collision-disabled joint peers; it does not read all body state or enumerate potential collision pairs.
Public PhysicsServer.BodyTestMotion, PhysicsBody movement and CharacterBody use
this path in a GPU World; CPU worlds retain their solver. [Public scene motion
conformance](physics-backends.md#public-scene-motion-conformance) records the shared
checks. The remaining full-contract and network obligations stay open. The shared
[adaptive query readback](gpu-resident-queries.md#device-work-and-traffic) preserves
one result per motion request, including recovery travel on a miss, with one fence.

## Behavior

Four recovery passes retain up to 32 deepest accepted contact planes. Sequential
projection applies 40% of excess penetration weighted by obstacle CollisionPriority,
normalized to mean one except for near-zero total priority. The allowance is 5% of
the motion margin. [Collision priority](physics-contact-policy.md#collision-priority)
describes metadata, numerical limits and verification. Ordinary shapes expand by the query margin; directed rays extend along
their positive axis. A zero margin retains the existing 0.0001-scene-unit minimum.
A recovered pose then tests the requested translation. Residual penetration above
0.05 scene units or inward motion stops at fraction zero. Ordinary conservative
advancement retains the actual root inside the 1/256 motion bracket. A final
scan samples the deepest eligible margin-inclusive contact after all recovery
corrections at the unsafe motion pose. New impacts retain their logical source
slot; recovery-only scans consider all source slots. Directed entry probes reject
negative-depth speculative manifolds before the eight refinements. A miss still
returns recovery plus requested travel and clears collision identity/contact
fields, including after a complete departure from the original overlap.

Moving-mask/target-layer filtering, both directions of explicit body exceptions, caller collider and
object exclusion spans, sensor rejection and one-way direction/margin rules apply.
Each active collision-disabled joint also excludes its peer, as required by ADR 0087.
Multiple joints and explicit exceptions contribute independently; a fixed-world
pin has no peer to exclude. Non-sliding rays
require CollideSeparationRay for the motion phase; sliding rays participate
normally, and both participate in recovery. Ray/ray and ray-origin containment
reject. Shared directed geometry serves both standalone shape and body-motion
queries; complete convex contours have no eight-vertex limit.

Each hit carries logical local/collider slots, collider/object identity, physical
shape/body generation tokens, point, outward normal, depth and collider point
velocity, travel/remainder and safe/unsafe fractions. Object zero denotes a collider
without a managed object. Physical tokens remain internal, not network identities.
The batch preserves unused output tails and validates owner thread, lifetime,
generation, finite unit-scale pose, motion, margin, ranges and destination capacity.
GPU failure poisons the store; queries never replay through CPU.

The motion shader shares one geometry evaluation call site across initial overlap,
directed reach, eight bisection refinements and final impact. Boundary and finite
ordinary geometry share one manifold call; either directed-ray argument uses one
directed-query call. This limits repeated expansion during driver compilation.
Recovery still makes four passes with the same plane cap; contact tolerances,
shape families, GPU buffers and host transfers are unchanged.

Metal/DXIL uses six ordered device passes: four recovery attempts, translation,
then rest sampling and final output. Four pipelines cover initial recovery,
continued recovery (reused three times), translation and final output. Two vectors in the existing result prefix
carry recovery, rejected-key count, fractions, source slot, convergence and flags between
passes. The same command buffer ends each compute pass before beginning its
consumer, providing storage-buffer visibility without an intermediate host wait.
The portable result/scratch buffer stores uint words. Floating fields explicitly
bitcast at numeric reads/writes, while flags, counters and identities remain
integers through storage and final result publication. This avoids expressing
small integer bit patterns as floating subnormal values in legacy compilers.
The 32 recovery planes and 32 rejected keys remain in the existing device scratch;
only the final 128-byte result is downloaded. Vulkan retains its single optimized
module. Numerical limits, early recovery convergence, zero-motion skipping and
the eight directed refinements are unchanged.

Recovery passes carry only recovery state. Translation initializes its full-motion
bracket in the existing final prefix word, loads it before each candidate pair
and stores it immediately afterwards. The caller reloads that same record after
the complete scan. Final rest sampling retains each accepted deeper contact in
the first six prefix words, using the original depth threshold; final output
reloads the sweep, recovery and retained contact records. Portable query bounds
start with the first transformed vertex, then visit all remaining vertices in
order. The caller already excludes empty geometry. All four portable stages now
use offline SPIR-V optimization. No buffer, transfer, fence, geometry visit or
physical refinement is added or removed; the ordinary Vulkan branch is unchanged.

The preceding optimized DXIL diagnostic passes the complete public GPU compound
fixture in run 39997310087 at `dbbfc9c2b`, including scene/raw/Area slots, momentum,
private history and 128 cycles at 0/0 bytes. The first hit retains safe/unsafe
fractions 0.49609375/0.5 and travel 9.921875. The corresponding complete Windows
control is run 39999737324 at `7e186ac72`; its final result is still pending.
That control includes diagnostic result-lane observations. The production port
removes them and requires its own complete Windows/Metal execution gate.

Earlier DXBC, explicit pair-result, traversal and bounds-only controls did not
correct the Windows miss. The pair/scan observations narrowed lost state to the
translation accumulator and final contact. They do not establish a definitive
driver/compiler root cause. Those temporary source rewrites are retired from the
manual workflow, which now runs the production motion source with cold-pipeline
logging and the existing failure-preserving debugger replay.

Local production-port controls force the ordered path on Linux Vulkan without
shader probes and pass the complete compound, functional motion and resident/
public one-way cohorts. Warmed intervals remain 0/0 bytes. The ordinary Vulkan
module retains SHA-256
`79fb6af39ee14df2d86e25cbc419dfc1596ad1275211eb97258c165cab41d87f`.
All five modules retain eight readonly buffers, one uniform and 64x1x1 workgroups.
Optimization removes the unused center binding from the first three portable
stages, which reflect three writable buffers; final output and ordinary Vulkan
reflect four. Host storage, binding positions and final result size are unchanged.
Generation checks pass include invalidation, phase separation, cleanup and
fresh-consumer embedding. These are local checks, not Windows/Metal acceptance.

## CPU corrections found by comparison

Backend pieces of a large convex polygon previously produced opposite recovery
normals: an embedded 12-vertex contour moved only about 1.45 units rather than
recovering about 20.32 units outward in the exercised case. Motion contacts now use
the existing full-contour SAT scratch in PhysicsShapeCollision for recovery,
initial penetration and impact geometry. Primitive-only contacts retain their
existing path. PhysicsFixtureTag borrows the complete resource weakly and retains
its authored local pose, so stale query candidate lists cannot own the resource.

Directed queries also test ray-origin containment against the whole contour or
its swept region before selecting any backend piece. This fixes an observed false
CPU hit from inside the polygon while preserving genuine outer-boundary hits.
The shared shape-resource hull builder and SAT storage are reused; backend fixed-size
hulls are not enlarged. CPU-only PhysicsMotionTests retains recovery in both
polygon roles, both ray-containment directions and warmed zero allocation.

## Verification and cost

GPUPhysicsMotionQueryTests compares circle/rectangle/capsule/segment/full convex/
paired concave and directed ray motion with public CPU body queries, including
rotated local/supplied poses, both ray policies, one-way surfaces, initial/deep
penetration and unchanged live transforms. Independent checks exercise explicit
exceptions, joint vetoes and live toggles, fixed-world pins, exclusion ranges, logical/object identity,
sensor/mask policy, angular surface velocity with custom COM, batch tails,
slot reuse, validation and lifecycle errors.

Geometry comparisons account for CPU approximate rotation construction. The
circle/floor tests allow 0.8 scene units of travel, 0.008 fractions, 0.1 contact
position, 0.001 normal and one unit of depth; complex families allow 1.2 travel.
Compound/ray cases avoid equal-depth vertex-normal ties and allow one unit of
travel. These are scoped convergence tolerances, not bitwise parity promises.

Each request uploads 80 bytes plus 8-byte shape/exception/joint-peer tokens and supplied
exclusion keys. One result reserves 128 bytes plus a 4-byte count; the batch has an
8-byte status exchange. Recovery and rejected-pair keys reserve another 1024 bytes per query in the GPU
output buffer (512 bytes each); this tail is never uploaded or read back. Inputs, outputs, scratch and transfers retain warmed
capacity. The shared driver safely alternates ray, shape and body-motion strides;
query-only tree preparation skips simulation pair enumeration. Changed world
geometry/poses require tree work; unchanged worlds reuse it.

The focused workload creates 65,536 static rectangle obstacles and one mover,
then executes 256 supplied-pose tests per batch with 96 warmup and 128 measured
batches. Every output is checked. Build tests in Release with
`-p:Electron2DBuildNativeFromSource=true`, then run
`ELECTRON2D_TEST_GPU_MOTION_QUERIES=1 dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release --no-build`.
This measures query submission, fence and readback against a static world;
it does not measure full simulation, dynamic refit cost, native allocations or
window FPS. Other platforms, public backend integration and user acceptance
remain unverified.

On Linux/.NET 10, Vulkan, NVIDIA GeForce RTX 3090 Ti, the focused run after sharing
shader call sites measured p50/p95/p99 **1.1254/1.4306/1.5208 ms** per 256-query batch,
with mean wait **1.1113 ms**, **22,536/33,800 upload/readback bytes per batch**, and **0/0
owner/all-thread managed bytes** over the measured interval. Evidence:
`/tmp/e2d-motion-cold-tests.log`. Timing includes all requested
queries and their recovery work; the world population is not reduced for measurement.

The complete GPU suite also passed with the same Release runner:
`ELECTRON2D_TEST_GPU_PHYSICS=1` (`/tmp/e2d-motion-full-gpu.log`), including failure,
lifetime and both renderer-independence paths. The focused selector also runs public
CPU PhysicsMotionTests and SeparationRayShapeTests. All 37 CPU collider groups
passed during the earlier shared directed-query extraction
(`/tmp/electron2d-motion-query-cpu.log`); that broad CPU run was not repeated for
this shader-only change. These checks do not establish public independent-GPU or
networking acceptance.

WorldBoundaryShape participates analytically in both query argument positions,
including far-away half-plane contacts, normal/distance transforms and ray pairs.
The shared shape and body-motion matrices now include this geometry. Direct-space
shape queries retain initial overlap during motion; body-motion recovery/casts
retain their own directed-ray policy. The [public GPU world adapter](physics-backends.md) now uses these kernels; full family conformance remains open.

## Cold pipeline preparation

`ELECTRON2D_TEST_PHYSICS_PIPELINE=/absolute/file.spv` loads an offline program through
the production GPUPhysicsDevice/ShaderCompiler path, times pipeline creation and
releases it. Device startup is timed separately. On the machine above, sequential
fresh processes with `__GL_SHADER_DISK_CACHE=0` produced:

| Motion shader | Samples | Pipeline creation, ms |
| --- | --- | --- |
| Before call-site sharing, f9f92da4 | 1 | 124,723.38 |
| Shared call sites | 3 | 12,863.22 / 13,711.77 / 15,887.83 (min / median / max) |

The new median is about 9.1 times faster than that baseline. Device startup took
301.48–555.52 ms in the new processes and 411.13 ms in the baseline process. Both
programs used the pinned Vulkan 1.0 offline recipe without optimization-flag changes;
SPIR-V sizes were 151,524 and 150,344 bytes respectively. The log records full
artifact hashes: `/tmp/e2d-motion-cold-serial.log`. This is cold creation of one
pipeline, not whole-world startup, simulation throughput or a cross-device guarantee.
Earlier parent measurements of 134–137 seconds are consistent with the same large
baseline cost but are separate runs.

Sharing only the outer recovery/motion scan did not reduce compilation time and
was discarded. Sharing initial/reach/refinement/impact geometry calls reduced it;
sharing the ordinary and directed evaluator branches reduced it further. The
original four-pass recovery loop remains. Folding recovery into the same phase
loop was slower than the selected version and was discarded. An offline `-Os`
experiment on the original source emitted repeated optimizer ID-overflow errors
despite returning success and valid bytecode; no compiler-policy change is shipped.

The focused geometry matrix, collision-priority checks and complete GPU suite pass
without changing tolerances, iteration counts or workload sizes. Cold creation still
takes seconds on this device. Public independent-GPU binding and startup selection now execute. The [shared scene checks](physics-backends.md#public-scene-motion-conformance) exercise character sliding, platforms and ray snap on both backends. Full-world performance and networking remain open acceptance work.

## Moving one-way recovery

Body motion now extends a kinematic/dynamic obstacle's accepted one-way depth by
its linear displacement towards the solid face over the last physical step,
including the locked-rotation dynamic role. This is added to the larger of the
shape and query margins. Static virtual surface velocity, receding/tangential
movement and angular-only motion do not add depth. Source normal/direction,
filters, logical slots, exclusions and supplied-pose semantics are unchanged.
CPU recovery/impact validation and resident GPU recovery/impact share this policy
under the updated ADR 0065. Fixed-step one-way episode selection is unchanged.

The scene/server GPU bridge explicitly supplies PhysicsSpace.LastStep, preserving
private and portable restoration even when numerical warm history is reset. Direct
store queries default to the retained outer simulation duration rather than a
substep/CCD duration. The existing unused fourth motion-margin lane carries this
value; request/result strides, scratch, uploads and readbacks are unchanged. No
host pose mirror, CPU geometry fallback or added GPU wait is introduced.

PhysicsMovingOneWayTests runs explicitly selected CPU/GPU worlds in ordinary and
separate numerical-thread modes. Raw BodyTestMotion and scene CharacterBody.TestMove
exercise all four obstacle roles, local/body rotations, six finite source families,
zero history, approach/recede/tangent/angular controls, actual duration changes,
private/portable restore, live margins, disabled index gaps and unchanged live
poses. Eight brackets each warm 128 complete frames and measure 128 at strict
0/0 owner/all-thread managed bytes. Numerical caller travel comparison uses .003
scene units; identity/role/direction/report and allocation decisions are exact.
The initial CPU/GPU build rejected the rising kinematic platform; evidence is
retained under bin/physics-moving-oneway/2026-10-14. A segment control now uses a
thicker obstacle with the same three-unit penetration so its zero-width edge is
inside the solid rather than beyond its underside. Temporary diagnostics are
removed from production source. Native allocation, other platforms and target-
hardware/window/performance acceptance remain open.

The final retained Release runtime passes ten fresh processes: four moving-platform
profiles (CPU/GPU, ordinary/separate numerical thread), CPU/GPU query and filter
contracts, resident motion and resident one-way suites. CPU profiles map no graphics
driver; GPU profiles map only CPU Vulkan lavapipe, with no NVIDIA driver. All eight
new allocation brackets per moving profile report 0/0 bytes. Release build, scoped
runtime/test format, compiled coverage and wiki test/generate/check gates pass.
The compiled surface remains 13,961 declarations; wiki output has 832 public types
and 850 files. Source/runtime manifests, process driver/thread proofs and logs are
retained in bin/physics-moving-oneway/2026-10-14/validation.json. These software
results establish functional regression evidence, not target-GPU speed or FPS.


[Special source motion](physics-registered-adapter.md#special-source-motion) retains
logical source-kind precedence during resident queries instead of sweeping every
physical piece as ordinary geometry. Recovery and residual stopping remain active;
two concave resources do not create piece contacts. The public --motion-matrix
checks all 64 ordered families, scene/raw/TestMove paths, live replacement and
128 complete warmed moving frames per profile with strict zero managed bytes.
Use ELECTRON2D_TEST_GPU_MOTION_QUERIES=functional for resident batch/geometry/
filter/lifetime/velocity checks without the separate massive measurement; value 1
retains that complete measurement. Software execution is not hardware performance.

## Final contact after motion

[Final body-motion contact sampling](physics-registered-adapter.md#final-body-motion-contact-sampling) replaces the earlier recovery snapshot and speculative ordinary impact pose with a current deepest contact. The final device scan supplies its point velocity and logical identity and uses the existing buffers and transfer count. Public-only software-Vulkan checks include every source family, an entry between ray refinement samples, a changed deepest collider after the last priority correction, zero/positive margins, scene/raw recipients and complete-frame zero managed allocation. Historical hardware timings above precede this kernel change; its target-device cost and FPS remain unmeasured while the power-cable check is pending.

## Rejected pairs after recovery

[Last-recovery one-way pair exclusions](physics-registered-adapter.md#last-recovery-one-way-pair-exclusions) keep up to 32 rejected source-geometry/target-slot keys across the sweep and final sample. All represented pieces contribute acceptance before a logical pair is rejected, and each recovery attempt replaces the set. Shared source resources share rejection; target slots and distinct equal resources remain separate. The additional device-only 512 bytes per query add no uploads, readbacks or fences. Public prepared-frame checks cover last-correction side/depth changes, aliasing, accepted concave edges and the 32/33-pair boundary. Historical hardware timings above precede this kernel; current target-device cost remains unmeasured.

## One-way impact admission

The resident motion kernel separates initial-overlap departure from [new impact side selection](physics-queries.md#one-way-body-motion-entry). A new one-way impact evaluates its normal at the unsafe pose before committing the earliest fraction, including tangent/receding front-corner approaches. Ordinary prospective geometry includes the query margin in its local hull radius, preventing a rounded cast root from losing its validation contact. Directed source bypass and recovery/final-depth checks retain their existing policy. The extra narrow contact evaluation stays on device; request/result layouts, scratch storage, uploads, readbacks and fence count are unchanged. This is a functional change, with no new hardware performance claim.

All-motion native DXBC run 39948784588 creates the four pipelines, then rejects the first compound response motion result. The integer-word transport change is a candidate correction; it retains the numerical workload and requires target verification. Failure messages now report hit, actual/expected RID, local/peer slots, travel and unchanged pose without allocating in successful measured cycles.

Optimized DXIL diagnostic run 39986163256 at 59aa16412 creates all four pipelines
but returns the same first-query miss. Its source header is
`0x0000000100000001`: one vertex, enabled circle policy and circle geometry kind.
The source-visit bit is present; root, leaf, candidate and pair bits are absent.
The explicit descent/advance control also misses. These receipts narrow the gap
to source validation through the first node read, without establishing a compiler
or driver cause. The next diagnostic brackets query-bound evaluation and reads
the vertex count from the live local geometry. Its unchanged compound control
passes locally with all reachability bits, but Windows execution remains required.

The query-bound probe in run 39988589701 at 58bbb2439 records state
`0x21044F02`: the call-entry mark is present, its return and all node marks are
absent, and the live source vertex count remains one. The fixture still fails.
The next diagnostic seeds bounds from the first vertex and starts its loop at
one, preserving the nonempty caller contract and vertex order. Its ordinary
Vulkan branch remains unchanged; this candidate is not a production correction.
The local ordered compound and functional motion controls pass with this change.
The ordinary Vulkan module keeps SHA-256
`79fb6af39ee14df2d86e25cbc419dfc1596ad1275211eb97258c165cab41d87f`.
Target execution and the complete platform gates remain required.

With seeded bounds, optimized DXIL run 39990536686 at ec9ffbd21 records
`0x7FC6CF02`: query-bound return, every traversal mark and pair entry/return are
now present. A pair-return observation sees blocked state, but the translation
exit and public result still miss. Unoptimized run 39990539157 still exits
`0xC0000409` during the first motion pipeline creation. The candidate therefore
does not close either target gate. The next receipt captures safe fractions
immediately after a pair and at the end of the source scan, using existing result
padding; no numerical result is replaced or accepted from these observations.

Run 39992096151 at b407ae189 records pair safe `0.49609375` but the end-of-scan
slot remains at its initial value `1`; the public result is still a miss. This
proves that the pair's valid fraction is not present in the final phase state,
without proving that the end-of-scan marker itself executed. The next diagnostic
loads and saves the translation accumulator around each candidate pair using the
existing phase-state word, then restores it after scan. Seeded bounds remain in
that candidate; recovery, traversal, geometry work and the ordinary Vulkan branch
are unchanged. Native execution is required before adopting either change.
The local ordered compound and functional motion controls pass with persistence;
their pair/end-of-scan fractions both remain `0.49609375`, and ordinary Vulkan
retains SHA-256 `79fb6af39ee14df2d86e25cbc419dfc1596ad1275211eb97258c165cab41d87f`.

Persistent-sweep run 39994374405 at 173ab9152 records `0x7FD2FF02`: the translation
history is now 767 and final blocked is true, while rested remains false and the
public query still misses. The candidate therefore preserves translation state
without completing the target fix. The next control stores each accepted deepest
rest contact directly in the result prefix, retaining its original strict depth
comparison and clearing the hit marker before scan. It restores fractions and
recovery after scan, with diagnostic words kept in a retired recovery-plane slot.
Request/result sizes, excluded-pair keys, workloads and transfers are unchanged;
this remains a diagnostic copy until target execution passes.
Local ordered compound, functional motion and resident/public one-way/polygon
checks pass, including their original zero-byte gates. The ordinary Vulkan module
still has SHA-256 `79fb6af39ee14df2d86e25cbc419dfc1596ad1275211eb97258c165cab41d87f`.
