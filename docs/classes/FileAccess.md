# FileAccess

Last updated: 2026-09-21

## Source and declaration

- Source: [`src/Core/IO/FileAccess.cs`](../../src/Core/IO/FileAccess.cs)
- Declaration: `public sealed class FileAccess : ElectronObject`
- Assembly and namespace: `Electron2D.dll`, `Electron2D`

## Responsibility and ownership

`FileAccess` owns one seekable physical stream or decoded in-memory view. It supplies blocking binary/text I/O, virtual-path resolution, file metadata, hashing, Linux/macOS/Windows extended attributes, temporary files, whole-file compression, and authenticated whole-file encryption. The instance exclusively owns and closes its stream; it never owns `ProjectSettings`.

## Complete public API

### State and metadata

| Member | Current behavior |
| --- | --- |
| `BigEndian` | Selects big-endian multi-byte numeric and Pascal-length encoding; false by default |
| `Path`, `AbsolutePath` | Original caller path and normalized resolved OS path; remain diagnostic after close/disposal |
| `IsOpen` | True while a stream is owned; disposed access throws |
| `EofReached` | True only after an incomplete read; successful seek clears it |
| `Position`, `Length` | Decoded byte cursor and decoded byte length |
| `Hidden`, `ReadOnly`, `UnixPermissions` | Instance access to physical file attributes/mode bits; hidden/read-only attributes are exposed only on Apple, BSD, and Windows platforms |

### Creation and lifetime

`Open(path, mode)`, `CreateTemp(mode = ReadWrite, prefix = "", extension = "", keep = false)`, `OpenCompressed(path, mode, compressionMode = FastLz)`, `OpenEncrypted(path, mode, key)`, `OpenEncryptedWithPassword(path, mode, password)`, `Flush()`, `Close()`, and inherited `Dispose()`.

`Close` is idempotent, commits dirty transformed data, releases the stream and sensitive buffers even on failure, and deletes non-kept temporary files. Disposal invokes the same close path. A failed commit is surfaced and never leaves the instance reporting an open stream.

### Cursor and binary/text I/O

- Cursor/size: `Seek(long)`, `SeekEnd(long = 0)`, `Resize(long)`.
- Numeric reads: `ReadByte`, `ReadUInt16`, `ReadUInt32`, `ReadUInt64`, `ReadHalf`, `ReadSingle`, `ReadDouble`, `ReadReal` (`float`/binary32, matching the engine's 2D numerics).
- Binary/text reads: `ReadBytes(int)`, `ReadString(int)`, `ReadLine`, `ReadCsvLine(char = ',')`, `ReadPascalString`, `ReadAllText(bool = false)`.
- Numeric writes: `WriteByte`, `WriteUInt16`, `WriteUInt32`, `WriteUInt64`, `WriteHalf`, `WriteSingle`, `WriteDouble`, `WriteReal` (`float`/binary32).
- Binary/text writes: `WriteBytes(ReadOnlySpan<byte>)`, `WriteString`, `WriteLine`, `WriteCsvLine(IEnumerable<string>, char = ',')`, `WritePascalString`.

Fixed-width and Pascal reads require complete values and throw `EndOfStreamException`; `ReadBytes` instead returns the available prefix. UTF-8 is strict. `ReadLine` recognizes LF, CR, CRLF, and null terminators without returning them; writes use LF. CSV supports quoted delimiters, escaped quotes, and quoted multiline fields. `ReadAllText` preserves the cursor even if decoding fails and can optionally remove CR bytes.

### Static file operations

`FileExists`, `GetFileAsBytes`, `GetFileAsString`, `GetAccessTime`, `GetModifiedTime`, `GetSize`, `GetMd5`, `GetSha256`, `IsHidden`, `SetHidden`, `IsReadOnly`, `SetReadOnly`, `GetUnixPermissions`, and `SetUnixPermissions` resolve ordinary, `res://`, and `user://` paths through the process `ProjectSettings` registry. Hidden/read-only attributes are available only on Apple, BSD, and Windows platforms; Linux callers use Unix permissions rather than receiving a simulated result. MD5 is compatibility/integrity-only; SHA-256 is available for stronger content identity.

### Extended attributes

Static `GetExtendedAttribute(path, name)`, `GetExtendedAttributeString(path, name)`, `GetExtendedAttributesList(path)`, `SetExtendedAttribute(path, name, value)`, `SetExtendedAttributeString(path, name, value)`, and `RemoveExtendedAttribute(path, name)` operate on arbitrary byte or strict UTF-8 values. Public names omit platform namespace syntax. Linux uses the `user` namespace, macOS uses native xattrs, and Windows uses alternate data streams. Support still depends on the physical filesystem.

The complete inherited `ElectronObject` identity, notification, typed-property, localization, and disposal API is documented in [`ElectronObject`](ElectronObject.md); `FileAccess` adds no override hooks beyond its protected disposal implementation.

## Lifecycle and state transitions

1. A static open validates path, exact mode, codec/key/password, and existing-file requirements before returning ownership.
2. Raw files operate directly. Compressed/encrypted files decode fully into a seekable memory stream before the instance is published.
3. Writes mark transformed state dirty. `Flush` encodes a complete snapshot and atomically replaces the physical destination; the in-memory cursor is restored.
4. `Close` commits when writable, releases/zeroes memory, removes a disposable temporary file, and enters the closed state. `Dispose` then enters the inherited terminal state.

Closed instances retain path diagnostics and reject stream operations with `InvalidOperationException`. Disposed instances reject guarded operations with `ObjectDisposedException`.

## Invariants and error behavior

- Only the four exact `FileAccessMode` values are accepted.
- Read and write permissions are enforced independently of the underlying in-memory stream's technical capabilities.
- Write modes truncate; read-write mode requires and preserves an existing file.
- No destination directory is implicitly created.
- Endianness affects every multi-byte numeric value and the Pascal byte-count prefix.
- Compression envelopes record version, codec, and decoded length; malformed, mismatched, or truncated envelopes fail before publication.
- Raw encryption requires exactly 32 key bytes. Password mode uses a random 16-byte salt, 600,000 PBKDF2-HMAC-SHA-256 iterations, and a 32-byte derived key.
- AES-256-GCM uses a fresh 12-byte nonce, a 16-byte tag, and authenticates the complete envelope header. Wrong credentials, mode confusion, and tampering fail before plaintext exposure.
- Atomic transformed commits use a flushed same-directory temporary file and replacement. Failure preserves any previous destination but cannot promise crash durability on every filesystem.
- Unknown virtual schemes and lexical root traversal are rejected by `ProjectSettings`.

Filesystem, authorization, decoder, compression, cryptographic, and disposed/closed failures are reported with standard typed C# exceptions. Numeric error-return state and a process-wide last-open error are intentionally absent.

## Threading and re-entrancy

All instance state/stream operations are serialized by one private lock. One call is atomic with respect to other calls, including Pascal prefix+payload operations and transformed flush. A caller-owned sequence such as `Seek` followed by `ReadBytes` is not atomic against another caller unless externally synchronized. Static operations own only local state and are safe concurrently. Blocking I/O, hashing, compression, PBKDF2, and allocation make the component unsuitable for frame, physics, audio, or render hot paths.

## Dependencies and interactions

- Inherits `ElectronObject` for deterministic lifetime and disposed-state ordering.
- Uses `ProjectSettings.Instance.GlobalizePath` for directory-backed virtual paths.
- Uses .NET file, UTF-8, hash, compression, PBKDF2, random, and AES-GCM primitives.
- Reuses the same internal atomic-replacement helper as `ConfigFile`.
- Uses Linux libc xattrs, macOS libSystem xattrs, and Windows alternate data streams for the extended-attribute surface.
- Has no SDL, Scene, renderer, resource loader, scripting, editor, physics, or external package dependency.

## Reference API coverage audit

| Reference contract area | Electron2D classification |
| --- | --- |
| Mode/compression/Unix-permission enums | Implemented with exact reference numeric values |
| Open, close, flush, seek, seek-end, resize, cursor, length, EOF, path, absolute path, open state | Implemented; failures use exceptions |
| Unsigned integers, half/float/double/real, buffers, raw/line/Pascal text, CSV, whole-text snapshot, endian toggle | Implemented with typed C# names and strict UTF-8 |
| Static existence, bytes/text snapshot, timestamps, size, MD5, SHA-256 | Implemented |
| Hidden/read-only/Unix permissions | Implemented as instance properties and static helpers; unsupported platforms throw instead of simulating attributes |
| Extended attributes | Implemented as static path operations on Linux, macOS, and Windows; Linux namespace prefixes are intentionally hidden |
| Temporary files | Implemented with keep/delete ownership |
| DEFLATE, GZip, Brotli compressed access | Implemented in a versioned Electron2D envelope |
| FastLZ and Zstandard compressed access | Deferred until an accepted codec dependency exists; known modes throw before side effects |
| Key/password encrypted access | Implemented as authenticated Electron2D envelopes; the optional caller-supplied IV is intentionally excluded to prevent nonce reuse |
| `get_error` and static last-open error | Permanently adapted to C# exceptions; there is no shared error slot |
| `get_var` / `store_var` | Permanently excluded by ADR 0001; no universal-value replacement is exposed |
| Reference-counted inheritance | Permanently adapted to managed memory plus `ElectronObject`/`IDisposable` |
| Packed/exported archive-backed `res://` | Deferred until a pack-mount/resource-loader domain exists; directory-backed virtual paths are implemented |
| `uid://` resource identities and `pipe://` streams | Deferred until resource-UID and platform-pipe domains exist; unknown schemes fail explicitly |

## Verification and known limits

`tests/Electron2D.Tests/Program.cs` covers invalid paths/modes, all four access modes, truncation/preservation, little/big-endian primitives, half/float/double/real, buffers, strict UTF-8, CR/LF/null lines, CSV/Pascal strings, EOF and seek transitions, resize, cursor preservation on decode failure, close/dispose, static metadata/hashes and missing-file errors, explicit unsupported Linux hidden/read-only attributes, Unix permissions, Linux xattrs, temporary ownership including prefix/extension/read-only mode, three codecs, malformed/mismatched compression, raw/password encryption, wrong keys/passwords/mode confusion, tampering, virtual project paths, concurrent static hashing and compound Pascal writes, and commit failure cleanup.

The checks ran on Linux with its active filesystem. They do not prove the implemented macOS xattr or Windows alternate-stream backends, and no Android/iOS extended-attribute backend or native-host verification is currently claimed. They also do not prove every filesystem's crash durability, very-large-file memory behavior for transformed whole-file access, hostile TOCTOU races, or protection after process compromise. FastLZ, Zstandard, and packed archives remain explicitly unimplemented.

## Decisions

- [0001: Typed C# without Variant](../decisions/0001-typed-csharp-without-variant.md)
- [0003: ElectronObject lifetime](../decisions/0003-electron-object-lifetime.md)
- [0014: Managed lifetime and realtime allocation](../decisions/0014-managed-resource-lifetime.md)
- [0019: Typed project settings and directory-backed virtual paths](../decisions/0019-typed-project-settings.md)
- [0020: Typed file access and transformed-file containers](../decisions/0020-file-access.md)
