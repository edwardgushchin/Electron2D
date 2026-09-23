# CurveXYZTexture

Last updated: 2026-09-23

- Declaration: `public sealed class CurveXYZTexture : Texture`
- Source: [CurveXYZTexture.cs](../../src/Scene/Resources/CurveXYZTexture.cs)
- Inherits: [Texture](Texture.md), [Resource](Resource.md)
- Component: [Curves](../components/curves.md)

## Description

Stores three independent scalar curves in a one-row RGBF texture. X, Y and Z mean red, green and blue channels; this is not a spatial 3D resource. Each texel i samples the corresponding curve at i/Width, not the curve's remapped domain. A null channel supplies zero. Float values retain signs, HDR range and the scalar sampler's derived IEEE behavior; alpha is one.

This resource shares [CurveTexture's ownership, threading, failure and renderer contract](CurveTexture.md#description). It borrows every curve and subscribes once per distinct instance. A curve used in several channels is sampled once into a coherent row and produces one texture Changed per source event. Removing one alias keeps the subscription until its last channel is removed. Different curves are captured independently; there is no cross-resource transaction.

## Example

```csharp
using var red = new Curve();
red.AddPoint(Vector2.One);
using var texture = new CurveXYZTexture { CurveX = red, CurveZ = red };
using var pixels = texture.GetImage()!;
// Every pixel is (1, 0, 1, 1); red is borrowed by both channels.
```

The shared-channel case executes in CurveTextureTests. Materials and atlas views accept this resource as a Texture.

## API summary

| Declaration | Contract |
| --- | --- |
| `CurveXYZTexture()` | Width 256; all channels null; no image. |
| `int Width { get; set; }` | Sample count, 32..4096 inclusive. |
| `Curve? CurveX { get; set; }` | Borrowed red-channel source. |
| `Curve? CurveY { get; set; }` | Borrowed green-channel source. |
| `Curve? CurveZ { get; set; }` | Borrowed blue-channel source. |
| `override int GetWidth()` | Current Width. |
| `override int GetHeight()` | One. |
| `override Image? GetImage()` | Caller-owned RGBF image, or null before initialization. |

GetSize, metadata, drawing and material binding are inherited. ResourceLocalToScene defaults to false; there is no storage-mode property. Before initialization PixelFormat is L8, afterward Rgbf; HasAlpha/HasMipmaps are false, MipmapCount zero, and IsPixelOpaque true.

## Property descriptions

### Width

Default 256; actual changes rebuild all channels and emit once. Equal assignment is silent. Out-of-range values throw ArgumentOutOfRangeException without changing settings, subscriptions or pixels. The inherited query is GetWidth().

### CurveX

Red source, null by default. Identity assignment is silent; replacing the reference rebuilds and emits. A disposed assignment throws ObjectDisposedException. Null supplies zero. Resource disposal never transfers ownership or disposes sources.

### CurveY

Green source, with the CurveX contract. It may borrow the same instance as either other channel.

### CurveZ

Blue source, with the CurveX contract. Removing the last use of a source detaches its Changed handler. BakeResolution-only changes do not notify/rebuild until another actual source/settings change.

## Method descriptions

### GetWidth

Returns Width under the state lock, throwing after disposal.

### GetHeight

Returns one, throwing after disposal.

### GetImage

Returns a new image with Width×1 texels, no mipmaps and Image.Format.Rgbf (12 source bytes/texel), independent of source/output mutations. Default resources have dimensions but no image until a changed setting/source initializes them. An initialized all-null resource returns zero RGB, alpha one. Native upload expands to RGBA32Float; compatibility without float support rejects drawing explicitly. Throws after disposal.

## Protected hooks, lifecycle and errors

GetPropertyDescriptors supplies typed Width/CurveX/CurveY/CurveZ descriptors. CreateDuplicateInstance creates the exact type. CopyCustomStateTo preserves width and initialized state, routes every source through the common duplication graph policy and rebakes the resulting curves; repeated references remain aliases. Shallow copies borrow original curves. Deep copies follow Internal/All/None policies, and their pixel snapshots are independent. CopyFromResource replaces subscriptions and coalesces notification. PackedScene disposes its own local texture and curve copies; ordinary texture disposal disconnects without disposing borrowed sources. Dispose(bool) drops pixels and delegates base cleanup.

Validation/sampling failure preserves previous texture state. Subscriber failures propagate after the new data is committed. Disposal during notification is safe; concurrent copy/disposal requires caller coordination. A borrowed disposed source retains the last baked image but prevents future rebaking until removed/replaced.

## Verification and limits

[CurveTextureTests](../../tests/Electron2D.Tests/CurveTextureTests.cs) covers channel values, deduplicated notifications, descriptor edits, shallow/deep/external/local copies, failures and concurrency. [RenderingCurveTextureTests](../../tests/Electron2D.Tests/RenderingCurveTextureTests.cs) verifies Linux Wayland GPU canvas and HLSL/GLSL materials with live channel/width/worker changes and atlas/default bindings. Wayland compatibility and dummy/software reject unsupported float storage. Other platforms, owner visual acceptance, disk import/editor authoring and fresh published/AOT delivery remain unverified. Shared base API gaps stay explicit in coverage. See ADRs [0013](../decisions/resources.md#adr-0013) and [0028](../decisions/rendering.md#adr-0028).
