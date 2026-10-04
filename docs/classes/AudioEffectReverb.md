# AudioEffectReverb

Last updated: 2026-10-04

**Declaration:** `public sealed class Electron2D.AudioEffectReverb` · **Source:** [AudioEffectReverb.cs](../../src/Scene/Resources/AudioEffectReverb.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffect](AudioEffect.md).

## Description

Reusable stereo room reverberation for an audio bus. A bus borrows the resource and owns one [independent instance](AudioEffectReverbInstance.md) per output stereo pair. Each instance prepares one predelay, eight parallel combs and four serial all-pass lines for each channel at the actual output rate. The right channel uses a fixed additional tuning offset; `Spread` scales the effective difference. Settings are sampled coherently once per block, and live edits retain histories. A structural bus edit or output closure replaces instances; copies contain configuration only.

## Example

Partial audio-owner host snippet; remove the effect before disposing the borrowed resource:

```csharp
using var room = new AudioEffectReverb { RoomSize = 0.9f, Wet = 0.35f };
AudioServer.AddBusEffect(0, room);
// Sources routed to Master now use the room tail.
AudioServer.RemoveBusEffect(0, 0);
```

## API summary

| Signature | Default | Contract |
| --- | --- | --- |
| `public AudioEffectReverb()` | — | Creates the default room configuration. |
| `public float PredelayMSEC { get; set; }` | 150 | Requested early-reflection period in milliseconds. |
| `public float PredelayFeedback { get; set; }` | 0.4 | Early-reflection gain, clamped to 0..0.98 at storage. |
| `public float RoomSize { get; set; }` | 0.8 | Controls comb feedback; effective result is bounded 0.7..0.98. |
| `public float Damping { get; set; }` | 0.5 | Controls high-frequency retention in comb echoes. |
| `public float Spread { get; set; }` | 1 | Stereo delay-line spread, 0..1. |
| `public float Hipass { get; set; }` | 0 | Wet-input high-pass control, clamped to 0..1 during processing. |
| `public float Dry { get; set; }` | 1 | Original-signal amplitude multiplier. |
| `public float Wet { get; set; }` | 0.5 | Reverberated-signal amplitude multiplier, before the fixed 0.6 wet scale. |
| `protected override AudioEffectInstance OnInstantiate()` | — | Creates independent prepared stereo state. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Stores all eight typed resource fields. |
| `protected override Resource CreateDuplicateInstance()` | — | Creates an exact concrete copy target. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | — | Copies configuration without histories. |

## Property descriptions

### PredelayMSEC and PredelayFeedback

Predelay writes input plus delayed feedback into a 500 ms prepared line. The effective read distance is rounded to frames and clamped from ten frames through the line capacity; a finite nonnegative raw request is retained. Values beyond the authored 20..500 ms interval therefore saturate the effective line. Feedback accepts finite requests and stores their clamp to 0..0.98. The initial sound enters the combs immediately; this line controls early repetitions, not the first comb's onset.

### RoomSize, Damping and Spread

Eight parallel comb line lengths follow fixed room tunings. `RoomSize` maps to a bounded feedback coefficient `clamp(0.7 + RoomSize × 0.28, 0.7, 0.98)`. `Damping` controls a one-pole comb filter through the square of `Damping/2 + 0.5`; in the ordinary 0..1 interval, larger values retain more high-frequency content. These two properties retain finite raw values outside the authored interval while effective feedback/filter coefficients remain bounded. `Spread` is finite from zero through one: zero makes both channels' effective line lengths coincide, one uses the full right-channel offset. The range guard prevents invalid array lengths.

### Hipass, Dry and Wet

`Hipass` is a finite raw normalized control clamped during processing. Positive effective values apply a stateful first-order high-pass to the wet input; zero leaves the high-pass state dormant. Final output is `Dry × input + Wet × 0.6 × diffusedTail`. Dry/Wet accept finite raw values beyond the usual 0..1 authored ratios; an internal or final nonfinite value clears every history and reports ArithmeticException. Each channel remains independent except for the shared authored settings.

### State, events and errors

Invalid scalar edits reject before mutation. Unchanged values do not raise `Changed`; changed values commit before notifying, so a throwing observer does not roll them back. Reads and writes reject disposed resources. Native processing of disposed or overflowing instances follows the existing effect-chain error policy on the owner frame.

## Method descriptions

### OnInstantiate

Prepares two separate channel histories at the actual mix rate before processing; one bus stereo pair receives one instance. Silent input is processed so tails continue after a finite source stops. An unsupported output rate above one million frames/s rejects before large allocation. Standalone instances are caller-owned; bus instances are borrowed.

### Descriptor and copy hooks

Eight writable stored descriptors provide typed Resource and scene-local state. Copies preserve the exact concrete resource and all eight controls without copying comb, all-pass or predelay histories.

## Verification and limits

[AudioReverbTests](../../tests/Electron2D.Tests/AudioReverbTests.cs) compares three profiles and 24576 channel samples to the byte-pinned C++ [oracle](../../tests/Electron2D.Tests/Fixtures/Audio/Reverb/PROVENANCE.md), checks independent stereo timing, typed copies/guards, observer failures, overflow recovery, finite-source native tails, bypass and 64 warmed active/silent CPU and active/paused FAudio blocks without measured managed or custom allocator calls. Public Window hosts capture processed audio on Linux Wayland GPU and compatibility renderers. Physical listening, SDL/OS allocation, target-platform execution beyond Linux x64 and broad-scene memory budgets remain unverified. [Coverage](../coverage/classes/AudioEffectReverb.md) records the mapped API; [ADR 0047](../decisions/audio.md#adr-0047) owns the mixer boundary.
