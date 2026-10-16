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
2. Reads take a consistent per-image state. `GetData()` returns a copy.
3. A bulk mutation serializes with other mutations, clones the current buffer, computes and validates a complete next state, commits once, releases image locks, and emits `Resource.Changed`. `SetPixel` updates one pixel under the same serialization without allocating a replacement buffer.
4. Uncompressed pixels decode to `Color`; writes quantize or preserve floating/integer values according to `Image.Format`.
5. Mipmap generation averages 2×2 samples to a complete chain; resize, crop, rotation, and region composition preserve or rebuild mip policy as documented.
6. Duplication copies the complete buffer and inherited Resource configuration but not external path identity.
7. Disposal drops the managed buffer, clears image state, then completes inherited Resource disposal.
8. BitMap can classify a copied Image alpha plane, edit packed bits, dilate or erode a region, and extract marching-squares contours without a renderer.

## Dependencies and interactions

Color-space conversions use `SRGBToLinear`, `LinearToSRGB` and `RGBEToSRGB`, following the same acronym spelling as the underlying Color API. Their pixel conversion and format contracts are unchanged.

The component depends on `Resource`, typed property descriptors, `Color`, `Vector2i`, `Rect2i`, and BCL binary/numeric primitives. BitMap consumes copied Image alpha data and uses Vector2 contours; it needs no native library. ImageTexture consumes its copied raw buffer and format metadata for GPU upload. `FileAccess` supplies encoded bytes through existing virtual paths. SDL3-CS provides internal native decoding/encoding; temporary surfaces are copied and released before Image commits. PNG decoding uses the already delivered SDL core decoder because SDL_image 3.4.6 corrupts grayscale and RGB16 colors; JPEG/WebP/BMP/TGA decoding and PNG/JPEG/WebP encoding use SDL_image. CPU processing retains its managed-only path.

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

## Exclusions and deferred integration

The dynamic image-data dictionary is permanently replaced by typed properties plus `GetData`/`SetData`; error-code returns are replaced by exceptions; metric dictionaries are replaced by `ImageMetrics`.

PNG/JPEG/WebP/BMP/TGA/SVG/DDS/EXR loading and PNG/JPEG/WebP/EXR saving are executable. BC1–BC7 compression/decompression and DDS file/buffer load/save feed existing resource and texture consumers. The SDL_image load profile normalizes ordinary files to RGBA8; DDS retains supported byte/BCn storage and EXR retains HDR channels and precision. Encoded input/output and decoder dimensions follow the documented codec budgets. The private WebP encoder supports exact RGB/RGBA and lossy saving. KTX1/KTX2 and Basis LDR/HDR loading now execute through the [private KTX reader](image-ktx.md), retaining complete mips and applicable precision. Importing and complete color/metadata semantics remain pending. [ADR 0039](../decisions/resources.md#deferred-coverage-and-exact-implementation-triggers) records exact dependencies. BC1–BC7 and RA-as-RG decode; S3TC/BPTC encode. [ETC/EAC/ASTC decoding](image-block-decoding.md) now supplies all remaining block roles; their encoding remains unavailable.

## Verification

`tests/Electron2D.Tests/Program.cs` covers format and block sizes, mip offsets, copy isolation, invalid dimensions/data, every processing family and interpolation mode, clipping/masking, normal/HDR helpers, typed metrics, observer failures, duplication, descriptors, and disposal. Release build and generated XML checks are part of the full gate.

`BitMapTests` covers the new mask contract, including contour crossings and resource duplication.

The managed checks are Linux/.NET 8 only. Native `ImageCodecTests` separately verifies all six integrated loaders, including SVG string/buffer scaling and XML rejection, PNG/JPEG saves, header/decoder failure atomicity, independent concurrent calls, virtual paths and atomic file failures. RenderingTextureTests verifies decoded PNG pixels in the fourteenth GPU frame for each source language. Visual-quality, memory-pressure, AOT and other-target acceptance remain pending.

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

BC1–BC7 sources retain their original format, complete mip chain and exact block bytes. Prepared CPU sampling decodes BC1–BC5/BC7 to RGBA8 and signed/unsigned BC6 to RGB floating point. GPU allocates native BC storage when the device supports it; otherwise it uploads prepared decoded pixels. Compatibility uses the decoded snapshot. The RA-as-RG role uses decoded upload to preserve channel semantics. ETC/EAC/ASTC use the shared decoded upload path; signed EAC and ASTC HDR retain floating-point precision. Integer sampling remains separate.

See [the complete block-codec slice](image-bcn.md) for bounded source/expansion rules, source provenance, independent fixtures and current platform evidence. The buffer-tool closure exists only to compile the selected codec APIs inside Electron2D.dll; consumers receive no backend types or managed dependency DLLs.

WebP lossless/lossy saving, live codec settings, compressed-source conversion, atomic files and texture/capture consumers are described in [the executable codec contract](../components/image-webp.md). Foreign-target native execution and prebuilt bundle publication remain unverified.

The [EXR I/O slice](../components/image-exr.md) supplies precision-preserving HDR file/buffer load and lossless save. Image and texture resource discovery includes DDS and EXR alongside PNG/JPEG/WebP/BMP/TGA/SVG; typed archives also expose their registered extensions. GPU float sampling, explicit Compatibility byte conversion and float-driver rejection are separate exercised profiles.
