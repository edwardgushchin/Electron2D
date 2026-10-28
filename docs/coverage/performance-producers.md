# Performance producer audit

Last updated: 2026-10-28

This audit follows reference `4.7.2-stable` at
`ed1daf0bf001b61586d9930840f2f1394092c079`. It separates executable planar
diagnostics from 3D-only source categories and the remaining allocation work.
The public current/peak producers remain incomplete. The later [registered extension
replacement](../components/extensions.md) closes a different inherited row; current
physics declaration coverage is **1363/1369 (99.56%)**, six open, 92 Excluded.

## Pipeline source ownership

The public monitor descriptions alone do not identify every renderer domain.
Trace the complete producer chain before classifying the categories:

1. [Performance](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/main/performance.cpp#L277-L286)
   forwards the five compilation selectors to RenderingServer.
2. [RenderingServer](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/rendering_server_default.cpp#L314-L323)
   reads canvas for CANVAS and scene rendering for SURFACE. MESH, DRAW and
   SPECIALIZATION sum canvas and scene slots. Those sums alone do not prove
   that both domains produce each category.
3. The pinned RD canvas has one
   [pipeline lookup](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/renderer_rd/renderer_canvas_render_rd.cpp#L475-L507),
   including planar mesh vertex-array selection. It always passes
   PIPELINE_SOURCE_CANVAS. Its
   [counter binding](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/renderer_rd/renderer_canvas_render_rd.cpp#L1683)
   and [reader](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/renderer_rd/renderer_canvas_render_rd.cpp#L2181-L2184)
   share a zero-initialized source array. The generic
   [pipeline cache](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/renderer_rd/pipeline_hash_map_rd.h#L110-L125)
   increments only the supplied source. The other canvas slots remain zero.
   [GLES canvas](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/drivers/gles3/rasterizer_canvas_gles3.h#L377)
   owns no such pipeline count.
4. Scene culling
   [delegates to the scene renderer](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/renderer_scene_cull.cpp#L1591-L1593).
   Forward Mobile and Forward Clustered produce the other categories during
   3D mesh generation, surface-cache preparation, scene drawing and background
   specialization. For example, Mobile selects
   [MESH](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/renderer_rd/forward_mobile/render_forward_mobile.cpp#L407),
   [DRAW/SPECIALIZATION](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/renderer_rd/forward_mobile/render_forward_mobile.cpp#L2591)
   and [SURFACE](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/renderer_rd/forward_mobile/render_forward_mobile.cpp#L3447).

Under [ADR 0004](../decisions/product.md#adr-0004), exclude those four
3D categories in all three declaration families: Performance.Monitor,
RenderingServer.RenderingInfo and RenderingServer.PipelineSource. These are
12 Excluded rows, not implemented counters or future planar dependencies.
Do not expose constant-zero mirrors. The applicable canvas producer remains
implemented under [ADR 0028](../decisions/rendering.md#adr-0028), including
planar mesh/material variants and its documented native ownership boundaries.
The remaining PipelineSource enum/CANVAS/sentinel rows retain their own audit
states; this source trace does not implement that separate public enum.

## Direct owned native storage

The internal NativeAllocation ledger now replaces direct NativeMemory and HGlobal payload allocation in the managed runtime. It owns the raw pointer and requested byte size in a private prefix, preserves ordinary fundamental or requested power-of-two alignment, and serializes successful allocation/resize/free publication with coherent current/peak reads. The peak updates at each successful boundary and survives frees; failed requests retain both the old block and counters. Requested payload bytes exclude allocator prefixes/padding. Zero-byte blocks remain releasable with a zero payload contribution. This is CPU native storage; GPU memory and managed wrapper objects are not counted again.

Current consumers are FAudio's engine/effect allocator callbacks and streaming callback storage, text font/data/table/bitmap callbacks, retained ICU data, accessibility callbacks and temporary byte buffers, DTLS cookies, gzip native wrapper state, filesystem stat buffers and native message-button records. Borrowed native pointers keep their original release functions and never enter this allocator. FAudio allocation failures return null across the unmanaged callback boundary; no managed out-of-memory exception escapes to native code. Ordinary managed callers still receive OutOfMemoryException.

This ledger does not yet count allocations performed inside libraries without the supplied callbacks, marshalled strings, the managed heap, driver memory or allocator bookkeeping. In particular gzip's internal codec allocations are distinct from its tracked wrapper state. It is not either public MemoryStatic monitor and must not be added to an independent managed peak to fabricate a concurrent total. Complete composed producer instrumentation and qualification remain required below.

NativeAllocationTests passes on Linux x64 CLR and strict full-trim NativeAOT, including native allocation/reallocation failure, zero-size blocks, alignment 1–4096, four concurrent workers, an unobserved transient high-water mark and 1024 warmed alloc/resize/free/read cycles at strict 0/0 owner/all-thread managed bytes. Sixteen source-bound root profiles also pass for native allocation, file identity, networking, fonts/60 independent raster oracle cases, text, dialogs, audio, accessibility and CPU/GPU owner/separate physics. Receipts and exact payload/PDB/source hashes are under `bin/native-allocation-accounting/2026-10-28/`. The full ordinary suite also passes on that unchanged cohort in 951.475 seconds with one completion marker. Other platforms and total-memory producers remain open.

## Allocation producers remain open

The two applicable missing selectors are MEMORY_STATIC and MEMORY_STATIC_MAX.
[Performance](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/main/performance.cpp#L251-L254)
delegates to the instrumented engine allocator's current and maximum usage.
[Allocation/reallocation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/os/memory.cpp#L100-L155)
updates the peak at allocation boundaries;
[get_mem_max_usage](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/os/memory.cpp#L212-L227)
reads that high-water mark. The XML's wording about available memory does not
turn the maximum into free host RAM. Debug-only native tracking also does not
authorize a constant-zero public implementation in this engine.

Electron2D's managed/native allocation adaptation remains required by
[ADR 0016](../decisions/core-object-runtime.md#adr-0016). A complete producer
must establish its accounting domains, collection/free lifetime and peak
publication together. In particular:

- Managed heap bytes and CPU native allocations need coherent accounting;
  the existing deferred-message peak and GPU allocation snapshot are narrower
  producers. Avoid double counting native wrappers, CPU mirrors and GPU storage.
- A native allocation/reallocation failure preserves the previous contribution;
  successful frees and committed teardown reduce current usage. The peak survives
  drain, collection and runtime-session replacement.
- Allocation followed by release between two monitor reads must still contribute
  to the peak. A maximum sampled only on demand or once per frame cannot prove
  that contract. GC generation and concurrent worker transitions need coverage.
- Prepared reads must preserve the current cross-thread, no-callback,
  no-managed-allocation contract without forcing collection, opening a device or
  waiting for GPU work. Define numeric range and failure behavior explicitly.

[GC.GetTotalMemory(false)](https://learn.microsoft.com/en-us/dotnet/api/system.gc.gettotalmemory?view=net-10.0)
reports managed heap size excluding fragmentation. It supplies neither native
allocation ownership nor the process-lifetime allocation peak. Process RSS,
host available RAM and cumulative allocated bytes measure different quantities.
Do not promote either selector, GetMonitor, Monitor or the Performance class
until the complete current/peak producers and their failure/lifetime checks run.

The source manifest and bounded trace receipt are retained in
`bin/physics-monitor-audit/2026-10-27/`. This audit adds no physical, native,
platform, network or performance acceptance evidence.
