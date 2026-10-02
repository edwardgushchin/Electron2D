# AudioEffectSpectrumAnalyzer

Last updated: 2026-10-03

**Declaration:** `public sealed class Electron2D.AudioEffectSpectrumAnalyzer` · **Source:** [AudioEffectSpectrumAnalyzer.cs](../../src/Scene/Resources/AudioEffectSpectrumAnalyzer.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffect](AudioEffect.md).

## Description

An audio bus analyzer that passes stereo PCM through unchanged while preparing queryable magnitudes. A bus borrows the resource and owns a [public instance](AudioEffectSpectrumAnalyzerInstance.md) per output stereo pair. `AudioServer.GetBusEffectInstance` returns that borrowed instance; structural effect edits or output closure invalidate it. Each instance owns a Hann window, transform work array and ring of stereo magnitude snapshots. Configuration is sampled at instance creation, so later resource edits affect new instances. Resource/scene-local copies retain only configuration.

## Example

```csharp
using var analyzer = new AudioEffectSpectrumAnalyzer { FFTSize = AudioFFTSize.Size512 };
AudioServer.Instance.AddBusEffect(0, analyzer);
var instance = (AudioEffectSpectrumAnalyzerInstance)AudioServer.Instance.GetBusEffectInstance(0, 0);
Vector2 lowBand = instance.GetMagnitudeForFrequencyRange(80, 250);
AudioServer.Instance.RemoveBusEffect(0, 0);
```

The instance is borrowed; do not dispose or directly process it. Reacquire it after changing the bus effect chain.

## API summary

| Declaration | Default | Contract |
| --- | --- | --- |
| `AudioEffectSpectrumAnalyzer()` | — | Creates a reusable configuration resource. |
| `float BufferLength { get; set; }` | 2 seconds | Retained history duration, finite 0.1..4 seconds. |
| `AudioFFTSize FFTSize { get; set; }` | `Size1024` | Transform preset; five sizes, `Max` rejected. |

Both properties have stored typed descriptors. The [shared enum](AudioFFTSize.md) follows ADR 0051. Invalid edits reject before mutation; observer callbacks run after a changed value commits. `BufferLength` determines the number of prepared historical magnitude frames and memory use. The current public query reads only the newest complete transform; no older-time query is exposed in the pinned contract. Each transform consumes twice the selected FFT-size number of PCM frames; larger sizes produce narrower frequency bins with greater latency. Silent input is processed so an old spectrum eventually becomes zero after its next complete window.

## Verification and limits

[AudioSpectrumTests](../../tests/Electron2D.Tests/AudioSpectrumTests.cs) checks all five numeric size presets, Hann-normalized stereo sine bins, max/average range queries, reversed/clamped ranges, preparation-time configuration, resource/scene copies, aliased and split PCM, silent replacement, overflow recovery and warmed CPU/native allocation. It verifies borrowed native bus queries and pass-through PCM across 2/4/6/8 logical output profiles plus Linux Wayland GPU and compatibility public hosts. Physical listening, actual multichannel devices, SDL/OS allocation and other platforms remain unverified. [Coverage](../coverage/classes/AudioEffectSpectrumAnalyzer.md) lists its own API; [ADR 0047](../decisions/audio.md#adr-0047) owns the FAudio backend.
