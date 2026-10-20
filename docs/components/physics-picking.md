# Physics pointer picking

Last updated: 2026-10-20

[Viewport](../classes/Viewport.md#physics-picking) queues unhandled pointer events
for [CollisionObject](../classes/CollisionObject.md#pointer-input) callbacks and
signals. [ADR 0099](../decisions/physics-picking.md#adr-0099) defines ordering,
coordinates, eligibility, lifetime and canvas scope.

## Pickability operation audit

The pinned CollisionObject boolean property, inherited PhysicsBody false default
and both required Body/Area implementation setters now have a complete operation
projection audit. The pinned public server has no pickable method binding. Under
ADRs 0099/0103 the common retained `InputPickable` authoring is the implementation
input used by selected point-query delivery; no parallel numerical flag or public
server alias is needed. Raw RIDs retain the accepted non-scene-callback policy.

The public-only `--picking` fixture captures and restores serial/separate policy,
checks the actual implementation name and observes both owner query hooks and the
captured numerical lane. Equal-value writes reject in both borrowed hooks; both
Body and Area reject foreign-owner writes and disposed get/set. Ordinary pointer
callbacks can author the value. A moving RigidBody advances its position and 128
physical ticks while changed policy produces exactly 64 passive entries and exits
at strict zero owner/all-thread managed allocation. Position tolerance is .001
scene unit across 128 steps, allowing float integration rounding while separately
checking exact ticks, callbacks and policy values.

Fresh JIT/strict Linux x64 NativeAOT CPU/no-device, software-GPU and registered
serial/separate profiles execute this fixture. Root checks retain defaults/packing,
canvas and association identity, masks/layers/visibility, GUI and callback/error
lifetime, sort/cap and stationary transitions. The 1,024-body full-frame bracket
retains 0/0 managed bytes and the GPU sparse-readback upper bound of 128 bytes per
frame. Native checks execute both renderers with CPU/GPU/registered physics, real
SDL pointer events and target-pixel assertions; these use software Vulkan/Mesa.
They establish exercised Linux rendering and input behavior, not target-GPU FPS
or a controlled performance comparison. The earlier hardware cost table below is
historical. These picking profiles did not run physical GPU workloads. The later
authorized [RTX baseline](physics-rtx-current.md) measures separate workloads and
does not establish target-hardware picking acceptance.

Evidence: `bin/physics-pickability-audit/2026-10-19/validation.json` and its pinned
four-declaration/eight-source contract receipt. Only the two pickable implementation
rows become Implemented. The unique physics ledger retains 175 open declarations
(144 Partial, 14 Unimplemented, 17 Blocked). Complete registered geometry/numerics,
network, native memory, foreign-platform and hardware/window acceptance stay open.
The ordinary root frozen at runtime revision `8912effe` completed successfully
(exit 0, 1271.3658184509986 seconds, 129 observed processes without hardware-driver
mappings). Its root binary is SHA-256
`dacaf45c069905605a911a4a660f41fb539fbaf216df95b8b8efdd2ca13d97ef`.
These expanded test sources have separate fresh build receipts: 15 profiles, six
native renderer/physics cases and 2,240 complete measured frames at 0/0 managed
bytes. The ordinary root predates these two test-source edits; runtime C# is unchanged.

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
uses [adaptive result readback](gpu-resident-queries.md#device-work-and-traffic)
before scene-policy filtering: predicted hit prefixes and counts arrive together,
with one extra copy only when additional hits need fetching. All eligible hits
remain available for scene filtering; no CPU body-state mirror is introduced.
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

Linux x64/.NET 10.0.1, Ryzen 7 5700X, Vulkan/RTX 3090 Ti. The readback change
is based on 4e10451: 1,024 static circles on a 32-by-32 grid, one hovered shape,
sorted passive query plus the complete 1/60 s physics frame, 96 warmup and 64
measured frames. CPU and GPU run the same authored scene and delivery code.

| Revision / backend | p50 / p95 / p99 (ms) | Owner / all-thread managed bytes | GPU upload / readback per frame | Mean device wait (ms) |
| --- | --- | --- | --- | --- |
| Parent / CPU | 0.2460 / 0.2517 / 0.2608 | 0 / 0 | 0 / 0 B | 0 |
| Parent / GPU | 0.5434 / 0.8443 / 0.9189 | 0 / 0 | 72 / 65,564 B | 0.1419 |
| Adaptive readback / CPU | 0.2533 / 0.2640 / 0.2768 | 0 / 0 | 0 / 0 B | 0 |
| Adaptive readback / GPU | 0.6439 / 2.2192 / 2.3360 | 0 / 0 | 72 / 92 B | 0.5234 |

Complete-frame readback falls 99.86%; the point query itself reads 76 bytes
(8 status + 4 count + one 64-byte hit), with 16 other frame-status bytes.
The regression test bounds this sparse frame's readback to 128 bytes. Query caps,
scene eligibility and selected objects are unchanged.

These measurements do **not** establish a latency win. GPU waits were variable;
a live WaterPlayground and desktop graphics applications shared the device during
the after run. They were not stopped. This quiet workload still favors CPU.
Evidence: `/tmp/e2d-query-picking-before.log` and `/tmp/e2d-query-picking-after.log`.
These numbers are not rigid-body stress results or rendered-window FPS. Native/device
allocation and other platforms remain unmeasured; controlled GPU timing and the
full-goal performance acceptance remain open.

[Registered pointer delivery](physics-registered-adapter.md#registered-pointer-delivery)
now verifies scene Body/Area policy through selected point queries, numerical
guards and 128 changed-policy full frames at zero managed allocation. Native
CPU/GPU/registered worlds each pass with both renderers and target pixel checks;
full registered geometry and final acceptance remain open.

## Prepared physical event delivery

Each emission leases an ordered immutable handler snapshot. Duplicate handlers and last-matching multicast subsequence removal retain ordinary delegate semantics. Reentrant/concurrent changes affect later emissions; user callbacks run outside the storage lock. Subscription prepares a spare buffer for one-shot removal. Capacity or overlapping-snapshot growth can allocate; arbitrary consumer event accessors and user callbacks keep their own allocation policy. Disposal clears these publisher-owned lists after teardown. Pointer events collect per-handler errors and retain eligibility checks; overlap/contact/sleep handlers stop the current event on failure.

EventHandlerTests compares 1,024 mixed delegate add/remove operations with native delegate lists, checks nested snapshot changes/errors, 256 concurrent producers and weak-reference release from disposed publishers. All 18 physical accessors each deliver 128 prepared first one-shot callbacks at strict zero owner/all-thread managed bytes. EventConnectionPhysicsTests adds a real full selected CPU/GPU Area transition with 128 one-shot listeners and a permanent listener. Serial/separate numerical profiles use CPU-only Vulkan for GPU kernels; native allocations, hardware performance, real-window FPS and foreign acceptance remain open.

See [typed handler storage](../classes/EventHandlers.Generic.md) and [ADR 0010](../decisions/core-object-runtime.md#adr-0010).

Inherited [ordinary event blocking](../classes/ElectronObject.md#ordinary-event-delivery) suppresses new event emissions at the publisher, preserving subscriptions and already accepted deferred work. Notifications, virtual callbacks, committed physical state and resource revisions continue; Disposed cleanup remains independent.

## Collapsed canvas mappings

Automatic physics picking skips one canvas when its determinant is exactly zero or nonfinite, or the existing affine inverse produces nonfinite query coordinates. It does not query an arbitrary origin or identity fallback. The root CanvasTransform and each CanvasLayer final transform use the same guard before the selected CPU/GPU point query; a collapsed follow scale is included. There is no epsilon cutoff: small finite mappings and negative determinants remain pickable when the mapped position is finite. Explicit mathematical AffineInverse calls retain their existing exception contract.

A skipped canvas contributes no hits to the current event. Other canvases continue in their normal order, and queued event copies still complete their ordinary disposal path. On mouse/passive refresh, prior object hover exits before shape hover through the existing cleanup; repeated collapsed frames emit no duplicate exits. Restoring a valid mapping reenters under a stationary known pointer, with one internal motion for a newly hovered object. Touch/drag still do not create mouse hover. Collapsing a later canvas from an earlier callback is observed when that canvas begins its query; the existing sampled-hit rules within an already queried canvas remain unchanged.

[PhysicsPickingTests.Singular.cs](../../tests/Electron2D.Tests/PhysicsPickingTests.Singular.cs) checks default and independent canvases, zero/collinear/follow transforms, numerical overflow, small/reflected valid maps, queued mouse/touch/drag, callback mutation, disposal observer failures and 64 active collapsed-canvas frames after 96 warmup. The same cases run on CPU/GPU through the public scene/pointer API. CanvasLayerRenderingTests retains its eight native transform/pixel stages, including zero and negative follow scales, after a warm retention baseline so initial window configuration does not masquerade as transform-triggered geometry rebuilding. Native/backend and allocation evidence is recorded with the verified runs; hardware performance, native allocator totals and other-platform acceptance remain separate.

The native pointer fixture converts its physical target to Wayland SDL logical coordinates at each injection, checks the current dispatch-time pixel density and final transform through ordinary input, then requires the deferred physics callback to preserve that local position. It queues window exit directly from the confirmed hit before a later passive frame can sample the desktop pointer. Target pixels remain independently checked. Dedicated native layer checks now pass the formerly failing collapsed-layer case. The wider renderer run still stops at the separate LegacyParallax retained-draw startup assertion; it is not recorded as a passing global renderer suite.
