# AudioStreamPolyphonic API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AudioStreamPolyphonic.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamPolyphonic.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioStream](AudioStream.md). Electron2D type: [`public sealed class Electron2D.AudioStreamPolyphonic`](../../classes/AudioStreamPolyphonic.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioStreamPolyphonic`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamPolyphonic.xml) | [`public sealed class Electron2D.AudioStreamPolyphonic`](../../classes/AudioStreamPolyphonic.md) | Implemented | ADR 0047: dynamic zero-through-128 creation-time voices, parent-local Int64 IDs, typed Stream/Sample/default and bus controls, prepared streamed PCM/ramp/pending/EOF semantics, native sample gain/pitch/parent lifecycle, input queues and owned cleanup execute. AudioPolyphonicTests covers boundaries/copies/failures, actual FAudio, public hosts and warmed CPU/native allocation. Immediate stop IDs, prior gain and native controls/stop/EOF defects are corrected explicitly; physical/hardware/other-platform verification remains separate. |
| [`property int polyphony = 32`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamPolyphonic.xml) | [`public System.Int32 Polyphony { get; set; }`](../../classes/AudioStreamPolyphonic.md) | Implemented | ADR 0047: dynamic zero-through-128 creation-time voices, parent-local Int64 IDs, typed Stream/Sample/default and bus controls, prepared streamed PCM/ramp/pending/EOF semantics, native sample gain/pitch/parent lifecycle, input queues and owned cleanup execute. AudioPolyphonicTests covers boundaries/copies/failures, actual FAudio, public hosts and warmed CPU/native allocation. Immediate stop IDs, prior gain and native controls/stop/EOF defects are corrected explicitly; physical/hardware/other-platform verification remains separate. |
