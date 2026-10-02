# AudioEffectDistortion.Mode

Last updated: 2026-10-02

**Declaration:** public nested enum · **Source:** [AudioEffectDistortion.cs](../../src/Scene/Resources/AudioEffectDistortion.cs) · **Owner:** [AudioEffectDistortion](AudioEffectDistortion.md).

| Value | Numeric identity | Transfer |
| --- | --- | --- |
| `Clip` | 0 | Power curve followed by hard unit clipping. |
| `ATan` | 1 | Smooth arctangent saturation. |
| `LoFi` | 2 | Drive-controlled amplitude quantization. |
| `Overdrive` | 3 | Transistor-style curve; drive is unused. |
| `WaveShape` | 4 | Absolute sigmoid curve. |

<a id="clip"></a>
<a id="atan"></a>
<a id="lofi"></a>
<a id="overdrive"></a>
<a id="waveshape"></a>
The [owner page](AudioEffectDistortion.md) describes filtering, gain and lifecycle. Invalid enum values reject. `AudioDistortionTests` verifies all five numeric identities, C++ reference PCM and native behavior.
