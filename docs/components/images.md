# Images component

Last updated: 2026-10-16

[Imported image textures](compressed-textures.md) retain raw/PNG/WebP/Basis and block source mips through CompressedTexture, with separate logical drawing size, ordinary canvas consumers and embedded resource/scene storage.

## Scope

This Resources component owns portable managed 2D pixel buffers, binary masks, raw format identity, mipmap layout, CPU image processing, and typed image metrics. It also owns file/buffer decoding, PNG/JPEG/DDS saving and BC1–BC7 CPU block codecs. Texture sampling and renderer handles consume prepared immutable snapshots; importing remains separate.

## Owned types

| Type | Role | Source |
| --- | --- | --- |
| [`ImageFormatLoader`](../classes/ImageFormatLoader.md), [`ImageFormatLoader.LoaderFlags`](../classes/ImageFormatLoader.LoaderFlags.md), [`ImageFormatLoaderExtension`](../classes/ImageFormatLoaderExtension.md) | Caller-owned decoder identity/options and protected file hooks | [`ImageFormatLoader.cs`](../../src/Core/IO/ImageFormatLoader.cs), [`ImageFormatLoaderExtension.cs`](../../src/Core/IO/ImageFormatLoaderExtension.cs) |
| [`Image`](../classes/Image.md) | Mutable managed pixel resource and processing API | [`Image.cs`](../../src/Core/IO/Image.cs), [`Image.Processing.cs`](../../src/Core/IO/Image.Processing.cs), [`Image.Codecs.cs`](../../src/Core/IO/Image.Codecs.cs) |
| [`BitMap`](../classes/BitMap.md) | Packed alpha-derived mask, morphology and polygon contours | [`BitMap.cs`](../../src/Core/IO/BitMap.cs) |
| [`Image.Format`](../classes/Image.Format.md) | Raw uncompressed and GPU block-compressed layout identity | [`Image.cs`](../../src/Core/IO/Image.cs) |
| [`Image.Interpolation`](../classes/Image.Interpolation.md) | Resize reconstruction filter | [`Image.cs`](../../src/Core/IO/Image.cs) |
| [`Image.AlphaMode`](../classes/Image.AlphaMode.md) | Detected alpha classification | [`Image.cs`](../../src/Core/IO/Image.cs) |
| [`Image.UsedChannels`](../classes/Image.UsedChannels.md) | Detected minimal channel set | [`Image.cs`](../../src/Core/IO/Image.cs) |
| [`Image.CompressSource`](../classes/Image.CompressSource.md) | Channel-detection source semantics | [`Image.cs`](../../src/Core/IO/Image.cs) |
| [`Image.CompressMode`](../classes/Image.CompressMode.md) | Block-compression family selection | [`Image.cs`](../../src/Core/IO/Image.cs) |
| [`Image.ASTCFormat`](../classes/Image.ASTCFormat.md) | Future ASTC footprint selection | [`Image.cs`](../../src/Core/IO/Image.cs) |
| [`ImageMetrics`](../classes/ImageMetrics.md) | Typed absolute-error and peak-SNR result | [`Image.cs`](../../src/Core/IO/Image.cs) |
| [`ClockDirection`](../classes/ClockDirection.md) | Stable 2D rotation direction | [`ClockDirection.cs`](../../src/Core/Math/ClockDirection.cs) |

## Runtime flow

1. A default `Image` starts empty, or a factory validates dimensions/format/exact byte count and copies storage.
2. Reads take a consistent per-image state. The metadata overload of `GetData` captures the dimensions, storage format, mipmap presence and copied bytes together. Mip offset queries use layout metadata without copying the pixel buffer.
3. A bulk mutation serializes with other mutations, clones the current buffer, computes and validates a complete next state, commits once, releases image locks, and emits `Resource.Changed`. `SetPixel` updates one pixel under the same serialization without allocating a replacement buffer.
4. Uncompressed pixels decode to `Color`; writes quantize or preserve floating/integer values according to `Image.Format`.
5. Mipmap generation averages 2×2 samples to a complete chain. Half-size reduction promotes authored levels when present. Resize, crop, rotation, and region composition preserve or rebuild mip policy as documented.
6. Duplication copies the complete buffer and inherited Resource configuration but not external path identity.
7. Disposal drops the managed buffer, clears image state, then completes inherited Resource disposal.
8. BitMap can classify a copied Image alpha plane, edit packed bits, dilate or erode a region, and extract marching-squares contours without a renderer.

WebP file/buffer decoding uses the existing private codec for straight RGBA8 and the first reconstructed animation canvas; see [WebP I/O](image-webp.md). The published native ABI requires the decoder export and static demux closure.

## Dependencies and interactions

Color-space conversions use `SRGBToLinear`, `LinearToSRGB` and `RGBEToSRGB`, following the same acronym spelling as the underlying Color API. Their pixel conversion and format contracts are unchanged.

The component depends on `Resource`, typed property descriptors, `Color`, `Vector2i`, `Rect2i`, and BCL binary/numeric primitives. BitMap consumes copied Image alpha data and uses Vector2 contours; it needs no native library. ImageTexture consumes its copied raw buffer and format metadata for GPU upload. `FileAccess` supplies encoded bytes through existing virtual paths. SDL_image handles JPEG and SVG through SDL3-CS. The managed BMP and TGA decoders handle raw and run-length encoded pixels. Temporary surfaces are copied and released before Image commits. PNG and WebP use their private native codecs. CPU processing retains its managed-only path.

Web statically links pinned SDL_image 3.4.6 and its libwebp/sharpyuv closure, retaining the same public codec API and shared SDL core. Internal SDL ReadIO/WriteIO imports return native pointer-sized `size_t`, preserving byte counts on wasm32 and 64-bit hosts while the binding convenience API still returns `ulong`. The browser app executes all six file/buffer decoders, PNG/JPEG/WebP saving, SVG validation and atomic failure checks; its nonthreaded profile excludes the parallel-call check. The shared fixture additionally checks exact read/write counts and EOF through actual SDL IO streams. This is CPU codec evidence, not browser texture presentation.

## Invariants and error behavior

- Populated dimensions are positive, within `MaxWidth`/`MaxHeight`, below 268,435,456 pixels, and representable by one managed byte array; oversized byte counts fail before allocation.
- The byte count exactly matches the base level and every declared mip level. Multi-byte fields are little-endian.
- The default zero-by-zero state is the only empty state.
- Operations that require decoded pixels reject empty or block-compressed data; raw compressed storage remains copyable and structurally queryable, while alpha classification uses format metadata where possible.
- Incoming/outgoing byte arrays never alias engine storage.
- Mutation failure before commit preserves the old state. Observer failure occurs after commit and does not roll state back.
- Source/mask images are snapshotted before destination mutation. No operation promises an atomic transaction across multiple images.
- Large processing calls allocate replacement buffers and are not intended for steady-state frame hot paths.
- BitMap stores eight pixels per byte, snapshots source bits for morphology, and commits state before synchronous change notification. It returns independent L8 images and polygon arrays.

## Current implementation status

All 47 current raw format identities, the complete compression/source/ASTC enum family, exact base/mipmap sizing, 25 uncompressed pixel codecs, 22 raw compressed layouts, pixel access, format conversion, mipmaps, fill, crop/region, flip/rotation, five resize filters, blit/blend/masks, alpha/channel/visible-bound detection, color adjustments, alpha processing, sRGB conversion, normal/RGBE processing, metrics, typed descriptors, Resource duplication, concurrency boundaries, and disposal are implemented.

BitMap's complete declared 2D mask surface is implemented: alpha thresholding, bit access, rectangle writes, nearest resize, L8 conversion, circular grow/shrink and contour extraction. Its managed copy and lifetime behavior use the Resource contract. Physics collision consumers are not yet implemented.

## Raw image snapshots

Read the raw bytes and matching metadata with [`Image.GetData`](../classes/Image.md#getdata-with-metadata). Replace populated storage with `SetData`. `CopyFrom` also accepts an empty image. Raw snapshots preserve compressed blocks and complete mip levels.

Resource and scene archives use the same snapshot when writing images. Saving during a concurrent edit records one committed state for each image. Metadata and pixel bytes retain the existing archive layout, including the canonical empty state and raw byte order under either archive endianness.

Run `ELECTRON2D_TEST_IMAGE_STORAGE=1` to check all raw format sizes, odd compressed mip layouts, copied ownership, empty and disposed state, and 64 archive round trips while a background writer alternates between two incompatible image layouts. The same profile checks an embedded image texture round trip.

## Pixel edits and orientation

`GetPixel` and `SetPixel` use zero-based base-level coordinates. Scalar and vector overloads share the same bounds checks. `SetPixel` leaves the authored mip bytes intact. `Fill` writes the same encoded color to every stored pixel, including all lower levels.

Unsigned channels clamp before truncating. Normalized channels scale in single precision so decoding and re-encoding a stored sample preserves its code. Byte luminance stores the maximum of RGB. For half formats, `SetPixel` and `Fill` truncate the significand, flush half subnormals and signed zero to zero, and retain the sign of overflow and canonical NaN. Raw buffers, conversion and mip filtering support half subnormals. Full floats retain their component bits. RGB9E5 uses a shared exponent and ignores alpha.

Flips and rotations permute the stored base pixels byte for byte. Quarter turns swap width and height. Every orientation change regenerates an existing mip chain from the result, including when a one-pixel axis makes the base permutation an identity. Images with only a base level stay base-only.

Empty or compressed images raise `InvalidOperationException`. Coordinates outside the base level and undefined directions raise `ArgumentOutOfRangeException`. Failed validation preserves the whole state. Each successful edit emits one synchronous `Resource.Changed` after publication. A callback exception propagates after commit. Textures keep their copied pixels until replaced explicitly.

Run `ELECTRON2D_TEST_IMAGE_PIXELS` with `managed`, `gpu`, and `compatibility` in separate processes. The managed profile compares 25 format vectors and exact mip buffers. The two windowed profiles sample an ordinary Sprite's ImageTexture before and after replacing it with a rotated image, then read the rendered pixels.

## Mip chain editing

`ShrinkX2` promotes the first stored mip and keeps the remaining bytes in order. It works on uncompressed pixels and complete block-compressed chains. When the promoted base is 1×1, the image reports no lower levels. Base-only uncompressed images halve through the generation filter and stay base-only.

`GenerateMIPMaps` averages 2×2 samples in storage space. Odd extents truncate and one-pixel axes repeat their sample. Byte and unsigned 16-bit channels round to the nearest value. RGB565 and RGBA4444 sum each stored channel exactly and truncate the mean, so reordering a block leaves its mip pixel unchanged. Normal renormalization applies to byte, float, half and unsigned 16-bit RGB/RGBA layouts. It normalizes the averaged RGB and preserves the averaged alpha.

`ClearMIPMaps` retains the base bytes, including compressed blocks. ImageTexture captures the resulting format, base extent and exact remaining mip chain. The image-storage profile checks authored BC1 colors, odd and one-pixel axes, integer and half-float values, base edits, and observer failure after commit.

## Image texture data

ImageTexture copies the original format, physical dimensions and all stored mips. GetImage returns a fresh snapshot in that format. Source format, alpha presence and mip count are available before the renderer starts. The uninitialized resource has zero logical size, format L8, no alpha, no mips and no image. A custom pixel-backed Texture implements the required logical width and height queries and returns a fresh GetImage copy. Its derived metadata refreshes on Changed.

SetSizeOverride changes logical dimensions and retains an axis when its input is zero. Opacity uses the corresponding physical texel and tests alpha above 0.1. Update matches the original physical dimensions, format and mip configuration. SetImage accepts a new configuration and resets the logical size. Validation failures preserve the previous state. Changed follows a successful publication. The texture RID stays stable across these mutations. Update reuses compatible native storage and SetImage replaces it. Scene-local ImageTexture copies retain original mip bytes and logical size and receive distinct RIDs after loading and instantiating the scene.

GPU sampling uses the prepared upload or supported native block storage, with complete mip chains. Windows/D3D12 and Windows/Vulkan support sampling the four [UInt16 formats](integer-textures.md), including the ordinary Sprite path and RenderingDevice export. RenderingDevice also reads BC3 blocks on both drivers. Compatibility samples the base mip of supported formats, uses decoded block pixels and rejects unsigned integer sampling. Its floating-point path requires a driver that preserves HDR precision.

The `ELECTRON2D_TEST_IMAGE_TEXTURE_DATA` profiles exercise managed data and scene archives, plus Sprite output and native lifetime with byte formats on D3D12 and D3D11. Readback from a rendered viewport verifies the sampled output independently from GetImage's source snapshot. Full package and host qualification follows ADR 0021.

## Image resizing

`Resize` and `ResizeToPowerOfTwo` run the same CPU filters and retain the original pixel format, including packed, integer and HDR layouts. Nearest copies the stored sample exactly. Bilinear and cubic sample around destination pixel centers with clamped source coordinates. Trilinear uses the mean destination/source scale to select the two mip levels and blends their bilinear samples before encoding the output. Temporary levels used to filter a base-only image stay private. An existing chain is rebuilt at the new size.

Lanczos widens the filter along each reduced axis to retain detail across the full footprint. It filters one axis at a time and normalizes the weights at image edges. Scratch storage holds one intermediate line and one set of weights. Color values are filtered in their stored color space.

The image-storage profile verifies exact nearest, bilinear and cubic samples, both orientations of a 32-to-1 reduction, HDR pixels at the boundary, and authored and temporary trilinear levels. It also checks rejected compressed and invalid requests, observer failure after commitment, and the resized chain consumed by ImageTexture.

## Image regions

`GetRegion` keeps the requested positive extent and pads outside the source with zero-valued storage. `Crop` changes the base extent from the top-left and also zero-fills new pixels. Both retain the pixel format and rebuild a source mip chain for the result. Atlas readback and movie capture use the clipped source intersection with base-level pixels.

Blit and blend preserve the translation between source and destination when clipping either side. Copying preserves stored pixel bytes. Blending skips transparent foreground pixels and uses straight alpha, including the full integer alpha range. Masked operations sample alpha at the source coordinates and read destination aliases from the original image. `FillRect` normalizes reversed axes and clips the result. Nonpositive blit and blend extents are empty.

The image-storage profile covers padded and disjoint regions, atlas readback, extreme coordinates, overlapping self-copy, mask aliases, hidden RGB and exact half-float bytes. The movie writer profile exercises the clip-and-resize path against actual resized render targets.

## Image format conversion

`Convert` changes storage for every level independently. Byte RGB-to-luminance uses rounded Rec. 709 weights. Luminance expands to equal RGB channels. For changes in channel count within float, half, unsigned 16-bit normalized or unsigned 16-bit integer storage, retained components keep their exact bytes. New color channels start at zero. A new alpha channel stores one in its numeric representation.

`SRGBToLinear` and `LinearToSRGB` apply the quantized transfer curve to every RGB8 or RGBA8 level and preserve alpha bytes. Format conversion, codec encoding, and texture preparation share the underlying channel conversion. Block storage uses the explicit compression and decompression paths.

The image-storage profile checks independent mip values, luma and alpha, signed zeros and NaN payloads, both 16-bit representations, texture copies, and all 256 byte values in each transfer direction.

Image encoders reject a directory destination with `IOException` before starting atomic replacement. Other path and permission errors retain the file layer's behavior.

## Radiance HDR loading

`Image.Load` and `Image.LoadFromFile` recognize `.hdr` paths. The managed decoder reads Radiance RGBE pixels into linear `Rgbe9995` base storage. It accepts both Radiance signatures, planar run-length packets, legacy repeat markers and flat scanlines. Both axis orders and signs normalize to top-left row order. Exponent zero decodes to black. The RGB9E5 output retains exact representable samples and rounds or saturates at the format limits. [Radiance picture format](https://radsite.lbl.gov/radiance/refer/Notes/picture_format.html).

The loader bounds the text header to 64 KiB and encoded and decoded pixel storage to 64 MiB each. Truncated packets, inconsistent scanline sizes, invalid dimensions and unsupported pixel encodings raise `InvalidDataException` before replacement. A successful instance load publishes one `Changed` notification. Exposure and other descriptive header metadata leave the encoded samples unchanged.

`ResourceLoader` discovers and loads `.hdr` as an `ImageTexture`, retaining RGB9E5 pixels in its source snapshot. For byte-color consumers, use `RGBEToSRGB`. The HDR codec profile checks exact values, both scanline encodings, orientation, retained texture storage and malformed-input atomicity.

## RGBE to sRGB

`RGBEToSRGB` decodes the base-level RGB9E5 mantissas and shared exponent, applies the linear-to-sRGB transfer, and quantizes to owned RGB8 bytes. HDR components above the byte range saturate. The operation retains the source image dimensions and leaves its bytes and notifications untouched.

When the source has mipmaps, the result generates every lower level from the new RGB8 base with the byte mip filter. A base-only source yields a base-only result. The image-storage test checks an authored HDR mip that differs from the regenerated value, an odd-width base, exact transfer and clamp bytes, and the independent ImageTexture snapshot.

## Image analysis

All four queries read an owned snapshot and ignore authored mip levels. They never publish pixels or emit `Changed`. Empty images return `DetectAlpha.None` and `IsInvisible = true`; channel and bounds queries reject empty or compressed storage with `InvalidOperationException`. Disposed images reject every query.

`DetectAlpha` uses byte thresholds in LA8/RGBA8: 0–1 selects Bit, 2–253 selects Blend, and 254–255 selects None unless another pixel needs transparency. Blend takes precedence. Other decoded alpha layouts use exact endpoints, with 65535 opaque for RGBA16I. This extends detection to packed, float, half and UInt16 storage. Compressed metadata reports Blend only for DXT3/DXT5 and None for every other block role; it does not certify decoded opacity.

`DetectUsedChannels` defaults to Generic (0); Srgb is 1 and Normal is 2. Normal returns RedGreen after storage validation. L8 and single-red formats retain their storage identity. Other formats use exact RGB equality for luminance, color components above 0.001, and alpha below 0.999 (below 65535 for RGBA16I). Non-gray color falls back to Red even below the component threshold. Srgb promotes detected Red/RedGreen color to RGB, except for the single-red storage shortcut. BCn calls this same analysis before selecting its block role. PNG uses the same alpha analysis on its export snapshot; its four direct byte layouts retain their channels.

`IsInvisible` requires every decoded alpha to equal zero; signed zero qualifies, negative and nonfinite alpha do not. `GetUsedRect` includes only pixels with alpha greater than zero, excludes NaN, and returns exact half-open bounds. Alpha-free layouts occupy the complete rectangle. These exact visibility queries intentionally differ from alpha classification thresholds.

The public `tests/ImageAnalysis.Consumer` exercises all 47 storage formats, enum identities/defaults, thresholds, authored mips, query purity, disposal and PNG/BCn integration. Host results and remaining platform limits are recorded in `.tasks/qa/T-0062` at the board root. This managed slice does not qualify native delivery or other platforms.

## Color adjustment and image metrics

`AdjustBCS` multiplies RGB by brightness, interpolates from 0.5 using contrast, then interpolates from the resulting RGB average using saturation. Every stored mip is transformed in its own format; alpha is retained. Finite factors may be negative or exceed one. Pixel encoding applies the format's existing quantization and range rules, including float HDR/nonfinite propagation. Empty/compressed storage and nonfinite factors fail before publication. The shared mutation gate publishes a complete copied buffer before one `Changed` event; subscriber failures leave the result committed.

`ComputeImageMetrics` reads snapshots and decompresses supported blocks without changing either source. It compares only the top-left common base-level rectangle, ignoring mips. Components are rounded to the nearest byte (halfway away from zero); luma uses integer Rec. 709 weights 13938/46869/4729 with rounding. RGBA adds four absolute errors per pixel and luma adds one, but **both modes divide their sums by four times the common pixel count**. Maximum is the largest histogram error, MeanSquared uses squared errors, and RootMeanSquared is its square root. PSNR is `20 log10(255 / RMS)`, clamped to 0..500 for nonzero error; identical images return `1e10`.

Half-float/shared-exponent storage after decoding is rejected. Empty images and any compared nonfinite or nonnormalized RGBA component raise `InvalidOperationException` in either mode; validation covers only the common area. These safety guards replace unsafe numeric indexing, including the reference luma branch's incomplete validation. The result is a typed `ImageMetrics`; queries emit no events. Decompression follows the existing bounded codec errors and native prerequisites. The public consumer is `tests/ImageColorMetrics.Consumer`; full platform qualification remains separate.

## Alpha-edge repair and premultiplication

Both operations edit the RGBA8 base level and preserve all authored mip bytes. Edge repair copies the nearest RGB whose alpha byte is at least 20 into each pixel below that threshold. It searches up to four pixels on each axis, measures squared distance, and visits candidates in top-to-bottom, left-to-right order. Repaired pixels remain below the threshold and cannot become sources. Premultiplication applies integer `(component * alpha + 255) >> 8` to each base RGB byte. Both operations keep alpha values.

`DPITexture` can repair transparent edges in its rasterized SVG and then premultiply the result before building a texture variant. The image storage and SVG texture profiles check authored mip retention, threshold and search bounds, and the combined processing order.

## Normal-map conversion

`BumpMapToNormalMap` samples red or luminance in the base image and wraps forward differences at the right and bottom edges. It encodes the normalized tangent-space vector in opaque RGBA8 and drops the old mip chain. Finite heights and scale retain their direction when float gradient arithmetic overflows. A gradient involving a nonfinite height encodes the zero vector. `NoiseTexture` uses this operation before generating its mip chain.

`NormalMapToXY` quantizes channels to RGBA8 as needed, then writes Y to luminance and X to alpha in LA8. It preserves independently authored levels. `ImageTexture` retains the packed source bytes.

The image storage profile checks exact wrapping and packing bytes, extreme and nonfinite inputs, and texture snapshots on Windows x64 and Linux x64 under WSL. On Windows x64 the noise image profile matches 122 pinned image cases and 20 RGBA kernel cases.

## PNG loading and saving

The file and buffer APIs decode to L8, LA8, RGB8 or RGBA8 base pixels. Indexed samples expand to RGB, low-bit grayscale expands to full-range bytes, and a transparency key or palette alpha expands to straight alpha. Interlaced images reconstruct all rows. libpng converts to the sRGB transfer encoding using the PNG transfer metadata. Samples without transfer metadata use the sRGB default, including 16-bit input. Alpha is scaled independently, and transparency keys compare at the original bit depth. The image owns pixels only.

PNG export uses a stable base-level snapshot. L8, LA8, RGB8 and RGBA8 keep their channels and bytes. Supported blocks are decompressed on that snapshot. Other layouts convert to RGB8 or RGBA8 based on base-level alpha. Export writes 8-bit samples tagged sRGB and shares the atomic file replacement path with the other image encoders.

Encoded input, decoded pixels, source pixels for encoding, and encoded output each have a 64 MiB ceiling. Native storage uses the common allocation ledger. PNG header and chunk structure are checked before native decode. A malformed file or allocation failure preserves the destination image, including its format and mip levels. Existing texture consumers receive the decoded layout through the standard ImageTexture path. The unified module reuses the FreeType libpng pin on desktop and Android and the existing packaged libpng archive for static Apple and Web builds.

## JPEG loading and saving

The file and buffer loaders produce L8 for single-component JPEG and RGB8 for three-component JPEG. Baseline and progressive scans use the platform's SDL_image decoder. The image API validates the marker stream through the first end-of-image marker, skips marker payloads and stuffed scan bytes, and checks dimensions and the 64 MiB decoded-pixel limit before native decode. Unsupported sample precision and CMYK/YCCK reject explicitly. Malformed data preserves the previous image. Texture loading uses the same channel layout.

Encoding copies the base level, decompresses supported GPU blocks, and converts to RGB8. It discards alpha and leaves the source, mipmaps and notifications untouched. Source pixels, converted RGB8 and returned bytes are each bounded to 64 MiB. The finite quality interval is 0.01–1.0. The engine truncates to an integer percentage and SDL_image applies its native quality mapping. File saving replaces the destination atomically after successful encoding.

`ImageCodecTests` exercised odd-width grayscale, progressive RGB, failure atomicity, BC1 export, and the file/texture paths on Windows x64. The AVI GPU test wrote and decoded real MJPEG frames. Other target packages and NativeAOT remain to be qualified.

## BMP loading

`LoadBMPFromBuffer`, `Load` and `LoadFromFile` share one managed decoder. The built-in ImageTexture loader and clipboard `image/bmp` offer use the same path. The result is an owned, straight RGBA8 base level in top-left order.

- Core headers use three-byte BGR palette entries. Info headers use four-byte palette entries with opaque output. Indexed depths are 1, 2, 4 and 8 bits, with high bits first and DWORD-aligned raw rows.
- Direct color includes packed RGB555, 24-bit BGR and 32-bit BGRA. Sixteen- and 32-bit bitfields use nonoverlapping contiguous masks, scaled to bytes by nearest rounding. An absent alpha mask produces opaque pixels. Explicit alpha masks preserve transparent pixels. For unmasked 32-bit data, the high bytes are preserved if any is nonzero. An entirely zero high byte is treated as unused padding.
- RLE4 and RLE8 accept encoded and absolute packets, word padding, end-of-line, end-of-bitmap and delta escapes. Skipped pixels use palette entry zero. Raw rows honor both height signs. RLE uses the bottom-up origin. [Bitmap info header](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/ns-wingdi-bitmapinfoheader), [bitmap compression](https://learn.microsoft.com/en-us/windows/win32/gdi/bitmap-compression).
- Input and RGBA output each have a 64 MiB cap. Header, palette, mask, row, packet and pixel-offset checks stay within the declared file and supplied bytes. A zero declared file size uses the supplied length. Unsupported compression and invalid data raise `InvalidDataException`, preserving the current pixels and mipmaps. A successful load publishes one change.

Coverage concerns decoded pixels. Embedded JPEG/PNG, OS/2 Huffman or RLE24, CMYK and color-profile conversion are outside this profile. The managed test exercises the buffer and file paths and the retained ImageTexture. Native clipboard transport has its own [platform checks](clipboard-images.md#verification-and-reproduction).

## TGA loading

File, buffer and texture loading use one managed decoder. It expands raw and run-length encoded true color, grayscale and indexed samples to straight RGBA8 in top-left row order. All four origin combinations are supported. Packed 15/16-bit color keeps the five high bits in each output channel. The declared 16-bit alpha bit and 32-bit color alpha are preserved. The decoder also supports 16-bit grayscale with byte alpha and 8/16-bit palette indices with a nonzero first entry.

Encoded input and decoded pixels each have a 64 MiB limit. The decoder checks the palette and packet ranges, permits packets that span rows, and rejects a run or literal packet that overruns the image. Optional footer metadata does not contribute pixel data. Interleaved images reject explicitly. Failed decode preserves the destination image, mipmaps and notification count.

`ImageCodecTests` exercised buffer, file and texture loading on Windows x64 and Linux x64 under WSL. Package, NativeAOT and system clipboard checks remain separate.

## SVG viewport sizing

SVG file, byte and string inputs resolve root width and height independently from presentation attributes, embedded CSS rules and the root inline style. A styled declaration overrides the presentation attribute. Among styled declarations, importance wins first, followed by inline style, selector specificity and source order. A selector list contributes the highest specificity among its matching selectors.

The viewport profile reads same-namespace CSS `<style>` elements in document order. It matches the root against type, class, ID, universal and `:root` selectors, including compounds and comma-separated lists. An empty list entry causes the loader to skip the whole rule. Type, class and ID names are case-sensitive. It skips unsupported selector forms and at-rules. Style elements can omit `media` or use `all`.

CSS comments preserve token boundaries. For example, `svg/**/.tile` matches a root SVG with class `tile`, while `svg/**/ .tile` includes a descendant combinator outside the viewport profile. A comment inside a value can split a dimension into separate tokens, so `12/**/px` fails length validation. See [CSS Syntax comment tokenization](https://www.w3.org/TR/css-syntax-3/#consume-comments).

Positive numeric, pixel and physical lengths use the 96 DPI profile. Percentages resolve against the corresponding viewBox axis or the 300×150 default viewport, and styled `auto` uses 100%. Only CSS whitespace can surround a dimension value. A nonbreaking space makes the value invalid. The winning declaration must pass length validation. A missing axis retains the viewBox-ratio or default fallback. The raster scale applies once, with integer rounding and a 16,384-pixel axis limit.

The winning width or height can use calc(), min(), max() or clamp() in any of the three sources. Inside a function, unitless values are numbers. Percentages convert to lengths using the same axis base as a plain percentage. Addition, subtraction and comparisons require matching dimensions. Multiplication and division combine dimensions, so a length divided by a length is a number. Multiplication and division bind before addition and subtraction. Binary + and - require whitespace on both sides. Parentheses and nested functions can change the evaluation order, up to 64 levels. When clamp() has inverted bounds, its lower bound wins. The result must be a positive finite length. Invalid tokens, incompatible dimensions, division by zero and nonfinite intermediates fail validation. See [CSS Values 4 math syntax](https://www.w3.org/TR/2024/WD-css-values-4-20240312/#math).

Styled root width and height resolve var() before parsing lengths or evaluating math. Custom property names are case-sensitive and use the same root cascade as the size declarations. References can chain and accept a fallback after the first comma, including an empty fallback. Missing or invalid custom properties use that fallback when supplied. References in unused fallbacks also count when detecting cycles. Every member of a cycle becomes invalid. A substituted value keeps its token boundaries and must satisfy the ordinary size rules. An invalid winning size fails loading even when a lower-priority size exists. The 64-level limit applies to both dependency chains and expansion. Dependency chains include references in custom property fallbacks, regardless of which value the substitution selects or which axis is evaluated first. Each group of mutually dependent properties counts every member once. A participating value is limited to 16,384 UTF-16 code units. Both axes share a work budget of 1,048,576 code units charged for scans and copies. Exceeding a limit fails loading. See [CSS Custom Properties](https://www.w3.org/TR/css-variables-1/).

For styled or calculated dimensions, the loader passes a normalized integer viewport to SDL_image with the authored viewBox or a logical viewBox derived from the resolved dimensions. Explicit aspect-ratio settings are preserved and the default is centered meet. Image and texture-file loading use the same decode path. [DPITexture](scalable-svg.md) also uses the shared sizing rules when validating source and regenerating raster variants. Failed validation or decoding preserves the previous image and emits no change notification.

On Windows the SVG codec profile checks exact raster dimensions and pixels for the root cascade, scaled buffer and file paths, texture-file loading, DPITexture raster variants and rollback. DPITexture tests cover styled source through typed archive reload and rendered GPU and Compatibility viewport variants at unit, twofold and threefold scale. The two renderer profiles each pass 64 warmed frames at zero measured managed allocation.

The viewport cascade covers root width and height. Remaining SVG work includes external and conditional stylesheets, escaped identifiers, combinators, other pseudo-classes, font-relative and viewport-relative units, variables in presentation attributes and other properties, other CSS math functions and named numeric constants, and nested viewport and renderer fidelity. The [SVG sizing properties](https://www.w3.org/TR/SVG2/geometry.html#Sizing) and [SDL_image viewport scaling](https://github.com/libsdl-org/SDL_image/blob/release-3.4.6/src/IMG_svg.c#L96-L148) describe the source behavior at each boundary.

## Exclusions and deferred integration

PNG/JPEG/WebP/BMP/TGA/SVG/DDS/EXR loading and PNG/JPEG/WebP/EXR saving are executable. BC1–BC7 compression/decompression and DDS file/buffer load/save feed existing resource and texture consumers. PNG preserves the decoded luminance or RGB layout and alpha. JPEG preserves grayscale or RGB. WebP, BMP, TGA and SVG produce RGBA8; DDS retains supported byte/BCn storage and EXR retains HDR channels and precision. Encoded input/output and decoder dimensions follow the documented codec budgets. The private WebP encoder supports exact RGB/RGBA and lossy saving. KTX1/KTX2 and Basis LDR/HDR loading now execute through the [private KTX reader](image-ktx.md), retaining complete mips and applicable precision. Importing and complete color/metadata semantics remain pending. [ADR 0039](../decisions/resources.md#deferred-coverage-and-exact-implementation-triggers) records exact dependencies. BC1–BC7 and RA-as-RG decode; S3TC/BPTC encode. [ETC/EAC/ASTC decoding](image-block-decoding.md) now supplies all remaining block roles; their encoding remains unavailable.

## Verification

`tests/Electron2D.Tests/Program.cs` covers format and block sizes, mip offsets, copy isolation, invalid dimensions/data, every processing family and interpolation mode, clipping/masking, normal/HDR helpers, typed metrics, observer failures, duplication, descriptors, and disposal. Release build and generated XML checks are part of the full gate.

`BitMapTests` covers the new mask contract, including contour crossings and resource duplication.

Native `ImageCodecTests` verifies the six loaders, PNG/JPEG saving, SVG scale and XML rejection, failure atomicity, concurrent calls, virtual paths and file replacement. The PNG profile checks all four layouts, 16-bit gamma and transparency, indexed alpha, Adam7, IDAT/IEND corruption, used alpha, BC export and LA8 through ImageTexture. It checks that tracked native storage returns to baseline after success and failure. The updated codec ran on Linux x64 with the pinned libpng source. The earlier GPU texture tests used the preceding codec. The changed native ABI still needs package, AOT and full target qualification under ADR 0021.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0013: Managed typed Resource contract](../decisions/resources.md#adr-0013)
- [0014: Managed Resource lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0021: Runtime and editor target platforms](../decisions/product.md#adr-0021)
- [0024: Typed color values](../decisions/core-math.md#adr-0024)
- [0035: Foreseeable type-family completeness](../decisions/core-math.md#adr-0035)
- [0039: Managed image buffers and codec boundaries](../decisions/resources.md#adr-0039)


## Custom image decoder integration

[ImageFormatLoader](../classes/ImageFormatLoader.md) and [ImageFormatLoaderExtension](../classes/ImageFormatLoaderExtension.md) provide caller-owned file decoder registration. Custom suffixes participate in Image loading, resource extension/existence/directory discovery, synchronous/threaded ImageTexture loading and cache replacement. Ordered callbacks execute outside the registry lock with borrowed temporary Image/FileAccess objects; false means unrecognized content, exceptions preserve the destination, and success copies pixels before one Changed notification. Encoded input retains the 64 MiB limit. Registration is process-local and must precede fresh-host file loading. Keep decoders alive through pending worker calls; removal affects subsequent snapshots. Built-in buffer codecs and their channel/color metadata limits remain separate.

## BC and DDS integration

BC1–BC7 sources retain their original format, complete mip chain and exact block bytes. Prepared CPU sampling decodes BC1–BC5/BC7 to RGBA8 and signed/unsigned BC6 to RGB floating point. GPU allocates native BC storage when the device supports it; otherwise it uploads prepared decoded pixels. Compatibility uses the decoded snapshot. The RA-as-RG role uses decoded upload to preserve channel semantics. ETC/EAC/ASTC use the shared decoded upload path; signed EAC and ASTC HDR retain floating-point precision. [Unsigned image sampling](integer-textures.md) now executes through prepared RGBA16 integer storage.

See [the complete block-codec slice](image-bcn.md) for bounded source/expansion rules, source provenance, independent fixtures and current platform evidence. The buffer-tool closure exists only to compile the selected codec APIs inside Electron2D.dll; consumers receive no backend types or managed dependency DLLs.

WebP lossless/lossy saving, live codec settings, compressed-source conversion, atomic files and texture/capture consumers are described in [the executable codec contract](../components/image-webp.md). Foreign-target native execution and prebuilt bundle publication remain unverified.

The [EXR I/O slice](../components/image-exr.md) supplies precision-preserving HDR file/buffer load and lossless save. Image and texture resource discovery includes DDS and EXR alongside PNG/JPEG/WebP/BMP/TGA/SVG; typed archives also expose their registered extensions. GPU float sampling, explicit Compatibility byte conversion and float-driver rejection are separate exercised profiles.

[UInt16 image texture sources](integer-textures.md) now retain all four original layouts and mip chains, use raw unsigned RGBA16 GPU storage, validate ordinary/layered sampler scalars and reject unsupported integer drawing on Compatibility.

Runtime [portable texture encoding](portable-textures.md) now supplies Image.Compress/CompressFromChannels for ETC1, ETC2/EAC and ASTC 4×4/8×8 LDR/HDR alongside S3TC/BPTC. All channel policies, complete mips and bounded atomic publication share the existing Image contract. Basis UASTC creation is exposed through PortableCompressedTexture.

[Clipboard image ingestion](clipboard-images.md) uses these same buffer decoders
and encoded-input limit. The native buffer retires after the independent Image
copy; returned precision and layout follow the selected decoder. Clipboard queries
remain cold display-owner operations and do not add a file-path interpretation.
