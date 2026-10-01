# ServerTexture

Last updated: 2026-10-01

- Declaration: `internal sealed class ServerTexture : Texture`
- Source: [RenderingTextureRegistry.cs](../../src/Servers/Rendering/RenderingTextureRegistry.cs)
- Inherits: [Texture](Texture.md)
- Component: [Canvas rendering](../components/canvas-rendering.md#texture-resource-identities)
- Visibility: internal; unavailable to engine consumers.

## Description

Carries a server-owned RID texture through existing Texture drawing, geometry and backend caches. It stores immutable source/upload pixels, logical size, diagnostic path and a stable bound RID. A second constructor creates an alias with only a borrowed source RID and cached metadata. RenderingServer owns construction, mutation and disposal on the scene owner thread. The object is never exposed by the public server API; public getters return image copies and metadata.

## Internal example

```csharp
var target = RenderingTextureRegistry.Owned(rid, server);
var imageCopy = target.GetImage();
// Dispose the copy after use; server owns target.
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal ServerTexture(RID target, Texture source)` | Validates/copies metadata and stores a borrowed alias link. |
| `internal bool IsProxy { get; }` / `RID ProxyTarget { get; }` | Alias role and immediate source link. |
| `internal void SetProxyTarget(RID target, Texture source)` | Preflights source callbacks, then publishes link and metadata. |
| `internal void RedirectProxy(RID target)` | Transfers a consumed source link before replacement disposal. |
| `internal void SetDisplaySize(Vector2i size)` | Sets logical size; a proxy override resets on retarget. |
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
| `public override Image? GetImage()` | New original image copy, or null for a disconnected proxy. |
| `internal override TexturePixels? CapturePixels()` | Current source snapshot, or null for a disconnected proxy. |
| `protected override void ValidateDisposal()` | Rejects direct disposal before server release. |
| `protected override void Dispose(bool disposing)` | Drops managed pixel/path/size state, then performs base texture cleanup. |

## Member descriptions and lifecycle

Construction receives already validated/copied pixels; Bind associates the freshly registered RID once. Pixels, Size and Path change only through owner-guarded server methods. Compatible Update preserves the allocation token; TextureReplace transfers a replacement payload and metadata into this object, so existing destination references keep working. Size overrides change new drawing geometry, while previously normalized source coordinates and destination geometry remain fixed.

GetRID/GetWidth/GetHeight/GetSize/GetImage/CapturePixels reject a disposed resource. GetImage copies bytes; CapturePixels borrows immutable engine data. The instance itself retains no native texture. Owned image textures retain prepared cache allocation until free/shutdown. Proxies resolve their source at replay; aliases share root storage. Borrowed empty sources replay through the internal immutable checkerboard texture.

FreeRID marks Released and removes ownership/identity before calling Dispose. Direct disposal is rejected. Dispose clears the payload even when the terminal Disposed callback throws; retained commands skip disposed ServerTexture objects. Repeated invalid/stale public free calls fail through registry checks. Shutdown drains all owned objects and still disposes the backend if any callback fails.

## Verification and limits

[RenderingTextureRIDTests](../../tests/Electron2D.Tests/RenderingTextureRIDTests.cs) exercises pixel snapshots, size/path changes, update/replace without redraw, consumed/free retained commands, disposal failure and backend teardown. GPU/compatibility execute the same baseline. Native allocator totals, other platforms, layers/drawable targets and owner acceptance remain separately tracked. See [RenderingServer](RenderingServer.md#texture-rid-verification) and [ADR 0028](../decisions/rendering.md#adr-0028).

Proxy GetSize/GetWidth/GetHeight follow the current root source unless a local display override is set. Root disappearance retains the cached metadata while CapturePixels/GetImage return null; it does not dispose the alias. PixelFormat follows live root pixels and keeps its last copied fallback on disconnection. SetProxyTarget collects size/format/path before mutation and resets a local size override. Registry traversal is iterative; proxy update requires a non-proxy source, so retargeting cannot introduce a cycle. Size/path remain metadata without owning pixels. See [proxy checks](../../tests/Electron2D.Tests/RenderingTextureProxyTests.cs).
