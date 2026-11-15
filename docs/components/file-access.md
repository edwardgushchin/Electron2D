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
2. Raw files use a `FileStream`. Read-only physical streams share read and delete access so [file save and swap](file-save-and-swap.md) can replace the name while existing readers finish reading the original file. Physical reads through the resource resolver use the same policy, including the initial reads of transformed files. Transformed views retain decoded data in memory and release the physical handle before returning.
3. A directory open captures physical/resource/user scope and its current directory. Listing begin captures an unsorted entry snapshot; sorted helpers create independent immutable snapshots.
4. Seekable files lock stream operations and directory sessions lock listing state. Process pipes lock reads and writes separately so a thread can read while another writes to the same instance.
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
- [Binary I/O](#binary-io) defines byte order, complete scalar reads, byte buffers, cursor movement and EOF transitions for each stream capability.
- Caller-visible decoded content is published only after compression validation or successful authentication.
- Transformed writes replace a destination atomically after the temporary file is flushed.
- Callers synchronize compound operations such as seek followed by read, or a write followed by a retry on the same pipe.
- The component is blocking and allocating, never a real-time hot-path service.

## File ownership and snapshots

`Open` captures the resolved file identity and returns a stream owner. An existing physical read handle can finish reading its original file after a save-and-swap replacement. An open mounted reader retains its selected PCK or ZIP entry when another pack is mounted. `Path` preserves the caller's string and `AbsolutePath` reports the resolved native path, normalized mounted path, or save-and-swap staging path.

The static existence, size, text and digest queries each resolve the current path independently. A mounted directory or file ancestor masks physical fallback. `GetSize` reports the encoded container size for compressed or encrypted files, and the digest methods hash those same raw byte streams. `GetFileAsString` returns a strict UTF-8 snapshot that preserves BOM, NUL and CR. Independent calls can observe different versions after replacement.

Transformed `ReadWrite` reads and later commits at the same captured physical destination. A failed atomic replacement leaves the previous file in place and permits a `Flush` retry. A failing `Close` still retires the stream and releases buffers. `CreateTemp` defaults to `ReadWrite` and deletes its file at close or disposal unless `keep` is true.

## Directory enumeration

An open [DirAccess](../classes/DirAccess.md) holds its captured physical root. Resource accessors also see the live PCK/ZIP overlay while that root is the configured project root. Mounted entries take precedence at an exact-name collision. A mounted file masks a physical directory and its descendants. A mounted directory masks a physical file. Explicit ZIP directories appear even when empty. Physical absolute paths and `user://` use the filesystem.

`ListDirBegin` captures immediate names and kinds. Both include flags default to false and apply when `GetNext` advances. Windows physical hidden entries carry the Hidden attribute. Unix-family hosts also hide dot names. Mounted dot names are hidden. The navigational names `.` and `..` are governed by `IncludeNavigational`, including at virtual roots, where changing to an escaping path still fails.

`CurrentIsDir` describes the last returned entry and is false before the first entry and after EOF, end, restart, or a successful directory change. A failed begin or directory change preserves the current path and cursor. Disposal closes the listing. Calls on one owner are serialized, so concurrent advances consume distinct entries. Use caller synchronization to read a name and its kind as a pair.

`GetFiles` and `GetDirectories` return independent, ordinally sorted, read-only snapshots and leave any active cursor intact. The static `At` methods accept fully qualified native or virtual paths and use the default filters. A missing directory, a mounted file masking the directory, or denied physical enumeration raises a standard exception.

See the [coverage audit](../coverage/dir-access-listing.md) for the pinned source comparison and verification limits.

## Binary I/O

Fresh sessions default to little endian. `BigEndian` selects the byte order for multi-byte unsigned values, IEEE 754 binary16/32/64 and Pascal length prefixes. `Real` uses binary32. Binary buffers move unchanged across the stream boundary. `ReadBytes` returns a caller-owned prefix up to the requested count, and `WriteBytes` consumes its input before returning.

On seekable files, a short scalar read consumes the available prefix and throws `EndOfStreamException`. A positive buffer request can return a short prefix. Either failure to fill a read sets EOF. Reaching the end exactly leaves EOF clear, and a zero-length read preserves cursor and EOF. A successful seek, write or resize clears EOF. `Position` and `Length` observe state without moving the cursor or clearing EOF. Seeks can place the cursor beyond the end without growing the file. Resizing removes truncated bytes or appends zero bytes while preserving the absolute cursor, including a cursor beyond the new end.

Process pipes reject seek, length, position and resize with `NotSupportedException` when the direction is otherwise permitted. A nonblocking exact read that runs out of available bytes consumes the prefix and throws `IOException` until the pipe reports actual EOF. `ReadBytes` can return the prefix in either case. Closing an owned stream releases it independently of the child process. See [process pipes](processes.md).

Binary writes can throw [`PipeWriteException`](../classes/PipeWriteException.md). `BytesWritten` counts the accepted prefix of the bytes supplied to `WriteBytes` or of the encoded numeric value. For a retryable failure, use `WriteBytes` to send the unwritten suffix. For numeric values, preserve the original byte order and bit representation when preparing that suffix. Calling the numeric method again would resend any accepted bytes. `WriteByte` can only fail before its one byte is accepted. If a suffix write fails again, add its `BytesWritten` to the offset before retrying.

`ELECTRON2D_TEST_FILE_BINARY=1` checks independent little- and big-endian wire vectors and seekable cursor and error behavior through public calls. `ELECTRON2D_TEST_OS_PROCESS=1` exercises native blocking/nonblocking streams, short scalar and buffer reads, and partial-write retries with SHA-256 verification. `ELECTRON2D_TEST_FILE_TEXT=1` covers the Pascal framing that shares the byte-order setting.

## Text framing

`ReadLine` consumes LF, CRLF, lone CR or NUL. A final unterminated line is returned with EOF set. For seekable files, a physical terminator at the end of the file sets EOF on the next incomplete read. `ReadCSVLine` uses the same framing, normalizing terminators inside quotes to LF. An empty record has one empty field. Exhaustion returns an empty array. Misplaced and unclosed quotes raise `FormatException`. CSV uses a single nonsurrogate character as delimiter, excluding quote and line terminators.

`WriteString` writes the supplied UTF-8 bytes without adding a BOM. `WriteLine` and `WriteCSVLine` append one LF. CSV quotes fields containing the delimiter, quotes or line terminators and doubles embedded quotes. Pascal strings use an unsigned 32-bit UTF-8 byte count in the selected byte order. A representable but truncated Pascal frame consumes available bytes and raises `EndOfStreamException` on files or closed pipes. On a live nonblocking pipe, temporary short reads raise `IOException` without setting EOF. Seekable files check the remaining length before allocating, and pipes grow storage as bytes arrive.

`ReadAllText` is a seekable snapshot. It preserves the cursor and EOF on success and failure. It preserves BOM and embedded NUL as managed characters and optionally drops CR after validating UTF-8. All text writes validate UTF-16 before emitting bytes. A partial pipe write reports the consumed prefix of the complete operation, including any Pascal header or appended LF. Use `ReadBytes` for incremental framing on a nonblocking pipe.

## Current status and exclusions

Implemented for raw access, scoped directory navigation/enumeration/mutation, directory-backed `res://`/`user://`, typed binary/text methods, metadata/hashes, temporary files/directories, links, filesystem identity/case/capacity/type queries, Unix permissions, Linux/macOS/Windows extended attributes, FastLZ/DEFLATE/Zstandard/GZip/Brotli, and raw-key/password AES-GCM containers. The Linux platform backend has executable coverage. Windows x64 CLR source builds with a matching native bundle have [file, text, process-pipe, codec and resource-loader coverage](#verification). Broader Windows filesystem and directory qualification and native-host macOS verification remain open. Android and iOS are product runtime targets, but link and drive enumeration deliberately reject them until host/storage integration exists; neither mobile target has native verification or a claimed extended-attribute backend. Web is also a runtime target, but no browser host or storage integration exists; the first Web host slice must define and verify its filesystem capability boundary.

FastLZ and Zstandard reuse the unchanged, accepted native dependencies via two private file-codec exports, without an ENet host or socket. The matching source-built native bundle is required; older published bundles lack this ABI. `Electron2DNativeLibrary` explicitly selects the target bridge and is copied after package assets for repository consumers. Native compression uses 64 KiB blocks, a compressed-length prefix and SHA-256 checksum; the decoder validates record structure before allocation and checks exact output and padding. BCL decoders consume only the declared decoded length and reject surplus output. The outer E2DCMP version-1 envelope and atomic whole-file commit remain unchanged. Compression is not authentication. Sparse/delta exported pack formats and arbitrary named `pipe://` connections retain their export-decoder/named-pipe dependencies. Ordinary PCK/ZIP mounts now share resource reads, hashes, transformed streams and directory lookup. Registered UID identities/imported resources use their existing loader/registry paths. OS-created process pipes now execute through the existing FileAccess methods; registered resources and UID paths retain their current loader/registry boundaries. Exact Android storage-volume discovery requires its future host integration. Universal-value storage and shared numeric/thread-local error slots are permanent typed-C# exclusions. No empty compatibility methods exist for these gaps.

## Verification

[`FileAccessLifecycleTests`](../../tests/Electron2D.Tests/FileAccessLifecycleTests.cs) uses public APIs to check the physical, project, user, PCK and ZIP paths against fixed UTF-8 and digest vectors. It also exercises pack replacement, root capture, password and raw-key ownership, repeated close, temporary files and Windows sharing failures that preserve the previous encrypted file.

[`FileTextTests`](../../tests/Electron2D.Tests/FileTextTests.cs) compares exact wire bytes and cursor/EOF transitions, including a fresh reader process and directory-backed and Gzip-backed files. [`OSProcessTests`](../../tests/Electron2D.Tests/OSProcessTests.cs) checks live pipe framing and verifies the SHA-256 of a Pascal frame delivered by partial-write retries.

The hidden-entry fixtures use the actual filesystem policy: dot-prefixed file and directory names on Unix, explicit `FileAttributes.Hidden` on Windows. Both fixture attributes are asserted before testing default/opt-in snapshots and streaming/concurrent filters. The Windows full suite in [run 37407321875](https://github.com/edwardgushchin/Electron2D/actions/runs/37407321875) passed configuration/file checks and exposed the previous dot-name-only fixture assumption on all three architectures. The focused Windows x64 CLR directory-listing profile verifies the corrected hidden-entry fixtures and concurrent cursor reads. The resource-pack profile verifies ZIP directories, shared file and directory lookup, and lexical virtual-root escape. Other Windows architectures and full directory backend qualification remain open.

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
