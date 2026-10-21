# VisualShaderNodeVectorOp.Operator

Last updated: 2026-10-21

Source: [`VisualShaderNodeVectorOp.cs`](../../src/Scene/Resources/VisualShaderNodeVectorOp.cs).

```csharp
public enum Electron2D.VisualShaderNodeVectorOp.Operator
```

## Description

The binary vector operation.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeVectorOp.Operator Add = 0`](#member-f555b31772a3) | Adds the inputs. |
| [`public const Electron2D.VisualShaderNodeVectorOp.Operator Atan2 = 9`](#member-879c3fadee29) | Computes the two-argument arctangent of the first and second inputs. |
| [`public const Electron2D.VisualShaderNodeVectorOp.Operator Cross = 8`](#member-b057ffa5f428) | Computes the three-component cross product. |
| [`public const Electron2D.VisualShaderNodeVectorOp.Operator Divide = 3`](#member-cd79bf13ba4b) | Divides the first input by the second. |
| [`public const Electron2D.VisualShaderNodeVectorOp.Operator EnumSize = 12`](#member-9638aab94965) | The nonselectable operation-count sentinel. |
| [`public const Electron2D.VisualShaderNodeVectorOp.Operator Max = 6`](#member-a32707b0df27) | Selects the larger input. |
| [`public const Electron2D.VisualShaderNodeVectorOp.Operator Min = 7`](#member-c81b1331662b) | Selects the smaller input. |
| [`public const Electron2D.VisualShaderNodeVectorOp.Operator Modulo = 4`](#member-cc3865717722) | Computes x minus y times floor(x divided by y). |
| [`public const Electron2D.VisualShaderNodeVectorOp.Operator Multiply = 2`](#member-f6acace96e52) | Multiplies the inputs. |
| [`public const Electron2D.VisualShaderNodeVectorOp.Operator Power = 5`](#member-db2071b5e504) | Raises the first input to the second input's power. |
| [`public const Electron2D.VisualShaderNodeVectorOp.Operator Reflect = 10`](#member-832eb2e7fe20) | Reflects the incident first vector around the second vector. |
| [`public const Electron2D.VisualShaderNodeVectorOp.Operator Step = 11`](#member-f2d0ee4d3c85) | Returns zero when the second input is below the first, otherwise one. |
| [`public const Electron2D.VisualShaderNodeVectorOp.Operator Subtract = 1`](#member-60aec2fac65d) | Subtracts the second input from the first. |

<a id="member-f555b31772a3"></a>
### `Add`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.Operator Add = 0
```

Adds the inputs.


<a id="member-879c3fadee29"></a>
### `Atan2`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.Operator Atan2 = 9
```

Computes the two-argument arctangent of the first and second inputs.


<a id="member-b057ffa5f428"></a>
### `Cross`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.Operator Cross = 8
```

Computes the three-component cross product.


<a id="member-cd79bf13ba4b"></a>
### `Divide`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.Operator Divide = 3
```

Divides the first input by the second.


<a id="member-9638aab94965"></a>
### `EnumSize`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.Operator EnumSize = 12
```

The nonselectable operation-count sentinel.


<a id="member-a32707b0df27"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.Operator Max = 6
```

Selects the larger input.


<a id="member-c81b1331662b"></a>
### `Min`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.Operator Min = 7
```

Selects the smaller input.


<a id="member-cc3865717722"></a>
### `Modulo`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.Operator Modulo = 4
```

Computes x minus y times floor(x divided by y).


<a id="member-f6acace96e52"></a>
### `Multiply`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.Operator Multiply = 2
```

Multiplies the inputs.


<a id="member-db2071b5e504"></a>
### `Power`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.Operator Power = 5
```

Raises the first input to the second input's power.


<a id="member-832eb2e7fe20"></a>
### `Reflect`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.Operator Reflect = 10
```

Reflects the incident first vector around the second vector.


<a id="member-f2d0ee4d3c85"></a>
### `Step`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.Operator Step = 11
```

Returns zero when the second input is below the first, otherwise one.


<a id="member-60aec2fac65d"></a>
### `Subtract`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.Operator Subtract = 1
```

Subtracts the second input from the first.


## Lifecycle and verification

This enum or immutable value carrier has ordinary value lifetime and is not disposable. Enum sentinels are not selectable roles; typed default entries validate their numeric value at construction. Authoring and snapshots are allocating cold operations.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
