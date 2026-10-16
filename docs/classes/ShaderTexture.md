# ShaderTexture

Last updated: 2026-10-16

- Declaration: `internal sealed record ShaderTexture(string Name, int Binding, bool IsArray = false, int PixelType = 0)`
- Source: [ShaderProgram.cs](../../src/Servers/Rendering/ShaderProgram.cs)
- Component: [shader-materials](../components/shader-materials.md)
- Visibility: internal; unavailable to engine consumers.

[Image-array resources](../components/texture-arrays.md) execute copied homogeneous layers, typed samplers/defaults, shape-aware reload, source archives, actual GPU layer/mip upload and owned/proxy RIDs. Ordinary texture drawing retains its separate resource branch. Current native evidence is Linux Wayland GPU; compatibility rejects shader use. Copied byte, normalized/float, block and unsigned-integer images execute; foreign/native-allocation acceptance remains separate.

## Description

One reflected float/signed/unsigned 32-bit scalar texture binding in descriptor set two. Name and Binding are positional record properties. Reflection validates unique names, contiguous bindings, supported image shape and paired HLSL image/sampler slots before constructing it.

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal bool IsCanvasTexture { get; }` | [Canvas texture](#canvas-texture) |
| `internal PropertyDescriptor Describe(string propertyName)` | [Descriptor](#descriptor) |

## Member descriptions

### Canvas texture

`internal bool IsCanvasTexture { get; }`

True only for the case-sensitive reserved name TEXTURE, which reflection requires at binding zero. The renderer supplies the command texture or white pixel; it is not a material parameter.

### Descriptor

`internal PropertyDescriptor Describe(string propertyName)`

Builds a stored Texture- or TextureLayered-valued ShaderMaterial descriptor backed by its texture parameter accessors, with null as revert/default-override value. Public property discovery omits the reserved canvas slot.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.

IsCanvasTexture identifies TEXTURE; IsScreenTexture identifies SCREEN_TEXTURE; IsEngineTexture excludes either from material/default descriptor access. Ordinary named texture bindings keep their validated contiguous resource layout. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots).

## Sampled shape

IsArray selects a TextureLayered descriptor and array-specific accessors. RequireShape rejects use through the other typed accessor family; Validate rejects disposed or wrong-role resources. Renderer-owned TEXTURE/SCREEN_TEXTURE stay ordinary images. Reload copies texture overrides only when Name, IsArray and PixelType still agree. ResourcePixelType reads prepared source snapshots, with explicit native bindings for RD resources; ordinary and layered UInt16 sources classify unsigned without per-frame copies.

[UInt16 image texture sources](../components/integer-textures.md) now retain all four original layouts and mip chains, use raw unsigned RGBA16 GPU storage, validate ordinary/layered sampler scalars and reject unsupported integer drawing on Compatibility.
