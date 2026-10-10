# Image-array resources and sampling

Last updated: 2026-10-10

## Scope and types

TextureLayered is a separate Resource branch for independently sampled arrays, with typed virtual getters. ImageTextureLayered owns homogeneous copied pixels; TextureArray is the concrete array role. PlaceholderTextureLayered/PlaceholderTextureArray retain metadata only. The shared TextureLayered.LayeredType has Array=0 and also supplies RenderingServer's role argument. Ordinary Texture retains its drawing API.

## Publication, metadata and lifetime

CreateFromImages copies nonempty images sharing dimensions, source format and mipmap configuration; failure preserves prior data and the stable RID. UpdateLayer replaces one compatible layer, retains native allocation and leaves other layers unchanged. Callbacks run after publication outside the resource lock; callback failure retains committed pixels. GetLayerData returns caller-owned copied pixels. Inputs can be disposed after creation. Resource state is synchronized; custom getter implementations synchronize their own producer state and emit Changed to invalidate cached snapshots.

Source plus upload pixels are bounded to 256 MiB before copying; native device limits fail explicitly at allocation. Placeholder Size writes notify even when equal, while Layers writes stay silent. Arbitrary integer metadata survives without image allocation. GetLayerData returns null for any placeholder index. Sampling uses one diagnostic checkerboard layer; its metadata does not guarantee shader dimensions/coordinates.

Duplication shares immutable snapshots but future publication is independent. Placeholder properties have typed stored descriptors. Standalone archives store all original source layers, including their mip chains; uninitialized arrays and placeholders have their own payloads/factories. Cache replacement uses existing Resource.CopyFromResource. Pixels and source-image lifetime never transfer to the texture.

## Shader and GPU integration

Both GLSL sampler2DArray and HLSL Texture2DArray<float4> use the common validated SPIR-V fragment path. Array images are distinct from arrays of descriptor bindings. Named samplers use set two with contiguous bindings and linear/base-level/clamp sampling. Reserved TEXTURE and SCREEN_TEXTURE remain ordinary images.

ShaderMaterial.SetShaderLayeredParameter/GetShaderLayeredParameter and Shader.SetDefaultLayeredTextureParameter/GetDefaultLayeredTextureParameter preserve typed borrowed resources and existing ordinary accessor signatures. Typed property descriptors store TextureLayered. Overrides win over defaults; reload keeps only matching sampled shape. Missing/disposed resources fail before drawing.

GPUCanvasBackend uses the existing device with a separate array cache. GPUTexture allocates a real SDL image array, copies every layer/mip through prepared transfer storage, cycles the native version once, and commits the uploaded version after successful submission. Named sampling still clamps LOD to base level; mip payload is independently verified by native readback. Ordinary viewport sampler dependencies keep their existing path; copied image arrays have no native render-target dependency. Compatibility rejects shader use. Unverified backend array upload/view support remains an explicit prerequisite.

## Server identities and proxies

Texture2DLayeredCreate owns copied layers until FreeRID/shutdown; Texture2DLayeredPlaceholderCreate owns a real diagnostic layer. Texture2DLayerGet accepts borrowed resources and owned arrays/proxies. Texture2DUpdate uses the layer argument for arrays. Format/path, replacement and disposal share the existing registry. Same-role replacement consumes the source identity and redirects its aliases.

Layered aliases support nested creation with iterative resolution. Retarget requires a same-role non-proxy source; an absent source fails explicitly on sampling/read. Aliases never own sources. Wrong-role or borrowed mutations are rejected. Public API exposes no SDL handles and introduces no second renderer.

## Example

```csharp
using var first = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
using var second = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
first.Fill(Colors.Red); second.Fill(Colors.Blue);
using var array = new TextureArray();
array.CreateFromImages([first, second]);
TextureLayered layers = array;
using var copy = layers.GetLayerData(1); // Caller-owned pixels.
```

## Verification and limits

[TextureArrayTests](../../tests/Electron2D.Tests/TextureArrayTests.cs) checks homogeneous copied layers, atomic update/failure/callback behavior, custom producers, typed descriptors/defaults/overrides, incompatible reload, duplication, archives/placeholders and 1000 prepared reads with zero owner-thread managed bytes. [TextureArrayRenderingTests](../../tests/Electron2D.Tests/TextureArrayRenderingTests.cs) checks HLSL/GLSL and owned-RID output, every mip of two physical GPU layers, compatible allocation reuse, HDR reallocation, proxy nesting/retarget/replacement, guards and cleanup. Linux Wayland GPU passes 24 warmup and 64 active frames with zero owner-thread managed bytes. Compatibility rejects shader use before drawing and releases the host.

Compressed/integer-sampled source formats, descriptor binding arrays, configurable named samplers, native allocation totals, foreign platforms, new-family trimming/AOT and owner acceptance remain separate. Cube sampling is outside the product boundary. See [array resources](texture-arrays.md), [ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0004](../decisions/product.md#adr-0004).
