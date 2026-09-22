# Texture

Last updated: 2026-09-23

Scene inheritance migration: [ADR 0008](../decisions/scene.md#adr-0008) assigns the neutral tree API to `SceneNode`, canvas behavior to `CanvasItem`, and the spatial API to `Node`. Signatures on this page describe the existing runtime until that migration is implemented.

- Declaration: `public abstract class Texture : Resource`
- Source: [Texture.cs](../../src/Scene/Resources/Texture.cs)
- Inherits: [Resource](Resource.md)
- Inherited by: [ImageTexture](ImageTexture.md)
- Component: [Shader materials](../components/shader-materials.md)

## Description

A two-dimensional image texture with a logical drawing size, original pixel metadata and readable image copies. All engine textures are two-dimensional; there is no dimension suffix or empty parent above Texture. The naming and hierarchy are fixed by [ADR 0004](../decisions/product.md#adr-0004).

Nodes, materials and shaders borrow textures. A texture owns managed pixel data; the renderer owns native allocations. Creating or reading a texture does not require an active window. Disposing a node, material or renderer does not dispose borrowed textures.

## Example

```csharp
using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
image.Fill(Colors.White);
using Texture texture = ImageTexture.CreateFromImage(image);
using var copy = texture.GetImage();
```

DisplayServer.CursorSetCustomImage also accepts this resource through its Resource parameter. It borrows the Texture, obtains and disposes a caller-owned GetImage copy, and installs an independent native cursor. Cursor dimensions and hotspots use actual image pixels rather than logical size overrides.

## API summary

| Declaration | Contract |
| --- | --- |
| `protected Texture()` | Registers invalidation of cached custom image data on Changed. |
| `abstract int Width { get; }` | Logical width in pixels. |
| `abstract int Height { get; }` | Logical height in pixels. |
| `virtual Vector2 Size { get; }` | Logical width and height. |
| `virtual Image.Format PixelFormat { get; }` | Original pixel format; L8 when uninitialized. |
| `virtual bool HasAlpha { get; }` | Original format has alpha; false when uninitialized. |
| `virtual bool HasMipmaps { get; }` | Original pixels contain a complete mip chain. |
| `virtual int MipmapCount { get; }` | Number of levels after the base level; zero when absent. |
| `virtual Image? GetImage()` | Independent caller-owned pixels, or null when unreadable/uninitialized. |
| `virtual void Draw(Node canvasItem, Vector2 position, Color? modulate = null, bool transpose = false)` | Draws at logical size during the target node's OnDraw. |
| `virtual void DrawRect(Node canvasItem, Rect rect, bool tile, Color? modulate = null, bool transpose = false)` | Stretches or repeats over a local rectangle. |
| `virtual void DrawRectRegion(Node canvasItem, Rect rect, Rect sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)` | Draws a source region in logical texture pixels. |
| `virtual bool IsPixelOpaque(int x, int y)` | Tests alpha above 0.1 at clamped logical coordinates. |

## Property descriptions

### Width

Logical horizontal size. Custom subclasses provide this property and define their own synchronization. ImageTexture returns zero before initialization and throws after disposal.

### Height

Logical vertical size, with the same contract as Width.

### Size

Returns Width and Height as a Vector2. ImageTexture reads both coordinates atomically.

### PixelFormat

Describes the original image, even when GPU upload requires conversion. The base implementation captures custom pixels and caches them until Changed. Disposed textures throw ObjectDisposedException.

### HasAlpha

Reports an alpha channel in the original format, including supported floating-point formats. It does not test whether any pixel is transparent.

### HasMipmaps

Reports the original complete mip-chain configuration. GPU upload includes every stored level.

### MipmapCount

Counts levels following the base image. It is zero for a texture without mipmaps or pixels.

## Method descriptions

### Draw

Records a borrowed texture at `position` with its current logical Size. The target must be inside its own OnDraw callback on the scene owner thread; null targets, disposed resources and nonfinite arguments are rejected. Null modulation means white. Transpose exchanges source axes and the destination width/height. An uninitialized zero-size texture records nothing.

### DrawRect

The destination origin is unchanged by negative dimensions: each negative axis reflects its image and the occupied extent uses the absolute size. Transpose then exchanges destination dimensions and UV axes. With `tile = false`, the full texture stretches across the destination. With `tile = true`, it repeats using its logical size. Zero-area destinations draw nothing. Filtering is nearest; ordinary sampling clamps and tiled sampling repeats. A compatibility driver lacking non-power-of-two repeat support rejects that operation explicitly.

### DrawRectRegion

Source positions and sizes use logical pixels, accounting for ImageTexture size overrides. Negative source dimensions toggle the corresponding destination reflection without moving the source origin. `clipUV` constrains sampling to texel centers within the source region while retaining interior interpolation; disabling it leaves that restriction off. Coordinates outside the full image still clamp to image edges. Transpose, modulation, zero-area behavior and errors follow DrawRect.

All three methods are virtual. Node.DrawTexture, DrawTextureRect and DrawTextureRectRegion invoke them so a custom texture can draw its own geometry. Default implementations retain resource references, normalized source coordinates, destination geometry and draw transform. Update and SetImage affect later frames without rerunning OnDraw; changing the destination or recorded logical scale requires QueueRedraw. Disposal before a retained command is consumed fails the frame. Commands do not transfer resource ownership.

### GetImage

The base method checks disposal and returns null. A renderable custom subclass overrides it to return an independent image and emits Changed when pixels change. The renderer copies and disposes this returned image. Returning null fails explicitly when the texture is bound for drawing. Custom implementations must serialize their own data access; the base cache serializes snapshot acquisition and invalidation.

### IsPixelOpaque

Scales integer logical coordinates into original pixel coordinates using widened arithmetic, clamps to the edges and compares alpha with 0.1. An uninitialized texture returns true. ImageTexture captures pixels and logical size under one gate. Disposal throws.

## Protected extension points and lifecycle

Width, Height and GetImage define a custom texture's readable pixel source. GetPropertyDescriptors includes typed read-only metadata. Dispose(bool) unsubscribes cache invalidation, clears cached pixels and delegates Resource cleanup. Custom subclasses own disposal and duplication of their own pixel source. Resource path, identity, local-scene and Changed behavior remain inherited.

## Limits and checks

The current GPU integration accepts float-sampled images up to 16384 pixels per axis. Compressed/integer-sampled formats, configurable filtering, texture arrays and placeholder textures remain unfinished. Compatibility checks native support for high-precision images and non-power-of-two repeat before drawing. See [the component](../components/shader-materials.md) for precise format, binding and platform limits. [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) checks custom snapshot reuse and invalidation through real shader draws. [CanvasTextureTests](../../tests/Electron2D.Tests/CanvasTextureTests.cs) verifies public drawing, virtual overrides, retained updates/replacement, UV clipping and disposal on Linux Wayland and the software renderer.
