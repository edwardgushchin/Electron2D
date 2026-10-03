# AudioEffect

Last updated: 2026-10-03

**Declaration:** `public abstract class Electron2D.AudioEffect` · **Source:** [AudioEffect.cs](../../src/Scene/Resources/AudioEffect.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [Resource](Resource.md). **Inherited By:** [AudioEffectAmplify](AudioEffectAmplify.md), [AudioEffectPanner](AudioEffectPanner.md), [AudioEffectEQ](AudioEffectEQ.md), [AudioEffectCapture](AudioEffectCapture.md), [AudioEffectFilter](AudioEffectFilter.md), [AudioEffectDelay](AudioEffectDelay.md), [AudioEffectReverb](AudioEffectReverb.md), [AudioEffectChorus](AudioEffectChorus.md), [AudioEffectDistortion](AudioEffectDistortion.md), [AudioEffectSpectrumAnalyzer](AudioEffectSpectrumAnalyzer.md), [AudioEffectRecord](AudioEffectRecord.md), [AudioEffectHardLimiter](AudioEffectHardLimiter.md), [AudioEffectLimiter](AudioEffectLimiter.md), [AudioEffectPitchShift](AudioEffectPitchShift.md), [AudioEffectStereoEnhance](AudioEffectStereoEnhance.md), [AudioEffectPhaser](AudioEffectPhaser.md).

## Description

Configuration resource for an ordered bus effect. Buses borrow the resource and own independent [AudioEffectInstance](AudioEffectInstance.md) objects for every output stereo pair. Factories run during explicit preparation on the audio owner and must not reenter server configuration. Processing hooks run on the audio thread with prepared storage. Custom resources synchronize their own mutable configuration and implement ordinary Resource copying when needed.

## Example

Compiled equivalent: AudioEffectTests.ArithmeticEffect. This partial extension creates a simple gain effect; production code can add typed configuration and Resource copy hooks.

```csharp
sealed class HalfVolume : AudioEffect
{
    protected override AudioEffectInstance OnInstantiate() => new HalfVolumeInstance();
    protected override Resource CreateDuplicateInstance() => new HalfVolume();
}
sealed class HalfVolumeInstance : AudioEffectInstance
{
    protected override void OnProcess(ReadOnlySpan<Vector2> source, Span<Vector2> destination)
    {
        for (int i = 0; i < source.Length; i++) destination[i] = source[i] * .5f;
    }
}
```

## API summary

| Signature | Contract |
| --- | --- |
| `protected AudioEffect()` | Initializes resource metadata. |
| `public AudioEffectInstance Instantiate()` | Creates independent caller-owned processing state. |
| `protected abstract AudioEffectInstance OnInstantiate()` | Factory extension point. |

## Method Descriptions

<a id="instantiate"></a>
### Instantiate

Checks resource lifetime and calls OnInstantiate. Returns a live unattached instance; null, disposed or already bus-owned results throw InvalidOperationException. Instances borrow the source resource. Standalone callers dispose their own instance. Buses attach their returned instances and dispose them after native detachment. A factory must create independent state on every call, including each output stereo pair.

<a id="oninstantiate"></a>
### OnInstantiate

Required factory. May prepare bounded storage and capture immutable configuration. Exceptions propagate to preparation; a failed structural edit keeps the previous chain. Server configuration and Lock/Unlock from factories reject reentrancy. Disposal of the source during the factory fails preparation and cleans the returned instance.

## Lifecycle, integration and verification

Resource disposal never silently disposes a bus's borrowed processing state. Mixing a disposed source silences that effect and raises an owner-frame error. Structural effect edits recreate all instances on the edited bus; routing edits and enable/bypass retain identity. Output closure invalidates bus-owned instances while retaining configured resources. See [AudioServer effects](AudioServer.md#effects) and [ADR 0047](../decisions/audio.md#adr-0047).

AudioEffectTests exercises custom processing, factory failures/reentrancy, native ordering, pair ownership and teardown. The native backend is verified on Linux x64; physical listening and other platforms remain unverified. Resource loaders, bus-layout files and editor effects remain separate coverage dependencies.
