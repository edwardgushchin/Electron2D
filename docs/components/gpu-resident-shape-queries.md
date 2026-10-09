# Resident GPU shape queries

Last updated: 2026-10-09

## Boundary and ownership

GPUPhysicsBodyStore.QueryShapes executes logical shape intersections, contact pairs,
deepest rest information and translational motion brackets on resident geometry.
It creates no CPU solver world or temporary simulation body. This remains an
internal adapter prerequisite: public PhysicsDirectSpaceState and ShapeCast still
use CPU. Public backend selection, body-motion/CharacterBody binding,
callback/event projection, missing shape families and networking remain open.

RetainQueryGeometry borrows an authored Shape in a disposable QueryGeometry lease.
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

Intersections include the initial pose and requested translation. Contacts report
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
the positive projection of motion. Reverse queries test the ray against the swept
convex region; support mapping represents that extrusion without adding vertices
or imposing an eight-vertex limit. Containment and ray/ray pairs reject; front-facing
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
authored-data traffic only when dirty. One result fence includes capped segments,
so no second count-discovery fence or complete state readback is needed. The center
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
retain their own directed-ray policy. Public independent-GPU binding remains open.

ObjectID now travels with each selected result, adding 16 B per record. The [object association report](physics-object-bindings.md) records the new transfer measurement and snapshot/lifetime checks.
