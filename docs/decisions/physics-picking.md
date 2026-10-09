# Physics pointer delivery

Last updated: 2026-10-09

<a id="adr-0099"></a>
## ADR 0099: Viewport physics picking

- Status: Accepted under the requested complete CPU/GPU physics contract.
- Scope: CollisionObject pointer callbacks/signals, PhysicsBody defaults, Viewport picking policies and their input/physics lifecycle.
- Depends on: [0004](product.md#adr-0004), [0038](input.md#adr-0038), [0054 and 0063](physics.md), [0090](agent-native.md#adr-0090).

### Decision

Use the same selected World and CPU/GPU point-query geometry as game queries.
Picking neither creates a second physics world nor approximates shapes in a UI
hit-test engine. Scene eligibility requires InputPickable, a nonzero layer,
visibility, processing permission and live physical membership. InputPickable
starts true on CollisionObject/Area and false on PhysicsBody. All flags are stored.
Detached and non-root viewports start with picking disabled. Scene activation samples
the typed physics/common/enable_object_picking project setting (true initially),
including feature overrides, for its root viewport. Because library hosts supply
the root object rather than receiving an engine-created root, an explicit caller
or packed-scene property value wins. Later project edits affect later activations;
live viewport edits remain available. This follows the root-project default in the
pinned scene-tree initialization while preserving caller-owned configuration.

After ordinary Input, GUI, shortcut, unhandled-key and unhandled stages, an enabled
viewport queues eligible unhandled mouse/touch/drag events and marks that input
handled. Copies preserve event coordinates/modifiers and live only through later
callback delivery. At the next physics-frame boundary, after PhysicsFrameStarted
and before node physics callbacks, each queued event receives fresh handled state.
A callback may stop later picks through the receiving viewport. Nested dispatch
retains the existing rejection policy. Touch/drag do not produce mouse hover.

The pinned [viewport implementation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/main/viewport.cpp)
applies a 64 logical-shape result cap separately to each canvas, then sorts by
effective Z and reverse tree order when enabled. First-only likewise stops that
canvas's delivery; marking handled stops subsequent canvases too. Preserve that
scope. Inter-canvas order and the capped unsorted subset are not a topmost-global
selection guarantee. Internal children participate in tree order. Each canvas's
inverse transform and exact instance ID select its point-query space. The event
itself stays in viewport coordinates. CustomViewport redirects canvas presentation,
not physics ownership: picking needs the selected viewport to share the collider's
World. Shared worlds may deliver an event from a viewport other than the node's
structural parent viewport.

The hit's assigned ColliderObject is the callback receiver when it is a live
CollisionObject in this tree. Null, disposed or non-collider associations receive
no pointer callbacks. Physical source eligibility and slot identity remain separate
from the target's processing permission. Sort by the assigned target's Z/tree order;
hover deduplicates by assigned object and shape index. Explicit rebinding cannot
retarget an already sampled hit; a later query observes the new association.

Virtual callbacks precede their typed signals. InputEvent keeps the pinned Node
signal parameter and concrete Viewport virtual-callback parameter. Entering an
object precedes entering its shape, then input delivery. Moving between its shapes
does not repeat object entry. Departed object callbacks precede departed shape
callbacks. Re-evaluate stationary hover for object/camera/eligibility changes;
internal mouse motion delivers input only to a newly hovered object. Reuse its
storage, use current native coordinates when available, and preserve the existing
headless local pointer position otherwise. No new event is required to enable
picking under an already known stationary pointer.

Queue/hover state belongs to each viewport. Disabling picking or GUI input, native
pointer exit, viewport detachment and disposal clear pending copies and hover.
Pointer capture suppresses picking. Input consumed by a GUI/control or ordinary
input callback never queues its original event for physics. Passive hover remains
a separate refresh and can resume when a GUI obstruction disappears. Stale/dead/removed/reindexed shape hits are
rejected using sampled slot identity, shape index and world/canvas association.
Mutations from callbacks commit; disposal skips further delivery to that object.
Virtual/signal errors are collected while other eligible callbacks continue, with
cleanup completed before the aggregate error escapes. Clear requests during a
picking callback finish at its current pass boundary.

CPU/GPU use one delivery path. Query and traversal scratch are reused after warmup;
queued external events may allocate their owned copies outside the physics step.
GPU point queries return complete hit metadata before CPU scene-policy filtering;
this is not a CPU body-state mirror. Predict transfer prefixes from prior counts
and fetch any missing tails without rerunning the GPU search. Prediction must never
truncate results or change the query cap. Measure both the normal fence and any
extra tail fence; returned bytes should follow hits rather than world capacity.
Broader GPU/network completion remains separate.

### Verification

[Physics picking](../components/physics-picking.md) records common CPU/GPU,
headless/native, lifecycle, allocation and cost checks. Inherited API gaps remain
on their own coverage pages. The complete CollisionObject shape-owner/disable
family still needs its full public cross-backend conformance audit.
