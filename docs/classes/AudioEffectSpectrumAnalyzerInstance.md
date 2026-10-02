# AudioEffectSpectrumAnalyzerInstance

Last updated: 2026-10-03

**Declaration:** `public sealed class Electron2D.AudioEffectSpectrumAnalyzerInstance` · **Source:** [AudioEffectSpectrumAnalyzer.cs](../../src/Scene/Resources/AudioEffectSpectrumAnalyzer.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffectInstance](AudioEffectInstance.md).

## Description

Queryable state for one [AudioEffectSpectrumAnalyzer](AudioEffectSpectrumAnalyzer.md) and one output stereo pair. The instance passes PCM through bit-identically, gathers independent left/right Hann-windowed samples, performs two prepared radix-two FFTs after each complete window and stores magnitudes normalized by the selected bin count. Queries synchronize with the audio callback and use the most recent complete spectrum. Before the first window, magnitudes are zero. Silent blocks refresh the spectrum. A nonfinite transform resets prepared history and reports an arithmetic error through the existing owner path.

## API summary

| Declaration | Contract |
| --- | --- |
| [`MagnitudeMode`](AudioEffectSpectrumAnalyzerInstance.MagnitudeMode.md) | Nested Average=0 and Max=1 modes. |
| `Vector2 GetMagnitudeForFrequencyRange(float fromHZ, float toHZ, MagnitudeMode mode = MagnitudeMode.Max)` | Returns left/right linear magnitudes across an inclusive frequency-bin range. |

The query clamps finite frequencies below zero and above Nyquist to edge bins; reversed endpoints are swapped. `Average` divides each channel's sum by the number of included bins; `Max` chooses its largest bin. Nonfinite frequencies or undefined modes reject. Query and processing reject a disposed source or instance. A bus-owned instance is borrowed from `AudioServer.GetBusEffectInstance`, so callers may query it but cannot process or dispose it directly. Structural bus-effect edits invalidate borrowed state; standalone instances are caller-owned.

[AudioSpectrumTests](../../tests/Electron2D.Tests/AudioSpectrumTests.cs) verifies frequency localization, normalization, per-pair independence, current native queries, public hosts and warmed allocation. See [coverage](../coverage/classes/AudioEffectSpectrumAnalyzerInstance.md) for the mapped class/method/enum rows. Physical listening and other platforms remain unverified.
