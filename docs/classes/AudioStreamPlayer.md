# AudioStreamPlayer

Last updated: 2026-10-01

**Declaration:** `public class Electron2D.AudioStreamPlayer` · **Source:** [AudioStreamPlayer.cs](../../src/Scene/Audio/AudioStreamPlayer.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [Node](Node.md).

## Description

Non-spatial Node with borrowed Stream and owned pooled playbacks/native voices. Defaults: no stream, Autoplay false, StreamPaused false without active voices, Master bus (unknown names query as Master), 0 dB, pitch one, one voice, Stereo mix target and Default playback type. Play requires tree membership; a null stream does nothing. Monophonic streams stop existing voices before Play; polyphonic capacity replaces the oldest active slot. Seek stops active voices and starts one requested cursor. Reducing capacity removes oldest slots at the next Play, preserving newest playback. Stopped pool slots stay internal: HasStreamPlayback becomes false and position zero. Natural completion emits Finished on owner-thread processing when any voice ends; explicit Stop does not. Stream replacement releases old slots without disposing either stream. EnterTree attaches the player, resumes retained detached voices and executes runtime Autoplay; exit pauses/retains cursors. Node process/pause policy pauses native sources; IsPlaying is false while paused, but HasStreamPlayback remains true. Explicit StreamPaused affects existing voices; a new Play samples tree policy. Detached registered players retain the audio configuration owner for reads/mutation/disposal; foreign disposal rejects before logical disposal. Disposal attempts slot cleanup, singleton detach and base Node cleanup even after user callbacks fail. Stored typed properties and an exact scene factory support PackedScene borrowing. Explicit Sample playback rejects because sample storage is absent; Default/Stream execute. Stereo output is verified; center/surround matrices retain the independent unity-gain LFE route and have literal coefficient and actual native 2/4/6/8-channel PCM checks; physical multichannel hardware is unverified. Editor-hint suppression and reference transition ramps for start/stop/pause/gain are precise Partial gaps in coverage; current native transitions are immediate.

## API summary

| Full signature | Contract |
| --- | --- |
| `public AudioStreamPlayer()` | Creates an empty player with one voice capacity, Master bus and unity gain/rate. |
| `public event System.Action? Finished` | Occurs when one or more voices complete naturally during an owner-thread frame. |
| Lifecycle | Explicit Stop and stream replacement do not emit Finished. |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Projects the inherited typed callback; see the concrete behavior above and base-class contract. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Projects the inherited typed callback; see the concrete behavior above and base-class contract. |
| `public System.Double GetPlaybackPosition()` | Gets the current position of the most recently started voice. Concrete stream seconds including resampling prefetch; zero without an active/paused voice. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Projects the inherited typed callback; see the concrete behavior above and base-class contract. |
| `public Electron2D.AudioStreamPlayback GetStreamPlayback()` | Returns the most recently prepared borrowed stream playback. The active player-owned playback; stopped pool slots are not exposed. |
| System.InvalidOperationException | No playback is prepared. |
| `public System.Boolean HasStreamPlayback()` | Gets whether a prepared stream playback is available. True while an active or paused voice exists. |
| `public System.Boolean IsPlaying()` | Gets whether any voice is actively playing rather than paused. False for an empty/stopped player. |
| `protected override System.Void OnNotification(System.Int32 what)` | Projects the inherited typed callback; see the concrete behavior above and base-class contract. |
| `public System.Void Play(System.Double fromPosition = 0)` | Starts a voice at a requested stream time, replacing the oldest when capacity is full. |
| fromPosition | Finite seconds; zero initially. |
| System.InvalidOperationException | The node is detached or native output is unavailable. |
| System.ArgumentOutOfRangeException | The time is nonfinite. |
| `public System.Void Seek(System.Double toPosition)` | Stops active voices and starts one voice at the requested time. |
| toPosition | Finite seconds. |
| `public System.Void Stop()` | Stops all voices without emitting Finished. |
| `protected override System.Void ValidateDisposal()` | Projects the inherited typed callback; see the concrete behavior above and base-class contract. |
| Lifecycle | A detached player retaining audio registration still requires its configuration owner. |
| `protected override System.Void ValidateMutation()` | Projects the inherited typed callback; see the concrete behavior above and base-class contract. |
| `public System.Boolean Autoplay { get; set; }` | Gets or sets Autoplay configuration. False; actual EnterTree starts a configured stream. |
| System.ArgumentOutOfRangeException | The numeric or enum value is invalid. |
| System.InvalidOperationException | Access is off-owner/capture-owned or native configuration fails. |
| System.ObjectDisposedException | The player is disposed. |
| `public System.String Bus { get; set; }` | Gets or sets Bus configuration. Master initially; absent names query and route as Master while retaining their requested identity. |
| System.ArgumentOutOfRangeException | The numeric or enum value is invalid. |
| System.InvalidOperationException | Access is off-owner/capture-owned or native configuration fails. |
| System.ObjectDisposedException | The player is disposed. |
| `public System.Int32 MaxPolyphony { get; set; }` | Gets or sets MaxPolyphony configuration. One initially; oldest voices are replaced when all slots are active. |
| System.ArgumentOutOfRangeException | The numeric or enum value is invalid. |
| System.InvalidOperationException | Access is off-owner/capture-owned or native configuration fails. |
| System.ObjectDisposedException | The player is disposed. |
| `public Electron2D.AudioStreamPlayer.MixTarget MixTargetMode { get; set; }` | Gets or sets MixTargetMode configuration. Stereo initially; undefined values reject. |
| System.ArgumentOutOfRangeException | The numeric or enum value is invalid. |
| System.InvalidOperationException | Access is off-owner/capture-owned or native configuration fails. |
| System.ObjectDisposedException | The player is disposed. |
| `public System.Single PitchScale { get; set; }` | Gets or sets PitchScale configuration. One initially; finite strictly positive values only. |
| System.ArgumentOutOfRangeException | The numeric or enum value is invalid. |
| System.InvalidOperationException | Access is off-owner/capture-owned or native configuration fails. |
| System.ObjectDisposedException | The player is disposed. |
| `public Electron2D.AudioServer.PlaybackType PlaybackType { get; set; }` | Gets or sets PlaybackType configuration. Default initially; explicit native sample playback requires a sample-capable stream. |
| System.ArgumentOutOfRangeException | The numeric or enum value is invalid. |
| System.InvalidOperationException | Access is off-owner/capture-owned or native configuration fails. |
| System.ObjectDisposedException | The player is disposed. |
| `public System.Boolean Playing { get; set; }` | Gets or sets whether any voice is playing. Setting true starts at zero; setting false stops all voices. |
| `public Electron2D.AudioStream? Stream { get; set; }` | Gets or sets the borrowed stream. Null initially. Replacement stops current voices before preparing the new resource. |
| System.ObjectDisposedException | The player or assigned stream is disposed. |
| System.InvalidOperationException | Mutation is off-owner/capture-owned. |
| `public System.Boolean StreamPaused { get; set; }` | Gets or sets StreamPaused configuration. False without active voices; pauses/resumes existing voices without advancing cursors. New Play uses tree pause policy. |
| System.ArgumentOutOfRangeException | The numeric or enum value is invalid. |
| System.InvalidOperationException | Access is off-owner/capture-owned or native configuration fails. |
| System.ObjectDisposedException | The player is disposed. |
| `public System.Single VolumeDB { get; set; }` | Gets or sets VolumeDB configuration. Zero dB initially; negative infinity silences volume-scaled channels. The center/surround low-frequency route retains unity player gain. |
| System.ArgumentOutOfRangeException | The numeric or enum value is invalid. |
| System.InvalidOperationException | Access is off-owner/capture-owned or native configuration fails. |
| System.ObjectDisposedException | The player is disposed. |
| `public System.Single VolumeLinear { get; set; }` | Gets or sets linear gain. One initially; zero maps to negative infinity dB. |
| System.ArgumentOutOfRangeException | The gain is negative or nonfinite. |

## Verification and limits

[Audio verification](../components/audio-playback.md#verification) distinguishes CPU behavior, actual native mixed PCM, public host lifecycle, packaging and physical listening. [ADR 0047](../decisions/audio.md#adr-0047) owns the backend/decoder boundary. Inherited members are documented on their declaring class.

[Own reference coverage](../coverage/classes/AudioStreamPlayer.md) retains missing and Partial members separately.
