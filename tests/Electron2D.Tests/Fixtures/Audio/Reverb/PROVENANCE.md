# Reverb conformance fixture

Last updated: 2026-10-02

`reference.json` contains three 4096-frame stereo profiles from the pinned unmodified C++ `reverb_filter` implementation. The profiles cover defaults, zero-spread/high-pass/short predelay with active feedback, and high damping/maximum predelay. Deterministic distinct-channel input begins with 0.75/-0.5 impulses. Runtime tests compare 24576 channel samples after the same 113/257/1024 frame split schedule.

The authoritative revision is `ed1daf0bf001b61586d9930840f2f1394092c079`: [source](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/effects/reverb_filter.cpp) and [header](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/audio/effects/reverb_filter.h). The C++ pair is downloaded byte-for-byte; only standalone math, audio-frame denormal and allocation shims are supplied. No production C# implementation generates this oracle.

| File | SHA-256 |
| --- | --- |
| reverb_filter.cpp | `d09bd31bdc4291cff8a0d18579690c531c9fc99ed3eafced19696a3052f6b806` |
| reverb_filter.h | `8b90f4990d0cdb2cf6adcb21a803028f79e462519a55fd3a5958d7957828ad9b` |
| reference.json | `349a51854dfcf9568f459a226162f1e3d214e83b39515e33ee5205a67bd42f77` |

Regenerate with `python3 -B tests/Electron2D.Tests/Fixtures/Audio/Reverb/generate.py`; this requires a C++17 compiler and network access to the pinned source. `oracle.cpp` is retained. Normal test runs consume the embedded fixture and need neither compiler nor network. Separate analytical tests cover zero/full spread, dry-only output, actual native tail, overflow recovery and warmed allocation. The adaptation uses the existing [MIT runtime notice](../../../../../licence/CanvasStyleGeometry-LICENSE.txt).
