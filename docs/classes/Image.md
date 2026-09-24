# Image

Last updated: 2026-09-23

**Inherits:** [Resource](Resource.md) → [ElectronObject](ElectronObject.md)

**Inherited By:** none

- **Sources:** [`src/Core/IO/Image.cs`](../../src/Core/IO/Image.cs), [`src/Core/IO/Image.Processing.cs`](../../src/Core/IO/Image.Processing.cs), [`src/Core/IO/Image.Codecs.cs`](../../src/Core/IO/Image.Codecs.cs), [`src/Core/IO/Image.SVG.cs`](../../src/Core/IO/Image.SVG.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed partial class Image : Resource`

> Owns portable CPU-side 2D pixel data and provides deterministic in-memory image processing.

## Description

`Image` stores one positive-sized base level and, optionally, its complete mip chain in one managed byte buffer. A newly constructed instance is the canonical empty state: zero width, zero height, `L8`, no mipmaps, and no data. Static factories validate dimensions, format, pixel count, and exact byte length before publishing an image.

All 47 raw [`Image.Format`](Image.Format.md) identities are accepted for storage. Twenty-five uncompressed formats support pixel access and processing. The 22 block-compressed formats support exact buffer ownership, sizing, copying, mip offsets, duplication, and metadata-only alpha inspection, but not CPU pixel decoding or encoding.

Pixel coordinates use a top-left origin: X increases rightward and Y increases downward. Rectangles are half-open. Stored multi-byte components use little-endian order. `GetData` and data-taking APIs copy their arrays, so callers never share mutable buffer ownership with an image.

Every state read and mutation is synchronized per instance. Bulk mutations prepare and commit a complete replacement state; `SetPixel` updates only the addressed pixel under serialization. Both raise inherited [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) after releasing locks. A throwing subscriber observes committed state and propagates its exception. Calls involving multiple images snapshot sources before changing the destination; no cross-image lock is held during callbacks.

## Examples

```csharp
using var image = Image.CreateEmpty(64, 32, useMipmaps: false, Image.Format.Rgba8);
image.Fill(Colors.Transparent);
image.FillRect(new RectI(8, 8, 16, 16), Colors.White);
image.GenerateMipmaps();

Color center = image.GetPixel(16, 16);
byte[] ownedCopy = image.GetData();
```

## Constructors

| Member | Description |
| --- | --- |
| [`public Image()`](#ctor) | Creates the canonical empty image. |

## Properties

| Member | Description |
| --- | --- |
| [`public int Width { get; }`](#width) | Base-level width in pixels. |
| [`public int Height { get; }`](#height) | Base-level height in pixels. |
| [`public Vector2i Size { get; }`](#size) | Base-level dimensions. |
| [`public Image.Format PixelFormat { get; }`](#pixelformat) | Raw buffer layout. |
| [`public bool HasMipmaps { get; }`](#hasmipmaps) | Whether a complete lower-resolution chain follows the base level. |
| [`public int MipmapCount { get; }`](#mipmapcount) | Number of stored levels excluding the base. |
| [`public int DataSize { get; }`](#datasize) | Complete raw-buffer length in bytes. |
| [`public bool IsCompressed { get; }`](#iscompressed) | Whether the format uses GPU block compression. |
| [`public bool IsEmpty { get; }`](#isempty) | Whether the image is in the canonical empty state. |
| [`public bool IsInvisible { get; }`](#isinvisible) | Whether every base-level alpha value is zero. |

## Methods

| Member | Description |
| --- | --- |
| [`public static Image CreateEmpty(int width, int height, bool useMipmaps, Image.Format format)`](#createempty) | Creates zero-filled storage. |
| [`public static Image Create(int width, int height, bool useMipmaps, Image.Format format)`](#create) | Obsolete alias of `CreateEmpty`. |
| [`public static Image CreateFromData(int width, int height, bool useMipmaps, Image.Format format, byte[] data)`](#createfromdata) | Creates an image from an exact copied raw buffer. |
| [`public void SetData(int width, int height, bool useMipmaps, Image.Format format, byte[] data)`](#setdata) | Atomically replaces all pixel state. |
| [`public byte[] GetData()`](#getdata) | Returns an independent raw-buffer copy. |
| [`public int GetMipmapOffset(int mipmap)`](#getmipmapoffset) | Gets a stored level's byte offset. |
| [`public void CopyFrom(Image source)`](#copyfrom) | Copies pixel state while preserving resource identity. |
| [`public Color GetPixel(int x, int y)`](#getpixel-xy) | Decodes one base-level pixel. |
| [`public Color GetPixel(Vector2i point)`](#getpixel-point) | Decodes one base-level pixel. |
| [`public void SetPixel(int x, int y, Color color)`](#setpixel-xy) | Encodes one base-level pixel. |
| [`public void SetPixel(Vector2i point, Color color)`](#setpixel-point) | Encodes one base-level pixel. |
| [`public Image.AlphaMode DetectAlpha()`](#detectalpha) | Classifies base-level alpha use. |
| [`public Image.UsedChannels DetectUsedChannels(Image.CompressSource source = Image.CompressSource.Generic)`](#detectusedchannels) | Detects the smallest meaningful channel set. |
| [`public RectI GetUsedRect()`](#getusedrect) | Finds nontransparent base-level bounds. |
| [`public void Fill(Color color)`](#fill) | Fills all stored levels. |
| [`public void FillRect(RectI rectangle, Color color)`](#fillrect) | Fills a clipped base-level rectangle. |
| [`public void ClearMipmaps()`](#clearmipmaps) | Removes lower-resolution levels. |
| [`public void GenerateMipmaps(bool renormalize = false)`](#generatemipmaps) | Rebuilds a complete mip chain. |
| [`public void Convert(Image.Format format)`](#convert) | Converts all stored levels to an uncompressed format. |
| [`public void Crop(int width, int height)`](#crop) | Crops or expands from the top-left corner. |
| [`public Image GetRegion(RectI region)`](#getregion) | Copies a clipped base-level region. |
| [`public void FlipX()`](#flipx) | Flips all levels horizontally. |
| [`public void FlipY()`](#flipy) | Flips all levels vertically. |
| [`public void Rotate90(ClockDirection direction)`](#rotate90) | Rotates by 90 degrees. |
| [`public void Rotate180()`](#rotate180) | Rotates by 180 degrees. |
| [`public void ShrinkX2()`](#shrinkx2) | Halves dimensions with bilinear filtering. |
| [`public void Resize(int width, int height, Image.Interpolation interpolation = Image.Interpolation.Bilinear)`](#resize) | Resamples the base level. |
| [`public void ResizeToPowerOfTwo(bool square = false, Image.Interpolation interpolation = Image.Interpolation.Bilinear)`](#resizetopot) | Resamples to power-of-two dimensions. |
| [`public void BlitRect(Image source, RectI sourceRect, Vector2i destination)`](#blitrect) | Copies a clipped source rectangle. |
| [`public void BlendRect(Image source, RectI sourceRect, Vector2i destination)`](#blendrect) | Alpha-composites a clipped source rectangle. |
| [`public void BlitRectMask(Image source, Image mask, RectI sourceRect, Vector2i destination)`](#blitrectmask) | Copies source pixels selected by mask alpha. |
| [`public void BlendRectMask(Image source, Image mask, RectI sourceRect, Vector2i destination)`](#blendrectmask) | Alpha-composites source pixels selected by mask alpha. |
| [`public void AdjustBCS(float brightness, float contrast, float saturation)`](#adjustbcs) | Adjusts brightness, contrast, and saturation. |
| [`public void FixAlphaEdges()`](#fixalphaedges) | Propagates nearby opaque RGB into low-alpha `Rgba8` pixels. |
| [`public void PremultiplyAlpha()`](#premultiplyalpha) | Multiplies `Rgba8` RGB bytes by alpha. |
| [`public void SRGBToLinear()`](#srgbtolinear) | Converts `Rgb8` or `Rgba8` RGB values to linear encoding. |
| [`public void LinearToSRGB()`](#lineartosrgb) | Converts `Rgb8` or `Rgba8` RGB values to nonlinear encoding. |
| [`public void BumpMapToNormalMap(float bumpScale = 1f)`](#bumpmaptonormalmap) | Converts height values to a wrapping tangent-space normal map. |
| [`public void NormalMapToXY()`](#normalmaptoxy) | Packs normal X/Y into `La8`. |
| [`public Image RGBEToSRGB()`](#rgbetosrgb) | Decodes `Rgbe9995` into a new `Rgb8` image. |
| [`public ImageMetrics ComputeImageMetrics(Image comparedImage, bool useLuma)`](#computeimagemetrics) | Computes absolute-error statistics over the common area. |
| [`public void Load(string path)`](#load) | Replaces pixels from a PNG, JPEG, WebP, BMP, TGA or SVG file. |
| [`public static Image LoadFromFile(string path)`](#loadfromfile) | Returns an independent caller-owned image decoded from a file. |
| [`public void LoadPNGFromBuffer(ReadOnlySpan<byte> buffer)`](#loadpngfrombuffer) | Replaces pixels from PNG bytes. |
| [`public void LoadJPGFromBuffer(ReadOnlySpan<byte> buffer)`](#loadjpgfrombuffer) | Replaces pixels from JPEG bytes. |
| [`public void LoadWebPFromBuffer(ReadOnlySpan<byte> buffer)`](#loadwebpfrombuffer) | Replaces pixels from WebP bytes. |
| [`public void LoadBMPFromBuffer(ReadOnlySpan<byte> buffer)`](#loadbmpfrombuffer) | Replaces pixels from BMP bytes. |
| [`public void LoadTGAFromBuffer(ReadOnlySpan<byte> buffer)`](#loadtgafrombuffer) | Replaces pixels from TGA bytes. |
| [`public void LoadSVGFromBuffer(ReadOnlySpan<byte> buffer, float scale = 1f)`](#loadsvgfrombuffer) | Rasterizes UTF-8 SVG bytes at the requested scale. |
| [`public void LoadSVGFromString(string svg, float scale = 1f)`](#loadsvgfromstring) | Rasterizes an SVG string at the requested scale. |
| [`public byte[] SavePNGToBuffer()`](#savepngtobuffer) | Encodes a stable base-level snapshot as PNG. |
| [`public byte[] SaveJPGToBuffer(float quality = 0.75f)`](#savejpgtobuffer) | Encodes the base level as lossy JPEG without alpha. |
| [`public void SavePNG(string path)`](#savepng) | Atomically replaces a file with encoded PNG bytes. |
| [`public void SaveJPG(string path, float quality = 0.75f)`](#savejpg) | Atomically replaces a file with JPEG bytes without alpha. |

## Enumerations

| Type | Description |
| --- | --- |
| [`Image.Format`](Image.Format.md) | Raw byte layouts, including uncompressed and block-compressed storage. |
| [`Image.Interpolation`](Image.Interpolation.md) | Reconstruction filters. |
| [`Image.AlphaMode`](Image.AlphaMode.md) | Detected alpha-use classes. |
| [`Image.UsedChannels`](Image.UsedChannels.md) | Minimal meaningful channel sets. |
| [`Image.CompressSource`](Image.CompressSource.md) | Source semantics used during channel detection and future compression. |
| [`Image.CompressMode`](Image.CompressMode.md) | Future block-compression family selection. |
| [`Image.AstcFormat`](Image.AstcFormat.md) | Future ASTC block-footprint selection. |

## Constants

| Member | Value | Description |
| --- | ---: | --- |
| [`public const int MaxWidth`](#maxwidth) | 16,777,216 | Maximum accepted width. |
| [`public const int MaxHeight`](#maxheight) | 16,777,216 | Maximum accepted height. |

## Protected extension points

`Image` is sealed. Its protected overrides exist only to integrate with inherited resource and object infrastructure.

| Member | Description |
| --- | --- |
| [`protected override Resource CreateDuplicateInstance()`](#createduplicateinstance) | Creates the duplicate target. |
| [`protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)`](#copycustomstateto) | Copies image state for resource duplication. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Adds read-only image descriptors. |
| [`protected override void Dispose(bool disposing)`](#dispose-bool) | Clears pixel state during deterministic teardown. |

## Inherited events and resource behavior

| Member | Description |
| --- | --- |
| [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) | Raised once after each successful image mutation commits; subscriber exceptions propagate after commit. |
| [`Resource.ResourceName`](Resource.md#p-electron2d-resource-resourcename) | Optional display name, independent of pixel state. |
| [`Resource.ResourcePath`](Resource.md#p-electron2d-resource-resourcepath) | Optional weak-cache identity, independent of pixel state. |
| [`Resource.Duplicate(bool)`](Resource.md#m-electron2d-resource-duplicate-system-boolean) | Produces an independent image buffer and copies base resource configuration. |
| [`ElectronObject.Dispose()`](ElectronObject.md#m-electron2d-electronobject-dispose) | Deterministically closes the image; later state access is rejected. |

## Property Descriptions

<a id="width"></a>
### `public int Width { get; }`

Returns zero for an empty image; otherwise returns the positive base-level width in pixels. Access after disposal throws `ObjectDisposedException`.

<a id="height"></a>
### `public int Height { get; }`

Returns zero for an empty image; otherwise returns the positive base-level height in pixels. Access after disposal throws `ObjectDisposedException`.

<a id="size"></a>
### `public Vector2i Size { get; }`

Returns `(Width, Height)` in pixels, or `Vector2i.Zero` for an empty image.

<a id="pixelformat"></a>
### `public Image.Format PixelFormat { get; }`

Returns the exact raw-buffer layout. A new empty image reports `L8`; format alone does not imply that storage exists.

<a id="hasmipmaps"></a>
### `public bool HasMipmaps { get; }`

True only when at least one complete lower-resolution level is stored after the base level. A 1×1 image cannot have mipmaps.

<a id="mipmapcount"></a>
### `public int MipmapCount { get; }`

Returns the number of lower-resolution levels, excluding level zero. Each dimension halves independently and is clamped to one until both are one.

<a id="datasize"></a>
### `public int DataSize { get; }`

Returns the byte length of the base level plus every stored mip level. Block-compressed formats use complete block counts at every level.

<a id="iscompressed"></a>
### `public bool IsCompressed { get; }`

True for DXT/BC, RGTC, BPTC, ETC/ETC2, and ASTC layouts. It describes storage, not the availability of an encoder or decoder.

<a id="isempty"></a>
### `public bool IsEmpty { get; }`

True only for the zero-dimension, zero-data canonical state.

<a id="isinvisible"></a>
### `public bool IsInvisible { get; }`

Returns true for an empty image and when every pixel in an uncompressed alpha-capable base level has zero alpha. Returns false for formats without alpha and for block-compressed storage because its pixels are not decoded.

## Method Descriptions

<a id="ctor"></a>
### `public Image()`

Creates an empty `L8` image with no allocated pixel buffer. It raises no events.

<a id="createempty"></a>
### `public static Image CreateEmpty(int width, int height, bool useMipmaps, Image.Format format)`

Returns a new zero-filled image. Width and height must be positive, fit `MaxWidth`/`MaxHeight`, remain within the 268,435,456-pixel safety ceiling, and produce a managed-array-sized buffer. Invalid dimensions or formats throw `ArgumentOutOfRangeException`.

<a id="create"></a>
### `public static Image Create(int width, int height, bool useMipmaps, Image.Format format)`

Obsolete source-compatibility alias of [`CreateEmpty`](#createempty). New code should use `CreateEmpty`.

<a id="createfromdata"></a>
### `public static Image CreateFromData(int width, int height, bool useMipmaps, Image.Format format, byte[] data)`

Validates the same structural limits as `CreateEmpty`, requires the exact calculated byte count, copies `data`, and returns an independent image. Null throws `ArgumentNullException`; a length mismatch throws `ArgumentException`.

<a id="setdata"></a>
### `public void SetData(int width, int height, bool useMipmaps, Image.Format format, byte[] data)`

Validates and copies an exact complete buffer before atomically replacing the current state. Invalid input leaves the image unchanged. `Changed` runs after commit; a subscriber exception does not roll back the new state.

<a id="getdata"></a>
### `public byte[] GetData()`

Returns a fresh copy containing the base level followed by all mip levels. Mutating the returned array cannot change the image.

<a id="getmipmapoffset"></a>
### `public int GetMipmapOffset(int mipmap)`

Returns the byte offset for level zero through `MipmapCount`. Empty images throw `InvalidOperationException`; absent levels throw `ArgumentOutOfRangeException`.

<a id="copyfrom"></a>
### `public void CopyFrom(Image source)`

Copies dimensions, format, mipmap policy, and bytes from a snapshot of `source`, while retaining this resource's name/path/scene identity. Copying from self is a no-op. Null or disposed inputs are rejected.

<a id="getpixel-xy"></a>
### `public Color GetPixel(int x, int y)`

Decodes an uncompressed base-level pixel. Coordinates are zero-based and must be inside the image; empty or compressed images throw `InvalidOperationException`.

<a id="getpixel-point"></a>
### `public Color GetPixel(Vector2i point)`

Equivalent to `GetPixel(point.X, point.Y)`.

<a id="setpixel-xy"></a>
### `public void SetPixel(int x, int y, Color color)`

Encodes one uncompressed base-level pixel using the current format's quantization rules and raises `Changed`. Existing mip levels are not regenerated; call `GenerateMipmaps` after a batch of per-pixel edits when a consistent chain is required.

<a id="setpixel-point"></a>
### `public void SetPixel(Vector2i point, Color color)`

Equivalent to `SetPixel(point.X, point.Y, color)`.

<a id="detectalpha"></a>
### `public Image.AlphaMode DetectAlpha()`

Returns `None` when no base pixel is transparent, `Bit` when alpha uses only fully transparent/opaque values, and `Blend` when any fractional alpha is present. `Rgba16I` uses 65,535 as its opaque endpoint. Empty images and formats without alpha report `None`. Opaque compressed formats report `None`; DXT3 and DXT5 report `Blend` from format metadata without decoding.

<a id="detectusedchannels"></a>
### `public Image.UsedChannels DetectUsedChannels(Image.CompressSource source = Image.CompressSource.Generic)`

Scans the base level and returns the smallest useful channel set. `Rgba16I` alpha is compared against 65,535; `Normal` always selects red/green; `Srgb` currently uses ordinary color detection. Invalid enum values are rejected.

<a id="getusedrect"></a>
### `public RectI GetUsedRect()`

Returns the smallest half-open base-level rectangle whose pixels have alpha greater than zero. An entirely transparent image returns `default(RectI)`; formats without alpha treat decoded pixels as opaque.

<a id="fill"></a>
### `public void Fill(Color color)`

Encodes `color` into every pixel of every stored level. Quantization and omitted-channel defaults follow the current format.

<a id="fillrect"></a>
### `public void FillRect(RectI rectangle, Color color)`

Clips the half-open rectangle to the base image. Nonpositive or disjoint rectangles change no pixels. When mipmaps exist, they are rebuilt from the resulting base level.

<a id="clearmipmaps"></a>
### `public void ClearMipmaps()`

Retains base bytes and removes every lower level. Calling it without mipmaps is safe.

<a id="generatemipmaps"></a>
### `public void GenerateMipmaps(bool renormalize = false)`

Builds the complete chain using 2×2 averaging. With `renormalize`, averaged RGB values are interpreted as signed normal components and normalized. Empty and compressed images are rejected.

<a id="convert"></a>
### `public void Convert(Image.Format format)`

Decodes and re-encodes every stored pixel into an uncompressed destination. Compressed source or destination layouts are rejected because block decompression is not integrated.

<a id="crop"></a>
### `public void Crop(int width, int height)`

Copies the top-left overlap into the requested positive size and fills newly exposed pixels with transparent black encoded in the current format. Existing mipmaps are regenerated.

<a id="getregion"></a>
### `public Image GetRegion(RectI region)`

Returns a new image containing the clipped base-level intersection without mipmaps. A disjoint or nonpositive region returns a new empty image.

<a id="flipx"></a>
### `public void FlipX()`

Reverses pixel order horizontally in each stored level.

<a id="flipy"></a>
### `public void FlipY()`

Reverses row order vertically in each stored level.

<a id="rotate90"></a>
### `public void Rotate90(ClockDirection direction)`

Rotates the base level 90 degrees, swaps width and height, and regenerates a prior mip chain. Undefined directions throw `ArgumentOutOfRangeException`.

<a id="rotate180"></a>
### `public void Rotate180()`

Rotates every stored level by 180 degrees.

<a id="shrinkx2"></a>
### `public void ShrinkX2()`

Halves each dimension using bilinear filtering, with each result clamped to one pixel, and regenerates existing mipmaps.

<a id="resize"></a>
### `public void Resize(int width, int height, Image.Interpolation interpolation = Image.Interpolation.Bilinear)`

Resamples the base level to positive validated dimensions with the selected filter and rebuilds a prior mip chain. Invalid dimensions or interpolation values throw `ArgumentOutOfRangeException`.

<a id="resizetopot"></a>
### `public void ResizeToPowerOfTwo(bool square = false, Image.Interpolation interpolation = Image.Interpolation.Bilinear)`

Rounds each dimension upward to a power of two. When `square` is true, both use the larger result. The selected interpolation and normal resize limits apply.

<a id="blitrect"></a>
### `public void BlitRect(Image source, RectI sourceRect, Vector2i destination)`

Copies the clipped source rectangle without alpha blending. Source and destination formats must match. Destination is aligned with `sourceRect.Position`, so source clipping preserves the corresponding destination offset.

<a id="blendrect"></a>
### `public void BlendRect(Image source, RectI sourceRect, Vector2i destination)`

Composites straight-alpha source colors over the destination. Formats must match and both images must be readable. For `Rgba16I`, alpha is normalized from `0..65535` during blending and converted back to integer storage afterward.

<a id="blitrectmask"></a>
### `public void BlitRectMask(Image source, Image mask, RectI sourceRect, Vector2i destination)`

Copies only pixels whose same-coordinate mask alpha is nonzero. Source and mask dimensions must match; source/destination formats must match; the mask must expose alpha.

<a id="blendrectmask"></a>
### `public void BlendRectMask(Image source, Image mask, RectI sourceRect, Vector2i destination)`

Combines mask selection with straight-alpha compositing. Source, destination, and mask validation occurs before destination commit.

<a id="adjustbcs"></a>
### `public void AdjustBCS(float brightness, float contrast, float saturation)`

Applies finite RGB factors to all stored levels while preserving alpha. `1` is neutral for each factor; non-finite factors throw `ArgumentOutOfRangeException`.

<a id="fixalphaedges"></a>
### `public void FixAlphaEdges()`

For each base-level `Rgba8` pixel with alpha below 20/255, copies RGB from the nearest pixel within a four-pixel radius whose alpha is at least 20/255. Alpha is unchanged and mipmaps are rebuilt.

<a id="premultiplyalpha"></a>
### `public void PremultiplyAlpha()`

Multiplies each base-level `Rgba8` RGB byte by its alpha byte with deterministic integer rounding, preserves alpha, and rebuilds mipmaps. Other formats are rejected.

<a id="srgbtolinear"></a>
### `public void SRGBToLinear()`

Transforms RGB components of every stored `Rgb8` or `Rgba8` pixel from nonlinear sRGB to linear encoding. Alpha is preserved.

<a id="lineartosrgb"></a>
### `public void LinearToSRGB()`

Transforms RGB components of every stored `Rgb8` or `Rgba8` pixel from linear to nonlinear sRGB encoding. Alpha is preserved.

<a id="bumpmaptonormalmap"></a>
### `public void BumpMapToNormalMap(float bumpScale = 1f)`

Uses each pixel's red/luminance-derived height and wrapping right/below differences to create a normalized `Rgba8` tangent-space normal. The result has no mipmaps. `bumpScale` must be finite.

<a id="normalmaptoxy"></a>
### `public void NormalMapToXY()`

Converts readable data to `Rgba8`, then stores green as luminance and red as alpha in `La8`. The existing mipmap layout is retained.

<a id="rgbetosrgb"></a>
### `public Image RGBEToSRGB()`

Returns a new `Rgb8` image by decoding shared-exponent `Rgbe9995` values and applying linear-to-sRGB conversion. Source dimensions and mipmap policy are preserved.

<a id="computeimagemetrics"></a>
### `public ImageMetrics ComputeImageMetrics(Image comparedImage, bool useLuma)`

Computes errors in 8-bit units over the two base levels' common top-left area. With `useLuma`, each pixel contributes Rec. 709 luminance; otherwise it contributes RGBA. Components outside `0..1`, empty images, and compressed images are rejected.

## Constant Descriptions

<a id="maxwidth"></a>
### `public const int MaxWidth = 16_777_216`

Maximum structurally accepted width. The lower pixel-count and managed-array ceilings normally become effective first.

<a id="maxheight"></a>
### `public const int MaxHeight = 16_777_216`

Maximum structurally accepted height. The lower pixel-count and managed-array ceilings normally become effective first.

## Protected Extension Point Descriptions

<a id="createduplicateinstance"></a>
### `protected override Resource CreateDuplicateInstance()`

Creates a new empty `Image` used as the target of inherited resource duplication. The class is sealed, so consumers do not override this member.

<a id="copycustomstateto"></a>
### `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)`

Copies a stable snapshot of dimensions, format, mipmap policy, and bytes into the target `Image`. Image pixels contain no nested resources, so deep-copy policy and the two resource delegates do not change the result. A mismatched or null target is rejected by the inherited duplication protocol.

<a id="getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Appends read-only descriptors for `Width`, `Height`, `Size`, `PixelFormat`, `HasMipmaps`, and `DataSize` to inherited resource descriptors.

<a id="dispose-bool"></a>
### `protected override void Dispose(bool disposing)`

When `disposing` is true, serializes with mutations, releases the managed pixel buffer, restores canonical empty fields, and then completes inherited resource cleanup. The winning disposal thread follows the lifecycle rules documented by [`ElectronObject`](ElectronObject.md).

## Encoded image methods

The current native codec profile decodes to copied `Rgba8` base pixels without mipmaps. Input is limited to 64 MiB and image dimensions are checked before invoking a decoder. Sixteen-bit PNG input is reduced to eight bits per channel. Source channel-layout preservation, color-profile/gamma metadata and complete codec-variant parity remain pending; this is not the complete codec contract.

<a id="load"></a>
### `public void Load(string path)`

Replaces pixels from a PNG, JPEG, WebP, BMP, TGA or SVG file. SVG uses scale 1. Decoding and native cleanup finish before committing all image state and emitting one synchronous `Changed` event. Decoder, input and I/O failures preserve previous pixels; an observer exception occurs after commit. The path accepts operating-system paths and existing `res://`/`user://` directory mappings through `FileAccess`. Extensions are case-insensitive; unknown extensions raise `NotSupportedException`. Normal path and I/O errors propagate.

<a id="loadfromfile"></a>
### `public static Image LoadFromFile(string path)`

Returns an independent caller-owned image decoded from a file. Decoding and native cleanup finish before committing all image state and emitting one synchronous `Changed` event. Decoder, input and I/O failures preserve previous pixels; an observer exception occurs after commit. The path accepts operating-system paths and existing `res://`/`user://` directory mappings through `FileAccess`. Extensions are case-insensitive; unknown extensions raise `NotSupportedException`. Normal path and I/O errors propagate.

<a id="loadpngfrombuffer"></a>
### `public void LoadPNGFromBuffer(ReadOnlySpan<byte> buffer)`

Replaces pixels from PNG bytes. Decoding and native cleanup finish before committing all image state and emitting one synchronous `Changed` event. Decoder, input and I/O failures preserve previous pixels; an observer exception occurs after commit. The input span is copied before validation. Invalid or truncated data raises `InvalidDataException`; disposed instances reject access.

<a id="loadjpgfrombuffer"></a>
### `public void LoadJPGFromBuffer(ReadOnlySpan<byte> buffer)`

Replaces pixels from JPEG bytes. Decoding and native cleanup finish before committing all image state and emitting one synchronous `Changed` event. Decoder, input and I/O failures preserve previous pixels; an observer exception occurs after commit. The input span is copied before validation. Invalid or truncated data raises `InvalidDataException`; disposed instances reject access.

<a id="loadwebpfrombuffer"></a>
### `public void LoadWebPFromBuffer(ReadOnlySpan<byte> buffer)`

Replaces pixels from WebP bytes. Decoding and native cleanup finish before committing all image state and emitting one synchronous `Changed` event. Decoder, input and I/O failures preserve previous pixels; an observer exception occurs after commit. The input span is copied before validation. Invalid or truncated data raises `InvalidDataException`; disposed instances reject access. Animated WebP supplies its first composited frame.

<a id="loadbmpfrombuffer"></a>
### `public void LoadBMPFromBuffer(ReadOnlySpan<byte> buffer)`

Replaces pixels from BMP bytes. Decoding and native cleanup finish before committing all image state and emitting one synchronous `Changed` event. Decoder, input and I/O failures preserve previous pixels; an observer exception occurs after commit. The input span is copied before validation. Invalid or truncated data raises `InvalidDataException`; disposed instances reject access.

<a id="loadtgafrombuffer"></a>
### `public void LoadTGAFromBuffer(ReadOnlySpan<byte> buffer)`

Replaces pixels from TGA bytes. Decoding and native cleanup finish before committing all image state and emitting one synchronous `Changed` event. Decoder, input and I/O failures preserve previous pixels; an observer exception occurs after commit. The input span is copied before validation. Invalid or truncated data raises `InvalidDataException`; disposed instances reject access.

<a id="loadsvgfrombuffer"></a>
### `public void LoadSVGFromBuffer(ReadOnlySpan<byte> buffer, float scale = 1f)`

Rasterizes uncompressed UTF-8 SVG bytes through the internal SDL_image decoder. Intrinsic dimensions come from pixel or physical `width`/`height`, or `viewBox`; absent dimensions default to a 300×150 viewport. Positive finite `scale` multiplies the dimensions, rounded to integer pixels. Each output axis is limited to 16,384 pixels and the usual image pixel limit applies. XML DTDs, malformed input, unsupported dimension units and oversized results throw `InvalidDataException` before native decoding. The input span is copied; success replaces the image with RGBA8 base pixels and emits `Changed` once. Failed validation or decoding preserves the previous image.

<a id="loadsvgfromstring"></a>
### `public void LoadSVGFromString(string svg, float scale = 1f)`

Encodes the string as strict UTF-8 and follows the buffer method's rasterization and state contract. Null input throws `ArgumentNullException`; malformed text throws `InvalidDataException`. A disposed image rejects the call.

<a id="savepngtobuffer"></a>
### `public byte[] SavePNGToBuffer()`

Encodes a stable base-level snapshot as PNG. Empty and compressed images are rejected. Encoding reads an independent snapshot and neither mutates pixels nor emits `Changed`. Only the base level is encoded; output is capped at 64 MiB. PNG converts the snapshot to RGBA8. Returned bytes belong to the caller.

<a id="savejpgtobuffer"></a>
### `public byte[] SaveJPGToBuffer(float quality = 0.75f)`

Encodes the base level as lossy JPEG without alpha. Empty and compressed images are rejected. Encoding reads an independent snapshot and neither mutates pixels nor emits `Changed`. Only the base level is encoded; output is capped at 64 MiB. JPEG converts to RGB8, discards alpha and remains lossy at maximum quality. Quality must be finite and in `[0.01, 1]`; invalid values throw `ArgumentOutOfRangeException`. Returned bytes belong to the caller.

<a id="savepng"></a>
### `public void SavePNG(string path)`

Atomically replaces a file with encoded PNG bytes. Empty and compressed images are rejected. Encoding reads an independent snapshot and neither mutates pixels nor emits `Changed`. Only the base level is encoded; output is capped at 64 MiB. PNG converts the snapshot to RGBA8. Encoding completes before `AtomicFile` replaces the destination in the same directory. Parent directories must exist; failures before replacement preserve an existing file. Paths use the same `res://`/`user://` resolution as loading.

<a id="savejpg"></a>
### `public void SaveJPG(string path, float quality = 0.75f)`

Atomically replaces a file with JPEG bytes without alpha. Empty and compressed images are rejected. Encoding reads an independent snapshot and neither mutates pixels nor emits `Changed`. Only the base level is encoded; output is capped at 64 MiB. JPEG converts to RGB8, discards alpha and remains lossy at maximum quality. Quality must be finite and in `[0.01, 1]`; invalid values throw `ArgumentOutOfRangeException`. Encoding completes before `AtomicFile` replaces the destination in the same directory. Parent directories must exist; failures before replacement preserve an existing file. Paths use the same `res://`/`user://` resolution as loading.

## Lifecycle and state transitions

`new Image()` starts empty. A factory or `SetData` establishes nonempty storage. Processing operations preserve or replace format, size, and mipmap policy as documented. `Dispose` clears the buffer and closes inherited resource state; all later public state access fails with `ObjectDisposedException`.

## Invariants and error behavior

- Empty state is exactly `0 × 0`, `L8`, no mipmaps, zero bytes.
- Nonempty dimensions are positive; width and height do not exceed the public maxima; total base pixels do not exceed 268,435,456. Required byte counts exceeding the managed-array ceiling are rejected before allocation.
- Data length always exactly matches dimensions, format, and complete mipmap policy.
- A stored mip chain is complete; partial chains are rejected.
- Factories and replacements copy caller arrays; getters return copies.
- Invalid arguments fail before state publication. Subscriber exceptions occur after commit. `SetPixel` leaves existing mip bytes unchanged until explicit regeneration.
- CPU pixel operations reject block-compressed storage explicitly.

## Threading guarantees

Public state reads and writes are safe for concurrent calls on the same image. Mutations on one image are serialized. Source images in multi-image operations are snapshotted, so a later concurrent source change cannot alter the in-progress result. Event handlers execute synchronously on the mutating thread and receive no affinity guarantee. `SetPixel` does not allocate a full replacement buffer; bulk processing does not promise allocation-free execution.

## Dependencies and interactions

`Image` depends on `Resource`, `Color`, `Vector2i`, `RectI`, `ClockDirection`, `Mathf`/BCL scalar operations, binary primitives, and managed arrays. Managed processing does not invoke native code. File/buffer codecs use internal SDL3-CS bindings, temporary native surfaces, `FileAccess`, and atomic file replacement; no native surface escapes to callers. Packed scenes duplicate image buffers through the normal resource graph rules.

## Verification

`tests/Electron2D.Tests/Program.cs` verifies empty/disposed/error states, enum identities, dimensions and exact data sizes, every uncompressed format's pixel access, every compressed format's structural storage, compressed mip sizing, copied-buffer ownership, mip generation/removal/offsets, strong `SetData` failure safety, flips, rotations, regions, all resize filters, crop, blit/blend/masks, alpha processing, color-space and format conversion, height/normal conversion, RGBE decoding, metrics, property descriptors, resource duplication, post-commit event exceptions, and disposal rejection.

## Known limitations and coverage boundary

The implemented surface covers all backend-independent CPU operations in the audited reference API, adapted to typed C# properties, exceptions, arrays, and `ImageMetrics`. The following members are intentionally absent, not stubs:

- PNG/JPEG/WebP/BMP/TGA/SVG file and buffer loading and PNG/JPEG saving are executable under the codec profile above. SVG raster dimensions use the documented XML preflight and may differ from SVG renderers with other CSS sizing rules. DDS/KTX/EXR, WebP saving, supported-format discovery, source channel layouts and complete color/metadata semantics remain unfinished. SDL_image 3.4.6 changes opaque colors when saving WebP at its lossless setting, so no public lossless encoder is claimed.
- `compress` and `compress_from_channels` start with the editor plus primary SDL3 GPU renderer and its selected offline texture-compression toolchain; they belong in the first approved texture import/compression slice.
- `decompress` starts when a portable CPU decoder or renderer readback path is selected; it belongs in the first compressed-CPU-consumption slice.
- ImageTexture converts copied pixels for GPU sampling in the first rendering slice; ordinary texture drawing and additional formats remain unfinished.
- Loader/import cache leases start with the typed loader/import domain and first native-backed texture resource.
- Dynamic dictionaries, universal values, integer error codes, and untyped format loaders are permanently replaced by typed return values, arrays, and exceptions.

See [ADR 0039](../decisions/resources.md#adr-0039).

The native codec checks in `ImageCodecTests` cover six load formats, PNG/JPEG saving, SVG size/color/scale and rejection of DTD or oversized input, PNG grayscale/16-bit/alpha vectors, BMP/TGA orientation and pitch, callback failure, state preservation, parallel independent decoding, disposal, virtual paths, oversized inputs and atomic-write failures on Linux x64. The rendering checks additionally upload decoded PNG pixels and verify GPU readback through both HLSL and GLSL. These checks do not establish AOT or other-platform acceptance.
