# Physics canvas diagnostics

Last updated: 2026-10-09

## Scope

[Shape.Draw](../classes/Shape.md#draw), [CollisionShape.DebugColor](../classes/CollisionShape.md#debugcolor)
and [SceneTree.DebugCollisionsHint](../classes/SceneTree.md#debugcollisionshint) use
ordinary retained canvas commands on both renderers. CPU/GPU physics selection is
independent. The flag defaults false and adds no game or sandbox UI.
[ADR 0100](../decisions/physics-debug.md#adr-0100) records the live-toggle adaptation.

```csharp
// During a scene callback, on its owner thread:
Tree!.DebugCollisionsHint = true;
// Inside a CanvasItem draw callback:
shape.Draw(GetCanvasItem(), new Color(0, .6f, .7f, .42f));
```

These are callback fragments, not a standalone host. Direct Shape.Draw needs an
active renderer and a live scene or caller-owned canvas-item RID. It snapshots the
resource's current local geometry; later resource edits do not mutate those copied
commands. Clear/redraw the canvas to refresh them. Wrong-kind/stale RIDs, foreign
owners and off-thread calls reject through existing canvas validation.

## Geometry and ownership

Filled circles use 24 contour samples; capsules add their straight side endpoints,
rectangles and full convex contours use polygons. Hollow segment collections remain
lines. Separation rays show arrowheads. An infinite boundary has a finite fading
line and normal marker at the normalized plane; it is not a finite physical wall.
The outline project setting adds opaque one-framebuffer-pixel edges to filled shapes.
Empty contours and collapsed capsules draw no fill.

CollisionShape stores a finite DebugColor, initially sampled from
ProjectSettings.DebugCollisionShapeColor, including active feature overrides.
Disabled shapes use gray with half the fill alpha; a one-way arrow identifies the
configured local direction. CollisionPolygon draws its authored closed contour and
one-way direction. Casts show their target, using cached collision red on a hit and
gray when disabled. ShapeCast draws a chain of shapes along its target, with at least
two intervals; excessive diagnostic work rejects before recording at 1,048,576
samples. Pin/groove/spring markers show authored local anchors and guide/offset
geometry. They do not claim to display solved impulses or current constraint error.

The tree samples its default cast/polygon color at construction. The outline key is
read on each recording; changing it requires redraw of existing commands. The tree
flag invalidates hidden nodes too. Ordinary transforms, clipping, modulation,
visibility, interpolation and canvas routing apply. Built-in diagnostics precede
user draw handlers. Renderer recording failures clear partial commands and retain a
pending redraw under the existing canvas error policy.

CollisionShape and ShapeCast compare borrowed geometry revisions/disposal before
recording, so committed edits still refresh after a throwing Changed subscriber.
Worker resource edits only invalidate logical recording; rendering remains on its
owner thread. Disposal never transfers ownership of the resource to the node.

## Runtime flow and cost

Shape.Drawing shares authored PhysicsShapeGeometry with internal node recording.
PhysicsDebugDrawing owns common arrows/tints/markers. The existing renderer validates
Shape.Draw's RID and enters its ordinary server-recording scope. Every primitive
uses existing polygon/stroke caches. Geometry is prepared during first use/growth,
then retained and replayed; no physics query or GPU body readback is required.

PhysicsDebugTests executes both explicit backends. After 96 warmups, 64 complete
rerecord/replay cycles over collision/cast/joint nodes and all shape families allocate
0 owner-thread and 0 process-wide managed bytes, with zero additional GPU physics
submissions or readback. PhysicsDebugNativeTests verifies actual pixels and lifetimes
for CPU/GPU physics paired with gpu/compatibility rendering; 64 warmed forced-redraw
frames allocate 0 measured owner-thread managed bytes. These measurements do not
cover first recording/growth, image capture, native/driver allocations or FPS.

## Verification and remaining scope

Use a source-native Release test build:

- `ELECTRON2D_TEST_PHYSICS_DEBUG=1` runs defaults/packing, all geometry, disabled and
  one-way drawing, joint edits, cast hit/miss state, mutation failure, worker edits,
  disposal, guards, work bounds, outline policy and warmed allocation.
- `ELECTRON2D_DEBUG_CPU_ONLY=1` limits that runner to CPU and verifies that no display
  or renderer was initialized; it is also run with unavailable GPU/display drivers.
- `ELECTRON2D_TEST_PHYSICS_DEBUG_NATIVE=1` checks public Shape.Draw on borrowed and
  owned canvas RIDs, native shape/joint pixels, live toggle/visibility/transform and
  resource edits, thread/RID errors, redraw allocation and teardown on all four
  physics/renderer combinations. PNGs are saved to `/tmp/e2d-physics-debug-*.png`.

Tile collision-owner diagnostics, backend extension drawing and the rest of the
full physics/network objective remain open. Other platforms, native allocation and
owner visual acceptance are not implied by these Linux tests.

## Contact-point snapshots

The same tree flag requests bounded contacts from each scene-bound space. Project
`DebugCollisionContactColor` (finite `(1,.2,.1,.8)`) and `DebugCollisionMaxContacts`
(nonnegative `10000`) are sampled with feature overrides at tree construction.
Zero maximum disables contact capture independently of shape drawing. Capacity is
prepared on enable and retained until world disposal; an impossible size fails
preparation without enabling the tree or leaving world requests active. Disable
immediately clears samples and stops diagnostic GPU work.
A caller-owned World retained after tree teardown loses the diagnostic request.

Each penetrating manifold point contributes up to two world-space boundary points,
sampled before that batch advances poses. Sensors, speculative nonpenetrating
contacts and sleeping pairs are omitted. The latest solver batch replaces earlier
CCD/substep batches; this is neither transient event history nor a whole-tick union.
Order and capped subset are not stable backend identities. `MaxContactsReported`
and gameplay reporting remain unchanged. A zero-time or inactive step retains the
last sample; an empty positive active step clears it. Owner, failure and lifecycle
guards cover reads and edits; a failed world permits disable/disposal only.

CPU reads retained manifold midpoints/separations. GPU records two compute passes
(clear/compact) inside the existing solver command, validates resident identities,
and copies only an 8-byte summary and bounded `Vector2` results. It first reads a
prefix sized by the previous count (at least two), then an exact missing tail when
the result grows. Disabling removes both compute and copies. No full body/contact
mirror or CPU collision pass is added. The internal space span is consumed by one
renderer-owned retained canvas item per world, drawn once through each viewport.
Markers are filled 5-by-5 scene-unit rectangles at point minus `(2,2)`. They inherit
the default world canvas and viewport transform; they are not fixed screen pixels.

The pinned `space_set_debug_contacts`, `space_get_contacts` and
`space_get_contact_count` hooks are internal server operations and extension virtuals,
not public scripted PhysicsServer methods. The concrete consumer is implemented;
the full extension family stays blocked until registration and virtual dispatch work.

`ELECTRON2D_TEST_PHYSICS_CONTACT_DEBUG=1` checks actual surface samples (.02 scene-unit
geometry tolerance), odd/zero/invalid limits, sampled settings, zero time, sleep/wake,
filters/sensors, body removal, owner guards, failed-world cleanup and warm allocation
on explicit CPU/GPU. `ELECTRON2D_CONTACT_DEBUG_CPU_ONLY=1` also verifies no renderer or
display was initialized and runs with missing GPU/display drivers.
`ELECTRON2D_TEST_PHYSICS_CONTACT_DEBUG_NATIVE=1` checks rendered contact pixels,
shared-world deduplication, separate viewport transforms, toggle/removal and resource
release on all four physics/renderer combinations. Pixel tolerance is .05 red/.04
other channels for render-target rounding. Captures: `/tmp/e2d-contact-debug-*.png`.

Pre-optimization measurement at `1dedab39`, on Linux x64 with 256 independent static/dynamic circle pairs (512 bodies),
96 warmup iterations and 64 samples, resetting every dynamic pose/velocity and then
stepping 1/60 s. CPU/GPU use the same fixture and public operations on the same source
candidate. Contact cap is 128 when enabled. Timings include the reset; no rendering.

| Backend / contacts | Whole path p50 / p95 / p99 ms | Reset / step p50 ms | GPU upload / readback B per iteration | GPU wait ms per iteration |
| --- | --- | --- | --- | --- |
| CPU off | .3676 / .4636 / .4719 | .0712 / .2964 | 0 / 0 | 0 |
| CPU on | .3742 / .4130 / .5306 | .0695 / .3042 | 0 / 0 | 0 |
| GPU off | 59.0417 / 65.2467 / 66.8194 | 54.9242 / 3.4243 | 98496 / 63696 | 36.4441 |
| GPU on | 56.7722 / 66.0741 / 88.4714 | 52.7402 / 3.6387 | 98496 / 64728 | 36.4958 |

All four intervals allocated **0 owner-thread / 0 process-wide managed bytes**.
Enabled contacts add 1032 readback bytes for the 128 selected points plus summary,
without a CPU upload. Native checks separately measured **0 owner-thread bytes**
in 64 warmed render frames. First preparation/growth, capture encoding and
native/driver allocations are excluded. These tiny-window checks establish pixels,
not representative FPS; timing noise prevents an on/off speedup claim.

The pre-optimization GPU path lost badly on this reset-heavy workload: 768 command
submissions per reset, before stepping. Current pose/velocity setters invalidate
GPU snapshots; preserving the other velocity component triggers synchronous
single-body reads. Phase timing and submission counts exposed this adapter bottleneck.
[Independent component commands](gpu-resident-bodies.md#component-velocity-writes)
now remove those setter reads; the linked report contains new whole-path and real
window measurements. Representative massive-scene GPU advantage remains required.
