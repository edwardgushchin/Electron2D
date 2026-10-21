# VisualShaderConnection

Last updated: 2026-10-21

Source: [`VisualShaderConnection.cs`](../../src/Scene/Resources/VisualShaderConnection.cs).

```csharp
public struct Electron2D.VisualShaderConnection
```

## Description

A copied directed connection between two visual shader ports.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderConnection(System.Int32 FromNode, System.Int32 FromPort, System.Int32 ToNode, System.Int32 ToPort)`](#member-1965554bdda8) | A copied directed connection between two visual shader ports. |
| [`public System.Void Deconstruct(out System.Int32 FromNode, out System.Int32 FromPort, out System.Int32 ToNode, out System.Int32 ToPort)`](#member-0e5187100035) | Value equality, hashing, formatting or deconstruction for the copied connection fields. |
| [`public virtual System.Boolean Equals(Electron2D.VisualShaderConnection other)`](#member-cb0ab107408a) | Value equality, hashing, formatting or deconstruction for the copied connection fields. |
| [`public override System.Boolean Equals(System.Object obj)`](#member-5528c17199d0) | Value equality, hashing, formatting or deconstruction for the copied connection fields. |
| [`public override System.Int32 GetHashCode()`](#member-7350e2a63da0) | Value equality, hashing, formatting or deconstruction for the copied connection fields. |
| [`public override System.String ToString()`](#member-1a5f0dd5c185) | Value equality, hashing, formatting or deconstruction for the copied connection fields. |
| [`public static System.Boolean op_Equality(Electron2D.VisualShaderConnection left, Electron2D.VisualShaderConnection right)`](#member-3f86774cb0bd) | Value equality, hashing, formatting or deconstruction for the copied connection fields. |
| [`public static System.Boolean op_Inequality(Electron2D.VisualShaderConnection left, Electron2D.VisualShaderConnection right)`](#member-50cb71988bc9) | Value equality, hashing, formatting or deconstruction for the copied connection fields. |
| [`public System.Int32 FromNode { get; set; }`](#member-810ce9ccb332) | The source node ID. |
| [`public System.Int32 FromPort { get; set; }`](#member-bafc79403b81) | The zero-based source output. |
| [`public System.Int32 ToNode { get; set; }`](#member-b6c4f8e8d989) | The destination node ID, including the reserved output. |
| [`public System.Int32 ToPort { get; set; }`](#member-6696ee3503db) | The zero-based destination input. |

<a id="member-1965554bdda8"></a>
### `.ctor`

```csharp
public VisualShaderConnection(System.Int32 FromNode, System.Int32 FromPort, System.Int32 ToNode, System.Int32 ToPort)
```

A copied directed connection between two visual shader ports.

- `FromNode`: The source node ID.
- `FromPort`: The zero-based source output.
- `ToNode`: The destination node ID, including the reserved output.
- `ToPort`: The zero-based destination input.

<a id="member-0e5187100035"></a>
### `Deconstruct`

```csharp
public System.Void Deconstruct(out System.Int32 FromNode, out System.Int32 FromPort, out System.Int32 ToNode, out System.Int32 ToPort)
```

This member uses the typed value-record or inherited Resource contract shown in the declaration.


<a id="member-cb0ab107408a"></a>
### `Equals`

```csharp
public virtual System.Boolean Equals(Electron2D.VisualShaderConnection other)
```

This member uses the typed value-record or inherited Resource contract shown in the declaration.


<a id="member-5528c17199d0"></a>
### `Equals`

```csharp
public override System.Boolean Equals(System.Object obj)
```

This member uses the typed value-record or inherited Resource contract shown in the declaration.


<a id="member-7350e2a63da0"></a>
### `GetHashCode`

```csharp
public override System.Int32 GetHashCode()
```

This member uses the typed value-record or inherited Resource contract shown in the declaration.


<a id="member-1a5f0dd5c185"></a>
### `ToString`

```csharp
public override System.String ToString()
```

This member uses the typed value-record or inherited Resource contract shown in the declaration.


<a id="member-3f86774cb0bd"></a>
### `op_Equality`

```csharp
public static System.Boolean op_Equality(Electron2D.VisualShaderConnection left, Electron2D.VisualShaderConnection right)
```

This member uses the typed value-record or inherited Resource contract shown in the declaration.


<a id="member-50cb71988bc9"></a>
### `op_Inequality`

```csharp
public static System.Boolean op_Inequality(Electron2D.VisualShaderConnection left, Electron2D.VisualShaderConnection right)
```

This member uses the typed value-record or inherited Resource contract shown in the declaration.


<a id="member-810ce9ccb332"></a>
### `FromNode`

```csharp
public System.Int32 FromNode { get; set; }
```

The source node ID.


<a id="member-bafc79403b81"></a>
### `FromPort`

```csharp
public System.Int32 FromPort { get; set; }
```

The zero-based source output.


<a id="member-b6c4f8e8d989"></a>
### `ToNode`

```csharp
public System.Int32 ToNode { get; set; }
```

The destination node ID, including the reserved output.


<a id="member-6696ee3503db"></a>
### `ToPort`

```csharp
public System.Int32 ToPort { get; set; }
```

The zero-based destination input.


## Lifecycle and verification

This enum or immutable value carrier has ordinary value lifetime and is not disposable. Enum sentinels are not selectable roles; typed default entries validate their numeric value at construction. Authoring and snapshots are allocating cold operations.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
