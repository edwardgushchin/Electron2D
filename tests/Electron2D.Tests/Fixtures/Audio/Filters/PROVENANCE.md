# Filter conformance fixture

Last updated: 2026-10-02

`reference.json` contains 72 cases from the pinned C++ filter and Processor implementations: six ordinary response kinds, four stage counts, three cutoff/resonance/gain profiles, 256 frames and two distinct input channels each. Inputs start with 0.75/-0.5 impulses and continue a deterministic signed integer sequence. Runtime tests compare all 36864 channel samples, coefficient values, arbitrary block splits and in-place operation.

The authoritative revision is `ed1daf0bf001b61586d9930840f2f1394092c079`; [source](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/audio_filter_sw.cpp) and [header](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/audio_filter_sw.h) remain unmodified when compiled. Only missing standalone typedef/Math includes are supplied from the C++ standard library. No production C# implementation generates this oracle.

| File | SHA-256 |
| --- | --- |
| audio_filter_sw.h | `1b954ac2c428a8eba9f68b18c26d96861e5b404ce160659df5a32e44fdd5dd01` |
| audio_filter_sw.cpp | `5639b43558814dd7cfd6dbbac11af85a9b0b09631e1932eaebc21f49345c02e0` |
| reference.json | `2183bc901d3039c48b8f147376a17833782cd9e9208c9b3dbcad48fadf16650c` |

Regenerate with `python3 -B tests/Electron2D.Tests/Fixtures/Audio/Filters/generate.py`; it requires a C++17 compiler and downloads only the byte-verified source/header. The C++ driver is retained as oracle.cpp. The standard console tests consume the embedded JSON and need no compiler or network.

BandLimit is covered separately by analytical band-rejection tests because its original numerator implements the opposite band-pass response. Nyquist and signed/zero/extreme-control stability checks likewise cover deliberate numerical fixes rather than copying unsafe coefficients. [ADR 0047](../../../../../docs/decisions/audio.md#adr-0047) and [coverage](../../../../../docs/coverage/classes/AudioEffectFilter.md) record these adaptations. The runtime adaptation is covered by the existing [MIT notice](../../../../../licence/CanvasStyleGeometry-LICENSE.txt); no new vendor source or dependency is shipped.
