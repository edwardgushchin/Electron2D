# ResourceLoader

Last updated: 2026-09-24

**Inherits:** None; static C# service

- **Source:** [ResourceLoader.cs](../../src/Core/IO/ResourceLoader.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public static class ResourceLoader`
- **Component:** [Resource loading](../components/resource-loading.md)

## Description

The first synchronous loader profile turns a PNG, JPEG, WebP, BMP, TGA or SVG file into a caller-owned `ImageTexture`. Decoding uses `Image.LoadFromFile`; the texture owns a copied pixel snapshot and uploads GPU data when drawn. `Load<TResource>` accepts `ImageTexture` or an assignable base such as `Texture` or `Resource`. Other concrete resource formats are not integrated and fail explicitly.

The exact path string is the ordinal, case-sensitive cache key. The process-wide `Resource.ResourcePath` cache holds only weak references; it never owns or keeps a texture alive. Loads serialize cache decisions, but callers own returned resources and may dispose them. `GetCachedRef<TResource>` returns a borrowed reference and has no separate retention protocol. `res://`, `user://` and operating-system file paths follow `FileAccess` resolution.

## Example

```csharp
using var texture = ResourceLoader.Load<ImageTexture>("res://art/player.png");
var sprite = new Sprite { Texture = texture };
// Add sprite to a scene; retain texture while the sprite borrows it.
```

## Nested enum

| Type | Role |
| --- | --- |
| [`CacheMode`](ResourceLoader.CacheMode.md) | Ignore, reuse or refresh a cached resource; deep modes equal ordinary modes for leaf images. |

## Methods

| Member | Contract |
| --- | --- |
| `public static TResource Load<TResource>(string path, CacheMode cacheMode = CacheMode.Reuse) where TResource : Resource` | Synchronously load or reuse an image texture. |
| `public static bool Exists<TResource>(string path) where TResource : Resource` | Test a live cached instance or a recognized file for the requested type. |
| `public static bool HasCached(string path)` | Test whether any live resource owns the exact registered path. |
| `public static TResource? GetCachedRef<TResource>(string path) where TResource : Resource` | Borrow a live cached resource of the requested type. |
| `public static string[] GetRecognizedExtensionsForType<TResource>() where TResource : Resource` | Return independent lowercase extensions without dots for the supported type. |

## Method descriptions

### `Load<TResource>`

The default `Reuse` returns a cached live `ImageTexture` without reading disk. A cache entry holding a different resource type throws `InvalidOperationException`. If no compatible entry exists, the loader decodes the file, copies its pixels into a texture, and registers the exact path. `Ignore` decodes independently and records a visible but unregistered path with `SetPathCache`. `Replace` decodes before mutation, then calls `SetImage` on an existing texture so sprites and materials keep their borrowed identity. If the path belongs to a different resource type, it constructs a new texture before taking over the path. Deep modes behave identically to their ordinary modes because image textures have no resource dependencies.

The caller owns the result and must dispose it when finished. `Replace` resets a previous logical size override to the decoded image dimensions, publishes `Changed` after updating pixels, and causes the renderer to upload the replacement before its next draw. A decode or validation failure leaves cached data intact. A user `Changed` handler can throw after the replacement has committed. Unsupported type/extension, malformed input, missing file, invalid cache mode and path-policy failures propagate as typed exceptions. File reads obey the existing 64 MiB codec limit.

### `Exists<TResource>`

Returns true for a live cached instance of `TResource` even if the source file has since been removed. Otherwise it checks whether the type can represent the current image-texture format, whether the extension is recognized, and whether `FileAccess.FileExists` finds the path. It checks file existence, not whether decoding would succeed. An unsupported concrete type with no matching cache entry returns false.

### `HasCached` and `GetCachedRef<TResource>`

Both read the existing `ResourcePath` registry. Manual path registration by another `Resource` is visible. Dead or disposed entries are removed during lookup. `HasCached` reports any live type; `GetCachedRef<TResource>` returns null for a missing or differently typed entry. The borrowed result can be disposed by its owner after retrieval, so callers must coordinate concurrent lifetime use.

### `GetRecognizedExtensionsForType<TResource>`

Returns `png`, `jpg`, `jpeg`, `webp`, `bmp`, `tga`, `svg` for `ImageTexture` and assignable base types. Unsupported concrete types return an empty array. Each call returns a caller-owned array.

## State, failures and scope

There is no loader-owned strong cache, manager-owned native payload or extra lease protocol. `ImageTexture` owns CPU pixels, and the existing renderer manages GPU upload and release. Loading and replacing may allocate; they are explicit asset operations outside the zero-allocation frame path. Registry and load decisions are synchronized, while returned resource state and callback use follow their own documented thread rules.

The coverage page keeps the general `ResourceLoader` type and its load/exists/extension methods Partial because only the synchronous image-texture format exists. Threaded request/status APIs, format-loader registration, UID and dependency catalogs and missing-resource policy have specific backend or file-format triggers. Resource-aware directory listing remains Unimplemented. See [ResourceLoader coverage](../coverage/classes/ResourceLoader.md) for every pinned declaration.

[ResourceLoaderTests](../../tests/Electron2D.Tests/ResourceLoaderTests.cs) verifies file decode, cache modes, same-instance replacement, type takeover, concurrent reuse, malformed rollback, path removal and disposal. Focused [Sprite pixel checks](../../tests/Electron2D.Tests/SpriteRenderingTests.cs) verify initial file loading and live Replace on Linux Wayland compatibility/GPU and dummy compatibility. Other platforms, AOT, future formats and owner asset-pipeline acceptance remain unverified.
