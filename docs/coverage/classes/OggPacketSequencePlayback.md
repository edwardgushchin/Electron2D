# OggPacketSequencePlayback API coverage

Last updated: 2026-10-02

Godot source: [modules/ogg/doc_classes/OggPacketSequencePlayback.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/ogg/doc_classes/OggPacketSequencePlayback.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: [`public sealed class Electron2D.OggPacketSequencePlayback`](../../classes/OggPacketSequencePlayback.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class OggPacketSequencePlayback`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/ogg/doc_classes/OggPacketSequencePlayback.xml) | [`public sealed class Electron2D.OggPacketSequencePlayback`](../../classes/OggPacketSequencePlayback.md) | Implemented | ADR 0047: copied mono/stereo compressed resources, validated imports, independently checked PCM, version-bound packet cursors, seek/EOF/beat loops, typed looping override and actual FAudio PCM. Warmed mixing is allocation-free; cold decoding prepares full PCM. Native allocation/platform/physical listening limits remain separate. |
