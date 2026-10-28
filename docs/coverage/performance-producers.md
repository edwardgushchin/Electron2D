# Performance producer audit

Last updated: 2026-10-29

This audit follows reference `4.7.2-stable` at
`ed1daf0bf001b61586d9930840f2f1394092c079`. It separates executable planar
diagnostics from 3D-only source categories and the remaining allocation work.
The public current/peak producers remain incomplete. The later [registered extension
replacement](../components/extensions.md) closes a different inherited row; current
physics declaration coverage after the [Resource audit](../components/resource-contract-audit.md)
is **1365/1369 (99.71%)**, four open, 92 Excluded.

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

This ledger does not yet count allocations performed inside libraries without the supplied callbacks, marshalled strings, the managed heap, driver memory or allocator bookkeeping. The StreamPeerGZIP domain now supplies its internal zlib storage as described below; BCL compression consumers retain their separate allocator boundary. It is not either public MemoryStatic monitor and must not be added to an independent managed peak to fabricate a concurrent total. Complete composed producer instrumentation and qualification remain required below.

NativeAllocationTests passes on Linux x64 CLR and strict full-trim NativeAOT, including native allocation/reallocation failure, zero-size blocks, alignment 1–4096, four concurrent workers, an unobserved transient high-water mark and 1024 warmed alloc/resize/free/read cycles at strict 0/0 owner/all-thread managed bytes. Sixteen source-bound root profiles also pass for native allocation, file identity, networking, fonts/60 independent raster oracle cases, text, dialogs, audio, accessibility and CPU/GPU owner/separate physics. Receipts and exact payload/PDB/source hashes are under `bin/native-allocation-accounting/2026-10-28/`. The full ordinary suite also passes on that unchanged cohort in 951.475 seconds with one completion marker. Other platforms and total-memory producers remain open.

## FreeType-owned allocation domain

Native outline faces now create an FT_Memory record with the same owned payload allocator and supply it to FT_New_Library. The record outlives every face, driver and library allocation; FT_Done_Library completes before the record is freed. Default modules and FT_Set_Default_Properties retain the previous initialization and FREETYPE_PROPERTIES behavior. Bitmap-only faces create no FreeType library. Callback signatures use CLong for native C long across supported ABI layouts. Native allocation failure returns null, and failed resize preserves the preceding block and contribution.

This follows the [documented custom-memory library lifetime](https://freetype.org/freetype2/docs/reference/ft2-module_management.html#ft_new_library) and [memory callback protocol](https://freetype.org/freetype2/docs/reference/ft2-system_interface.html#ft_memoryrec). Face/metadata/glyph/raster allocations routed through FT_Memory now contribute alongside copied input/table buffers, without counting the wrappers twice. The rebuilt private WOFF2 producer also routes its Brotli decoder through this allocator, as qualified [below](#woff2-decoder-allocation-domain-2026-10-28). HarfBuzz, ICU and other independent native allocators remain separate domains. This does not complete either public total-memory selector.

The earlier **legacy 2.13.3 override qualification** used NativeFontAllocationTests to verify actual production face ownership, explicit/failed/finalizer cleanup, native callbacks and fail-at-every-request initialization/decoding/unhinted rendering. Its prepared shaping interval measures managed allocations; new raster image creation remains a cold allocating operation. Linux x64 CLR and strict full-trim NativeAOT pass all 1162 unhinted allocation-failure positions and 128 warmed shaping calls at strict 0/0 owner/all-thread managed bytes. Source-bound raster/cache/metadata/variation/paragraph checks pass, including 60 independent packaged-library raster oracle cases both with default properties and with truetype:interpreter-version=35. Real-window GPU and compatibility text checks pass; CPU/GPU owner/separate role profiles retain 512 zero-allocation full frames. The GPU rerun uses the current-main native render bridge; earlier stale-bridge failures are retained separately. Receipts, the native C reproduction, immutable cohort hashes and matching runtime/test/consumer PDB/resource proofs are under bin/freetype-allocation-accounting/2026-10-28/. The full ordinary suite and other platforms were not rerun for this slice. Native package export validation now also requires the four custom-library initialization/teardown entry points.

### Hinted allocation-failure guard (2026-10-28)

The earlier Linux 2.13.3 reproduction is valid for the legacy source revision
`42608f77f20749dd6ddc9e0536788eaad70ea4b5`: request 1158 of 1179 returned a
null bytecode context that was dereferenced. Its backport checks, GDB stack,
CLR/AOT receipts and libraries remain historical evidence under
`bin/freetype-hinted-oom/2026-10-28/`.

**Provenance correction:** those builds used explicit source overrides, not the
recipe's declared pin `0a0221a1347e2f1e07c395263540026e9a0aa7c7`. That pin is
FreeType **2.14.3** and already checks TT_New_Context before publishing it.
The obsolete backport helper has been removed. The legacy 2.13.3 evidence does
not qualify the pinned producer; the current qualification follows below.

### Source revision qualification (2026-10-28)

All five earlier local overrides differed from the declared recipe revisions.
Their FreeType/HarfBuzz versions were 2.13.3/5.2.0. The producer now rejects an
incorrect Git HEAD or changed tracked source for every dependency. Supplied
source overrides are checked before their CMakeLists.txt can execute; fetched
sources are checked after population. Successful configuration records each
exact revision. The self-contained regression checks clean, changed,
wrong-revision and unversioned
inputs; the native workflow runs it before selecting its producer matrix.

The actual pinned closure is FreeType **2.14.3**, HarfBuzz **14.5.1**, zlib
**1.3.2**, libpng **1.6.59** and Brotli **1.2.0**. Its existing recipe revisions
are unchanged. FreeType's default runtime HarfBuzz lookup is explicitly disabled;
the supplied static target is used instead. A runtime version query confirms
2.14.3 and embedded HarfBuzz 14.5.1, and the ELF dependency list has no dynamic
HarfBuzz. The bytecode null-context check comes from that pinned upstream source.

The WOFF2 generated unit now checks the actual 2.14.3 source hash. Native failure
matrices pass **1172/1180** unhinted/hinted positions with zero retained blocks,
two same-face context retries, decoder refusal reported as Out_Of_Memory and
corrupt-stream cleanup, under default properties and interpreter 35. The raster
oracle compares its version with the actual engine-owned FreeType library before
checking every pixel; it no longer assumes a legacy package version.

Linux CLR and full-trim NativeAOT pass these matrices, explicit/error/finalizer
cleanup and 128 warmed shaping calls at strict 0/0 owner/all-thread managed bytes.
The root consumer passes 120 independent raster comparisons across default and
interpreter-35 properties and its raster-policy suite. A separate 60-case run on
the legacy 2.13.3 library also passes the version-aware oracle. Cached packages,
other platforms and complete ordinary-suite acceptance remain separate gates.

Commands, exact revision receipts, the original configuration failure and current
qualification are under `bin/font-source-provenance/2026-10-28/`. Qualified library
SHA-256: `bf0006ee1e333aa90537282b937af6be7965721b98064283dedae7c702e8b836`.

### WOFF2 decoder allocation domain (2026-10-28)

The unmodified pinned WOFF2 reader used BrotliDecoderDecompress with default
allocation callbacks. Its temporary decoder and scratch storage bypassed
FT_Memory even when the face supplied one. The private producer now creates a
decoder with that face's allocation/free callbacks and performs the same single
streaming decode. Every exit destroys the decoder before its stack-owned callback
context expires. Callback allocation failure returns Out_Of_Memory; oversized
size_t requests reject before conversion to C long. Invalid/incomplete streams
and decoded-length mismatches retain Invalid_Table. Trailing-input behavior and
normal decoded bytes follow the preceding single-shot implementation.

`tools/font-native/freetype-woff2-memory.cmake` generates the SFNT aggregation
unit in the build directory, leaving the dependency checkout unchanged. Its
normalized-source SHA-256 and compilation-unit checks reject a changed pin.
The macOS incremental source target includes this helper. No native export,
dependency pin or public managed API changes.

The target-native checker calibrates the opaque Brotli decoder-state allocation
through its public callback API, then observes exactly one such allocation in
the actual WOFF2 face's supplied domain. The preceding qualified library fails
this check with zero observations. The legacy 2.13.3 override passes all **1172/1189**
unhinted/hinted allocation-failure positions with zero retained blocks, including
Out_Of_Memory from decoder-state refusal, same-face bytecode-context recovery and
cleanup after a corrupt Brotli stream inside a valid WOFF2 directory. Both default
properties and interpreter 35 pass. The production CLR and full-trim NativeAOT allocator routes pass
the same matrices, explicit/error/finalizer lifetime and 128 warmed shaping calls
with strict **0/0 owner/all-thread managed bytes**. The AOT process maps the
qualified FreeType and no CoreCLR. A fresh ordinary root consumer also passes
its allocator route, 60 independent raster-oracle cases under default properties,
60 under interpreter 35, and the raster-policy suite. Both consumer cohorts stay
unchanged across their checks.

Legacy override library SHA-256:
`58cbe1a5fa253466a2da64a5a8ae45f4955cac6fe57bcaed2b38dc8d7b60d2ba`.
Commands, RED result, source-drift rejection, hashes and receipts are under
`bin/freetype-woff2-memory/2026-10-28/`.

Those receipts qualify the legacy override, not the declared source pins or cached
packages. The corrected pinned-producer qualification is recorded above. Neither
set of checks qualifies all native heap domains. HarfBuzz/ICU/other codecs and coherent managed/native current/peak remain
open. Physics declaration coverage remains **1365/1369 (99.71%)**, four Partial
and 92 Excluded.

### FreeType auto-hinter HarfBuzz allocation domain (2026-10-28)

The pinned HarfBuzz target now uses its four compile-time allocator hooks. The
private FreeType bridge registers the process-lifetime NativeAllocation
allocate/resize/free callbacks before the first outline or bitmap font owner.
Registration is published atomically, accepts an identical repeat and rejects a
different owner. Before registration, allocation fails without a libc fallback.
Zeroed requests check multiplication overflow; failed resize preserves its old
payload, while zero-size resize releases it. Native out-of-memory returns null
through the C ABI rather than throwing across it.

Face-owned HarfBuzz storage drains with the FreeType face. Global HarfBuzz caches
remain counted for the process lifetime; HB_NO_ATEXIT prevents a late native
destructor from invoking managed callbacks after CLR teardown. These caches are
not attributed to an individual face or subtracted on session retirement.

Linux now builds and packages libElectron2DFreeType.so from the existing private
recipe, like the other native producers, and removes the old MonoGame dependency.
Source builds and explicit Electron2DFontNativeLibrary overrides preserve the
runtime RID directory. Export audits require e2d_ft_hb_memory; older bundles reject
before accepting a font owner. The independent desktop HarfBuzzSharp shaping
allocator and coherent managed/native total remain open. This adds no public API
or declaration coverage.

The Linux native check exercises actual forced automatic hinting, all 853 observed
HarfBuzz refusal positions, resize preservation, calloc overflow/zeroing and 512
automatic-hinter face lifetimes on four threads. Each refusal drains its face
storage. Prepared global caches retain 434 payload bytes; the peak survives face
teardown.
Linux CLR and strict full-trim NativeAOT pass production lifetime, failure and
finalizer checks, forced automatic hinting and 128 warmed shaping calls at 0/0
owner/all-thread managed bytes. One unchanged final cohort also passes the font
family, 528 independent raster/MSDF comparisons and GPU/Compatibility windows,
each with 64 active and 64 idle 0/0 intervals. An old uninstrumented private library
fails before owner initialization. The actual Linux MSBuild font-source target
passes its decoder and concurrent allocator checks without changing the qualified
library. These are bounded font/allocator checks, not physical throughput or FPS.

Qualified Linux x64 library SHA-256:
`aeca770f6208f8ccb79ef48dce1265baf24aef92498934c5a1ba24faa90e91e1`.
Receipts are under `bin/harfbuzz-allocation-accounting/2026-10-28/`; complete bundle,
foreign-platform and full-goal qualification remain separate.

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

## Streaming GZIP allocation domain

StreamPeerGZIP now uses the existing private bridge's opaque streaming gzip/zlib context instead of the runtime compression PAL. Context and all zlib allocation/free callbacks share NativeAllocation with ENet and file block codecs. Requested payload bytes update coherent current/high-water counters at their actual native boundaries, including lazy decoder storage and short-lived failed initialization. SafeHandle disposal/finalization drains owned storage; no managed heap or GPU bytes enter this producer. These four private exports are required by native packaging and publish checks on every profile; no new public declaration or native dependency is added.

The preceding runtime reports only 40 tracked bytes for a prepared compressor in the retained RED control. The replacement must count the actual codec storage and return to baseline on retirement. The native production-bridge oracle verifies independent stock-zlib wire compatibility, output backpressure, borrowed-pointer retirement and injected failure at every allocation position for gzip/zlib compression/decompression, alongside existing ENet/file checks. Linux x64 CLR and strict full-trim NativeAOT pass normal and calibrated native-probe checks. The prepared public control now observes 350456 tracked bytes and complete drain, rather than the preceding 40-byte outer record. Both profiled hosts balance 131/131 compressor and 258/258 decoder failure-window allocations/frees, with no reallocations/failures; these are process-wide call observations, not requested-byte totals. The exact production oracle passes 60 injected allocation positions across all ENet/file/streaming modes, including eight positions in the four streaming gzip/zlib roles. HTTP/HTTPS, all five file codecs, ordinary native accounting, ENet accounting and desktop export preflight pass. Four CPU/no-device or NVIDIA GPU owner/separate scene profiles retain 512 measured complete physical frames at strict 0/0 managed bytes. An old-bundle negative control rejects the missing GZIP export. The NativeAOT maps contain the bridge and no CoreCLR. Commands, payload/source/PDB identities and receipts are retained under `bin/gzip-allocation-accounting/2026-10-28/`. This domain alone does not implement either public composed memory selector. BCL file/archive compression, other native libraries and exact coherent CLR/NativeAOT allocation/GC aggregation remain open.

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
complete engine. At the time of this probe, GetMonitor and both memory selectors
stayed open, with physics declaration coverage **1364/1369 (99.63%)**, five
Partial and 92 Excluded. The current declaration count is reported above.

The accepted requirement remains an allocation/free/GC producer with coherent
aggregation and startup/lifetime/error qualification on each supported host.
The owner explicitly retained the exact current/peak contract on 2026-10-28,
including short-lived peaks between monitor reads. The bounded observed-peak
C# adaptation is rejected; the local `contract-choice.txt` retains the comparison
and this decision. The existing ADR remains unchanged. Coverage stays Partial
until the exact producer is implemented and qualified.

## Runtime allocation hook boundary

The [allocation-boundary probe](../../tools/physics/allocation-boundary-probe/README.md)
compares the existing calibrated CLR native profiler with a separate published
NativeAOT executable on matching .NET 10.0.1/Linux x64 with EventSourceSupport enabled. Its own linker wrappers
count entries to RhpNewFast and RhpGcAlloc without changing the SDK archives or
engine runtime. The profiler client loads the exact configured library instance;
a copy at a different path can have uninitialized independent state. The checker
also pins child apphosts to the selected CLI's runtime root: the first automatic
apphost selection used CLR 10.0.12 and was retained as a separate checkpoint.

| Retained witness | CLR objects / owner bytes | NativeAOT fast / slow helper entries |
| --- | ---: | ---: |
| 512 ordinary objects | 512 / 12288 | 512 / 1 |
| 64 strings of 128 characters | 64 / 17920 | 0 / 2 |
| One 131072-byte pinned array | 1 / 131096 | 0 / 0 |

The calibrated CLR ObjectAllocated callback observes all 577 witness objects.
The string raw-size sum is 17792 bytes; eight-byte alignment gives the observed
17920 owner bytes. Summing raw profiler sizes directly therefore loses 128 bytes
in this bounded case. Object and array totals already have that alignment.
The reused Info2 recorder queries a [32-bit object size](https://learn.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/icorprofilerinfo-getobjectsize-method); these witnesses do not
qualify large-object sizing. A complete producer needs a qualified [64-bit size
path](https://learn.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/icorprofilerinfo4-getobjectsize2-method) as well as collection/free accounting.
Disabled CLR profiling and the same configured NativeAOT profiler both reject an
uninitialized trace bridge with exit 132. NativeAOT maps contain no CoreCLR; no
CLR trace initialization file appears. This qualifies that tested bridge's host
boundary, rather than every possible NativeAOT instrumentation mechanism.

The pinned [fast helper](https://github.com/dotnet/runtime/blob/v10.0.1/src/coreclr/runtime/amd64/AllocFast.S)
updates the thread allocation pointer directly, with a distinct string/array
path. The pinned [GC helpers](https://github.com/dotnet/runtime/blob/v10.0.1/src/coreclr/nativeaot/Runtime/GCHelpers.cpp#L658-L717)
also route direct array/object requests into GcAllocInternal independently of
RhpGcAlloc. A hook on that slow export alone misses both ordinary fast allocation
and the pinned-array witness. The observed entries count attempts, not committed
allocations or surviving heap bytes. Collection/free lifetime, startup, failure
and cross-thread aggregation remain requirements of a complete producer.

Commands, calibrated traces, raw/aligned totals, process maps, source/native
identities and the matched-host receipt are retained in
`bin/runtime-memory-boundary/2026-10-28/final-qualified/`. These results
select the next implementation boundary: successful allocation publication must
cover the fast helpers and common allocator, with GC retirement and the shared
native ledger composed at actual producer transitions. No sampled peak,
constant monitor or partial shipping hook is introduced. GetMonitor and both
memory selectors remain Partial; physics declaration coverage remains
**1365/1369 (99.71%)**, four Partial and 92 Excluded. This diagnostic adds no
physical, network, foreign-platform or GPU/FPS acceptance.


## CLR dynamic-heap lifetime producer

The [managed live probe](../../tools/physics/managed-live-probe/README.md) now
executes successful allocation plus GC retirement on Linux x64 / CoreCLR 10.0.1.
It is a separate native profiler and public-CLR consumer, not a production monitor.
Its pinned Info4 size and Callback4 relocation/survivor slots are checked against
v10.0.1 corprof.idl before execution. Pointer-sized sizes/ranges replace the older
32-bit recorder path for this experiment. The [CLR implementation](https://github.com/dotnet/runtime/blob/v10.0.1/src/coreclr/vm/proftoeeinterfaceimpl.cpp#L1676-L1730)
queries successful object sizes without triggering GC. Objects retain their old
map keys until every relocation range is applied; dead objects retire only from
collected generations, in one native bookkeeping critical section at GC finish.

Workstation GC and two-heap server GC passed the same 25 numeric witness checks:

| Witness | Allocated aligned bytes | Verified lifetime |
| --- | ---: | --- |
| 1024 ordinary objects | 24576 | Half retired in gen 0, half retained through gen 1/2, then reclaimed |
| 65 strings of 128 characters | 18200 | Raw 18070; aligned bytes reclaimed after roots are dropped |
| Pinned 131072-byte array | 131096 | Dead payload retained across gen 0/1, reclaimed by gen 2 |
| Ordinary 262144-byte array | 262168 | Dead large object retained across gen 0/1, reclaimed by gen 2 |
| Eight finalizable objects | 192 | Queued objects survive GC, then retire after finalization and another GC |
| 512 temporary objects | 12288 | First read after GC reports current zero and peak 12288 |
| Two retained worker cohorts | 98304 each | Survive relocation while rooted, then both retire completely |
| Two workers overlapping induced GC/read intervals | 786432 each | All 32768 allocations per worker commit and retire |

Twenty thousand prepared native snapshot reads allocate zero managed bytes on
the caller. Profiling-disabled, invalid-tag and invalid-snapshot children reject
with exit 132. The checker retains native/CLR maps, raw child logs and source,
assembly, library and pinned-IDL hashes under
`bin/runtime-memory-producer/2026-10-29/qualified/`. This is measured callback-ledger
behavior; the same results do not establish complete process ownership or GPU/FPS
performance.

The [CLR GC callback implementation](https://github.com/dotnet/runtime/blob/v10.0.1/src/coreclr/vm/eetoprofinterfaceimpl.cpp#L5726-L5799)
requires non-concurrent GC in this profiling mode. MONITOR_GC therefore changes
the host collector, which must be included in any future performance comparison.
A tight forced-GC stress loop starved mutators and reached its 90-second deadline;
the final workload leaves a 1 ms mutator interval while preserving the full
allocation count and forced collections overlapping both workers.

The initial run also exposed non-GC frozen objects: GetObjectGeneration returns
CORPROF_E_NOT_GC_OBJECT (0x80131389). This experiment excludes and counts those
objects explicitly, and rejects frozen tagged witnesses. Their actual lifetime,
image objects, NativeAOT, over-4-GiB sizing, failed-allocation behavior and the
production managed/native shared commit boundary remain unqualified. In
particular, callback-ledger peaks cannot establish an exact simultaneous native
and managed peak if publication is delayed after an allocator commit. Do not
wire these diagnostic snapshots to GetMonitor or treat separate domain peaks as
a composition. The full exact contract remains accepted; physics coverage stays
**1365/1369 (99.71%)**, four Partial and 92 Excluded.
