# VisualShaderNodeCompare.FunctionEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeCompare.cs](../../src/Scene/Resources/VisualShaderNodeCompare.cs).

```csharp
public enum Electron2D.VisualShaderNodeCompare.FunctionEnum
```

## Description

The comparison performed on each input component.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeCompare.FunctionEnum Equal = 0`](#member-8f36cef1ea39) | Tests equality, using tolerance only for floating scalars. |
| [`public const Electron2D.VisualShaderNodeCompare.FunctionEnum GreaterThan = 2`](#member-9b769d0defd4) | Tests whether the first value is larger. |
| [`public const Electron2D.VisualShaderNodeCompare.FunctionEnum GreaterThanEqual = 3`](#member-a4ecfe5491db) | Tests whether the first value is at least the second. |
| [`public const Electron2D.VisualShaderNodeCompare.FunctionEnum LessThan = 4`](#member-43e72ea8df8b) | Tests whether the first value is smaller. |
| [`public const Electron2D.VisualShaderNodeCompare.FunctionEnum LessThanEqual = 5`](#member-c569f51c1f37) | Tests whether the first value is at most the second. |
| [`public const Electron2D.VisualShaderNodeCompare.FunctionEnum Max = 6`](#member-a2a921cdfc2b) | The nonselectable function-count sentinel. |
| [`public const Electron2D.VisualShaderNodeCompare.FunctionEnum NotEqual = 1`](#member-0b4a7962cbd8) | Negates scalar tolerance equality or tests exact component inequality. |

<a id="member-8f36cef1ea39"></a>
### `Equal`

```csharp
public const Electron2D.VisualShaderNodeCompare.FunctionEnum Equal = 0
```

Tests equality, using tolerance only for floating scalars.

<a id="member-9b769d0defd4"></a>
### `GreaterThan`

```csharp
public const Electron2D.VisualShaderNodeCompare.FunctionEnum GreaterThan = 2
```

Tests whether the first value is larger.

<a id="member-a4ecfe5491db"></a>
### `GreaterThanEqual`

```csharp
public const Electron2D.VisualShaderNodeCompare.FunctionEnum GreaterThanEqual = 3
```

Tests whether the first value is at least the second.

<a id="member-43e72ea8df8b"></a>
### `LessThan`

```csharp
public const Electron2D.VisualShaderNodeCompare.FunctionEnum LessThan = 4
```

Tests whether the first value is smaller.

<a id="member-c569f51c1f37"></a>
### `LessThanEqual`

```csharp
public const Electron2D.VisualShaderNodeCompare.FunctionEnum LessThanEqual = 5
```

Tests whether the first value is at most the second.

<a id="member-a2a921cdfc2b"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeCompare.FunctionEnum Max = 6
```

The nonselectable function-count sentinel.

<a id="member-0b4a7962cbd8"></a>
### `NotEqual`

```csharp
public const Electron2D.VisualShaderNodeCompare.FunctionEnum NotEqual = 1
```

Negates scalar tolerance equality or tests exact component inequality.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; excluded roles do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed requests, dormant tolerance, condition presence, transactional operand conversion, copy/archives and real software-rendered pixels. See [the component contract](../components/visual-shaders.md#comparisons-and-conditional-results) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
