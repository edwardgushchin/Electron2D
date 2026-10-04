# AudioEffectChorus

Last updated: 2026-10-04

**Declaration:** `public sealed class Electron2D.AudioEffectChorus` · **Source:** [AudioEffectChorus.cs](../../src/Scene/Resources/AudioEffectChorus.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffect](AudioEffect.md).

## Description

Reusable four-voice stereo chorus for an audio bus. Each [instance](AudioEffectChorusInstance.md) prepares its own ring, one-pole filter and oscillator state at the output mix rate. A bus borrows the resource and owns one instance per output stereo pair. Resource and scene-local copies retain configuration but create fresh processing state. Live edits are sampled coherently per block; deactivating a voice preserves its settings and histories.

## Example

```csharp
using var chorus = new AudioEffectChorus { VoiceCount = 2, Wet = 0.4f };
chorus.SetVoiceRateHZ(0, 0.7f);
AudioServer.AddBusEffect(0, chorus);
// Play a source routed through Master.
AudioServer.RemoveBusEffect(0, 0);
```

Remove the effect before disposing the borrowed resource.

## API summary

| Declaration | Default | Contract |
| --- | --- | --- |
| `AudioEffectChorus()` | — | Creates four stored voices, two initially active. |
| `int VoiceCount { get; set; }` | 2 | Active voices, 1..4. |
| `float Dry { get; set; }` | 1 | Original signal multiplier. |
| `float Wet { get; set; }` | 0.5 | Shared multiplier for delayed voices. |
| `GetVoiceDelayMS(int)` / `SetVoiceDelayMS(int, float)` | 15, 20, 12, 12 | Per-voice delay, 0..50 ms. |
| `GetVoiceRateHZ(int)` / `SetVoiceRateHZ(int, float)` | 0.8, 1.2, 1, 1 | Per-voice LFO frequency, 0.1..20 Hz. |
| `GetVoiceDepthMS(int)` / `SetVoiceDepthMS(int, float)` | 2, 3, 0, 0 | Maximum sinusoidal delay modulation, 0..20 ms. |
| `GetVoiceLevelDB(int)` / `SetVoiceLevelDB(int, float)` | 0 each | Per-voice gain in decibels; negative infinity silences it. |
| `GetVoiceCutoffHZ(int)` / `SetVoiceCutoffHZ(int, float)` | 8000, 8000, 16000, 16000 | One-pole low-pass threshold; zero skips the voice, at least 16000 bypasses filtering. |
| `GetVoicePan(int)` / `SetVoicePan(int, float)` | -0.5, 0.5, 0, 0 | Signed stereo pan, -1..1. |

Voice indices are zero-based, 0..3, even for inactive voices. The twenty-four stored `Voice/1..4/{DelayMS,RateHZ,DepthMS,LevelDB,CutoffHZ,Pan}` descriptors project these methods into Resource and scene storage. `VoiceCount`, `Dry` and `Wet` are also stored. Invalid indices, nonfinite controls, out-of-range delay/rate/depth/cutoff/pan or decibel gains without finite linear conversion reject before mutation. Scalar dry/wet accept finite values outside the usual 0..1 authored ratio. Changed observers run after a committed edit.

## Processing and lifecycle

The source block is written to the prepared stereo ring in chunks of at most 256 frames, then each active voice reads a sinusoidally offset delay with linear interpolation. The effective delay stays at least ten frames behind the write position to avoid reading ahead when depth exceeds the requested base delay. Filter history is separate per voice; pan applies independent left/right gains after filtering. Dry input and all wet voices sum in float PCM. Silence is processed so a finite source can leave a delayed tail. Instances accept aliased spans and retain histories across blocks and live edits. Invalid output clears all histories and reports an arithmetic error through the existing effect-chain owner path. A structural bus edit or output closure replaces the instance.

## Verification and limits

[AudioChorusTests](../../tests/Electron2D.Tests/AudioChorusTests.cs) checks every control/default/descriptor, copy and scene-local storage, exact static impulse positions, aliasing, modulation, filter/level/pan, native FAudio PCM and finite-source tails, bypass, and 64 warmed active/silent CPU and active/paused native passes without measured managed or custom allocator calls. Public Linux Wayland GPU and compatibility hosts capture processed PCM; 2/4/6/8 logical output profiles pass. Physical listening, actual multichannel devices, SDL/OS allocations, other platforms and broad-scene memory budgets remain unverified. [Coverage](../coverage/classes/AudioEffectChorus.md) records every own declaration; [ADR 0047](../decisions/audio.md#adr-0047) owns the backend.
