# StreamPeerSocket

Last updated: 2026-10-05

**Namespace:** `Electron2D`. **Declaration:** `public abstract class Electron2D.StreamPeerSocket`.

**Inherits:** [StreamPeer](StreamPeer.md). **Inherited by:** [StreamPeerTCP](StreamPeerTCP.md), [StreamPeerUDS](StreamPeerUDS.md). **Source:** [StreamPeerSocket.cs](../../src/Core/Networking/StreamPeerSocket.cs).

## Description

Owns a nonblocking native stream socket with explicit connection polling.

Partial operations do not wait. Full reads/writes can block until the peer supplies progress or closes. Poll detects connection completion, nonzero socket errors and FIN after queued bytes drain. Read readiness with no available bytes uses a non-consuming peek: zero-byte success is FIN, a reset is a socket error, and WouldBlock retains the connection. Queued data is preserved. Status queries are cached. Calls and disposal require the constructing thread.

The [networking component](../components/networking.md) records ownership, native/private boundaries, typed failures, allocation scopes and remaining protocol prerequisites. [ADR 0094](../decisions/networking.md#adr-0094) defines the accepted contract.

## Example

Public API excerpt; identifiers supplied by the caller are context, and connection/readiness polling remains required where shown. NetworkingTests exercises complete public workflows.

```csharp
StreamPeerSocket connection = connectedPeer;
connection.Poll();
StreamSocketStatus phase = connection.GetStatus();
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `protected StreamPeerSocket()` | Creates a disconnected stream socket. |

## Constructor Descriptions

<a id="member-7c3b24239969"></a>
### .ctor

`protected StreamPeerSocket()`

Creates a disconnected stream socket.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void DisconnectFromHost()` | Closes native connection and binding state and resets the phase. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public override System.Int32 GetAvailableBytes()` | Reports immediately readable bytes without consuming them. |
| `public Electron2D.StreamSocketStatus GetStatus()` | Returns the cached connection phase without polling. |
| `public System.Void Poll()` | Advances connection completion and distinguishes socket errors from drained graceful closure without consuming queued bytes. |
| `protected override System.Int32 ReadCore(System.Span<System.Byte> destination, System.Boolean block)` | Reads a prefix for the concrete transport. |
| `protected override System.Int32 WriteCore(System.ReadOnlySpan<System.Byte> data, System.Boolean block)` | Writes a prefix for the concrete transport. |

## Method Descriptions

<a id="member-891b842f74a7"></a>
### DisconnectFromHost

`public System.Void DisconnectFromHost()`

Closes native connection and binding state and resets the phase.

<a id="member-b67cd2657310"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-36d0bdaf6cb2"></a>
### GetAvailableBytes

`public override System.Int32 GetAvailableBytes()`

Reports immediately readable bytes without consuming them.

Returns: A nonnegative count.

<a id="member-dad5ce634aea"></a>
### GetStatus

`public Electron2D.StreamSocketStatus GetStatus()`

Returns the cached connection phase without polling.

Returns: The latest explicit phase.

<a id="member-2b9391cbcffd"></a>
### Poll

`public System.Void Poll()`

Advances connection completion and distinguishes socket errors from drained graceful closure without consuming queued bytes.

System.Net.Sockets.SocketException: The connection fails; state becomes Error and native resources close.

System.TimeoutException: The configured connection deadline expires.

<a id="member-5cc9baf4e3bf"></a>
### ReadCore

`protected override System.Int32 ReadCore(System.Span<System.Byte> destination, System.Boolean block)`

Reads a prefix for the concrete transport.

destination: The destination.

block: Whether waiting is permitted.

Returns: The received byte count.

<a id="member-a569f25c4daa"></a>
### WriteCore

`protected override System.Int32 WriteCore(System.ReadOnlySpan<System.Byte> data, System.Boolean block)`

Writes a prefix for the concrete transport.

data: The source.

block: Whether waiting is permitted.

Returns: The sent byte count.

## Verification and limits

[NetworkingTests](../../tests/Electron2D.Tests/NetworkingTests.cs) verifies the exercised native Linux x64 IPv4/IPv6 loopback and UDS paths, binary/framing/queue edges, lifecycle, owner checks, scene request/reply and warmed allocation boundaries. Native allocator totals, other hosts/browser transport, real routed traffic, network throughput and owner acceptance remain unverified. Dynamic-value wire encoding is excluded under ADR 0001; raw typed bytes are executable. See [coverage](../coverage/classes/StreamPeerSocket.md).
