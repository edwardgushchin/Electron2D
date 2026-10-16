# VideoStreamTheora API coverage

Last updated: 2026-10-16

Godot source: [modules/theora/doc_classes/VideoStreamTheora.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/theora/doc_classes/VideoStreamTheora.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [VideoStream](VideoStream.md). Electron2D type: [`public sealed class Electron2D.VideoStreamTheora`](../../classes/VideoStreamTheora.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VideoStreamTheora`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/theora/doc_classes/VideoStreamTheora.xml) | [`public sealed class Electron2D.VideoStreamTheora`](../../classes/VideoStreamTheora.md) | Implemented | ADR 0110: executable typed source/decoder/control family, prepared Theora frames and bounded Vorbis PCM over existing resampling/FAudio, both canvas backends, scene clocks/pause/seek/loop/completion, transactional ownership and exact archive factories. Independent pixel/PCM, malformed input, prepared allocation, fresh-process authoring and local Linux Wayland verification are recorded in docs/components/video-playback.md; foreign execution and physical listening remain separate gates. |
