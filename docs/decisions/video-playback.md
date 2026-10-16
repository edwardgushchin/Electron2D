# Electron2D video playback decisions

Last updated: 2026-10-16

<a id="adr-0110"></a>
## ADR 0110: Timed Theora textures and queued video audio

- Status: Accepted
- Scope: VideoStream, VideoStreamPlayback, VideoStreamTheora and VideoStreamPlayer

Preserve the applicable reference family under [ADR 0004](product.md#adr-0004):
VideoStream and VideoStreamPlayback are Resource branches; VideoStreamPlayer is a
Control. Typed protected hooks provide decoder extension points. File configuration
and stored player properties belong to resource/scene authoring, while decoder,
texture and PCM queue state belong to each independent playback.

Extend the existing private Ogg/Theora/Vorbis closure selected for
[movie writing](movie-writing.md#adr-0109), rather than adding another codec or a
system executable dependency. This explicitly selects native Vorbis synthesis for
embedded video audio; ordinary audio imports retain the managed decoder selection
in [ADR 0047](audio.md#adr-0047). No public codec handles or backend types escape.
The native build includes the pinned releases' portable decoder source lists and
shares their existing notices, source receipts and static/dynamic producers.

Preparation validates and indexes compressed packets from bounded input, initializes
the selected video/audio decoders and prepares their working storage. Retain
compressed packets and reusable decoded-frame/PCM buffers, rather than retaining
every decoded image or the complete decoded soundtrack. Repeated decoding, loops,
texture publication and prepared audio consumption follow the zero-allocation rule
in [ADR 0014](resources.md#adr-0014). Explicit loading, track changes and seeking are
cold operations. Corrupt or unsupported input fails explicitly without publishing a
partial replacement or leaking a native decoder.

Video decoding and texture publication run on the scene owner. MixAudio accepts
interleaved source PCM into an attached bounded queue and reports accepted frames;
unconsumed data stays with the decoder for a later update. The existing FAudio
stream path consumes that queue, reusing bus routing, source gain and prepared
resampling. Keep process delta, speed, first-frame timing, scene pause, explicit
pause, stop, seek, looping and natural completion coherent across both clocks.
Use the current frame's captured time scale for the soundtrack, including zero,
rather than rereading a setting that game code may already have changed for the
next frame. Sample AudioVideoDelayCompensationMSEC with feature overrides at Play:
positive values delay presentation relative to audio. The reported presentation
timestamp subtracts this delay, while explicit seeks address source seconds;
negative source time retains the first picture during preroll. Track/queue
replacement retains the existing raw time and sampled delay.
Standalone decoder state remains affine to its creating thread. Before first Play,
a player's prepared state may follow a caller-coordinated detached hierarchy to
its initial scene owner; custom decoders must support that cold transfer. Started
state cannot transfer between scene owner threads.
Custom decoder callbacks remain synchronous and owner-affine; the audio callback
never calls user video hooks. Borrowed stream/texture queries do not transfer
ownership. Teardown attempts all owned cleanup even when callbacks fail.

Both active renderers consume the same stable Texture identity. Publish changed
pixels through prepared storage without constructing an Image or allocating a new
GPU texture each frame. Exported images remain independent copies. GUI sizing,
expand, visibility and inherited canvas behavior retain their ordinary contracts.
Track and queue replacement preserve the frame resource for unchanged compressed
video, including consumers borrowing it in another canvas item.
The concrete frame resource is the existing ImageTexture. Duplicating or saving it
captures independent current pixels; compatible public image edits retain later
decoder updates. Changing its pixel dimensions, format or mipmaps rejects the next
incompatible decoded frame rather than publishing mismatched storage.

Verify actual decoded pixels, PCM, durations, seeking, tracks and corruption with
independent files/decoding, then exercise public scene authoring, fresh-process
storage and real GPU/compatibility rendering under [ADR 0090](agent-native.md#adr-0090).
The current native release gate remains Linux Wayland under
[ADR 0021](product.md#adr-0021); source inclusion does not establish foreign-host
execution or physical listening acceptance.
