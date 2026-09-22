# Material

Last updated: 2026-09-22

- Declaration: `public abstract class Material : Resource`
- Source: [Material.cs](../../src/Scene/Resources/Material.cs)
- Inherits: [Resource](Resource.md)
- Inherited by: [ShaderMaterial](ShaderMaterial.md)
- Component: [Shader materials](../components/shader-materials.md)

## Description and API

The base resource for selecting canvas shading. Its `private protected Material()` constructor restricts executable material implementations to the runtime assembly. There is no added public state or consumer extension hook. Use `new ShaderMaterial { Shader = shader }` and assign it to `Node.Material`.

Nodes borrow materials. Their disposal does not dispose a shared material. Resource identity, synchronous `Changed`, graph copying and logical disposal follow the inherited Resource contract. The internal render state contains shader code/layout and uniform buffers, without exposing SDL handles to consumers.

## Limits and verification

Render priorities, next-pass chains, editor shader inspection and texture integration are absent. The current public material is verified by the resource and GPU frame checks in [RenderingRuntimeTests.cs](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs). Native verification covers Linux Wayland/Vulkan. See [ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0014](../decisions/resources.md#adr-0014).
