# AudioEffectEQ10 API coverage

Last updated: 2026-10-02

Godot source: [doc/classes/AudioEffectEQ10.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectEQ10.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioEffectEQ](AudioEffectEQ.md). Electron2D type: [`public sealed class Electron2D.AudioEffectEQ10`](../../classes/AudioEffectEQ10.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioEffectEQ10`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectEQ10.xml) | [`public sealed class Electron2D.AudioEffectEQ10`](../../classes/AudioEffectEQ10.md) | Implemented | ADR 0047: fixed 6/10/21-band equalizers execute through independent prepared stereo filter histories in the FAudio bus chain. Indexed finite DB gains, typed Resource/scene-local copies, live edits, 1,536 byte-pinned C++ PCM samples plus frequency-selective CPU/native output, public Wayland hosts and warmed zero-allocation checks pass; physical listening and other platforms remain unverified. |
