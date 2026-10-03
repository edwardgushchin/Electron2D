# Compressor conformance fixture

Last updated: 2026-10-03

The generator compiles the process function extracted byte-for-byte from pinned [audio_effect_compressor.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/effects/audio_effect_compressor.cpp). The C++ shim provides float AudioFrame operators, math, rate and named detector data, including missing-name Master fallback. Seven 1,024-frame stereo profiles at each of 44,100 and 48,000 Hz contain default/slow/mixed/expanded/raw-negative-ratio/self and named detector cases. There are 28,672 channel samples overall. Ordinary tests check seven profiles/14,336 samples at the current actual/default engine rate; the separate native graph checks verify bus ordering, source/effect/gain/sends and lookup lifetime. Ordinary tests need no network or C++ compiler.

| File | SHA-256 |
| --- | --- |
| Pinned `audio_effect_compressor.cpp` | `d073aa69a35842e29a46bcf4fdd326920ff595007dad1f6d29a9646c86e5106e` |
| Pinned `audio_effect_compressor.h` | `171d6e5bed5c450d6956f68fc643eddb38db4ae32ac86dfc4a9361ddf86f5b0a` |
| Pinned `audio_server.cpp` used to audit lookup/order | `d7641ef5852aa86f75c4d506759de2838a2c3886c29f7eb25fd9e9310d87c102` |
| `reference.json` | `869a422416296802e2b467bc12b65fed6afb23ade078a469fad1dc6ddc5cd50a` |

Regenerate with `python3 -B tests/Electron2D.Tests/Fixtures/Audio/Compressor/generate.py`. The runtime omits followers/meters that never affect the source's published PCM; it preserves the linked envelope equation. Zero input/timing and log-domain extreme-threshold guards prevent nonfinite contamination; overflow resets state. These edges are checked separately. The existing [MIT runtime adaptation notice](../../../../../licence/CanvasStyleGeometry-LICENSE.txt) accompanies the equations and processing-point buffer adaptation.
