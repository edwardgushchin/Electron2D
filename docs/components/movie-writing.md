# Movie writing

Last updated: 2026-10-15

Owns non-real-time rendered recording through [MovieWriter](../classes/MovieWriter.md), [MovieRecording](../classes/MovieRecording.md) and the internal [PNGMovieWriter](../classes/PNGMovieWriter.md). [ADR 0109](../decisions/movie-writing.md#adr-0109) owns the typed protocol and host adaptations.

## Runtime flow

Engine.Run(window, path, fps) and its RunAsync counterpart reserve the ordinary owned window lifecycle. After native rendering opens, selection probes custom writers newest-first, then the built-in .png handler. Cold output/bus-effect preparation is closed and recreated for the movie rate; independently attached players or standalone samples reject before conversion. Metadata configures an offline FAudio master and the existing audio bus graph before SceneTree construction/Autoplay. Begin receives the initial rendered pixel dimensions. Each successful process step submits a real canvas frame, reads completed viewport pixels, obtains that frame's PCM, and calls the selected writer. Native input, Ready, physics/process, pause, deferred work and scene teardown remain the ordinary runtime paths.

Fixed mode uses a rational integer physics-tick remainder and bypasses realtime jitter compensation/catch-up dropping. One second at 7 FPS contains exactly 60 ticks at the default physics rate, avoiding a floating-accumulator boundary loss. Process/render delta is exactly 1/FPS times TimeScale; fixed physics callbacks execute every due step, including more than MaxPhysicsStepsPerFrame at low FPS. Audio duration uses an independent output clock. For 48000 Hz at 59 FPS, successive stereo blocks have 1626 or 1628 samples and the total after N frames is floor(N*48000/59) stereo frames. Native mixer quanta may prefetch less than one quantum beyond that consumed boundary; unused PCM is retained, not remixed or lost.

The private audio bridge initializes a master with no platform output stream and invokes the existing FAudio engine procedure. Voices, native SRC, source callbacks, ordered effects and meters use the same graph as live output. The native procedure still contains callback failures; the owner checks source/effect failures before publishing a movie frame. Device switching rejects. Last-player detachment retains the offline graph until Engine cleanup. Recording does not make external microphone input deterministic.

## Lifetime and failures

The creating thread registers/disposes custom writers; runtime hooks execute serially on the runtime owner. Registered instances remain borrowed and cannot be disposed while any runtime is reserved. Hook reentry into registration, nested runs and active disposal rejects. Invalid public path/FPS arguments preserve an unconsumed root; after reservation the runtime owns and cleans the root even if selection, metadata, native preparation, Begin, Ready, frame or End fails.

End is attempted after an attempted Begin, including a partially failed Begin. Metadata/selection failures do not invoke End. End runs before scene/native teardown; later cleanup always continues. Callback exceptions remain failures, and distinct cleanup errors aggregate. The requested movie path stays visible through teardown and clears when the reservation releases. Captured Images and PCM spans are borrowed only for OnWriteFrame; retention requires an explicit caller copy.

## Built-in output

A .png path writes basename00000000.png onward and basename.wav. Frame images are opaque RGB8. Size changes use a centered aspect-cover crop and bilinear resize to the initial dimensions. WAV stores little-endian 32-bit signed PCM and finalizes RIFF/data lengths on End, including after a failed frame. RIFF overflow rejects before appending. The destination directory must exist. Prior files matching the exact basename plus eight decimal digits and .png are removed at Begin; other files are preserved. Produced filenames are overwritten; codec/file failure can leave a valid partial recording. Two typed project settings select the built-in audio rate (48000 Hz default, 1000..200000) and speaker mode (Stereo default).

Built-in AVI/MJPEG still needs a RIFF interleaver/index over existing JPEG output; OGV needs executable Theora/Vorbis encoders and Ogg muxing. Custom writers can supply other formats now. Those missing built-in encoders keep the class Partial rather than becoming exclusions.

## Executable checks and limits

MovieWriterTests has a portable registry/argument profile and actual GPU/Compatibility profiles. It checks native 2/4/6/8 channel PCM, stream/native-sample voices, exact process deltas and audio counts, canvas pixels, source frame lifetime, registration/disposal/nested-run guards, Begin/frame/End/Ready failures, restart and independent PNG/WAV reading. Its prepared PCM bracket measures 128 native reads after warmup, separately from allocating image/encoder work. Evidence and package verification are retained in ignored bin/movie-writer-validation/2026-10-15. Foreign platform hosts, physical input/output/listening, complete external/native allocation totals and user acceptance remain unverified.
