# Private KTX decoder

The bridge loads flat KTX1/KTX2 images with complete mip chains and transcodes Basis LDR/HDR to portable pixels. Runtime consumers use Image; this ABI is internal.

CMake pins KTX-Software 5.0.0-rc2 by SHA-256 and builds its read-only static closure. Platform uploads, tools, optional SIMD requirements and the Ericsson ETC software decoder are disabled. The original archive remains unchanged. `decoder-guards.cmake` applies an exact-preimage guard to generated code so failed Basis palette/table decoding returns an error before sampling empty tables. Zstandard source/declarations receive a mechanical private symbol namespace to coexist with the font static archives. License texts remain unchanged in `licence/`.

```bash
cmake -S tools/ktx-native -B obj/ktx-native -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build obj/ktx-native --target Electron2DKTXCheck --parallel 2
python3 -B tools/ktx-native/test_bridge.py obj/ktx-native/Electron2DKTXCheck tests/Electron2D.Tests/TestData/KTX/oracle.json
```

`-DELECTRON2D_STATIC_NATIVE=ON` produces one merged archive; no separate codec archive is needed by a consumer. The native check validates owner/input/output failure paths and compares all mips against the independent tool oracles. Set `ELECTRON2D_KTX_REPRO` to an ignored output path to retain the last deterministic mutation when diagnosing a test failure.

`generate_fixtures.py` regenerates the small test corpus with the official standalone 5.0.0-rc2 `ktx` tool. It records the tool hash and creates raw, Zstandard, ZLIB, ETC1S, UASTC LDR and two HDR profiles, plus corrupted codebook/DFD regression inputs. Only fixtures and recipe belong in Git; downloaded tools, archives and build products stay ignored.
