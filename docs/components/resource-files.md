# Typed resource and scene files

Last updated: 2026-10-05

## Scope and owned types

ResourceSaver and ResourceUID are permanent retained services with static operations. ResourceLoader now retains its ordered file-extension state on the same service model. ResourceFormatSaver/ResourceFormatLoader are ordinary caller-owned extensions; registration borrows them. ResourceFileTypes registers compiled factories and value codecs on retained ResourceLoader registry state. SaverFlags controls dependency paths, embedding, temporary paths, editor omission, byte order, compression and committed subresource paths.

The runtime producer writes `.e2dres` and `.e2dscene`. It supports registered typed schemas rather than arbitrary object serialization. Built-in resource factories cover Resource, PackedScene, PhysicsMaterial, Image, ImageTexture, AtlasTexture, FontFile, StyleBoxFlat/Line/Texture/Empty and Circle/Capsule/Segment/RectangleShape. The explicit node factory list is [ResourceFileTypes.Nodes.cs](../../src/Core/IO/ResourceFileTypes.Nodes.cs); application types register a static exact-type factory in every saving/loading process. Every persisted field uses a writable IsStored descriptor or dedicated metadata/payload schema. Application authors must opt in their state; registration alone does not discover fields.

## Runtime flow

1. Save snapshots the ordered format list, validates resource/path/flags and rejects saving the same resource recursively. Recognition chooses eligible extensions; failed candidates fall through and final failures aggregate. ChangePath exposes a temporary localized destination and restores both its old path and cache-registration role.
2. The archive encoder visits graph identities, writes external UID/type/fallback paths or embeds resources, and encodes typed properties plus PackedScene node tables. It preserves aliases and resource cycles. Reserved `__editor` properties can be omitted. Subresource IDs must be unique within the archive.
3. Encoding/checks complete before a temporary sibling file is flushed and atomically replaces the destination. The saved UID is registered and ReplaceSubresourcePaths applies only after replacement. Unsupported schema/codec failures preserve the old destination.
4. Loading validates version, lengths, flags, UID, bounded decompression and SHA-256 before allocating resource factories. All internal identities are allocated before properties; declared schema types and reference indices are checked. Scene tables validate parent/owner topology, names and duplicate sibling names.
5. Cache publication occurs after decode. Reuse returns the existing compatible wrapper. Ignore leaves its path unregistered; Replace refreshes the same exact type through CopyFromResource. Ordinary modes reuse external dependencies; Deep modes propagate. External cycles between files reject by canonical absolute path with a 64-file depth bound and require bundling.
6. Internal resources and newly loaded external dependencies belong to the decoded root. PackedScene instances retain the graph through private disposable leases. Copies of file roots retain their source graph for borrowed stored state; failed arbitrary custom copying retains both graphs until disposal or a subsequent complete replacement. Instantiation retains the source graph before invoking application factories. Template Replace retires its old graph; old instances keep it until their final root disposal. Node.ReplaceBy transfers these leases alongside local resource ownership. Reused dependencies remain borrowed.

## Version 1 encoding

The fixed 64-byte header uses little-endian fields: magic `E2DASSET` (0..7), version 1 (8..11), save flags (12..15), nonnegative Int64 UID (16..23), raw length (24..27), encoded length (28..31), SHA-256 of raw payload (32..63). The body is plain or bounded Deflate; SaveBigEndian changes numeric payload order, including property substreams. The total raw/encoded budget is 64 MiB. Resource and node tables are limited to 65,536 entries; value arrays have a 1,048,576 element bound and resource arrays/dictionaries use 65,536.

The payload starts with an Int32 resource count. Each entry stores stable type ID, resource name, local-scene byte, scene ID, external fallback path, Int64 external UID and length-prefixed payload. Strings use an Int32 UTF16 code-unit count followed by UInt16 code units in payload byte order. This preserves embedded NUL, BOM and unpaired CLR surrogates; the networking StreamPeer text decoder has different NUL/BOM behavior and is not the file decoder. An empty fallback denotes an internal definition; the root must be internal. External records carry no internal payload. Internal property records store name, stable value-type ID and a length-prefixed typed value. PackedScene records include registered node ID, name, parent/owner/sibling indices, persistent groups and typed property records. Image and ImageTexture use validated raw pixel schemas; font bytes/configuration use typed stored descriptors. Unsupported versions reject; there is no automatic migration or assembly loading.

Encoding is ordered and a saved UID is retained on subsequent writes. Binary fields can be inspected through typed metadata queries, Resource properties and SceneState. Human-oriented text authoring/diff export is a future tooling capability; no JSON or foreign scene-file interoperability is claimed.

## Typed values, UID and extensions

Built-in codecs cover primitive integer/floating values, strings, six vectors, Color, rectangles, Transform, selected nullable scalar/math values, byte/string/int/float/Vector2/Color arrays, int contours, resource arrays and string/int dictionaries. Enums preserve their numeric typed values. Node references are relative paths restored after topology construction. Custom codecs are explicit schema contracts and must validate input; reference-shaped custom values are snapshotted through their codec. Native pointers, RID, delegates and arbitrary ElectronObject values reject.

ResourceUID uses canonical base-34 `uid://` text and a process-local catalog with explicit Add/Set/Remove. Saved file headers retain identities between processes; loading or GetResourceIDForPath imports them. CreateIDForPath hashes project name and path with SHA-256. No background import scan, catalog disk database or arbitrary relocation discovery exists. A registered UID resolves before FileAccess/DirAccess/ProjectSettings directory path policy; unknown UIDs throw. Dependency tokens retain UID, optional stable type and fallback path; RenameDependencies rewrites metadata atomically without resource construction.

Formats are invoked synchronously from copied registration lists. Duplicate identities do not reorder entries; atFront gives new registrations priority. Disposed extensions are skipped; removal does not dispose them. Metadata queries honor the same priority. Custom loaders return live compatible resources; exact-type Replace copies into the existing wrapper. Custom copy/setter and Changed failures can occur after mutation under the existing Resource contract. Custom savers own their format's atomicity and validation; only the built-in archive implements the version-1 transaction above.

## Verification and remaining dependencies

[ResourceArchiveTests](../../tests/Electron2D.Tests/ResourceArchiveTests.cs) exercises endian/compression flags, graph aliases/cycles, resource/node schema, UID/dependency fallback, cache identity, deep ownership, file scene change/reload/spawner registration, pixel and font/layout payloads, UI theme reconstruction, scalar shape geometry, old-instance graph retention, custom-format priority/fallback/Replace, malformed headers/checksum, wrong types and atomic dependency rewrite. A separate process registers the compiled schemas, reads the saved file, instantiates it and runs ordinary SceneTree lifecycle. These are public runtime authoring and headless checks, not rendered/editor acceptance.

Further resource payloads require concrete stored schemas and factories, particularly audio imports, curves, materials, mesh/animation data and full Theme resources; existing leaf audio/security loading does not make their archive embedding implemented. Threaded load tokens/jobs, tolerant missing-resource placeholders, exported resource packs, import remapping, persistent event endpoints, inherited scene authoring, project CLI/editor and text diff tools retain separate triggers. All file/capture/registration operations allocate outside the frame path. Linux x64 and native font layout execute locally; foreign hosts, AOT, rendered archive scenes, unmeasured native allocations and human acceptance remain unverified.

## Decisions

- [ADR 0013](../decisions/resources.md#adr-0013): typed schemas, formats and file identities.
- [ADR 0014](../decisions/resources.md#adr-0014): private ownership leases and allocation boundary.
- [ADR 0023](../decisions/scene.md#adr-0023): stored scene model and detached lifecycle.
- [ADR 0090](../decisions/agent-native.md#adr-0090): fresh-process authoring evidence and verification limits.
- [ADR 0095](../decisions/singleton-services.md#adr-0095): static operations over retained objects.

Exported SceneState views also retain their file-backed graph snapshot through a private lease, including after template disposal. Dispose such a view when finished. Internal unexported views retain no extra ownership; later state/content transitions update exported-view retention. ResourceArchiveTests verifies the final resource snapshot and deterministic release.

Internal subresource paths are visible metadata; direct reload by a `file::subresource` path remains a separate ResourceLoader dependency requiring identity-table lookup with ownership rebasing. Load the file root and use its typed properties/SceneState in the current profile.
