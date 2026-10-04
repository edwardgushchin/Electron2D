# UDPServer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.UDPServer`.

**Inherits:** [ElectronObject](ElectronObject.md). **Inherited by:** —. **Source:** [UDPServer.cs](../../src/Core/Networking/UDPServer.cs).

## Description

Routes UDP sender endpoints into independent caller-owned packet peers.

Poll receives packets and queues new endpoints up to MaxPendingConnections. Pending peers are server-owned; TakeConnection transfers logical ownership and the server keeps only a weak reference. Stop closes the shared socket and detaches accepted peers. Existing accepted endpoints continue receiving when the pending limit is zero. Receive queues retain packet boundaries and drop packets that exceed their budget.

The [networking component](../components/networking.md) records ownership, native/private boundaries, typed failures, allocation scopes and remaining protocol prerequisites. [ADR 0094](../decisions/networking.md#adr-0094) defines the accepted contract.

## Example

Public API excerpt; identifiers supplied by the caller are context, and connection/readiness polling remains required where shown. NetworkingTests exercises complete public workflows.

```csharp
using var server = new UDPServer { MaxPendingConnections = 16 };
server.Listen(0, "127.0.0.1");
server.Poll();
using var peer = server.TakeConnection();
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public UDPServer()` | Creates a stopped UDP listener. |

## Constructor Descriptions

<a id="member-24d448093715"></a>
### .ctor

`public UDPServer()`

Creates a stopped UDP listener.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Int32 MaxPendingConnections { get; set; }` | Gets or sets the maximum queued new endpoints. |

## Property Descriptions

<a id="member-c0c58c4164d4"></a>
### MaxPendingConnections

`public System.Int32 MaxPendingConnections { get; set; }`

Gets or sets the maximum queued new endpoints.

Value: 16 initially; zero rejects new endpoints while preserving accepted ones. Lowering trims newest pending peers.

System.ArgumentOutOfRangeException: The value is negative.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public System.Int32 GetLocalPort()` | Returns the native listener's local port or zero while stopped. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `public System.Boolean IsConnectionAvailable()` | Reports whether a pending endpoint can be taken; does not poll. |
| `public System.Boolean IsListening()` | Reports whether the native UDP listener is open. |
| `public System.Void Listen(System.Int32 port, System.String bindAddress = "*")` | Listens on a UDP port and IP bind address. |
| `public System.Void Poll()` | Receives all immediately available datagrams and routes them by remote endpoint. |
| `public System.Void Stop()` | Closes the shared socket, disposes pending peers and detaches accepted peers. |
| `public Electron2D.PacketPeerUDP TakeConnection()` | Transfers the oldest pending endpoint, including its first queued packet. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-163e453a4831"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-1f9e95b2b407"></a>
### GetLocalPort

`public System.Int32 GetLocalPort()`

Returns the native listener's local port or zero while stopped.

Returns: The port.

<a id="member-3bf186af4730"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Overrides append or replace descriptors; they must not yield null entries.

<a id="member-008ceec0a1bd"></a>
### IsConnectionAvailable

`public System.Boolean IsConnectionAvailable()`

Reports whether a pending endpoint can be taken; does not poll.

Returns: True when the pending queue is nonempty.

<a id="member-4b9528f06c21"></a>
### IsListening

`public System.Boolean IsListening()`

Reports whether the native UDP listener is open.

Returns: True while listening.

<a id="member-181e8d9b3b19"></a>
### Listen

`public System.Void Listen(System.Int32 port, System.String bindAddress = "*")`

Listens on a UDP port and IP bind address.

port: Zero through 65535; zero selects an ephemeral port.

bindAddress: IP literal or wildcard.

<a id="member-cac2fa60f5eb"></a>
### Poll

`public System.Void Poll()`

Receives all immediately available datagrams and routes them by remote endpoint.

System.InvalidOperationException: The listener is stopped.

<a id="member-4f4c687637d0"></a>
### Stop

`public System.Void Stop()`

Closes the shared socket, disposes pending peers and detaches accepted peers.

<a id="member-87f3f9290bf7"></a>
### TakeConnection

`public Electron2D.PacketPeerUDP TakeConnection()`

Transfers the oldest pending endpoint, including its first queued packet.

Returns: A caller-owned peer, or null.

<a id="member-98ae33bd219c"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Verification and limits

[NetworkingTests](../../tests/Electron2D.Tests/NetworkingTests.cs) verifies the exercised native Linux x64 IPv4/IPv6 loopback and UDS paths, binary/framing/queue edges, lifecycle, owner checks, scene request/reply and warmed allocation boundaries. Native allocator totals, other hosts/browser transport, real routed traffic, network throughput and owner acceptance remain unverified. Dynamic-value wire encoding is excluded under ADR 0001; raw typed bytes are executable. See [coverage](../coverage/classes/UDPServer.md).
