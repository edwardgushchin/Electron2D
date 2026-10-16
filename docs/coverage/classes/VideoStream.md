# VideoStream API coverage

Last updated: 2026-10-16

Godot source: [doc/classes/VideoStream.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/VideoStream.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: [`public abstract class Electron2D.VideoStream`](../../classes/VideoStream.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VideoStream`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/VideoStream.xml) | [`public abstract class Electron2D.VideoStream`](../../classes/VideoStream.md) | Implemented | ADR 0110: executable typed source/decoder/control family, prepared Theora frames and bounded Vorbis PCM over existing resampling/FAudio, both canvas backends, scene clocks/pause/seek/loop/completion, transactional ownership and exact archive factories. Independent pixel/PCM, malformed input, prepared allocation, fresh-process authoring and local Linux Wayland verification are recorded in docs/components/video-playback.md; foreign execution and physical listening remain separate gates. |
| [`method _instantiate_playback() -> VideoStreamPlayback`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/VideoStream.xml) | [`protected abstract Electron2D.VideoStreamPlayback OnInstantiatePlayback()`](../../classes/VideoStream.md) | Implemented | ADR 0110: executable typed source/decoder/control family, prepared Theora frames and bounded Vorbis PCM over existing resampling/FAudio, both canvas backends, scene clocks/pause/seek/loop/completion, transactional ownership and exact archive factories. Independent pixel/PCM, malformed input, prepared allocation, fresh-process authoring and local Linux Wayland verification are recorded in docs/components/video-playback.md; foreign execution and physical listening remain separate gates. |
| [`property String file = ""`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/VideoStream.xml) | [`public System.String File { get; set; }`](../../classes/VideoStream.md) | Implemented | ADR 0110: executable typed source/decoder/control family, prepared Theora frames and bounded Vorbis PCM over existing resampling/FAudio, both canvas backends, scene clocks/pause/seek/loop/completion, transactional ownership and exact archive factories. Independent pixel/PCM, malformed input, prepared allocation, fresh-process authoring and local Linux Wayland verification are recorded in docs/components/video-playback.md; foreign execution and physical listening remain separate gates. |
