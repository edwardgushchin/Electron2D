# ResourceLoader

Last updated: 2026-10-02

**Inherits:** None; static C# service

- **Source:** [ResourceLoader.cs](../../src/Core/IO/ResourceLoader.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public static class ResourceLoader`
- **Component:** [Resource loading](../components/resource-loading.md)

## Description

The first synchronous loader profile turns a PNG, JPEG, WebP, BMP, TGA or SVG file into a caller-owned `ImageTexture`. Decoding uses `Image.LoadFromFile`; the texture owns a copied pixel snapshot and uploads GPU data when drawn. `Load<TResource>` accepts `ImageTexture` or an assignable base such as `Texture` or `Resource`. Dynamic [FontFile](FontFile.md) loading also executes for SFNT font files through [Font](Font.md) or Resource base views. Other concrete resource formats remain separate integrations and fail explicitly.

The exact path string is the ordinal, case-sensitive cache key. The process-wide `Resource.ResourcePath` cache holds only weak references; it never owns or keeps a resource alive. Loads serialize cache decisions, but callers own returned resources and may dispose them. `GetCachedRef<TResource>` returns a borrowed reference and has no separate retention protocol. `res://`, `user://` and operating-system file paths follow `FileAccess` resolution.

## Example

```csharp
using var texture = ResourceLoader.Load<ImageTexture>("res://art/player.png");
var sprite = new Sprite { Texture = texture };
// Add sprite to a scene; retain texture while the sprite borrows it.
```

## Nested enum

| Type | Role |
| --- | --- |
| [`CacheMode`](ResourceLoader.CacheMode.md) | Ignore, reuse or refresh a cached resource; deep modes equal ordinary modes for leaf images and dynamic font files. |

## Methods

| Member | Contract |
| --- | --- |
| `public static TResource Load<TResource>(string path, CacheMode cacheMode = CacheMode.Reuse) where TResource : Resource` | Synchronously load or reuse an image texture or dynamic font file. |
| `public static bool Exists<TResource>(string path) where TResource : Resource` | Test a live cached instance or a recognized file for the requested type. |
| `public static bool HasCached(string path)` | Test whether any live resource owns the exact registered path. |
| `public static TResource? GetCachedRef<TResource>(string path) where TResource : Resource` | Borrow a live cached resource of the requested type. |
| `public static string[] GetRecognizedExtensionsForType<TResource>() where TResource : Resource` | Return independent lowercase extensions without dots for the supported type. |

## Method descriptions

### `Load<TResource>`

The default `Reuse` returns a cached live `ImageTexture` or `FontFile` without reading disk. A cache entry holding a different resource type throws `InvalidOperationException`. If no compatible entry exists, the loader decodes the file, copies its pixels into a texture, and registers the exact path. `Ignore` decodes independently and records a visible but unregistered path with `SetPathCache`. `Replace` decodes before mutation, then calls `SetImage` on an existing texture so sprites and materials keep their borrowed identity. If the path belongs to a different resource type, it constructs a new texture before taking over the path. Deep modes behave identically to their ordinary modes for the currently integrated leaf image/font file formats. Font replacement calls LoadDynamicFont atomically on an existing FontFile and retains its borrowed fallback configuration; it does not recursively load a fallback file graph.

The caller owns the result and must dispose it when finished. `Replace` resets a previous logical size override to the decoded image dimensions, publishes `Changed` after updating pixels, and causes the renderer to upload the replacement before its next draw. A decode or validation failure leaves cached data intact. A user `Changed` handler can throw after the replacement has committed. Unsupported type/extension, malformed input, missing file, invalid cache mode and path-policy failures propagate as typed exceptions. File reads obey the existing 64 MiB codec limit.

### `Exists<TResource>`

Returns true for a live cached instance of `TResource` even if the source file has since been removed. Otherwise it checks whether the type can represent the current image or font format, whether the extension is recognized, and whether `FileAccess.FileExists` finds the path. It checks file existence, not whether decoding would succeed. An unsupported concrete type with no matching cache entry returns false.

### `HasCached` and `GetCachedRef<TResource>`

Both read the existing `ResourcePath` registry. Manual path registration by another `Resource` is visible. Dead or disposed entries are removed during lookup. `HasCached` reports any live type; `GetCachedRef<TResource>` returns null for a missing or differently typed entry. The borrowed result can be disposed by its owner after retrieval, so callers must coordinate concurrent lifetime use.

### `GetRecognizedExtensionsForType<TResource>`

Returns `png`, `jpg`, `jpeg`, `webp`, `bmp`, `tga`, `svg` for `ImageTexture` and assignable base types. FontFile and Font base views return `ttf`, `otf`, `woff`, `woff2`, `ttc`, `otc`; Resource combines both families. Unsupported concrete types return an empty array. Each call returns a caller-owned array.

## State, failures and scope

There is no loader-owned strong cache, manager-owned native payload or extra lease protocol. `ImageTexture` owns CPU pixels, and the existing renderer manages GPU upload and release. Loading and replacing may allocate; they are explicit asset operations outside the zero-allocation frame path. Registry and load decisions are synchronized, while returned resource state and callback use follow their own documented thread rules.

The coverage page keeps the general `ResourceLoader` type and its load/exists/extension methods Partial because only the synchronous image-texture format exists. Threaded request/status APIs, format-loader registration, UID and dependency catalogs and missing-resource policy have specific backend or file-format triggers. Resource-aware directory listing remains Unimplemented. See [ResourceLoader coverage](../coverage/classes/ResourceLoader.md) for every pinned declaration.

[ResourceLoaderTests](../../tests/Electron2D.Tests/ResourceLoaderTests.cs) verifies file decode, cache modes, same-instance replacement, type takeover, concurrent reuse, malformed rollback, path removal and disposal. Focused [Sprite pixel checks](../../tests/Electron2D.Tests/SpriteRenderingTests.cs) verify initial file loading and live Replace on Linux Wayland compatibility/GPU and dummy compatibility. Other platforms, AOT, future formats and owner asset-pipeline acceptance remain unverified.

## Font file integration

A font owns its validated encoded bytes and native glyph data. Ignore returns a distinct independent FontFile without replacing a cached identity. Reuse does not reread a live cached font; Replace and ReplaceDeep validate the new file before committing to the same font. Malformed input preserves the old character map, metrics and layout. Disposing the font removes its weak path entry and retires its owned glyph state. Extension discovery describes the supported SFNT containers; it does not promise system-font search or bitmap-font import. [FontResourceLoaderTests](../../tests/Electron2D.Tests/FontResourceLoaderTests.cs) verifies actual WOFF2 loading, base views, concurrent reuse, replacement, rollback and weak-cache lifetime.

## Audio files

WAV, MP3 and Ogg Vorbis participate through exact concrete types and compatible AudioStream/Resource base views. Discovery reports wav/mp3/ogg. Uncached existence requires a compatible extension; format mismatch rejects explicit audio loads. Reuse borrows the live cached identity; Ignore and IgnoreDeep preserve that identity while returning independent owned resources. Replace/ReplaceDeep validate before publishing into the same concrete audio wrapper and issue Changed afterward. A malformed file preserves old state; a throwing Changed callback follows committed replacement. Ogg reload owns an independent imported packet copy and retains retired imports for old playback captures. There is no external dependency graph in these file formats, so their deep cache modes equal ordinary modes. AudioCompressedTests checks identity, independent ignores, retained playback after reload, malformed rollback and typed discovery. General scene-file serialization and public format registration remain their own coverage dependencies.
