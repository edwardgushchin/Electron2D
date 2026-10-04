# AudioEffectStereoEnhance

Last updated: 2026-10-04

**Declaration:** `public sealed class Electron2D.AudioEffectStereoEnhance` · **Source:** [AudioEffectStereoEnhance.cs](../../src/Scene/Resources/AudioEffectStereoEnhance.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffect](AudioEffect.md).

## Description

Widens a bus stereo pair by scaling the left/right difference from their center, then delaying either the right channel or a center contribution. PanPullout zero downmixes to mono; one preserves the original width; up to four increases it. At Surround zero, TimePulloutMS delays the right channel. At positive Surround, the delayed center is added to left and subtracted from right. With zero delay, surround pans mono content left. This follows the pinned processor's actual equations.

A bus borrows this resource and owns an [independent instance](AudioEffectStereoEnhanceInstance.md) per output stereo pair. Standalone instances are caller-owned. Each processor snapshots live settings once per block and retains one ring history across edits. Resource and scene-local copies retain configuration, not PCM history. Processing continues over silent input to deliver the delayed tail.

## Example

```csharp
using var stereo = new AudioEffectStereoEnhance { PanPullout = 1.5f, TimePulloutMS = 12 };
AudioServer.AddBusEffect(0, stereo);
// Play sources on bus zero, then remove the borrowed effect.
AudioServer.RemoveBusEffect(0, 0);
```

This owner-thread snippet requires native mixing. `AudioStereoEnhanceTests.RunHost` exercises the public Window/player/effect/capture path.

## API summary

| Declaration | Default | Contract |
| --- | --- | --- |
| `public AudioEffectStereoEnhance()` | — | Creates a neutral stereo effect. |
| `public float PanPullout { get; set; }` | 1 | Finite side gain from 0 through 4. |
| `public float TimePulloutMS { get; set; }` | 0 | Finite delay from 0 through 50 milliseconds. |
| `public float Surround { get; set; }` | 0 | Finite delayed-center contribution from 0 through 1. |
| `protected override AudioEffectInstance OnInstantiate()` | — | Prepares an independent bounded delay ring at output rate. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Adds three stored typed fields. |
| `protected override Resource CreateDuplicateInstance()` | — | Creates an exact-type copy target. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | — | Copies controls without the processing ring. |

## Property descriptions

<a id="panpullout"></a>
### PanPullout

Zero sends the common center to both channels. One preserves their existing width; larger values increase opposite side components. Values outside 0–4 and nonfinite values reject before mutation. Edits affect the next block.

<a id="timepulloutms"></a>
### TimePulloutMS

Delay in milliseconds, truncated to a whole frame at the actual output mix rate. With Surround zero it delays the right channel. With positive Surround it delays the center contribution. Values outside 0–50 and nonfinite values reject before mutation. The history is retained when this value changes.

<a id="surround"></a>
### Surround

Zero selects right-channel delay. A positive value selects delayed-center injection, adding it to left and subtracting it from right; one uses the full center amplitude. Values outside 0–1 and nonfinite values reject before mutation. At zero delay, source and delayed frames coincide.

## Lifecycle, verification and limits

The prepared ring is bounded to the authored 50 ms plus two frames. Process and ProcessSilence use it without per-block storage. Finite PCM arithmetic uses a widened intermediate so a legal large center does not overflow; a result outside finite float PCM clears the ring and reports ArithmeticException. Disposed resources reject access and processing; attached instances remain bus-owned.

[AudioStereoEnhanceTests](../../tests/Electron2D.Tests/AudioStereoEnhanceTests.cs) checks analytic width/center/delay PCM, wrap, split/alias behavior, validation, live edits, copies, overflow recovery, native output/tail/bypass, 2/4/6/8 logical profiles, both public Linux Wayland renderer hosts and warmed allocation. Physical listening, actual multichannel hardware, SDL/OS allocations and other platforms remain unverified. See [coverage](../coverage/classes/AudioEffectStereoEnhance.md) and [ADR 0047](../decisions/audio.md#adr-0047).
