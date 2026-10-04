# AudioEffectBandLimitFilter

Last updated: 2026-10-04

**Declaration:** `public sealed class Electron2D.AudioEffectBandLimitFilter` · **Source:** [AudioEffectFilter.cs](../../src/Scene/Resources/AudioEffectFilter.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffectFilter](AudioEffectFilter.md).

## Description

Rejects a wider band between its Resonance lower edge and effective CutoffHZ upper edge. The center is their mean and width is logarithmic. The complementary band-rejection numerator corrects the opposite pass-band response while preserving the bandwidth denominator. Nonpositive/reversed edges receive bounded positive effective values; literal finite controls remain stored. Gain is ignored but remains in authoring descriptors. Every instance has independent stereo stage history and uses the actual output mix rate. All inherited scalar settings/defaults, finite guards, live snapshot/copy behavior, preset history and bus lifetime apply.

## Example

Partial host snippet; add on the audio owner and remove before disposing the caller-owned resource.

```csharp
using var filter = new AudioEffectBandLimitFilter { CutoffHZ = 2000 };
AudioServer.AddBusEffect(0, filter);
AudioServer.RemoveBusEffect(0, 0);
```

## API summary

| Signature | Contract |
| --- | --- |
| `public AudioEffectBandLimitFilter()` | Default broad band-rejection response; CutoffHZ 2000, Resonance 0.5, Gain one, DB Filter6DB. |
| `protected override Resource CreateDuplicateInstance()` | Creates an empty target with the same concrete response. |

## Method Descriptions

<a id="constructor"></a>
### Constructor

Selects the fixed broad band-rejection kernel. No native handle or history is owned by this resource. Instantiate creates prepared pair state; buses borrow this resource and own instances. Inherited controls affect the next block and preserve history.

<a id="createduplicateinstance"></a>
### CreateDuplicateInstance

Returns a new AudioEffectBandLimitFilter. Inherited copy hooks retain coherent scalar configuration and metadata. Scene-local and deep copies preserve the concrete response while omitting transient histories/native state.

## Verification and limits

AudioFilterTests exercises this concrete resource, all four presets, analytic/native response, independent histories, aliases, copies and numerical boundaries. See the [base reference](AudioEffectFilter.md#lifecycle-errors-and-verification) and [component evidence](../components/audio-playback.md#frequency-filters). Native Linux x64 and current Wayland hosts are verified; physical listening, actual multichannel hardware and other platforms remain unverified.
