# Distortion conformance fixture

Last updated: 2026-10-02

`reference.json` contains five 256-frame stereo profiles at each of 44100 and 48000 Hz. A C++17 harness compiles the processor function extracted byte-for-byte from the pinned [audio_effect_distortion.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/effects/audio_effect_distortion.cpp). The standalone shim supplies the exact float denormal threshold, output rate and type fields; it does not alter the extracted function. Each profile uses distinct deterministic left/right PCM, 3 dB pre-gain, -2 dB post-gain, 4000 Hz cutoff and a mode-specific drive. The runtime test consumes the stored input and output, processing 37/69/150-frame blocks.

| File | SHA-256 |
| --- | --- |
| Pinned `audio_effect_distortion.cpp` | `633aa12deed0800c4ba738407374435a7d58fa123d53cf6e21326248495c047b` |
| `reference.json` | `4c27248c6bb5dcf19cc66436e9d0e4c9e8e2b0964e5477b8bd7df99d302ce5fa` |

Regenerate using `python3 -B tests/Electron2D.Tests/Fixtures/Audio/Distortion/generate.py` with a C++17 compiler and network access to the pinned source. Normal tests need neither. The existing [MIT adaptation notice](../../../../../licence/CanvasStyleGeometry-LICENSE.txt) accompanies the processor adaptation.
