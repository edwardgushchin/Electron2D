# GraphConnection

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public struct Electron2D.GraphConnection`. **Source:** [source](../../src/Scene/GUI/GraphEdit.cs). **Component:** [Graph authoring](../components/graph-authoring.md).

## Description

Immutable value projection of a directed connection dictionary. FromNode/ToNode are direct scene names; FromPort/ToPort are compressed enabled-port indices. KeepAlive retains missing endpoint records; activity remains runtime state on GraphEdit. Record equality compares all five fields.

## Members

| Declaration | Contract |
| --- | --- |
| [`public GraphConnection(System.String FromNode, System.Int32 FromPort, System.String ToNode, System.Int32 ToPort, System.Boolean KeepAlive = false)`](#member-570ee6f6c1b0) | Describes a directed graph connection independently of scene or resource ownership. |
| [`public System.Void Deconstruct(out System.String FromNode, out System.Int32 FromPort, out System.String ToNode, out System.Int32 ToPort, out System.Boolean KeepAlive)`](#member-e5497f58e382) | Standard immutable record value/equality behavior. |
| [`public virtual System.Boolean Equals(Electron2D.GraphConnection other)`](#member-f8b7cb385961) | Standard immutable record value/equality behavior. |
| [`public override System.Boolean Equals(System.Object obj)`](#member-f83991f90a96) | Standard immutable record value/equality behavior. |
| [`public override System.Int32 GetHashCode()`](#member-8d4f004f5df6) | Standard immutable record value/equality behavior. |
| [`public override System.String ToString()`](#member-16b2bd5acb6a) | Standard immutable record value/equality behavior. |
| [`public static System.Boolean op_Equality(Electron2D.GraphConnection left, Electron2D.GraphConnection right)`](#member-b3630dded861) | Standard immutable record value/equality behavior. |
| [`public static System.Boolean op_Inequality(Electron2D.GraphConnection left, Electron2D.GraphConnection right)`](#member-ab177ba7d8b5) | Standard immutable record value/equality behavior. |
| [`public System.String FromNode { get; set; }`](#member-e161dbf7df13) | Output node name. |
| [`public System.Int32 FromPort { get; set; }`](#member-fc11816fd160) | Compressed output port. |
| [`public System.Boolean KeepAlive { get; set; }`](#member-5b70fec4a3e7) | Retain the record while an endpoint is missing. |
| [`public System.String ToNode { get; set; }`](#member-c8b04822cd83) | Input node name. |
| [`public System.Int32 ToPort { get; set; }`](#member-91a7783dda2a) | Compressed input port. |

## Example

```csharp
var connection = new GraphConnection("Source", 0, "Target", 1, KeepAlive: true);
using var graph = new GraphEdit { Connections = [connection] };
```

## Lifetime and verification

Attached reads and mutations use the scene owner thread. Own mutations reject active port/connection drawing; borrowed resources remain caller-owned. GraphNode and GraphFrame titlebars and title labels, and GraphEdit menu/layers cannot be disposed independently. GraphElement/GraphEdit disposal releases owned children and event subscriptions. See the component for executable checks, preparation boundaries and remaining dependencies.

## Member details

<a id="member-570ee6f6c1b0"></a>
### `GraphConnection(System.String, System.Int32, System.String, System.Int32, System.Boolean)`

```csharp
public GraphConnection(System.String FromNode, System.Int32 FromPort, System.String ToNode, System.Int32 ToPort, System.Boolean KeepAlive = false)
```

Describes a directed graph connection independently of scene or resource ownership.

- `FromNode`: Output node name.
- `FromPort`: Compressed output port.
- `ToNode`: Input node name.
- `ToPort`: Compressed input port.
- `KeepAlive`: Retain the record while an endpoint is missing.

<a id="member-e5497f58e382"></a>
### `Deconstruct(ref System.String, ref System.Int32, ref System.String, ref System.Int32, ref System.Boolean)`

```csharp
public System.Void Deconstruct(out System.String FromNode, out System.Int32 FromPort, out System.String ToNode, out System.Int32 ToPort, out System.Boolean KeepAlive)
```

Standard immutable record value/equality behavior.

<a id="member-f8b7cb385961"></a>
### `Equals(Electron2D.GraphConnection)`

```csharp
public virtual System.Boolean Equals(Electron2D.GraphConnection other)
```

Standard immutable record value/equality behavior.

<a id="member-f83991f90a96"></a>
### `Equals(System.Object)`

```csharp
public override System.Boolean Equals(System.Object obj)
```

Standard immutable record value/equality behavior.

<a id="member-8d4f004f5df6"></a>
### `GetHashCode()`

```csharp
public override System.Int32 GetHashCode()
```

Standard immutable record value/equality behavior.

<a id="member-16b2bd5acb6a"></a>
### `ToString()`

```csharp
public override System.String ToString()
```

Standard immutable record value/equality behavior.

<a id="member-b3630dded861"></a>
### `op_Equality(Electron2D.GraphConnection, Electron2D.GraphConnection)`

```csharp
public static System.Boolean op_Equality(Electron2D.GraphConnection left, Electron2D.GraphConnection right)
```

Standard immutable record value/equality behavior.

<a id="member-ab177ba7d8b5"></a>
### `op_Inequality(Electron2D.GraphConnection, Electron2D.GraphConnection)`

```csharp
public static System.Boolean op_Inequality(Electron2D.GraphConnection left, Electron2D.GraphConnection right)
```

Standard immutable record value/equality behavior.

<a id="member-e161dbf7df13"></a>
### `FromNode`

```csharp
public System.String FromNode { get; set; }
```

Output node name.

<a id="member-fc11816fd160"></a>
### `FromPort`

```csharp
public System.Int32 FromPort { get; set; }
```

Compressed output port.

<a id="member-5b70fec4a3e7"></a>
### `KeepAlive`

```csharp
public System.Boolean KeepAlive { get; set; }
```

Retain the record while an endpoint is missing.

<a id="member-c8b04822cd83"></a>
### `ToNode`

```csharp
public System.String ToNode { get; set; }
```

Input node name.

<a id="member-91a7783dda2a"></a>
### `ToPort`

```csharp
public System.Int32 ToPort { get; set; }
```

Compressed input port.
