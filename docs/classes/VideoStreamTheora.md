# VideoStreamTheora

Last updated: 2026-10-29

`public sealed class VideoStreamTheora : VideoStream` in [VideoStreamTheora.cs](../../src/Scene/Resources/VideoStreamTheora.cs).
Part of [video playback](../components/video-playback.md), governed by [ADR 0110](../decisions/video-playback.md#adr-0110).

The parameterless constructor creates an empty source. Set inherited File or load an `.ogv` through ResourceLoader. The exact resource factory also restores configuration from `.e2dres` and bundled `.e2dscene` archives. Duplicating the resource copies its path and immutable compressed snapshot; decoder cursors remain independent.

Preparation accepts 1 through 512 MiB of compressed Ogg data, at most 64 logical streams and 16777216 packets per stream. Coded and visible axes are bounded to 16384 pixels. The first Theora stream supplies rational-FPS video; selected zero-based Vorbis audio supplies one through eight channels. -1 and missing audio tracks select silence. Unsupported/corrupt packets fail during preparation. The private pinned decoder validates all data and warms reusable working storage, retaining compressed packets, one opaque RGBA8 ImageTexture and bounded PCM buffers. It never retains the full decoded movie.

4:2:0, 4:2:2 and 4:4:4 source chroma, cropped visible rectangles, rewinds, duplicate frames and selected audio tracks execute. Integer limited-range YCbCr conversion can differ by three byte levels from the independent decoder oracle. Source duration is the maximum selected video/audio clock. Embedded audio retains Vorbis channel order until the player downmixes it.

The private TheoraPlayback implements all required hooks, raw/presentation time, pending PCM and native ownership. Play samples AudioVideoDelayCompensationMSEC with feature overrides; positive values delay the picture. Seek uses uncompensated source seconds. Stop retains the last picture. Track/queue replacement for unchanged compressed data preserves the existing frame identity, sampled delay and raw time. Its ImageTexture can be duplicated/saved as an independent current snapshot; compatible public pixel edits permit subsequent decoder updates.

OnInstantiatePlayback creates this independent state; CreateDuplicateInstance preserves the concrete resource type. No codec handle or backend type is public. Source imports and seeks are cold work; prepared updates, looping and frame publication reuse storage. [NativeVideoDecoder](NativeVideoDecoder.md) describes the private ABI and [video playback](../components/video-playback.md#verification) records checks and platform limits.

Preparation and audio-track replacement report direct native storage refusal as OutOfMemoryException. A failed replacement preserves the active decoder, texture and timing. The [bridge allocation domain](../coverage/performance-producers.md#ogv-bridge-allocation-domain-2026-10-29) does not yet include nested codec allocators.
