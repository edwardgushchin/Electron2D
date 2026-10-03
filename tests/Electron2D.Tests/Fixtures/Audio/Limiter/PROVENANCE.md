# Soft limiter conformance fixture

Last updated: 2026-10-03

The generator compiles `AudioEffectLimiterInstance::process` extracted byte-for-byte from pinned [audio_effect_limiter.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/effects/audio_effect_limiter.cpp). A minimal C++17 shim provides the stereo frame, float math and settings. Eight profiles contain 256 stereo frames each (4,096 channel samples overall), including defaults, ordinary range endpoints, compensated gain, raw values beyond authoring hints and ratio-only variations. The processor has no rate dependency. Ordinary tests use committed JSON and require no network/compiler.

| File | SHA-256 |
| --- | --- |
| Pinned `audio_effect_limiter.cpp` | `b3159a4736e10e333aff60d20c76223ea6e23918b9cf1bcf2e91010a94a6faa4` |
| Pinned `audio_effect_limiter.h` | `c98283832cc06eecae4396c37c0d4b49e863b70936b40672fcf0ba2f80404a4b` |
| `reference.json` | `d6ba5ef7c7ee0c407d38f95cecd503c8feca8f4ffaebffb9514bcb8ee9a15d5a` |

Regenerate with `python3 -B tests/Electron2D.Tests/Fixtures/Audio/Limiter/generate.py`, a C++17 compiler and access to the pinned source. The runtime uses widened intermediate math and skips an ineffective soft branch at/above the final ceiling, preventing finite raw-control singularities and silent-input overflow. Separate boundary checks cover those corrections; ordinary oracle PCM permits one millionth absolute error. The existing [MIT runtime adaptation notice](../../../../../licence/CanvasStyleGeometry-LICENSE.txt) accompanies the adapted transfer curve.
