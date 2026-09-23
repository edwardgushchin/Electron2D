# ImageTexture

Last updated: 2026-09-23

- Declaration: `public sealed class ImageTexture : Texture`
- Source: [ImageTexture.cs](../../src/Scene/Resources/ImageTexture.cs)
- Inherits: [Texture](Texture.md), [Resource](Resource.md)
- Component: [Shader materials](../components/shader-materials.md)

## Description

Owns a snapshot of an Image, independent of the original image and any active renderer. All incoming and returned image data is copied. SetImage replaces the pixel configuration; Update keeps it. The renderer uploads pixels on first use and after a change, and owns all native resources. Textures shared by materials remain caller-owned.

State access is serialized. Validation and copying finish before mutation; failure preserves the old state. Successful mutations release the gate and then emit Changed. Observer exceptions propagate after the mutation has committed.

## Example

```csharp
using var image = Image.CreateEmpty(32, 32, false, Image.Format.Rgba8);
image.Fill(Colors.Red);
using var texture = ImageTexture.CreateFromImage(image);
image.Fill(Colors.Blue);
texture.Update(image);
```

## API summary

| Declaration | Contract |
| --- | --- |
| `ImageTexture()` | Uninitialized, zero size, no image. |
| `static ImageTexture CreateFromImage(Image image)` | Copies a live nonempty image. |
| `void SetImage(Image image)` | Copies pixels, replaces allocation configuration and resets logical size. |
| `void Update(Image image)` | Copies matching pixels while retaining configuration and logical size. |
| `void SetSizeOverride(Vector2I size)` | Overrides logical axes; zero retains the current axis. |
| `override int GetWidth()` | Logical width. |
| `override int GetHeight()` | Logical height. |
| `override Vector2 GetSize()` | Atomic logical dimensions. |
| `override bool IsPixelOpaque(int x, int y)` | Clamped alpha test using one state snapshot. |
| `override Image? GetImage()` | New independent original image, or null when uninitialized. |

PixelFormat, HasAlpha, HasMipmaps and MipmapCount follow the [Texture metadata contract](Texture.md#property-descriptions).

## Method descriptions

### GetWidth

Current logical width; zero before initialization. Disposal throws ObjectDisposedException.

### GetHeight

Current logical height, with the same lifecycle as GetWidth.

### GetSize

Reads both logical dimensions under one lock. Logical overrides do not change the image returned by GetImage.

### CreateFromImage

Requires a live nonempty Image, at most 16384 pixels per axis, in an integrated sampling format. Copies and validates before creating the resource. Empty/oversized images throw ArgumentException, unimplemented formats throw NotSupportedException, null throws ArgumentNullException and disposal throws ObjectDisposedException. The source remains caller-owned.

### SetImage

Uses the same validation and copy contract. Resets logical size to original pixel dimensions. Allocations are replaced on the next use, even if the new dimensions match; outstanding GPU work retains its native version through SDL lifetime management. The renderer uploads all mip levels before drawing.

### Update

Requires prior initialization and matching original pixel dimensions, original format and mip configuration. Logical size overrides are retained and do not alter those requirements. Mismatch throws ArgumentException; an uninitialized texture throws InvalidOperationException. Successful updates reuse native allocation identity, with native cycling for queued frames.

### SetSizeOverride

Negative axes throw ArgumentOutOfRangeException. Zero leaves that axis unchanged. Only an actual change emits Changed. This operation does not resample pixels, change their format or replace allocation identity.

### GetImage

Returns a new Image with the original format, physical dimensions, mip configuration and copied data. Caller mutations and disposal are independent. Returns null before initialization and throws after disposal.

### IsPixelOpaque

Maps logical coordinates into the original image, clamps to its edges and tests alpha above 0.1. Empty textures return true. Widened coordinate arithmetic avoids integer overflow.

## Resource lifecycle and protected hooks

CreateDuplicateInstance creates an empty ImageTexture. CopyCustomStateTo copies logical size and shares only immutable internal snapshots; later pixel replacement is independent in each copy. GetImage never exposes those snapshots. Dispose(bool) clears managed pixel references and logical size, then delegates Texture and Resource cleanup. It does not dispose source images, materials, shaders or another copy.

## Formats, verification and remaining work

Byte formats upload as RGBA8. Other accepted uncompressed normalized/float formats upload as RGBA32Float, retaining HDR values. Original image bytes remain available unchanged. Compressed and integer-sampled formats fail explicitly; backend support is checked before native allocation. Canvas sampling is selected by CanvasItem and Viewport; the resource stores no sampler state. Named material textures retain the fixed nearest/nearest-mip/clamp profile. Ordinary canvas drawing is implemented; other platforms remain unverified.

[RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) covers copy isolation, logical sizes, update constraints, duplication, default and override bindings, disposal, custom textures and native shader readback. This remains part of the unfinished rendering vertical, not a claim of complete texture API coverage.
