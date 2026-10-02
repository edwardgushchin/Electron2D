# AudioEffectBandLimitFilter API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AudioEffectBandLimitFilter.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectBandLimitFilter.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioEffectFilter](AudioEffectFilter.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioEffectBandLimitFilter`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectBandLimitFilter.xml) | — | Unimplemented | The generic AudioEffect/AudioEffectInstance/FAPO chain now executes (ADR 0047). Remaining executable slice: typed filter coefficients/slope/resonance/gain state and verified stable stereo IIR kernels/defaults. No absent generic mixer/chain prerequisite remains; implement the concrete resource/kernel and verify it on the existing backend. |
