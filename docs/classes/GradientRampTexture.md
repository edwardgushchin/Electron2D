# GradientRampTexture

Last updated: 2026-09-23

- Declaration: `public sealed class GradientRampTexture : Texture`
- Source: [GradientRampTexture.cs](../../src/Scene/Resources/GradientRampTexture.cs)
- Inherits: [Texture](Texture.md), [Resource](Resource.md)
- Component: [Gradients](../components/gradients.md)

## Description

A single row of inclusive samples from a borrowed Gradient. Image access and renderer consumption lazily bake a complete immutable pixel snapshot. Multiple edits before consumption coalesce. Gradient changes invalidate pixels without emitting this texture's Changed event. Source and UseHDR setters notify only on actual changes; every assignment to the other settings invalidates and emits, including equality. Pixel generation itself emits nothing.

The texture borrows its source and owns copied pixels. The renderer owns native allocations. Removing the source freezes the last payload; later size/HDR/fill settings can differ from the retained physical image. With an initially null source there is no image. HasAlpha is always true, including before initialization; no mipmaps are generated. Initial PixelFormat is L8, then Rgba8 or Rgbaf according to the stored payload. ResourceLocalToScene defaults to false.

One lock serializes texture settings and baking; the gradient lock holds across all pixel samples, giving each image a coherent source state. A notification runs outside the texture lock on the editing thread; exceptions propagate after commitment. Borrowed gradients must remain alive while baking. Deferred bake failure retains the previous payload and pending work; detach/replace the source or correct dimensions to recover. Copy, disposal and multi-call edits require caller coordination. Large valid dimensions can fail checked buffer sizing or allocation. There is no throughput or bounded-latency guarantee for baking.

## Example

```csharp
using var gradient = new Gradient();
using var texture = new GradientRampTexture { Gradient = gradient, Width = 3 };
using var pixels = texture.GetImage()!;
// First and last columns include the endpoint colors.
```

No window is needed for construction or GetImage. Canvas drawing, Sprite and ShaderMaterial consume the ordinary Texture API.

## API summary

| Declaration | Contract |
| --- | --- |
| `GradientRampTexture()` | 256 by 1, null source, byte storage. |
| `int Width { get; set; }` | Default 256. Horizontal sample count; inclusive range 1..16384. |
| `Gradient? Gradient { get; set; }` | Default null. Borrowed source; identity reassignment is silent. Null preserves the last generated pixels. |
| `bool UseHDR { get; set; }` | Default false. RGBAF storage when true; clamped RGBA8 otherwise. Actual changes notify; equality is silent. |
| `override int GetWidth()` | Logical Width. |
| `override int GetHeight()` | Always one. |
| `override Vector2 GetSize()` | Logical size under one lock. |
| `override bool HasAlpha { get; }` | Always true. |
| `override Image? GetImage()` | Flush pending work and copy original pixels, or null before initialization. |

## Property descriptions

### Width

Default 256. Horizontal sample count; inclusive range 1..16384. Every assignment notifies and invalidates. Invalid values throw ArgumentOutOfRangeException before mutation.

### Gradient

Default null. Borrowed source; identity reassignment is silent. Null preserves the last generated pixels. Old subscriptions disconnect and the new live source subscribes immediately. A disposed source throws ObjectDisposedException before replacement. The texture never disposes its source.

### UseHDR

Default false. RGBAF storage when true; clamped RGBA8 otherwise. Actual changes notify; equality is silent.

### HasAlpha

Returns true, independently of initialization and pixel contents. Throws after disposal.

## Method descriptions

### GetWidth

Returns logical Width without baking.

### GetHeight

Returns one without baking.

### GetSize

Returns both logical dimensions atomically without baking.

### GetImage

Bakes once if dirty and a source is assigned, then returns an independent caller-owned Image in the original RGBA8/RGBAF format. Editing or disposing the image does not mutate the texture. Null source preserves prior pixels, so their dimensions can differ from logical settings. Without prior pixels returns null; rendering that state fails explicitly. Invalid source lifetime throws ObjectDisposedException; excessive buffer size can throw OverflowException or OutOfMemoryException without replacing the last payload.

## Sampling and storage

Texel i samples i/(Width-1), including both endpoints. Width one samples offset zero explicitly. RGBA8 channels round to the nearest byte after clamping; HDR channels preserve float values, including signs and values above one.

## Protected hooks and lifecycle

GetPropertyDescriptors adds typed writable settings to inherited resource/pixel metadata. CreateDuplicateInstance returns this exact type. CopyCustomStateTo copies all settings, follows the Resource shallow/deep policy for the source and invalidates derived pixels. A null-source copy preserves the last immutable payload. CopyFromResource replaces subscriptions and emits one coalesced event; copying defaults resets to no image. PackedScene owns only scene-local duplicates. Dispose(bool) disconnects the source and drops pixels before base cleanup; it does not own borrowed resources. Public state access throws ObjectDisposedException after disposal.

## Verification and limits

[GradientTests](../../tests/Electron2D.Tests/GradientTests.cs) checks CPU pixels, metadata, events, width bounds, copies/local ownership, failure and concurrent baking. [RenderingGradientTests](../../tests/Electron2D.Tests/RenderingGradientTests.cs) checks real Linux Wayland GPU/compatibility and dummy/software LDR canvas output, retained geometry updates, GPU HDR canvas and six HDR/negative/update stages for each HLSL/GLSL material. Tested compatibility drivers reject unsupported HDR explicitly. Other platforms, owner visual acceptance, fresh AOT/self-contained delivery, disk import and editor authoring remain unverified or absent. Shared Texture/Resource gaps remain separate. See [ADR 0013](../decisions/resources.md#adr-0013) and [ADR 0028](../decisions/rendering.md#adr-0028).
