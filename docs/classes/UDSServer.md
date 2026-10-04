# UDSServer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.UDSServer`.

**Inherits:** [SocketServer](SocketServer.md). **Inherited by:** —. **Source:** [SocketServer.cs](../../src/Core/Networking/SocketServer.cs).

## Description

Accepts native Unix-domain stream connections.

Endpoint support and path limits follow the native runtime. Existing namespace entries are not replaced.

The [networking component](../components/networking.md) records ownership, native/private boundaries, typed failures, allocation scopes and remaining protocol prerequisites. [ADR 0094](../decisions/networking.md#adr-0094) defines the accepted contract.

## Example

Public API excerpt; identifiers supplied by the caller are context, and connection/readiness polling remains required where shown. NetworkingTests exercises complete public workflows.

```csharp
using var server = new UDSServer();
server.Listen(endpointPath);
// Own the result of TakeConnection; Stop preserves accepted peers.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public UDSServer()` | Creates a stopped Unix-domain listener. |

## Constructor Descriptions

<a id="member-ce0bc51bf6bc"></a>
### .ctor

`public UDSServer()`

Creates a stopped Unix-domain listener.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void Listen(System.String path)` | Listens on a nonempty local Unix-domain endpoint. |
| `public Electron2D.StreamPeerUDS TakeConnection()` | Takes a queued Unix-domain connection. |

## Method Descriptions

<a id="member-b455a310ccac"></a>
### Listen

`public System.Void Listen(System.String path)`

Listens on a nonempty local Unix-domain endpoint.

path: The native endpoint path.

System.PlatformNotSupportedException: Unix-domain sockets are unavailable.

<a id="member-a17149aef095"></a>
### TakeConnection

`public Electron2D.StreamPeerUDS TakeConnection()`

Takes a queued Unix-domain connection.

Returns: A caller-owned peer, or null.

## Verification and limits

[NetworkingTests](../../tests/Electron2D.Tests/NetworkingTests.cs) verifies the exercised native Linux x64 IPv4/IPv6 loopback and UDS paths, binary/framing/queue edges, lifecycle, owner checks, scene request/reply and warmed allocation boundaries. Native allocator totals, other hosts/browser transport, real routed traffic, network throughput and owner acceptance remain unverified. Dynamic-value wire encoding is excluded under ADR 0001; raw typed bytes are executable. See [coverage](../coverage/classes/UDSServer.md).
