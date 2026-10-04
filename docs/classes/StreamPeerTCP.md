# StreamPeerTCP

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.StreamPeerTCP`.

**Inherits:** [StreamPeerSocket](StreamPeerSocket.md). **Inherited by:** —. **Source:** [StreamPeerTCP.cs](../../src/Core/Networking/StreamPeerTCP.cs).

## Description

Transfers ordered binary stream data through a native TCP connection.

Bind accepts an IP literal or wildcard. ConnectToHost resolves a name before starting a nonblocking connect. Poll is required while connecting and for FIN/error detection. Timeout is sampled from project settings.

The [networking component](../components/networking.md) records ownership, native/private boundaries, typed failures, allocation scopes and remaining protocol prerequisites. [ADR 0094](../decisions/networking.md#adr-0094) defines the accepted contract.

## Example

Public API excerpt; identifiers supplied by the caller are context, and connection/readiness polling remains required where shown. NetworkingTests exercises complete public workflows.

```csharp
using var client = new StreamPeerTCP();
client.ConnectToHost("127.0.0.1", localPort);
// Call Poll until GetStatus is Connected before transferring bytes.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public StreamPeerTCP()` | Creates an unbound disconnected TCP peer. |

## Constructor Descriptions

<a id="member-7ddf48232c6e"></a>
### .ctor

`public StreamPeerTCP()`

Creates an unbound disconnected TCP peer.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void Bind(System.Int32 port, System.String host = "*")` | Binds a local address and port before connecting. |
| `public System.Void ConnectToHost(System.String host, System.Int32 port)` | Starts a connection, reusing an explicitly bound socket. |
| `public System.String GetConnectedHost()` | Returns the selected remote IP, or empty after disconnection. |
| `public System.Int32 GetConnectedPort()` | Returns the selected remote port, or zero after disconnection. |
| `public System.Int32 GetLocalPort()` | Returns the local bound port or zero before opening. |
| `public System.Void SetNoDelay(System.Boolean enabled)` | Enables or disables TCP_NODELAY on an open socket. |

## Method Descriptions

<a id="member-d0fe721d4de4"></a>
### Bind

`public System.Void Bind(System.Int32 port, System.String host = "*")`

Binds a local address and port before connecting.

port: Zero through 65535; zero requests an ephemeral port.

host: IP literal or wildcard.

<a id="member-73741a9f09b8"></a>
### ConnectToHost

`public System.Void ConnectToHost(System.String host, System.Int32 port)`

Starts a connection, reusing an explicitly bound socket.

host: IP literal or resolvable host name.

port: Remote port, one through 65535.

<a id="member-3224a034dc94"></a>
### GetConnectedHost

`public System.String GetConnectedHost()`

Returns the selected remote IP, or empty after disconnection.

Returns: A normalized IP literal.

<a id="member-46bff1275e31"></a>
### GetConnectedPort

`public System.Int32 GetConnectedPort()`

Returns the selected remote port, or zero after disconnection.

Returns: The port.

<a id="member-f83fd5562485"></a>
### GetLocalPort

`public System.Int32 GetLocalPort()`

Returns the local bound port or zero before opening.

Returns: The port.

<a id="member-18409972c2a1"></a>
### SetNoDelay

`public System.Void SetNoDelay(System.Boolean enabled)`

Enables or disables TCP_NODELAY on an open socket.

enabled: Whether to bypass Nagle aggregation.

## Verification and limits

[NetworkingTests](../../tests/Electron2D.Tests/NetworkingTests.cs) verifies the exercised native Linux x64 IPv4/IPv6 loopback and UDS paths, binary/framing/queue edges, lifecycle, owner checks, scene request/reply and warmed allocation boundaries. Native allocator totals, other hosts/browser transport, real routed traffic, network throughput and owner acceptance remain unverified. Dynamic-value wire encoding is excluded under ADR 0001; raw typed bytes are executable. See [coverage](../coverage/classes/StreamPeerTCP.md).

HTTP address-fallback verification fixes local-port capture during implicit binding: the native endpoint is queried before starting nonblocking connect, so later Poll retains the pending connection failure for candidate fallback. HTTPTests exercises localhost with an IPv4 listener; existing native TCP/IPv6 tests remain applicable.
