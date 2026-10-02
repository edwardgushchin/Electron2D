# AudioEffectLowPassFilter API coverage

Last updated: 2026-10-02

Godot source: [doc/classes/AudioEffectLowPassFilter.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectLowPassFilter.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioEffectFilter](AudioEffectFilter.md). Electron2D type: [`public sealed class Electron2D.AudioEffectLowPassFilter`](../../classes/AudioEffectLowPassFilter.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioEffectLowPassFilter`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioEffectLowPassFilter.xml) | [`public sealed class Electron2D.AudioEffectLowPassFilter`](../../classes/AudioEffectLowPassFilter.md) | Implemented | ADR 0047: executable typed filter family through existing FAPO bus chains, finite coherent settings, independent prepared four-stage stereo histories, actual output-rate coefficients, live/preset history retention and concrete Resource/scene-local copying. 72 byte-pinned C++ cases/36864 channel samples verify ordinary PCM; analytical/native tests cover every response, stable edges and zero measured warmed allocation. Physical listening, multichannel hardware and other platforms remain unverified. Numerical corrections retain literal finite controls while limiting effective cutoff below Nyquist and damping away from unit-circle poles; BandLimit uses complementary band rejection instead of the opposite source pass-band numerator. Numeric DB presets retain one-through-four biquad behavior. These proven source defects/adaptations are explicit in ADR 0047 and filter class pages. |
