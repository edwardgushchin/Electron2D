# VisualShaderNodeColorFunc.FunctionEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeColorFunc.cs](../../src/Scene/Resources/VisualShaderNodeColorFunc.cs).

```csharp
public enum Electron2D.VisualShaderNodeColorFunc.FunctionEnum
```

## Description

The numeric color transform.

## Member summary

| Member | Contract |
| --- | --- |
| [`public enum Electron2D.VisualShaderNodeColorFunc.FunctionEnum`](#member-dfce5b329ef8) | The numeric color transform. |
| [`public const Electron2D.VisualShaderNodeColorFunc.FunctionEnum Grayscale = 0`](#member-03bda46d5117) | Replicates the maximum RGB component. |
| [`public const Electron2D.VisualShaderNodeColorFunc.FunctionEnum HSVToRGB = 1`](#member-8165961b2d34) | Converts hue, saturation and value to RGB. |
| [`public const Electron2D.VisualShaderNodeColorFunc.FunctionEnum LinearToSRGB = 4`](#member-5d1fc18bb8da) | Converts linear RGB to sRGB using the piecewise transfer function. |
| [`public const Electron2D.VisualShaderNodeColorFunc.FunctionEnum Max = 6`](#member-14d0b810f29a) | The nonselectable function count. |
| [`public const Electron2D.VisualShaderNodeColorFunc.FunctionEnum RGBToHSV = 2`](#member-e0a0b5eb242b) | Converts RGB to hue, saturation and value with a 1e-10 denominator stabilizer. |
| [`public const Electron2D.VisualShaderNodeColorFunc.FunctionEnum SRGBToLinear = 5`](#member-a0906b5bb0a3) | Converts sRGB to linear RGB using the piecewise transfer function. |
| [`public const Electron2D.VisualShaderNodeColorFunc.FunctionEnum Sepia = 3`](#member-2b2910f00af5) | Applies the three sepia coefficient rows without clamping. |

<a id="member-dfce5b329ef8"></a>
### `FunctionEnum`

```csharp
public enum Electron2D.VisualShaderNodeColorFunc.FunctionEnum
```

The numeric color transform.

<a id="member-03bda46d5117"></a>
### `Grayscale`

```csharp
public const Electron2D.VisualShaderNodeColorFunc.FunctionEnum Grayscale = 0
```

Replicates the maximum RGB component.

<a id="member-8165961b2d34"></a>
### `HSVToRGB`

```csharp
public const Electron2D.VisualShaderNodeColorFunc.FunctionEnum HSVToRGB = 1
```

Converts hue, saturation and value to RGB.

<a id="member-5d1fc18bb8da"></a>
### `LinearToSRGB`

```csharp
public const Electron2D.VisualShaderNodeColorFunc.FunctionEnum LinearToSRGB = 4
```

Converts linear RGB to sRGB using the piecewise transfer function.

<a id="member-14d0b810f29a"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeColorFunc.FunctionEnum Max = 6
```

The nonselectable function count.

<a id="member-e0a0b5eb242b"></a>
### `RGBToHSV`

```csharp
public const Electron2D.VisualShaderNodeColorFunc.FunctionEnum RGBToHSV = 2
```

Converts RGB to hue, saturation and value with a 1e-10 denominator stabilizer.

<a id="member-a0906b5bb0a3"></a>
### `SRGBToLinear`

```csharp
public const Electron2D.VisualShaderNodeColorFunc.FunctionEnum SRGBToLinear = 5
```

Converts sRGB to linear RGB using the piecewise transfer function.

<a id="member-2b2910f00af5"></a>
### `Sepia`

```csharp
public const Electron2D.VisualShaderNodeColorFunc.FunctionEnum Sepia = 3
```

Applies the three sepia coefficient rows without clamping.

## Lifecycle and verification

This owner-specific selector has value lifetime. Max is a nonselectable bound; undefined casts are rejected by its owning property.

The public VisualShader.Consumer exercises both canvas stages, selector transitions, errors, source/compiled archives and composed image-effect materials. See [the operation contract](../components/visual-shaders.md#uv-and-color-image-effects) for formulas, native arithmetic and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
