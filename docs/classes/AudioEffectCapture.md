# AudioEffectCapture

Last updated: 2026-10-02

**Declaration:** `public sealed class Electron2D.AudioEffectCapture` · **Source:** [AudioEffectCapture.cs](../../src/Scene/Resources/AudioEffectCapture.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffect](AudioEffect.md).

## Description

Copies stereo float bus PCM into one resource-owned ring without changing the audio. All instances share that ring, including multiple buses/pairs. First Instantiate prepares power-of-two storage strictly larger than the truncated output-rate × BufferLength, with one reserved slot. Later BufferLength changes do not resize it; later instantiations clear pending data but keep capacity and cumulative counters. A complete processing block fits or is wholly discarded, preserving earlier queued samples. FIFO reads, clearing and capture serialize through a data gate independent of native configuration.

The resource is borrowed by buses and caller-owned. Duplication copies BufferLength and inherited metadata; no queued samples, capacity, counters or live instances enter copies. No input device, network transmission, encoded recording or file export is implied. To capture microphone audio, configure AudioDriverEnableInput and route an ordinary AudioStreamMicrophone player through the bus.

## Example

Partial owner-thread host snippet; call before the player starts, consume in a Node process callback, remove the effect and dispose the resource after the host closes.

```csharp
using var capture = new AudioEffectCapture { BufferLength = .2f };
AudioServer.Instance.AddBusEffect(0, capture);
// After real native mixing:
int frames = capture.GetFramesAvailable();
Vector2[] pcm = capture.GetBuffer(frames);
AudioServer.Instance.RemoveBusEffect(0, 0);
```

AudioEffectTests.RunHost exercises this public workflow through a real Window and Engine.Run.

## API summary

| Signature | Contract |
| --- | --- |
| `public AudioEffectCapture()` | Uninitialized 0.1-second request. |
| `public float BufferLength { get; set; }` | Positive finite requested duration. |
| `public bool CanGetBuffer(int frames)` | Complete readable block availability. |
| `public Vector2[] GetBuffer(int frames)` | Copied FIFO block or empty without consumption. |
| `public void ClearBuffer()` | Drops queued data; keeps counters. |
| `public int GetFramesAvailable()` | Readable frame count. |
| `public int GetBufferLengthFrames()` | Total storage including reserved slot. |
| `public long GetPushedFrames()` | Successfully copied frames. |
| `public long GetDiscardedFrames()` | Rejected whole-block frames. |
| `protected override AudioEffectInstance OnInstantiate()` | Prepares ring and clears pending data. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Adds stored typed BufferLength. |
| `protected override Resource CreateDuplicateInstance()` | Creates empty copy target. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies configuration only. |
| `protected override void Dispose(bool disposing)` | Releases logical ring storage. |

## Property Descriptions

<a id="bufferlength"></a>
### BufferLength

Seconds; 0.1 initially, UI-oriented ordinary values are 0.01–10. Positive finite values are accepted; invalid values throw ArgumentOutOfRangeException before mutation. Changed runs after the value commits. Initial requests reaching 2^27 frames or nonfinite rate products reject before allocation; the maximum storage is 1 GiB. A positive duration truncating to zero frames prepares one reserved slot and therefore zero usable capacity. Read/write operations reject disposed resources.

## Method Descriptions

<a id="cangetbuffer"></a>
### CanGetBuffer

Accepts nonnegative counts; zero returns true even before initialization. Negative counts throw ArgumentOutOfRangeException. Queries never consume.

<a id="getbuffer"></a>
### GetBuffer

Returns exactly the requested stereo frames as a caller-owned Vector2 array when available. Zero, uninitialized or insufficient data returns empty without consumption. Negative counts throw ArgumentOutOfRangeException. Successful reads allocate explicitly on the caller thread. Values are raw finite bus PCM; bus gain may produce amplitudes beyond unity, so consumers choose their own output conversion/clipping.

<a id="clearbuffer"></a>
### ClearBuffer

Drops queued data during active or inactive capture without changing capacity or pushed/discarded totals. Concurrent capture is serialized; clearing can remove samples needed by a monitoring consumer.

<a id="getframesavailable"></a>
### GetFramesAvailable

Current readable count, between zero and storage size minus one; zero before instantiation.

<a id="getbufferlengthframes"></a>
### GetBufferLengthFrames

Total prepared power-of-two storage, including the reserved slot; zero before instantiation. Capacity remains fixed even when the output later closes/reopens at another rate.

<a id="getpushedframes"></a>
<a id="getdiscardedframes"></a>
### GetPushedFrames and GetDiscardedFrames

Nonnegative Int64 lifetime totals, saturated at Int64.MaxValue. Capture increments pushed by the whole accepted block or discarded by the whole rejected block. Clearing, reading and later Instantiate do not reset either counter.

<a id="oninstantiate"></a>
### OnInstantiate and resource hooks

Uses current AudioServer mix rate (44100 before output preparation), prepares storage once, clears pending samples and creates an internal [AudioEffectCaptureInstance](AudioEffectCaptureInstance.md). Buses prepare their actual native rate before calling the factory. Resource copy hooks retain configuration and metadata; disposing the source causes later borrowed native processing to fail through the owner error path.

## Verification, lifecycle and limits

AudioEffectTests checks defaults, zero/exact power boundaries, FIFO wrap, whole-block overflow, insufficient/negative/empty reads, aliasing, counters, copies, concurrency, custom processing, disabled/bypass/mute, ordering, routing and teardown. Capture processes inactive silence indefinitely. Linux dummy native output checks distinctive stereo PCM; public hosts run twice normally and once with an intentional callback failure on each current Wayland renderer. After warmup, 64 active and 64 paused native passes and 64 standalone capture/query cycles allocate zero measured managed bytes; native passes also make zero custom FAudio allocator calls. Copied GetBuffer results, preparation, user callbacks and SDL/OS allocations are outside that budget. Physical listening, actual multichannel devices and other platforms remain unverified.

See [ADR 0047](../decisions/audio.md#adr-0047) and [coverage](../coverage/classes/AudioEffectCapture.md).
