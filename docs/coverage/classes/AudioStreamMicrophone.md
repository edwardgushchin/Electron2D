# AudioStreamMicrophone API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AudioStreamMicrophone.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamMicrophone.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioStream](AudioStream.md). Electron2D type: [`public sealed class Electron2D.AudioStreamMicrophone`](../../classes/AudioStreamMicrophone.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioStreamMicrophone`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamMicrophone.xml) | [`public sealed class Electron2D.AudioStreamMicrophone`](../../classes/AudioStreamMicrophone.md) | Implemented | ADR 0047: actual paused SDL3 input frequency, gated live recording, bounded four-quantum finite stereo ring, full/overflow-safe independent server/microphone cursors, 50 ms priming, continuous underrun silence, global explicit pause, multi-playback automatic request ownership, transactional device switching, copied resource graphs and engine cleanup. Input generator mode prepares/uses real input frequency without recording. Linux dummy input callbacks, native converted PCM/FAudio output, concurrent/warm active/stopped work and public Wayland GPU/compatibility hosts are verified; physical microphones, permissions, external native allocation totals and other platforms remain unverified. Typed errors replace return codes; invalid device names reject. Monotonic cursors correct modulo-full ambiguity/stale replay and automatic Stop retains other microphone requests, while explicit server false still pauses global input. |
