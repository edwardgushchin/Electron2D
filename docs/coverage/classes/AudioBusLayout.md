# AudioBusLayout API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AudioBusLayout.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioBusLayout.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: [`public sealed class Electron2D.AudioBusLayout`](../../classes/AudioBusLayout.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioBusLayout`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioBusLayout.xml) | [`public sealed class Electron2D.AudioBusLayout`](../../classes/AudioBusLayout.md) | Implemented | ADR 0047: typed bus snapshots preserve order, sends, gain, solo/mute/bypass and borrowed ordered effect identities. Built-in archives save/load all 27 concrete DSP resources and aliases, including default project loading before MainLoop/Window autoplay. Live Stream/Sample replacement preserves playback, resets effect histories, retains internal file graphs and prepares factories before commit; invalid data/factory failure rolls back, cleanup/notification reports after commitment. AudioBusLayoutTests verifies archives, fresh-process execution, native PCM and 64 warmed passes; public GPU/compatibility hosts execute. Physical listening, other-target execution and editor UI remain separate. |
