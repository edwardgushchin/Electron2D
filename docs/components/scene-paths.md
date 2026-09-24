# Scene paths component

Last updated: 2026-09-23

## Scope and owned types

[Path](../classes/Path.md) and [PathFollow](../classes/PathFollow.md) are direct Entity subclasses in `src/Scene/2D/`. They implement spatial path containment, optional debug drawing and point sampling; they do not implement navigation or route search. Path owns a borrowed Curve2D reference; followers use only their direct parent while attached. Resources stay outside the Node hierarchy.

## Runtime flow

1. Construct a Path with a Curve and add PathFollow children; add game/drawable nodes under followers.
2. Real scene entry binds a follower to its direct Path parent and samples stored progress. Exiting clears that binding. Detached reconstruction keeps stored transforms without pretending to activate a tree.
3. Progress/offset/rotation setters sample the existing curve cache, set rotation then position and preserve scale/skew. Loop and CubicInterp edits only store policy. Gameplay or Tween supplies progress; no extra clock or scheduler exists.
4. A Path's resource Changed event updates eligible children on the scene owner; worker events queue existing SceneTree deferred work. Each queued action revalidates resource identity, tree and membership generation. Paused scenes still execute a deferred flush.
5. PackedScene stores typed settings and handles ordinary borrowing or local graph duplication. Node disposal disconnects Curve; the scene root owns only local duplicates. All live follower siblings are attempted when an update fails. Callback failure leaves committed state visible; reentry cannot let an older rotation update overwrite a newer follower position.

## Invariants and dependencies

Uses only Entity, CanvasItem, Node, Curve2D, Resource, typed descriptors, PackedScene and SceneTree. Positions/distances/offsets use local units. Canonical epsilon governs wrapped endpoint selection. Null/zero-length curves retain transforms, while progress assignment on a zero-length curve clamps to zero. Curve edits/entry do not renormalize progress; ProgressRatio can exceed one. Rotates=false keeps rotation and makes offsets local X/Y; true uses tangent/perpendicular directions. Reparent keepGlobalTransform=true preserves the original global transform until another path update.

Mutation and transform callbacks stay on the owner thread. Worker changes allocate deferred actions rather than updating scene state directly. Bulk editing should be coordinated by the caller; no atomic multi-resource transaction or queue coalescing is claimed. Warm repeated progress updates allocate no managed memory in the measured path. The shared CanvasItem transform traversal now uses pooled child snapshots, returned with cleared references even after callback failures; this preserves mutation-safe traversal without an array allocation per moved ancestor.

## Remaining shared capabilities

| Capability | Exact implementation trigger |
| --- | --- |
| Editor curve handles/selection and delayed editor refresh | First self-hosted editor curve-authoring slice. Runtime paths do not add editor timers or private drawing hooks. |
| CurveTexture and CurveXYZTexture | Separate resource/rendering slice already described by [Curves](curves.md#dependent-slices). |
| Path3D and PathFollow3D | Excluded by ADR 0004; no implementation task. |

## Verification

[PathTests](../../tests/Electron2D.Tests/PathTests.cs) exercises defaults, finite validation, exact positive/negative multiples, wrap/clamp, ratio failures, parent/lifecycle/neutral boundaries, offsets, rotation and preserved scale/skew, policy timing, Tween-driven movement, curve replacement, same-resource events, reparenting with both transform policies, PackedScene stored/local ownership, worker/owner delivery, reentry, failed sibling updates, stale deferred work, disposal and warm allocations. Full managed regression checks pass.

[PathRenderingTests](../../tests/Electron2D.Tests/PathRenderingTests.cs) runs a seven-frame native readback sequence: initial placement, progress, changed direction, worker edit, offset, hidden movement and showing. Verified on Linux Wayland compatibility and GPU/Vulkan, plus dummy/software. It is part of the normal rendering harness; set ELECTRON2D_TEST_RENDER=1 and ELECTRON2D_TEST_PATHS=1 for only this native slice. Owner visual acceptance, other platforms, published/AOT delivery and frame-time scaling remain unverified.

See [Scene domain](../domains/scene.md), [Path coverage](../coverage/classes/Path2D.md), [PathFollow coverage](../coverage/classes/PathFollow2D.md), ADRs [0008](../decisions/scene.md#adr-0008), [0011](../decisions/scene.md#adr-0011), [0013](../decisions/resources.md#adr-0013), [0023](../decisions/scene.md#adr-0023).

## Configuration diagnostics and path drawing

Node.GetConfigurationWarnings returns ordered typed strings; PathFollow appends a missing-direct-parent warning only while visible in a tree. Node.UpdateConfigurationWarnings emits SceneTree.NodeConfigurationWarningChanged synchronously only for EditedSceneRoot and its descendants. The host explicitly selects a live attached root; removal clears it before NodeRemoved. The query is independent of selection and is not invoked implicitly by a refresh. Empty results and repeated requests remain observable. There is no blanket subscription to all property/visibility changes. Implemented setters listed below explicitly request refresh; other derived setters or the consuming tool request it.

SceneTree.DebugPathsHint defaults false and invalidates all attached paths on toggles. Path records one-pixel line segments sampled about every ten local units and two five-unit tangent markers every fourth sample through existing CanvasItem.DrawLine. Color comes from the typed debug/shapes/paths/geometry_color setting, default (0.1, 1, 0.7, 0.4), sampled with feature overrides when the tree is constructed. Runtime setting edits affect later trees. Geometry follows canvas transforms, visibility, material/modulation and sorting. Resource edits invalidate before follower callbacks, so a failing follower cannot suppress required redraw. Reentry records fresh geometry; disable/null/empty/near-zero curves leave no stale path commands. More than 1,048,576 samples raises InvalidOperationException before geometry generation, using the existing recording-failure cleanup.

These optional diagnostics are available from the single runtime assembly in every build configuration under ADRs 0012/0027. EditedSceneRoot is an opt-in tooling boundary; it does not instantiate an editor. The reference's documented ineffective live path toggle is corrected by invalidation under ADR 0034. Automatic editor hint drawing, curve handles and scene dock presentation remain editor work. The pinned 2D drawing uses one-pixel mesh lines and does not consume the separate geometry_width setting; that setting is not claimed implemented here.

SceneDiagnosticsTests covers query/refresh separation, repeated events, selected roots/descendants/outside nodes, errors, off-thread rejection, disposal/removal and the geometry budget. PathRenderingTests adds fourteen native readback stages for toggle on/off, direction markers, visibility, worker edits, transform, null/single/zero curves and exit/reentry. Verified on Linux Wayland GPU/compatibility and dummy/software; other platforms, visual owner acceptance and published/AOT builds are unverified.

The SDL software triangle backend truncates destination vertices: at half-pixel placement, a one-pixel diagonal can collapse completely, including tangent markers. PathRenderingTests explicitly checks that limitation and separately proves a marker is visible at integer placement. Wayland hardware backends preserve the half-pixel marker. No identical subpixel rasterization is claimed for software under ADR 0028.

Sibling audit: Node2D adds no warning; Sprite has no warning override. Timer warns below 0.05 - Mathf.Epsilon seconds; AnimatedSprite warns about a null library. CanvasItem ZIndex, Timer WaitTime/Start(duration), AnimatedSprite library replacement and Window title changes now request the corresponding refresh. CanvasItem clipping/CanvasGroup warnings require those absent features; window accessibility-name refresh requires the absent accessibility API. No inactive warning placeholders are added. SceneDiagnosticsTests exercises the implemented sibling conditions, ordering and failures.
