# VisualShaderNodeFloatOp.OperatorEnum

Last updated: 2026-10-21

Source: [VisualShaderNodeFloatOp.cs](../../src/Scene/Resources/VisualShaderNodeFloatOp.cs).

```csharp
public enum Electron2D.VisualShaderNodeFloatOp.OperatorEnum
```

## Description

The binary scalar operation.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Add = 0`](#member-a164bd4ebfd5) | Adds the inputs. |
| [`public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Atan2 = 8`](#member-ec2bfc38948d) | Computes the two-argument arctangent of the first and second inputs. |
| [`public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Divide = 3`](#member-1802ffacd3ac) | Divides the first input by the second. |
| [`public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum EnumSize = 10`](#member-269bccd5f5e6) | The nonselectable operation-count sentinel. |
| [`public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Max = 6`](#member-83450dd1ce83) | Selects the larger input. |
| [`public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Min = 7`](#member-ed67ee48fa93) | Selects the smaller input. |
| [`public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Modulo = 4`](#member-33fa813588ae) | Computes x minus y times floor(x divided by y). |
| [`public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Multiply = 2`](#member-deebfbaa8a79) | Multiplies the inputs. |
| [`public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Power = 5`](#member-2037757e6064) | Raises the first input to the second input's power. |
| [`public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Step = 9`](#member-71522ff93c47) | Returns zero when the second input is below the first, otherwise one. |
| [`public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Subtract = 1`](#member-91c74e52fa19) | Subtracts the second input from the first. |

<a id="member-a164bd4ebfd5"></a>
### `Add`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Add = 0
```

Adds the inputs.

<a id="member-ec2bfc38948d"></a>
### `Atan2`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Atan2 = 8
```

Computes the two-argument arctangent of the first and second inputs.

<a id="member-1802ffacd3ac"></a>
### `Divide`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Divide = 3
```

Divides the first input by the second.

<a id="member-269bccd5f5e6"></a>
### `EnumSize`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum EnumSize = 10
```

The nonselectable operation-count sentinel.

<a id="member-83450dd1ce83"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Max = 6
```

Selects the larger input.

<a id="member-ed67ee48fa93"></a>
### `Min`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Min = 7
```

Selects the smaller input.

<a id="member-33fa813588ae"></a>
### `Modulo`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Modulo = 4
```

Computes x minus y times floor(x divided by y).

<a id="member-deebfbaa8a79"></a>
### `Multiply`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Multiply = 2
```

Multiplies the inputs.

<a id="member-2037757e6064"></a>
### `Power`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Power = 5
```

Raises the first input to the second input's power.

<a id="member-71522ff93c47"></a>
### `Step`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Step = 9
```

Returns zero when the second input is below the first, otherwise one.

<a id="member-91c74e52fa19"></a>
### `Subtract`

```csharp
public const Electron2D.VisualShaderNodeFloatOp.OperatorEnum Subtract = 1
```

Subtracts the second input from the first.

## Lifecycle and verification

This owner-specific numeric selector has value lifetime and is not disposable. EnumSize is a nonselectable count sentinel. Max selects the maximum operation. Distinct scalar/vector domains retain their own numeric identities.

The public VisualShader.Consumer covers typed requests, copy/archives, every scalar/vector function and real software-rendered pixels. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
