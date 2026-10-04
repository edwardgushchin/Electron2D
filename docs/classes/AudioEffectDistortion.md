# AudioEffectDistortion

Last updated: 2026-10-04

**Declaration:** `public sealed class Electron2D.AudioEffectDistortion` · **Source:** [AudioEffectDistortion.cs](../../src/Scene/Resources/AudioEffectDistortion.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffect](AudioEffect.md).

## Description

Reusable stereo bus distortion with five [curves](AudioEffectDistortion.Mode.md). Each [instance](AudioEffectDistortionInstance.md) separates a low-frequency component with independent per-channel one-pole histories, applies the selected nonlinear curve after pre-gain, and recombines the post-gain result with the preserved high-frequency component. A bus borrows the resource and owns one instance per output stereo pair. Copies retain settings, while each new instance starts with clean histories. Live edits use one coherent snapshot per block.

## Example

```csharp
using var distortion = new AudioEffectDistortion
{
    DistortionMode = AudioEffectDistortion.Mode.ATan,
    Drive = 0.6f,
    PreGain = 3,
    PostGain = -2
};
AudioServer.AddBusEffect(0, distortion);
// Play a source routed through Master.
AudioServer.RemoveBusEffect(0, 0);
```

Remove the effect before disposing the borrowed resource.

## API summary

| Declaration | Default | Contract |
| --- | --- | --- |
| `AudioEffectDistortion()` | — | Creates the clip preset with no gain or drive. |
| `Mode DistortionMode { get; set; }` | `Clip` | One of five nonlinear curves; the property name distinguishes it from the nested enum type. |
| `float Drive { get; set; }` | 0 | Curve intensity, finite 0..1. Overdrive ignores it. |
| `float KeepHFHZ { get; set; }` | 16000 | Positive finite low-pass cutoff in hertz; frequencies above it bypass the curve. |
| `float PreGain { get; set; }` | 0 | Decibel gain before the curve; negative infinity silences only its input. |
| `float PostGain { get; set; }` | 0 | Decibel gain after the curve; negative infinity silences only its output. |

All five controls have writable stored descriptors and survive Resource/scene-local copying. Drive and mode are range-checked; cutoff must be finite positive. Decibel gains accept negative infinity or values with finite linear conversion. The usual authoring ranges are -60..60 dB pre, -80..24 dB post and 1..20500 Hz cutoff; finite raw values outside those ranges remain queryable until arithmetic overflow rejects processing. Invalid edits reject before mutation. A changed value is committed before notifying `Changed` observers.

## Processing and lifecycle

The low-pass coefficient is `exp(-τ × KeepHFHZ / outputRate)`. The high component is `input - low`; output is `curve(low × preGain) × postGain + high`. Every mode follows its pinned curve, including the subtle zero-drive alteration. The actual pinned Overdrive transfer is asymmetric across positive and negative input even though its reference prose describes the family as symmetric; this page documents the executed formula. A numerically stable algebraic form avoids avoidable `exp` overflow on extreme finite inputs while preserving ordinary source PCM. Nonfinite filter history or output reset both histories before raising an arithmetic error. Silent blocks continue processing the filter tail. Bypass/disable follows the inherited bus effect chain; structural graph edits replace instances.

## Verification and limits

[AudioDistortionTests](../../tests/Electron2D.Tests/AudioDistortionTests.cs) compares 2560 channel samples at each supported oracle rate across all five modes against the byte-pinned [C++ fixture](../../tests/Electron2D.Tests/Fixtures/Audio/Distortion/PROVENANCE.md). It covers defaults, validation, copies, scene-local state, observer failure, aliased/split blocks, live modes/drive, nonlinear endpoints, asymmetric overdrive, overflow recovery, native output/tails/bypass and 64 warmed CPU/native active/silent allocation passes. Linux Wayland GPU and compatibility public hosts capture processed PCM; 2/4/6/8 logical output profiles pass. Physical listening, actual multichannel devices, SDL/OS allocations and other platforms remain unverified. [Coverage](../coverage/classes/AudioEffectDistortion.md) records every own declaration; [ADR 0047](../decisions/audio.md#adr-0047) owns the backend.
