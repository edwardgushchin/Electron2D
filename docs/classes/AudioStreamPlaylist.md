# AudioStreamPlaylist

Last updated: 2026-10-05

**Declaration:** `public sealed class Electron2D.AudioStreamPlaylist` · **Source:** [AudioStreamPlaylist.cs](../../src/Scene/Resources/AudioStreamPlaylist.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioStream](AudioStream.md). No production subclass.

## Description

Borrows a fixed 64-slot stream list and creates independent [AudioStreamPlaybackPlaylist](AudioStreamPlaybackPlaylist.md) cohorts. StreamCount selects the active prefix; hidden assignments remain editable but are not stored in Resource copies. Meta/monophonic policy is true, finite sampling capability false and typed parameter list empty. Playlist Loop is independent of child loop settings. Native Sample selection of this meta resource falls back to streamed playlist execution.

Duration is the sum of eligible active resources: positive BPM and BeatCount use `beats * 60 / BPM`, otherwise child GetLength. GetBPM returns the first nonzero active-slot tempo, independent of current/shuffled track; it does not follow playback selection. Invalid nonfinite tempo/duration, negative duration or overflowing total reject. Zero-duration/null tracks cannot supply a timed segment and are skipped during playback.

Changed count or any slot assignment (also equal/hidden slot writes) prepares complete new cohorts and leaves registered playback stopped. New factories/queued control preparation finish before configuration commits; failure preserves old graph and ownership. Old cleanup failures report after the new stopped state commits. Equal count only notifies PropertyListChanged; no setter emits Changed. Fade/loop/shuffle are live scalar controls and do not rebuild cohorts.

## Example

```csharp
using var playlist = new AudioStreamPlaylist
{
    StreamCount = 2, Loop = true, Shuffle = false, FadeTime = .3
};
playlist.SetListStream(0, firstTrack);
playlist.SetListStream(1, secondTrack);
player.Stream = playlist;
player.Play();
```

The streams are borrowed and `player` must be attached. Child streams should provide extra PCM after their declared musical end for outgoing fades.

## API summary

| Full signature | Contract |
| --- | --- |
| `public const int MaxStreams = 64` | Fixed authoring capacity. |
| `public AudioStreamPlaylist()` | Empty, Loop=true, Shuffle=false, FadeTime=0.3 s. |
| `public int StreamCount { get; set; }` | Active prefix, zero through 64. |
| `public double FadeTime { get; set; }` | Finite nonnegative outgoing fade seconds. |
| `public bool Shuffle { get; set; }` | Fresh order on Start and cycle wrap. |
| `public bool Loop { get; set; }` | Repeat the playlist at its final boundary. |
| `public AudioStream? GetListStream(int streamIndex)` | Borrowed resource, including hidden slots. |
| `public void SetListStream(int streamIndex, AudioStream? audioStream)` | Transactional structural cohort replacement. |
| `public double GetBPM()` | First nonzero tempo in the active prefix. |
| `public override bool IsMetaStream()` | True. |
| `protected override AudioStreamPlayback OnInstantiatePlayback()` | New independent prepared cohort. |
| `protected override double OnGetLength()` | Summed musical/sample duration. |
| `protected override double OnGetBPM()` | Delegates to GetBPM. |
| `protected override bool OnHasLoop()` | Playlist Loop. |
| `protected override string OnGetStreamName()` | Playlist. |
| `protected override Resource CreateDuplicateInstance()` | Same concrete type. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Active-prefix resources/configuration, standard alias/deep policy. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Typed stored controls and active Stream_index slots. |
| `protected override void ValidateDisposal()` | Rejects factory/mix/metadata/cleanup reentry. |
| `protected override void Dispose(bool disposing)` | Drops borrowed configuration and weak tracking, then Resource cleanup. |

## Constant Descriptions

### MaxStreams

The entire fixed capacity is addressable regardless of StreamCount. Valid slot indices are 0–63.

## Constructor Descriptions

### AudioStreamPlaylist

Creates configuration only. Devices, child playbacks and active order belong to consumers.

## Property Descriptions

### StreamCount

Changed values rebuild all registered cohorts, closing old hidden active states and preparing newly eligible slots. Equal values leave ownership/play state untouched but still notify PropertyListChanged. Invalid values throw ArgumentOutOfRangeException before mutation. Notification exceptions occur after committed state. Hidden source assignments survive shrink/expand.

### FadeTime

Outgoing tail gain starts at unity and decreases over these seconds while the destination starts at full gain. Zero means immediate outgoing stop. Negative/nonfinite values reject before mutation; values above the usual editor hint of one second are valid. Live edits affect the next processed block. No Changed notification.

### Shuffle

True applies the pinned full-range-swap permutation algorithm at each Start and cycle wrap. Mid-cycle edits do not reorder the current cycle. Shuffle may repeat the previous cycle's final track; no no-repeat promise is made.

### Loop

Read at the final track boundary. False completes the sequence and stops all owned children; true generates the next cycle order, increments playback loop count and resets cycle-relative cursor. Child Loop metadata independently controls same-track continuation.

## Method Descriptions

### GetListStream

Returns a borrowed slot identity or null. Invalid index throws ArgumentOutOfRangeException and disposed resource throws ObjectDisposedException.

### SetListStream

Validates live borrowed resource/self/mixed composite cycles, then prepares each live playback's new child cohort on the audio owner where needed. Factories must return independent caller-owned state; duplicate/already-owned/scene-owned instances reject without consuming the prior owner. Preparation failure preserves old configuration, while cleanup failure follows a committed replacement. Resource getters remain available for diagnostics; structural callback reentry rejects. No child resource is disposed by replacement or playlist disposal.

### GetBPM

Reads current active-prefix resource metadata, returning first nonzero tempo or zero. Nonfinite/recursive metadata rejects. Playback shuffle/current position does not affect this resource query.

### OnInstantiatePlayback

Prepares independent ownership for each active nonnull child and its queued controls. Input preparation stays paused until an eligible track actually starts. Hidden children have no playback. Factories and rollback are cold; unchanged count/volume do not allocate in mixing.

### OnGetLength

Adds live child musical durations when BPM/BeatCount are positive, otherwise sample/declared lengths. Fades do not subtract overlap from sequence length. Empty lists return zero.

### OnGetBPM

Projects GetBPM into the inherited metadata hook.

### OnHasLoop

Projects the playlist's live Loop flag, independent of active child loops.

### OnGetStreamName

Returns Playlist for descriptive metadata.

### CreateDuplicateInstance

Creates this concrete Resource type for the standard copy session.

### CopyCustomStateTo

Copies active-prefix resource identities or deep/scene-local replacements plus count/fade/shuffle/loop. Hidden assignments and transient playback states are not stored. Standard Resource metadata copying remains inherited.

### GetPropertyDescriptors

Stored typed controls plus each active `Stream_index` resource field. PackedScene borrows the resource through existing player/emitter properties. No untyped dispatch or file-serialization capability is added.

### ValidateDisposal

Serializes with the audio gate and rejects resource disposal during factories, metadata, mix or child cleanup callbacks before logical disposal.

### Dispose

Clears borrowed configuration/weak tracking only. Independently owned playbacks still need their caller/player cleanup and reject later source consumption after source disposal.

## Verification and limits

FadeTime publishes and reads one atomic Int64 bit pattern, including on 32-bit hosts. The shared portability applications verify exact public fade-setting round trips without opening native output.

[AudioPlaylistTests](../../tests/Electron2D.Tests/AudioPlaylistTests.cs) verifies metadata, configuration, copies/PackedScene and executable behavior. [Playlist playback](../components/audio-playback.md#playlist-playback) records native/host/allocation/platform boundaries. [ADR 0047](../decisions/audio.md#adr-0047) owns the capability; inherited usage/general-parameter/editor/file-authoring gaps remain in coverage.
