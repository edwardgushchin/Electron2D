# AudioStreamMicrophone

Last updated: 2026-10-03

**Source:** [AudioStreamMicrophone.cs](../../src/Scene/Resources/AudioStreamMicrophone.cs). **Declaration:** `public sealed class AudioStreamMicrophone : AudioStream`. **Inherits:** [AudioStream](AudioStream.md). **Inherited By:** —.

Creates continuous playbacks of the selected recording device.

## Description

The resource owns no native device and has no stored audio properties. AudioServer owns the bounded shared capture ring; each playback owns an independent cursor and cubic history and borrows the resource. Enable [ProjectSettings.AudioDriverEnableInput](ProjectSettings.md#audiodriverenableinput) before starting. New capture requires the audio configuration owner and native OS permission/device availability; failed activation throws before playback becomes active. Factory/resource configuration and mixing can run on workers; playback Start, Stop and Dispose require the audio owner. Mixing/history reset serialize per playback and input ring access has a separate gate.

Start ignores a finite position, stays idempotent while active and prepares history. Initial input waits for the smaller of 50 ms or half the input ring. Available frames are read once; shortages supply silence and never finish playback. Playback time and loop count stay zero, and finite Seek is a no-op. Monophonic player policy applies; different players/playbacks still have independent cursors. Stop releases one automatic capture request. Another microphone or explicit manual request keeps recording. Explicit AudioServer.SetInputDeviceActive(false) pauses global capture while playback remains active. Engine closure stops every request, including standalone playbacks. Device switches reset history on the next mix and use the new source frequency.

Dispose the caller-owned playback before disposing a standalone resource. A player owns its playback and borrows this resource. Disposed source reads fail; playback cleanup can still release capture. Duplication/local-scene graphs copy inherited Resource metadata and create no input device or active playback. Direct speaker monitoring can produce acoustic feedback.

## Example

Partial scene snippet; the existing `window` is an owned Window root. Native input/OS permission is required. A muted player still consumes live microphone data without speaker monitoring.

```csharp
ProjectSettings.Instance.Set(ProjectSettings.AudioDriverEnableInput, true);
using var microphone = new AudioStreamMicrophone();
window.AddChild(new AudioStreamPlayer
{
    Stream = microphone,
    Autoplay = true,
    VolumeLinear = 0
});
Engine.Instance.Run(window);
```

## API summary

| Signature | Contract |
| --- | --- |
| `public AudioStreamMicrophone()` | Monophonic, unknown-duration resource, no input opened. |

## Constructor descriptions

### AudioStreamMicrophone

Creates independent inherited resource metadata. GetLength returns zero, IsMonophonic returns true, IsMetaStream returns false and GetParameterList is empty. InstantiatePlayback returns a new caller-owned AudioStreamPlaybackResampled implementation. No additional public playback type is exposed.

## Protected extension points

| Signature | Behavior |
| --- | --- |
| `protected override AudioStreamPlayback OnInstantiatePlayback()` | Creates an independent internal microphone playback; no capture begins. |
| `protected override string OnGetStreamName()` | Returns Microphone. |
| `protected override Resource CreateDuplicateInstance()` | Creates an empty microphone resource for graph copying. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Validates the source; no custom state beyond inherited Resource metadata exists. |

## Method descriptions

### OnInstantiatePlayback

Called by the inherited disposed-checked factory. Every playback has its own cursor/history; it borrows the source and acquires a capture request only on Start. Player borrowing rules apply to GetStreamPlayback handles.

### OnGetStreamName

Supplies the diagnostic stream name, without exposing device identity as resource state.

### CreateDuplicateInstance

Creates the same concrete resource type without copying devices or transient playback state.

### CopyCustomStateTo

The base copy session handles inherited metadata; this resource has no custom state to copy. Disposed sources reject. Deep, shallow and local-scene copies never share playback cursors.

## Verification and limits

[AudioInputTests](../../tests/Electron2D.Tests/AudioInputTests.cs) checks resources, copies, disabled/failed activation, native SDL conversion/ring behavior, independent cursors, priming, underrun, global pause, multiple-request ownership, disposal, failed native pause recovery, switching, concurrency and native FAudio output. Dummy recording proves callback execution and injected PCM proves native conversion/mixing; neither proves physical microphone quality, OS privacy permission flows or listening. Public Window hosts execute on Linux Wayland GPU/compatibility. See [input flow and measured limits](../components/audio-playback.md#recording-input) and [ADR 0047](../decisions/audio.md#adr-0047). Inherited stream capabilities retain their own coverage dependencies.

Its internal playback advertises the existing owner-thread requirement to synchronized/randomizer containers. Those factories/controls/disposal preflight that affinity so an active recording request cannot be lost through off-owner wrapper cleanup. The microphone's own owner/activation/capture behavior is unchanged.

Interactive parents now prepare child controls on the audio owner, including paused microphone input and request capacity, then schedule selected child Start/Stop under the shared audio gate. Public microphone controls/disposal retain owner checks; preparation alone does not record. Mixed interactive/randomizer/synchronized graphs share cycle/owner validation. See [interactive streams](../components/audio-playback.md#interactive-streams) for timing, lifecycle, native evidence and limits.
