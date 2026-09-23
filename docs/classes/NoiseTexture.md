# NoiseTexture

Last updated: 2026-09-23

**Inherits:** [Texture](Texture.md) → [Resource](Resource.md) → [ElectronObject](ElectronObject.md)

- **Source:** [NoiseTexture.cs](../../src/Scene/Resources/NoiseTexture.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class NoiseTexture : Texture`
- **Component:** [Noise](../components/noise.md)

## Description

`NoiseTexture` turns a borrowed [Noise](Noise.md) resource into a readable two-dimensional texture. Settings and source `Changed` events invalidate a cached pixel snapshot and emit this texture's `Changed` event. The next `GetImage`, pixel metadata query or renderer capture regenerates the image. The texture owns its generated pixels but neither source resource. Without a noise source it has the configured logical size and no readable image.

Generation calls the noise source's virtual `GetImage` or `GetSeamlessImage`. It then optionally maps luminance through a borrowed [Gradient](Gradient.md), converts height to a wrapping normal map, and builds mipmaps, in that order. The source image must match the configured dimensions and have no mipmaps. A sampled image and every `GetImage` result have independent ownership. Successful copies follow the [Resource](Resource.md) graph policy; deep copying a custom noise source requires that source to support duplication.

## Example

This partial snippet assumes `noise` is a concrete `Noise` subclass supplied by the application:

```csharp
using var texture = new NoiseTexture
{
    Width = 256,
    Height = 256,
    Noise = noise,
    Seamless = true,
};
using Image? image = texture.GetImage();
```

## Constructor

| Declaration | Contract |
| --- | --- |
| `public NoiseTexture()` | Creates a 512×512 texture without noise pixels. |

## Properties

| Declaration | Default and effect |
| --- | --- |
| `public int Width { get; set; }` | 512; positive image width, at most 16,384. |
| `public int Height { get; set; }` | 512; positive image height, at most 16,384. |
| `public bool GenerateMipmaps { get; set; }` | True; generate the complete mip chain after other processing. |
| `public Noise? Noise { get; set; }` | Null; borrowed sampling source. |
| `public Gradient? ColorRamp { get; set; }` | Null; borrowed luminance-to-color source. |
| `public bool Seamless { get; set; }` | False; choose the noise source's seamless image path. |
| `public bool Invert { get; set; }` | False; invert the source samples. |
| `public bool AsNormalMap { get; set; }` | False; convert the result to an RGBA8 normal map. |
| `public bool Normalize { get; set; }` | True; request normalized samples from Noise. |
| `public float SeamlessBlendSkirt { get; set; }` | 0.1; finite overlap fraction from zero to one. |
| `public float BumpStrength { get; set; }` | 8; finite normal-map strength. |
| `public override bool HasAlpha { get; }` | False, including with an RGBA color ramp or normal map. |

All settings are synchronized. Equal assignments are silent. A changed setting invalidates pixels and emits `Changed` after the state is committed; `BumpStrength` affects pixels only while `AsNormalMap` is true. Changing `Seamless` or `AsNormalMap` also emits the inherited property-list notification. Invalid dimensions, skirt and nonfinite strength throw before changing state. Assigning a disposed source throws. Changes to a borrowed source invalidate the image without transferring ownership.

## Methods

| Declaration | Contract |
| --- | --- |
| `public override int GetWidth()` | Returns the configured logical width. |
| `public override int GetHeight()` | Returns the configured logical height. |
| `public override Vector2 GetSize()` | Returns both configured dimensions in one snapshot. |
| `public override Image? GetImage()` | Returns an independent generated image, or null without noise. |

`GetImage` may throw if a source returns an incompatible image, emits nonfinite noise, is disposed, or fails during image processing. A failed bake does not publish incomplete pixels; correcting the source allows a later read to retry. Recursive generation and continuous source mutation beyond eight retries fail with `InvalidOperationException`. Other [Texture](Texture.md) drawing, opacity and pixel metadata methods are inherited and use this same generated snapshot.

`HasAlpha` is overridden to return false, matching the noise texture's declared sampling contract even when `GetImage()` returns an RGBA8 color-ramp or normal-map image. Pixel alpha and `IsPixelOpaque` still use the generated image.

## Protected extension points

`CreateDuplicateInstance`, `CopyCustomStateTo`, `GetPropertyDescriptors`, and `Dispose(bool)` override Resource/Texture hooks for graph copying, typed stored properties and source unsubscription. The class is sealed.

## Verification and limits

[NoiseTextureTests](../../tests/Electron2D.Tests/NoiseTextureTests.cs) checks managed generation, source invalidation, color mapping, normal maps, mipmaps, seamless selection, copying, disposal and failure recovery. [NoiseTextureRenderingTests](../../tests/Electron2D.Tests/NoiseTextureRenderingTests.cs) checks initial and changed Sprite pixels on Linux Wayland GPU and compatibility backends. Other platforms and exact derived-format pixel parity remain unverified. Lazy generation and immediate invalidation differ from the reference's deferred worker schedule. A built-in FastNoiseLite generator remains absent. The 3D-space option is excluded under [ADR 0004](../decisions/product.md#adr-0004); the generation policy is in [ADR 0013](../decisions/resources.md#adr-0013). [Coverage](../coverage/classes/NoiseTexture2D.md) records these limits per member.
