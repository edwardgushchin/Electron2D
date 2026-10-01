# AudioStreamGeneratorPlayback API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AudioStreamGeneratorPlayback.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGeneratorPlayback.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioStreamPlaybackResampled](AudioStreamPlaybackResampled.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioStreamGeneratorPlayback`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGeneratorPlayback.xml) | — | Blocked | Trigger: bounded producer/consumer PCM ring, underrun/skip accounting and typed generator playback over the now executable stream path (ADR 0047). |
| [`method can_push_buffer(int amount) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGeneratorPlayback.xml) | — | Blocked | Trigger: bounded producer/consumer PCM ring, underrun/skip accounting and typed generator playback over the now executable stream path (ADR 0047). |
| [`method clear_buffer() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGeneratorPlayback.xml) | — | Blocked | Trigger: bounded producer/consumer PCM ring, underrun/skip accounting and typed generator playback over the now executable stream path (ADR 0047). |
| [`method get_frames_available() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGeneratorPlayback.xml) | — | Blocked | Trigger: bounded producer/consumer PCM ring, underrun/skip accounting and typed generator playback over the now executable stream path (ADR 0047). |
| [`method get_skips() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGeneratorPlayback.xml) | — | Blocked | Trigger: bounded producer/consumer PCM ring, underrun/skip accounting and typed generator playback over the now executable stream path (ADR 0047). |
| [`method push_buffer(PackedVector2Array frames) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGeneratorPlayback.xml) | — | Blocked | Trigger: bounded producer/consumer PCM ring, underrun/skip accounting and typed generator playback over the now executable stream path (ADR 0047). |
| [`method push_frame(Vector2 frame) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamGeneratorPlayback.xml) | — | Blocked | Trigger: bounded producer/consumer PCM ring, underrun/skip accounting and typed generator playback over the now executable stream path (ADR 0047). |
