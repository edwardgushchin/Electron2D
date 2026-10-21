# Resource loading component

Last updated: 2026-10-21

## Scope and owned types

[`ResourceLoader`](../classes/ResourceLoader.md) and its [`CacheMode`](../classes/ResourceLoader.CacheMode.md) enum provide the first synchronous resource-file path: six integrated image decoders produce an `ImageTexture` that the current Sprite and renderer can draw. This component uses the existing Resource weak path cache and the typed archive/format extension layer below. The threaded profile now runs isolated preparation and owner publication; general import remapping remains a separate dependency.

## Runtime flow and ownership

`Load<ImageTexture>` validates the cache mode and asks `Image.LoadFromFile` to decode through `FileAccess`. The codec limit and path policy apply unchanged. A successful decode is copied into an `ImageTexture` and is registered or given an unregistered visible path according to cache mode. `Reuse` returns a live cached texture immediately; `Replace` decodes first and refreshes the same texture through `SetImage`. A different cached type can be displaced only after the new texture is ready. The returned wrapper is caller-owned; weak registration does not retain it. Disposal unregisters the path.

The GPU payload is managed by the existing texture renderer when the resource is drawn. A Sprite or Material borrows the texture wrapper and sees its `Changed` notification on replacement. No separate lease or manager-owned native payload is introduced by this first file format. Cache decisions are serialized; the loader does not synchronize arbitrary caller changes to a returned resource.

## Invariants, failures and remaining work

- An exact nonempty path has at most one live registered owner. Ignore modes do not claim ownership.
- Cache modes have their pinned numeric identities. Deep modes equal ordinary modes for dependency-free image files.
- Malformed input, unsupported types/extensions and invalid paths fail explicitly. Failed decoding preserves a cached texture and its pixels; callback failure can propagate after committed replacement.
- `Exists` may return true for a cached resource after its source file is removed. Discovery returns caller-owned extension arrays.
- Further concrete resource formats retain their own coverage triggers. Generic, mesh and ordinary-texture tolerant archives execute under the [resource file contract](resource-files.md#missing-resource-recovery). Generic load/exists/discovery remain Partial across the full applicable asset API.

## Verification

[ResourceLoaderTests](../../tests/Electron2D.Tests/ResourceLoaderTests.cs) checks native file decoding, cache identity, replacement, independent ignores, wrong-type takeover, concurrent reuse, malformed rollback and disposal. [SpriteRenderingTests](../../tests/Electron2D.Tests/SpriteRenderingTests.cs) verifies pixels from a loaded PNG and after file replacement on Linux Wayland compatibility/GPU and dummy compatibility. Other platforms, AOT, further formats and owner acceptance remain unverified.

## Decisions

- [ADR 0013: Managed typed Resource and first loader profile](../decisions/resources.md#adr-0013)
- [ADR 0014: Resource lifetime and hot paths](../decisions/resources.md#adr-0014)
- [ADR 0039: Image codecs](../decisions/resources.md#adr-0039)

## Dynamic font files

[FontFile](../classes/FontFile.md) shares the same serialized path-cache decision and caller ownership used by image textures. `Load<FontFile>` and compatible Font/Resource views create an actual decoded font; Ignore yields an independent instance, Reuse preserves the cached wrapper and Replace validates bytes before updating the same wrapper. The font source owns native faces and glyph textures; the loader never owns or leases them. `ttf`, `otf`, `woff`, `woff2`, `ttc` and `otc` participate in typed extension discovery. Bitmap fonts, system discovery and threaded loading retain their separate dependencies. [FontResourceLoaderTests](../../tests/Electron2D.Tests/FontResourceLoaderTests.cs) verifies WOFF2 data, concurrent reuse, rollback and cache lifetime.

## Audio files

WAV, MP3 and Ogg Vorbis participate through exact concrete types and compatible AudioStream/Resource base views. Discovery reports wav/mp3/ogg. Uncached existence requires a compatible extension; format mismatch rejects explicit audio loads. Reuse borrows the live cached identity; Ignore and IgnoreDeep preserve that identity while returning independent owned resources. Replace/ReplaceDeep validate before publishing into the same concrete audio wrapper and issue Changed afterward. A malformed file preserves old state; a throwing Changed callback follows committed replacement. Ogg reload owns an independent imported packet copy and retains retired imports for old playback captures. There is no external dependency graph in these file formats, so their deep cache modes equal ordinary modes. AudioCompressedTests checks identity, independent ignores, retained playback after reload, malformed rollback and typed discovery. The typed archive producer below adds file scenes and public format registration.

## Certificate/private-key loading

X509Certificate `.crt` and CryptoKey `.key` files now execute through ResourceLoader with typed discovery, Exists, weak cache reuse, independent ignore and decode-before-replace that preserves cached metadata. PEM and DER input use the resource parsers; malformed replacement preserves the cached payload, and active TLS use rejects replacement. [TLSTests](../../tests/Electron2D.Tests/TLSTests.cs) verifies this integration. See [TLS](tls.md) for certificate/key ownership, duplication, security and backend boundaries. Archive saving does not add certificate/key schemas or editor persistence.

## Typed archive files

ResourceSaver, ResourceUID, ResourceFormatSaver, ResourceFormatLoader and ResourceFileTypes implement the [typed resource-file component](resource-files.md). `.e2dres` and `.e2dscene` preserve registered compiled types, stored typed properties, graph aliases/cycles, scene hierarchy/owners/groups, relative node references and UID/external dependency identity. Image/texture pixel snapshots, font data/features/fallback arrays, style boxes, PhysicsMaterial and the four scalar shapes have built-in resource schemas. Other concrete resource types need their own stored schema and factory integration; no unknown state is silently encoded by assembly reflection.

Ordinary Ignore/Replace reuses external dependencies; IgnoreDeep/ReplaceDeep propagates the policy. Newly decoded internal and deep-ignore resources belong to the file root. An instantiated scene retains its file graph until its root is disposed; Replace retires the old graph without invalidating old instances. Decode failure attempts all owned cleanup before publishing cache identity. Root refresh uses the existing CopyFromResource contract: custom copy/setter or Changed failure can occur after mutation and is not a transaction for arbitrary application state.

Ordered borrowed format extensions support front priority, deduplication, metadata, UID/dependencies, exact type discovery and dependency rewrite. File operations allocate and execute synchronously on the initiating thread. The weak cache still owns no resource. ResourceArchiveTests exercises public save/load, fresh-process scene lifecycle, deep ownership, font/theme/pixels/geometry, custom formats, replacement and malformed boundaries. Rendered batches, editor UI, project CLI, all-platform validation, arbitrary import formats and human acceptance remain separate gates.

## Compiled C# source assets

Registered `.cs` sources load as Script resources through portable-PDB source/type and checksum validation. Reuse/Ignore/Replace retain ordinary weak-cache policy; malformed, changed or unknown compiled associations fail before compatible cache replacement. Source saving is atomic UTF-8 and does not reload CLR code. Host registration shares ResourceFileTypes exact factories and typed schemas with scene/resource reconstruction. See [Scripting](scripting.md) for the executable workflow, symbol prerequisites and live reload boundary.

[Threaded resource loading](threaded-resource-loading.md) executes the WorkerThreadPool prerequisite under ADR 0013/0104. Preparation does not mutate registered cache objects; owner publication preserves cache modes and graph leases, and private remapping handles stored/PackedScene/shader references. AsyncGallery verifies separately authored scenes with real background dependencies and native rendered output.

## Resource directory discovery

ResourceLoader.ListDirectory returns a caller-owned, ordinally sorted snapshot of recognized resource leaf names and immediate subdirectories ending in `/`. It uses DirAccess path roots and hides hidden/navigational entries. Native relative paths, res://, user:// and registered directory UID paths follow the existing directory policy; invalid/root-escaping paths and enumeration errors propagate.

Live format-loader RecognizePath overrides participate alongside built-in image/font/audio/key/certificate support. Registration is captured once; removed/disposed loaders do not participate in later calls. Recognition does not decode, validate contents or consult the weak cache. Files removed from disk are absent even if a cached resource remains. The accepted native resource format preserves its filenames in deployment; no foreign import/remap sidecar generates a synthetic loadable name. An import/export resolver must own original-name reconstruction when that format exists.

ResourceDirectoryTests saves a scene and curve, discovers them and loads/runs the scene in a fresh process. It checks virtual roots, hidden entries, case-insensitive built-in extensions, plugin path overrides/removal/disposal/failure, result independence, invalid paths and missing directories. This is blocking authoring/discovery work and allocates; it is not a frame-loop operation.


## Custom image decoder integration

[ImageFormatLoader](../classes/ImageFormatLoader.md) and [ImageFormatLoaderExtension](../classes/ImageFormatLoaderExtension.md) provide caller-owned file decoder registration. Custom suffixes participate in Image loading, resource extension/existence/directory discovery, synchronous/threaded ImageTexture loading and cache replacement. Ordered callbacks execute outside the registry lock with borrowed temporary Image/FileAccess objects; false means unrecognized content, exceptions preserve the destination, and success copies pixels before one Changed notification. Encoded input retains the 64 MiB limit. Registration is process-local and must precede fresh-host file loading. Keep decoders alive through pending worker calls; removal affects subsequent snapshots. Built-in buffer codecs and their channel/color metadata limits remain separate.

The [EXR I/O slice](../components/image-exr.md) supplies precision-preserving HDR file/buffer load and lossless save. Image and texture resource discovery includes DDS and EXR alongside PNG/JPEG/WebP/BMP/TGA/SVG; typed archives also expose their registered extensions. GPU float sampling, explicit Compatibility byte conversion and float-driver rejection are separate exercised profiles.


Mounted res:// resources now share [resource pack resolution](resource-packs.md). PCK/ZIP overlays, synthetic directories, read-only packed metadata, relative scene dependencies and bitmap-font pages use the mounted bytes. Ordinary absolute paths and user:// retain physical access. Cached resources keep their normal cache-mode policy.

## Localized resource files

Ordered locale candidates now select supported backing files before decode while retaining logical cache/path/UID identity. Live resources reload before translation notification, including associated Ignore instances; missing variants fall back and malformed data leaves previous payloads intact. Threaded requests capture the same immutable plan and reject stale mapped work before publication. See [localized resources](localized-resources.md).
