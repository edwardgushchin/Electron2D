# MultiplayerAPI

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public abstract class Electron2D.MultiplayerAPI`. **Source:** [MultiplayerAPI.cs](../../src/Core/Networking/MultiplayerAPI.cs).

## Description

Defines typed scene multiplayer dispatch, peer queries, configuration and connection events.

Managed consumers override the public typed hooks directly. Calls/disposal require the constructing thread. The API borrows its transport; SceneTree owns only default interfaces it creates. Applications dispose supplied APIs.

**Inherits:** [ElectronObject](ElectronObject.md).

[Scene multiplayer](../components/scene-multiplayer.md) defines typed codecs/policies, owner/lifetime, wire validation, authentication, branch integration and preparation. RPC tokens/delegates are immutable borrowed configuration; no backend handle or general dynamic value is exposed.

## Example

Public API excerpt. The server example requires port 8080 available; native tests execute the full two-client WS/WSS scene workflow.

```csharp
using MultiplayerAPI api = MultiplayerAPI.CreateDefaultInterface();
api.Poll();
int localID = api.GetUniqueID();
int[] admitted = api.GetPeers();
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `protected MultiplayerAPI()` | Creates an interface on the current owner thread. |

## Constructor Descriptions

<a id="member-f42421a26f2d"></a>
### .ctor

`protected MultiplayerAPI()`

Creates an interface on the current owner thread.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.MultiplayerPeer MultiplayerPeer { get; set; }` | Gets or replaces the borrowed connecting/connected transport. |

## Property Descriptions

<a id="member-3f3871ca1651"></a>
### MultiplayerPeer

`public Electron2D.MultiplayerPeer MultiplayerPeer { get; set; }`

Gets or replaces the borrowed connecting/connected transport.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected System.Void CheckMultiplayer()` | Validates owner thread and lifetime. |
| `public static Electron2D.MultiplayerAPI CreateDefaultInterface()` | Creates a caller-owned instance through the typed default factory. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `protected System.Void EmitConnectedToServer()` | Delivers all client admission subscribers; failures aggregate. |
| `protected System.Void EmitConnectionFailed()` | Delivers all establishment failure subscribers; failures aggregate. |
| `protected System.Void EmitPeerConnected(System.Int32 id)` | Delivers all admitted-peer subscribers. |
| `protected System.Void EmitPeerDisconnected(System.Int32 id)` | Delivers all removed-peer subscribers. |
| `protected System.Void EmitServerDisconnected()` | Delivers all server loss subscribers. |
| `public static System.Func<Electron2D.MultiplayerAPI> GetDefaultInterface()` | Gets the currently configured typed default factory. |
| `public abstract System.Int32[] GetPeers()` | Returns a caller-owned snapshot of admitted remote identities. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `public abstract System.Int32 GetRemoteSenderID()` | Gets the sender of the currently executing message/local RPC. |
| `public abstract System.Int32 GetUniqueID()` | Gets the local transport identity. |
| `public System.Boolean HasMultiplayerPeer()` | Reports whether a transport is configured. |
| `public System.Boolean IsServer()` | Reports whether the local multiplayer identity is the server. |
| `public abstract System.Void ObjectConfigurationAdd(System.String rootPath)` | Configures a scene-root path through a typed overload. |
| `public abstract System.Void ObjectConfigurationRemove(System.String rootPath)` | Removes the currently matching root configuration. |
| `public abstract System.Void Poll()` | Advances transport and synchronous scene messages. |
| `public abstract System.Void RPC<TNode, T>(System.Int32 peer, TNode node, RPCMethod<TNode, T> method, T arguments)` | Projects inherited lifecycle behavior for this concrete type. |
| `public static System.Void SetDefaultInterface(System.Func<Electron2D.MultiplayerAPI> factory)` | Sets the default factory for future trees/interfaces. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-9db5b23a733a"></a>
### CheckMultiplayer

`protected System.Void CheckMultiplayer()`

Validates owner thread and lifetime.

<a id="member-311d6b647fcc"></a>
### CreateDefaultInterface

`public static Electron2D.MultiplayerAPI CreateDefaultInterface()`

Creates a caller-owned instance through the typed default factory.

Returns: A live interface owned by the calling thread.

<a id="member-9f44466910e2"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-269855b7ab6a"></a>
### EmitConnectedToServer

`protected System.Void EmitConnectedToServer()`

Delivers all client admission subscribers; failures aggregate.

<a id="member-ad9383a59642"></a>
### EmitConnectionFailed

`protected System.Void EmitConnectionFailed()`

Delivers all establishment failure subscribers; failures aggregate.

<a id="member-8e877af9dc1a"></a>
### EmitPeerConnected

`protected System.Void EmitPeerConnected(System.Int32 id)`

Delivers all admitted-peer subscribers.

id: Committed identity.

<a id="member-5d88e8f4923d"></a>
### EmitPeerDisconnected

`protected System.Void EmitPeerDisconnected(System.Int32 id)`

Delivers all removed-peer subscribers.

id: Removed identity.

<a id="member-c644d26ee583"></a>
### EmitServerDisconnected

`protected System.Void EmitServerDisconnected()`

Delivers all server loss subscribers.

<a id="member-9d98be9e3f56"></a>
### GetDefaultInterface

`public static System.Func<Electron2D.MultiplayerAPI> GetDefaultInterface()`

Gets the currently configured typed default factory.

Returns: The shared factory delegate.

<a id="member-4f8a979e43ba"></a>
### GetPeers

`public abstract System.Int32[] GetPeers()`

Returns a caller-owned snapshot of admitted remote identities.

Returns: Connected peers, excluding authenticating peers.

<a id="member-7b736bd5afce"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Overrides append or replace descriptors; they must not yield null entries.

<a id="member-6ab2749dc260"></a>
### GetRemoteSenderID

`public abstract System.Int32 GetRemoteSenderID()`

Gets the sender of the currently executing message/local RPC.

Returns: Zero outside synchronous dispatch.

<a id="member-94b8e7314ec1"></a>
### GetUniqueID

`public abstract System.Int32 GetUniqueID()`

Gets the local transport identity.

Returns: Zero without a peer; server identity is one.

<a id="member-f556a2b774dc"></a>
### HasMultiplayerPeer

`public System.Boolean HasMultiplayerPeer()`

Reports whether a transport is configured.

Returns: True when MultiplayerPeer is nonnull.

<a id="member-db702775ee09"></a>
### IsServer

`public System.Boolean IsServer()`

Reports whether the local multiplayer identity is the server.

Returns: True when the unique ID equals one.

<a id="member-8423b11348ac"></a>
### ObjectConfigurationAdd

`public abstract System.Void ObjectConfigurationAdd(System.String rootPath)`

Configures a scene-root path through a typed overload.

rootPath: Absolute scene root path, or empty to clear it.

<a id="member-f69af9ff74ac"></a>
### ObjectConfigurationRemove

`public abstract System.Void ObjectConfigurationRemove(System.String rootPath)`

Removes the currently matching root configuration.

rootPath: The configured root path.

<a id="member-e6b6c6b74caf"></a>
### Poll

`public abstract System.Void Poll()`

Advances transport and synchronous scene messages.

<a id="member-94f22be8aa6c"></a>
### RPC

`public abstract System.Void RPC<TNode, T>(System.Int32 peer, TNode node, RPCMethod<TNode, T> method, T arguments)`

Projects inherited lifecycle behavior for this concrete type.

<a id="member-c132061c31ac"></a>
### SetDefaultInterface

`public static System.Void SetDefaultInterface(System.Func<Electron2D.MultiplayerAPI> factory)`

Sets the default factory for future trees/interfaces.

factory: Typed constructor; existing interfaces are unaffected.

<a id="member-983ab2525bdd"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Event summary

| Complete C# signature | Contract |
| --- | --- |
| `public event System.Action ConnectedToServer` | Occurs when a client is admitted by the server. |
| `public event System.Action ConnectionFailed` | Occurs when transport connection establishment fails. |
| `public event System.Action<System.Int32> PeerConnected` | Occurs when an admitted peer is committed. |
| `public event System.Action<System.Int32> PeerDisconnected` | Occurs after an admitted peer is removed. |
| `public event System.Action ServerDisconnected` | Occurs when an established client loses its server. |

## Event Descriptions

<a id="member-df565bbda008"></a>
### ConnectedToServer

`public event System.Action ConnectedToServer`

Occurs when a client is admitted by the server.

<a id="member-eb3f1d94b23c"></a>
### ConnectionFailed

`public event System.Action ConnectionFailed`

Occurs when transport connection establishment fails.

<a id="member-df2b07dd2a23"></a>
### PeerConnected

`public event System.Action<System.Int32> PeerConnected`

Occurs when an admitted peer is committed.

<a id="member-79f4836d1677"></a>
### PeerDisconnected

`public event System.Action<System.Int32> PeerDisconnected`

Occurs after an admitted peer is removed.

<a id="member-a33741eb5132"></a>
### ServerDisconnected

`public event System.Action ServerDisconnected`

Occurs when an established client loses its server.

## Verification and limits

[SceneMultiplayerTests](../../tests/Electron2D.Tests/SceneMultiplayerTests.cs) verifies public Node/SceneTree WS/WSS relay/RPC/authentication and managed provider hooks, lifetime/validation/failure recovery and prepared active/idle allocation boundaries. General dynamic object decoding is excluded. Spawner/synchronizer/property-schema replication and its packet limits remain exact dependent capabilities; they are absent from implemented member tables. Foreign hosts/routed throughput/native allocator totals and human/rendered/editor/agent acceptance are separate gates.
