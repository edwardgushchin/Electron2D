# Physics pointer picking

Last updated: 2026-10-09

[Viewport](../classes/Viewport.md#physics-picking) queues unhandled pointer events
for [CollisionObject](../classes/CollisionObject.md#pointer-input) callbacks and
signals. [ADR 0099](../decisions/physics-picking.md#adr-0099) defines ordering,
coordinates, eligibility, lifetime and canvas scope.

## Using the feature

```csharp
viewport.PhysicsObjectPicking = true;
viewport.PhysicsObjectPickingSort = true;
viewport.PhysicsObjectPickingFirstOnly = true;
body.InputPickable = true; // PhysicsBody defaults false; Area defaults true.
body.InputEvent += (node, input, shapeIndex) =>
{
    if (input is InputEventMouseButton { Pressed: true })
        ((Viewport)node).SetInputAsHandled();
};
```

Newly activated root viewports sample ProjectSettings.PhysicsObjectPicking
(physics/common/enable_object_picking, true initially), unless the caller or packed
scene already authored PhysicsObjectPicking. Other viewports retain the false
constructor default. Later project changes affect later scene activations.

The callbacks run at the start of the next physics frame, after frame-start
notification and before node physics callbacks. The source event may be disposed
or changed after PushInput; delivery uses an owned copy. Duplicate a callback's
borrowed event to keep it. Event-only PushInput does not alter global Input state.

A visible processing collider with an active shape and at least one collision-layer
bit can be picked. CollisionMask does not restrict picking. Bodies require explicit
InputPickable=true; Area starts enabled. Ordinary direct queries are unaffected.
Effective Z and reverse tree order sort up to 64 selected logical shapes within
each canvas. First-only selects one hit per canvas; SetInputAsHandled stops later
canvases too. Sorting cannot recover hits omitted by the cap.

CanvasTransform and each CanvasLayer's final transform map the viewport point into
physics coordinates; canvas identity filters matches exactly. Callback coordinates
remain local to the receiving viewport. Shared worlds preserve collider identity
while allowing either viewport to select it. CustomViewport changes presentation
only; it must share the collider's physics world to pick it. Embedded containers
forward their localized input through the same unhandled-event path.

Stationary hover detects movement and eligibility changes without repeatedly
sending input to an already hovered object. Shape changes preserve object hover.
Explicit BodyAttachObject/AreaAttachObject redirects delivery to the sampled live
CollisionObject target. Null or other object types suppress pointer delivery.
Geometry eligibility remains with the physical owner; the shape index also refers
to that physical source. Shared assigned identities deduplicate hover, and rebinding
does not rewrite earlier hits. Object/shape exits clear state before notifications; queued input is released when
picking is disabled or a viewport is removed. Capture, GUI consumption, invisible
objects, paused processing and zero layers prevent picking. Removing/reindexing a
shape in a callback cannot redirect a previously sampled hit to its replacement.
Exceptions are collected after remaining eligible callbacks and cleanup.

## Implementation and cost

[SceneTree.PhysicsPicking.cs](../../src/Scene/Main/SceneTree.PhysicsPicking.cs) and
its internal PhysicsPickingState own retained queues, hit lists and hover maps.
CPU and GPU reuse PhysicsDirectSpaceState's actual point queries. Scene eligibility
is applied before the 64-hit cap. Node.IsGreaterThan now walks ancestry without
allocating temporary lists and correctly includes internal children.

The steady physics pass and sorted passive hover reuse scratch; newly queued input
copies, first use, growth and user callbacks have separate costs. GPU picking
currently reads capacity-sized point-hit metadata before scene-policy filtering.
That transfer is a known optimization target, not a required CPU simulation mirror.
The native path clears the exact window viewport on SDL mouse exit, also fixing
stale GUI hover when the global GUI selection pointed elsewhere.

## Verification

PhysicsPickingTests runs the same public API scenarios on explicit CPU and GPU
worlds: stored/default policy, ordering, logical shapes, layers/masks/visibility,
pause, stationary hover, GUI and unhandled consumption, sorting/first-only/cap,
shared/custom/embedded viewports, touch/drag/custom events, detachment, callback
failure and disposal. Geometry checks use circle centers and exact affine mappings;
they do not substitute picking tests for the separate shape-query conformance suite.
64 measured warmed sorted-passive frames allocate 0 owner and process-wide managed
bytes. The 1,024-body complete-frame measurement is recorded below.

PhysicsPickingNativeTests injects real SDL events through Engine.Run and verifies
localization, capture suppression, native exit and actual drawing with both physics
backends paired with gpu/compatibility rendering on Linux. It does not measure FPS
or establish other-platform or visual-design acceptance.

Run `ELECTRON2D_TEST_PHYSICS_PICKING=1` or
`ELECTRON2D_TEST_PHYSICS_PICKING_NATIVE=1` with the Release executable checks.
`ELECTRON2D_PICKING_CPU_ONLY=1` limits the former to headless CPU and asserts that
DisplayServer and RenderingServer remain unavailable.

### Current complete-frame cost

Linux x64/.NET 10.0.1, Ryzen 7 5700X, Vulkan/RTX 3090 Ti, this change over parent
450b7f8: 1,024 static circles on a 32-by-32 grid, one hovered shape, sorted passive
query plus the complete 1/60 s physics frame, 96 warmup and 64 measured frames.
Both implementations run the same authored scene and delivery code.

| Backend | p50 / p95 / p99 (ms) | Owner / all-thread managed bytes | GPU upload / readback per frame | Mean device wait (ms) |
| --- | --- | --- | --- | --- |
| CPU | 0.2497 / 0.2594 / 0.2807 | 0 / 0 | 0 / 0 B | 0 |
| GPU | 0.5601 / 0.8000 / 1.2509 | 0 / 0 | 72 / 65,564 B | 0.1448 |

This quiet picking workload favors CPU. GPU transfers expose the current
capacity-sized query-result download; reducing that transfer remains open. These
numbers are not rigid-body stress results or rendered-window FPS. Native/device
allocation and other platforms remain unmeasured.
