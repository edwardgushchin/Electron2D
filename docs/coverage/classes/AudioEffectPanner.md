# AudioEffectPanner API coverage

Last updated: 2026-10-02

Godot source: [doc/classes/AudioEffectPanner.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectPanner.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioEffect](AudioEffect.md). Electron2D type: [`public sealed class Electron2D.AudioEffectPanner`](../../classes/AudioEffectPanner.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioEffectPanner`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectPanner.xml) | [`public sealed class Electron2D.AudioEffectPanner`](../../classes/AudioEffectPanner.md) | Implemented | ADR 0047: executable scalar gain and stereo pan resources in the existing FAudio bus chain; live block-ramped gain, channel fold, typed copies and native PCM/zero-allocation checks. Finite gain bounds and processing clamp retain safe typed PCM; physical listening and other platforms remain unverified. |
| [`property float pan = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectPanner.xml) | [`public System.Single Pan { get; set; }`](../../classes/AudioEffectPanner.md) | Implemented | ADR 0047: executable scalar gain and stereo pan resources in the existing FAudio bus chain; live block-ramped gain, channel fold, typed copies and native PCM/zero-allocation checks. Finite gain bounds and processing clamp retain safe typed PCM; physical listening and other platforms remain unverified. |
