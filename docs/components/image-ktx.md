# KTX images

Last updated: 2026-11-05

This component loads flat KTX1 and KTX2 assets through `Image.LoadKTXFromBuffer`, `Image.Load`, `Image.LoadFromFile` and synchronous or threaded `ResourceLoader` image-texture loading. It follows [ADR 0039](../decisions/resources.md#adr-0039) and the private native ownership rules of [ADR 0012](../decisions/product.md#adr-0012).

## Behavior and ownership

Input is copied before decoding. A private native owner validates and materializes the complete image, then copies every mip into managed Image storage. Its source memory may retire before level copies. The owner is released on success, decoding failure, transformation failure and disposal. Only the final managed state is published; failure preserves pixels, metadata and change notifications. Subscriber exceptions occur after publication.

A flat two-dimensional image may have a base level or a complete mip chain. Cube, array and volume containers are rejected by this Image role. Raw byte, packed, unsigned 16-bit, half, float and supported block layouts preserve their channel/mip bytes. KTX1 row padding is removed at every level, including ragged RGB mips. Opposite-endian KTX1 samples become canonical little-endian Image bytes. Legacy luminance and luminance-alpha header roles are normalized for the reader while preserving Image L8/La8 semantics.

KTX2 Basis ETC1S, UASTC LDR, UASTC HDR 4x4 and HDR 6x6 intermediate payloads transcode to portable RGBA8 or RGB half pixels without an active rendering device. This keeps loaded assets usable through the existing texture pipeline on each backend. RGB8 sRGB pixels convert to linear bytes, including their mips; alpha remains unchanged. Orientation and channel swizzles apply to every level before publication. BC/ETC/EAC/ASTC data is decoded when a transformation requires pixels. [Shared block decoding](image-block-decoding.md) supplies the corresponding executable texture path, including signed EAC and punchthrough alpha.

Source, managed output, native materialization and conservative RGBA-float expansion are each bounded to 64 MiB. Key/value metadata is bounded to 1 MiB and 4096 entries. Header roles, complete levels, metadata/DFD ranges and sample bit fields, non-overlapping KTX2 sections, Basis global data and slice ranges are checked before native decoding. Invalid sources use `InvalidDataException`; unsupported roles/formats use `NotSupportedException`. Decoder allocation failures use `IOException`.

## Native source and delivery

The private decoder ABI is `e2d_ktx_create`, `e2d_basis_create`, `e2d_ktx_level`, `e2d_ktx_destroy` and `e2d_block_decode`. The legacy Basis entry point supplies imported array payloads; public Image loading retains the KTX container contract. KTX-Software 5.0.0-rc2 is pinned to commit `16059fa93d834242a0aaed9bb54952d76b143fac` and archive SHA-256 `bce6794bca57d86de2f00683ba8eaa6b3e8875d5abe45ac1136e206eb68aa476`. Stable 4.4.2 predates the required HDR transcoder. The read-only build disables platform uploads, tools and optional SIMD requirements; the optional Ericsson ETC software decoder is disabled. Selected Basis, DFD, miniz, ASTC and Zstandard sources remain private. The [portable texture encoder](portable-textures.md) reuses the approved BasisU/astcenc closure and adds the private e2d_image_encode/e2d_image_encoded_free exports. The package validator requires all seven exports.

The pinned reader ignored failed Basis palette/table decoding and could dereference empty tables on corrupt content. `tools/ktx-native/decoder-guards.cmake` applies an exact-preimage error-propagation correction to generated build source, preserving the fetched archive. Regression fixtures exercise malformed codebooks and invalid DFD sample lengths. The Zstandard amalgamation and declarations receive a mechanical private symbol namespace so the static closure can coexist with the font codecs. Neither adaptation changes compression/transcoding algorithms. Original notices are retained in `licence/`.

### Allocation refusal and initialization

The same pinned source now handles allocation refusal in common stream construction,
DFD creation, metadata hash insertion and Basis vector growth. Partial streams,
textures, metadata tables, prototypes and level indices retire on failure. Shared
decode tables publish their initialized flag only after completion; failed partial
initialization can be retried in the same process. DFD allocation refusal reports
an allocation error rather than malformed input. Public Image loading projects
these errors as `IOException`, preserving the previous image, input and `Changed`
boundary. The guards change ownership/error paths, retaining the codec algorithms
and the single approved source version.

`prepare_allocations.py` verifies eight complete original/output SHA-256 pairs
before writing the extracted build copy. Unknown input refuses the entire batch;
repeated preparation is idempotent. `decoder-guards.cmake` adds scoped C++ ownership
to the generated transcoder. The immutable download remains unchanged.

`prepare_memory.py` composes those ownership guards with a checked C allocation
namespace in the extracted build source. Its 63 original/output SHA-256 pairs
cover libktx, DFD, Basis vector/miniz/Zstandard and ASTC storage/ownership.
The lexical C namespace pass preserves comments, literals, includes and allocator
attributes; unknown C allocation-bearing sources or changed pinned files refuse configuration.
`allocator.h` and `memory.c` route those requests and the encoder's returned buffer
directly to the common native ledger before first use. The ledger's own backing
allocator and global C++ allocation operators are outside that namespace. No managed
callback registration, global allocator interposition or additional module is used.
The unified producer uses the existing common core; standalone component checks
link the same core as a separate CMake static dependency.

`memory.hpp` supplies aligned, exception-safe objects, trivially destructible
arrays and standard vector storage through that same ledger. The bridge's
normalization, mip, input/output, Basis alignment and encoder working vectors,
generated level indices and all selected ASTC context/working arrays use it.
`prepare_cpp.py` adapts four pinned ASTC implementation files and its synchronous
parallel-manager header. Scoped owners retire partial context, block descriptor,
percentile, partition and alpha-preprocessing storage on exceptions; publication
occurs only after successful initialization. Immediate init/term callbacks are
templates, avoiding `std::function` heap boxing without retaining callbacks.
Diagnostics are disabled in this profile. Its ASTC archive has no remaining
default C++ new/delete references.

`basis-memory.hpp` applies that allocator to Basis strings, formatting streams,
vectors, ordered/unordered maps and sets, and priority queues. Its queued callable
retains independent value copies, strong assignment failure and deferred LIFO
execution; it owns its captures through the same native core. The adapter changes
storage ownership without replacing global C++ operators or codec algorithms.
`prepare_basis.py` covers 22 pinned implementation/header inputs. Nine preceding
prepared states have exact upgrade hashes; unknown drift still refuses the batch.
A UTF8 BOM is removed from the prepared build copy before prefixing includes.

Both byte and float resampling paths now own all channel resamplers before growing
sample vectors. Constructor status is checked before borrowing contributor lists
or submitting rows. An injected C allocation refusal previously reached
`Resampler::put_line` with incomplete state and crashed; it now returns failure,
retires every partial owner and permits exact retry. Remaining raw TinyEXR utility
owners, thread/exception/locale runtime storage, support/provider heaps and full
platform/package qualification remain open.

The Linux `Electron2DKTXMemoryCheck` fixture links this same production codec
profile to a test-only refusal core in one `libElectron2D.so`.
It tests the routed C/C++ request ownership, every calibrated
refusal, independent mip pixels, full temporary drain and a healthy retry. Cold
ETC1S cases use a fresh process for each failure, checking retry against the healthy
global-table baseline. `NativeKTXMemoryTests` exercises the public Image boundary
through the same fixture. See [reproduction commands](../../tools/ktx-native/README.md#allocation-refusal-checks).

The public binary checks on self-contained CLR and strict full-trim
NativeAOT 10.0.14 each exercise 212 warm refusals across nine independent KTX2 mip
oracles, six legacy-normalization, 60 block-decoder and 507 Image encoder refusals.
The portable Basis/ETC2/ASTC LDR/HDR/RDO consumer adds 566 refusals preserving RID,
retained envelope, size/override, complete mips, alpha, source and both notification
boundaries. Every failed operation drains temporary storage and permits exact
healthy retry. Seven native LDR encoder modes additionally exercise 486 warm refusals with
complete mip chains, requiring no published output, complete temporary drain and
byte-identical healthy retry. A separate native container check covers eight
constructor/copy/shrink roles and 35 element-construction failures; the original
header fails the same check. Partial nested copies now retain their constructed
element count, retire both elements and backing storage on unwind, and preserve
valid shrink bounds. The encoder shares the legacy reader's slice-alignment
adapter before native block transcoding; codec algorithms and output bytes remain
unchanged.

A nonconstant complete-mip RDO input adds 128 native refusals, exercising hash
history rather than the solid-block bypass. A production-core control observes
49 live bytes for the actual default Basis parameter strings; the matching
preceding graph observes zero and fails the same check. The C++ owner check also
verifies string/stream/map/hash/queue storage, independent callable copies and
failed assignment. `Electron2DBasisMemoryCheck` passes four parameter, six job,
50 byte-resampler and 50 float-resampler refusals, with full native drain and retry.

The final Linux x64 profile passes 35,002 independent cold refusals/retries and
1,497 warm requests (823 decoder/metadata, 486 ordinary encoder, 128 nonconstant
RDO encoder and 60 ASTC block).
The native C++ owner check covers 128-byte alignment, nested-constructor,
partial-vector-copy and array-constructor failures with full retirement and retry.
A separate actual ASTC alpha-preprocessing check covers 32 refusals across
LDR/HDR and 4x4/8x8 footprints. The public CLR/NativeAOT checks use the same
final refusal module.

The entire codec closure also passes the warm refusal matrix under ASan/UBSan.
Native decoder, block, encoder, container, C++ owner, Basis and ASTC alpha checks keep
leak detection enabled. The Python driver disables interpreter leak reporting
and independently requires exact native temporary drain.

The refusal core is never packaged. Production source now routes the documented
C allocation domain and the described C++ working storage to the shared ledger;
this does not cover the remaining utility/runtime allocation,
support/provider heaps or the complete monitor. Rebuilt release packages,
foreign hosts and coherent managed/native composition remain open in the
[allocation audit](../coverage/performance-producers.md#ktx-allocation-refusal-2026-11-05).

The rebuilt Linux x64 unified module retains exactly 166 engine exports. Public
CLR and strict full-trim NativeAOT consumers record a 262,497-byte unsampled native
peak from a raw Image load and a 2,097,573-byte peak from legacy luminance
normalization, including overlapping C++ scratch and native pixels. They validate
nine independent mip oracles and repeat 72
loads while preserving live GZIP ownership in the same ledger. A control using
the preceding C-only accounting module fails the overlapping C++ peak assertion
after decoding correct pixels. Windows x64 MSVC standalone decoder, block,
encoder and C++ owner checks pass against the same prepared source profile;
its archive merge uses the native library manager. The producer probes the raw
localized compiler include prefix so Ninja retains header dependencies. Its hash
enters compile commands to refresh old empty dependency records; regression checks
cover cache migration and later header invalidation. These checks qualify the
described domains, with complete provider/runtime composition still open.
The standard-storage update's local receipts are in `bin/basis-cpp-accounting/2026-11-05`.
The preceding working-vector/ASTC profile is in `bin/ktx-cpp-accounting/2026-11-05`;
the preceding C-only profile is in `bin/ktx-production-accounting/2026-11-05`.

Desktop, Windows, Android, Apple and Web producers require the private bridge. The unified static product includes its codec and common-core closure in one archive; codec/merge changes force it to refresh. The standalone component archive embeds its codec closure and links Electron2DKTXCore through CMake. Packaging rejects global codec symbols that could collide with the font closure. Ordinary engine/game builds restore native assets without invoking a native compiler. The compiled managed API contains no codec type, pointer or backend handle.

## Verification and limits

`ImageKTXTests` checks nine independent standalone-tool KTX2 pixel oracles, 25 KTX1 raw formats, 17 opaque block roles, all stored mips, row padding, 16/32-bit opposite endian, orientation, swizzles, sRGB, atomic failure, concurrent decoder initialization, threaded resource loading and cache identity. Native checks retire borrowed source memory before level copies, reject every oracle truncation, mutate 2304 headers and 2304 complete containers, and exercise malformed codebook/DFD regressions. ASan, UBSan and leak checks use the same corpus. Clang's function-type sanitizer is disabled only for the upstream C vtable target because that ABI erases derived object pointer types; address and other undefined-behavior instrumentation remain enabled.

Linux Wayland GPU and Compatibility hosts compare rendered pixels with independent decoded Image textures for all nine oracles plus BC source data. GPU retains half precision; Compatibility explicitly converts HDR to RGBA8. The separate block corpus establishes ETC/EAC/ASTC sampling; foreign-target execution, native package publication and owner acceptance remain separate. Fresh Linux source/project/NuGet consumers and self-contained publishes execute public Basis LDR/HDR loading with native compilers blocked. A separate NativeAOT application executes the same public codec consumers. The managed self-contained publish passes the 72-ELF and 92-notice audits. [File-backed arrays](compressed-arrays.md) now consume GSTL, legacy .basis/KTX2 channel roles and the common decoder through resource lifecycle/cache/archive integration and GPU layer/mip texel fetches. Their imported transfer-value policy is separate from public Image input.

Those rendered/package checks predate this allocation update and the combined
native module. Their ELF/notice counts are historical. Current evidence qualifies
the Linux unified C/C++ working-storage domain, isolated refusal profile and Windows
standalone codec profile; rebuilt release packages and complete platform
acceptance remain open.
