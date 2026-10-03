# AudioStreamEmitter

Last updated: 2026-10-03

**Inherits:** [Entity](Entity.md), CanvasItem, Node, ElectronObject

**Source:** [AudioStreamEmitter.cs](../../src/Scene/Audio/AudioStreamEmitter.cs)

**Declaration:** `public sealed class AudioStreamEmitter : Entity`

**Component:** [Audio playback](../components/audio-playback.md)

## Description

Plays borrowed [AudioStream](AudioStream.md) resources through the existing FAudio buses with 2D distance attenuation and stereo panning. A private internal [AudioStreamPlayer](AudioStreamPlayer.md) child owns prepared playbacks and native voices; normal public child enumeration contains only authored scene children. The spatial node owns no stream resource. Its position is in scene canvas coordinates; the root [Viewport](Viewport.md) supplies client width, canvas transform and optional current [AudioListener](AudioListener.md). Active gain and routing refresh each fixed physics step. `Play` also computes the initial matrix and Area route before starting a voice. Without an active listening viewport, output is silent.

## Example

```csharp
using var stream = AudioStreamWAV.LoadFromFile("res://audio/step.wav");
var player = new AudioStreamEmitter { Stream = stream, Position = new Vector2(100, 50), Autoplay = true };
window.AddChild(player);
Engine.Instance.Run(window);
```

The window owns the node; dispose the borrowed stream after the scene finishes.

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `public AudioStreamEmitter()` | — | Creates one private playback child and samples the global panning setting. |
| `public AudioStream? Stream { get; set; }` | null | Borrows a stream; replacement stops prepared voices. |
| `public bool Autoplay { get; set; }` | false | Starts on the first fixed scene step after entry. |
| `public bool StreamPaused { get; set; }` | false | Pauses or resumes existing voices. |
| `public string Bus { get; set; }` | Master | Authored route before an eligible Area overrides it. |
| `public float VolumeDB { get; set; }` | 0 | Source gain in decibels. |
| `public float VolumeLinear { get; set; }` | 1 | Nonnegative linear source gain. |
| `public float PitchScale { get; set; }` | 1 | Positive playback-rate multiplier. |
| `public int MaxPolyphony { get; set; }` | 1 | Positive native voice capacity; oldest voice is replaced when full. |
| `public AudioServer.PlaybackType PlaybackType { get; set; }` | Default | Stream/sample/default selector through the private player. |
| `public bool Playing { get; set; }` | false | Setter starts at zero or stops all voices. |
| `public float MaxDistance { get; set; }` | 2000 | Positive maximum audible scene distance. |
| `public float Attenuation { get; set; }` | 1 | Nonnegative distance-rolloff exponent. |
| `public float PanningStrength { get; set; }` | 1 | Nonnegative stereo-pan multiplier. |
| `public uint AreaMask { get; set; }` | 0 | Collision-layer bits eligible for Area bus routing; zero disables the query. |
| `public event Action? Finished` | — | Raised after natural playback completion on the scene owner frame. |
| `public void Play(double fromPosition = 0)` | — | Starts a fresh voice at finite stream seconds. |
| `public void Seek(double toPosition)` | — | Restarts the active voice at finite stream seconds. |
| `public void Stop()` | — | Stops all voices without `Finished`. |
| `public bool IsPlaying()` | — | Reports at least one playing voice. |
| `public double GetPlaybackPosition()` | — | Most recently started voice position, or zero. |
| `public bool HasStreamPlayback()` | — | Whether an active or paused playback exists. |
| `public AudioStreamPlayback GetStreamPlayback()` | — | Borrows the latest playback handle; replacement invalidates it. |
| `public void SetParameter<TPlayback, TValue>(PropertyDescriptor<TPlayback, TValue> parameter, TValue value) where TPlayback : AudioStreamPlayback` | — | Applies a declared typed stream parameter. |
| `public TValue GetParameter<TPlayback, TValue>(PropertyDescriptor<TPlayback, TValue> parameter) where TPlayback : AudioStreamPlayback` | — | Reads authored or default typed stream state. |
| `protected override void OnNotification(int what)` | — | Refreshes spatial routing on internal physics ticks and starts pending autoplay. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Stores authored audio/spatial configuration and a supported looping override. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | — | Recreates the spatial node and its private playback child. |
| `protected override void Dispose(bool disposing)` | — | Releases child voices and event references; borrowed streams survive. |

## Property descriptions

### `MaxDistance`, `Attenuation` and `PanningStrength`

Distance is measured between source and explicit listener positions in scene units. Without one, the inverse canvas transform locates the viewport center in scene coordinates. A source farther than `MaxDistance` is silent. Within the range, gain is `(max(0.000001, 1 - distance / MaxDistance)) ^ Attenuation`; zero attenuation disables rolloff inside the range. The panning fraction is the source's horizontal offset divided by viewport width, weighted by `PanningStrength` and the sampled `ProjectSettings.AudioGeneral2DPanningStrength` (0.5 default), then clamped to the stereo pair. Listener rotation changes this horizontal axis. The authored volume and bus gains still apply. The three properties reject nonfinite values; distance must be positive and the two strengths nonnegative. A singular canvas transform without an explicit listener rejects spatial refresh.

### `AreaMask` and `Bus`

When `AreaMask` intersects a shaped [Area](Area.md)'s `CollisionLayer` and that Area has `AudioBusOverride=true`, a point query at the source position selects its `AudioBusName`. Equal candidates use the smallest physics RID. Otherwise the authored `Bus` applies. Unknown bus names resolve to Master; the authored name is retained for a later bus creation. Area monitoring flags and the player's collision mask are unrelated. Route changes preserve active playbacks.

### `Stream`, `Autoplay`, `StreamPaused`, `PlaybackType`, `Playing` and gain fields

The private player provides the same borrowed-resource, polyphony, cursor, event and typed-parameter lifecycle as [AudioStreamPlayer](AudioStreamPlayer.md). Autoplay waits for the first fixed step after scene entry. `Playing=true` calls `Play(0)` and `false` calls `Stop()`. A scene with no listening viewport is silent. Public configuration and playback operations enforce owner-thread, capture and disposal guards. `Play` and `Seek` reject nonfinite times. `MaxPolyphony` and `PitchScale` must be positive; `VolumeLinear` must be nonnegative.

## Verification and limits

[AudioSpatialTests](../../tests/Electron2D.Tests/AudioSpatialTests.cs) covers typed scene storage, listener selection, centered/right/attenuated PCM, Area bus capture, route recovery, and 64 warmed spatial point-query/matrix updates without managed allocation or custom FAudio allocator calls. Linux Wayland GPU and compatibility hosts use the SDL dummy audio driver; physical listening and other platforms remain unverified. Shared stream start/stop/pause/gain transitions now execute through the internal player. Editor-hint autoplay suppression retains the [base player's precise dependencies](../coverage/classes/AudioStreamPlayer.md). [Own coverage](../coverage/classes/AudioStreamPlayer2D.md) keeps those dependencies Partial.


## Stream transitions

The internal player shares [AudioStreamPlayer transitions](AudioStreamPlayer.md#stream-transitions), including 64-frame initial lookahead, full attack, gain/pan interpolation, synchronous final-block preparation before pause/stop, frozen paused cursors and copied outgoing fades for Seek/oldest replacement. VolumeDB rejects an unrepresentable linear gain before mutation. The public signatures, scene storage and borrowed stream ownership stay intact. [AudioTransitionTests](../../tests/Electron2D.Tests/AudioTransitionTests.cs) exercises two successive spatial Window hosts on each current Wayland renderer and distinguishes PCM/cursor/cleanup evidence from physical listening and other targets.

Typed interactive SwitchToClipParameter is stored/restored in PackedScene along with Stream, using the same private player parameter path; name selection and an empty cancellation retain exact string types. See [interactive streams](../components/audio-playback.md#interactive-streams).

[Dynamic polyphonic voices](../components/audio-playback.md#dynamic-polyphony) now integrate with this audio ownership/control path; native children inherit enclosing scene mix/pause/spatial controls and retain their own requested bus. Retired sample/input ownership stays owner-affine until cleanup.
