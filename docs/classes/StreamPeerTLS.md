# StreamPeerTLS

Last updated: 2026-10-05

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.StreamPeerTLS`. **Inherits:** [StreamPeer](StreamPeer.md). **Source:** [StreamPeerTLS.cs](../../src/Core/Networking/StreamPeerTLS.cs).

## Description

Encrypts ordered bytes over a borrowed StreamPeer through a polled TLS session.

Calls require the constructing thread. The native backend uses host OpenSSL 3 on Linux and private packaged OpenSSL on Windows/macOS, with TLS 1.2/1.3, system/custom trust and expected-name validation. Poll processes handshake/control records and buffered output. Full I/O may wait; partial I/O retains bounded record buffers. Close notification disconnects after preceding plaintext drains; abrupt transport EOF is an authentication error. Disconnect/Dispose never disposes the borrowed stream. Independent SslStream TLS 1.2 and OpenSSL TLS 1.3 oracles cover both roles locally; complete Windows/macOS target execution remains pending.

The [TLS component](../components/tls.md) and [ADR 0094](../decisions/networking.md#adr-0094) specify trust, owner/lifetime, preparation, failure and platform boundaries.

## Example

Public API excerpt. Context identifiers belong to the caller; the component records full workflow and readiness requirements.

```csharp
using var options = TLSOptions.Client(trustedChain);
using var tls = new StreamPeerTLS();
tls.ConnectToStream(connectedStream, "service.example", options);
// Poll until Connected, then Poll plus GetAvailableBytes/partial I/O.
// TLS disposal preserves connectedStream ownership.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public StreamPeerTLS()` | Creates a disconnected TLS peer with prepared encrypted record buffers. |

## Constructor Descriptions

<a id="member-97bb35b9f3b6"></a>
### .ctor

`public StreamPeerTLS()`

Creates a disconnected TLS peer with prepared encrypted record buffers.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void AcceptStream(Electron2D.StreamPeer stream, Electron2D.TLSOptions options)` | Starts a server handshake over a borrowed stream. |
| `public System.Void ConnectToStream(Electron2D.StreamPeer stream, System.String commonName, Electron2D.TLSOptions options = null)` | Starts a validating or explicitly unsafe client handshake over a borrowed stream. |
| `public System.Void DisconnectFromStream()` | Sends a best-effort close notification and releases TLS state while preserving the borrowed stream. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `protected override System.Void Finalize()` | Releases abandoned native TLS state and resource-use retention without touching the borrowed transport. |
| `public override System.Int32 GetAvailableBytes()` | Reports immediately readable bytes without consuming them. |
| `public Electron2D.TLSStatus GetStatus()` | Gets the cached TLS phase without polling. |
| `public Electron2D.StreamPeer GetStream()` | Gets the currently borrowed transport. |
| `public System.Void Poll()` | Advances handshake, encrypted input/output and remote closure without waiting. |
| `protected override System.Int32 ReadCore(System.Span<System.Byte> destination, System.Boolean block)` | Reads a prefix for the concrete transport. |
| `protected override System.Int32 WriteCore(System.ReadOnlySpan<System.Byte> data, System.Boolean block)` | Writes a prefix for the concrete transport. |

## Method Descriptions

<a id="member-310a08b32963"></a>
### AcceptStream

`public System.Void AcceptStream(Electron2D.StreamPeer stream, Electron2D.TLSOptions options)`

Starts a server handshake over a borrowed stream.

stream: A live ordered transport.

options: Server certificate-chain and private-key options.

System.ArgumentException: Options do not describe a server.

<a id="member-fa7a66f5ea09"></a>
### ConnectToStream

`public System.Void ConnectToStream(Electron2D.StreamPeer stream, System.String commonName, Electron2D.TLSOptions options = null)`

Starts a validating or explicitly unsafe client handshake over a borrowed stream.

stream: A live ordered transport; self-attachment is invalid.

commonName: Expected DNS name or IP; options can override it.

options: Client options, or null for system trust and name validation.

System.Security.Authentication.AuthenticationException: TLS initialization or handshake validation fails.

System.PlatformNotSupportedException: The current TLS backend is unavailable.

<a id="member-30dc4d7a6e4f"></a>
### DisconnectFromStream

`public System.Void DisconnectFromStream()`

Sends a best-effort close notification and releases TLS state while preserving the borrowed stream.

<a id="member-9f6c5e85f163"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-54260efab982"></a>
### Finalize

`protected override System.Void Finalize()`

Releases abandoned native TLS state and resource-use retention without touching the borrowed transport.

<a id="member-7819489da747"></a>
### GetAvailableBytes

`public override System.Int32 GetAvailableBytes()`

Reports immediately readable bytes without consuming them.

Returns: A nonnegative count.

<a id="member-87db4f6ac0da"></a>
### GetStatus

`public Electron2D.TLSStatus GetStatus()`

Gets the cached TLS phase without polling.

Returns: The TLS phase.

<a id="member-b539cf749e55"></a>
### GetStream

`public Electron2D.StreamPeer GetStream()`

Gets the currently borrowed transport.

Returns: Null after disconnection or failure.

<a id="member-4675775005c6"></a>
### Poll

`public System.Void Poll()`

Advances handshake, encrypted input/output and remote closure without waiting.

System.Security.Authentication.AuthenticationException: TLS authentication or record processing fails.

<a id="member-0109eeb8a0c1"></a>
### ReadCore

`protected override System.Int32 ReadCore(System.Span<System.Byte> destination, System.Boolean block)`

Reads a prefix for the concrete transport.

destination: The destination.

block: Whether waiting is permitted.

Returns: The received byte count.

<a id="member-5b920637eecc"></a>
### WriteCore

`protected override System.Int32 WriteCore(System.ReadOnlySpan<System.Byte> data, System.Boolean block)`

Writes a prefix for the concrete transport.

data: The source.

block: Whether waiting is permitted.

Returns: The sent byte count.

## Verification and limits

[TLSTests](../../tests/Electron2D.Tests/TLSTests.cs) exercises the TLS/security resource contract, native Linux OpenSSL 3 streams, independent .NET SslStream interoperability and warmed managed-allocation boundaries. Other-platform TLS backend/packaging, system-wide trust variation, native OpenSSL allocation totals, routed throughput and owner acceptance remain separate. See [coverage](../coverage/classes/StreamPeerTLS.md).
