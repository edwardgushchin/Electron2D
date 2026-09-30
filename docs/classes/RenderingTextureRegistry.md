# RenderingTextureRegistry

Last updated: 2026-10-01

- Declaration: `internal static class RenderingTextureRegistry`
- Source: [RenderingTextureRegistry.cs](../../src/Servers/Rendering/RenderingTextureRegistry.cs)
- Component: [Canvas rendering](../components/canvas-rendering.md#texture-resource-identities)
- Visibility: internal; unavailable to engine consumers.

## Description

Maps process-wide, non-reused RIDs to logical Texture resources. A borrowed entry is weak and independent of the active renderer; atlas views forward the current source RID rather than register another entry; a server entry strongly retains its ServerTexture and owning RenderingServer until free/shutdown. One registry gate protects registration, lookup and removal. No native handles live here. The lazy immutable 4×4 magenta/black RGBA8 checkerboard supplies empty-resource server queries and explicit placeholder creation.

## Internal example

```csharp
var texture = RenderingTextureRegistry.Resolve(rid);
// Resolve does not transfer resource ownership.
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal static TexturePixels PlaceholderPixels { get; }` | Immutable lazy fallback pixel payload. |
| `internal static RID Register(Texture texture, RenderingServer? owner = null)` | Assigns a fresh identity and records borrowed/owned lifetime. |
| `internal static Texture Resolve(RID rid)` | Returns a live logical resource or rejects an invalid/stale/wrong-kind identity. |
| `internal static ServerTexture Owned(RID rid, RenderingServer owner)` | Requires a live server texture belonging to this exact renderer. |
| `internal static void Remove(RID rid)` | Removes identity registration without invoking resource callbacks. |

## Member descriptions

### PlaceholderPixels

Constructed once under Lazy synchronization, using the existing Image/TexturePixels conversion. Server image queries copy it; no caller sees or mutates its arrays. Access after warmup allocates nothing.

### Register

Uses RID.Allocate and the registry gate. Every 256 cold registrations scans weak references into a reused stale-RID list and removes dead/disposed entries. The scan never runs during warmed Resolve/GetRID/replay. Identity creation and dictionary growth may allocate. A Texture caches its borrowed RID under its separate identity gate; concurrent requests return one identity. Server creation also binds its internal texture and tracks it for shutdown.

### Resolve

Rejects empty, foreign-kind, collected or disposed resources with ArgumentException and removes a stale entry encountered on lookup. Returning a reference does not lock the resource beyond lookup; concrete textures define their own data synchronization. Retained canvas commands can prolong the resource's managed lifetime.

### Owned

Rejects absent/dead identities with ArgumentException and borrowed/foreign-owner identities with InvalidOperationException. Server methods additionally enforce their owner thread and submission/shutdown phase before mutation.

### Remove

Idempotently removes the entry. The caller remains responsible for disposal. Server free removes registration and its owned list entry before callbacks, making callback failure unable to resurrect the consumed RID.

### Entry

Private sealed `Entry(Texture texture, RenderingServer? owner)` stores `WeakReference<Texture> Borrowed`, optional strong `Texture? Owned`, and `RenderingServer? Owner`. A null owner identifies a borrowed resource. Strong state exists only for server-owned textures; this bookkeeping does not itself own native data.

## Verification and limits

[RenderingTextureRIDTests](../../tests/Electron2D.Tests/RenderingTextureRIDTests.cs) checks identity, duplication, concurrent access, weak collection/cold sweeping, wrong-kind/stale/disposal errors and warmed zero managed allocations. Native create/update/replace/draw/free/shutdown runs through both renderer backends. The single gate follows existing physics RID bookkeeping; contention and native allocations are not measured. See [ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0014](../decisions/resources.md#adr-0014).
