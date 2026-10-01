# AudioEffectLowShelfFilter API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AudioEffectLowShelfFilter.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectLowShelfFilter.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioEffectFilter](AudioEffectFilter.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioEffectLowShelfFilter`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectLowShelfFilter.xml) | — | Blocked | Trigger: typed AudioEffect/AudioEffectInstance resources and ordered FAPO chain ownership, bypass/enable/reorder with real processed PCM; current private meter is observation, not a public effect (ADR 0047). |
