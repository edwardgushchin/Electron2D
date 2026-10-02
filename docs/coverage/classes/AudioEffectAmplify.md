# AudioEffectAmplify API coverage

Last updated: 2026-10-02

Godot source: [doc/classes/AudioEffectAmplify.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectAmplify.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioEffect](AudioEffect.md). Electron2D type: [`public sealed class Electron2D.AudioEffectAmplify`](../../classes/AudioEffectAmplify.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioEffectAmplify`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectAmplify.xml) | [`public sealed class Electron2D.AudioEffectAmplify`](../../classes/AudioEffectAmplify.md) | Implemented | ADR 0047: executable scalar gain and stereo pan resources in the existing FAudio bus chain; live block-ramped gain, channel fold, typed copies and native PCM/zero-allocation checks. Finite gain bounds and processing clamp retain safe typed PCM; physical listening and other platforms remain unverified. |
| [`property float volume_db = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectAmplify.xml) | [`public System.Single VolumeDB { get; set; }`](../../classes/AudioEffectAmplify.md) | Implemented | ADR 0047: executable scalar gain and stereo pan resources in the existing FAudio bus chain; live block-ramped gain, channel fold, typed copies and native PCM/zero-allocation checks. Finite gain bounds and processing clamp retain safe typed PCM; physical listening and other platforms remain unverified. |
| [`property float volume_linear`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectAmplify.xml) | [`public System.Single VolumeLinear { get; set; }`](../../classes/AudioEffectAmplify.md) | Implemented | ADR 0047: executable scalar gain and stereo pan resources in the existing FAudio bus chain; live block-ramped gain, channel fold, typed copies and native PCM/zero-allocation checks. Finite gain bounds and processing clamp retain safe typed PCM; physical listening and other platforms remain unverified. |
