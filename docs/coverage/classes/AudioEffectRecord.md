# AudioEffectRecord API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AudioEffectRecord.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectRecord.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioEffect](AudioEffect.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioEffectRecord`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectRecord.xml) | — | Unimplemented | The generic AudioEffect/AudioEffectInstance/FAPO chain now executes (ADR 0047). Remaining executable slice: typed record state, bounded captured PCM lifetime and output WAV resource/format encoding. No absent generic mixer/chain prerequisite remains; implement the concrete resource/kernel and verify it on the existing backend. |
| [`method get_recording() -> AudioStreamWAV`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectRecord.xml) | — | Unimplemented | The generic AudioEffect/AudioEffectInstance/FAPO chain now executes (ADR 0047). Remaining executable slice: typed record state, bounded captured PCM lifetime and output WAV resource/format encoding. No absent generic mixer/chain prerequisite remains; implement the concrete resource/kernel and verify it on the existing backend. |
| [`method is_recording_active() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectRecord.xml) | — | Unimplemented | The generic AudioEffect/AudioEffectInstance/FAPO chain now executes (ADR 0047). Remaining executable slice: typed record state, bounded captured PCM lifetime and output WAV resource/format encoding. No absent generic mixer/chain prerequisite remains; implement the concrete resource/kernel and verify it on the existing backend. |
| [`method set_recording_active(bool record) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectRecord.xml) | — | Unimplemented | The generic AudioEffect/AudioEffectInstance/FAPO chain now executes (ADR 0047). Remaining executable slice: typed record state, bounded captured PCM lifetime and output WAV resource/format encoding. No absent generic mixer/chain prerequisite remains; implement the concrete resource/kernel and verify it on the existing backend. |
| [`property int format [AudioStreamWAV.Format] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectRecord.xml) | — | Unimplemented | The generic AudioEffect/AudioEffectInstance/FAPO chain now executes (ADR 0047). Remaining executable slice: typed record state, bounded captured PCM lifetime and output WAV resource/format encoding. No absent generic mixer/chain prerequisite remains; implement the concrete resource/kernel and verify it on the existing backend. |
