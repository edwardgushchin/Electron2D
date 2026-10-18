# DrawableTexture

Last updated: 2026-10-18

- Declaration: `public sealed class DrawableTexture : Texture`
- Source: [DrawableTexture.cs](../../src/Scene/Resources/DrawableTexture.cs)
- Inherits: [Texture](Texture.md), [Resource](Resource.md)
- Component: [Writable textures](../components/drawable-textures.md)

## Description

Owns a writable sampled GPU image. `Setup` authors dimensions, color format, clear color and optional complete mip chain before or during a renderer session. First GPU use allocates and clears the image. The resource has a stable logical RID across setup; each native allocation has an independent lifetime. Shutdown retires native storage while borrowed resource objects retain setup for another session.

`BlitRect` scales the complete source into a destination rectangle on one mip level. Clipping preserves the original UV mapping; a negative size flips that axis. `BlitRectMulti` writes this target and up to three distinct equal-sized, equal-format targets. Sources and materials remain borrowed. Null, absent and atlas sources use opaque white. No sampled input, including material parameters and native/logical aliases, may share a target allocation. Configuration, disposal and recursive blit preparation reject during source callbacks.

## API

| Member | Contract |
| --- | --- |
| `DrawableTexture()` | Zero dimensions, RGBA8, no mip chain, white authored clear color. |
| `Setup(width, height, format, color, useMIPMaps)` | Dimensions 1–16384; finite color; clears replacement storage. |
| `DrawableFormat` | RGBA8, RGBA8SRGB, RGBAH, RGBAF. |
| `SetFormat(format)` | Changes storage format; initialized contents reset to authored clear color. |
| `SetUseMIPMaps(mipmaps)`, `GetUseMIPMaps()` | Changes/queries complete mip allocation; changing it resets initialized contents. |
| `BlitRect(rect, source, modulate, mipmap, material)` | One target, default source-alpha mixing. |
| `BlitRectMulti(rect, sources, extraTargets, modulate, mipmap, material)` | One through four targets and up to four supplied sources. |
| `GenerateMIPMaps()` | Regenerates lower levels from level zero; does nothing without lower levels. |
| `GetImage()` | Explicit synchronized independent snapshot including mip levels; null without setup or an active GPU renderer. |
| `GetWidth()`, `GetHeight()`, `PixelFormat`, `HasMIPMaps`, `MIPMapCount` | Actual setup metadata; alpha and pixel-opacity follow the inherited opaque texture policy. |

A 1×1 texture can request mip allocation while HasMIPMaps remains false and MIPMapCount is zero, since no lower level exists.

Duplication, scene-local copies and typed archives preserve setup and clear color with independent native storage; transient GPU results are not serialized. Format or mip changes reset content. CopyFromResource and ResetState retire transient native results, retaining authored clear setup. Compatibility rendering rejects writable texture sampling and blit execution. See the component page for shader, server ownership and verification details.
