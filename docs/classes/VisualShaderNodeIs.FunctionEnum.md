# VisualShaderNodeIs.FunctionEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeIs.cs](../../src/Scene/Resources/VisualShaderNodeIs.cs).

```csharp
public enum Electron2D.VisualShaderNodeIs.FunctionEnum
```

## Description

The floating classification performed by this definition.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeIs.FunctionEnum IsInf = 0`](#member-39ea41517e6f) | Tests positive or negative infinity. |
| [`public const Electron2D.VisualShaderNodeIs.FunctionEnum IsNaN = 1`](#member-23a06ea065be) | Tests not-a-number values. |
| [`public const Electron2D.VisualShaderNodeIs.FunctionEnum Max = 2`](#member-cbeeb9487033) | The nonselectable function-count sentinel. |

<a id="member-39ea41517e6f"></a>
### `IsInf`

```csharp
public const Electron2D.VisualShaderNodeIs.FunctionEnum IsInf = 0
```

Tests positive or negative infinity.

<a id="member-23a06ea065be"></a>
### `IsNaN`

```csharp
public const Electron2D.VisualShaderNodeIs.FunctionEnum IsNaN = 1
```

Tests not-a-number values.

<a id="member-cbeeb9487033"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeIs.FunctionEnum Max = 2
```

The nonselectable function-count sentinel.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; excluded roles do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed requests, dormant tolerance, condition presence, transactional operand conversion, copy/archives and real software-rendered pixels. See [the component contract](../components/visual-shaders.md#comparisons-and-conditional-results) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
