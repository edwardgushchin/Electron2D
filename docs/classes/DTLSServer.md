# DTLSServer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.DTLSServer`. **Source:** [DTLSServer.cs](../../src/Core/Networking/DTLSServer.cs).

## Description

Prepares a DTLS server identity and address-bound cookie secret for borrowed UDP peers.

Calls/disposal require the constructing thread. Setup retains key/certificate payloads; accepted peers independently retain them and survive server disposal. The helper owns no UDP socket. Native setup and cookie/session preparation allocate; repeated authenticated packet polling uses prepared buffers.

**Inherits:** [ElectronObject](ElectronObject.md).

[Native DTLS](../components/dtls.md) defines complete record/cookie/timer flow, shared TLSStatus, trust/ownership, input budgets, cleanup and platform boundaries.

## Example

Public API excerpt. DTLSTests executes the complete client/server and independent native roles; the client excerpt requires a listening native server at port 4242 for handshake completion. Both excerpts compile; setup/polling is headless native networking and does not establish rendered or external application acceptance.

```csharp
using var crypto = new Crypto();
using var key = crypto.GenerateRSA(2048);
using var cert = crypto.GenerateSelfSignedCertificate(key);
using var options = TLSOptions.Server(key, cert);
using var server = new DTLSServer();
server.Setup(options);
// Poll UDPServer, TakeConnection, then server.TakeConnection(udpPeer).
// The caller owns each returned DTLS peer and its borrowed UDP peer.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public DTLSServer()` | Creates an unconfigured server helper. |

## Constructor Descriptions

<a id="member-b1f359050287"></a>
### .ctor

`public DTLSServer()`

Creates an unconfigured server helper.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `protected override System.Void Finalize()` | Releases abandoned native identity and resource retention without touching any UDP peer. |
| `public System.Void Setup(Electron2D.TLSOptions serverOptions)` | Configures one server identity and a fresh cookie secret. |
| `public Electron2D.PacketPeerDTLS TakeConnection(Electron2D.PacketPeerUDP udpPeer)` | Creates a caller-owned DTLS server peer and starts its polled cookie/handshake exchange. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-43a84e9b4220"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-9c16dd71173f"></a>
### Finalize

`protected override System.Void Finalize()`

Releases abandoned native identity and resource retention without touching any UDP peer.

<a id="member-38bed66b0375"></a>
### Setup

`public System.Void Setup(Electron2D.TLSOptions serverOptions)`

Configures one server identity and a fresh cookie secret.

serverOptions: Live server options containing a matching private key and certificate chain.

System.InvalidOperationException: The server is already configured.

System.ArgumentException: Options have the wrong role.

Remarks: Failure releases partial state, allowing a later valid setup. Options are consumed during setup; resources stay retained until helper and accepted sessions release them.

<a id="member-d1bb9f29bcf0"></a>
### TakeConnection

`public Electron2D.PacketPeerDTLS TakeConnection(Electron2D.PacketPeerUDP udpPeer)`

Creates a caller-owned DTLS server peer and starts its polled cookie/handshake exchange.

udpPeer: Borrowed live connected UDP endpoint, including an accepted UDPServer peer.

Returns: A handshaking or connected DTLS peer; cookie challenges continue on the same logical peer.

System.InvalidOperationException: Setup has not succeeded or UDP is unconnected.

Remarks: The caller owns both logical peers. A transport generation change rejects later DTLS use; authentication failures release session state and preserve the UDP owner.

<a id="member-2f291279e97c"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.


## Lifecycle, verification and limits

Constructing-thread operations/disposal reject a foreign thread before state mutation. Polling/send/disconnect/disposal reentry is guarded; UDP generation changes invalidate borrowed sessions. Security resources stay retained until helper and independent sessions release them. No wrapper closes the caller-owned UDP socket. Preparation/cookies/identity/snapshots allocate; prepared record/span/queue polling reuses bounded storage.

[DTLSTests](../../tests/Electron2D.Tests/DTLSTests.cs) verifies native public UDPServer/Node/SceneTree packets, cookie challenge, first-hello loss/retry, 488-byte and empty/oversized/undersized edges, anti-replay/bad-MAC recovery, name/chain/system/unsafe policy, rollback/reentry/owner/lifetime and independent OpenSSL s_server/s_client roles. Sixty-four warmed active/idle intervals report zero managed bytes. OpenSSL3.2+ native backend is currently Linux64; other platforms/browser, native allocation/routed performance, ENet and human/editor/rendered acceptance remain separate. See [platform verification](../platform-verification.md).
