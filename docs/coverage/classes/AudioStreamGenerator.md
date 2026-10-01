# AudioStreamGenerator API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AudioStreamGenerator.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGenerator.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioStream](AudioStream.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioStreamGenerator`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGenerator.xml) | — | Blocked | Trigger: bounded producer/consumer PCM ring, underrun/skip accounting and typed generator playback over the now executable stream path (ADR 0047). |
| [`enum AudioStreamGeneratorMixRate`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGenerator.xml) | — | Blocked | Trigger: bounded producer/consumer PCM ring, underrun/skip accounting and typed generator playback over the now executable stream path (ADR 0047). |
| [`enum_value MIX_RATE_CUSTOM [AudioStreamGeneratorMixRate] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGenerator.xml) | — | Blocked | Trigger: bounded producer/consumer PCM ring, underrun/skip accounting and typed generator playback over the now executable stream path (ADR 0047). |
| [`enum_value MIX_RATE_INPUT [AudioStreamGeneratorMixRate] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGenerator.xml) | — | Blocked | Trigger: bounded producer/consumer PCM ring, underrun/skip accounting and typed generator playback over the now executable stream path (ADR 0047). |
| [`enum_value MIX_RATE_MAX [AudioStreamGeneratorMixRate] = 3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGenerator.xml) | — | Blocked | Trigger: bounded producer/consumer PCM ring, underrun/skip accounting and typed generator playback over the now executable stream path (ADR 0047). |
| [`enum_value MIX_RATE_OUTPUT [AudioStreamGeneratorMixRate] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGenerator.xml) | — | Blocked | Trigger: bounded producer/consumer PCM ring, underrun/skip accounting and typed generator playback over the now executable stream path (ADR 0047). |
| [`property float buffer_length = 0.5`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGenerator.xml) | — | Blocked | Trigger: bounded producer/consumer PCM ring, underrun/skip accounting and typed generator playback over the now executable stream path (ADR 0047). |
| [`property float mix_rate = 44100.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGenerator.xml) | — | Blocked | Trigger: bounded producer/consumer PCM ring, underrun/skip accounting and typed generator playback over the now executable stream path (ADR 0047). |
| [`property int mix_rate_mode [AudioStreamGenerator.AudioStreamGeneratorMixRate] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGenerator.xml) | — | Blocked | Trigger: bounded producer/consumer PCM ring, underrun/skip accounting and typed generator playback over the now executable stream path (ADR 0047). |
