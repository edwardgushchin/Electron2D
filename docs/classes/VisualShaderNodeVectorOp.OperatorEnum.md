# VisualShaderNodeVectorOp.OperatorEnum

Last updated: 2026-10-21

Source: [VisualShaderNodeVectorOp.cs](../../src/Scene/Resources/VisualShaderNodeVectorOp.cs).

```csharp
public enum Electron2D.VisualShaderNodeVectorOp.OperatorEnum
```

## Description

The binary vector operation.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Add = 0`](#member-77d2cf7f869c) | Adds the inputs. |
| [`public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Atan2 = 9`](#member-bdb652e1dea9) | Computes the two-argument arctangent of the first and second inputs. |
| [`public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Cross = 8`](#member-50c3cfb865e4) | Computes the three-component cross product. |
| [`public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Divide = 3`](#member-efc699a9356f) | Divides the first input by the second. |
| [`public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum EnumSize = 12`](#member-f6549ede1f82) | The nonselectable operation-count sentinel. |
| [`public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Max = 6`](#member-ef3ae32f7584) | Selects the larger input. |
| [`public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Min = 7`](#member-5398bdb8d122) | Selects the smaller input. |
| [`public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Modulo = 4`](#member-36197d00eabe) | Computes x minus y times floor(x divided by y). |
| [`public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Multiply = 2`](#member-1603e8e8e61c) | Multiplies the inputs. |
| [`public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Power = 5`](#member-981895871204) | Raises the first input to the second input's power. |
| [`public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Reflect = 10`](#member-5b12d2f9406f) | Reflects the incident first vector around the second vector. |
| [`public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Step = 11`](#member-69ac73792ec7) | Returns zero when the second input is below the first, otherwise one. |
| [`public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Subtract = 1`](#member-e1f2f4896759) | Subtracts the second input from the first. |

<a id="member-77d2cf7f869c"></a>
### `Add`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Add = 0
```

Adds the inputs.

<a id="member-bdb652e1dea9"></a>
### `Atan2`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Atan2 = 9
```

Computes the two-argument arctangent of the first and second inputs.

<a id="member-50c3cfb865e4"></a>
### `Cross`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Cross = 8
```

Computes the three-component cross product.

<a id="member-efc699a9356f"></a>
### `Divide`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Divide = 3
```

Divides the first input by the second.

<a id="member-f6549ede1f82"></a>
### `EnumSize`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum EnumSize = 12
```

The nonselectable operation-count sentinel.

<a id="member-ef3ae32f7584"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Max = 6
```

Selects the larger input.

<a id="member-5398bdb8d122"></a>
### `Min`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Min = 7
```

Selects the smaller input.

<a id="member-36197d00eabe"></a>
### `Modulo`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Modulo = 4
```

Computes x minus y times floor(x divided by y).

<a id="member-1603e8e8e61c"></a>
### `Multiply`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Multiply = 2
```

Multiplies the inputs.

<a id="member-981895871204"></a>
### `Power`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Power = 5
```

Raises the first input to the second input's power.

<a id="member-5b12d2f9406f"></a>
### `Reflect`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Reflect = 10
```

Reflects the incident first vector around the second vector.

<a id="member-69ac73792ec7"></a>
### `Step`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Step = 11
```

Returns zero when the second input is below the first, otherwise one.

<a id="member-e1f2f4896759"></a>
### `Subtract`

```csharp
public const Electron2D.VisualShaderNodeVectorOp.OperatorEnum Subtract = 1
```

Subtracts the second input from the first.

## Lifecycle and verification

This owner-specific numeric selector has value lifetime and is not disposable. EnumSize is a nonselectable count sentinel. Max selects the maximum operation. Distinct scalar/vector domains retain their own numeric identities.

The public VisualShader.Consumer covers typed requests, copy/archives, every scalar/vector function and real software-rendered pixels. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
