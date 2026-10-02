# AudioInputDevice

Last updated: 2026-10-02

**Source:** [AudioInputDevice.cs](../../src/Servers/Audio/AudioInputDevice.cs). **Declaration:** `internal sealed class AudioInputDevice : IDisposable`. This backend type is inaccessible to applications.

## Description

Owns a paused/resumable SDL3 recording stream, an independent SDL audio reference, one prepared native-quantum scratch array and four-quanta stereo history ring. InputHandle is the private SafeHandle: it retains the callback delegate until stream destruction joins callbacks and balances SDL audio initialization. Preparation validates a positive input frequency and a 1..262144-frame quantum before allocating bounded storage. Logical device names map to exact enumeration or Default. Device-side format belongs to SDL; application-side float stereo conversion uses the actual opened rate.

## Runtime operations and lifetime

Construction/enumeration/activation/disposal are owner preparation operations. Capture drains complete available stereo frames in scratch-sized chunks, clamps finite values and silences nonfinite input. Its native-boundary failure flag is checked by readers; no exception crosses the SDL callback. A stop/start resets queues, generation and failure. Read allocates a caller-owned whole-count array or returns empty without advancing. Available clamps indirectly through writer overflow correction. Mix reuses a caller span, independently primes each cursor, catches up overwritten readers and zero-fills shortage. It never takes the FAudio/configuration gate.

Dispose destroys the stream outside the ring gate, then marks the ring disposed. A concurrent old-device reader obtains silence after closure; successful server replacement publishes the new device before releasing this one. Monotonic sequence and generation prevent modulo full/empty ambiguity and stale microphone replay. SDL conversion/driver allocations remain outside the managed allocation measurement boundary.

## Verification

[AudioInputTests](../../tests/Electron2D.Tests/AudioInputTests.cs) drives real SDL device callbacks on dummy, bounded wrap/full/overflow, mono conversion on an unbound native stream using the same production callback, callback failure, switching, warm managed allocations and FAudio integration. [Audio component](../components/audio-playback.md#recording-input) records backend/platform limits. Applications use [AudioServer](AudioServer.md) and [AudioStreamMicrophone](AudioStreamMicrophone.md).
