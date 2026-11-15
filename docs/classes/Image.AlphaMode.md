# Image.AlphaMode

Last updated: 2026-09-21

**Inherits:** `System.Enum`

**Inherited By:** none

- **Source:** [`src/Core/IO/Image.cs`](../../src/Core/IO/Image.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Image.AlphaMode`

> Classifies the alpha values found by [`Image.DetectAlpha`](Image.md#detectalpha).

## Description

The classification is derived from decoded base-level pixels. LA8/RGBA8 use byte thresholds 2 and 254; other decoded formats use exact endpoints, including 65535 for RGBA16I. It describes actual values, not merely whether the storage format declares an alpha channel.

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

At least one base-level pixel is classified transparent and none is classified blended. Byte alpha 0–1 is transparent; other decoded formats use zero as the endpoint.

<a id="blend"></a>
### `Blend = 2`

At least one base-level alpha is blended: bytes 2–253, or a fractional value between the exact endpoints of another decoded format.

## Invariants and errors

The enum is immutable and thread-safe. Empty images report `None`. DXT3 and DXT5 report `Blend` from format metadata; other compressed formats report `None` without decoding pixels.

## Verification and limitations

Tests cover opaque, one-bit, fractional, and no-alpha formats. Only the base level participates. See [Image](Image.md).
