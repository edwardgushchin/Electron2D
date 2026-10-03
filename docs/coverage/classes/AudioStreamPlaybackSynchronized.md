# AudioStreamPlaybackSynchronized API coverage

Last updated: 2026-10-03

Godot source: [modules/interactive_music/doc_classes/AudioStreamPlaybackSynchronized.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/interactive_music/doc_classes/AudioStreamPlaybackSynchronized.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioStreamPlayback](AudioStreamPlayback.md). Electron2D type: [`public sealed class Electron2D.AudioStreamPlaybackSynchronized`](../../classes/AudioStreamPlaybackSynchronized.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioStreamPlaybackSynchronized`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/interactive_music/doc_classes/AudioStreamPlaybackSynchronized.xml) | [`public sealed class Electron2D.AudioStreamPlaybackSynchronized`](../../classes/AudioStreamPlaybackSynchronized.md) | Implemented | ADR 0047: full fixed-32-slot composition, finite live dB, coherent 128-frame stereo mixing/rate/cursors/last-child latch, metadata, transactional cohort replacement, callback/cleanup/cycle/owner guards and shallow/deep/local copies execute on current FAudio. Native opposite-wave/gain/input-owner tests, public finite-WAV Wayland GPU/compatibility hosts and warmed CPU/native measurements distinguish physical/driver/platform limits. Changed-count and partial-factory cohort defects are corrected; inherited usage/sample/parameter dependencies stay on their declaring pages. |
