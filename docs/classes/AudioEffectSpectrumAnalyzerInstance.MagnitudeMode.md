# AudioEffectSpectrumAnalyzerInstance.MagnitudeMode

Last updated: 2026-10-03

**Declaration:** public nested enum · **Source:** [AudioEffectSpectrumAnalyzer.cs](../../src/Scene/Resources/AudioEffectSpectrumAnalyzer.cs) · **Owner:** [AudioEffectSpectrumAnalyzerInstance](AudioEffectSpectrumAnalyzerInstance.md).

| Value | Numeric identity | Frequency-range query |
| --- | --- | --- |
| `Average` | 0 | Arithmetic mean of every included bin, per channel. |
| `Max` | 1 | Largest included bin, per channel; default. |

Invalid values reject. The query accepts reversed finite endpoints and clamps them to the transform's bin range. [AudioSpectrumTests](../../tests/Electron2D.Tests/AudioSpectrumTests.cs) exercises both modes with stereo sine peaks and native bus state.
