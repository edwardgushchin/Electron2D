# Native memory ownership inventory

Last updated: 2026-10-29

This routes the remaining allocation work required by
[ADR 0016](../decisions/core-object-runtime.md#adr-0016). A qualified allocation
domain is not a qualified library, process or composed memory monitor. Every
unqualified boundary below stays open; this inventory introduces no exclusions
from the required managed/native contract.

## Recorded boundary

The baseline `6f49a25a9` runtime assembly declares **2124 P/Invoke imports in
38 library-name families**, including internal bindings and platform-specific
imports that are not necessarily exercised on Linux. The Release test consumer
contains **66 ELF payload files**, including duplicate aliases. Its native
dependency records expose closures hidden by the top-level bridge names.
These are metadata and dependency counts, not allocation counts or proof of
execution. The mesh allocator registration adds one private import; the
hash-bound refreshed snapshot records **2125 imports in the same 38 families**. The shaping refusal epoch and actual shaping/auto-hinting version queries add three more. The video allocator installation and shared callback query add two; the current compiled snapshot records **2130 imports in 38 families** at `bin/ogv-bridge-allocation/2026-10-29/compiled-imports.json`.

The reflection exporter, assembly identities, complete import records and
`readelf -d` payload records are retained under
`bin/native-memory-inventory/2026-10-29/`. The export inspects compiled
DllImport metadata, including generated LibraryImport thunks. Source review also
starts at [NativeLibraries](../../src/Properties/NativeLibraries.cs), native
producer recipes and dynamic loading sites. BCL/runtime imports, runtime-loaded
OS plugins and target-specific static closures require their own audit; they
are not silently covered by this assembly snapshot.

## Engine and codec families

| Import family | Owned producer / closure | Current qualification and remaining work |
| --- | --- | --- |
| `Electron2DENet` | [shared C ledger](../../tools/enet-native/memory.c), ENet, FastLZ, zlib and Zstandard | Direct native payload, host/packet/file/streaming-codec domains have allocation-boundary evidence. This does not instrument unrelated libc/runtime storage. |
| `FAudio` | [audio producer](../../tools/audio-native/CMakeLists.txt), FAPO/effects and platform output | Engine/effect callbacks are tracked. SDL output also allocates device/staging storage through SDL_malloc in `FAudio_platform_sdl3.c`; it follows the open SDL boundary. Decoder and platform closures need complete allocation routing evidence. |
| `freetype` | [private font producer](../../tools/font-native/CMakeLists.txt), PNG/zlib, WOFF2/Brotli and auto-hinter HarfBuzz | FT_Memory and the qualified WOFF2/private auto-hinter domains are tracked. Preserve actual source/profile identity and do not transfer that result to desktop shaping. |
| `HarfBuzzSharp` | Separate desktop shaping library, selected by NativeLibraries | The selected [15.0.0 source producer](../../tools/shaping-native/README.md) routes original face/font/buffer/table/cache allocations to the shared C ledger, including native exit. Native-thread refusal detection and checked lazy construction protect publication/retry. It shares the single upstream source/version pin with the private FreeType auto-hinter. Runtime preparation rejects mixed versions. Foreign targets, final package qualification and C++/OS provider closure remain open. |
| `Electron2DTextBreak` | [text producer](../../src/Servers/Text/Native/CMakeLists.txt), private ICU, PCRE2 and MSDF | Qualified owned callbacks and generated overrides cover their documented domains. Managed/runtime/module composition and delivery remain separate. |
| `Electron2DWebP` | [WebP/SharpYUV](../../tools/webp-native/CMakeLists.txt), static codec/demux/animation | Both utility allocator variants and SharpYUV use the immutable caller allocator. See [WebP evidence](performance-producers.md#webp-owned-allocation-domain-2026-10-29). |
| `Electron2DMesh` | [meshoptimizer](../../tools/mesh-native/CMakeLists.txt), allocator.cpp and simplifier.cpp | The connected mesh allocation slice uses the pinned library's allocator hook. Preserve throwing-new semantics, typed failure, full scratch retirement, pristine topology and public all-surface atomic publication. |
| `Electron2DOGV` | [OGV producer](../../tools/ogv-native/CMakeLists.txt), Ogg/Vorbis/Theora | The [bridge domain](performance-producers.md#ogv-bridge-allocation-domain-2026-10-29) tracks contexts, frame/packet buffers and copies. The [container domain](performance-producers.md#ogg-container-allocation-domain-2026-10-29) also tracks original Ogg stream/lacing/granule/sync storage and growth, with thread-local refusal classification. Startup/growth refusal, retry, independent lifetime and prepared replay are checked. Ogg bit buffers and Vorbis/Theora allocation remain open; this is not complete codec ownership. |
| `Electron2DKTX` | [KTX/Basis producer](../../tools/ktx-native/CMakeLists.txt), libktx, BasisU, detex and C++ containers | Vector/object/codec/transcode storage requires a connected allocation domain, including errors and retained contexts. |
| `Electron2DWebRTC` | [WebRTC producer](../../tools/webrtc-native/CMakeLists.txt), libdatachannel, SCTP, transport and crypto | Bridge handles, events and C++ containers allocate independently. The observed ELF also needs libssl/libcrypto; no complete ownership qualification exists for that closure. |
| `Electron2DSSL`, `Electron2DCrypto` | [TLS producer](../../tools/native/build_tls.py), contexts/sessions/BIO/crypto caches | A tracked DTLS cookie is a narrower caller allocation. The opaque crypto/session/cache domain and allocator registration before first use remain open. |
| `SDL3` | SDL core, window/render/audio/input and platform backends | The [owned SDL source profile](../../tools/sdl-native/README.md) supplies the common ledger as its original allocator, including first use, cross-thread TLS cleanup and native exit. Stock SDK/package selection, direct libc/foreign backend allocations and complete provider closure remain open. This route also receives SDL_malloc requests from FAudio; it does not qualify all audio/platform allocation paths. |
| `SDL3_image` | Image provider plus its statically included codecs | ELF dependence on SDL alone does not mean every nested codec uses SDL_malloc. Audit the selected codec build and actual image call paths; independent WebP/EXR/BCL paths have their own boundaries. |
| `SDL3_shadercross`, `spirv-cross-c-shared` | Shadercross, SPIR-V Cross, DXC and its zlib/runtime closure | The observed shadercross ELF depends on SPIR-V Cross and DXC; DXC also needs libz. Compiler/object/container storage remains open. Prepared render recording is not compilation allocation evidence. |
| `Electron2DRender` | Private renderer bridge plus SDL backend and graphics driver interfaces | Existing render/GPU allocation selectors cover their documented resources. They do not compose CPU native storage or qualify every SDL/backend/host allocation. Preserve the accepted GPU-versus-CPU accounting boundary. |

All seventeen engine/codec library-name families from the compiled snapshot are
represented above. Static target aliases may combine them into `__Internal` or
archive modules; that changes symbol delivery, not ownership.

## Platform and runtime boundaries

The other **21 import-name families** are libc; libdl; DBus; fontconfig; GTK/GDK,
GLib/GIO/GObject; X11/Xext; Windows comctl32/gdi32/kernel32/ntdll/psapi/user32;
and Darwin CoreFoundation, Objective-C and the absolute/short libSystem names.
Their owning source call sites must distinguish caller-owned buffers, retained
provider objects, borrowed references and OS/driver services. Returning an
opaque pointer or calling the correct release function alone does not establish
requested-byte current/peak accounting. Unproven ownership remains open, rather
than being implicitly excluded as host memory.

Additional boundaries outside the Engine assembly's P/Invoke table remain:

- BCL compression PAL: the isolated source-runtime component is qualified;
  ordinary runtime selection and shipping delivery remain open.
- CLR/NativeAOT heap reservation, allocation, GC/free/frozen/module lifetime and
  concurrent collection: use the existing source-runtime probes and preserve
  their explicit production limits.
- Runtime interop, marshalled heap strings/buffers, loader and C/C++ support
  allocations: establish the actual ownership domain and publication boundary.
- Coherent composition: all required managed/native producers must share the
  same boundary and preserve a transient peak, failures, teardown and prepared
  allocation-free reads. Summing independent domain maxima is invalid.

This is a finite routing inventory of the observed imports and producer graph.
It deliberately records the remaining ownership audits as open. It does not
prove that every allocation in a dynamically loaded or foreign closure is
accounted, and cannot promote GetMonitor or the four remaining physics rows.

## System ALSA MIDI input boundary

[OS.MIDI.cs](../../src/Core/OS/OS.MIDI.cs) owns one explicit sequencer session and releases its native client, ports, subscriptions and buffered data on close/failure/engine cleanup. Virtual native source tests exercise release after callback and startup failures. ALSA query allocations are freed on success/failure; returned event storage is borrowed from ALSA. The system library owns its internal heap/pool implementation. That heap is not yet attributed to Electron2D native allocation accounting or MemoryMonitor, so handle-lifetime verification does not close the native memory contract. See [MIDI input](../components/midi-input.md).
