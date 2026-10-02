# AudioFFTSize

Last updated: 2026-10-03

**Declaration:** public enum `Electron2D.AudioFFTSize` · **Source:** [AudioEffectSpectrumAnalyzer.cs](../../src/Scene/Resources/AudioEffectSpectrumAnalyzer.cs) · **Decision:** [ADR 0051](../decisions/product.md#adr-0051).

One transform-size value contract is shared by audio spectrum analysis and the future pitch-shift slice. The numeric identities map both reference FFT-size enums to one C# type; `Max` is a nonselectable sentinel.

| Value | Numeric identity | Analyzer window |
| --- | --- | --- |
| `Size256` | 0 | 512 input frames, 256 frequency bins. |
| `Size512` | 1 | 1024 input frames, 512 bins. |
| `Size1024` | 2 | 2048 input frames, 1024 bins; default. |
| `Size2048` | 3 | 4096 input frames, 2048 bins. |
| `Size4096` | 4 | 8192 input frames, 4096 bins. |
| `Max` | 5 | Enum bound, rejected as a preset. |

The [analyzer](AudioEffectSpectrumAnalyzer.md) uses all five sizes now. Its exact latency, magnitude normalization and verification are described there. Pitch shifting remains Unimplemented in coverage and must reuse this type when its executable slice is added.
