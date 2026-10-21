# VisualShaderNodeMatrixOp.OperatorEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeMatrixOp.cs](../../src/Scene/Resources/VisualShaderNodeMatrixOp.cs).

```csharp
public enum Electron2D.VisualShaderNodeMatrixOp.OperatorEnum
```

## Description

The selected native numeric operation.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum Add = 4`](#member-e371072452fb) | Adds matching components. |
| [`public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum Divide = 7`](#member-0ab009c9b932) | Divides matching components of the first matrix by the second. |
| [`public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum DivideReversed = 8`](#member-3abcc3b7336a) | Divides matching components of the second matrix by the first. |
| [`public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum Max = 9`](#member-f821b8ee27e0) | The nonselectable operator-count sentinel. |
| [`public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum Multiply = 0`](#member-d244f6e1c02a) | Multiplies the first matrix by the second. |
| [`public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum MultiplyComponents = 2`](#member-bf8fa1de4e83) | Multiplies matching components. |
| [`public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum MultiplyComponentsReversed = 3`](#member-a67a893c6c7f) | Multiplies matching components in reversed operand order. |
| [`public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum MultiplyReversed = 1`](#member-3979ea11369b) | Multiplies the second matrix by the first. |
| [`public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum Subtract = 5`](#member-f4815d2013a1) | Subtracts the second matrix from the first. |
| [`public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum SubtractReversed = 6`](#member-08a1ad0d0d9b) | Subtracts the first matrix from the second. |

<a id="member-e371072452fb"></a>
### `Add`

```csharp
public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum Add = 4
```

Adds matching components.

<a id="member-0ab009c9b932"></a>
### `Divide`

```csharp
public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum Divide = 7
```

Divides matching components of the first matrix by the second.

<a id="member-3abcc3b7336a"></a>
### `DivideReversed`

```csharp
public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum DivideReversed = 8
```

Divides matching components of the second matrix by the first.

<a id="member-f821b8ee27e0"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum Max = 9
```

The nonselectable operator-count sentinel.

<a id="member-d244f6e1c02a"></a>
### `Multiply`

```csharp
public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum Multiply = 0
```

Multiplies the first matrix by the second.

<a id="member-bf8fa1de4e83"></a>
### `MultiplyComponents`

```csharp
public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum MultiplyComponents = 2
```

Multiplies matching components.

<a id="member-a67a893c6c7f"></a>
### `MultiplyComponentsReversed`

```csharp
public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum MultiplyComponentsReversed = 3
```

Multiplies matching components in reversed operand order.

<a id="member-3979ea11369b"></a>
### `MultiplyReversed`

```csharp
public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum MultiplyReversed = 1
```

Multiplies the second matrix by the first.

<a id="member-f4815d2013a1"></a>
### `Subtract`

```csharp
public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum Subtract = 5
```

Subtracts the second matrix from the first.

<a id="member-08a1ad0d0d9b"></a>
### `SubtractReversed`

```csharp
public const Electron2D.VisualShaderNodeMatrixOp.OperatorEnum SubtractReversed = 6
```

Subtracts the first matrix from the second.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed requests, all sixteen matrix components, copy/archive state, row/column layout and real software-rendered pixels. See [the component contract](../components/visual-shaders.md#comparisons-and-conditional-results) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
