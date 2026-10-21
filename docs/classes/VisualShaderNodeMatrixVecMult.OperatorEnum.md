# VisualShaderNodeMatrixVecMult.OperatorEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeMatrixVecMult.cs](../../src/Scene/Resources/VisualShaderNodeMatrixVecMult.cs).

```csharp
public enum Electron2D.VisualShaderNodeMatrixVecMult.OperatorEnum
```

## Description

The selected native numeric operation.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeMatrixVecMult.OperatorEnum DirectionTimesMatrix = 3`](#member-42c0cfa630e8) | Multiplies the row (x,y,z,0) by the matrix and returns xyz. |
| [`public const Electron2D.VisualShaderNodeMatrixVecMult.OperatorEnum MatrixTimesDirection = 2`](#member-cdba55aea58e) | Multiplies the matrix by the column (x,y,z,0) and returns xyz. |
| [`public const Electron2D.VisualShaderNodeMatrixVecMult.OperatorEnum MatrixTimesVector = 0`](#member-ac2a415e877c) | Multiplies the matrix by the column (x,y,z,1) and returns xyz. |
| [`public const Electron2D.VisualShaderNodeMatrixVecMult.OperatorEnum Max = 4`](#member-5f28ec718472) | The nonselectable operator-count sentinel. |
| [`public const Electron2D.VisualShaderNodeMatrixVecMult.OperatorEnum VectorTimesMatrix = 1`](#member-3945fa138b9f) | Multiplies the row (x,y,z,1) by the matrix and returns xyz. |

<a id="member-42c0cfa630e8"></a>
### `DirectionTimesMatrix`

```csharp
public const Electron2D.VisualShaderNodeMatrixVecMult.OperatorEnum DirectionTimesMatrix = 3
```

Multiplies the row (x,y,z,0) by the matrix and returns xyz.

<a id="member-cdba55aea58e"></a>
### `MatrixTimesDirection`

```csharp
public const Electron2D.VisualShaderNodeMatrixVecMult.OperatorEnum MatrixTimesDirection = 2
```

Multiplies the matrix by the column (x,y,z,0) and returns xyz.

<a id="member-ac2a415e877c"></a>
### `MatrixTimesVector`

```csharp
public const Electron2D.VisualShaderNodeMatrixVecMult.OperatorEnum MatrixTimesVector = 0
```

Multiplies the matrix by the column (x,y,z,1) and returns xyz.

<a id="member-5f28ec718472"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeMatrixVecMult.OperatorEnum Max = 4
```

The nonselectable operator-count sentinel.

<a id="member-3945fa138b9f"></a>
### `VectorTimesMatrix`

```csharp
public const Electron2D.VisualShaderNodeMatrixVecMult.OperatorEnum VectorTimesMatrix = 1
```

Multiplies the row (x,y,z,1) by the matrix and returns xyz.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed requests, all sixteen matrix components, copy/archive state, row/column layout and real software-rendered pixels. See [the component contract](../components/visual-shaders.md#comparisons-and-conditional-results) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
