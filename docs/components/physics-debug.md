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

Contact-point/Space debug APIs, contact limits/color, tile collision-owner diagnostics,
backend extension drawing and the rest of the full physics/network objective remain
open. Other platforms, native allocation and owner visual acceptance are not implied
by these Linux tests.
