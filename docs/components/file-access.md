# File and directory access component

Last updated: 2026-09-22

## Scope

This Core component owns blocking seekable file I/O, scoped directory access, directory-backed virtual paths, typed binary/text encoding, physical file metadata, Linux/macOS/Windows extended attributes, temporary file/directory ownership, content hashes, compression, and authenticated encryption.

## Owned types

| Type | Role |
| --- | --- |
| [`FileAccess`](../classes/FileAccess.md) | Stream ownership and complete file API |
| [`DirAccess`](../classes/DirAccess.md) | Current-directory ownership, enumeration, mutations, links, temporary directories, and filesystem queries |
| [`FileAccessMode`](../classes/FileAccessMode.md) | Exact open/create/truncate permissions |
| [`FileCompressionMode`](../classes/FileCompressionMode.md) | Container codec identity |
| [`UnixPermissionFlags`](../classes/UnixPermissionFlags.md) | Typed Unix mode bits |

Sources live in [`src/Core/IO/`](../../src/Core/IO/).

## Runtime flow

1. A file open resolves the path through `ProjectSettings`, validates the exact mode and transformation inputs, and acquires/decodes the content before returning an instance.
2. Raw files use a `FileStream`; transformed files use an owned expandable memory stream.
3. A directory open captures physical/resource/user scope and its current directory. Listing begin captures an unsorted entry snapshot; sorted helpers create independent immutable snapshots.
4. Every instance operation locks its stream/cursor or directory/listing state boundary.
5. Dirty compressed/encrypted content is encoded to a complete snapshot and atomically replaces its target on flush/close.
6. Disposal releases file state and optionally deletes a temporary file; directory disposal closes listing state and recursively deletes only a non-kept temporary directory it created and owns.

## Dependencies and direction

- Depends on Core object lifetime and process `ProjectSettings` path resolution.
- Shares an internal atomic-file helper with `ConfigFile`.
- Uses .NET BCL compression/cryptography/hash/file/directory/drive/link APIs, native Linux/macOS xattrs, Windows alternate data streams, native filesystem identity/case/capacity metadata, Linux mount/bookmark metadata, and macOS volume/file-package metadata.
- Does not depend on Scene, SDL, rendering, input, audio, physics, scripting, editor, or resource loading.
- Future pack mounts/resource loaders may provide an archive-backed path backend without changing typed binary methods.

## Invariants

- No universal value or numeric error state crosses the public boundary.
- Directory creation is never implicit.
- Directory access scope is immutable; virtual roots are captured, lexically confined, and never switched by an instance. Case-sensitivity queries use read-only native metadata and do not create probe entries.
- Public directory removal is permanent and nonrecursive; link removal never traverses the target.
- Streaming enumeration order is unspecified; file/directory snapshot helpers sort ordinally.
- Fixed-size values either read completely or fail; EOF records only an attempted incomplete read.
- Caller-visible decoded content is published only after compression validation or successful authentication.
- Transformed writes replace a destination atomically after the temporary file is flushed.
- Instance calls are lock-serialized; compound caller sequences require caller synchronization.
- The component is blocking and allocating, never a real-time hot-path service.

## Current status and exclusions

Implemented for raw access, scoped directory navigation/enumeration/mutation, directory-backed `res://`/`user://`, typed binary/text methods, metadata/hashes, temporary files/directories, links, filesystem identity/case/capacity/type queries, Unix permissions, Linux/macOS/Windows extended attributes, DEFLATE/GZip/Brotli, and raw-key/password AES-GCM containers. The executable test host verifies the Linux platform backend; macOS and Windows backends compile but require native-host verification. Android and iOS are product runtime targets, but link and drive enumeration deliberately reject them until host/storage integration exists; neither mobile target has native verification or a claimed extended-attribute backend. Web is also a runtime target, but no browser host or storage integration exists; the first Web host slice must define and verify its filesystem capability boundary.

FastLZ and Zstandard require codec providers that are not integrated. Packed/exported archive filesystems, import remapping, `uid://` identities, and `pipe://` streams require absent pack, resource-loader, resource-UID, and platform-pipe domains. Exact Android storage-volume discovery requires its future host integration. Universal-value storage and shared numeric/thread-local error slots are permanent typed-C# exclusions. No empty compatibility methods exist for these gaps.

## Verification

The single executable harness covers file and directory success, invalid input, malformed/truncated data, wrong credentials/modes, tampering, scope/traversal rejection, listing state/filtering/concurrency, directory creation/copy/rename/removal, link target safety, filesystem identity/case/capacity/type, temporary ownership, close/disposal, concurrent independent operations, and commit failure. Platform-specific verification currently covers Linux only; transformed files are deliberately whole-file and are not stress-tested near managed array limits.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0003: ElectronObject lifetime](../decisions/core-object-runtime.md#adr-0003)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0019: Typed project settings and directory-backed virtual paths](../decisions/core-data-io.md#adr-0019)
- [0020: Typed file access and transformed-file containers](../decisions/core-data-io.md#adr-0020)
- [0021: Runtime and editor target platforms](../decisions/product.md#adr-0021)
- [0022: Typed directory access](../decisions/core-data-io.md#adr-0022)
