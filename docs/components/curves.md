# Curves component

Last updated: 2026-09-23

## Scope and owned types

Resources domain CPU curve data: [Curve](../classes/Curve.md), [Curve.TangentMode](../classes/Curve.TangentMode.md) and [PathCurve](../classes/PathCurve.md). Sources live in `src/Scene/Resources/`. Curve describes scalar y(x); PathCurve describes spatial Bézier paths. Neither is a Node. Entity consumers remain in the separate spatial scene branch; Control remains a sibling under CanvasItem.

## Runtime flow and dependencies

1. Typed point edits commit under the resource lock, invalidate caches and emit the applicable synchronous events outside that lock.
2. Direct scalar/segment sampling uses existing Mathf/Vector2 Bézier operations. Scalar cached sampling uses evenly spaced domain samples; spatial baking uses a bounded polyline, cumulative distance and analytic tangent frames.
3. Spatial distance queries binary-search that cache; closest queries scan its segments. Array-returning methods copy storage.
4. Resource duplication copies exact points/settings independently; PackedScene uses the same hooks and owns local resource copies. ResetState drops only caches. No SDL, renderer, import or editor dependency exists.

## Invariants and current behavior

Explicit inputs are typed and finite; indices and positive ranges are validated. Scalar tangents are slopes, positions are sorted with source-defined tie ordering, insertion/movement clamps coordinates and SetPointValue does not. Spatial handles are relative, order is explicit, and out-of-range insertion indices append. Callback failures preserve committed changes. Concurrent multi-call edits or interleaved notifications are not transactions. Resource copying/disposal uses the inherited caller-coordination contract.

Warm cached sampling and closest queries allocate no managed memory. Editing/baking/tessellating and point-array exports may allocate. Closest queries are linear in baked segments. Per-resource locking serializes independent readers as well as writers; no whole-frame or throughput guarantee is claimed. Spatial subdivision depth is bounded: cache ten, public 0..20. Bake interval is a target density. Nonfinite derived spatial geometry fails without publishing an incomplete cache. Scalar sampling retains float overflow/overshoot and duplicate-offset slope behavior.

[ADR 0013](../decisions/resources.md#adr-0013) records typed mappings and proven pre-release corrections under ADR 0034. Complete member contracts, event traces, edge behavior and API examples are on the class pages. Reference class/member correspondence is in [Curve coverage](../coverage/classes/Curve.md) and [Curve2D coverage](../coverage/classes/Curve2D.md).

## Dependent slices

| Consumer | State and exact trigger |
| --- | --- |
| Scene Path and PathFollow concepts | Unimplemented. Curve geometry and Entity are now available; the first path scene slice must add curve ownership/subscriptions, progress/distance/ratio updates, tangent rotation, offsets, looping, reparenting and PackedScene semantics. Navigation is not a prerequisite. |
| CurveTexture and CurveXYZTexture | Unimplemented. Scalar Curve and Texture exist; the first curve-texture slice must implement curve-change rebaking, width/channel/storage policy, native float-format sampling and both renderer capability/error paths. GUI is not a prerequisite. XYZ names three scalar channels, not a spatial 3D resource. |
| Curve3D | Excluded under strict 2D scope, ADR 0004. |
| Editor curve widgets and disk serialization | Deferred to the first editor and typed asset-format slices. No inert public declarations are added. |

## Verification and limits

[CurveTests](../../tests/Electron2D.Tests/CurveTests.cs) runs through the ordinary managed test executable. It checks exact analytic and baked samples, defaults, events, clamping/tie order, modes, duplicate cleanup and tangent refresh, cache invalidation, tessellation density/depth, closed/degenerate geometry, closest projection, overflow recovery, descriptors, exact shallow/deep/CopyFromResource copies, PackedScene local ownership, a processing Entity consumer, callback failure/disposal, concurrent access and warm-query allocation. The class examples match executable analytic tests.

Verified locally on Linux through Release build and managed tests. This CPU slice changes no native backend; it does not establish native/visual/owner acceptance, cross-platform or published AOT delivery, or performance beyond the measured warm allocations. No native probe is required for the independent CPU resource behavior. See [Resources](../domains/resources.md), ADRs [0013](../decisions/resources.md#adr-0013), [0014](../decisions/resources.md#adr-0014), [0034](../decisions/core-math.md#adr-0034), [0035](../decisions/core-math.md#adr-0035).
