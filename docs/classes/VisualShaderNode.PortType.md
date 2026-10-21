# VisualShaderNode.PortType

Last updated: 2026-10-21

Source: [`VisualShaderNode.cs`](../../src/Scene/Resources/VisualShaderNode.cs).

```csharp
public enum Electron2D.VisualShaderNode.PortType
```

## Description

The numeric value role carried by a graph port.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNode.PortType Boolean = 6`](#member-bc51c1d68f30) | A Boolean scalar. |
| [`public const Electron2D.VisualShaderNode.PortType Max = 9`](#member-042e6a654b2d) | The nonselectable upper bound of the complete logical port type table. |
| [`public const Electron2D.VisualShaderNode.PortType Scalar = 0`](#member-bed930ad0a4f) | A single-precision scalar. |
| [`public const Electron2D.VisualShaderNode.PortType ScalarInt = 1`](#member-a5ab7a2bf8c0) | A signed 32-bit scalar. |
| [`public const Electron2D.VisualShaderNode.PortType ScalarUInt = 2`](#member-dc29b158f555) | An unsigned 32-bit scalar. |
| [`public const Electron2D.VisualShaderNode.PortType Vector2 = 3`](#member-ab6e384ab888) | Two numeric floating components. |
| [`public const Electron2D.VisualShaderNode.PortType Vector3 = 4`](#member-93033b68b5b5) | Three numeric floating components. |
| [`public const Electron2D.VisualShaderNode.PortType Vector4 = 5`](#member-6926103f2c95) | Four numeric floating components. |

<a id="member-bc51c1d68f30"></a>
### `Boolean`

```csharp
public const Electron2D.VisualShaderNode.PortType Boolean = 6
```

A Boolean scalar.


<a id="member-042e6a654b2d"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNode.PortType Max = 9
```

The nonselectable upper bound of the complete logical port type table.


<a id="member-bed930ad0a4f"></a>
### `Scalar`

```csharp
public const Electron2D.VisualShaderNode.PortType Scalar = 0
```

A single-precision scalar.


<a id="member-a5ab7a2bf8c0"></a>
### `ScalarInt`

```csharp
public const Electron2D.VisualShaderNode.PortType ScalarInt = 1
```

A signed 32-bit scalar.


<a id="member-dc29b158f555"></a>
### `ScalarUInt`

```csharp
public const Electron2D.VisualShaderNode.PortType ScalarUInt = 2
```

An unsigned 32-bit scalar.


<a id="member-ab6e384ab888"></a>
### `Vector2`

```csharp
public const Electron2D.VisualShaderNode.PortType Vector2 = 3
```

Two numeric floating components.


<a id="member-93033b68b5b5"></a>
### `Vector3`

```csharp
public const Electron2D.VisualShaderNode.PortType Vector3 = 4
```

Three numeric floating components.


<a id="member-6926103f2c95"></a>
### `Vector4`

```csharp
public const Electron2D.VisualShaderNode.PortType Vector4 = 5
```

Four numeric floating components.


## Lifecycle and verification

This enum or immutable value carrier has ordinary value lifetime and is not disposable. Enum sentinels are not selectable roles; typed default entries validate their numeric value at construction. Authoring and snapshots are allocating cold operations.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
