# Unsigned image texture sources

Last updated: 2026-10-16

## Scope and storage

The four managed Image formats R16I, Rg16I, Rgb16I and Rgba16I now feed the existing
ordinary and layered texture pipelines. Despite their format suffix, these
source lanes are unsigned sixteen-bit integers. ImageTexture, TextureArray,
CompressedTexture/CompressedTextureArray imports, custom Texture/TextureLayered
producers and owned RenderingServer image identities share TexturePixels
preparation. No additional public type, service or shader dialect is introduced.

The immutable source retains its exact original format, byte order and full mip
chain. Prepared sampling expands each pixel into RGBA16 unsigned lanes. Missing
RGB lanes are zero and missing alpha is one; existing lanes preserve all bits,
including zero, one and 65535. Source plus eight upload bytes per pixel across
every mip count towards the existing storage budgets before copying. Image
queries, duplication and embedded archives preserve original source bytes.

GPUTexture allocates R16G16B16A16UINT ordinary/array storage when the active device
supports it. Every mip and layer uploads explicitly. Borrowed RenderingDevice
identities report this actual format, so byte readback and updates use exact
sixteen-bit lane sizes. Compatible image/layer updates retain native allocation;
whole replacement retires the old borrowed storage. Writing the borrowed device
image does not rewrite the authored CPU snapshot; later source publication
replaces its native contents according to the existing lifetime contract.

## Sampling and consumers

Default canvas rendering uses the existing unsigned shader. Samples expose raw
values: an integer one produces color one. They are not normalized by 65535.
Custom unsigned material samplers use the same source scalar classification as
native RD textures. Ordinary/array shape and float/signed/unsigned scalar checks
apply to defaults, overrides, server RID adapters, draw-time validation and reload
migration. Prepared snapshots classify custom producers without repeated image
reads. Atlas, proxy and CanvasTexture channel resolution retain their source role.

Direct integer filtering requires nearest without anisotropy. Named integer
samplers use nearest/base-level LOD clamp with clamp addressing; GLSL texelFetch
and HLSL Load address explicit stored mip levels. Arbitrary configurable named
samplers remain a separate capability. Existing scalar-aware built-in and
programmable lighting consumes raw diffuse, normal and specular channel values.
Opacity tests use raw sampled alpha above 0.1, including integer alpha one.
Image's CPU alpha blending/detection arithmetic retains its separate full-range
alpha normalization contract.

Compatibility cannot preserve integer sampling and rejects drawing before
submission. CPU creation/loading, metadata, copies and archives remain usable
without a capable renderer; unsupported native sampling never silently converts
integer values to normalized RGBA8.

## Public example

```csharp
using var image = Image.CreateEmpty(4, 4, false, Image.Format.R16I);
image.Fill(new Color(1, 0, 0));
using var texture = ImageTexture.CreateFromImage(image);
var sprite = new Sprite { Texture = texture, TextureFilter = TextureFilter.Nearest };
// Add sprite to a GPU-rendered scene. The raw red value one draws full red.
```

A custom shader uses usampler2D/usampler2DArray in GLSL or paired
Texture2D<uint4>/Texture2DArray<uint4> and SamplerState declarations in HLSL.
The existing pinned importer/reflection contract applies to both languages.

## Verification and limits

[Managed checks](../../tests/Electron2D.Tests/IntegerSourceTextureTests.cs) cover
all four formats, missing lanes, every mip, exact budget boundaries, opacity,
KTX1 little/big-endian rows, typed ordinary/layered scalar failures and reload,
worker imports, copies/embedded archives, atomic invalid updates, committed
observer failures and zero-allocation prepared scalar reads.

[Native checks](../../tests/Electron2D.Tests/IntegerSourceTextureTests.Rendering.cs)
compare six ordinary source roles against independent raw-valued float oracles;
GLSL/HLSL and server materials fetch original integer ordinary/array mip values.
Actual format/all-mip/all-layer readback, device writes, source updates,
replacement expiration, prepared custom producer reads, invalid linear filtering,
scalar mutation and Compatibility rejection execute on Linux Wayland/Vulkan.
Twenty-four warmup plus sixty-four active frames allocate zero managed bytes on
the owner thread. [IntegerLightingTests](../../tests/Electron2D.Tests/IntegerLightingTests.cs)
also substitutes ordinary UInt16 sources into all twenty-seven scalar channel
combinations for built-in and programmable lighting, with independent float
references and zero owner-thread bytes after warmup.

A local NuGet consumer using only public APIs verifies all four source formats,
complete ordinary/layered imports and embedded archives. Separate CoreCLR and
NativeAOT processes restore the archives after source deletion and check actual
GPU unsigned shader pixels. This is local Linux package evidence; it does not
establish publication or foreign-platform acceptance.

Native allocation totals, device-loss recovery, foreign backend execution,
performance targets and user acceptance are separate gates. See
[ADR 0028](../decisions/rendering.md#adr-0028),
[ADR 0039](../decisions/resources.md#adr-0039) and the
[coverage register](../coverage/index.md).
