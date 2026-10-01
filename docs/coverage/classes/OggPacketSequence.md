# OggPacketSequence API coverage

Last updated: 2026-09-23

Godot source: [modules/ogg/doc_classes/OggPacketSequence.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/ogg/doc_classes/OggPacketSequence.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class OggPacketSequence`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/ogg/doc_classes/OggPacketSequence.xml) | — | Blocked | Trigger: pinned internal NVorbis decoding plus Ogg packet/seek/loop/metadata resources feeding the existing FAudio stream path (ADR 0047). |
| [`method get_length() -> float`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/ogg/doc_classes/OggPacketSequence.xml) | — | Blocked | Trigger: pinned internal NVorbis decoding plus Ogg packet/seek/loop/metadata resources feeding the existing FAudio stream path (ADR 0047). |
| [`property PackedInt64Array granule_positions = PackedInt64Array()`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/ogg/doc_classes/OggPacketSequence.xml) | — | Blocked | Trigger: pinned internal NVorbis decoding plus Ogg packet/seek/loop/metadata resources feeding the existing FAudio stream path (ADR 0047). |
| [`property Array[] packet_data = []`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/ogg/doc_classes/OggPacketSequence.xml) | — | Blocked | Trigger: pinned internal NVorbis decoding plus Ogg packet/seek/loop/metadata resources feeding the existing FAudio stream path (ADR 0047). |
| [`property float sampling_rate = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/ogg/doc_classes/OggPacketSequence.xml) | — | Blocked | Trigger: pinned internal NVorbis decoding plus Ogg packet/seek/loop/metadata resources feeding the existing FAudio stream path (ADR 0047). |
