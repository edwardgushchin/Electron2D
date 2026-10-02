# AudioEffectPitchShift

Last updated: 2026-10-03

**Declaration:** `public sealed class Electron2D.AudioEffectPitchShift` · **Source:** [AudioEffectPitchShift.cs](../../src/Scene/Resources/AudioEffectPitchShift.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffect](AudioEffect.md).

## Description

Shifts the pitch of stereo bus PCM without changing its frame count or playback speed. A bus borrows the resource and owns one [internal instance](AudioEffectPitchShiftInstance.md) per output stereo pair; standalone instances belong to their callers. Each instance prepares two independent phase-vocoder histories and a fixed transform size from the shared [AudioFFTSize](AudioFFTSize.md) preset. PitchScale and Oversampling edits apply at the next processed block. FFTSize edits affect newly instantiated processors only. Resource and scene-local copies retain configuration, not processing history.

The algorithm has an FFT-size and overlap dependent onset delay; higher sizes improve frequency stability while increasing latency. Unit pitch passes PCM through bit-for-bit without delay. Switching into or out of bypass, or changing Oversampling, resets both histories so stale windows cannot replay. The processor accepts silent blocks to flush delayed audio. Oversampling is bounded to 4–32 to keep the transform step positive and CPU work bounded; the pinned setter only checks its lower bound, which otherwise permits a zero-step configuration.

## Example

```csharp
using var pitch = new AudioEffectPitchShift
{
    PitchScale = 2,
    FFTSize = AudioFFTSize.Size512,
    Oversampling = 8
};
AudioServer.Instance.AddBusEffect(0, pitch);
// Run a player routed to bus zero, then remove the borrowed effect.
AudioServer.Instance.RemoveBusEffect(0, 0);
```

This partial owner-thread host snippet requires real audio mixing. `AudioPitchShiftTests.RunHost` exercises the full public Window/player/effect/capture path.

## API summary

| Declaration | Default | Contract |
| --- | --- | --- |
| `public AudioEffectPitchShift()` | — | Creates a unit-pitch resource. |
| `public float PitchScale { get; set; }` | 1 | Positive finite pitch multiplier; 1 bypasses processing. |
| `public int Oversampling { get; set; }` | 4 | Transform overlap factor from 4 through 32. |
| `public AudioFFTSize FFTSize { get; set; }` | `Size2048` | New-instance transform size, 256 through 4096 frames. |
| `protected override AudioEffectInstance OnInstantiate()` | — | Prepares independent stereo spectral state. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Adds three stored typed fields. |
| `protected override Resource CreateDuplicateInstance()` | — | Creates an exact-type copy target. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | — | Copies configuration without histories. |

## Property Descriptions

<a id="pitchscale"></a>
### PitchScale

Positive finite ratio; one initially and passed through unchanged. Two raises every source frequency one octave, and 0.5 lowers it one octave while output duration stays the same. The ordinary authored range is 0.01–16. Zero, negative, NaN and infinities reject before mutation. Changes affect the next block. Values within one-millionth of one take the exact bypass path and reset stale history on transition.

<a id="oversampling"></a>
### Oversampling

Integer overlap factor, four initially. Four through thirty-two are supported; invalid values throw ArgumentOutOfRangeException. A live edit resets the current instance's temporal and phase state, then begins processing with the new transform step. Larger values use more CPU and can improve quality.

<a id="fftsize"></a>
### FFTSize

The named transform frame count. Size2048 initially; Size256, Size512, Size1024, Size2048 and Size4096 are selectable. Max and undefined values reject. Each existing instance retains its prepared size; create a new instance or structurally rebuild the bus effect chain to apply a changed size. The analyzer interprets the same enum as a bin count from a window twice as long, as specified by ADR 0051.

## Lifecycle, verification and limits

The resource uses the current output mix rate when preparing an instance. Bypass leaves PCM unchanged and carries no FFT latency. Active and silent blocks use prepared arrays and create no per-block storage; an internal nonfinite transform or accumulation resets both channels and reports ArithmeticException. Native callback containment follows [AudioEffectInstance](AudioEffectInstance.md). Disposed resources reject access and processing. The source routines are covered by the [Wide Open License notice](../../licence/AudioFFT-WOL-LICENSE.txt).

[AudioPitchShiftTests](../../tests/Electron2D.Tests/AudioPitchShiftTests.cs) checks two byte-pinned C++ profiles at the available 44,100 Hz output rate (maximum observed absolute PCM error 3.23e-5), all five FFT presets, up/down stereo frequency shifts, split/alias equivalence, bypass, live reset, silent tail, overflow recovery, Resource/scene copies and warmed allocation. Native FAudio output passes on 2/4/6/8 logical profiles; public Wayland GPU/compatibility hosts capture shifted tones. Sixty-four warmed active and paused native passes make zero measured managed allocations and zero custom FAudio allocator calls. Physical listening, sound-quality judgment, actual multichannel hardware, SDL/OS allocations, 48,000 Hz engine execution and other platforms remain unverified. See [coverage](../coverage/classes/AudioEffectPitchShift.md) and [ADR 0047](../decisions/audio.md#adr-0047).
