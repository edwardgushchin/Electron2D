# StreamPeerUDS

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.StreamPeerUDS`.

**Inherits:** [StreamPeerSocket](StreamPeerSocket.md). **Inherited by:** —. **Source:** [StreamPeerUDS.cs](../../src/Core/Networking/StreamPeerUDS.cs).

## Description

Transfers ordered binary stream data through a native Unix-domain socket.

Requires runtime Unix-domain socket support. Paths follow the platform endpoint limits. The caller owns filesystem namespace permissions; the socket backend owns bound endpoint cleanup.

The [networking component](../components/networking.md) records ownership, native/private boundaries, typed failures, allocation scopes and remaining protocol prerequisites. [ADR 0094](../decisions/networking.md#adr-0094) defines the accepted contract.

## Example

Public API excerpt; identifiers supplied by the caller are context, and connection/readiness polling remains required where shown. NetworkingTests exercises complete public workflows.

```csharp
using var client = new StreamPeerUDS();
client.ConnectToHost(endpointPath);
// Call Poll before transferring bytes; the runtime must support UDS.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public StreamPeerUDS()` | Creates a disconnected Unix-domain peer. |

## Constructor Descriptions

<a id="member-73330a440f68"></a>
### .ctor

`public StreamPeerUDS()`

Creates a disconnected Unix-domain peer.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void Bind(System.String path)` | Binds a local Unix-domain endpoint before connecting. |
| `public System.Void ConnectToHost(System.String path)` | Starts a nonblocking connection to a Unix-domain endpoint. |
| `public System.String GetConnectedPath()` | Returns the selected remote path, or empty after disconnection. |

## Method Descriptions

<a id="member-dbd36c33145b"></a>
### Bind

`public System.Void Bind(System.String path)`

Binds a local Unix-domain endpoint before connecting.

path: Nonempty platform endpoint path.

<a id="member-2c5fae1b9940"></a>
### ConnectToHost

`public System.Void ConnectToHost(System.String path)`

Starts a nonblocking connection to a Unix-domain endpoint.

path: Nonempty platform endpoint path.

<a id="member-76ea845126ee"></a>
### GetConnectedPath

`public System.String GetConnectedPath()`

Returns the selected remote path, or empty after disconnection.

Returns: The endpoint path.

## Verification and limits

[NetworkingTests](../../tests/Electron2D.Tests/NetworkingTests.cs) verifies the exercised native Linux x64 IPv4/IPv6 loopback and UDS paths, binary/framing/queue edges, lifecycle, owner checks, scene request/reply and warmed allocation boundaries. Native allocator totals, other hosts/browser transport, real routed traffic, network throughput and owner acceptance remain unverified. Dynamic-value wire encoding is excluded under ADR 0001; raw typed bytes are executable. See [coverage](../coverage/classes/StreamPeerUDS.md).
