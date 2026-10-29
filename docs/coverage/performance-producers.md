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

The internal NativeAllocation ledger replaces direct NativeMemory and HGlobal payload allocation in the managed runtime. Its pure-C core lives in the existing private ENet/file-codec bundle; both the engine and source-instrumented NativeAOT producer call this same allocation/resize/free/snapshot ABI and native gate. Loading it opens no networking or device. Managed-side preparation checks ABI 1 before supplying callbacks; missing/mismatched bundles fail before entering an unmanaged allocator callback. It owns the raw pointer and requested byte size in a private prefix, preserves ordinary fundamental or requested power-of-two alignment, and serializes successful allocation/resize/free publication with coherent current/peak reads. The peak updates at each successful boundary and survives frees; failed requests retain both the old block and counters. Requested payload bytes exclude allocator prefixes/padding. Zero-byte blocks remain releasable with a zero payload contribution. This is CPU native storage; GPU memory and managed wrapper objects are not counted again.

Current consumers include [ENet hosts/packets/codecs and file blocks](#enet-owned-allocation-domain), FAudio's engine/effect allocator callbacks and streaming callback storage, text font/data/table/bitmap callbacks, retained ICU data, accessibility callbacks and temporary byte buffers, DTLS cookies, gzip native wrapper state, filesystem stat buffers and native message-button records. Borrowed native pointers keep their original release functions and never enter this allocator. FAudio allocation failures return null across the unmanaged callback boundary; no managed out-of-memory exception escapes to native code. Ordinary managed callers still receive OutOfMemoryException.

This ledger does not yet count allocations performed inside libraries without the supplied callbacks, marshalled strings, the managed heap, driver memory or allocator bookkeeping. The StreamPeerGZIP domain now supplies its internal zlib storage as described below; BCL compression consumers use a separate runtime boundary, qualified only in the [owned source-runtime profile](#bcl-compression-source-runtime-domain-2026-10-29). It is not either public MemoryStatic monitor and must not be added to an independent managed peak to fabricate a concurrent total. Complete composed producer instrumentation and qualification remain required below.

Before the shared-core extraction, NativeAllocationTests passed on Linux x64 CLR and strict full-trim NativeAOT, including native allocation/reallocation failure, zero-size blocks, alignment 1–4096, four concurrent workers, an unobserved transient high-water mark and 1024 warmed alloc/resize/free/read cycles at strict 0/0 owner/all-thread managed bytes. Sixteen source-bound root profiles also pass for native allocation, file identity, networking, fonts/60 independent raster oracle cases, text, dialogs, audio, accessibility and CPU/GPU owner/separate physics. Receipts and exact payload/PDB/source hashes are under `bin/native-allocation-accounting/2026-10-28/`. The full ordinary suite also passed on that unchanged cohort in 951.475 seconds with one completion marker. Other platforms and total-memory producers remain open.

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
source only. Runtime compression PAL/BCL allocation now has a separate
[qualified source-runtime profile](#bcl-compression-source-runtime-domain-2026-10-29);
stock runtime delivery, other uninstrumented native libraries, marshalled strings
and coherent managed/native current/peak aggregation remain open. The complete
MEMORY_STATIC/MEMORY_STATIC_MAX producers and Performance.GetMonitor remain open;
current declaration totals are recorded at the top of this audit. Other platforms
and final whole-goal acceptance remain separate.

## Private ICU allocation domain (2026-10-29)

The private text bridge installs ICU memory callbacks before locale or data
initialization. They use the same NativeAllocation ledger as direct storage,
FreeType/HarfBuzz, FAudio and codecs. Requested iterator, dictionary,
normalization and security-checker payloads now contribute beside the already
accounted aligned Unicode database. Allocation failure returns null across the
managed callback boundary; failed resize retains its preceding contribution.
The callback set is immutable for the process lifetime, including retained ICU
caches. Missing private registration exports reject before native initialization.
This follows the [ICU memory callback contract](https://unicode-org.github.io/icu-docs/apidoc/released/icu4c/uclean_8h.html).

The pinned upstream files remain unchanged. ICUMemory.cmake generates nine
bounded source copies for failure handling: fallible locale/cache preparation,
nonadvancing dictionary traversal, CJK vector/hash errors, compiled-rule adoption
and reference counts, partial resource cleanup, stack resource copies and
security-data/set ownership. The iterator bridge reports a failed traversal
separately from ordinary end-of-input; managed callers propagate the error and
retire the active iterator. These corrections preserve the ordinary algorithm.

The native oracle executes actual character/word/line iterators, dictionary
traversal, titlecase and security skeletons, then ICU teardown. Every one of the
963 observed allocation/reallocation positions is rejected in a fresh process;
all retained payloads drain. The normal peak is 293141 bytes with 214268 bytes
retained before teardown, independent of the separately borrowed database.
Earlier controls exposed infinite retry, null dereferences and partial-owner
leaks; their failed receipts are retained with the final passing matrix under
`bin/icu-allocation-accounting/2026-10-29/`.

The public TextServer consumer and root check exercise database initialization,
dictionaries, scalar ends, locale casing, security, repeated temporary work,
concurrent calls and 20000 prepared value/snapshot cycles. Ordinary Linux x64 CLR and
strict full-trim NativeAOT each observe 4815946 initialization bytes, including
4811936 database bytes, with zero owner/all-thread managed bytes in the prepared
interval. CPU without a device and NVIDIA GPU pass owner/separate physical
regressions; current GPU/compatibility windows and fresh children retain actual
Unicode output. Six DLL/PDB cohorts match 9392 authored documents. Exact identities
and native-selection receipts remain with the checks.
This domain does not complete either public composed-memory selector. Other
native libraries, concurrent managed collection, module/unload lifetime, runtime
delivery, performance and foreign-platform qualification remain open. Physics declaration coverage
stays **1365/1369 (99.71%)**, four Partial and 92 Excluded.

## PCRE2-owned allocation domain (2026-10-29)

RegEx compilation now prepares the existing immutable text allocation callbacks
without initializing the ICU database. Private PCRE2 general contexts select
that same requested-payload allocator for compilation, code, match contexts,
match-data vectors, interpreter heap frames and substitution work. The compiled
program keeps copied callbacks after its temporary compile/general contexts
retire; each operation releases its own contexts before return. No process-wide
PCRE2 cache or public API is added. See the documented
[general context](https://www.pcre.org/current/doc/html/pcre2_general_context_create.html)
and [match-data allocator ownership](https://www.pcre.org/current/doc/html/pcre2_match_data_create_from_pattern.html).

A failed context cannot fall back to the system allocator. Unregistered native
compilation returns an allocation error with a defined offset. Failed native
compile/match/substitution allocations propagate their existing typed errors;
no-match remains distinct. Normal and ASan/UBSan native checks pass all 29
observed allocation-failure positions across valid/invalid compilation, match,
no-match, substitution sizing/output/replacement errors and work-limit cases.
Operation failure preserves the compiled program for a successful retry and
all payloads drain after retirement. The existing 512-cycle native ABI/deadline
check and the independent 297-vector interpreter comparison also pass.

The public CLR and full-trim NativeAOT fault profiles replace only the native
memory core with a test build of its production source that fails one malloc.
All 11 compile/search/substitution positions must raise OutOfMemoryException,
retain a usable program after operation failure and drain every payload. This
control exposed a substitution bug: PCRE2 leaves SIZE_MAX as its output length
on allocation failure. RegExProgram now excludes that sentinel from the
buffer-sizing retry, preserving the typed allocation error instead of reporting
an output-budget error. The retained pre-fix public control fails on that error.

The public consumer observes 288 retained compiled bytes and a 21040-byte native
peak from temporary matching storage between reads. Clear, failed recompilation,
forgotten-owner finalization and four concurrent workers drain their ownership;
20000 prepared metadata/snapshot cycles allocate zero owner/all-thread managed
bytes. Matching itself remains cold allocating work under ADR 0112.

The export closure remains twenty-six functions. Exact source-built native
identity matters: the preceding ICU-only bridge can register callbacks but fails
the compiled-payload control, as does the preceding managed/native cohort.
Receipts, hashes and qualification limits are retained under
`bin/regex-allocation-accounting/2026-10-29/`. This domain does not implement the
public composed memory monitors. Other native domains, concurrent managed
collection, module/unload ownership and runtime delivery remain open. Physics
coverage stays **1365/1369 (99.71%)**, four Partial and 92 Excluded.

## MSDF-owned allocation domain (2026-10-29)

The pinned msdfgen sources remain byte-identical to their manifest. The native
build verifies that entire closure, then prepares a private build tree that
replaces container/edge/pixel allocation with the existing immutable text
callbacks. Vectors, queue storage, split edges, float/stencil images and the
returned RGBA buffer enter the same requested-payload native ledger as ICU and
PCRE2. No process-wide C++ allocation operator is replaced. The compiled private
core has no unresolved default new/delete or malloc/calloc/realloc references.

The overlay also gives partially split edges scoped ownership until adoption,
and constructs bitmap/edge assignment replacements before retiring prior owners.
A control without split ownership leaks on the fifth cubic allocation failure.
All 162 observed allocation failures across nine native modes drain completely,
preserve the error/output boundary and allow retry; fourteen separate ownership
controls preserve prior bitmap/edge owners and drain partial split/adoption.
Normal and ASan/UBSan/leak profiles pass. Normal images and bounds match a
separately compiled pristine core byte for byte, including quadratic/cubic,
teardrop, multiple contours and reversed orientation. Invalid/empty outlines and
the image-byte budget keep their existing behavior.

The managed adapter prepares callbacks without loading the ICU database and
releases the returned native image after copying. The public 1536-pixel source
control observes an 18056512-byte temporary float image, a 22607307-byte native
peak, complete native drain on disposal and 20000 prepared preference/snapshot
cycles at zero owner/all-thread managed bytes. Public FontFile consumers on
CLR/full-trim NativeAOT, the existing independent glyph oracle and native window
profiles qualify their exercised paths; exact commands, source/PDB and native
identities and any remaining limits live under
`bin/msdf-allocation-accounting/2026-10-29/`. Cold generation remains allocating
work. This domain does not complete the public composed memory selectors,
remaining native/runtime ownership, concurrent collection, runtime delivery,
network/performance or foreign-platform acceptance. Physics declaration coverage
remains **1365/1369 (99.71%)**, four Partial and 92 Excluded.

## Streaming GZIP allocation domain

StreamPeerGZIP now uses the existing private bridge's opaque streaming gzip/zlib context instead of the runtime compression PAL. Context and all zlib allocation/free callbacks share NativeAllocation with ENet and file block codecs. Requested payload bytes update coherent current/high-water counters at their actual native boundaries, including lazy decoder storage and short-lived failed initialization. SafeHandle disposal/finalization drains owned storage; no managed heap or GPU bytes enter this producer. These four private exports are required by native packaging and publish checks on every profile; no new public declaration or native dependency is added.

The preceding runtime reports only 40 tracked bytes for a prepared compressor in the retained RED control. The replacement must count the actual codec storage and return to baseline on retirement. The native production-bridge oracle verifies independent stock-zlib wire compatibility, output backpressure, borrowed-pointer retirement and injected failure at every allocation position for gzip/zlib compression/decompression, alongside existing ENet/file checks. Linux x64 CLR and strict full-trim NativeAOT pass normal and calibrated native-probe checks. The prepared public control now observes 350456 tracked bytes and complete drain, rather than the preceding 40-byte outer record. Both profiled hosts balance 131/131 compressor and 258/258 decoder failure-window allocations/frees, with no reallocations/failures; these are process-wide call observations, not requested-byte totals. The exact production oracle passes 60 injected allocation positions across all ENet/file/streaming modes, including eight positions in the four streaming gzip/zlib roles. HTTP/HTTPS, all five file codecs, ordinary native accounting, ENet accounting and desktop export preflight pass. Four CPU/no-device or NVIDIA GPU owner/separate scene profiles retain 512 measured complete physical frames at strict 0/0 managed bytes. An old-bundle negative control rejects the missing GZIP export. The NativeAOT maps contain the bridge and no CoreCLR. Commands, payload/source/PDB identities and receipts are retained under `bin/gzip-allocation-accounting/2026-10-28/`. This domain alone does not implement either public composed memory selector. BCL file/archive compression has a separate source-runtime profile below; stock runtime delivery, other native libraries and exact coherent CLR/NativeAOT allocation/GC aggregation remain open.

## BCL compression source-runtime domain (2026-10-29)

The [owned compression component](../../tools/runtime-native/compression/README.md)
builds the pinned .NET v10.0.1 PAL and its static zlib-ng/Brotli source closure.
The zlib outer record, internal callbacks and Brotli default/bootstrap/dictionary
allocations publish requested payload ownership into the existing production
native ledger. Explicit caller allocators retain ownership. Failed zlib
initialization retires both the partial internal state and outer record; Brotli
encoder OOM cleanup returns failure instead of exiting the process. There is no
global allocator replacement or installed SDK mutation. The component preserves
the twenty upstream exports and adds a private ABI witness; internal Brotli
symbol binding prevents allocation/free mixing with another loaded library.

Linux x64 native release and ASan/UBSan/leak profiles each pass **181 cases,
151 allocation-failure positions**. They cover raw deflate/zlib/gzip, inflate
reset, Brotli streaming and one-shot paths, quality 0/5/11, repeated and
incompressible data, invalid initialization, explicit allocator ownership,
complete native drain and healthy retry. Separate host zlib/Brotli codecs verify
encoded bytes and provide independent decode fixtures. Preparation checks reject
a bad archive, modified source, extra source and a source symlink.

Self-contained CLR and strict full-trim NativeAOT consumers execute actual BCL
streams, public ByteBuffer codecs and the public ZIP writer/reader. All four
stream modes show positive native storage, complete explicit/finalizer drain
and retained peaks: **350448 bytes** for the deflate/gzip/zlib roles and
**9930959 bytes** for Brotli. Each fresh process verifies **20000** prepared
snapshot reads at strict **0/0 owner/all-thread managed bytes**, one actual
compression PAL and one common ledger mapping. NativeAOT maps contain no
CoreCLR. The stock self-contained runtime is a retained RED control: its codec
storage bypasses the ledger in all eight CLR/NativeAOT role controls. The existing
ByteBuffer, file access/compression, ZIP, resource archive, resource pack and
managed EXR regressions also pass against a self-contained test host using this
PAL. Commands, source/binary/PDB identities and receipts are under
`bin/compression-pal-allocation/2026-10-29/`.

This is an opt-in isolated native runtime component, not the default runtime
or a qualified shipping runtime selection. It does not instrument managed heap
allocation/GC or compose the whole current/peak producer. Other native domains,
runtime delivery, foreign-platform execution and the full public monitor remain
open. Physics declaration coverage stays **1365/1369 (99.71%)**, four Partial
and 92 Excluded.

## WebP-owned allocation domain (2026-10-29)

The private image codec now supplies the existing native ledger through one
immutable allocation/free callback pair. Hash-checked generated copies of the
pinned utility allocator and SharpYUV implementation route codec, demux,
animation canvas and sharp color-conversion scratch ownership into that ledger.
Downloaded sources stay pristine; both utility object-library variants and the
SharpYUV target use the generated inputs. The existing browser codec target is
reused when present. A missing allocator export rejects before codec use.
Concurrent identical registration succeeds; incomplete or different ownership
rejects. Native allocation failures return null across the callback ABI.

Linux x64 release and ASan/UBSan/leak profiles each pass **2763 cases, 2734
allocation-failure positions**, covering all seven lossless/lossy methods,
static decoding and first-canvas animation. Every attempted failure retains
complete native drain and a healthy retry. Separate host libwebp decoding
verifies actual encoded pixels; the animation fixture checks its known offset
red rectangle on the transparent canvas. Four registration workers, prepared-use
rejection and allocator replacement controls execute in each child. The existing
native bridge checks retain output canaries, 128 writer rejections and 128
animation/error cleanups. Both source owners reject changed pinned input.

The public NativeMemory.CompressionHost `webp` role executes fourteen encoding
and decoding combinations on CLR and strict full-trim NativeAOT. Both observe
**2972898 bytes** of native high-water storage and **zero current delta** after
codec completion. Each host passes **20000 prepared snapshot reads at 0/0
owner/all-thread managed bytes**, one actual WebP library and one common ledger
mapping; NativeAOT has no CoreCLR. Old-bundle controls reject the missing allocator
entry point. Existing BCL codec roles still pass after sharing the report helper.
The ordinary image checks and real GPU/Compatibility texture-and-capture
profiles pass. Static bridge builds and reuse of an existing pinned codec target
also execute the native bridge checks on Linux; this does not qualify foreign
platform execution or browser delivery.
Receipts and exact source/binary/PDB identities are under
`bin/webp-allocation-accounting/2026-10-29/`.

This accounts the exercised CPU native payload domain; managed image/output
arrays, GPU storage and allocator bookkeeping are separate. Cold image I/O still
allocates. Remaining native ownership, managed collection/composition, runtime
delivery and foreign-platform/package execution remain open. The complete
GetMonitor contract stays Partial; physics declaration coverage remains
**1365/1369 (99.71%)**, four Partial and 92 Excluded.

## Allocation producers remain open

The [compression initialization cleanup](../components/http.md#failed-codec-initialization)
now retires the runtime's partially initialized native record before the engine's
tracked outer PAL record. The calibrated allocator regression reproduces the
preceding leak and observes balanced allocation/free calls after the fix. It
neither instruments codec-owned bytes nor completes the managed/native total;
both memory selectors and the current four open physics declarations stay open.

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
production managed/native shared commit boundary remain unqualified. The
separate NativeAOT producer below now qualifies its selected frozen registry flow;
this callback-only CLR profiler retains that ownership gap. The source CLR
profile below qualifies its selected reservation/GC/registry boundaries. In
particular, callback-ledger peaks cannot establish an exact simultaneous native
and managed peak if publication is delayed after an allocator commit. Do not
wire these diagnostic snapshots to GetMonitor or treat separate domain peaks as
a composition. The full exact contract remains accepted; physics coverage stays
**1365/1369 (99.71%)**, four Partial and 92 Excluded.


## CoreCLR reservation and frozen lifetime producer

The [source CLR profile](../../tools/physics/clr-memory-probe/README.md) now uses
actual reservation hooks in a separate pinned v10.0.1 CoreCLR. The shared GC
patch is byte-identical to the preceding NativeAOT patch. VM allocation prepares
a native scope, SOH/UOH ownership commits publish under the production native
ledger gate, and typed notification completes watches. A failed finalizer slot
retires at conversion to a free object. Scopes remain valid across allocator
retries and GC; the native scope storage lasts through the later typed callback.

GC completion independently walks surviving objects and checks frozen registry
bytes against actual object sizes. Insert/update/remove events publish under the
GC registry lock. Frozen snapshots now copy their dynamic contribution in the
same critical section: CLR interop/JIT preparation can allocate between separate
queries, making the former two-query test comparison invalid.

Workstation and two-heap server GC pass the shared 29 dynamic, 13 frozen,
14 native and seven common phases. Thirteen bounded children include source/SDK
baselines, disabled profiling, an actual enabled concurrent collector, invalid
inputs, the delayed-producer RED case and two 128-MiB-heap OOM controls. The
5-GiB native/4120-byte pinned overlap survives delayed typed publication; the
late producer is rejected. All prepared query/cycle brackets report zero managed
caller bytes. These are source-runtime correctness results, with receipts under
`bin/clr-shared-memory/2026-10-29/`.

The same public StreamPeerGZIP host now runs on the copied CLR and NativeAOT:
startup contributes 350456 native bytes, Clear and 64 repeated sessions drain,
and 20000 prepared reads allocate zero managed caller bytes. Each process maps
exactly one selected production native ledger. The measurement host disables
tiered compilation, matching the standalone CLR probe; its first tiered run
included preparation allocations and was not accepted as a prepared-read result.
The ordinary callback-only CLR profile also retains its five successful controls.

The isolated runtime retains exact CLR/JIT/CoreLib hashes. Native
FEATURE_EVENTSOURCE_XPLAT=0 requires managed FeatureXplatEventSource=false;
otherwise even the uninstrumented source baseline can fault in XplatEventLogger
before Main. PerfTracing remains enabled. The source CoreLib reports
.NET 10.0.1-dev; installed runtime binaries remain unchanged. Allocation profiling
routes ordinary objects through the common VM path and disables concurrent GC.
Observer native storage, per-collection full-walk cost, complete native-library
ownership, module/unload scenarios, foreign platforms and shipping runtime
qualification remain open. This does not implement either public total-memory
selector or change the **1365/1369 (99.71%)** physics declaration coverage.


## NativeAOT success and retirement producer

The [source-runtime probe](../../tools/physics/aot-memory-probe/README.md) executes
allocation/GC lifetime instrumentation in a separate v10.0.1 NativeAOT runtime
copy on Linux x64. The installed SDK archives remain unchanged. The production
engine now shares the native ledger described below; its ordinary managed runtime
is not instrumented. Four pinned source edits observe successful ordinary and shared
string/array fast paths, the common GcAllocInternal success path (including
pinned/direct and finalizable objects), and stopped-EE GC completion. Helper entry
attempts alone are no longer the measurement for this fixture.

The fast paths preserve the returned object, callee-saved registers and existing
slow/OOM frame paths; their [compiler kill set](https://github.com/dotnet/runtime/blob/v10.0.1/src/coreclr/jit/codegencommon.cpp#L705-L762)
uses the standard caller-trash ABI. The common [allocator](https://github.com/dotnet/runtime/blob/v10.0.1/src/coreclr/nativeaot/Runtime/GCHelpers.cpp)
notifies after a successful allocation and header/length initialization, never on
a null return. A one-shot pre-allocation failure tests propagation and a clean
retry without exhausting host RAM. At [GcDone](https://github.com/dotnet/runtime/blob/v10.0.1/src/coreclr/nativeaot/Runtime/gcenv.ee.cpp),
DiagWalkHeap enumerates actual objects in SOH/LOH/POH with fixed allocation contexts;
free objects are skipped. Summed aligned sizes rebase the dynamic ledger and
retire the difference. This uses actual object sizes rather than the GC's
ApproxTotalBytesInUse or a managed heap estimate. Cached reads never walk or
collect the heap.

Workstation and two-heap server GC exercise the same 29 numeric checks. The
ordinary, string, pinned, large, finalizable, temporary-peak and worker byte
oracles match the previous CLR lifetime witnesses. Each stress worker additionally
commits/retires 32768 objects (786432 bytes), with its earlier retained 4096-object
cohort giving a cumulative 884736 bytes. A midpoint barrier forces collection
before either worker finishes its remaining 16 batches, independently of scheduling.
Both retained worker payloads and final
stress payloads are checked after collection. Twenty thousand prepared snapshot
reads require zero managed caller bytes. The over-limit array publishes zero;
the injected pinned allocation also publishes zero, then its successful retry
publishes and later retires 123480 bytes exactly. This injection does not qualify
real OS out-of-memory behavior.

The source baseline and ordinary SDK hosts reject the absent hooks with exit 132.
An enabled concurrent collector rejects the stopped-heap precondition with exit
135; invalid-watch/snapshot children reject with exit 132. The checker retains
process maps without CoreCLR, actual link inputs and source/archive/native image
hashes. Changing NativeLibrary after SetupOSSpecificProps alone was insufficient:
the linker arguments already contained the SDK archive. The final project changes
both in their original link order and writes their actual identities. A mismatched
source pin is rejected before mutation; a repeated patch is also rejected.

Frozen/image bytes now use actual registration, update and removal events in the
shared GC registry. All three callbacks run before releasing gc_lock; the native
observer takes its bookkeeping gate afterward. The stopped-EE census holds the
same outer GC lock, so it cannot observe an unpublished registry transition. The
snapshot path acquires only the observer gate and never enters the runtime. This
keeps a consistent lock order, including native callers that wait for collection
while registering a segment.

The registered footprint is allocated minus first-object address. It excludes the
extra header and unused commit/reserve space, and handles empty identity, growth,
shrink and removal without a collection or a heap walk. This does not inspect
partially initialized array/string lengths at append time. The independent GC
walk checks that aligned frozen object sizes equal that live footprint. The
combined managed event ledger publishes dynamic plus frozen current and retains
the maximum of those simultaneous totals under one gate.

The same consumer now adds thirteen frozen phases: failed/empty registration,
growth from zero to 48/120 bytes, shrink to 24, commit-only/no-op updates, actual
ordinary-object field access before/after GC and complete retirement. A 64-cycle
worker publishes/retires 7680 bytes, with a live segment at the mandatory GC
midpoint. A 100000-object region contributes a 2400000-byte peak between reads.
A later 2400024-byte dynamic array first occurs separately, then overlaps another
large frozen region: the disjoint combined peak must be below the sum of domain
peaks; the overlap raises it and retirement preserves it. Both cached query paths
execute 20000 prepared reads with zero managed caller bytes. An invalid frozen
resize adds an eighth isolated rejection child. These controlled cases qualify
the object-footprint ledger, not all engine-host or native backing-storage domains.

The copied runtime keeps performance tracing enabled and disables the optional
LTTng provider, whose development headers are absent on this host. Its compiler
and tracing configuration differ from the shipping SDK. Original allocation/GC
receipts remain under `bin/aot-memory-producer/2026-10-29/`; extended frozen lifetime
receipts are under `bin/aot-frozen-memory/2026-10-29/`.

The composed fixture adds a native allocator under that same gate, covering the
actual malloc/realloc/free operation and ledger publication without a managed
callback. Fast object and shared string/array paths now acquire the gate before
their allocation-pointer commit; a register-preserving native shim and a
thread-local publication assertion retain the helper ABI and enforce the pairing.
The fourteen new phases exercise requested/aligned/zeroed storage, resize content,
failure preservation, a native worker alive at forced GC, an unseen 7000000-byte
native peak, and disjoint/overlapping native and managed lifetimes. A sparse
4294967313-byte native block grows to 4294967329 with preserved endpoint content;
this qualifies width rather than a physical 4-GiB working set. The prepared native
allocate/resize/free/read cycle also requires zero managed caller bytes.

The common path now also publishes at reservation: SOH pointer commit, UOH
free-list removal and UOH segment-end commit. Caller-owned native scope storage
survives retries/GC without retaining the bookkeeping gate; initialized-object
callbacks only complete typed watches. Failed finalizer registration converts its
uninitialized slot to a free object under the gate and retires its contribution,
retaining the actual transient peak. Memory clearing, event emission and
GC-capable allocation remain outside that critical section.

Seven common phases force a UOH hole reuse, finalizer failure/retry/retirement and
zero-byte prepared reads. A native worker frees a sparse 5-GiB block between a
4120-byte pinned reservation and its typed callback. The exact overlapping peak
must survive; a deliberately deferred producer must fail the same assertion.
Both GC profiles also exercise a 256-MiB request under a verified 128-MiB heap
limit, including GC, OOM and scope rollback. This is bounded allocator exhaustion,
not exhausted host RAM. See the source-runtime probe for commands and boundaries.
Receipts are under `bin/aot-common-memory/2026-10-29/`; earlier composed witnesses
remain under `bin/aot-composed-memory/2026-10-29/`.

The production NativeAllocation now delegates to the same pure-C core, replacing
the fixture's duplicate native allocator. A real strict full-trim NativeAOT engine
host uses only public StreamPeerGZIP operations: compressor startup contributes
350456 requested native bytes to the composed ledger, Clear returns native usage
to zero, 64 further sessions drain and 20000 prepared reads allocate zero managed
caller bytes. Its maps contain exactly one copied native bundle and no CoreCLR.
The twelve-child probe matrix retains all success and rejection witnesses with
that same bundle. The standalone assert-enabled C oracle independently checks
alignment, failure preservation, ownership drain and composed-peak arithmetic;
its synthetic managed values are not managed-runtime acceptance. Current CLR
regressions pass native allocation, GZIP (ordinary and calibrated native probe),
HTTP/HTTPS, ENet allocation, FreeType (1172/1180 unhinted/hinted failure positions),
audio and native export preflight. CPU without a device and NVIDIA GPU each pass
owner/separate execution: 128 measured complete frames per profile, strict 0/0
managed bytes (512 frames total). An old-bundle control rejects ABI preparation
before native use. Eleven assembly/PDB cohorts match their current authored
sources. New receipts are under `bin/shared-native-memory/2026-10-29/`.

This closes one shared native publication boundary for the accounted domains,
not complete process/host acceptance.

Production integration still needs CoreCLR image/frozen accounting and full
engine/module-host acceptance, concurrent collector support, qualified large sizes
and OS failures, the complete shared
native/managed publication boundary and complete native-library domains. The
full walk adds O(total heap) work to each collection; neither its cost nor observer
overhead is accepted for production by these correctness results. Other platforms,
GPU physics, networking and real-window FPS are not qualified here. GetMonitor
remains Partial; physics declarations remain **1365/1369 (99.71%)**, four Partial
and 92 Excluded.
