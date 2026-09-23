# Material

Last updated: 2026-09-23

- Declaration: `public abstract class Material : Resource`
- Source: [Material.cs](../../src/Scene/Resources/Material.cs)
- Inherits: [Resource](Resource.md)
- Inherited by: [ShaderMaterial](ShaderMaterial.md), [CanvasItemMaterial](CanvasItemMaterial.md)
- Component: [Shader materials](../components/shader-materials.md)

## Description and API

The base resource for selecting canvas shading. Its `private protected Material()` constructor restricts executable material implementations to the runtime assembly. There is no added public state or consumer extension hook. Use ShaderMaterial for programmable fragments or CanvasItemMaterial for fixed blend modes, then assign the resource to `CanvasItem.Material`.

Nodes borrow materials. Their disposal does not dispose a shared material. Resource identity, synchronous `Changed`, graph copying and logical disposal follow the inherited Resource contract. The internal render state contains shader code/layout and uniform buffers, without exposing SDL handles to consumers.

## Limits and verification

Render priorities, next-pass chains and editor shader inspection remain absent. ShaderMaterial is verified by resource and GPU frame checks in [RenderingRuntimeTests.cs](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs); CanvasItemMaterial is verified by [native blend checks](../../tests/Electron2D.Tests/CanvasMaterialRenderingTests.cs). Native verification covers Linux Wayland/Vulkan and the compatibility hardware backend; software accepts Mix only. See [ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0014](../decisions/resources.md#adr-0014).
