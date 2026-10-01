# Audio playback and routed buses

Last updated: 2026-10-01

## Scope and types

Implements reusable [AudioStream](../classes/AudioStream.md), caller-owned [AudioStreamPlayback](../classes/AudioStreamPlayback.md), prepared cubic [AudioStreamPlaybackResampled](../classes/AudioStreamPlaybackResampled.md), copied [AudioStreamWAV](../classes/AudioStreamWAV.md), typed [AudioWAVImportOptions](../classes/AudioWAVImportOptions.md), non-spatial [AudioStreamPlayer](../classes/AudioStreamPlayer.md) and borrowed singleton [AudioServer](../classes/AudioServer.md). Nested format/loop/playback/speaker/mix selectors retain numeric identities.

Internal FAudioContext owns engine/master/submix voices and native volume meters; FAudioStreamVoice owns callback/ring storage and one borrowed playback; AudioPCMCodec owns cold PCM/IMA/QOA encoding/decoding. Backend bindings and codec types are internal to Electron2D.dll. Vendor manifests pin FAudio 26.09 and qoa-fu; their executable algorithms are preserved, with namespace/visibility/diagnostic integration changes only.

## Runtime flow and invariants

WAV import validates RIFF chunks, PCM/IEEE-float dimensions and finite samples, uses the existing SDL WAV/conversion bindings, then performs typed edits and prepares encoded PCM/IMA/QOA. Encoded bytes and tags are copied; resource duplication is independent. Decode publishes an immutable versioned float PCM snapshot. Previously prepared resources refresh on their setter thread; warmed audio callbacks do not decode/allocate. Invalid compressed input fails on consumption. WAV SaveToWAV uses the existing AtomicFile writer; unsupported compressed save leaves an existing file intact.

A player first prepares independent playbacks, fixed Vector2 quantum buffers and pinned stereo float rings. OnVoiceProcessingPassStart fills exactly the next ring quantum using the public playback extension contract; a single infinite native ring submission avoids repeated native submission allocations. Native source voices use NOPITCH/NOSRC because owned cubic mixing implements source/global pitch. Source callbacks are synchronized with graph/configuration through the engine procedure gate. Callback failures silence the ring and are raised at the owner scene frame; failed voices stop before reporting. The copied test PCM observer is internal instrumentation.

The first owner-bound operation claims the audio configuration thread; passive singleton/rate/speed reads and worker resource mixing do not claim it. Foreign configuration rejects thereafter. Buses are real native submix nodes with named sends to earlier indices. Unknown/later/self sends resolve to Master. Live reorder/remove/add/send edits prepare replacement nodes before releasing old ones, redirect existing source sends without replacing playback/cursor/history/pause/polyphony, and apply gains/mute/solo. Errors commit configuration but close all native output coherently for a later retry. Routing refresh attempts all players after individual failures. Queries report actual bus peaks, output devices, rate, channel arrangement and native mix clocks.

Play supports monophonic policy, bounded configured polyphony and oldest replacement; Seek restarts one voice. Detached nodes pause/retain playback; disposal releases it. Engine.Run and manual Engine.Stop close native resources even after loop/callback failure. The singleton remains usable for another lifecycle, and borrowed streams are never disposed by the player.

## Example

Partial scene snippet; attach this player to a root Node/Window. Runtime Autoplay starts on entry. Dispose the stream after the owned scene finishes.

```csharp
using var stream = AudioStreamWAV.LoadFromFile("res://audio/click.wav");
var player = new AudioStreamPlayer { Stream = stream, Autoplay = true, VolumeDB = -12 };
window.AddChild(player);
Engine.Instance.Run(window);
```

## Dependencies and limits

[ADR 0047](../decisions/audio.md#adr-0047) selects FAudio/SDL3 and internal managed decoders. WAV plus PCM/IMA/QOA now execute; Ogg/Vorbis and MP3 require their approved NVorbis/NLayer source slices. Public effects/instances, sample storage/registration, microphone/input ring, bus-layout resource import, output-device switching/latency, generator/composite/music/spatial streams, typed playback parameters and usage tagging retain precise coverage dependencies. Static WAV file load/save executes; generic ResourceLoader audio registration and fresh-process scene-file authoring do not.

Current native start/stop/pause/gain transitions are immediate; reference transition ramps remain Partial with explicit ramp-state/discontinuity tests as the trigger. Runtime Autoplay works; editor-hint suppression requires the editor-hint lifecycle. Stereo is verified. Surround/center matrices retain the source stereo-pair routing and independent unity player-gain LFE route; literal coefficient checks and actual native 2/4/6/8-channel PCM checks pass, while physical multichannel hardware is unverified. Mono device metadata is promoted to the stereo mix profile; unsupported odd layouts and empty quanta reject explicitly. Native audio defaults to Linux x64/ARM64 build profiles; x64 execution is checked. Cross-RID builds require an explicit toolchain or target library. Other audio platform backends/packages and physical listening remain unverified.

## Verification

- `dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release`: AudioResourceTests runs in the standard suite; all four storage formats, independent IMA nibble vector, import/save and retained file rejection, copied state, duplication, loop modes, corrupt input, disposal and PackedScene configuration.
- `ELECTRON2D_TEST_AUDIO=1 SDL_AUDIODRIVER=dummy dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release`: actual FAudio mixed PCM, source waveform, native mix quanta, pause, pitch ×2, concrete loop cursor, detach/reentry, post-volume routing/mute/gain/solo, live graph edits, polyphony and natural completion.
- `ELECTRON2D_TEST_AUDIO_SPEAKERS=1 SDL_AUDIO_CHANNELS=N SDL_AUDIODRIVER=dummy dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release`: N=1/4/6/8 checks actual 2/4/6/8-channel native stereo/center/surround PCM, including LFE gain, through the preserved backend hint; this verifies logical native mix profiles, not physical speakers.
- Prepared CPU cubic/loop mixing performs 64 repeated spans with zero managed allocation. Warmed native quantum callbacks allocate zero measured managed bytes and make zero calls to the custom FAudio allocator. Cold preparation, Play/graph edits, SDL/device allocations and broader workloads are outside that measurement.
- AudioLifetimeTests checks custom monophonic streams, callback failures, failing exit callbacks and cleanup, including teardown under recursively held mix locks. Public AudioPublicScenarioTests uses Engine.Run, Window, autoplay, position and driver queries, then machine-readable lifecycle results. Two sequential runs pass with Linux Wayland GPU and compatibility using the dummy audio driver, and with compatibility using the real PipeWire audio driver. Physical listening remains unverified. Internal PCM capture does not establish public audio export or human listening.
- Fresh self-contained linux-x64 HostExample publish passes native architecture/SONAME/shared SDL3 checks and the license inventory: 67 ELF files and 49 matching notices. Other target architectures/execution remain unverified.

The shared Node.EnsureMutable path now calls the virtual ValidateMutation hook after its existing guards. Detached registered audio nodes retain their native configuration owner; mutation and disposal reject on foreign threads before state changes. Worker-first CPU mixing leaves that owner unclaimed until an actual owner-bound operation.
