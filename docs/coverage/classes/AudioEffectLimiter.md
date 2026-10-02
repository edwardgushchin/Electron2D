# AudioEffectLimiter API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AudioEffectLimiter.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectLimiter.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioEffect](AudioEffect.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioEffectLimiter`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectLimiter.xml) | — | Unimplemented | The generic AudioEffect/AudioEffectInstance/FAPO chain now executes (ADR 0047). Remaining executable slice: typed level/release/lookahead policy, bounded detector/delay state and verified limiter response. No absent generic mixer/chain prerequisite remains; implement the concrete resource/kernel and verify it on the existing backend. |
| [`property float ceiling_db = -0.1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectLimiter.xml) | — | Unimplemented | The generic AudioEffect/AudioEffectInstance/FAPO chain now executes (ADR 0047). Remaining executable slice: typed level/release/lookahead policy, bounded detector/delay state and verified limiter response. No absent generic mixer/chain prerequisite remains; implement the concrete resource/kernel and verify it on the existing backend. |
| [`property float soft_clip_db = 2.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectLimiter.xml) | — | Unimplemented | The generic AudioEffect/AudioEffectInstance/FAPO chain now executes (ADR 0047). Remaining executable slice: typed level/release/lookahead policy, bounded detector/delay state and verified limiter response. No absent generic mixer/chain prerequisite remains; implement the concrete resource/kernel and verify it on the existing backend. |
| [`property float soft_clip_ratio = 10.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectLimiter.xml) | — | Unimplemented | The generic AudioEffect/AudioEffectInstance/FAPO chain now executes (ADR 0047). Remaining executable slice: typed level/release/lookahead policy, bounded detector/delay state and verified limiter response. No absent generic mixer/chain prerequisite remains; implement the concrete resource/kernel and verify it on the existing backend. |
| [`property float threshold_db = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectLimiter.xml) | — | Unimplemented | The generic AudioEffect/AudioEffectInstance/FAPO chain now executes (ADR 0047). Remaining executable slice: typed level/release/lookahead policy, bounded detector/delay state and verified limiter response. No absent generic mixer/chain prerequisite remains; implement the concrete resource/kernel and verify it on the existing backend. |
