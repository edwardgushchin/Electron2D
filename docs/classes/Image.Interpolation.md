# Image.Interpolation

Last updated: 2026-09-21

**Inherits:** `System.Enum`

**Inherited By:** none

- **Source:** [`src/Core/IO/Image.cs`](../../src/Core/IO/Image.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Image.Interpolation`

> Selects the reconstruction filter used by [`Image.Resize`](Image.md#resize) and [`Image.ResizeToPowerOfTwo`](Image.md#resizetopot).

## Description

Interpolation changes sampling quality and cost, not ownership or destination dimensions. Every mode samples in straight RGBA component space and applies the destination format's normal encoding and quantization. Resizing block-compressed images is unsupported.

## Examples

Partial snippet: `image` is an existing nonempty, uncompressed `Image` supplied by the caller.

```csharp
image.Resize(320, 180, Image.Interpolation.Lanczos);
```

## Enumeration values

| Member | Value | Description |
| --- | ---: | --- |
| [`Nearest`](#nearest) | 0 | Nearest source pixel. |
| [`Bilinear`](#bilinear) | 1 | Four-sample linear interpolation. |
| [`Cubic`](#cubic) | 2 | Sixteen-sample bicubic reconstruction. |
| [`Trilinear`](#trilinear) | 3 | Blend between bilinear samples at two scales. |
| [`Lanczos`](#lanczos) | 4 | Radius-three windowed sinc reconstruction. |

## Enumeration Descriptions

<a id="nearest"></a>
### `Nearest = 0`

Copies the nearest source pixel's stored bytes and preserves hard pixel-art edges.

<a id="bilinear"></a>
### `Bilinear = 1`

Blends the nearest four source pixels. This is the default resize mode.

<a id="cubic"></a>
### `Cubic = 2`

Uses a Catmull–Rom-style bicubic reconstruction over sixteen neighboring samples, clamping sample coordinates at image edges.

<a id="trilinear"></a>
### `Trilinear = 3`

When the mean of the two destination/source scale ratios is below one, blends bilinear samples from adjacent mip levels. Reuses stored levels or generates a temporary chain. At a mean scale of one or higher it samples the base level.

<a id="lanczos"></a>
### `Lanczos = 4`

Uses a separable radius-three Lanczos filter. Its sampling window widens along each reduced axis and normalizes the in-bounds weights at the edges.

## Invariants and errors

The enum is immutable and thread-safe. Undefined values are rejected with `ArgumentOutOfRangeException` before an image changes.

## Verification and limitations

The executable checks exercise all five modes, dimensions, and stable representative output. Filtering is CPU-based, synchronous, and not color-profile aware. See [Image](Image.md) and [ADR 0039](../decisions/resources.md#adr-0039).
