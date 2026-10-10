# NoiseTexture

Last updated: 2026-10-11

**Inherits:** [Texture](Texture.md) → [Resource](Resource.md) → [ElectronObject](ElectronObject.md)

- **Source:** [NoiseTexture.cs](../../src/Scene/Resources/NoiseTexture.cs), [NoiseTexture.Generation.cs](../../src/Scene/Resources/NoiseTexture.Generation.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class NoiseTexture : Texture`
- **Component:** [Noise](../components/noise.md)

## Description

`NoiseTexture` turns a borrowed [Noise](Noise.md) into a retained image texture. In an active SceneTree, changes coalesce into deferred owner work: first generation computes there, subsequent images compute on workers and publish Changed on the owner. Reads keep complete preceding images while work waits. Stale results are discarded; errors preserve preceding pixels and can retry after corrected input. Without a running scene loop, image reads generate synchronously for library hosts.

Generation preserves virtual source image dispatch, then optionally applies a copied Gradient, normal conversion and mipmaps in that order. Built-in FastNoiseLite and Gradient input snapshots belong to jobs; original sources stay borrowed. Custom Noise methods must support background execution and coordinated state. Sources must return the requested dimensions without mipmaps. Image results belong to callers. Removing noise publishes a null readable image while retaining the previous rendering allocation; first drawing uses the standard diagnostic placeholder.

All authored settings have portable descriptors/factories; image/jobs are transient. Local packed scenes preserve aliases and independently local generators. File-owned graphs remain retained through pending work. Recursive generation/disposal and worker disposal of unfinished generation reject before lifetime mutation. Ordinary disposal drains its owned task outside the texture gate. See the [Noise component](../components/noise.md) for complete behavior, numeric corrections, executable authoring and verification boundaries.

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

[NoiseTextureTests](../../tests/Electron2D.Tests/NoiseTextureTests.cs) checks managed generation, source invalidation, color mapping, normal maps, mipmaps, seamless selection, copying, disposal and failure recovery. [NoiseTextureRenderingTests](../../tests/Electron2D.Tests/NoiseTextureRenderingTests.cs) checks initial and changed Sprite pixels on Linux Wayland GPU and compatibility backends. Other platforms and exact derived-format pixel parity remain unverified. Lazy generation and immediate invalidation differ from the reference's deferred worker schedule. The built-in [FastNoiseLite](FastNoiseLite.md) generator passes managed texture integration and retained-pixel checks on both tested backends. The 3D-space option is excluded under [ADR 0004](../decisions/product.md#adr-0004); the generation policy is in [ADR 0013](../decisions/resources.md#adr-0013). [Coverage](../coverage/classes/NoiseTexture2D.md) records these limits per member.
