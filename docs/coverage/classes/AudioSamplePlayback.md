# AudioSamplePlayback API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AudioSamplePlayback.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioSamplePlayback.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: [`public sealed class Electron2D.AudioSamplePlayback`](../../classes/AudioSamplePlayback.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioSamplePlayback`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioSamplePlayback.xml) | [`public sealed class Electron2D.AudioSamplePlayback`](../../classes/AudioSamplePlayback.md) | Implemented | ADR 0047/0051: immutable mono/stereo PCM snapshots, typed ownership and native complete-buffer voices execute for WAV/MP3/Vorbis, scene players/emitter and standalone associations. Cold registration, live offset/pitch/gains/bus/looping, defaults, cleanup and failure containment are verified; physical listening and other platforms remain unverified. |
