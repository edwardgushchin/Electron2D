# AudioFFTSize

Last updated: 2026-10-03

**Declaration:** public enum `Electron2D.AudioFFTSize` · **Source:** [AudioEffectSpectrumAnalyzer.cs](../../src/Scene/Resources/AudioEffectSpectrumAnalyzer.cs) · **Decision:** [ADR 0051](../decisions/product.md#adr-0051).

One transform-size value contract is shared by audio spectrum analysis and pitch shifting. The numeric identities map both reference FFT-size enums to one C# type; `Max` is a nonselectable sentinel. The analyzer uses twice the named number of PCM frames and returns that many bins. Pitch shifting uses the named number of PCM frames for each stereo channel.

| Value | Numeric identity | Analyzer window | Pitch window |
| --- | --- | --- | --- |
| `Size256` | 0 | 512 input frames, 256 frequency bins. | 256 frames. |
| `Size512` | 1 | 1024 input frames, 512 bins. | 512 frames. |
| `Size1024` | 2 | 2048 input frames, 1024 bins; analyzer default. | 1024 frames. |
| `Size2048` | 3 | 4096 input frames, 2048 bins. | 2048 frames; pitch default. |
| `Size4096` | 4 | 8192 input frames, 4096 bins. | 4096 frames. |
| `Max` | 5 | Enum bound, rejected as a preset. | Rejected. |

The [analyzer](AudioEffectSpectrumAnalyzer.md) and [pitch shifter](AudioEffectPitchShift.md) use all five sizes. Their latency, processing and verification are described in their respective class pages.
