# BlitMaterial

Last updated: 2026-10-18

- Declaration: `public sealed class BlitMaterial : Material`
- Source: [BlitMaterial.cs](../../src/Scene/Resources/BlitMaterial.cs)
- Inherits: [Material](Material.md), [Resource](Resource.md)
- Component: [Writable textures](../components/drawable-textures.md)

Copies each complete supplied source texture to the corresponding writable target, multiplying sampled channels by the call's modulation color. Missing sources use opaque white. This GPU material executes only through writable-texture blit operations; canvas use rejects.

| Member | Contract |
| --- | --- |
| `BlitMaterial()` | Creates source-alpha mixing. |
| `BlitBlendMode` | Mix, Add, Sub, Mul, Disabled. |
| `BlendMode` | Validated operation for every output; actual changes emit Changed. |

Mix performs source-alpha-over, Add/Sub use alpha-weighted source channels, Mul multiplies source/destination channels, and Disabled replaces the destination. Duplication and typed archives preserve the selected mode. The inherited RID is borrowed by blit consumers; disposing a target never disposes this material.
