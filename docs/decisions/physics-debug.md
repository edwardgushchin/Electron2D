# Physics canvas diagnostics

Last updated: 2026-10-09

<a id="adr-0100"></a>
## ADR 0100: Retained shape and scene physics drawing

- Status: Accepted under the complete CPU/GPU physics objective.
- Scope: Shape.Draw, CollisionShape.DebugColor, SceneTree.DebugCollisionsHint and the existing collision/cast/joint scene families.
- Depends on: [0004](product.md#adr-0004), [0014](resources.md#adr-0014), [0028](rendering.md#adr-0028), [0054 and 0063](physics.md).

Use existing scene and caller-owned canvas-item RIDs and retained drawing commands.
Shape.Draw appends a snapshot of authored local geometry. It requires the current
rendering session and owner thread, observes canvas RID ownership/lifetime and works
inside an existing draw callback. Internal node recording uses the same geometry
routine without requiring a device. No physics backend, body-state mirror, GPU
readback, second world or diagnostic renderer is created by drawing.

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

Keep contact-point publication, Space debug APIs/limits/contact color and virtual
tile owners open until their own shared CPU/GPU consumer is implemented and tested.
Do not introduce unused project settings for those gaps or mark the complete debug
group finished from shape/joint drawing alone. Native rendered evidence and common
CPU/GPU tests belong to the component report; compilation alone is insufficient.
