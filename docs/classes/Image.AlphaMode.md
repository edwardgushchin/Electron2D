# Image.AlphaMode

Last updated: 2026-09-21

**Inherits:** `System.Enum`

**Inherited By:** none

- **Source:** [`src/Core/IO/Image.cs`](../../src/Core/IO/Image.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Image.AlphaMode`

> Classifies the alpha values found by [`Image.DetectAlpha`](Image.md#detectalpha).

## Description

The classification is derived from decoded base-level pixels. It describes actual values, not merely whether the storage format declares an alpha channel.

## Examples

Partial snippet: `image` is an existing nonempty `Image` supplied by the caller.

```csharp
Image.AlphaMode mode = image.DetectAlpha();
bool needsBlending = mode == Image.AlphaMode.Blend;
```

## Enumeration values

| Member | Value | Description |
| --- | ---: | --- |
| [`None`](#none) | 0 | No transparent pixel. |
| [`Bit`](#bit) | 1 | Alpha contains transparency but only endpoint values. |
| [`Blend`](#blend) | 2 | At least one fractional alpha value. |

## Enumeration Descriptions

<a id="none"></a>
### `None = 0`

Every decoded base-level pixel is fully opaque, or the format has no alpha channel.

<a id="bit"></a>
### `Bit = 1`

At least one base-level pixel is fully transparent and every other alpha is either zero or one.

<a id="blend"></a>
### `Blend = 2`

At least one base-level alpha lies strictly between zero and one, so ordinary alpha blending may be required.

## Invariants and errors

The enum is immutable and thread-safe. Empty images report `None`. DXT3 and DXT5 report `Blend` from format metadata; other compressed formats report `None` without decoding pixels.

## Verification and limitations

Tests cover opaque, one-bit, fractional, and no-alpha formats. Only the base level participates. See [Image](Image.md).
