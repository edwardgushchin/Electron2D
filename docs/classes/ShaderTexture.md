# ShaderTexture

Last updated: 2026-10-04

- Declaration: `internal sealed record ShaderTexture(string Name, int Binding)`
- Source: [ShaderProgram.cs](../../src/Servers/Rendering/ShaderProgram.cs)
- Component: [shader-materials](../components/shader-materials.md)
- Visibility: internal; unavailable to engine consumers.

## Description

One reflected float-sampled texture binding in descriptor set two. Name and Binding are positional record properties. Reflection validates unique names, contiguous bindings, supported image shape and paired HLSL image/sampler slots before constructing it.

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

Builds a stored Texture-valued ShaderMaterial descriptor backed by its texture parameter accessors, with null as revert/default-override value. Public property discovery omits the reserved canvas slot.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.

IsCanvasTexture identifies TEXTURE; IsScreenTexture identifies SCREEN_TEXTURE; IsEngineTexture excludes either from material/default descriptor access. Ordinary named texture bindings keep their validated contiguous resource layout. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots).
