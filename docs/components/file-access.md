# File and directory access component

Last updated: 2026-10-13

## Scope

This Core component owns blocking seekable file I/O, scoped directory access, directory-backed virtual paths, typed binary/text encoding, physical file metadata, Linux/macOS/Windows extended attributes, temporary file/directory ownership, content hashes, compression, and authenticated encryption.

## Owned types

| Type | Role |
| --- | --- |
| [`FileAccess`](../classes/FileAccess.md) | Stream ownership and complete file API |
| [`DirAccess`](../classes/DirAccess.md) | Current-directory ownership, enumeration, mutations, links, temporary directories, and filesystem queries |
| [`FileAccessModeFlags`](../classes/FileAccessModeFlags.md) | Exact open/create/truncate permissions |
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
- Ordinary files do not depend on Scene, rendering, input, audio, physics or editor. Process-pipe stream adapters use the Core OS owner and approved internal SDL process/IO binding.
- Mounted PCK/ZIP resource reads share the typed binary methods and [resource pack resolver](resource-packs.md). Sparse/delta export formats retain separate decoder dependencies.

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

Implemented for raw access, scoped directory navigation/enumeration/mutation, directory-backed `res://`/`user://`, typed binary/text methods, metadata/hashes, temporary files/directories, links, filesystem identity/case/capacity/type queries, Unix permissions, Linux/macOS/Windows extended attributes, FastLZ/DEFLATE/Zstandard/GZip/Brotli, and raw-key/password AES-GCM containers. The executable test host verifies the Linux platform backend; macOS and Windows backends compile but require native-host verification. Android and iOS are product runtime targets, but link and drive enumeration deliberately reject them until host/storage integration exists; neither mobile target has native verification or a claimed extended-attribute backend. Web is also a runtime target, but no browser host or storage integration exists; the first Web host slice must define and verify its filesystem capability boundary.

FastLZ and Zstandard reuse the unchanged, accepted native dependencies via two private file-codec exports, without an ENet host or socket. The matching source-built native bundle is required; older published bundles lack this ABI. `Electron2DENetNativeLibrary` explicitly selects the target bridge and is copied after package assets for repository consumers. Native compression uses 64 KiB blocks, a compressed-length prefix and SHA-256 checksum; the decoder validates record structure before allocation and checks exact output and padding. BCL decoders consume only the declared decoded length and reject surplus output. The outer E2DCMP version-1 envelope and atomic whole-file commit remain unchanged. Compression is not authentication. Sparse/delta exported pack formats and arbitrary named `pipe://` connections retain their export-decoder/named-pipe dependencies. Ordinary PCK/ZIP mounts now share resource reads, hashes, transformed streams and directory lookup. Registered UID identities/imported resources use their existing loader/registry paths. OS-created process pipes now execute through the existing FileAccess methods; registered resources and UID paths retain their current loader/registry boundaries. Exact Android storage-volume discovery requires its future host integration. Universal-value storage and shared numeric/thread-local error slots are permanent typed-C# exclusions. No empty compatibility methods exist for these gaps.

## Verification

The hidden-entry fixtures use the actual filesystem policy: dot-prefixed file and directory names on Unix, explicit `FileAttributes.Hidden` on Windows. Both fixture attributes are asserted before testing default/opt-in snapshots and streaming/concurrent filters. The Windows full suite in [run 37407321875](https://github.com/edwardgushchin/Electron2D/actions/runs/37407321875) passed configuration/file checks and exposed the previous dot-name-only fixture assumption on all three architectures. Runtime hidden-entry policy is unchanged; corrected target execution remains required.

The single executable harness covers file and directory success, invalid input, malformed/truncated data, wrong credentials/modes, tampering, scope/traversal rejection, listing state/filtering/concurrency, directory creation/copy/rename/removal, link target safety, filesystem identity/case/capacity/type, temporary ownership, close/disposal, concurrent independent operations, and commit failure. Platform-specific verification currently covers Linux only; transformed files are deliberately whole-file and are not stress-tested near managed array limits.

The file-codec slice additionally exercises 110 random/repeated empty, tiny, block-boundary and multi-block round trips, seek/edit/reopen, virtual paths, exact BCL decoded lengths, corrupt native records/checksums, replacement failure and a fresh reader process. A separate codec oracle compares 32 bidirectional blocks against directly loaded FastLZ/Zstandard, and actual GPU/compatibility consumers load compressed PNG bytes into sampled textures. ENet socket/oracle regressions verify the shared bridge. These checks cover Linux x64 with a matching native asset, not foreign hosts or old published bundles.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0003: ElectronObject lifetime](../decisions/core-object-runtime.md#adr-0003)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0019: Typed project settings and directory-backed virtual paths](../decisions/core-data-io.md#adr-0019)
- [0020: Typed file access and transformed-file containers](../decisions/core-data-io.md#adr-0020)
- [0021: Runtime and editor target platforms](../decisions/product.md#adr-0021)
- [0022: Typed directory access](../decisions/core-data-io.md#adr-0022)

[Process pipes](processes.md) now add owned non-seekable stdin/stdout/stderr, blocking/nonblocking reads/writes, exact temporary readiness versus EOF and consumed-prefix exceptions. These streams retain native process lifetime and close independently of child termination.

## ZIP asset preparation

[ZIP archives](zip-archives.md) add owned ZIPReader/ZIPPacker sessions, streamed full-level deflate, ZIP64/prefix/append preservation, portable permission/time metadata and complete length/CRC validation. Bundled scene/resource ZIP round trips execute real GPU/compatibility rendering; they do not introduce automatic extraction or pack mounts.


Mounted res:// resources now share [resource pack resolution](resource-packs.md). PCK/ZIP overlays, synthetic directories, read-only packed metadata, relative scene dependencies and bitmap-font pages use the mounted bytes. Ordinary absolute paths and user:// retain physical access. Cached resources keep their normal cache-mode policy.
