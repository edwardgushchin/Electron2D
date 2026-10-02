# AudioEffectEQ API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AudioEffectEQ.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectEQ.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioEffect](AudioEffect.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioEffectEQ`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectEQ.xml) | — | Unimplemented | The generic AudioEffect/AudioEffectInstance/FAPO chain now executes (ADR 0047). Remaining executable slice: fixed band layouts, indexed DB gains and prepared per-band stereo filter state. No absent generic mixer/chain prerequisite remains; implement the concrete resource/kernel and verify it on the existing backend. |
| [`method get_band_count() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectEQ.xml) | — | Unimplemented | The generic AudioEffect/AudioEffectInstance/FAPO chain now executes (ADR 0047). Remaining executable slice: fixed band layouts, indexed DB gains and prepared per-band stereo filter state. No absent generic mixer/chain prerequisite remains; implement the concrete resource/kernel and verify it on the existing backend. |
| [`method get_band_gain_db(int band_idx) -> float`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectEQ.xml) | — | Unimplemented | The generic AudioEffect/AudioEffectInstance/FAPO chain now executes (ADR 0047). Remaining executable slice: fixed band layouts, indexed DB gains and prepared per-band stereo filter state. No absent generic mixer/chain prerequisite remains; implement the concrete resource/kernel and verify it on the existing backend. |
| [`method set_band_gain_db(int band_idx, float volume_db) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectEQ.xml) | — | Unimplemented | The generic AudioEffect/AudioEffectInstance/FAPO chain now executes (ADR 0047). Remaining executable slice: fixed band layouts, indexed DB gains and prepared per-band stereo filter state. No absent generic mixer/chain prerequisite remains; implement the concrete resource/kernel and verify it on the existing backend. |
