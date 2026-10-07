# Curves component

Last updated: 2026-10-07

## Scope and owned types

Resources domain curve data and generated textures: [Curve](../classes/Curve.md), [Curve.TangentMode](../classes/Curve.TangentMode.md), [Curve2D](../classes/Curve2D.md), [CurveTexture](../classes/CurveTexture.md), [CurveXYZTexture](../classes/CurveXYZTexture.md), [CurveTexture.TextureModeEnum](../classes/CurveTexture.TextureModeEnum.md) and internal [CurveTextureData](../classes/CurveTextureData.md). Sources live in `src/Scene/Resources/`. Curve describes scalar y(x); Curve2D describes spatial Bézier paths. Curves and textures are resources, not Nodes. Entity consumers remain in the separate spatial scene branch; Control remains a sibling under CanvasItem.

## Runtime flow and dependencies

1. Typed point edits commit under the resource lock, invalidate caches and emit the applicable synchronous events outside that lock.
2. Direct scalar/segment sampling uses existing Mathf/Vector2 Bézier operations. Scalar cached sampling uses evenly spaced domain samples; spatial baking uses a bounded polyline, cumulative distance and analytic tangent frames.
3. Spatial distance queries binary-search that cache; closest queries scan its segments. Array-returning methods copy storage.
4. Resource duplication copies exact points/settings independently; PackedScene uses the same hooks and owns local resource copies. ResetState drops only caches. CPU curve operations and texture baking need no active renderer, import or editor. Texture sampling uses the existing renderer.
5. Generated textures subscribe once to each distinct borrowed curve, sample each curve under its own lock, and publish a complete immutable float payload. Sampling uses i/Width in the unit domain, not i/(Width-1) or a remapped curve domain. Source changes rebuild compatible storage; width/mode changes replace allocation identity. Synchronous texture events run after publication outside the texture lock.

## Invariants and current behavior

Explicit inputs are typed and finite; indices and positive ranges are validated. Scalar tangents are slopes, positions are sorted with source-defined tie ordering, insertion/movement clamps coordinates and SetPointValue does not. Spatial handles are relative, order is explicit, and out-of-range insertion indices append. Callback failures preserve committed changes. Concurrent multi-call edits or interleaved notifications are not transactions. Resource copying/disposal uses the inherited caller-coordination contract.

Warm cached sampling and closest queries allocate no managed memory. Editing/baking/tessellating and point-array exports may allocate. Closest queries are linear in baked segments. Per-resource locking serializes independent readers as well as writers; no whole-frame or throughput guarantee is claimed. Spatial subdivision depth is bounded: cache ten, public 0..20. Bake interval is a target density. Nonfinite derived spatial geometry fails without publishing an incomplete cache. Scalar sampling retains float overflow/overshoot and duplicate-offset slope behavior.

[ADR 0013](../decisions/resources.md#adr-0013) records typed mappings and proven pre-release corrections under ADR 0034. Complete member contracts, event traces, edge behavior and API examples are on the class pages. Reference class/member correspondence is in [Curve coverage](../coverage/classes/Curve.md) and [Curve2D coverage](../coverage/classes/Curve2D.md).

## Dependent slices

| Consumer | State and exact trigger |
| --- | --- |
| [Scene Path and PathFollow](scene-paths.md) | Runtime implemented: borrowed curves/subscriptions, progress/ratio, tangent rotation, offsets, looping, reparenting and PackedScene. Configuration diagnostics and optional debug path drawing are integrated; GUI editor authoring remains absent. |
| [CurveTexture](../classes/CurveTexture.md) and [CurveXYZTexture](../classes/CurveXYZTexture.md) | Implemented RF/RGBF generated snapshots, distinct source events, typed copy/local ownership, GPU canvas and HLSL/GLSL bindings. Native compatibility drivers without float textures reject them explicitly. XYZ names three scalar channels. |
| Curve3D | Excluded under strict 2D scope, ADR 0004. |
| Editor curve widgets and disk serialization | Deferred to the first editor and typed asset-format slices. No inert public declarations are added. |

## Verification and limits

[CurveTests](../../tests/Electron2D.Tests/CurveTests.cs) runs through the ordinary managed test executable. It checks exact analytic and baked samples, defaults, events, clamping/tie order, modes, duplicate cleanup and tangent refresh, cache invalidation, tessellation density/depth, closed/degenerate geometry, closest projection, overflow recovery, descriptors, exact shallow/deep/CopyFromResource copies, PackedScene local ownership, a processing Entity consumer, callback failure/disposal, concurrent access and warm-query allocation. The class examples match executable analytic tests.

Verified locally on Linux through Release build and managed tests. [CurveTextureTests](../../tests/Electron2D.Tests/CurveTextureTests.cs) checks RGB/Red/XYZ storage, width bounds, unit-domain sampling, null defaults, copy/subscription aliases, local scene ownership, callback/disposal paths and concurrent coherent publication. [RenderingCurveTextureTests](../../tests/Electron2D.Tests/RenderingCurveTextureTests.cs) verifies Linux Wayland GPU canvas and eight frame stages for each HLSL/GLSL material, preserving negative and HDR values through worker edits, mode/width changes, defaults and atlas bindings. Wayland compatibility and dummy/software reject unsupported float precision explicitly and release resources. Readable images are copied from generated CPU snapshots, following the documented image-copy contract; the pinned reference curve types inherit null/unspecified base queries, an omission recorded in ADR 0013. Default curve textures report 256×1 but have no image until an actual setting/source change; drawing them uninitialized fails explicitly. Red-only mode retains RF CPU storage; both modes upload as RGBA32Float, with no present GPU memory saving. These checks do not establish owner visual acceptance, other platforms, fresh self-contained/AOT delivery or throughput guarantees. See [Resources](../domains/resources.md), ADRs [0013](../decisions/resources.md#adr-0013), [0014](../decisions/resources.md#adr-0014), [0034](../decisions/core-math.md#adr-0034), [0035](../decisions/core-math.md#adr-0035).

## Particle authoring persistence

Scalar Curve now supplies registered bounded typed file storage. `_curve_data` preserves exact points/tangents/modes, domain/value limits and bake resolution; validation precedes atomic state publication. The copied record omits baked samples and native identity. CPUParticlesTests loads it in a fresh process, checks aliases and tangent values, then consumes it in real simulation. This extends persistence without altering scalar sampling behavior.
