# AudioStreamPlaybackPolyphonic

Last updated: 2026-10-04

**Declaration:** `public sealed class Electron2D.AudioStreamPlaybackPolyphonic` · **Source:** [AudioStreamPlaybackPolyphonic.cs](../../src/Scene/Resources/AudioStreamPlaybackPolyphonic.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioStreamPlayback](AudioStreamPlayback.md). Created by AudioStreamPolyphonic, with no public constructor.

## Description

Owns a fixed child-voice array and independent parent-local Int64 IDs. A voice borrows its resource and owns its independent playback; aliased/scene-owned/already-owned factory results reject without consuming the prior owner. Encoded IDs carry slot in high 32 bits and generation in low 32 bits, starting at one and retaining UInt32 wrap semantics. IDs are meaningful only on their producing mixer. End, parent Stop/restart and StopStream invalidate them; slots recycle after streamed fade or native completion/stop. Inactive child ownership is reclaimed during later owner PlayStream/Stop/Dispose, rather than allocating/disposing inside the audio callback.

The parent is an indefinite control mixer: Start sets active, including zero capacity; active restart clears old children. A finite Start/Seek position does not move the parent timeline; time/loop count remain zero. When children end, active parent mixing returns full silence and stays active for later dynamic calls. Zero-frame mix does not start pending voices or consume a fade. Stop cleans children even when the parent has not started, preventing native pre-start leaks.

All control/mixing serializes on the audio gate. Sample or recording child ownership requires the audio owner, including retained inactive children. Factories, child callbacks and cleanup cannot reenter mutation/mixing/disposal. Scene queries borrow the mixer and reject direct Dispose while attached. Custom child callbacks must support serialized owner/native threads and keep warmed processing allocation-free.

## Example

```csharp
using var source = new AudioStreamPolyphonic { Polyphony = 2 };
using var mixer = (AudioStreamPlaybackPolyphonic)source.InstantiatePlayback();
mixer.Start();
long id = mixer.PlayStream(sound, playbackType: AudioServer.PlaybackType.Stream);
Vector2[] frames = mixer.MixAudio(1, 256); // Caller-owned CPU result.
mixer.SetStreamVolume(id, -6);
mixer.StopStream(id);
mixer.MixAudio(1, 256); // Final streamed fade.
mixer.Stop();
```

`sound` is a borrowed live AudioStream. Native samples instead emit actual output independently of MixAudio; source bytes never copy per quantum.

## API summary

| Full signature | Contract |
| --- | --- |
| `public const long InvalidID = -1` | No free voice/null stream. |
| `public long PlayStream(AudioStream? stream, double fromOffset = 0, float volumeDB = 0, float pitchScale = 1, AudioServer.PlaybackType playbackType = AudioServer.PlaybackType.Default, string bus = "Master")` | Fresh child and parent-local ID; capacity failure returns InvalidID. |
| `public bool IsStreamPlaying(long stream)` | False for invalid/stale/finishing/ended IDs. |
| `public void SetStreamVolume(long stream, float volumeDB)` | Live gain; invalid IDs no-op. |
| `public void SetStreamPitchScale(long stream, float pitchScale)` | Live ratio; invalid IDs no-op. |
| `public void StopStream(long stream)` | Immediate ID invalidation, streamed fade/native stop. |
| `protected override void OnStart(double fromPosition)` | Parent activation; active restart clears children. |
| `protected override void OnStop()` | Stops/releases every owned child, collecting cleanup errors. |
| `protected override bool OnIsPlaying()` | Parent active latch. |
| `protected override double OnGetPlaybackPosition()` | Zero. |
| `protected override void OnSeek(double time)` | Validated no-op. |
| `protected override int OnMix(Span<Vector2> buffer, float rateScale)` | Prepared finite stereo summation/child control. |
| `protected override void ValidateDisposal()` | Owner/phase/borrow guards. |
| `protected override void Dispose(bool disposing)` | Releases transient ownership, then base cleanup. |

## Constant Descriptions

### InvalidID

Returned for null stream or exhausted captured capacity. It cannot name a voice.

## Method Descriptions

### PlayStream

`fromOffset` is finite seconds passed to child Start without float narrowing. Stream voices start on the next nonempty mix; native sample voices start on the audio owner. Default resolves AudioGeneralDefaultPlaybackType. Sample chooses actual finite WAV/MP3/Vorbis samples when supported, otherwise stream. A supplied bus applies to native samples; streamed voices use the parent PCM route. Native rates/effective pitch retain sample-driver limits. Scene parent local gain/pitch, pause/process policy, mix target and emitter panning/attenuation propagate to native children while their requested bus remains independent.

Zero or positive finite stream pitch is valid; zero freezes supporting stream resamplers. Native sample pitch must be positive and within the effective driver range. Gain accepts negative infinity as mute and finite dB whose linear multiplier fits float. Nonfinite offset/ratio/gain, negative ratio and invalid selector throw ArgumentOutOfRangeException; null bus throws ArgumentNullException. Unsupported native rate/ratio throws NotSupportedException. Disposed resource/source throws ObjectDisposedException. Resource self/cycles, callback reentry or shared/borrowed playback throw InvalidOperationException. Failed creation/preparation consumes only newly owned child state; cleanup failure is reported after the retired free slot is consumed.

### IsStreamPlaying

Checks slot/generation and logical finish state. Native EOF is queried immediately; completed samples do not remain live IDs until another allocation. Stream completion becomes visible when a short child read is consumed by mixing. Inactive parent may hold pending stream IDs, matching pre-start queuing.

### SetStreamVolume

Invalid IDs no-op before validating the new value. Stream gain interpolates from previous mixed gain across the next full requested block, including 128-frame chunk boundaries. Native gain updates prepared four-pair coefficients, retaining unity LFE policy; no restart or gain-array allocation. Invalid gain rejects; native coefficient overflow preserves previous configuration. Source/owner/phase errors reject.

### SetStreamPitchScale

Invalid IDs no-op. Stream ratios combine with parent rate; nonfinite combined ratio fails during mixing. Native ratios combine with enclosing scene-player local pitch and global speed. Invalid values or unsupported effective native range preserve prior ratio. Setting zero on a native sample throws NotSupportedException.

### StopStream

Invalid IDs no-op. The ID becomes invalid synchronously. A pending streamed start is cancelled without invoking Start. An already mixed stream emits one ramp from its prior gain to zero, then receives queued Stop. Native output stops immediately. Ownership stays bounded in the inactive slot until cold reclamation. No parent Finished event is caused by an individual child end.

### OnStart

Activates an indefinite mixer. Starting an already active parent releases old children; queued scheduling retires them without owner-only disposal inside mixing. Native child pause state follows activation and the enclosing scene context.

### OnStop

Deactivates the parent and attempts Stop and Dispose for every owned child even after failure. Resources stay borrowed. Native pre-start children are also consumed. Cleanup errors combine; logical ownership is removed before reporting.

### OnIsPlaying

Reports the parent latch, independently of children. No natural empty-parent completion is inferred.

### OnGetPlaybackPosition

Reports zero; child cursors are not exposed as parent time.

### OnSeek

Ignores finite time and leaves children unchanged; inherited nonfinite validation still applies.

### OnMix

Inactive returns zero and clears output. Active nonempty demand returns its full size, even with no voices. It starts pending stream children under prepared queued control, sums finite PCM without clipping to unity, zero-pads short reads, and retires ended voices. Native sample children skip managed PCM, avoiding double mixing. Callback/count/nonfinite/overflow failures clear the aggregate, deactivate all voices and issue queued stops before propagating to the existing native containment boundary. Child cleanup is cold, so retained retired slots continue to enforce input/native owner affinity.

### ValidateDisposal

Rejects callback reentry, foreign owner disposal and direct scene/composite-owned borrowed disposal before logical disposal.

### Dispose

Releases all owned child state and handles without disposing source resources. Cleanup continues after user Stop failures.

## Verification and limits

[AudioPolyphonicTests](../../tests/Electron2D.Tests/AudioPolyphonicTests.cs) checks exact streamed ramp/sum/rate/offset/boundary behavior, IDs/capacity/short reads, source/factory/mix/stop failures and copies. Actual FAudio verifies Stream/Sample/default/fallback, gain/pitch/parent pause/volume and completion. [Dynamic polyphony](../components/audio-playback.md#dynamic-polyphony) distinguishes logical native channels, public hosts, allocation and physical/platform limits. Inherited usage tagging and general parameter dependencies remain on their declaring pages.

Animation audio bindings now prepare internal reusable child/native state and share a receiver transport. Ordinary public factory/player calls retain fresh playback semantics; prepared randomizer selection occurs at actual trigger/start. [Audio tracks](../components/scene-animation.md#audio-tracks) records ownership, gain/trim/seek, cache rebuild and native/host limits.
