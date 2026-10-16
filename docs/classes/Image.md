# Image

Last updated: 2026-10-16

**Inherits:** [Resource](Resource.md) → [ElectronObject](ElectronObject.md)

**Inherited By:** none

- **Sources:** [`src/Core/IO/Image.cs`](../../src/Core/IO/Image.cs), [`src/Core/IO/Image.Processing.cs`](../../src/Core/IO/Image.Processing.cs), [`src/Core/IO/Image.Codecs.cs`](../../src/Core/IO/Image.Codecs.cs), [`src/Core/IO/Image.SVG.cs`](../../src/Core/IO/Image.SVG.cs), [`src/Core/IO/Image.BCn.cs`](../../src/Core/IO/Image.BCn.cs), [`src/Core/IO/Image.DDS.cs`](../../src/Core/IO/Image.DDS.cs), [`src/Core/IO/Image.DDS.Pixels.cs`](../../src/Core/IO/Image.DDS.Pixels.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed partial class Image : Resource`

> Owns portable CPU-side 2D pixel data and provides deterministic in-memory image processing.

## Description

`Image` stores one positive-sized base level and, optionally, its complete mip chain in one managed byte buffer. A newly constructed instance is the canonical empty state: zero width, zero height, `L8`, no mipmaps, and no data. Static factories validate dimensions, format, pixel count, and exact byte length before publishing an image.

All 47 raw [`Image.Format`](Image.Format.md) identities are accepted for storage. Twenty-five uncompressed formats support pixel access and processing. The 22 block-compressed formats support exact buffer ownership, sizing, copying, mip offsets, duplication, and metadata-only alpha inspection, and explicit decoding for all 22 roles. S3TC/BPTC also support compression. Pixel access and ordinary processing still require an uncompressed image.

Pixel coordinates use a top-left origin: X increases rightward and Y increases downward. Rectangles are half-open. Stored multi-byte components use little-endian order. `GetData` and data-taking APIs copy their arrays, so callers never share mutable buffer ownership with an image.

The browser platform package statically links the existing SDL core/image codecs. Its contract app executes PNG/JPEG/WebP/BMP/TGA/SVG loading and PNG/JPEG saving through this same API, including decoder rejection and state preservation. Canvas presentation and additional codec formats remain outside that check.

Every state read and mutation is synchronized per instance. Bulk mutations prepare and commit a complete replacement state; `SetPixel` updates only the addressed pixel under serialization. Both raise inherited [`Resource.Changed`](Resource.md#e-electron2d-resource-changed) after releasing locks. A throwing subscriber observes committed state and propagates its exception. Calls involving multiple images snapshot sources before changing the destination; no cross-image lock is held during callbacks.

DDS loading validates the complete source before publication and returns the first image of an array/cube, or the first slice at every volume mip. Canonical complete mip chains and tightly packed raw layouts are required; truncated, partial-chain and inconsistent payloads fail atomically. Legacy masks, numeric float/unorm FourCC and applicable DX10 raw/HDR/BC formats are supported. Saving preserves exact supported raw/block bytes and uses atomic file replacement. Signed RGTC and formats with no DDS storage representation fail explicitly.

`Compress` and `CompressFromChannels` consume S3TC (BC1/BC3/BC4/BC5) or BPTC (BC7; BC6 for HDR RGB). Explicit RGB drops alpha in BC1. Normal sources choose RG channels. Existing mips are compressed individually; none are synthesized. HDR BC6 requires finite colors and preserves sign/range. `Decompress` returns RGBA8, RGB/RGBA float or RGBA half while preserving every mip; an uncompressed image is a silent no-op. Source bytes and the conservative RGBA-float expansion must each fit 64 MiB, checked before codec allocation. ETC/EAC/ASTC decoding now supplies the remaining block roles; signed EAC becomes normalized RGBA float and ASTC HDR becomes RGBA half. Their encoding remains unavailable.

BC1–BC7 sources retain their original format, complete mip chain and exact block bytes. Prepared CPU sampling decodes BC1–BC5/BC7 to RGBA8 and signed/unsigned BC6 to RGB floating point. GPU allocates native BC storage when the device supports it; otherwise it uploads prepared decoded pixels. Compatibility uses the decoded snapshot; BC6 requires an HDR-capable driver and the current Linux driver rejects that role. The RA-as-RG role uses decoded upload to preserve channel semantics. ETC/EAC/ASTC use shared decoded upload; integer sampling remains separate.

## Examples

```csharp
using var image = Image.CreateEmpty(64, 32, useMipmaps: false, Image.Format.Rgba8);
image.Fill(Colors.Transparent);
image.FillRect(new Rect2i(8, 8, 16, 16), Colors.White);
image.GenerateMIPMaps();

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
| [`public bool HasMIPMaps { get; }`](#hasmipmaps) | Whether a complete lower-resolution chain follows the base level. |
| [`public int MIPMapCount { get; }`](#mipmapcount) | Number of stored levels excluding the base. |
| [`public int DataSize { get; }`](#datasize) | Complete raw-buffer length in bytes. |
| [`public bool IsCompressed { get; }`](#iscompressed) | Whether the format uses GPU block compression. |
| [`public bool IsEmpty { get; }`](#isempty) | Whether the image is in the canonical empty state. |
| [`public bool IsInvisible { get; }`](#isinvisible) | Whether every base-level alpha value is zero. |

## Methods

| Member | Description |
| --- | --- |
| [`public static Image CreateEmpty(int width, int height, bool useMipmaps, Image.Format format)`](#createempty) | Creates zero-filled storage. |
| [`public static Image CreateFromData(int width, int height, bool useMipmaps, Image.Format format, byte[] data)`](#createfromdata) | Creates an image from an exact copied raw buffer. |
| [`public void Compress(Image.CompressMode mode, Image.CompressSource source = Image.CompressSource.Generic, Image.ASTCFormat astcFormat = Image.ASTCFormat.Format4X4)`](#compress) | Atomically compresses all stored mips with detected channels. |
| [`public void CompressFromChannels(Image.CompressMode mode, Image.UsedChannels channels, Image.ASTCFormat astcFormat = Image.ASTCFormat.Format4X4)`](#compressfromchannels) | Compresses with an explicit channel role. |
| [`public void Decompress()`](#decompress) | Decodes BC/ETC/EAC/ASTC, preserving mips. |
| [`public void LoadKTXFromBuffer(ReadOnlySpan<byte> buffer)`](#loadktxfrombuffer) | Loads flat KTX1/KTX2 and portable Basis LDR/HDR pixels with complete mip chains. |
| [`public void LoadDDSFromBuffer(ReadOnlySpan<byte> buffer)`](#loadddsfrombuffer) | Validates and loads copied DDS pixels. |
| [`public byte[] SaveDDSToBuffer()`](#saveddstobuffer) | Returns an independent DDS container without recompressing blocks. |
| [`public void SaveDDS(string path)`](#savedds) | Atomically saves DDS to an existing parent directory. |
| [`public void SetData(int width, int height, bool useMipmaps, Image.Format format, byte[] data)`](#setdata) | Atomically replaces all pixel state. |
| [`public byte[] GetData()`](#getdata) | Returns an independent raw-buffer copy. |
| [`public int GetMIPMapOffset(int mipmap)`](#getmipmapoffset) | Gets a stored level's byte offset. |
| [`public void CopyFrom(Image source)`](#copyfrom) | Copies pixel state while preserving resource identity. |
| [`public Color GetPixel(int x, int y)`](#getpixel-xy) | Decodes one base-level pixel. |
| [`public Color GetPixel(Vector2i point)`](#getpixel-point) | Decodes one base-level pixel. |
| [`public void SetPixel(int x, int y, Color color)`](#setpixel-xy) | Encodes one base-level pixel. |
| [`public void SetPixel(Vector2i point, Color color)`](#setpixel-point) | Encodes one base-level pixel. |
| [`public Image.AlphaMode DetectAlpha()`](#detectalpha) | Classifies base-level alpha use. |
| [`public Image.UsedChannels DetectUsedChannels(Image.CompressSource source = Image.CompressSource.Generic)`](#detectusedchannels) | Detects the smallest meaningful channel set. |
| [`public Rect2i GetUsedRect()`](#getusedrect) | Finds nontransparent base-level bounds. |
| [`public void Fill(Color color)`](#fill) | Fills all stored levels. |
| [`public void FillRect(Rect2i rectangle, Color color)`](#fillrect) | Fills a clipped base-level rectangle. |
| [`public void ClearMIPMaps()`](#clearmipmaps) | Removes lower-resolution levels. |
| [`public void GenerateMIPMaps(bool renormalize = false)`](#generatemipmaps) | Rebuilds a complete mip chain. |
| [`public void Convert(Image.Format format)`](#convert) | Converts all stored levels to an uncompressed format. |
| [`public void Crop(int width, int height)`](#crop) | Crops or expands from the top-left corner. |
| [`public Image GetRegion(Rect2i region)`](#getregion) | Copies a clipped base-level region. |
| [`public void FlipX()`](#flipx) | Flips all levels horizontally. |
| [`public void FlipY()`](#flipy) | Flips all levels vertically. |
| [`public void Rotate90(ClockDirection direction)`](#rotate90) | Rotates by 90 degrees. |
| [`public void Rotate180()`](#rotate180) | Rotates by 180 degrees. |
| [`public void ShrinkX2()`](#shrinkx2) | Halves dimensions with bilinear filtering. |
| [`public void Resize(int width, int height, Image.Interpolation interpolation = Image.Interpolation.Bilinear)`](#resize) | Resamples the base level. |
| [`public void ResizeToPowerOfTwo(bool square = false, Image.Interpolation interpolation = Image.Interpolation.Bilinear)`](#resizetopot) | Resamples to power-of-two dimensions. |
| [`public void BlitRect(Image source, Rect2i sourceRect, Vector2i destination)`](#blitrect) | Copies a clipped source rectangle. |
| [`public void BlendRect(Image source, Rect2i sourceRect, Vector2i destination)`](#blendrect) | Alpha-composites a clipped source rectangle. |
| [`public void BlitRectMask(Image source, Image mask, Rect2i sourceRect, Vector2i destination)`](#blitrectmask) | Copies source pixels selected by mask alpha. |
| [`public void BlendRectMask(Image source, Image mask, Rect2i sourceRect, Vector2i destination)`](#blendrectmask) | Alpha-composites source pixels selected by mask alpha. |
| [`public void AdjustBCS(float brightness, float contrast, float saturation)`](#adjustbcs) | Adjusts brightness, contrast, and saturation. |
| [`public void FixAlphaEdges()`](#fixalphaedges) | Propagates nearby opaque RGB into low-alpha `Rgba8` pixels. |
| [`public void PremultiplyAlpha()`](#premultiplyalpha) | Multiplies `Rgba8` RGB bytes by alpha. |
| [`public void SRGBToLinear()`](#srgbtolinear) | Converts `Rgb8` or `Rgba8` RGB values to linear encoding. |
| [`public void LinearToSRGB()`](#lineartosrgb) | Converts `Rgb8` or `Rgba8` RGB values to nonlinear encoding. |
| [`public void BumpMapToNormalMap(float bumpScale = 1f)`](#bumpmaptonormalmap) | Converts height values to a wrapping tangent-space normal map. |
| [`public void NormalMapToXY()`](#normalmaptoxy) | Packs normal X/Y into `La8`. |
| [`public Image RGBEToSRGB()`](#rgbetosrgb) | Decodes `Rgbe9995` into a new `Rgb8` image. |
| [`public ImageMetrics ComputeImageMetrics(Image comparedImage, bool useLuma)`](#computeimagemetrics) | Computes absolute-error statistics over the common area. |
| [`public void Load(string path)`](#load) | Replaces pixels from a supported raster, vector, DDS, EXR or KTX image file. |
| [`public static Image LoadFromFile(string path)`](#loadfromfile) | Returns an independent caller-owned image decoded from a file. |
| [`public void LoadPNGFromBuffer(ReadOnlySpan<byte> buffer)`](#loadpngfrombuffer) | Replaces pixels from PNG bytes. |
| [`public void LoadJPGFromBuffer(ReadOnlySpan<byte> buffer)`](#loadjpgfrombuffer) | Replaces pixels from JPEG bytes. |
| [`public void LoadEXRFromBuffer(ReadOnlySpan<byte> buffer)`](#loadexrfrombuffer) | Loads flat HDR channels and half/float precision. |
| [`public byte[] SaveEXRToBuffer(bool grayscale = false, bool colorImage = false, float maxLinearValue = -1f)`](#saveexrtobuffer) | Encodes lossless HDR data with channel and clamp options. |
| [`public void SaveEXR(string path, bool grayscale = false, bool colorImage = false, float maxLinearValue = -1f)`](#saveexr) | Atomically saves lossless EXR bytes. |
| [`public void LoadWebPFromBuffer(ReadOnlySpan<byte> buffer)`](#loadwebpfrombuffer) | Replaces pixels from WebP bytes. |
| [`public void LoadBMPFromBuffer(ReadOnlySpan<byte> buffer)`](#loadbmpfrombuffer) | Replaces pixels from BMP bytes. |
| [`public void LoadTGAFromBuffer(ReadOnlySpan<byte> buffer)`](#loadtgafrombuffer) | Replaces pixels from TGA bytes. |
| [`public void LoadSVGFromBuffer(ReadOnlySpan<byte> buffer, float scale = 1f)`](#loadsvgfrombuffer) | Rasterizes UTF-8 SVG bytes at the requested scale. |
| [`public void LoadSVGFromString(string svg, float scale = 1f)`](#loadsvgfromstring) | Rasterizes an SVG string at the requested scale. |
| [`public byte[] SavePNGToBuffer()`](#savepngtobuffer) | Encodes a stable base-level snapshot as PNG. |
| [`public byte[] SaveJPGToBuffer(float quality = 0.75f)`](#savejpgtobuffer) | Encodes the base level as lossy JPEG without alpha. |
| [`public byte[] SaveWebPToBuffer(bool lossy = false, float quality = 0.75f)`](#savewebptobuffer) | Encodes the base image as exact or lossy WebP. |
| [`public void SaveWebP(string path, bool lossy = false, float quality = 0.75f)`](#savewebp) | Atomically saves WebP bytes. |
| [`public void SavePNG(string path)`](#savepng) | Atomically replaces a file with encoded PNG bytes. |
| [`public void SaveJPG(string path, float quality = 0.75f)`](#savejpg) | Atomically replaces a file with JPEG bytes without alpha. |

## Enumerations

| Type | Description |
| --- | --- |
| [`Image.Format`](Image.Format.md) | Raw byte layouts, including uncompressed and block-compressed storage. |
| [`Image.Interpolation`](Image.Interpolation.md) | Reconstruction filters. |
| [`Image.AlphaMode`](Image.AlphaMode.md) | Detected alpha-use classes. |
| [`Image.UsedChannels`](Image.UsedChannels.md) | Minimal meaningful channel sets. |
| [`Image.CompressSource`](Image.CompressSource.md) | Source semantics used during channel detection and compression. |
| [`Image.CompressMode`](Image.CompressMode.md) | Block-compression family selection. |
| [`Image.ASTCFormat`](Image.ASTCFormat.md) | Validated ASTC footprint selection; codec remains separate. |

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
### `public bool HasMIPMaps { get; }`

True only when at least one complete lower-resolution level is stored after the base level. A 1×1 image cannot have mipmaps.

<a id="mipmapcount"></a>
### `public int MIPMapCount { get; }`

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
### `public int GetMIPMapOffset(int mipmap)`

Returns the byte offset for level zero through `MIPMapCount`. Empty images throw `InvalidOperationException`; absent levels throw `ArgumentOutOfRangeException`.

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

Encodes one uncompressed base-level pixel using the current format's quantization rules and raises `Changed`. Existing mip levels are not regenerated; call `GenerateMIPMaps` after a batch of per-pixel edits when a consistent chain is required.

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
### `public Rect2i GetUsedRect()`

Returns the smallest half-open base-level rectangle whose pixels have alpha greater than zero. An entirely transparent image returns `default(Rect2i)`; formats without alpha treat decoded pixels as opaque.

<a id="fill"></a>
### `public void Fill(Color color)`

Encodes `color` into every pixel of every stored level. Quantization and omitted-channel defaults follow the current format.

<a id="fillrect"></a>
### `public void FillRect(Rect2i rectangle, Color color)`

Clips the half-open rectangle to the base image. Nonpositive or disjoint rectangles change no pixels. When mipmaps exist, they are rebuilt from the resulting base level.

<a id="clearmipmaps"></a>
### `public void ClearMIPMaps()`

Retains base bytes and removes every lower level. Calling it without mipmaps is safe.

<a id="generatemipmaps"></a>
### `public void GenerateMIPMaps(bool renormalize = false)`

Builds the complete chain using 2×2 averaging. With `renormalize`, averaged RGB values are interpreted as signed normal components and normalized. Empty and compressed images are rejected.

<a id="convert"></a>
### `public void Convert(Image.Format format)`

Decodes and re-encodes every stored pixel into an uncompressed destination. Compressed source or destination layouts are rejected because block decompression is not integrated.

<a id="crop"></a>
### `public void Crop(int width, int height)`

Copies the top-left overlap into the requested positive size and fills newly exposed pixels with transparent black encoded in the current format. Existing mipmaps are regenerated.

<a id="getregion"></a>
### `public Image GetRegion(Rect2i region)`

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
### `public void BlitRect(Image source, Rect2i sourceRect, Vector2i destination)`

Copies the clipped source rectangle without alpha blending. Source and destination formats must match. Destination is aligned with `sourceRect.Position`, so source clipping preserves the corresponding destination offset.

<a id="blendrect"></a>
### `public void BlendRect(Image source, Rect2i sourceRect, Vector2i destination)`

Composites straight-alpha source colors over the destination. Formats must match and both images must be readable. For `Rgba16I`, alpha is normalized from `0..65535` during blending and converted back to integer storage afterward.

<a id="blitrectmask"></a>
### `public void BlitRectMask(Image source, Image mask, Rect2i sourceRect, Vector2i destination)`

Copies only pixels whose same-coordinate mask alpha is nonzero. Source and mask dimensions must match; source/destination formats must match; the mask must expose alpha.

<a id="blendrectmask"></a>
### `public void BlendRectMask(Image source, Image mask, Rect2i sourceRect, Vector2i destination)`

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

Appends read-only descriptors for `Width`, `Height`, `Size`, `PixelFormat`, `HasMIPMaps`, and `DataSize` to inherited resource descriptors.

<a id="dispose-bool"></a>
### `protected override void Dispose(bool disposing)`

When `disposing` is true, serializes with mutations, releases the managed pixel buffer, restores canonical empty fields, and then completes inherited resource cleanup. The winning disposal thread follows the lifecycle rules documented by [`ElectronObject`](ElectronObject.md).

## Encoded image methods

The current native codec profile decodes to copied `Rgba8` base pixels without mipmaps. Input is limited to 64 MiB and image dimensions are checked before invoking a decoder. Sixteen-bit PNG input is reduced to eight bits per channel. Source channel-layout preservation, color-profile/gamma metadata and complete codec-variant parity remain pending; this is not the complete codec contract.

<a id="load"></a>
### `public void Load(string path)`

Replaces pixels from a supported raster, vector, DDS, EXR or KTX image file. SVG uses scale 1. Decoding and native cleanup finish before committing all image state and emitting one synchronous `Changed` event. Decoder, input and I/O failures preserve previous pixels; an observer exception occurs after commit. The path accepts operating-system paths and existing `res://`/`user://` directory mappings through `FileAccess`. Extensions are case-insensitive; unknown extensions raise `NotSupportedException`. Normal path and I/O errors propagate.

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

<a id="savewebptobuffer"></a>
### `public byte[] SaveWebPToBuffer(bool lossy = false, float quality = 0.75f)`

Returns independent encoded WebP bytes from a stable RGBA8 base-level snapshot. Lossless preserves all RGB and alpha bytes, including transparent RGB. Lossy quality is finite `[0, 1]`; one remains lossy. Valid quality is ignored for lossless output, which uses `ProjectSettings.WebPLosslessCompressionFactor`; both modes use `WebPCompressionMethod`. Invalid quality rejects in both modes. Supported BC data decompresses without modifying source blocks or mipmaps. Empty/disposed images and unavailable compressed codecs reject. Dimensions are limited to 16383; base RGBA8 input and emitted bytes each have a 64 MiB budget. See [complete codec and delivery contract](../components/image-webp.md).

<a id="savewebp"></a>
### `public void SaveWebP(string path, bool lossy = false, float quality = 0.75f)`

Encodes with `SaveWebPToBuffer`, then atomically replaces the destination through the existing file writer. Operating-system, `res://` and `user://` paths are accepted; parent directories must exist. Encoding failure preserves an existing file and source image. No `Changed` event is emitted.

<a id="loadexrfrombuffer"></a>
### `public void LoadEXRFromBuffer(ReadOnlySpan<byte> buffer)`

Loads a flat scanline/tiled EXR base image with one through four channels and half/float precision; Y expands to RGB. UINT channels convert numerically to float. Decoding and budgets finish before publication; failure preserves state, success emits Changed. Deep/multipart and unavailable compression reject. Input and decoded storage are bounded to 64 MiB. See [complete HDR codec contract](../components/image-exr.md).

<a id="saveexrtobuffer"></a>
### `public byte[] SaveEXRToBuffer(bool grayscale = false, bool colorImage = false, float maxLinearValue = -1f)`

Returns owned lossless PIZ EXR bytes. The twelve R/RG/RGB/RGBA byte/half/float formats are accepted; byte inputs become HALF. Grayscale names one channel Y and rejects multichannel input. colorImage clamps negative components to zero; a nonnegative maxLinearValue caps stored components, including alpha. NaN bounds reject. Unclamped raw data preserves its numerical range and special float values. Encoding reads a stable base snapshot, preserving all source mipmaps and events. Source channel storage and output are each bounded to 64 MiB.

<a id="saveexr"></a>
### `public void SaveEXR(string path, bool grayscale = false, bool colorImage = false, float maxLinearValue = -1f)`

Uses SaveEXRToBuffer, then atomically replaces the file at an operating-system path, res:// path or user:// path. Parent directories must exist; encoding failure preserves the destination.

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

`Image` depends on `Resource`, `Color`, `Vector2i`, `Rect2i`, `ClockDirection`, `Mathf`/BCL scalar operations, binary primitives, and managed arrays. Managed processing does not invoke native code. File/buffer codecs use internal SDL3-CS bindings, temporary native surfaces, `FileAccess`, and atomic file replacement; no native surface escapes to callers. Packed scenes duplicate image buffers through the normal resource graph rules.

## Verification

`tests/Electron2D.Tests/Program.cs` verifies empty/disposed/error states, enum identities, dimensions and exact data sizes, every uncompressed format's pixel access, every compressed format's structural storage, compressed mip sizing, copied-buffer ownership, mip generation/removal/offsets, strong `SetData` failure safety, flips, rotations, regions, all resize filters, crop, blit/blend/masks, alpha processing, color-space and format conversion, height/normal conversion, RGBE decoding, metrics, property descriptors, resource duplication, post-commit event exceptions, and disposal rejection.

## Known limitations and coverage boundary

The implemented surface covers all backend-independent CPU operations in the audited reference API, adapted to typed C# properties, exceptions, arrays, and `ImageMetrics`. The following capability boundaries remain:

- PNG/JPEG/WebP/BMP/TGA/SVG/DDS/EXR/KTX file and buffer loading and PNG/JPEG/WebP/EXR saving are executable under their documented codec profiles. WebP saving uses the private exact/lossy encoder; EXR retains HDR channels and precision. SVG raster dimensions use the documented XML preflight and may differ from SVG renderers with other CSS sizing rules. KTX1/KTX2 and Basis LDR/HDR loading execute under their [documented profile](../components/image-ktx.md); complete color/metadata semantics remain unfinished.
- Compress/CompressFromChannels execute S3TC/BPTC; ETC/EAC/ASTC require portable encoders; decoding and shared texture consumers execute.
- `Decompress` executes every stored BC/ETC/EAC/ASTC role, with complete mips and signed/HDR precision.
- ImageTexture and layered resources sample the supported byte/float and all block families; integer sampling and target-driver precision limits remain explicit.
- Loader/import cache leases start with the typed loader/import domain and first native-backed texture resource.
- Dynamic dictionaries, universal values, integer error codes, and untyped format loaders are permanently replaced by typed return values, arrays, and exceptions.

See [ADR 0039](../decisions/resources.md#adr-0039).

The native codec checks in `ImageCodecTests` cover six load formats, PNG/JPEG saving, SVG size/color/scale and rejection of DTD or oversized input, PNG grayscale/16-bit/alpha vectors, BMP/TGA orientation and pitch, callback failure, state preservation, parallel independent decoding, disposal, virtual paths, oversized inputs and atomic-write failures on Linux x64. The rendering checks additionally upload decoded PNG pixels and verify GPU readback through both HLSL and GLSL. These checks do not establish AOT or other-platform acceptance.

Non-renormalized mipmap generation for L8/LA8/R8/RG8/RGB8/RGBA8 uses encoded byte averages rounded to nearest. Odd extents truncate when halved; one-pixel axes repeat their sample. The connected noise image fixtures verify complete grayscale/colored/normal mip chains. Floating, packed and renormalized paths keep their existing typed processing profiles.


## Custom image decoder integration

[ImageFormatLoader](ImageFormatLoader.md) and [ImageFormatLoaderExtension](ImageFormatLoaderExtension.md) provide caller-owned file decoder registration. Custom suffixes participate in Image loading, resource extension/existence/directory discovery, synchronous/threaded ImageTexture loading and cache replacement. Ordered callbacks execute outside the registry lock with borrowed temporary Image/FileAccess objects; false means unrecognized content, exceptions preserve the destination, and success copies pixels before one Changed notification. Encoded input retains the 64 MiB limit. Registration is process-local and must precede fresh-host file loading. Keep decoders alive through pending worker calls; removal affects subsequent snapshots. Built-in buffer codecs and their channel/color metadata limits remain separate.

<a id="compress"></a>
### Compress

Uses detected base-level channels, then compresses each stored mip under the serialized mutation boundary. Color/data source policy uses channel detection; normal policy selects RG. Invalid enums, empty/compressed inputs, unavailable families and work-budget failures preserve the old state. Changed runs after publication and outside image locks.

<a id="compressfromchannels"></a>
### CompressFromChannels

Explicit Red/RedGreen choose BC4/BC5 in S3TC; alpha roles choose BC3, RGB/luminance choose opaque BC1. BPTC uses BC7 for ordinary/alpha data and BC6 for finite HDR RGB, with signed storage when needed. Compression is lossy.

<a id="decompress"></a>
### Decompress

BC1–BC5/BC7 and RA-as-RG decode to RGBA8; signed/unsigned BC6 decodes to RGB float. The complete stored chain is retained. Calling on uncompressed data emits no change.

<a id="loadddsfrombuffer"></a>
### LoadDDSFromBuffer

Requires bounded header, dimensions, supported color layout and exact complete container payload before invoking the reader or layout adapter. Array/cube/volume selection does not bypass validation of remaining images. Failures preserve prior state; observer errors occur after publication.

<a id="saveddstobuffer"></a>
### SaveDDSToBuffer

Writes supported raw precision/channel layouts or exact BC blocks and all mips. The caller owns the returned buffer. RGBH, RGB16/RGB16I, RA-as-RG and other compressed families have no accepted DDS mapping and throw.

<a id="savedds"></a>
### SaveDDS

Uses FileAccess path resolution and existing atomic replacement. The parent directory must exist. Encoding failure preserves an existing destination.

<a id="loadktxfrombuffer"></a>
### `public void LoadKTXFromBuffer(ReadOnlySpan<byte> buffer)`

Loads copied flat KTX1/KTX2 bytes through the private reader and Basis LDR/HDR transcoder. Preserves supported raw precision and complete mip chains, removes KTX1 row padding, normalizes endian/orientation/swizzles and converts RGB8 sRGB data to linear pixels. Failure preserves the image; success emits Changed after publication. Source/output/expanded pixels are bounded to 64 MiB; key/value metadata to 1 MiB and 4096 entries. Unsupported image roles and unavailable block transformations reject explicitly. See [KTX image integration](../components/image-ktx.md) for native ownership, source corrections, packages and verification limits.
