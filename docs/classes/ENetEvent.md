# ENetEvent

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public struct Electron2D.ENetEvent`. **Source:** [ENetPacketFlags.cs](../../src/Core/Networking/ENetPacketFlags.cs).

## Description

Contains one typed ENet service event; packet payloads are read from the associated peer.



See [native ENet](../components/enet.md) for channel mapping, packet/native ownership, DTLS, budgets and verification.

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public ENetEvent(Electron2D.ENetEventType Type, Electron2D.ENetPacketPeer Peer, System.UInt32 Data, System.Int32 Channel)` | Contains one typed ENet service event; packet payloads are read from the associated peer. |

## Constructor Descriptions

<a id="member-e62b4cc1b1bc"></a>
### .ctor

`public ENetEvent(Electron2D.ENetEventType Type, Electron2D.ENetPacketPeer Peer, System.UInt32 Data, System.Int32 Channel)`

Contains one typed ENet service event; packet payloads are read from the associated peer.

- `Type`: Event domain.
- `Peer`: Borrowed source peer or null for None.
- `Data`: Connection/disconnection data.
- `Channel`: Native channel for Receive, otherwise zero.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Int32 Channel { get; set; }` | Native channel for Receive, otherwise zero. |
| `public System.UInt32 Data { get; set; }` | Connection/disconnection data. |
| `public Electron2D.ENetPacketPeer Peer { get; set; }` | Borrowed source peer or null for None. |
| `public Electron2D.ENetEventType Type { get; set; }` | Event domain. |

## Property Descriptions

<a id="member-bf5e56a46ef1"></a>
### Channel

`public System.Int32 Channel { get; set; }`

Native channel for Receive, otherwise zero.

<a id="member-639c63a1070b"></a>
### Data

`public System.UInt32 Data { get; set; }`

Connection/disconnection data.

<a id="member-b0c41f04cd34"></a>
### Peer

`public Electron2D.ENetPacketPeer Peer { get; set; }`

Borrowed source peer or null for None.

<a id="member-738cda0bcd21"></a>
### Type

`public Electron2D.ENetEventType Type { get; set; }`

Event domain.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void Deconstruct(out Electron2D.ENetEventType Type, out Electron2D.ENetPacketPeer Peer, out System.UInt32 Data, out System.Int32 Channel)` | Compiler-provided typed value operation. |
| `public virtual System.Boolean Equals(Electron2D.ENetEvent other)` | Compiler-provided typed value operation. |
| `public override System.Boolean Equals(System.Object obj)` | Compiler-provided typed value operation. |
| `public override System.Int32 GetHashCode()` | Compiler-provided typed value operation. |
| `public override System.String ToString()` | Returns a diagnostic string containing the runtime class name and instance identifier. |

## Method Descriptions

<a id="member-b074a08e29eb"></a>
### Deconstruct

`public System.Void Deconstruct(out Electron2D.ENetEventType Type, out Electron2D.ENetPacketPeer Peer, out System.UInt32 Data, out System.Int32 Channel)`

Compiler-provided typed value operation.

<a id="member-b7ef4f6734f6"></a>
### Equals

`public virtual System.Boolean Equals(Electron2D.ENetEvent other)`

Compiler-provided typed value operation.

<a id="member-ec0079698ee0"></a>
### Equals

`public override System.Boolean Equals(System.Object obj)`

Compiler-provided typed value operation.

<a id="member-32aa335a8d5e"></a>
### GetHashCode

`public override System.Int32 GetHashCode()`

Compiler-provided typed value operation.

<a id="member-faa8c6d09334"></a>
### ToString

`public override System.String ToString()`

Returns a diagnostic string containing the runtime class name and instance identifier.

Returns: A string in the form `<ClassName>#<InstanceID>`.

## Operator summary

| Complete C# signature | Contract |
| --- | --- |
| `public static System.Boolean op_Equality(Electron2D.ENetEvent left, Electron2D.ENetEvent right)` | Compiler-provided typed value operation. |
| `public static System.Boolean op_Inequality(Electron2D.ENetEvent left, Electron2D.ENetEvent right)` | Compiler-provided typed value operation. |

## Operator Descriptions

<a id="member-506d7196938e"></a>
### op_Equality

`public static System.Boolean op_Equality(Electron2D.ENetEvent left, Electron2D.ENetEvent right)`

Compiler-provided typed value operation.

<a id="member-2b561952725d"></a>
### op_Inequality

`public static System.Boolean op_Inequality(Electron2D.ENetEvent left, Electron2D.ENetEvent right)`

Compiler-provided typed value operation.

## Lifecycle, verification and limits

ENetTests verifies native host/peer and multiplayer flows through public API, including the scene consumer. Connection/setup/snapshots allocate; prepared owner-thread packet/span/service/poll intervals reuse managed storage. Native core packet queues and codec internals allocate separately. Linux x64 execution and deployed native payload are checked; Linux ARM64, foreign/browser/routed performance and human/editor/rendered acceptance remain unverified.
