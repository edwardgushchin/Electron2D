# TCPServer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.TCPServer`.

**Inherits:** [SocketServer](SocketServer.md). **Inherited by:** —. **Source:** [SocketServer.cs](../../src/Core/Networking/SocketServer.cs).

## Description

Accepts native TCP connections on an IP literal or wildcard.



The [networking component](../components/networking.md) records ownership, native/private boundaries, typed failures, allocation scopes and remaining protocol prerequisites. [ADR 0094](../decisions/networking.md#adr-0094) defines the accepted contract.

## Example

Public API excerpt; identifiers supplied by the caller are context, and connection/readiness polling remains required where shown. NetworkingTests exercises complete public workflows.

```csharp
using var server = new TCPServer();
server.Listen(0, "127.0.0.1");
int localPort = server.GetLocalPort();
// Poll IsConnectionAvailable, then own the returned connection.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public TCPServer()` | Creates a stopped TCP listener. |

## Constructor Descriptions

<a id="member-80d2701cf366"></a>
### .ctor

`public TCPServer()`

Creates a stopped TCP listener.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Int32 GetLocalPort()` | Returns the listening local port or zero after stopping. |
| `public System.Void Listen(System.Int32 port, System.String bindAddress = "*")` | Listens on a local port and bind address. |
| `public Electron2D.StreamPeerTCP TakeConnection()` | Takes a queued TCP connection. |

## Method Descriptions

<a id="member-bd5db453c24b"></a>
### GetLocalPort

`public System.Int32 GetLocalPort()`

Returns the listening local port or zero after stopping.

Returns: The port.

<a id="member-5026f76d8e90"></a>
### Listen

`public System.Void Listen(System.Int32 port, System.String bindAddress = "*")`

Listens on a local port and bind address.

port: Zero through 65535; zero selects an ephemeral port.

bindAddress: IP literal or wildcard.

System.Net.Sockets.SocketException: Binding or listening fails.

<a id="member-06bccb6de3e0"></a>
### TakeConnection

`public Electron2D.StreamPeerTCP TakeConnection()`

Takes a queued TCP connection.

Returns: A caller-owned peer, or null.

## Verification and limits

[NetworkingTests](../../tests/Electron2D.Tests/NetworkingTests.cs) verifies the exercised native Linux x64 IPv4/IPv6 loopback and UDS paths, binary/framing/queue edges, lifecycle, owner checks, scene request/reply and warmed allocation boundaries. Native allocator totals, other hosts/browser transport, real routed traffic, network throughput and owner acceptance remain unverified. Dynamic-value wire encoding is excluded under ADR 0001; raw typed bytes are executable. See [coverage](../coverage/classes/TCPServer.md).
