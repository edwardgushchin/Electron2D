# AudioEffectLowPassFilter

Last updated: 2026-10-02

**Declaration:** `public sealed class Electron2D.AudioEffectLowPassFilter` · **Source:** [AudioEffectFilter.cs](../../src/Scene/Resources/AudioEffectFilter.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffectFilter](AudioEffectFilter.md).

## Description

Attenuates frequencies above the cutoff and retains low frequencies. Gain is ignored and hidden from authoring descriptors. Every instance has independent stereo stage history and uses the actual output mix rate. All inherited scalar settings/defaults, finite guards, live snapshot/copy behavior, preset history and bus lifetime apply.

## Example

Partial host snippet; add on the audio owner and remove before disposing the caller-owned resource.

```csharp
using var filter = new AudioEffectLowPassFilter { CutoffHZ = 2000 };
AudioServer.Instance.AddBusEffect(0, filter);
AudioServer.Instance.RemoveBusEffect(0, 0);
```

## API summary

| Signature | Contract |
| --- | --- |
| `public AudioEffectLowPassFilter()` | Default low-pass response; CutoffHZ 2000, Resonance 0.5, Gain one, DB Filter6DB. |
| `protected override Resource CreateDuplicateInstance()` | Creates an empty target with the same concrete response. |

## Method Descriptions

<a id="constructor"></a>
### Constructor

Selects the fixed low-pass kernel. No native handle or history is owned by this resource. Instantiate creates prepared pair state; buses borrow this resource and own instances. Inherited controls affect the next block and preserve history.

<a id="createduplicateinstance"></a>
### CreateDuplicateInstance

Returns a new AudioEffectLowPassFilter. Inherited copy hooks retain coherent scalar configuration and metadata. Scene-local and deep copies preserve the concrete response while omitting transient histories/native state.

## Verification and limits

AudioFilterTests exercises this concrete resource, all four presets, analytic/native response, independent histories, aliases, copies and numerical boundaries. See the [base reference](AudioEffectFilter.md#lifecycle-errors-and-verification) and [component evidence](../components/audio-playback.md#frequency-filters). Native Linux x64 and current Wayland hosts are verified; physical listening, actual multichannel hardware and other platforms remain unverified.
