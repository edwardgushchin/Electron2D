# Resource loading component

Last updated: 2026-10-05

## Scope and owned types

[`ResourceLoader`](../classes/ResourceLoader.md) and its [`CacheMode`](../classes/ResourceLoader.CacheMode.md) enum provide the first synchronous resource-file path: six integrated image decoders produce an `ImageTexture` that the current Sprite and renderer can draw. This component uses the existing Resource weak path cache and the typed archive/format extension layer below. Threaded workers and general import remapping remain separate dependencies.

## Runtime flow and ownership

`Load<ImageTexture>` validates the cache mode and asks `Image.LoadFromFile` to decode through `FileAccess`. The codec limit and path policy apply unchanged. A successful decode is copied into an `ImageTexture` and is registered or given an unregistered visible path according to cache mode. `Reuse` returns a live cached texture immediately; `Replace` decodes first and refreshes the same texture through `SetImage`. A different cached type can be displaced only after the new texture is ready. The returned wrapper is caller-owned; weak registration does not retain it. Disposal unregisters the path.

The GPU payload is managed by the existing texture renderer when the resource is drawn. A Sprite or Material borrows the texture wrapper and sees its `Changed` notification on replacement. No separate lease or manager-owned native payload is introduced by this first file format. Cache decisions are serialized; the loader does not synchronize arbitrary caller changes to a returned resource.

## Invariants, failures and remaining work

- An exact nonempty path has at most one live registered owner. Ignore modes do not claim ownership.
- Cache modes have their pinned numeric identities. Deep modes equal ordinary modes for dependency-free image files.
- Malformed input, unsupported types/extensions and invalid paths fail explicitly. Failed decoding preserves a cached texture and its pixels; callback failure can propagate after committed replacement.
- `Exists` may return true for a cached resource after its source file is removed. Discovery returns caller-owned extension arrays.
- Further concrete resource formats, threaded loading, tolerant missing-resource policy and resource-aware directory listing have separate coverage triggers. Generic load/exists/discovery remain Partial across the full applicable asset API.

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
