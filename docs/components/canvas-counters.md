# Completed canvas submission counters

Last updated: 2026-10-21

## Selection and scope

Canvas counters unlock per-viewport diagnostics, static server queries and existing Performance consumers on both current renderers. An all-object memory monitor needs a broader weak lifetime/cache/scene registry. Discardable device images need executable last-use analysis across later framebuffer/sampling/copy consumers; immediate native recording cannot safely remove stores from a flag alone. Neither alternative was reduced to a metadata-only implementation.

## Public API

| Consumer | Operation |
| --- | --- |
| [Viewport](../classes/Viewport.md) | `long GetRenderInfo(ViewportRenderInfoType type, ViewportRenderInfo info)` |
| [RenderingServer](../classes/RenderingServer.md) | `long ViewportGetRenderInfo(RID viewport, ViewportRenderInfoType type, ViewportRenderInfo info)` |
| RenderingServer | `long GetRenderingInfo(RenderingInfo info)` |
| [Performance](../classes/Performance.md) | GetMonitor with RenderTotalObjectsInFrame=11, RenderTotalPrimitivesInFrame=12, RenderTotalDrawCallsInFrame=13 |

[ViewportRenderInfo](../classes/ViewportRenderInfo.md) selects ObjectsInFrame=0, PrimitivesInFrame=1 and DrawCallsInFrame=2; Max=3 is nonselectable. [ViewportRenderInfoType](../classes/ViewportRenderInfoType.md) selects Canvas=2; Max=3 and undefined values reject. The 3D scene and shadow domains are outside the 2D product. [RenderingServer.RenderingInfo](../classes/RenderingServer.RenderingInfo.md) exposes the three matching aggregate counters. Other memory/pipeline producers remain coverage dependencies.

## Meaning and completion

Objects counts logical generated drawing records before compatible batch merging, rather than Nodes. Primitives counts submitted point/line/triangle topology multiplied by requested instances. DrawCalls counts native canvas geometry calls. Tessellation, expanded mesh instances, thin-line fallback and the software triangle workaround retain their actual backend granularity; counters are not expected to be identical across different rendering methods.

GPU-authored indirect visibility/LOD and particle activity use requested capacity/base-geometry budgets. No native synchronization or readback is introduced to inspect active work. These counters do not measure surviving fragments, exact GPU activity, native allocation or hardware throughput. Internal lighting/SDF, clears, copies, resolves, output conversion, presentation and secondary native-surface replay are excluded.

Publication follows successful completion of the renderer frame and precedes FramePostDraw/one-shot completion observers. Observer failures after commitment retain the published values. Earlier failure preserves prior diagnostics, including when another viewport already submitted. Empty completed frames publish zero. Skipped viewport frames retain their own prior values and contribute nothing to that completed frame's aggregate. Disabled rendering and low-processor skipping do not publish a fabricated empty frame.

A viewport starts at zero and retains its completed values when detached; disposed access rejects. Attached Viewport/RID reads require their ordinary owner. Aggregate and Performance reads are allocation-free and safe on any thread, return zero without a native session and never create or synchronize a backend. Several reads are nearby scalar observations, not an atomic snapshot.

## Integration and verification

CanvasBatch retains pre-merge ObjectCount. Both native backends count at their actual geometry calls and retain per-draw scalar totals. RenderingServer stages per-viewport values, aggregates only drawn frames and publishes after successful native completion; borrowed and server-owned viewport RIDs use the same fields. No geometry, GPU memory or callback lifetime is retained by counters.

[CanvasCounterTests](../../tests/Electron2D.Tests/CanvasCounterTests.cs) exercises public unavailable-session queries, invalid selectors, actual native root/offscreen pixels, scene/RID/Performance agreement, empty/skipped/failing/completed-observer frames, owner/disposal, owned low-level viewports, native lines and mesh/indirect budgets. Prepared active and idle render/read cohorts cover managed allocation. Results must identify actual backend/display and software-driver selection; local evidence does not imply foreign/hardware, native allocator, editor or owner acceptance.
