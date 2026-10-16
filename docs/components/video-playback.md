# Video playback

Last updated: 2026-10-16

[ADR 0110](../decisions/video-playback.md#adr-0110) owns the runtime video family.
[VideoStream](../classes/VideoStream.md) is reusable Resource configuration,
[VideoStreamPlayback](../classes/VideoStreamPlayback.md) owns independent decoder state,
[VideoStreamTheora](../classes/VideoStreamTheora.md) supplies the built-in Ogg source,
and [VideoStreamPlayer](../classes/VideoStreamPlayer.md) displays it as a Control.

## Public workflow

Load an `.ogv` with ResourceLoader.Load<VideoStreamTheora>, or set File on a new
source. Assign Stream to a player, attach the control to a scene, then call Play
or enable Autoplay. Players borrow the source; the scene owns its controls.
Resource and scene archives restore exact public types and stored configuration;
fresh-process load prepares a working first picture. Custom sources implement a
typed factory and all playback hooks. MixAudio feeds interleaved source floats into
the attached bounded sink and reports partial acceptance. No user decoder hook
runs on the native audio callback.

Stream preparation validates before replacing live state. Track and queue changes
preserve activity, pause, raw time, sampled delay and the built-in frame identity.
A second TextureRect or Sprite can keep that borrowed Texture through these
changes. A different source retires the previous playback and its frame. Failed
preparation leaves the old cohort usable; postcommit cleanup errors are reported
after remaining cleanup/publication is attempted. Recursive decoder/player
mutation and disposal are rejected before state changes.

## Decode, clocks and transport

The private existing Ogg 1.3.6 / Vorbis 1.3.7 / Theora 1.2.0 closure now includes
portable decoders and five additional C entry points, nine total with encoding.
All source producers share this recipe; no system decoder or subprocess is needed
by a game. Preparation reads bounded compressed input, indexes packets, validates
the complete selected streams and warms decoder storage. It retains compressed
packets, one RGBA8 frame and bounded PCM rather than a fully decoded movie.
Limits are 512 MiB input, 64 logical streams, 16777216 packets per stream, 16384
pixels per coded/visible axis and eight audio channels. Missing audio is silent.
Theora chroma 4:2:0, 4:2:2, 4:4:4 and cropped visible rectangles execute.

Play restarts without clearing explicit pause. The first eligible process step
uses zero delta. Subsequent video time follows captured process delta and speed;
scene pause, zero Engine time scale and local zero speed freeze both transports.
Looping reuses prepared state and emits no Finished; nonlooping completion stops
before Finished. Stop resets time while retaining the picture. Finite negative
seeks provide first-picture preroll; built-in seeks at/beyond duration are ignored
without clearing valid queued audio or interpolation history.
AudioVideoDelayCompensationMSEC defaults to zero and is sampled with feature
overrides on Play. Presentation time subtracts that delay; explicit seeks address
uncompensated source seconds. The delay may be negative.

The private bounded stereo queue downmixes Vorbis order, omits LFE and reuses
AudioStreamPlaybackResampled and the ordinary FAudio voice/bus path. It applies
source volume to every output route, clears stale queued/lookahead PCM after seek,
and supplies silence on underrun. Missing buses fall back to Master. Video uses
its scene clock independently of AudioServer.PlaybackSpeedScale, including tiny
positive global speeds. Silent video prepares no native output. Cold detached
preparation may transfer to the initial tree owner before first Play; started
state remains affine to that owner.

The existing ImageTexture holds prepared mutable pixels with a revision counter.
GPU and Compatibility caches compare identity plus revision; GPU publication
commits the recorded revision only after submission. Duplicating/saving a frame
captures independent pixels. Compatible public Update/SetImage edits preserve
subsequent decoding; incompatible dimension/format/mipmap edits fail explicitly
at the next frame publication. Ordinary texture roles retain their existing
immutable snapshot sharing.

## Verification

VideoPlaybackTests covers public defaults/validation, independent frame oracles,
rational duration, negative/preroll/seek/restart/pause/stop behavior, first-frame
scene timing, speed/tree pause/loop/completion, custom callback guards and failed
replacement/update/disposal cleanup, detached owner transfer, downmix basis vectors
for one through eight channels, tiny/normal global audio speeds, bounded queue
backpressure, typed loading/cache rollback, duplication, delay settings with
feature overrides, fresh-process bundled scene storage and zero-byte prepared
managed updates. Fixtures are generated patterns/sines with SHA-256 provenance.

VideoRenderingTests executes GPU and Compatibility Wayland hosts. Each records
100 real rendered/FAudio frames, compares every source pixel against independent
FFmpeg RGBA (integer conversion tolerance three levels), verifies a separate
TextureRect alias through track changes, tests ignored-seek audio continuity, gain/bus/silence and native transport cleanup after failing custom Play/Stop/pause hooks, then checks
64 active plus 64 idle prepared render frames for zero owner-thread managed bytes.
Evidence is retained in ignored bin/video-playback-validation.

The native independent oracle verifies 27 chroma/channel profiles plus a two-track
file, every picture, nonmonotonic frame access, PCM samples/counts and audio seeks,
track selection and malformed input. FFmpeg priming is handled explicitly from
the Vorbis identification header; its default trimming loses endpoint samples on
these fixtures. The allocation harness executes 64 complete prepared loops on each
of 28 files with zero malloc/calloc/realloc/free calls. Address/undefined/leak
sanitizers pass the same set. The initial sanitizer run exposed signed wrapping
arithmetic in the upstream codecs; the recipe applies -fwrapv to those codec
targets under GNU/Clang, leaving the bridge under ordinary sanitizer checks.
No vendored source was changed for this decoder slice.

These are local Linux Wayland, decoder and measured-loop checks. Foreign native
execution, hardware synchronization/listening, total process/driver allocation
and user acceptance remain separate gates under ADR 0021. See the native recipe
for reproducible decoder and encoder regression commands.

The public-only package consumer also executes independent decoder pixels, custom factory delegation, scene clocks and a fresh-process bundled archive under NativeAOT with reflection-based JSON disabled. Its GPU/Compatibility recording checks use the same independent pixel/PCM/alias/allocation contracts. The self-contained JIT payload is the input for the dependency/license audit, because NativeAOT emits no application deps.json. Source, ProjectReference and NuGet fresh-consumer delivery checks run with native compilers blocked and LD_LIBRARY_PATH unset. No package or wiki is published by these checks.

During the full verification run, PhysicsActivityTests exposed a warmup mismatch: it measured global/local suspension paths after warming only uninterrupted steps. Temporary operation probes isolated one cold active-step allocation; the test now warms the same suspension/resume scenario before its unchanged zero-byte interval. This calibrates the benchmark and changes no physics runtime behavior. RED traces and the passing full-run result remain in the video validation evidence.
