# AudioEffectHardLimiter

Last updated: 2026-10-03

**Declaration:** `public sealed class Electron2D.AudioEffectHardLimiter` · **Source:** [AudioEffectHardLimiter.cs](../../src/Scene/Resources/AudioEffectHardLimiter.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffect](AudioEffect.md).

## Description

Limits each bus stereo pair with one linked peak detector. A two-millisecond delay lets the detector reduce gain before a peak reaches output; a short sustain window holds the recent minimum and Release restores gain after that. Left and right share one gain so stereo balance remains stable. The bus borrows the resource and owns an independent [internal instance](AudioEffectHardLimiterInstance.md) for each pair. Standalone instances are caller-owned. Live property edits apply at the next processed block; Resource and scene-local copies retain only the three settings.

The output uses a final sample ceiling. This corrects the pinned processor's small sample overshoot and keeps the published ceiling true after live edits. It does not prove true-peak or inter-sample-peak limiting on physical devices. Invalid nonfinite pre-gain results reset the instance and report an arithmetic error. Silent input keeps processing to flush delayed frames.

## Example

```csharp
using var limiter = new AudioEffectHardLimiter { CeilingDB = -1, PreGainDB = 3 };
AudioServer.Instance.AddBusEffect(0, limiter);
// Run players routed to bus zero, then remove the borrowed effect.
AudioServer.Instance.RemoveBusEffect(0, 0);
```

This partial owner-thread host snippet needs a player and real native mixing. The complete public Window path is exercised by `AudioHardLimiterTests.RunHost`.

## API summary

| Declaration | Default | Contract |
| --- | --- | --- |
| `public AudioEffectHardLimiter()` | — | Creates a reusable limiting resource. |
| `public float CeilingDB { get; set; }` | -0.3 dB | Output sample ceiling. |
| `public float PreGainDB { get; set; }` | 0 dB | Gain before detection and limiting. |
| `public float Release { get; set; }` | 0.1 s | Positive gain-reduction release time. |
| `protected override AudioEffectInstance OnInstantiate()` | — | Prepares independent delay and detector state at output rate. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Exposes three stored typed fields. |
| `protected override Resource CreateDuplicateInstance()` | — | Creates the exact concrete copy target. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | — | Copies settings without delay or gain history. |

## Property Descriptions

<a id="ceilingdb"></a>
### CeilingDB

The maximum absolute output sample, in decibels. -0.3 initially; the ordinary authored range is -24 through 0. Any value whose linear conversion remains finite is accepted, including negative infinity for silence. Invalid values throw ArgumentOutOfRangeException before mutation. The final ceiling is applied even if a live edit makes the earlier detector history stale.

<a id="pregaindb"></a>
### PreGainDB

Gain before the linked peak detector. Zero initially; the ordinary authored range is -24 through 24 dB. Negative infinity produces silence. Nonfinite linear conversion rejects; a finite but excessively large input times gain that overflows at processing throws ArithmeticException and resets the instance.

<a id="release"></a>
### Release

Seconds to restore gain after reduction; 0.1 initially, ordinarily 0.01 through 3. Any positive finite value is accepted. Zero, negative and nonfinite values throw ArgumentOutOfRangeException. Sub-sample positive times recover completely once the sustain window expires, including after repeated peaks. Property observers run after a changed value commits; their exceptions propagate. Disposed resources reject all property access.

## Lifecycle, verification and limits

Each instance fixes its delay and gain-bucket sizes at construction from the current output rate. The delay starts silent; output remains silent for `ceil(rate × 0.002) + 1` frames. Changed resource controls affect existing instances without clearing history. Bypass leaves their history in place; structural bus edits recreate and invalidate borrowed instances. The instance processes inactive silent blocks and holds no native resources of its own.

[AudioHardLimiterTests](../../tests/Electron2D.Tests/AudioHardLimiterTests.cs) checks the pinned C++ profiles, alias/split equivalence, linked channels, initial delay, strict ceiling, live edits, tiny release, silence, overflow recovery, copies, native PCM and 2/4/6/8 logical profiles. Current Linux Wayland GPU/compatibility hosts exercise the public player-to-effect-to-capture path. Sixty-four warmed active and paused native passes allocate zero measured managed bytes and make zero custom FAudio allocator calls; preparation, SDL/OS allocation, physical listening, true peaks, actual multichannel hardware and other platforms remain separate limits. See [coverage](../coverage/classes/AudioEffectHardLimiter.md) and [ADR 0047](../decisions/audio.md#adr-0047).
