# AudioStreamMP3 API coverage

Last updated: 2026-09-23

Godot source: [modules/mp3/doc_classes/AudioStreamMP3.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mp3/doc_classes/AudioStreamMP3.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioStream](AudioStream.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioStreamMP3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mp3/doc_classes/AudioStreamMP3.xml) | — | Blocked | Trigger: pinned internal NLayer decoding, frame seeking/loop/metadata and corruption checks feeding the existing FAudio stream path (ADR 0047). |
| [`method load_from_buffer(PackedByteArray stream_data) -> AudioStreamMP3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mp3/doc_classes/AudioStreamMP3.xml) | — | Blocked | Trigger: pinned internal NLayer decoding, frame seeking/loop/metadata and corruption checks feeding the existing FAudio stream path (ADR 0047). |
| [`method load_from_file(String path) -> AudioStreamMP3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mp3/doc_classes/AudioStreamMP3.xml) | — | Blocked | Trigger: pinned internal NLayer decoding, frame seeking/loop/metadata and corruption checks feeding the existing FAudio stream path (ADR 0047). |
| [`property int bar_beats = 4`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mp3/doc_classes/AudioStreamMP3.xml) | — | Blocked | Trigger: pinned internal NLayer decoding, frame seeking/loop/metadata and corruption checks feeding the existing FAudio stream path (ADR 0047). |
| [`property int beat_count = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mp3/doc_classes/AudioStreamMP3.xml) | — | Blocked | Trigger: pinned internal NLayer decoding, frame seeking/loop/metadata and corruption checks feeding the existing FAudio stream path (ADR 0047). |
| [`property float bpm = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mp3/doc_classes/AudioStreamMP3.xml) | — | Blocked | Trigger: pinned internal NLayer decoding, frame seeking/loop/metadata and corruption checks feeding the existing FAudio stream path (ADR 0047). |
| [`property PackedByteArray data = PackedByteArray()`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mp3/doc_classes/AudioStreamMP3.xml) | — | Blocked | Trigger: pinned internal NLayer decoding, frame seeking/loop/metadata and corruption checks feeding the existing FAudio stream path (ADR 0047). |
| [`property bool loop = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mp3/doc_classes/AudioStreamMP3.xml) | — | Blocked | Trigger: pinned internal NLayer decoding, frame seeking/loop/metadata and corruption checks feeding the existing FAudio stream path (ADR 0047). |
| [`property float loop_offset = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mp3/doc_classes/AudioStreamMP3.xml) | — | Blocked | Trigger: pinned internal NLayer decoding, frame seeking/loop/metadata and corruption checks feeding the existing FAudio stream path (ADR 0047). |
