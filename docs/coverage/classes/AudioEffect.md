# AudioEffect API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AudioEffect.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffect.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: [`public abstract class Electron2D.AudioEffect`](../../classes/AudioEffect.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioEffect`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffect.xml) | [`public abstract class Electron2D.AudioEffect`](../../classes/AudioEffect.md) | Implemented | ADR 0047: executable ordered FAPO bus processing, independent stereo-pair instances, bounded resource-shared capture, pre-gain effects/post-gain peaks, enable/bypass/reorder, tail activity, native callback error containment and coherent owner teardown; AudioEffectTests checks actual Linux x64 PCM and public Wayland hosts. Physical listening, actual multichannel devices and other platforms remain unverified. Typed finite positive duration has a 2^27-frame/1 GiB preparation bound; counter returns use Int64, matching their native lifetime counts. Registry activity settings validate finite values and nonnegative timeout. |
| [`method _instantiate() -> AudioEffectInstance`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffect.xml) | [`public Electron2D.AudioEffectInstance Instantiate()`](../../classes/AudioEffect.md) | Implemented | ADR 0047: executable ordered FAPO bus processing, independent stereo-pair instances, bounded resource-shared capture, pre-gain effects/post-gain peaks, enable/bypass/reorder, tail activity, native callback error containment and coherent owner teardown; AudioEffectTests checks actual Linux x64 PCM and public Wayland hosts. Physical listening, actual multichannel devices and other platforms remain unverified. Typed finite positive duration has a 2^27-frame/1 GiB preparation bound; counter returns use Int64, matching their native lifetime counts. Registry activity settings validate finite values and nonnegative timeout. |
