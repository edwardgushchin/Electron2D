# AudioEffectLimiter

Last updated: 2026-10-03

**Declaration:** `public sealed class Electron2D.AudioEffectLimiter` · **Source:** [AudioEffectLimiter.cs](../../src/Scene/Resources/AudioEffectLimiter.cs) · **Component:** [Audio playback](../components/audio-playback.md#legacy-soft-limiting).

**Inherits:** [AudioEffect](AudioEffect.md).

## Description

A legacy, stateless stereo transfer curve. Each channel first receives gain compensation of `CeilingDB - ThresholdDB`. Above the linear amplitude of `-SoftClipDB`, the curve adds a compressed exponential contribution to that amplitude; a final cap limits every output sample to the linear CeilingDB. It preserves sign and supplies no delay, linked detector or release. The legacy branch can be discontinuous at its threshold. Prefer [AudioEffectHardLimiter](AudioEffectHardLimiter.md) for linked lookahead peak limiting.

Buses borrow this resource and own independent [internal instances](AudioEffectLimiterInstance.md) per output stereo pair. Standalone instances are caller-owned and borrow the resource. Each block reads one coherent snapshot. Resource and scene-local copies retain all four settings; processing state never enters resource graphs. Scalar access is serialized; bus preparation and edits follow the audio owner's lifecycle. No native resource or output-rate-dependent storage is needed by the curve itself.

## Example

```csharp
using var limiter = new AudioEffectLimiter { ThresholdDB = -18, CeilingDB = -12 };
AudioServer.Instance.AddBusEffect(0, limiter);
// Run players routed to bus zero before removing the borrowed effect.
AudioServer.Instance.RemoveBusEffect(0, 0);
```

This partial owner-thread host snippet requires a player and native mixing. Its complete public Window counterpart executes in `AudioLimiterTests.RunHost`.

## API summary

| Declaration | Default | Contract |
| --- | --- | --- |
| `public AudioEffectLimiter()` | — | Creates the legacy curve resource. |
| `public float ThresholdDB { get; set; }` | 0 dB | Determines pre-curve compensation. |
| `public float CeilingDB { get; set; }` | -0.1 dB | Absolute output sample cap. |
| `public float SoftClipDB { get; set; }` | 2 dB | Negated decibel branch threshold. |
| `public float SoftClipRatio { get; set; }` | 10 | Stored legacy value with no PCM effect. |
| `protected override AudioEffectInstance OnInstantiate()` | — | Creates independent stateless processing. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Four stored typed settings plus inherited descriptors. |
| `protected override Resource CreateDuplicateInstance()` | — | Constructs an exact concrete copy target. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | — | Copies the four settings. |

## Constructor Descriptions

<a id="constructor"></a>
### AudioEffectLimiter()

Creates default settings without opening output or creating an instance. Dispose the resource after removing it from borrowing buses and disposing standalone instances.

## Property Descriptions

<a id="thresholddb"></a>
### ThresholdDB

Finite decibel value, initially zero. The ordinary authoring range is -30 through 0; values outside it remain raw. Lowering it increases gain compensation. It is distinct from the soft-clip branch threshold and final cap.

<a id="ceilingdb"></a>
### CeilingDB

Initially -0.1 dB, ordinarily -20 through -0.1. Finite values with a finite float linear amplitude are accepted, including values above 0 dB. A sufficiently negative finite value underflows that amplitude to zero and produces silence. Nonfinite values or overflowing linear conversion throw ArgumentOutOfRangeException before mutation. The final cap uses that exact float amplitude; it is a sample ceiling, not a true-peak guarantee.

<a id="softclipdb"></a>
### SoftClipDB

Finite decibel control, initially two, ordinarily zero through six. The branch starts strictly above `DBToLinear(-SoftClipDB)` after compensation. Equality remains on the linear branch. With start `s`, ceiling `c` and peak `p = c + 25` in decibels, the exponential multiplier is `abs((c - s) / (p - s))`. The contribution uses the compensated magnitude's decibel excess above the ceiling. A branch starting at or above the final cap cannot alter the capped result and is skipped; this also removes the raw-control singularity at `s == c + 25`. Widened intermediates and an explicit zero case keep finite extreme controls/input bounded without `0 × infinity` contaminating silence.

<a id="softclipratio"></a>
### SoftClipRatio

Finite stored value, initially ten, ordinarily three through twenty. It does not affect PCM. Negative or large finite values remain authored data and copy normally. This preserves the legacy contract rather than inventing another curve control.

All changed property assignments commit before Changed observers run; observer errors propagate after commit. Equal assignments do not notify. Invalid nonfinite controls reject before mutation. Disposed resources reject property access and processing through their borrowing instances.

## Method Descriptions

<a id="oninstantiate"></a>
### OnInstantiate

Creates a caller-owned AudioEffectInstance that borrows this resource. It has no sample history or rate dependency and inherits `ProcessSilence() == false` because silent input yields silence. Buses attach and own instances; borrowed attached instances cannot be directly processed or disposed.

<a id="getpropertydescriptors"></a>
### GetPropertyDescriptors

Adds the four stored float settings with their defaults to the inherited metadata. Descriptors use the same validation and notification behavior as direct setters.

<a id="createduplicateinstance"></a>
### CreateDuplicateInstance

Creates the exact concrete resource target for inherited Duplicate/DuplicateDeep and scene copying.

<a id="copycustomstateto"></a>
### CopyCustomStateTo

Copies one coherent setting snapshot, including the inert ratio, into the newly constructed target. No instance or native bus ownership transfers.

## Lifecycle, verification and limits

Instantiate, add/remove, bypass/enable and output closure use the existing AudioEffect/AudioServer contracts. Structural chain edits invalidate borrowed instances and preserve borrowed resources. Live settings apply on the next block without reset or smoothing. Empty and silent standalone blocks, split blocks and aliased input/output are supported. The inherited processor rejects unequal spans and nonfinite input.

[AudioLimiterTests](../../tests/Electron2D.Tests/AudioLimiterTests.cs) compares 4,096 channel samples across eight byte-pinned C++ profiles, split/alias equivalence, raw values, copies, observers, finite extreme settings, exact sample caps and native live/bypass/ownership behavior. Sixty-four warmed active/silent CPU blocks and active/paused native passes measure zero managed bytes and custom FAudio allocator calls. Linux x64 native 2/4/6/8 logical profiles and two public host cycles on each Wayland renderer are checked. Physical listening, true peaks, actual multichannel hardware, SDL/OS allocations and other platforms remain unverified. See [coverage](../coverage/classes/AudioEffectLimiter.md) and [ADR 0047](../decisions/audio.md#adr-0047).

The shared [AudioEffectInstance](AudioEffectInstance.md) path now preserves original PCM for partially overlapping standalone spans in either direction. Limiter, Amplify and stateful Delay checks compare disjoint output and subsequent tails, then verify 64 warmed overlapping blocks allocate zero managed bytes. First use or capacity growth prepares a reusable copy; native exact-in-place blocks need no extra copy.
