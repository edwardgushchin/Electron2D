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

The component depends on `Resource`, typed property descriptors, `Color`, `Vector2i`, `Rect2i`, and BCL binary/numeric primitives. BitMap consumes copied Image alpha data and uses Vector2 contours; it needs no native library. ImageTexture consumes its copied raw buffer and format metadata for GPU upload. `FileAccess` supplies encoded bytes through existing virtual paths. SDL_image handles JPEG, BMP and SVG through SDL3-CS. The managed TGA decoder handles raw and run-length encoded pixels. Temporary surfaces are copied and released before Image commits. PNG and WebP use their private native codecs. CPU processing retains its managed-only path.

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

## Mip chain editing

`ShrinkX2` promotes the first stored mip and keeps the remaining bytes in order. It works on uncompressed pixels and complete block-compressed chains. When the promoted base is 1×1, the image reports no lower levels. Base-only uncompressed images halve through the generation filter and stay base-only.

`GenerateMIPMaps` averages 2×2 samples in storage space. Odd extents truncate and one-pixel axes repeat their sample. Byte and unsigned 16-bit channels round to the nearest value. Normal renormalization applies to byte, float, half and unsigned 16-bit RGB/RGBA layouts. It normalizes the averaged RGB and preserves the averaged alpha. Packed and one- or two-channel formats use ordinary averaging.

`ClearMIPMaps` retains the base bytes, including compressed blocks. ImageTexture captures the resulting format, base extent and exact remaining mip chain. The image-storage profile checks authored BC1 colors, odd and one-pixel axes, integer and half-float values, base edits, and observer failure after commit.

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

## TGA loading

File, buffer and texture loading use one managed decoder. It expands raw and run-length encoded true color, grayscale and indexed samples to straight RGBA8 in top-left row order. All four origin combinations are supported. Packed 15/16-bit color keeps the five high bits in each output channel. The declared 16-bit alpha bit and 32-bit color alpha are preserved. The decoder also supports 16-bit grayscale with byte alpha and 8/16-bit palette indices with a nonzero first entry.

Encoded input and decoded pixels each have a 64 MiB limit. The decoder checks the palette and packet ranges, permits packets that span rows, and rejects a run or literal packet that overruns the image. Optional footer metadata does not contribute pixel data. Interleaved images reject explicitly. Failed decode preserves the destination image, mipmaps and notification count.

`ImageCodecTests` exercised buffer, file and texture loading on Windows x64 and Linux x64 under WSL. Package, NativeAOT and system clipboard checks remain separate.

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
