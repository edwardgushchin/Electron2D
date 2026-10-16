# Private KTX decoder

The bridge loads flat KTX1/KTX2 images with complete mip chains and transcodes Basis LDR/HDR to portable pixels. Imported array assets also decode legacy .basis channel roles through e2d_basis_create. Both creators share e2d_ktx_level/e2d_ktx_destroy ownership. The fifth export e2d_block_decode supplies ETC1/ETC2/EAC and ASTC pixels to Image and texture snapshots. The ABI remains internal.

CMake pins KTX-Software 5.0.0-rc2 by SHA-256 and builds its read-only static closure. Platform uploads, tools, optional SIMD requirements and the Ericsson ETC software decoder are disabled. The original archive remains unchanged. `decoder-guards.cmake` applies an exact-preimage guard to generated code so failed Basis palette/table decoding returns an error before sampling empty tables. A shared once-only initializer coordinates cold KTX and legacy Basis use. Zstandard source/declarations receive a mechanical private symbol namespace to coexist with the font static archives. License texts remain unchanged in `licence/`.

```bash
cmake -S tools/ktx-native -B obj/ktx-native -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build obj/ktx-native --target Electron2DKTXCheck --parallel 2
python3 -B tools/ktx-native/test_bridge.py obj/ktx-native/Electron2DKTXCheck tests/Electron2D.Tests/TestData/KTX/oracle.json
python3 -B tools/ktx-native/test_bridge.py obj/ktx-native/Electron2DKTXCheck tests/Electron2D.Tests/TestData/CompressedArray/basis-oracle.json
```

`-DELECTRON2D_STATIC_NATIVE=ON` produces one merged archive; no separate codec archive is needed by a consumer. The native check validates owner/input/output failure paths and compares all mips against the independent tool oracles. Set `ELECTRON2D_KTX_REPRO` to an ignored output path to retain the last deterministic mutation when diagnosing a test failure.

`generate_fixtures.py` regenerates the small test corpus with the official standalone 5.0.0-rc2 `ktx` tool. It records the tool hash and creates raw, Zstandard, ZLIB, ETC1S, UASTC LDR and two HDR profiles, plus corrupted codebook/DFD regression inputs. Only fixtures and recipe belong in Git; downloaded tools, archives and build products stay ignored.

`generate_basis_fixtures.py BASISU EXR_ORACLE OUTPUT_JSON` uses the pinned standalone Basis tool and independent OpenEXR reader for legacy ETC1S/UASTC LDR/HDR mip oracles. Tests cover every truncation, unaligned borrowed input, checksum-repaired payload mutations and mixed cold initialization (`--mixed-oracle PATH` with the KTX oracle). The adapter privately materializes aligned legacy slices within 64 MiB; no codec source modification is needed for unaligned container offsets.

Block decoding adds the ISC detex subset at cab11584d9be140602a66fd9a88ef0b99f08a97a. Exact-preimage generated arithmetic corrections replace negative signed shifts with multiplication and overflowing signed selector shifts with unsigned masks. ASTC uses the scalar astcenc closure already merged into ktx_read. No extra codec archive is needed. Source and conservative RGBA-float expansion are each bounded to 64 MiB. HDR half output requires two-byte alignment; borrowed source bytes need no alignment.

Build `Electron2DBlockCheck`, then run `python3 -B tools/ktx-native/test_blocks.py PATH_TO_CHECK tests/Electron2D.Tests/TestData/Blocks/oracle.json`. The same corpus runs against shared, merged static and ASan/UBSan/leak builds. Native tests cover every mip, exact lengths, guards, 9216 block mutations and 1152 concurrent calls. RA-as-RG swizzling belongs to the managed boundary.

To regenerate block fixtures, build `etc-oracle/Oracle.csproj` with `-p:EtcOracleSource=PATH` pointing at the AssetRipper.TextureDecoder project from commit 13b4f6ee4906877402ab69fc5a0b51b83c15db44. That standalone oracle compiles upstream test-only source without packages. Build the standalone scalar astcenc CLI from the pinned KTX archive. Then run `generate_block_fixtures.py ETC_ORACLE_DLL ASTCENC EXR_ORACLE OUTPUT_JSON`. ETC byte pixels come from the independent oracle, signed EAC from explicit numerical vectors, and ASTC pixels from the official CLI plus OpenEXR. The fixture records tool hashes; these tools are absent from the runtime and packages.

The generated detex header selects __forceinline for the MSVC producer and retains the original inline spelling for other compilers. Source copies include that header consistently; this mechanical adaptation does not change decoder algorithms. Windows execution still requires its own verification.
