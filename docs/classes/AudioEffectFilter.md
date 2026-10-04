# AudioEffectFilter

Last updated: 2026-10-04

**Declaration:** `public class Electron2D.AudioEffectFilter` · **Source:** [AudioEffectFilter.cs](../../src/Scene/Resources/AudioEffectFilter.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffect](AudioEffect.md). **Inherited By:** [LowPass](AudioEffectLowPassFilter.md), [HighPass](AudioEffectHighPassFilter.md), [BandPass](AudioEffectBandPassFilter.md), [Notch](AudioEffectNotchFilter.md), [BandLimit](AudioEffectBandLimitFilter.md), [LowShelf](AudioEffectLowShelfFilter.md), [HighShelf](AudioEffectHighShelfFilter.md).

## Description

Reusable typed frequency-filter configuration. The directly constructed base has low-pass response; prefer a concrete name for application intent. Each Instantiate creates eight independent prepared histories: four cascaded stages for each stereo channel. DB selects one through four stages. Live cutoff/resonance/gain/preset edits affect the next processing block without clearing history; unused stages retain their histories for later reuse. Disable/bypass likewise retain state; structural bus edits or output closure dispose instances.

The resource is borrowed by buses and processing instances. Its setters synchronize a coherent configuration snapshot independently of the instance processing gate; Changed follows the committed value on the setter thread. Every value is finite, and DB is a defined enum. Ordinary coefficients and float history match the pinned C++ PCM oracle. Effective cutoff is bounded below actual output Nyquist, with positive damping floors that avoid unstable/unit-circle poles at extreme controls. Raw finite values remain queryable. Overflowing float PCM raises ArithmeticException and clears histories; the native chain contains the failure and reports it on the owner frame.

Resource duplication and scene-local copies retain the exact concrete type and scalar configuration, never transient histories or native state. The concrete low/high/band-pass types omit unused Gain from authoring descriptors; base, notch, band-limit and shelf descriptors retain it. Only shelf processing consumes Gain. Typed scene-local packing executes; disk bus-layout and editor effects remain separate dependencies.

## Example

Partial audio-owner host snippet; attach/play an ordinary AudioStreamPlayer, then remove the effect before disposing caller-owned resources.

```csharp
using var filter = new AudioEffectLowPassFilter
{
    CutoffHZ = 2000,
    Resonance = .5f,
    DB = AudioEffectFilter.FilterDB.Filter12DB
};
AudioServer.AddBusEffect(0, filter);
// Live edits affect the next actual output-rate processing block:
filter.CutoffHZ = 4000;
AudioServer.RemoveBusEffect(0, 0);
```

AudioFilterTests.RunHost executes public Window/Autoplay/filter/capture and teardown on both current renderers.

## API summary

| Signature | Contract |
| --- | --- |
| `public AudioEffectFilter()` | Low-pass base defaults. |
| `public float CutoffHZ { get; set; }` | Requested hertz; 2000 initially. |
| `public float Resonance { get; set; }` | Finite response-dependent resonance/width; 0.5 initially. |
| `public float Gain { get; set; }` | Finite shelf gain coefficient; one initially. |
| `public FilterDB DB { get; set; }` | First preset initially, one through four stages. |
| `public enum FilterDB` | [Numeric presets](AudioEffectFilter.FilterDB.md). |
| `protected override AudioEffectInstance OnInstantiate()` | Fresh independent prepared stereo state. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Typed scalar authoring/storage descriptors. |
| `protected override Resource CreateDuplicateInstance()` | Empty exact base copy target; concrete types override. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Coherent scalar configuration copy. |

## Property Descriptions

<a id="cutoffhz"></a>
### CutoffHZ

Requested cutoff in hertz. Finite values below one clamp to one. The ordinary authored range is 1–20500; larger finite values remain stored. Processing uses min(requested, 0.4999 × actual output rate), preventing the negative damping produced beyond Nyquist. Nonfinite values throw ArgumentOutOfRangeException before mutation. The native output rate is read by each instance, so a 48000 Hz source resampled into a 44100 Hz output is filtered in output frequency space.

<a id="resonance"></a>
### Resonance

0.5 initially. Ordinary authored values are 0–1, while finite signed/out-of-range values remain stored. Pass/notch/shelf quality is floored at 0.0001; band-pass doubles it. Multiple stages use the quality root when quality exceeds one. An effective quality ceiling/damping floor prevents unit-circle poles at extreme values. BandLimit uses Resonance as the lower edge of its band, bounded from 0.0001 to just below its effective upper cutoff; its logarithmic bandwidth gives a broader rejection response than the notch. See that [concrete response](AudioEffectBandLimitFilter.md).

<a id="gain"></a>
### Gain

Shelf coefficient, one initially; ordinary authored values are 0–4. Finite signed values are preserved; processing floors the effective coefficient at 0.001. One stage's asymptotic changed-shelf amplitude is Gain squared. For N stages greater than one, the effective per-stage coefficient is Gain raised to 1/(N+1); the complete changed shelf therefore tends to Gain raised to 2N/(N+1). Unity is all-pass for shelf filters. Other kinds ignore Gain in their kernel. Nonfinite values reject before mutation.

<a id="db"></a>
### DB

Filter6DB/Filter12DB/Filter18DB/Filter24DB retain numeric values zero through three and select one/two/three/four cascaded biquad stages. These are the contract's preset names; the actual transfer curve, quality/gain distribution and stage histories are preserved and checked numerically rather than relabeled. Invalid enum values throw ArgumentOutOfRangeException without changing the preset. Preset changes retain all stage history, including temporarily unused stages.

## Method Descriptions

<a id="oninstantiate"></a>
### OnInstantiate

Creates internal [AudioEffectFilterInstance](AudioEffectFilterInstance.md), borrows the source and prepares histories/current coefficients before mixing. Factory/disposal ownership follows AudioEffect. A disposed source fails instantiation or later processing. Each stereo pair/bus obtains independent state.

<a id="getpropertydescriptors"></a>
### GetPropertyDescriptors

Adds writable stored typed CutoffHZ, Resonance, Gain and DB. Concrete low/high/band-pass filters hide their unused Gain authoring descriptor. This does not remove the inherited property. Base and other concrete types retain the descriptor.

<a id="createduplicateinstance"></a>
<a id="copycustomstateto"></a>
### Resource copy hooks

Create an empty target of the exact concrete type and copy a coherent cutoff/resonance/gain/preset snapshot. Scalar fields and inherited metadata are copied; there are no owned resource children or transient buffers in the resource graph. ResourceLocalToScene participates in the ordinary PackedScene local-copy session. Copies create their own histories only when instantiated.

## Lifecycle, errors and verification

All resource reads/mutations reject disposal. Standalone Process supports aliases and arbitrary block splits; finite length/input/output validation comes from AudioEffectInstance. Active silent blocks continue filter tails; ProcessSilence remains false, so inactive buses skip normal filter processing after their configured timeout. Property changes preserve history. DSP float overflow clears contaminated histories for later standalone recovery; a failed native effect remains silent until structural recreation.

AudioFilterTests checks all eight runtime resource types, defaults/invalid values, exact concrete Resource/scene-local copies, descriptor visibility, observer failure timing, 72 independent C++ cases/36864 channel samples, analytic responses, stereo separation, arbitrary splits/aliasing, 640 boundary combinations including Nyquist/negative/zero/extreme controls, live history/preset changes and concurrency. Native checks exercise every concrete response after 48000 Hz source resampling and real output-rate filtering, bypass, cleanup and 64 warmed active/live-edit plus 64 paused passes with zero measured managed bytes/custom FAudio allocator calls. CPU also checks 64 prepared live-edit and silent blocks. Public host runs and measurement limits are recorded in the [component](../components/audio-playback.md#frequency-filters). Physical listening, actual multichannel hardware and other platforms remain unverified.

See [ADR 0047](../decisions/audio.md#adr-0047) and [coverage](../coverage/classes/AudioEffectFilter.md).
