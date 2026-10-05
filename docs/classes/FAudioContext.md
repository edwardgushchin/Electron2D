# FAudioContext

Last updated: 2026-10-05

**Declaration:** `internal sealed unsafe partial class Electron2D.FAudioContext : IDisposable` · **Source:** [FAudioContext.cs](../../src/Servers/Audio/FAudioContext.cs) · **Component:** [Audio playback](../components/audio-playback.md).

## Description

Internal owner of the native output engine/master voice, prepared stream/sample voices, bus graph, FAPO lifetime and callback procedure. Actual device format supplies mix rate, stereo-pair channel arrangement and native quantum. Preparation/configuration require the AudioServer owner; the shared mix gate serializes native callbacks and graph edits. A prepared capture buffer and callback counters support scoped native tests without exposing a backend API to applications.

The native backend is delivered under `runtimes/<RID>/native` as `libFAudio.so.0` on Linux, `libFAudio.0.dylib` on macOS or `FAudio.dll` on Windows. [NativeLibraries](NativeLibraries.md) resolves it after loading the shared SDL3 core. Applications need no backend references or loader setup. macOS producers and fresh consumers passed; complete full-suite acceptance remains pending. Windows x86/x64/ARM64 package resolution/full-suite checks are connected with target execution pending.

Android selects `libFAudio.so` from its transitive native package and SDK APK library directory. The SDLActivity test host checks nonzero finite bus PCM, playback progress, output latency and two complete engine lifecycles. Its headless CI profile uses SDL dummy output; physical output/listening remains separate. Local x64/ARM64 execution passed; 32-bit and CI acceptance remains required.

Bus input voices execute before descending public-effect voices, initializing [prepared detector PCM](FAudioBusBuffer.md) from direct sources. Final effects/gain publish current-quantum buffers and sends. Source/sample activity maps to the shared bus activity, which is advanced once per quantum. Input taps and voices participate in graph replacement, native error reporting and output teardown. Closing state suppresses further mixing; the engine joins its native worker while explicit held mix locks are temporarily released, then releases master/engine and callback lifetime.

## Verification and limits

Web uses the same static FAudio/SDL3 graph through named archive modules in Mono's generated P/Invoke tables. Allocator/procedure imports pass pointer-sized callback addresses; typed Cdecl casts remain at their call sites. The browser host yields between owner-thread frames, captures finite nonzero PCM and verifies progress/latency over two balanced lifecycles. This check covers the headless browser output bridge, not physical speakers or the full desktop DSP suite.

iOS/tvOS use the transitive static FAudio archive and SDL core with executable-symbol imports, including the existing engine-owned output bridge and custom allocator/procedure callbacks. Apple native tests require captured finite nonzero PCM, progress/latency and two balanced engine lifecycles with SDL dummy output. Actual simulator execution remains pending; physical device audio is not inferred from compilation or capture.

Generic audio resource/runtime/bus/effect/sample checks verify owned voice and routing behavior. [AudioCompressorTests](../../tests/Electron2D.Tests/AudioCompressorTests.cs) verifies ordered sidechain PCM and repeated active/paused callback allocations. Current native output is verified on Linux x64 with logical 2/4/6/8 channel profiles; physical multichannel devices, listening, SDL/OS allocations and other platforms remain separate gates. See [ADR 0047](../decisions/audio.md#adr-0047).

## Live output transport

[FAudioContext.Output.cs](../../src/Servers/Audio/FAudioContext.Output.cs) uses two engine-owned native exports compiled beside the unmodified pinned FAudio source. A replacement SDL stream retains the engine format/graph and original callback workload. The managed procedure supplies silence while replacement joins driver callbacks, and temporarily releases explicit caller-held mix locks to avoid stream-lock/mix-lock inversion. Opened-device buffering and queued PCM refresh a cached callback snapshot; public reads never wait for a callback. Output preference/name ownership remains on AudioServer. The helper's four-field SDL platform payload and FAudio layout are pinned and compiled into the same CMake target; library overrides must supply both exports.
