# VideoStreamPlayback

Last updated: 2026-10-16

`public abstract class VideoStreamPlayback : Resource` in [VideoStreamPlayback.cs](../../src/Scene/Resources/VideoStreamPlayback.cs).
Part of [video playback](../components/video-playback.md), governed by [ADR 0110](../decisions/video-playback.md#adr-0110).

Independent decoder state with a borrowed current-frame Texture. Standalone state stays on its creating thread. Before first Play, player-owned prepared state may follow a caller-coordinated detached scene to its initial tree owner. After first Play it cannot move between owner threads. Custom providers must support that initial cold transfer. Hooks execute synchronously on the owner; the native audio callback never invokes them. Recursive playback operations and direct disposal of player-owned state fail before mutation.

| Public member | Protected implementation hook | Contract |
| --- | --- | --- |
| GetChannels | OnGetChannels | Zero for silence, otherwise one through eight source channels. |
| GetMixRate | OnGetMixRate | Source Hertz, zero for silence; a player requires a positive audio rate. |
| GetLength | OnGetLength | Finite nonnegative seconds; zero means unknown. |
| GetPlaybackPosition | OnGetPlaybackPosition | Finite presentation seconds, including configured negative preroll. |
| GetTexture | OnGetTexture | Borrowed decoder-owned texture, optionally null for a custom provider. |
| IsPlaying / IsPaused | OnIsPlaying / OnIsPaused | Explicit pause preserves active state. |
| Play / Stop | OnPlay / OnStop | Restart preserves pause; stop resets time and retains the last picture. |
| Seek | OnSeek | Finite source seconds; the built-in ignores positions at or beyond duration. Negative time retains picture zero. |
| SetAudioTrack | OnSetAudioTrack | Zero-based track, -1 disables audio; missing tracks are silent. |
| SetPaused | OnSetPaused | Explicit pause independent of scene policy. |
| Update | OnUpdate | Finite nonnegative source delta; prepared decode and PCM submission on the owner. |
| MixAudio | none | Copies interleaved finite source floats into an attached bounded sink. Offset counts floats. Returns accepted frames, zero when full, -1 without a sink. |

All thirteen typed hooks are required. The protected constructor initializes owner affinity. ValidateDisposal and Dispose guard lifecycle and release sink ownership. A custom decoder retains unaccepted PCM and retries it during a later Update; it must reuse working storage. Call GetChannels before submitting source frames. Borrowing the frame does not extend its owning playback's lifetime.

[VideoStreamTheora](VideoStreamTheora.md) supplies the built-in implementation; [VideoStreamPlayer](VideoStreamPlayer.md) owns its queue and scene integration.
