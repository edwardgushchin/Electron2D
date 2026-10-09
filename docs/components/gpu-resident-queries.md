# Resident GPU ray and point queries

Last updated: 2026-10-09

## Boundary

GPUPhysicsBodyStore.Query performs batched world ray and point queries against
current resident geometry, without a CPU solver world or a CPU pose/bounds mirror.
PhysicsDirectSpaceState now selects this path for public GPU worlds; CPU spaces retain Box2D.NET. [Resident shape sweeps/contact/rest queries](gpu-resident-shape-queries.md) now execute internally. [Body-motion recovery](gpu-resident-motion-queries.md) also executes internally. The [shared world adapter](physics-backends.md) now supplies public canvas association, backend selection and event projection. [Pointer picking](physics-picking.md) uses the same public queries. Networking and the remaining complete-backend obligations stay open.

Each resident shape contains a complete authored Shape, including large convex
contours or all paired concave segments. SetQueryIdentity assigns its logical
collider key, shape index and canvas association. The public adapter supplies
retained RIDs and logical slots; default untagged store keys use local body/shape
slots. Returned physical indices and generations qualify the live attachment and
are neither public RIDs nor portable network identities. Mapping metadata remains
on the CPU because it is authored identity, not simulated state.

## Query contract

- Query masks inspect all 32 layer bits, independent of reciprocal collision masks,
  body exceptions, joint collision vetoes and one-way response. Body and sensor
  flags are independent. Explicit collider exclusions use batched ranges. Point canvas
  selection uses exact 64-bit equality, including zero for the default canvas. Rays
  ignore canvas association. PhysicsCanvasTests compares this with public CPU
  queries; PhysicsGPUSpaceTests additionally exercises public GPU binding.
- Rays return the nearest hit; exact fraction ties sort by collider key and logical
  slot. Point results sort and deduplicate by those keys before applying each cap.
  The final physical-index tie is internal. Keys compare as unsigned 64-bit values;
  current public RID values are positive signed 64-bit integers.
- Filled circle, capsule, rectangle and convex interiors answer points. Ordinary
  segments and paired concave segments are hollow. The existing short-segment
  collapse yields zero-radius points; short capsules collapse to circles. Directed
  separation rays remain excluded from ordinary ray/point queries.
- A zero-length ray misses even with HitFromInside. A filled inside start skips the
  complete shape unless enabled, in which case the hit is the origin with zero
  normal. External hits carry world-space positions and outward normals. Segments
  accept both sides. Tangent circle hits and very short capsule crossings execute.
- Query work flushes pending authored poses/geometry without advancing simulation.
  Disposal, slot reuse, filters and shared-resource edits affect the next query.
  Capacities, numeric inputs, exclusion ranges, handles, lifetime and owner thread
  validate before execution. Only returned counts are copied into each caller
  segment; misses and unused tails stay unchanged. Submitted device failure poisons
  the store rather than replaying on CPU.

The comparison tests exposed a CPU defect: a ray beginning inside a large convex
polygon could hit another piece's internal seam. PhysicsDirectSpaceState now checks
containment across the contiguous fixture group for that logical slot before
casting. Other slots of the same collider remain eligible. Scene rays, server rays
and Area rays all use the corrected scan; public signatures are unchanged.

## Device work and traffic

Queries reuse the resident stackless bounds tree. A separate version check rebuilds
or refits it after geometry/pose changes, without enumerating simulation candidate
pairs. Current conservative bounds from prior simulation work may be reused; exact
geometry still decides each hit. Query-only updates invalidate the simulation pair
cache, so subsequent contact work cannot use old pairs. One compute invocation owns
one query and its disjoint output segment. Stable point insertion costs O(hits × cap);
a bounded heap is a measured upgrade if large caps dominate.

Retained buffers contain 48 bytes per query, 32 bytes per shape mapping, 8 bytes per
exclusion key, 4 bytes per returned count and 64 bytes per reserved output slot.
An ordinary batch uploads queries, exclusions and 8 status bytes. The GPU reserves
complete capped result segments, but the download packs only predicted prefixes:
for each query ordinal, clamp the previous returned count to the current cap, with
a minimum of one slot for a nonzero cap. Counts and 8 status bytes share this first
submission. If counts exceed those prefixes, one additional copy submission reads
only the missing tails from the already completed result buffer; it neither reruns
the search nor advances simulation. All counts validate before copying hits. The
owner-thread operation cannot interleave world edits between these reads.

Predictions shrink after sparse results and are shared across ray/point, shape and
body-motion queries; changing batch sizes, caps or record strides does not affect
correctness. No-hit and unused destination tails remain untouched. Contiguous full
segments (including ray batches) retain one bulk copy. Transfer storage grows only
for packed prefixes or actual missing tails and is reused at its high-water size.
A sudden increase in hits can add one fence; alternating dense/sparse requests may
pay that fence repeatedly. Both waits and every downloaded byte enter the existing
store counters. Submitted failures keep the store poisoned and never replay on CPU. A metadata edit uploads the retained mapping table once; unchanged reads
upload no geometry or identity table. Capacity growth and changed-tree preparation
have additional recorded traffic/waits; they are outside the static warm-read timing.
QuerySubmissionCount and QuerySpatialSubmissionCount distinguish search from tree
preparation; an overflow copy adds to SubmissionCount but not QuerySubmissionCount.
BroadPhaseSubmissionCount counts only simulation pair submissions.

## Verification

GPUPhysicsQueryTests first reproduced capacity-sized downloads on the old driver.
Its adaptive-readback checks now cover sparse/dense transitions, exact transfer and
submission counts, two packed result segments separated by a zero cap, changed
limits, shape/point stride changes, caller sentinels and zero owner/all-thread
managed allocation through repeated overflow after capacity warmup. PhysicsPickingTests
also bounds complete-frame readback for one selected shape among 1,024 bodies.

GPUPhysicsQueryTests compares 64 rays and 64 points per geometry case against
public CPU queries, including body rotation, clockwise/counterclockwise 12-gons,
hollow/degenerate segments and collapsed capsules. The CPU rotation uses an
approximate deterministic sine/cosine, so the oracle gives GPU the same actual
rotation before comparing query positions/normals; no CPU arithmetic is added to
GPU execution. Initial tests using equal authored angles found that pose difference,
then the compound-inside regression. A sampled polygon point also differed within
0.0004 scene units of an edge: the test allows a 0.001 edge band only when an
independent analytic half-plane check confirms GPU membership. Positions/normals
use 0.004/0.001 tolerances. Bitwise boundary classification is not promised. The suite also checks sorted caps, duplicate
point identities, exclusions, sensor/canvas filters, bit 31, batched mixed results,
tangency, segment normals, inside/zero rays, very short crossings, integrated motion,
query/contact interleaving, edits, reuse, growth, errors and caller-tail preservation.
PhysicsQueryTests independently retains the compound-inside CPU regression and
warmed allocation check, without requiring a GPU.

The adaptive-driver run on the same 65,536-shape/256-ray workload below measured
p50/p95/p99 0.0948/0.8183/1.5048 ms, mean wait 0.2029 ms, with unchanged
12,296/17,416 B upload/readback per batch and 0/0 owner/all-thread managed bytes.
Evidence: `/tmp/e2d-query-green2.log`. Its GPU was shared with a running graphics
example and desktop applications; comparison with the historical timing below is
not a controlled speedup claim. The [same-scene CPU/GPU picking measurement](physics-picking.md#current-complete-frame-cost)
records the sparse-query transfer reduction and observed timing limits.

Historical baseline before adaptive readback: the static workload creates all 65,536 circle shapes, executes 256 ray queries per
batch, warms 96 batches and records 128 batches. Every ray result is checked. The
measured interval includes command submission, the result fence and copies; it does
not include world construction or dynamic tree refits. On Linux/.NET 10, Vulkan, NVIDIA GeForce RTX 3090 Ti, the focused run measured
p50/p95/p99 **0.0697 / 0.0749 / 0.2509 ms per 256-ray batch**. The included average
fence wait was 0.0606 ms. Upload/readback were **12,296 / 17,416 bytes per batch**.
Owner-thread and all-thread managed allocation were **0 / 0 bytes** across the
128 measured batches, with no Gen0/1/2 collections. There were no simulation-pair
submissions or warm static-tree rebuilds. Evidence:
`/tmp/electron2d-world-query-focused-final.log`. This measures query cost, not whole-physics-step time or
window FPS. Native allocator accounting, other devices/backends and user visual
acceptance are unverified.

Reproduce with a source-native Release test build and
`ELECTRON2D_TEST_GPU_QUERIES=1 dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release --no-build`.
The focused runner also executes the public CPU query and scene RayCast suites.
The regular full GPU runner includes the resident query suite. The complete GPU
suite and the 37-suite CPU collider group passed on this host; logs are
`/tmp/electron2d-world-query-gpu.log` and `/tmp/electron2d-world-query-cpu.log`.
The full run measured query p50/p95/p99 0.0680/0.0747/0.0913 ms with the same
zero managed allocation and transfer counts.

Ray/point results now preserve the sampled 64-bit object ID in existing record padding, without changing the 64-byte stride. See [object associations](physics-object-bindings.md).
