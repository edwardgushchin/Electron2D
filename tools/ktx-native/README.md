# Private KTX decoder

The bridge loads flat KTX1/KTX2 images with complete mip chains and transcodes Basis LDR/HDR to portable pixels. Imported array assets also decode legacy .basis channel roles through e2d_basis_create. Both creators share e2d_ktx_level/e2d_ktx_destroy ownership; the four-export ABI remains internal. Runtime consumers use Image and CompressedTextureArray.

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
