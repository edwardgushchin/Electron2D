# AudioEffectRecord

Last updated: 2026-10-03

**Declaration:** `public sealed class Electron2D.AudioEffectRecord` · **Source:** [AudioEffectRecord.cs](../../src/Scene/Resources/AudioEffectRecord.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffect](AudioEffect.md).

## Description

Records the front stereo pair of a bus while passing its finite PCM through unchanged. The bus borrows the resource and owns a processing instance per output stereo pair; only pair zero supplies this resource's sample. A standalone `Instantiate()` also prepares a recorder. The resource remains caller-owned. Start creates a new sample and discards the prior one. Stop drains pending PCM. `GetRecording()` can snapshot a running or stopped sample into a new caller-owned, nonlooping [AudioStreamWAV](AudioStreamWAV.md). It returns null before any frames are captured. Format changes affect subsequent snapshots; encoded bytes are created outside the audio callback.

A prepared 1.5-second capture ring separates the callback from a worker that accumulates PCM. The ring is bounded; if the worker cannot drain it, recording stops and `GetRecording()` reports an incomplete sample. Long recordings still consume growing worker-side memory. Resource and scene-local copies retain Format and inherited metadata, never sample data, worker threads or ring storage. Removing the effect or closing output destroys its current instance and sample. Rebuilding a chain selects the new front pair only when prepared; a failed build restores the prior live instance.

## Example

```csharp
using var record = new AudioEffectRecord { Format = AudioStreamWAV.Format.PCM16 };
AudioServer.Instance.AddBusEffect(0, record);
record.SetRecordingActive(true);
// Run the scene with a player routed to bus zero.
record.SetRecordingActive(false);
using AudioStreamWAV? sample = record.GetRecording();
AudioServer.Instance.RemoveBusEffect(0, 0);
```

This is a partial owner-thread host snippet; a player and real native mixing are required for nonempty data. `GetRecording()` returns an audio resource, not a file on disk.

## API summary

| Declaration | Contract |
| --- | --- |
| `public AudioEffectRecord()` | Creates an inactive PCM16 recorder. |
| `public AudioStreamWAV.Format Format { get; set; }` | Selects PCM8, PCM16, IMAADPCM or QOA encoding. |
| `public void SetRecordingActive(bool record)` | Starts a fresh sample or stops and drains. |
| `public bool IsRecordingActive()` | True while the current instance accepts PCM. |
| `public AudioStreamWAV? GetRecording()` | Returns an independent encoded snapshot or null. |
| `protected override AudioEffectInstance OnInstantiate()` | Prepares independent stereo processing state. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Exposes stored typed Format. |
| `protected override Resource CreateDuplicateInstance()` | Creates an empty exact-type copy. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies Format only. |
| `protected override void Dispose(bool disposing)` | Stops the worker; resource disposal invalidates further queries. |

## Property Descriptions

<a id="format"></a>
### Format

Defaults to PCM16. The shared WAV format enum is used directly. Invalid values throw ArgumentOutOfRangeException before mutation. A changed value commits before Changed observers run; observer exceptions propagate. Querying and setting a disposed resource throw ObjectDisposedException.

## Method Descriptions

<a id="setrecordingactive"></a>
### SetRecordingActive

True requires a live prepared instance; otherwise InvalidOperationException is thrown. Every true call starts fresh and removes the previous sample. False stops the worker, drains pending PCM and retains the finished sample. Starting after an incomplete capture resets its failure state. The caller should start before playing or routing the desired source through the bus.

<a id="isrecordingactive"></a>
### IsRecordingActive

Returns false before preparation, after stop, after ring overflow and after output closure. It does not mean nonzero PCM has arrived.

<a id="getrecording"></a>
### GetRecording

Returns null if no frames were captured in the current sample. Otherwise it copies and encodes the currently accumulated PCM and returns a separately owned stereo WAV resource with the native output mix rate and looping disabled. An active query does not stop recording. Quantized formats clip values outside their representable range. A ring overrun or worker failure throws InvalidOperationException rather than silently exporting an incomplete sample. Allocation and encoding occur on the caller thread. Disposed resources reject queries.

## Lifecycle, verification and limits

[AudioRecordTests](../../tests/Electron2D.Tests/AudioRecordTests.cs) checks defaults, invalid formats, stored descriptors, all four codec roundtrips, active snapshots, restarts, aliases, whole-block ring overflow and recovery, copies, failed-chain rollback, real native front-pair PCM, 2/4/6/8 logical channel profiles, cleanup and warmed callback allocation. Current Linux Wayland GPU/compatibility public hosts exercise player-to-bus-to-WAV flow. The native callback makes zero measured managed allocations and zero custom FAudio allocator calls across 64 warmed active and silent passes; worker growth, snapshots, codecs and SDL/OS allocations are outside this measurement. Physical listening, actual multichannel hardware and other platforms remain unverified.

See [internal instance](AudioEffectRecordInstance.md), [coverage](../coverage/classes/AudioEffectRecord.md) and [ADR 0047](../decisions/audio.md#adr-0047).
