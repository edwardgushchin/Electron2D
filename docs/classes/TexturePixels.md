# TexturePixels

Last updated: 2026-10-16

- Declaration: `internal sealed class TexturePixels`
- Source: [Texture.cs](../../src/Scene/Resources/Texture.cs)
- Component: [shader-materials](../components/shader-materials.md)
- Visibility: internal; unavailable to engine consumers.

## Description

The immutable pixel payload shared by ImageTexture, server-owned textures and generated texture snapshots and backend caches. Source preserves original format/mips; Upload contains a sampling-compatible copy. Array data is engine-owned and must never be mutated after publication. Allocation is the only mutable identity token, assigned before publishing an Update snapshot to preserve an existing GPU allocation.

## Internal usage

This fragment belongs inside the runtime and requires the surrounding owner state.

```csharp
var snapshot = TexturePixels.FromImage(image);
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal readonly Image.State Source` | [Source](#source) |
| `internal readonly Image.State Upload` | [Upload payload](#upload-payload) |
| `internal readonly int Levels` | [Levels](#levels) |
| `internal object Allocation` | [Allocation identity](#allocation-identity) |
| `internal int BytesPerPixel { get; }` | [Pixel stride](#pixel-stride) |
| `internal int PixelType { get; }` | [Sampled scalar](#sampled-scalar) |
| `internal static TexturePixels FromImage(Image image)` | [Capture](#capture) |
| `internal Image CopyImage()` | [Image copy](#image-copy) |

## Member descriptions

### Source

`internal readonly Image.State Source`

Copied original dimensions, format, mip flag and bytes.

### Upload payload

`internal readonly Image.State Upload`

Byte formats and BC1–BC5/BC7 decode to RGBA8; BC6 and other supported normalized/float formats convert to RGBA32Float, retaining HDR values. UInt16 formats expand to raw unsigned RGBA16. Source retains original blocks or integer lanes; CPU opacity uses source semantics or the prepared decoded payload. No decode runs during retained drawing.

### Levels

`internal readonly int Levels`

Total stored levels, including the base image; follows the complete halving chain to 1x1 when mipmaps exist.

### Allocation identity

`internal object Allocation`

New by default. Compatible ImageTexture.Update and curve-source rebakes copy the previous identity; SetImage or changed curve width/storage format creates a new one. RF/RGBF curve payloads retain original storage and expand to RGBA32Float for upload.

### Pixel stride

`internal int BytesPerPixel { get; }`

Four for RGBA8 uploads, eight for unsigned RGBA16 uploads and sixteen for RGBA32Float uploads.

### Sampled scalar

`internal int PixelType { get; }`

Two for raw unsigned integer upload and zero for float/normalized upload. This retained metadata feeds ordinary/layered shader validation, canvas pipeline selection and the Compatibility capability check without image copies during replay.

### Capture

`internal static TexturePixels FromImage(Image image)`

Copies a live Image under its snapshot contract and converts for upload. Rejects null, empty data, axes outside 1..16384 and formats without an integrated decoder. UInt16 sources expand into raw RGBA16 integer upload; block sources use prepared decoded upload or supported native BC storage. Image disposal propagates. Construction and conversion may allocate; this is not a frame replay operation.

Server texture updates use the same compatible allocation token; TextureReplace transfers the replacement snapshot/token rather than mutating immutable arrays.

### Image copy

`internal Image CopyImage()`

Returns a new caller-owned Image with copied original bytes and metadata; never exposes the shared snapshot array.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.

Prepared video RGBA snapshots additionally carry a Mutable role and UInt64 Revision. Publication copies into fixed source/upload buffers and advances the revision; CopySnapshot freezes independent pixels by default. Ordinary immutable roles keep existing sharing semantics.

[ETC/EAC/ASTC decoding](../components/image-block-decoding.md) now supplies prepared pixels for these texture snapshots while retaining original block formats, bytes and all mips. Signed EAC and ASTC HDR use floating-point upload; the current Compatibility driver rejects that precision. [Unsigned image sampling](../components/integer-textures.md) now preserves all four UInt16 source formats through native RGBA16 integer storage.

[UInt16 image texture sources](../components/integer-textures.md) now retain all four original layouts and mip chains, use raw unsigned RGBA16 GPU storage, validate ordinary/layered sampler scalars and reject unsupported integer drawing on Compatibility.
