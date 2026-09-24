# BlendMode

Last updated: 2026-09-24

- Declaration: `public enum BlendMode`
- Source: [CanvasItemMaterial.cs](../../src/Scene/Resources/CanvasItemMaterial.cs)
- Inherits: `System.Enum`
- Used by: [CanvasItemMaterial](CanvasItemMaterial.md)

## Description

Fixed canvas blend equations. `S` is source, `D` is destination and `A` is the source alpha. The equations apply to each RGB component; alpha factors are listed separately. Results are clamped to the target format. PremultAlpha expects the source RGB to be premultiplied by its alpha before drawing.

## Example

```csharp
material.BlendMode = BlendMode.Mul;
```

## Values

| Value | ID | RGB | Alpha |
| --- | ---: | --- | --- |
| `Mix` | 0 | `S × A + D × (1 − A)` | `Sα + Dα × (1 − A)` |
| `Add` | 1 | `S × A + D` | `Sα × A + Dα` |
| `Sub` | 2 | `D − S × A` | `Dα − Sα × A` |
| `Mul` | 3 | `S × D` | `Sα × Dα` |
| `PremultAlpha` | 4 | `S + D × (1 − A)` | `Sα + Dα × (1 − A)` |

## Value descriptions

`Mix` is the default. `Add` brightens, `Sub` subtracts source contribution, and `Mul` multiplies the destination. `PremultAlpha` uses already premultiplied source RGB. All values are checked by GPU and compatibility hardware readback. SDL software supports only Mix for retained canvas geometry and explicitly rejects the other values; see [the material contract](CanvasItemMaterial.md#verification-and-limits).
