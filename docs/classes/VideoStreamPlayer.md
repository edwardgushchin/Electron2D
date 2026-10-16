# VideoStreamPlayer

Last updated: 2026-10-16

`public sealed class VideoStreamPlayer : Control` in [VideoStreamPlayer.cs](../../src/Scene/GUI/VideoStreamPlayer.cs).
Part of [video playback](../components/video-playback.md), governed by [ADR 0110](../decisions/video-playback.md#adr-0110).

Displays a borrowed VideoStream, owns independent decoder/texture/PCM state, and routes its soundtrack through an internal AudioStreamPlayer. Source preparation is cold and transactional: failed preparation preserves the old active cohort. Once replacement commits, cleanup failures are reported after remaining cleanup/publication is attempted. Silent video does not prepare a native output device.

```csharp
using var source = ResourceLoader.Load<VideoStreamTheora>("res://intro.ogv");
var player = new VideoStreamPlayer { Stream = source, Autoplay = true, Expand = true };
var window = new Window();
window.AddChild(player);
Engine.Run(window);
```

| Member | Default and behavior |
| --- | --- |
| constructor | Empty control; MouseFilter.Ignore, internal audio child. |
| Stream | Null; borrowed source, stored. Replacement prepares the first image and stops previous playback. |
| Autoplay | False; starts on scene entry. |
| Expand | False; intrinsic video minimum and natural drawing size. True fills the control rectangle. |
| Loop | False; natural completion restarts without Finished. |
| Paused | False; explicit pause preserves IsPlaying. |
| SpeedScale | 1; finite nonnegative. Zero pauses both clocks. |
| AudioTrack | 0; -1 or missing track is silent. Cold replacement preserves time, pause, activity and built-in frame identity. |
| BufferingMSEC | 500; positive requested duration, prepared power-of-two capacity must fit 2^24 source frames. Cold changes preserve time and frame. |
| Bus | Master; unavailable names use the ordinary Master fallback. |
| Volume / VolumeDB | 1 / 0 dB; nonnegative finite linear gain, negative-infinity dB means silence. DB is stored; linear gain is a view. |
| StreamPosition | Presentation seconds; an accepted seek addresses source time and clears stale PCM/lookahead; an ignored out-of-range seek preserves them. Not stored. |
| GetStreamLength / GetStreamName | Duration or zero; resource name or `<No Stream>`. |
| GetVideoTexture | Borrowed current-frame Texture, or null. Other canvas consumers can use it. |
| IsPlaying | Includes explicit and scene pause. |
| Play / Stop | Play requires tree membership and restarts without clearing pause. Stop resets time, retains the picture and emits nothing. |
| Finished | Emitted once after nonlooping natural completion; handlers may restart playback. |

The first process step advances by zero. Later steps use the captured process delta, local speed, tree pause and captured Engine time scale; video audio stays independent of AudioServer.PlaybackSpeedScale. Zero time scale pauses both transports. Vorbis source channels downmix to stereo and use the ordinary surround/bus transport with gain applied to every output route. Queue overflow reports backpressure; underruns supply silence.

Protected overrides provide descriptors, exact scene factories, intrinsic sizing, notifications and mutation/disposal guards. Decoder callbacks cannot recursively mutate or dispose their player. Notification failure still attempts transport cleanup. Both renderers upload changed pixels under the stable ImageTexture identity; exports and duplicates are independent snapshots. Dispose releases the internal child, native decoder, frame and queue while preserving the borrowed source. Initial detached scene preparation may transfer to the first tree owner before Play; active transfer between owner threads is rejected.

See [verification](../components/video-playback.md#verification) for executable source/archive/backend checks and limits.
