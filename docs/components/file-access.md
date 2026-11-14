# File and directory access component

Last updated: 2026-11-14

## Scope

This Core component owns blocking seekable file I/O, scoped directory access, directory-backed virtual paths, typed binary/text encoding, physical file metadata, Linux/macOS/Windows extended attributes, temporary file/directory ownership, content hashes, compression, and authenticated encryption.

[Caller-owned binary buffers](byte-buffers.md) share numeric conventions and the compressed container without creating a file session.

## Owned types

Native directory and extended-attribute paths use [tracked UTF8 input storage](../coverage/performance-producers.md#legacy-platform-input-conversion-2026-10-29). Linux/macOS xattr calls pin caller arrays or spans for the native call, including zero-length data.

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
2. Raw files use a `FileStream`; opt-in exact Write access uses a streaming sibling with close-time replacement through [file save and swap](file-save-and-swap.md). Transformed files use an owned expandable memory stream.
3. A directory open captures physical/resource/user scope and its current directory. Listing begin captures an unsorted entry snapshot; sorted helpers create independent immutable snapshots.
4. Every instance operation locks its stream/cursor or directory/listing state boundary.
5. Dirty compressed/encrypted content is encoded to a complete snapshot and atomically replaces its target on flush/close.
6. Disposal releases file state and optionally deletes a temporary file; directory disposal closes listing state and recursively deletes only a non-kept temporary directory it created and owns.

## Dependencies and direction

- Depends on Core object lifetime and process `ProjectSettings` path resolution.
- Shares an internal atomic-file helper with `ConfigFile`.
- Uses .NET BCL compression/cryptography/hash/file/directory/drive/link APIs, native Linux/macOS xattrs, Windows alternate data streams, native filesystem identity/case/capacity metadata, Linux mount/bookmark metadata, and macOS volume/file-package metadata.
- Ordinary files do not depend on Scene, rendering, input, audio, physics or editor. Process-pipe stream adapters use the Core OS owner and approved internal SDL process/IO binding. Opt-in staged opens use its existing native filesystem classifier without video initialization.
- Mounted PCK/ZIP resource reads share the typed binary methods and [resource pack resolver](resource-packs.md). Sparse/delta export formats retain separate decoder dependencies.

## Invariants

- No universal value or numeric error state crosses the public boundary.
- Directory creation is never implicit.
- Directory access scope is immutable; virtual roots are captured, lexically confined, and never switched by an instance. Case-sensitivity queries use read-only native metadata and do not create probe entries.
- Public directory removal is permanent and nonrecursive; link removal never traverses the target.
- Streaming enumeration order is unspecified; file/directory snapshot helpers sort ordinally.
- Caller-visible decoded content is published only after compression validation or successful authentication.
- Transformed writes replace a destination atomically after the temporary file is flushed.
- Instance calls are lock-serialized; compound caller sequences require caller synchronization.
- The component is blocking and allocating, never a real-time hot-path service.

## Operation failures and EOF

`FileAccess.Open` acquires an ordinary or virtual file stream, and `DirAccess.Open` resolves and checks the directory before returning an owner. A missing file requested for reading raises `FileNotFoundException`. A missing parent when creating a physical file or a missing directory on directory open raises `DirectoryNotFoundException`. Invalid modes or paths and denied access raise exceptions from the open call.

`OpenCompressed`, `OpenEncrypted`, and `OpenEncryptedWithPassword` validate their inputs before returning. Modes that preserve existing contents also read and decode or authenticate the existing container during open. With a truncating mode (`Write` or `WriteRead`), the factory starts with an empty memory stream and can return before creating the physical file. A dirty transformed stream encodes and publishes its contents on `Flush` or `Close`. Encoding or publication failures surface from that call, including `DirectoryNotFoundException` if the destination's parent is missing. A native bundle with basic Zstandard support can open a truncating write and later raise `PlatformNotSupportedException` during commit if it lacks support for the configured level or window.

On a seekable file, `ReadBytes` returns the available prefix. A nonzero request that runs out of bytes sets `EOFReached`. A fixed-width read that runs out consumes any available prefix, sets EOF and throws `EndOfStreamException`. Reading exactly through the last byte leaves a clear EOF flag unchanged. Seek, resize and successful writes clear it. Mode violations throw `InvalidOperationException`, and I/O failures propagate exceptions from the operation.

A live nonblocking process pipe can return a short or empty buffer with EOF clear. An incomplete exact read in that state can consume bytes before throwing `IOException`. When the reader detects that the writer has closed and the pipe is drained, `EOFReached` becomes true. A failed pipe write can throw [`PipeWriteException`](../classes/PipeWriteException.md) with the exact `BytesWritten` accepted by the pipe during that call.

The existing `ELECTRON2D_TEST_PACK_FILE_SYSTEM=1` runner checks file and directory open failures, access modes and seekable EOF transitions. `ELECTRON2D_TEST_OS_PROCESS=1` checks live pipe readiness, EOF and the integrity of writes retried from the reported byte offset.

## Text framing

`ReadLine` consumes LF, CRLF, lone CR or NUL. A final unterminated line is returned with EOF set. For seekable files, a physical terminator at the end of the file sets EOF on the next incomplete read. `ReadCSVLine` uses the same framing, normalizing terminators inside quotes to LF. An empty record has one empty field. Exhaustion returns an empty array. Misplaced and unclosed quotes raise `FormatException`. CSV uses a single nonsurrogate character as delimiter, excluding quote and line terminators.

`WriteString` writes the supplied UTF-8 bytes without adding a BOM. `WriteLine` and `WriteCSVLine` append one LF. CSV quotes fields containing the delimiter, quotes or line terminators and doubles embedded quotes. Pascal strings use an unsigned 32-bit UTF-8 byte count in the selected byte order. A representable but truncated Pascal frame consumes available bytes and raises `EndOfStreamException` on files or closed pipes. On a live nonblocking pipe, temporary short reads raise `IOException` without setting EOF. Seekable files check the remaining length before allocating, and pipes grow storage as bytes arrive.

`ReadAllText` is a seekable snapshot. It preserves the cursor and EOF on success and failure. It preserves BOM and embedded NUL as managed characters and optionally drops CR after validating UTF-8. All text writes validate UTF-16 before emitting bytes. A partial pipe write reports the consumed prefix of the complete operation, including any Pascal header or appended LF. Use `ReadBytes` for incremental framing on a nonblocking pipe.

## Current status and exclusions

Implemented for raw access, scoped directory navigation/enumeration/mutation, directory-backed `res://`/`user://`, typed binary/text methods, metadata/hashes, temporary files/directories, links, filesystem identity/case/capacity/type queries, Unix permissions, Linux/macOS/Windows extended attributes, FastLZ/DEFLATE/Zstandard/GZip/Brotli, and raw-key/password AES-GCM containers. The Linux platform backend has executable coverage. Windows x64 CLR source builds with a matching native bundle have [file, text, process-pipe, codec and resource-loader coverage](#verification). Broader Windows filesystem and directory qualification and native-host macOS verification remain open. Android and iOS are product runtime targets, but link and drive enumeration deliberately reject them until host/storage integration exists; neither mobile target has native verification or a claimed extended-attribute backend. Web is also a runtime target, but no browser host or storage integration exists; the first Web host slice must define and verify its filesystem capability boundary.

FastLZ and Zstandard reuse the unchanged, accepted native dependencies via two private file-codec exports, without an ENet host or socket. The matching source-built native bundle is required; older published bundles lack this ABI. `Electron2DNativeLibrary` explicitly selects the target bridge and is copied after package assets for repository consumers. Native compression uses 64 KiB blocks, a compressed-length prefix and SHA-256 checksum; the decoder validates record structure before allocation and checks exact output and padding. BCL decoders consume only the declared decoded length and reject surplus output. The outer E2DCMP version-1 envelope and atomic whole-file commit remain unchanged. Compression is not authentication. Sparse/delta exported pack formats and arbitrary named `pipe://` connections retain their export-decoder/named-pipe dependencies. Ordinary PCK/ZIP mounts now share resource reads, hashes, transformed streams and directory lookup. Registered UID identities/imported resources use their existing loader/registry paths. OS-created process pipes now execute through the existing FileAccess methods; registered resources and UID paths retain their current loader/registry boundaries. Exact Android storage-volume discovery requires its future host integration. Universal-value storage and shared numeric/thread-local error slots are permanent typed-C# exclusions. No empty compatibility methods exist for these gaps.

## Verification

[`FileTextTests`](../../tests/Electron2D.Tests/FileTextTests.cs) compares exact wire bytes and cursor/EOF transitions, including a fresh reader process and directory-backed and Gzip-backed files. [`OSProcessTests`](../../tests/Electron2D.Tests/OSProcessTests.cs) checks live pipe framing and verifies the SHA-256 of a Pascal frame delivered by partial-write retries.

The hidden-entry fixtures use the actual filesystem policy: dot-prefixed file and directory names on Unix, explicit `FileAttributes.Hidden` on Windows. Both fixture attributes are asserted before testing default/opt-in snapshots and streaming/concurrent filters. The Windows full suite in [run 37407321875](https://github.com/edwardgushchin/Electron2D/actions/runs/37407321875) passed configuration/file checks and exposed the previous dot-name-only fixture assumption on all three architectures. Runtime hidden-entry policy is unchanged; corrected target execution remains required.

The Linux executable harness covers file and directory success, invalid input, malformed/truncated data, wrong credentials/modes, tampering, scope/traversal rejection, listing state/filtering/concurrency, directory creation/copy/rename/removal, link target safety, filesystem identity/case/capacity/type, temporary ownership, close/disposal, concurrent independent operations, and commit failure. On Windows x64 CLR with a matching native bundle, the focused profiles verify text and binary file operations, ordinary and directory-backed virtual paths, Gzip-backed text and a fresh reader process. They also exercise blocking and nonblocking process pipes, including exact partial-write retry, 110 round trips across all five codecs, image-texture loading and cache/failure behavior, and 119 resource-pack assertions. NativeAOT, release packages and other runtime targets are outside this Windows verification. Transformed files are whole-file and have no stress-test results near managed array limits.

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

## Compression levels

`ProjectSettings.GZIPCompressionLevel` and `ProjectSettings.ZLibCompressionLevel` use the keys `compression/formats/gzip/compression_level` and `compression/formats/zlib/compression_level`. Both accept -1 through 9. The default -1 delegates to the codec, and 0 stores uncompressed blocks within the selected container. Each encode samples the effective level and active feature overrides. For files this occurs during a dirty flush or close. ByteBuffer shares the same file envelope and encoder. The zlib level also controls raw DEFLATE inside [compressed resource and scene archives](resource-files.md). Existing containers remain readable independently of the configured encoder levels. Streaming network codecs and ZIP packing retain their own policies.

The compression-level test runs on Windows and Arch Linux under WSL. It checks exact intermediate levels against the BCL, typed persistence and feature overrides, file/buffer round trips, scene and resource saves, and compressed dependency rewrites. The full FileAccess codec regression passes on Windows, including atomic replacement failure and fresh-process decoding. Other packaged targets remain unverified.

`ZstandardCompressionLevel` accepts 1–22 and defaults to 3. `ZstandardLongDistanceMatching` defaults to false. When enabled, `ZstandardWindowLogSize` sets the base-two window limit for the encoder and reader, with a 10–30 range and default 27. The native reader checks the declared frame window before one-shot decompression. A window smaller than an existing frame rejects that frame. The container preserves independent 64 KiB blocks and checksums. Effective feature overrides are sampled for each encode or decode. Nondefault encoding and window-limited decoding require the updated private native bundle.

Source-built Windows x64 and Arch Linux x64 codec bridges pass public multiblock file and buffer checks and 110 file-codec regressions per host. The Linux codec oracle independently verifies configured levels and both window acceptance and refusal. Both native allocation oracles verify cleanup at 74 failure positions. An older Windows bundle retains default operation and explicitly rejects the new options. These checks exercise the production codec bridge in isolation. Unified package and other target verification remain open.

[ZIP archives](zip-archives.md) add owned ZIPReader/ZIPPacker sessions, streamed full-level deflate, ZIP64/prefix/append preservation, portable permission/time metadata and complete length/CRC validation. Bundled scene/resource ZIP round trips execute real GPU/compatibility rendering; they do not introduce automatic extraction or pack mounts.


Mounted res:// resources now share [resource pack resolution](resource-packs.md). PCK/ZIP overlays, synthetic directories, read-only packed metadata, relative scene dependencies and bitmap-font pages use the mounted bytes. Ordinary absolute paths and user:// retain physical access. Cached resources keep their normal cache-mode policy.

[File save and swap](file-save-and-swap.md) documents the default-disabled OS policy, physical staging paths, Flush/Close distinction, links, metadata, failure recovery and PCK/OGV integration.
