# Performance producer audit

Last updated: 2026-10-28

This audit follows reference `4.7.2-stable` at
`ed1daf0bf001b61586d9930840f2f1394092c079`. It separates executable planar
diagnostics from 3D-only source categories and the remaining allocation work.
The public current/peak producers remain incomplete. The later [registered extension
replacement](../components/extensions.md) closes a different inherited row; current
physics declaration coverage after the [Node audit](../components/node-contract-audit.md)
is **1364/1369 (99.63%)**, five open, 92 Excluded.

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

Current consumers include [ENet hosts/packets/codecs and file blocks](#enet-owned-allocation-domain), FAudio's engine/effect allocator callbacks and streaming callback storage, text font/data/table/bitmap callbacks, retained ICU data, accessibility callbacks and temporary byte buffers, DTLS cookies, gzip native wrapper state, filesystem stat buffers and native message-button records. Borrowed native pointers keep their original release functions and never enter this allocator. FAudio allocation failures return null across the unmanaged callback boundary; no managed out-of-memory exception escapes to native code. Ordinary managed callers still receive OutOfMemoryException.

This ledger does not yet count allocations performed inside libraries without the supplied callbacks, marshalled strings, the managed heap, driver memory or allocator bookkeeping. In particular gzip's internal codec allocations are distinct from its tracked wrapper state. It is not either public MemoryStatic monitor and must not be added to an independent managed peak to fabricate a concurrent total. Complete composed producer instrumentation and qualification remain required below.

NativeAllocationTests passes on Linux x64 CLR and strict full-trim NativeAOT, including native allocation/reallocation failure, zero-size blocks, alignment 1–4096, four concurrent workers, an unobserved transient high-water mark and 1024 warmed alloc/resize/free/read cycles at strict 0/0 owner/all-thread managed bytes. Sixteen source-bound root profiles also pass for native allocation, file identity, networking, fonts/60 independent raster oracle cases, text, dialogs, audio, accessibility and CPU/GPU owner/separate physics. Receipts and exact payload/PDB/source hashes are under `bin/native-allocation-accounting/2026-10-28/`. The full ordinary suite also passes on that unchanged cohort in 951.475 seconds with one completion marker. Other platforms and total-memory producers remain open.

## FreeType-owned allocation domain

Native outline faces now create an FT_Memory record with the same owned payload allocator and supply it to FT_New_Library. The record outlives every face, driver and library allocation; FT_Done_Library completes before the record is freed. Default modules and FT_Set_Default_Properties retain the previous initialization and FREETYPE_PROPERTIES behavior. Bitmap-only faces create no FreeType library. Callback signatures use CLong for native C long across supported ABI layouts. Native allocation failure returns null, and failed resize preserves the preceding block and contribution.

This follows the [documented custom-memory library lifetime](https://freetype.org/freetype2/docs/reference/ft2-module_management.html#ft_new_library) and [memory callback protocol](https://freetype.org/freetype2/docs/reference/ft2-system_interface.html#ft_memoryrec). Face/metadata/glyph/raster allocations routed through FT_Memory now contribute alongside copied input/table buffers, without counting the wrappers twice. HarfBuzz, ICU and codecs with independent allocators remain separate domains: the pinned [WOFF2 decoder](https://github.com/freetype/freetype/blob/0a0221a1347e2f1e07c395263540026e9a0aa7c7/src/sfnt/sfwoff2.c#L286-L307) calls Brotli's single-shot decoder without this allocator. This change does not complete either public total-memory selector.

NativeFontAllocationTests verifies actual production face ownership, explicit/failed/finalizer cleanup, native callbacks and fail-at-every-request initialization/decoding/unhinted rendering. Its prepared shaping interval measures managed allocations; new raster image creation remains a cold allocating operation. Linux x64 CLR and strict full-trim NativeAOT pass all 1162 unhinted allocation-failure positions and 128 warmed shaping calls at strict 0/0 owner/all-thread managed bytes. Source-bound raster/cache/metadata/variation/paragraph checks pass, including 60 independent packaged-library raster oracle cases both with default properties and with truetype:interpreter-version=35. Real-window GPU and compatibility text checks pass; CPU/GPU owner/separate role profiles retain 512 zero-allocation full frames. The GPU rerun uses the current-main native render bridge; earlier stale-bridge failures are retained separately. Receipts, the native C reproduction, immutable cohort hashes and matching runtime/test/consumer PDB/resource proofs are under bin/freetype-allocation-accounting/2026-10-28/. The full ordinary suite and other platforms were not rerun for this slice. Native package export validation now also requires the four custom-library initialization/teardown entry points.

### Hinted allocation-failure guard (2026-10-28)

The preceding packaged Linux 2.13.3 trial crashed at request 1158 of 1179.
A fresh Debug build of the pinned producer reproduces the same native C failure:
TT_New_Context returns null and tt_size_init_bytecode passes it to
TT_Load_Context, which dereferences exec->memory. This is independent of the
managed allocator callback ABI.

The private producer now backports the context-allocation check, matching the
[upstream guard](https://github.com/freetype/freetype/blob/3f6dad8e4f82c4f9c7cf9ffbf82972b912c08e48/src/truetype/ttobjs.c#L1036).
The source pin remains `0a0221a1347e2f1e07c395263540026e9a0aa7c7` (2.13.3).
`tools/font-native/freetype-context-oom.cmake` generates a marked TrueType unit
in the build directory; the dependency checkout stays unchanged. The exact
initializer and compilation-unit checks fail configuration if the pinned layout
changes. The macOS incremental source target includes this helper among its
inputs. No new native export or public managed API is introduced.

The target-native Electron2DFreeTypeCheck now injects failure at every allocation
for both unhinted and hinted rendering, checks zero retained callback blocks and
retries the same face after a failed bytecode-context allocation. Debug and
Release pass all 1162/1179 positions. Release also passes both default properties
and truetype:interpreter-version=35, including two successful context retries
per run. The managed NativeFontAllocationTests now executes both complete
failure matrices, production/finalizer cleanup and 128 warmed shaping calls at
strict 0/0 owner/all-thread managed bytes on Linux CLR and full-trim NativeAOT.
The AOT process maps the qualified FreeType and no CoreCLR.

The qualified producer library SHA-256 is
`8de7fa02d02599eebf027d51e22eb0c58861dfe07836b118cf0dc16dc3ef592c`.
The root consumer also passes its allocation route, 60 independent raster-oracle
cases under default properties and 60 under interpreter 35, plus the native
raster-policy suite. Receipts, hashes, original failing GDB stack and commands
are under `bin/freetype-hinted-oom/2026-10-28/`.

This qualifies the rebuilt library and explicit local consumer cohorts. Existing
published/cached native packages must be rebuilt before they acquire the fix;
their files were not replaced by these checks. Foreign targets, complete ordinary
suite, independent allocation domains and coherent managed/native current/peak
producers remain open. Physics declaration coverage stays **1365/1369 (99.71%)**,
four Partial and 92 Excluded; neither memory selector is promoted by this fix.

The focused FastTests route requires explicit native qualification: `dotnet run --project tests/Electron2D.FastTests -c Release -r linux-x64 -p:Electron2DConsumerPlatform=Linux -- --freetype-allocation`. The existing consumer platform property admits the standard native package for that run; ordinary managed fast builds keep their existing dependency boundary.

## ENet-owned allocation domain

The private ENet bridge now installs the process-lifetime NativeAllocation callbacks
before any host or file-block allocation. Host/peer/packet storage and bridge
budgets/codecs share the supplied allocator. zlib uses zalloc/zfree; Zstandard
uses advanced contexts with custom memory, including the transient file-block
contexts. FastLZ retains its stack/caller buffers. No vendor source, wire format,
public API or external library dependency changes.

Registration is idempotent for the same callbacks and rejects a different owner;
live pointers cannot be switched to another deallocator. Managed out-of-memory
returns null across the native callback boundary. ENet's no-memory callback
returns control to its checked failure paths, preserving cleanup rather than
aborting. The new private e2d_enet_memory export is required in the matching
native bundle; older bridges reject before socket/host allocation. Package export
validation covers the added entry point on every target. Native file-codec setup
installs memory callbacks without initializing a socket transport.

The production-bridge C oracle checks all 52 allocation-failure positions in its
host/peer/packet, five-codec and file-block scenarios, each returning to zero live
payload. AddressSanitizer/UBSan and leak detection pass the same scenarios. The
independent file oracle retains 32 bidirectional FastLZ/Zstandard comparisons;
the C oracle also decodes tracked zlib output through the standard entry point
and decodes standard output through the tracked context.
Fresh-process ENetAllocationTests checks actual managed/native registration,
transient file peaks, fragmented wire data with all five codecs, four concurrent
host workers, SafeHandle finalization and 128 warmed service/accounting intervals
per codec at zero owner/all-thread managed bytes. Linux x64 CLR and strict full-trim
NativeAOT both pass; the ELF control maps the exact bridge and no CoreCLR.
The 600-tick separate-process scenario also passes with a CPU/no-GPU authority,
GPU prediction, late CPU join, impairment, ownership/lifecycle and confirmed events;
its prepared step/replay allocation intervals remain zero. Existing ENet IPv4/IPv6, DTLS,
loss/replication, file-compression and byte-buffer regressions also pass.

Receipts and source/cohort manifests are under
`bin/enet-allocation-accounting/2026-10-28/`. This closes the ENet bridge allocation
source only. Runtime compression PAL/BCL codecs, other uninstrumented native
libraries, marshalled strings and coherent managed/native current/peak aggregation
remain open. The complete MEMORY_STATIC/MEMORY_STATIC_MAX producers and Performance.GetMonitor
remain open; physics remains 1364/1369 (99.63%), five Partial
and 92 Excluded. Other platforms and final whole-goal acceptance remain separate.

## Allocation producers remain open

The [compression initialization cleanup](../components/http.md#failed-codec-initialization)
now retires the runtime's partially initialized native record before the engine's
tracked outer PAL record. The calibrated allocator regression reproduces the
preceding leak and observes balanced allocation/free calls after the fix. It
neither instruments codec-owned bytes nor completes the managed/native total;
both memory selectors and the current five open physics declarations stay open.

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

## Managed heap source qualification

The standalone [managed-memory probe](../../tools/physics/managed-memory-probe/ManagedMemoryProbe.csproj)
checks the stock .NET 10.0.1 APIs on Linux x64 CLR and strict full-trim NativeAOT.
It uses workstation, nonconcurrent GC and explicitly enables EventSource support.
Forced collections belong only to this diagnostic counterexample; they are not
an accepted implementation of GetMonitor. No Electron2D runtime code changes.

| Observation | CLR | NativeAOT |
| --- | ---: | ---: |
| Owner managed bytes for 1024 warmed GetGCMemoryInfo calls | 294912 | 294912 |
| Owner managed bytes for 1024 warmed GetTotalMemory(false) calls | 0 | 0 |
| Observed live managed bytes with a temporary 64 MiB array | 67167368 | 67150800 |
| Current managed bytes after release and subsequent collections | 58480 | 41912 |
| Maximum of the ordinary before/after observations | 58480 | 41912 |
| Latest collection's sum of generation SizeBeforeBytes | 58936 | 41912 |
| GC heap-stat callbacks after eight explicit collections | 8 | 8 |
| Callback thread / caller thread | 3 / 1 | 2 / 1 |

Both snapshots lose the known temporary peak. GCMemoryInfo is additionally an
allocating query: the [pinned CLR implementation](https://github.com/dotnet/runtime/blob/v10.0.1/src/coreclr/System.Private.CoreLib/src/System/GC.CoreCLR.cs#L64-L76)
creates a fresh data object per call. Its generation fields describe the
[selected most recent collection](https://github.com/dotnet/runtime/blob/v10.0.1/src/libraries/System.Private.CoreLib/src/System/GCMemoryInfo.cs),
not a retained maximum over every intervening collection. Cumulative allocated
bytes cannot supply current live storage or a concurrent native/managed peak.

Runtime GC events do execute in both checked hosts; they are not an absent
NativeAOT prerequisite here. Their callbacks run away from the caller, and the
[native-event forwarding implementation](https://github.com/dotnet/runtime/blob/v10.0.1/src/libraries/System.Private.CoreLib/src/System/Diagnostics/Tracing/NativeRuntimeEventSource.cs#L47-L70)
allocates decoded payload objects before dispatch. The documented
[AllocationTick](https://learn.microsoft.com/en-us/dotnet/fundamentals/diagnostics/runtime-garbage-collection-events#gcallocationtick_v3-event)
is a sampled event around each 100 KB, not an exact object-allocation boundary.
Sampling a later native ledger from such a callback does not establish that its
value coexisted with the managed sample. Event counts and this probe do not prove
that every possible instrumented runtime approach is infeasible.

`qualified-comparison.json`, `qualified-aot-proof.json`, process maps and source/build hashes retain
the exact runs. The NativeAOT executable maps no CoreCLR. These receipts reject
the tested snapshot shortcuts; they do not implement a producer or qualify the
complete engine. GetMonitor and both memory selectors stay open, with physics
declaration coverage **1364/1369 (99.63%)**, five Partial and 92 Excluded.

The accepted requirement remains an allocation/free/GC producer with coherent
aggregation and startup/lifetime/error qualification on each supported host.
A bounded observed-peak C# adaptation is a separate pending user decision,
described in the local `contract-choice.txt`; no ADR or coverage status changes
until that decision and its executable implementation are complete.
