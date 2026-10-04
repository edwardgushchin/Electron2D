# AudioEffectAmplify

Last updated: 2026-10-04

**Declaration:** `public sealed class Electron2D.AudioEffectAmplify` · **Source:** [AudioEffectLevel.cs](../../src/Scene/Resources/AudioEffectLevel.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffect](AudioEffect.md).

## Description

Reusable bus gain resource. A bus borrows it and owns one independent stereo [instance](AudioEffectAmplifyInstance.md) per output pair. Each instance reads the live setting at the start of a processed block and linearly ramps from its previous decibel setting's gain to the new gain across that block. The final target applies from the following block. Silent inactive blocks are skipped by the ordinary bus policy; no separate tail is generated. Disabling/bypassing retains the instance and its previous gain; structural bus edits replace it.

## Example

Partial snippet in an audio-owner host, with cleanup after playback:

```csharp
using var gain = new AudioEffectAmplify { VolumeDB = -6 };
AudioServer.AddBusEffect(0, gain);
gain.VolumeLinear = .5f;
AudioServer.RemoveBusEffect(0, 0);
```

## API summary

| Signature | Contract |
| --- | --- |
| `public AudioEffectAmplify()` | Unity gain. |
| `public float VolumeDB { get; set; }` | Decibels, zero initially; negative infinity is silence. |
| `public float VolumeLinear { get; set; }` | Nonnegative linear gain, one initially; zero is silence. |
| `protected override AudioEffectInstance OnInstantiate()` | Creates independent ramp state. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stored DB and derived linear authoring properties. |
| `protected override Resource CreateDuplicateInstance()` | Creates an exact concrete copy target. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies the committed gain. |

## Property Descriptions

### VolumeDB

Finite decibel values whose linear conversion fits a float are accepted, as is negative infinity for silence. NaN, positive infinity and overflowing gain reject before mutation. Getter/setter reject disposal. `Changed` runs after the value commits, so an observer exception does not roll back the gain.

### VolumeLinear

Converts through `Mathf.LinearToDB` and `Mathf.DBToLinear`. Zero maps to negative-infinity DB; negative or nonfinite values reject. It is an authoring alias, so only VolumeDB is stored in a Resource or scene-local copy. Float conversion may round slightly.

## Method Descriptions

### OnInstantiate

Captures the current gain as this instance's ramp start. The instance borrows the resource and fails processing after source disposal. Different instances retain independent previous gains.

### GetPropertyDescriptors and copy hooks

Expose typed gain authoring and preserve the concrete type and committed scalar in `Duplicate` and scene-local copying. Native instances and ramp history are never copied.

## Lifecycle, errors and verification

Standalone `Process` accepts aliased spans, validates finite PCM and equal lengths, and propagates output overflow. The native chain contains processing errors and reports them on the owner frame. Empty blocks emit no samples and capture the current target for the next block. `AudioEffectLevelTests` checks ramp edges, zero, copies, invalid values, instance independence, real FAudio bus PCM and warmed allocations. Linux x64 dummy audio ran; physical listening, other platforms and native allocations beyond FAudio's custom allocator remain unverified.

See [ADR 0047](../decisions/audio.md#adr-0047) and [coverage](../coverage/classes/AudioEffectAmplify.md).
