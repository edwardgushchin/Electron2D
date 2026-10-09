# Physics canvas diagnostics

Last updated: 2026-10-09

<a id="adr-0100"></a>
## ADR 0100: Retained scene physics drawing and bounded contacts

- Status: Accepted under the complete CPU/GPU physics objective.
- Scope: Shape.Draw, CollisionShape.DebugColor, SceneTree.DebugCollisionsHint the existing collision/cast/joint scene families and bounded space contacts.
- Depends on: [0004](product.md#adr-0004), [0014](resources.md#adr-0014), [0028](rendering.md#adr-0028), [0054 and 0063](physics.md).

Use existing scene and caller-owned canvas-item RIDs and retained drawing commands.
Shape.Draw appends a snapshot of authored local geometry. It requires the current
rendering session and owner thread, observes canvas RID ownership/lifetime and works
inside an existing draw callback. Internal node recording uses the same geometry
routine without requiring a device. Authored shape drawing creates no physics backend, body-state mirror, GPU
readback, second world or diagnostic renderer. Contact diagnostics use the existing
world, as specified below.

Circles/capsules use fixed contour tessellation, rectangles/full convex contours
are filled, hollow segments stay lines, separation rays carry direction arrows,
and world boundaries draw finite markers on the normalized analytic plane already
accepted in ADR 0054. The typed collision-outline project setting controls opaque
one-pixel outlines for filled shapes. It is sampled when commands are recorded;
existing commands require explicit redraw after a setting change.

CollisionShape.DebugColor is stored, finite and initialized from the typed project
shape color at node construction, including active feature overrides. Disabled
shapes use half-alpha gray; one-way shapes additionally show their local direction.
CollisionPolygon shows its authored closed contour and one-way marker. RayCast and
ShapeCast show their target direction and cached collision state; ShapeCast repeats
its shape along the target. Pin, groove and spring markers describe authored local
anchors/guide extents, not reconstructed solved constraint impulses.

SceneTree.DebugCollisionsHint starts false. It invalidates diagnostic nodes on live
changes, following the existing DebugPathsHint library-host behavior. This is an
explicit adaptation of the pinned SceneTree property's editor-startup hint: its
documentation warns that runtime writes may not refresh existing drawings. A host
without a separate editor must be able to switch its retained diagnostics directly.
Diagnostics work in Release too; this adds no sandbox buttons or editor service.
Visibility, clipping, interpolation, layers, transforms and modulation use ordinary
canvas rules. Mutating/disposed resources invalidate recordings; steady rendering
and warmed rerecording reuse canvas storage. Degenerate geometry draws no fill;
nonfinite or unbounded diagnostic work rejects explicitly before recording.

Contact diagnostics are a bounded snapshot of boundary points in the latest solved
manifold batch, sampled before its pose advancement. Each penetrating manifold
point contributes up to two world-space surface samples; the cap counts individual
points and may be odd. Speculative nonpenetrating points, sensors and sleeping
pairs are omitted. This is a visual sample, not an event history, body contact report
or union of every CCD/substep contact. Neither ordering nor the capped subset is
promised across backends. Gameplay report limits and impulses stay independent.

SceneTree samples the project contact color and maximum at construction. The default
limit is 10,000; zero keeps shape diagnostics but disables contact capture. Enabling
prepares high-water host/device storage before stepping. Every positive active step
replaces the sample, including an empty world; zero-time/inactive steps retain it.
Disabling clears the sample immediately and stops GPU diagnostic compute/copies;
tree detach relinquishes the request even when the caller retains its World.

CPU consumes already retained manifold coordinates. GPU compacts boundary positions
before pose advancement in the existing solver submission. Only an 8-byte status
and selected point coordinates cross to the host, with an exact tail read if the
sample grows. No full manifold/body mirror or extra collision pass is allowed.
Capacity growth and driver/native allocations are outside the warmed managed budget;
readback bytes, fences and waits must be counted in whole-path measurements.

The renderer owns one retained item per world on its default canvas, so viewports
sharing that world do not duplicate points. Markers are filled 5-by-5 scene-unit
rectangles offset by (-2,-2), using the sampled color and ordinary viewport/canvas
transforms. CPU contact capture runs without a display or renderer. No sandbox
controls are restored by this engine capability.

The pinned debug-space methods are internal server operations and virtual extension
hooks, not the scripted PhysicsServer API. Keep the implemented consumers internal;
do not add unsupported public static wrappers or close the complete backend-extension
family. Tile-owner diagnostics and backend extensions remain open. Native rendered
evidence and common CPU/GPU tests belong to the component report; compilation alone
is insufficient.
