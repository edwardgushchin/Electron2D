# Completed canvas submission counters

Last updated: 2026-10-27

## Selection and scope

Canvas counters unlock per-viewport diagnostics, static server queries and existing Performance consumers on both current renderers. An all-object memory monitor needs a broader weak lifetime/cache/scene registry. Discardable device images need executable last-use analysis across later framebuffer/sampling/copy consumers; immediate native recording cannot safely remove stores from a flag alone. Neither alternative was reduced to a metadata-only implementation.

## Public API

| Consumer | Operation |
| --- | --- |
| [Viewport](../classes/Viewport.md) | `long GetRenderInfo(ViewportRenderInfoType type, ViewportRenderInfo info)` |
| [RenderingServer](../classes/RenderingServer.md) | `long ViewportGetRenderInfo(RID viewport, ViewportRenderInfoType type, ViewportRenderInfo info)` |
| RenderingServer | `long GetRenderingInfo(RenderingInfo info)` |
| [Performance](../classes/Performance.md) | GetMonitor with RenderTotalObjectsInFrame=11, RenderTotalPrimitivesInFrame=12, RenderTotalDrawCallsInFrame=13 |

[ViewportRenderInfo](../classes/ViewportRenderInfo.md) selects ObjectsInFrame=0, PrimitivesInFrame=1 and DrawCallsInFrame=2; Max=3 is nonselectable. [ViewportRenderInfoType](../classes/ViewportRenderInfoType.md) selects Canvas=2; Max=3 and undefined values reject. The 3D scene and shadow domains are outside the 2D product. [RenderingServer.RenderingInfo](../classes/RenderingServer.RenderingInfo.md) exposes those aggregates and PipelineCompilationsCanvas=6. Performance.PipelineCompilationsCanvas=34 reads the same cumulative producer. Device memory and other pipeline sources remain coverage dependencies.

## Meaning and completion

Objects counts logical generated drawing records before compatible batch merging, rather than Nodes. Primitives counts submitted point/line/triangle topology multiplied by requested instances. DrawCalls counts native canvas geometry calls. Tessellation, expanded mesh instances, thin-line fallback and the software triangle workaround retain their actual backend granularity; counters are not expected to be identical across different rendering methods.

GPU-authored indirect visibility/LOD and particle activity use requested capacity/base-geometry budgets. No native synchronization or readback is introduced to inspect active work. These counters do not measure surviving fragments, exact GPU activity, native allocation or hardware throughput. Internal lighting/SDF, clears, copies, resolves, output conversion, presentation and secondary native-surface replay are excluded.

Publication follows successful completion of the renderer frame and precedes FramePostDraw/one-shot completion observers. Observer failures after commitment retain the published values. Earlier failure preserves prior diagnostics, including when another viewport already submitted. Empty completed frames publish zero. Skipped viewport frames retain their own prior values and contribute nothing to that completed frame's aggregate. Disabled rendering and low-processor skipping do not publish a fabricated empty frame.

A viewport starts at zero and retains its completed values when detached; disposed access rejects. Attached Viewport/RID reads require their ordinary owner. Aggregate and Performance reads are allocation-free and safe on any thread, return zero without a native session and never create or synchronize a backend. Several reads are nearby scalar observations, not an atomic snapshot.

## Canvas pipeline compilation

RenderingServer.RenderingInfo.PipelineCompilationsCanvas (6) and
Performance.Monitor.PipelineCompilationsCanvas (34) read one atomic cumulative
count of successfully created engine-owned SDL GPU canvas graphics pipelines.
It includes the initial prepared pipeline before the first frame, material/mesh
variants and internal canvas lighting, conversion and output passes using that
same factory. Cache hits do not increment it. A retired pipeline remains part of
the historical total; creating it again increments the count. Native creation
failure contributes nothing; a later frame or completion-observer failure retains
already successful creations. Unlike submission totals, it needs no successful
frame publication. Completed closure returns zero, and each renderer starts a
new count. A racing reader may see the preceding managed value without using a
retired native handle.

Compatibility owns no explicit SDL GPU pipeline objects and reports zero for
this capability. Local RenderingDevice graphics, compute and physics pipelines,
source import and driver-internal cache/compiler activity are outside the canvas
producer. This counts successful native pipeline creation requests; it does not
claim exact driver compilation events, latency or retained device bytes. Other
pipeline source categories are pinned 3D scene-renderer sources, excluded under ADR 0004 without constant adapters. See the [producer audit](../coverage/performance-producers.md).

The pinned [renderer aggregate](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/rendering_server_default.cpp)
and [canvas pipeline source](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/renderer_rd/renderer_canvas_render_rd.cpp)
distinguish cumulative creation sources from completed drawing counts. Electron2D
maps its canvas-owned native factory to that canvas source without assigning
unimplemented mesh/surface/draw/background producers to it.

The public-only [CanvasPipelineContract](../../tests/Portability/CanvasPipelineContract.cs)
uses `--canvas-pipelines managed|gpu|compatibility`; the root profile
`ELECTRON2D_TEST_CANVAS_PIPELINES` adds rejected native pipeline creation through
[CanvasPipelineFaultTests](../../tests/Electron2D.Tests/CanvasPipelineFaultTests.cs).
The contract covers cold owner/worker reads, exact selector gaps, actual canvas
pixels, successful variants/cache hits, custom-pipeline retirement/recreation,
failed authoring/completion, two session lifetimes, reads racing teardown,
prepared full render/read cycles and worker reads. The Linux cohort passes ten
selected CLR/strict NativeAOT profiles: Vulkan canvas rendering on RTX 3090 Ti,
X11 software compatibility with framebuffer acceleration disabled, cold reads
and adjacent deferred/navigation regressions. GPU sessions start at one and end
at six successful creations (seven with the root retry); software remains zero.
Across six native render profiles, two sessions each measure 64 complete active
render/read cycles: 768 total, with zero owner/all-thread managed bytes. The root
also rejects three injected native creations, including startup before service
publication. Source/PDB checks match all exercised runtime/consumer inputs.

See the local [validation receipt](../../bin/canvas-pipeline-monitors/2026-10-27/validation.json).
Software Add blending remains unsupported; that control uses ordinary Mix. This
counts the exercised native creation contract and does not establish other
compilation sources, total device memory, GPU physics throughput, foreign-platform
or full goal acceptance.

## Integration and verification

CanvasBatch retains pre-merge ObjectCount. Both native backends count at their actual geometry calls and retain per-draw scalar totals. RenderingServer stages per-viewport values, aggregates only drawn frames and publishes after successful native completion; borrowed and server-owned viewport RIDs use the same fields. No geometry, GPU memory or callback lifetime is retained by counters.

[CanvasCounterTests](../../tests/Electron2D.Tests/CanvasCounterTests.cs) exercises public unavailable-session queries, invalid selectors, actual native root/offscreen pixels, scene/RID/Performance agreement, empty/skipped/failing/completed-observer frames, owner/disposal, owned low-level viewports, native lines and mesh/indirect budgets. Prepared active and idle render/read cohorts cover managed allocation. Results must identify actual backend/display and software-driver selection; local evidence does not imply foreign/hardware, native allocator, editor or owner acceptance.
