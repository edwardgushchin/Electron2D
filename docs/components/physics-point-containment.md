# Polygon point containment and ray origins

Last updated: 2026-10-15

## Corrected behavior

CPU point queries test an unrounded convex fixture against its outward edge
half-planes in body-local solver coordinates. A positive edge distance excludes
the point. This avoids GJK distance convergence reporting zero just outside an
edge. Rectangle and partitioned convex geometry share the same test; each logical
slot still deduplicates its pieces before publishing identity or applying caps.

`PhysicsShapeBackend.ContainsPoint` owns the geometric test. Its polygon uses a
stable `ref` because the native fixed-array indexers return element references.
The first readonly-parameter candidate failed the public regression and was
rejected. Rounded polygons retain their existing rounded-distance test; circles,
capsules, boundaries and hollow segment behavior retain their primitive paths.
This introduces no public API, epsilon expansion, CPU/GPU ordering requirement,
native resource or simulation-state mirror.

The CPU world adapter uses this test for direct points, the complete logical-slot
inside-start decision for ordinary rays, Area audio selection and directed shape
query containment. `PhysicsSeparationRay` uses it for its unrounded proxy regions
as well. A ray just outside the contour reaches its physical entry under either
HitFromInside policy; a ray inside the slot remains skipped or returns its origin
and zero normal according to that policy. Internal partition seams remain hidden.
Authored collision layers, exclusions, canvas selection, owner/phase/lifetime
checks and sampled identities retain the common query contract under ADR 0063.

## Evidence and numerical bounds

The reproduced failure uses a radius-12, 12-vertex polygon at `(1800,80)`, rotated
.37 rad. Point `(1790.1703,73.80258)` lies outside by .0013154149 scene units.
The actual CPU eight-vertex partition has radius zero and an outward distance of
.0013153534 scene units, yet its old GJK containment returns true. Another partition
also excludes that point. The native contour and authored contour therefore agree;
the old distance-based membership was the source of the false hit.

`PhysicsPointContainmentTests` uses only public runtime APIs and is linked into
the existing public-only NativeAOT consumer. Each backend runs sixteen combinations
of scene/server colliders, both windings, identity/offset local slots and serial/
separate numerical policies. Body and Area results, layer/exclusion/disabled-slot
controls, ordinary ray origins and directed shape query origins are checked.
After 64 warm cycles, a blocking collection/finalizer drain and 32 more warm
cycles, 64 complete 1/60-second frames plus queries must allocate 0/0 owner/all-thread
managed bytes. The fixture contains a static body and an Area; it is a query
conformance workload. The explicit outside gap exceeds coordinate-conversion roundoff; the inside
control lies .01 units inward. Edge hits must travel .0004 through .004 units from
the outside origin and have a normal within .00002 unit-vector length. These
bounds admit float transform/partition rounding while distinguishing a real entry
from the erroneous zero-distance inside result.

`PhysicsQueryTests` runs the public fixture on its selected backend and checks the
same exterior/interior Area through audio-bus selection without starting audio
output. `GPUPhysicsQueryTests` now gives CPU and GPU the same authored .37-rad pose.
Its former conversion through backend-specific approximate trigonometry gave GPU
.37121636 rad. Both rectangle normals now have an independent authored-basis
oracle within .00001 unit-vector length. Existing .004-position, .001-normal and
.001 contour-edge comparison bounds remain unchanged. Query boundary rounding
still has its explicit analytic oracle; bitwise boundary agreement is not promised.

The RED public regression, native partition diagnostic, rejected readonly helper
and subsequent checks are retained under ignored
`bin/physics-point-containment/2026-10-15/`. Execution results and fingerprints must
be read with their recorded host/backend/revision scope. This bounded correction
does not establish full inherited/registered numerical conformance, native heap
budgets, foreign-platform execution, hardware GPU advantage or real-window FPS.

## Executed Linux validation

The frozen candidate is based on `c02b5cd4` plus this correction; the earlier
independent movie-profile and AVI-test commits are preserved. Source-native
Release managed and strict NativeAOT builds pass. CPU-only profiles run with an
unavailable Vulkan ICD. GPU profiles use pinned software Vulkan (llvmpipe LLVM
23.1.1), with no NVIDIA driver in process maps; NativeAOT has no CoreCLR mapping.

All 23 recorded final gates pass: formatting/build/publish, both focused managed
profiles, resident ray/point and shape-query suites, the full CPU collider contract,
the sixteen shared GPU physics families, CPU/GPU picking, both NativeAOT profiles,
coverage and wiki generation/check. Across the standalone and connected query
profiles there are 128 executions of the new public cases and 8192 measured
frame/query cycles, all at 0/0 owner/all-thread managed bytes. The internal Area
audio regression passes on both selected backends. The unrelated full default
test runner was not run for this correction.

Compile/PDB input audits cover 2220 runtime and 706 test files for managed, and
2220 runtime plus 129 public-consumer files for NativeAOT, with no missing or
mismatched documents. Generated documents are counted separately. Source/binary
manifests and `validation.json` retain exact commands and digests under the evidence
directory above. These are local numerical/query gates with the broader limits
already stated, including physical GPU and window acceptance.
