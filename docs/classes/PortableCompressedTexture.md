# PortableCompressedTexture

Last updated: 2026-10-18

Source: [PortableCompressedTexture.cs](../../src/Scene/Resources/PortableCompressedTexture.cs).
Inherits [Texture](Texture.md). Component: [Portable compressed textures](../components/portable-textures.md).

```csharp
public sealed class PortableCompressedTexture : Texture
```

Creates an ordinary sampled texture from an image at runtime. Creation encodes and prepares every mip before publishing the replacement. GetImage returns independent stored pixels: exact block format for block modes, converted image pixels for PNG/WebP, and decoded RGBA8 or RGB half for Basis. The resource's RID stays stable across replacement; the renderer owns its native storage. GetWidth/GetHeight/GetSize retain the source dimensions. SizeOverride changes the pixel dimensions used for source regions, nine-patch and repeat sampling. It does not resample pixels. Zero selects the corresponding source dimension; positive fractions truncate for rendering. Creation resets the override to source dimensions.

| Member | Contract |
| --- | --- |
| `PortableCompressedTexture()` | Empty image, zero dimensions/override, Lossless mode, no retained buffer; ResourceLocalToScene defaults false. |
| `CreateFromImage(Image image, CompressionMode compressionMode, bool normalMap = false, float lossyQuality = 0.8f)` | Copies and encodes a nonempty image, preserving complete mip chains. Quality must be finite in 0–1. Normal-map policy selects the Basis error metric. Matching block input passes through unchanged; mismatched block input rejects. |
| `GetCompressionMode()` | Last successful mode, initially Lossless. |
| `SetBASISUCompressorParams(int uastcLevel, float rdoQualityLoss)` | Next Basis creation uses effort 0–4 and LDR RDO 0–50; -1 selects defaults 2/0. HDR uses effort and ignores LDR RDO. |
| `KeepCompressedBuffer` | Retains the next encoded payload. False immediately discards the current payload, including one retained globally. True cannot recover discarded data. |
| `IsKeepingAllCompressedBuffers()` | Process-wide policy; initially false. |
| `SetKeepAllCompressedBuffers(bool keep)` | Affects subsequent creation/loading; existing buffers are unchanged. |
| `SizeOverride` | Finite dimensions from zero through 16384. Original image queries remain unchanged. |
| `HasAlpha` | Reports true only for LA8/RGBA8 encoded source format, including the original Basis source format rather than its RGBA transcoder output. Per-pixel opacity inspects prepared decoded alpha even for block formats. |

CompressionMode values are Lossless (0), Lossy (1), BasisUniversal (2), S3TC (3), ETC2 (4), BPTC (5), ASTC (6). Lossless/lossy PNG/WebP storage uses RGBA8 precision; alpha remains lossless for WebP. Lossless uses PNG when an axis exceeds WebP's 16383-pixel range. Basis stores UASTC LDR or RGB HDR in KTX2 with numeric channel values preserved. HDR Basis has opaque alpha. S3TC/BPTC/ETC2 select channel-appropriate blocks through Image.Compress; ASTC uses 4×4 LDR/HDR blocks. Imported matching ASTC 8×8 blocks are also accepted.

Populated archives require the encoded buffer retained before creation. Missing buffers produce an explicit save failure while preserving an existing destination. Typed resource/scene archives embed the bounded, versioned EPT1 envelope; no runtime input file or encoder executable is required. Duplicate and scene-local copies own independent pixels and retention state, including when no encoded buffer remains.

Source and encoded payloads are bounded to 64 MiB. Image's codec-work budget applies before block encoding/decoding. Errors before publication preserve pixels, mode, size and RID. PropertyListChanged precedes Changed outside the resource lock after publication; subscriber failure retains committed state. Readback/encoding are cold operations. Prepared ordinary rendering reuses immutable pixels.

Linux Wayland GPU and Compatibility checks and native/managed checks are described in the component page. Foreign platform execution and native allocation totals remain separate gates.
