# Phaser conformance fixture

Last updated: 2026-10-03

`reference.json` contains two 4,096-frame stereo profiles at each of 44,100 and 48,000 Hz. A C++17 shim compiles the process function extracted byte-for-byte from pinned [audio_effect_phaser.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/effects/audio_effect_phaser.cpp). The shim supplies the six-stage all-pass state and resource defaults from the paired pinned header; it does not modify the extracted process function. One profile uses defaults and the other uses faster modulation, stronger feedback and a wider frequency sweep. Distinct left/right waveforms exercise independent histories.

| File | SHA-256 |
| --- | --- |
| Pinned `audio_effect_phaser.cpp` | `b35c05757aaa26316f159523d9647109ee1c0c31a013c07ccbe30c559c4c3dd2` |
| `reference.json` | `a2b67ce317a04c57eb3b89769214d5a49746250ec3201cf75d09cf495088318b` |

Regenerate with `python3 -B tests/Electron2D.Tests/Fixtures/Audio/Phaser/generate.py`, a C++17 compiler and network access to the pinned source. Ordinary tests use only the committed JSON. The current engine output was observed at 44,100 Hz; the stored 48,000 Hz cases are not a claim that the engine executed that rate. The existing [MIT runtime adaptation notice](../../../../../licence/CanvasStyleGeometry-LICENSE.txt) accompanies the adapted processor.
