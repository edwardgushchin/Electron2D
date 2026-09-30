# ServerTexture

Last updated: 2026-10-01

- Declaration: `internal sealed class ServerTexture(TexturePixels pixels) : Texture`
- Source: [RenderingTextureRegistry.cs](../../src/Servers/Rendering/RenderingTextureRegistry.cs)
- Inherits: [Texture](Texture.md)
- Component: [Canvas rendering](../components/canvas-rendering.md#texture-resource-identities)
- Visibility: internal; unavailable to engine consumers.

## Description

Carries a server-owned RID texture through existing Texture drawing, geometry and backend caches. It stores immutable source/upload pixels, logical size, diagnostic path and a stable bound RID. RenderingServer owns construction, mutation and disposal on the scene owner thread. The object is never exposed by the public server API; public getters return image copies and metadata.

## Internal example

```csharp
var target = RenderingTextureRegistry.Owned(rid, server);
var imageCopy = target.GetImage();
// Dispose the copy after use; server owns target.
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `ServerTexture(TexturePixels pixels)` | Starts with source dimensions and empty path. |
| `internal TexturePixels Pixels { get; set; }` | Current immutable payload; reading checks disposal. |
| `internal Vector2i Size` | Logical drawing size. |
| `internal string Path` | Diagnostic metadata. |
| `internal bool Released` | Allows server-authorized disposal. |
| `internal void Bind(RID rid)` | Stores the registered identity at construction. |
| `public override RID GetRID()` | Returns bound RID after disposal guard. |
| `public override int GetWidth()` | Logical width. |
| `public override int GetHeight()` | Logical height. |
| `public override Vector2 GetSize()` | Logical dimensions. |
| `public override Image GetImage()` | New caller-owned original image copy. |
| `internal override TexturePixels CapturePixels()` | Current immutable snapshot without copying. |
| `protected override void ValidateDisposal()` | Rejects direct disposal before server release. |
| `protected override void Dispose(bool disposing)` | Drops managed pixel/path/size state, then performs base texture cleanup. |

## Member descriptions and lifecycle

Construction receives already validated/copied pixels; Bind associates the freshly registered RID once. Pixels, Size and Path change only through owner-guarded server methods. Compatible Update preserves the allocation token; TextureReplace transfers a replacement payload and metadata into this object, so existing destination references keep working. Size overrides change new drawing geometry, while previously normalized source coordinates and destination geometry remain fixed.

GetRID/GetWidth/GetHeight/GetSize/GetImage/CapturePixels reject a disposed resource. GetImage copies bytes; CapturePixels borrows immutable engine data. The instance itself retains no native texture. Backend allocations follow ordinary cache replacement/eviction and renderer shutdown.

FreeRID marks Released and removes ownership/identity before calling Dispose. Direct disposal is rejected. Dispose clears the payload even when the terminal Disposed callback throws; retained commands skip disposed ServerTexture objects. Repeated invalid/stale public free calls fail through registry checks. Shutdown drains all owned objects and still disposes the backend if any callback fails.

## Verification and limits

[RenderingTextureRIDTests](../../tests/Electron2D.Tests/RenderingTextureRIDTests.cs) exercises pixel snapshots, size/path changes, update/replace without redraw, consumed/free retained commands, disposal failure and backend teardown. GPU/compatibility execute the same baseline. Native allocations, other platforms, proxies/layers/drawable targets and owner acceptance remain separately tracked. See [RenderingServer](RenderingServer.md#texture-rid-verification) and [ADR 0028](../decisions/rendering.md#adr-0028).
