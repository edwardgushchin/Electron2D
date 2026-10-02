# AudioEffectPanner

Last updated: 2026-10-02

**Declaration:** `public sealed class Electron2D.AudioEffectPanner` · **Source:** [AudioEffectLevel.cs](../../src/Scene/Resources/AudioEffectLevel.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffect](AudioEffect.md).

## Description

Reusable stereo bus panner. A bus borrows the resource and owns one [instance](AudioEffectPannerInstance.md) per stereo output pair. Pan zero passes both channels; minus one folds both channels to left and plus one folds both to right. Intermediate settings use independent clamped channel gains and crossfeed. The live value is read once at each processed block. This changes existing stereo bus PCM and creates no tail.

## Example

Partial snippet in an audio-owner host:

```csharp
using var pan = new AudioEffectPanner { Pan = -.5f };
AudioServer.Instance.AddBusEffect(0, pan);
pan.Pan = 1;
AudioServer.Instance.RemoveBusEffect(0, 0);
```

## API summary

| Signature | Contract |
| --- | --- |
| `public AudioEffectPanner()` | Centered pan. |
| `public float Pan { get; set; }` | Finite raw pan, zero initially. |
| `protected override AudioEffectInstance OnInstantiate()` | Creates an independent processing instance. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stored typed Pan authoring property. |
| `protected override Resource CreateDuplicateInstance()` | Creates an exact concrete copy target. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies Pan. |

## Property Descriptions

### Pan

Finite values are retained exactly, including outside the authored -1 to 1 interval. Processing clamps the left coefficient `1 - Pan` and right coefficient `1 + Pan` to [0, 1]; it mixes `left * leftGain + right * (1 - rightGain)` into left and `right * rightGain + left * (1 - leftGain)` into right. NaN/infinity reject before mutation; disposal rejects reads and writes. `Changed` follows a committed edit.

## Method Descriptions

### OnInstantiate

Creates a resource-borrowing stereo processor. Each block uses one coherent Pan snapshot; source and destination may alias.

### GetPropertyDescriptors and copy hooks

Expose and store Pan through typed Resource/scene-local authoring, preserve the concrete type and scalar, and leave processing instances outside copies.

## Lifecycle, errors and verification

`AudioEffectLevelTests` checks endpoints, out-of-range clamps, aliasing, resource copies, native bus output, enable/disable and warmed allocations. The base instance validates finite PCM and equal spans; the native chain contains errors. Linux x64 dummy audio ran; physical listening, other platforms and native allocations beyond FAudio's custom allocator remain unverified. See [ADR 0047](../decisions/audio.md#adr-0047) and [coverage](../coverage/classes/AudioEffectPanner.md).
