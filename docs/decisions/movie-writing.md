# Non-real-time movie writing

Last updated: 2026-10-15

<a id="adr-0109"></a>
## ADR 0109: Typed movie writers over the owned window lifecycle

- Status: Accepted
- Scope: MovieWriter hooks, Engine recording entry points, synchronous FAudio output and built-in recording encoders

### Decision

Keep MovieWriter as an abstract ElectronObject. Register borrowed custom instances with static AddWriter before runtime startup; most recent matching handler wins. Registering and disposing use the creating thread. Extension discovery executes during registration; selection and recording hooks execute serially on the runtime owner, which may differ on a platform application host. Registered writers cannot be disposed while a runtime is reserved, including selection before a recording lease exists. At most eight live custom registrations are retained; a later registration reclaims disposed entries. Built-in encoders are internally owned per recording and follow custom handlers in selection precedence.

Map the virtual callbacks to protected OnGetAudioMixRate, OnGetAudioSpeakerMode, OnGetSupportedExtensions, OnHandlesFile, OnWriteBegin, OnWriteFrame and OnWriteEnd. The first two retain 48000 Hz/stereo defaults. Replace unsafe audio pointers with a borrowed ReadOnlySpan<int> of interleaved full-range signed 32-bit PCM; replace Error-return callbacks with typed C# exceptions. Never expose backend pointers or a Variant payload. The frame Image is borrowed until its callback returns. Begin failure still attempts End for partial-output cleanup; later hook failures abort the run and collect cleanup failures.

Project command-line movie startup into Engine.Run/RunAsync overloads accepting a path and positive FPS. Keep the ordinary window reservation, event delivery, scene activation, fixed/process ordering, deferred work, pause/TimeScale and teardown. Prepare the movie/audio graph before constructing SceneTree, whose constructor already activates Autoplay. Set process and render delta to exactly 1/FPS before TimeScale; use the same fixed-step scheduler with explicit fixed-cadence accumulation, using a rational integer physics-tick remainder, without realtime jitter smoothing, catch-up dropping or MaxFPS waits. A low movie FPS must execute all due physics steps. RunAsync still yields through the existing application scheduler. GetWriteMoviePath returns the unchanged requested path through startup, hooks and teardown, and empty after release. Do not expose a second game loop or a simulated successful headless capture.

Use the existing FAudio graph, sources, sample voices, buses and effects. A private bridge, compiled against the pinned FAudio internals in the same target, creates a mastering voice with no SDL output stream and drives the existing engine procedure synchronously. Its state uses the same ordinary native voice destructor. No vendored source modification or second mixer is introduced. Recreate prior cold output/effect preparation at the movie rate, while rejecting independently attached playback instead of silently replacing it. Prepare maximum-quantum float storage once; retain unused samples between video frames. Return cumulative floor(frameCount * mixRate / FPS) audio frames, alternating block lengths when needed instead of the pinned truncated-rate drift. Output-device switching rejects during recording; output latency is zero. Input/microphone remains a separately timed external device and is not a deterministic recording source.

The PNG sequence backend writes RGB8 frames and 32-bit PCM WAV, with the initial pixel dimensions fixed for the recording. Later viewport size changes use centered cover cropping and bilinear resize. Preserve explicit loss of alpha. Readback, resizing, encoding and filesystem operations are cold offline work and may allocate/block; the prepared mixer path retains ADR 0014's zero-allocation requirement. The WAV RIFF size limit rejects before appending an overflowing frame. MovieWriterMixRate and MovieWriterSpeakerMode project the existing editor/movie_writer configuration roles and are consumed by the built-in handler.

All applicable encoder behavior remains implementation work: the generic hooks and PNG/WAV path do not implement built-in AVI/MJPEG or OGV/Theora/Vorbis. AVI needs a RIFF interleaver/index over the existing JPEG encoder. OGV needs native Theora/Vorbis encoding plus Ogg muxing through the source/native packaging gates, not merely the current Vorbis decoder. Implementing that layer is authorized by this decision and requires no separate user command. Keep the class Partial with those exact mechanisms; do not invent inert codec switches or exclude formats for backend convenience. The next codec slice must re-evaluate prerequisites and extend the existing recording flow.

### Verification boundary

Apply ADRs 0001/0003/0004/0014/0016/0021/0047/0045/0090/0095. Exercise actual pixels and native PCM, non-divisible clocks, stream/sample transport, channel profiles, lifecycle/failure cleanup and independent PNG/WAV decoding. Check cold registry errors without native initialization, prepared mixer allocations, fresh package/NativeAOT operation and ordinary Engine scheduling regressions. Local Linux execution does not establish foreign hosts, physical input/listening, full allocator totals or owner acceptance.
