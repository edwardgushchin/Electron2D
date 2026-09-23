# TexturePixels

Last updated: 2026-09-23

- Declaration: `internal sealed class TexturePixels`
- Source: [Texture.cs](../../src/Scene/Resources/Texture.cs)
- Component: [shader-materials](../components/shader-materials.md)
- Visibility: internal; unavailable to engine consumers.

## Description

The immutable pixel payload shared by ImageTexture and curve-texture snapshots and backend caches. Source preserves original format/mips; Upload contains a sampling-compatible copy. Array data is engine-owned and must never be mutated after publication. Allocation is the only mutable identity token, assigned before publishing an Update snapshot to preserve an existing GPU allocation.

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
| `internal static TexturePixels FromImage(Image image)` | [Capture](#capture) |
| `internal Image CopyImage()` | [Image copy](#image-copy) |

## Member descriptions

### Source

`internal readonly Image.State Source`

Copied original dimensions, format, mip flag and bytes.

### Upload payload

`internal readonly Image.State Upload`

Byte formats convert to RGBA8; other supported normalized/float formats convert to RGBA32Float, retaining HDR values.

### Levels

`internal readonly int Levels`

Total stored levels, including the base image; follows the complete halving chain to 1x1 when mipmaps exist.

### Allocation identity

`internal object Allocation`

New by default. Compatible ImageTexture.Update and curve-source rebakes copy the previous identity; SetImage or changed curve width/storage format creates a new one. RF/RGBF curve payloads retain original storage and expand to RGBA32Float for upload.

### Pixel stride

`internal int BytesPerPixel { get; }`

Four for RGBA8 uploads; sixteen for RGBA32Float uploads.

### Capture

`internal static TexturePixels FromImage(Image image)`

Copies a live Image under its snapshot contract and converts for upload. Rejects null, empty data, axes outside 1..16384 and compressed/integer-sampled formats. Image disposal propagates. Construction and conversion may allocate; this is not a frame replay operation.

### Image copy

`internal Image CopyImage()`

Returns a new caller-owned Image with copied original bytes and metadata; never exposes the shared snapshot array.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.
