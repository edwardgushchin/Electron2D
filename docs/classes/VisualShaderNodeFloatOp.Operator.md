# VisualShaderNodeFloatOp.Operator

Last updated: 2026-10-21

Source: [`VisualShaderNodeFloatOp.cs`](../../src/Scene/Resources/VisualShaderNodeFloatOp.cs).

```csharp
public enum Electron2D.VisualShaderNodeFloatOp.Operator
```

## Description

The binary scalar operation.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeFloatOp.Operator Add = 0`](#member-dcb027157993) | Adds the inputs. |
| [`public const Electron2D.VisualShaderNodeFloatOp.Operator Atan2 = 8`](#member-516c8ee935f7) | Computes the two-argument arctangent of the first and second inputs. |
| [`public const Electron2D.VisualShaderNodeFloatOp.Operator Divide = 3`](#member-a55bb748e180) | Divides the first input by the second. |
| [`public const Electron2D.VisualShaderNodeFloatOp.Operator EnumSize = 10`](#member-763de3393072) | The nonselectable operation-count sentinel. |
| [`public const Electron2D.VisualShaderNodeFloatOp.Operator Max = 6`](#member-528767c35ee0) | Selects the larger input. |
| [`public const Electron2D.VisualShaderNodeFloatOp.Operator Min = 7`](#member-7c4aabb7b924) | Selects the smaller input. |
| [`public const Electron2D.VisualShaderNodeFloatOp.Operator Modulo = 4`](#member-cb2c94692c9e) | Computes x minus y times floor(x divided by y). |
| [`public const Electron2D.VisualShaderNodeFloatOp.Operator Multiply = 2`](#member-f907fb01d2d5) | Multiplies the inputs. |
| [`public const Electron2D.VisualShaderNodeFloatOp.Operator Power = 5`](#member-f30a0721cf8d) | Raises the first input to the second input's power. |
| [`public const Electron2D.VisualShaderNodeFloatOp.Operator Step = 9`](#member-592c27a3e5e5) | Returns zero when the second input is below the first, otherwise one. |
| [`public const Electron2D.VisualShaderNodeFloatOp.Operator Subtract = 1`](#member-c92be24ba196) | Subtracts the second input from the first. |

<a id="member-dcb027157993"></a>
### `Add`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.Operator Add = 0
```

Adds the inputs.


<a id="member-516c8ee935f7"></a>
### `Atan2`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.Operator Atan2 = 8
```

Computes the two-argument arctangent of the first and second inputs.


<a id="member-a55bb748e180"></a>
### `Divide`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.Operator Divide = 3
```

Divides the first input by the second.


<a id="member-763de3393072"></a>
### `EnumSize`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.Operator EnumSize = 10
```

The nonselectable operation-count sentinel.


<a id="member-528767c35ee0"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.Operator Max = 6
```

Selects the larger input.


<a id="member-7c4aabb7b924"></a>
### `Min`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.Operator Min = 7
```

Selects the smaller input.


<a id="member-cb2c94692c9e"></a>
### `Modulo`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.Operator Modulo = 4
```

Computes x minus y times floor(x divided by y).


<a id="member-f907fb01d2d5"></a>
### `Multiply`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.Operator Multiply = 2
```

Multiplies the inputs.


<a id="member-f30a0721cf8d"></a>
### `Power`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.Operator Power = 5
```

Raises the first input to the second input's power.


<a id="member-592c27a3e5e5"></a>
### `Step`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.Operator Step = 9
```

Returns zero when the second input is below the first, otherwise one.


<a id="member-c92be24ba196"></a>
### `Subtract`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.Operator Subtract = 1
```

Subtracts the second input from the first.


## Lifecycle and verification

This enum or immutable value carrier has ordinary value lifetime and is not disposable. Enum sentinels are not selectable roles; typed default entries validate their numeric value at construction. Authoring and snapshots are allocating cold operations.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
