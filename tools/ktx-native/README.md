# Private KTX and image codecs

The bridge loads flat KTX1/KTX2 images with complete mip chains and transcodes Basis LDR/HDR to portable pixels. Imported array assets also decode legacy .basis channel roles through e2d_basis_create. Both creators share e2d_ktx_level/e2d_ktx_destroy ownership. The fifth export e2d_block_decode supplies ETC1/ETC2/EAC and ASTC pixels to Image and texture snapshots. The ABI remains internal.

CMake pins KTX-Software 5.0.0-rc2 by SHA-256 and builds its private static closure. Platform uploads, tools, optional SIMD requirements and the Ericsson ETC software decoder are disabled. The original archive remains unchanged. The extracted build copy removes the exact pinned project/library x86 rejections for the bounded scalar codec; unknown source text fails the adapter. Android ARMv7/x86 retain API 21 by using the encoder's ordinary 32-bit stdio ABI instead of requesting API-24 large-file symbols. ARMv7 retains precise arithmetic and explicitly ignores the unsupported floating-point exception mode. Encoder input remains memory-backed and bounded by the bridge. `decoder-guards.cmake` applies an exact-preimage guard to generated code so failed Basis palette/table decoding returns an error before sampling empty tables. A shared once-only initializer coordinates cold KTX and legacy Basis use. Zstandard source/declarations receive a mechanical private symbol namespace to coexist with the font static archives. License texts remain unchanged in `licence/`.

```bash
cmake -S tools/ktx-native -B obj/ktx-native -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build obj/ktx-native --target Electron2DKTXCheck --parallel 2
python3 -B tools/ktx-native/test_bridge.py obj/ktx-native/Electron2DKTXCheck tests/Electron2D.Tests/TestData/KTX/oracle.json
python3 -B tools/ktx-native/test_bridge.py obj/ktx-native/Electron2DKTXCheck tests/Electron2D.Tests/TestData/CompressedArray/basis-oracle.json
```

`-DELECTRON2D_STATIC_NATIVE=ON` embeds the codec closure in the component archive; standalone consumers also link the common `Electron2DKTXCore` CMake dependency. The unified static product embeds that common core once through its existing engine component. The pinned `ktx_read` archive already embeds the complete BasisU encoder closure, so the merge includes it once. Appending `basisu_encoder` again duplicates strong symbols when Apple applications load the complete archive. The native check validates owner/input/output failure paths and compares all mips against the independent tool oracles. Set `ELECTRON2D_KTX_REPRO` to an ignored output path to retain the last deterministic mutation when diagnosing a test failure.

`generate_fixtures.py` regenerates the small test corpus with the official standalone 5.0.0-rc2 `ktx` tool. It records the tool hash and creates raw, Zstandard, ZLIB, ETC1S, UASTC LDR and two HDR profiles, plus corrupted codebook/DFD regression inputs. Only fixtures and recipe belong in Git; downloaded tools, archives and build products stay ignored.

`generate_basis_fixtures.py BASISU EXR_ORACLE OUTPUT_JSON` uses the pinned standalone Basis tool and independent OpenEXR reader for legacy ETC1S/UASTC LDR/HDR mip oracles. Tests cover every truncation, unaligned borrowed input, checksum-repaired payload mutations and mixed cold initialization (`--mixed-oracle PATH` with the KTX oracle). The adapter privately materializes aligned legacy slices within 64 MiB; no codec source modification is needed for unaligned container offsets.

Block decoding adds the ISC detex subset at cab11584d9be140602a66fd9a88ef0b99f08a97a. Exact-preimage generated arithmetic corrections replace negative signed shifts with multiplication and overflowing signed selector shifts with unsigned masks. ASTC uses the scalar astcenc closure already merged into ktx_read. No extra codec archive is needed. Source and conservative RGBA-float expansion are each bounded to 64 MiB. HDR half output requires two-byte alignment; borrowed source bytes need no alignment.

Build `Electron2DBlockCheck`, then run `python3 -B tools/ktx-native/test_blocks.py PATH_TO_CHECK tests/Electron2D.Tests/TestData/Blocks/oracle.json`. The same corpus runs against shared, merged static and ASan/UBSan/leak builds. Native tests cover every mip, exact lengths, guards, 9216 block mutations and 1152 concurrent calls. RA-as-RG swizzling belongs to the managed boundary.

To regenerate block fixtures, build `etc-oracle/Oracle.csproj` with `-p:EtcOracleSource=PATH` pointing at the AssetRipper.TextureDecoder project from commit 13b4f6ee4906877402ab69fc5a0b51b83c15db44. That standalone oracle compiles upstream test-only source without packages. Build the standalone scalar astcenc CLI from the pinned KTX archive. Then run `generate_block_fixtures.py ETC_ORACLE_DLL ASTCENC EXR_ORACLE OUTPUT_JSON`. ETC byte pixels come from the independent oracle, signed EAC from explicit numerical vectors, and ASTC pixels from the official CLI plus OpenEXR. The fixture records tool hashes; these tools are absent from the runtime and packages.

The generated detex header selects __forceinline for the MSVC producer and retains the original inline spelling for other compilers. Source copies include that header consistently; this mechanical adaptation does not change decoder algorithms. Standalone Windows native checks pass under MSVC; packaged/rendered Windows consumers remain separate.

The encoder adds private e2d_image_encode/e2d_image_encoded_free exports. It reuses the pinned BasisU encoder for UASTC KTX2 and ETC/EAC transcoding, plus scalar astcenc for 4x4/8x8 LDR/HDR. No external process or extra dependency is required. Native compressor miniz uses the read closure's implementation to avoid duplicate symbols. Build Electron2DEncodeCheck to verify complete mip chains, input bounds and concurrent use. Platform bundle producers build and validate all seven exports; source overrides use Electron2DNativeLibrary for both engine and local consumers.

## Allocation refusal checks

`prepare_allocations.py` checks eight original/output SHA-256 pairs before changing
the extracted build copy. It adds partial stream/texture/DFD/hash cleanup, throwing
Basis vector failure, partial nested construction cleanup and transactional shared-table initialization. The generated
transcoder owns its prototype and level indices through C++ unwinding. All roles
continue using the single pinned archive; these ownership/error guards retain the
codec algorithms. Check the immutable source, idempotence and atomic drift refusal:

```bash
python3 -B tools/ktx-native/test_prepare_allocations.py PATH_TO_PINNED_ARCHIVE
python3 -B tools/ktx-native/test_prepare_memory.py PATH_TO_PINNED_ARCHIVE
cmake -S tools/ktx-native -B BUILD -G Ninja -DCMAKE_BUILD_TYPE=Release \
  -DELECTRON2D_STATIC_NATIVE=ON -DELECTRON2D_KTX_MEMORY_CHECK=ON
cmake --build BUILD --target Electron2DKTXMemoryCheck Electron2DKTXContainerCheck \
  Electron2DKTXCPPCheck Electron2DASTCMemoryCheck Electron2DBasisMemoryCheck --parallel 2
BUILD/Electron2DKTXContainerCheck
BUILD/Electron2DKTXCPPCheck
BUILD/Electron2DASTCMemoryCheck
BUILD/Electron2DBasisMemoryCheck
python3 -B tools/ktx-native/test_memory.py BUILD/fault/libElectron2D.so \
  tests/Electron2D.Tests/TestData/KTX/oracle.json \
  tests/Electron2D.Tests/TestData/CompressedArray/basis-oracle.json \
  --blocks-oracle tests/Electron2D.Tests/TestData/Blocks/oracle.json --cold
```

`prepare_memory.py` composes the ownership guards with a lexical C allocation
namespace and `prepare_cpp.py`'s ASTC ownership guards in the extracted build
source, verifying 63 complete input/output hash
pairs. Its lexical C pass preserves comments, literals, includes and allocator
attributes, rejects unregistered C allocation-bearing sources, and validates the profile on every
configuration. Its C/aligned allocation calls and returned encoder buffers use
`allocator.h`/`memory.c` and the process-lifetime native ledger before first use.
`memory.hpp` routes bridge/transcoder vectors, level indices and selected ASTC
context/working arrays through that same ledger with aligned scoped ownership.
Synchronous ASTC init/term callbacks use templates, eliminating function boxing.
The selected ASTC archive must have no default C++ new/delete references;
`basis-memory.hpp` routes Basis standard strings, streams, vectors, maps/sets,
priority queues and queued callable captures through the existing allocator.
Callable copies retain independent state and deferred LIFO order. Resampler owners
retire across sample-vector exceptions and constructor status failure before row use.
`prepare_basis.py` verifies 22 complete inputs; nine preceding prepared states
have exact upgrade hashes. Diagnostics are disabled in this profile.
No managed callback registration is needed. The immutable source archive is retained.

The Linux-only refusal fixture links that production codec profile to the test
fault core in one `libElectron2D.so`. It changes no allocation call sites at link
time and must never replace production output. Global linker wrapping would also
intercept the ledger's backing allocator and recurse. The production unified module
uses the existing common core once; standalone checks link that same core through
`Electron2DKTXCore`.

The native script also checks all seven LDR encoder modes with complete mip chains: every warm refusal must
leave null/empty output and unchanged source, drain temporary storage and preserve
the exact healthy encoded bytes on retry. `Electron2DKTXContainerCheck` injects
element-construction failures across the vector constructor/copy family, verifies
partial retirement and retry, and checks shrinking a nontrivial element span.
The encoder reuses the reader's bounded slice alignment before transcoding its
internal Basis container; this also covers unaligned complete-mip payload offsets.
`Electron2DKTXCPPCheck` exercises aligned object/array and nested construction/copy
retirement. `Electron2DASTCMemoryCheck` injects real context/array failures with
alpha preprocessing across LDR/HDR and both block footprints. Block refusal checks
require unchanged borrowed input/output guards, independent pixels and retry for
every mip. Windows Ninja builds use the raw localized include-prefix probe from
`tools/native/msvc-dependencies.cmake`; verify header invalidation after changing
the compiler/toolchain.

`Electron2DBasisMemoryCheck` checks actual parameter-string storage, deferred jobs
and LDR/HDR resampling at every allocation position, with full drain and exact
retry. The production-core C++ check includes actual Basis parameter strings;
the preceding matching source/archive graph reports zero tracked bytes instead
of their 49-byte minimum on the qualified Linux profile. The native Python driver
also uses nonconstant complete-mip RDO input to exercise hash-history allocation.
Remaining raw utility owners and thread/exception/locale runtime allocation are
separate domains; no global C++ operator is replaced.

Publish FastTests into separate CLR and strict full-trim NativeAOT directories,
using `Electron2DNativeLibrary=ABSOLUTE_BUILD/fault/libElectron2D.so`. Run each with
`--ktx-memory`. The public check requires `IOException`, unchanged pixels/format/
dimensions/input/Changed, full native temporary drain, exact independent mip pixels
and healthy retry at every warm request. It also checks Image normalization/block/
encoder working storage and portable Basis/ETC2/ASTC LDR/HDR RID, envelope, mip,
metadata and notification atomicity. Keep ordinary decoder, block, encoder and
sanitizer checks. Separately run FastTests `--ktx-production-memory` against the
rebuilt unified module, on CLR and strict NativeAOT. Its first raw Image load must
record a native peak after decoder retirement; legacy luminance normalization
must count overlapping C++ scratch and native pixels. Independent mip loads must drain
without changing concurrent GZIP ownership. This does not qualify remaining utility/runtime/provider allocation,
foreign hosts, release packages or the complete managed/native monitor; see the
[component limits](../../docs/components/image-ktx.md#allocation-refusal-and-initialization).
