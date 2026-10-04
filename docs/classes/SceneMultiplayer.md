# SceneMultiplayer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.SceneMultiplayer`. **Source:** [SceneMultiplayer.cs](../../src/Core/Networking/SceneMultiplayer.cs).

## Description

Polls admitted scene peers, typed RPCs, authentication and custom packets with server relay.

The transport remains caller-owned. Packet/authentication spans are borrowed during callbacks. Root paths locate nodes; immutable typed RPC tokens replace reflection. Preparation/path discovery allocates, while repeated ready span/message processing reuses bounded buffers. Calls/disposal require the owner thread.

**Inherits:** [MultiplayerAPI](MultiplayerAPI.md).

[Scene multiplayer](../components/scene-multiplayer.md) defines typed codecs/policies, owner/lifetime, wire validation, authentication, branch integration and preparation. RPC tokens/delegates are immutable borrowed configuration; no backend handle or general dynamic value is exposed.

## Example

Public API excerpt. The server example requests an ephemeral local port; native tests execute the full two-client WS/WSS scene workflow.

```csharp
using var transport = new WebSocketMultiplayerPeer();
transport.CreateServer(0, "127.0.0.1");
using var api = new SceneMultiplayer { MultiplayerPeer = transport };
using var tree = new SceneTree(new Node { Name = "Root" });
tree.SetMultiplayer(api);
// ProcessFrame polls api before node callbacks; continue frames while connected.
tree.ProcessFrame(0);
tree.SetMultiplayer(null); // Detach before disposing the caller-owned api.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public SceneMultiplayer()` | Creates a scene interface with local offline authority. |

## Constructor Descriptions

<a id="member-f2503f38b13b"></a>
### .ctor

`public SceneMultiplayer()`

Creates a scene interface with local offline authority.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.MultiplayerPacketHandler AuthCallback { get; set; }` | Gets or sets a borrowed authentication-data callback; null automatically admits new peers. |
| `public System.Double AuthTimeout { get; set; }` | Gets or sets the monotonic authentication deadline in seconds. |
| `public System.Int32 MaxPacketBytes { get; set; }` | Gets or sets the complete encoded message budget prepared on transport assignment. |
| `public Electron2D.MultiplayerPeer MultiplayerPeer { get; set; }` | Gets or replaces a borrowed connecting/connected transport; replacement clears network/path state. |
| `public System.Boolean RefuseNewConnections { get; set; }` | Gets or sets admission policy on the configured transport. |
| `public System.String RootPath { get; set; }` | Gets or sets the absolute scene root used by relative node RPC paths. |
| `public System.Boolean ServerRelay { get; set; }` | Gets or sets whether supported transports notify clients and relay messages through the server. |

## Property Descriptions

<a id="member-aa470999dbec"></a>
### AuthCallback

`public Electron2D.MultiplayerPacketHandler AuthCallback { get; set; }`

Gets or sets a borrowed authentication-data callback; null automatically admits new peers.

Value: Changes apply to new authentication sessions; pending sessions retain the current callback.

<a id="member-5625cae26a2c"></a>
### AuthTimeout

`public System.Double AuthTimeout { get; set; }`

Gets or sets the monotonic authentication deadline in seconds.

Value: 3 initially; finite nonnegative, zero disables expiration.

<a id="member-50f328162681"></a>
### MaxPacketBytes

`public System.Int32 MaxPacketBytes { get; set; }`

Gets or sets the complete encoded message budget prepared on transport assignment.

Value: 65536 initially; 64 through 64 MiB. Change while no live network transport is configured.

<a id="member-e65893f37630"></a>
### MultiplayerPeer

`public Electron2D.MultiplayerPeer MultiplayerPeer { get; set; }`

Gets or replaces a borrowed connecting/connected transport; replacement clears network/path state.

System.InvalidOperationException: A disconnected or disposed peer is supplied.

<a id="member-6fed0e47a687"></a>
### RefuseNewConnections

`public System.Boolean RefuseNewConnections { get; set; }`

Gets or sets admission policy on the configured transport.

Value: False without a peer; setting without a peer fails.

<a id="member-1e5ae6a74ccd"></a>
### RootPath

`public System.String RootPath { get; set; }`

Gets or sets the absolute scene root used by relative node RPC paths.

Value: Empty until SceneTree assignment. Changing it invalidates prepared path discovery.

<a id="member-8d031a92d7dd"></a>
### ServerRelay

`public System.Boolean ServerRelay { get; set; }`

Gets or sets whether supported transports notify clients and relay messages through the server.

Value: True initially; changing live topology is caller-controlled.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void Clear()` | Clears admitted/pending/path state without closing the borrowed transport. |
| `public System.Void CompleteAuth(System.Int32 id)` | Commits local authentication and sends its completion; admission waits for remote completion. |
| `public System.Void DisconnectPeer(System.Int32 id)` | Removes a direct peer and closes its connection, suppressing this interface's local removal event. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public System.Int32[] GetAuthenticatingPeers()` | Returns a caller-owned snapshot of direct peers awaiting bilateral authentication. |
| `public override System.Int32[] GetPeers()` | Returns a caller-owned snapshot of admitted remote identities. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `public override System.Int32 GetRemoteSenderID()` | Gets the sender of the currently executing message/local RPC. |
| `public override System.Int32 GetUniqueID()` | Gets the local transport identity. |
| `public override System.Void ObjectConfigurationAdd(System.String rootPath)` | Configures a scene-root path through a typed overload. |
| `public override System.Void ObjectConfigurationRemove(System.String rootPath)` | Removes the currently matching root configuration. |
| `public override System.Void Poll()` | Advances transport and synchronous scene messages. |
| `public override System.Void RPC<TNode, T>(System.Int32 peer, TNode node, RPCMethod<TNode, T> method, T arguments)` | Projects inherited lifecycle behavior for this concrete type. |
| `public System.Void SendAuth(System.Int32 id, System.ReadOnlySpan<System.Byte> data)` | Sends nonempty authentication bytes before either participant completes authentication. |
| `public System.Void SendBytes(System.ReadOnlySpan<System.Byte> bytes, System.Int32 id = 0, Electron2D.TransferMode mode = Reliable, System.Int32 channel = 0)` | Sends nonempty custom bytes to admitted peers. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-5afc63969042"></a>
### Clear

`public System.Void Clear()`

Clears admitted/pending/path state without closing the borrowed transport.

<a id="member-9e8e07eac447"></a>
### CompleteAuth

`public System.Void CompleteAuth(System.Int32 id)`

Commits local authentication and sends its completion; admission waits for remote completion.

id: Pending direct identity.

<a id="member-3cb7f01fef01"></a>
### DisconnectPeer

`public System.Void DisconnectPeer(System.Int32 id)`

Removes a direct peer and closes its connection, suppressing this interface's local removal event.

id: Direct remote identity; relayed client peers cannot be closed by another client.

<a id="member-b370fe1cf0ac"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-d65363d70329"></a>
### GetAuthenticatingPeers

`public System.Int32[] GetAuthenticatingPeers()`

Returns a caller-owned snapshot of direct peers awaiting bilateral authentication.

Returns: Pending identities, without admitted peers.

<a id="member-63688b5bf373"></a>
### GetPeers

`public override System.Int32[] GetPeers()`

Returns a caller-owned snapshot of admitted remote identities.

Returns: Connected peers, excluding authenticating peers.

<a id="member-d4d9c15ceb15"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Overrides append or replace descriptors; they must not yield null entries.

<a id="member-5f8ce42a6dc7"></a>
### GetRemoteSenderID

`public override System.Int32 GetRemoteSenderID()`

Gets the sender of the currently executing message/local RPC.

Returns: Zero outside synchronous dispatch.

<a id="member-f92e609fe0c1"></a>
### GetUniqueID

`public override System.Int32 GetUniqueID()`

Gets the local transport identity.

Returns: Zero without a peer; server identity is one.

<a id="member-1158693ed856"></a>
### ObjectConfigurationAdd

`public override System.Void ObjectConfigurationAdd(System.String rootPath)`

Configures a scene-root path through a typed overload.

rootPath: Absolute scene root path, or empty to clear it.

<a id="member-3211d425a458"></a>
### ObjectConfigurationRemove

`public override System.Void ObjectConfigurationRemove(System.String rootPath)`

Removes the currently matching root configuration.

rootPath: The configured root path.

<a id="member-7002c4f159fd"></a>
### Poll

`public override System.Void Poll()`

Advances transport and synchronous scene messages.

<a id="member-54067aae6ebb"></a>
### RPC

`public override System.Void RPC<TNode, T>(System.Int32 peer, TNode node, RPCMethod<TNode, T> method, T arguments)`

Projects inherited lifecycle behavior for this concrete type.

<a id="member-fd449f485062"></a>
### SendAuth

`public System.Void SendAuth(System.Int32 id, System.ReadOnlySpan<System.Byte> data)`

Sends nonempty authentication bytes before either participant completes authentication.

id: Pending direct remote identity.

data: Borrowed nonempty bytes.

<a id="member-86d41acc8734"></a>
### SendBytes

`public System.Void SendBytes(System.ReadOnlySpan<System.Byte> bytes, System.Int32 id = 0, Electron2D.TransferMode mode = Reliable, System.Int32 channel = 0)`

Sends nonempty custom bytes to admitted peers.

bytes: Borrowed payload.

id: Zero broadcasts, positive targets, negative excludes.

mode: Requested transport mode.

channel: Nonnegative channel.

<a id="member-66a62d486241"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Event summary

| Complete C# signature | Contract |
| --- | --- |
| `public event System.Action<System.Int32> PeerAuthenticating` | Occurs before admission when a direct peer begins authentication. |
| `public event System.Action<System.Int32> PeerAuthenticationFailed` | Occurs after a pending peer fails/disconnects/expires. |
| `public event Electron2D.MultiplayerPacketHandler PeerPacket` | Delivers custom bytes with original sender identity; storage is borrowed only during the callback. |

## Event Descriptions

<a id="member-2cd3055ffce6"></a>
### PeerAuthenticating

`public event System.Action<System.Int32> PeerAuthenticating`

Occurs before admission when a direct peer begins authentication.

<a id="member-68a1234b7933"></a>
### PeerAuthenticationFailed

`public event System.Action<System.Int32> PeerAuthenticationFailed`

Occurs after a pending peer fails/disconnects/expires.

<a id="member-e5686654deb9"></a>
### PeerPacket

`public event Electron2D.MultiplayerPacketHandler PeerPacket`

Delivers custom bytes with original sender identity; storage is borrowed only during the callback.

## Verification and limits

[SceneMultiplayerTests](../../tests/Electron2D.Tests/SceneMultiplayerTests.cs) verifies public Node/SceneTree WS/WSS relay/RPC/authentication and managed provider hooks, lifetime/validation/failure recovery and prepared active/idle allocation boundaries. General dynamic object decoding is excluded. Spawner/synchronizer/property-schema replication and its packet limits remain exact dependent capabilities; they are absent from implemented member tables. Foreign hosts/routed throughput/native allocator totals and human/rendered/editor/agent acceptance are separate gates.
