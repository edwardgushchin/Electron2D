# Speech Dispatcher qualification consumer

This public-only consumer uses the same checks as the root runner and Windows TTS consumer. It references the source project or, with `-p:Electron2DUsePackage=true`, an exact candidate Core package. Supply an independently qualified Linux native runtime through the loader search path. This profile deliberately does not fabricate or qualify a delivered platform package.

Use an installed Speech Dispatcher service, its real synthesis module and a private Pulse sink. Set `SPEECHD_ADDRESS` to that service's Unix/inet address and `ELECTRON2D_TTS_PULSE_SOURCE` to the private sink's monitor. Route only the test producer to that sink in its private configuration. Never record a system/default monitor containing other applications.

Build the two independent native inspectors with the host SDK. Set inspectors and receipts to absolute directories, configure the candidate Core package feed, and supply the native loader path:

```sh
cc -O2 tools/text-to-speech/linux-voices.c $(pkg-config --cflags --libs speech-dispatcher) -o "$inspectors/voices.exe"
cc -O2 tools/text-to-speech/linux-loopback.c $(pkg-config --cflags --libs libpulse-simple) -lm -o "$inspectors/loopback.exe"
dotnet run --project tests/TTS.SpeechDispatcher.Consumer -c Release -p:Electron2DUsePackage=true -- "$inspectors" "$receipts"
dotnet run --project tests/TTS.SpeechDispatcher.Consumer -c Release -p:Electron2DUsePackage=true -- protocol
dotnet run --project tests/TTS.SpeechDispatcher.Consumer -c Release -p:Electron2DUsePackage=true -- disabled
```

For source qualification, build this project without Electron2DUsePackage on a configured runtime build host, then execute the portable TTS.SpeechDispatcher.Consumer.dll on Linux with the same native loader profile and arguments. The current source receipt uses the Windows build host and real WSL Linux execution; it does not establish an unconfigured Linux source-build toolchain.

The `.exe` filenames are shared harness names; these two Linux files are native ELF executables. The voice inspector independently calls libspeechd. The capture helper records only the explicitly configured private producer sink, checks the consumer PID, and writes PCM16 stereo at 44.1 kHz, ready/stop markers and capture metadata. This is a separate system producer, so Linux private-sink capture is not engine-process loopback or engine audio-bus capture.

The `protocol` mode uses a synthetic SSIP peer through the public display API. It checks fragmentation across owner pumps and cold queries, source marks, framing/UTF-8/size failures, unsolicited cancellation, disconnected paused queues and unavailable endpoints. It makes no sound-output claim. The real-provider profile separately requires actual PCM signal/silence, pause/resume, duration and pitch changes, boundaries, queue/callback lifetime and active/idle allocation checks.

Repeat with a fresh candidate Core package, the root runner and strict full-trim NativeAOT on the actual host. Preserve exact managed/native/provider/tool/package identities. System-provider behavior, physical listening, external native heaps, FreeBSD and the full delivered-package platform matrix require separate evidence. See the [owning component](../../docs/components/text-to-speech.md).
