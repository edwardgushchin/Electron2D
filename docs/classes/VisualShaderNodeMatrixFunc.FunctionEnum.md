# VisualShaderNodeMatrixFunc.FunctionEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeMatrixFunc.cs](../../src/Scene/Resources/VisualShaderNodeMatrixFunc.cs).

```csharp
public enum Electron2D.VisualShaderNodeMatrixFunc.FunctionEnum
```

## Description

The selected native numeric operation.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeMatrixFunc.FunctionEnum Inverse = 0`](#member-20c6ffa85549) | Computes the native matrix inverse. |
| [`public const Electron2D.VisualShaderNodeMatrixFunc.FunctionEnum Max = 2`](#member-2d4fc12b6a59) | The nonselectable function-count sentinel. |
| [`public const Electron2D.VisualShaderNodeMatrixFunc.FunctionEnum Transpose = 1`](#member-9d1cce917890) | Swaps rows and columns. |

<a id="member-20c6ffa85549"></a>
### `Inverse`

```csharp
public const Electron2D.VisualShaderNodeMatrixFunc.FunctionEnum Inverse = 0
```

Computes the native matrix inverse.

<a id="member-2d4fc12b6a59"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeMatrixFunc.FunctionEnum Max = 2
```

The nonselectable function-count sentinel.

<a id="member-9d1cce917890"></a>
### `Transpose`

```csharp
public const Electron2D.VisualShaderNodeMatrixFunc.FunctionEnum Transpose = 1
```

Swaps rows and columns.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed requests, all sixteen matrix components, copy/archive state, row/column layout and real software-rendered pixels. See [the component contract](../components/visual-shaders.md#comparisons-and-conditional-results) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
