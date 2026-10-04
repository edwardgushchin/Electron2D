# FAudioContext

Last updated: 2026-10-04

**Declaration:** `internal sealed unsafe partial class Electron2D.FAudioContext : IDisposable` · **Source:** [FAudioContext.cs](../../src/Servers/Audio/FAudioContext.cs) · **Component:** [Audio playback](../components/audio-playback.md).

## Description

Internal owner of the native output engine/master voice, prepared stream/sample voices, bus graph, FAPO lifetime and callback procedure. Actual device format supplies mix rate, stereo-pair channel arrangement and native quantum. Preparation/configuration require the AudioServer owner; the shared mix gate serializes native callbacks and graph edits. A prepared capture buffer and callback counters support scoped native tests without exposing a backend API to applications.

The native backend is delivered under `runtimes/<RID>/native/libFAudio.so.0` in engine builds, project-reference publishes and NuGet package entries. [NativeLibraries](NativeLibraries.md) resolves that file after loading the shared SDL3 core; its ordinary .NET fallback supports flattened RID-specific NuGet deployments. Applications need no backend references or loader setup.

Bus input voices execute before descending public-effect voices, initializing [prepared detector PCM](FAudioBusBuffer.md) from direct sources. Final effects/gain publish current-quantum buffers and sends. Source/sample activity maps to the shared bus activity, which is advanced once per quantum. Input taps and voices participate in graph replacement, native error reporting and output teardown. Closing state suppresses further mixing; the engine joins its native worker while explicit held mix locks are temporarily released, then releases master/engine and callback lifetime.

## Verification and limits

Generic audio resource/runtime/bus/effect/sample checks verify owned voice and routing behavior. [AudioCompressorTests](../../tests/Electron2D.Tests/AudioCompressorTests.cs) verifies ordered sidechain PCM and repeated active/paused callback allocations. Current native output is verified on Linux x64 with logical 2/4/6/8 channel profiles; physical multichannel devices, listening, SDL/OS allocations and other platforms remain separate gates. See [ADR 0047](../decisions/audio.md#adr-0047).

## Live output transport

[FAudioContext.Output.cs](../../src/Servers/Audio/FAudioContext.Output.cs) uses two engine-owned native exports compiled beside the unmodified pinned FAudio source. A replacement SDL stream retains the engine format/graph and original callback workload. The managed procedure supplies silence while replacement joins driver callbacks, and temporarily releases explicit caller-held mix locks to avoid stream-lock/mix-lock inversion. Opened-device buffering and queued PCM refresh a cached callback snapshot; public reads never wait for a callback. Output preference/name ownership remains on AudioServer. The helper's four-field SDL platform payload and FAudio layout are pinned and compiled into the same CMake target; library overrides must supply both exports.
