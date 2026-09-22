# Images component

Last updated: 2026-09-22

## Scope

This Resources component owns portable managed 2D pixel buffers, raw format identity, mipmap layout, CPU image processing, and typed image metrics. It does not own file codecs, importing, textures, renderer handles, GPU compression, or presentation.

## Owned types

| Type | Role | Source |
| --- | --- | --- |
| [`Image`](../classes/Image.md) | Mutable managed pixel resource and processing API | [`Image.cs`](../../src/Core/IO/Image.cs), [`Image.Processing.cs`](../../src/Core/IO/Image.Processing.cs) |
| [`Image.Format`](../classes/Image.Format.md) | Raw uncompressed and GPU block-compressed layout identity | [`Image.cs`](../../src/Core/IO/Image.cs) |
| [`Image.Interpolation`](../classes/Image.Interpolation.md) | Resize reconstruction filter | [`Image.cs`](../../src/Core/IO/Image.cs) |
| [`Image.AlphaMode`](../classes/Image.AlphaMode.md) | Detected alpha classification | [`Image.cs`](../../src/Core/IO/Image.cs) |
| [`Image.UsedChannels`](../classes/Image.UsedChannels.md) | Detected minimal channel set | [`Image.cs`](../../src/Core/IO/Image.cs) |
| [`Image.CompressSource`](../classes/Image.CompressSource.md) | Channel-detection source semantics | [`Image.cs`](../../src/Core/IO/Image.cs) |
| [`Image.CompressMode`](../classes/Image.CompressMode.md) | Future block-compression family selection | [`Image.cs`](../../src/Core/IO/Image.cs) |
| [`Image.AstcFormat`](../classes/Image.AstcFormat.md) | Future ASTC footprint selection | [`Image.cs`](../../src/Core/IO/Image.cs) |
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

## Dependencies and interactions

Color-space conversions use `SRGBToLinear`, `LinearToSRGB` and `RGBEToSRGB`, following the same acronym spelling as the underlying Color API. Their pixel conversion and format contracts are unchanged.

The component depends on `Resource`, typed property descriptors, `Color`, `Vector2I`, `RectI`, and BCL binary/numeric primitives. Future texture upload consumes its copied raw buffer and format metadata. `FileAccess` may later supply encoded bytes to an approved codec layer, but Image currently has no dependency on file formats, SDL, GPU APIs, or third-party packages.

## Invariants and error behavior

- Populated dimensions are positive, within `MaxWidth`/`MaxHeight`, below 268,435,456 pixels, and representable by one managed byte array; oversized byte counts fail before allocation.
- The byte count exactly matches the base level and every declared mip level. Multi-byte fields are little-endian.
- The default zero-by-zero state is the only empty state.
- Operations that require decoded pixels reject empty or block-compressed data; raw compressed storage remains copyable and structurally queryable, while alpha classification uses format metadata where possible.
- Incoming/outgoing byte arrays never alias engine storage.
- Mutation failure before commit preserves the old state. Observer failure occurs after commit and does not roll state back.
- Source/mask images are snapshotted before destination mutation. No operation promises an atomic transaction across multiple images.
- Large processing calls allocate replacement buffers and are not intended for steady-state frame hot paths.

## Current implementation status

All 47 current raw format identities, the complete compression/source/ASTC enum family, exact base/mipmap sizing, 25 uncompressed pixel codecs, 22 raw compressed layouts, pixel access, format conversion, mipmaps, fill, crop/region, flip/rotation, five resize filters, blit/blend/masks, alpha/channel/visible-bound detection, color adjustments, alpha processing, sRGB conversion, normal/RGBE processing, metrics, typed descriptors, Resource duplication, concurrency boundaries, and disposal are implemented.

## Exclusions and deferred integration

The dynamic image-data dictionary is permanently replaced by typed properties plus `GetData`/`SetData`; error-code returns are replaced by exceptions; metric dictionaries are replaced by `ImageMetrics`.

File/buffer codecs, saving, GPU compression/decompression, texture upload, importing, resource loading, and native-backed asset leases are absent. [ADR 0039](../decisions/resources.md#deferred-coverage-and-exact-implementation-triggers) gives each row a concrete prerequisite and first required vertical slice. Raw DXT/RGTC/BPTC/ETC/ASTC bytes can be held but not decoded or encoded.

## Verification

`tests/Electron2D.Tests/Program.cs` covers format and block sizes, mip offsets, copy isolation, invalid dimensions/data, every processing family and interpolation mode, clipping/masking, normal/HDR helpers, typed metrics, observer failures, duplication, descriptors, and disposal. Release build and generated XML checks are part of the full gate.

Current execution is Linux/.NET 8 only. No codec, GPU, visual-quality, memory-pressure, AOT, or six-target claim follows from these checks.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0013: Managed typed Resource contract](../decisions/resources.md#adr-0013)
- [0014: Managed Resource lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0021: Runtime and editor target platforms](../decisions/product.md#adr-0021)
- [0024: Typed color values](../decisions/core-math.md#adr-0024)
- [0035: Foreseeable type-family completeness](../decisions/core-math.md#adr-0035)
- [0039: Managed image buffers and codec boundaries](../decisions/resources.md#adr-0039)
