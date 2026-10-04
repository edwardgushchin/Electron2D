# StreamPeerGZIP

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.StreamPeerGZIP`. **Inherits:** [StreamPeer](StreamPeer.md). **Source:** [StreamPeerGZIP.cs](../../src/Core/Networking/StreamPeerGZIP.cs).

## Description

Incrementally compresses or decompresses gzip and zlib-wrapped deflate bytes through bounded output storage.

Input and output spans are borrowed only during a call. Drain output when partial writes stop progressing. Native codec state and output capacity are prepared on Start. Calls and disposal require the constructing thread.

[HTTP and compression](../components/http.md) and [ADR 0094](../decisions/networking.md#adr-0094) define wire, ownership, worker, allocation and platform boundaries.

## Example

Public API excerpt. Caller context and indicated polling/tree prerequisites are required; HTTPTests exercises full workflows.

```csharp
using var stream = new StreamPeerGZIP();
stream.StartCompression();
stream.PutData("hello"u8);
stream.Finish();
byte[] gzip = stream.GetData(stream.GetAvailableBytes());
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public StreamPeerGZIP()` | Creates an unconfigured compression stream. |

## Constructor Descriptions

<a id="member-858e0c621352"></a>
### .ctor

`public StreamPeerGZIP()`

Creates an unconfigured compression stream.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void Clear()` | Releases codec state and discards queued output. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public System.Void Finish()` | Finishes a compression stream, retaining output for reading. |
| `public override System.Int32 GetAvailableBytes()` | Reports immediately readable bytes without consuming them. |
| `public System.Boolean IsFinished()` | Reports whether the compression finish or decompression member boundary was reached. |
| `protected override System.Int32 ReadCore(System.Span<System.Byte> destination, System.Boolean block)` | Reads a prefix for the concrete transport. |
| `public System.Void StartCompression(System.Boolean useDeflate = false, System.Int32 bufferSize = 65535)` | Starts compression with a fresh bounded output buffer. |
| `public System.Void StartDecompression(System.Boolean useDeflate = false, System.Int32 bufferSize = 65535)` | Starts decompression with a fresh bounded output buffer. |
| `protected override System.Int32 WriteCore(System.ReadOnlySpan<System.Byte> data, System.Boolean block)` | Writes a prefix for the concrete transport. |

## Method Descriptions

<a id="member-168a025e7e0a"></a>
### Clear

`public System.Void Clear()`

Releases codec state and discards queued output.

<a id="member-0989f3640a5c"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-a1137afe3ae7"></a>
### Finish

`public System.Void Finish()`

Finishes a compression stream, retaining output for reading.

System.InvalidOperationException: The stream is not compressing, or output must be drained before retrying Finish.

<a id="member-2e40753fd9b1"></a>
### GetAvailableBytes

`public override System.Int32 GetAvailableBytes()`

Reports immediately readable bytes without consuming them.

Returns: A nonnegative count.

<a id="member-4d611d8266b2"></a>
### IsFinished

`public System.Boolean IsFinished()`

Reports whether the compression finish or decompression member boundary was reached.

Returns: False before configuration or while a member remains incomplete.

<a id="member-07266a6e434a"></a>
### ReadCore

`protected override System.Int32 ReadCore(System.Span<System.Byte> destination, System.Boolean block)`

Reads a prefix for the concrete transport.

destination: The destination.

block: Whether waiting is permitted.

Returns: The received byte count.

<a id="member-a82a9f70c9e7"></a>
### StartCompression

`public System.Void StartCompression(System.Boolean useDeflate = false, System.Int32 bufferSize = 65535)`

Starts compression with a fresh bounded output buffer.

useDeflate: True selects zlib-wrapped deflate; false selects gzip.

bufferSize: Positive prepared capacity up to 64 MiB, rounded to a power of two with one reserved byte.

<a id="member-72103282f8cc"></a>
### StartDecompression

`public System.Void StartDecompression(System.Boolean useDeflate = false, System.Int32 bufferSize = 65535)`

Starts decompression with a fresh bounded output buffer.

useDeflate: True selects zlib-wrapped deflate; false selects gzip, including concatenated members.

bufferSize: Positive prepared capacity up to 64 MiB.

<a id="member-f4dc5f056fd4"></a>
### WriteCore

`protected override System.Int32 WriteCore(System.ReadOnlySpan<System.Byte> data, System.Boolean block)`

Writes a prefix for the concrete transport.

data: The source.

block: Whether waiting is permitted.

Returns: The sent byte count.

## Verification and limits

[HTTPTests](../../tests/Electron2D.Tests/HTTPTests.cs) verifies the exercised native Linux IPv4/DNS/TLS/proxy and public scene/worker paths, fragmented binary/framing/codec/security edges and prepared managed-allocation intervals. Native codec internals, real routed throughput, other-platform/browser delivery and owner acceptance remain separate gates. See [coverage](../coverage/classes/StreamPeerGZIP.md).
