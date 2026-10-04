# PacketPeerDTLS

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.PacketPeerDTLS`. **Source:** [PacketPeerDTLS.cs](../../src/Core/Networking/PacketPeerDTLS.cs).

## Description

Transfers authenticated DTLS datagrams over a borrowed connected PacketPeerUDP.

Calls/disposal require the constructing thread. Linux OpenSSL 3.2 or later supplies DTLS 1.2 and fixed datagram BIO storage. Poll preserves packet boundaries, progresses cookies/handshake/timers and remote closure. Preparation allocates; warmed span packet/poll cycles reuse bounded storage. The borrowed UDP peer must remain connected to its original endpoint and exclusively used for this session.

**Inherits:** [PacketPeer](PacketPeer.md).

[Native DTLS](../components/dtls.md) defines complete record/cookie/timer flow, shared TLSStatus, trust/ownership, input budgets, cleanup and platform boundaries.

## Example

Public API excerpt. DTLSTests executes the complete client/server and independent native roles; the client excerpt requires a listening native server at port 4242 for handshake completion. Both excerpts compile; setup/polling is headless native networking and does not establish rendered or external application acceptance.

```csharp
using var udp = new PacketPeerUDP();
udp.ConnectToHost("127.0.0.1", 4242);
using var options = TLSOptions.ClientUnsafe(); // Explicit local test policy.
using var peer = new PacketPeerDTLS();
peer.ConnectToPeer(udp, "localhost", options);
peer.Poll();
TLSStatus phase = peer.GetStatus();
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public PacketPeerDTLS()` | Creates a disconnected DTLS peer with prepared receive/record storage. |

## Constructor Descriptions

<a id="member-80026b8fad47"></a>
### .ctor

`public PacketPeerDTLS()`

Creates a disconnected DTLS peer with prepared receive/record storage.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void ConnectToPeer(Electron2D.PacketPeerUDP packetPeer, System.String hostname, Electron2D.TLSOptions clientOptions = null)` | Starts a polled validating or explicitly unsafe client handshake. |
| `public System.Void DisconnectFromPeer()` | Sends a best-effort close notification and releases DTLS state without closing UDP. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `protected override System.Void Finalize()` | Releases abandoned native state and resource retention without accessing the borrowed UDP peer. |
| `public override System.Int32 GetAvailablePacketCount()` | Returns the number of complete immediately available packets. |
| `public override System.Int32 GetMaxPacketSize()` | Reports the maximum outgoing raw payload supported by this transport. |
| `public Electron2D.TLSStatus GetStatus()` | Gets the cached authenticated session phase without polling. |
| `protected override System.Int32 NextPacketSize()` | Reports the next packet size without consuming it. |
| `public System.Void Poll()` | Advances datagram input/output, handshake/cookies, native retransmission timers and remote close. |
| `public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)` | Sends exactly one packet. |
| `protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)` | Consumes the next complete packet into a validated destination. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-201e1e53bc1a"></a>
### ConnectToPeer

`public System.Void ConnectToPeer(Electron2D.PacketPeerUDP packetPeer, System.String hostname, Electron2D.TLSOptions clientOptions = null)`

Starts a polled validating or explicitly unsafe client handshake.

packetPeer: Live connected UDP transport borrowed until disconnect/failure.

hostname: Expected DNS/IP identity, subject to an options override.

clientOptions: Client configuration; null selects system trust.

System.Security.Authentication.AuthenticationException: Setup, trust, expected-name or handshake validation fails.

System.PlatformNotSupportedException: The selected native datagram backend is unavailable.

<a id="member-09e18863e1e0"></a>
### DisconnectFromPeer

`public System.Void DisconnectFromPeer()`

Sends a best-effort close notification and releases DTLS state without closing UDP.

Remarks: Available packets clear; retained security resources are released. Idle calls are valid.

<a id="member-a415a1123805"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-d771f3d7e249"></a>
### Finalize

`protected override System.Void Finalize()`

Releases abandoned native state and resource retention without accessing the borrowed UDP peer.

<a id="member-2549f1b79d2f"></a>
### GetAvailablePacketCount

`public override System.Int32 GetAvailablePacketCount()`

Returns the number of complete immediately available packets.

Returns: Nonnegative packet count.

<a id="member-74c10b89580b"></a>
### GetMaxPacketSize

`public override System.Int32 GetMaxPacketSize()`

Reports the maximum outgoing raw payload supported by this transport.

Returns: A nonnegative byte count; operating-system limits may be narrower.

<a id="member-480130d8196b"></a>
### GetStatus

`public Electron2D.TLSStatus GetStatus()`

Gets the cached authenticated session phase without polling.

Returns: The shared TLS/DTLS status domain.

<a id="member-1343ac75512b"></a>
### NextPacketSize

`protected override System.Int32 NextPacketSize()`

Reports the next packet size without consuming it.

Returns: Length, or minus one when absent.

<a id="member-9500debdf576"></a>
### Poll

`public System.Void Poll()`

Advances datagram input/output, handshake/cookies, native retransmission timers and remote close.

System.Security.Authentication.AuthenticationException: Native authentication/record processing fails.

System.InvalidOperationException: Polling reenters or the borrowed UDP connection changes.

Remarks: Receive queue pressure drops complete plaintext packets; no partial packet is exposed.

<a id="member-1c5653df0d5f"></a>
### PutPacket

`public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)`

Sends exactly one packet.

data: Packet bytes, borrowed only for this call.

<a id="member-fc146115a09b"></a>
### ReadPacketCore

`protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)`

Consumes the next complete packet into a validated destination.

destination: An exact-sized destination.

<a id="member-0ffec3d887da"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.


## Lifecycle, verification and limits

Constructing-thread operations/disposal reject a foreign thread before state mutation. Polling/send/disconnect/disposal reentry is guarded; UDP generation changes invalidate borrowed sessions. Security resources stay retained until helper and independent sessions release them. No wrapper closes the caller-owned UDP socket. Preparation/cookies/identity/snapshots allocate; prepared record/span/queue polling reuses bounded storage.

[DTLSTests](../../tests/Electron2D.Tests/DTLSTests.cs) verifies native public UDPServer/Node/SceneTree packets, cookie challenge, first-hello loss/retry, 488-byte and empty/oversized/undersized edges, anti-replay/bad-MAC recovery, name/chain/system/unsafe policy, rollback/reentry/owner/lifetime and independent OpenSSL s_server/s_client roles. Sixty-four warmed active/idle intervals report zero managed bytes. OpenSSL3.2+ native backend is currently Linux64; other platforms/browser, native allocation/routed performance, ENet and human/editor/rendered acceptance remain separate. See [platform verification](../platform-verification.md).
