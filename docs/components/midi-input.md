# MIDI input

Last updated: 2026-10-29

The [ADR 0038](../decisions/input.md#adr-0038) input contract has a concrete desktop Linux producer using the system `libasound.so.2` ALSA Sequencer. It works independently of SDL, a window, renderer or audio output. The public values are [InputEventMIDI](../classes/InputEventMIDI.md) and [MIDIMessage](../classes/MIDIMessage.md); static [OS](../classes/OS.md) operations address a retained internal session owner.

## Session and delivery

`OS.OpenMIDIInputs()` opens one nonblocking input client, creates an input port, subscribes to source ports present at open and to ALSA lifetime announcements. Acquisition failure closes all partial ownership. Repeated open preserves an active session. `OS.GetConnectedMIDIInputs()` returns a defensive source-name array whose indices are the session's event `Device` values. Before open or after close it returns an empty array; a successfully opened session may have no source. Missing adapter/library, denied access and native acquisition errors throw distinct exceptions. A failed session remains observable until close or a fresh open.

Newly connected sources require close/open. A subscribed source's port/client removal or subscription removal invalidates the whole session, closes the client and throws `IOException`. The application should clear its held-note state and reopen explicitly. This policy preserves stable Device indices and prevents reused ALSA addresses from inheriting stale input. It does not silently renumber a still-running session.

Engine frame entry polls before physics/process callbacks. `Input.FlushBufferedEvents()` also polls on the active MainLoop owner thread. No active loop leaves native data pending. Each pump handles at most 4096 native records; remaining records stay in ALSA's bounded input pool. Reported native overflow terminates the session with an error rather than silently losing note releases. Reentrant pumping is rejected. Close/open from an input callback is supported: the pump stops reading the previous session. Native reading and public parsing use the same session lock, while no background native callback invokes user code.

Every translated event passes through ordinary `Input.ParseInputEvent` and SceneTree input/unhandled stages. The event is borrowed during synchronous callbacks and disposed afterwards; retain a duplicate when needed. MIDI does not create action-compatible bindings or raw keyboard/controller button state. User callback exceptions retain ordinary Input/SceneTree aggregation. Engine.Stop and Engine.Run cleanup close MIDI after finalization even when callbacks throw; failed Engine.Start also retires input acquired during initialization. Close is repeat-safe.

## Messages and resource values

The adapter projects MIDI 1 note on/off, polyphonic pressure, controller, program, channel pressure and pitch-bend events. Channels are 0–15, data bytes 0–127 and pitch bend is unsigned 0–16383, centered on 8192. A zero-velocity note-on retains its message status. ALSA supplies complete protocol events; the independent source fixture exercises fragmented byte writes, running status and realtime bytes interleaved with incomplete messages.

System quarter-frame, song position/select, tuning, clock, transport, sensing and reset retain their message identity and channel zero. Quarter-frame/song data has no extra public payload projection in the pinned event contract. Variable SysEx payloads and UMP records are not projected into this MIDI 1 event. MIDI output, synthesis and file playback are outside this slice. Invalid channel-message data closes the session with an explicit error.

All eight concrete properties have zero defaults and stored typed descriptors, emit resource changes and preserve manually authored integer values. Text is localized through the normal event template formatter. Duplicate and portable resource save/load preserve the exact type, enum and inherited Device. The event retains base press/release and action semantics.

## Verification and remaining platform work

`tests/Electron2D.Tests/MIDIInputTests.cs` is shared with the public-only `tests/MIDIInput.Consumer` executable. `Fixtures/MIDI/source.c` creates independent ALSA virtual sources through system headers, including an ABI assertion, malformed messages and a flood producer. Checks cover two sources, boundaries, system statuses, owner-thread delivery, close/reopen during a callback, disconnect, overflow, acquisition rollback and cleanup after input/finalization/startup failures. The public consumer passes 37 CLR checks on Linux with the same native producer, including delivery before both frame lanes and rejected reentrant pumping. Strict NativeAOT and local managed-package qualification exercise the same source-to-scene workflow; executable receipts remain separate from full release/native-package qualification. Separate CLR and NativeAOT children pass two assertions each for a missing ALSA configuration and an injected native permission denial. These are virtual-source and controlled-failure checks, not physical hardware acceptance.

Virtual-source execution does not establish physical controller acceptance. WinMM, CoreMIDI, Android MidiManager and browser permission-aware Web MIDI adapters are still missing; tvOS source acquisition remains the unresolved ADR platform gap. Those exact dependencies keep the three OS methods Partial. Event/resource/message declaration completion is independent of native platform acceptance under ADR 0021.

ALSA client/port/subscription and internal pool lifetime is released by the native client close. Its system-library heap is outside Electron2D's allocation ledger; no complete native allocation/MemoryMonitor claim follows from the handle-lifetime tests. See [native memory inventory](../coverage/native-memory-inventory.md).
