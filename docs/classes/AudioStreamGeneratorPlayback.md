# AudioStreamGeneratorPlayback

Last updated: 2026-10-02

**Namespace:** `Electron2D` · **Declaration:** `public sealed class AudioStreamGeneratorPlayback : AudioStreamPlaybackResampled` · **Source:** [AudioStreamGeneratorPlayback.cs](../../src/Scene/Resources/AudioStreamGeneratorPlayback.cs).

**Inherits:** [AudioStreamPlaybackResampled](AudioStreamPlaybackResampled.md) → [AudioStreamPlayback](AudioStreamPlayback.md).

## Description

Independent bounded source queue, generated-time/underrun counters and prepared cubic interpolation history, created only by [AudioStreamGenerator](AudioStreamGenerator.md). The resource remains borrowed. A standalone playback is caller-owned; a handle obtained from AudioStreamPlayer is player-owned and becomes disposed when replaced or released. X/Y samples represent left/right channels, preserve finite values outside the unit range and are copied into private storage. Reuse prepared spans to avoid producer allocations.

Producer, control and mixing operations serialize through one per-playback gate also used by inherited BeginResample. Writes never grow storage. Bulk validation/overflow rejection leaves the queue unchanged. Concurrent producers are supported through serialization; no multi-call reservation is promised by CanPushBuffer. Each independent playback owns its queue. Capacity is captured at creation and never follows later BufferLength changes.

Start ignores finite fromPosition, resets skips and generated time, and initializes silent history when the previous generated-time counter is zero. This retains queued producer frames. At unit source/output rate, the initial 128 silent source frames plus two cubic-history frames precede the first queued frame. Stop retains queued/history data. Restart after nonzero generated time retains history while resetting counters. ClearBuffer is stopped-only, resets queue/time, and causes fresh silent history on next Start. Seek is a validated no-op; loop count is always zero.

Source-block underrun fills the missing source samples with zeros, increments skips once for that block and reports a complete output demand. It never naturally ends playback. Generated position advances by each requested source block divided by the effective source rate, including silence/prefetch; it is not a device-latency-corrected audible cursor. Stop or external player lifecycle ends the stream. Source disposal makes producer/query/mix operations reject; Stop remains usable for cleanup. Playback disposal releases its queue, retaining the borrowed source.

## Example

```csharp
using var generator = new AudioStreamGenerator();
using var playback = (AudioStreamGeneratorPlayback)generator.InstantiatePlayback();
Vector2[] frames = new Vector2[256];
frames.AsSpan().Fill(new Vector2(.1f, -.1f));
playback.PushBuffer(frames);
playback.Start();
Vector2[] mixed = playback.MixAudio(1, 512); // Caller-owned cold query result.
playback.Stop();
playback.ClearBuffer();
```

Use the existing AudioStreamPlayer/Engine.Run scene path for native output, as exercised by [AudioGeneratorTests.RunHost](../../tests/Electron2D.Tests/AudioGeneratorTests.cs). MixAudio itself allocates a caller-owned result; engine mixing uses reusable spans.

## API summary

No public constructor exists.

| Signature | Contract |
| --- | --- |
| `public int GetFramesAvailable()` | Current free producer slots. |
| `public bool CanPushBuffer(int amount)` | Nonnegative whole-write capacity check. |
| `public bool PushFrame(Vector2 frame)` | Copies one finite frame or reports overflow. |
| `public bool PushBuffer(ReadOnlySpan<Vector2> frames)` | Atomic finite whole-span copy. |
| `public int GetSkips()` | Underrun source blocks since Start. |
| `public void ClearBuffer()` | Stopped-only queue/time reset. |

## Method descriptions

### GetFramesAvailable

Returns unused source queue slots, zero when full. The power-of-two backing array reserves one slot. Prefetched history is separate and may have already freed source slots before those frames become audible. Throws ObjectDisposedException when playback/source is disposed.

### CanPushBuffer

Returns whether `amount` currently fits, including true for zero. Negative counts throw ArgumentOutOfRangeException. A second producer may change free space before PushBuffer; the actual write validates again atomically. Disposal follows GetFramesAvailable.

### PushFrame

Copies finite left/right values into the next source slot, then advances the write cursor. Returns false if full, preserving state. NaN/infinity throws ArgumentException before commit, including on a full queue. Values are not clamped. May be called before Start, while playing or while stopped.

### PushBuffer

Validates every sample before changing storage, then copies both contiguous segments across wrap and publishes the cursor once. Returns false if the complete span does not fit, with no prefix write. Empty spans return true; no external array is retained. Nonfinite input throws ArgumentException without changing the queue. Successful warmed copies allocate no managed storage.

### GetSkips

Returns a nonnegative underrun source-block counter, saturated at Int32.MaxValue. One incomplete source read adds one skip irrespective of the missing sample count. Start resets it; Stop, Seek and ClearBuffer do not. It does not count owner frame lateness or hardware underruns.

### ClearBuffer

Requires stopped playback or throws InvalidOperationException before mutation. Resets read/write cursors and generated time without changing capacity or skips. Next Start rebuilds silent history, so old prefetched data is not replayed after a clear. Dispose/source lifetime validation precedes edits.

## Protected overrides

| Signature | Contract |
| --- | --- |
| `protected override void OnStart(double fromPosition)` | Validated finite position ignored; counters/history policy above. |
| `protected override void OnStop()` | Stops mixing while preserving queue/history. |
| `protected override bool OnIsPlaying()` | Serialized active state. |
| `protected override double OnGetPlaybackPosition()` | Generated source-block duration. |
| `protected override void OnSeek(double time)` | Validated finite no-op. |
| `protected override float OnGetStreamSamplingRate()` | Effective rate prepared for source mixing. |
| `protected override int OnMix(Span<Vector2> buffer, float rateScale)` | Serialized active cubic mixing or stopped output clear. |
| `protected override int OnMixResampled(Span<Vector2> buffer)` | Bounded FIFO read, silent remainder and underrun accounting. |
| `protected override void Dispose(bool disposing)` | Queue release and inherited logical cleanup. |

### OnStart

Checks borrowed-source lifetime and captures current effective sampling rate. When generated time is zero, initializes inactive silence into the shared resampler as a complete block, retaining queued frames and avoiding a false end marker. Marks active and resets skips/time. Parent Start rejects nonfinite positions; finite positive/negative positions are ignored.

### OnStop

Marks inactive under the shared gate. Does not discard source/history/time/counters, and remains available after borrowed-source disposal for teardown.

### OnIsPlaying

Reads active state with playback/source lifetime validation. Persistent source underrun does not change this state.

### OnGetPlaybackPosition

Reads accumulated requested source-block seconds. It includes silence and prefetch, and remains unchanged while stopped. Start and ClearBuffer reset it.

### OnSeek

Checks source lifetime but changes no queue/history/counters. Parent Seek handles finite validation.

### OnGetStreamSamplingRate

Supplies effective rate prepared by active mix/source preparation. The generator's Custom/Output selection remains live. Input cannot be selected before its actual capture-frequency integration.

### OnMix

Checks both lifetimes under the gate. Stopped calls clear the entire output and report zero; active calls update effective metadata and invoke the existing cubic resampler with local/global pitch. The output count remains complete during underrun. Invalid rate/cursor behavior follows the parent contract.

### OnMixResampled

Copies available source frames across wrap, zero-fills the remainder, increments one saturated skip on a short read and advances generated time by full requested duration. Inactive initial history preparation fills silence without consuming the queue or changing counters. Reports a complete span to preserve the indefinite producer stream.

### Dispose

Marks inactive and drops owned queue storage while holding the shared gate, then delegates logical lifetime cleanup. The source is borrowed and remains live. Further playback operations reject disposal.

## Verification and limits

[AudioGeneratorTests](../../tests/Electron2D.Tests/AudioGeneratorTests.cs) verifies FIFO segments/copy isolation/overflow rollback, active clear rejection, finite/lifetime boundaries, independent unit/pitch/rate cubic PCM oracles, persistent silence/recovery, counter/control behavior and concurrent producer/mix/public history reset. Twenty CPU warmup cycles precede 64 active push/mix/query cycles and 64 stopped mixes, both with zero managed bytes. Native dummy FAudio checks actual distinct stereo generated PCM, persistence after underrun, replacement lifetime and 64 active/64 paused passes with zero measured managed bytes/custom allocator calls after explicit warmup. Public host scenarios run twice per Linux Wayland renderer.

[Audio component](../components/audio-playback.md#procedural-generator) and [ADR 0047](../decisions/audio.md#adr-0047) record the integration and initial-history adaptation. Input capture, inherited sample/usage tagging, physical listening, unmeasured native/driver allocations, broader workloads/platforms and scene-file authoring keep their separate exact coverage limits.
