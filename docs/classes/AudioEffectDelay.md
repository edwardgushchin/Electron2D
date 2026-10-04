# AudioEffectDelay

Last updated: 2026-10-04

**Declaration:** `public sealed class Electron2D.AudioEffectDelay` · **Source:** [AudioEffectDelay.cs](../../src/Scene/Resources/AudioEffectDelay.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffect](AudioEffect.md).

## Description

Reusable stereo bus delay with a dry signal, two independent delayed taps and optional low-pass feedback. A bus borrows this resource and owns one [independent instance](AudioEffectDelayInstance.md) per output stereo pair. Each instance prepares bounded ring storage at the output rate, reads all thirteen settings once per processed block and retains its histories across live edits, bypass and temporary silence. Native output closure or structural effect edits replace the instance; Resource copies contain configuration only.

## Example

Partial snippet in an audio-owner host; remove the bus effect before disposing the borrowed resource:

```csharp
using var delay = new AudioEffectDelay { Dry = 0.5f, Tap1DelayMS = 180, Tap2Active = false };
AudioServer.AddBusEffect(0, delay);
// A source routed to Master now has a delayed tap.
AudioServer.RemoveBusEffect(0, 0);
```

## API summary

| Signature | Default | Contract |
| --- | --- | --- |
| `public AudioEffectDelay()` | — | Creates the documented two-tap configuration. |
| `public float Dry { get; set; }` | 1 | Finite original-signal multiplier. |
| `public bool Tap1Active { get; set; }` | true | Enables first tap output. |
| `public float Tap1DelayMS { get; set; }` | 250 | First tap delay in milliseconds, 0..1500. |
| `public float Tap1LevelDB { get; set; }` | -6 | First tap decibel gain; negative infinity silences. |
| `public float Tap1Pan { get; set; }` | 0.2 | First tap signed channel attenuation. |
| `public bool Tap2Active { get; set; }` | true | Enables second tap output. |
| `public float Tap2DelayMS { get; set; }` | 500 | Second tap delay in milliseconds, 0..1500. |
| `public float Tap2LevelDB { get; set; }` | -12 | Second tap decibel gain; negative infinity silences. |
| `public float Tap2Pan { get; set; }` | -0.4 | Second tap signed channel attenuation. |
| `public bool FeedbackActive { get; set; }` | false | Enables filtered repeating feedback. |
| `public float FeedbackDelayMS { get; set; }` | 340 | Feedback period in milliseconds, 0..1500. |
| `public float FeedbackLevelDB { get; set; }` | -6 | Feedback decibel gain; negative infinity silences. |
| `public float FeedbackLowpass { get; set; }` | 16000 | Positive low-pass threshold in hertz. |
| `protected override AudioEffectInstance OnInstantiate()` | — | Creates prepared independent stereo history. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Stores all thirteen fields in typed Resource/scene state. |
| `protected override Resource CreateDuplicateInstance()` | — | Creates an exact concrete copy target. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | — | Copies configuration without history. |

## Property descriptions

### Dry, Tap1 and Tap2 fields

Dry multiplies the current input. Each enabled tap reads its channel independently from an input ring after truncated `DelayMS × outputRate / 1000` frames. A zero delay reads the input just written. `LevelDB` converts to linear gain, and a tap's left/right coefficients multiply by clamped `1 - Pan` / `1 + Pan`; this attenuates channels without crossfeed. The common authored pan interval is -1..1, but finite out-of-range values remain stored and clamp only during processing. The gain accepts negative infinity or any DB value whose linear conversion is finite; extreme output overflow fails instead of contaminating later history. Delay fields accept finite values from 0 through 1500 ms. Dry is finite; it can exceed the usual authored 0..1 ratio.

### Feedback fields

When active, feedback gain converts from decibels and feeds the processed dry-plus-tap-plus-prior-feedback output into a separate delay line. The low-pass coefficient is `exp(-2π × FeedbackLowpass / outputRate)`; independent left/right histories use it for one-pole smoothing. Zero feedback delay reads the preceding feedback sample. The output includes the prior feedback slot even when feedback generation is switched off, so an existing tail can finish. A nonpositive/nonfinite low-pass threshold and feedback delay outside 0..1500 ms reject. Gain follows the same conversion rule as tap gains.

### State, events and errors

Property reads and writes reject disposed resources. Invalid scalar edits reject before mutation. An unchanged setting emits no `Changed`; a changed setting commits before notification, so a throwing observer does not roll it back. Each block gets one coherent snapshot under the resource gate. A disposed resource causes later instance processing to fail through the effect chain's usual owner-frame error path.

## Method descriptions

### OnInstantiate

Allocates independent input/feedback rings before processing. Standalone instances are caller-owned; bus instances are borrowed. The instance processes silent input to deliver delayed tails after a source ends. Storage is bounded to at most 2²² stereo frames per ring; an unsupported output rate rejects during instance preparation.

### Descriptor and copy hooks

All thirteen own fields are writable stored descriptors. `Duplicate` and scene-local copies preserve values and the concrete resource type; no running delay history or native bus ownership is copied.

## Verification and limits

[AudioDelayTests](../../tests/Electron2D.Tests/AudioDelayTests.cs) verifies defaults, validation, Resource/scene-local copies, Changed failure timing, independent stereo taps, live edits, aliasing, zero-delay/filtered feedback, overflow recovery and 64 warmed active and silent-tail blocks with zero managed allocation. Actual FAudio PCM, native finite-source tail, enable/disable and 64 active/paused passes use the dummy driver; GPU and compatibility `Engine.Run` hosts capture delayed bus output on Linux Wayland. Physical listening, OS allocation outside the custom FAudio allocator and other platforms are unverified. [Coverage](../coverage/classes/AudioEffectDelay.md) lists all mapped members; [ADR 0047](../decisions/audio.md#adr-0047) owns the mixer boundary.
