# SocketServer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public abstract class Electron2D.SocketServer`.

**Inherits:** [ElectronObject](ElectronObject.md). **Inherited by:** [TCPServer](TCPServer.md), [UDSServer](UDSServer.md). **Source:** [SocketServer.cs](../../src/Core/Networking/SocketServer.cs).

## Description

Owns a nonblocking native stream listener and transfers accepted peers to callers.

Server calls require the constructing thread. Stopping the listener leaves accepted stream peers alive. TakeSocketConnection returns null when no connection is queued. Socket handles remain internal.

The [networking component](../components/networking.md) records ownership, native/private boundaries, typed failures, allocation scopes and remaining protocol prerequisites. [ADR 0094](../decisions/networking.md#adr-0094) defines the accepted contract.

## Example

Public API excerpt; identifiers supplied by the caller are context, and connection/readiness polling remains required where shown. NetworkingTests exercises complete public workflows.

```csharp
SocketServer listener = tcpServer;
if (listener.IsConnectionAvailable())
{
    using StreamPeerSocket? peer = listener.TakeSocketConnection();
}
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `protected SocketServer()` | Creates a stopped listener. |

## Constructor Descriptions

<a id="member-849376a5a38c"></a>
### .ctor

`protected SocketServer()`

Creates a stopped listener.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected System.Void CheckServer()` | Checks listener lifetime and thread ownership. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public System.Boolean IsConnectionAvailable()` | Reports whether an accepted connection can be taken immediately. |
| `public System.Boolean IsListening()` | Reports whether a listener is open. |
| `public System.Void Stop()` | Stops accepting new connections while leaving caller-owned accepted peers alive. |
| `public Electron2D.StreamPeerSocket TakeSocketConnection()` | Takes one pending connection through the shared stream-socket interface. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-935a4ed7c8d3"></a>
### CheckServer

`protected System.Void CheckServer()`

Checks listener lifetime and thread ownership.

<a id="member-9acd1a148074"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-a12f3117591c"></a>
### IsConnectionAvailable

`public System.Boolean IsConnectionAvailable()`

Reports whether an accepted connection can be taken immediately.

Returns: True when the listening socket is readable.

<a id="member-ee98cdb02e02"></a>
### IsListening

`public System.Boolean IsListening()`

Reports whether a listener is open.

Returns: True after a successful Listen until Stop.

<a id="member-639dcafa1b9d"></a>
### Stop

`public System.Void Stop()`

Stops accepting new connections while leaving caller-owned accepted peers alive.

<a id="member-06dfe9b50af9"></a>
### TakeSocketConnection

`public Electron2D.StreamPeerSocket TakeSocketConnection()`

Takes one pending connection through the shared stream-socket interface.

Returns: A caller-owned peer, or null when none is available.

<a id="member-dd21bdd8c18b"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Verification and limits

[NetworkingTests](../../tests/Electron2D.Tests/NetworkingTests.cs) verifies the exercised native Linux x64 IPv4/IPv6 loopback and UDS paths, binary/framing/queue edges, lifecycle, owner checks, scene request/reply and warmed allocation boundaries. Native allocator totals, other hosts/browser transport, real routed traffic, network throughput and owner acceptance remain unverified. Dynamic-value wire encoding is excluded under ADR 0001; raw typed bytes are executable. See [coverage](../coverage/classes/SocketServer.md).
