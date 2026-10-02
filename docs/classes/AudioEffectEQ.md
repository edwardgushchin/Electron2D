# AudioEffectEQ

Last updated: 2026-10-02

**Declaration:** `public class Electron2D.AudioEffectEQ` · **Source:** [AudioEffectEQ.cs](../../src/Scene/Resources/AudioEffectEQ.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffect](AudioEffect.md). **Inherited By:** [AudioEffectEQ6](AudioEffectEQ6.md), [AudioEffectEQ10](AudioEffectEQ10.md), [AudioEffectEQ21](AudioEffectEQ21.md).

## Description

Fixed-band graphic equalizer resource. The directly constructed base and EQ6 use six bands centered at 32, 100, 320, 1000, 3200 and 10000 Hz; EQ10 and EQ21 use their documented fixed layouts. An effect bus borrows the resource and owns an independent [AudioEffectEQInstance](AudioEffectEQInstance.md) for every output stereo pair. Every instance prepares one band filter per channel at the actual output rate and reads current gains once at the start of each processed block. Live gain edits retain filter histories. Bypass/disable retains instances; structural edits and output closure replace them.

## Example

Partial snippet in an audio-owner host; remove the effect before caller-owned resource disposal:

```csharp
using var eq = new AudioEffectEQ10();
eq.SetBandGainDB(2, 6); // 125 Hz
eq.SetBandGainDB(8, -6); // 8 kHz
AudioServer.Instance.AddBusEffect(0, eq);
AudioServer.Instance.RemoveBusEffect(0, 0);
```

AudioEQTests.RunHost executes a public Window/Autoplay/EQ/capture path on both current Wayland renderers.

## API summary

| Signature | Contract |
| --- | --- |
| `public AudioEffectEQ()` | Six-band zero-gain configuration. |
| `public int GetBandCount()` | Fixed preset count. |
| `public float GetBandGainDB(int bandIndex)` | Reads one authored DB gain. |
| `public void SetBandGainDB(int bandIndex, float volumeDB)` | Publishes one band gain and Changed. |
| `protected override AudioEffectInstance OnInstantiate()` | Creates independent prepared stereo filters. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Adds stored indexed `BandDB/{frequency}HZ` fields. |
| `protected override Resource CreateDuplicateInstance()` | Creates a six-band base copy target. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies every band DB value. |

## Method Descriptions

### GetBandCount

Returns six for the base/EQ6, ten for EQ10, or twenty-one for EQ21. It rejects disposed resources.

### GetBandGainDB

Returns the stored decibel gain for a zero-based band. Every band starts at zero DB. Negative infinity represents a silent band. Negative or out-of-range indices throw ArgumentOutOfRangeException; disposal throws ObjectDisposedException.

### SetBandGainDB

Stores finite decibels whose linear conversion fits a float, or negative infinity for silence. NaN, positive infinity, gain overflow and invalid indices reject without mutation. Unchanged values do not notify. Changed runs after the value commits; a throwing observer does not roll it back. Processing samples all band gains coherently once per block through the resource gate, with independent instance histories and no per-block allocation.

### OnInstantiate

Prepares independent left/right filters at AudioServer's actual mix rate. The resource remains borrowed; disposal causes later processing to fail through the ordinary effect error path. The instance reads the rate again per block and refreshes coefficients if it changes.

### GetPropertyDescriptors and copy hooks

Expose one writable, stored `BandDB/{frequency}HZ` descriptor per preset band; fractional center labels truncate to integer hertz like the pinned authoring fields. Resource and scene-local copies preserve the exact concrete type and DB values, without histories or native ownership. The base uses six-band copy construction; descendants override it.

## Lifecycle, errors and verification

Standalone `Process` accepts aliased input/output and arbitrary block boundaries. Each band filters the original stereo input independently; weighted band outputs sum in preset order. Output overflow throws ArithmeticException, clears contaminated history and permits standalone recovery after gain correction. Normal active silent input advances filter tails; ProcessSilence remains false for inactive buses. FAudio contains native callback errors and reports them at the owner frame.

AudioEQTests checks all four types, band indices/defaults/authoring, invalid values, exact Resource and scene-local copies, observer failure, independent and aliased processing, selective live responses, native PCM/bypass, overflow recovery and 64 warmed active/idle intervals with zero measured managed bytes/custom FAudio allocator calls. Three [byte-pinned C++ cases](../../tests/Electron2D.Tests/Fixtures/Audio/EQ/PROVENANCE.md) match 1,536 ordinary channel samples with maximum absolute error `1.758337E-06`. Public Wayland GPU and compatibility hosts observed 15,435 captured processed frames each. Physical listening, actual multichannel devices, external native allocations and other platforms remain unverified.

See [ADR 0047](../decisions/audio.md#adr-0047), [component](../components/audio-playback.md#graphic-eq) and [coverage](../coverage/classes/AudioEffectEQ.md).
