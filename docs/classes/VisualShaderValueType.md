# VisualShaderValueType

Last updated: 2026-10-22

Source: [VisualShaderValueType.cs](../../src/Scene/Resources/VisualShaderValueType.cs).

```csharp
public enum Electron2D.VisualShaderValueType
```

## Description

The numeric or Boolean value selected or compared by a graph operation.

This selection domain excludes sampled images. PortType remains the broader logical port table.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderValueType Boolean = 6`](#member-69643643a65b) | A Boolean scalar. |
| [`public const Electron2D.VisualShaderValueType Float = 0`](#member-1e364425eb7f) | A floating scalar. |
| [`public const Electron2D.VisualShaderValueType Int = 1`](#member-295a8d4cae8c) | A signed integer scalar. |
| [`public const Electron2D.VisualShaderValueType Matrix = 7`](#member-0985b569113c) | A four-by-four numeric matrix. |
| [`public const Electron2D.VisualShaderValueType Max = 8`](#member-3dd983a982d8) | The nonselectable upper bound of the logical selection table. |
| [`public const Electron2D.VisualShaderValueType UInt = 2`](#member-12a1c6bbd064) | An unsigned integer scalar. |
| [`public const Electron2D.VisualShaderValueType Vector2 = 3`](#member-2cdd684920ad) | Two floating components. |
| [`public const Electron2D.VisualShaderValueType Vector3 = 4`](#member-d47bdbd178ba) | Three floating components. |
| [`public const Electron2D.VisualShaderValueType Vector4 = 5`](#member-b3ebcbcd9e74) | Four floating components. |

<a id="member-69643643a65b"></a>
### `Boolean`

```csharp
public const Electron2D.VisualShaderValueType Boolean = 6
```

A Boolean scalar.

<a id="member-1e364425eb7f"></a>
### `Float`

```csharp
public const Electron2D.VisualShaderValueType Float = 0
```

A floating scalar.

<a id="member-295a8d4cae8c"></a>
### `Int`

```csharp
public const Electron2D.VisualShaderValueType Int = 1
```

A signed integer scalar.

<a id="member-0985b569113c"></a>
### `Matrix`

```csharp
public const Electron2D.VisualShaderValueType Matrix = 7
```

A four-by-four numeric matrix.

<a id="member-3dd983a982d8"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderValueType Max = 8
```

The nonselectable upper bound of the logical selection table.

<a id="member-12a1c6bbd064"></a>
### `UInt`

```csharp
public const Electron2D.VisualShaderValueType UInt = 2
```

An unsigned integer scalar.

<a id="member-2cdd684920ad"></a>
### `Vector2`

```csharp
public const Electron2D.VisualShaderValueType Vector2 = 3
```

Two floating components.

<a id="member-d47bdbd178ba"></a>
### `Vector3`

```csharp
public const Electron2D.VisualShaderValueType Vector3 = 4
```

Three floating components.

<a id="member-b3ebcbcd9e74"></a>
### `Vector4`

```csharp
public const Electron2D.VisualShaderValueType Vector4 = 5
```

Four floating components.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed requests, all sixteen matrix components, copy/archive state, row/column layout and real software-rendered pixels. See [the component contract](../components/visual-shaders.md#comparisons-and-conditional-results) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
