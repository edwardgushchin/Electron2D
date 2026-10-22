# VisualShaderNodeColorOp.OperatorEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeColorOp.cs](../../src/Scene/Resources/VisualShaderNodeColorOp.cs).

```csharp
public enum Electron2D.VisualShaderNodeColorOp.OperatorEnum
```

## Description

The component-wise color operation.

## Member summary

| Member | Contract |
| --- | --- |
| [`public enum Electron2D.VisualShaderNodeColorOp.OperatorEnum`](#member-b73282771d08) | The component-wise color operation. |
| [`public const Electron2D.VisualShaderNodeColorOp.OperatorEnum Burn = 6`](#member-455ae8bd71c6) | Subtracts from one the complement of the first input divided by the second. |
| [`public const Electron2D.VisualShaderNodeColorOp.OperatorEnum Darken = 2`](#member-57d640b446e5) | Selects the smaller components. |
| [`public const Electron2D.VisualShaderNodeColorOp.OperatorEnum Difference = 1`](#member-9ab2776954c7) | Computes absolute component differences. |
| [`public const Electron2D.VisualShaderNodeColorOp.OperatorEnum Dodge = 5`](#member-65cbb6015c26) | Divides the first input by one minus the second. |
| [`public const Electron2D.VisualShaderNodeColorOp.OperatorEnum HardLight = 8`](#member-100047e5703f) | Applies the doubled piecewise blend selected by the first input. |
| [`public const Electron2D.VisualShaderNodeColorOp.OperatorEnum Lighten = 3`](#member-135bed554da1) | Selects the larger components. |
| [`public const Electron2D.VisualShaderNodeColorOp.OperatorEnum Max = 9`](#member-46f801e12813) | The nonselectable operator count. |
| [`public const Electron2D.VisualShaderNodeColorOp.OperatorEnum Overlay = 4`](#member-239a86c75fcc) | Uses doubled product below 0.5 in the first input and complementary product otherwise. |
| [`public const Electron2D.VisualShaderNodeColorOp.OperatorEnum Screen = 0`](#member-0d8fd665a29b) | Computes one minus the product of the complementary inputs. |
| [`public const Electron2D.VisualShaderNodeColorOp.OperatorEnum SoftLight = 7`](#member-87d1f66432ae) | Applies the half-offset piecewise blend selected by the first input. |

<a id="member-b73282771d08"></a>
### `OperatorEnum`

```csharp
public enum Electron2D.VisualShaderNodeColorOp.OperatorEnum
```

The component-wise color operation.

<a id="member-455ae8bd71c6"></a>
### `Burn`

```csharp
public const Electron2D.VisualShaderNodeColorOp.OperatorEnum Burn = 6
```

Subtracts from one the complement of the first input divided by the second.

<a id="member-57d640b446e5"></a>
### `Darken`

```csharp
public const Electron2D.VisualShaderNodeColorOp.OperatorEnum Darken = 2
```

Selects the smaller components.

<a id="member-9ab2776954c7"></a>
### `Difference`

```csharp
public const Electron2D.VisualShaderNodeColorOp.OperatorEnum Difference = 1
```

Computes absolute component differences.

<a id="member-65cbb6015c26"></a>
### `Dodge`

```csharp
public const Electron2D.VisualShaderNodeColorOp.OperatorEnum Dodge = 5
```

Divides the first input by one minus the second.

<a id="member-100047e5703f"></a>
### `HardLight`

```csharp
public const Electron2D.VisualShaderNodeColorOp.OperatorEnum HardLight = 8
```

Applies the doubled piecewise blend selected by the first input.

<a id="member-135bed554da1"></a>
### `Lighten`

```csharp
public const Electron2D.VisualShaderNodeColorOp.OperatorEnum Lighten = 3
```

Selects the larger components.

<a id="member-46f801e12813"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeColorOp.OperatorEnum Max = 9
```

The nonselectable operator count.

<a id="member-239a86c75fcc"></a>
### `Overlay`

```csharp
public const Electron2D.VisualShaderNodeColorOp.OperatorEnum Overlay = 4
```

Uses doubled product below 0.5 in the first input and complementary product otherwise.

<a id="member-0d8fd665a29b"></a>
### `Screen`

```csharp
public const Electron2D.VisualShaderNodeColorOp.OperatorEnum Screen = 0
```

Computes one minus the product of the complementary inputs.

<a id="member-87d1f66432ae"></a>
### `SoftLight`

```csharp
public const Electron2D.VisualShaderNodeColorOp.OperatorEnum SoftLight = 7
```

Applies the half-offset piecewise blend selected by the first input.

## Lifecycle and verification

This owner-specific selector has value lifetime. Max is a nonselectable bound; undefined casts are rejected by its owning property.

The public VisualShader.Consumer exercises both canvas stages, selector transitions, errors, source/compiled archives and composed image-effect materials. See [the operation contract](../components/visual-shaders.md#uv-and-color-image-effects) for formulas, native arithmetic and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
