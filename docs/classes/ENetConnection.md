# ENetConnection

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.ENetConnection`. **Source:** [ENetConnection.cs](../../src/Core/Networking/ENetConnection.cs).

## Description

Owns an ENet protocol host, its typed peers and native UDP/DTLS transport.

Owner-thread Service advances reliability/events and queues received packets on associated peers. Constructors, host/channel/peer/codec preparation allocate; ready service/span packet loops reuse managed storage. ENet/FastLZ are unchanged native dependency sources; native allocator totals remain separate.

See [native ENet](../components/enet.md) for channel mapping, packet/native ownership, DTLS, budgets and verification.

## Example

The public excerpt compiles and executes in the documentation consumer. The executable tests supply complete connection/poll/scene flows.

```csharp
using var host = new ENetConnection();
host.CreateHostBound("127.0.0.1", 0, 4, 3);
host.Compress(ENetCompressionMode.RangeCoder);
int port = host.GetLocalPort();
ENetEvent progress = host.Service();
ENetPacketPeer[] peers = host.GetPeers();
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public ENetConnection()` | Creates an inactive ENet host owner. |

## Constructor Descriptions

<a id="member-d67ec31c30a6"></a>
### .ctor

`public ENetConnection()`

Creates an inactive ENet host owner.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void BandwidthLimit(System.UInt32 inBandwidth = 0, System.UInt32 outBandwidth = 0)` | Sets native incoming/outgoing rate limits. |
| `public System.Void BandwidthThrottle()` | Runs native bandwidth-throttle adaptation. |
| `public System.Void Broadcast(System.Int32 channel, System.ReadOnlySpan<System.Byte> packet, Electron2D.ENetPacketFlags flags)` | Queues a packet to every currently connected peer. |
| `public System.Void ChannelLimit(System.Int32 maxChannels)` | Sets the channel limit for future connections. |
| `public System.Void Compress(Electron2D.ENetCompressionMode mode)` | Replaces the native codec used for subsequent packets. |
| `public Electron2D.ENetPacketPeer ConnectToHost(System.String address, System.Int32 port, System.Int32 channels = 0, System.UInt32 data = 0)` | Starts one outgoing connection from this host. |
| `public System.Void CreateHost(System.Int32 maxPeers = 32, System.Int32 maxChannels = 0, System.UInt32 inBandwidth = 0, System.UInt32 outBandwidth = 0)` | Creates a native host bound to an automatically selected local UDP port. |
| `public System.Void CreateHostBound(System.String bindAddress, System.Int32 port, System.Int32 maxPeers = 32, System.Int32 maxChannels = 0, System.UInt32 inBandwidth = 0, System.UInt32 outBandwidth = 0)` | Creates a bound native host. |
| `public System.Void DTLSClientSetup(System.String hostname, Electron2D.TLSOptions clientOptions = null)` | Selects validating/unsafe DTLS for the next outgoing connection. |
| `public System.Void DTLSServerSetup(Electron2D.TLSOptions serverOptions)` | Upgrades an idle bound host to DTLS server transport. |
| `public System.Void Destroy()` | Destroys host/peer state and owned transports; idle destruction is valid. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public System.Void Flush()` | Flushes queued outgoing protocol data without waiting for events. |
| `public System.Int32 GetLocalPort()` | Returns the bound/auto-bound local port. |
| `public System.Int32 GetMaxChannels()` | Returns the current native channel limit. |
| `public Electron2D.ENetPacketPeer[] GetPeers()` | Copies the current logical peer membership. |
| `public System.Double PopStatistic(Electron2D.ENetHostStatistic statistic)` | Returns and resets one native host statistic. |
| `public System.Void RefuseNewConnections(System.Boolean refuse)` | Rejects previously unknown inbound endpoints while preserving current peers. |
| `public Electron2D.ENetEvent Service(System.Int32 timeout = 0)` | Services one event, optionally waiting for network progress. |
| `public System.Void SocketSend(System.String destinationAddress, System.Int32 destinationPort, System.ReadOnlySpan<System.Byte> packet)` | Sends one raw native transport datagram. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-7ef89c71b5ee"></a>
### BandwidthLimit

`public System.Void BandwidthLimit(System.UInt32 inBandwidth = 0, System.UInt32 outBandwidth = 0)`

Sets native incoming/outgoing rate limits.

- `inBandwidth`: Bytes/second; zero unlimited.
- `outBandwidth`: Bytes/second; zero unlimited.

<a id="member-ac6c9ef6ac5b"></a>
### BandwidthThrottle

`public System.Void BandwidthThrottle()`

Runs native bandwidth-throttle adaptation.

<a id="member-af2d331082e3"></a>
### Broadcast

`public System.Void Broadcast(System.Int32 channel, System.ReadOnlySpan<System.Byte> packet, Electron2D.ENetPacketFlags flags)`

Queues a packet to every currently connected peer.

- `channel`: Native channel.
- `packet`: Borrowed bytes copied for each recipient.
- `flags`: ENet packet flags.

<a id="member-f9dcd4aecc7b"></a>
### ChannelLimit

`public System.Void ChannelLimit(System.Int32 maxChannels)`

Sets the channel limit for future connections.

- `maxChannels`: Zero selects 255; otherwise one through 255.

<a id="member-adbd66b5f9d6"></a>
### Compress

`public System.Void Compress(Electron2D.ENetCompressionMode mode)`

Replaces the native codec used for subsequent packets.

Codec setup allocates; the prepared native adapter uses bounded buffers.

- `mode`: The ENet compression domain; peers must agree.

<a id="member-9de1ee3a2f8c"></a>
### ConnectToHost

`public Electron2D.ENetPacketPeer ConnectToHost(System.String address, System.Int32 port, System.Int32 channels = 0, System.UInt32 data = 0)`

Starts one outgoing connection from this host.

The host must have no current peers, matching its outgoing-client role.

Returns: A borrowed connecting peer; host owns its native lifetime.

- `address`: Native IP literal or DNS name.
- `port`: One through 65535.
- `channels`: Zero selects 255, otherwise one through 255.
- `data`: Remote connection-event data.

<a id="member-bcdab6cbf800"></a>
### CreateHost

`public System.Void CreateHost(System.Int32 maxPeers = 32, System.Int32 maxChannels = 0, System.UInt32 inBandwidth = 0, System.UInt32 outBandwidth = 0)`

Creates a native host bound to an automatically selected local UDP port.

- `maxPeers`: One through 4095.
- `maxChannels`: Zero selects 255; otherwise one through 255.
- `inBandwidth`: Incoming bytes/second; zero unlimited.
- `outBandwidth`: Outgoing bytes/second; zero unlimited.

<a id="member-4d295d64a90f"></a>
### CreateHostBound

`public System.Void CreateHostBound(System.String bindAddress, System.Int32 port, System.Int32 maxPeers = 32, System.Int32 maxChannels = 0, System.UInt32 inBandwidth = 0, System.UInt32 outBandwidth = 0)`

Creates a bound native host.

- `bindAddress`: IPv4/IPv6 literal or wildcard.
- `port`: Zero selects an ephemeral port; otherwise zero through 65535.
- `maxPeers`: One through 4095.
- `maxChannels`: Zero selects 255.
- `inBandwidth`: Incoming byte rate, zero unlimited.
- `outBandwidth`: Outgoing byte rate, zero unlimited.

<a id="member-f425a6d0251c"></a>
### DTLSClientSetup

`public System.Void DTLSClientSetup(System.String hostname, Electron2D.TLSOptions clientOptions = null)`

Selects validating/unsafe DTLS for the next outgoing connection.

- `hostname`: Expected DNS/IP identity.
- `clientOptions`: Client options, null selects retained default system-trust options.

<a id="member-38dde65a1dc8"></a>
### DTLSServerSetup

`public System.Void DTLSServerSetup(Electron2D.TLSOptions serverOptions)`

Upgrades an idle bound host to DTLS server transport.

- `serverOptions`: Server key/certificate configuration.

<a id="member-27dfb4e07ef3"></a>
### Destroy

`public System.Void Destroy()`

Destroys host/peer state and owned transports; idle destruction is valid.

<a id="member-e6a77f725a3c"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

- `System.AggregateException`: Both notification delivery and derived cleanup fail.
- `System.Exception`: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-fbb80d9036db"></a>
### Flush

`public System.Void Flush()`

Flushes queued outgoing protocol data without waiting for events.

<a id="member-f8bbc7676c8a"></a>
### GetLocalPort

`public System.Int32 GetLocalPort()`

Returns the bound/auto-bound local port.

Returns: The active host local UDP port.

<a id="member-a9bf546274cb"></a>
### GetMaxChannels

`public System.Int32 GetMaxChannels()`

Returns the current native channel limit.

Returns: One through 255.

<a id="member-aa403c9c11ac"></a>
### GetPeers

`public Electron2D.ENetPacketPeer[] GetPeers()`

Copies the current logical peer membership.

Returns: Borrowed peers, including connecting/disconnecting entries.

<a id="member-6993dab03e3c"></a>
### PopStatistic

`public System.Double PopStatistic(Electron2D.ENetHostStatistic statistic)`

Returns and resets one native host statistic.

Returns: The counter before reset as an exact double.

- `statistic`: Host statistic domain.

<a id="member-8365f6ca7d85"></a>
### RefuseNewConnections

`public System.Void RefuseNewConnections(System.Boolean refuse)`

Rejects previously unknown inbound endpoints while preserving current peers.

- `refuse`: Whether to refuse new admission.

<a id="member-b123e62185cb"></a>
### Service

`public Electron2D.ENetEvent Service(System.Int32 timeout = 0)`

Services one event, optionally waiting for network progress.

Returns: A typed event; Receive payloads queue on Peer for inherited packet reads.

- `timeout`: Nonnegative milliseconds; zero is nonblocking.

- `System.IO.IOException`: Native service or transport fails.

<a id="member-d6470d3e7d81"></a>
### SocketSend

`public System.Void SocketSend(System.String destinationAddress, System.Int32 destinationPort, System.ReadOnlySpan<System.Byte> packet)`

Sends one raw native transport datagram.

This bypasses ENet reliability/framing and uses configured transport encryption if applicable.

- `destinationAddress`: IP literal or resolvable DNS name.
- `destinationPort`: One through 65535.
- `packet`: Borrowed packet bytes.

<a id="member-f8c47d8c64d0"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Lifecycle, verification and limits

ENetTests verifies native host/peer and multiplayer flows through public API, including the scene consumer. Connection/setup/snapshots allocate; prepared owner-thread packet/span/service/poll intervals reuse managed storage. Native core packet queues and codec internals allocate separately. Linux x64 execution and deployed native payload are checked; Linux ARM64, foreign/browser/routed performance and human/editor/rendered acceptance remain unverified.
