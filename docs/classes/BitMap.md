# BitMap

Last updated: 2026-09-23

**Inherits:** [Resource](Resource.md)

**Inherited By:** —

- **Source:** [BitMap.cs](../../src/Core/IO/BitMap.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class BitMap : Resource`

## Description

`BitMap` owns a packed, mutable 2D boolean mask. One byte stores eight pixels in row-major order. It works without a renderer or physics server. State access and commits are serialized per instance; alpha import prepares its image snapshot before taking the mask lock. Successful mutations publish a single inherited `Resource.Changed` event after committing; unchanged writes do not publish it. Event handlers may reenter the bitmap. Disposal releases its buffer and prevents later access. Inherited Resource duplication copies the buffer independently.

```csharp
using var mask = new BitMap();
mask.Create(new Vector2i(8, 8));
mask.SetBitRect(new Rect2i(2, 2, 4, 4), true);
Vector2[][] outlines = mask.OpaqueToPolygons(new Rect2i(0, 0, 8, 8));
using Image preview = mask.ConvertToImage();
```

## Constructor

| Member | Contract |
| --- | --- |
| `public BitMap()` | Creates an empty zero-sized resource. |

## Methods

| Member | Contract |
| --- | --- |
| `public Vector2i GetSize()` | Returns dimensions. |
| `public void Create(Vector2i size)` | Replaces the mask with false bits. |
| `public void CreateFromImageAlpha(Image image, float threshold = 0.1f)` | Copies alpha classification from a source image. |
| `public bool GetBit(int x, int y)` | Reads one bit. |
| `public bool GetBitv(Vector2i position)` | Reads one bit by point. |
| `public void SetBit(int x, int y, bool bit)` | Writes one bit. |
| `public void SetBitv(Vector2i position, bool bit)` | Writes one bit by point. |
| `public void SetBitRect(Rect2i rect, bool bit)` | Writes a clipped rectangle. |
| `public int GetTrueBitCount()` | Counts set bits. |
| `public void Resize(Vector2i newSize)` | Nearest-neighbor resize. |
| `public Image ConvertToImage()` | Creates independent L8 black/white pixels. |
| `public void GrowMask(int pixels, Rect2i rect)` | Circular dilation or erosion within a rectangle. |
| `public Vector2[][] OpaqueToPolygons(Rect2i rect, float epsilon = 2f)` | Extracts and reduces mask contours. |

## Protected extension points

| Member | Contract |
| --- | --- |
| `protected override Resource CreateDuplicateInstance()` | Creates the copy target; a derived class must override it to preserve its runtime type. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies dimensions and buffer into independent storage. |
| `protected override void Dispose(bool disposing)` | Clears the buffer on disposal. |

## Method descriptions

### GetSize

Returns `Vector2i(width, height)`; a default instance is `(0, 0)`.

### Create

Requires positive `size` with at most `int.MaxValue` pixels. Replaces previous contents with false bits; invalid size throws before mutation.

### CreateFromImageAlpha

Duplicates `image`, converts the copy to LA8 and marks each pixel true only when its alpha divided by 255 strictly exceeds `threshold`. A null, empty, disposed or unreadable image fails before the mask changes. The source remains unchanged.

### GetBit and GetBitv

Read at zero-based integer coordinates. An invalid coordinate throws `ArgumentOutOfRangeException`, the typed C# adaptation of the upstream error-return path. `GetBitv` uses `Vector2i`.

### SetBit and SetBitv

Write one pixel; `SetBitv` uses `Vector2i`. An invalid coordinate throws without mutation. A successful value change raises `Changed` after the write.

### SetBitRect

Clips the half-open `rect` to the current mask and writes `bit` to the intersection. A disjoint or unchanged region does nothing; changed regions raise one event after the full write.

### GetTrueBitCount

Counts only valid mask bits; padding bits in the last byte remain zero.

### Resize

Requires positive `newSize` and samples the old mask with nearest-neighbor integer coordinates. An uninitialized mask becomes all false. Equal dimensions do nothing.

### ConvertToImage

Returns an independent L8 image with byte value 255 for true and 0 for false. A default empty mask returns an empty image. Image dimension and storage limits still apply.

### GrowMask

Uses a circular pixel radius: positive `pixels` dilates, negative erodes, zero does nothing. Only pixels inside the intersection with `rect` change. Source bits are snapshotted, so one operation does not feed its own newly changed pixels. Outside `rect` counts as false for erosion. Large operations may allocate and take time proportional to area and radius; do not call them in a frame hot path.

### OpaqueToPolygons

Scans the clipped `rect` in row-major order, joins touching opaque pixels, traces marching-squares contours including diagonal crossings, and reduces long contours with Ramer-Douglas-Peucker. Returned `Vector2` points are relative to the clipped rectangle origin. `epsilon` must be finite and nonnegative; it is capped at half the smaller clipped dimension. Empty or disjoint regions return no polygons. This operation allocates output and working buffers.

## State, ownership and verification

`BitMap` inherits `Resource` identity, change events, graph-aware duplication and disposal. A duplicate shares no bit storage. All mutating methods publish changes only after a successful commit, so an observer exception does not revert the mask. Instance methods fail after disposal.

[BitMapTests](../../tests/Electron2D.Tests/BitMapTests.cs) checks bit order, clipped writes, alpha threshold equality, L8 output, resize, dilation and erosion, simple and diagonal contours, reduction termination, independent duplication, failures and disposal on managed Linux/.NET. Other platforms and physics integration are not verified. [Coverage](../coverage/classes/BitMap.md) accounts for the complete pinned class surface and C# lifecycle projections.

The mask and contour behavior follows the [pinned implementation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/resources/bit_map.cpp); its [MIT notice](../coverage/GODOT-LICENSE.txt) is retained in this repository.
