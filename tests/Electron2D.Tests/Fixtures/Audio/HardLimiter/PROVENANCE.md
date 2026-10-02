# Hard limiter conformance fixture

Last updated: 2026-10-03

`reference.json` contains two 4,096-frame stereo profiles at each of 44,100 and 48,000 Hz. A C++17 shim compiles the processor function extracted byte-for-byte from the pinned [audio_effect_hard_limiter.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/effects/audio_effect_hard_limiter.cpp). The shim prepares the source's two-millisecond delay and gain buckets and supplies exact float defaults; it does not modify the extracted process function. Distinct left/right signals include recurring peaks and a shorter-release, pre-gain profile. The runtime test processes the stored input in split and aliased blocks.

| File | SHA-256 |
| --- | --- |
| Pinned `audio_effect_hard_limiter.cpp` | `3a3f5e8679d47705bcd65efc59d2779fddbd531c78d029dae17fc5d35952747e` |
| `reference.json` | `a754d6cc3654431e5e541949a14bead3476be96afcf008231855c42a8a5bc6b9` |

Regenerate with `python3 -B tests/Electron2D.Tests/Fixtures/Audio/HardLimiter/generate.py`, a C++17 compiler and network access to the pinned source. Ordinary tests use only the committed JSON. The runtime corrects source-level sample-ceiling overshoot (up to 0.0001282 in this fixture) and incomplete gain release at sub-sample release times; oracle comparison therefore allows 0.0003 absolute error while separate tests require an exact ceiling and full recovery. The existing [MIT runtime adaptation notice](../../../../../licence/CanvasStyleGeometry-LICENSE.txt) accompanies the processor adaptation.
