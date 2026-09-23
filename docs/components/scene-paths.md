# Scene paths component

Last updated: 2026-09-23

## Scope and owned types

[Path](../classes/Path.md) and [PathFollow](../classes/PathFollow.md) are direct Entity subclasses in `src/Scene/2D/`. They implement spatial path containment and point sampling; they do not implement navigation or route search. Path owns a borrowed PathCurve reference; followers use only their direct parent while attached. Resources stay outside the Node hierarchy.

## Runtime flow

1. Construct a Path with a Curve and add PathFollow children; add game/drawable nodes under followers.
2. Real scene entry binds a follower to its direct Path parent and samples stored progress. Exiting clears that binding. Detached reconstruction keeps stored transforms without pretending to activate a tree.
3. Progress/offset/rotation setters sample the existing curve cache, set rotation then position and preserve scale/skew. Loop and CubicInterp edits only store policy. Gameplay or Tween supplies progress; no extra clock or scheduler exists.
4. A Path's resource Changed event updates eligible children on the scene owner; worker events queue existing SceneTree deferred work. Each queued action revalidates resource identity, tree and membership generation. Paused scenes still execute a deferred flush.
5. PackedScene stores typed settings and handles ordinary borrowing or local graph duplication. Node disposal disconnects Curve; the scene root owns only local duplicates. All live follower siblings are attempted when an update fails. Callback failure leaves committed state visible; reentry cannot let an older rotation update overwrite a newer follower position.

## Invariants and dependencies

Uses only Entity, CanvasItem, Node, PathCurve, Resource, typed descriptors, PackedScene and SceneTree. Positions/distances/offsets use local units. Canonical epsilon governs wrapped endpoint selection. Null/zero-length curves retain transforms, while progress assignment on a zero-length curve clamps to zero. Curve edits/entry do not renormalize progress; ProgressRatio can exceed one. Rotates=false keeps rotation and makes offsets local X/Y; true uses tangent/perpendicular directions. Reparent keepGlobalTransform=true preserves the original global transform until another path update.

Mutation and transform callbacks stay on the owner thread. Worker changes allocate deferred actions rather than updating scene state directly. Bulk editing should be coordinated by the caller; no atomic multi-resource transaction or queue coalescing is claimed. Warm repeated progress updates allocate no managed memory in the measured path. The shared CanvasItem transform traversal now uses pooled child snapshots, returned with cleared references even after callback failures; this preserves mutation-safe traversal without an array allocation per moved ancestor.

## Remaining shared capabilities

| Capability | Exact implementation trigger |
| --- | --- |
| Node configuration warnings and their SceneTree change signal | First typed Node diagnostic API slice must add the override for a visible attached PathFollow whose direct parent is not Path, with warning-change delivery. Base coverage remains Unimplemented. |
| SceneTree debug_paths_hint visualization | First scene debug-path visualization slice must propagate toggles, draw the path and tangent markers, and react to resource/visibility/tree changes. It can use the existing backend-neutral canvas primitives; no new dependency approval or navigation subsystem is needed. |
| Editor curve handles/selection and delayed editor refresh | First self-hosted editor curve-authoring slice. Runtime paths do not add editor timers or private drawing hooks. |
| CurveTexture and CurveXYZTexture | Separate resource/rendering slice already described by [Curves](curves.md#dependent-slices). |
| Path3D and PathFollow3D | Excluded by ADR 0004; no implementation task. |

## Verification

[PathTests](../../tests/Electron2D.Tests/PathTests.cs) exercises defaults, finite validation, exact positive/negative multiples, wrap/clamp, ratio failures, parent/lifecycle/neutral boundaries, offsets, rotation and preserved scale/skew, policy timing, Tween-driven movement, curve replacement, same-resource events, reparenting with both transform policies, PackedScene stored/local ownership, worker/owner delivery, reentry, failed sibling updates, stale deferred work, disposal and warm allocations. Full managed regression checks pass.

[PathRenderingTests](../../tests/Electron2D.Tests/PathRenderingTests.cs) runs a seven-frame native readback sequence: initial placement, progress, changed direction, worker edit, offset, hidden movement and showing. Verified on Linux Wayland compatibility and GPU/Vulkan, plus dummy/software. It is part of the normal rendering harness; set ELECTRON2D_TEST_RENDER=1 and ELECTRON2D_TEST_PATHS=1 for only this native slice. Owner visual acceptance, other platforms, published/AOT delivery and frame-time scaling remain unverified.

See [Scene domain](../domains/scene.md), [Path coverage](../coverage/classes/Path2D.md), [PathFollow coverage](../coverage/classes/PathFollow2D.md), ADRs [0008](../decisions/scene.md#adr-0008), [0011](../decisions/scene.md#adr-0011), [0013](../decisions/resources.md#adr-0013), [0023](../decisions/scene.md#adr-0023).
