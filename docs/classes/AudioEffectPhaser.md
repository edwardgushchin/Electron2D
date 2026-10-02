# AudioEffectPhaser

Last updated: 2026-10-03

**Declaration:** `public sealed class Electron2D.AudioEffectPhaser` · **Source:** [AudioEffectPhaser.cs](../../src/Scene/Resources/AudioEffectPhaser.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffect](AudioEffect.md).

## Description

Mixes stereo bus PCM with six swept all-pass stages per channel. A sinusoidal oscillator selects a frequency between RangeMinHZ and RangeMaxHZ for each frame. Each channel feeds its own filtered output back into its six stages; Depth sets the filtered contribution mixed with the original signal. The frequency endpoints may be reversed, which reverses the sweep direction.

Each bus owns an independent [instance](AudioEffectPhaserInstance.md) per output stereo pair while borrowing this resource. Live controls apply at the next processing block without discarding oscillator or filter history. Resource and scene-local copies retain the five controls, never transient PCM state. Silent processing continues to flush the feedback tail.

## Example

```csharp
using var phaser = new AudioEffectPhaser { RateHZ = 1.2f, Depth = 1.5f };
AudioServer.Instance.AddBusEffect(0, phaser);
// Play sources on bus zero, then remove the borrowed effect.
AudioServer.Instance.RemoveBusEffect(0, 0);
```

This owner-thread snippet needs native mixing. `AudioPhaserTests.RunHost` exercises a complete public Window/player/effect/capture path.

## API summary

| Declaration | Default | Contract |
| --- | --- | --- |
| `public AudioEffectPhaser()` | — | Creates the six-stage default configuration. |
| `public float RangeMinHZ { get; set; }` | 440 Hz | Finite endpoint from 10 through 10,000 Hz. |
| `public float RangeMaxHZ { get; set; }` | 1,600 Hz | Finite endpoint from 10 through 10,000 Hz. |
| `public float RateHZ { get; set; }` | 0.5 Hz | Finite sweep rate from 0.01 through 20 Hz. |
| `public float Feedback { get; set; }` | 0.7 | Finite filtered feedback from 0.1 through 0.9. |
| `public float Depth { get; set; }` | 1 | Finite filtered contribution from 0.1 through 4. |
| `protected override AudioEffectInstance OnInstantiate()` | — | Creates independent six-stage stereo state. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Adds five stored typed fields. |
| `protected override Resource CreateDuplicateInstance()` | — | Creates an exact-type copy target. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | — | Copies controls without filter histories. |

## Property descriptions

The two range endpoints are individually bounded; neither ordering is required. RateHZ changes oscillator advance at the next block. Feedback determines how much of the prior six-stage output enters the next sample. Depth scales only the filtered output in the dry-plus-filtered mix. Values outside the stated ranges or nonfinite values reject before mutation. Existing instances retain phase and channel histories after valid edits.

## Lifecycle, verification and limits

Each instance stores twelve all-pass histories, one oscillator phase and one feedback value per channel. Active and silent blocks allocate no processing buffers. Nonfinite intermediate or output PCM resets the histories and phase, then reports ArithmeticException. Attached instances are bus-owned; disposed resources reject access and processing.

[AudioPhaserTests](../../tests/Electron2D.Tests/AudioPhaserTests.cs) compares 16,384 channel samples from the [byte-pinned C++ processor](../../tests/Electron2D.Tests/Fixtures/Audio/Phaser/PROVENANCE.md) at the available 44,100 Hz output rate with zero observed PCM difference. It checks all controls, reversed endpoints, copies, live edits, split/alias state, analytic six-stage impulse response, overflow recovery, native output/tail/bypass, 2/4/6/8 logical profiles, both Linux Wayland public hosts and warmed allocation. Physical listening, actual multichannel hardware, 48,000 Hz engine output, SDL/OS allocations and other platforms remain unverified. See [coverage](../coverage/classes/AudioEffectPhaser.md) and [ADR 0047](../decisions/audio.md#adr-0047).
