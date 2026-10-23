# Resident GPU shape queries

[Cold query geometry](physics-queries.md#cold-query-geometry) is resolved under the using-world numerical/lifetime guard before retained query-cache selection or replacement. Nested queries complete before outer cache preparation; resident algorithms, transfers and result formats are unchanged.

Last updated: 2026-10-23

## Backend compiler profiles

Resident shape queries, body-motion queries and CCD embed two modules from each
shared source. Vulkan retains the `-O` module and its measured cold-pipeline
improvement; Metal and Direct3D 12 use a function-preserving portable module.
The three additional modules pass `spirv-val` and native reflection reports the
same nine resource and workgroup values as their optimized counterparts.
Shape-query SPIR-V uses 1,310,952 optimized or 153,216 portable bytes;
generated backend source avoids the multi-megabyte
expansion measured with the optimized module. Native ShaderCross compiled all 35
physics modules to DXIL locally. The common compute boundary selects DXIL when
the device advertises it, retaining the mature translator and reflected bindings.

CI run `39858176735` failed at shape-query creation on Windows x64/x86 and returned
an invalid collider identity on macOS ARM64. These failures motivate the profile
change; the exact Metal cause is not isolated. Compilation, reflection and local
Vulkan results do not establish corrected Windows/Metal query execution. Their
full target suites remain the gate. Query algorithms, identity layouts, iteration
limits, allocation gates and public coverage states are unchanged.

Local NVIDIA Vulkan checks passed the pipeline ownership/hash profile and the
functional shape/motion suites without a display, and the complete resident CCD
profile through Wayland with zero warmed managed bytes per tick. The isolated
offscreen CCD run failed while creating a later device with no supported backend;
it did not complete. This host/environment difference remains separate from the
unverified Metal/Direct3D profile execution and the complete Linux Mesa CI suite.

The query distance simplex now retains only its three Minkowski difference
vectors. Its former support-point pair fields were never consumed. The distance,
duplicate detection, barycentric reduction, iteration limits and sweep brackets
are unchanged; no device record or public result layout changes. This reduces
the private array element from a three-vector struct to one vector before backend
translation. It is a portability candidate for the Windows query failure, not
confirmed Windows acceptance. Run `39869572938` still crashed during the first
GPU shape intersection on Windows x64/x86; the ARM64 diagnostic located
`d3d10warp!PixelJITProcessor::ExecuteJIT` and a return-address authentication
failure. The exact faulty operation remains unisolated.

The same run missed a compound CastRay impact on Linux x64. Isolated local
Lavapipe 26.2.4 and the CI Mesa 26.0.8 library both passed the unchanged public
compound consumer, including CastRay/CastShape and 128 zero-allocation cycles.
The compiled local and CI Linux runtime profile strings and query/CCD resource
hashes match. Those controls do not explain or clear the CI failure.

The reduced simplex and guarded spring product pass eight focused local profiles:
CPU/GPU public compound checks, functional shape/motion queries, CPU/GPU shared
CCD/joint policies, CPU scene physics and pipeline ownership/hash checks. The
updated public compound and shared CCD/joint profiles also pass with the isolated
CI Mesa 26.0.8 library. All 35 updated modules compile through native ShaderCross
to DXIL, and each optimized/portable pair retains identical nine-value reflection.
Three original modules change: shape queries, motion queries and joint preparation;
the other 29 original module hashes match. These are local execution/compiler
controls, not corrected Windows/Metal or complete target-suite acceptance.

## Boundary and ownership

GPUPhysicsBodyStore.QueryShapes executes logical shape intersections, contact pairs,
deepest rest information and translational motion brackets on resident geometry.
It creates no CPU solver world or temporary simulation body. PhysicsDirectSpaceState
and ShapeCast route through this store when their World uses GPU; CPU worlds retain
the managed solver query path. [Shared world integration](physics-backends.md)
records the executed public checks and remaining full-contract/network obligations.

RetainQueryGeometry borrows an authored Shape in a disposable QueryGeometry lease.
The public direct-space adapter rebinds its one retained lease when the selected
resource changes. It acquires the new geometry before releasing the old reference,
reuses the wrapper and retains no history of previous query resources. Rejected
null/disposed/off-owner rebinding preserves the current lease; a retired lease
cannot be rebound. World disposal still releases its current geometry. Existing
geometry slot/vertex reuse, uploads and fences remain unchanged.
The lease shares the world's geometry cache, observes resource changes and has no
collider identity, mass, body or pair entry. Dispose releases its geometry reference;
store disposal clears resource references even when a lease survives. Querying a
disposed lease/resource, a foreign store or the wrong owner thread rejects. The
lease does not own or dispose the caller's Shape. Creation/growth is cold work;
repeated queries retain input arrays, device buffers and transfer storage.

The existing SetQueryIdentity mapping supplies logical collider/shape keys for
results and exclusions. Physical slots/generations remain internal attachments,
not network identities. Pending world poses and geometry are prepared without a
simulation advance. The same tree preparation/cache, failure state and batch
submission path serve ray/point and shape queries. Byte capacities allow their
48/80-byte inputs and 64/96/128-byte outputs to alternate safely.
[Body-motion queries](gpu-resident-motion-queries.md) now reuse this driver and the
shared directed-contact include for supplied-pose recovery and sweeps.

## Geometry and public-query semantics

Intersections include the initial pose and applicable requested translation;
[special query displacement](physics-queries.md#special-query-displacement) preserves
resource-family precedence and boundary endpoint contacts. Contacts report
query/collider surface pairs; rest information chooses greatest penetration, with
logical key ties, and reads collider point velocity from resident actual plus
surface motion about its center of mass. Sensors report zero velocity. Results
honor layer masks independently of reciprocal masks, body/sensor flags and explicit
collider exclusions. Capped intersections sort and deduplicate logical keys before
limiting; contact pairs sort by logical key and retained piece encounter order.
Misses and unused caller output/count tails remain unchanged.

A support-map distance kernel handles complete convex contours and rounded cores.
Conservative advancement tests the whole translation, rather than sampling poses
that could skip a thin obstacle. Ordinary motion brackets use 1/256-wide intervals;
zero motion and initially overlapping logical slots do not produce a new hit.
A Cast query with count zero denotes (1, 1); no destination hit is written.
The distance search uses actual extreme vertices, not the tolerant contact-face
selector: the latter cycled for crossing segments. Closest-simplex progress and
finite/range guards bound numerical failure. Device failure is reported and poisons
the store; there is no hidden CPU calculation or replay.

Circle, capsule, rectangle, segment, full convex contours, paired concave segments
and directed separation rays participate. Existing short-segment/capsule collapse
rules remain shared with resident contact geometry. Ordinary contact generation
was extracted unchanged into PhysicsResidentManifold.inc.glsl and is now reused by
both simulation and queries. Shader loops retain their iterative form instead of
requesting large constant unrolling.

Directed queries extend only along their positive local axis, including margin and
the positive projection of motion. Reverse queries test the ray against the current
counterpart, ignoring that counterpart's requested displacement. Infinite-boundary
precedence retains its finite operand endpoint. Containment and ray/ray pairs reject; front-facing
hits retain SlideOnSlope or axis-directed normals. Existing device analytic ray routines
handle stationary primitive boundaries; advancement handles extrusions and rounded
polygons. The query bounds use the projected extension for a directed ray, which
can extend outside an axis-aligned envelope of the raw motion vector.

## CPU compound-query corrections

The GPU comparison exposed three false contacts from CPU polygon decomposition.
CastMotion now removes a whole logical collider slot when any initial query piece
overlaps any part of that slot. Other slots of the same collider remain eligible.
A directed query starting inside a compound polygon ignores its internal seams;
a stationary ray whose origin is inside a compound convex query or its swept
region likewise cannot hit a different query piece's seam. All four public shape
operations share query preparation and the latter containment filter.
PhysicsSeparationRay reuses its existing swept-region piece builder for this test.
These corrections preserve public signatures and the existing containment rules.

## Cost and verification

A shape request uploads 80 bytes; each capped result reserves 96 bytes, plus a
4-byte count and the batch's 8-byte status exchange. Mapping/geometry edits add
authored-data traffic only when dirty. The shared [adaptive readback](gpu-resident-queries.md#device-work-and-traffic)
packs predicted result prefixes with counts in one submission, then reads only
missing tails if needed. GPU search caps and complete results are unchanged; no
complete body-state readback is introduced. Cast and rest need at most one result
and keep their single fence. The center
buffer is bound in the existing read/write storage group but only read by this
kernel, keeping the pipeline within eight read-only buffer slots.

GPUPhysicsShapeQueryTests compares the four operations with public CPU queries for
ordinary primitive pairs and rotated, clockwise compound, hollow, segment and
both directed-ray policies. Independent checks validate contact surfaces, unit
normals, deepest selection, logical ordering/caps, angular surface velocity about
custom COM, sensor velocity, mixed-stride batches, edits and lifecycle errors.
PhysicsShapeQueryTests retains CPU-only regressions for compound initial overlap
and both ray-containment directions; ShapeCastTests covers the public scene consumer.
The focused runner also reruns the existing resident ray/point query suite.

The CPU and GPU use the same actual rotation for geometry comparison, since CPU
angle construction uses approximate trigonometry. Motion comparisons allow 0.025
ordinary and 0.035 complex fraction differences for the tested 50–80-unit paths:
CPU casting uses a 0.5-unit skin, backend convergence thresholds and eight interval
refinements. Rest depth allows 1.2 scene units, covering the existing 1-unit
post-impact contact advance; static collider surface checks use 0.01, normals and
point velocity use 0.001. Equal-depth features of one shape may choose different
valid endpoints/faces; tests compare physical surfaces and depth instead of internal
feature order. These tolerances are scoped tests, not promises of bitwise equality.

The workload creates all 65,536 static circle shapes and executes 256 rest queries
per batch. After 96 warmup batches, 128 batches include submission, fence and copy
cost, checking every output. The final focused run on Linux/.NET 10,
Vulkan, NVIDIA GeForce RTX 3090 Ti measured p50/p95/p99 **0.1076/0.1256/0.1363 ms**,
including mean wait **0.0887 ms**. Upload/readback were **20,488/21,512 bytes per
batch**, with **0/0 owner/all-thread managed bytes** across the measured interval.
Evidence: `/tmp/electron2d-shape-query-focused12.log`. This is static query cost,
not full simulation, dynamic-tree refit cost, native allocator accounting or window
FPS. Other hardware/backends and user visual acceptance remain unverified.

Build tests with `-c Release -p:Electron2DBuildNativeFromSource=true`, then run
`ELECTRON2D_TEST_GPU_SHAPE_QUERIES=1 dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release --no-build`.

The complete GPU suite and the 37-suite CPU collider group passed on this host
(`/tmp/electron2d-shape-query-gpu.log`, `/tmp/electron2d-shape-query-cpu.log`).
This includes the shared manifold extraction and existing failure/lifetime checks;
it does not establish public independent-backend or networking acceptance.

WorldBoundaryShape participates analytically in both query argument positions,
including far-away half-plane contacts, normal/distance transforms and ray pairs.
The shared shape and body-motion matrices now include this geometry. Direct-space
shape queries retain initial overlap during motion; body-motion recovery/casts
retain their own directed-ray policy. The [public GPU world adapter](physics-backends.md) now uses these kernels; full family conformance remains open.

ObjectID now travels with each selected result, adding 16 B per record. The [object association report](physics-object-bindings.md) records the new transfer measurement and snapshot/lifetime checks.

The public GPU adapter retains one rebindable query-geometry lease. Alternating live resources reuses its wrapper and retires the previous reference; past resources are not accumulated. ConvexPolygonShapeTests exercises 128 warmed overlap/contact/rest/motion cycles between shared and standalone geometry on CPU/GPU with zero owner/all-thread bytes, preserving current contour results and initial-overlap fractions. GPUPhysicsShapeQueryTests checks successful replacement, null/disposed/thread rejection with preserved previous geometry, and retired lease failure. Geometry uploads and query waits retain their existing separate costs.


ELECTRON2D_TEST_GPU_SHAPE_QUERIES=functional runs resident shape geometry,
filter/publication/lifetime and public shape/ShapeCast checks. Value 1 and the
ordinary complete runner retain the massive shape and ray/point measurements.
The separate query-contract profile retains functional ray/point coverage.
Functional execution does not establish throughput or hardware acceptance.
