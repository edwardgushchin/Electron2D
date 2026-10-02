# Pitch-shift conformance fixture

Last updated: 2026-10-03

`reference.json` contains stereo 256-frame/fourfold-overlap/upward and 2048-frame/eightfold-overlap/downward profiles at both 44,100 and 48,000 Hz. A C++17 shim compiles the `SMBPitchShift` processor and FFT functions extracted byte-for-byte from the pinned [audio_effect_pitch_shift.cpp](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/effects/audio_effect_pitch_shift.cpp). It supplies only the documented state arrays and math constant; it does not change the extracted functions. The runtime test processes the stored input in split and aliased blocks.

| File | SHA-256 |
| --- | --- |
| Pinned `audio_effect_pitch_shift.cpp` | `17badfd0941b6c384ecc732751c35f81bebbf63f8785850f0946612e95aeb600` |
| `reference.json` | `dcbe4c0b21bce3e2691768bb165cf2778600169dbb67205fcecdfa13238e4f6b` |

Regenerate with `python3 -B tests/Electron2D.Tests/Fixtures/Audio/PitchShift/generate.py`, a C++17 compiler and network access. Ordinary tests use only the committed JSON. The existing [Wide Open License notice](../../../../../licence/AudioFFT-WOL-LICENSE.txt) accompanies the adapted pitch and FFT routines.
