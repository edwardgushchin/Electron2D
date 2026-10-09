# Resident GPU body-motion queries

Last updated: 2026-10-09

## Boundary

GPUPhysicsBodyStore.TestMotion tests a registered body's shapes at a supplied pose,
without changing its live transform, advancing simulation or creating a CPU world.
A batch reads current GPU geometry, tree, poses and center-aware point velocities.
The CPU uploads authored queries, attached shape tokens and incident explicit
exceptions; it does not read all body state or enumerate potential collision pairs.
This is an internal prerequisite. Public PhysicsServer.BodyTestMotion, PhysicsBody
movement and CharacterBody still use the CPU world; independent public GPU binding,
backend selection, event projection and networking remain open.

## Behavior

Four deepest-contact recovery passes apply 40% of penetration after a 5%-of-margin
allowance. Ordinary shapes expand by the query margin; directed rays extend along
their positive axis. A zero margin retains the existing 0.0001-scene-unit minimum.
A recovered pose then tests the requested translation. Residual penetration above
0.05 scene units or inward motion stops at fraction zero. Ordinary conservative
advancement uses the existing CPU skin and 1/256 motion bracket; a one-unit
post-impact advance obtains contact geometry. A miss still returns recovery plus
requested travel, and clears collision identity/contact fields.

Reciprocal masks, both directions of explicit body exceptions, caller collider and
object exclusion spans, sensor rejection and one-way direction/margin rules apply.
Joint contact vetoes do not implicitly exclude a body-motion query. Non-sliding rays
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
exceptions, joint independence, exclusion ranges, logical/object identity,
sensor/mask policy, angular surface velocity with custom COM, batch tails,
slot reuse, validation and lifecycle errors.

Geometry comparisons account for CPU approximate rotation construction. The
circle/floor tests allow 0.8 scene units of travel, 0.008 fractions, 0.1 contact
position, 0.001 normal and one unit of depth; complex families allow 1.2 travel.
Compound/ray cases avoid equal-depth vertex-normal ties and allow one unit of
travel. These are scoped convergence tolerances, not bitwise parity promises.

Each request uploads 80 bytes plus 8-byte shape/exception tokens and supplied
exclusion keys. One result reserves 128 bytes plus a 4-byte count; the batch has an
8-byte status exchange. Inputs, outputs, scratch and transfers retain warmed
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

On Linux/.NET 10, Vulkan, NVIDIA GeForce RTX 3090 Ti, the final focused run measured
p50/p95/p99 **0.9236/1.1029/1.2147 ms** per 256-query batch, with mean wait
**0.9186 ms**, **22,536/33,800 upload/readback bytes per batch**, and **0/0
owner/all-thread managed bytes** over the measured interval. Evidence:
`/tmp/electron2d-motion-query-focused-final2.log`. Timing includes all requested
queries and their recovery work; the world population is not reduced for measurement.

The complete GPU suite and all 37 CPU collider groups pass on this revision:
`ELECTRON2D_TEST_GPU_PHYSICS=1` and `ELECTRON2D_TEST_COLLIDER_BACKEND=1` with the
same Release runner (`/tmp/electron2d-motion-query-gpu.log`,
`/tmp/electron2d-motion-query-cpu.log`). These cover the shared directed-query
extraction and existing failure/lifetime/renderer-independence boundaries;
they do not establish public independent-GPU or networking acceptance.
