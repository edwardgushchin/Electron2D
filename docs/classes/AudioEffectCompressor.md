# AudioEffectCompressor

Last updated: 2026-10-04

**Declaration:** `public sealed class Electron2D.AudioEffectCompressor` · **Source:** [AudioEffectCompressor.cs](../../src/Scene/Resources/AudioEffectCompressor.cs) · **Component:** [Audio playback](../components/audio-playback.md#linked-compression-and-sidechain).

**Inherits:** [AudioEffect](AudioEffect.md).

## Description

Compresses stereo audio with one linked peak envelope per pair. Detection takes the larger absolute channel magnitude. Decibel excess above Threshold is multiplied by the legacy response coefficient 2.08136898, follows attack/release, and produces reduction `-envelopeDB × (Ratio - 1) / Ratio`. Both channels receive the same reduction and wet makeup gain before blending with original PCM. This is a compressor, not a sample-ceiling limiter.

Each [internal instance](AudioEffectCompressorInstance.md) borrows the resource, fixes its rate at creation and owns an independent envelope. Live resource edits apply as one coherent snapshot on the next block and retain history. Bypass/disable retain state; structural effect-chain edits and output closure invalidate bus-owned instances. Resource and scene-local copies retain the seven settings only. Standalone instances are caller-owned and always detect their supplied input.

Named detection on a bus-owned instance reads the matching stereo pair's current quantum buffer. Buses execute in descending index order. A bus processed earlier supplies its post-effect/post-gain PCM; a later bus supplies direct source PCM plus sends already received. Self detection sees the input at this effect's position, including preceding effects. Empty Sidechain means local input; an absent nonempty name resolves to Master. First access to an unused pair activates it and reads silence. This intentionally preserves processing-point semantics; it does not reorder buses to force every detector to execute first.

Scalar access is serialized. Sidechain edits additionally serialize with the native mix gate, may run off-owner outside audio callbacks, and reject callback reentrancy before mutation. Bus graph configuration remains owner-thread work. Native failures follow the existing silence/owner-report/structural-recreation policy.

## Example

```csharp
AudioServer.BusCount = 3;
AudioServer.SetBusName(1, "Music");
AudioServer.SetBusName(2, "Voice");
using var compressor = new AudioEffectCompressor
{
    Threshold = -18,
    Ratio = 4,
    Sidechain = "Voice"
};
AudioServer.AddBusEffect(1, compressor);
// Route players to Music and Voice, then remove the borrowed effect before disposal.
AudioServer.RemoveBusEffect(1, 0);
AudioServer.BusCount = 1;
```

This partial owner-thread host snippet requires two players and real native mixing. `AudioCompressorTests.RunHost` executes its public Window counterpart.

## API summary

| Declaration | Default | Contract |
| --- | --- | --- |
| `public AudioEffectCompressor()` | — | Creates the linked compressor resource. |
| `public float Threshold { get; set; }` | 0 dB | Finite detector threshold. |
| `public float Ratio { get; set; }` | 4 | Finite nonzero transfer ratio. |
| `public float Gain { get; set; }` | 0 dB | Wet makeup gain. |
| `public float AttackUS { get; set; }` | 20 µs | Nonnegative attack time. |
| `public float ReleaseMS { get; set; }` | 250 ms | Nonnegative release time. |
| `public float Mix { get; set; }` | 1 | Finite wet/dry balance, retaining raw extrapolation. |
| `public string Sidechain { get; set; }` | empty | Exact detector bus name. |
| `protected override AudioEffectInstance OnInstantiate()` | — | Creates an independent envelope at the current output rate. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Seven stored typed settings and inherited metadata. |
| `protected override Resource CreateDuplicateInstance()` | — | Constructs the exact concrete resource copy target. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | — | Copies a coherent settings snapshot without envelope/native state. |

## Constructor Descriptions

<a id="constructor"></a>
### AudioEffectCompressor()

Creates defaults without opening output. Buses borrow the resource; remove it from chains and dispose standalone instances before disposing it.

## Property Descriptions

<a id="threshold"></a>
### Threshold

Decibel detector level, zero initially, ordinarily -60 through zero. Any finite value is stored, including values outside authoring hints. Very negative finite thresholds use a widened log-domain fallback when float division overflows or linear conversion underflows; the envelope remains finite. Zero detected PCM remains zero excess.

<a id="ratio"></a>
### Ratio

Four initially, ordinarily one through 48. One disables reduction; positive raw values below one expand. Negative raw values retain the original equation's stronger downward slope. Zero and nonfinite values throw ArgumentOutOfRangeException before mutation. Extreme expansion may overflow PCM during processing; that throws ArithmeticException, clears the complete output block and resets the envelope.

<a id="gain"></a>
### Gain

Wet signal makeup in decibels, zero initially, ordinarily -20 through 20. Any value with finite linear conversion is accepted, including negative infinity for a silent wet component. NaN and overflowing gain reject before mutation. Gain does not affect the detector or dry component. Resulting PCM must remain finite.

<a id="attackus"></a>
### AttackUS

Microseconds to follow increasing detector excess, 20 initially, ordinarily 20 through 2000. Any nonnegative finite time is retained. Both signs of zero and sub-sample times react immediately; large times can preserve transients for many processed frames. Negative/nonfinite times reject.

<a id="releasems"></a>
### ReleaseMS

Milliseconds to follow decreasing detector excess, 250 initially, ordinarily 20 through 2000. Any nonnegative finite time is retained; both signs of zero release immediately. The envelope advances only while processing blocks. Inactive or bypassed instances retain it. Negative/nonfinite times reject.

<a id="mix"></a>
### Mix

Wet/dry balance, one initially, ordinarily zero through one. Zero supplies exact original PCM while the detector still advances; one supplies the wet signal. Finite raw values outside the ordinary range extrapolate, including a negative dry coefficient above one. Nonfinite values reject; finite output overflow follows the same reset/error policy.

<a id="sidechain"></a>
### Sidechain

Nonnull exact bus name; empty uses local effect input. A nonempty missing name reads Master, rather than falling back to local input. Rename and routing rebuild refresh prepared lookup/buffers, but authored names are not rewritten. Buffer contents depend on processing position as described above, including effects, volume, mute, bypass and upstream sends. Standalone instances ignore the name. The setter takes the shared mix gate before committing; callback reentrancy rejects before mutation. No graph reorder or envelope reset is needed for a name edit.

All changed settings commit before Changed observers run; their errors propagate after commit. Equal assignments do not notify. Disposed resources reject access and processing through borrowing instances.

## Method Descriptions

<a id="oninstantiate"></a>
### OnInstantiate

Creates independent caller-owned state. Bus attachment selects the native pair; the generic instance API remains [AudioEffectInstance](AudioEffectInstance.md). Initial envelope is zero; there is no delay or generated silent-input tail. `ProcessSilence()` inherits false.

<a id="getpropertydescriptors"></a>
### GetPropertyDescriptors

Adds seven stored typed controls and defaults to inherited metadata. Descriptor writes use the same validation, locking and notifications as direct property assignments.

<a id="createduplicateinstance"></a>
### CreateDuplicateInstance

Constructs an exact concrete target for inherited Resource copying.

<a id="copycustomstateto"></a>
### CopyCustomStateTo

Copies all seven controls together, including the immutable string name. No envelope, native pair, buffers or bus ownership transfer.

## Verification and limits

[AudioCompressorTests](../../tests/Electron2D.Tests/AudioCompressorTests.cs) checks seven byte-pinned C++ profiles at the available output rate, split/alias equivalence, linked channels, raw ratio/mix, zero/signed-zero timing, extreme thresholds, overflow recovery, observers, copies and callback guards. Native tests exercise named/self/empty/missing detection, both bus orders, rename, effects, volume, sends, bypass, removal and retained instance identity. Sixty-four warmed CPU active/silent and native active/paused passes measure zero managed bytes/custom FAudio allocator calls. Current Linux x64 native logical 2/4/6/8 profiles and two public Window cycles on each Wayland renderer execute. Physical listening, actual multichannel speakers, 48,000 Hz engine output, SDL/OS allocations and other platforms remain unverified. See [coverage](../coverage/classes/AudioEffectCompressor.md) and [ADR 0047](../decisions/audio.md#adr-0047).
