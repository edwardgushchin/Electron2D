# AudioStreamPlayer

Last updated: 2026-10-03

**Declaration:** `public class Electron2D.AudioStreamPlayer` · **Source:** [AudioStreamPlayer.cs](../../src/Scene/Audio/AudioStreamPlayer.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [Node](Node.md).

## Description

Non-spatial Node with borrowed Stream and bounded reusable native slots, each owning an independent playback. Each Play creates one fresh playback, grows native slots lazily and adopts it into an unused/oldest slot; replacing a slot disposes its old borrowed playback handle. Defaults: no stream, Autoplay false, StreamPaused false without active voices, Master bus (unknown names query as Master), 0 dB, pitch one, one voice, Stereo mix target and Default playback type. Play requires tree membership; a null stream does nothing. Monophonic streams stop existing voices before Play; polyphonic capacity replaces the oldest active slot. Seek stops active voices and starts one requested cursor. Reducing capacity removes oldest slots at the next Play, preserving newest playback. Stopped pool slots stay internal: HasStreamPlayback becomes false and position zero. Natural completion emits Finished on owner-thread processing when any voice ends; explicit Stop does not. Stream replacement releases old slots without disposing either stream. EnterTree attaches the player, resumes retained detached voices and executes runtime Autoplay; exit pauses/retains cursors. Node process/pause policy pauses native sources; IsPlaying is false while paused, but HasStreamPlayback remains true. Explicit StreamPaused affects existing voices; a new Play samples tree policy. Detached registered players retain the audio configuration owner for reads/mutation/disposal; foreign disposal rejects before logical disposal. Disposal attempts slot cleanup, singleton detach and base Node cleanup even after user callbacks fail. Stored typed properties and an exact scene factory support PackedScene borrowing. Sample uses complete native buffers for sample-capable streams, with streamed fallback otherwise. Default resolves the typed project setting; Stream uses managed mixing. Stereo output is verified; center/surround matrices retain the independent unity-gain LFE route and have literal coefficient and actual native 2/4/6/8-channel PCM checks; physical multichannel hardware is unverified. Prepared stream transitions execute; editor-hint autoplay suppression retains its exact Partial dependency.

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
| `public Electron2D.AudioServer.PlaybackType PlaybackType { get; set; }` | Gets or sets PlaybackType configuration. Default initially; native samples execute for sample-capable streams, with streamed fallback otherwise. |
| System.ArgumentOutOfRangeException | The numeric or enum value is invalid. |
| System.InvalidOperationException | Access is off-owner/capture-owned or native configuration fails. |
| System.ObjectDisposedException | The player is disposed. |
| `public System.Boolean Playing { get; set; }` | Gets or sets whether any voice is playing. Setting true starts at zero; setting false stops all voices. |
| `public Electron2D.AudioStream? Stream { get; set; }` | Gets or sets the borrowed stream. Null initially. Replacement stops current voices before preparing the new resource. |
| System.ObjectDisposedException | The player or assigned stream is disposed. |
| System.InvalidOperationException | Mutation is off-owner/capture-owned. |
| `public System.Boolean StreamPaused { get; set; }` | Gets or sets StreamPaused configuration. False without active voices; prepares one final fading block, then freezes cursors; resume ramps from silence. New Play uses tree pause policy. |
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

## Typed stream parameters

| Complete signature | Contract |
| --- | --- |
| `public void SetParameter<TPlayback, TValue>(PropertyDescriptor<TPlayback, TValue> parameter, TValue value) where TPlayback : AudioStreamPlayback` | Validate the declared writable descriptor, apply it to every prepared voice under the audio gate and retain it for later Play. |
| `public TValue GetParameter<TPlayback, TValue>(PropertyDescriptor<TPlayback, TValue> parameter) where TPlayback : AudioStreamPlayback` | Read authored state or the descriptor's typed revert value. |

### SetParameter and GetParameter

Off-owner/capture-owned mutation and unknown/read-only descriptors reject before state changes. Missing stream, incompatible owner/default and disposed player/stream reject. Cold validation can instantiate a temporary playback before native voices exist. Generic setter callbacks can fail after some voice updates; caller code must coordinate that error. Stream replacement clears authored parameters; null looping restores stream policy. PackedScene retains the concrete looping override and interactive SwitchToClipParameter through typed stored descriptors. The latter selects a name at the next active mix and an empty name cancels a pending request; player GetParameter reads authored state, while the interactive handle reports its current index. These explicit authoring/query operations allocate; repeated native mixing is prepared separately.

```csharp
using var stream = AudioStreamMP3.LoadFromFile("res://audio/theme.mp3");
var player = new AudioStreamPlayer { Stream = stream };
player.SetParameter(AudioStreamPlayback.LoopingParameter, (bool?)true);
// Attach the player to a scene; Play/Autoplay owns native output.
```

## Fresh playback and callback phases

Each actual Play invokes Stream.InstantiatePlayback exactly once, including reuse, configured polyphony, monophonic restarts and Seek's replacement. Randomizer history therefore advances by actual plays. Source parameters restore into the fresh playback before adoption. Native frames/ring storage remains slot-owned and reused. Failed preparation leaves an existing polyphonic slot intact; a committed replacement followed by old-playback disposal failure leaves the new child slot-owned for cleanup/retry. A failed Start leaves the adopted slot stopped and retryable. Replaced, trimmed and released borrowed playback handles become disposed; current bus graph edits retain them.

Source factories and Start/Stop/Dispose callbacks cannot reenter player mutation or disposal. Operations clear their guard after exceptions, attempt all stop/teardown callbacks and preserve borrowed streams. Finished handlers run after the stop phase and may start another Play. Allocation checks cover repeated prepared mixing, not fresh Play/factory/cleanup/graph operations. AudioRandomizerTests checks actual native selection/polyphony, old handle disposal, reentrant callbacks and failure recovery; the public host scenario verifies repeated runs on both Linux Wayland renderer backends.


## Stream transitions

Player transitions now execute with 64 stereo lookahead frames, full initial attack and linear frame/native-quantum gain interpolation. Stop and pause synchronously prepare one final block on the scene owner before their source callback/cursor freeze; ordinary mixing runs on the native output thread, serialized by the existing context gate. This keeps Stop callback failures synchronous and permits pooled replacement to dispose its old playback while retaining copied outgoing PCM. Resume starts at zero gain; Seek and oldest replacement overlap the copied fade and fresh attack. Repeated transitions before a native pass accumulate finite copied tails in one prepared block, and their finite-sum validation precedes publication. A paused query is immediately true and its cursor stays fixed after preparation. Explicit teardown discards transient output. Solo gating remains immediate, including LFE; stored gain whose linear value cannot fit finite float rejects before mutation. Source mixing callbacks reject graph configuration/Lock/Unlock reentrancy on either thread and owner preparation rejects player mutation/disposal. Native source buffers have the actual output channel count so independent per-channel gains, spatial pan and the unity-gain LFE path interpolate before an identity FAudio send. Prepared blocks, native PCM and public spatial hosts are checked by [AudioTransitionTests](../../tests/Electron2D.Tests/AudioTransitionTests.cs); physical listening and other targets remain unverified.

`Play` preserves attack after the initial silent lookahead. `VolumeDB` and `VolumeLinear` edits reach the new gain at the following block boundary. `StreamPaused = true` can advance one source block synchronously before its cursor freezes; repeated true is idempotent. `Stop` immediately makes IsPlaying/HasStreamPlayback false and position zero, while one copied fading block may still be audible. It invokes source Stop once and never emits Finished. `Seek` leaves a paused player unchanged. Destroying the player/stream storage discards transient PCM. An unrepresentable VolumeDB gain throws ArgumentOutOfRangeException before stored mutation; final-block mixing and source-stop failures are collected while still closing logical playback.

Reducing polyphony at the next Play transfers removed slots' copied outgoing fades into a retained slot before disposal. Failed disposal still removes each dead slot and reports collected cleanup errors, so a later Play can recover while the newest voice remains owned. Pause notifications likewise attempt every active voice after individual final-block failures. AudioTransitionTests checks retained tail PCM and failed-trim retry.


## Playback role

AudioStreamPlayer inherits Node and plays streams independently of scene position, including music and UI sounds. Use [AudioStreamEmitter](AudioStreamEmitter.md) for a positioned scene source with distance attenuation, stereo panning and listener/Area routing. The emitter inherits Entity and reuses this player through a private child; their public responsibilities and APIs stay distinct under [ADR 0047](../decisions/audio.md#adr-0047).

[Dynamic polyphonic voices](../components/audio-playback.md#dynamic-polyphony) now integrate with this audio ownership/control path; native children inherit enclosing scene mix/pause/spatial controls and retain their own requested bus. Retired sample/input ownership stays owner-affine until cleanup.

[Playlist playback](../components/audio-playback.md#playlist-playback) now executes timed sequences/shuffle/fades through this audio contract, including scene/native fallback, prepared input controls and owner cleanup.
