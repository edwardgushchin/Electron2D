# AudioEffectSpectrumAnalyzerInstance API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AudioEffectSpectrumAnalyzerInstance.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectSpectrumAnalyzerInstance.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioEffectInstance](AudioEffectInstance.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioEffectSpectrumAnalyzerInstance`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectSpectrumAnalyzerInstance.xml) | — | Blocked | Trigger: typed AudioEffect/AudioEffectInstance resources and ordered FAPO chain ownership, bypass/enable/reorder with real processed PCM; current private meter is observation, not a public effect (ADR 0047). |
| [`enum MagnitudeMode`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectSpectrumAnalyzerInstance.xml) | — | Blocked | Trigger: typed AudioEffect/AudioEffectInstance resources and ordered FAPO chain ownership, bypass/enable/reorder with real processed PCM; current private meter is observation, not a public effect (ADR 0047). |
| [`enum_value MAGNITUDE_AVERAGE [MagnitudeMode] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectSpectrumAnalyzerInstance.xml) | — | Blocked | Trigger: typed AudioEffect/AudioEffectInstance resources and ordered FAPO chain ownership, bypass/enable/reorder with real processed PCM; current private meter is observation, not a public effect (ADR 0047). |
| [`enum_value MAGNITUDE_MAX [MagnitudeMode] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectSpectrumAnalyzerInstance.xml) | — | Blocked | Trigger: typed AudioEffect/AudioEffectInstance resources and ordered FAPO chain ownership, bypass/enable/reorder with real processed PCM; current private meter is observation, not a public effect (ADR 0047). |
| [`method get_magnitude_for_frequency_range(float from_hz, float to_hz, int mode [AudioEffectSpectrumAnalyzerInstance.MagnitudeMode] = 1) -> Vector2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectSpectrumAnalyzerInstance.xml) | — | Blocked | Trigger: typed AudioEffect/AudioEffectInstance resources and ordered FAPO chain ownership, bypass/enable/reorder with real processed PCM; current private meter is observation, not a public effect (ADR 0047). |
