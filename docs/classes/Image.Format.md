# Image.Format

Last updated: 2026-09-21

**Inherits:** `System.Enum`

**Inherited By:** none

- **Source:** [`src/Core/IO/Image.cs`](../../src/Core/IO/Image.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Image.Format`

> Identifies the exact byte layout of [`Image`](Image.md) storage.

## Description

Values `0..46` are stable raw-format identities. `Max` is a sentinel and is rejected as image storage. Uncompressed formats support CPU pixel access; compressed formats support validated opaque storage only. Multi-byte components use little-endian order. Missing RGB channels decode as zero and missing alpha decodes as one.

Normalized formats clamp writes to `0..1`. Floating formats retain finite and IEEE component behavior allowed by their representation. Unsigned integer formats map each `Color` component to the numeric `UInt16` range rather than normalizing it.

## Examples

```csharp
using var image = Image.CreateEmpty(32, 32, false, Image.Format.Rgba8);
if (!image.IsCompressed)
    image.SetPixel(0, 0, Colors.White);
```

## Enumeration values

| Member | Value | Storage |
| --- | ---: | --- |
| [`L8`](#l8) | 0 | 1 byte/pixel |
| [`La8`](#la8) | 1 | 2 bytes/pixel |
| [`R8`](#r8) | 2 | 1 byte/pixel |
| [`Rg8`](#rg8) | 3 | 2 bytes/pixel |
| [`Rgb8`](#rgb8) | 4 | 3 bytes/pixel |
| [`Rgba8`](#rgba8) | 5 | 4 bytes/pixel |
| [`Rgba4444`](#rgba4444) | 6 | 2 bytes/pixel |
| [`Rgb565`](#rgb565) | 7 | 2 bytes/pixel |
| [`Rf`](#rf) | 8 | 4 bytes/pixel |
| [`Rgf`](#rgf) | 9 | 8 bytes/pixel |
| [`Rgbf`](#rgbf) | 10 | 12 bytes/pixel |
| [`Rgbaf`](#rgbaf) | 11 | 16 bytes/pixel |
| [`Rh`](#rh) | 12 | 2 bytes/pixel |
| [`Rgh`](#rgh) | 13 | 4 bytes/pixel |
| [`Rgbh`](#rgbh) | 14 | 6 bytes/pixel |
| [`Rgbah`](#rgbah) | 15 | 8 bytes/pixel |
| [`Rgbe9995`](#rgbe9995) | 16 | 4 bytes/pixel |
| [`Dxt1`](#dxt1) | 17 | 8 bytes/4×4 block |
| [`Dxt3`](#dxt3) | 18 | 16 bytes/4×4 block |
| [`Dxt5`](#dxt5) | 19 | 16 bytes/4×4 block |
| [`RgtcR`](#rgtcr) | 20 | 8 bytes/4×4 block |
| [`RgtcRg`](#rgtcrg) | 21 | 16 bytes/4×4 block |
| [`BptcRgba`](#bptcrgba) | 22 | 16 bytes/4×4 block |
| [`BptcRgbf`](#bptcrgbf) | 23 | 16 bytes/4×4 block |
| [`BptcRgbfu`](#bptcrgbfu) | 24 | 16 bytes/4×4 block |
| [`Etc`](#etc) | 25 | 8 bytes/4×4 block |
| [`Etc2R11`](#etc2r11) | 26 | 8 bytes/4×4 block |
| [`Etc2R11S`](#etc2r11s) | 27 | 8 bytes/4×4 block |
| [`Etc2Rg11`](#etc2rg11) | 28 | 16 bytes/4×4 block |
| [`Etc2Rg11S`](#etc2rg11s) | 29 | 16 bytes/4×4 block |
| [`Etc2Rgb8`](#etc2rgb8) | 30 | 8 bytes/4×4 block |
| [`Etc2Rgba8`](#etc2rgba8) | 31 | 16 bytes/4×4 block |
| [`Etc2Rgb8A1`](#etc2rgb8a1) | 32 | 8 bytes/4×4 block |
| [`Etc2RaAsRg`](#etc2raasrg) | 33 | 16 bytes/4×4 block |
| [`Dxt5RaAsRg`](#dxt5raasrg) | 34 | 16 bytes/4×4 block |
| [`Astc4X4`](#astc4x4) | 35 | 16 bytes/4×4 block |
| [`Astc4X4Hdr`](#astc4x4hdr) | 36 | 16 bytes/4×4 block |
| [`Astc8X8`](#astc8x8) | 37 | 16 bytes/8×8 block |
| [`Astc8X8Hdr`](#astc8x8hdr) | 38 | 16 bytes/8×8 block |
| [`R16`](#r16) | 39 | 2 bytes/pixel |
| [`Rg16`](#rg16) | 40 | 4 bytes/pixel |
| [`Rgb16`](#rgb16) | 41 | 6 bytes/pixel |
| [`Rgba16`](#rgba16) | 42 | 8 bytes/pixel |
| [`R16I`](#r16i) | 43 | 2 bytes/pixel |
| [`Rg16I`](#rg16i) | 44 | 4 bytes/pixel |
| [`Rgb16I`](#rgb16i) | 45 | 6 bytes/pixel |
| [`Rgba16I`](#rgba16i) | 46 | 8 bytes/pixel |
| [`Max`](#max) | 47 | Sentinel; invalid storage format |

## Enumeration Descriptions

<a id="l8"></a>
### `L8 = 0`

One normalized 8-bit luminance value. It decodes into equal RGB components with alpha one.

<a id="la8"></a>
### `La8 = 1`

Normalized 8-bit luminance followed by 8-bit alpha. Luminance decodes into equal RGB components.

<a id="r8"></a>
### `R8 = 2`

One normalized 8-bit red component; green and blue decode as zero.

<a id="rg8"></a>
### `Rg8 = 3`

Normalized 8-bit red and green components.

<a id="rgb8"></a>
### `Rgb8 = 4`

Normalized 8-bit red, green, and blue components.

<a id="rgba8"></a>
### `Rgba8 = 5`

Normalized 8-bit red, green, blue, and alpha components. Alpha-repair and premultiplication operate directly on this format.

<a id="rgba4444"></a>
### `Rgba4444 = 6`

One little-endian 16-bit word containing four normalized 4-bit RGBA components.

<a id="rgb565"></a>
### `Rgb565 = 7`

One little-endian 16-bit word containing normalized 5-bit red, 6-bit green, and 5-bit blue.

<a id="rf"></a>
### `Rf = 8`

One 32-bit IEEE floating-point red component.

<a id="rgf"></a>
### `Rgf = 9`

Two 32-bit IEEE floating-point red and green components.

<a id="rgbf"></a>
### `Rgbf = 10`

Three 32-bit IEEE floating-point RGB components.

<a id="rgbaf"></a>
### `Rgbaf = 11`

Four 32-bit IEEE floating-point RGBA components.

<a id="rh"></a>
### `Rh = 12`

One 16-bit IEEE half-precision red component.

<a id="rgh"></a>
### `Rgh = 13`

Two 16-bit IEEE half-precision red and green components.

<a id="rgbh"></a>
### `Rgbh = 14`

Three 16-bit IEEE half-precision RGB components.

<a id="rgbah"></a>
### `Rgbah = 15`

Four 16-bit IEEE half-precision RGBA components.

<a id="rgbe9995"></a>
### `Rgbe9995 = 16`

Positive HDR RGB stored as three 9-bit mantissas and one shared 5-bit exponent in a little-endian 32-bit word. Alpha decodes as one.

<a id="dxt1"></a>
### `Dxt1 = 17`

BC1/DXT1 RGB block storage. CPU decoding and encoding are not implemented.

<a id="dxt3"></a>
### `Dxt3 = 18`

BC2/DXT3 RGBA block storage. CPU decoding and encoding are not implemented.

<a id="dxt5"></a>
### `Dxt5 = 19`

BC3/DXT5 RGBA block storage. CPU decoding and encoding are not implemented.

<a id="rgtcr"></a>
### `RgtcR = 20`

BC4/RGTC single-channel block storage.

<a id="rgtcrg"></a>
### `RgtcRg = 21`

BC5/RGTC two-channel block storage.

<a id="bptcrgba"></a>
### `BptcRgba = 22`

BC7/BPTC normalized RGBA block storage.

<a id="bptcrgbf"></a>
### `BptcRgbf = 23`

Signed BC6H/BPTC floating-point RGB block storage.

<a id="bptcrgbfu"></a>
### `BptcRgbfu = 24`

Unsigned BC6H/BPTC floating-point RGB block storage.

<a id="etc"></a>
### `Etc = 25`

ETC1 RGB block storage.

<a id="etc2r11"></a>
### `Etc2R11 = 26`

Unsigned ETC2/EAC single-channel block storage.

<a id="etc2r11s"></a>
### `Etc2R11S = 27`

Signed ETC2/EAC single-channel block storage.

<a id="etc2rg11"></a>
### `Etc2Rg11 = 28`

Unsigned ETC2/EAC two-channel block storage.

<a id="etc2rg11s"></a>
### `Etc2Rg11S = 29`

Signed ETC2/EAC two-channel block storage.

<a id="etc2rgb8"></a>
### `Etc2Rgb8 = 30`

ETC2 RGB block storage.

<a id="etc2rgba8"></a>
### `Etc2Rgba8 = 31`

ETC2 RGBA block storage.

<a id="etc2rgb8a1"></a>
### `Etc2Rgb8A1 = 32`

ETC2 RGB block storage with one-bit alpha.

<a id="etc2raasrg"></a>
### `Etc2RaAsRg = 33`

ETC2 RGBA storage interpreted by future consumers as red and green channels.

<a id="dxt5raasrg"></a>
### `Dxt5RaAsRg = 34`

BC3/DXT5 RGBA storage interpreted by future consumers as red and green channels.

<a id="astc4x4"></a>
### `Astc4X4 = 35`

Normalized ASTC storage using 4×4 pixel blocks.

<a id="astc4x4hdr"></a>
### `Astc4X4Hdr = 36`

HDR ASTC storage using 4×4 pixel blocks.

<a id="astc8x8"></a>
### `Astc8X8 = 37`

Normalized ASTC storage using 8×8 pixel blocks.

<a id="astc8x8hdr"></a>
### `Astc8X8Hdr = 38`

HDR ASTC storage using 8×8 pixel blocks.

<a id="r16"></a>
### `R16 = 39`

One normalized unsigned 16-bit red component.

<a id="rg16"></a>
### `Rg16 = 40`

Normalized unsigned 16-bit red and green components.

<a id="rgb16"></a>
### `Rgb16 = 41`

Normalized unsigned 16-bit RGB components.

<a id="rgba16"></a>
### `Rgba16 = 42`

Normalized unsigned 16-bit RGBA components.

<a id="r16i"></a>
### `R16I = 43`

One unsigned 16-bit integer red component represented directly as a numeric `Color` component.

<a id="rg16i"></a>
### `Rg16I = 44`

Unsigned 16-bit integer red and green components represented directly as numeric `Color` components.

<a id="rgb16i"></a>
### `Rgb16I = 45`

Unsigned 16-bit integer RGB components represented directly as numeric `Color` components.

<a id="rgba16i"></a>
### `Rgba16I = 46`

Unsigned 16-bit integer RGBA components represented directly as numeric `Color` components.

<a id="max"></a>
### `Max = 47`

Counts defined storage identities and is not a valid image format.

## Invariants, threading, and errors

The enum is immutable and thread-safe. [`Image`](Image.md) rejects undefined values and `Max` with `ArgumentOutOfRangeException`. Block storage rounds each mip dimension up to a complete block; CPU pixel methods reject every block-compressed value with `InvalidOperationException`.

## Dependencies and interactions

`Image.Format` is consumed by `Image` factories, state inspection, raw-data methods, conversion, and processing. No renderer capability is implied by accepting a raw compressed layout.

## Verification and known limitations

The executable checks cover numeric identities, byte counts and pixel round trips for all 25 uncompressed formats, structural size and access rejection for all 22 compressed formats, and mip sizing across compressed block boundaries. CPU block codecs remain deferred under [ADR 0039](../decisions/resources.md#adr-0039).
